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
using Epanet.Log;
using Epanet.Network;
using Epanet.Network.IO;
using Epanet.Network.Structures;
using Epanet.Properties;
using Epanet.Util;

namespace Epanet.Hydraulic.Structures {

    public class SimulationControl {
        private readonly Control control;
        private readonly SimulationLink link;
        private readonly SimulationNode node;

        public SimulationControl(List<SimulationNode> nodes, List<SimulationLink> links, Control @ref) {
            if (@ref.Node != null) {
                string nid = @ref.Node.Id;
                foreach (SimulationNode simulationNode  in  nodes) {
                    if (simulationNode.Id.Equals(nid, StringComparison.OrdinalIgnoreCase)) {
                        this.node = simulationNode;
                        break;
                    }
                }
            }

            if (@ref.Link != null) {
                string linkId = @ref.Link.Id;
                foreach (SimulationLink simulationLink  in  links) {
                    if (simulationLink.Link.Id.Equals(linkId, StringComparison.OrdinalIgnoreCase)) {
                        this.link = simulationLink;
                        break;
                    }
                }
            }

            this.control = @ref;
        }

        public SimulationLink Link { get { return this.link; } }

        public SimulationNode Node { get { return this.node; } }

        public long Time { get { return this.control.Time; } }

        public double Grade { get { return this.control.Grade; } }

        public double Setting { get { return this.control.Setting; } }

        public Link.StatType Status { get { return this.control.Status; } }

        public Control.ControlType Type { get { return this.control.Type; } }

        ///<summary>Get the shortest time step to activate the control.</summary>
        private long GetRequiredTimeStep(FieldsMap fMap, PropertiesMap pMap, long htime, long tstep) {

            long t = 0;

            // Node control
            if (this.Node != null) {

                if (!(this.Node is SimulationTank)) // Check if node is a tank
                    return tstep;

                double h = this.node.SimHead; // Current tank grade
                double q = this.node.SimDemand; // Flow into tank

                if (Math.Abs(q) <= Constants.QZERO)
                    return tstep;

                if ((h < this.Grade && this.Type == Control.ControlType.HILEVEL && q > 0.0) // Tank below hi level & filling
                    || (h > this.Grade && this.Type == Control.ControlType.LOWLEVEL && q < 0.0))
                    // Tank above low level & emptying
                {
                    SimulationTank tank = ((SimulationTank)this.Node);
                    double v = tank.FindVolume(fMap, this.Grade) - tank.SimVolume;
                    t = (long)Math.Round(v / q); // Time to reach level
                }
            }

            // Time control
            if (this.Type == Control.ControlType.TIMER) {
                if (this.Time > htime)
                    t = this.Time - htime;
            }

            // Time-of-day control
            if (this.Type == Control.ControlType.TIMEOFDAY) {
                long t1 = (htime + pMap.TStart) % Constants.SECperDAY;
                long t2 = this.Time;
                if (t2 >= t1) t = t2 - t1;
                else t = Constants.SECperDAY - t1 + t2;
            }

            // Revise time step
            if (t > 0 && t < tstep) {
                SimulationLink link = this.Link;

                // Check if rule actually changes link status or setting
                if (link != null
                    && (link.Type > Network.Structures.Link.LinkType.PIPE && link.SimSetting != this.Setting)
                    || (link.SimStatus != this.Status))
                    tstep = t;
            }

            return tstep;
        }

        /// <summary>Revises time step based on shortest time to fill or drain a tank.</summary>
        public static long MinimumTimeStep(
            FieldsMap fMap,
            PropertiesMap pMap,
            List<SimulationControl> controls,
            long htime,
            long tstep) {
            long newTStep = tstep;
            foreach (SimulationControl control  in  controls)
                newTStep = control.GetRequiredTimeStep(fMap, pMap, htime, newTStep);
            return newTStep;
        }


        /// <summary>Implements simple controls based on time or tank levels.</summary>
        public static int StepActions(
            TraceSource log,
            FieldsMap fMap,
            PropertiesMap pMap,
            List<SimulationControl> controls,
            long htime) {
            int setsum = 0;

            // Examine each control statement
            foreach (SimulationControl control  in  controls) {
                bool reset = false;

                // Make sure that link is defined
                if (control.Link == null)
                    continue;

                // Link is controlled by tank level
                if (control.Node != null && control.Node is SimulationTank) {

                    double h = control.Node.SimHead;
                    double vplus = Math.Abs(control.Node.SimDemand);

                    SimulationTank tank = (SimulationTank)control.Node;

                    double v1 = tank.FindVolume(fMap, h);
                    double v2 = tank.FindVolume(fMap, control.Grade);

                    if (control.Type == Control.ControlType.LOWLEVEL && v1 <= v2 + vplus)
                        reset = true;
                    if (control.Type == Control.ControlType.HILEVEL && v1 >= v2 - vplus)
                        reset = true;
                }

                // Link is time-controlled
                if (control.Type == Control.ControlType.TIMER) {
                    if (control.Time == htime)
                        reset = true;
                }

                //  Link is time-of-day controlled
                if (control.Type == Control.ControlType.TIMEOFDAY) {
                    if ((htime + pMap.TStart) % Constants.SECperDAY == control.Time)
                        reset = true;
                }

                // Update link status & pump speed or valve setting
                if (reset) {
                    Link.StatType s1, s2;
                    SimulationLink link = control.Link;

                    if (link.SimStatus <= Network.Structures.Link.StatType.CLOSED)
                        s1 = Network.Structures.Link.StatType.CLOSED;
                    else
                        s1 = Network.Structures.Link.StatType.OPEN;

                    s2 = control.Status;

                    double k1 = link.SimSetting;
                    double k2 = k1;

                    if (control.Link.Type > Network.Structures.Link.LinkType.PIPE)
                        k2 = control.Setting;

                    if (s1 != s2 || k1 != k2) {
                        link.SimStatus = s2;
                        link.SimSetting = k2;
                        if (pMap.Stat_Flag != PropertiesMap.StatFlag.NO)
                            LogControlAction(log, control, htime);
                        setsum++;
                    }
                }
            }

            return (setsum);
        }


        /// <summary>Adjusts settings of links controlled by junction pressures after a hydraulic solution is found.</summary>
        public static bool PSwitch(
            TraceSource log,
            PropertiesMap pMap,
            FieldsMap fMap,
            List<SimulationControl> controls) {
            bool anychange = false;

            foreach (SimulationControl control  in  controls) {
                bool reset = false;
                if (control.Link == null)
                    continue;

                // Determine if control based on a junction, not a tank
                if (control.Node != null && !(control.Node is SimulationTank)) {

                    // Determine if control conditions are satisfied
                    if (control.Type == Control.ControlType.LOWLEVEL
                        && control.Node.SimHead <= control.Grade + pMap.HTol)
                        reset = true;

                    if (control.Type == Control.ControlType.HILEVEL
                        && control.Node.SimHead >= control.Grade - pMap.HTol)
                        reset = true;
                }

                SimulationLink link = control.Link;

                //  Determine if control forces a status or setting change
                if (reset) {
                    bool change = false;

                    Link.StatType s = link.SimStatus;

                    if (link.Type == Network.Structures.Link.LinkType.PIPE) {
                        if (s != control.Status) change = true;
                    }

                    if (link.Type == Network.Structures.Link.LinkType.PUMP) {
                        if (link.SimSetting != control.Setting) change = true;
                    }

                    if (link.Type >= Network.Structures.Link.LinkType.PRV) {
                        if (link.SimSetting != control.Setting)
                            change = true;
                        else if (link.SimSetting == Constants.MISSING &&
                                 s != control.Status) change = true;
                    }

                    // If a change occurs, update status & setting
                    if (change) {
                        link.SimStatus = control.Status;
                        if (link.Type > Network.Structures.Link.LinkType.PIPE)
                            link.SimSetting = control.Setting;
                        if (pMap.Stat_Flag == PropertiesMap.StatFlag.FULL)
                            LogStatChange(log, fMap, link, s);

                        anychange = true;
                    }
                }
            }
            return (anychange);
        }

        private static void LogControlAction(TraceSource log, SimulationControl control, long htime) {
            SimulationNode n = control.Node;
            SimulationLink l = control.Link;
            string msg;
            switch (control.Type) {

            case Control.ControlType.LOWLEVEL:
            case Control.ControlType.HILEVEL: {
                string type = Keywords.w_JUNC; //  NodeType type= NodeType.JUNC;
                if (n is SimulationTank) {
                    type = ((SimulationTank)n).IsReservoir ? Keywords.w_RESERV : Keywords.w_TANK;
                }
                msg = string.Format(
                    Text.ResourceManager.GetString("FMT54"),
                    htime.GetClockTime(),
                    l.Type.ParseStr(),
                    l.Link.Id,
                    type,
                    n.Id);
                break;
            }
            case Control.ControlType.TIMER:
            case Control.ControlType.TIMEOFDAY:
                msg = string.Format(
                    Text.ResourceManager.GetString("FMT55"),
                    htime.GetClockTime(),
                    l.Type.ParseStr(),
                    l.Link.Id);
                break;
            default:
                return;
            }
            log.Warning(msg);
        }

        private static void LogStatChange(TraceSource log, FieldsMap fMap, SimulationLink link, Link.StatType oldstatus) {
            Link.StatType s1 = oldstatus;
            Link.StatType s2 = link.SimStatus;
            try {
                if (s2 == s1) {
                    double setting = link.SimSetting;
                    switch (link.Type) {
                    case Network.Structures.Link.LinkType.PRV:
                    case Network.Structures.Link.LinkType.PSV:
                    case Network.Structures.Link.LinkType.PBV:
                        setting *= fMap.GetUnits(FieldsMap.FieldType.PRESSURE);
                        break;
                    case Network.Structures.Link.LinkType.FCV:
                        setting *= fMap.GetUnits(FieldsMap.FieldType.FLOW);
                        break;
                    }

                    log.Warning(
                        string.Format(
                            Text.ResourceManager.GetString("FMT56"),
                            link.Type.ParseStr(),
                            link.Link.Id,
                            setting));
                    return;
                }

                Link.StatType j1, j2;

                if (s1 == Network.Structures.Link.StatType.ACTIVE)
                    j1 = Network.Structures.Link.StatType.ACTIVE;
                else if (s1 <= Network.Structures.Link.StatType.CLOSED)
                    j1 = Network.Structures.Link.StatType.CLOSED;
                else
                    j1 = Network.Structures.Link.StatType.OPEN;
                if (s2 == Network.Structures.Link.StatType.ACTIVE) j2 = Network.Structures.Link.StatType.ACTIVE;
                else if (s2 <= Network.Structures.Link.StatType.CLOSED)
                    j2 = Network.Structures.Link.StatType.CLOSED;
                else
                    j2 = Network.Structures.Link.StatType.OPEN;

                if (j1 != j2) {
                    log.Warning(
                        Text.ResourceManager.GetString("FMT57"),
                        link.Type.ParseStr(),
                        link.Link.Id,
                        j1.ReportStr(),
                        j2.ReportStr());
                }
            }
            catch (ENException) {}
        }
    }

}