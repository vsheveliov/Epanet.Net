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

namespace Epanet.Hydraulic.Models {

    ///<summary>Hazen-Williams model calculator.</summary>
    public class HWModelCalculator:PipeHeadModel {

        public override LinkCoeffs compute(PropertiesMap pMap, SimulationLink sL) {
            // Evaluate headloss coefficients
            double q = Math.Abs(sL.SimFlow); // Absolute flow
            double ml = sL.Link.Km; // Minor loss coeff.
            double r = sL.Link.FlowResistance; // Resistance coeff.

            double r1 = 1.0 * r + ml;

            // Use large P coefficient for small flow resistance product
            if (r1 * q < pMap.RQtol) {
                return new LinkCoeffs(1d / pMap.RQtol, sL.SimFlow / pMap.Hexp);
            }

            double hpipe = r * Math.Pow(q, pMap.Hexp); // Friction head loss
            double p = pMap.Hexp * hpipe; // Q*dh(friction)/dQ
            double hml;
            if (ml > 0d) {
                hml = ml * q * q; // Minor head loss
                p += 2d * hml; // Q*dh(Total)/dQ
            }
            else
                hml = 0d;

            p = sL.SimFlow / p; // 1 / (dh/dQ)
            return new LinkCoeffs(Math.Abs(p), p * (hpipe + hml));
        }


    }

}