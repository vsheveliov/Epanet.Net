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

    ///<summary>Hydraulic tank structure.</summary>
    public class Tank:Node {
        public Tank(string name):base(name) { }

        public override NodeType Type {
            get {
                return Math.Abs(Area) < double.Epsilon * 10 ? NodeType.RESERV : NodeType.TANK;
            }
        }

        ///<summary>Tank area (feet^2).</summary>
        public double Area { get; set; }

        ///<summary>Species concentration.</summary>
        public double C { get; set; }

        ///<summary>Initial water elev.</summary>
        public double H0 { get; set; }

        ///<summary>Maximum water elev (feet).</summary>
        public double Hmax { get; set; }

        ///<summary>Minimum water elev (feet).</summary>
        public double Hmin { get; set; }

        ///<summary>Reaction coeff. (1/days).</summary>
        public double Kb { get; set; }

        ///<summary>Type of mixing model</summary>
        public MixType MixModel { get; set; }

        ///<summary>Fixed grade time pattern.</summary>
        public Pattern Pattern { get; set; }

        ///<summary>Initial volume (feet^3).</summary>
        public double V0 { get; set; }

        ///<summary>Mixing compartment size</summary>
        public double V1Max { get; set; }

        ///<summary>Fixed grade time pattern</summary>
        public Curve Vcurve { get; set; }

        ///<summary>Maximum volume (feet^3).</summary>
        public double Vmax { get; set; }

        ///<summary>Minimum volume (feet^3).</summary>
        public double Vmin { get; set; }

#if NUCONVERT

        public double GetNuArea(UnitsType type) { return NUConvert.RevertArea(type, Area); }

        public double GetNuInitHead(UnitsType type) { return NUConvert.RevertDistance(type, H0); }

        public double GetNuInitVolume(UnitsType type) { return NUConvert.RevertVolume(type, V0); }

        public double GetNuMaximumHead(UnitsType type) {
            return NUConvert.RevertDistance(type, Hmax);
        }

        public double GetNuMaxVolume(UnitsType type) { return NUConvert.RevertVolume(type, Vmax); }

        public double GetNuMinimumHead(UnitsType type) {
            return NUConvert.RevertDistance(type, Hmin);
        }

        public double GetNuMinVolume(UnitsType type) { return NUConvert.RevertVolume(type, Vmin); }

        public void SetNuMinVolume(UnitsType type, double value) {
            Vmin = NUConvert.ConvertVolume(type, value);
        }


        public double GetNuMixCompartimentSize(UnitsType type) {
            return NUConvert.RevertVolume(type, V1Max);
        }


        public void SetNuArea(UnitsType type, double value) {
            Area = NUConvert.ConvertArea(type, value);
        }

        public void SetNuInitHead(UnitsType type, double value) {
            H0 = NUConvert.RevertDistance(type, value);
        }

        public void SetNuInitVolume(UnitsType type, double value) {
            V0 = NUConvert.ConvertVolume(type, value);
        }


        public void SetNuMaximumHead(UnitsType type, double value) {
            Hmax = NUConvert.RevertDistance(type, value);
        }

        public void SetNuMaxVolume(UnitsType type, double value) {
            Vmax = NUConvert.ConvertVolume(type, value);
        }

        public void SetNuMinimumHead(UnitsType type, double value) {
            Hmin = NUConvert.ConvertArea(type, value);
        }

        public void SetNuMixCompartimentSize(UnitsType type, double value) {
            V1Max = NUConvert.ConvertVolume(type, value);
        }

#endif

    }

}