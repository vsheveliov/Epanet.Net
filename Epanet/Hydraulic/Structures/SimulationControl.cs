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
        private readonly SimulationLink _link;
        private readonly SimulationNode _node;

        public SimulationControl(IEnumerable<SimulationNode> nodes, IEnumerable<SimulationLink> links, Control @ref) {
            if (@ref.Node != null) {
                string nid = @ref.Node.Name;
                foreach (SimulationNode simulationNode  in  nodes) {
                    if (simulationNode.Id.Equals(nid, StringComparison.OrdinalIgnoreCase)) {
                        _node = simulationNode;
                        break;
                    }
                }
            }

            if (@ref.Link != null) {
                string linkId = @ref.Link.Name;
                foreach (SimulationLink simulationLink  in  links) {
                    if (simulationLink.Link.Name.Equals(linkId, StringComparison.OrdinalIgnoreCase)) {
                        _link = simulationLink;
                        break;
                    }
                }
            }

            _control = @ref;
        }

        public SimulationLink Link { get { return _link; } }

        public SimulationNode Node { get { return _node; } }

        public long Time { get { return _control.Time; } }

        public double Grade { get { return _control.Grade; } }

        public double Setting { get { return _control.Setting; } }

        public StatType Status { get { return _control.Status; } }

        public ControlType Type { get { return _control.Type; } }

        ///<summary>Get the shortest time step to activate the control.</summary>
        private long GetRequiredTimeStep(EpanetNetwork net, long htime, long tstep) {

            long t = 0;

            // Node control
            if (Node != null) {

                if (!(Node is SimulationTank)) // Check if node is a tank
                    return tstep;

                double h = _node.SimHead; // Current tank grade
                double q = _node.SimDemand; // Flow into tank

                if (Math.Abs(q) <= Constants.QZERO)
                    return tstep;

                if ((h < Grade && Type == ControlType.HILEVEL && q > 0.0) // Tank below hi level & filling
                    || (h > Grade && Type == ControlType.LOWLEVEL && q < 0.0))
                    // Tank above low level & emptying
                {
                    SimulationTank tank = (SimulationTank)Node;
                    double v = tank.FindVolume(net.FieldsMap, Grade) - tank.SimVolume;
                    t = (long)Math.Round(v / q); // Time to reach level
                }
            }

            // Time control
            if (Type == ControlType.TIMER) {
                if (Time > htime)
                    t = Time - htime;
            }

            // Time-of-day control
            if (Type == ControlType.TIMEOFDAY) {
                long t1 = (htime + net.Tstart) % Constants.SECperDAY;
                long t2 = Time;
                if (t2 >= t1) t = t2 - t1;
                else t = Constants.SECperDAY - t1 + t2;
            }

            // Revise time step
            if (t > 0 && t < tstep) {
                SimulationLink link = Link;

                // Check if rule actually changes link status or setting
                if (link != null
                    && (link.Type > LinkType.PIPE && link.SimSetting != Setting)
                    || (link.SimStatus != Status))
                    tstep = t;
            }

            return tstep;
        }

        /// <summary>Revises time step based on shortest time to fill or drain a tank.</summary>
        public static long MinimumTimeStep(
            EpanetNetwork net,
            IEnumerable<SimulationControl> controls,
            long htime,
            long tstep) {
            long newTStep = tstep;
            foreach (SimulationControl control  in  controls)
                newTStep = control.GetRequiredTimeStep(net, htime, newTStep);

            return newTStep;
        }


        /// <summary>Implements simple controls based on time or tank levels.</summary>
        public static int StepActions(
            TraceSource log,
            EpanetNetwork net,
            IEnumerable<SimulationControl> controls,
            long htime) {
            int setsum = 0;

            // Examine each control statement
            foreach (SimulationControl control  in  controls) {
                bool reset = false;

                // Make sure that link is defined
                if (control.Link == null)
                    continue;

                // Link is controlled by tank level
                var node = control.Node as SimulationTank;
                if (node != null) {

                    double h = node.SimHead;
                    double vplus = Math.Abs(node.SimDemand);

                    SimulationTank tank = node;

                    double v1 = tank.FindVolume(net.FieldsMap, h);
                    double v2 = tank.FindVolume(net.FieldsMap, control.Grade);

                    if (control.Type == ControlType.LOWLEVEL && v1 <= v2 + vplus)
                        reset = true;
                    if (control.Type == ControlType.HILEVEL && v1 >= v2 - vplus)
                        reset = true;
                }

                // Link is time-controlled
                if (control.Type == ControlType.TIMER) {
                    if (control.Time == htime)
                        reset = true;
                }

                //  Link is time-of-day controlled
                if (control.Type == ControlType.TIMEOFDAY) {
                    if ((htime + net.Tstart) % Constants.SECperDAY == control.Time)
                        reset = true;
                }

                // Update link status & pump speed or valve setting
                if (reset) {
                    StatType s1, s2;
                    SimulationLink link = control.Link;

                    if (link.SimStatus <= StatType.CLOSED)
                        s1 = StatType.CLOSED;
                    else
                        s1 = StatType.OPEN;

                    s2 = control.Status;

                    double k1 = link.SimSetting;
                    double k2 = k1;

                    if (control.Link.Type > LinkType.PIPE)
                        k2 = control.Setting;

                    if (s1 != s2 || k1 != k2) {
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

                    // Determine if control conditions are satisfied
                    if (control.Type == ControlType.LOWLEVEL
                        && control.Node.SimHead <= control.Grade + net.HTol)
                        reset = true;

                    if (control.Type == ControlType.HILEVEL
                        && control.Node.SimHead >= control.Grade - net.HTol)
                        reset = true;
                }

                SimulationLink link = control.Link;

                //  Determine if control forces a status or setting change
                if (reset) {
                    bool change = false;

                    StatType s = link.SimStatus;

                    if (link.Type == LinkType.PIPE) {
                        if (s != control.Status) change = true;
                    }

                    if (link.Type == LinkType.PUMP) {
                        if (link.SimSetting != control.Setting) change = true;
                    }

                    if (link.Type >= LinkType.PRV) {
                        if (link.SimSetting != control.Setting)
                            change = true;
                        else if (link.SimSetting.IsMissing() &&
                                 s != control.Status) change = true;
                    }

                    // If a change occurs, update status & setting
                    if (change) {
                        link.SimStatus = control.Status;
                        if (link.Type > LinkType.PIPE)
                            link.SimSetting = control.Setting;
                        if (net.StatFlag == StatFlag.FULL)
                            LogStatChange(log, net.FieldsMap, link, s);

                        anychange = true;
                    }
                }
            }
            return anychange;
        }

        private static void LogControlAction(TraceSource log, SimulationControl control, long htime) {
            SimulationNode n = control.Node;
            SimulationLink l = control.Link;
            string msg;
            switch (control.Type) {

            case ControlType.LOWLEVEL:
            case ControlType.HILEVEL: {
                string type = Keywords.w_JUNC; //  NodeType type= NodeType.JUNC;
                    var tank = n as SimulationTank;
                    if (tank != null)
                        type = tank.IsReservoir ? Keywords.w_RESERV : Keywords.w_TANK;

                    msg = string.Format(
                    Text.FMT54,
                    htime.GetClockTime(),
                    l.Type.ParseStr(),
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
                    l.Type.ParseStr(),
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
                    switch (link.Type) {
                    case LinkType.PRV:
                    case LinkType.PSV:
                    case LinkType.PBV:
                        setting *= fMap.GetUnits(FieldType.PRESSURE);
                        break;
                    case LinkType.FCV:
                        setting *= fMap.GetUnits(FieldType.FLOW);
                        break;
                    }

                    log.Warning(
                        string.Format(
                            Text.FMT56,
                            link.Type.ParseStr(),
                            link.Link.Name,
                            setting));
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
                    log.Warning(
                        Text.FMT57,
                        link.Type.ParseStr(),
                        link.Link.Name,
                        j1.ReportStr(),
                        j2.ReportStr());
                }
            }
            catch (ENException) {}
        }
    }

}