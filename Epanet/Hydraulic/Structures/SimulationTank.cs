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

using Epanet.Enums;
using Epanet.Network;
using Epanet.Network.Structures;

namespace Epanet.Hydraulic.Structures {

    public class SimulationTank:SimulationNode {
        public SimulationTank(Node @ref, int idx):base(@ref, idx) {
            _volume = ((Tank)node).V0;

            // Init
            head = ((Tank)node).H0;
            demand = 0.0;
            OldStat = StatType.TEMPCLOSED;
        }

        // public Tank Node { get { return (Tank)this.node; } }

        private double _volume;

        public double Area { get { return ((Tank)node).Area; } }

        public double Hmin { get { return ((Tank)node).Hmin; } }

        public double Hmax { get { return ((Tank)node).Hmax; } }

        public double Vmin { get { return ((Tank)node).Vmin; } }

        public double Vmax { get { return ((Tank)node).Vmax; } }

        public double V0 { get { return ((Tank)node).V0; } }

        public Pattern Pattern { get { return ((Tank)node).Pattern; } }

        public Curve Vcurve { get { return ((Tank)node).Vcurve; } }

#if COMMENTED

        public double H0 { get { return ((Tank)node).H0; } }

        public double Kb { get { return ((Tank)node).Kb; } }

        public double Concentration { get { return ((Tank)node).C; } }

        public MixType MixModel { get { return ((Tank)node).MixModel; } }

        public double V1Max { get { return ((Tank)node).V1Max; } }

#endif

        /// Simulation getters & setters.
        public double SimVolume { get { return _volume; } }

        public bool IsReservoir { get { return ((Tank)node).Area == 0; } }

        public StatType OldStat { get; set; }

        /// Simulation methods

        ///<summary>Finds water volume in tank corresponding to elevation 'h'</summary>
        public double FindVolume(FieldsMap fMap, double h) {

            Curve curve = Vcurve;
            if (curve == null)
                return Vmin + (h - Hmin) * Area;
            else {
                return
                    curve.Interpolate((h - Elevation) * fMap.GetUnits(FieldType.HEAD)
                                      / fMap.GetUnits(FieldType.VOLUME));
            }

        }

        /// <summary>Computes new water levels in tank after current time step, with Euler integrator.</summary>
        private void UpdateLevel(FieldsMap fMap, long tstep) {

            if (Area == 0.0) // Reservoir
                return;

            // Euler
            double dv = demand * tstep;
            _volume += dv;

            if (_volume + demand >= Vmax)
                _volume = Vmax;

            if (_volume - demand <= Vmin)
                _volume = Vmin;

            head = FindGrade(fMap);
        }

        /// <summary>Finds water level in tank corresponding to current volume.</summary>
        private double FindGrade(FieldsMap fMap) {
            Curve curve = Vcurve;
            if (curve == null)
                return Hmin + (_volume - Vmin) / Area;
            else
                return Elevation
                       + curve.Interpolate(_volume * fMap.GetUnits(FieldType.VOLUME))
                       / fMap.GetUnits(FieldType.HEAD);
        }

        /// <summary>Get the required time step based to fill or drain a tank.</summary>
        private long GetRequiredTimeStep(long tstep) {
            if (IsReservoir) return tstep; //  Skip reservoirs

            double h = head; // Current tank grade
            double q = demand; // Flow into tank
            double v;

            if (Math.Abs(q) <= Constants.QZERO)
                return tstep;

            if (q > 0.0 && h < Hmax)
                v = Vmax - SimVolume; // Volume to fill
            else if (q < 0.0 && h > Hmin)
                v = Vmin - SimVolume; // Volume to drain
            else
                return tstep;

            // Compute time to fill/drain
            long t = (long)Math.Round(v / q);

            // Revise time step
            if (t > 0 && t < tstep)
                tstep = t;

            return tstep;
        }

        /// <summary>Revises time step based on shortest time to fill or drain a tank.</summary>
        public static long MinimumTimeStep(List<SimulationTank> tanks, long tstep) {
            long newTStep = tstep;
            foreach (SimulationTank tank  in  tanks)
                newTStep = tank.GetRequiredTimeStep(newTStep);
            return newTStep;
        }

        /// <summary>Computes new water levels in tanks after current time step.</summary>
        public static void StepWaterLevels(List<SimulationTank> tanks, FieldsMap fMap, long tstep) {
            foreach (SimulationTank tank  in  tanks)
                tank.UpdateLevel(fMap, tstep);
        }

    }

}