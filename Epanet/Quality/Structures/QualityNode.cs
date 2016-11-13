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

using org.addition.epanet.network.structures;

namespace org.addition.epanet.quality.structures {

///<summary>Wrapper class for the Node in the water quality simulation.</summary>
public class QualityNode {

    ///<summary>Factory method to instantiate the quality node from the hydraulic network node.</summary>
    public static QualityNode create(Node node) {
        return node is Tank ? new QualityTank(node) : new QualityNode(node);
    }

    ///<summary>Node demand [Feet^3/Second]</summary>
    private double  demand;

    ///<summary>Total mass inflow to node.</summary>
    private double  massIn;

    /**
     *
     */
    private double  massRate;

    ///<summary>Hydraulic network node reference.</summary>
    private readonly Node    node;

    ///<summary>Species concentration [user units].</summary>
    private double  quality;

    /**
     *
     */
    private double  sourceContribution;

    ///<summary>Total volume inflow to node.</summary>
    private double  volumeIn;

    ///<summary>Init quality node properties.</summary>
    protected QualityNode(Node node) {
        this.node = node;
        quality = node.getC0()[0];
        if(this.node.getSource()!=null)
            massRate = 0.0;
    }

    public double getDemand() {
        return demand;
    }

    public double getMassIn() {
        return massIn;
    }

    public double getMassRate() {
        return massRate;
    }


    ///<summary>Get the original hydraulic network node.</summary>
    public Node getNode() {
        return node;
    }

    public double getQuality() {
        return quality;
    }

    public double getSourceContribution() {
        return sourceContribution;
    }

    public double getVolumeIn() {
        return volumeIn;
    }

    public void setDemand(double value)
    {
        this.demand = value;
    }

    public void setMassIn(double value)
    {
        this.massIn = value;
    }

    public void setMassRate(double value)
    {
        this.massRate = value;
    }

    public void setQuality(double value)
    {
        this.quality = value;
    }

    public void setSourceContribution(double sourceConcentration) {
        this.sourceContribution = sourceConcentration;
    }

    public void setVolumeIn(double value)
    {
        this.volumeIn = value;
    }
}
}