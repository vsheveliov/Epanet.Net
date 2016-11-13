/*
 * Copyright (C) 2012  Addition, Lda. (addition at addition dot pt)
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

namespace org.addition.epanet.msx.Structures {

// Chemical species object
    public class Species {
        string id; // name
        string units; // mass units code [MAXUNITS]
        double aTol; // absolute tolerance
        double rTol; // relative tolerance
        EnumTypes.SpeciesType type; // BULK or WALL
        EnumTypes.ExpressionType pipeExprType; // type of pipe chemistry
        EnumTypes.ExpressionType tankExprType; // type of tank chemistry
        int precision; // reporting precision
        byte rpt; // reporting flag
        MathExpr pipeExpr; // pipe chemistry expression
        MathExpr tankExpr; // tank chemistry expression

        public Species() {
            id = "";
            units = "";
            pipeExpr = null;
            tankExpr = null;
            pipeExprType = EnumTypes.ExpressionType.NO_EXPR;
            tankExprType = EnumTypes.ExpressionType.NO_EXPR;
            precision = 2;
            rpt = 0;
        }

        public string getId() { return id; }

        public void setId(string value) { this.id = value; }

        public string getUnits() { return units; }

        public void setUnits(string value) {
            this.units = value;
            if (this.units.Length > Constants.MAXUNITS)
                this.units = this.units.Substring(0, Constants.MAXUNITS);
        }

        public double getaTol() { return aTol; }

        public void setaTol(double value) { this.aTol = value; }

        public double getrTol() { return rTol; }

        public void setrTol(double value) { this.rTol = value; }

        public EnumTypes.SpeciesType getType() { return type; }

        public void setType(EnumTypes.SpeciesType value) { this.type = value; }

        public EnumTypes.ExpressionType getPipeExprType() { return pipeExprType; }

        public void setPipeExprType(EnumTypes.ExpressionType value) { this.pipeExprType = value; }

        public EnumTypes.ExpressionType getTankExprType() { return tankExprType; }

        public void setTankExprType(EnumTypes.ExpressionType value) { this.tankExprType = value; }

        public int getPrecision() { return precision; }

        public void setPrecision(int value) { this.precision = value; }

        public byte getRpt() { return rpt; }

        public void setRpt(byte value) { this.rpt = value; }

        public MathExpr getPipeExpr() { return pipeExpr; }

        public void setPipeExpr(MathExpr value) { this.pipeExpr = value; }

        public MathExpr getTankExpr() { return tankExpr; }

        public void setTankExpr(MathExpr value) { this.tankExpr = value; }
    }

}