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

namespace org.addition.epanet.hydraulic.structures {

    public class LSVariables {
        private readonly double[] nodalInflows; // Epanet 'X[n]' variable
        private readonly double[] matrixOffDiagonal; // Epante Aij[n] variable
        private readonly double[] matrixDiagonal; // Epanet Aii[n] variable
        private readonly double[] rightHandSideCoeffs; // Epanet F[n] variable

        public void clear() {
            Array.Clear(nodalInflows, 0, nodalInflows.Length);
            Array.Clear(matrixOffDiagonal, 0, this.matrixOffDiagonal.Length);
            Array.Clear(matrixDiagonal, 0, this.matrixDiagonal.Length);
            Array.Clear(rightHandSideCoeffs, 0, this.rightHandSideCoeffs.Length);
        }

        public LSVariables(int nodes, int coeffs) {
            nodalInflows = new double[nodes];
            matrixDiagonal = new double[nodes];
            matrixOffDiagonal = new double[coeffs];
            rightHandSideCoeffs = new double[nodes];
            clear();
        }

        public void addRHSCoeff(int id, double value) { rightHandSideCoeffs[id] += value; }

        public double getRHSCoeff(int id) { return rightHandSideCoeffs[id]; }


        public void addNodalInFlow(int id, double value) { nodalInflows[id] += value; }

        public double getNodalInFlow(int id) { return nodalInflows[id]; }


        public void addNodalInFlow(SimulationNode id, double value) { nodalInflows[id.Index] += value; }

        public double getNodalInFlow(SimulationNode id) { return nodalInflows[id.Index]; }


        public void addAii(int id, double value) { matrixDiagonal[id] += value; }

        public double getAii(int id) { return matrixDiagonal[id]; }

        public void addAij(int id, double value) { matrixOffDiagonal[id] += value; }

        public double getAij(int id) { return matrixOffDiagonal[id]; }

        public double[] getAiiVector() { return matrixDiagonal; }

        public double[] getAijVector() { return matrixOffDiagonal; }

        public double[] getRHSCoeffs() { return rightHandSideCoeffs; }
    }

}