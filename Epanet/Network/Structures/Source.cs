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

namespace org.addition.epanet.network.structures {

    ///<summary>Water quality object, source quality.</summary>
    public class Source {

        ///<summary>Source type</summary>
        public enum SourceType {
            ///<summary>Inflow concentration.</summary>
            CONCEN = 0,

            ///<summary>Flow paced booster.</summary>
            FLOWPACED = 3,

            ///<summary>Mass inflow booster.</summary>
            MASS = 1,

            ///<summary>Setpoint booster.</summary>
            SETPOINT = 2
        }

        ///<summary>Base concentration.</summary>
        public double C0 { get; set; }

        ///<summary>Time pattern reference.</summary>
        public Pattern Pattern { get; set; }

        ///<summary>Source type.</summary>
        public SourceType Type { get; set; }
    }

}