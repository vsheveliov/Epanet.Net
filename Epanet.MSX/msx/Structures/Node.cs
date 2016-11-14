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

namespace org.addition.epanet.msx.Structures {

// Node object
    public class Node {
        ///<summary>ptr. to WQ source list</summary>
        private readonly List<Source> sources;

        public Node(int species) {
            sources = new List<Source>();
            this.Tank = 0;
            this.Rpt = false;
            this.C = new double[species];
            this.C0 = new double[species];
        }

        ///<summary>ptr. to WQ source list</summary>
        public List<Source> Sources { get { return this.sources; } }

        ///<summary>current species concentrations</summary>
        public double[] C { get; set; }

        ///<summary>initial species concentrations</summary>
        public double[] C0 { get; set; }

        ///<summary>tank index</summary>
        public int Tank { get; set; }

        ///<summary>reporting flag</summary>
        public bool Rpt { get; set; }
    }

}