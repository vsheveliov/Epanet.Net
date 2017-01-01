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

    ///<summary>Hydraulic node structure  (junction)</summary>

    public class Node:IComparable<Node>, IStringKeyed {
        private readonly string id;
        private readonly List<Demand> demand = new List<Demand>();

        [NonSerialized]
        private double initDemand;
        
        public Node(string id) {
            this.id = id;
            this.C0 = 0.0;
            this.Comment = "";
            this.initDemand = 0;
            this.Position = EnPoint.Invalid;
        }

        ///<summary>Node comment.</summary>
        public string Comment { get; set; }

        public double InitDemand { get { return this.initDemand; } set { this.initDemand = value; } }

        public virtual NodeType Type { get { return NodeType.JUNC; } }

        ///<summary>Node position.</summary>
        public EnPoint Position { get; set; }

        ///<summary>Node id string.</summary>
        public string Id { get { return this.id; } }

        ///<summary>Node elevation(foot).</summary>
        public double Elevation { get; set; }

        ///<summary>Node demand list.</summary>
        public List<Demand> Demand { get { return this.demand; } }

        ///<summary>Water quality source.</summary>
        public Source Source { get; set; }

        ///<summary>Initial species concentrations.</summary>
        public double C0 { get; set; }

        ///<summary>Emitter coefficient.</summary>
        public double Ke { get; set; }

        ///<summary>Node reporting flag.</summary>
        public bool RptFlag { get; set; }

        public override int GetHashCode() { return string.IsNullOrEmpty(this.id) ? 0 : this.id.GetHashCode(); }

        public int CompareTo(Node o) {
            if (o == null) return 1;
            return string.Compare(this.Id, o.Id, StringComparison.OrdinalIgnoreCase);
        }

        /*
        public override bool Equals(object obj) {
            Node o = obj as Node;
            if (o == null) return false;

            return string.Equals(this.Id, o.Id, StringComparison.OrdinalIgnoreCase);
        }
        */

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