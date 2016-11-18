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

namespace Epanet.Hydraulic.Structures {

    public class LSVariables {

        ///<summary>Epanet 'X[n]' variable</summary>
        private readonly double[] nodalInflows; 

        ///<summary>Epante Aij[n] variable</summary>
        private readonly double[] matrixOffDiagonal; 

        ///<summary>Epanet Aii[n] variable</summary>
        private readonly double[] matrixDiagonal; 

        ///<summary>Epanet F[n] variable</summary>
        private readonly double[] rightHandSideCoeffs; 
        
        public void Clear() {
            Array.Clear(this.nodalInflows, 0, this.nodalInflows.Length);
            Array.Clear(this.matrixOffDiagonal, 0, this.matrixOffDiagonal.Length);
            Array.Clear(this.matrixDiagonal, 0, this.matrixDiagonal.Length);
            Array.Clear(this.rightHandSideCoeffs, 0, this.rightHandSideCoeffs.Length);
        }

        public LSVariables(int nodes, int coeffs) {
            this.nodalInflows = new double[nodes];
            this.matrixDiagonal = new double[nodes];
            this.matrixOffDiagonal = new double[coeffs];
            this.rightHandSideCoeffs = new double[nodes];
            this.Clear();
        }

        public void AddRhsCoeff(int id, double value) { this.rightHandSideCoeffs[id] += value; }

        public double GetRhsCoeff(int id) { return this.rightHandSideCoeffs[id]; }
        
        public void AddNodalInFlow(int id, double value) { this.nodalInflows[id] += value; }

        public double GetNodalInFlow(int id) { return this.nodalInflows[id]; }
        
        public void AddNodalInFlow(SimulationNode id, double value) { this.nodalInflows[id.Index] += value; }

        public double GetNodalInFlow(SimulationNode id) { return this.nodalInflows[id.Index]; }

        public void AddAii(int id, double value) { this.matrixDiagonal[id] += value; }

        public double GetAii(int id) { return this.matrixDiagonal[id]; }

        public void AddAij(int id, double value) { this.matrixOffDiagonal[id] += value; }

        public double GetAij(int id) { return this.matrixOffDiagonal[id]; }

        public double[] AiiVector { get { return this.matrixDiagonal; } }

        public double[] AijVector { get { return this.matrixOffDiagonal; } }

        public double[] RhsCoeffs { get { return this.rightHandSideCoeffs; } }
    }

}