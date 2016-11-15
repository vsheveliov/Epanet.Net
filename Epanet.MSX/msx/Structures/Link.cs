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

namespace Epanet.MSX.Structures {

    /// <summary>Link  Object.</summary>
    public class Link {
        public Link(int species, int parameter) {
            this.C0 = new double[species];
            this.Param = new double[parameter];
            this.Rpt = false;
        }

        ///<summary>start node index</summary>
        public int N1 { get; set; }

        ///<summary>end node index</summary>
        public int N2 { get; set; }

        ///<summary>diameter</summary>
        public double Diam { get; set; }

        ///<summary>length</summary>
        public double Len { get; set; }

        ///<summary>reporting flag</summary>
        public bool Rpt { get; set; }

        ///<summary>initial species concentrations</summary>
        public double[] C0 { get; set; }

        ///<summary>kinetic parameter values</summary>
        public double[] Param { get; set; }

        ///<summary>roughness</summary>
        public double Roughness { get; set; }
    }

}