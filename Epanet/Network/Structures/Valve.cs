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

    ///<summary>Hydraulic valve structure.</summary>
    public class Valve:Link {

        public Valve(string name, ValveType type):base(name) => ValveType = type;

        public ValveType ValveType { get; }

        public override LinkType LinkType => LinkType.VALVE;

        public override void InitResistance(FormType formflag, double hexp) {  }

        public override void ConvertUnits(Network nw) {
            FieldsMap fMap = nw.FieldsMap;

            Diameter /= fMap.GetUnits(FieldType.DIAM);

            double diam = Math.Pow(Diameter, 2);
            Km = 0.02517 * Km / diam / diam;

            if(!double.IsNaN(Kc))
                switch (ValveType) {
                    case ValveType.FCV:
                        Kc /= fMap.GetUnits(FieldType.FLOW);
                        break;
                    case ValveType.PRV:
                    case ValveType.PSV:
                    case ValveType.PBV:
                        Kc /= fMap.GetUnits(FieldType.PRESSURE);
                        break;
                }

        }

        ///<summary>Settings curve.</summary>
        public Curve Curve { get; set; }

#if NUCONVERT
        public override double GetNuRoughness(FlowUnitsType fType, PressUnitsType pType, double spGrav) {

            switch (ValveType) {
                case ValveType.FCV:
                    return NUConvert.RevertFlow(fType, Kc);
                case ValveType.PRV:
                case ValveType.PSV:
                case ValveType.PBV:
                    return NUConvert.RevertPressure(pType, spGrav, Kc);
            }

            return Kc;
        }

#endif

    }

}