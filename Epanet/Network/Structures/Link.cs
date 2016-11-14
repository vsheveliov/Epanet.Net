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

namespace org.addition.epanet.network.structures {

    ///<summary>Hydraulic link structure (pipe)</summary>
    public class Link:IComparable<Link> {

        ///<summary>Init links flow resistance values.</summary>
        public void initResistance(PropertiesMap.FormType formflag, double hexp) {
            double e, d, L;
            this.FlowResistance = Constants.CSMALL;

            switch (this.Type) {
            case LinkType.CV:
            case LinkType.PIPE:
                e = this.Roughness;
                d = this.Diameter;
                L = this.Lenght;
                switch (formflag) {
                case PropertiesMap.FormType.HW:
                    this.FlowResistance = 4.727 * L / Math.Pow(e, hexp) / Math.Pow(d, 4.871);
                    break;
                case PropertiesMap.FormType.DW:
                    this.FlowResistance = L / 2.0 / 32.2 / d / Math.Pow(Math.PI * Math.Pow(d, 2) / 4.0, 2);
                    break;
                case PropertiesMap.FormType.CM:
                    this.FlowResistance = Math.Pow(4.0 * e / (1.49 * Math.PI * d * d), 2) *
                                          Math.Pow((d / 4.0), -1.333) * L;
                    break;
                }
                break;

            case LinkType.PUMP:
                this.FlowResistance = Constants.CBIG;
                break;
            }
        }

        /// <summary>Type of link</summary>
        public enum LinkType {
            /// <summary>Pipe with check valve.</summary>
            CV = 0,
            /// <summary>Regular pipe.</summary>
            PIPE = 1,
            /// <summary>Pump.</summary>
            PUMP = 2,
            /// <summary>Pressure reducing valve.</summary>
            PRV = 3,
            /// <summary>Pressure sustaining valve.</summary>
            PSV = 4,
            /// <summary>Pressure breaker valve.</summary>
            PBV = 5,
            /// <summary>Flow control valve.</summary>
            FCV = 6,
            /// <summary>Throttle control valve.</summary>
            TCV = 7,
            /// <summary>General purpose valve.</summary>
            GPV = 8
        }

        ///<summary>Link/Tank/Pump status</summary>
        public enum StatType {
            /// <summary>Pump cannot deliver head (closed).</summary>
            XHEAD = 0,
            /// <summary>Temporarily closed.</summary>
            TEMPCLOSED = 1,
            /// <summary>Closed.</summary>
            CLOSED = 2,
            /// <summary>Open.</summary>
            OPEN = 3,
            /// <summary>Valve active (partially open).</summary>
            ACTIVE = 4,
            /// <summary>Pump exceeds maximum flow.</summary>
            XFLOW = 5,
            /// <summary>FCV cannot supply flow.</summary>
            XFCV = 6,
            /// <summary>Valve cannot supply pressure.</summary>
            XPRESSURE = 7,
            /// <summary>Tank filling.</summary>
            FILLING = 8,
            /// <summary>Tank emptying.</summary>
            EMPTYING = 9,
        }

        private readonly string _id;
        private readonly List<EnPoint> _vertices;

        public Link(string id) {
            this._id = id;
            this.Comment = "";
            this._vertices = new List<EnPoint>();
            this.Type = LinkType.CV;
            this.Status = StatType.XHEAD;
        }

        ///<summary>Initial species concentrations.</summary>
        public double[] C0 { get; set; }

        ///<summary>Link comment (parsed from INP or excel file)</summary>
        public string Comment { get; set; }

        ///<summary>Link diameter (feet).</summary>
        public double Diameter { get; set; }

        ///<summary>First node.</summary>
        public Node FirstNode { get; set; }

        ///<summary>Flow resistance.</summary>
        public double FlowResistance { get; set; }

        ///<summary>Link name.</summary>
        public string Id { get { return this._id; } }

        ///<summary>Bulk react. coeff.</summary>
        public double Kb { get; set; }

        ///<summary>Minor loss coeff.</summary>
        public double Km { get; set; }

        ///<summary>Wall react. coeff.</summary>
        public double Kw { get; set; }

        ///<summary>Link length (feet).</summary>
        public double Lenght { get; set; }

        ///<summary>Kinetic parameter values.</summary>
        public double[] Param { get; set; }

        ///<summary>Roughness factor.</summary>
        public double Roughness { get; set; }

        ///<summary>Second node.</summary>
        public Node SecondNode { get; set; }

        ///<summary>Link status.</summary>
        public StatType Status { get; set; }

        ///<summary>Link subtype.</summary>
        public LinkType Type { get; set; }

        ///<summary>List of points for link path rendering.</summary>
        public List<EnPoint> Vertices { get { return this._vertices; } }

        ///<summary>Link report flag.</summary>
        public bool RptFlag { get; set; }

        public override int GetHashCode() { return string.IsNullOrEmpty(this._id) ? 0 : this._id.GetHashCode(); }

        public int CompareTo(Link o) {
            if(o == null) return 1;
            return string.Compare(this._id, o._id, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) {
            Link o = obj as Link;
            if(o == null) return false;

            return string.Equals(this.Id, o.Id, StringComparison.OrdinalIgnoreCase);
        }

        public void SetDiameterAndUpdate(double diameter, Network net) {
            double realkm = this.Km * Math.Pow(this.Diameter, 4.0) / 0.02517;
            this.Diameter = diameter;
            this.Km = 0.02517 * realkm / Math.Pow(diameter, 4);
            initResistance(net.PropertiesMap.Formflag, net.PropertiesMap.Hexp);
        }

#if DEBUG // NUCONVERT
        public double GetNuDiameter(PropertiesMap.UnitsType utype) {
            return NUConvert.revertDiameter(utype, this.Diameter);
        }

        public double GetNuLength(PropertiesMap.UnitsType utype) {
            return NUConvert.revertDistance(utype, this.Lenght);
        }

        public void SetNuDiameter(PropertiesMap.UnitsType utype, double value) {
            this.Diameter = NUConvert.convertDistance(utype, value);
        }

        public void SetNuLenght(PropertiesMap.UnitsType utype, double value) {
            this.Lenght = NUConvert.convertDistance(utype, value);
        }

        public double GetNuRoughness(
            PropertiesMap.FlowUnitsType fType,
            PropertiesMap.PressUnitsType pType,
            double spGrav) {
            switch (this.Type) {
            case LinkType.FCV:
                return NUConvert.revertFlow(fType, this.Roughness);
            case LinkType.PRV:
            case LinkType.PSV:
            case LinkType.PBV:
                return NUConvert.revertPressure(pType, spGrav, this.Roughness);
            }
            return this.Roughness;
        }

#endif
    }

}