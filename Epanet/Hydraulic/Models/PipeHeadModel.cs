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

using org.addition.epanet.hydraulic.structures;
using org.addition.epanet.network;

namespace org.addition.epanet.hydraulic.models {

    ///<summary>Pipe head loss model calculator.</summary>
    public abstract class PipeHeadModel {

        ///<summary>Link coefficients.</summary>
        public class LinkCoeffs {
            public LinkCoeffs(double invHeadLoss, double flowCorrection) {
                this.invHeadLoss = invHeadLoss;
                this.flowCorrection = flowCorrection;
            }

            private readonly double invHeadLoss;
            private readonly double flowCorrection;

            public double InvHeadLoss { get { return this.invHeadLoss; } }

            public double FlowCorrection { get { return this.flowCorrection; } }
        }

        ///<summary>Compute link coefficients through the implemented pipe headloss model.</summary>
        /// <param name="pMap">Network properties map.</param>
        /// <param name="sL">Simulation link.</param>
        /// <returns>Computed link coefficients.</returns>
        public abstract LinkCoeffs compute(PropertiesMap pMap, SimulationLink sL);
    }

}