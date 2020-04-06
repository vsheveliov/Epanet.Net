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

    ///<summary>Report field properties.</summary>
    public class Field {
        ///<summary>Lower/upper report limits.</summary>
        private readonly double[] _rptLim = {0d, 0d, 0d};

        ///<summary>Init field name, precision, report limit and state.</summary>
        /// <param name="name">Field name.</param>
        public Field(FieldType type) {
            Type = type;
            Enabled = false;
            Precision = 2;
            SetRptLim(RangeType.LOW, Constants.BIG * Constants.BIG);
            SetRptLim(RangeType.HI, -Constants.BIG * Constants.BIG);
        }


        public FieldType Type { get; }
        ///<summary>Name of reported variable.</summary>
        public string Name => Type.ParseStr();

        ///<summary>Number of decimal places.</summary>
        public int Precision { get; set; }
        
        ///<summary>Units of reported variable.</summary>
        public string Units { get; set; }

        ///<summary>Enabled if in table.</summary>
        public bool Enabled { get; set; }

        public void SetRptLim(RangeType type, double value) { _rptLim[(int)type] = value; }
        public double GetRptLim(RangeType type) { return _rptLim[(int)type]; }
    }

}