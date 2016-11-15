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

    public class rk5 {

        ///<summary>max. number of equations</summary>
        int Nmax;

        ///<summary>max. number of integration steps</summary>
        int Itmax;

        ///<summary>use adjustable step size</summary>
        int Adjust;

        ///<summary>work arrays</summary>
        double[] Ak;

        double[] K1;

        int K2off;
        int K3off;
        int K4off;
        int K5off;
        int K6off;

        ///<summary>updated solution</summary>

        double[] Ynew;



        ///<summary>Opens the RK5 solver to solve system of n equations</summary>
        public void rk5_open(int n, int itmax, int adjust) {
            int n1 = n + 1;
            this.Nmax = 0;
            this.Itmax = itmax;
            this.Adjust = adjust;
            this.Ynew = new double[n1];
            this.Ak = new double[n1 * 6];
            this.Nmax = n;
            this.K1 = (this.Ak);
            this.K2off = (n1);
            this.K3off = (2 * n1);
            this.K4off = (3 * n1);
            this.K5off = (4 * n1);
            this.K6off = (5 * n1);
        }

        ///<summary>Integrates system of equations dY/dt = F(t,Y) over a given interval.</summary>
        public int rk5_integrate(
            double[] y,
            int n,
            double t,
            double tnext,
            double[] htry,
            double[] atol,
            double[] rtol,
            JacobianInterface jInt,
            JacobianInterface.Operation op) {
            double c2 = 0.20, c3 = 0.30, c4 = 0.80, c5 = 8.0 / 9.0;
            double a21 = 0.20,
                   a31 = 3.0 / 40.0,
                   a32 = 9.0 / 40.0,
                   a41 = 44.0 / 45.0,
                   a42 = -56.0 / 15.0,
                   a43 = 32.0 / 9.0,
                   a51 = 19372.0 / 6561.0,
                   a52 = -25360.0 / 2187.0,
                   a53 = 64448.0 / 6561.0,
                   a54 = -212.0 / 729.0,
                   a61 = 9017.0 / 3168.0,
                   a62 = -355.0 / 33.0,
                   a63 = 46732.0 / 5247.0,
                   a64 = 49.0 / 176.0,
                   a65 = -5103.0 / 18656.0,
                   a71 = 35.0 / 384.0,
                   a73 = 500.0 / 1113.0,
                   a74 = 125.0 / 192.0,
                   a75 = -2187.0 / 6784.0,
                   a76 = 11.0 / 84.0;
            double e1 = 71.0 / 57600.0,
                   e3 = -71.0 / 16695.0,
                   e4 = 71.0 / 1920.0,
                   e5 = -17253.0 / 339200.0,
                   e6 = 22.0 / 525.0,
                   e7 = -1.0 / 40.0;

            double tnew, h, hmax, hnew, ytol, err, sk, fac, fac11 = 1.0;
            
            // parameters for step size control
            double UROUND = 2.3e-16;
            double SAFE = 0.90;
            double fac1 = 0.2;
            double fac2 = 10.0;
            double beta = 0.04;
            double facold = 1e-4;
            double expo1 = 0.2 - beta * 0.75;
            double facc1 = 1.0 / fac1;
            double facc2 = 1.0 / fac2;

            // various counters
            int nstep = 1;
            int nfcn = 0;
            int naccpt = 0;
            int nrejct = 0;
            int reject = 0;
            int adjust = this.Adjust;

            // initial function evaluation
            jInt.solve(t, y, n, this.K1, 0, op);
            nfcn++;

            // initial step size
            h = htry[0];
            hmax = tnext - t;
            if (h == 0.0) {
                adjust = 1;
                h = tnext - t;
                for (int i = 1; i <= n; i++) {
                    ytol = atol[i] + rtol[i] * Math.Abs(y[i]);
                    if (this.K1[i] != 0.0) h = Math.Min(h, (ytol / Math.Abs(this.K1[i])));
                }
            }
            h = Math.Max(1e-8, h);

            // while not at end of time interval
            while (t < tnext) {
                // --- check for zero step size
                if (0.10 * Math.Abs(h) <= Math.Abs(t) * UROUND) return -2;

                // --- adjust step size if interval exceeded
                if ((t + 1.01 * h - tnext) > 0.0) h = tnext - t;

                tnew = t + c2 * h;
                for (int i = 1; i <= n; i++)
                    this.Ynew[i] = y[i] + h * a21 * this.K1[i];
                jInt.solve(tnew, this.Ynew, n, this.K1, this.K2off, op);

                tnew = t + c3 * h;
                for (int i = 1; i <= n; i++)
                    this.Ynew[i] = y[i] + h * (a31 * this.K1[i] + a32 * this.K1[this.K2off + i]);
                        // y[i] + h*(a31*K1[i] + a32*K2[i]
                jInt.solve(tnew, this.Ynew, n, this.K1, this.K3off, op);

                tnew = t + c4 * h;
                for (int i = 1; i <= n; i++)
                    this.Ynew[i] = y[i]
                                   + h
                                   * (a41 * this.K1[i] + a42 * this.K1[this.K2off + i] + a43 * this.K1[this.K3off + i]);
                        //a42*K2[i] + a43*K3[i]);
                jInt.solve(tnew, this.Ynew, n, this.K1, this.K4off, op);

                tnew = t + c5 * h;
                for (int i = 1; i <= n; i++)
                    this.Ynew[i] = y[i]
                                   + h
                                   * (a51 * this.K1[i] + a52 * this.K1[this.K2off + i] + a53 * this.K1[this.K3off + i]
                                      + a54 * this.K1[this.K4off + i]); //a52*K2[i] + a53*K3[i]+a54*K4[i]);
                jInt.solve(tnew, this.Ynew, n, this.K1, this.K5off, op);

                tnew = t + h;
                for (int i = 1; i <= n; i++)
                    this.Ynew[i] = y[i] + h * (a61 * this.K1[i] + a62 * this.K1[i + this.K2off] +
                                               a63 * this.K1[i + this.K3off] + a64 * this.K1[i + this.K4off]
                                               + a65 * this.K1[i + this.K5off]);
                //Ynew[i] = y[i] + h*(a61*K1[i] + a62*K2[i] +
                //        a63*K3[i] + a64*K4[i] + a65*K5[i]);
                jInt.solve(tnew, this.Ynew, n, this.K1, this.K6off, op);

                for (int i = 1; i <= n; i++)
                    this.Ynew[i] = y[i] + h * (a71 * this.K1[i] + a73 * this.K1[i + this.K3off] +
                                               a74 * this.K1[i + this.K4off] + a75 * this.K1[i + this.K5off]
                                               + a76 * this.K1[i + this.K6off]);

                // Ynew[i] = y[i] + h*(a71*K1[i] + a73*K3[i] +
                //        a74*K4[i] + a75*K5[i] + a76*K6[i]);
                jInt.solve(tnew, this.Ynew, n, this.K1, this.K2off, op);
                nfcn += 6;

                // step size adjustment

                err = 0.0;
                hnew = h;
                if (adjust != 0) {
                    for (int i = 1; i <= n; i++)
                        this.K1[i + this.K4off] = (e1 * this.K1[i] + e3 * this.K1[i + this.K3off]
                                                   + e4 * this.K1[i + this.K4off] + e5 * this.K1[i + this.K5off] +
                                                   e6 * this.K1[i + this.K6off] + e7 * this.K1[i + this.K2off]) * h;
                    //K4[i] = (e1*K1[i] + e3*K3[i] + e4*K4[i] + e5*K5[i] +
                    //        e6*K6[i] + e7*K2[i])*h;

                    for (int i = 1; i <= n; i++) {
                        sk = atol[i] + rtol[i] * Math.Max(Math.Abs(y[i]), Math.Abs(this.Ynew[i]));
                        sk = this.K1[i + this.K4off] / sk; //K4[i]/sk;
                        err = err + (sk * sk);
                    }
                    err = Math.Sqrt(err / n);

                    // computation of hnew
                    fac11 = Math.Pow(err, expo1);
                    fac = fac11 / Math.Pow(facold, beta); // LUND-stabilization
                    fac = Math.Max(facc2, Math.Min(facc1, (fac / SAFE))); // must have FAC1 <= HNEW/H <= FAC2
                    hnew = h / fac;
                }

                // step is accepted

                if (err <= 1.0) {
                    facold = Math.Max(err, 1.0e-4);
                    naccpt++;
                    for (int i = 1; i <= n; i++) {
                        this.K1[i] = this.K1[i + this.K2off]; //K2[i];
                        y[i] = this.Ynew[i];
                    }
                    t = t + h;
                    if (adjust != 0 && t <= tnext) htry[0] = h;
                    if (Math.Abs(hnew) > hmax) hnew = hmax;
                    if (reject != 0) hnew = Math.Min(Math.Abs(hnew), Math.Abs(h));
                    reject = 0;
                    //if (Report) Report(t, y, n);
                }

                // step is rejected

                else {
                    if (adjust != 0) hnew = h / Math.Min(facc1, (fac11 / SAFE));
                    reject = 1;
                    if (naccpt >= 1) nrejct++;
                }

                // take another step

                h = hnew;
                if (adjust != 0) htry[0] = h;
                nstep++;
                if (nstep >= this.Itmax)
                    return -1;
            }
            return nfcn;
        }
    }

}