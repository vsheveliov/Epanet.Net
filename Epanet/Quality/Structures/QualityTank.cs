/*
 * Copyright (C) 2012  Addition, Lda. (addition at addition dot pt)
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
using org.addition.epanet.network.structures;

namespace org.addition.epanet.quality.structures {

///<summary>Wrapper class for the Tank in the water quality simulation.</summary>
public class QualityTank : QualityNode
{
    ///<summary>Current species concentration [user units].</summary>
    private double concentration;

    ///<summary>Discrete water quality segments assigned to this tank.</summary>
    private readonly LinkedList<QualitySegment> segments;

    ///<summary>Tank current volume [Feet^3].</summary>
    private double volume;

    ///<summary>Initialize tank properties from the original tank node.</summary>
    public QualityTank(Node node):base(node) {
        segments = new LinkedList<QualitySegment>();
        volume = ((Tank)node).getV0();
        concentration =node.getC0()[0];
    }

    ///<summary>Get species concentration.</summary>
    public double getConcentration() {
        return concentration;
    }

    public LinkedList<QualitySegment> getSegments() {
        return segments;
    }

    /**
     * Get tank water volume.
     * @return Water volume [Feet^3].
     */
    public double getVolume() {
        return volume;
    }

    ///<summary>Set species concentrations.</summary>
    public void setConcentration(double value)
    {
        this.concentration = value;
    }

    /**
     * Set water tank volume.
     * @param volume Water volume [Feet^3].
     */
    public void setVolume(double value)
    {
        this.volume = value;
    }
}

}