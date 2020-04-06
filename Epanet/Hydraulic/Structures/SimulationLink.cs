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
using Epanet.Properties;
using Epanet.Util;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet.Hydraulic.Structures {


    public class SimulationLink {
        protected readonly SimulationNode first;
        protected readonly SimulationNode second;
        protected readonly Link link;
        protected readonly int index;

        /// <summary>Epanet 'P[k]', Inverse headloss derivatives</summary>
        private double _simInvHeadLoss;
        /// <summary>Epanet 'Y[k]', Flow correction factors</summary>
        private double _simFlowCorrection;

        public SimulationLink(Dictionary<string, SimulationNode> byId, Link @ref, int idx) {

            link = @ref;
            first = byId[link.FirstNode.Name];
            second = byId[link.SecondNode.Name];
            index = idx;

            // Init
            SimSetting = link.Kc;
            SimStatus = link.Status;

        }

        public SimulationLink(List<SimulationNode> indexedNodes, Link @ref, int idx) {
            link = @ref;

            foreach (SimulationNode indexedNode  in  indexedNodes) {
                if (indexedNode.Id == link.FirstNode.Name)
                    first = indexedNode;
                else if (indexedNode.Id == link.SecondNode.Name)
                    second = indexedNode;
                if (first != null && second != null) break;
            }
            index = idx;

            // Init
            SimSetting = link.Kc;
            SimStatus = link.Status;
        }

#region Indexed link methods

        public SimulationNode First => first;

        public SimulationNode Second => second;

        public Link Link => link;

        public int Index => index;

        #endregion

#region Network link Getters

        public double Roughness => link.Kc;

        public double Km => link.Km;

        public LinkType LinkType => link.LinkType;

        #endregion

#region Simulation getters & setters

        /// <summary>Epanet 'S[k]', link current status</summary>
        public StatType SimStatus { get; set; }

        /// <summary>Epanet 'Q[k]', link flow value</summary>
        public double SimFlow { get; set; }

        /// <summary>Epanet 'K[k]', Link setting</summary>
        public double SimSetting { get; set; }

        /// <summary>Epanet 'P[k]', Inverse headloss derivatives</summary>
        public double SimInvHeadLoss {
            get => _simInvHeadLoss;
            set => _simInvHeadLoss = value;
        }

        /// <summary>Epanet 'Y[k]', Flow correction factors</summary>
        public double SimFlowCorrection {
            get => _simFlowCorrection;
            set => _simFlowCorrection = value;
        }

        public StatType SimOldStatus { get; set; }

        #endregion

#region Simulation Methods

        /// <summary>Sets link status to OPEN(true) or CLOSED(false).</summary>
        public virtual void SetLinkStatus(bool value) {
            
            if (value) {
                SimSetting = double.NaN;
                SimStatus = StatType.OPEN;
            }
            else {
                SimSetting = double.NaN;
                SimStatus = StatType.CLOSED;
            }
        }

        // TODO: dispatch it in ineritors
        /// <summary>Sets pump speed or valve setting, adjusting link status and flow when necessary.</summary>
        public void SetLinkSetting(double value) {
            if (LinkType == LinkType.PUMP) {
                SimSetting = value;
                if (value > 0 && SimStatus <= StatType.CLOSED)
                    SimStatus = StatType.OPEN;

                if (value.IsZero() && SimStatus > StatType.CLOSED)
                    SimStatus = StatType.CLOSED;

            }
            else if (LinkType == LinkType.VALVE && ((Valve)link).ValveType == ValveType.FCV) {
                SimSetting = value;
                SimStatus = StatType.ACTIVE;
            }
            else {
                if (double.IsNaN(SimSetting) && SimStatus <= StatType.CLOSED)
                    SimStatus = StatType.OPEN;

                SimSetting = value;
            }
        }


        /// <summary>
        /// Sets initial flow in link to QZERO if link is closed, to design flow for a pump, 
        /// or to flow at velocity of 1 fps for other links.
        /// </summary>
        public void InitLinkFlow() {
            if (SimStatus == StatType.CLOSED)
                SimFlow = Constants.QZERO;
            else if (this is SimulationPump)
                SimFlow = Roughness * ((SimulationPump)this).Q0;
            else
                SimFlow = Math.PI * Math.Pow(link.Diameter, 2) / 4.0;
        }

        public void InitLinkFlow(StatType type, double kc) {
            if (type == StatType.CLOSED)
                SimFlow = Constants.QZERO;
            else if (this is SimulationPump)
                SimFlow = kc * ((SimulationPump)this).Q0;
            else
                SimFlow = Math.PI * Math.Pow(link.Diameter, 2) / 4.0;
        }

        /// <summary>Compute P, Y and matrix coeffs.</summary>
        internal void ComputeMatrixCoeff(
            EpanetNetwork net,
            PipeHeadModelCalculator hlModel,
            IList<Curve> curves,
            SparseMatrix smat,
            LsVariables ls) {

            switch (LinkType) {
            // Pipes
            case LinkType.PIPE:
                ComputePipeCoeff(net, hlModel);
                break;
            // Pumps
            case LinkType.PUMP:
                ((SimulationPump)this).ComputePumpCoeff(net);
                break;
            // Valves
            case LinkType.VALVE:
                // If valve status fixed then treat as pipe
                // otherwise ignore the valve for now.
                if (!((SimulationValve)this).ComputeValveCoeff(net, curves))
                    return;
                break;
            default:
                return;
            }

            int n1 = first.Index;
            int n2 = second.Index;

            ls.x[n1] -= SimFlow;
            ls.x[n2] += SimFlow;

            ls.aij[smat.GetNdx(Index)] -= _simInvHeadLoss;

            if (first is SimulationTank) {
                ls.f[smat.GetRow(n2)] += _simInvHeadLoss * first.SimHead;
            }
            else {
                ls.aii[smat.GetRow(n1)] += _simInvHeadLoss;
                ls.f[smat.GetRow(n1)] += _simFlowCorrection;
            }

            if (second is SimulationTank) {
                ls.f[smat.GetRow(n1)] += _simInvHeadLoss * second.SimHead;
            }
            else {
                ls.aii[smat.GetRow(n2)] += _simInvHeadLoss;
                ls.f[smat.GetRow(n2)] -= _simFlowCorrection;
            }
        }

        /// <summary>Computes P & Y coefficients for pipe k.</summary>
        private void ComputePipeCoeff(EpanetNetwork net, PipeHeadModelCalculator hlModel) {
            // For closed pipe use headloss formula: h = CBIG*q
            if (SimStatus <= StatType.CLOSED) {
                _simInvHeadLoss = 1.0 / Constants.CBIG;
                _simFlowCorrection = SimFlow;
                return;
            }

            hlModel(net, this, out _simInvHeadLoss, out _simFlowCorrection);
        }


        /// <summary>Closes link flowing into full or out of empty tank.</summary>
        private void TankStatus(EpanetNetwork net) {
            double q = SimFlow;
            SimulationNode n1 = First;
            SimulationNode n2 = Second;

            // Make node n1 be the tank
            if (!(n1 is SimulationTank)) {
                if (!(n2 is SimulationTank))
                    return; // neither n1 or n2 is a tank
                // N2 is a tank, swap !
                SimulationNode n = n1;
                n1 = n2;
                n2 = n;
                q = -q;
            }

            double h = n1.SimHead - n2.SimHead;

            SimulationTank tank = (SimulationTank)n1;

            // Skip reservoirs & closed links
            if (tank.Area.IsZero() || SimStatus <= StatType.CLOSED)
                return;

            // If tank full, then prevent flow into it
            if (tank.SimHead >= tank.Hmax - net.HTol) {
                //Case 1: Link is a pump discharging into tank
                if (LinkType == LinkType.PUMP) {
                    if (Second == n1)
                        SimStatus = StatType.TEMPCLOSED;
                }
                else if (CvStatus(net, StatType.OPEN, h, q) == StatType.CLOSED)
                    //  Case 2: Downstream head > tank head
                    SimStatus = StatType.TEMPCLOSED;
            }

            // If tank empty, then prevent flow out of it
            if (tank.SimHead <= tank.Hmin + net.HTol) {
                // Case 1: Link is a pump discharging from tank
                if (LinkType == LinkType.PUMP) {
                    if (First == n1)
                        SimStatus = StatType.TEMPCLOSED;
                }
                // Case 2: Tank head > downstream head
                else if (CvStatus(net, StatType.CLOSED, h, q) == StatType.OPEN)
                    SimStatus = StatType.TEMPCLOSED;
            }
        }

        /// <summary>Updates status of a check valve.</summary>
        private static StatType CvStatus(EpanetNetwork net, StatType s, double dh, double q) {
            if (Math.Abs(dh) > net.HTol) {
                if (dh < -net.HTol) return StatType.CLOSED;
                return q < -net.QTol ? StatType.CLOSED : StatType.OPEN;
            }

            return q < -net.QTol ? StatType.CLOSED : s;
        }

        /// <summary>Determines new status for pumps, CVs, FCVs & pipes to tanks.</summary>
        private bool LinkStatus(EpanetNetwork net, TraceSource log) {
            bool change = false;

            double dh = first.SimHead - second.SimHead;

            StatType tStatus = SimStatus;

            if (tStatus == StatType.XHEAD || tStatus == StatType.TEMPCLOSED)
                SimStatus = StatType.OPEN;

            if (link.LinkType == LinkType.PIPE  && ((Pipe)link).HasCheckValve)
                SimStatus = CvStatus(net, SimStatus, dh, SimFlow);

            if (this is SimulationPump pump && SimStatus >= StatType.OPEN && SimSetting > 0.0)
                SimStatus = pump.PumpStatus(net, -dh);

            if (LinkType == LinkType.VALVE && ((Valve)link).ValveType == ValveType.FCV) {
                if (!double.IsNaN(SimSetting))
                    SimStatus = ((SimulationValve)this).FcvStatus(net, tStatus);
            }

            if (first is SimulationTank || second is SimulationTank)
                TankStatus(net);

            if (tStatus != SimStatus) {
                change = true;
                if (net.StatFlag == StatFlag.FULL)
                    LogStatChange(net.FieldsMap, log, this, tStatus, SimStatus);
            }

            return change;
        }

        protected static void LogStatChange(
            FieldsMap fMap,
            TraceSource logger,
            SimulationLink link,
            StatType s1,
            StatType s2) {

            if (s1 == s2) {
                if (link.LinkType == LinkType.VALVE) {
                    switch (((Valve)link.link).ValveType) {
                        case ValveType.PRV:
                        case ValveType.PSV:
                        case ValveType.PBV:
                            link.SimSetting *= fMap.GetUnits(FieldType.PRESSURE);
                            break;
                        case ValveType.FCV:
                            link.SimSetting *= fMap.GetUnits(FieldType.FLOW);
                            break;
                    }
                }

                logger.Verbose(Text.FMT56, link.LinkType.Keyword2(), link.Link.Name, link.SimSetting);
                return;
            }

            StatType j1, j2;

            switch (s1) {
                case StatType.XHEAD:
                case StatType.TEMPCLOSED:
                case StatType.CLOSED:
                    j1 = StatType.CLOSED;
                    break;

                case StatType.ACTIVE:
                    j1 = StatType.ACTIVE;
                    break;

                default:
                    j1 = StatType.OPEN;
                    break;
            }

            switch (s2) {
                case StatType.XHEAD:
                case StatType.TEMPCLOSED:
                case StatType.CLOSED:
                    j2 = StatType.CLOSED;
                    break;

                case StatType.ACTIVE:
                    j2 = StatType.ACTIVE;
                    break;

                default:
                    j2 = StatType.OPEN;
                    break;
            }

            if (j1 != j2) {
                logger.Verbose(Text.FMT57, link.LinkType.Keyword2(), link.Link.Name, j1.ReportStr(), j2.ReportStr());
            }

        }

        /// <summary>Determines new status for pumps, CVs, FCVs & pipes to tanks.</summary>
        public static bool LinkStatus(EpanetNetwork net, TraceSource log, IEnumerable<SimulationLink> links) {
            bool change = false;

            foreach (SimulationLink link  in  links) {
                if (link.LinkStatus(net, log))
                    change = true;
            }

            return change;
        }

        #endregion

    }

}