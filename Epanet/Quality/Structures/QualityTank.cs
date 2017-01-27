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
using Epanet.Network.Structures;

namespace Epanet.Quality.Structures {

    ///<summary>Wrapper class for the Tank in the water quality simulation.</summary>
    public class QualityTank:QualityNode {
        ///<summary>Discrete water quality segments assigned to this tank.</summary>
        private readonly LinkedList<QualitySegment> _segments;

        ///<summary>Initialize tank properties from the original tank node.</summary>
        public QualityTank(Node node):base(node) {
            _segments = new LinkedList<QualitySegment>();
            Volume = ((Tank)node).V0;
            Concentration = node.C0;
        }

        ///<summary>Get/set species concentration.</summary>
        /// <remarks>Current species concentration [user units].</remarks>
        public double Concentration { get; set; }

        public LinkedList<QualitySegment> Segments { get { return _segments; } }

        ///<summary>Get tank water volume.</summary>
        ///<return>Water volume [Feet^3].</return>
        /// <remarks>Tank current volume [Feet^3].</remarks>
        public double Volume { get; set; }
    }

}