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

namespace Epanet.Network.Structures {

    ///<summary>Hydraulic pump structure.</summary>
    public class Pump:Link {
        public Pump(string name):base(name) {
            // Link attributes
            Kc = 1.0;
            Status = StatType.OPEN;
            
            // Pump attributes
            Ptype = PumpType.NOCURVE;
        }

        public override LinkType LinkType => LinkType.PUMP;

        public override void InitResistance(FormType formflag, double hexp) {
            FlowResistance = Constants.CBIG;
        }

        public override void ConvertUnits(Network nw) {
            FieldsMap fMap = nw.FieldsMap;

            if(Ptype == PumpType.CONST_HP) {
                if(nw.UnitsFlag == UnitsType.SI)
                    FlowCoefficient /= fMap.GetUnits(FieldType.POWER);
            }
            else {
                if(Ptype == PumpType.POWER_FUNC) {
                    H0 /= fMap.GetUnits(FieldType.HEAD);

                    FlowCoefficient *=
                                           Math.Pow(fMap.GetUnits(FieldType.FLOW), N) /
                                           fMap.GetUnits(FieldType.HEAD);
                }

                Q0 /= fMap.GetUnits(FieldType.FLOW);
                Qmax /= fMap.GetUnits(FieldType.FLOW);
                Hmax /= fMap.GetUnits(FieldType.HEAD);
            }
                        
        }


        ///<summary>Unit energy cost.</summary>
        public double ECost { get; set; }

        ///<summary>Effic. v. flow curve reference.</summary>
        public Curve ECurve { get; set; }

        ///<summary>Energy usage statistics.</summary>
        public double[] Energy { get; } = {0, 0, 0, 0, 0, 0};

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
            return NUConvert.RevertPower(utype, FlowCoefficient);
        }


        public double GetNuInitialFlow(FlowUnitsType utype) {
            return NUConvert.RevertFlow(utype, Q0);
        }

        public double GetNuMaxFlow(FlowUnitsType utype) {
            return NUConvert.RevertFlow(utype, Qmax);
        }

        public double GetNuMaxHead(UnitsType utype) {
            return NUConvert.RevertDistance(utype, Hmax);
        }

        public double GetNuShutoffHead(UnitsType utype) {
            return NUConvert.RevertDistance(utype, Hmax);
        }


        public void SetNuFlowCoefficient(UnitsType utype, double value) {
            FlowCoefficient = NUConvert.ConvertPower(utype, value);
        }

        public void SetNuInitialFlow(FlowUnitsType utype, double value) {
            Q0 = NUConvert.ConvertFlow(utype, value);
        }

        public void SetNuMaxFlow(FlowUnitsType utype, double value) {
            Qmax = NUConvert.ConvertFlow(utype, value);
        }

        public void SetNuMaxHead(UnitsType utype, double value) {
            Hmax = NUConvert.ConvertDistance(utype, value);
        }

        public void SetNuShutoffHead(UnitsType utype, double value) {
            H0 = NUConvert.ConvertDistance(utype, value);
        }


#endif

        

        
    }

}