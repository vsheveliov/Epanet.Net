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

using Epanet.Enums;
using Epanet.Hydraulic.Structures;
using Epanet.Network.Structures;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet.Hydraulic.Models {

    ///<summary>Pipe head loss model calculators.</summary>
    public static class PipeHeadModelCalculators {
        /// <summary>Compute link coefficients through the implemented pipe headloss model.</summary>
        ///  <param name="net">Epanet Network.</param>
        ///  <param name="sL">Simulation link.</param>
        /// <param name="invHeadLoss">Computed link coefficients.</param>
        /// <param name="flowCorrection">Computed link coefficients.</param>
        /// <returns>Computed link coefficients.</returns>
        public delegate void Compute(
            EpanetNetwork net,
            SimulationLink sL,
            out double invHeadLoss,
            out double flowCorrection);

        #region Darcy-Weishbach model calculator

        // Constants used for computing Darcy-Weisbach friction factor
        private const double A1 = 0.314159265359e04; // 1000*PI
        private const double A2 = 0.157079632679e04; // 500*PI
        private const double A3 = 0.502654824574e02; // 16*PI
        private const double A4 = Math.PI * 2; // 2*PI
        private const double A8 = 4.61841319859; // 5.74*(PI/4)^.9
        private const double A9 = -8.685889638e-01; // -2/ln(10)
        // private const double AA = -1.5634601348; // -2*.9*2/ln(10)
        private const double AB = 3.28895476345e-03; // 5.74/(4000^.9)
        private const double AC = -5.14214965799e-03; // AA*AB

        ///<summary>Darcy-Weishbach model calculator.</summary>
        public static void DwModelCalculator(
            EpanetNetwork net,
            SimulationLink sL,
            out double invHeadLoss,
            out double flowCorrection) {
            Link link = sL.Link;
            double simFlow = sL.SimFlow;
            double q = Math.Abs(simFlow); // Absolute flow
            double km = link.Km; // Minor loss coeff.
            double flowResistance = link.FlowResistance; // Resistance coeff.
            double roughness = link.Kc;
            double diameter = link.Diameter;
            bool isOne = sL.Type > LinkType.PIPE;

            double resistance;

            if (isOne)
                resistance = 1d;
            else {
                double s = net.Viscos * diameter;
                double w = q / s;

                if (w >= A1) {
                    double y1 = A8 / Math.Pow(w, 0.9d);
                    double y2 = roughness / (3.7 * diameter) + y1;
                    double y3 = A9 * Math.Log(y2);
                    resistance = 1.0 / (y3 * y3);
                }
                else if (w > A2) {
                    double y2 = roughness / (3.7 * diameter) + AB;
                    double y3 = A9 * Math.Log(y2);
                    double fa = 1.0 / (y3 * y3);
                    double fb = (2.0 + AC / (y2 * y3)) * fa;
                    double r2 = w / A2;
                    double x1 = 7.0 * fa - fb;
                    double x2 = 0.128 - 17.0 * fa + 2.5 * fb;
                    double x3 = -0.128 + 13.0 * fa - (fb + fb);
                    double x4 = r2 * (0.032 - 3.0 * fa + 0.5 * fb);
                    resistance = x1 + r2 * (x2 + r2 * (x3 + x4));
                }
                else if (w > A4) {
                    resistance = A3 * s / q;
                }
                else {
                    resistance = 8d;
                }
            }

            double r1 = resistance * flowResistance + km;
            // Use large P coefficient for small flow resistance product
            if (r1 * q < net.RQtol) {
                invHeadLoss = 1d / net.RQtol;
                flowCorrection = simFlow / net.HExp;
            }
            else {
                // Compute P and Y coefficients
                double hpipe = r1 * (q * q); // Total head loss
                double p = 2d * r1 * q; // |dh/dQ|
                invHeadLoss = 1d / p;
                flowCorrection = simFlow < 0 ? -hpipe * invHeadLoss : hpipe * invHeadLoss;
            }
        }

        #endregion

        #region Chezy-Manning model calculator

        ///<summary>Chezy-Manning model calculator, which is implemented through the Hazen-Williams model.</summary>
        public static void CmModelCalculator(
            EpanetNetwork net,
            SimulationLink sl,
            out double invheadloss,
            out double flowcorrection) {
            HwModelCalculator(net, sl, out invheadloss, out flowcorrection);
        }

        #endregion

        #region Hazen-Williams model calculator

        ///<summary>Hazen-Williams model calculator.</summary>
        public static void HwModelCalculator(
            EpanetNetwork net,
            SimulationLink sL,
            out double invHeadLoss,
            out double flowCorrection) {
            // Evaluate headloss coefficients
            double q = Math.Abs(sL.SimFlow); // Absolute flow
            double ml = sL.Link.Km; // Minor loss coeff.
            double r = sL.Link.FlowResistance; // Resistance coeff.

            double r1 = 1.0 * r + ml;

            // Use large P coefficient for small flow resistance product
            if (r1 * q < net.RQtol) {
                invHeadLoss = 1d / net.RQtol;
                flowCorrection = sL.SimFlow / net.HExp;
                return;
            }

            double hpipe = r * Math.Pow(q, net.HExp); // Friction head loss
            double p = net.HExp * hpipe; // Q*dh(friction)/dQ
            double hml;

            if (ml > 0d) {
                hml = ml * q * q; // Minor head loss
                p += 2d * hml; // Q*dh(Total)/dQ
            }
            else
                hml = 0d;

            p = sL.SimFlow / p; // 1 / (dh/dQ)

            invHeadLoss = Math.Abs(p);
            flowCorrection = p * (hpipe + hml);
        }

        #endregion
    }

}
