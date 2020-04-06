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
        /// <summary>Initialize a new water quality Link wrapper from the original Link.</summary>
        public QualityLink(IList<Node> oNodes, IList<QualityNode> qNodes, Link link) {
            FirstNode = qNodes[oNodes.IndexOf(link.FirstNode)];
            SecondNode = qNodes[oNodes.IndexOf(link.SecondNode)];
            Segments = new LinkedList<QualitySegment>();
            Link = link;
        }

        /// <summary>Get first node reference.</summary>
        /// <value>Reference to the water quality simulation node.</value>
        public QualityNode FirstNode { get; }

        /// <summary>Get/set the water flow.</summary>
        ///<remarks>Current water flow[Feet^3/Second].</remarks>
        public double Flow { get; set; }

        /// <summary>Get/set the water flow direction.</summary>
        public bool FlowDir { get; set; }

        /// <summary>Get/set the link flow resistance.</summary>
        /// <value>[Feet/Second]</value>
        public double FlowResistance { get; set; }

        ///<summary>Get the original link.</summary>
        ///<return>Reference to the original hydraulic network link.</return>
        public Link Link { get; }

        ///<summary>Get the second node reference</summary>
        ///<return>Reference to the water quality simulation node.</return>
        public QualityNode SecondNode { get; }

        ///<summary>Linked list of discrete water parcels.</summary>
        /// <remarks>Get the water quality segments in this link.</remarks>
        public LinkedList<QualitySegment> Segments { get; }

        ///<summary>Get the upstream node.</summary>
        public QualityNode UpStreamNode => FlowDir ? FirstNode : SecondNode;

        ///<summary>Get the downstream node.</summary>
        public QualityNode DownStreamNode => FlowDir ? SecondNode : FirstNode;

        ///<summary>Get link volume.</summary>
        public double LinkVolume => 0.785398 * Link.Lenght * (Link.Diameter * Link.Diameter);

        ///<summary>Get link average quality.</summary>
        public double GetAverageQuality(EpanetNetwork net = null) {
            double vsum = 0.0,
                   msum = 0.0;

            try {
                if (net != null && net.QualFlag == QualType.NONE)
                    return 0.0;
            }
            catch (EnException) {
                return 0.0;
            }

            foreach (QualitySegment seg  in  Segments) {
                vsum += seg.Vol;
                msum += seg.Conc * seg.Vol;
            }

            return vsum > 0.0 
                ? msum / vsum
                : (FirstNode.Quality + SecondNode.Quality) / 2.0;
        }
    }

}