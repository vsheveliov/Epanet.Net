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

    ///<summary>Rule source code class.</summary>
    public class Rule {

        #region enums

        ///<summary>Rule object types.</summary>
        public enum Objects {
            r_JUNC,
            r_LINK,
            r_NODE,
            r_PIPE,
            r_PUMP,
            r_RESERV,
            r_SYSTEM,
            r_TANK,
            r_VALVE
        }

        ///<summary>Rule operators.</summary>
        public enum Operators {
            ABOVE,
            BELOW,
            EQ,
            GE,
            GT,
            IS,
            LE,
            LT,
            NE,
            NOT
        }

        ///<summary>Rule statements.</summary>
        public enum Rulewords {
            r_AND,
            r_ELSE,
            r_ERROR,
            r_IF,
            r_OR,
            r_PRIORITY,
            r_RULE,
            r_THEN
        }

        ///<summary>Rule values types.</summary>
        public enum Values {
            IS_NUMBER = 0,
            IS_OPEN = 1,
            IS_CLOSED = 2,
            IS_ACTIVE = 3

        }

        ///<summary>Rule variables.</summary>
        public enum Varwords {
            r_CLOCKTIME,
            r_DEMAND,
            r_DRAINTIME,
            r_FILLTIME,
            r_FLOW,
            r_GRADE,
            r_HEAD,
            r_LEVEL,
            r_POWER,
            r_PRESSURE,
            r_SETTING,
            r_STATUS,
            r_TIME
        }

        #endregion

        private readonly string _label;
        private readonly List<string> _code = new List<string>();

        public Rule(string label) { this._label = label; }

        public List<string> Code { get { return this._code; } }

        public string Label { get { return this._label; } }
    }

}