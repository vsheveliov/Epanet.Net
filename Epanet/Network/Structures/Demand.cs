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

using Epanet.Enums;

namespace Epanet.Network.Structures {

    ///<summary>Node demand category.</summary>
    public class Demand {
        public Demand(double @base, Pattern pattern) {
            this.Base = @base;
            this.Pattern = pattern;
        }

        ///<summary>Baseline demand (Feet^3/t)</summary>
        public double Base;

        ///<summary>Pattern reference.</summary>
        public Pattern Pattern;

#if NUCONVERT
        public double GetBaseNu(FlowUnitsType units) { return NUConvert.revertFlow(units, this.Base); }

        public void SetBaseNu(FlowUnitsType units, double value) { this.Base = NUConvert.convertFlow(units, value); }
#endif

    }

}