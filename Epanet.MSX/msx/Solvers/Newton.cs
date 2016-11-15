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

namespace Epanet.MSX.Solvers {

    public class Newton {

        ///<summary>max. number of equations</summary>
        int Nmax;

        ///<summary>permutation vector of row indexes</summary>
        int[] Indx;

        ///<summary>function & adjustment vector</summary>
        double[] F;

        ///<summary>work vector</summary>
        double[] W;

        ///<summary>Jacobian matrix</summary>
        double[][] J;


        ///<summary>opens the algebraic solver to handle a system of n equations.</summary>
        public void newton_open(int n) {
            this.Nmax = 0;
            this.Indx = null;
            this.F = null;
            this.W = null;
            this.Indx = new int[n + 1];
            this.F = new double[n + 1];
            this.W = new double[n + 1];
            this.J = Utilities.CreateMatrix(n + 1, n + 1);
            this.Nmax = n;
        }

        ///<summary>uses newton-raphson iterations to solve n nonlinear eqns.</summary>
        public int newton_solve(
            double[] x,
            int n,
            int maxit,
            int numsig,
            JacobianInterface jint,
            JacobianInterface.Operation op) {
            double errx, errmax, cscal, relconvg = Math.Pow(10.0, -numsig);

            // check that system was sized adequetely

            if (n > this.Nmax) return -3;

            // use up to maxit iterations to find a solution

            for (int i = 1; i <= maxit; i++) {
                // evaluate the Jacobian matrix

                Utilities.Jacobian(x, n, this.F, this.W, this.J, jint, op);

                // factorize the Jacobian

                if (Utilities.Factorize(this.J, n, this.W, this.Indx) == 0) return -1;

                // solve for the updates to x (returned in F)

                for (int j = 1; j <= n; j++) this.F[j] = -this.F[j];
                Utilities.Solve(this.J, n, this.Indx, this.F);

                // update solution x & check for convergence

                errmax = 0.0;
                for (int j = 1; j <= n; j++) {
                    cscal = x[j];
                    if (cscal < relconvg) cscal = relconvg;
                    x[j] += this.F[j];
                    errx = Math.Abs(this.F[j] / cscal);
                    if (errx > errmax) errmax = errx;
                }
                if (errmax <= relconvg) return i;
            }

            // return error code if no convergence

            return -2;
        }
    }

}