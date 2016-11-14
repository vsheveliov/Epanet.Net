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
using org.addition.epanet.msx.Solvers;

namespace org.addition.epanet.msx {

    public static class Utilities {

        public static EnumTypes.ErrorCodeType Call(EnumTypes.ErrorCodeType err, EnumTypes.ErrorCodeType f) {
            return err > (EnumTypes.ErrorCodeType)100 ? err : f;
        }

        /// <summary>Finds a match between a string and an array of keyword strings.</summary>
        public static int MSXutils_findmatch(string s, string[] keyword) {
            int i = 0;
            //while (keyword[i] != NULL)
            foreach (string key  in  keyword) {
                if (MSXutils_match(s, key)) return i;
                i++;
            }
            return -1;
        }

        /// <summary>Sees if a sub-string of characters appears in a string.</summary>
        public static bool MSXutils_match(string a, string b) {
            a = a.Trim();
            b = b.Trim();

            // --- fail if substring is empty
            if (b.Length == 0) return false;


            if (a.ToLower().Contains(b.ToLower()))
                return true;


            return false;
        }

        /// <summary>Allocates memory for a 2-dimensional array of doubles.</summary>
        public static double[][] CreateMatrix(int nrows, int ncols) {
            var result = new double[nrows][];
            for (int i = 0; i < nrows; i++) {
                result[i] = new double[ncols];
            }

            return result;
        }

        /// <summary>Performs an LU decomposition of a matrix.</summary>
        public static int Factorize(double[][] a, int n, double[] w, int[] indx) {
            double big, dum, sum, temp;

            for (int i = 1; i <= n; i++) {
                //Loop over rows to get the implicit scaling information.
                big = 0.0;
                for (int j = 1; j <= n; j++)
                    if ((temp = Math.Abs(a[i][j])) > big) big = temp;

                if (big == 0.0)
                    return 0; // Warning for singular matrix
                //No nonzero largest element.
                w[i] = 1.0 / big; //Save the scaling.
            }
            for (int j = 1; j <= n; j++) //for each column
            {
                //This is the loop over columns of Croutís method.

                for (int i = 1; i < j; i++) {
                    //Up from the diagonal
                    sum = a[i][j];
                    for (int k = 1; k < i; k++) sum -= a[i][k] * a[k][j];
                    a[i][j] = sum;
                }
                big = 0.0; //Initialize for the search for largest pivot element.
                int imax = j;
                for (int i = j; i <= n; i++) {
                    sum = a[i][j];
                    for (int k = 1; k < j; k++) sum -= a[i][k] * a[k][j];
                    a[i][j] = sum;
                    if ((dum = w[i] * Math.Abs(sum)) >= big) {
                        big = dum;
                        imax = i;
                    }
                }
                if (j != imax) {
                    //Do we need to interchange rows?
                    for (int i = 1; i <= n; i++) {
                        //Yes,do so...
                        dum = a[imax][i];
                        a[imax][i] = a[j][i];
                        a[j][i] = dum;
                    }
                    w[imax] = w[j]; // interchange the scale factor.
                }
                indx[j] = imax;
                if (a[j][j] == 0.0) a[j][j] = Constants.TINY1;
                if (j != n) // divide by the pivot element.
                {
                    dum = 1.0 / a[j][j];
                    for (int i = j + 1; i <= n; i++) a[i][j] *= dum;
                }
            }
            return 1;
        }

        /// <summary>Solves linear equations AX = B after LU decomposition of A.</summary>
        public static void Solve(double[][] a, int n, int[] indx, double[] b) {
            int ii = 0;
            int j;
            double sum;

            //forward substitution
            for (int i = 1; i <= n; i++) {
                int ip = indx[i];
                sum = b[ip];
                b[ip] = b[i];
                if (ii != 0)
                    for (j = ii; j <= i - 1; j++)
                        sum -= a[i][j] * b[j];
                else if (sum != 0) ii = i;
                b[i] = sum;
            }

            // back substitution
            for (int i = n; i >= 1; i--) {
                sum = b[i];
                for (j = i + 1; j <= n; j++)
                    sum -= a[i][j] * b[j];
                b[i] = sum / a[i][i];
            }
        }


        /// <summary>Computes Jacobian matrix of F(t,X) at given X.</summary>
        public static void Jacobian(
            double[] x,
            int n,
            double[] f,
            double[] w,
            double[][] a,
            JacobianInterface jint,
            JacobianInterface.Operation op) {
            double eps = 1.0e-7;

            for (int j = 1; j <= n; j++) {
                double temp = x[j];
                x[j] = temp + eps;
                jint.solve(0.0, x, n, f, 0, op);

                double eps2;

                if (temp == 0.0) {
                    x[j] = temp;
                    eps2 = eps;
                }
                else {
                    x[j] = temp - eps;
                    eps2 = 2.0 * eps;
                }

                jint.solve(0.0, x, n, w, 0, op);
                for (int i = 1; i <= n; i++) a[i][j] = (f[i] - w[i]) / eps2;
                x[j] = temp;
            }

        }
    }

}