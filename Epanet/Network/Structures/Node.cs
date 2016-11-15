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

namespace Epanet.Network.Structures {

    ///<summary>Hydraulic node structure  (junction)</summary>

    public class Node:IComparable<Node> {
        /// <summary>Type of node.</summary>
        public enum NodeType {
            /// <summary>junction</summary>
            JUNC = 0,
            /// <summary>reservoir</summary>
            RESERV = 1,
            /// <summary>tank</summary>
            TANK = 2
        }

        private readonly string _id;
        private readonly List<Demand> _demand = new List<Demand>();

        [NonSerialized]
        private double initDemand;
        
        public Node(string id) {
            this._id = id;
            this.C0 = new double[1];
            this.Comment = "";
            this.initDemand = 0;
            this.Type = NodeType.JUNC;
            this.Position = EnPoint.Invalid;
        }

        ///<summary>Node comment.</summary>
        public string Comment { get; set; }

        public double InitDemand { get { return this.initDemand; } set { this.initDemand = value; } }

        public NodeType Type { get; set; }

        ///<summary>Node position.</summary>
        public EnPoint Position { get; set; }

        ///<summary>Node id string.</summary>
        public string Id { get { return this._id; } }

        ///<summary>Node elevation(foot).</summary>
        public double Elevation { get; set; }

        ///<summary>Node demand list.</summary>
        public List<Demand> Demand { get { return this._demand; } }

        ///<summary>Water quality source.</summary>
        public Source Source { get; set; }

        ///<summary>Initial species concentrations.</summary>
        public double[] C0 { get; set; }

        ///<summary>Emitter coefficient.</summary>
        public double Ke { get; set; }

        ///<summary>Node reporting flag.</summary>
        public bool RptFlag { get; set; }

        public override int GetHashCode() { return string.IsNullOrEmpty(this._id) ? 0 : this._id.GetHashCode(); }

        public int CompareTo(Node o) {
            if (o == null) return 1;
            return string.Compare(this.Id, o.Id, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) {
            Node o = obj as Node;
            if (o == null) return false;

            return string.Equals(this.Id, o.Id, StringComparison.OrdinalIgnoreCase);
        }

#if DEBUG // NUCONVERT

        public double GetNuElevation(PropertiesMap.UnitsType units) {
            return NUConvert.revertDistance(units, this.Elevation);
        }

        public void SetNuElevation(PropertiesMap.UnitsType units, double elev) {
            this.Elevation = NUConvert.convertDistance(units, elev);
        }

#endif

    }

}