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
using org.addition.epanet.hydraulic.models;
using org.addition.epanet.log;
using org.addition.epanet.network;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.hydraulic.structures {


public class SimulationLink {


    protected SimulationNode first = null;
    protected SimulationNode second = null;
    protected readonly Link link;
    protected readonly int index;

    protected Link.StatType status;         // Epanet 'S[k]', link current status
    protected double flow;           // Epanet 'Q[k]', link flow value
    protected double invHeadLoss;    // Epanet 'P[k]', Inverse headloss derivatives
    protected double flowCorrection; // Epanet 'Y[k]', Flow correction factors
    protected double setting;        // Epanet 'K[k]', Link setting
    protected Link.StatType oldStatus;


    public static SimulationLink createIndexedLink(Dictionary<string, SimulationNode> byId, Link @ref, int idx) {
        SimulationLink ret;
        if (@ref is Valve)
            ret = new SimulationValve(new List<SimulationNode>(byId.Values), @ref, idx);
        else if (@ref is Pump)
            ret = new SimulationPump(new List<SimulationNode>(byId.Values), @ref, idx);
        else
            ret = new SimulationLink(byId, @ref, idx);

        return ret;
    }

    public static SimulationLink createIndexedLink(List<SimulationNode> indexedNodes, Link @ref, int idx) {
        SimulationLink ret = null;
        if (@ref is Valve)
            ret = new SimulationValve(indexedNodes, @ref, idx);
        else if (@ref is Pump)
            ret = new SimulationPump(indexedNodes, @ref, idx);
        else
            ret = new SimulationLink(indexedNodes, @ref, idx);

        return ret;
    }

    public SimulationLink(Dictionary<string, SimulationNode> byId, Link @ref, int idx) {

        link = @ref;
        first = byId[link.getFirst().getId()];
        second = byId[link.getSecond().getId()];
        this.index = idx;

        // Init
        setting = link.getRoughness();
        status = link.getStat();

    }

    public SimulationLink(List<SimulationNode> indexedNodes, Link @ref, int idx) {
        link = @ref;

        foreach (SimulationNode indexedNode  in  indexedNodes) {
            if (indexedNode.getId() == link.getFirst().getId())
                first = indexedNode;
            else if (indexedNode.getId() == link.getSecond().getId())
                second = indexedNode;
            if (first != null && second != null) break;
        }
        this.index = idx;

        // Init
        setting = link.getRoughness();
        status = link.getStat();
    }

    // Indexed link methods

    public SimulationNode getFirst() {
        return first;
    }

    public SimulationNode getSecond() {
        return second;
    }

    public Link getLink() {
        return link;
    }

    public int getIndex() {
        return index;
    }

    // Network link Getters

    public double[] getC0() {
        return link.getC0();
    }

#if COMMENTED1
    public double[] getParam() { return node.getParam(); }
#endif

    public double getDiameter() {
        return link.getDiameter();
    }

#if COMMENTED1
    public double getLenght() { return node.getLenght(); }
#endif

    public double getRoughness() {
        return link.getRoughness();
    }

    public double getKm() {
        return link.getKm();
    }

#if COMMENTED1
    public double getKb() { return node.getKb(); }
    public double getKw() { return node.getKw(); }
#endif

    public double getFlowResistance() {
        return link.getFlowResistance();
    }

    public Link.LinkType getType() {
        return link.getType();
    }

#if COMMENTED1
    public Link.StatType getStat() { return node.getStat(); }
    public bool getRptFlag() { return node.isRptFlag(); }
#endif

    // Simulation getters & setters

    public Link.StatType getSimStatus() {
        return status;
    }

    public void setSimStatus(Link.StatType type) {
        status = type;
    }

    public double getSimFlow() {
        return flow;
    }

    public void setSimFlow(double flow) {
        this.flow = flow;
    }

    public double getSimSetting() {
        return setting;
    }

    public void setSimSetting(double value) {
        setting = value;
    }

    public double getSimInvHeadLoss() {
        return invHeadLoss;
    }

    public void setSimInvHeadLoss(double value) {
        invHeadLoss = value;
    }

    public double getSimFlowCorrection() {
        return flowCorrection;
    }

    public void setSimFlowCorrection(double value) {
        flowCorrection = value;
    }

    public Link.StatType getSimOldStatus() {
        return oldStatus;
    }

    public void setSimOldStatus(Link.StatType oldStatus) {
        this.oldStatus = oldStatus;
    }

    // Simulation Methods

    // Sets link status to OPEN(true) or CLOSED(false)
    public void setLinkStatus(bool value) {
        if (value) {
            if (this is SimulationPump)
                setting = 1.0;

            else if (getType() != Link.LinkType.GPV)
                setting = Constants.MISSING;

            status = Link.StatType.OPEN;
        } else {
            if (this is SimulationPump)
                setting = 0.0;
            else if (getType() != Link.LinkType.GPV)
                setting = Constants.MISSING;

            status = Link.StatType.CLOSED;
        }
    }

    // Sets pump speed or valve setting, adjusting link status and flow when necessary
    public void setLinkSetting(double value) {
        if (this is SimulationPump) {
            setting = value;
            if (value > 0 && status <= Link.StatType.CLOSED)
                status = Link.StatType.OPEN;

            if (value == 0 && status > Link.StatType.CLOSED)
                status = Link.StatType.CLOSED;

        } else if (getType() == Link.LinkType.FCV) {
            setting = value;
            status = Link.StatType.ACTIVE;
        } else {
            if (setting == Constants.MISSING && status <= Link.StatType.CLOSED)
                status = Link.StatType.OPEN;

            setting = value;
        }
    }

    // Sets initial flow in link to QZERO if link is closed, to design flow for a pump,
    // or to flow at velocity of 1 fps for other links.
    public void initLinkFlow() {
        if (getSimStatus() == Link.StatType.CLOSED)
            flow = Constants.QZERO;
        else if (this is SimulationPump)
            flow = getRoughness() * ((SimulationPump) this).getQ0();
        else
            flow = Constants.PI * Math.Pow(getDiameter(), 2) / 4.0;
    }

    public void initLinkFlow(Link.StatType type, double Kc) {
        if (type == Link.StatType.CLOSED)
            flow = Constants.QZERO;
        else if (this is SimulationPump)
            flow = Kc * ((SimulationPump) this).getQ0();
        else
            flow = Constants.PI * Math.Pow(getDiameter(), 2) / 4.0;
    }

//    public static long T1 = 0, T2 = 0, T3 = 0; //TODO:REMOVE THIS

    // Compute P, Y and matrix coeffs
    private void computeMatrixCoeff(FieldsMap fMap,
                                    PropertiesMap pMap,
                                    PipeHeadModel hlModel,
                                    Curve[] curves,
                                    SparseMatrix smat, LSVariables ls) {

        switch (getType()) {
            // Pipes
            case Link.LinkType.CV:
            case Link.LinkType.PIPE:
                computePipeCoeff(pMap, hlModel);
                break;
            // Pumps
            case Link.LinkType.PUMP:
                ((SimulationPump) this).computePumpCoeff(fMap, pMap);
                break;
            // Valves
            case Link.LinkType.PBV:
            case Link.LinkType.TCV:
            case Link.LinkType.GPV:
            case Link.LinkType.FCV:
            case Link.LinkType.PRV:
            case Link.LinkType.PSV:
                // If valve status fixed then treat as pipe
                // otherwise ignore the valve for now.
                if (!((SimulationValve) this).computeValveCoeff(fMap, pMap, curves))
                    return;
                break;
            default:
                return;
        }

        int n1 = first.getIndex();
        int n2 = second.getIndex();

        ls.addNodalInFlow(n1, -flow);
        ls.addNodalInFlow(n2, +flow);

        ls.addAij(smat.getNdx(getIndex()), -invHeadLoss);

        if (!(first is SimulationTank)) {
            ls.addAii(smat.getRow(n1), +invHeadLoss);
            ls.addRHSCoeff(smat.getRow(n1), +flowCorrection);
        } else
            ls.addRHSCoeff(smat.getRow(n2), +(invHeadLoss * first.getSimHead()));

        if (!(second is SimulationTank)) {
            ls.addAii(smat.getRow(n2), +invHeadLoss);
            ls.addRHSCoeff(smat.getRow(n2), -flowCorrection);
        } else
            ls.addRHSCoeff(smat.getRow(n1), +(invHeadLoss * second.getSimHead()));

    }

    // Computes P & Y coefficients for pipe k
    private void computePipeCoeff(PropertiesMap pMap, PipeHeadModel hlModel) {
        // For closed pipe use headloss formula: h = CBIG*q
        if (status <= Link.StatType.CLOSED) {
            invHeadLoss = 1.0 / Constants.CBIG;
            flowCorrection = flow;
            return;
        }

        PipeHeadModel.LinkCoeffs coeffs = hlModel.compute(pMap, this);
        invHeadLoss = coeffs.getInvHeadLoss();
        flowCorrection = coeffs.getFlowCorrection();
    }


    // Closes link flowing into full or out of empty tank
    private void tankStatus(PropertiesMap pMap) {
        double q = flow;
        SimulationNode n1 = getFirst();
        SimulationNode n2 = getSecond();

        // Make node n1 be the tank
        if (!(n1 is SimulationTank)) {
            if (!(n2 is SimulationTank))
                return;                      // neither n1 or n2 is a tank
            // N2 is a tank, swap !
            SimulationNode n = n1;
            n1 = n2;
            n2 = n;
            q = -q;
        }

        double h = n1.getSimHead() - n2.getSimHead();

        SimulationTank tank = (SimulationTank) n1;

        // Skip reservoirs & closed links
        if (tank.getArea() == 0.0 || status <= Link.StatType.CLOSED)
            return;

        // If tank full, then prevent flow into it
        if (tank.getSimHead() >= tank.getHmax() - pMap.getHtol()) {
            //Case 1: Link is a pump discharging into tank
            if (getType() == Link.LinkType.PUMP) {
                if (getSecond() == n1)
                    status = Link.StatType.TEMPCLOSED;
            } else if (cvStatus(pMap, Link.StatType.OPEN, h, q) == Link.StatType.CLOSED) //  Case 2: Downstream head > tank head
                status = Link.StatType.TEMPCLOSED;
        }

        // If tank empty, then prevent flow out of it
        if (tank.getSimHead() <= tank.getHmin() + pMap.getHtol()) {
            // Case 1: Link is a pump discharging from tank
            if (getType() == Link.LinkType.PUMP) {
                if (getFirst() == n1)
                    status = Link.StatType.TEMPCLOSED;
            }
            // Case 2: Tank head > downstream head
            else if (cvStatus(pMap, Link.StatType.CLOSED, h, q) == Link.StatType.OPEN)
                status = Link.StatType.TEMPCLOSED;
        }
    }

    // Updates status of a check valve.
    private static Link.StatType cvStatus(PropertiesMap pMap, Link.StatType s, double dh, double q) {
        if (Math.Abs(dh) > pMap.getHtol()) {
            if (dh < -pMap.getHtol())
                return (Link.StatType.CLOSED);
            else if (q < -pMap.getQtol())
                return (Link.StatType.CLOSED);
            else
                return (Link.StatType.OPEN);
        } else {
            if (q < -pMap.getQtol())
                return (Link.StatType.CLOSED);
            else
                return (s);
        }
    }

    // Determines new status for pumps, CVs, FCVs & pipes to tanks.
    private bool linkStatus(PropertiesMap pMap, FieldsMap fMap, TraceSource log) {
        bool change = false;

        double dh = first.getSimHead() - second.getSimHead();

        Link.StatType tStatus = status;

        if (tStatus == Link.StatType.XHEAD || tStatus == Link.StatType.TEMPCLOSED)
            status = Link.StatType.OPEN;

        if (getType() == Link.LinkType.CV)
            status = cvStatus(pMap, status, dh, flow);

        if (this is SimulationPump && status >= Link.StatType.OPEN && setting > 0.0)
            status = ((SimulationPump) this).pumpStatus(pMap, -dh);

        if (getType() == Link.LinkType.FCV && setting != Constants.MISSING)
            status = ((SimulationValve) this).fcvStatus(pMap, tStatus);

        if (first is SimulationTank || second is SimulationTank)
            tankStatus(pMap);

        if (tStatus != status) {
            change = true;
            if (pMap.getStatflag() == PropertiesMap.StatFlag.FULL)
                logStatChange(fMap, log, this, tStatus, status);
        }

        return (change);
    }

    protected static void logStatChange(FieldsMap fMap, TraceSource logger, SimulationLink link, Link.StatType s1, Link.StatType s2) {

        if (s1 == s2) {

            switch (link.getType()) {
                case Link.LinkType.PRV:
                case Link.LinkType.PSV:
                case Link.LinkType.PBV:
                    link.setting *= fMap.getUnits(FieldsMap.Type.PRESSURE);
                    break;
                case Link.LinkType.FCV:
                    link.setting *= fMap.getUnits(FieldsMap.Type.FLOW);
                    break;
            }
            logger.Verbose(Utilities.getText("FMT56"), link.getType().ParseStr(), link.getLink().getId(), link.setting);
            return;
        }

        Link.StatType j1, j2;

        if (s1 == Link.StatType.ACTIVE)
            j1 = Link.StatType.ACTIVE;
        else if (s1 <= Link.StatType.CLOSED)
            j1 = Link.StatType.CLOSED;
        else
            j1 = Link.StatType.OPEN;
        if (s2 == Link.StatType.ACTIVE) j2 = Link.StatType.ACTIVE;
        else if (s2 <= Link.StatType.CLOSED)
            j2 = Link.StatType.CLOSED;
        else
            j2 = Link.StatType.OPEN;

        if (j1 != j2) {
            logger.Verbose(Utilities.getText("FMT57"), link.getType().ParseStr(), link.getLink().getId(), j1.ReportStr(), j2.ReportStr());
        }

    }

    // Determines new status for pumps, CVs, FCVs & pipes to tanks.
    public static bool linkStatus(PropertiesMap pMap, FieldsMap fMap, TraceSource log, List<SimulationLink> links) {
        bool change = false;
        foreach (SimulationLink link  in  links) {
            if (link.linkStatus(pMap, fMap, log))
                change = true;
        }
        return change;
    }

    // Computes solution matrix coefficients for links
    public static void computeMatrixCoeffs(FieldsMap fMap,
                                           PropertiesMap pMap,
                                           PipeHeadModel hlModel,
                                           List<SimulationLink> links, Curve[] curves,
                                           SparseMatrix smat, LSVariables ls) {

        foreach (SimulationLink link  in  links) {
            link.computeMatrixCoeff(fMap, pMap, hlModel, curves, smat, ls);
        }
    }
}
}