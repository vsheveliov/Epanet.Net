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

namespace org.addition.epanet.quality.structures {

///<summary>Discrete water quality segment.</summary>
public class QualitySegment {
    ///<summary>Segment concentration.</summary>
    public double  c;

    ///<summary>Segment volume.</summary>
    public double  v;

    public QualitySegment(double v, double c) {
        this.v = v;
        this.c = c;
    }
}

}