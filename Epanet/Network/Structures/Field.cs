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
        private readonly double[] rptLim = {0d, 0d, 0d};

        ///<summary>Init field name, precision, report limit and state.</summary>
        /// <param name="name">Field name.</param>
        public Field(string name) {
            this.Name = name;
            this.Enabled = false;
            this.Precision = 2;
            this.SetRptLim(RangeType.LOW, Constants.BIG * Constants.BIG);
            this.SetRptLim(RangeType.HI, -Constants.BIG * Constants.BIG);
        }

        ///<summary>Name of reported variable.</summary>
        public string Name { get; private set; }

        ///<summary>Number of decimal places.</summary>
        public int Precision { get; set; }

        public double GetRptLim(RangeType type) { return this.rptLim[(int)type]; }

        ///<summary>Units of reported variable.</summary>
        public string Units { get; set; }

        ///<summary>Enabled if in table.</summary>
        public bool Enabled { get; set; }

        public void SetRptLim(RangeType type, double value) { this.rptLim[(int)type] = value; }
    }

}