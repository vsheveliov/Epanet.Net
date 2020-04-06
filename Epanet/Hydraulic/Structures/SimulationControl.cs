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
using System.Diagnostics;

using Epanet.Enums;
using Epanet.Log;
using Epanet.Network;
using Epanet.Network.Structures;
using Epanet.Properties;
using Epanet.Util;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet.Hydraulic.Structures {

    public class SimulationControl {
        private readonly Control _control;

        public SimulationControl(IEnumerable<SimulationNode> nodes, IEnumerable<SimulationLink> links, Control @ref) {
            if (@ref.Node != null) {
                string nid = @ref.Node.Name;
                foreach (SimulationNode simulationNode  in  nodes) {
                    if (string.Equals(simulationNode.Id, nid, StringComparison.OrdinalIgnoreCase)) {
                        Node = simulationNode;
                        break;
                    }
                }
            }

            if (@ref.Link != null) {
                string linkId = @ref.Link.Name;
                foreach (SimulationLink simulationLink  in  links) {
                    if (string.Equals(simulationLink.Link.Name, linkId, StringComparison.OrdinalIgnoreCase)) {
                        Link = simulationLink;
                        break;
                    }
                }
            }

            _control = @ref;
        }

        public SimulationLink Link { get; }

        public SimulationNode Node { get; }

        public TimeSpan Time => _control.Time;

        public double Grade => _control.Grade;

        public double Setting => _control.Setting;

        public StatType Status => _control.Status;

        public ControlType Type => _control.Type;

        ///<summary>Get the shortest time step to activate the control.</summary>
        private TimeSpan GetRequiredTimeStep(EpanetNetwork net, TimeSpan htime, TimeSpan tstep)
        {

            TimeSpan t = TimeSpan.Zero;

            // Node control
            if (Node != null) {

                // Check if node is a tank
                SimulationTank tank = Node as SimulationTank;
                if (tank == null) return tstep;

                double h = Node.SimHead; // Current tank grade
                double q = Node.SimDemand; // Flow into tank

                if (Math.Abs(q) <= Constants.QZERO)
                    return tstep;

                if (h < Grade && Type == ControlType.HILEVEL && q > 0.0 || // Tank below hi level & filling
                    h > Grade && Type == ControlType.LOWLEVEL && q < 0.0   // Tank above low level & emptying
                    )
                    
                {
                    double v = tank.FindVolume(net.FieldsMap, Grade) - tank.SimVolume;
                    t = TimeSpan.FromSeconds(Math.Round(v / q)); // Time to reach level
                }
            }

            
            switch (Type) {
                case ControlType.TIMER:
                    // Time control
                    if (Time > htime) t = Time - htime;
                    break;

                case ControlType.TIMEOFDAY:
                    // Time-of-day control
                    TimeSpan timeOfDay = (htime + net.Tstart).TimeOfDay();
                    if (Time >= timeOfDay) t = Time - timeOfDay;
                    else t = TimeSpan.FromDays(1) - timeOfDay + Time;
                    break;
            }

            
            // Revise time step
            if (t > TimeSpan.Zero && t < tstep) {
                // Check if rule actually changes link status or setting
                if (Link == null) return tstep;

                if (Link.LinkType > LinkType.PIPE &&
                    !Link.SimSetting.EqualsTo(Setting) ||
                    Link.SimStatus != Status)
                {
                    tstep = t;
                }
            }

            return tstep;
        }

        /// <summary>Revises time step based on shortest time to fill or drain a tank.</summary>
        public static TimeSpan MinimumTimeStep(
            EpanetNetwork net,
            IEnumerable<SimulationControl> controls,
            TimeSpan htime,
            TimeSpan tstep) {

            TimeSpan newTStep = tstep;

            foreach (SimulationControl control  in  controls)
                newTStep = control.GetRequiredTimeStep(net, htime, newTStep);

            return newTStep;
        }


        /// <summary>Implements simple controls based on time or tank levels.</summary>
        public static int StepActions(
            TraceSource log,
            EpanetNetwork net,
            IEnumerable<SimulationControl> controls,
            TimeSpan htime) {
            int setsum = 0;

            // Examine each control statement
            foreach (SimulationControl control  in  controls) {
                bool reset = false;

                // Make sure that link is defined
                if (control.Link == null)
                    continue;

                // Link is controlled by tank level
                if (control.Node is SimulationTank node) {

                    double h = node.SimHead;
                    double vplus = Math.Abs(node.SimDemand);

                    SimulationTank tank = node;

                    double v1 = tank.FindVolume(net.FieldsMap, h);
                    double v2 = tank.FindVolume(net.FieldsMap, control.Grade);

                    switch (control.Type) {
                        case ControlType.LOWLEVEL:
                            if (v1 <= v2 + vplus) reset = true;
                            break;
                        case ControlType.HILEVEL:
                            if (v1 >= v2 - vplus) reset = true;
                            break;
                    }
                }

                switch (control.Type) {
                    case ControlType.TIMER:
                        // Link is time-controlled
                        if (control.Time == htime)
                            reset = true;

                        break;

                    case ControlType.TIMEOFDAY:
                        //  Link is time-of-day controlled
                        if ((htime + net.Tstart).TimeOfDay() == control.Time)
                            reset = true;

                        break;
                }

                // Update link status & pump speed or valve setting
                if (reset) {
                    SimulationLink link = control.Link;
                    StatType s1 = link.SimStatus <= StatType.CLOSED ? StatType.CLOSED : StatType.OPEN;
                    StatType s2 = control.Status;

                    double k1 = link.SimSetting;
                    double k2 = k1;

                    if (control.Link.LinkType > LinkType.PIPE)
                        k2 = control.Setting;

                    if (s1 != s2 || !k1.EqualsTo(k2)) {
                        link.SimStatus = s2;
                        link.SimSetting = k2;
                        if (net.StatFlag != StatFlag.NO)
                            LogControlAction(log, control, htime);
                        setsum++;
                    }
                }
            }

            return setsum;
        }


        /// <summary>Adjusts settings of links controlled by junction pressures after a hydraulic solution is found.</summary>
        public static bool PSwitch(
            TraceSource log,
            EpanetNetwork net,
            IEnumerable<SimulationControl> controls) {
            bool anychange = false;

            foreach (SimulationControl control  in  controls) {
                bool reset = false;
                if (control.Link == null)
                    continue;

                // Determine if control based on a junction, not a tank
                if (control.Node != null && !(control.Node is SimulationTank)) {
                    switch (control.Type) {
                        // Determine if control conditions are satisfied
                        case ControlType.LOWLEVEL: {
                            if (control.Node.SimHead <= control.Grade + net.HTol) reset = true;
                            break;
                        }
                        case ControlType.HILEVEL: {
                            if (control.Node.SimHead >= control.Grade - net.HTol) reset = true;
                            break;
                        }
                    }
                }

                SimulationLink link = control.Link;

                //  Determine if control forces a status or setting change
                if (!reset) continue;

                bool change = false;

                StatType s = link.SimStatus;

                switch (link.LinkType) {
                    case LinkType.PIPE:
                        if (s != control.Status) change = true;
                        break;

                    case LinkType.PUMP:
                        if (!link.SimSetting.EqualsTo(control.Setting)) change = true;
                        break;

                    case LinkType.VALVE:
                        if (link.SimSetting.EqualsTo(control.Setting))
                            change = true;
                        else if (double.IsNaN(link.SimSetting) &&
                                 s != control.Status) change = true;
                        break;
                }

                // If a change occurs, update status & setting
                if (change) {
                    link.SimStatus = control.Status;
                    if (link.LinkType > LinkType.PIPE)
                        link.SimSetting = control.Setting;
                    if (net.StatFlag == StatFlag.FULL)
                        LogStatChange(log, net.FieldsMap, link, s);

                    anychange = true;
                }
            }

            return anychange;
        }

        private static void LogControlAction(TraceSource log, SimulationControl control, TimeSpan htime) {
            SimulationNode n = control.Node;
            SimulationLink l = control.Link;
            string msg;
            switch (control.Type) {

            case ControlType.LOWLEVEL:
            case ControlType.HILEVEL: {
                string type = Keywords.w_JUNC; //  NodeType type= NodeType.JUNC;
                if (n is SimulationTank tank) type = tank.IsReservoir ? Keywords.w_RESERV : Keywords.w_TANK;

                msg = string.Format(
                    Text.FMT54,
                    htime.GetClockTime(),
                    l.LinkType.Keyword2(),
                    l.Link.Name,
                    type,
                    n.Id);
                break;
            }
            case ControlType.TIMER:
            case ControlType.TIMEOFDAY:
                msg = string.Format(
                    Text.FMT55,
                    htime.GetClockTime(),
                    l.LinkType.Keyword2(),
                    l.Link.Name);
                break;
            default:
                return;
            }
            log.Warning(msg);
        }

        private static void LogStatChange(TraceSource log, FieldsMap fMap, SimulationLink link, StatType oldstatus) {
            StatType s1 = oldstatus;
            StatType s2 = link.SimStatus;
            try {
                if (s2 == s1) {
                    double setting = link.SimSetting;

                    if (link.LinkType == LinkType.VALVE) {
                        switch (((Valve)link.Link).ValveType) {
                            case ValveType.PRV:
                            case ValveType.PSV:
                            case ValveType.PBV:
                                setting *= fMap.GetUnits(FieldType.PRESSURE);
                                break;
                            case ValveType.FCV:
                                setting *= fMap.GetUnits(FieldType.FLOW);
                                break;
                        }
                    }

                    log.Warning(Text.FMT56, link.LinkType.Keyword2(), link.Link.Name, setting);
                    return;
                }

                StatType j1, j2;

                if (s1 == StatType.ACTIVE)
                    j1 = StatType.ACTIVE;
                else if (s1 <= StatType.CLOSED)
                    j1 = StatType.CLOSED;
                else
                    j1 = StatType.OPEN;
                if (s2 == StatType.ACTIVE) j2 = StatType.ACTIVE;
                else if (s2 <= StatType.CLOSED)
                    j2 = StatType.CLOSED;
                else
                    j2 = StatType.OPEN;

                if (j1 != j2) {
                    log.Warning(Text.FMT57, link.LinkType.Keyword2(), link.Link.Name, j1.ReportStr(), j2.ReportStr());
                }
            }
            catch (EnException) {}
        }
    }

}