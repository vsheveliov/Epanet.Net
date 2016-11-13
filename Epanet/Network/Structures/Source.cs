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
        public enum Type {
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
        private double C0;

        ///<summary>Time pattern reference.</summary>
        private Pattern pattern;

        ///<summary>Source type.</summary>
        private Type type;

        public double getC0() {
            return C0;
        }

        public Pattern getPattern() {
            return pattern;
        }

        public Type getType() {
            return type;
        }

        public void setC0(double c0) {
            C0 = c0;
        }


        public void setPattern(Pattern pattern) {
            this.pattern = pattern;
        }

        public void setType(Type type) {
            this.type = type;
        }
    }
}