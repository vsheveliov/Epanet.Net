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

        public PumpType Ptype { get { return ((Pump)link).Ptype; } }

        public double Q0 { get { return ((Pump)link).Q0; } }

        public double Qmax { get { return ((Pump)link).Qmax; } }

        public double Hmax { get { return ((Pump)link).Hmax; } }

        public Curve Hcurve { get { return ((Pump)link).HCurve; } }

        public Curve Ecurve { get { return ((Pump)link).ECurve; } }

        public Pattern Upat { get { return ((Pump)link).UPat; } }

        public Pattern Epat { get { return ((Pump)link).EPat; } }

        public double Ecost { get { return ((Pump)link).ECost; } }

        // Simulation getters and setters
        public double[] Energy { get { return _energy; } }

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

            if (status <= StatType.CLOSED) {
                return;
            }

            double q = Math.Abs(flow);
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
            long n,
            double c0,
            double f0,
            double dt) {
            //Skip closed pumps
            if (status <= StatType.CLOSED) return 0.0;
            double q = Math.Max(Constants.QZERO, Math.Abs(flow));

            // Find pump-specific energy cost
            double c = Ecost > 0.0 ? Ecost : c0;

            if (Epat != null) {
                int m = (int)(n % Epat.Count);
                c *= Epat[m];
            }
            else
                c *= f0;

            // Find pump energy & efficiency
            double power, efficiency;
            GetFlowEnergy(net, out power, out efficiency);

            // Update pump's cumulative statistics
            _energy[0] = _energy[0] + dt; // Time on-line
            _energy[1] = _energy[1] + efficiency * dt; // Effic.-hrs
            _energy[2] = _energy[2] + power / q * dt; // kw/cfs-hrs
            _energy[3] = _energy[3] + power * dt; // kw-hrs
            _energy[4] = Math.Max(_energy[4], power);
            _energy[5] = _energy[5] + c * power * dt; // cost-hrs.

            return power;
        }

        /// <summary>Computes P and Y coeffs. for pump in the link.</summary>
        public void ComputePumpCoeff(EpanetNetwork net) {
            if (status <= StatType.CLOSED || setting == 0.0) {
                invHeadLoss = 1.0 / Constants.CBIG;
                flowCorrection = flow;
                return;
            }

            
            double q = Math.Max(Math.Abs(flow), Constants.TINY);

            if (Ptype == PumpType.CUSTOM) {
                double hh0, rr;
                Hcurve.GetCoeff(net.FieldsMap, q / setting, out hh0, out rr);

                H0 = -hh0;
                FlowCoefficient = -rr;
                N = 1.0;
            }

            double h0 = setting * setting * H0;
            double n = N;
            double r = FlowCoefficient * Math.Pow(setting, 2.0 - n);
            if (n != 1.0) r = n * r * Math.Pow(q, n - 1.0);

            invHeadLoss = 1.0 / Math.Max(r, net.RQtol);
            flowCorrection = flow / n + invHeadLoss * h0;
        }

        /// <summary>Get new pump status.</summary>
        /// <param name="net"></param>
        /// <param name="dh">head gain</param>
        /// <returns></returns>
        public StatType PumpStatus(EpanetNetwork net, double dh) {
            double hmax = Ptype == PumpType.CONST_HP 
                ? Constants.BIG
                : setting * setting * Hmax;

            return dh > hmax + net.HTol
                ? StatType.XHEAD 
                : StatType.OPEN;
        }

        /// <summary>Update pumps energy.</summary>
        public static double StepEnergy(
            EpanetNetwork net,
            Pattern epat,
            List<SimulationPump> pumps,
            long htime,
            long hstep) {
            double dt, psum = 0.0;


            if (net.Duration == 0)
                dt = 1.0;
            else if (htime < net.Duration)
                dt = hstep / 3600.0;
            else
                dt = 0.0;

            if (dt == 0.0)
                return 0.0;

            long n = (htime + net.PStart) / net.PStep;


            double c0 = net.ECost;
            double f0 = 1.0;

            if (epat != null) {
                long m = n % epat.Count;
                f0 = epat[(int)m];
            }

            foreach (SimulationPump pump  in  pumps) {
                psum += pump.UpdateEnergy(net, n, c0, f0, dt);
            }

            return psum;
        }


    }

}