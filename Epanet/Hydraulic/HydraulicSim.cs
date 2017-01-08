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
        private bool running;
        [NonSerialized]
        private Thread runningThread;

        ///<summary>Event logger reference.</summary>
        private readonly TraceSource logger;


        private readonly SimulationNode[] nodes;
        private readonly SimulationLink[] links;
        private readonly List<SimulationPump> pumps;
        private readonly List<SimulationTank> tanks;
        private readonly List<SimulationNode> junctions;
        private readonly List<SimulationValve> valves;


        private readonly SimulationControl[] controls;
        private readonly SimulationRule[] rules;
        private readonly Curve[] curves;

        ///<summary>Simulation properties.</summary>
        protected readonly EpanetNetwork Net;

        ///<summary>Energy cost time pattern.</summary>
        private readonly Pattern epat;

        ///<summary>Linear system solver support class.</summary>
        private readonly SparseMatrix smat;

        ///<summary>Linear system variable storage class.</summary>
        private readonly LSVariables lsv;

        ///<summary>System wide demand.</summary>
        private double dsystem;

        ///<summary>Output stream of the hydraulic solution.</summary>
        private BinaryWriter simulationOutput;

        ///<summary>Pipe headloss model calculator.</summary>
        private readonly PipeHeadModelCalculators.Compute pHlModel;

        ///<summary>Get current hydraulic simulation time.</summary>
        public long Htime { get; private set; }

        ///<summary>Get current report time.</summary>
        public long Rtime { get; private set; }

        ///<summary>Init hydraulic simulation, preparing the linear solver and the hydraulic structures wrappers.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        /// <param name="log">Logger reference.</param>
        public HydraulicSim(EpanetNetwork net, TraceSource log) {
            this.running = false;
            this.logger = log;
            // this.CreateSimulationNetwork(net);


            this.nodes = new SimulationNode[net.Nodes.Count];
            this.links = new SimulationLink[net.Links.Count];
            this.pumps = new List<SimulationPump>();
            this.tanks = new List<SimulationTank>();
            this.junctions = new List<SimulationNode>();
            this.valves = new List<SimulationValve>();

            var nodesById = new Dictionary<string, SimulationNode>(net.Nodes.Count);

            for(int i = 0; i < net.Nodes.Count; i++) {
                SimulationNode node;

                var networkNode = net.Nodes[i];

                if (networkNode is Tank) {
                    node = new SimulationTank(networkNode, i);
                    this.tanks.Add((SimulationTank)node);
                }
                else {
                    node = new SimulationNode(networkNode, i);
                    this.junctions.Add(node);
                }

                this.nodes[i] = node;
                nodesById[node.Id] = node;
            }

            for(int i = 0; i < net.Links.Count; i++) {
                SimulationLink link;

                var networkLink = net.Links[i];

                if (networkLink is Valve) {
                    var valve = new SimulationValve(nodesById, networkLink, i);
                    this.valves.Add(valve);
                    link = valve;
                }
                else if (networkLink is Pump) {
                    var pump = new SimulationPump(nodesById, networkLink, i);
                    this.pumps.Add(pump);
                    link = pump;
                }
                else {
                    link = new SimulationLink(nodesById, networkLink, i);
                }
                
                this.links[i] = link;

            }

            this.rules = net.Rules.Select(r => new SimulationRule(r, this.links, this.nodes)).ToArray();

            this.curves = net.Curves.ToArray();

            this.controls = net.Controls.Select(x => new SimulationControl(this.nodes, this.links, x)).ToArray();


            this.Net = net;
            this.epat = net.GetPattern(this.Net.EPatId);
            this.smat = new SparseMatrix(this.nodes, this.links, this.junctions.Count);
            this.lsv = new LSVariables(this.nodes.Length, this.smat.CoeffsCount);

            this.Htime = 0;

            switch (this.Net.FormFlag) {

            case FormType.HW:
                this.pHlModel = PipeHeadModelCalculators.HWModelCalculator;
                break;
            case FormType.DW:
                this.pHlModel = PipeHeadModelCalculators.DwModelCalculator;
                break;
            case FormType.CM:
                this.pHlModel = PipeHeadModelCalculators.CMModelCalculator;
                break;
            }

            foreach(SimulationLink link in this.links)
                link.InitLinkFlow();

            foreach (SimulationNode node in this.junctions)
                if (node.Ke > 0.0)
                    node.SimEmitter = 1.0;

            foreach(SimulationLink link in this.links) {

                if((link.Type == LinkType.PRV ||
                     link.Type == LinkType.PSV ||
                     link.Type == LinkType.FCV)
                    && !link.Roughness.IsMissing())
                    link.SimStatus = StatType.ACTIVE;


                if(link.SimStatus <= StatType.CLOSED)
                    link.SimFlow = Constants.QZERO;
                else if(Math.Abs(link.SimFlow) <= Constants.QZERO)
                    link.InitLinkFlow(link.SimStatus, link.SimSetting);

                link.SimOldStatus = link.SimStatus;
            }


            this.Htime = 0;
            this.Rtime = this.Net.RStep;

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

                this.Simulate(new BinaryWriter(@out));
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
        public void Simulate(Stream @out) { this.Simulate(new BinaryWriter(@out)); }

        public long SimulateSingleStep() {

            if (!this.running)
                this.running = true;

            if (!this.RunHyd()) {
                this.running = false;
                return 0;
            }

            long hydstep = 0;

            if (this.Htime < this.Net.Duration)
                hydstep = this.TimeStep();

            if (this.Net.Duration == 0)
                SimulationPump.StepEnergy(this.Net, this.epat, this.pumps, this.Htime, 0);
            else if (this.Htime < this.Net.Duration)
                SimulationPump.StepEnergy(this.Net, this.epat, this.pumps, this.Htime, hydstep);

            if (this.Htime < this.Net.Duration) {
                this.Htime += hydstep;
                if (this.Htime >= this.Rtime)
                    this.Rtime += this.Net.RStep;
            }

            long tstep = hydstep;

            if (!this.running && tstep > 0) {
                this.running = false;
                return 0;
            }

            if (this.running && tstep > 0)
                return tstep;
            else {
                this.running = false;
                return 0;
            }
        }

        public void Simulate(BinaryWriter @out) {
            bool halted = false;

            if (this.running)
                throw new InvalidOperationException("Already running");

            this.runningThread = Thread.CurrentThread;
            this.running = true;

            this.simulationOutput = @out;
            if (this.simulationOutput != null)
                AwareStep.WriteHeader(@out, this, this.Net.RStart, this.Net.RStep, this.Net.Duration);

//        writeHeader(simulationOutput);
            try {
                long tstep;
                do {
                    if (!this.RunHyd())
                        break;

                    tstep = this.NextHyd();

                    if (!this.running && tstep > 0)
                        halted = true;
                }
                while (this.running && tstep > 0);
            }
            catch (IOException e) {
                Debug.Print(e.ToString());
                throw new ENException(ErrorCode.Err1000);
            }
            finally {
                this.running = false;
                this.runningThread = null;
            }

            if (halted)
                throw new ENException(ErrorCode.Err1000);

        }

        ///<summary>Halt hydraulic simulation.</summary>
        public void StopRunning() {
            this.running = false;
            if (this.runningThread != null && this.runningThread.IsAlive)
                this.runningThread.Join(1000);
        }

        ///<summary>Solves network hydraulics in a single time period.</summary>
        private bool RunHyd() {

            // Find new demands & control actions
            this.ComputeDemands();
            this.ComputeControls();

            // Solve network hydraulic equations
            double Relerr;
            int Iter;
            this.NetSolve(out Iter, out Relerr);

            // Report new status & save results
            if (this.Net.Stat_Flag != StatFlag.NO)
                this.LogHydStat(Iter, Relerr);

            // If system unbalanced and no extra trials
            // allowed, then activate the Haltflag.
            if (Relerr > this.Net.HAcc && this.Net.ExtraIter == -1) {
                this.Htime = this.Net.Duration;
                return false;
            }

            this.LogHydWarn(Iter, Relerr);

            return true;
        }

        ///<summary>Solve the linear equation system to compute the links flows and nodes heads.</summary>
        /// <returns>Solver steps and relative error.</returns>
        protected void NetSolve(out int iter, out double relerr) {
            iter = 0;
            relerr = 0.0;

            int nextCheck = this.Net.CheckFreq;

            if (this.Net.Stat_Flag == StatFlag.FULL)
                this.LogRelErr(iter, relerr);

            int maxTrials = this.Net.MaxIter;

            if (this.Net.ExtraIter > 0)
                maxTrials += this.Net.ExtraIter;

            double relaxFactor = 1.0;
            int errcode = 0;
            iter = 1;

            while (iter <= maxTrials) {
                //Compute coefficient matrices A & F and solve A*H = F
                // where H = heads, A = Jacobian coeffs. derived from
                // head loss gradients, & F = flow correction terms.
                this.NewCoeffs();

                //dumpMatrixCoeffs(new File("dumpMatrix.txt"),true);

                // Solution for H is returned in F from call to linsolve().
                errcode = this.smat.LinSolve(
                                  this.junctions.Count,
                                  this.lsv.AiiVector,
                                  this.lsv.AijVector,
                                  this.lsv.RhsCoeffs);

                // Ill-conditioning problem
                if (errcode > 0) {
                    // If control valve causing problem, fix its status & continue,
                    // otherwise end the iterations with no solution.
                    if (SimulationValve.CheckBadValve(
                        this.Net,
                        this.logger,
                        this.valves,
                        this.Htime,
                        this.smat.GetOrder(errcode)))
                        continue;

                    break;
                }

                // Update current solution.
                // (Row[i] = row of solution matrix corresponding to node i).
                foreach (SimulationNode node  in  this.junctions) {
                    node.SimHead = this.lsv.GetRhsCoeff(this.smat.GetRow(node.Index)); // Update heads
                }

                // Update flows
                relerr = this.NewFlows(relaxFactor);

                // Write convergence error to status report if called for
                if (this.Net.Stat_Flag == StatFlag.FULL)
                    this.LogRelErr(iter, relerr);

                relaxFactor = 1.0;

                bool valveChange = false;

                //  Apply solution damping & check for change in valve status
                if (this.Net.DampLimit > 0.0) {
                    if (relerr <= this.Net.DampLimit) {
                        relaxFactor = 0.6;
                        valveChange = SimulationValve.ValveStatus(this.Net, this.logger, this.valves);
                    }
                }
                else
                    valveChange = SimulationValve.ValveStatus(this.Net, this.logger, this.valves);

                // Check for convergence
                if (relerr <= this.Net.HAcc) {

                    //  We have convergence. Quit if we are into extra iterations.
                    if (iter > this.Net.MaxIter)
                        break;

                    //  Quit if no status changes occur.
                    bool statChange = valveChange;

                    if (SimulationLink.LinkStatus(this.Net, this.logger, this.links))
                        statChange = true;

                    if (SimulationControl.PSwitch(this.logger, this.Net, this.controls))
                        statChange = true;

                    if (!statChange)
                        break;

                    //  We have a status change so continue the iterations
                    nextCheck = iter + this.Net.CheckFreq;
                }
                else if (iter <= this.Net.MaxCheck && iter == nextCheck) {
                    // No convergence yet. See if its time for a periodic status
                    // check  on pumps, CV's, and pipes connected to tanks.
                    SimulationLink.LinkStatus(this.Net, this.logger, this.links);
                    nextCheck += this.Net.CheckFreq;
                }

                iter++;
            }


            foreach (SimulationNode node  in  this.junctions)
                node.SimDemand = node.SimDemand + node.SimEmitter;

            if (errcode > 0) {
                this.LogHydErr(this.smat.GetOrder(errcode));
                errcode = 110;
                return;
            }

            if (errcode != 0)
                throw new ENException((ErrorCode)errcode);
        }



        ///<summary>Computes coefficients of linearized network eqns.</summary>
        private void NewCoeffs() {
            this.lsv.Clear();

            foreach (SimulationLink link  in  this.links) {
                link.SimInvHeadLoss = 0;
                link.SimFlowCorrection = 0;
            }

            SimulationLink.ComputeMatrixCoeffs(
                this.Net,
                this.pHlModel,
                this.links,
                this.curves,
                this.smat,
                this.lsv); // Compute link coeffs.

            SimulationNode.ComputeEmitterCoeffs(this.Net, this.junctions, this.smat, this.lsv); // Compute emitter coeffs.
            SimulationNode.ComputeNodeCoeffs(this.junctions, this.smat, this.lsv); // Compute node coeffs.
            SimulationValve.ComputeMatrixCoeffs(this.Net, this.lsv, this.smat, this.valves); // Compute valve coeffs.
        }

        ///<summary>Updates link flows after new nodal heads computed.</summary>
        private double NewFlows(double relaxFactor) {

            foreach (SimulationTank node  in  this.tanks)
                node.SimDemand = 0;

            double qsum = 0.0;
            double dqsum = 0.0;

            foreach (SimulationLink link  in  this.links) {
                SimulationNode n1 = link.First;
                SimulationNode n2 = link.Second;

                double dh = n1.SimHead - n2.SimHead;
                double dq = link.SimFlowCorrection - link.SimInvHeadLoss * dh;

                dq *= relaxFactor;

                if (link is SimulationPump) {
                    if (((SimulationPump)link).Ptype == PumpType.CONST_HP && dq > link.SimFlow)
                        dq = link.SimFlow / 2.0;
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

            foreach (SimulationNode node  in  this.junctions) {

                if (node.Ke == 0.0)
                    continue;
                double dq = node.EmitFlowChange(this.Net);
                node.SimEmitter = node.SimEmitter - dq;
                qsum += Math.Abs(node.SimEmitter);
                dqsum += Math.Abs(dq);
            }

            return qsum > this.Net.HAcc 
                ? dqsum / qsum
                : dqsum;
        }

        ///<summary>Implements simple controls based on time or tank levels.</summary>
        private void ComputeControls() {
            SimulationControl.StepActions(this.logger, this.Net, this.controls, this.Htime);
        }

        ///<summary>Computes demands at nodes during current time period.</summary>
        private void ComputeDemands() {
            // Determine total elapsed number of pattern periods
            long p = (this.Htime + this.Net.PStart) / this.Net.PStep;

            this.dsystem = 0.0; //System-wide demand

            // Update demand at each node according to its assigned pattern
            foreach (SimulationNode node  in  this.junctions) {
                double sum = 0.0;
                foreach (Demand demand  in  node.Demand) {
                    // pattern period (k) = (elapsed periods) modulus (periods per pattern)
                    var pat = demand.Pattern;

                    long k = p % pat.Count;
                    double djunc = demand.Base * pat[(int)k] * this.Net.DMult;

                    if (djunc > 0.0)
                        this.dsystem += djunc;

                    sum += djunc;
                }
                node.SimDemand = sum;
            }

            // Update head at fixed grade nodes with time patterns
            foreach (SimulationTank tank  in  this.tanks) {
                if (tank.IsReservoir) {
                    Pattern pat = tank.Pattern;
                    if (pat != null) {
                        long k = p % pat.Count;
                        tank.SimHead = tank.Elevation * pat[(int)k];
                    }
                }
            }

            // Update status of pumps with utilization patterns
            foreach (SimulationPump pump  in  this.pumps) {
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

            if (this.simulationOutput != null)
                AwareStep.Write(this.simulationOutput, this, this.Htime);

            if (this.Htime < this.Net.Duration)
                hydstep = this.TimeStep();

            if (this.Net.Duration == 0)
                SimulationPump.StepEnergy(this.Net, this.epat, this.pumps, this.Htime, 0);
            else if (this.Htime < this.Net.Duration)
                SimulationPump.StepEnergy(this.Net, this.epat, this.pumps, this.Htime, hydstep);

            if (this.Htime < this.Net.Duration) {
                this.Htime += hydstep;
                if (this.Htime >= this.Rtime)
                    this.Rtime += this.Net.RStep;
            }

            return hydstep;
        }

        ///<summary>Computes time step to advance hydraulic simulation.</summary>
        private long TimeStep() {
            long tstep = this.Net.HStep;

            long n = ((this.Htime + this.Net.PStart) / this.Net.PStep) + 1;
            long t = n * this.Net.PStep - this.Htime;

            if (t > 0 && t < tstep)
                tstep = t;

            // Revise time step based on smallest time to fill or drain a tank
            t = this.Rtime - this.Htime;
            if (t > 0 && t < tstep) tstep = t;

            tstep = SimulationTank.MinimumTimeStep(this.tanks, tstep);
            tstep = SimulationControl.MinimumTimeStep(this.Net, this.controls, this.Htime, tstep);

            if (this.rules.Length > 0) {
                long step, htime;

                SimulationRule.MinimumTimeStep(
                    this.Net,
                    this.logger,
                    this.rules,
                    this.tanks,
                    this.Htime,
                    tstep,
                    this.dsystem,
                    out step,
                    out htime);

                tstep = step;
                this.Htime = htime;
            }
            else
                SimulationTank.StepWaterLevels(this.tanks, this.Net.FieldsMap, tstep);

            return (tstep);
        }


        ///<summary>Save current step simulation results in the temp hydfile.</summary>
        private void SaveStep() {
            /*
            int capacity = this.links.Length * 3 * sizeof(float) + this.nodes.Length * 2 * sizeof(float) + sizeof(int);
            BinaryWriter bb = new BinaryWriter(new MemoryStream(capacity));
            */

            BinaryWriter bb = this.simulationOutput;

            try {
                this.simulationOutput.Write((int)this.Htime);

                foreach (SimulationNode node  in  this.nodes)
                    bb.Write((float)node.SimDemand);

                foreach (SimulationNode node  in  this.nodes)
                    bb.Write((float)node.SimHead);

                foreach (SimulationLink link  in  this.links)
                    if (link.SimStatus <= StatType.CLOSED)
                        bb.Write((float)0.0);
                    else
                        bb.Write((float)link.SimFlow);

                foreach (SimulationLink link  in  this.links)
                    bb.Write((int)link.SimStatus);

                foreach (SimulationLink link  in  this.links)
                    bb.Write((float)link.SimSetting);
                

            }
            catch (IOException) {
                throw new ENException(ErrorCode.Err308);
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

                string atime = this.Htime.GetClockTime();

                if (iter > this.Net.MaxIter && relerr <= this.Net.HAcc) {
                    if (this.Net.MessageFlag)
                        this.logger.Warning(Error.WARN02, atime);
                    flag = 2;
                }

                // Check for negative pressures
                foreach (SimulationNode node  in  this.junctions) {
                    if (node.SimHead < node.Elevation && node.SimDemand > 0.0) {
                        if (this.Net.MessageFlag)
                            this.logger.Warning(Error.WARN06, atime);
                        flag = 6;
                        break;
                    }
                }

                // Check for abnormal valve condition
                foreach (SimulationValve valve  in  this.valves) {
                    int j = valve.Index;
                    if (valve.SimStatus >= StatType.XFCV) {
                        if (this.Net.MessageFlag)
                            this.logger.Warning(
                                    Error.WARN05,
                                    valve.Type.ParseStr(),
                                    valve.Link.Name,
                                    valve.SimStatus.ReportStr(),
                                    atime);
                        flag = 5;
                    }
                }

                // Check for abnormal pump condition
                foreach (SimulationPump pump  in  this.pumps) {
                    StatType s = pump.SimStatus;
                    if (pump.SimStatus >= StatType.OPEN) {
                        if (pump.SimFlow > pump.SimSetting * pump.Qmax)
                            s = StatType.XFLOW;
                        if (pump.SimFlow < 0.0)
                            s = StatType.XHEAD;
                    }

                    if (s == StatType.XHEAD || s == StatType.XFLOW) {
                        if (this.Net.MessageFlag)
                            this.logger.Warning(
                                    Error.WARN04,
                                    pump.Link.Name,
                                    pump.SimStatus.ReportStr(),
                                    atime);
                        flag = 4;
                    }
                }

                // Check if system is unbalanced
                if (iter > this.Net.MaxIter && relerr > this.Net.HAcc) {
                    string str = string.Format(Error.WARN01, atime);

                    if (this.Net.ExtraIter == -1)
                        str += Keywords.t_HALTED;

                    if (this.Net.MessageFlag)
                        this.logger.Warning(str);

                    flag = 1;
                }
            }
            catch (ENException) {}
        }

        /// <summary>Report hydraulic status.</summary>
        private void LogHydStat(int iter, double relerr) {
            try {
                string atime = this.Htime.GetClockTime();
                if (iter > 0) {
                    if (relerr <= this.Net.HAcc)
                        this.logger.Warning(Text.FMT58, atime, iter);
                    else
                        this.logger.Warning(Text.FMT59, atime, iter, relerr);
                }

                foreach (SimulationTank tank  in  this.tanks) {
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
                            this.logger.Warning(
                                    Text.FMT50,
                                    atime,
                                    tank.Id,
                                    newstat.ReportStr(),
                                    (tank.SimHead - tank.Elevation) * this.Net.FieldsMap.GetUnits(FieldType.HEAD),
                                    this.Net.FieldsMap.GetField(FieldType.HEAD).Units);

                        else
                            this.logger.Warning(
                                    Text.FMT51,
                                    atime,
                                    tank.Id,
                                    newstat.ReportStr());

                        tank.OldStat = newstat;
                    }
                }

                foreach (SimulationLink link  in  this.links) {
                    if (link.SimStatus != link.SimOldStatus) {
                        if (this.Htime == 0)
                            this.logger.Warning(
                                    Text.FMT52,
                                    atime,
                                    link.Type.ParseStr(),
                                    link.Link.Name,
                                    link.SimStatus.ReportStr());
                        else
                            this.logger.Warning(
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
                this.logger.Warning(Text.FMT64, this.Htime.GetClockTime());
            }
            else {
                this.logger.Warning(Text.FMT65, iter, relerr);
            }
        }

        private void LogHydErr(int order) {
            try {
                if (this.Net.MessageFlag)
                    this.logger.Warning(
                            Text.FMT62,
                            this.Htime.GetClockTime(),
                            this.nodes[order].Id);
            }
            catch (ENException) {}
            this.LogHydStat(0, 0.0);
        }

        public IList<SimulationNode> Nodes { get { return this.nodes; } }

        public IList<SimulationLink> Links { get { return this.links; } }

        public SimulationRule[] Rules { get { return this.rules; } }

        public SimulationControl[] Controls { get { return this.controls; } }
    }

}