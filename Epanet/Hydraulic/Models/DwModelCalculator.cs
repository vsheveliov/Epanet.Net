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
using Epanet.Hydraulic.Structures;
using Epanet.Network;
using Epanet.Network.Structures;

namespace Epanet.Hydraulic.Models {

    ///<summary>Darcy-Weishbach model calculator.</summary>
    public class DwModelCalculator:PipeHeadModel {

        // Constants used for computing Darcy-Weisbach friction factor
        public const double A1 = 0.314159265359e04; // 1000*PI
        public const double A2 = 0.157079632679e04; // 500*PI
        public const double A3 = 0.502654824574e02; // 16*PI
        public const double A4 = 6.283185307; // 2*PI
        public const double A8 = 4.61841319859; // 5.74*(PI/4)^.9
        public const double A9 = -8.685889638e-01; // -2/ln(10)
        public const double AA = -1.5634601348; // -2*.9*2/ln(10)
        public const double AB = 3.28895476345e-03; // 5.74/(4000^.9)
        public const double AC = -5.14214965799e-03; // AA*AB


        public override LinkCoeffs compute(PropertiesMap pMap, SimulationLink sL) {
            double viscos = pMap.Viscos;
            double rQtol = pMap.RQtol;
            double hexp = pMap.Hexp;

            Link link = sL.Link;
            double simFlow = sL.SimFlow;
            double q = Math.Abs(simFlow); // Absolute flow
            double km = link.Km; // Minor loss coeff.
            double flowResistance = link.FlowResistance; // Resistance coeff.
            double roughness = link.Roughness;
            double diameter = link.Diameter;
            bool isOne = sL.Type > Link.LinkType.PIPE;

            LinkCoeffs linkCoeffs = innerDwCalc(
                viscos,
                rQtol,
                hexp,
                simFlow,
                q,
                km,
                flowResistance,
                roughness,
                diameter,
                isOne);
            return linkCoeffs;
        }

        private static LinkCoeffs innerDwCalc(
            double viscos,
            double rQtol,
            double hexp,
            double simFlow,
            double q,
            double km,
            double flowResistance,
            double roughness,
            double diameter,
            bool one) {

            double resistance;
            double x1, x2, x3, x4, y1, y2, y3, fa, fb, r2; 
            double s, w;

            if (one)
                resistance = 1d;
            else {
                s = viscos * diameter;

                w = q / s;
                if (w >= A1) {
                    y1 = A8 / Math.Pow(w, 0.9d);
                    y2 = roughness / (3.7 * diameter) + y1;
                    y3 = A9 * Math.Log(y2);
                    resistance = 1.0 / (y3 * y3);
                }
                else if (w > A2) {
                    y2 = roughness / (3.7 * diameter) + AB;
                    y3 = A9 * Math.Log(y2);
                    fa = 1.0 / (y3 * y3);
                    fb = (2.0 + AC / (y2 * y3)) * fa;
                    r2 = w / A2;
                    x1 = 7.0 * fa - fb;
                    x2 = 0.128 - 17.0 * fa + 2.5 * fb;
                    x3 = -0.128 + 13.0 * fa - (fb + fb);
                    x4 = r2 * (0.032 - 3.0 * fa + 0.5 * fb);
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
            LinkCoeffs linkCoeffs;
            // Use large P coefficient for small flow resistance product
            if (r1 * q < rQtol) {

                linkCoeffs = new LinkCoeffs(1d / rQtol, simFlow / hexp);
            }
            else {
                // Compute P and Y coefficients
                double hpipe = r1 * (q * q); // Total head loss
                double p = 2d * r1 * q; // |dh/dQ|
                p = 1d / p;
                linkCoeffs = new LinkCoeffs(p, simFlow < 0 ? -hpipe * p : hpipe * p);
            }
            return linkCoeffs;
        }


    }

}