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
using Epanet.Hydraulic.Models;
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

        /// <summary>Epanet 'S[k]', link current status</summary>
        protected StatType status;

        /// <summary>Epanet 'Q[k]', link flow value</summary>
        protected double flow;

        /// <summary>Epanet 'P[k]', Inverse headloss derivatives</summary>
        protected double invHeadLoss;

        /// <summary>Epanet 'Y[k]', Flow correction factors</summary>
        protected double flowCorrection;

        /// <summary>Epanet 'K[k]', Link setting</summary>
        protected double setting;

        protected StatType oldStatus;



        public SimulationLink(Dictionary<string, SimulationNode> byId, Link @ref, int idx) {

            this.link = @ref;
            this.first = byId[this.link.FirstNode.Name];
            this.second = byId[this.link.SecondNode.Name];
            this.index = idx;

            // Init
            this.setting = this.link.Roughness;
            this.status = this.link.Status;

        }

        public SimulationLink(List<SimulationNode> indexedNodes, Link @ref, int idx) {
            this.link = @ref;

            foreach (SimulationNode indexedNode  in  indexedNodes) {
                if (indexedNode.Id == this.link.FirstNode.Name)
                    this.first = indexedNode;
                else if (indexedNode.Id == this.link.SecondNode.Name)
                    this.second = indexedNode;
                if (this.first != null && this.second != null) break;
            }
            this.index = idx;

            // Init
            this.setting = this.link.Roughness;
            this.status = this.link.Status;
        }

#region Indexed link methods

        public SimulationNode First { get { return this.first; } }

        public SimulationNode Second { get { return this.second; } }

        public Link Link { get { return this.link; } }

        public int Index { get { return this.index; } }

#endregion

#region Network link Getters

        public double C0 { get { return this.link.C0; } }

        public double Diameter { get { return this.link.Diameter; } }

        public double Roughness { get { return this.link.Roughness; } }

        public double Km { get { return this.link.Km; } }

        public double FlowResistance { get { return this.link.FlowResistance; } }

        public LinkType Type { get { return this.link.Type; } }

#endregion

#region Simulation getters & setters

        /// <summary>Epanet 'S[k]', link current status</summary>
        public StatType SimStatus { get { return this.status; } set { this.status = value; } }

        /// <summary>Epanet 'Q[k]', link flow value</summary>
        public double SimFlow { get { return this.flow; } set { this.flow = value; } }

        /// <summary>Epanet 'K[k]', Link setting</summary>
        public double SimSetting { get { return this.setting; } set { this.setting = value; } }

        /// <summary>Epanet 'P[k]', Inverse headloss derivatives</summary>
        public double SimInvHeadLoss { get { return this.invHeadLoss; } set { this.invHeadLoss = value; } }

        /// <summary>Epanet 'Y[k]', Flow correction factors</summary>
        public double SimFlowCorrection { get { return this.flowCorrection; } set { this.flowCorrection = value; } }

        public StatType SimOldStatus { get { return this.oldStatus; } set { this.oldStatus = value; } }

#endregion

#region Simulation Methods

        /// <summary>Sets link status to OPEN(true) or CLOSED(false).</summary>
        public void SetLinkStatus(bool value) {
            if (value) {
                if (this is SimulationPump)
                    this.setting = 1.0;

                else if (this.Type != LinkType.GPV)
                    this.setting = Constants.MISSING;

                this.status = StatType.OPEN;
            }
            else {
                if (this is SimulationPump)
                    this.setting = 0.0;
                else if (this.Type != LinkType.GPV)
                    this.setting = Constants.MISSING;

                this.status = StatType.CLOSED;
            }
        }

        /// <summary>Sets pump speed or valve setting, adjusting link status and flow when necessary.</summary>
        public void SetLinkSetting(double value) {
            if (this is SimulationPump) {
                this.setting = value;
                if (value > 0 && this.status <= StatType.CLOSED)
                    this.status = StatType.OPEN;

                if (value == 0 && this.status > StatType.CLOSED)
                    this.status = StatType.CLOSED;

            }
            else if (this.Type == LinkType.FCV) {
                this.setting = value;
                this.status = StatType.ACTIVE;
            }
            else {
                if (this.setting.IsMissing() && this.status <= StatType.CLOSED)
                    this.status = StatType.OPEN;

                this.setting = value;
            }
        }


        /// <summary>
        /// Sets initial flow in link to QZERO if link is closed, to design flow for a pump, 
        /// or to flow at velocity of 1 fps for other links.
        /// </summary>
        public void InitLinkFlow() {
            if (this.SimStatus == StatType.CLOSED)
                this.flow = Constants.QZERO;
            else if (this is SimulationPump)
                this.flow = this.Roughness * ((SimulationPump)this).Q0;
            else
                this.flow = Math.PI * Math.Pow(this.Diameter, 2) / 4.0;
        }

        public void InitLinkFlow(StatType type, double kc) {
            if (type == StatType.CLOSED)
                this.flow = Constants.QZERO;
            else if (this is SimulationPump)
                this.flow = kc * ((SimulationPump)this).Q0;
            else
                this.flow = Math.PI * Math.Pow(this.Diameter, 2) / 4.0;
        }

        /// <summary>Compute P, Y and matrix coeffs.</summary>
        private void ComputeMatrixCoeff(
            EpanetNetwork net,
            PipeHeadModelCalculators.Compute hlModel,
            IList<Curve> curves,
            SparseMatrix smat,
            LSVariables ls) {

            switch (this.Type) {
            // Pipes
            case LinkType.CV:
            case LinkType.PIPE:
                this.ComputePipeCoeff(net, hlModel);
                break;
            // Pumps
            case LinkType.PUMP:
                ((SimulationPump)this).ComputePumpCoeff(net);
                break;
            // Valves
            case LinkType.PBV:
            case LinkType.TCV:
            case LinkType.GPV:
            case LinkType.FCV:
            case LinkType.PRV:
            case LinkType.PSV:
                // If valve status fixed then treat as pipe
                // otherwise ignore the valve for now.
                if (!((SimulationValve)this).ComputeValveCoeff(net, curves))
                    return;
                break;
            default:
                return;
            }

            int n1 = this.first.Index;
            int n2 = this.second.Index;

            ls.AddNodalInFlow(n1, -this.flow);
            ls.AddNodalInFlow(n2, +this.flow);

            ls.AddAij(smat.GetNdx(this.Index), -this.invHeadLoss);

            if (!(this.first is SimulationTank)) {
                ls.AddAii(smat.GetRow(n1), +this.invHeadLoss);
                ls.AddRhsCoeff(smat.GetRow(n1), +this.flowCorrection);
            }
            else
                ls.AddRhsCoeff(smat.GetRow(n2), +(this.invHeadLoss * this.first.SimHead));

            if (!(this.second is SimulationTank)) {
                ls.AddAii(smat.GetRow(n2), +this.invHeadLoss);
                ls.AddRhsCoeff(smat.GetRow(n2), -this.flowCorrection);
            }
            else
                ls.AddRhsCoeff(smat.GetRow(n1), +(this.invHeadLoss * this.second.SimHead));

        }

        /// <summary>Computes P & Y coefficients for pipe k.</summary>
        private void ComputePipeCoeff(EpanetNetwork net, PipeHeadModelCalculators.Compute hlModel) {
            // For closed pipe use headloss formula: h = CBIG*q
            if (this.status <= StatType.CLOSED) {
                this.invHeadLoss = 1.0 / Constants.CBIG;
                this.flowCorrection = this.flow;
                return;
            }

            hlModel(net, this, out this.invHeadLoss, out this.flowCorrection);
        }


        /// <summary>Closes link flowing into full or out of empty tank.</summary>
        private void TankStatus(EpanetNetwork net) {
            double q = this.flow;
            SimulationNode n1 = this.First;
            SimulationNode n2 = this.Second;

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
            if (tank.Area == 0.0 || this.status <= StatType.CLOSED)
                return;

            // If tank full, then prevent flow into it
            if (tank.SimHead >= tank.Hmax - net.HTol) {
                //Case 1: Link is a pump discharging into tank
                if (this.Type == LinkType.PUMP) {
                    if (this.Second == n1)
                        this.status = StatType.TEMPCLOSED;
                }
                else if (CvStatus(net, StatType.OPEN, h, q) == StatType.CLOSED)
                    //  Case 2: Downstream head > tank head
                    this.status = StatType.TEMPCLOSED;
            }

            // If tank empty, then prevent flow out of it
            if (tank.SimHead <= tank.Hmin + net.HTol) {
                // Case 1: Link is a pump discharging from tank
                if (this.Type == LinkType.PUMP) {
                    if (this.First == n1)
                        this.status = StatType.TEMPCLOSED;
                }
                // Case 2: Tank head > downstream head
                else if (CvStatus(net, StatType.CLOSED, h, q) == StatType.OPEN)
                    this.status = StatType.TEMPCLOSED;
            }
        }

        /// <summary>Updates status of a check valve.</summary>
        private static StatType CvStatus(EpanetNetwork net, StatType s, double dh, double q) {
            if (Math.Abs(dh) > net.HTol) {
                if (dh < -net.HTol)
                    return (StatType.CLOSED);
                else if (q < -net.QTol)
                    return (StatType.CLOSED);
                else
                    return (StatType.OPEN);
            }
            else {
                if (q < -net.QTol)
                    return (StatType.CLOSED);
                else
                    return (s);
            }
        }

        /// <summary>Determines new status for pumps, CVs, FCVs & pipes to tanks.</summary>
        private bool LinkStatus(EpanetNetwork net, TraceSource log) {
            bool change = false;

            double dh = this.first.SimHead - this.second.SimHead;

            StatType tStatus = this.status;

            if (tStatus == StatType.XHEAD || tStatus == StatType.TEMPCLOSED)
                this.status = StatType.OPEN;

            if (this.Type == LinkType.CV)
                this.status = CvStatus(net, this.status, dh, this.flow);

            if (this is SimulationPump && this.status >= StatType.OPEN && this.setting > 0.0)
                this.status = ((SimulationPump)this).PumpStatus(net, -dh);

            if (this.Type == LinkType.FCV && !this.setting.IsMissing())
                this.status = ((SimulationValve)this).FcvStatus(net, tStatus);

            if (this.first is SimulationTank || this.second is SimulationTank)
                this.TankStatus(net);

            if (tStatus != this.status) {
                change = true;
                if (net.Stat_Flag == StatFlag.FULL)
                    LogStatChange(net.FieldsMap, log, this, tStatus, this.status);
            }

            return (change);
        }

        protected static void LogStatChange(
            FieldsMap fMap,
            TraceSource logger,
            SimulationLink link,
            StatType s1,
            StatType s2) {

            if (s1 == s2) {

                switch (link.Type) {
                case LinkType.PRV:
                case LinkType.PSV:
                case LinkType.PBV:
                    link.setting *= fMap.GetUnits(FieldType.PRESSURE);
                    break;
                case LinkType.FCV:
                    link.setting *= fMap.GetUnits(FieldType.FLOW);
                    break;
                }
                logger.Verbose(
                    Text.FMT56,
                    link.Type.ParseStr(),
                    link.Link.Name,
                    link.setting);
                return;
            }

            StatType j1, j2;

            if (s1 == StatType.ACTIVE)
                j1 = StatType.ACTIVE;
            else if (s1 <= StatType.CLOSED)
                j1 = StatType.CLOSED;
            else
                j1 = StatType.OPEN;
            if (s2 == StatType.ACTIVE) j2 = StatType.ACTIVE;
            else if (s2 <= StatType.CLOSED)
                j2 = StatType.CLOSED;
            else
                j2 = StatType.OPEN;

            if (j1 != j2) {
                logger.Verbose(
                    Text.FMT57,
                    link.Type.ParseStr(),
                    link.Link.Name,
                    j1.ReportStr(),
                    j2.ReportStr());
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

        /// <summary>Computes solution matrix coefficients for links.</summary>
        public static void ComputeMatrixCoeffs(
            EpanetNetwork net,
            PipeHeadModelCalculators.Compute hlModel,
            IEnumerable<SimulationLink> links,
            IList<Curve> curves,
            SparseMatrix smat,
            LSVariables ls) {

            foreach (SimulationLink link  in  links) {
                link.ComputeMatrixCoeff(net, hlModel, curves, smat, ls);
            }
        }

#endregion

    }

}