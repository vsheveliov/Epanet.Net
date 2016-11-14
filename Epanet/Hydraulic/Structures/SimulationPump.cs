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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using org.addition.epanet.network;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.hydraulic.structures {

public class SimulationPump : SimulationLink {

    private double h0;                  // Simulated shutoff head
    private double flowCoefficient;     // Simulated Flow coefficent
    private double n;                   // Simulated flow expoent

    public class Energy {
        public Energy(double power, double efficiency) {
            this.power = power;
            this.efficiency = efficiency;
        }

        public double power;        // Pump used power (KW)
        public double efficiency;   // Pump effiency
    }

    public SimulationPump(List<SimulationNode> indexedNodes, Link @ref, int idx):base(indexedNodes, @ref, idx) {
        
        for (int i = 0; i < 6; i++)
            energy[i] = ((Pump)@ref).Energy[0]; // BUG: Baseform bug ?

        h0 = ((Pump) @ref).H0;
        flowCoefficient = ((Pump) @ref).FlowCoefficient;
        n = ((Pump) @ref).N;
    }

    private double[] energy = {0, 0, 0, 0, 0, 0};


    public Pump.PumpType getPtype() {
        return ((Pump) this.link).Ptype;
    }

    public double getQ0() {
        return ((Pump) this.link).Q0;
    }

    public double getQmax() {
        return ((Pump) this.link).Qmax;
    }

    public double getHmax() {
        return ((Pump) this.link).Hmax;
    }


    public Curve getHcurve() {
        return ((Pump) this.link).Hcurve;
    }

    public Curve getEcurve() {
        return ((Pump) this.link).Ecurve;
    }

    public Pattern getUpat() {
        return ((Pump) this.link).Upat;
    }

    public Pattern getEpat() {
        return ((Pump) this.link).Epat;
    }

    public double getEcost() {
        return ((Pump) this.link).Ecost;
    }


    // Simulation getters and setters


    public double getEnergy(int id) {
        return energy[id];//((Pump)node).getEnergy(id);
    }

    public void setEnergy(int id, double value) {
        energy[id] = value;
    }


    private void setH0(double value) {
        this.h0 = value;
    }

    public double getH0() {
        return h0;
    }

    public double getFlowCoefficient() {
        return flowCoefficient;
    }

    private void setFlowCoefficient(double value) {
        this.flowCoefficient = value;
    }

    private void setN(double n) {
        this.n = n;
    }

    public double getN() {
        return n;
    }

    // Computes flow energy associated with this link pump.
    private Energy getFlowEnergy(PropertiesMap pMap, FieldsMap fMap) {
        Energy ret = new Energy(0.0, 0.0);

        if (status <= Link.StatType.CLOSED) {
            return ret;
        }

        double q = Math.Abs(flow);
        double dh = Math.Abs(first.getSimHead() - second.getSimHead());

        double e = pMap.Epump;

        if (getEcurve() != null) {
            Curve curve = getEcurve();
            e = curve.LinearInterpolator(q * fMap.GetUnits(FieldsMap.FieldType.FLOW));
        }
        e = Math.Min(e, 100.0);
        e = Math.Max(e, 1.0);
        e /= 100.0;

        ret.power = dh * q * pMap.SpGrav / 8.814 / e * Constants.KWperHP;
        ret.efficiency = e;

        return ret;
    }


    // Accumulates pump energy usage.
    private double updateEnergy(PropertiesMap pMap, FieldsMap fMap,
                                long n, double c0, double f0, double dt) {
        double c = 0;

        //Skip closed pumps
        if (status <= Link.StatType.CLOSED) return 0.0;
        double q = Math.Max(Constants.QZERO, Math.Abs(flow));

        // Find pump-specific energy cost
        if (getEcost() > 0.0)
            c = getEcost();
        else
            c = c0;

        if (getEpat() != null) {
            int m = (int) (n % this.getEpat().FactorsList.Count);
            c *= this.getEpat().FactorsList[m];
        } else
            c *= f0;

        // Find pump energy & efficiency
        Energy energy = getFlowEnergy(pMap, fMap);

        // Update pump's cumulative statistics
        setEnergy(0, getEnergy(0) + dt);                        // Time on-line
        setEnergy(1, getEnergy(1) + energy.efficiency * dt);    // Effic.-hrs
        setEnergy(2, getEnergy(2) + energy.power / q * dt);     // kw/cfs-hrs
        setEnergy(3, getEnergy(3) + energy.power * dt);         // kw-hrs
        setEnergy(4, Math.Max(getEnergy(4), energy.power));
        setEnergy(5, getEnergy(5) + c * energy.power * dt);         // cost-hrs.

        return energy.power;
    }

    // Computes P & Y coeffs. for pump in the link
    public void computePumpCoeff(FieldsMap fMap, PropertiesMap pMap) {
        double h0, q, r, n;

        if (status <= Link.StatType.CLOSED || setting == 0.0) {
            invHeadLoss = 1.0 / Constants.CBIG;
            flowCorrection = flow;
            return;
        }

        q = Math.Max(Math.Abs(flow), Constants.TINY);

        if (getPtype() == Pump.PumpType.CUSTOM) {

            Curve.Coeffs coeffs = getHcurve().getCoeff(fMap, q / setting);

            setH0(-coeffs.h0);
            setFlowCoefficient(-coeffs.r);
            setN(1.0);
        }

        h0 = (setting * setting) * getH0();
        n = getN();
        r = getFlowCoefficient() * Math.Pow(setting, 2.0 - n);
        if (n != 1.0) r = n * r * Math.Pow(q, n - 1.0);

        invHeadLoss = 1.0 / Math.Max(r, pMap.RQtol);
        flowCorrection = flow / n + invHeadLoss * h0;
    }

    // Get new pump status
    // dh head gain
    public Link.StatType pumpStatus(PropertiesMap pMap, double dh) {
        double hmax;

        if (getPtype() == Pump.PumpType.CONST_HP)
            hmax = Constants.BIG;
        else
            hmax = (setting * setting) * getHmax();

        if (dh > hmax + pMap.Htol)
            return (Link.StatType.XHEAD);

        return (Link.StatType.OPEN);
    }

    // Update pumps energy
    public static double stepEnergy(PropertiesMap pMap, FieldsMap fMap,
                                    Pattern Epat,
                                    List<SimulationPump> pumps,
                                    long htime, long hstep) {
        double dt, psum = 0.0;


        if (pMap.Duration == 0)
            dt = 1.0;
        else if (htime < pMap.Duration)
            dt = (double) hstep / 3600.0;
        else
            dt = 0.0;

        if (dt == 0.0)
            return 0.0;

        long n = (htime + pMap.Pstart) / pMap.Pstep;


        double c0 = pMap.Ecost;
        double f0 = 1.0;

        if (Epat != null) {
            long m = n % (long) Epat.FactorsList.Count;
            f0 = Epat.FactorsList[(int) m];
        }

        foreach (SimulationPump pump  in  pumps) {
            psum += pump.updateEnergy(pMap, fMap, n, c0, f0, dt);
        }

        return psum;
    }


}
}