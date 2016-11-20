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
using System.Threading;
using Epanet.Hydraulic.IO;
using Epanet.Hydraulic.Models;
using Epanet.Hydraulic.Structures;
using Epanet.Log;
using Epanet.Network;
using Epanet.Network.IO;
using Epanet.Network.Structures;
using Epanet.Properties;
using Epanet.Util;

namespace Epanet.Hydraulic {

    ///<summary>Hydraulic simulation class.</summary>
    public class HydraulicSim {
        private const int BUFFER_SIZE = 512 * 1024; //512kb
        [NonSerialized]
        private bool running;
        [NonSerialized]
        private Thread runningThread;

        protected HydraulicSim() { }

        ///<summary>Step solving result</summary>
        protected class NetSolveStep {
            public NetSolveStep(int iter, double relerr) {
                this.Iter = iter;
                this.Relerr = relerr;
            }

            public int Iter;
            public double Relerr;
        }

        ///<summary>Event logger reference.</summary>
        private readonly TraceSource logger;


        private List<SimulationNode> nodes;
        private List<SimulationLink> links;
        private List<SimulationPump> pumps;
        private List<SimulationTank> tanks;
        private List<SimulationNode> junctions;
        private List<SimulationValve> valves;


        private List<SimulationControl> controls;
        private List<SimulationRule> rules;
        private IList<Curve> curves;


        ///<summary>Simulation conversion units.</summary>
        protected FieldsMap fMap;

        ///<summary>Simulation properties.</summary>
        private PropertiesMap pMap;

        ///<summary>Energy cost time pattern.</summary>
        private Pattern epat;

        ///<summary>Linear system solver support class.</summary>
        private SparseMatrix smat;

        ///<summary>Linear system variable storage class.</summary>
        private LSVariables lsv;

        ///<summary>System wide demand.</summary>
        private double dsystem;

        ///<summary>Output stream of the hydraulic solution.</summary>
        private BinaryWriter simulationOutput;

        ///<summary>Pipe headloss model calculator.</summary>
        private PipeHeadModelCalculators.Compute pHlModel;

        ///<summary>Get current hydraulic simulation time.</summary>
        public long Htime { get; private set; }

        ///<summary>Get current report time.</summary>
        public long Rtime { get; private set; }

        ///<summary>Init hydraulic simulation, preparing the linear solver and the hydraulic structures wrappers.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        /// <param name="log">Logger reference.</param>
        public HydraulicSim(Network.Network net, TraceSource log) {
            this.running = false;
            this.logger = log;
            this.CreateSimulationNetwork(net);
        }

        private void CreateSimulationNetwork(Network.Network net) {
            this.nodes = new List<SimulationNode>();
            this.links = new List<SimulationLink>();
            this.pumps = new List<SimulationPump>();
            this.tanks = new List<SimulationTank>();
            this.junctions = new List<SimulationNode>();
            this.valves = new List<SimulationValve>();
            this.rules = new List<SimulationRule>();


            Dictionary<string, SimulationNode> nodesById = new Dictionary<string, SimulationNode>();
         
            foreach (Node n  in  net.Nodes) {
                SimulationNode node = SimulationNode.CreateIndexedNode(n, this.nodes.Count);
                this.nodes.Add(node);
                nodesById[node.Id] = node;

                var tank = node as SimulationTank;
                if (tank != null)
                    this.tanks.Add(tank);
                else
                    this.junctions.Add(node);
            }

            foreach (Link l  in  net.Links) {
                SimulationLink link = SimulationLink.CreateIndexedLink(nodesById, l, this.links.Count);
                this.links.Add(link);

                var valve = link as SimulationValve;
                if (valve != null)
                    this.valves.Add(valve);
                else if (link is SimulationPump)
                    this.pumps.Add((SimulationPump)link);
            }

            foreach (Rule r  in  net.Rules) {
                SimulationRule rule = new SimulationRule(r, this.links, this.nodes); //, tmpLinks, tmpNodes);
                this.rules.Add(rule);
            }

            this.curves = net.Curves;

            this.controls = new List<SimulationControl>();

            foreach (Control ctr  in  net.Controls)
                this.controls.Add(new SimulationControl(this.nodes, this.links, ctr));

            this.fMap = net.FieldsMap;
            this.pMap = net.PropertiesMap;
            this.epat = net.GetPattern(this.pMap.EPatId);
            this.smat = new SparseMatrix(this.nodes, this.links, this.junctions.Count);
            this.lsv = new LSVariables(this.nodes.Count, this.smat.CoeffsCount);

            this.Htime = 0;

            switch (this.pMap.FormFlag) {

            case PropertiesMap.FormType.HW:
                this.pHlModel = PipeHeadModelCalculators.HWModelCalculator;
                break;
            case PropertiesMap.FormType.DW:
                this.pHlModel = PipeHeadModelCalculators.DwModelCalculator;
                break;
            case PropertiesMap.FormType.CM:
                this.pHlModel = PipeHeadModelCalculators.CMModelCalculator;
                break;
            }

            foreach (SimulationLink link  in  this.links) {
                link.InitLinkFlow();
            }

            foreach (SimulationNode node  in  this.junctions) {
                if (node.Ke > 0.0)
                    node.SimEmitter = 1.0;
            }

            foreach (SimulationLink link  in  this.links) {

                if ((link.Type == Link.LinkType.PRV ||
                     link.Type == Link.LinkType.PSV ||
                     link.Type == Link.LinkType.FCV)
                    && !link.Roughness.IsMissing())
                    link.SimStatus = Link.StatType.ACTIVE;


                if (link.SimStatus <= Link.StatType.CLOSED)
                    link.SimFlow = Constants.QZERO;
                else if (Math.Abs(link.SimFlow) <= Constants.QZERO)
                    link.InitLinkFlow(link.SimStatus, link.SimSetting);

                link.SimOldStatus = link.SimStatus;
            }

            foreach (SimulationPump pump  in  this.pumps) {
                Array.Clear(pump.Energy, 0, pump.Energy.Length);
            }

            this.Htime = 0;
            this.Rtime = this.pMap.RStep;
        }

        ///<summary>Run hydraulic simuation.</summary>
        /// <param name="hyd">File name where the output of the hydraulic simulation will be writen.</param>
        public void Simulate(string hyd) {
            try {
                FileStream @out = new FileStream(
                    hyd,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read,
                    BUFFER_SIZE);

                using (@out) {
                    this.Simulate(@out);
                }
            }
            catch (IOException) {
                throw new ENException(ErrorCode.Err305);
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

            if (this.Htime < this.pMap.Duration)
                hydstep = this.TimeStep();

            if (this.pMap.Duration == 0)
                SimulationPump.StepEnergy(this.pMap, this.fMap, this.epat, this.pumps, this.Htime, 0);
            else if (this.Htime < this.pMap.Duration)
                SimulationPump.StepEnergy(this.pMap, this.fMap, this.epat, this.pumps, this.Htime, hydstep);

            if (this.Htime < this.pMap.Duration) {
                this.Htime += hydstep;
                if (this.Htime >= this.Rtime)
                    this.Rtime += this.pMap.RStep;
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
            if (this.simulationOutput != null) {
                AwareStep.WriteHeader(@out, this, this.pMap.RStart, this.pMap.RStep, this.pMap.Duration);
            }
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
            NetSolveStep nss = this.NetSolve();

            // Report new status & save results
            if (this.pMap.Stat_Flag != PropertiesMap.StatFlag.NO)
                this.LogHydStat(nss);

            // If system unbalanced and no extra trials
            // allowed, then activate the Haltflag.
            if (nss.Relerr > this.pMap.HAcc && this.pMap.ExtraIter == -1) {
                this.Htime = this.pMap.Duration;
                return false;
            }

            this.LogHydWarn(nss);

            return true;
        }

        ///<summary>Solve the linear equation system to compute the links flows and nodes heads.</summary>
        /// <returns>Solver steps and relative error.</returns>
        protected NetSolveStep NetSolve() {
            NetSolveStep ret = new NetSolveStep(0, 0);

            int nextCheck = this.pMap.CheckFreq;

            if (this.pMap.Stat_Flag == PropertiesMap.StatFlag.FULL)
                this.LogRelErr(ret);

            int maxTrials = this.pMap.MaxIter;

            if (this.pMap.ExtraIter > 0)
                maxTrials += this.pMap.ExtraIter;

            double relaxFactor = 1.0;
            int errcode = 0;
            ret.Iter = 1;

            while (ret.Iter <= maxTrials) {
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
                        this.pMap,
                        this.logger,
                        this.valves,
                        this.Htime,
                        this.smat.GetOrder(errcode)))
                        continue;
                    else break;
                }

                // Update current solution.
                // (Row[i] = row of solution matrix corresponding to node i).
                foreach (SimulationNode node  in  this.junctions) {
                    node.SimHead = this.lsv.GetRhsCoeff(this.smat.GetRow(node.Index)); // Update heads
                }

                // Update flows
                ret.Relerr = this.NewFlows(relaxFactor);

                // Write convergence error to status report if called for
                if (this.pMap.Stat_Flag == PropertiesMap.StatFlag.FULL)
                    this.LogRelErr(ret);

                relaxFactor = 1.0;

                bool valveChange = false;

                //  Apply solution damping & check for change in valve status
                if (this.pMap.DampLimit > 0.0) {
                    if (ret.Relerr <= this.pMap.DampLimit) {
                        relaxFactor = 0.6;
                        valveChange = SimulationValve.ValveStatus(this.fMap, this.pMap, this.logger, this.valves);
                    }
                }
                else
                    valveChange = SimulationValve.ValveStatus(this.fMap, this.pMap, this.logger, this.valves);

                // Check for convergence
                if (ret.Relerr <= this.pMap.HAcc) {

                    //  We have convergence. Quit if we are into extra iterations.
                    if (ret.Iter > this.pMap.MaxIter)
                        break;

                    //  Quit if no status changes occur.
                    bool statChange = valveChange;

                    if (SimulationLink.LinkStatus(this.pMap, this.fMap, this.logger, this.links))
                        statChange = true;

                    if (SimulationControl.PSwitch(this.logger, this.pMap, this.fMap, this.controls))
                        statChange = true;

                    if (!statChange)
                        break;

                    //  We have a status change so continue the iterations
                    nextCheck = ret.Iter + this.pMap.CheckFreq;
                }
                else if (ret.Iter <= this.pMap.MaxCheck && ret.Iter == nextCheck) {
                    // No convergence yet. See if its time for a periodic status
                    // check  on pumps, CV's, and pipes connected to tanks.
                    SimulationLink.LinkStatus(this.pMap, this.fMap, this.logger, this.links);
                    nextCheck += this.pMap.CheckFreq;
                }

                ret.Iter++;
            }


            foreach (SimulationNode node  in  this.junctions)
                node.SimDemand = node.SimDemand + node.SimEmitter;

            if (errcode > 0) {
                this.LogHydErr(this.smat.GetOrder(errcode));
                errcode = 110;
                return ret;
            }

            if (errcode != 0)
                throw new ENException((ErrorCode)errcode);

            return ret;
        }


        ///<summary>Computes coefficients of linearized network eqns.</summary>
        private void NewCoeffs() {
            this.lsv.Clear();

            foreach (SimulationLink link  in  this.links) {
                link.SimInvHeadLoss = 0;
                link.SimFlowCorrection = 0;
            }

            SimulationLink.ComputeMatrixCoeffs(
                this.fMap,
                this.pMap,
                this.pHlModel,
                this.links,
                this.curves,
                this.smat,
                this.lsv); // Compute link coeffs.
            SimulationNode.ComputeEmitterCoeffs(this.pMap, this.junctions, this.smat, this.lsv);
                // Compute emitter coeffs.
            SimulationNode.ComputeNodeCoeffs(this.junctions, this.smat, this.lsv); // Compute node coeffs.
            SimulationValve.ComputeMatrixCoeffs(this.pMap, this.lsv, this.smat, this.valves); // Compute valve coeffs.
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
                    if (((SimulationPump)link).Ptype == Pump.PumpType.CONST_HP && dq > link.SimFlow)
                        dq = link.SimFlow / 2.0;
                }

                link.SimFlow = link.SimFlow - dq;

                qsum += Math.Abs(link.SimFlow);
                dqsum += Math.Abs(dq);

                if (link.SimStatus > Link.StatType.CLOSED) {
                    if (n1 is SimulationTank)
                        n1.SimDemand = n1.SimDemand - link.SimFlow;
                    if (n2 is SimulationTank)
                        n2.SimDemand = n2.SimDemand + link.SimFlow;
                }
            }

            foreach (SimulationNode node  in  this.junctions) {

                if (node.Ke == 0.0)
                    continue;
                double dq = node.EmitFlowChange(this.pMap);
                node.SimEmitter = node.SimEmitter - dq;
                qsum += Math.Abs(node.SimEmitter);
                dqsum += Math.Abs(dq);
            }

            return qsum > this.pMap.HAcc 
                ? dqsum / qsum
                : dqsum;
        }

        ///<summary>Implements simple controls based on time or tank levels.</summary>
        private void ComputeControls() {
            SimulationControl.StepActions(this.logger, this.fMap, this.pMap, this.controls, this.Htime);
        }

        ///<summary>Computes demands at nodes during current time period.</summary>
        private void ComputeDemands() {
            // Determine total elapsed number of pattern periods
            long p = (this.Htime + this.pMap.PStart) / this.pMap.PStep;

            this.dsystem = 0.0; //System-wide demand

            // Update demand at each node according to its assigned pattern
            foreach (SimulationNode node  in  this.junctions) {
                double sum = 0.0;
                foreach (Demand demand  in  node.Demand) {
                    // pattern period (k) = (elapsed periods) modulus (periods per pattern)
                    List<double> factors = demand.Pattern.FactorsList;

                    long k = p % factors.Count;
                    double djunc = (demand.Base) * factors[(int)k] * this.pMap.DMult;
                    if (djunc > 0.0)
                        this.dsystem += djunc;

                    sum += djunc;
                }
                node.SimDemand = sum;
            }

            // Update head at fixed grade nodes with time patterns
            foreach (SimulationTank tank  in  this.tanks) {
                if (tank.Area == 0.0) {
                    Pattern pat = tank.Pattern;
                    if (pat != null) {
                        List<double> factors = pat.FactorsList;
                        long k = p % factors.Count;

                        tank.SimHead = tank.Elevation * factors[(int)k];
                    }
                }
            }

            // Update status of pumps with utilization patterns
            foreach (SimulationPump pump  in  this.pumps) {
                if (pump.Upat != null) {
                    List<double> factors = pump.Upat.FactorsList;
                    int k = (int)(p % factors.Count);
                    pump.SetLinkSetting(factors[k]);
                }
            }
        }

        ///<summary>Finds length of next time step & updates tank levels and rule-based contol actions.</summary>
        protected virtual long NextHyd() {
            long hydstep = 0;

            if (this.simulationOutput != null)
                AwareStep.Write(this.simulationOutput, this, this.Htime);

            if (this.Htime < this.pMap.Duration)
                hydstep = this.TimeStep();

            if (this.pMap.Duration == 0)
                SimulationPump.StepEnergy(this.pMap, this.fMap, this.epat, this.pumps, this.Htime, 0);
            else if (this.Htime < this.pMap.Duration)
                SimulationPump.StepEnergy(this.pMap, this.fMap, this.epat, this.pumps, this.Htime, hydstep);

            if (this.Htime < this.pMap.Duration) {
                this.Htime += hydstep;
                if (this.Htime >= this.Rtime)
                    this.Rtime += this.pMap.RStep;
            }

            return hydstep;
        }

        ///<summary>Computes time step to advance hydraulic simulation.</summary>
        private long TimeStep() {
            long tstep = this.pMap.HStep;

            long n = ((this.Htime + this.pMap.PStart) / this.pMap.PStep) + 1;
            long t = n * this.pMap.PStep - this.Htime;

            if (t > 0 && t < tstep)
                tstep = t;

            // Revise time step based on smallest time to fill or drain a tank
            t = this.Rtime - this.Htime;
            if (t > 0 && t < tstep) tstep = t;

            tstep = SimulationTank.MinimumTimeStep(this.tanks, tstep);
            tstep = SimulationControl.MinimumTimeStep(this.fMap, this.pMap, this.controls, this.Htime, tstep);

            if (this.rules.Count > 0) {
                long step, htime;

                SimulationRule.MinimumTimeStep(
                    this.fMap,
                    this.pMap,
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
                SimulationTank.StepWaterLevels(this.tanks, this.fMap, tstep);

            return (tstep);
        }


        ///<summary>Save current step simulation results in the temp hydfile.</summary>
        private void SaveStep() {

            MemoryStream ms =
                new MemoryStream(
                    this.links.Count * 3 * sizeof(float) + this.nodes.Count * 2 * sizeof(float) + sizeof(int));
            BinaryWriter bb = new BinaryWriter(ms);


            try {
                bb.Write((int)this.Htime);

                foreach (SimulationNode node  in  this.nodes)
                    bb.Write((float)node.SimDemand);

                foreach (SimulationNode node  in  this.nodes)
                    bb.Write((float)node.SimHead);

                foreach (SimulationLink link  in  this.links)
                    if (link.SimStatus <= Link.StatType.CLOSED)
                        bb.Write((float)0.0);

                    else
                        bb.Write((float)link.SimFlow);

                foreach (SimulationLink link  in  this.links)
                    bb.Write((int)link.SimStatus);

                foreach (SimulationLink link  in  this.links)
                    bb.Write((float)link.SimSetting);

                this.simulationOutput.Write(ms.GetBuffer());

            }
            catch (IOException) {
                throw new ENException(ErrorCode.Err308);
            }
        }

        /// <summary>Report hydraulic warning.</summary>
        /// <param name="nss"></param>
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
        private void LogHydWarn(NetSolveStep nss) {
            try {
                int flag;

                string atime = this.Htime.GetClockTime();

                if (nss.Iter > this.pMap.MaxIter && nss.Relerr <= this.pMap.HAcc) {
                    if (this.pMap.MessageFlag)
                        this.logger.Warning(Error.ResourceManager.GetString("WARN02"), atime);
                    flag = 2;
                }

                // Check for negative pressures
                foreach (SimulationNode node  in  this.junctions) {
                    if (node.SimHead < node.Elevation && node.SimDemand > 0.0) {
                        if (this.pMap.MessageFlag)
                            this.logger.Warning(Error.ResourceManager.GetString("WARN06"), atime);
                        flag = 6;
                        break;
                    }
                }

                // Check for abnormal valve condition
                foreach (SimulationValve valve  in  this.valves) {
                    int j = valve.Index;
                    if (valve.SimStatus >= Link.StatType.XFCV) {
                        if (this.pMap.MessageFlag)
                            this.logger.Warning(
                                    Error.ResourceManager.GetString("WARN05"),
                                    valve.Type.ParseStr(),
                                    valve.Link.Id,
                                    valve.SimStatus.ReportStr(),
                                    atime);
                        flag = 5;
                    }
                }

                // Check for abnormal pump condition
                foreach (SimulationPump pump  in  this.pumps) {
                    Link.StatType s = pump.SimStatus;
                    if (pump.SimStatus >= Link.StatType.OPEN) {
                        if (pump.SimFlow > pump.SimSetting * pump.Qmax)
                            s = Link.StatType.XFLOW;
                        if (pump.SimFlow < 0.0)
                            s = Link.StatType.XHEAD;
                    }

                    if (s == Link.StatType.XHEAD || s == Link.StatType.XFLOW) {
                        if (this.pMap.MessageFlag)
                            this.logger.Warning(
                                    Error.ResourceManager.GetString("WARN04"),
                                    pump.Link.Id,
                                    pump.SimStatus.ReportStr(),
                                    atime);
                        flag = 4;
                    }
                }

                // Check if system is unbalanced
                if (nss.Iter > this.pMap.MaxIter && nss.Relerr > this.pMap.HAcc) {
                    string str = string.Format(Error.ResourceManager.GetString("WARN01"), atime);

                    if (this.pMap.ExtraIter == -1)
                        str += Keywords.t_HALTED;

                    if (this.pMap.MessageFlag)
                        this.logger.Warning(str);

                    flag = 1;
                }
            }
            catch (ENException e) {}
        }

        /// <summary>Report hydraulic status.</summary>
        private void LogHydStat(NetSolveStep nss) {
            try {
                string atime = this.Htime.GetClockTime();
                if (nss.Iter > 0) {
                    if (nss.Relerr <= this.pMap.HAcc)
                        this.logger.Warning(Text.ResourceManager.GetString("FMT58"), atime, nss.Iter);
                    else
                        this.logger.Warning(Text.ResourceManager.GetString("FMT59"), atime, nss.Iter, nss.Relerr);
                }

                foreach (SimulationTank tank  in  this.tanks) {
                    Link.StatType newstat;

                    if (Math.Abs(tank.SimDemand) < 0.001)
                        newstat = Link.StatType.CLOSED;
                    else if (tank.SimDemand > 0.0)
                        newstat = Link.StatType.FILLING;
                    else if (tank.SimDemand < 0.0)
                        newstat = Link.StatType.EMPTYING;
                    else
                        newstat = tank.OldStat;

                    if (newstat != tank.OldStat) {
                        if (!tank.IsReservoir)
                            this.logger.Warning(
                                    Text.ResourceManager.GetString("FMT50"),
                                    atime,
                                    tank.Id,
                                    newstat.ReportStr(),
                                    (tank.SimHead - tank.Elevation) * this.fMap.GetUnits(FieldsMap.FieldType.HEAD),
                                    this.fMap.GetField(FieldsMap.FieldType.HEAD).Units);

                        else
                            this.logger.Warning(
                                    Text.ResourceManager.GetString("FMT51"),
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
                                    Text.ResourceManager.GetString("FMT52"),
                                    atime,
                                    link.Type.ParseStr(),
                                    link.Link.Id,
                                    link.SimStatus.ReportStr());
                        else
                            this.logger.Warning(
                                    Text.ResourceManager.GetString("FMT53"),
                                    atime,
                                    link.Type.ParseStr(),
                                    link.Link.Id,
                                    link.SimOldStatus.ReportStr(),
                                    link.SimStatus.ReportStr());
                        link.SimOldStatus = link.SimStatus;
                    }
                }
            }
            catch (ENException) {}
        }


        private void LogRelErr(NetSolveStep ret) {
            if (ret.Iter == 0) {
                this.logger.Warning(Text.ResourceManager.GetString("FMT64"), this.Htime.GetClockTime());
            }
            else {
                this.logger.Warning(Text.ResourceManager.GetString("FMT65"), ret.Iter, ret.Relerr);
            }
        }

        private void LogHydErr(int order) {
            try {
                if (this.pMap.MessageFlag)
                    this.logger.Warning(
                            Text.ResourceManager.GetString("FMT62"),
                            this.Htime.GetClockTime(),
                            this.nodes[order].Id);
            }
            catch (ENException) {}
            this.LogHydStat(new NetSolveStep(0, 0));
        }

        public List<SimulationNode> Nodes { get { return this.nodes; } }

        public List<SimulationLink> Links { get { return this.links; } }

        public List<SimulationRule> Rules { get { return this.rules; } }

        public List<SimulationControl> Controls { get { return this.controls; } }
    }

}