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

namespace org.addition.epanet.network.structures {

    ///<summary>Hydraulic tank structure.</summary>
    public class Tank:Node {

        /// <summary>Tank mixing regimes.</summary>
        public enum MixType {
            /// <summary>1-compartment model</summary>
            MIX1 = 0,
            /// <summary>2-compartment model</summary>
            MIX2 = 1,
            /// <summary>First in, first out model</summary>
            FIFO = 2,
            /// <summary> Last in, first out model</summary>
            LIFO = 3,
        }

        ///<summary>Tank area (feet^2).</summary>
        private double area;
        ///<summary>Tank volume (feet^3).</summary>
        //private double v;
        ///<summary>Species concentration.</summary>
        private double[] c;
        ///<summary>Initial water elev.</summary>
        private double h0;
        ///<summary>Maximum water elev (feet).</summary>
        private double hMax;
        ///<summary>Minimum water elev (feet).</summary>
        private double hMin;
        ///<summary>Reaction coeff. (1/days).</summary>
        private double kb;
        ///<summary>Type of mixing model</summary>
        private MixType mixModel;
        ///<summary>Fixed grade time pattern.</summary>
        private Pattern pattern;
        ///<summary>Initial volume (feet^3).</summary>
        private double v0;
        ///<summary>Mixing compartment size</summary>
        private double v1max;
        ///<summary>Fixed grade time pattern</summary>
        private Curve vCurve;
        ///<summary>Maximum volume (feet^3).</summary>
        private double vMax;
        ///<summary>Minimum volume (feet^3).</summary>
        private double vMin;


        public double getArea() { return area; }

        public double[] getConcentration() { return c; }

        public double getH0() { return h0; }

        public double getHmax() { return hMax; }

        public double getHmin() { return hMin; }

        public double getKb() { return kb; }

        public MixType getMixModel() { return mixModel; }

        public double getNUArea(PropertiesMap.UnitsType type) { return NUConvert.revertArea(type, area); }

        public double getNUInitHead(PropertiesMap.UnitsType type) { return NUConvert.revertDistance(type, h0); }

        public double getNUInitVolume(PropertiesMap.UnitsType type) { return NUConvert.revertVolume(type, v0); }

        public double getNUMaximumHead(PropertiesMap.UnitsType type) { return NUConvert.revertDistance(type, hMax); }

        public double getNUMaxVolume(PropertiesMap.UnitsType type) { return NUConvert.revertVolume(type, vMax); }

        public double getNUMinimumHead(PropertiesMap.UnitsType type) { return NUConvert.revertDistance(type, hMin); }

        public double getNUMinVolume(PropertiesMap.UnitsType type) { return NUConvert.revertVolume(type, vMin); }

        public void setNUMinVolume(PropertiesMap.UnitsType type, double value) {
            vMin = NUConvert.convertVolume(type, value);
        }


        public double getNUMixCompartimentSize(PropertiesMap.UnitsType type) {
            return NUConvert.revertVolume(type, v1max);
        }


        public Pattern getPattern() { return pattern; }

        public double getV0() { return v0; }

        public double getV1max() { return this.v1max; }

        public Curve getVcurve() { return vCurve; }

        public double getVmax() { return vMax; }

        public double getVmin() { return vMin; }

        public void setArea(double a) { area = a; }

        public void setConcentration(double[] value) { this.c = value; }

        public void setH0(double value) { this.h0 = value; }

        public void setHmax(double value) { this.hMax = value; }

        public void setHmin(double value) { this.hMin = value; }

        public void setKb(double value) { this.kb = value; }

        public void setMixModel(MixType value) { this.mixModel = value; }

        public void setNUArea(PropertiesMap.UnitsType type, double value) { area = NUConvert.convertArea(type, value); }

        public void setNUInitHead(PropertiesMap.UnitsType type, double value) {
            h0 = NUConvert.revertDistance(type, value);
        }

        public void setNUInitVolume(PropertiesMap.UnitsType type, double value) {
            v0 = NUConvert.convertVolume(type, value);
        }

        //public double getVolume() {
        //    return v;
        //}
        //
        //public void setVolume(double v) {
        //    this.v = v;
        //}

        public void setNUMaximumHead(PropertiesMap.UnitsType type, double value) {
            hMax = NUConvert.revertDistance(type, value);
        }

        public void setNUMaxVolume(PropertiesMap.UnitsType type, double value) {
            vMax = NUConvert.convertVolume(type, value);
        }

        public void setNUMinimumHead(PropertiesMap.UnitsType type, double value) {
            hMin = NUConvert.convertArea(type, value);
        }

        public void setNUMixCompartimentSize(PropertiesMap.UnitsType type, double value) {
            v1max = NUConvert.convertVolume(type, value);
        }

        public void setPattern(Pattern value) { this.pattern = value; }

        public void setV0(double value) { this.v0 = value; }

        public void setV1max(double vLmax) { this.v1max = vLmax; }

        public void setVcurve(Curve vcurve) { vCurve = vcurve; }

        public void setVmax(double vmax) { vMax = vmax; }

        public void setVmin(double vmin) { vMin = vmin; }

        public bool IsReservoir { get { return Math.Abs(this.area) < double.Epsilon * 10; } }
    }

}