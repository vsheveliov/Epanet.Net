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

using Epanet.Enums;
using Epanet.Network.Structures;

namespace Epanet.Quality.Structures {

    ///<summary>Wrapper class for the Node in the water quality simulation.</summary>
    public class QualityNode {

        ///<summary>Factory method to instantiate the quality node from the hydraulic network node.</summary>
        public static QualityNode Create(Node node) {
            return node.Type > NodeType.JUNC ? new QualityTank(node) : new QualityNode(node);

            /*
            return node.Type == NodeType.TANK || node.Type == NodeType.RESERV 
                ? new QualityTank(node) 
                : new QualityNode(node);
             */
        }

        ///<summary>Hydraulic network node reference.</summary>
        private readonly Node _node;

        ///<summary>Init quality node properties.</summary>
        protected QualityNode(Node node) {
            _node = node;
            Quality = node.C0;
            if (_node.QualSource != null)
                MassRate = 0.0;
        }

        ///<summary>Node demand [Feet^3/Second]</summary>
        public double Demand { get; set; }

        ///<summary>Total mass inflow to node.</summary>
        public double MassIn { get; set; }

        public double MassRate { get; set; }

        ///<summary>Get the original hydraulic network node.</summary>
        public Node Node { get { return _node; } }

        ///<summary>Species concentration [user units].</summary>
        public double Quality { get; set; }

        public double SourceContribution { get; set; }

        ///<summary>Total volume inflow to node.</summary>
        public double VolumeIn { get; set; }
    }

}