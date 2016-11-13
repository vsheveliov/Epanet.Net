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
using org.addition.epanet.network;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.quality.structures {

///<summary>Wrapper class for the Link in the water quality simulation.</summary>
public class QualityLink {

    ///<summary>Reference to the first water quality node.</summary>
    private readonly QualityNode first;

    ///<summary>Current water flow[Feet^3/Second].</summary>
    private double  flow;

    ///<summary>Current flow direction.</summary>
    private bool flowDir;

    ///<summary>Current flow resistance[Feet/Second].</summary>
    private double  flowResistance;

    ///<summary>Reference to the original link.</summary>
    private readonly Link link;

    ///<summary>Reference to the second water quality node.</summary>
    private readonly QualityNode second;

    ///<summary>Linked list of discrete water parcels.</summary>
    private readonly LinkedList<QualitySegment> segments;

    /**
     * Initialize a new water quality Link wrapper from the original Link.
     * @param oNodes
     * @param qNodes
     * @param link
     */
    public QualityLink(List<Node> oNodes,List<QualityNode> qNodes, Link link)
    {
        int n1 = oNodes.IndexOf(link.getFirst());
        int n2 = oNodes.IndexOf(link.getSecond());
        first = qNodes[n1];
        second = qNodes[n2];
        segments = new LinkedList<QualitySegment>();
        this.link = link;
    }

    /**
     * Get first node reference.
     * @return Reference to the water quality simulation node.
     */
    public QualityNode getFirst() {
        return first;
    }

    /**
     * Get the water flow.
     * @return
     */
    public double getFlow() {
        return flow;
    }

    /**
     * Get the water flow direction.
     * @return
     */
    public bool getFlowDir() {
        return flowDir;
    }

    ///**
    // * Current reaction rate.
    // */
    //double  reactionRate;    // Pipe reaction rate
    //
    //public double getReactionRate() {
    //    return reactionRate;
    //}
    //
    //public void setReactionRate(double reactionRate) {
    //    this.reactionRate = reactionRate;
    //}

    /**
     * Get the link flow resistance.
     * @return [Feet/Second]
     */
    public double getFlowResistance() {
        return flowResistance;
    }

    /**
     * Get the original link.
     * @return Reference to the hydraulic network link.
     */
    public Link getLink() {
        return link;
    }

    /**
     * Get the second node reference
     * @return Reference to the water quality simulation node.
     */
    public QualityNode getSecond() {
        return second;
    }

    /**
     * Get the water quality segments in this link.
     * @return
     */
    public LinkedList<QualitySegment> getSegments() {
        return segments;
    }

    /**
     * Set the water flow.
     * @param hydFlow
     */
    public void setFlow(double hydFlow) {
        this.flow = hydFlow;
    }

    /**
     * Set the water flow direction.
     * @param flowDir
     */
    public void setFlowDir(bool value) {
        this.flowDir = value;
    }

    /**
     * Set the link flow resistance.
     * @param kw [Feet/Second]
     */
    public void setFlowResistance(double kw) {
        flowResistance = kw;
    }

    /**
     * Get the upstream node.
     * @return
     */
    public QualityNode getUpStreamNode(){
        return ((flowDir) ? first : second);
    }

    /**
     * Get the downstream node.
     * @return
     */
    public QualityNode getDownStreamNode(){
        return ((flowDir) ? second : first);
    }

    /**
     * Get link volume.
     * @return
     */
    public double getLinkVolume(){
        return ( 0.785398*link.getLenght()*(link.getDiameter()*link.getDiameter()) );
    }

    /**
     * Get link average quality.
     * @param pMap
     * @return
     */
    public double getAverageQuality(PropertiesMap pMap) {
         double vsum = 0.0,
                msum = 0.0;

        try {
            if (pMap != null && pMap.getQualflag() == PropertiesMap.QualType.NONE)
                return 0.0;
        } catch (ENException) {
            return 0.0;
        }

        foreach (QualitySegment seg  in  getSegments()) {
            vsum += seg.v;
            msum += (seg.c) * (seg.v);
        }

        if (vsum > 0.0)
            return (msum / vsum);
        else
            return ((getFirst().getQuality() + getSecond().getQuality()) / 2.0);
    }
}
}