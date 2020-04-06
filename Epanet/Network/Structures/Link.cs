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
    public abstract class Link:Element {

        ///<summary>Init links flow resistance values.</summary>
        public abstract void InitResistance(FormType formflag, double hexp);

        public abstract void ConvertUnits(Network nw);

        protected Link(string name):base(name) {
            Vertices = new List<EnPoint>();
            Status = StatType.XHEAD;
        }

        public override ElementType ElementType => ElementType.LINK;

        ///<summary>Link subtype.</summary>
        public abstract LinkType LinkType { get; }

        ///<summary>Initial species concentrations.</summary>
        public double C0 { get; set; }

        ///<summary>Link diameter (feet).</summary>
        public double Diameter { get; set; }

        ///<summary>First node.</summary>
        public Node FirstNode { get; set; }

        ///<summary>Flow resistance.</summary>
        public double FlowResistance { get; protected set; }

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

        ///<summary>List of points for link path rendering.</summary>
        public List<EnPoint> Vertices { get; }

        ///<summary>Link report flag.</summary>
        public bool RptFlag { get; set; }

        public void SetDiameterAndUpdate(double diameter, Network net) {
            double realkm = Km * Math.Pow(Diameter, 4.0) / 0.02517;
            Diameter = diameter;
            Km = 0.02517 * realkm / Math.Pow(diameter, 4);
            InitResistance(net.FormFlag, net.HExp);
        }

        /// <summary>Returns string with length of pipe with given index.</summary>

        public double GetPipeLength(UnitsType type)
        {
            double length = 0;
            
            EnPoint pt1 = FirstNode.Coordinate;
            
            foreach(var pt in Vertices) {
                length += pt1.DistanceTo(pt);
                pt1 = pt;
            }
            
            length += pt1.DistanceTo(SecondNode.Coordinate);

            // length = MapDimensions.LengthUCF * length;
            if(type == UnitsType.SI)
                length *= 1 / Constants.MperFT;

            return Lenght = length;
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

        public virtual double GetNuRoughness(FlowUnitsType fType, PressUnitsType pType, double spGrav) {
            return Kc;
        }

#endif
    }

}