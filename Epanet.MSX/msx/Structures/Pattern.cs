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

namespace Epanet.MSX.Structures {

    /// <summary>Time Pattern object.</summary>
    public class Pattern {
        private readonly List<double> multipliers;
        public int Length { get { return this.Multipliers.Count; } }

        ///<summary>pattern ID</summary>
        public string Id { set; get; }

        ///<summary>list of multipliers</summary>
        public List<double> Multipliers { get { return this.multipliers; } }

        ///<summary>current time interval</summary>
        public long Interval { get; set; }

        ///<summary>current multiplier</summary>
        public int Current { get; set; }

        public Pattern() {
            this.multipliers = new List<double>();
            this.Id = "";
            this.Current = 0;
            this.Interval = 0;
        }
    }

}