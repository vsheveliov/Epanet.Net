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

using Epanet.Enums;

namespace Epanet.Network.Structures {

    ///<summary>Hydraulic pump structure.</summary>
    public class Pump:Link {
        ///<summary>Energy usage statistics.</summary>
        private readonly double[] energy = {0, 0, 0, 0, 0, 0};

        public Pump(string name):base(name) {
            // Link attributes
            this.Kc = 1.0;
            this.Type = LinkType.PUMP;
            this.Status = StatType.OPEN;
            
            // Pump attributes
            this.Ptype = PumpType.NOCURVE;
        }

        ///<summary>Unit energy cost.</summary>
        public double ECost { get; set; }

        ///<summary>Effic. v. flow curve reference.</summary>
        public Curve ECurve { get; set; }

        public double[] Energy { get { return this.energy; } }

        ///<summary>Energy cost pattern.</summary>
        public Pattern EPat { get; set; }

        ///<summary>Flow coefficient.</summary>
        public double FlowCoefficient { get; set; }

        ///<summary>Shutoff head (feet)</summary>
        public double H0 { get; set; }

        ///<summary>Head v. flow curve reference.</summary>
        public Curve HCurve { get; set; }

        ///<summary>Maximum head (feet)</summary>
        public double Hmax { get; set; }

        ///<summary>Flow exponent.</summary>
        public double N { get; set; }

        ///<summary>Pump curve type.</summary>
        public PumpType Ptype { get; set; }

        ///<summary>Initial flow (feet^3/s).</summary>
        public double Q0 { get; set; }

        ///<summary>Maximum flow (feet^3/s).</summary>
        public double Qmax { get; set; }

        ///<summary>Utilization pattern reference.</summary>
        public Pattern UPat { get; set; }

#if NUCONVERT

        public double GetNuFlowCoefficient(UnitsType utype) {
            return NUConvert.revertPower(utype, this.FlowCoefficient);
        }


        public double GetNuInitialFlow(FlowUnitsType utype) {
            return NUConvert.revertFlow(utype, this.Q0);
        }

        public double GetNuMaxFlow(FlowUnitsType utype) {
            return NUConvert.revertFlow(utype, this.Qmax);
        }

        public double GetNuMaxHead(UnitsType utype) {
            return NUConvert.revertDistance(utype, this.Hmax);
        }

        public double GetNuShutoffHead(UnitsType utype) {
            return NUConvert.revertDistance(utype, this.Hmax);
        }


        public void SetNuFlowCoefficient(UnitsType utype, double value) {
            this.FlowCoefficient = NUConvert.convertPower(utype, value);
        }

        public void SetNuInitialFlow(FlowUnitsType utype, double value) {
            this.Q0 = NUConvert.convertFlow(utype, value);
        }

        public void SetNuMaxFlow(FlowUnitsType utype, double value) {
            this.Qmax = NUConvert.convertFlow(utype, value);
        }

        public void SetNuMaxHead(UnitsType utype, double value) {
            this.Hmax = NUConvert.convertDistance(utype, value);
        }

        public void SetNuShutoffHead(UnitsType utype, double value) {
            this.H0 = NUConvert.convertDistance(utype, value);
        }


#endif
    }

}