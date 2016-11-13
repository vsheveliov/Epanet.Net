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

namespace org.addition.epanet.msx.Structures {

public class Tank {
    int    node;        // node index of tank
    double hstep;       // integration time step
    double a;           // tank area
    double v0;          // initial volume
    double v;           // tank volume
    EnumTypes.MixType    mixModel;    // type of mixing model
    double vMix;        // mixing compartment size
    double [] param;    // kinetic parameter values
    double [] c;        // current species concentrations

    public Tank(int @params, int species) {
        param = new double[@params];
        c = new double[species];
    }

    public int getNode() {
        return node;
    }

    public void setNode(int value) {
        this.node = value;
    }

    public double getHstep() {
        return hstep;
    }

    public void setHstep(double value) {
        this.hstep = value;
    }

    public double getA() {
        return a;
    }

    public void setA(double value) {
        this.a = value;
    }

    public double getV0() {
        return v0;
    }

    public void setV0(double value) {
        this.v0 = value;
    }

    public double getV() {
        return v;
    }

    public void setV(double value) {
        this.v = value;
    }

    public EnumTypes.MixType getMixModel() {
        return mixModel;
    }

    public void setMixModel(EnumTypes.MixType value) {
        this.mixModel = value;
    }

    public double getvMix() {
        return vMix;
    }

    public void setvMix(double value) {
        this.vMix = value;
    }

    public double[] getParam() {
        return param;
    }

    public void setParam(double[] value) {
        this.param = value;
    }

    public double[] getC() {
        return c;
    }

    public void setC(double[] value) {
        this.c = value;
    }
}
}