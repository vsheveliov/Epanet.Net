/*
 * Copyright (C) 2012  Addition, Lda. (addition at addition dot pt)
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
    protected Curve[] nCurves;


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

    /**
     * Get current hydraulic simulation time.
     *
     * @return
     */
    public long getHtime() {
        return Htime;
    }

    /**
     * Get current report time.
     *
     * @return
     */
    public long getRtime() {
        return Rtime;
    }


    /**
     * Init hydraulic simulation, preparing the linear solver and the hydraulic structures wrappers.
     *
     * @param net Hydraulic network reference.
     * @param log Logger reference.
     * @throws ENException
     */
    
    public HydraulicSim(Network net, TraceSource log) {
        List<Node> tmpNodes = new List<Node>(net.getNodes());
        List<Link> tmpLinks = new List<Link>(net.getLinks());
        running = false;
        logger = log;
        createSimulationNetwork(tmpNodes, tmpLinks, net);
    }

    protected void createSimulationNetwork(List<Node> tmpNodes, List<Link> tmpLinks, Network net) {

        nNodes = new List<SimulationNode>();
        nLinks = new List<SimulationLink>();
        nPumps = new List<SimulationPump>();
        nTanks = new List<SimulationTank>();
        nJunctions = new List<SimulationNode>();
        nValves = new List<SimulationValve>();
        nRules = new List<SimulationRule>();


        Dictionary<string, SimulationNode> nodesById = new Dictionary<string, SimulationNode>();
        foreach (Node n  in  tmpNodes) {
            SimulationNode node = SimulationNode.createIndexedNode(n, nNodes.Count);
            nNodes.Add(node);
            nodesById[node.getId()] = node;

            if (node is SimulationTank)
                nTanks.Add((SimulationTank) node);
            else
                nJunctions.Add(node);
        }

        foreach (Link l  in  tmpLinks) {
            SimulationLink link = SimulationLink.createIndexedLink(nodesById, l, nLinks.Count);
            nLinks.Add(link);

            if (link is SimulationValve)
                nValves.Add((SimulationValve) link);
            else if (link is SimulationPump)
                nPumps.Add((SimulationPump) link);
        }

        foreach (Rule r  in  net.getRules()) {
            SimulationRule rule = new SimulationRule(r, nLinks, nNodes);//, tmpLinks, tmpNodes);
            nRules.Add(rule);
        }

        nCurves = net.getCurves();
        nControls = new List<SimulationControl>();

        foreach (Control ctr  in  net.getControls())
            nControls.Add(new SimulationControl(nNodes, nLinks, ctr));


        fMap = net.getFieldsMap();
        pMap = net.getPropertiesMap();
        Epat = net.getPattern(pMap.getEpatId());
        smat = new SparseMatrix(nNodes, nLinks, nJunctions.Count);
        lsv = new LSVariables(nNodes.Count, smat.getCoeffsCount());

        Htime = 0;

        switch (pMap.getFormflag()) {

            case PropertiesMap.FormType.HW:
                pHLModel = new HWModelCalculator();
                break;
            case PropertiesMap.FormType.DW:
                pHLModel = new DwModelCalculator();
                break;
            case PropertiesMap.FormType.CM:
                pHLModel = new CMModelCalculator();
                break;
        }


        foreach (SimulationLink link  in  nLinks) {
            link.initLinkFlow();
        }


        foreach (SimulationNode node  in  nJunctions) {
            if (node.getKe() > 0.0)
                node.setSimEmitter(1.0);
        }

        foreach (SimulationLink link  in  nLinks) {

            if ((link.getType() == Link.LinkType.PRV ||
                    link.getType() == Link.LinkType.PSV ||
                    link.getType() == Link.LinkType.FCV)
                    &&
                    (link.getRoughness() != Constants.MISSING))
                link.setSimStatus(Link.StatType.ACTIVE);


            if (link.getSimStatus() <= Link.StatType.CLOSED)
                link.setSimFlow(Constants.QZERO);
            else if (Math.Abs(link.getSimFlow()) <= Constants.QZERO)
                link.initLinkFlow(link.getSimStatus(), link.getSimSetting());

            link.setSimOldStatus(link.getSimStatus());
        }

        foreach (SimulationPump pump  in  nPumps) {
            for (int j = 0; j < 6; j++)
                pump.setEnergy(j, 0.0);
        }

        Htime = 0;
        Rtime = pMap.getRstep();
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
                simulate(@out);
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
        simulate( new BinaryWriter(@out));
    }

    public long simulateSingleStep() {

        if (!running)
            running = true;

        if (!runHyd()) {
            running = false;
            return 0;
        }

        long hydstep = 0;

        if (Htime < pMap.getDuration())
            hydstep = timeStep();

        if (pMap.getDuration() == 0)
            SimulationPump.stepEnergy(pMap, fMap, Epat, nPumps, Htime, 0);
        else if (Htime < pMap.getDuration())
            SimulationPump.stepEnergy(pMap, fMap, Epat, nPumps, Htime, hydstep);

        if (Htime < pMap.getDuration()) {
            Htime += hydstep;
            if (Htime >= Rtime)
                Rtime += pMap.getRstep();
        }

        long tstep = hydstep;

        if (!running && tstep > 0) {
            running = false;
            return 0;
        }

        if (running && tstep > 0)
            return tstep;
        else {
            running = false;
            return 0;
        }
    }

    public void simulate(BinaryWriter @out) {
        bool halted = false;

        if (running)
            throw new InvalidOperationException("Already running");

        runningThread = Thread.CurrentThread;
        running = true;

        simulationOutput = @out;
        if (simulationOutput != null) {
            AwareStep.writeHeader(@out, this, pMap.getRstart(), pMap.getRstep(), pMap.getDuration());
        }
//        writeHeader(simulationOutput);
        try {
            long tstep;
            do {
                if (!runHyd())
                    break;

                tstep = nextHyd();

                if (!running && tstep > 0)
                    halted = true;
            }
            while (running && tstep > 0);
        } catch (IOException e) {
            Debug.Print(e.ToString());
            throw new ENException(ErrorCode.Err1000);
        } finally {
            running = false;
            runningThread = null;
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
        running = false;
        if (runningThread != null && runningThread.IsAlive)
            runningThread.Join(1000);
    }

    ///<summary>Solves network hydraulics in a single time period.</summary>
    private bool runHyd() {

        // Find new demands & control actions
        computeDemands();
        computeControls();

        // Solve network hydraulic equations
        NetSolveStep nss = netSolve();

        // Report new status & save results
        if (pMap.getStatflag() != PropertiesMap.StatFlag.NO)
            logHydStat(nss);

        // If system unbalanced and no extra trials
        // allowed, then activate the Haltflag.
        if (nss.relerr > pMap.getHacc() && pMap.getExtraIter() == -1) {
            Htime = pMap.getDuration();
            return false;
        }

        logHydWarn(nss);

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

        int nextCheck = pMap.getCheckFreq();

        if (pMap.getStatflag() == PropertiesMap.StatFlag.FULL)
            logRelErr(ret);

        int maxTrials = pMap.getMaxIter();

        if (pMap.getExtraIter() > 0)
            maxTrials += pMap.getExtraIter();

        double relaxFactor = 1.0;
        int errcode = 0;
        ret.iter = 1;

        while (ret.iter <= maxTrials) {
            //Compute coefficient matrices A & F and solve A*H = F
            // where H = heads, A = Jacobian coeffs. derived from
            // head loss gradients, & F = flow correction terms.
            newCoeffs();

            //dumpMatrixCoeffs(new File("dumpMatrix.txt"),true);

            // Solution for H is returned in F from call to linsolve().
            errcode = this.smat.linsolve(this.nJunctions.Count, this.lsv.getAiiVector(), this.lsv.getAijVector(), this.lsv.getRHSCoeffs());

            // Ill-conditioning problem
            if (errcode > 0) {
                // If control valve causing problem, fix its status & continue,
                // otherwise end the iterations with no solution.
                if (SimulationValve.checkBadValve(pMap, logger, nValves, Htime, smat.getOrder(errcode)))
                    continue;
                else break;
            }

            // Update current solution.
            // (Row[i] = row of solution matrix corresponding to node i).
            foreach (SimulationNode node  in  nJunctions) {
                node.setSimHead(lsv.getRHSCoeff(smat.getRow(node.getIndex()))); // Update heads
            }

            // Update flows
            ret.relerr = newFlows(relaxFactor);

            // Write convergence error to status report if called for
            if (pMap.getStatflag() == PropertiesMap.StatFlag.FULL)
                logRelErr(ret);

            relaxFactor = 1.0;

            bool valveChange = false;

            //  Apply solution damping & check for change in valve status
            if (pMap.getDampLimit() > 0.0) {
                if (ret.relerr <= pMap.getDampLimit()) {
                    relaxFactor = 0.6;
                    valveChange = SimulationValve.valveStatus(fMap, pMap, logger, nValves);
                }
            } else
                valveChange = SimulationValve.valveStatus(fMap, pMap, logger, nValves);

            // Check for convergence
            if (ret.relerr <= pMap.getHacc()) {

                //  We have convergence. Quit if we are into extra iterations.
                if (ret.iter > pMap.getMaxIter())
                    break;

                //  Quit if no status changes occur.
                bool statChange = false;

                if (valveChange)
                    statChange = true;

                if (SimulationLink.linkStatus(pMap, fMap, logger, nLinks))
                    statChange = true;

                if (SimulationControl.pSwitch(logger, pMap, fMap, nControls))
                    statChange = true;

                if (!statChange)
                    break;

                //  We have a status change so continue the iterations
                nextCheck = ret.iter + pMap.getCheckFreq();
            } else if (ret.iter <= pMap.getMaxCheck() && ret.iter == nextCheck) {
                // No convergence yet. See if its time for a periodic status
                // check  on pumps, CV's, and pipes connected to tanks.
                SimulationLink.linkStatus(pMap, fMap, logger, nLinks);
                nextCheck += pMap.getCheckFreq();
            }

            ret.iter++;
        }


        foreach (SimulationNode node  in  nJunctions)
            node.setSimDemand(node.getSimDemand() + node.getSimEmitter());

        if (errcode > 0) {
            logHydErr(smat.getOrder(errcode));
            errcode = 110;
            return ret;
        }

        if (errcode != 0)
            throw new ENException((ErrorCode)errcode);

        return ret;
    }


    ///<summary>Computes coefficients of linearized network eqns.</summary>
    void newCoeffs() {
        lsv.clear();

        foreach (SimulationLink link  in  nLinks) {
            link.setSimInvHeadLoss(0);
            link.setSimFlowCorrection(0);
        }

        SimulationLink.computeMatrixCoeffs(fMap, pMap, pHLModel, nLinks, nCurves, smat, lsv);   // Compute link coeffs.
        SimulationNode.computeEmitterCoeffs(pMap, nJunctions, smat, lsv);                       // Compute emitter coeffs.
        SimulationNode.computeNodeCoeffs(nJunctions, smat, lsv);                                // Compute node coeffs.
        SimulationValve.computeMatrixCoeffs(pMap, lsv, smat, nValves);                          // Compute valve coeffs.
    }

    ///<summary>Updates link flows after new nodal heads computed.</summary>
    double newFlows(double RelaxFactor) {

        foreach (SimulationTank node  in  nTanks)
            node.setSimDemand(0);

        double qsum = 0.0;
        double dqsum = 0.0;

        foreach (SimulationLink link  in  nLinks) {
            SimulationNode n1 = link.getFirst();
            SimulationNode n2 = link.getSecond();

            double dh = n1.getSimHead() - n2.getSimHead();
            double dq = link.getSimFlowCorrection() - link.getSimInvHeadLoss() * dh;

            dq *= RelaxFactor;

            if (link is SimulationPump) {
                if (((SimulationPump) link).getPtype() == Pump.Type.CONST_HP && dq > link.getSimFlow())
                    dq = link.getSimFlow() / 2.0;
            }

            link.setSimFlow(link.getSimFlow() - dq);

            qsum += Math.Abs(link.getSimFlow());
            dqsum += Math.Abs(dq);

            if (link.getSimStatus() > Link.StatType.CLOSED) {
                if (n1 is SimulationTank)
                    n1.setSimDemand(n1.getSimDemand() - link.getSimFlow());
                if (n2 is SimulationTank)
                    n2.setSimDemand(n2.getSimDemand() + link.getSimFlow());
            }
        }

        foreach (SimulationNode node  in  nJunctions) {

            if (node.getKe() == 0.0)
                continue;
            double dq = node.emitFlowChange(pMap);
            node.setSimEmitter(node.getSimEmitter() - dq);
            qsum += Math.Abs(node.getSimEmitter());
            dqsum += Math.Abs(dq);
        }

        if (qsum > pMap.getHacc())
            return (dqsum / qsum);
        else
            return (dqsum);

    }

    ///<summary>Implements simple controls based on time or tank levels.</summary>
    private void computeControls() {
        SimulationControl.stepActions(logger, fMap, pMap, nControls, Htime);
    }

    ///<summary>Computes demands at nodes during current time period.</summary>
    private void computeDemands() {
        // Determine total elapsed number of pattern periods
        long p = (Htime + pMap.getPstart()) / pMap.getPstep();

        Dsystem = 0.0; //System-wide demand

        // Update demand at each node according to its assigned pattern
        foreach (SimulationNode node  in  nJunctions) {
            double sum = 0.0;
            foreach (Demand demand  in  node.getDemand()) {
                // pattern period (k) = (elapsed periods) modulus (periods per pattern)
                List<double> factors = demand.getPattern().getFactorsList();

                long k = p % (long) factors.Count;
                double djunc = (demand.getBase()) * factors[(int) k] * pMap.getDmult();
                if (djunc > 0.0)
                    Dsystem += djunc;

                sum += djunc;
            }
            node.setSimDemand(sum);
        }

        // Update head at fixed grade nodes with time patterns
        foreach (SimulationTank tank  in  nTanks) {
            if (tank.getArea() == 0.0) {
                Pattern pat = tank.getPattern();
                if (pat != null) {
                    List<double> factors = pat.getFactorsList();
                    long k = p % factors.Count;

                    tank.setSimHead(tank.getElevation() * factors[(int)k]);
                }
            }
        }

        // Update status of pumps with utilization patterns
        foreach (SimulationPump pump  in  nPumps) {
            if (pump.getUpat() != null) {
                List<Double> factors = pump.getUpat().getFactorsList();
                int k = (int)(p % factors.Count);
                pump.setLinkSetting(factors[k]);
            }
        }
    }

    ///<summary>Finds length of next time step & updates tank levels and rule-based contol actions.</summary>
    protected long nextHyd() {
        long hydstep = 0;

        if (simulationOutput != null)
            AwareStep.write(simulationOutput, this, this.Htime);

        if (Htime < pMap.getDuration())
            hydstep = timeStep();

        if (pMap.getDuration() == 0)
            SimulationPump.stepEnergy(pMap, fMap, Epat, nPumps, Htime, 0);
        else if (Htime < pMap.getDuration())
            SimulationPump.stepEnergy(pMap, fMap, Epat, nPumps, Htime, hydstep);

        if (Htime < pMap.getDuration()) {
            Htime += hydstep;
            if (Htime >= Rtime)
                Rtime += pMap.getRstep();
        }

        return hydstep;
    }

    ///<summary>Computes time step to advance hydraulic simulation.</summary>
    long timeStep() {
        long tstep = pMap.getHstep();

        long n = ((Htime + pMap.getPstart()) / pMap.getPstep()) + 1;
        long t = n * pMap.getPstep() - Htime;

        if (t > 0 && t < tstep)
            tstep = t;

        // Revise time step based on smallest time to fill or drain a tank
        t = Rtime - Htime;
        if (t > 0 && t < tstep) tstep = t;

        tstep = SimulationTank.minimumTimeStep(nTanks, tstep);
        tstep = SimulationControl.minimumTimeStep(fMap, pMap, nControls, Htime, tstep);

        if (nRules.Count > 0) {
            SimulationRule.Result res = SimulationRule.minimumTimeStep(fMap, pMap, logger, nRules, nTanks, Htime, tstep, Dsystem);
            tstep = res.step;
            Htime = res.htime;
        } else
            SimulationTank.stepWaterLevels(nTanks, fMap, tstep);

        return (tstep);
    }


    ///<summary>Save current step simulation results in the temp hydfile.</summary>
    private void saveStep() {

        MemoryStream ms = new MemoryStream(nLinks.Count * 3 * sizeof(float) + nNodes.Count * 2 * sizeof(float) + sizeof(int));
        BinaryWriter bb = new BinaryWriter(ms);


        try {
            bb.Write((int) Htime);

            foreach (SimulationNode node  in  nNodes)
                bb.Write((float) node.getSimDemand());

            foreach (SimulationNode node  in  nNodes)
                bb.Write((float) node.getSimHead());

            foreach (SimulationLink link  in  nLinks)
                if (link.getSimStatus() <= Link.StatType.CLOSED)
                    bb.Write((float) 0.0);

                else
                    bb.Write((float) link.getSimFlow());

            foreach (SimulationLink link  in  nLinks)
                bb.Write((int)link.getSimStatus());

            foreach (SimulationLink link  in  nLinks)
                bb.Write((float) link.getSimSetting());
            
            simulationOutput.Write(ms.GetBuffer());

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

            string atime = this.Htime.getClockTime();

            if (nss.iter > pMap.getMaxIter() && nss.relerr <= pMap.getHacc()) {
                if (pMap.getMessageflag())
                    logger.Warning(Utilities.getError("WARN02"), atime);
                flag = 2;
            }

            // Check for negative pressures
            foreach (SimulationNode node  in  nJunctions) {
                if (node.getSimHead() < node.getElevation() && node.getSimDemand() > 0.0) {
                    if (pMap.getMessageflag())
                        logger.Warning(Utilities.getError("WARN06"), atime);
                    flag = 6;
                    break;
                }
            }

            // Check for abnormal valve condition
            foreach (SimulationValve valve  in  nValves) {
                int j = valve.getIndex();
                if (valve.getSimStatus() >= Link.StatType.XFCV) {
                    if (pMap.getMessageflag())
                        logger.Warning(Utilities.getError("WARN05"), valve.getType().ParseStr(), valve.getLink().getId(),
                                valve.getSimStatus().ReportStr(), atime);
                    flag = 5;
                }
            }

            // Check for abnormal pump condition
            foreach (SimulationPump pump  in  nPumps) {
                Link.StatType s = pump.getSimStatus();
                if (pump.getSimStatus() >= Link.StatType.OPEN) {
                    if (pump.getSimFlow() > pump.getSimSetting() * pump.getQmax())
                        s = Link.StatType.XFLOW;
                    if (pump.getSimFlow() < 0.0)
                        s = Link.StatType.XHEAD;
                }

                if (s == Link.StatType.XHEAD || s == Link.StatType.XFLOW) {
                    if (pMap.getMessageflag())
                        logger.Warning(Utilities.getError("WARN04"), pump.getLink().getId(), pump.getSimStatus().ReportStr(), atime);
                    flag = 4;
                }
            }

            // Check if system is unbalanced
            if (nss.iter > pMap.getMaxIter() && nss.relerr > pMap.getHacc()) {
                string str = string.Format(Utilities.getError("WARN01"), atime);

                if (pMap.getExtraIter() == -1)
                    str += Keywords.t_HALTED;

                if (pMap.getMessageflag())
                    logger.Warning(str);

                flag = 1;
            }
        } catch (ENException e) {
        }
    }

    // Report hydraulic status.
    private void logHydStat(NetSolveStep nss) {
        try {
            string atime = this.Htime.getClockTime();
            if (nss.iter > 0) {
                if (nss.relerr <= pMap.getHacc())
                    logger.Warning(Utilities.getText("FMT58"), atime, nss.iter);
                else
                    logger.Warning(Utilities.getText("FMT59"), atime, nss.iter, nss.relerr);
            }

            foreach (SimulationTank tank  in  nTanks) {
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
                        logger.Warning(Utilities.getText("FMT50"), atime, tank.getId(), newstat.ReportStr(),
                            (tank.getSimHead() - tank.getElevation())*fMap.getUnits(FieldsMap.Type.HEAD),
                            fMap.getField(FieldsMap.Type.HEAD).getUnits());

                    else
                        logger.Warning(Utilities.getText("FMT51"), atime, tank.getId(), newstat.ReportStr());

                    tank.setOldStat(newstat);
                }
            }

            foreach (SimulationLink link  in  nLinks) {
                if (link.getSimStatus() != link.getSimOldStatus()) {
                    if (Htime == 0)
                        logger.Warning(Utilities.getText("FMT52"),
                                atime,
                                link.getType().ParseStr(),
                                link.getLink().getId(),
                                link.getSimStatus().ReportStr());
                    else
                        logger.Warning(Utilities.getText("FMT53"), atime,
                                link.getType().ParseStr(),
                                link.getLink().getId(),
                                link.getSimOldStatus().ReportStr(),
                                link.getSimStatus().ReportStr());
                    link.setSimOldStatus(link.getSimStatus());
                }
            }
        } catch (ENException e) {
        }
    }


    private void logRelErr(NetSolveStep ret) {
        if (ret.iter == 0) {
            logger.Warning(Utilities.getText("FMT64"), this.Htime.getClockTime());
        } else {
            logger.Warning(Utilities.getText("FMT65"), ret.iter, ret.relerr);
        }
    }

    private void logHydErr(int order) {
        try {
            if (pMap.getMessageflag())
                logger.Warning(Utilities.getText("FMT62"),
                        this.Htime.getClockTime(), nNodes[order].getId());
        } catch (ENException e) {
        }
        logHydStat(new NetSolveStep(0, 0));
    }

    public List<SimulationNode> getnNodes() {
        return nNodes;
    }

    public List<SimulationLink> getnLinks() {
        return nLinks;
    }

    public List<SimulationRule> getnRules() {
        return nRules;
    }

    public List<SimulationControl> getnControls() {
        return nControls;
    }

}
}