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

namespace Epanet.MSX.Structures {

    /// <summary>Chemical species object.</summary>
    public class Species {

        private string units;

        public Species() {
            this.Id = "";
            this.units = "";
            this.PipeExpr = null;
            this.TankExpr = null;
            this.PipeExprType = ExpressionType.NO_EXPR;
            this.TankExprType = ExpressionType.NO_EXPR;
            this.Precision = 2;
            this.Rpt = 0;
        }

        ///<summary>name</summary>
        public string Id { get; set; }

        ///<summary>mass units code [MAXUNITS]</summary>
        public string Units {
            get { return this.units; }
            set {
                this.units = value;
                if (this.units.Length > Constants.MAXUNITS)
                    this.units = this.units.Substring(0, Constants.MAXUNITS);
            }
        }

        ///<summary>absolute tolerance</summary>
        public double ATol { get; set; }

        ///<summary>relative tolerance</summary>
        public double RTol { get; set; }

        ///<summary>BULK or WALL</summary>
        public SpeciesType Type { get; set; }

        ///<summary>type of pipe chemistry</summary>
        public ExpressionType PipeExprType { get; set; }

        ///<summary>type of tank chemistry</summary>
        public ExpressionType TankExprType { get; set; }

        ///<summary>reporting precision</summary>
        public int Precision { get; set; }

        ///<summary>reporting flag</summary>
        public byte Rpt { get; set; }

        ///<summary>pipe chemistry expression</summary>
        public MathExpr PipeExpr { get; set; }

        ///<summary>tank chemistry expression</summary>
        public MathExpr TankExpr { get; set; }
    }

}