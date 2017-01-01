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

using Epanet.Enums;
using Epanet.Log;
using Epanet.Network;
using Epanet.Network.Structures;
using Epanet.Util;

namespace Epanet.Hydraulic.Structures {

    public class SimulationValve:SimulationLink {

        public SimulationValve(List<SimulationNode> indexedNodes, Link @ref, int idx)
            :base(indexedNodes, @ref, idx) {}

        /// <summary>
        /// Computes solution matrix coeffs. for a completely open, 
        /// closed, or throttled control valve.
        /// </summary>
        private void ValveCoeff(PropertiesMap pMap) {
            // Valve is closed. Use a very small matrix coeff.
            if (this.status <= StatType.CLOSED) {
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
            }
            else {
                this.invHeadLoss = 1.0 / pMap.RQtol;
                this.flowCorrection = this.flow;
            }
        }

        /// <summary>
        /// Computes solution matrix coeffs. for a completely open, 
        /// closed, or throttled control valve.
        /// </summary>
        private void ValveCoeff(PropertiesMap pMap, double km) {
            // Valve is closed. Use a very small matrix coeff.
            if (this.status <= StatType.CLOSED) {
                this.invHeadLoss = 1.0 / Constants.CBIG;
                this.flowCorrection = this.flow;
                return;
            }

            // Account for any minor headloss through the valve
            if (km > 0.0) {
                double p = 2.0 * km * Math.Abs(this.flow);
                if (p < pMap.RQtol)
                    p = pMap.RQtol;

                this.invHeadLoss = 1.0 / p;
                this.flowCorrection = this.flow / 2.0;
            }
            else {
                this.invHeadLoss = 1.0 / pMap.RQtol;
                this.flowCorrection = this.flow;
            }
        }

        /// <summary>Computes P & Y coeffs. for pressure breaker valve.</summary>
        private void PbvCoeff(PropertiesMap pMap) {
            if (this.setting.IsMissing() || this.setting == 0.0)
                this.ValveCoeff(pMap);
            else if (this.Km * (this.flow * this.flow) > this.setting)
                this.ValveCoeff(pMap);
            else {
                this.invHeadLoss = Constants.CBIG;
                this.flowCorrection = this.setting * Constants.CBIG;
            }
        }

        /// <summary>Computes P & Y coeffs. for throttle control valve.</summary>
        private void TcvCoeff(PropertiesMap pMap) {
            double km = this.Km;

            if (!this.setting.IsMissing())
                km = 0.02517 * this.setting / Math.Pow(this.Diameter, 4);

            this.ValveCoeff(pMap, km);
        }

        /// <summary>Computes P & Y coeffs. for general purpose valve.</summary>
        private void GpvCoeff(FieldsMap fMap, PropertiesMap pMap, IList<Curve> curves) {
            if (this.status == StatType.CLOSED)
                this.ValveCoeff(pMap);
            else {
                double q = Math.Max(Math.Abs(this.flow), Constants.TINY);
                double h0, r;
                curves[(int)Math.Round(this.setting)].GetCoeff(fMap, q, out h0, out r);
                this.invHeadLoss = 1.0 / Math.Max(r, pMap.RQtol);
                this.flowCorrection = this.invHeadLoss * (h0 + r * q) * Utilities.GetSignal(this.flow);
            }
        }

        /// <summary>Updates status of a flow control valve.</summary>
        public StatType FcvStatus(PropertiesMap pMap, StatType s) {
            StatType stat = s;
            if (this.First.SimHead - this.Second.SimHead < -pMap.HTol) stat = StatType.XFCV;
            else if (this.flow < -pMap.QTol) stat = StatType.XFCV;
            else if (s == StatType.XFCV && this.flow >= this.setting) stat = StatType.ACTIVE;
            return stat;
        }


        /// <summary>Computes solution matrix coeffs. for pressure reducing valves.</summary>
        private void PrvCoeff(PropertiesMap pMap, LSVariables ls, SparseMatrix smat) {
            int k = this.Index;
            int n1 = smat.GetRow(this.first.Index);
            int n2 = smat.GetRow(this.second.Index);

            double hset = this.second.Elevation + this.setting;

            if (this.status == StatType.ACTIVE) {

                this.invHeadLoss = 0.0;
                this.flowCorrection = this.flow + ls.GetNodalInFlow(this.second);
                ls.AddRhsCoeff(n2, +(hset * Constants.CBIG));
                ls.AddAii(n2, +Constants.CBIG);
                if (ls.GetNodalInFlow(this.second) < 0.0)
                    ls.AddRhsCoeff(n1, +ls.GetNodalInFlow(this.second));

                return;
            }

            this.ValveCoeff(pMap);

            ls.AddAij(smat.GetNdx(k), -this.invHeadLoss);
            ls.AddAii(n1, +this.invHeadLoss);
            ls.AddAii(n2, +this.invHeadLoss);
            ls.AddRhsCoeff(n1, +(this.flowCorrection - this.flow));
            ls.AddRhsCoeff(n2, -(this.flowCorrection - this.flow));
        }


        /// <summary>Computes solution matrix coeffs. for pressure sustaining valve.</summary>
        private void PsvCoeff(PropertiesMap pMap, LSVariables ls, SparseMatrix smat) {
            int k = this.Index;
            int n1 = smat.GetRow(this.first.Index);
            int n2 = smat.GetRow(this.second.Index);
            double hset = this.first.Elevation + this.setting;

            if (this.status == StatType.ACTIVE) {
                this.invHeadLoss = 0.0;
                this.flowCorrection = this.flow - ls.GetNodalInFlow(this.first);
                ls.AddRhsCoeff(n1, +(hset * Constants.CBIG));
                ls.AddAii(n1, +Constants.CBIG);
                if (ls.GetNodalInFlow(this.first) > 0.0) ls.AddRhsCoeff(n2, +ls.GetNodalInFlow(this.first));
                return;
            }

            this.ValveCoeff(pMap);
            ls.AddAij(smat.GetNdx(k), -this.invHeadLoss);
            ls.AddAii(n1, +this.invHeadLoss);
            ls.AddAii(n2, +this.invHeadLoss);
            ls.AddRhsCoeff(n1, +(this.flowCorrection - this.flow));
            ls.AddRhsCoeff(n2, -(this.flowCorrection - this.flow));
        }

        /// <summary>Computes solution matrix coeffs. for flow control valve.</summary>
        private void FcvCoeff(PropertiesMap pMap, LSVariables ls, SparseMatrix smat) {
            int k = this.Index;
            double q = this.setting;
            int n1 = smat.GetRow(this.first.Index);
            int n2 = smat.GetRow(this.second.Index);

            // If valve active, break network at valve and treat
            // flow setting as external demand at upstream node
            // and external supply at downstream node.
            if (this.status == StatType.ACTIVE) {
                ls.AddNodalInFlow(this.first.Index, -q);
                ls.AddRhsCoeff(n1, -q);
                ls.AddNodalInFlow(this.second.Index, +q);
                ls.AddRhsCoeff(n2, +q);
                this.invHeadLoss = 1.0 / Constants.CBIG;
                ls.AddAij(smat.GetNdx(k), -this.invHeadLoss);
                ls.AddAii(n1, +this.invHeadLoss);
                ls.AddAii(n2, +this.invHeadLoss);
                this.flowCorrection = this.flow - q;
            }
            else {
                //  Otherwise treat valve as an open pipe
                this.ValveCoeff(pMap);
                ls.AddAij(smat.GetNdx(k), -this.invHeadLoss);
                ls.AddAii(n1, +this.invHeadLoss);
                ls.AddAii(n2, +this.invHeadLoss);
                ls.AddRhsCoeff(n1, +(this.flowCorrection - this.flow));
                ls.AddRhsCoeff(n2, -(this.flowCorrection - this.flow));
            }
        }

        /// <summary>
        /// Determines if a node belongs to an active control valve
        /// whose setting causes an inconsistent set of eqns. If so,
        /// the valve status is fixed open and a warning condition
        /// is generated.
        /// </summary>
        public static bool CheckBadValve(
            PropertiesMap pMap,
            TraceSource log,
            List<SimulationValve> valves,
            long htime,
            int n) {
            foreach (SimulationValve link  in  valves) {
                SimulationNode n1 = link.First;
                SimulationNode n2 = link.Second;
                if (n == n1.Index || n == n2.Index) {
                    if (link.Type == LinkType.PRV || link.Type == LinkType.PSV
                        || link.Type == LinkType.FCV) {
                        if (link.status == StatType.ACTIVE) {
                            if (pMap.Stat_Flag == StatFlag.FULL) {
                                LogBadValve(log, link, htime);
                            }

                            link.status = link.Type == LinkType.FCV
                                ? StatType.XFCV
                                : StatType.XPRESSURE;

                            return true;
                        }
                    }
                    return false;
                }
            }

            return false;
        }

        private static void LogBadValve(TraceSource log, SimulationLink link, long htime) {
            log.Warning(Properties.Text.FMT61, htime.GetClockTime(), link.Link.Id);
        }

        /// <summary>Updates status of a pressure reducing valve.</summary>
        private StatType PrvStatus(PropertiesMap pMap, double hset) {
            if (this.setting.IsMissing())
                return this.status;

            double htol = pMap.HTol;
            double hml = this.Km * (this.flow * this.flow);
            double h1 = this.first.SimHead;
            double h2 = this.second.SimHead;

            StatType stat = this.status;

            switch (this.status) {
            case StatType.ACTIVE:
                if (this.flow < -pMap.QTol)
                    stat = StatType.CLOSED;
                else if (h1 - hml < hset - htol)
                    stat = StatType.OPEN;
                else
                    stat = StatType.ACTIVE;
                break;

            case StatType.OPEN:
                if (this.flow < -pMap.QTol)
                    stat = StatType.CLOSED;
                else if (h2 >= hset + htol)
                    stat = StatType.ACTIVE;
                else
                    stat = StatType.OPEN;
                break;

            case StatType.CLOSED:
                if (h1 >= hset + htol && h2 < hset - htol)
                    stat = StatType.ACTIVE;
                else if (h1 < hset - htol && h1 > h2 + htol)
                    stat = StatType.OPEN;
                else
                    stat = StatType.CLOSED;
                break;

            case StatType.XPRESSURE:
                if (this.flow < -pMap.QTol)
                    stat = StatType.CLOSED;
                break;
            }

            return stat;
        }

        /// <summary>Updates status of a pressure sustaining valve.</summary>
        private StatType PsvStatus(PropertiesMap pMap, double hset) {
            if (this.setting.IsMissing())
                return this.status;

            double h1 = this.first.SimHead;
            double h2 = this.second.SimHead;
            double htol = pMap.HTol;
            double hml = this.Km * (this.flow * this.flow);
            StatType stat = this.status;

            switch (this.status) {
            case StatType.ACTIVE:
                if (this.flow < -pMap.QTol)
                    stat = StatType.CLOSED;
                else if (h2 + hml > hset + htol)
                    stat = StatType.OPEN;
                else
                    stat = StatType.ACTIVE;
                break;

            case StatType.OPEN:
                if (this.flow < -pMap.QTol)
                    stat = StatType.CLOSED;
                else if (h1 < hset - htol)
                    stat = StatType.ACTIVE;
                else
                    stat = StatType.OPEN;
                break;

            case StatType.CLOSED:
                if (h2 > hset + htol && h1 > h2 + htol)
                    stat = StatType.OPEN;
                else if (h1 >= hset + htol && h1 > h2 + htol)
                    stat = StatType.ACTIVE;
                else
                    stat = StatType.CLOSED;
                break;

            case StatType.XPRESSURE:
                if (this.flow < -pMap.QTol)
                    stat = StatType.CLOSED;
                break;
            }

            return stat;
        }

        /// <summary>Compute P & Y coefficients for PBV,TCV,GPV valves.</summary>
        public bool ComputeValveCoeff(FieldsMap fMap, PropertiesMap pMap, IList<Curve> curves) {
            switch (this.Type) {
            case LinkType.PBV:
                this.PbvCoeff(pMap);
                break;

            case LinkType.TCV:
                this.TcvCoeff(pMap);
                break;

            case LinkType.GPV:
                this.GpvCoeff(fMap, pMap, curves);
                break;

            case LinkType.FCV:
            case LinkType.PRV:
            case LinkType.PSV:

                if (this.SimSetting.IsMissing())
                    this.ValveCoeff(pMap);
                else
                    return false;

                break;
            }

            return true;
        }

        /// <summary>Updates status for PRVs & PSVs whose status is not fixed to OPEN/CLOSED.</summary>
        public static bool ValveStatus(
            FieldsMap fMap,
            PropertiesMap pMap,
            TraceSource log,
            List<SimulationValve> valves) {
            bool change = false;

            foreach (SimulationValve v  in  valves) {

                if (v.setting.IsMissing()) continue;

                StatType s = v.status;

                switch (v.Type) {
                case LinkType.PRV: {
                    double hset = v.second.Elevation + v.setting;
                    v.status = v.PrvStatus(pMap, hset);
                    break;
                }

                case LinkType.PSV: {
                    double hset = v.first.Elevation + v.setting;
                    v.status = v.PsvStatus(pMap, hset);
                    break;
                }

                default:
                    continue;
                }

                if (s != v.status) {
                    if (pMap.Stat_Flag == StatFlag.FULL)
                        LogStatChange(fMap, log, v, s, v.status);
                    change = true;
                }
            }

            return change;
        }


        /// <summary>
        /// Computes solution matrix coeffs. for PRVs, PSVs & FCVs 
        /// whose status is not fixed to OPEN/CLOSED.
        /// </summary>
        public static void ComputeMatrixCoeffs(
            PropertiesMap pMap,
            LSVariables ls,
            SparseMatrix smat,
            List<SimulationValve> valves) {
            foreach (SimulationValve valve  in  valves) {
                if (valve.SimSetting.IsMissing())
                    continue;

                switch (valve.Type) {
                case LinkType.PRV:
                    valve.PrvCoeff(pMap, ls, smat);
                    break;
                case LinkType.PSV:
                    valve.PsvCoeff(pMap, ls, smat);
                    break;
                case LinkType.FCV:
                    valve.FcvCoeff(pMap, ls, smat);
                    break;
                }
            }
        }

    }

}