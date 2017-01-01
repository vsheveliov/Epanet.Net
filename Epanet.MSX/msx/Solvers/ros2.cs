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

    public class ros2 {

        ///<summary>Jacobian matrix</summary>
        double[,] A;

        ///<summary>Intermediate solutions</summary>
        double[] K1;

        double[] K2;
        ///<summary>Updated function values</summary>
        double[] Ynew;

        ///<summary>Jacobian column indexes</summary>
        int[] Jindx;

        ///<summary>Max. number of equations</summary>
        int Nmax;

        ///<summary>use adjustable step size</summary>
        int Adjust;



        ///<summary>Opens the ROS2 integrator.</summary>
        public void ros2_open(int n, int adjust) {
            int n1 = n + 1;
            this.Nmax = 0;
            this.Adjust = adjust;

            this.K1 = new double[n1];
            this.K2 = new double[n1];
            this.Jindx = new int[n1];
            this.Ynew = new double[n1];
            this.A = new double[n1,n1];
            this.Nmax = n;
        }

        ///<summary>Integrates a system of ODEs over a specified time interval.</summary>
        public int ros2_integrate(
            double[] y,
            int n,
            double t,
            double tnext,
            double[] htry,
            double[] atol,
            double[] rtol,
            JacobianInterface jInt,
            Operation op) {
            double UROUND = 2.3e-16;
            double g, ghinv, ghinv1, dghinv, ytol;
            double h, hold, hmin, hmax, tplus;
            double ej, err, factor, facmax;
            int nfcn, njac, naccept, nreject;
            int isReject;
            int adjust = this.Adjust;

            // Initialize counters, etc.
            g = 1.0 + 1.0 / Math.Sqrt(2.0);
            ghinv1 = 0.0;
            tplus = t;
            isReject = 0;
            naccept = 0;
            nreject = 0;
            nfcn = 0;
            njac = 0;

            // Initial step size
            hmax = tnext - t;
            hmin = 1e-8;
            h = htry[0];
            if (h == 0.0) {
                jInt.solve(t, y, n, this.K1, 0, op);
                nfcn += 1;
                adjust = 1;
                h = tnext - t;
                for (int i = 1; i <= n; i++) {
                    ytol = atol[i] + rtol[i] * Math.Abs(y[i]);
                    if (this.K1[i] != 0.0) h = Math.Min(h, (ytol / Math.Abs(this.K1[i])));
                }
            }
            h = Math.Max(hmin, h);
            h = Math.Min(hmax, h);

            // Start the time loop
            while (t < tnext) {
                // Check for zero step size

                if (0.10 * Math.Abs(h) <= Math.Abs(t) * UROUND) return -2;

                // Adjust step size if interval exceeded

                tplus = t + h;
                if (tplus > tnext) {
                    h = tnext - t;
                    tplus = tnext;
                }

                // Re-compute the Jacobian if step size accepted

                if (isReject == 0) {
                    Utilities.Jacobian(y, n, this.K1, this.K2, this.A, jInt, op);
                    njac++;
                    nfcn += 2 * n;
                    ghinv1 = 0.0;
                }

                // Update the Jacobian to reflect new step size
                ghinv = -1.0 / (g * h);
                dghinv = ghinv - ghinv1;
                for (int i = 1; i <= n; i++) {
                    this.A[i, i] += dghinv;
                }
                ghinv1 = ghinv;
                if (Utilities.Factorize(this.A, n, this.K1, this.Jindx) == 0) return -1;

                // Stage 1 solution

                jInt.solve(t, y, n, this.K1, 0, op);
                nfcn += 1;
                for (int j = 1; j <= n; j++) this.K1[j] *= ghinv;
                Utilities.Solve(this.A, n, this.Jindx, this.K1);

                // Stage 2 solution

                for (int i = 1; i <= n; i++) {
                    this.Ynew[i] = y[i] + h * this.K1[i];
                }
                jInt.solve(t, this.Ynew, n, this.K2, 0, op);
                nfcn += 1;
                for (int i = 1; i <= n; i++) {
                    this.K2[i] = (this.K2[i] - 2.0 * this.K1[i]) * ghinv;
                }
                Utilities.Solve(this.A, n, this.Jindx, this.K2);

                // Overall solution

                for (int i = 1; i <= n; i++) {
                    this.Ynew[i] = y[i] + 1.5 * h * this.K1[i] + 0.5 * h * this.K2[i];
                }

                // Error estimation
                hold = h;
                err = 0.0;
                if (adjust != 0) {
                    for (int i = 1; i <= n; i++) {
                        ytol = atol[i] + rtol[i] * Math.Abs(this.Ynew[i]);
                        ej = Math.Abs(this.Ynew[i] - y[i] - h * this.K1[i]) / ytol;
                        err = err + ej * ej;
                    }
                    err = Math.Sqrt(err / n);
                    err = Math.Max(UROUND, err);

                    // Choose the step size

                    factor = 0.9 / Math.Sqrt(err);
                    if (isReject != 0) facmax = 1.0;
                    else facmax = 10.0;
                    factor = Math.Min(factor, facmax);
                    factor = Math.Max(factor, 1.0e-1);
                    h = factor * h;
                    h = Math.Min(hmax, h);
                }

                // Reject/accept the step
                if (err > 1.0) {
                    isReject = 1;
                    nreject++;
                    h = 0.5 * h;
                }
                else {
                    isReject = 0;
                    for (int i = 1; i <= n; i++) {
                        y[i] = this.Ynew[i];
                        if (y[i] <= UROUND) y[i] = 0.0;
                    }
                    if (adjust != 0) htry[0] = h;
                    t = tplus;
                    naccept++;
                }

                // End of the time loop

            }
            return nfcn;
        }
    }

}