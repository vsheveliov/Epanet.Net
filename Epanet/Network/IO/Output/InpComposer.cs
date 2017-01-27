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
using System.IO;
using System.Linq;
using System.Text;

using Epanet.Enums;
using Epanet.Network.Structures;
using Epanet.Util;

namespace Epanet.Network.IO.Output {

    ///<summary>INP file composer.</summary>
    public class InpComposer:OutputComposer {

        private const string JUNCS_SUBTITLE = ";ID\tElev\tDemand\tPattern";
        private const string RESERVOIRS_SUBTITLE = ";ID\tHead\tPattern";
        private const string TANK_SUBTITLE = ";ID\tElevation\tInitLevel\tMinLevel\tMaxLevel\tDiameter\tMinVol\tVolCurve";
        private const string PUMPS_SUBTITLE = ";ID\tNode1\tNode2\tParameters";
        private const string VALVES_SUBTITLE = ";ID\tNode1\tNode2\tDiameter\tType\tSetting\tMinorLoss";
        private const string DEMANDS_SUBTITLE = ";Junction\tDemand\tPattern\tCategory";
        private const string STATUS_SUBTITLE = ";ID\tStatus/Setting";
        private const string PIPES_SUBTITLE = ";ID\tNode1\tNode2\tLength\tDiameter\tRoughness\tMinorLoss\tStatus";
        private const string PATTERNS_SUBTITLE = ";ID\tMultipliers";
        private const string EMITTERS_SUBTITLE = ";Junction\tCoefficient";
        private const string CURVE_SUBTITLE = ";ID\tX-Value\tY-Value";
        private const string QUALITY_SUBTITLE = ";Node\tInitQual";
        private const string SOURCE_SUBTITLE = ";Node\tType\tQuality\tPattern";
        private const string MIXING_SUBTITLE = ";Tank\tModel";
        private const string REACTIONS_SUBTITLE = ";Type\tPipe/Tank";
        private const string COORDINATES_SUBTITLE = ";Node\tX-Coord\tY-Coord";

        TextWriter buffer;

        public override void Composer(Network net, string fileName) {
            try {
                buffer = new StreamWriter(File.OpenWrite(fileName), Encoding.Default); // "ISO-8859-1"
                ComposeHeader(net);
                ComposeJunctions(net);
                ComposeReservoirs(net);
                ComposeTanks(net);
                ComposePipes(net);
                ComposePumps(net);
                ComposeValves(net);
                ComposeDemands(net);
                ComposeEmitters(net);
                ComposeStatus(net);
                ComposePatterns(net);
                ComposeCurves(net);
                ComposeControls(net);
                ComposeQuality(net);
                ComposeSource(net);
                ComposeMixing(net);
                ComposeReaction(net);
                ComposeEnergy(net);
                ComposeTimes(net);
                ComposeOptions(net);
                ComposeExtraOptions(net);
                ComposeReport(net);
                ComposeLabels(net);
                ComposeCoordinates(net);
                ComposeVertices(net);
                ComposeRules(net);

                buffer.WriteLine(SectType.END.ParseStr());
                buffer.Close();
            }
            catch (IOException) {}
        }

        private void ComposeHeader(Network net) {
            if (net.Title.Count == 0)
                return;

            buffer.WriteLine(SectType.TITLE.ParseStr());

            foreach (string str  in  net.Title) {
                buffer.WriteLine(str);
            }

            buffer.WriteLine();
        }


        private void ComposeJunctions(Network net) {
            FieldsMap fMap = net.FieldsMap;
            
            if (!net.Junctions.Any())
                return;

            buffer.WriteLine(SectType.JUNCTIONS.ParseStr());
            buffer.WriteLine(JUNCS_SUBTITLE);

            foreach (Node node in net.Junctions) {
                buffer.Write(" {0}\t{1}", node.Name, fMap.RevertUnit(FieldType.ELEV, node.Elevation));

                //if(node.getDemand()!=null && node.getDemand().size()>0 && !node.getDemand()[0].getPattern().getId().equals(""))
                //    buffer.write("\t"+node.getDemand()[0].getPattern().getId());

                if (node.Demands.Count > 0) {
                    Demand demand = node.Demands[0];
                    buffer.Write("\t{0}", fMap.RevertUnit(FieldType.DEMAND, demand.Base));

                    if (!string.IsNullOrEmpty(demand.pattern.Name)
                        && !net.DefPatId.Equals(demand.pattern.Name, StringComparison.OrdinalIgnoreCase))
                        buffer.Write("\t" + demand.pattern.Name);
                }

                if (!string.IsNullOrEmpty(node.Comment))
                    buffer.Write("\t;" + node.Comment);

                buffer.WriteLine();
            }


            buffer.WriteLine();
        }

        private void ComposeReservoirs(Network net) {
            FieldsMap fMap = net.FieldsMap;

            if (!net.Reservoirs.Any())
                return;

            buffer.WriteLine(SectType.RESERVOIRS.ParseStr());
            buffer.WriteLine(RESERVOIRS_SUBTITLE);

            foreach(Tank tank in net.Reservoirs) {
                buffer.Write(" {0}\t{1}", tank.Name, fMap.RevertUnit(FieldType.ELEV, tank.Elevation));


                if (tank.Pattern != null)
                    buffer.Write("\t{0}", tank.Pattern.Name);


                if (!string.IsNullOrEmpty(tank.Comment))
                    buffer.Write("\t;" + tank.Comment);

                buffer.WriteLine();
            }

            buffer.WriteLine();
        }

        private void ComposeTanks(Network net) {
            FieldsMap fMap = net.FieldsMap;

            if (!net.Tanks.Any())
                return;

            buffer.WriteLine(SectType.TANKS.ParseStr());
            buffer.WriteLine(TANK_SUBTITLE);

            foreach(Tank tank in net.Tanks) {
                double vmin = tank.Vmin;
                if (Math.Round(vmin / tank.Area) == Math.Round(tank.Hmin - tank.Elevation))
                    vmin = 0;

                buffer.Write(
                        " {0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                        tank.Name,
                        fMap.RevertUnit(FieldType.ELEV, tank.Elevation),
                        fMap.RevertUnit(FieldType.ELEV, tank.H0 - tank.Elevation),
                        fMap.RevertUnit(FieldType.ELEV, tank.Hmin - tank.Elevation),
                        fMap.RevertUnit(FieldType.ELEV, tank.Hmax - tank.Elevation),
                        fMap.RevertUnit(FieldType.ELEV, 2 * Math.Sqrt(tank.Area / Math.PI)),
                        fMap.RevertUnit(FieldType.VOLUME, vmin));

                if (tank.Vcurve != null)
                    buffer.Write(" " + tank.Vcurve.Name);

                if (!string.IsNullOrEmpty(tank.Comment))
                    buffer.Write("\t;" + tank.Comment);
                buffer.WriteLine();
            }

            buffer.WriteLine();
        }

        private void ComposePipes(Network net) {
            FieldsMap fMap = net.FieldsMap;
            
            if (net.Links.Count == 0)
                return;

            List<Link> pipes = new List<Link>();
            foreach (Link link  in  net.Links)
                if (link.Type == LinkType.PIPE || link.Type == LinkType.CV)
                    pipes.Add(link);


            buffer.WriteLine(SectType.PIPES.ParseStr());
            buffer.WriteLine(PIPES_SUBTITLE);

            foreach (Link link  in  pipes) {
                double d = link.Diameter;
                double kc = link.Kc;
                if (net.FormFlag == FormType.DW)
                    kc = fMap.RevertUnit(FieldType.ELEV, kc * 1000.0);

                double km = link.Km * Math.Pow(d, 4.0) / 0.02517;

                buffer.Write(
                        " {0}\t{1}\t{2}\t{3}\t{4}",
                        link.Name,
                        link.FirstNode.Name,
                        link.SecondNode.Name,
                        fMap.RevertUnit(FieldType.LENGTH, link.Lenght),
                        fMap.RevertUnit(FieldType.DIAM, d));

                // if (net.FormFlag == FormType.DW)
                buffer.Write(" {0}\t{1}", kc, km);

                if (link.Type == LinkType.CV)
                    buffer.Write(" CV");
                else if (link.Status == StatType.CLOSED)
                    buffer.Write(" CLOSED");
                else if (link.Status == StatType.OPEN)
                    buffer.Write(" OPEN");

                if (!string.IsNullOrEmpty(link.Comment))
                    buffer.Write("\t;" + link.Comment);

                buffer.WriteLine();
            }

            buffer.WriteLine();
        }

        private void ComposePumps(Network net) {
            FieldsMap fMap = net.FieldsMap;

            if (!net.Pumps.Any())
                return;

            buffer.WriteLine(SectType.PUMPS.ParseStr());
            buffer.WriteLine(PUMPS_SUBTITLE);

            foreach (Pump pump in net.Pumps) {
                buffer.Write(
                        " {0}\t{1}\t{2}",
                        pump.Name,
                        pump.FirstNode.Name,
                        pump.SecondNode.Name);


                // Pump has constant power
                if (pump.Ptype == PumpType.CONST_HP)
                    buffer.Write(" POWER " + pump.Km);
                // Pump has a head curve
                else if (pump.HCurve != null)
                    buffer.Write(" HEAD " + pump.HCurve.Name);
                // Old format used for pump curve
                else {
                    buffer.Write(
                            " {0}\t{1}\t{2}\t0.0\t{3}",
                            fMap.RevertUnit(FieldType.HEAD, -pump.H0),
                            fMap.RevertUnit(
                                FieldType.HEAD,
                                -pump.H0 - pump.FlowCoefficient * Math.Pow(pump.Q0, pump.N)),
                            fMap.RevertUnit(FieldType.FLOW, pump.Q0),
                            fMap.RevertUnit(FieldType.FLOW, pump.Qmax));

                    continue;
                }

                if (pump.UPat != null)
                    buffer.Write(" PATTERN " + pump.UPat.Name);


                if (pump.Kc != 1.0)
                    buffer.Write(" SPEED {0}", pump.Kc);

                if (!string.IsNullOrEmpty(pump.Comment))
                    buffer.Write("\t;" + pump.Comment);

                buffer.WriteLine();
            }

            buffer.WriteLine();
        }

        private void ComposeValves(Network net) {
            FieldsMap fMap = net.FieldsMap;

            if (!net.Valves.Any())
                return;

            buffer.WriteLine(SectType.VALVES.ParseStr());
            buffer.WriteLine(VALVES_SUBTITLE);
            
            foreach (Valve valve in net.Valves) {
                double d = valve.Diameter;
                double kc = valve.Kc;
                if(kc.IsMissing())
                    kc = 0.0;

                switch (valve.Type) {
                case LinkType.FCV:
                    kc = fMap.RevertUnit(FieldType.FLOW, kc);
                    break;
                case LinkType.PRV:
                case LinkType.PSV:
                case LinkType.PBV:
                    kc = fMap.RevertUnit(FieldType.PRESSURE, kc);
                    break;
                }

                double km = valve.Km * Math.Pow(d, 4) / 0.02517;

                buffer.Write(
                        " {0}\t{1}\t{2}\t{3}\t{4}",
                        valve.Name,
                        valve.FirstNode.Name,
                        valve.SecondNode.Name,
                        fMap.RevertUnit(FieldType.DIAM, d),
                        valve.Type.ParseStr());

                if (valve.Type == LinkType.GPV && valve.Curve != null)
                    buffer.Write(" {0}\t{1}", valve.Curve.Name, km);
                else
                    buffer.Write(" {0}\t{1}", kc, km);

                if (!string.IsNullOrEmpty(valve.Comment))
                    buffer.Write("\t;" + valve.Comment);

                buffer.WriteLine();
            }
            buffer.WriteLine();
        }

        private void ComposeDemands(Network net) {
            FieldsMap fMap = net.FieldsMap;

            if (!net.Junctions.Any())
                return;

            buffer.WriteLine(SectType.DEMANDS.ParseStr());
            buffer.WriteLine(DEMANDS_SUBTITLE);

            double ucf = fMap.GetUnits(FieldType.DEMAND);

            foreach (Node node in net.Junctions) {
                foreach (Demand demand in node.Demands) {
                    buffer.Write("{0}\t{1}", node.Name, ucf * demand.Base);

                    if (demand.pattern != null)
                        buffer.Write("\t" + demand.pattern.Name);

                    buffer.WriteLine();
                }
            }

            buffer.WriteLine();
        }

        private void ComposeEmitters(Network net) {

            if (net.Nodes.Count == 0)
                return;

            buffer.WriteLine(SectType.EMITTERS.ParseStr());
            buffer.WriteLine(EMITTERS_SUBTITLE);

            double uflow = net.FieldsMap.GetUnits(FieldType.FLOW);
            double upressure = net.FieldsMap.GetUnits(FieldType.PRESSURE);
            double qexp = net.QExp;

            foreach (Node node  in  net.Junctions) {
                if (node.Ke == 0.0) continue;
                double ke = uflow / Math.Pow(upressure * node.Ke, (1.0 / qexp));
                buffer.WriteLine(" {0}\t{1}", node.Name, ke);
            }

            buffer.WriteLine();
        }

        private void ComposeStatus(Network net) {

            if (net.Links.Count == 0)
                return;

            buffer.WriteLine(SectType.STATUS.ParseStr());
            buffer.WriteLine(STATUS_SUBTITLE);

            foreach (Link link  in  net.Links) {
                if (link.Type <= LinkType.PUMP) {
                    if (link.Status == StatType.CLOSED)
                        buffer.WriteLine(" {0}\t{1}", link.Name, StatType.CLOSED);

                    // Write pump speed here for pumps with old-style pump curve input
                    else if (link.Type == LinkType.PUMP) {
                        Pump pump = (Pump)link;
                        if (pump.HCurve == null &&
                            pump.Ptype != PumpType.CONST_HP &&
                            pump.Kc != 1.0)
                            buffer.WriteLine(" {0}\t{1}", link.Name, link.Kc);
                    }
                }
                // Write fixed-status PRVs & PSVs (setting = MISSING)
                else if (link.Kc.IsMissing()) {
                    switch (link.Status) {
                    case StatType.OPEN:
                    case StatType.CLOSED:
                        buffer.WriteLine(" {0}\t{1}", link.Name, link.Status);
                        break;
                    }

                }

            }

            buffer.WriteLine();
        }

        private void ComposePatterns(Network net) {

            var pats = net.Patterns;

            if (pats.Count <= 1)
                return;

            buffer.WriteLine(SectType.PATTERNS.ParseStr());
            buffer.WriteLine(PATTERNS_SUBTITLE);

            for (int i = 1; i < pats.Count; i++) {
                Pattern pat = pats[i];
                for (int j = 0; j < pats[i].Count; j++) {
                    if (j % 6 == 0)
                        buffer.Write(" {0}", pat.Name);

                    buffer.Write(" {0}", pat[j]);

                    if (j % 6 == 5)
                        buffer.WriteLine();
                }
                buffer.WriteLine();
            }

            buffer.WriteLine();
        }

        private void ComposeCurves(Network net) {

            var curves = net.Curves;

            if (curves.Count == 0)
                return;

            buffer.WriteLine(SectType.CURVES.ParseStr());
            buffer.WriteLine(CURVE_SUBTITLE);

            foreach (Curve curve  in  curves) {
                foreach (var pt in curve) {
                    buffer.WriteLine(" {0}\t{1}\t{2}", curve.Name, pt.X, pt.Y);
                }
            }

            buffer.WriteLine();
        }

        private void ComposeControls(Network net) {
            var controls = net.Controls;
            FieldsMap fmap = net.FieldsMap;

            if (controls.Count == 0)
                return;

            buffer.WriteLine(SectType.CONTROLS.ParseStr());

            foreach (Control control  in  controls) {
                // Check that controlled link exists
                if (control.Link == null) continue;

                // Get text of control's link status/setting
                if (control.Setting.IsMissing()) {
                    buffer.Write(" LINK {0} {1} ", control.Link.Name, control.Status);
                }
                else {
                    double kc = control.Setting;
                    switch (control.Link.Type) {
                    case LinkType.PRV:
                    case LinkType.PSV:
                    case LinkType.PBV:
                        kc = fmap.RevertUnit(FieldType.PRESSURE, kc);
                        break;
                    case LinkType.FCV:
                        kc = fmap.RevertUnit(FieldType.FLOW, kc);
                        break;
                    }
                    buffer.Write(" LINK {0} {1} ", control.Link.Name, kc);
                }


                switch (control.Type) {
                // Print level control
                case ControlType.LOWLEVEL:
                case ControlType.HILEVEL:
                    double kc = control.Grade - control.Node.Elevation;
                    kc = fmap.RevertUnit(
                        control.Node.Type == NodeType.JUNC
                            ? FieldType.PRESSURE
                            : FieldType.HEAD,
                        kc);

                    buffer.Write(
                            " IF NODE {0} {1} {2}",
                            control.Node.Name,
                            control.Type.ParseStr(),
                            kc);

                    break;

                // Print timer control
                case ControlType.TIMER:
                    buffer.Write(
                            " AT {0} {1} HOURS",
                            ControlType.TIMER.ParseStr(),
                            control.Time / 3600.0f);

                    break;

                // Print time-of-day control
                case ControlType.TIMEOFDAY:
                    buffer.Write(
                            " AT {0} {1}",
                            ControlType.TIMEOFDAY.ParseStr(),
                            control.Time.GetClockTime());

                    break;
                }
                buffer.WriteLine();
            }
            buffer.WriteLine();
        }

        private void ComposeQuality(Network net) {
            FieldsMap fmap = net.FieldsMap;

            if (net.Nodes.Count == 0)
                return;

            buffer.WriteLine(SectType.QUALITY.ParseStr());
            buffer.WriteLine(QUALITY_SUBTITLE);

            foreach (Node node  in  net.Nodes) {
                if (node.C0 == 0.0) continue;
                buffer.WriteLine(" {0}\t{1}", node.Name, fmap.RevertUnit(FieldType.QUALITY, node.C0));
            }

            buffer.WriteLine();
        }

        private void ComposeSource(Network net) {

            if (net.Nodes.Count == 0)
                return;

            buffer.WriteLine(SectType.SOURCES.ParseStr());
            buffer.WriteLine(SOURCE_SUBTITLE);


            foreach (Node node in net.Nodes) {
                QualSource source = node.QualSource;
                if (source == null)
                    continue;

                buffer.Write(
                        " {0}\t{1}\t{2}",
                        node.Name,
                        source.Type,
                        source.C0);

                if (source.Pattern != null)
                    buffer.Write(" " + source.Pattern.Name);

                buffer.WriteLine();
            }
            buffer.WriteLine();
        }


        private void ComposeMixing(Network net) {

            if (!net.Tanks.Any())
                return;

            buffer.WriteLine(SectType.MIXING.ParseStr());
            buffer.WriteLine(MIXING_SUBTITLE);

            foreach (Tank tank in net.Tanks) {
                buffer.WriteLine(
                        " {0}\t{1}\t{2}",
                        tank.Name,
                        tank.MixModel.ParseStr(),
                        tank.V1Max / tank.Vmax);
            }

            buffer.WriteLine();
        }

        private void ComposeReaction(Network net) {
            
            buffer.WriteLine(SectType.REACTIONS.ParseStr());
            buffer.WriteLine(REACTIONS_SUBTITLE);

            buffer.WriteLine("ORDER BULK {0}", net.BulkOrder);
            buffer.WriteLine("ORDER WALL {0}", net.WallOrder);
            buffer.WriteLine("ORDER TANK {0}", net.TankOrder);
            buffer.WriteLine("GLOBAL BULK {0}", net.KBulk * Constants.SECperDAY);
            buffer.WriteLine("GLOBAL WALL {0}", net.KWall * Constants.SECperDAY);

            // if (net.CLimit > 0.0)
            buffer.WriteLine("LIMITING POTENTIAL {0}", net.CLimit);

            // if (!net.RFactor.IsMissing() && net.RFactor != 0.0)
            buffer.WriteLine("ROUGHNESS CORRELATION {0}", net.RFactor);


            foreach (Link link  in  net.Links) {
                if (link.Type > LinkType.PIPE)
                    continue;

                if (link.Kb != net.KBulk)
                    buffer.WriteLine("BULK {0} {1}", link.Name, link.Kb * Constants.SECperDAY);
                if (link.Kw != net.KWall)
                    buffer.WriteLine("WALL {0} {1}", link.Name, link.Kw * Constants.SECperDAY);
            }

            foreach (Tank tank  in  net.Tanks) {
                if (tank.Kb != net.KBulk)
                    buffer.WriteLine("TANK {0} {1}", tank.Name, tank.Kb * Constants.SECperDAY);
            }

            buffer.WriteLine();
        }

        private void ComposeEnergy(Network net) {
            
            buffer.WriteLine(SectType.ENERGY.ParseStr());

            if (net.ECost != 0.0)
                buffer.WriteLine("GLOBAL PRICE {0}", net.ECost);

            if (!net.EPatId.Equals(""))
                buffer.WriteLine("GLOBAL PATTERN {0}", net.EPatId);

            buffer.WriteLine("GLOBAL EFFIC {0}", net.EPump);
            buffer.WriteLine("DEMAND CHARGE {0}", net.DCost);

            foreach (Pump p  in  net.Pumps) {
                if (p.ECost > 0.0)
                    buffer.WriteLine("PUMP {0} PRICE {1}", p.Name, p.ECost);

                if (p.EPat != null)
                    buffer.WriteLine("PUMP {0} PATTERN {1}", p.Name, p.EPat.Name);

                if (p.ECurve != null)
                    buffer.WriteLine("PUMP {0} EFFIC {1}", p.Name, p.ECurve.Name);
            }

            buffer.WriteLine();

        }

        private void ComposeTimes(Network net) {
            
            buffer.WriteLine(SectType.TIMES.ParseStr());
            buffer.WriteLine("DURATION {0}", net.Duration.GetClockTime());
            buffer.WriteLine("HYDRAULIC TIMESTEP {0}", net.HStep.GetClockTime());
            buffer.WriteLine("QUALITY TIMESTEP {0}", net.QStep.GetClockTime());
            buffer.WriteLine("REPORT TIMESTEP {0}", net.RStep.GetClockTime());
            buffer.WriteLine("REPORT START {0}", net.RStart.GetClockTime());
            buffer.WriteLine("PATTERN TIMESTEP {0}", net.PStep.GetClockTime());
            buffer.WriteLine("PATTERN START {0}", net.PStart.GetClockTime());
            buffer.WriteLine("RULE TIMESTEP {0}", net.RuleStep.GetClockTime());
            buffer.WriteLine("START CLOCKTIME {0}", net.Tstart.GetClockTime());
            buffer.WriteLine("STATISTIC {0}", net.TstatFlag.ParseStr());
            buffer.WriteLine();
        }

        private void ComposeOptions(Network net) {
            
            FieldsMap fMap = net.FieldsMap;

            buffer.WriteLine(SectType.OPTIONS.ParseStr());
            buffer.WriteLine("UNITS               " + net.FlowFlag);
            buffer.WriteLine("PRESSURE            " + net.PressFlag);
            buffer.WriteLine("HEADLOSS            " + net.FormFlag);

            if (!string.IsNullOrEmpty(net.DefPatId))
                buffer.WriteLine("PATTERN             " + net.DefPatId);

            if (net.HydFlag == HydType.USE)
                buffer.WriteLine("HYDRAULICS USE      " + net.HydFname);

            if (net.HydFlag == HydType.SAVE)
                buffer.WriteLine("HYDRAULICS SAVE     " + net.HydFname);

            if (net.ExtraIter == -1)
                buffer.WriteLine("UNBALANCED          STOP");

            if (net.ExtraIter >= 0)
                buffer.WriteLine("UNBALANCED          CONTINUE " + net.ExtraIter);

            switch (net.QualFlag) {
            case QualType.CHEM:
                buffer.WriteLine("QUALITY             {0} {1}", net.ChemName, net.ChemUnits);
                break;
            case QualType.TRACE:
                buffer.WriteLine("QUALITY             TRACE " + net.TraceNode);
                break;
            case QualType.AGE:
                buffer.WriteLine("QUALITY             AGE");
                break;
            case QualType.NONE:
                buffer.WriteLine("QUALITY             NONE");
                break;
            }

            buffer.WriteLine("DEMAND MULTIPLIER {0}", net.DMult);
            buffer.WriteLine("EMITTER EXPONENT  {0}", 1.0 / net.QExp);
            buffer.WriteLine("VISCOSITY         {0}", net.Viscos / Constants.VISCOS);
            buffer.WriteLine("DIFFUSIVITY       {0}", net.Diffus / Constants.DIFFUS);
            buffer.WriteLine("SPECIFIC GRAVITY  {0}", net.SpGrav);
            buffer.WriteLine("TRIALS            {0}", net.MaxIter);
            buffer.WriteLine("ACCURACY          {0}", net.HAcc);
            buffer.WriteLine("TOLERANCE         {0}", fMap.RevertUnit(FieldType.QUALITY, net.Ctol));
            buffer.WriteLine("CHECKFREQ         {0}", net.CheckFreq);
            buffer.WriteLine("MAXCHECK          {0}", net.MaxCheck);
            buffer.WriteLine("DAMPLIMIT         {0}", net.DampLimit);
            buffer.WriteLine();
        }

        private void ComposeExtraOptions(Network net) {
            var extraOptions = net.ExtraOptions;

            if(extraOptions.Count == 0)
                return;

            foreach(var pair in extraOptions) {
                buffer.WriteLine(pair.Key + " " + pair.Value);
            }

            buffer.WriteLine();
        }

        private void ComposeReport(Network net) {

            buffer.WriteLine(SectType.REPORT.ParseStr());

            FieldsMap fMap = net.FieldsMap;
            buffer.WriteLine("PAGESIZE       {0}", net.PageSize);
            buffer.WriteLine("STATUS         " + net.StatFlag);
            buffer.WriteLine("SUMMARY        " + (net.SummaryFlag ? Keywords.w_YES : Keywords.w_NO));
            buffer.WriteLine("ENERGY         " + (net.EnergyFlag ? Keywords.w_YES : Keywords.w_NO));

            switch (net.NodeFlag) {
            case ReportFlag.FALSE:
                buffer.WriteLine("NODES NONE");
                break;
            case ReportFlag.TRUE:
                buffer.WriteLine("NODES ALL");
                break;
            case ReportFlag.SOME: {
                int j = 0;
                foreach (Node node  in  net.Nodes) {
                    if (node.RptFlag) {
                        // if (j % 5 == 0) buffer.WriteLine("NODES "); // BUG: Baseform bug

                        if (j % 5 == 0) {
                            buffer.WriteLine();
                            buffer.Write("NODES ");
                        }

                        buffer.Write("{0} ", node.Name);
                        j++;
                    }
                }
                break;
            }
            }

            switch (net.LinkFlag) {
            case ReportFlag.FALSE:
                buffer.WriteLine("LINKS NONE");
                break;
            case ReportFlag.TRUE:
                buffer.WriteLine("LINKS ALL");
                break;
            case ReportFlag.SOME: {
                int j = 0;
                foreach (Link link  in  net.Links) {
                    if (link.RptFlag) {
                        // if (j % 5 == 0) buffer.write("LINKS \n"); // BUG: Baseform bug
                        if (j % 5 == 0) {
                            buffer.WriteLine();
                            buffer.Write("LINKS ");
                        }

                        buffer.Write("{0} ", link.Name);
                        j++;
                    }
                }
                break;
            }
            }

            for (FieldType i = 0; i < FieldType.FRICTION; i++) {
                Field f = fMap.GetField(i);

                if (!f.Enabled) {
                    buffer.WriteLine("{0,-19} NO", f.Name);
                    continue;
                }

                buffer.WriteLine("{0,-19} PRECISION {1}", f.Name, f.Precision);

                if (f.GetRptLim(RangeType.LOW) < Constants.BIG)
                    buffer.WriteLine("{0,-19} BELOW {1:0.######}", f.Name, f.GetRptLim(RangeType.LOW));

                if (f.GetRptLim(RangeType.HI) > -Constants.BIG)
                    buffer.WriteLine("{0,-19} ABOVE {1:0.######}", f.Name, f.GetRptLim(RangeType.HI));
            }

            buffer.WriteLine();
        }

        private void ComposeCoordinates(Network net) {
            buffer.WriteLine(SectType.COORDINATES.ParseStr());
            buffer.WriteLine(COORDINATES_SUBTITLE);

            foreach (Node node  in  net.Nodes) {
                if (!node.Position.IsInvalid) {
                    buffer.WriteLine(
                            " {0,-16}\t{1,-12}\t{2,-12}",
                            node.Name,
                            node.Position.X,
                            node.Position.Y);
                }
            }
            buffer.WriteLine();
        }


        private void ComposeLabels(Network net) {
            buffer.WriteLine(SectType.LABELS.ParseStr());
            buffer.WriteLine(";X-Coord\tY-Coord\tLabel & Anchor Node");

            foreach (Label label  in  net.Labels) {
                buffer.WriteLine(
                        " {0,-16}\t{1,-16}\t\"{2}\" {3,-16}",
                        label.Position.X,
                        label.Position.Y,
                        label.Text,
                        // label.AnchorNodeId // TODO: add AnchorNodeId property to label
                        ""
                    );

            }
            buffer.WriteLine();
        }

        private void ComposeVertices(Network net) {
            buffer.WriteLine(SectType.VERTICES.ParseStr());
            buffer.WriteLine(";Link\tX-Coord\tY-Coord");

            foreach (Link link  in  net.Links) {
                if (link.Vertices.Count == 0)
                    continue;

                foreach (EnPoint p  in  link.Vertices) {
                    buffer.WriteLine(" {0,-16}\t{1,-16}\t{2,-16}", link.Name, p.X, p.Y);
                }
            }

            buffer.WriteLine();
        }

        private void ComposeRules(Network net) {
            buffer.WriteLine(SectType.RULES.ParseStr());
            buffer.WriteLine();

            foreach (Rule r  in  net.Rules) {
                buffer.WriteLine("RULE " + r.Name);
                foreach (string s  in  r.Code)
                    buffer.WriteLine(s);
                buffer.WriteLine();
            }
            buffer.WriteLine();
        }
    }

}