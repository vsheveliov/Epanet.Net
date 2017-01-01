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

namespace Epanet.Network.Structures {

    ///<summary>Temporal pattern.</summary>
    public class Pattern:IStringKeyed {
        ///<summary>Pattern factors list.</summary>
        private readonly List<double> factors = new List<double>();

        private readonly string id;
        public Pattern(string id) { this.id = id; }

        public void Add(double factor) { this.factors.Add(factor); }

        public List<double> FactorsList { get { return this.factors; } }

        ///<summary>Pattern name.</summary>
        public string Id { get { return this.id; } }

        public int Length { get { return this.factors.Count; } }
    }

}
