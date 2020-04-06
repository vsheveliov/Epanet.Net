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

    internal class LsVariables {
        ///<summary>Nodal inflows.</summary>
        ///<remarks>Epanet 'X[n]' variable</remarks>
        public readonly double[] x;

        ///<summary>Matrix off diagonal.</summary>
        ///<remarks>Epante Aij[n] variable</remarks>
        public readonly double[] aij;

        ///<summary>Matrix diagonal.</summary>
        ///<remarks>Epanet Aii[n] variable</remarks>
        public readonly double[] aii;

        ///<summary>Right hand side coeffs.</summary>
        ///<summary>Epanet F[n] variable</summary>
        public readonly double[] f;

        public LsVariables(int nodes, int coeffs) {
            x = new double[nodes];
            aii = new double[nodes];
            aij = new double[coeffs];
            f = new double[nodes];
        }

        public void Clear() {
            Array.Clear(x, 0, x.Length);
            Array.Clear(aij, 0, aij.Length);
            Array.Clear(aii, 0, aii.Length);
            Array.Clear(f, 0, f.Length);
        }
    }

}
