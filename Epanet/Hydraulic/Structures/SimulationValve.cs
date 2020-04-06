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
using Epanet.Network.Structures;
using Epanet.Util;
using EpanetNetwork = Epanet.Network.Network;

namespace Epanet.Hydraulic.Structures {

    public class SimulationValve:SimulationLink {

        public SimulationValve(Dictionary<string, SimulationNode> indexedNodes, Link @ref, int idx)
            :base(indexedNodes, @ref, idx) {}

        public ValveType ValveType => ((Valve)link).ValveType;

        /// <summary>
        /// Computes solution matrix coeffs. for a completely open, 
        /// closed, or throttled control valve.
        /// </summary>
        private void ValveCoeff(EpanetNetwork net) {
            ValveCoeff(net, Km);
        }

        /// <summary>
        /// Computes solution matrix coeffs. for a completely open, 
        /// closed, or throttled control valve.
        /// </summary>
        private void ValveCoeff(EpanetNetwork net, double km) {
            // Valve is closed. Use a very small matrix coeff.
            if (SimStatus <= StatType.CLOSED) {
                SimInvHeadLoss = 1.0 / Constants.CBIG;
                SimFlowCorrection = SimFlow;
                return;
            }

            // Account for any minor headloss through the valve
            if (km > 0.0) {
                double p = 2.0 * km * Math.Abs(SimFlow);
                if (p < net.RQtol)
                    p = net.RQtol;

                SimInvHeadLoss = 1.0 / p;
                SimFlowCorrection = SimFlow / 2.0;
            }
            else {
                SimInvHeadLoss = 1.0 / net.RQtol;
                SimFlowCorrection = SimFlow;
            }
        }

        /// <summary>Computes P & Y coeffs. for pressure breaker valve.</summary>
        private void PbvCoeff(EpanetNetwork net) {
            if (double.IsNaN(SimSetting) || SimSetting.IsZero())
                ValveCoeff(net);
            else if (Km * (SimFlow * SimFlow) > SimSetting)
                ValveCoeff(net);
            else {
                SimInvHeadLoss = Constants.CBIG;
                SimFlowCorrection = SimSetting * Constants.CBIG;
            }
        }

        /// <summary>Computes P & Y coeffs. for throttle control valve.</summary>
        private void TcvCoeff(EpanetNetwork net) {

            /* Save original loss coeff. for open valve */
            double km = Km;

            /* If valve not fixed OPEN or CLOSED, compute its loss coeff. */
            if (!double.IsNaN(SimSetting))
                km = 0.02517 * SimSetting / Math.Pow(link.Diameter, 4);

            /* Then apply usual pipe formulas */
            ValveCoeff(net, km);
        }

        /// <summary>Computes P & Y coeffs. for general purpose valve.</summary>
        private void GpvCoeff(EpanetNetwork net, IList<Curve> curves) {
            if (SimStatus == StatType.CLOSED)
                ValveCoeff(net);
            else {
                double q = Math.Max(Math.Abs(SimFlow), Constants.TINY);
                curves[(int)Math.Round(SimSetting)].GetCoeff(net.FieldsMap, q, out double h0, out double r);
                SimInvHeadLoss = 1.0 / Math.Max(r, net.RQtol);
                SimFlowCorrection = SimInvHeadLoss * (h0 + r * q) * SimFlow.Sign();
            }
        }

        /// <summary>Updates status of a flow control valve.</summary>
        public StatType FcvStatus(EpanetNetwork net, StatType s) {
            StatType stat = s;
            if (First.SimHead - Second.SimHead < -net.HTol) stat = StatType.XFCV;
            else if (SimFlow < -net.QTol) stat = StatType.XFCV;
            else if (s == StatType.XFCV && SimFlow >= SimSetting) stat = StatType.ACTIVE;
            return stat;
        }


        /// <summary>Computes solution matrix coeffs. for pressure reducing valves.</summary>
        private void PrvCoeff(EpanetNetwork net, LsVariables ls, SparseMatrix smat) {
            int k = Index;
            int n1 = smat.GetRow(first.Index);
            int n2 = smat.GetRow(second.Index);

            double hset = second.Elevation + SimSetting;

            if (SimStatus == StatType.ACTIVE) {

                SimInvHeadLoss = 0.0;
                SimFlowCorrection = SimFlow + ls.x[second.Index];
                ls.f[n2] += hset * Constants.CBIG;
                ls.aii[n2] += Constants.CBIG;

                if (ls.x[second.Index] < 0.0)
                    ls.f[n1] += ls.x[second.Index];

                return;
            }

            ValveCoeff(net);

            ls.aij[smat.GetNdx(k)] -= SimInvHeadLoss;
            ls.aii[n1] += SimInvHeadLoss;
            ls.aii[n2] += SimInvHeadLoss;
            ls.f[n1] += SimFlowCorrection - SimFlow;
            ls.f[n2] -= SimFlowCorrection - SimFlow;
        }


        /// <summary>Computes solution matrix coeffs. for pressure sustaining valve.</summary>
        private void PsvCoeff(EpanetNetwork net, LsVariables ls, SparseMatrix smat) {
            int k = Index;
            int n1 = smat.GetRow(first.Index);
            int n2 = smat.GetRow(second.Index);
            double hset = first.Elevation + SimSetting;

            if (SimStatus == StatType.ACTIVE) {
                SimInvHeadLoss = 0.0;
                SimFlowCorrection = SimFlow - ls.x[first.Index];
                ls.f[n1] += hset * Constants.CBIG;
                ls.aii[n1] += Constants.CBIG;
                if (ls.x[first.Index] > 0.0) {
                    ls.f[n2] += ls.x[first.Index];
                }
                return;
            }

            ValveCoeff(net);
            ls.aij[smat.GetNdx(k)] -= SimInvHeadLoss;
            ls.aii[n1] += SimInvHeadLoss;
            ls.aii[n2] += SimInvHeadLoss;
            ls.f[n1] += SimFlowCorrection - SimFlow;
            ls.f[n2] -= SimFlowCorrection - SimFlow;
        }

        /// <summary>Computes solution matrix coeffs. for flow control valve.</summary>
        private void FcvCoeff(EpanetNetwork net, LsVariables ls, SparseMatrix smat) {
            int k = Index;
            double q = SimSetting;
            int n1 = smat.GetRow(first.Index);
            int n2 = smat.GetRow(second.Index);

            // If valve active, break network at valve and treat
            // flow setting as external demand at upstream node
            // and external supply at downstream node.
            if (SimStatus == StatType.ACTIVE) {
                ls.x[first.Index] -= q;
                ls.f[n1] -= q;
                ls.x[second.Index] += q;
                ls.f[n2] += q;
                SimInvHeadLoss = 1.0 / Constants.CBIG;
                ls.aij[smat.GetNdx(k)] -= SimInvHeadLoss;
                ls.aii[n1] += SimInvHeadLoss;
                ls.aii[n2] += SimInvHeadLoss;
                SimFlowCorrection = SimFlow - q;
                return;
            }
            
            //  Otherwise treat valve as an open pipe
            ValveCoeff(net);
            ls.aij[smat.GetNdx(k)] -= SimInvHeadLoss;
            ls.aii[n1] += SimInvHeadLoss;
            ls.aii[n2] += SimInvHeadLoss;
            ls.f[n1] += SimFlowCorrection - SimFlow;
            ls.f[n2] -= SimFlowCorrection - SimFlow;
        }

        /// <summary>
        /// Determines if a node belongs to an active control valve
        /// whose setting causes an inconsistent set of eqns. If so,
        /// the valve status is fixed open and a warning condition
        /// is generated.
        /// </summary>
        public static bool CheckBadValve(
            EpanetNetwork net,
            TraceSource log,
            List<SimulationValve> valves,
            TimeSpan htime,
            int n) {

            foreach (SimulationValve valve  in  valves) {

                if(n != valve.First.Index && n != valve.Second.Index) 
                    continue;

                switch (valve.ValveType) {
                    case ValveType.PRV:
                    case ValveType.PSV:
                    case ValveType.FCV:
                        if (valve.SimStatus == StatType.ACTIVE) {
                            if (net.StatFlag == StatFlag.FULL) {
                                LogBadValve(log, valve, htime);
                            }

                            valve.SimStatus = valve.ValveType == ValveType.FCV
                                ? StatType.XFCV
                                : StatType.XPRESSURE;

                            return true;
                        }

                        break;
                }

                return false;
            }

            return false;
        }

        private static void LogBadValve(TraceSource log, SimulationLink link, TimeSpan htime) {
            log.Warning(Properties.Text.FMT61, htime.GetClockTime(), link.Link.Name);
        }

        /// <summary>Updates status of a pressure reducing valve.</summary>
        private StatType PrvStatus(EpanetNetwork net, double hset) {
            if (double.IsNaN(SimSetting))
                return SimStatus;

            double htol = net.HTol;
            double hml = Km * (SimFlow * SimFlow);
            double h1 = first.SimHead;
            double h2 = second.SimHead;

            StatType stat = SimStatus;

            switch (SimStatus) {
            case StatType.ACTIVE:
                if (SimFlow < -net.QTol)
                    stat = StatType.CLOSED;
                else if (h1 - hml < hset - htol)
                    stat = StatType.OPEN;
                else
                    stat = StatType.ACTIVE;
                break;

            case StatType.OPEN:
                if (SimFlow < -net.QTol)
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
                if (SimFlow < -net.QTol)
                    stat = StatType.CLOSED;
                break;
            }

            return stat;
        }

        /// <summary>Updates status of a pressure sustaining valve.</summary>
        private StatType PsvStatus(EpanetNetwork net, double hset) {
            if (double.IsNaN(SimSetting))
                return SimStatus;

            double h1 = first.SimHead;
            double h2 = second.SimHead;
            double htol = net.HTol;
            double hml = Km * (SimFlow * SimFlow);
            StatType stat = SimStatus;

            switch (SimStatus) {
            case StatType.ACTIVE:
                if (SimFlow < -net.QTol)
                    stat = StatType.CLOSED;
                else if (h2 + hml > hset + htol)
                    stat = StatType.OPEN;
                else
                    stat = StatType.ACTIVE;
                break;

            case StatType.OPEN:
                if (SimFlow < -net.QTol)
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
                if (SimFlow < -net.QTol)
                    stat = StatType.CLOSED;
                break;
            }

            return stat;
        }

        /// <summary>Compute P & Y coefficients for PBV,TCV,GPV valves.</summary>
        public bool ComputeValveCoeff(EpanetNetwork net, IList<Curve> curves) {
            switch (ValveType) {
            case ValveType.PBV:
                PbvCoeff(net);
                break;

            case ValveType.TCV:
                TcvCoeff(net);
                break;

            case ValveType.GPV:
                GpvCoeff(net, curves);
                break;

            case ValveType.FCV:
            case ValveType.PRV:
            case ValveType.PSV:

                if (double.IsNaN(SimSetting))
                    ValveCoeff(net);
                else
                    return false;

                break;
            }

            return true;
        }

        /// <summary>Updates status for PRVs & PSVs whose status is not fixed to OPEN/CLOSED.</summary>
        public static bool ValveStatus(
            EpanetNetwork net,
            TraceSource log,
            IEnumerable<SimulationValve> valves) {
            bool change = false;

            foreach (SimulationValve v  in  valves) {

                if (double.IsNaN(v.SimSetting)) continue;

                StatType s = v.SimStatus;

                switch (v.ValveType) {
                case ValveType.PRV: {
                    double hset = v.second.Elevation + v.SimSetting;
                    v.SimStatus = v.PrvStatus(net, hset);
                    break;
                }

                case ValveType.PSV: {
                    double hset = v.first.Elevation + v.SimSetting;
                    v.SimStatus = v.PsvStatus(net, hset);
                    break;
                }

                default:
                    continue;
                }

                if (s != v.SimStatus) {
                    if (net.StatFlag == StatFlag.FULL)
                        LogStatChange(net.FieldsMap, log, v, s, v.SimStatus);
                    change = true;
                }
            }

            return change;
        }

        /// <summary>
        /// Computes solution matrix coeffs. for PRVs, PSVs & FCVs 
        /// whose status is not fixed to OPEN/CLOSED.
        /// </summary>
        internal void ComputeMatrixCoeff(EpanetNetwork net, LsVariables ls, SparseMatrix smat) {
            if (double.IsNaN(SimSetting))
                return;

            switch (ValveType) {
                case ValveType.PRV:
                    PrvCoeff(net, ls, smat);
                    break;
                case ValveType.PSV:
                    PsvCoeff(net, ls, smat);
                    break;
                case ValveType.FCV:
                    FcvCoeff(net, ls, smat);
                    break;
            }
        }

        /// <summary>Sets link status to OPEN(true) or CLOSED(false)</summary>
        public override void SetLinkStatus(bool value) {
            if (ValveType != ValveType.GPV) SimSetting = double.NaN;
            SimStatus = value ? StatType.OPEN : StatType.CLOSED;
        }

        
    }

}