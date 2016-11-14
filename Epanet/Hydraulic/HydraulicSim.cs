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
using Epanet.Properties;
using org.addition.epanet.hydraulic.io;
using org.addition.epanet.hydraulic.models;
using org.addition.epanet.hydraulic.structures;
using org.addition.epanet.log;
using org.addition.epanet.network;
using org.addition.epanet.network.io;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.hydraulic {

///<summary>Hydraulic simulation class.</summary>
public class HydraulicSim {


    public const int BUFFER_SIZE = 512 * 1024; //512kb
    [NonSerialized]
    protected bool running;
    [NonSerialized]
    protected Thread runningThread;

    protected HydraulicSim() {
    }

    ///<summary>Step solving result</summary>
    protected class NetSolveStep {
        public NetSolveStep(int iter, double relerr) {
            this.iter = iter;
            this.relerr = relerr;
        }

        public int iter;
        public double relerr;
    }

    ///<summary>Event logger reference.</summary>
    protected TraceSource logger;


    protected List<SimulationNode> nNodes;
    protected List<SimulationLink> nLinks;
    protected List<SimulationPump> nPumps;
    protected List<SimulationTank> nTanks;
    protected List<SimulationNode> nJunctions;
    protected List<SimulationValve> nValves;


    protected List<SimulationControl> nControls;
    protected List<SimulationRule> nRules;
    protected IList<Curve> nCurves;


    ///<summary>Simulation conversion units.</summary>
    protected FieldsMap fMap;

    ///<summary>Simulation properties.</summary>
    protected PropertiesMap pMap;

    ///<summary>Energy cost time pattern.</summary>
    protected Pattern Epat;

    ///<summary>Linear system solver support class.</summary>
    protected SparseMatrix smat;

    ///<summary>Linear system variable storage class.</summary>
    protected LSVariables lsv;

    ///<summary>Current report time.</summary>
    protected long Rtime;

    ///<summary>Current hydraulic simulation time.</summary>
    protected long Htime;

    ///<summary>System wide demand.</summary>
    protected double Dsystem;

    ///<summary>Output stream of the hydraulic solution.</summary>
    protected BinaryWriter simulationOutput;

    ///<summary>Pipe headloss model calculator.</summary>
    protected PipeHeadModel pHLModel;

    ///<summary>Get current hydraulic simulation time.</summary>
    public long getHtime() {
        return this.Htime;
    }

    ///<summary>Get current report time.</summary>
    public long getRtime() {
        return this.Rtime;
    }


    /**
     * Init hydraulic simulation, preparing the linear solver and the hydraulic structures wrappers.
     *
     * @param net Hydraulic network reference.
     * @param log Logger reference.
     * @throws ENException
     */
    
    public HydraulicSim(Network net, TraceSource log) {
        this.running = false;
        this.logger = log;
        this.createSimulationNetwork(net);
    }

    protected void createSimulationNetwork(Network net) {
        this.nNodes = new List<SimulationNode>();
        this.nLinks = new List<SimulationLink>();
        this.nPumps = new List<SimulationPump>();
        this.nTanks = new List<SimulationTank>();
        this.nJunctions = new List<SimulationNode>();
        this.nValves = new List<SimulationValve>();
        this.nRules = new List<SimulationRule>();


        Dictionary<string, SimulationNode> nodesById = new Dictionary<string, SimulationNode>();
        foreach (Node n  in  net.Nodes) {
            SimulationNode node = SimulationNode.createIndexedNode(n, this.nNodes.Count);
            this.nNodes.Add(node);
            nodesById[node.Id] = node;

            if (node is SimulationTank)
                this.nTanks.Add((SimulationTank) node);
            else
                this.nJunctions.Add(node);
        }

        foreach (Link l  in  net.Links) {
            SimulationLink link = SimulationLink.createIndexedLink(nodesById, l, this.nLinks.Count);
            this.nLinks.Add(link);

            if (link is SimulationValve)
                this.nValves.Add((SimulationValve) link);
            else if (link is SimulationPump)
                this.nPumps.Add((SimulationPump) link);
        }

        foreach (Rule r  in  net.Rules) {
            SimulationRule rule = new SimulationRule(r, this.nLinks, this.nNodes);//, tmpLinks, tmpNodes);
            this.nRules.Add(rule);
        }

        this.nCurves = net.Curves;

        this.nControls = new List<SimulationControl>();

        foreach (Control ctr  in  net.Controls)
            this.nControls.Add(new SimulationControl(this.nNodes, this.nLinks, ctr));

        this.fMap = net.FieldsMap;
        this.pMap = net.PropertiesMap;
        this.Epat = net.GetPattern(this.pMap.EpatId);
        this.smat = new SparseMatrix(this.nNodes, this.nLinks, this.nJunctions.Count);
        this.lsv = new LSVariables(this.nNodes.Count, this.smat.getCoeffsCount());

        this.Htime = 0;

        switch (this.pMap.Formflag) {

            case PropertiesMap.FormType.HW:
            this.pHLModel = new HWModelCalculator();
                break;
            case PropertiesMap.FormType.DW:
            this.pHLModel = new DwModelCalculator();
                break;
            case PropertiesMap.FormType.CM:
            this.pHLModel = new CMModelCalculator();
                break;
        }


        foreach (SimulationLink link  in  this.nLinks) {
            link.initLinkFlow();
        }


        foreach (SimulationNode node  in  this.nJunctions) {
            if (node.getKe() > 0.0)
                node.setSimEmitter(1.0);
        }

        foreach (SimulationLink link  in  this.nLinks) {

            if ((link.Type == Link.LinkType.PRV ||
                    link.Type == Link.LinkType.PSV ||
                    link.Type == Link.LinkType.FCV)
                    &&
                    (link.Roughness != Constants.MISSING))
                link.SimStatus = Link.StatType.ACTIVE;


            if (link.SimStatus <= Link.StatType.CLOSED)
                link.SimFlow = Constants.QZERO;
            else if (Math.Abs(link.SimFlow) <= Constants.QZERO)
                link.initLinkFlow(link.SimStatus, link.SimSetting);

            link.SimOldStatus = link.SimStatus;
        }

        foreach (SimulationPump pump  in  this.nPumps) {
            for (int j = 0; j < 6; j++)
                pump.setEnergy(j, 0.0);
        }

        this.Htime = 0;
        this.Rtime = this.pMap.Rstep;
    }

    /**
     * Run hydraulic simuation.
     *
     * @param hyd Abstract file where the output of the hydraulic simulation will be writen.
     * @throws ENException
     */

    public void simulate(string hyd) {
        try {
            FileStream @out = new FileStream(hyd, FileMode.Create, FileAccess.Write, FileShare.Read,
                BUFFER_SIZE);

            using (@out)
            {
                this.simulate(@out);
            }
        } catch (IOException) {
            throw new ENException(ErrorCode.Err305);
        }
    }


    /**
     * Run hydraulic simuation.
     *
     * @param out output stream for the hydraulic simulation data.
     * @throws ENException, IOException 
     */
    public void simulate(Stream @out) {
        this.simulate( new BinaryWriter(@out));
    }

    public long simulateSingleStep() {

        if (!this.running)
            this.running = true;

        if (!this.runHyd()) {
            this.running = false;
            return 0;
        }

        long hydstep = 0;

        if (this.Htime < this.pMap.Duration)
            hydstep = this.timeStep();

        if (this.pMap.Duration == 0)
            SimulationPump.stepEnergy(this.pMap, this.fMap, this.Epat, this.nPumps, this.Htime, 0);
        else if (this.Htime < this.pMap.Duration)
            SimulationPump.stepEnergy(this.pMap, this.fMap, this.Epat, this.nPumps, this.Htime, hydstep);

        if (this.Htime < this.pMap.Duration) {
            this.Htime += hydstep;
            if (this.Htime >= this.Rtime)
                this.Rtime += this.pMap.Rstep;
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

    public void simulate(BinaryWriter @out) {
        bool halted = false;

        if (this.running)
            throw new InvalidOperationException("Already running");

        this.runningThread = Thread.CurrentThread;
        this.running = true;

        this.simulationOutput = @out;
        if (this.simulationOutput != null) {
            AwareStep.writeHeader(@out, this, this.pMap.Rstart, this.pMap.Rstep, this.pMap.Duration);
        }
//        writeHeader(simulationOutput);
        try {
            long tstep;
            do {
                if (!this.runHyd())
                    break;

                tstep = this.nextHyd();

                if (!this.running && tstep > 0)
                    halted = true;
            }
            while (this.running && tstep > 0);
        } catch (IOException e) {
            Debug.Print(e.ToString());
            throw new ENException(ErrorCode.Err1000);
        } finally {
            this.running = false;
            this.runningThread = null;
        }

        if (halted)
            throw new ENException(ErrorCode.Err1000);

    }


    /**
     * Halt hydraulic simulation.
     *
     * @throws InterruptedException
     */
    public void stopRunning() {
        this.running = false;
        if (this.runningThread != null && this.runningThread.IsAlive)
            this.runningThread.Join(1000);
    }

    ///<summary>Solves network hydraulics in a single time period.</summary>
    private bool runHyd() {

        // Find new demands & control actions
        this.computeDemands();
        this.computeControls();

        // Solve network hydraulic equations
        NetSolveStep nss = this.netSolve();

        // Report new status & save results
        if (this.pMap.Statflag != PropertiesMap.StatFlag.NO)
            this.logHydStat(nss);

        // If system unbalanced and no extra trials
        // allowed, then activate the Haltflag.
        if (nss.relerr > this.pMap.Hacc && this.pMap.ExtraIter == -1) {
            this.Htime = this.pMap.Duration;
            return false;
        }

        this.logHydWarn(nss);

        return true;
    }

    /**
     * Solve the linear equation system to compute the links flows and nodes heads.
     *
     * @return Solver steps and relative error
     * @throws ENException
     */
    protected NetSolveStep netSolve() {
        NetSolveStep ret = new NetSolveStep(0, 0);

        int nextCheck = this.pMap.CheckFreq;

        if (this.pMap.Statflag == PropertiesMap.StatFlag.FULL)
            this.logRelErr(ret);

        int maxTrials = this.pMap.MaxIter;

        if (this.pMap.ExtraIter > 0)
            maxTrials += this.pMap.ExtraIter;

        double relaxFactor = 1.0;
        int errcode = 0;
        ret.iter = 1;

        while (ret.iter <= maxTrials) {
            //Compute coefficient matrices A & F and solve A*H = F
            // where H = heads, A = Jacobian coeffs. derived from
            // head loss gradients, & F = flow correction terms.
            this.newCoeffs();

            //dumpMatrixCoeffs(new File("dumpMatrix.txt"),true);

            // Solution for H is returned in F from call to linsolve().
            errcode = this.smat.linsolve(this.nJunctions.Count, this.lsv.getAiiVector(), this.lsv.getAijVector(), this.lsv.getRHSCoeffs());

            // Ill-conditioning problem
            if (errcode > 0) {
                // If control valve causing problem, fix its status & continue,
                // otherwise end the iterations with no solution.
                if (SimulationValve.checkBadValve(this.pMap, this.logger, this.nValves, this.Htime, this.smat.getOrder(errcode)))
                    continue;
                else break;
            }

            // Update current solution.
            // (Row[i] = row of solution matrix corresponding to node i).
            foreach (SimulationNode node  in  this.nJunctions) {
                node.setSimHead(this.lsv.getRHSCoeff(this.smat.getRow(node.Index))); // Update heads
            }

            // Update flows
            ret.relerr = this.newFlows(relaxFactor);

            // Write convergence error to status report if called for
            if (this.pMap.Statflag == PropertiesMap.StatFlag.FULL)
                this.logRelErr(ret);

            relaxFactor = 1.0;

            bool valveChange = false;

            //  Apply solution damping & check for change in valve status
            if (this.pMap.DampLimit > 0.0) {
                if (ret.relerr <= this.pMap.DampLimit) {
                    relaxFactor = 0.6;
                    valveChange = SimulationValve.valveStatus(this.fMap, this.pMap, this.logger, this.nValves);
                }
            } else
                valveChange = SimulationValve.valveStatus(this.fMap, this.pMap, this.logger, this.nValves);

            // Check for convergence
            if (ret.relerr <= this.pMap.Hacc) {

                //  We have convergence. Quit if we are into extra iterations.
                if (ret.iter > this.pMap.MaxIter)
                    break;

                //  Quit if no status changes occur.
                bool statChange = false;

                if (valveChange)
                    statChange = true;

                if (SimulationLink.linkStatus(this.pMap, this.fMap, this.logger, this.nLinks))
                    statChange = true;

                if (SimulationControl.PSwitch(this.logger, this.pMap, this.fMap, this.nControls))
                    statChange = true;

                if (!statChange)
                    break;

                //  We have a status change so continue the iterations
                nextCheck = ret.iter + this.pMap.CheckFreq;
            } else if (ret.iter <= this.pMap.MaxCheck && ret.iter == nextCheck) {
                // No convergence yet. See if its time for a periodic status
                // check  on pumps, CV's, and pipes connected to tanks.
                SimulationLink.linkStatus(this.pMap, this.fMap, this.logger, this.nLinks);
                nextCheck += this.pMap.CheckFreq;
            }

            ret.iter++;
        }


        foreach (SimulationNode node  in  this.nJunctions)
            node.setSimDemand(node.getSimDemand() + node.getSimEmitter());

        if (errcode > 0) {
            this.logHydErr(this.smat.getOrder(errcode));
            errcode = 110;
            return ret;
        }

        if (errcode != 0)
            throw new ENException((ErrorCode)errcode);

        return ret;
    }


    ///<summary>Computes coefficients of linearized network eqns.</summary>
    void newCoeffs() {
        this.lsv.clear();

        foreach (SimulationLink link  in  this.nLinks) {
            link.SimInvHeadLoss = 0;
            link.SimFlowCorrection = 0;
        }

        SimulationLink.computeMatrixCoeffs(this.fMap, this.pMap, this.pHLModel, this.nLinks, this.nCurves, this.smat, this.lsv);   // Compute link coeffs.
        SimulationNode.computeEmitterCoeffs(this.pMap, this.nJunctions, this.smat, this.lsv);                       // Compute emitter coeffs.
        SimulationNode.computeNodeCoeffs(this.nJunctions, this.smat, this.lsv);                                // Compute node coeffs.
        SimulationValve.computeMatrixCoeffs(this.pMap, this.lsv, this.smat, this.nValves);                          // Compute valve coeffs.
    }

    ///<summary>Updates link flows after new nodal heads computed.</summary>
    double newFlows(double RelaxFactor) {

        foreach (SimulationTank node  in  this.nTanks)
            node.setSimDemand(0);

        double qsum = 0.0;
        double dqsum = 0.0;

        foreach (SimulationLink link  in  this.nLinks) {
            SimulationNode n1 = link.First;
            SimulationNode n2 = link.Second;

            double dh = n1.getSimHead() - n2.getSimHead();
            double dq = link.SimFlowCorrection - link.SimInvHeadLoss * dh;

            dq *= RelaxFactor;

            if (link is SimulationPump) {
                if (((SimulationPump) link).getPtype() == Pump.PumpType.CONST_HP && dq > link.SimFlow)
                    dq = link.SimFlow / 2.0;
            }

            link.SimFlow = link.SimFlow - dq;

            qsum += Math.Abs(link.SimFlow);
            dqsum += Math.Abs(dq);

            if (link.SimStatus > Link.StatType.CLOSED) {
                if (n1 is SimulationTank)
                    n1.setSimDemand(n1.getSimDemand() - link.SimFlow);
                if (n2 is SimulationTank)
                    n2.setSimDemand(n2.getSimDemand() + link.SimFlow);
            }
        }

        foreach (SimulationNode node  in  this.nJunctions) {

            if (node.getKe() == 0.0)
                continue;
            double dq = node.emitFlowChange(this.pMap);
            node.setSimEmitter(node.getSimEmitter() - dq);
            qsum += Math.Abs(node.getSimEmitter());
            dqsum += Math.Abs(dq);
        }

        if (qsum > this.pMap.Hacc)
            return (dqsum / qsum);
        else
            return (dqsum);

    }

    ///<summary>Implements simple controls based on time or tank levels.</summary>
    private void computeControls() {
        SimulationControl.StepActions(this.logger, this.fMap, this.pMap, this.nControls, this.Htime);
    }

    ///<summary>Computes demands at nodes during current time period.</summary>
    private void computeDemands() {
        // Determine total elapsed number of pattern periods
        long p = (this.Htime + this.pMap.Pstart) / this.pMap.Pstep;

        this.Dsystem = 0.0; //System-wide demand

        // Update demand at each node according to its assigned pattern
        foreach (SimulationNode node  in  this.nJunctions) {
            double sum = 0.0;
            foreach (Demand demand  in  node.getDemand()) {
                // pattern period (k) = (elapsed periods) modulus (periods per pattern)
                List<double> factors = demand.Pattern.FactorsList;

                long k = p % (long) factors.Count;
                double djunc = (demand.Base) * factors[(int) k] * this.pMap.Dmult;
                if (djunc > 0.0)
                    this.Dsystem += djunc;

                sum += djunc;
            }
            node.setSimDemand(sum);
        }

        // Update head at fixed grade nodes with time patterns
        foreach (SimulationTank tank  in  this.nTanks) {
            if (tank.getArea() == 0.0) {
                Pattern pat = tank.getPattern();
                if (pat != null) {
                    List<double> factors = pat.FactorsList;
                    long k = p % factors.Count;

                    tank.setSimHead(tank.getElevation() * factors[(int)k]);
                }
            }
        }

        // Update status of pumps with utilization patterns
        foreach (SimulationPump pump  in  this.nPumps) {
            if (pump.getUpat() != null) {
                List<Double> factors = pump.getUpat().FactorsList;
                int k = (int)(p % factors.Count);
                pump.setLinkSetting(factors[k]);
            }
        }
    }

    ///<summary>Finds length of next time step & updates tank levels and rule-based contol actions.</summary>
    protected long nextHyd() {
        long hydstep = 0;

        if (this.simulationOutput != null)
            AwareStep.write(this.simulationOutput, this, this.Htime);

        if (this.Htime < this.pMap.Duration)
            hydstep = this.timeStep();

        if (this.pMap.Duration == 0)
            SimulationPump.stepEnergy(this.pMap, this.fMap, this.Epat, this.nPumps, this.Htime, 0);
        else if (this.Htime < this.pMap.Duration)
            SimulationPump.stepEnergy(this.pMap, this.fMap, this.Epat, this.nPumps, this.Htime, hydstep);

        if (this.Htime < this.pMap.Duration) {
            this.Htime += hydstep;
            if (this.Htime >= this.Rtime)
                this.Rtime += this.pMap.Rstep;
        }

        return hydstep;
    }

    ///<summary>Computes time step to advance hydraulic simulation.</summary>
    long timeStep() {
        long tstep = this.pMap.Hstep;

        long n = ((this.Htime + this.pMap.Pstart) / this.pMap.Pstep) + 1;
        long t = n * this.pMap.Pstep - this.Htime;

        if (t > 0 && t < tstep)
            tstep = t;

        // Revise time step based on smallest time to fill or drain a tank
        t = this.Rtime - this.Htime;
        if (t > 0 && t < tstep) tstep = t;

        tstep = SimulationTank.minimumTimeStep(this.nTanks, tstep);
        tstep = SimulationControl.MinimumTimeStep(this.fMap, this.pMap, this.nControls, this.Htime, tstep);

        if (this.nRules.Count > 0) {
            SimulationRule.Result res = SimulationRule.minimumTimeStep(this.fMap, this.pMap, this.logger, this.nRules, this.nTanks, this.Htime, tstep, this.Dsystem);
            tstep = res.step;
            this.Htime = res.htime;
        } else
            SimulationTank.stepWaterLevels(this.nTanks, this.fMap, tstep);

        return (tstep);
    }


    ///<summary>Save current step simulation results in the temp hydfile.</summary>
    private void saveStep() {

        MemoryStream ms = new MemoryStream(this.nLinks.Count * 3 * sizeof(float) + this.nNodes.Count * 2 * sizeof(float) + sizeof(int));
        BinaryWriter bb = new BinaryWriter(ms);


        try {
            bb.Write((int)this.Htime);

            foreach (SimulationNode node  in  this.nNodes)
                bb.Write((float) node.getSimDemand());

            foreach (SimulationNode node  in  this.nNodes)
                bb.Write((float) node.getSimHead());

            foreach (SimulationLink link  in  this.nLinks)
                if (link.SimStatus <= Link.StatType.CLOSED)
                    bb.Write((float) 0.0);

                else
                    bb.Write((float) link.SimFlow);

            foreach (SimulationLink link  in  this.nLinks)
                bb.Write((int)link.SimStatus);

            foreach (SimulationLink link  in  this.nLinks)
                bb.Write((float) link.SimSetting);

            this.simulationOutput.Write(ms.GetBuffer());

        } catch (IOException) {
            throw new ENException(ErrorCode.Err308);
        }
    }


    // Report hydraulic warning.
    // Note: Warning conditions checked in following order:
    //  1. System balanced but unstable
    //  2. Negative pressures
    //  3. FCV cannot supply flow or PRV/PSV cannot maintain pressure
    //  4. Pump out of range
    //  5. Network disconnected
    //  6. System unbalanced
    private void logHydWarn(NetSolveStep nss) {
        try {
            int flag;

            string atime = this.Htime.GetClockTime();

            if (nss.iter > this.pMap.MaxIter && nss.relerr <= this.pMap.Hacc) {
                if (this.pMap.Messageflag)
                    this.logger.Warning(Error.ResourceManager.GetString("WARN02"), atime);
                flag = 2;
            }

            // Check for negative pressures
            foreach (SimulationNode node  in  this.nJunctions) {
                if (node.getSimHead() < node.getElevation() && node.getSimDemand() > 0.0) {
                    if (this.pMap.Messageflag)
                        this.logger.Warning(Error.ResourceManager.GetString("WARN06"), atime);
                    flag = 6;
                    break;
                }
            }

            // Check for abnormal valve condition
            foreach (SimulationValve valve  in  this.nValves) {
                int j = valve.Index;
                if (valve.SimStatus >= Link.StatType.XFCV) {
                    if (this.pMap.Messageflag)
                        this.logger.Warning(Error.ResourceManager.GetString("WARN05"), valve.Type.ParseStr(), valve.Link.Id,
                                valve.SimStatus.ReportStr(), atime);
                    flag = 5;
                }
            }

            // Check for abnormal pump condition
            foreach (SimulationPump pump  in  this.nPumps) {
                Link.StatType s = pump.SimStatus;
                if (pump.SimStatus >= Link.StatType.OPEN) {
                    if (pump.SimFlow > pump.SimSetting * pump.getQmax())
                        s = Link.StatType.XFLOW;
                    if (pump.SimFlow < 0.0)
                        s = Link.StatType.XHEAD;
                }

                if (s == Link.StatType.XHEAD || s == Link.StatType.XFLOW) {
                    if (this.pMap.Messageflag)
                        this.logger.Warning(Error.ResourceManager.GetString("WARN04"), pump.Link.Id, pump.SimStatus.ReportStr(), atime);
                    flag = 4;
                }
            }

            // Check if system is unbalanced
            if (nss.iter > this.pMap.MaxIter && nss.relerr > this.pMap.Hacc) {
                string str = string.Format(Error.ResourceManager.GetString("WARN01"), atime);

                if (this.pMap.ExtraIter == -1)
                    str += Keywords.t_HALTED;

                if (this.pMap.Messageflag)
                    this.logger.Warning(str);

                flag = 1;
            }
        } catch (ENException e) {
        }
    }

    // Report hydraulic status.
    private void logHydStat(NetSolveStep nss) {
        try {
            string atime = this.Htime.GetClockTime();
            if (nss.iter > 0) {
                if (nss.relerr <= this.pMap.Hacc)
                    this.logger.Warning(Text.ResourceManager.GetString("FMT58"), atime, nss.iter);
                else
                    this.logger.Warning(Text.ResourceManager.GetString("FMT59"), atime, nss.iter, nss.relerr);
            }

            foreach (SimulationTank tank  in  this.nTanks) {
                Link.StatType newstat;

                if (Math.Abs(tank.getSimDemand()) < 0.001)
                    newstat = Link.StatType.CLOSED;
                else if (tank.getSimDemand() > 0.0)
                    newstat = Link.StatType.FILLING;
                else if (tank.getSimDemand() < 0.0)
                    newstat = Link.StatType.EMPTYING;
                else
                    newstat = tank.getOldStat();

                if (newstat != tank.getOldStat()) {
                    if (!tank.isReservoir())
                        this.logger.Warning(Text.ResourceManager.GetString("FMT50"), atime, tank.Id, newstat.ReportStr(),
                            (tank.getSimHead() - tank.getElevation())*this.fMap.GetUnits(FieldsMap.FieldType.HEAD),
                            this.fMap.GetField(FieldsMap.FieldType.HEAD).Units);

                    else
                        this.logger.Warning(Text.ResourceManager.GetString("FMT51"), atime, tank.Id, newstat.ReportStr());

                    tank.setOldStat(newstat);
                }
            }

            foreach (SimulationLink link  in  this.nLinks) {
                if (link.SimStatus != link.SimOldStatus) {
                    if (this.Htime == 0)
                        this.logger.Warning(Text.ResourceManager.GetString("FMT52"),
                                atime,
                                link.Type.ParseStr(),
                                link.Link.Id,
                                link.SimStatus.ReportStr());
                    else
                        this.logger.Warning(Text.ResourceManager.GetString("FMT53"), atime,
                                link.Type.ParseStr(),
                                link.Link.Id,
                                link.SimOldStatus.ReportStr(),
                                link.SimStatus.ReportStr());
                    link.SimOldStatus = link.SimStatus;
                }
            }
        } catch (ENException e) {
        }
    }


    private void logRelErr(NetSolveStep ret) {
        if (ret.iter == 0) {
            this.logger.Warning(Text.ResourceManager.GetString("FMT64"), this.Htime.GetClockTime());
        } else {
            this.logger.Warning(Text.ResourceManager.GetString("FMT65"), ret.iter, ret.relerr);
        }
    }

    private void logHydErr(int order) {
        try {
            if (this.pMap.Messageflag)
                this.logger.Warning(Text.ResourceManager.GetString("FMT62"),
                        this.Htime.GetClockTime(), this.nNodes[order].Id);
        } catch (ENException e) {
        }
        this.logHydStat(new NetSolveStep(0, 0));
    }

    public List<SimulationNode> getnNodes() {
        return this.nNodes;
    }

    public List<SimulationLink> getnLinks() {
        return this.nLinks;
    }

    public List<SimulationRule> getnRules() {
        return this.nRules;
    }

    public List<SimulationControl> getnControls() {
        return this.nControls;
    }

}
}