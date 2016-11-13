/*
 * Copyright (C) 2012  Addition, Lda. (addition at addition dot pt)
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
using org.addition.epanet.log;
using org.addition.epanet.network;
using org.addition.epanet.network.io;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.hydraulic.structures {

public class SimulationControl {
    private readonly Control control;
    private  SimulationLink link;
    private SimulationNode node=null;

    public SimulationControl(List<SimulationNode> nodes, List<SimulationLink> links, Control @ref) {
        if (@ref.getNode() != null) {
            string nid = @ref.getNode().getId();
            foreach (SimulationNode simulationNode  in  nodes) {
                if (simulationNode.getId().Equals(nid, StringComparison.OrdinalIgnoreCase)) {
                    node = simulationNode;
                    break;
                }
            }
        }

        if (@ref.getLink() != null)
        {
            string linkId = @ref.getLink().getId();
            foreach (SimulationLink simulationLink  in  links) {
                if(simulationLink.getLink().getId().Equals(linkId, StringComparison.OrdinalIgnoreCase))
                {
                    link = simulationLink;break;
                }
            }
        }

        control = @ref;
    }

    public SimulationLink getLink() {
        return link;
    }

    public SimulationNode getNode() {
        return node;
    }


    public long getTime() {
        return control.getTime();
    }


    public double getGrade() {
        return control.getGrade();
    }


    public double getSetting() {
        return control.getSetting();
    }


    public Link.StatType getStatus() {
        return control.getStatus();
    }

    public Control.ControlType getType() {
        return control.getType();
    }

    /**
     * Get the shortest time step to activate the control.
     *
     * @param fMap
     * @param pMap
     * @param htime
     * @param tstep
     * @return
     * @throws ENException
     */
    private long getRequiredTimeStep(FieldsMap fMap, PropertiesMap pMap, long htime, long tstep) {

        long t = 0;

        // Node control
        if (getNode() != null) {

            if (!(getNode() is SimulationTank)) // Check if node is a tank
                return tstep;

            double h = node.getSimHead();           // Current tank grade
            double q = node.getSimDemand();         // Flow into tank

            if (Math.Abs(q) <= Constants.QZERO)
                return tstep;

            if ((h < getGrade() && getType() == Control.ControlType.HILEVEL && q > 0.0)  // Tank below hi level & filling
                    || (h > getGrade() && getType() == Control.ControlType.LOWLEVEL && q < 0.0)) // Tank above low level & emptying
            {
                SimulationTank tank = ((SimulationTank) getNode());
                double v = tank.findVolume(fMap, getGrade()) - tank.getSimVolume();
                t = (long)Math.Round(v / q); // Time to reach level
            }
        }

        // Time control
        if (getType() == Control.ControlType.TIMER) {
            if (getTime() > htime)
                t = getTime() - htime;
        }

        // Time-of-day control
        if (getType() == Control.ControlType.TIMEOFDAY) {
            long t1 = (htime + pMap.getTstart()) % Constants.SECperDAY;
            long t2 = getTime();
            if (t2 >= t1) t = t2 - t1;
            else t = Constants.SECperDAY - t1 + t2;
        }

        // Revise time step
        if (t > 0 && t < tstep) {
            SimulationLink link = getLink();

            // Check if rule actually changes link status or setting
            if (link != null && (link.getType() > Link.LinkType.PIPE && link.getSimSetting() != getSetting())
                    || (link.getSimStatus() != getStatus()))
                tstep = t;
        }

        return tstep;
    }

    // Revises time step based on shortest time to fill or drain a tank
    public static long minimumTimeStep(FieldsMap fMap, PropertiesMap pMap, List<SimulationControl> controls,
                                       long htime, long tstep) {
        long newTStep = tstep;
        foreach (SimulationControl control  in  controls)
            newTStep = control.getRequiredTimeStep(fMap, pMap, htime, newTStep);
        return newTStep;
    }


    // Implements simple controls based on time or tank levels
    public static int stepActions(TraceSource log,
                                  FieldsMap fMap,
                                  PropertiesMap pMap,
                                  List<SimulationControl> controls,
                                  long htime) {
        int setsum = 0;

        // Examine each control statement
        foreach (SimulationControl control  in  controls) {
            bool reset = false;

            // Make sure that link is defined
            if (control.getLink() == null)
                continue;

            // Link is controlled by tank level
            if (control.getNode() != null && control.getNode() is SimulationTank) {

                double h = control.getNode().getSimHead();
                double vplus = Math.Abs(control.getNode().getSimDemand());

                SimulationTank tank = (SimulationTank) control.getNode();

                double v1 = tank.findVolume(fMap, h);
                double v2 = tank.findVolume(fMap, control.getGrade());

                if (control.getType() == Control.ControlType.LOWLEVEL && v1 <= v2 + vplus)
                    reset = true;
                if (control.getType() == Control.ControlType.HILEVEL && v1 >= v2 - vplus)
                    reset = true;
            }

            // Link is time-controlled
            if (control.getType() == Control.ControlType.TIMER) {
                if (control.getTime() == htime)
                    reset = true;
            }

            //  Link is time-of-day controlled
            if (control.getType() == Control.ControlType.TIMEOFDAY) {
                if ((htime + pMap.getTstart()) % Constants.SECperDAY == control.getTime())
                    reset = true;
            }

            // Update link status & pump speed or valve setting
            if (reset) {
                Link.StatType s1, s2;
                SimulationLink link = control.getLink();

                if (link.getSimStatus() <= Link.StatType.CLOSED)
                    s1 = Link.StatType.CLOSED;
                else
                    s1 = Link.StatType.OPEN;

                s2 = control.getStatus();

                double k1 = link.getSimSetting();
                double k2 = k1;

                if (control.getLink().getType() > Link.LinkType.PIPE)
                    k2 = control.getSetting();

                if (s1 != s2 || k1 != k2) {
                    link.setSimStatus(s2);
                    link.setSimSetting(k2);
                    if (pMap.getStatflag() != null)
                        logControlAction(log, control, htime);
                    setsum++;
                }
            }
        }

        return (setsum);
    }


    // Adjusts settings of links controlled by junction pressures after a hydraulic solution is found
    public static bool pSwitch(TraceSource log, PropertiesMap pMap, FieldsMap fMap, List<SimulationControl> controls) {
        bool anychange = false;

        foreach (SimulationControl control  in  controls) {
            bool reset = false;
            if (control.getLink() == null)
                continue;

            // Determine if control based on a junction, not a tank
            if (control.getNode() != null && !(control.getNode() is SimulationTank)) {

                // Determine if control conditions are satisfied
                if (control.getType() == Control.ControlType.LOWLEVEL && control.getNode().getSimHead() <= control.getGrade() + pMap.getHtol())
                    reset = true;

                if (control.getType() == Control.ControlType.HILEVEL && control.getNode().getSimHead() >= control.getGrade() - pMap.getHtol())
                    reset = true;
            }

            SimulationLink link = control.getLink();

            //  Determine if control forces a status or setting change
            if (reset) {
                bool change = false;

                Link.StatType s = link.getSimStatus();

                if (link.getType() == Link.LinkType.PIPE) {
                    if (s != control.getStatus()) change = true;
                }

                if (link.getType() == Link.LinkType.PUMP) {
                    if (link.getSimSetting() != control.getSetting()) change = true;
                }

                if (link.getType() >= Link.LinkType.PRV) {
                    if (link.getSimSetting() != control.getSetting())
                        change = true;
                    else if (link.getSimSetting() == Constants.MISSING &&
                            s != control.getStatus()) change = true;
                }

                // If a change occurs, update status & setting
                if (change) {
                    link.setSimStatus(control.getStatus());
                    if (link.getType() > Link.LinkType.PIPE)
                        link.setSimSetting(control.getSetting());
                    if (pMap.getStatflag() == PropertiesMap.StatFlag.FULL)
                        logStatChange(log, fMap, link, s);

                    anychange = true;
                }
            }
        }
        return (anychange);
    }

    private static void logControlAction(TraceSource log, SimulationControl control, long Htime) {
        SimulationNode n = control.getNode();
        SimulationLink l = control.getLink();
        string Msg = "";
        switch (control.getType()) {

            case Control.ControlType.LOWLEVEL:
            case Control.ControlType.HILEVEL: {
                string type = Keywords.w_JUNC;//  NodeType type= NodeType.JUNC;
                if (n is SimulationTank) {
                    type = ((SimulationTank) n).isReservoir() ? Keywords.w_RESERV : Keywords.w_TANK;
                }
                Msg = string.Format(Utilities.getText("FMT54"), Htime.getClockTime(), l.getType().ParseStr(),
                        l.getLink().getId(), type, n.getId());
                break;
            }
            case Control.ControlType.TIMER:
            case Control.ControlType.TIMEOFDAY:
                Msg = string.Format(Utilities.getText("FMT55"), Htime.getClockTime(), l.getType().ParseStr(),
                        l.getLink().getId());
                break;
            default:
                return;
        }
        log.Warning(Msg);
    }

    private static void logStatChange(TraceSource log, FieldsMap fMap, SimulationLink link, Link.StatType oldstatus) {
        Link.StatType s1 = oldstatus;
        Link.StatType s2 = link.getSimStatus();
        try {
            if (s2 == s1) {
                double setting = link.getSimSetting();
                switch (link.getType()) {
                    case Link.LinkType.PRV:
                    case Link.LinkType.PSV:
                    case Link.LinkType.PBV:
                        setting *= fMap.getUnits(FieldsMap.Type.PRESSURE);
                        break;
                    case Link.LinkType.FCV:
                        setting *= fMap.getUnits(FieldsMap.Type.FLOW);
                        break;
                }

                log.Warning(string.Format(Utilities.getText("FMT56"), link.getType().ParseStr(), link.getLink().getId(), setting));
                return;
            }

            Link.StatType j1, j2;

            if (s1 == Link.StatType.ACTIVE)
                j1 = Link.StatType.ACTIVE;
            else if (s1 <= Link.StatType.CLOSED)
                j1 = Link.StatType.CLOSED;
            else
                j1 = Link.StatType.OPEN;
            if (s2 == Link.StatType.ACTIVE) j2 = Link.StatType.ACTIVE;
            else if (s2 <= Link.StatType.CLOSED)
                j2 = Link.StatType.CLOSED;
            else
                j2 = Link.StatType.OPEN;

            if (j1 != j2) {
                log.Warning(Utilities.getText("FMT57"), link.getType().ParseStr(),
                        link.getLink().getId(), j1.ReportStr(), j2.ReportStr());
            }
        } catch (ENException e) {
        }
    }
}
}