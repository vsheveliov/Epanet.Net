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


        private readonly SimulationControl[] _controls;
        private readonly SimulationRule[] _rules;
        private readonly Curve[] _curves;

        ///<summary>Simulation properties.</summary>
        protected readonly EpanetNetwork net;

        ///<summary>Energy cost time pattern.</summary>
        private readonly Pattern _epat;

        ///<summary>Linear system solver support class.</summary>
        private readonly SparseMatrix _smat;

        ///<summary>Linear system variable storage class.</summary>
        private readonly LsVariables _lsv;

        ///<summary>System wide demand.</summary>
        private double _dsystem;

        ///<summary>Output stream of the hydraulic solution.</summary>
        private BinaryWriter _simulationOutput;

        ///<summary>Pipe headloss model calculator.</summary>
        private readonly PipeHeadModelCalculators.Compute _pHlModel;

        ///<summary>Get current hydraulic simulation time.</summary>
        public long Htime { get; private set; }

        ///<summary>Get current report time.</summary>
        public long Rtime { get; private set; }

        ///<summary>Init hydraulic simulation, preparing the linear solver and the hydraulic structures wrappers.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        /// <param name="log">Logger reference.</param>
        public HydraulicSim(EpanetNetwork net, TraceSource log) {
            _running = false;
            _logger = log;
            // this.CreateSimulationNetwork(net);


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

                if (networkNode.Type == NodeType.JUNC) {
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

                if (networkLink is Valve) {
                    var valve = new SimulationValve(nodesById, networkLink, i);
                    _valves.Add(valve);
                    link = valve;
                }
                else if (networkLink is Pump) {
                    var pump = new SimulationPump(nodesById, networkLink, i);
                    _pumps.Add(pump);
                    link = pump;
                }
                else {
                    link = new SimulationLink(nodesById, networkLink, i);
                }
                
                _links[i] = link;

            }

            _rules = net.Rules.Select(r => new SimulationRule(r, _links, _nodes)).ToArray();

            _curves = net.Curves.ToArray();

            _controls = net.Controls.Select(x => new SimulationControl(_nodes, _links, x)).ToArray();


            this.net = net;
            _epat = net.GetPattern(this.net.EPatId);
            _smat = new SparseMatrix(_nodes, _links, _junctions.Count);
            _lsv = new LsVariables(_nodes.Length, _smat.CoeffsCount);

            Htime = 0;

            switch (this.net.FormFlag) {

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

                if((link.Type == LinkType.PRV ||
                     link.Type == LinkType.PSV ||
                     link.Type == LinkType.FCV)
                    && !double.IsNaN(link.Roughness))
                    link.SimStatus = StatType.ACTIVE;


                if(link.SimStatus <= StatType.CLOSED)
                    link.SimFlow = Constants.QZERO;
                else if(Math.Abs(link.SimFlow) <= Constants.QZERO)
                    link.InitLinkFlow(link.SimStatus, link.SimSetting);

                link.SimOldStatus = link.SimStatus;
            }


            Htime = 0;
            Rtime = this.net.RStep;

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
                throw new ENException(ErrorCode.Err305);
            }
            finally {
                if (@out != null)
                    @out.Close();
            }
        }


        ///<summary>Run hydraulic simuation.</summary>
        /// <param name="out">Output stream for the hydraulic simulation data.</param>
        public void Simulate(Stream @out) { Simulate(new BinaryWriter(@out)); }

        public long SimulateSingleStep() {

            if (!_running)
                _running = true;

            if (!RunHyd()) {
                _running = false;
                return 0;
            }

            long hydstep = 0;

            if (Htime < net.Duration)
                hydstep = TimeStep();

            if (net.Duration == 0)
                SimulationPump.StepEnergy(net, _epat, _pumps, Htime, 0);
            else if (Htime < net.Duration)
                SimulationPump.StepEnergy(net, _epat, _pumps, Htime, hydstep);

            if (Htime < net.Duration) {
                Htime += hydstep;
                if (Htime >= Rtime)
                    Rtime += net.RStep;
            }

            long tstep = hydstep;

            if (!_running && tstep > 0) {
                _running = false;
                return 0;
            }

            if (_running && tstep > 0)
                return tstep;
            else {
                _running = false;
                return 0;
            }
        }

        public void Simulate(BinaryWriter @out) {
            bool halted = false;

            if (_running)
                throw new InvalidOperationException("Already running");

            _runningThread = Thread.CurrentThread;
            _running = true;

            _simulationOutput = @out;
            if (_simulationOutput != null)
                AwareStep.WriteHeader(@out, this, net.RStart, net.RStep, net.Duration);

//        writeHeader(simulationOutput);
            try {
                long tstep;
                do {
                    if (!RunHyd())
                        break;

                    tstep = NextHyd();

                    if (!_running && tstep > 0)
                        halted = true;
                }
                while (_running && tstep > 0);
            }
            catch (IOException e) {
                Debug.Print(e.ToString());
                throw new ENException(ErrorCode.Err1000);
            }
            finally {
                _running = false;
                _runningThread = null;
            }

            if (halted)
                throw new ENException(ErrorCode.Err1000);

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
            double relerr;
            int iter;
            NetSolve(out iter, out relerr);

            // Report new status & save results
            if (net.StatFlag != StatFlag.NO)
                LogHydStat(iter, relerr);

            // If system unbalanced and no extra trials
            // allowed, then activate the Haltflag.
            if (relerr > net.HAcc && net.ExtraIter == -1) {
                Htime = net.Duration;
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

            int nextCheck = net.CheckFreq;

            if (net.StatFlag == StatFlag.FULL)
                LogRelErr(iter, relerr);

            int maxTrials = net.MaxIter;

            if (net.ExtraIter > 0)
                maxTrials += net.ExtraIter;

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
                errcode = _smat.LinSolve(
                                  _junctions.Count,
                                  _lsv.AiiVector,
                                  _lsv.AijVector,
                                  _lsv.RhsCoeffs);

                // Ill-conditioning problem
                if (errcode > 0) {
                    // If control valve causing problem, fix its status & continue,
                    // otherwise end the iterations with no solution.
                    if (SimulationValve.CheckBadValve(
                        net,
                        _logger,
                        _valves,
                        Htime,
                        _smat.GetOrder(errcode)))
                        continue;

                    break;
                }

                // Update current solution.
                // (Row[i] = row of solution matrix corresponding to node i).
                foreach (SimulationNode node  in  _junctions) {
                    node.SimHead = _lsv.GetRhsCoeff(_smat.GetRow(node.Index)); // Update heads
                }

                // Update flows
                relerr = NewFlows(relaxFactor);

                // Write convergence error to status report if called for
                if (net.StatFlag == StatFlag.FULL)
                    LogRelErr(iter, relerr);

                relaxFactor = 1.0;

                bool valveChange = false;

                //  Apply solution damping & check for change in valve status
                if (net.DampLimit > 0.0) {
                    if (relerr <= net.DampLimit) {
                        relaxFactor = 0.6;
                        valveChange = SimulationValve.ValveStatus(net, _logger, _valves);
                    }
                }
                else
                    valveChange = SimulationValve.ValveStatus(net, _logger, _valves);

                // Check for convergence
                if (relerr <= net.HAcc) {

                    //  We have convergence. Quit if we are into extra iterations.
                    if (iter > net.MaxIter)
                        break;

                    //  Quit if no status changes occur.
                    bool statChange = valveChange;

                    if (SimulationLink.LinkStatus(net, _logger, _links))
                        statChange = true;

                    if (SimulationControl.PSwitch(_logger, net, _controls))
                        statChange = true;

                    if (!statChange)
                        break;

                    //  We have a status change so continue the iterations
                    nextCheck = iter + net.CheckFreq;
                }
                else if (iter <= net.MaxCheck && iter == nextCheck) {
                    // No convergence yet. See if its time for a periodic status
                    // check  on pumps, CV's, and pipes connected to tanks.
                    SimulationLink.LinkStatus(net, _logger, _links);
                    nextCheck += net.CheckFreq;
                }

                iter++;
            }


            foreach (SimulationNode node  in  _junctions)
                node.SimDemand = node.SimDemand + node.SimEmitter;

            if (errcode > 0) {
                LogHydErr(_smat.GetOrder(errcode));
                errcode = 110;
                return;
            }

            if (errcode != 0)
                throw new ENException((ErrorCode)errcode);
        }



        ///<summary>Computes coefficients of linearized network eqns.</summary>
        private void NewCoeffs() {
            _lsv.Clear();

            foreach (SimulationLink link  in  _links) {
                link.SimInvHeadLoss = 0;
                link.SimFlowCorrection = 0;
            }

            SimulationLink.ComputeMatrixCoeffs(
                net,
                _pHlModel,
                _links,
                _curves,
                _smat,
                _lsv); // Compute link coeffs.

            SimulationNode.ComputeEmitterCoeffs(net, _junctions, _smat, _lsv); // Compute emitter coeffs.
            SimulationNode.ComputeNodeCoeffs(_junctions, _smat, _lsv); // Compute node coeffs.
            SimulationValve.ComputeMatrixCoeffs(net, _lsv, _smat, _valves); // Compute valve coeffs.
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

                var pump = link as SimulationPump;
                if (pump != null) {
                    if (pump.Ptype == PumpType.CONST_HP && dq > pump.SimFlow)
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

                if (node.Ke == 0.0)
                    continue;
                double dq = node.EmitFlowChange(net);
                node.SimEmitter = node.SimEmitter - dq;
                qsum += Math.Abs(node.SimEmitter);
                dqsum += Math.Abs(dq);
            }

            return qsum > net.HAcc 
                ? dqsum / qsum
                : dqsum;
        }

        ///<summary>Implements simple controls based on time or tank levels.</summary>
        private void ComputeControls() {
            SimulationControl.StepActions(_logger, net, _controls, Htime);
        }

        ///<summary>Computes demands at nodes during current time period.</summary>
        private void ComputeDemands() {
            // Determine total elapsed number of pattern periods
            long p = (Htime + net.PStart) / net.PStep;

            _dsystem = 0.0; //System-wide demand

            // Update demand at each node according to its assigned pattern
            foreach (SimulationNode node  in  _junctions) {
                double sum = 0.0;
                foreach (Demand demand  in  node.Demand) {
                    // pattern period (k) = (elapsed periods) modulus (periods per pattern)
                    var pat = demand.pattern;

                    long k = p % pat.Count;
                    double djunc = demand.Base * pat[(int)k] * net.DMult;

                    if (djunc > 0.0)
                        _dsystem += djunc;

                    sum += djunc;
                }
                node.SimDemand = sum;
            }

            // Update head at fixed grade nodes with time patterns
            foreach (SimulationTank tank  in  _tanks) {
                if (tank.IsReservoir) {
                    Pattern pat = tank.Pattern;
                    if (pat != null) {
                        long k = p % pat.Count;
                        tank.SimHead = tank.Elevation * pat[(int)k];
                    }
                }
            }

            // Update status of pumps with utilization patterns
            foreach (SimulationPump pump  in  _pumps) {
                if (pump.Upat != null) {
                    var pat = pump.Upat;
                    int k = (int)(p % pat.Count);
                    pump.SetLinkSetting(pat[k]);
                }
            }
        }

        ///<summary>Finds length of next time step & updates tank levels and rule-based contol actions.</summary>
        protected virtual long NextHyd() {
            long hydstep = 0;

            if (_simulationOutput != null)
                AwareStep.Write(_simulationOutput, this, Htime);

            if (Htime < net.Duration)
                hydstep = TimeStep();

            if (net.Duration == 0)
                SimulationPump.StepEnergy(net, _epat, _pumps, Htime, 0);
            else if (Htime < net.Duration)
                SimulationPump.StepEnergy(net, _epat, _pumps, Htime, hydstep);

            if (Htime < net.Duration) {
                Htime += hydstep;
                if (Htime >= Rtime)
                    Rtime += net.RStep;
            }

            return hydstep;
        }

        ///<summary>Computes time step to advance hydraulic simulation.</summary>
        private long TimeStep() {
            long tstep = net.HStep;

            long n = (Htime + net.PStart) / net.PStep + 1;
            long t = n * net.PStep - Htime;

            if (t > 0 && t < tstep)
                tstep = t;

            // Revise time step based on smallest time to fill or drain a tank
            t = Rtime - Htime;
            if (t > 0 && t < tstep) tstep = t;

            tstep = SimulationTank.MinimumTimeStep(_tanks, tstep);
            tstep = SimulationControl.MinimumTimeStep(net, _controls, Htime, tstep);

            if (_rules.Length > 0) {
                long step, htime;

                SimulationRule.MinimumTimeStep(
                    net,
                    _logger,
                    _rules,
                    _tanks,
                    Htime,
                    tstep,
                    _dsystem,
                    out step,
                    out htime);

                tstep = step;
                Htime = htime;
            }
            else
                SimulationTank.StepWaterLevels(_tanks, net.FieldsMap, tstep);

            return (tstep);
        }


        ///<summary>Save current step simulation results in the temp hydfile.</summary>
        private void SaveStep() {
            /*
            int capacity = this.links.Length * 3 * sizeof(float) + this.nodes.Length * 2 * sizeof(float) + sizeof(int);
            BinaryWriter bb = new BinaryWriter(new MemoryStream(capacity));
            */

            BinaryWriter bb = _simulationOutput;

            try {
                _simulationOutput.Write((int)Htime);

                foreach (SimulationNode node  in  _nodes)
                    bb.Write((float)node.SimDemand);

                foreach (SimulationNode node  in  _nodes)
                    bb.Write((float)node.SimHead);

                foreach (SimulationLink link  in  _links)
                    if (link.SimStatus <= StatType.CLOSED)
                        bb.Write((float)0.0);
                    else
                        bb.Write((float)link.SimFlow);

                foreach (SimulationLink link  in  _links)
                    bb.Write((int)link.SimStatus);

                foreach (SimulationLink link  in  _links)
                    bb.Write((float)link.SimSetting);
                

            }
            catch (IOException ex) {
                throw new ENException(ErrorCode.Err308, ex);
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
        private void LogHydWarn(int iter, double relerr) {
            try {
                int flag;

                string atime = Htime.GetClockTime();

                if (iter > net.MaxIter && relerr <= net.HAcc) {
                    if (net.MessageFlag)
                        _logger.Warning(Error.WARN02, atime);
                    flag = 2;
                }

                // Check for negative pressures
                foreach (SimulationNode node  in  _junctions) {
                    if (node.SimHead < node.Elevation && node.SimDemand > 0.0) {
                        if (net.MessageFlag)
                            _logger.Warning(Error.WARN06, atime);
                        flag = 6;
                        break;
                    }
                }

                // Check for abnormal valve condition
                foreach (SimulationValve valve  in  _valves) {
                    int j = valve.Index;
                    if (valve.SimStatus >= StatType.XFCV) {
                        if (net.MessageFlag)
                            _logger.Warning(
                                    Error.WARN05,
                                    valve.Type.ParseStr(),
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
                        if (net.MessageFlag)
                            _logger.Warning(
                                    Error.WARN04,
                                    pump.Link.Name,
                                    pump.SimStatus.ReportStr(),
                                    atime);
                        flag = 4;
                    }
                }

                // Check if system is unbalanced
                if (iter > net.MaxIter && relerr > net.HAcc) {
                    string str = string.Format(Error.WARN01, atime);

                    if (net.ExtraIter == -1)
                        str += Keywords.t_HALTED;

                    if (net.MessageFlag)
                        _logger.Warning(str);

                    flag = 1;
                }
            }
            catch (ENException) {}
        }

        /// <summary>Report hydraulic status.</summary>
        private void LogHydStat(int iter, double relerr) {
            try {
                string atime = Htime.GetClockTime();
                if (iter > 0) {
                    if (relerr <= net.HAcc)
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
                                    (tank.SimHead - tank.Elevation) * net.FieldsMap.GetUnits(FieldType.HEAD),
                                    net.FieldsMap.GetField(FieldType.HEAD).Units);

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
                        if (Htime == 0)
                            _logger.Warning(
                                    Text.FMT52,
                                    atime,
                                    link.Type.ParseStr(),
                                    link.Link.Name,
                                    link.SimStatus.ReportStr());
                        else
                            _logger.Warning(
                                    Text.FMT53,
                                    atime,
                                    link.Type.ParseStr(),
                                    link.Link.Name,
                                    link.SimOldStatus.ReportStr(),
                                    link.SimStatus.ReportStr());
                        link.SimOldStatus = link.SimStatus;
                    }
                }
            }
            catch (ENException) {}
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
                if (net.MessageFlag)
                    _logger.Warning(
                            Text.FMT62,
                            Htime.GetClockTime(),
                            _nodes[order].Id);
            }
            catch (ENException) {}
            LogHydStat(0, 0.0);
        }

        public IList<SimulationNode> Nodes { get { return _nodes; } }

        public IList<SimulationLink> Links { get { return _links; } }

        public SimulationRule[] Rules { get { return _rules; } }

        public SimulationControl[] Controls { get { return _controls; } }
    }

}