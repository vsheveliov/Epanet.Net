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

using Epanet.Enums;

namespace Epanet.Network.Structures {

    ///<summary>Hydraulic link structure (pipe)</summary>
    public class Link:Element {

        ///<summary>Init links flow resistance values.</summary>
        public void InitResistance(FormType formflag, double hexp) {
            FlowResistance = Constants.CSMALL;

            switch (Type) {
            case LinkType.CV:
            case LinkType.PIPE:
                double e = Kc;
                double d = Diameter;
                double L = Lenght;

                switch (formflag) {
                case FormType.HW:
                    FlowResistance = 4.727 * L / Math.Pow(e, hexp) / Math.Pow(d, 4.871);
                    break;
                case FormType.DW:
                    FlowResistance = L / 2.0 / 32.2 / d / Math.Pow(Math.PI * Math.Pow(d, 2) / 4.0, 2);
                    break;
                case FormType.CM:
                    FlowResistance = Math.Pow(4.0 * e / (1.49 * Math.PI * d * d), 2) *
                                          Math.Pow((d / 4.0), -1.333) * L;
                    break;
                }
                break;

            case LinkType.PUMP:
                FlowResistance = Constants.CBIG;
                break;
            }
        }

        private readonly List<EnPoint> vertices;

        public Link(string name):base(name) {
            vertices = new List<EnPoint>();
            Type = LinkType.CV;
            Status = StatType.XHEAD;
        }

        ///<summary>Initial species concentrations.</summary>
        public double C0 { get; set; }

        public override ElementType ElementType {
            get { return ElementType.Link; }
        }

        ///<summary>Link diameter (feet).</summary>
        public double Diameter { get; set; }

        ///<summary>First node.</summary>
        public Node FirstNode { get; set; }

        ///<summary>Flow resistance.</summary>
        public double FlowResistance { get; set; }

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
        public double Kc { get; set; }

        ///<summary>Second node.</summary>
        public Node SecondNode { get; set; }

        ///<summary>Link status.</summary>
        public StatType Status { get; set; }

        ///<summary>Link subtype.</summary>
        public LinkType Type { get; set; }

        ///<summary>List of points for link path rendering.</summary>
        public List<EnPoint> Vertices { get { return vertices; } }

        ///<summary>Link report flag.</summary>
        public bool RptFlag { get; set; }

        public void SetDiameterAndUpdate(double diameter, Network net) {
            double realkm = Km * Math.Pow(Diameter, 4.0) / 0.02517;
            Diameter = diameter;
            Km = 0.02517 * realkm / Math.Pow(diameter, 4);
            InitResistance(net.FormFlag, net.HExp);
        }

#if NUCONVERT
        public double GetNuDiameter(UnitsType utype) {
            return NUConvert.RevertDiameter(utype, Diameter);
        }

        public double GetNuLength(UnitsType utype) {
            return NUConvert.RevertDistance(utype, Lenght);
        }

        public void SetNuDiameter(UnitsType utype, double value) {
            Diameter = NUConvert.ConvertDistance(utype, value);
        }

        public void SetNuLenght(UnitsType utype, double value) {
            Lenght = NUConvert.ConvertDistance(utype, value);
        }

        public double GetNuRoughness(
            FlowUnitsType fType,
            PressUnitsType pType,
            double spGrav) {
            switch (Type) {
            case LinkType.FCV:
                return NUConvert.RevertFlow(fType, Kc);
            case LinkType.PRV:
            case LinkType.PSV:
            case LinkType.PBV:
                return NUConvert.RevertPressure(pType, spGrav, Kc);
            }
            return Kc;
        }

#endif
    }

}