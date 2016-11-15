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
using Epanet.Log;
using Epanet.Network;
using Epanet.Network.Structures;
using Epanet.Util;

namespace Epanet.Hydraulic.Structures {


public class SimulationValve : SimulationLink {

    public SimulationValve(List<SimulationNode> indexedNodes, Link @ref, int idx)
        : base(indexedNodes, @ref, idx)
    {
        
    }


    // Computes solution matrix coeffs. for a completely open, closed, or throttled control valve.
    private void valveCoeff(PropertiesMap pMap) {
        // Valve is closed. Use a very small matrix coeff.
        if (this.status <= Link.StatType.CLOSED) {
            this.invHeadLoss = 1.0 / Constants.CBIG;
            this.flowCorrection = this.flow;
            return;
        }

        // Account for any minor headloss through the valve
        if (this.Km > 0.0) {
            double p = 2.0 * this.Km * Math.Abs(this.flow);
            if (p < pMap.RQtol)
                p = pMap.RQtol;

            this.invHeadLoss = 1.0 / p;
            this.flowCorrection = this.flow / 2.0;
        } else {
            this.invHeadLoss = 1.0 / pMap.RQtol;
            this.flowCorrection = this.flow;
        }
    }

    // Computes solution matrix coeffs. for a completely open, closed, or throttled control valve.
    private void valveCoeff(PropertiesMap pMap, double km) {
        double p;

        // Valve is closed. Use a very small matrix coeff.
        if (this.status <= Link.StatType.CLOSED) {
            this.invHeadLoss = 1.0 / Constants.CBIG;
            this.flowCorrection = this.flow;
            return;
        }

        // Account for any minor headloss through the valve
        if (km > 0.0) {
            p = 2.0 * km * Math.Abs(this.flow);
            if (p < pMap.RQtol)
                p = pMap.RQtol;

            this.invHeadLoss = 1.0 / p;
            this.flowCorrection = this.flow / 2.0;
        } else {
            this.invHeadLoss = 1.0 / pMap.RQtol;
            this.flowCorrection = this.flow;
        }
    }

    // Computes P & Y coeffs. for pressure breaker valve
    void pbvCoeff(PropertiesMap pMap) {
        if (this.setting == Constants.MISSING || this.setting == 0.0)
            this.valveCoeff(pMap);
        else if (this.Km * (this.flow * this.flow) > this.setting)
            this.valveCoeff(pMap);
        else {
            this.invHeadLoss = Constants.CBIG;
            this.flowCorrection = this.setting * Constants.CBIG;
        }
    }

    // Computes P & Y coeffs. for throttle control valve
    void tcvCoeff(PropertiesMap pMap) {
        double km = this.Km;

        if (this.setting != Constants.MISSING)
            km = (0.02517 * this.setting / Math.Pow(this.Diameter, 4));

        this.valveCoeff(pMap, km);
    }

    // Computes P & Y coeffs. for general purpose valve
    void gpvCoeff(FieldsMap fMap, PropertiesMap pMap, IList<Curve> curves) {
        if (this.status == Link.StatType.CLOSED)
            this.valveCoeff(pMap);
        else {
            double q = Math.Max(Math.Abs(this.flow), Constants.TINY);
            Curve.Coeffs coeffs = curves[(int) Math.Round(this.setting)].getCoeff(fMap, q);
            this.invHeadLoss = 1.0 / Math.Max(coeffs.r, pMap.RQtol);
            this.flowCorrection = this.invHeadLoss * (coeffs.h0 + coeffs.r * q) * Utilities.GetSignal(this.flow);
        }
    }

    // Updates status of a flow control valve.
    public Link.StatType fcvStatus(PropertiesMap pMap, Link.StatType s) {
        Link.StatType status;
        status = s;
        if (this.First.SimHead - this.Second.SimHead < -pMap.Htol) status = Link.StatType.XFCV;
        else if (this.flow < -pMap.Qtol) status = Link.StatType.XFCV;
        else if (s == Link.StatType.XFCV && this.flow >= this.setting) status = Link.StatType.ACTIVE;
        return (status);
    }


    // Computes solution matrix coeffs. for pressure reducing valves
    void prvCoeff(PropertiesMap pMap, LSVariables ls, SparseMatrix smat) {
        int k = this.Index;
        int i = smat.getRow(this.first.Index);
        int j = smat.getRow(this.second.Index);

        double hset = this.second.Elevation + this.setting;

        if (this.status == Link.StatType.ACTIVE) {

            this.invHeadLoss = 0.0;
            this.flowCorrection = this.flow + ls.getNodalInFlow(this.second);
            ls.addRHSCoeff(j, +(hset * Constants.CBIG));
            ls.addAii(j, +Constants.CBIG);
            if (ls.getNodalInFlow(this.second) < 0.0)
                ls.addRHSCoeff(i, +ls.getNodalInFlow(this.second));
            return;
        }

        this.valveCoeff(pMap);

        ls.addAij(smat.getNdx(k), -this.invHeadLoss);
        ls.addAii(i, +this.invHeadLoss);
        ls.addAii(j, +this.invHeadLoss);
        ls.addRHSCoeff(i, +(this.flowCorrection - this.flow));
        ls.addRHSCoeff(j, -(this.flowCorrection - this.flow));
    }


    // Computes solution matrix coeffs. for pressure sustaining valve
    void psvCoeff(PropertiesMap pMap, LSVariables ls, SparseMatrix smat) {
        int k = this.Index;
        int i = smat.getRow(this.first.Index);
        int j = smat.getRow(this.second.Index);
        double hset = this.first.Elevation + this.setting;

        if (this.status == Link.StatType.ACTIVE) {
            this.invHeadLoss = 0.0;
            this.flowCorrection = this.flow - ls.getNodalInFlow(this.first);
            ls.addRHSCoeff(i, +(hset * Constants.CBIG));
            ls.addAii(i, +Constants.CBIG);
            if (ls.getNodalInFlow(this.first) > 0.0) ls.addRHSCoeff(j, +ls.getNodalInFlow(this.first));
            return;
        }

        this.valveCoeff(pMap);
        ls.addAij(smat.getNdx(k), -this.invHeadLoss);
        ls.addAii(i, +this.invHeadLoss);
        ls.addAii(j, +this.invHeadLoss);
        ls.addRHSCoeff(i, +(this.flowCorrection - this.flow));
        ls.addRHSCoeff(j, -(this.flowCorrection - this.flow));
    }

    // computes solution matrix coeffs. for flow control valve
    void fcvCoeff(PropertiesMap pMap, LSVariables ls, SparseMatrix smat) {
        int k = this.Index;
        double q = this.setting;
        int i = smat.getRow(this.first.Index);
        int j = smat.getRow(this.second.Index);

        // If valve active, break network at valve and treat
        // flow setting as external demand at upstream node
        // and external supply at downstream node.
        if (this.status == Link.StatType.ACTIVE) {
            ls.addNodalInFlow(this.first.Index, -q);
            ls.addRHSCoeff(i, -q);
            ls.addNodalInFlow(this.second.Index, +q);
            ls.addRHSCoeff(j, +q);
            this.invHeadLoss = 1.0 / Constants.CBIG;
            ls.addAij(smat.getNdx(k), -this.invHeadLoss);
            ls.addAii(i, +this.invHeadLoss);
            ls.addAii(j, +this.invHeadLoss);
            this.flowCorrection = this.flow - q;
        } else {
            //  Otherwise treat valve as an open pipe
            this.valveCoeff(pMap);
            ls.addAij(smat.getNdx(k), -this.invHeadLoss);
            ls.addAii(i, +this.invHeadLoss);
            ls.addAii(j, +this.invHeadLoss);
            ls.addRHSCoeff(i, +(this.flowCorrection - this.flow));
            ls.addRHSCoeff(j, -(this.flowCorrection - this.flow));
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
        if (this.setting == Constants.MISSING)
            return (this.status);

        double htol = pMap.Htol;
        double hml = this.Km * (this.flow * this.flow);
        double h1 = this.first.SimHead;
        double h2 = this.second.SimHead;

        Link.StatType tStatus = this.status;
        switch (this.status) {
            case Link.StatType.ACTIVE:
                if (this.flow < -pMap.Qtol)
                    tStatus = Link.StatType.CLOSED;
                else if (h1 - hml < hset - htol)
                    tStatus = Link.StatType.OPEN;
                else
                    tStatus = Link.StatType.ACTIVE;
                break;
            case Link.StatType.OPEN:
                if (this.flow < -pMap.Qtol)
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
                if (this.flow < -pMap.Qtol)
                    tStatus = Link.StatType.CLOSED;
                break;
        }
        return (tStatus);
    }

    // Updates status of a pressure sustaining valve.
    private Link.StatType psvStatus(PropertiesMap pMap, double hset) {
        if (this.setting == Constants.MISSING)
            return (this.status);

        double h1 = this.first.SimHead;
        double h2 = this.second.SimHead;
        double htol = pMap.Htol;
        double hml = this.Km * (this.flow * this.flow);
        Link.StatType tStatus = this.status;
        switch (this.status) {
            case Link.StatType.ACTIVE:
                if (this.flow < -pMap.Qtol)
                    tStatus = Link.StatType.CLOSED;
                else if (h2 + hml > hset + htol)
                    tStatus = Link.StatType.OPEN;
                else
                    tStatus = Link.StatType.ACTIVE;
                break;
            case Link.StatType.OPEN:
                if (this.flow < -pMap.Qtol)
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
                if (this.flow < -pMap.Qtol)
                    tStatus = Link.StatType.CLOSED;
                break;
        }
        return (tStatus);
    }

    // Compute P & Y coefficients for PBV,TCV,GPV valves
    public bool computeValveCoeff(FieldsMap fMap, PropertiesMap pMap, IList<Curve> curves) {
        switch (this.Type) {
            case Link.LinkType.PBV:
                this.pbvCoeff(pMap);
                break;
            case Link.LinkType.TCV:
                this.tcvCoeff(pMap);
                break;
            case Link.LinkType.GPV:
                this.gpvCoeff(fMap, pMap, curves);
                break;
            case Link.LinkType.FCV:
            case Link.LinkType.PRV:
            case Link.LinkType.PSV:
                if (this.SimSetting == Constants.MISSING)
                    this.valveCoeff(pMap);
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
                    double hset = v.second.Elevation + v.setting;
                    v.status = v.prvStatus(pMap, hset);
                    break;
                }
                case Link.LinkType.PSV: {
                    double hset = v.first.Elevation + v.setting;
                    v.status = v.psvStatus(pMap, hset);
                    break;
                }

                default:
                    continue;
            }

            if (s != v.status) {
                if (pMap.Statflag == PropertiesMap.StatFlag.FULL)
                    LogStatChange(fMap, log, v, s, v.status);
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