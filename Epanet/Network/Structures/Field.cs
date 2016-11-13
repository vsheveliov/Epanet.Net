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

using System;

namespace org.addition.epanet.network.structures {

    ///<summary>Report field properties.</summary>
    public class Field {
        ///<summary>Range limits.</summary>
        public enum RangeType {
            ///<summary>upper limit</summary>
            HI = 1,

            ///<summary>lower limit</summary>
            LOW = 0,

            ///<summary>precision</summary>
            PREC = 2
        }

        ///<summary>Enabled if in table.</summary>
        private bool enabled;

        ///<summary>Name of reported variable.</summary>
        private string name;

        ///<summary>Number of decimal places.</summary>
        private int precision;

        ///<summary>Lower/upper report limits.</summary>
        private double[] rptLim = {0d, 0d, 0d};

        ///<summary>Units of reported variable.</summary>
        private String units;

        /**
     * Init field name, precision, report limit and state.
     * @param name Field name.
     */

        public Field(String name) {
            this.name = name;
            enabled = false;
            precision = 2;
            setRptLim(RangeType.LOW, Constants.BIG*Constants.BIG);
            setRptLim(RangeType.HI, -Constants.BIG*Constants.BIG);
        }

        public String getName() {
            return name;
        }

        public int getPrecision() {
            return precision;
        }

        public double getRptLim(RangeType type) {
            return rptLim[(int)type];
        }

        public String getUnits() {
            return units;
        }

        public bool isEnabled() {
            return this.enabled;
        }

        public void setEnabled(bool enabled) {
            this.enabled = enabled;
        }

        public void setName(String name) {
            this.name = name;
        }

        public void setPrecision(int precision) {
            this.precision = precision;
        }

        public void setRptLim(RangeType type, double rptLim) {
            this.rptLim[(int)type] = rptLim;
        }

        public void setUnits(String units) {
            this.units = units;
        }
    }
}