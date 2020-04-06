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
using Epanet.Util;

namespace Epanet.Hydraulic.Structures {

    public class SimulationTank:SimulationNode {
        private readonly Tank _tank;
        public SimulationTank(Node @ref, int idx):base(@ref, idx) {
            _tank = (Tank)@ref;
            _volume = _tank.V0;

            // Init
            SimHead = _tank.H0;
            SimDemand = 0.0;
            OldStat = StatType.TEMPCLOSED;
        }

        private double _volume;

        public double Area => _tank.Area;

        public double Hmin => _tank.Hmin;

        public double Hmax => _tank.Hmax;

        public double Vmin => _tank.Vmin;

        public double Vmax => _tank.Vmax;

        public double V0 => _tank.V0;

        public Pattern Pattern => _tank.Pattern;

        public Curve Vcurve => _tank.Vcurve;

#if COMMENTED

        public double H0 => _tank.H0;

        public double Kb => _tank.Kb;

        public double Concentration => _tank.C;

        public MixType MixModel => _tank.MixModel;

        public double V1Max => _tank.V1Max;

#endif

        /// Simulation getters & setters.
        public double SimVolume => _volume;

        public bool IsReservoir => _tank.Area.IsZero();

        public StatType OldStat { get; set; }

        /// Simulation methods

        ///<summary>Finds water volume in tank corresponding to elevation 'h'</summary>
        public double FindVolume(FieldsMap fMap, double h) {

            Curve curve = Vcurve;
            if (curve == null)
                return Vmin + (h - Hmin) * Area;

            return
                curve.Interpolate((h - Elevation) * fMap.GetUnits(FieldType.HEAD)
                                  / fMap.GetUnits(FieldType.VOLUME));

        }

        /// <summary>Computes new water levels in tank after current time step, with Euler integrator.</summary>
        private void UpdateLevel(FieldsMap fMap, TimeSpan tstep) {

            if (Area.IsZero()) // Reservoir
                return;

            // Euler
            double dv = SimDemand * tstep.TotalSeconds;
            _volume += dv;

            if (_volume + SimDemand >= Vmax)
                _volume = Vmax;

            if (_volume - SimDemand <= Vmin)
                _volume = Vmin;

            SimHead = FindGrade(fMap);
        }

        /// <summary>Finds water level in tank corresponding to current volume.</summary>
        private double FindGrade(FieldsMap fMap) {
            Curve curve = Vcurve;
            if (curve == null)
                return Hmin + (_volume - Vmin) / Area;

            return Elevation
                   + curve.Interpolate(_volume * fMap.GetUnits(FieldType.VOLUME))
                   / fMap.GetUnits(FieldType.HEAD);
        }

        /// <summary>Get the required time step based to fill or drain a tank.</summary>
        private TimeSpan GetRequiredTimeStep(TimeSpan tstep) {
            if (IsReservoir) return tstep; //  Skip reservoirs

            double h = SimHead; // Current tank grade
            double q = SimDemand; // Flow into tank
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
            TimeSpan t = TimeSpan.FromSeconds(Math.Round(v / q));

            // Revise time step
            if (t > TimeSpan.Zero && t < tstep)
                tstep = t;

            return tstep;
        }

        /// <summary>Revises time step based on shortest time to fill or drain a tank.</summary>
        public static TimeSpan MinimumTimeStep(List<SimulationTank> tanks, TimeSpan tstep) {
            TimeSpan newTStep = tstep;
            foreach (SimulationTank tank  in  tanks)
                newTStep = tank.GetRequiredTimeStep(newTStep);

            return newTStep;
        }

        /// <summary>Computes new water levels in tanks after current time step.</summary>
        public static void StepWaterLevels(List<SimulationTank> tanks, FieldsMap fMap, TimeSpan tstep) {
            foreach (SimulationTank tank  in  tanks)
                tank.UpdateLevel(fMap, tstep);
        }

    }

}