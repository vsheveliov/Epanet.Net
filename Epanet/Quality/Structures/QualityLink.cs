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
using Epanet.Network.Structures;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet.Quality.Structures {

    ///<summary>Wrapper class for the Link in the water quality simulation.</summary>
    public class QualityLink {

        ///<summary>Reference to the first water quality node.</summary>
        private readonly QualityNode _first;

        ///<summary>Reference to the original link.</summary>
        private readonly Link _link;

        ///<summary>Reference to the second water quality node.</summary>
        private readonly QualityNode _second;

        ///<summary>Linked list of discrete water parcels.</summary>
        private readonly LinkedList<QualitySegment> _segments;

        /// <summary>Initialize a new water quality Link wrapper from the original Link.</summary>
        public QualityLink(IList<Node> oNodes, IList<QualityNode> qNodes, Link link) {
            int n1 = oNodes.IndexOf(link.FirstNode);
            int n2 = oNodes.IndexOf(link.SecondNode);
            _first = qNodes[n1];
            _second = qNodes[n2];
            _segments = new LinkedList<QualitySegment>();
            _link = link;
        }

        /// <summary>Get first node reference.</summary>
        /// <value>Reference to the water quality simulation node.</value>
        public QualityNode FirstNode { get { return _first; } }

        /// <summary>Get/set the water flow.</summary>
        ///<remarks>Current water flow[Feet^3/Second].</remarks>
        public double Flow { get; set; }

        /// <summary>Get/set the water flow direction.</summary>
        public bool FlowDir { get; set; }

        /// <summary>Get/set the link flow resistance.</summary>
        /// <value>[Feet/Second]</value>
        public double FlowResistance { get; set; }

        ///<summary>Get the original link.</summary>
        ///<return>Reference to the hydraulic network link.</return>
        public Link Link { get { return _link; } }

        ///<summary>Get the second node reference</summary>
        ///<return>Reference to the water quality simulation node.</return>
        public QualityNode SecondNode { get { return _second; } }

        /// <summary>Get the water quality segments in this link.</summary>
        public LinkedList<QualitySegment> Segments { get { return _segments; } }

        ///<summary>Get the upstream node.</summary>
        public QualityNode UpStreamNode { get { return FlowDir ? _first : _second; } }

        ///<summary>Get the downstream node.</summary>
        public QualityNode DownStreamNode { get { return FlowDir ? _second : _first; } }

        ///<summary>Get link volume.</summary>
        public double LinkVolume {
            get { return 0.785398 * _link.Lenght * (_link.Diameter * _link.Diameter); }
        }

        ///<summary>Get link average quality.</summary>
        public double GetAverageQuality(EpanetNetwork net) {
            double vsum = 0.0,
                   msum = 0.0;

            try {
                if (net != null && net.QualFlag == QualType.NONE)
                    return 0.0;
            }
            catch (ENException) {
                return 0.0;
            }

            foreach (QualitySegment seg  in  Segments) {
                vsum += seg.V;
                msum += seg.C * seg.V;
            }

            return vsum > 0.0 
                ? msum / vsum
                : (FirstNode.Quality + SecondNode.Quality) / 2.0;
        }
    }

}