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
using Epanet.Hydraulic.Models;
using Epanet.Log;
using Epanet.Network;
using Epanet.Network.Structures;
using Epanet.Properties;
using Epanet.Util;

namespace Epanet.Hydraulic.Structures {


    public class SimulationLink {


        protected readonly SimulationNode first;
        protected readonly SimulationNode second;
        protected readonly Link link;
        protected readonly int index;

        /// <summary>Epanet 'S[k]', link current status</summary>
        protected Link.StatType status;

        /// <summary>Epanet 'Q[k]', link flow value</summary>
        protected double flow;

        /// <summary>Epanet 'P[k]', Inverse headloss derivatives</summary>
        protected double invHeadLoss;

        /// <summary>Epanet 'Y[k]', Flow correction factors</summary>
        protected double flowCorrection;

        /// <summary>Epanet 'K[k]', Link setting</summary>
        protected double setting;

        protected Link.StatType oldStatus;


        public static SimulationLink CreateIndexedLink(Dictionary<string, SimulationNode> byId, Link @ref, int idx) {
            SimulationLink ret;
            if (@ref is Valve)
                ret = new SimulationValve(new List<SimulationNode>(byId.Values), @ref, idx);
            else if (@ref is Pump)
                ret = new SimulationPump(new List<SimulationNode>(byId.Values), @ref, idx);
            else
                ret = new SimulationLink(byId, @ref, idx);

            return ret;
        }

        public static SimulationLink CreateIndexedLink(List<SimulationNode> indexedNodes, Link @ref, int idx) {
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

            this.link = @ref;
            this.first = byId[this.link.FirstNode.Id];
            this.second = byId[this.link.SecondNode.Id];
            this.index = idx;

            // Init
            this.setting = this.link.Roughness;
            this.status = this.link.Status;

        }

        public SimulationLink(List<SimulationNode> indexedNodes, Link @ref, int idx) {
            this.link = @ref;

            foreach (SimulationNode indexedNode  in  indexedNodes) {
                if (indexedNode.Id == this.link.FirstNode.Id)
                    this.first = indexedNode;
                else if (indexedNode.Id == this.link.SecondNode.Id)
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

        public double[] C0 { get { return this.link.C0; } }

        public double Diameter { get { return this.link.Diameter; } }

        public double Roughness { get { return this.link.Roughness; } }

        public double Km { get { return this.link.Km; } }

        public double FlowResistance { get { return this.link.FlowResistance; } }

        public Link.LinkType Type { get { return this.link.Type; } }

        #endregion

        #region Simulation getters & setters

        /// <summary>Epanet 'S[k]', link current status</summary>
        public Link.StatType SimStatus { get { return this.status; } set { this.status = value; } }

        /// <summary>Epanet 'Q[k]', link flow value</summary>
        public double SimFlow { get { return this.flow; } set { this.flow = value; } }

        /// <summary>Epanet 'K[k]', Link setting</summary>
        public double SimSetting { get { return this.setting; } set { this.setting = value; } }

        /// <summary>Epanet 'P[k]', Inverse headloss derivatives</summary>
        public double SimInvHeadLoss { get { return this.invHeadLoss; } set { this.invHeadLoss = value; } }

        /// <summary>Epanet 'Y[k]', Flow correction factors</summary>
        public double SimFlowCorrection { get { return this.flowCorrection; } set { this.flowCorrection = value; } }

        public Link.StatType SimOldStatus { get { return this.oldStatus; } set { this.oldStatus = value; } }

        #endregion

        // Simulation Methods

        /// <summary>Sets link status to OPEN(true) or CLOSED(false).</summary>
        public void SetLinkStatus(bool value) {
            if (value) {
                if (this is SimulationPump)
                    this.setting = 1.0;

                else if (this.Type != Link.LinkType.GPV)
                    this.setting = Constants.MISSING;

                this.status = Link.StatType.OPEN;
            }
            else {
                if (this is SimulationPump)
                    this.setting = 0.0;
                else if (this.Type != Link.LinkType.GPV)
                    this.setting = Constants.MISSING;

                this.status = Link.StatType.CLOSED;
            }
        }

        /// <summary>Sets pump speed or valve setting, adjusting link status and flow when necessary.</summary>
        public void SetLinkSetting(double value) {
            if (this is SimulationPump) {
                this.setting = value;
                if (value > 0 && this.status <= Link.StatType.CLOSED)
                    this.status = Link.StatType.OPEN;

                if (value == 0 && this.status > Link.StatType.CLOSED)
                    this.status = Link.StatType.CLOSED;

            }
            else if (this.Type == Link.LinkType.FCV) {
                this.setting = value;
                this.status = Link.StatType.ACTIVE;
            }
            else {
                if (this.setting == Constants.MISSING && this.status <= Link.StatType.CLOSED)
                    this.status = Link.StatType.OPEN;

                this.setting = value;
            }
        }


        /// <summary>
        /// Sets initial flow in link to QZERO if link is closed, to design flow for a pump, 
        /// or to flow at velocity of 1 fps for other links.
        /// </summary>
        public void InitLinkFlow() {
            if (this.SimStatus == Link.StatType.CLOSED)
                this.flow = Constants.QZERO;
            else if (this is SimulationPump)
                this.flow = this.Roughness * ((SimulationPump)this).Q0;
            else
                this.flow = Math.PI * Math.Pow(this.Diameter, 2) / 4.0;
        }

        public void InitLinkFlow(Link.StatType type, double kc) {
            if (type == Link.StatType.CLOSED)
                this.flow = Constants.QZERO;
            else if (this is SimulationPump)
                this.flow = kc * ((SimulationPump)this).Q0;
            else
                this.flow = Math.PI * Math.Pow(this.Diameter, 2) / 4.0;
        }

        /// <summary>Compute P, Y and matrix coeffs.</summary>
        private void ComputeMatrixCoeff(
            FieldsMap fMap,
            PropertiesMap pMap,
            PipeHeadModel hlModel,
            IList<Curve> curves,
            SparseMatrix smat,
            LSVariables ls) {

            switch (this.Type) {
            // Pipes
            case Link.LinkType.CV:
            case Link.LinkType.PIPE:
                this.ComputePipeCoeff(pMap, hlModel);
                break;
            // Pumps
            case Link.LinkType.PUMP:
                ((SimulationPump)this).ComputePumpCoeff(fMap, pMap);
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
                if (!((SimulationValve)this).computeValveCoeff(fMap, pMap, curves))
                    return;
                break;
            default:
                return;
            }

            int n1 = this.first.Index;
            int n2 = this.second.Index;

            ls.addNodalInFlow(n1, -this.flow);
            ls.addNodalInFlow(n2, +this.flow);

            ls.addAij(smat.getNdx(this.Index), -this.invHeadLoss);

            if (!(this.first is SimulationTank)) {
                ls.addAii(smat.getRow(n1), +this.invHeadLoss);
                ls.addRHSCoeff(smat.getRow(n1), +this.flowCorrection);
            }
            else
                ls.addRHSCoeff(smat.getRow(n2), +(this.invHeadLoss * this.first.SimHead));

            if (!(this.second is SimulationTank)) {
                ls.addAii(smat.getRow(n2), +this.invHeadLoss);
                ls.addRHSCoeff(smat.getRow(n2), -this.flowCorrection);
            }
            else
                ls.addRHSCoeff(smat.getRow(n1), +(this.invHeadLoss * this.second.SimHead));

        }

        // Computes P & Y coefficients for pipe k
        private void ComputePipeCoeff(PropertiesMap pMap, PipeHeadModel hlModel) {
            // For closed pipe use headloss formula: h = CBIG*q
            if (this.status <= Link.StatType.CLOSED) {
                this.invHeadLoss = 1.0 / Constants.CBIG;
                this.flowCorrection = this.flow;
                return;
            }

            PipeHeadModel.LinkCoeffs coeffs = hlModel.compute(pMap, this);
            this.invHeadLoss = coeffs.InvHeadLoss;
            this.flowCorrection = coeffs.FlowCorrection;
        }


        // Closes link flowing into full or out of empty tank
        private void TankStatus(PropertiesMap pMap) {
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
            if (tank.Area == 0.0 || this.status <= Link.StatType.CLOSED)
                return;

            // If tank full, then prevent flow into it
            if (tank.SimHead >= tank.Hmax - pMap.Htol) {
                //Case 1: Link is a pump discharging into tank
                if (this.Type == Link.LinkType.PUMP) {
                    if (this.Second == n1)
                        this.status = Link.StatType.TEMPCLOSED;
                }
                else if (CvStatus(pMap, Link.StatType.OPEN, h, q) == Link.StatType.CLOSED)
                    //  Case 2: Downstream head > tank head
                    this.status = Link.StatType.TEMPCLOSED;
            }

            // If tank empty, then prevent flow out of it
            if (tank.SimHead <= tank.Hmin + pMap.Htol) {
                // Case 1: Link is a pump discharging from tank
                if (this.Type == Link.LinkType.PUMP) {
                    if (this.First == n1)
                        this.status = Link.StatType.TEMPCLOSED;
                }
                // Case 2: Tank head > downstream head
                else if (CvStatus(pMap, Link.StatType.CLOSED, h, q) == Link.StatType.OPEN)
                    this.status = Link.StatType.TEMPCLOSED;
            }
        }

        /// <summary>Updates status of a check valve.</summary>
        private static Link.StatType CvStatus(PropertiesMap pMap, Link.StatType s, double dh, double q) {
            if (Math.Abs(dh) > pMap.Htol) {
                if (dh < -pMap.Htol)
                    return (Link.StatType.CLOSED);
                else if (q < -pMap.Qtol)
                    return (Link.StatType.CLOSED);
                else
                    return (Link.StatType.OPEN);
            }
            else {
                if (q < -pMap.Qtol)
                    return (Link.StatType.CLOSED);
                else
                    return (s);
            }
        }

        /// <summary>Determines new status for pumps, CVs, FCVs & pipes to tanks.</summary>
        private bool LinkStatus(PropertiesMap pMap, FieldsMap fMap, TraceSource log) {
            bool change = false;

            double dh = this.first.SimHead - this.second.SimHead;

            Link.StatType tStatus = this.status;

            if (tStatus == Link.StatType.XHEAD || tStatus == Link.StatType.TEMPCLOSED)
                this.status = Link.StatType.OPEN;

            if (this.Type == Link.LinkType.CV)
                this.status = CvStatus(pMap, this.status, dh, this.flow);

            if (this is SimulationPump && this.status >= Link.StatType.OPEN && this.setting > 0.0)
                this.status = ((SimulationPump)this).PumpStatus(pMap, -dh);

            if (this.Type == Link.LinkType.FCV && this.setting != Constants.MISSING)
                this.status = ((SimulationValve)this).fcvStatus(pMap, tStatus);

            if (this.first is SimulationTank || this.second is SimulationTank)
                this.TankStatus(pMap);

            if (tStatus != this.status) {
                change = true;
                if (pMap.Statflag == PropertiesMap.StatFlag.FULL)
                    LogStatChange(fMap, log, this, tStatus, this.status);
            }

            return (change);
        }

        protected static void LogStatChange(
            FieldsMap fMap,
            TraceSource logger,
            SimulationLink link,
            Link.StatType s1,
            Link.StatType s2) {

            if (s1 == s2) {

                switch (link.Type) {
                case Link.LinkType.PRV:
                case Link.LinkType.PSV:
                case Link.LinkType.PBV:
                    link.setting *= fMap.GetUnits(FieldsMap.FieldType.PRESSURE);
                    break;
                case Link.LinkType.FCV:
                    link.setting *= fMap.GetUnits(FieldsMap.FieldType.FLOW);
                    break;
                }
                logger.Verbose(
                    Text.ResourceManager.GetString("FMT56"),
                    link.Type.ParseStr(),
                    link.Link.Id,
                    link.setting);
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
                logger.Verbose(
                    Text.ResourceManager.GetString("FMT57"),
                    link.Type.ParseStr(),
                    link.Link.Id,
                    j1.ReportStr(),
                    j2.ReportStr());
            }

        }

        /// <summary>Determines new status for pumps, CVs, FCVs & pipes to tanks.</summary>
        public static bool LinkStatus(PropertiesMap pMap, FieldsMap fMap, TraceSource log, List<SimulationLink> links) {
            bool change = false;
            foreach (SimulationLink link  in  links) {
                if (link.LinkStatus(pMap, fMap, log))
                    change = true;
            }
            return change;
        }

        /// <summary>Computes solution matrix coefficients for links.</summary>
        public static void ComputeMatrixCoeffs(
            FieldsMap fMap,
            PropertiesMap pMap,
            PipeHeadModel hlModel,
            List<SimulationLink> links,
            IList<Curve> curves,
            SparseMatrix smat,
            LSVariables ls) {

            foreach (SimulationLink link  in  links) {
                link.ComputeMatrixCoeff(fMap, pMap, hlModel, curves, smat, ls);
            }
        }
    }

}