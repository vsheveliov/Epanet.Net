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
using org.addition.epanet.log;
using org.addition.epanet.network;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.hydraulic.structures {


/**
 *
 */
public class SimulationValve : SimulationLink {

    public SimulationValve(List<SimulationNode> indexedNodes, Link @ref, int idx)
        : base(indexedNodes, @ref, idx)
    {
        
    }


    // Computes solution matrix coeffs. for a completely open, closed, or throttled control valve.
    protected void valveCoeff(PropertiesMap pMap) {
        double p;

        // Valve is closed. Use a very small matrix coeff.
        if (status <= Link.StatType.CLOSED) {
            invHeadLoss = 1.0 / Constants.CBIG;
            flowCorrection = flow;
            return;
        }

        // Account for any minor headloss through the valve
        if (getKm() > 0.0) {
            p = 2.0 * getKm() * Math.Abs(flow);
            if (p < pMap.getRQtol())
                p = pMap.getRQtol();

            invHeadLoss = 1.0 / p;
            flowCorrection = flow / 2.0;
        } else {
            invHeadLoss = 1.0 / pMap.getRQtol();
            flowCorrection = flow;
        }
    }

    // Computes solution matrix coeffs. for a completely open, closed, or throttled control valve.
    private void valveCoeff(PropertiesMap pMap, double km) {
        double p;

        // Valve is closed. Use a very small matrix coeff.
        if (status <= Link.StatType.CLOSED) {
            invHeadLoss = 1.0 / Constants.CBIG;
            flowCorrection = flow;
            return;
        }

        // Account for any minor headloss through the valve
        if (km > 0.0) {
            p = 2.0 * km * Math.Abs(flow);
            if (p < pMap.getRQtol())
                p = pMap.getRQtol();

            invHeadLoss = 1.0 / p;
            flowCorrection = flow / 2.0;
        } else {
            invHeadLoss = 1.0 / pMap.getRQtol();
            flowCorrection = flow;
        }
    }

    // Computes P & Y coeffs. for pressure breaker valve
    void pbvCoeff(PropertiesMap pMap) {
        if (setting == Constants.MISSING || setting == 0.0)
            valveCoeff(pMap);
        else if (getKm() * (flow * flow) > setting)
            valveCoeff(pMap);
        else {
            invHeadLoss = Constants.CBIG;
            flowCorrection = setting * Constants.CBIG;
        }
    }

    // Computes P & Y coeffs. for throttle control valve
    void tcvCoeff(PropertiesMap pMap) {
        double km = getKm();

        if (setting != Constants.MISSING)
            km = (0.02517 * setting / Math.Pow(getDiameter(), 4));

        valveCoeff(pMap, km);
    }

    // Computes P & Y coeffs. for general purpose valve
    void gpvCoeff(FieldsMap fMap, PropertiesMap pMap, Curve[] curves) {
        if (status == Link.StatType.CLOSED)
            valveCoeff(pMap);
        else {
            double q = Math.Max(Math.Abs(flow), Constants.TINY);
            Curve.Coeffs coeffs = curves[(int) Math.Round(setting)].getCoeff(fMap, q);
            invHeadLoss = 1.0 / Math.Max(coeffs.r, pMap.getRQtol());
            flowCorrection = invHeadLoss * (coeffs.h0 + coeffs.r * q) * Utilities.getSignal(flow);
        }
    }

    // Updates status of a flow control valve.
    public Link.StatType fcvStatus(PropertiesMap pMap, Link.StatType s) {
        Link.StatType status;
        status = s;
        if (getFirst().getSimHead() - getSecond().getSimHead() < -pMap.getHtol()) status = Link.StatType.XFCV;
        else if (flow < -pMap.getQtol()) status = Link.StatType.XFCV;
        else if (s == Link.StatType.XFCV && flow >= setting) status = Link.StatType.ACTIVE;
        return (status);
    }


    // Computes solution matrix coeffs. for pressure reducing valves
    void prvCoeff(PropertiesMap pMap, LSVariables ls, SparseMatrix smat) {
        int k = getIndex();
        int i = smat.getRow(first.getIndex());
        int j = smat.getRow(second.getIndex());

        double hset = second.getElevation() + setting;

        if (status == Link.StatType.ACTIVE) {

            invHeadLoss = 0.0;
            flowCorrection = flow + ls.getNodalInFlow(second);
            ls.addRHSCoeff(j, +(hset * Constants.CBIG));
            ls.addAii(j, +Constants.CBIG);
            if (ls.getNodalInFlow(second) < 0.0)
                ls.addRHSCoeff(i, +ls.getNodalInFlow(second));
            return;
        }

        valveCoeff(pMap);

        ls.addAij(smat.getNdx(k), -invHeadLoss);
        ls.addAii(i, +invHeadLoss);
        ls.addAii(j, +invHeadLoss);
        ls.addRHSCoeff(i, +(flowCorrection - flow));
        ls.addRHSCoeff(j, -(flowCorrection - flow));
    }


    // Computes solution matrix coeffs. for pressure sustaining valve
    void psvCoeff(PropertiesMap pMap, LSVariables ls, SparseMatrix smat) {
        int k = getIndex();
        int i = smat.getRow(first.getIndex());
        int j = smat.getRow(second.getIndex());
        double hset = first.getElevation() + setting;

        if (status == Link.StatType.ACTIVE) {
            invHeadLoss = 0.0;
            flowCorrection = flow - ls.getNodalInFlow(first);
            ls.addRHSCoeff(i, +(hset * Constants.CBIG));
            ls.addAii(i, +Constants.CBIG);
            if (ls.getNodalInFlow(first) > 0.0) ls.addRHSCoeff(j, +ls.getNodalInFlow(first));
            return;
        }

        valveCoeff(pMap);
        ls.addAij(smat.getNdx(k), -invHeadLoss);
        ls.addAii(i, +invHeadLoss);
        ls.addAii(j, +invHeadLoss);
        ls.addRHSCoeff(i, +(flowCorrection - flow));
        ls.addRHSCoeff(j, -(flowCorrection - flow));
    }

    // computes solution matrix coeffs. for flow control valve
    void fcvCoeff(PropertiesMap pMap, LSVariables ls, SparseMatrix smat) {
        int k = getIndex();
        double q = setting;
        int i = smat.getRow(first.getIndex());
        int j = smat.getRow(second.getIndex());

        // If valve active, break network at valve and treat
        // flow setting as external demand at upstream node
        // and external supply at downstream node.
        if (status == Link.StatType.ACTIVE) {
            ls.addNodalInFlow(first.getIndex(), -q);
            ls.addRHSCoeff(i, -q);
            ls.addNodalInFlow(second.getIndex(), +q);
            ls.addRHSCoeff(j, +q);
            invHeadLoss = 1.0 / Constants.CBIG;
            ls.addAij(smat.getNdx(k), -invHeadLoss);
            ls.addAii(i, +invHeadLoss);
            ls.addAii(j, +invHeadLoss);
            flowCorrection = flow - q;
        } else {
            //  Otherwise treat valve as an open pipe
            valveCoeff(pMap);
            ls.addAij(smat.getNdx(k), -invHeadLoss);
            ls.addAii(i, +invHeadLoss);
            ls.addAii(j, +invHeadLoss);
            ls.addRHSCoeff(i, +(flowCorrection - flow));
            ls.addRHSCoeff(j, -(flowCorrection - flow));
        }
    }

    // Determines if a node belongs to an active control valve
    // whose setting causes an inconsistent set of eqns. If so,
    // the valve status is fixed open and a warning condition
    // is generated.
    public static bool checkBadValve(PropertiesMap pMap, TraceSource log, List<SimulationValve> valves, long Htime, int n) {
        foreach (SimulationValve link  in  valves) {
            SimulationNode n1 = link.getFirst();
            SimulationNode n2 = link.getSecond();
            if (n == n1.getIndex() || n == n2.getIndex()) {
                if (link.getType() == Link.LinkType.PRV || link.getType() == Link.LinkType.PSV || link.getType() == Link.LinkType.FCV) {
                    if (link.status == Link.StatType.ACTIVE) {
                        if (pMap.getStatflag() == PropertiesMap.StatFlag.FULL) {
                            logBadValve(log, link, Htime);
                        }
                        if (link.getType() == Link.LinkType.FCV)
                            link.status = Link.StatType.XFCV;
                        else
                            link.status = Link.StatType.XPRESSURE;
                        return true;
                    }
                }
                return false;
            }
        }

        return false;
    }

    private static void logBadValve(TraceSource log, SimulationLink link, long Htime) {
        log.Warning(Utilities.getText("FMT61"), Htime.getClockTime(), link.getLink().getId());
    }

    // Updates status of a pressure reducing valve.
    private Link.StatType prvStatus(PropertiesMap pMap, double hset) {
        if (setting == Constants.MISSING)
            return (status);

        double htol = pMap.getHtol();
        double hml = getKm() * (flow * flow);
        double h1 = first.getSimHead();
        double h2 = second.getSimHead();

        Link.StatType tStatus = status;
        switch (status) {
            case Link.StatType.ACTIVE:
                if (flow < -pMap.getQtol())
                    tStatus = Link.StatType.CLOSED;
                else if (h1 - hml < hset - htol)
                    tStatus = Link.StatType.OPEN;
                else
                    tStatus = Link.StatType.ACTIVE;
                break;
            case Link.StatType.OPEN:
                if (flow < -pMap.getQtol())
                    tStatus = Link.StatType.CLOSED;
                else if (h2 >= hset + htol)
                    tStatus = Link.StatType.ACTIVE;
                else
                    tStatus = Link.StatType.OPEN;
                break;
            case Link.StatType.CLOSED:
                if (h1 >= hset + htol && h2 < hset - htol)
                    tStatus = Link.StatType.ACTIVE;
                else if (h1 < hset - htol && h1 > h2 + htol)
                    tStatus = Link.StatType.OPEN;
                else
                    tStatus = Link.StatType.CLOSED;
                break;
            case Link.StatType.XPRESSURE:
                if (flow < -pMap.getQtol())
                    tStatus = Link.StatType.CLOSED;
                break;
        }
        return (tStatus);
    }

    // Updates status of a pressure sustaining valve.
    private Link.StatType psvStatus(PropertiesMap pMap, double hset) {
        if (setting == Constants.MISSING)
            return (status);

        double h1 = first.getSimHead();
        double h2 = second.getSimHead();
        double htol = pMap.getHtol();
        double hml = getKm() * (flow * flow);
        Link.StatType tStatus = status;
        switch (status) {
            case Link.StatType.ACTIVE:
                if (flow < -pMap.getQtol())
                    tStatus = Link.StatType.CLOSED;
                else if (h2 + hml > hset + htol)
                    tStatus = Link.StatType.OPEN;
                else
                    tStatus = Link.StatType.ACTIVE;
                break;
            case Link.StatType.OPEN:
                if (flow < -pMap.getQtol())
                    tStatus = Link.StatType.CLOSED;
                else if (h1 < hset - htol)
                    tStatus = Link.StatType.ACTIVE;
                else
                    tStatus = Link.StatType.OPEN;
                break;
            case Link.StatType.CLOSED:
                if (h2 > hset + htol && h1 > h2 + htol)
                    tStatus = Link.StatType.OPEN;
                else if (h1 >= hset + htol && h1 > h2 + htol)
                    tStatus = Link.StatType.ACTIVE;
                else
                    tStatus = Link.StatType.CLOSED;
                break;
            case Link.StatType.XPRESSURE:
                if (flow < -pMap.getQtol())
                    tStatus = Link.StatType.CLOSED;
                break;
        }
        return (tStatus);
    }

    // Compute P & Y coefficients for PBV,TCV,GPV valves
    public bool computeValveCoeff(FieldsMap fMap, PropertiesMap pMap, Curve[] curves) {
        switch (getType()) {
            case Link.LinkType.PBV:
                pbvCoeff(pMap);
                break;
            case Link.LinkType.TCV:
                tcvCoeff(pMap);
                break;
            case Link.LinkType.GPV:
                gpvCoeff(fMap, pMap, curves);
                break;
            case Link.LinkType.FCV:
            case Link.LinkType.PRV:
            case Link.LinkType.PSV:
                if (getSimSetting() == Constants.MISSING)
                    valveCoeff(pMap);
                else
                    return false;
                break;
        }
        return true;
    }

    // Updates status for PRVs & PSVs whose status is not fixed to OPEN/CLOSED
    public static bool valveStatus(FieldsMap fMap, PropertiesMap pMap, TraceSource log, List<SimulationValve> valves) {
        bool change = false;

        foreach (SimulationValve v  in  valves) {

            if (v.setting == Constants.MISSING) continue;

            Link.StatType s = v.status;

            switch (v.getType()) {
                case Link.LinkType.PRV: {
                    double hset = v.second.getElevation() + v.setting;
                    v.status = v.prvStatus(pMap, hset);
                    break;
                }
                case Link.LinkType.PSV: {
                    double hset = v.first.getElevation() + v.setting;
                    v.status = v.psvStatus(pMap, hset);
                    break;
                }

                default:
                    continue;
            }

            if (s != v.status) {
                if (pMap.getStatflag() == PropertiesMap.StatFlag.FULL)
                    logStatChange(fMap, log, v, s, v.status);
                change = true;
            }
        }
        return (change);
    }


    // Computes solution matrix coeffs. for PRVs, PSVs & FCVs whose status is not fixed to OPEN/CLOSED
    public static void computeMatrixCoeffs(PropertiesMap pMap, LSVariables ls, SparseMatrix smat, List<SimulationValve> valves) {
        foreach (SimulationValve valve  in  valves) {
            if (valve.getSimSetting() == Constants.MISSING)
                continue;

            switch (valve.getType()) {
                case Link.LinkType.PRV:
                    valve.prvCoeff(pMap, ls, smat);
                    break;
                case Link.LinkType.PSV:
                    valve.psvCoeff(pMap, ls, smat);
                    break;
                case Link.LinkType.FCV:
                    valve.fcvCoeff(pMap, ls, smat);
                    break;
            }
        }
    }

}
}