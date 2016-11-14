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
using org.addition.epanet.network;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.hydraulic.structures {

public class SimulationTank : SimulationNode {

    private Link.StatType oldStat;

    public SimulationTank(Node @ref, int idx) : base(@ref, idx) {
        volume = ((Tank) this.node).V0;

        // Init
        head = ((Tank) this.node).H0;
        demand = (0.0);
        oldStat = Link.StatType.TEMPCLOSED;
    }


    public Tank getNode() {
        return (Tank) node;
    }


    private double volume;


    public double getArea() {
        return ((Tank) this.node).Area;
    }

    public double getHmin() {
        return ((Tank) this.node).Hmin;
    }

    public double getHmax() {
        return ((Tank) this.node).Hmax;
    }

    public double getVmin() {
        return ((Tank) this.node).Vmin;
    }

    public double getVmax() {
        return ((Tank) this.node).Vmax;
    }

    public double getV0() {
        return ((Tank) this.node).V0;
    }

    public Pattern getPattern() {
        return ((Tank) this.node).Pattern;
    }

    public Curve getVcurve() {
        return ((Tank) this.node).Vcurve;
    }

#if COMMENTED
    public double getH0()
    {
        return ((Tank)this.node).H0;
    }
    public double getKb()
    {
        return ((Tank)this.node).Kb;
    }

    public double[] getConcentration()
    {
        return ((Tank)this.node).Concentration;
    }

    public Tank.MixType getMixModel()
    {
        return ((Tank)this.node).MixModel;
    }

    public double getV1max()
    {
        return ((Tank)this.node).V1Max;
    }

#endif

    /// Simulation getters & setters.

    public double getSimVolume() {
        return volume;//((Tank)node).getSimVolume();
    }

    public bool isReservoir() {
        return ((Tank) this.node).Area == 0;
    }

    public Link.StatType getOldStat() {
        return oldStat;
    }

    public void setOldStat(Link.StatType value) {
        this.oldStat = value;
    }


    /// Simulation methods

    // Finds water volume in tank corresponding to elevation 'h'
    public double findVolume(FieldsMap fMap, double h) {

        Curve curve = getVcurve();
        if (curve == null)
            return (getVmin() + (h - getHmin()) * getArea());
        else {
            return
            curve.LinearInterpolator(
                (h - this.getElevation()) * fMap.GetUnits(FieldsMap.FieldType.HEAD)
                / fMap.GetUnits(FieldsMap.FieldType.VOLUME));
        }

    }

    // Computes new water levels in tank after current time step, with Euler integrator.
    private void updateLevel(FieldsMap fMap, long tstep) {

        if (getArea() == 0.0) // Reservoir
            return;

        // Euler
        double dv = demand * tstep;
        volume += dv;

        if (volume + demand >= getVmax())
            volume = getVmax();

        if (volume - demand <= getVmin())
            volume = getVmin();

        head = findGrade(fMap);
    }

    // Finds water level in tank corresponding to current volume
    private double findGrade(FieldsMap fMap) {
        Curve curve = getVcurve();
        if (curve == null)
            return (getHmin() + (volume - getVmin()) / getArea());
        else
            return this.getElevation() + curve.LinearInterpolator(this.volume * fMap.GetUnits(FieldsMap.FieldType.VOLUME)) / fMap.GetUnits(FieldsMap.FieldType.HEAD);
    }

    // Get the required time step based to fill or drain a tank
    private long getRequiredTimeStep(long tstep) {
        if (isReservoir()) return tstep;  //  Skip reservoirs

        double h = head;    // Current tank grade
        double q = demand;  // Flow into tank
        double v = 0.0;

        if (Math.Abs(q) <= Constants.QZERO)
            return tstep;

        if (q > 0.0 && h < getHmax())
            v = getVmax() - getSimVolume();        // Volume to fill
        else if (q < 0.0 && h > getHmin())
            v = getVmin() - getSimVolume();        // Volume to drain
        else
            return tstep;

        // Compute time to fill/drain
        long t = (long) Math.Round(v / q);

        // Revise time step
        if (t > 0 && t < tstep)
            tstep = t;

        return tstep;
    }

    // Revises time step based on shortest time to fill or drain a tank
    public static long minimumTimeStep(List<SimulationTank> tanks, long tstep) {
        long newTStep = tstep;
        foreach (SimulationTank tank  in  tanks)
            newTStep = tank.getRequiredTimeStep(newTStep);
        return newTStep;
    }

    // Computes new water levels in tanks after current time step.
    public static void stepWaterLevels(List<SimulationTank> tanks, FieldsMap fMap, long tstep) {
        foreach (SimulationTank tank  in  tanks)
            tank.updateLevel(fMap, tstep);
    }

}
}