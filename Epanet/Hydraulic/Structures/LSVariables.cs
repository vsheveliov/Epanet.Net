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

    public class LsVariables {

        ///<summary>Epanet 'X[n]' variable</summary>
        private readonly double[] _nodalInflows; 

        ///<summary>Epante Aij[n] variable</summary>
        private readonly double[] _matrixOffDiagonal; 

        ///<summary>Epanet Aii[n] variable</summary>
        private readonly double[] _matrixDiagonal; 

        ///<summary>Epanet F[n] variable</summary>
        private readonly double[] _rightHandSideCoeffs; 
        
        public void Clear() {
            Array.Clear(_nodalInflows, 0, _nodalInflows.Length);
            Array.Clear(_matrixOffDiagonal, 0, _matrixOffDiagonal.Length);
            Array.Clear(_matrixDiagonal, 0, _matrixDiagonal.Length);
            Array.Clear(_rightHandSideCoeffs, 0, _rightHandSideCoeffs.Length);
        }

        public LsVariables(int nodes, int coeffs) {
            _nodalInflows = new double[nodes];
            _matrixDiagonal = new double[nodes];
            _matrixOffDiagonal = new double[coeffs];
            _rightHandSideCoeffs = new double[nodes];
            Clear();
        }

        public void AddRhsCoeff(int id, double value) { _rightHandSideCoeffs[id] += value; }

        public double GetRhsCoeff(int id) { return _rightHandSideCoeffs[id]; }
        
        public void AddNodalInFlow(int id, double value) { _nodalInflows[id] += value; }

        public double GetNodalInFlow(int id) { return _nodalInflows[id]; }
        
        public void AddNodalInFlow(SimulationNode id, double value) { _nodalInflows[id.Index] += value; }

        public double GetNodalInFlow(SimulationNode id) { return _nodalInflows[id.Index]; }

        public void AddAii(int id, double value) { _matrixDiagonal[id] += value; }

        public double GetAii(int id) { return _matrixDiagonal[id]; }

        public void AddAij(int id, double value) { _matrixOffDiagonal[id] += value; }

        public double GetAij(int id) { return _matrixOffDiagonal[id]; }

        public double[] AiiVector { get { return _matrixDiagonal; } }

        public double[] AijVector { get { return _matrixOffDiagonal; } }

        public double[] RhsCoeffs { get { return _rightHandSideCoeffs; } }
    }

}