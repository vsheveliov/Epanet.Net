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
        if (Km > 0.0) {
            p = 2.0 * Km * Math.Abs(flow);
            if (p < pMap.RQtol)
                p = pMap.RQtol;

            invHeadLoss = 1.0 / p;
            flowCorrection = flow / 2.0;
        } else {
            invHeadLoss = 1.0 / pMap.RQtol;
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
            if (p < pMap.RQtol)
                p = pMap.RQtol;

            invHeadLoss = 1.0 / p;
            flowCorrection = flow / 2.0;
        } else {
            invHeadLoss = 1.0 / pMap.RQtol;
            flowCorrection = flow;
        }
    }

    // Computes P & Y coeffs. for pressure breaker valve
    void pbvCoeff(PropertiesMap pMap) {
        if (setting == Constants.MISSING || setting == 0.0)
            valveCoeff(pMap);
        else if (Km * (flow * flow) > setting)
            valveCoeff(pMap);
        else {
            invHeadLoss = Constants.CBIG;
            flowCorrection = setting * Constants.CBIG;
        }
    }

    // Computes P & Y coeffs. for throttle control valve
    void tcvCoeff(PropertiesMap pMap) {
        double km = Km;

        if (setting != Constants.MISSING)
            km = (0.02517 * setting / Math.Pow(Diameter, 4));

        valveCoeff(pMap, km);
    }

    // Computes P & Y coeffs. for general purpose valve
    void gpvCoeff(FieldsMap fMap, PropertiesMap pMap, IList<Curve> curves) {
        if (status == Link.StatType.CLOSED)
            valveCoeff(pMap);
        else {
            double q = Math.Max(Math.Abs(flow), Constants.TINY);
            Curve.Coeffs coeffs = curves[(int) Math.Round(setting)].getCoeff(fMap, q);
            invHeadLoss = 1.0 / Math.Max(coeffs.r, pMap.RQtol);
            flowCorrection = invHeadLoss * (coeffs.h0 + coeffs.r * q) * Utilities.GetSignal(flow);
        }
    }

    // Updates status of a flow control valve.
    public Link.StatType fcvStatus(PropertiesMap pMap, Link.StatType s) {
        Link.StatType status;
        status = s;
        if (First.getSimHead() - Second.getSimHead() < -pMap.Htol) status = Link.StatType.XFCV;
        else if (flow < -pMap.Qtol) status = Link.StatType.XFCV;
        else if (s == Link.StatType.XFCV && flow >= setting) status = Link.StatType.ACTIVE;
        return (status);
    }


    // Computes solution matrix coeffs. for pressure reducing valves
    void prvCoeff(PropertiesMap pMap, LSVariables ls, SparseMatrix smat) {
        int k = Index;
        int i = smat.getRow(this.first.Index);
        int j = smat.getRow(this.second.Index);

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
        int k = Index;
        int i = smat.getRow(this.first.Index);
        int j = smat.getRow(this.second.Index);
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
        int k = Index;
        double q = setting;
        int i = smat.getRow(this.first.Index);
        int j = smat.getRow(this.second.Index);

        // If valve active, break network at valve and treat
        // flow setting as external demand at upstream node
        // and external supply at downstream node.
        if (status == Link.StatType.ACTIVE) {
            ls.addNodalInFlow(this.first.Index, -q);
            ls.addRHSCoeff(i, -q);
            ls.addNodalInFlow(this.second.Index, +q);
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
            SimulationNode n1 = link.First;
            SimulationNode n2 = link.Second;
            if (n == n1.Index || n == n2.Index) {
                if (link.Type == Link.LinkType.PRV || link.Type == Link.LinkType.PSV || link.Type == Link.LinkType.FCV) {
                    if (link.status == Link.StatType.ACTIVE) {
                        if (pMap.Statflag == PropertiesMap.StatFlag.FULL) {
                            logBadValve(log, link, Htime);
                        }
                        if (link.Type == Link.LinkType.FCV)
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
        log.Warning(Epanet.Properties.Text.ResourceManager.GetString("FMT61"), Htime.GetClockTime(), link.Link.Id);
    }

    // Updates status of a pressure reducing valve.
    private Link.StatType prvStatus(PropertiesMap pMap, double hset) {
        if (setting == Constants.MISSING)
            return (status);

        double htol = pMap.Htol;
        double hml = Km * (flow * flow);
        double h1 = first.getSimHead();
        double h2 = second.getSimHead();

        Link.StatType tStatus = status;
        switch (status) {
            case Link.StatType.ACTIVE:
                if (flow < -pMap.Qtol)
                    tStatus = Link.StatType.CLOSED;
                else if (h1 - hml < hset - htol)
                    tStatus = Link.StatType.OPEN;
                else
                    tStatus = Link.StatType.ACTIVE;
                break;
            case Link.StatType.OPEN:
                if (flow < -pMap.Qtol)
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
                if (flow < -pMap.Qtol)
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
        double htol = pMap.Htol;
        double hml = Km * (flow * flow);
        Link.StatType tStatus = status;
        switch (status) {
            case Link.StatType.ACTIVE:
                if (flow < -pMap.Qtol)
                    tStatus = Link.StatType.CLOSED;
                else if (h2 + hml > hset + htol)
                    tStatus = Link.StatType.OPEN;
                else
                    tStatus = Link.StatType.ACTIVE;
                break;
            case Link.StatType.OPEN:
                if (flow < -pMap.Qtol)
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
                if (flow < -pMap.Qtol)
                    tStatus = Link.StatType.CLOSED;
                break;
        }
        return (tStatus);
    }

    // Compute P & Y coefficients for PBV,TCV,GPV valves
    public bool computeValveCoeff(FieldsMap fMap, PropertiesMap pMap, IList<Curve> curves) {
        switch (Type) {
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
                if (this.SimSetting == Constants.MISSING)
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

            switch (v.Type) {
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
                if (pMap.Statflag == PropertiesMap.StatFlag.FULL)
                    logStatChange(fMap, log, v, s, v.status);
                change = true;
            }
        }
        return (change);
    }


    // Computes solution matrix coeffs. for PRVs, PSVs & FCVs whose status is not fixed to OPEN/CLOSED
    public static void computeMatrixCoeffs(PropertiesMap pMap, LSVariables ls, SparseMatrix smat, List<SimulationValve> valves) {
        foreach (SimulationValve valve  in  valves) {
            if (valve.SimSetting == Constants.MISSING)
                continue;

            switch (valve.Type) {
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