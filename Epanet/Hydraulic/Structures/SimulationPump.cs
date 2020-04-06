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
using Epanet.Network.Structures;
using Epanet.Util;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet.Hydraulic.Structures {

    public class SimulationPump:SimulationLink {
        public SimulationPump(Dictionary<string, SimulationNode> indexedNodes, Link @ref, int idx):base(indexedNodes, @ref, idx) {

            for (int i = 0; i < 6; i++)
                _energy[i] = ((Pump)@ref).Energy[0]; // BUG: Baseform bug ?

            H0 = ((Pump)@ref).H0;
            FlowCoefficient = ((Pump)@ref).FlowCoefficient;
            N = ((Pump)@ref).N;
        }

        private readonly double[] _energy = {0, 0, 0, 0, 0, 0};

        public PumpType Ptype => ((Pump)link).Ptype;

        public double Q0 => ((Pump)link).Q0;

        public double Qmax => ((Pump)link).Qmax;

        public double Hmax => ((Pump)link).Hmax;

        public Curve Hcurve => ((Pump)link).HCurve;

        public Curve Ecurve => ((Pump)link).ECurve;

        public Pattern Upat => ((Pump)link).UPat;

        public Pattern Epat => ((Pump)link).EPat;

        public double Ecost => ((Pump)link).ECost;

        // Simulation getters and setters
        public double[] Energy => _energy;

        ///<summary>Simulated shutoff head</summary>
        public double H0 { set; get; }

        ///<summary>Simulated Flow coefficent</summary>
        private double FlowCoefficient { get; set; }

        ///<summary>Simulated flow expoent</summary>
        public double N { set; get; }

        /// <summary>Computes flow energy associated with this link pump.</summary>
        /// <param name="net"></param>
        /// <param name="power">Pump used power (KW)</param>
        /// <param name="efficiency">Pump effiency</param>
        private void GetFlowEnergy(EpanetNetwork net, out double power, out double efficiency) {
            power = efficiency = 0.0;

            if (SimStatus <= StatType.CLOSED) {
                return;
            }

            double q = Math.Abs(SimFlow);
            double dh = Math.Abs(first.SimHead - second.SimHead);

            double e = net.EPump;

            if (Ecurve != null) {
                Curve curve = Ecurve;
                e = curve.Interpolate(q * net.FieldsMap.GetUnits(FieldType.FLOW));
            }

            e = Math.Min(e, 100.0);
            e = Math.Max(e, 1.0);
            e /= 100.0;

            power = dh * q * net.SpGrav / 8.814 / e * Constants.KWperHP;
            efficiency = e;
        }


        /// <summary>Accumulates pump energy usage.</summary>
        private double UpdateEnergy(
            EpanetNetwork net,
            int n,
            double c0,
            double f0,
            TimeSpan dt) {
            //Skip closed pumps
            if (SimStatus <= StatType.CLOSED) return 0.0;
            double q = Math.Max(Constants.QZERO, Math.Abs(SimFlow));

            // Find pump-specific energy cost
            double c = Ecost > 0.0 ? Ecost : c0;

            c *= Epat?[n % Epat.Count] ?? f0;

            // Find pump energy & efficiency
            GetFlowEnergy(net, out double power, out double efficiency);

            // Update pump's cumulative statistics
            _energy[0] = _energy[0] + dt.TotalHours;              // Time on-line
            _energy[1] = _energy[1] + efficiency * dt.TotalHours; // Effic.-hrs
            _energy[2] = _energy[2] + power / q * dt.TotalHours;  // kw/cfs-hrs
            _energy[3] = _energy[3] + power * dt.TotalHours;      // kw-hrs
            _energy[4] = Math.Max(_energy[4], power);
            _energy[5] = _energy[5] + c * power * dt.TotalHours;  // cost-hrs.

            return power;
        }

        /// <summary>Computes P and Y coeffs. for pump in the link.</summary>
        public void ComputePumpCoeff(EpanetNetwork net) {
            if (SimStatus <= StatType.CLOSED || SimSetting.IsZero()) {
                SimInvHeadLoss = 1.0 / Constants.CBIG;
                SimFlowCorrection = SimFlow;
                return;
            }

            
            double q = Math.Max(Math.Abs(SimFlow), Constants.TINY);

            if (Ptype == PumpType.CUSTOM) {
                Hcurve.GetCoeff(net.FieldsMap, q / SimSetting, out double hh0, out double rr);

                H0 = -hh0;
                FlowCoefficient = -rr;
                N = 1.0;
            }

            double h0 = SimSetting * SimSetting * H0;
            double n = N;
            double r = FlowCoefficient * Math.Pow(SimSetting, 2.0 - n);
            if (!n.EqualsTo(1.0)) r = n * r * Math.Pow(q, n - 1.0);

            SimInvHeadLoss = 1.0 / Math.Max(r, net.RQtol);
            SimFlowCorrection = SimFlow / n + SimInvHeadLoss * h0;
        }

        /// <summary>Get new pump status.</summary>
        /// <param name="net"></param>
        /// <param name="dh">head gain</param>
        /// <returns></returns>
        public StatType PumpStatus(EpanetNetwork net, double dh) {
            double hmax = Ptype == PumpType.CONST_HP 
                ? Constants.BIG
                : SimSetting * SimSetting * Hmax;

            return dh > hmax + net.HTol
                ? StatType.XHEAD 
                : StatType.OPEN;
        }

        /// <summary>Update pumps energy.</summary>
        public static double StepEnergy(
            EpanetNetwork net,
            Pattern epat,
            List<SimulationPump> pumps,
            TimeSpan htime,
            TimeSpan hstep) {
            
            TimeSpan dt;
            
            /* Determine current time interval in hours */
            if (net.Duration.IsZero())
                dt = TimeSpan.FromHours(1);
            else if (htime < net.Duration)
                dt = hstep;
            else
                dt = TimeSpan.Zero;

            if(dt.IsZero())
                return 0.0;

            int n = (int)((htime.Ticks + net.PStart.Ticks) / net.PStep.Ticks);

            /* Compute default energy cost at current time */
            double c0 = net.ECost;
            double f0 = epat?[n % epat.Count] ?? 1.0;

            double psum = 0.0;

            foreach (SimulationPump pump  in  pumps) {
                psum += pump.UpdateEnergy(net, n, c0, f0, dt);
            }

            /* Update maximum kw value */
            net.EMax = Math.Max(net.EMax, psum);

            return psum;
        }

        public override void SetLinkStatus(bool value) {
            if (value) {
                SimSetting = 1.0;
                SimStatus = StatType.OPEN;
            }
            else {
                SimSetting = 0.0;
                SimStatus = StatType.CLOSED;
            }
        }

    }

}