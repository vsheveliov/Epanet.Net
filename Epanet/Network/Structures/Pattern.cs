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

namespace org.addition.epanet.network.structures {

    ///<summary>Temporal pattern.</summary>
    public class Pattern {
        ///<summary>Pattern factors list.</summary>
        private readonly List<double> _factors = new List<double>();

        private readonly string _id;
        public Pattern(string id) { this._id = id; }

        public void Add(double factor) { this._factors.Add(factor); }

        public List<double> FactorsList { get { return this._factors; } }

        ///<summary>Pattern name.</summary>
        public string Id { get { return this._id; } }

        public int Length { get { return this._factors.Count; } }
    }

}
