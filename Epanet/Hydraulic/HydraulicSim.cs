/*
 * Copyright (C) 2016 Vyacheslav Shevelyov (slavash at aha dot ru)
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see http://www.gnu.org/licenses/.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using Epanet.Enums;
using Epanet.Hydraulic.IO;
using Epanet.Hydraulic.Models;
using Epanet.Hydraulic.Structures;
using Epanet.Log;
using Epanet.Network.Structures;
using Epanet.Properties;
using Epanet.Util;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet.Hydraulic {

    ///<summary>Hydraulic simulation class.</summary>
    public class HydraulicSim {
        private const int BUFFER_SIZE = 512 * 1024; //512kb
        [NonSerialized]
        private bool _running;
        [NonSerialized]
        private Thread _runningThread;

        ///<summary>Event logger reference.</summary>
        private readonly TraceSource _logger;


        private readonly SimulationNode[] _nodes;
        private readonly SimulationLink[] _links;
        private readonly List<SimulationPump> _pumps;
        private readonly List<SimulationTank> _tanks;
        private readonly List<SimulationNode> _junctions;
        private readonly List<SimulationValve> _valves;

        private readonly Curve[] _curves;

        ///<summary>Simulation properties.</summary>
        private readonly EpanetNetwork _net;

        ///<summary>Energy cost time pattern.</summary>
        private readonly Pattern _epat;

        ///<summary>Linear system solver support class.</summary>
        private readonly SparseMatrix _smat;

        ///<summary>Linear system variable storage class.</summary>
        private readonly LsVariables _lsv;

        ///<summary>System wide demand.</summary>
        private double _dsystem;

        ///<summary>Output stream of the hydraulic solution.</summary>
        private BinaryWriter _writer;

        ///<summary>Pipe headloss model calculator.</summary>
        private readonly PipeHeadModelCalculator _pHlModel;

        ///<summary>Get current hydraulic simulation time.</summary>
        public TimeSpan Htime { get; private set; }

        ///<summary>Get current report time.</summary>
        public TimeSpan Rtime { get; private set; }

        protected EpanetNetwork Network => _net;

        ///<summary>Init hydraulic simulation, preparing the linear solver and the hydraulic structures wrappers.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        /// <param name="log">Logger reference.</param>
        public HydraulicSim(EpanetNetwork net, TraceSource log) {
            _running = false;
            _logger = log;
            // CreateSimulationNetwork(net);


            _nodes = new SimulationNode[net.Nodes.Count];
            _links = new SimulationLink[net.Links.Count];
            _pumps = new List<SimulationPump>();
            _tanks = new List<SimulationTank>();
            _junctions = new List<SimulationNode>();
            _valves = new List<SimulationValve>();

            var nodesById = new Dictionary<string, SimulationNode>(net.Nodes.Count);

            for(int i = 0; i < net.Nodes.Count; i++) {
                SimulationNode node;

                var networkNode = net.Nodes[i];

                if (networkNode.NodeType == NodeType.JUNC) {
                    node = new SimulationNode(networkNode, i);
                    _junctions.Add(node);
                }
                else {
                    node = new SimulationTank(networkNode, i);
                    _tanks.Add((SimulationTank)node);
                }

                _nodes[i] = node;
                nodesById[node.Id] = node;
            }

            for(int i = 0; i < net.Links.Count; i++) {
                SimulationLink link;
                var networkLink = net.Links[i];

                switch (networkLink.LinkType) {
                    case LinkType.VALVE:
                        var valve = new SimulationValve(nodesById, networkLink, i);
                        _valves.Add(valve);
                        link = valve;
                        break;

                    case LinkType.PUMP:
                        var pump = new SimulationPump(nodesById, networkLink, i);
                        _pumps.Add(pump);
                        link = pump;
                        break;

                    default:
                        link = new SimulationLink(nodesById, networkLink, i);
                        break;
                }

                _links[i] = link;

            }

            Rules = net.Rules.Select(r => new SimulationRule(r, _links, _nodes)).ToArray();
            _curves = net.Curves.ToArray();
            Controls = net.Controls.Select(x => new SimulationControl(_nodes, _links, x)).ToArray();


            _net = net;
            _epat = net.GetPattern(_net.EPatId);
            _smat = new SparseMatrix(_nodes, _links, _junctions.Count);
            _lsv = new LsVariables(_nodes.Length, _smat.CoeffsCount);

            Htime = TimeSpan.Zero;

            switch (_net.FormFlag) {

            case FormType.HW:
                _pHlModel = PipeHeadModelCalculators.HwModelCalculator;
                break;
            case FormType.DW:
                _pHlModel = PipeHeadModelCalculators.DwModelCalculator;
                break;
            case FormType.CM:
                _pHlModel = PipeHeadModelCalculators.CmModelCalculator;
                break;
            }

            foreach(SimulationLink link in _links)
                link.InitLinkFlow();

            foreach (SimulationNode node in _junctions)
                if (node.Ke > 0.0)
                    node.SimEmitter = 1.0;

            foreach(SimulationLink link in _links) {

                if (link.LinkType == LinkType.VALVE) {
                    var valveType = ((Valve)link.Link).ValveType;
                    if (valveType == ValveType.PRV || valveType == ValveType.PSV || valveType == ValveType.FCV) {
                        if (!double.IsNaN(link.Roughness)) {
                            link.SimStatus = StatType.ACTIVE;
                        }
                    }
                }


                if(link.SimStatus <= StatType.CLOSED)
                    link.SimFlow = Constants.QZERO;
                else if(Math.Abs(link.SimFlow) <= Constants.QZERO)
                    link.InitLinkFlow(link.SimStatus, link.SimSetting);

                link.SimOldStatus = link.SimStatus;
            }


            Htime = TimeSpan.Zero;
            Rtime = _net.RStep;

        }
        
        ///<summary>Run hydraulic simuation.</summary>
        /// <param name="hyd">File name where the output of the hydraulic simulation will be writen.</param>
        public void Simulate(string hyd) {
            FileStream @out = null;

            try {
                @out = new FileStream(
                    hyd,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read,
                    BUFFER_SIZE);

                Simulate(new BinaryWriter(@out));
            }
            catch (IOException) {
                throw new EnException(ErrorCode.Err305);
            }
            finally {
                @out?.Close();
            }
        }


        ///<summary>Run hydraulic simuation.</summary>
        /// <param name="out">Output stream for the hydraulic simulation data.</param>
        public void Simulate(Stream @out) { Simulate(new BinaryWriter(@out)); }

        public TimeSpan SimulateSingleStep() {

            if (!_running)
                _running = true;

            if (!RunHyd()) {
                _running = false;

                return TimeSpan.Zero;
            }

            TimeSpan hydstep = TimeSpan.Zero;

            if (Htime < _net.Duration)
                hydstep = TimeStep();

            if (_net.Duration.IsZero())
                SimulationPump.StepEnergy(_net, _epat, _pumps, Htime, TimeSpan.Zero);
            else if (Htime < _net.Duration)
                SimulationPump.StepEnergy(_net, _epat, _pumps, Htime, hydstep);

            if (Htime < _net.Duration) {
                Htime += hydstep;
                if (Htime >= Rtime)
                    Rtime += _net.RStep;
            }

            TimeSpan tstep = hydstep;

            if(!_running && tstep > TimeSpan.Zero) {
                _running = false;
                return TimeSpan.Zero;
            }

            if(_running && tstep > TimeSpan.Zero)
                return tstep;

            _running = false;
            return TimeSpan.Zero;
        }

        public void Simulate(BinaryWriter @out) {
            bool halted = false;

            if (_running)
                throw new InvalidOperationException("Already running");

            _runningThread = Thread.CurrentThread;
            _running = true;

            _writer = @out;
            if (_writer != null)
                AwareStep.WriteHeader(@out, this, _net.RStart, _net.RStep, _net.Duration);

//        writeHeader(simulationOutput);
            try {
                TimeSpan tstep;
                do {
                    if (!RunHyd())
                        break;

                    tstep = NextHyd();

                    if (!_running && tstep > TimeSpan.Zero)
                        halted = true;
                }
                while(_running && tstep > TimeSpan.Zero);
            }
            catch (IOException e) {
                Debug.Print(e.ToString());
                throw new EnException(ErrorCode.Err1000);
            }
            finally {
                _running = false;
                _runningThread = null;
            }

            if (halted)
                throw new EnException(ErrorCode.Err1000);

        }

        ///<summary>Halt hydraulic simulation.</summary>
        public void StopRunning() {
            _running = false;
            if (_runningThread != null && _runningThread.IsAlive)
                _runningThread.Join(1000);
        }

        ///<summary>Solves network hydraulics in a single time period.</summary>
        private bool RunHyd() {

            // Find new demands & control actions
            ComputeDemands();
            ComputeControls();

            // Solve network hydraulic equations
            NetSolve(out int iter, out double relerr);

            // Report new status & save results
            if (_net.StatFlag != StatFlag.NO)
                LogHydStat(iter, relerr);

            // If system unbalanced and no extra trials
            // allowed, then activate the Haltflag.
            if (relerr > _net.HAcc && _net.ExtraIter == -1) {
                Htime = _net.Duration;
                return false;
            }

            LogHydWarn(iter, relerr);

            return true;
        }

        ///<summary>Solve the linear equation system to compute the links flows and nodes heads.</summary>
        /// <returns>Solver steps and relative error.</returns>
        protected void NetSolve(out int iter, out double relerr) {
            iter = 0;
            relerr = 0.0;

            int nextCheck = _net.CheckFreq;

            if (_net.StatFlag == StatFlag.FULL)
                LogRelErr(iter, relerr);

            int maxTrials = _net.MaxIter;

            if (_net.ExtraIter > 0)
                maxTrials += _net.ExtraIter;

            double relaxFactor = 1.0;
            int errcode = 0;
            iter = 1;

            while (iter <= maxTrials) {
                //Compute coefficient matrices A & F and solve A*H = F
                // where H = heads, A = Jacobian coeffs. derived from
                // head loss gradients, & F = flow correction terms.
                NewCoeffs();

                //dumpMatrixCoeffs(new File("dumpMatrix.txt"),true);

                // Solution for H is returned in F from call to linsolve().
                errcode = _smat.LinSolve(_junctions.Count, _lsv.aii, _lsv.aij, _lsv.f);

                // Ill-conditioning problem
                if (errcode > 0) {
                    // If control valve causing problem, fix its status & continue,
                    // otherwise end the iterations with no solution.
                    if (SimulationValve.CheckBadValve(_net, _logger, _valves, Htime, _smat.GetOrder(errcode)))
                        continue;

                    break;
                }

                // Update current solution.
                // (Row[i] = row of solution matrix corresponding to node i).
                foreach (SimulationNode node  in  _junctions) {
                    node.SimHead = _lsv.f[_smat.GetRow(node.Index)];
                }

                // Update flows
                relerr = NewFlows(relaxFactor);

                // Write convergence error to status report if called for
                if (_net.StatFlag == StatFlag.FULL)
                    LogRelErr(iter, relerr);

                relaxFactor = 1.0;

                bool valveChange = false;

                //  Apply solution damping & check for change in valve status
                if (_net.DampLimit > 0.0) {
                    if (relerr <= _net.DampLimit) {
                        relaxFactor = 0.6;
                        valveChange = SimulationValve.ValveStatus(_net, _logger, _valves);
                    }
                }
                else
                    valveChange = SimulationValve.ValveStatus(_net, _logger, _valves);

                // Check for convergence
                if (relerr <= _net.HAcc) {

                    //  We have convergence. Quit if we are into extra iterations.
                    if (iter > _net.MaxIter)
                        break;

                    //  Quit if no status changes occur.
                    bool statChange = valveChange || 
                                      SimulationLink.LinkStatus(_net, _logger, _links) ||
                                      SimulationControl.PSwitch(_logger, _net, Controls);

                    if (!statChange)
                        break;

                    //  We have a status change so continue the iterations
                    nextCheck = iter + _net.CheckFreq;
                }
                else if (iter <= _net.MaxCheck && iter == nextCheck) {
                    // No convergence yet. See if its time for a periodic status
                    // check  on pumps, CV's, and pipes connected to tanks.
                    SimulationLink.LinkStatus(_net, _logger, _links);
                    nextCheck += _net.CheckFreq;
                }

                iter++;
            }


            foreach (SimulationNode node  in  _junctions)
                node.SimDemand = node.SimDemand + node.SimEmitter;

            if (errcode > 0) {
                LogHydErr(_smat.GetOrder(errcode));
                //errcode = 110;
                return;
            }

            if (errcode != 0)
                throw new EnException((ErrorCode)errcode);
        }



        ///<summary>Computes coefficients of linearized network eqns.</summary>
        private void NewCoeffs() {
            
            _lsv.Clear();

            foreach (SimulationLink link  in  _links) {
                link.SimFlowCorrection = link.SimInvHeadLoss = 0;
            }

            // Compute link coeffs.
            foreach (SimulationLink link in _links)
                link.ComputeMatrixCoeff(_net, _pHlModel, _curves, _smat, _lsv);

            // Compute emitter coeffs.
            foreach (SimulationNode node in _junctions)
                node.ComputeEmitterCoeff(_net, _smat, _lsv);

            // Compute node coeffs.
            foreach (SimulationNode node in _junctions)
                node.ComputeNodeCoeff(_smat, _lsv);

            // Compute valve coeffs.
            foreach (SimulationValve valve  in  _valves)
                valve.ComputeMatrixCoeff(_net, _lsv, _smat);

        }

        ///<summary>Updates link flows after new nodal heads computed.</summary>
        private double NewFlows(double relaxFactor) {

            foreach (SimulationTank node  in  _tanks)
                node.SimDemand = 0;

            double qsum = 0.0;
            double dqsum = 0.0;

            foreach (SimulationLink link  in  _links) {
                SimulationNode n1 = link.First;
                SimulationNode n2 = link.Second;

                double dh = n1.SimHead - n2.SimHead;
                double dq = link.SimFlowCorrection - link.SimInvHeadLoss * dh;

                dq *= relaxFactor;

                if (link is SimulationPump pump &&
                    pump.Ptype == PumpType.CONST_HP && 
                    dq > pump.SimFlow)
                {
                    dq = pump.SimFlow / 2.0;
                }

                link.SimFlow = link.SimFlow - dq;

                qsum += Math.Abs(link.SimFlow);
                dqsum += Math.Abs(dq);

                if (link.SimStatus > StatType.CLOSED) {
                    if (n1 is SimulationTank)
                        n1.SimDemand = n1.SimDemand - link.SimFlow;
                    if (n2 is SimulationTank)
                        n2.SimDemand = n2.SimDemand + link.SimFlow;
                }
            }

            foreach (SimulationNode node  in  _junctions) {

                if (node.Ke.IsZero())
                    continue;
                double dq = node.EmitFlowChange(_net);
                node.SimEmitter = node.SimEmitter - dq;
                qsum += Math.Abs(node.SimEmitter);
                dqsum += Math.Abs(dq);
            }

            return qsum > _net.HAcc 
                ? dqsum / qsum
                : dqsum;
        }

        ///<summary>Implements simple controls based on time or tank levels.</summary>
        private void ComputeControls() {
            SimulationControl.StepActions(_logger, _net, Controls, Htime);
        }

        ///<summary>Computes demands at nodes during current time period.</summary>
        private void ComputeDemands() {
            // Determine total elapsed number of pattern periods
            int nsteps = (int)((Htime + _net.PStart).Ticks / _net.PStep.Ticks);

            _dsystem = 0.0; //System-wide demand

            // Update demand at each node according to its assigned pattern
            foreach (SimulationNode node  in  _junctions) {
                double sum = 0.0;
                foreach (Demand demand  in  node.Demand) {
                    // pattern period (k) = (elapsed periods) modulus (periods per pattern)
                    var pat = demand.Pattern;

                    int k = nsteps % pat.Count;
                    double djunc = demand.Base * pat[k] * _net.DMult;

                    if (djunc > 0.0)
                        _dsystem += djunc;

                    sum += djunc;
                }
                node.SimDemand = sum;
            }

            // Update head at fixed grade nodes with time patterns
            foreach (SimulationTank tank  in  _tanks.Where(x => x.IsReservoir && x.Pattern != null)) {
                int k = nsteps % tank.Pattern.Count;
                tank.SimHead = tank.Elevation * tank.Pattern[k];
            }

            // Update status of pumps with utilization patterns
            foreach (SimulationPump pump  in  _pumps.Where(x => x.Upat !=null)) {
                var pat = pump.Upat;
                int k = nsteps % pat.Count;
                pump.SetLinkSetting(pat[k]);
            }
        }

        ///<summary>Finds length of next time step & updates tank levels and rule-based contol actions.</summary>
        protected virtual TimeSpan NextHyd() {
            TimeSpan hydstep = TimeSpan.Zero;

            if (_writer != null)
                AwareStep.Write(_writer, this, Htime);

            if (Htime < _net.Duration)
                hydstep = TimeStep();

            if(_net.Duration.IsZero())
                SimulationPump.StepEnergy(_net, _epat, _pumps, Htime, TimeSpan.Zero);
            else if (Htime < _net.Duration)
                SimulationPump.StepEnergy(_net, _epat, _pumps, Htime, hydstep);

            if (Htime < _net.Duration) {
                Htime += hydstep;
                if (Htime >= Rtime)
                    Rtime += _net.RStep;
            }

            return hydstep;
        }

        ///<summary>Computes time step to advance hydraulic simulation.</summary>
        private TimeSpan TimeStep() {
            TimeSpan tstep = _net.HStep;

            /* Revise time step based on time until next demand period */
            // FIXME: simplify
            long n = ((Htime + _net.PStart).Ticks / _net.PStep.Ticks) + 1; /* Next pattern period   */
            TimeSpan t = new TimeSpan(n * _net.PStep.Ticks) - Htime;      /* Time till next period */

            if (t > TimeSpan.Zero && t < tstep)
                tstep = t;

            // Revise time step based on smallest time to fill or drain a tank
            t = Rtime - Htime;
            if(t > TimeSpan.Zero && t < tstep)
                tstep = t;

            /* Revise time step based on smallest time to fill or drain a tank */
            tstep = SimulationTank.MinimumTimeStep(_tanks, tstep);

            /* Revise time step based on smallest time to activate a control */
            tstep = SimulationControl.MinimumTimeStep(_net, Controls, Htime, tstep);

            /* Evaluate rule-based controls (which will also update tank levels) */

            if (Rules.Length > 0) {
                SimulationRule.MinimumTimeStep(
                    _net,
                    _logger,
                    Rules,
                    _tanks,
                    Htime,
                    tstep,
                    _dsystem,
                    out TimeSpan step,
                    out TimeSpan htime);

                tstep = step;
                Htime = htime;
            }
            else
                SimulationTank.StepWaterLevels(_tanks, _net.FieldsMap, tstep);

            return tstep;
        }


        ///<summary>Save current step simulation results in the temp hydfile.</summary>
        private void SaveStep() {
            /*
            int capacity = Links.Count * 3 * sizeof(float) + Nodes.Count * 2 * sizeof(float) + sizeof(int);
            BinaryWriter bb = new BinaryWriter(new MemoryStream(capacity));
            */

            BinaryWriter bb = _writer;

            try {
                _writer.Write((int)Htime.TotalSeconds);

                foreach (SimulationNode node  in  _nodes)
                    bb.Write((float)node.SimDemand);

                foreach (SimulationNode node  in  _nodes)
                    bb.Write((float)node.SimHead);

                foreach (SimulationLink link  in  _links)
                    bb.Write(link.SimStatus > StatType.CLOSED ? (float)link.SimFlow : 0f);

                foreach (SimulationLink link  in  _links)
                    bb.Write((int)link.SimStatus);

                foreach (SimulationLink link  in  _links)
                    bb.Write((float)link.SimSetting);
                

            }
            catch (IOException ex) {
                throw new EnException(ErrorCode.Err308, ex);
            }
        }

        /// <summary>Report hydraulic warning.</summary>
        /// <remarks>
        /// Note: Warning conditions checked in following order:
        /// <list type="number">
        /// <item>1. System balanced but unstable</item>
        /// <item>2. Negative pressures</item>
        /// <item>3. FCV cannot supply flow or PRV/PSV cannot maintain pressure</item>
        /// <item>4. Pump out of range</item>
        /// <item>5. Network disconnected</item>
        /// <item>6. System unbalanced</item> 
        /// </list>
        /// </remarks>
        private int LogHydWarn(int iter, double relerr) {
            int flag = 0;

            try {
              

                string atime = Htime.GetClockTime();

                if (iter > _net.MaxIter && relerr <= _net.HAcc) {
                    if (_net.MessageFlag)
                        _logger.Warning(Error.WARN02, atime);
                    flag = 2;
                }

                // Check for negative pressures
                foreach (SimulationNode node  in  _junctions) {
                    if (node.SimHead < node.Elevation && node.SimDemand > 0.0) {
                        if (_net.MessageFlag)
                            _logger.Warning(Error.WARN06, atime);
                        flag = 6;
                        break;
                    }
                }

                // Check for abnormal valve condition
                foreach (SimulationValve valve  in  _valves) {
                    if (valve.SimStatus >= StatType.XFCV) {
                        if (_net.MessageFlag)
                            _logger.Warning(
                                    Error.WARN05,
                                    valve.ValveType.Keyword2(),
                                    valve.Link.Name,
                                    valve.SimStatus.ReportStr(),
                                    atime);
                        flag = 5;
                    }
                }

                // Check for abnormal pump condition
                foreach (SimulationPump pump  in  _pumps) {
                    StatType s = pump.SimStatus;
                    if (pump.SimStatus >= StatType.OPEN) {
                        if (pump.SimFlow > pump.SimSetting * pump.Qmax)
                            s = StatType.XFLOW;
                        if (pump.SimFlow < 0.0)
                            s = StatType.XHEAD;
                    }

                    if (s == StatType.XHEAD || s == StatType.XFLOW) {
                        if (_net.MessageFlag)
                            _logger.Warning(
                                    Error.WARN04,
                                    pump.Link.Name,
                                    pump.SimStatus.ReportStr(),
                                    atime);
                        flag = 4;
                    }
                }

                // Check if system is unbalanced
                if (iter > _net.MaxIter && relerr > _net.HAcc) {
                    string str = string.Format(Error.WARN01, atime);

                    if (_net.ExtraIter == -1)
                        str += Keywords.t_HALTED;

                    if (_net.MessageFlag)
                        _logger.Warning(str);

                    flag = 1;
                }
            }
            catch (EnException) {}

            return flag;
        }

        /// <summary>Report hydraulic status.</summary>
        private void LogHydStat(int iter, double relerr) {
            try {
                string atime = Htime.GetClockTime();
                if (iter > 0) {
                    if (relerr <= _net.HAcc)
                        _logger.Warning(Text.FMT58, atime, iter);
                    else
                        _logger.Warning(Text.FMT59, atime, iter, relerr);
                }

                foreach (SimulationTank tank  in  _tanks) {
                    StatType newstat;

                    if (Math.Abs(tank.SimDemand) < 0.001)
                        newstat = StatType.CLOSED;
                    else if (tank.SimDemand > 0.0)
                        newstat = StatType.FILLING;
                    else if (tank.SimDemand < 0.0)
                        newstat = StatType.EMPTYING;
                    else
                        newstat = tank.OldStat;

                    if (newstat != tank.OldStat) {
                        if (!tank.IsReservoir)
                            _logger.Warning(
                                    Text.FMT50,
                                    atime,
                                    tank.Id,
                                    newstat.ReportStr(),
                                    (tank.SimHead - tank.Elevation) * _net.FieldsMap.GetUnits(FieldType.HEAD),
                                    _net.FieldsMap.GetField(FieldType.HEAD).Units);

                        else
                            _logger.Warning(
                                    Text.FMT51,
                                    atime,
                                    tank.Id,
                                    newstat.ReportStr());

                        tank.OldStat = newstat;
                    }
                }

                foreach (SimulationLink link  in  _links) {
                    if (link.SimStatus != link.SimOldStatus) {
                        if(Htime.IsZero())
                            _logger.Warning(
                                    Text.FMT52,
                                    atime,
                                    link.LinkType.Keyword2(),
                                    link.Link.Name,
                                    link.SimStatus.ReportStr());
                        else
                            _logger.Warning(
                                    Text.FMT53,
                                    atime,
                                    link.LinkType.Keyword2(),
                                    link.Link.Name,
                                    link.SimOldStatus.ReportStr(),
                                    link.SimStatus.ReportStr());

                        link.SimOldStatus = link.SimStatus;
                    }
                }
            }
            catch (EnException) {}
        }

        private void LogRelErr(int iter, double relerr) {
            if (iter == 0) {
                _logger.Warning(Text.FMT64, Htime.GetClockTime());
            }
            else {
                _logger.Warning(Text.FMT65, iter, relerr);
            }
        }

        private void LogHydErr(int order) {
            try {
                if (_net.MessageFlag)
                    _logger.Warning(
                            Text.FMT62,
                            Htime.GetClockTime(),
                            _nodes[order].Id);
            }
            catch (EnException) {}
            LogHydStat(0, 0.0);
        }

        public IList<SimulationNode> Nodes => _nodes;

        public IList<SimulationLink> Links => _links;

        public SimulationRule[] Rules { get; }

        public SimulationControl[] Controls { get; }
    }

}