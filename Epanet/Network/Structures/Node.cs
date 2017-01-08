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

using System.Collections.Generic;

using Epanet.Enums;

namespace Epanet.Network.Structures {

    ///<summary>Hydraulic node structure  (junction)</summary>

    public class Node: Element {
        private readonly List<Demand> demand = new List<Demand>();

        public Node(string name):base(name) {
            this.C0 = 0.0;
            this.InitDemand = 0;
            this.Position = EnPoint.Invalid;
        }

        public override ElementType ElementType { get { return ElementType.Node; } }

        public double InitDemand { get; set; }

        public virtual NodeType Type { get { return NodeType.JUNC; } }

        ///<summary>Node position.</summary>
        public EnPoint Position { get; set; }

        ///<summary>Node elevation(foot).</summary>
        public double Elevation { get; set; }

        ///<summary>Node demand list.</summary>
        public List<Demand> Demand { get { return this.demand; } }

        ///<summary>Water quality source.</summary>
        public QualSource QualSource { get; set; }

        ///<summary>Initial species concentrations.</summary>
        public double C0 { get; set; }

        ///<summary>Emitter coefficient.</summary>
        public double Ke { get; set; }

        ///<summary>Node reporting flag.</summary>
        public bool RptFlag { get; set; }


        

#if NUCONVERT

        public double GetNuElevation(PropertiesMap.UnitsType units) {
            return NUConvert.revertDistance(units, this.Elevation);
        }

        public void SetNuElevation(PropertiesMap.UnitsType units, double elev) {
            this.Elevation = NUConvert.convertDistance(units, elev);
        }

#endif

    }

}