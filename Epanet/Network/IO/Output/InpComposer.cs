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

        private TextWriter _writer;

        public InpComposer(Network net) : base(net) { }

        public override void Compose(string fileName) {

            try {
                using (_writer = new StreamWriter(File.OpenWrite(fileName), Encoding.Default)) { // "ISO-8859-1"
                    ComposeHeader();
                    ComposeJunctions();
                    ComposeReservoirs();
                    ComposeTanks();
                    ComposePipes();
                    ComposePumps();
                    ComposeValves();
                    ComposeTags();
                    ComposeDemands();
                    ComposeEmitters();
                    ComposeStatus();
                    ComposePatterns();
                    ComposeCurves();
                    ComposeControls();
                    ComposeQuality();
                    ComposeSource();
                    ComposeMixing();
                    ComposeReaction();
                    ComposeEnergy();
                    ComposeTimes();
                    ComposeOptions();
                    ComposeExtraOptions();
                    ComposeReport();
                    ComposeLabels();
                    ComposeCoordinates();
                    ComposeVertices();
                    ComposeRules();

                    _writer.WriteLine(SectType.END.ToString());
                }
            }
            catch(IOException ex) {
                throw new EnException(ErrorCode.Err308, ex);
            }

        }

        private void ComposeHeader() {
            if (_net.Title.Count == 0)
                return;

            _writer.WriteLine(SectType.TITLE.ParseStr());

            foreach (string str  in  _net.Title) {
                _writer.WriteLine(str);
            }

            _writer.WriteLine();
        }


        private void ComposeJunctions() {
            FieldsMap fMap = _net.FieldsMap;
            
            if (!_net.Junctions.Any())
                return;

            _writer.WriteLine(SectType.JUNCTIONS.ParseStr());
            _writer.WriteLine(JUNCS_SUBTITLE);

            foreach (Node node in _net.Junctions) {
                _writer.Write(" {0}\t{1}", node.Name, fMap.RevertUnit(FieldType.ELEV, node.Elevation));

                //if(node.getDemand()!=null && node.getDemand().size()>0 && !node.getDemand()[0].getPattern().getId().equals(""))
                //    buffer.write("\t"+node.getDemand()[0].getPattern().getId());

                if (node.Demands.Count > 0) {
                    Demand demand = node.Demands[0];
                    _writer.Write("\t{0}", fMap.RevertUnit(FieldType.DEMAND, demand.Base));

                    if (!string.IsNullOrEmpty(demand.Pattern.Name)
                        && !_net.DefPatId.Equals(demand.Pattern.Name, StringComparison.OrdinalIgnoreCase))
                        _writer.Write("\t" + demand.Pattern.Name);
                }

                if (!string.IsNullOrEmpty(node.Comment))
                    _writer.Write("\t;" + node.Comment);

                _writer.WriteLine();
            }


            _writer.WriteLine();
        }

        private void ComposeReservoirs() {
            FieldsMap fMap = _net.FieldsMap;

            if (!_net.Reservoirs.Any())
                return;

            _writer.WriteLine(SectType.RESERVOIRS.ParseStr());
            _writer.WriteLine(RESERVOIRS_SUBTITLE);

            foreach(Tank tank in _net.Reservoirs) {
                _writer.Write(" {0}\t{1}", tank.Name, fMap.RevertUnit(FieldType.ELEV, tank.Elevation));


                if (tank.Pattern != null)
                    _writer.Write("\t{0}", tank.Pattern.Name);


                if (!string.IsNullOrEmpty(tank.Comment))
                    _writer.Write("\t;" + tank.Comment);

                _writer.WriteLine();
            }

            _writer.WriteLine();
        }

        private void ComposeTanks() {
            FieldsMap fMap = _net.FieldsMap;

            if (!_net.Tanks.Any())
                return;

            _writer.WriteLine(SectType.TANKS.ParseStr());
            _writer.WriteLine(TANK_SUBTITLE);

            foreach(Tank tank in _net.Tanks) {
                double vmin = tank.Vmin;
                if (Math.Round(vmin / tank.Area).EqualsTo(Math.Round(tank.Hmin - tank.Elevation)))
                    vmin = 0;

                _writer.Write(
                        " {0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                        tank.Name,
                        fMap.RevertUnit(FieldType.ELEV, tank.Elevation),
                        fMap.RevertUnit(FieldType.ELEV, tank.H0 - tank.Elevation),
                        fMap.RevertUnit(FieldType.ELEV, tank.Hmin - tank.Elevation),
                        fMap.RevertUnit(FieldType.ELEV, tank.Hmax - tank.Elevation),
                        fMap.RevertUnit(FieldType.ELEV, 2 * Math.Sqrt(tank.Area / Math.PI)),
                        fMap.RevertUnit(FieldType.VOLUME, vmin));

                if (tank.Vcurve != null)
                    _writer.Write(" " + tank.Vcurve.Name);

                if (!string.IsNullOrEmpty(tank.Comment))
                    _writer.Write("\t;" + tank.Comment);
                _writer.WriteLine();
            }

            _writer.WriteLine();
        }

        private void ComposePipes() {
            FieldsMap fMap = _net.FieldsMap;
            
            if (_net.Links.Count == 0)
                return;
            
            _writer.WriteLine(SectType.PIPES.ParseStr());
            _writer.WriteLine(PIPES_SUBTITLE);

            foreach (Pipe pipe in  _net.Pipes) {
                double d = pipe.Diameter;
                double kc = pipe.Kc;
                if (_net.FormFlag == FormType.DW)
                    kc = fMap.RevertUnit(FieldType.ELEV, kc * 1000.0);

                double km = pipe.Km * Math.Pow(d, 4.0) / 0.02517;

                _writer.Write(
                        " {0}\t{1}\t{2}\t{3}\t{4}",
                        pipe.Name,
                        pipe.FirstNode.Name,
                        pipe.SecondNode.Name,
                        fMap.RevertUnit(FieldType.LENGTH, pipe.Lenght),
                        fMap.RevertUnit(FieldType.DIAM, d));

                // if (net.FormFlag == FormType.DW)
                _writer.Write(" {0}\t{1}", kc, km);

                if (pipe.HasCheckValve)
                    _writer.Write(" CV");
                else {
                    switch (pipe.Status) {
                        case StatType.CLOSED:
                            _writer.Write(" CLOSED");
                            break;
                        case StatType.OPEN:
                            _writer.Write(" OPEN");
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(pipe.Comment))
                    _writer.Write("\t;" + pipe.Comment);

                _writer.WriteLine();
            }

            _writer.WriteLine();
        }

        private void ComposePumps() {
            FieldsMap fMap = _net.FieldsMap;

            if (!_net.Pumps.Any())
                return;

            _writer.WriteLine(SectType.PUMPS.ParseStr());
            _writer.WriteLine(PUMPS_SUBTITLE);

            foreach (Pump pump in _net.Pumps) {
                _writer.Write(
                        " {0}\t{1}\t{2}",
                        pump.Name,
                        pump.FirstNode.Name,
                        pump.SecondNode.Name);


                // Pump has constant power
                if (pump.Ptype == PumpType.CONST_HP)
                    _writer.Write(" POWER " + pump.Km);
                // Pump has a head curve
                else if (pump.HCurve != null)
                    _writer.Write(" HEAD " + pump.HCurve.Name);
                // Old format used for pump curve
                else {
                    _writer.Write(
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
                    _writer.Write(" PATTERN " + pump.UPat.Name);


                if (!pump.Kc.EqualsTo(1.0))
                    _writer.Write(" SPEED {0}", pump.Kc);

                if (!string.IsNullOrEmpty(pump.Comment))
                    _writer.Write("\t;" + pump.Comment);

                _writer.WriteLine();
            }

            _writer.WriteLine();
        }

        private void ComposeValves() {
            FieldsMap fMap = _net.FieldsMap;

            if (!_net.Valves.Any())
                return;

            _writer.WriteLine(SectType.VALVES.ParseStr());
            _writer.WriteLine(VALVES_SUBTITLE);
            
            foreach (Valve valve in _net.Valves) {
                double d = valve.Diameter;
                double kc = valve.Kc;
                if(double.IsNaN(kc))
                    kc = 0.0;

                switch (valve.ValveType) {
                case ValveType.FCV:
                    kc = fMap.RevertUnit(FieldType.FLOW, kc);
                    break;
                case ValveType.PRV:
                case ValveType.PSV:
                case ValveType.PBV:
                    kc = fMap.RevertUnit(FieldType.PRESSURE, kc);
                    break;
                }

                double km = valve.Km * Math.Pow(d, 4) / 0.02517;

                _writer.Write(
                        " {0}\t{1}\t{2}\t{3}\t{4}",
                        valve.Name,
                        valve.FirstNode.Name,
                        valve.SecondNode.Name,
                        fMap.RevertUnit(FieldType.DIAM, d),
                        valve.ValveType.Keyword2());

                if (valve.ValveType == ValveType.GPV && valve.Curve != null)
                    _writer.Write(" {0}\t{1}", valve.Curve.Name, km);
                else
                    _writer.Write(" {0}\t{1}", kc, km);

                if (!string.IsNullOrEmpty(valve.Comment))
                    _writer.Write("\t;" + valve.Comment);

                _writer.WriteLine();
            }
            _writer.WriteLine();
        }

        private void ComposeTags() {
            _writer.WriteLine(SectType.TAGS.ParseStr());
            char[] spaces = {' ', '\t',};

            foreach (var node in _net.Nodes) {
                string tag = node.Tag.Trim();

                if (string.IsNullOrEmpty(tag)) continue;

                if (tag.IndexOf('\r') >= 0) tag = tag.Replace('\r', ' ');
                if (tag.IndexOf('\n') >= 0) tag = tag.Replace('\n', ' ');
                if (tag.IndexOfAny(spaces) > 0) tag = '"' + tag + '"';

                _writer.WriteLine(" {0} \t{1,-16} {2}", Keywords.w_NODE, node.Name, tag);
            }

            foreach(var link in _net.Links) {
                string tag = link.Tag;

                if(string.IsNullOrEmpty(tag))
                    continue;

                if(tag.IndexOf('\r') >= 0) tag = tag.Replace('\r', ' ');
                if(tag.IndexOf('\n') >= 0) tag = tag.Replace('\n', ' ');
                if(tag.IndexOfAny(spaces) > 0) tag = '"' + tag + '"';

                _writer.WriteLine(" {0} \t{1,-16} {2}", Keywords.w_LINK, link.Name, tag);
            }

        }


        private void ComposeDemands() {
            FieldsMap fMap = _net.FieldsMap;

            if (!_net.Junctions.Any())
                return;

            _writer.WriteLine(SectType.DEMANDS.ParseStr());
            _writer.WriteLine(DEMANDS_SUBTITLE);

            double ucf = fMap.GetUnits(FieldType.DEMAND);

            foreach (Node node in _net.Junctions) {
                foreach (Demand demand in node.Demands) {
                    _writer.Write("{0}\t{1}", node.Name, ucf * demand.Base);

                    if (demand.Pattern != null)
                        _writer.Write("\t" + demand.Pattern.Name);

                    _writer.WriteLine();
                }
            }

            _writer.WriteLine();
        }

        private void ComposeEmitters() {

            if (_net.Nodes.Count == 0)
                return;

            _writer.WriteLine(SectType.EMITTERS.ParseStr());
            _writer.WriteLine(EMITTERS_SUBTITLE);

            double uflow = _net.FieldsMap.GetUnits(FieldType.FLOW);
            double upressure = _net.FieldsMap.GetUnits(FieldType.PRESSURE);
            double qexp = _net.QExp;

            foreach (Node node  in  _net.Junctions) {
                if (node.Ke.IsZero()) continue;
                double ke = uflow / Math.Pow(upressure * node.Ke, 1.0 / qexp);
                _writer.WriteLine(" {0}\t{1}", node.Name, ke);
            }

            _writer.WriteLine();
        }

        private void ComposeStatus() {

            if (_net.Links.Count == 0)
                return;

            _writer.WriteLine(SectType.STATUS.ParseStr());
            _writer.WriteLine(STATUS_SUBTITLE);

            foreach (Link link  in  _net.Links) {
                if (link.LinkType <= LinkType.PUMP) {
                    if (link.Status == StatType.CLOSED)
                        _writer.WriteLine(" {0}\t{1}", link.Name, StatType.CLOSED);

                    // Write pump speed here for pumps with old-style pump curve input
                    else if (link.LinkType == LinkType.PUMP) {
                        Pump pump = (Pump)link;
                        if (pump.HCurve == null &&
                            pump.Ptype != PumpType.CONST_HP &&
                            !pump.Kc.EqualsTo(1.0))
                            _writer.WriteLine(" {0}\t{1}", link.Name, link.Kc);
                    }
                }
                // Write fixed-status PRVs & PSVs (setting = MISSING)
                else if (double.IsNaN(link.Kc)) {
                    switch (link.Status) {
                    case StatType.OPEN:
                    case StatType.CLOSED:
                        _writer.WriteLine(" {0}\t{1}", link.Name, link.Status);
                        break;
                    }

                }

            }

            _writer.WriteLine();
        }

        private void ComposePatterns() {

            var pats = _net.Patterns;

            if (pats.Count <= 1)
                return;

            _writer.WriteLine(SectType.PATTERNS.ParseStr());
            _writer.WriteLine(PATTERNS_SUBTITLE);

            for (int i = 1; i < pats.Count; i++) {
                Pattern pat = pats[i];
                for (int j = 0; j < pats[i].Count; j++) {
                    if (j % 6 == 0)
                        _writer.Write(" {0}", pat.Name);

                    _writer.Write(" {0}", pat[j]);

                    if (j % 6 == 5)
                        _writer.WriteLine();
                }
                _writer.WriteLine();
            }

            _writer.WriteLine();
        }

        private void ComposeCurves() {

            var curves = _net.Curves;

            if (curves.Count == 0)
                return;

            _writer.WriteLine(SectType.CURVES.ParseStr());
            _writer.WriteLine(CURVE_SUBTITLE);

            foreach (Curve curve  in  curves) {
                foreach (var pt in curve) {
                    _writer.WriteLine(" {0}\t{1}\t{2}", curve.Name, pt.X, pt.Y);
                }
            }

            _writer.WriteLine();
        }

        private void ComposeControls() {
            var controls = _net.Controls;
            FieldsMap fmap = _net.FieldsMap;

            if (controls.Count == 0)
                return;

            _writer.WriteLine(SectType.CONTROLS.ParseStr());

            foreach (Control control  in  controls) {
                // Check that controlled link exists
                if (control.Link == null) continue;

                // Get text of control's link status/setting
                if (double.IsNaN(control.Setting)) {
                    _writer.Write(" LINK {0} {1} ", control.Link.Name, control.Status);
                }
                else {
                    double kc = control.Setting;

                    if (control.Link.LinkType == LinkType.VALVE) {
                        switch (((Valve)control.Link).ValveType) {
                            case ValveType.PRV:
                            case ValveType.PSV:
                            case ValveType.PBV:
                                kc = fmap.RevertUnit(FieldType.PRESSURE, kc);
                                break;
                            case ValveType.FCV:
                                kc = fmap.RevertUnit(FieldType.FLOW, kc);
                                break;
                        }
                    }

                    _writer.Write(" LINK {0} {1} ", control.Link.Name, kc);
                }


                switch (control.Type) {
                // Print level control
                case ControlType.LOWLEVEL:
                case ControlType.HILEVEL:
                    double kc = control.Grade - control.Node.Elevation;
                    kc = fmap.RevertUnit(
                        control.Node.NodeType == NodeType.JUNC
                            ? FieldType.PRESSURE
                            : FieldType.HEAD,
                        kc);

                    _writer.Write(
                            " IF NODE {0} {1} {2}",
                            control.Node.Name,
                            control.Type.ParseStr(),
                            kc);

                    break;

                // Print timer control
                case ControlType.TIMER:
                    _writer.Write(
                            " AT {0} {1} HOURS",
                            ControlType.TIMER.ParseStr(),
                            control.Time.TotalHours);

                    break;

                // Print time-of-day control
                case ControlType.TIMEOFDAY:
                    _writer.Write(
                            " AT {0} {1}",
                            ControlType.TIMEOFDAY.ParseStr(),
                            control.Time.GetClockTime());

                    break;
                }
                _writer.WriteLine();
            }
            _writer.WriteLine();
        }

        private void ComposeQuality() {
            FieldsMap fmap = _net.FieldsMap;

            if (_net.Nodes.Count == 0)
                return;

            _writer.WriteLine(SectType.QUALITY.ParseStr());
            _writer.WriteLine(QUALITY_SUBTITLE);

            foreach (Node node  in  _net.Nodes) {
                if (node.C0.IsZero()) continue;
                _writer.WriteLine(" {0}\t{1}", node.Name, fmap.RevertUnit(FieldType.QUALITY, node.C0));
            }

            _writer.WriteLine();
        }

        private void ComposeSource() {

            if (_net.Nodes.Count == 0)
                return;

            _writer.WriteLine(SectType.SOURCES.ParseStr());
            _writer.WriteLine(SOURCE_SUBTITLE);


            foreach (Node node in _net.Nodes) {
                QualSource source = node.QualSource;
                if (source == null)
                    continue;

                _writer.Write(
                        " {0}\t{1}\t{2}",
                        node.Name,
                        source.Type,
                        source.C0);

                if (source.Pattern != null)
                    _writer.Write(" " + source.Pattern.Name);

                _writer.WriteLine();
            }
            _writer.WriteLine();
        }


        private void ComposeMixing() {

            if (!_net.Tanks.Any())
                return;

            _writer.WriteLine(SectType.MIXING.ParseStr());
            _writer.WriteLine(MIXING_SUBTITLE);

            foreach (Tank tank in _net.Tanks) {
                _writer.WriteLine(
                        " {0}\t{1}\t{2}",
                        tank.Name,
                        tank.MixModel.ParseStr(),
                        tank.V1Max / tank.Vmax);
            }

            _writer.WriteLine();
        }

        private void ComposeReaction() {
            
            _writer.WriteLine(SectType.REACTIONS.ParseStr());
            _writer.WriteLine(REACTIONS_SUBTITLE);

            _writer.WriteLine("ORDER BULK {0}", _net.BulkOrder);
            _writer.WriteLine("ORDER WALL {0}", _net.WallOrder);
            _writer.WriteLine("ORDER TANK {0}", _net.TankOrder);
            _writer.WriteLine("GLOBAL BULK {0}", _net.KBulk * Constants.SECperDAY);
            _writer.WriteLine("GLOBAL WALL {0}", _net.KWall * Constants.SECperDAY);

            // if (net.CLimit > 0.0)
            _writer.WriteLine("LIMITING POTENTIAL {0}", _net.CLimit);

            // if (!net.RFactor.IsMissing() && net.RFactor != 0.0)
            _writer.WriteLine("ROUGHNESS CORRELATION {0}", _net.RFactor);


            foreach (Link link  in  _net.Links) {
                if (link.LinkType > LinkType.PIPE)
                    continue;

                if (!link.Kb.EqualsTo(_net.KBulk))
                    _writer.WriteLine("BULK {0} {1}", link.Name, link.Kb * Constants.SECperDAY);
                if (!link.Kw.EqualsTo(_net.KWall))
                    _writer.WriteLine("WALL {0} {1}", link.Name, link.Kw * Constants.SECperDAY);
            }

            foreach (Tank tank  in  _net.Tanks) {
                if (!tank.Kb.EqualsTo(_net.KBulk))
                    _writer.WriteLine("TANK {0} {1}", tank.Name, tank.Kb * Constants.SECperDAY);
            }

            _writer.WriteLine();
        }

        private void ComposeEnergy() {
            
            _writer.WriteLine(SectType.ENERGY.ParseStr());

            if (!_net.ECost.IsZero())
                _writer.WriteLine("GLOBAL PRICE {0}", _net.ECost);

            if (!_net.EPatId.Equals(""))
                _writer.WriteLine("GLOBAL PATTERN {0}", _net.EPatId);

            _writer.WriteLine("GLOBAL EFFIC {0}", _net.EPump);
            _writer.WriteLine("DEMAND CHARGE {0}", _net.DCost);

            foreach (Pump p  in  _net.Pumps) {
                if (p.ECost > 0.0)
                    _writer.WriteLine("PUMP {0} PRICE {1}", p.Name, p.ECost);

                if (p.EPat != null)
                    _writer.WriteLine("PUMP {0} PATTERN {1}", p.Name, p.EPat.Name);

                if (p.ECurve != null)
                    _writer.WriteLine("PUMP {0} EFFIC {1}", p.Name, p.ECurve.Name);
            }

            _writer.WriteLine();

        }

        private void ComposeTimes() {
            
            _writer.WriteLine(SectType.TIMES.ParseStr());
            _writer.WriteLine("DURATION {0}", _net.Duration.GetClockTime());
            _writer.WriteLine("HYDRAULIC TIMESTEP {0}", _net.HStep.GetClockTime());
            _writer.WriteLine("QUALITY TIMESTEP {0}", _net.QStep.GetClockTime());
            _writer.WriteLine("REPORT TIMESTEP {0}", _net.RStep.GetClockTime());
            _writer.WriteLine("REPORT START {0}", _net.RStart.GetClockTime());
            _writer.WriteLine("PATTERN TIMESTEP {0}", _net.PStep.GetClockTime());
            _writer.WriteLine("PATTERN START {0}", _net.PStart.GetClockTime());
            _writer.WriteLine("RULE TIMESTEP {0}", _net.RuleStep.GetClockTime());
            _writer.WriteLine("START CLOCKTIME {0}", _net.Tstart.GetClockTime());
            _writer.WriteLine("STATISTIC {0}", _net.TstatFlag.ParseStr());
            _writer.WriteLine();
        }

        private void ComposeOptions() {
            
            FieldsMap fMap = _net.FieldsMap;

            _writer.WriteLine(SectType.OPTIONS.ParseStr());
            _writer.WriteLine("UNITS               " + _net.FlowFlag);
            _writer.WriteLine("PRESSURE            " + _net.PressFlag);
            _writer.WriteLine("HEADLOSS            " + _net.FormFlag);

            if (!string.IsNullOrEmpty(_net.DefPatId))
                _writer.WriteLine("PATTERN             " + _net.DefPatId);

            switch (_net.HydFlag) {
                case HydType.USE:
                    _writer.WriteLine("HYDRAULICS USE      " + _net.HydFname);
                    break;
                case HydType.SAVE:
                    _writer.WriteLine("HYDRAULICS SAVE     " + _net.HydFname);
                    break;
            }

            if (_net.ExtraIter == -1)
                _writer.WriteLine("UNBALANCED          STOP");

            if (_net.ExtraIter >= 0)
                _writer.WriteLine("UNBALANCED          CONTINUE " + _net.ExtraIter);

            switch (_net.QualFlag) {
            case QualType.CHEM:
                _writer.WriteLine("QUALITY             {0} {1}", _net.ChemName, _net.ChemUnits);
                break;
            case QualType.TRACE:
                _writer.WriteLine("QUALITY             TRACE " + _net.TraceNode);
                break;
            case QualType.AGE:
                _writer.WriteLine("QUALITY             AGE");
                break;
            case QualType.NONE:
                _writer.WriteLine("QUALITY             NONE");
                break;
            }

            _writer.WriteLine("DEMAND MULTIPLIER {0}", _net.DMult);
            _writer.WriteLine("EMITTER EXPONENT  {0}", 1.0 / _net.QExp);
            _writer.WriteLine("VISCOSITY         {0}", _net.Viscos / Constants.VISCOS);
            _writer.WriteLine("DIFFUSIVITY       {0}", _net.Diffus / Constants.DIFFUS);
            _writer.WriteLine("SPECIFIC GRAVITY  {0}", _net.SpGrav);
            _writer.WriteLine("TRIALS            {0}", _net.MaxIter);
            _writer.WriteLine("ACCURACY          {0}", _net.HAcc);
            _writer.WriteLine("TOLERANCE         {0}", fMap.RevertUnit(FieldType.QUALITY, _net.Ctol));
            _writer.WriteLine("CHECKFREQ         {0}", _net.CheckFreq);
            _writer.WriteLine("MAXCHECK          {0}", _net.MaxCheck);
            _writer.WriteLine("DAMPLIMIT         {0}", _net.DampLimit);
            _writer.WriteLine();
        }

        private void ComposeExtraOptions() {
            var extraOptions = _net.ExtraOptions;

            if(extraOptions.Count == 0)
                return;

            foreach(var pair in extraOptions) {
                _writer.WriteLine(pair.Key + " " + pair.Value);
            }

            _writer.WriteLine();
        }

        private void ComposeReport() {

            _writer.WriteLine(SectType.REPORT.ParseStr());

            FieldsMap fMap = _net.FieldsMap;
            _writer.WriteLine("PAGESIZE       {0}", _net.PageSize);
            _writer.WriteLine("STATUS         " + _net.StatFlag);
            _writer.WriteLine("SUMMARY        " + (_net.SummaryFlag ? Keywords.w_YES : Keywords.w_NO));
            _writer.WriteLine("ENERGY         " + (_net.EnergyFlag ? Keywords.w_YES : Keywords.w_NO));

            switch (_net.NodeFlag) {
            case ReportFlag.FALSE:
                _writer.WriteLine("NODES NONE");
                break;
            case ReportFlag.TRUE:
                _writer.WriteLine("NODES ALL");
                break;
            case ReportFlag.SOME: {
                int j = 0;
                foreach (Node node  in  _net.Nodes) {
                    if (node.RptFlag) {
                        // if (j % 5 == 0) buffer.WriteLine("NODES "); // BUG: Baseform bug

                        if (j % 5 == 0) {
                            _writer.WriteLine();
                            _writer.Write("NODES ");
                        }

                        _writer.Write("{0} ", node.Name);
                        j++;
                    }
                }
                break;
            }
            }

            switch (_net.LinkFlag) {
            case ReportFlag.FALSE:
                _writer.WriteLine("LINKS NONE");
                break;
            case ReportFlag.TRUE:
                _writer.WriteLine("LINKS ALL");
                break;
            case ReportFlag.SOME: {
                int j = 0;
                foreach (Link link  in  _net.Links) {
                    if (link.RptFlag) {
                        // if (j % 5 == 0) buffer.write("LINKS \n"); // BUG: Baseform bug
                        if (j % 5 == 0) {
                            _writer.WriteLine();
                            _writer.Write("LINKS ");
                        }

                        _writer.Write("{0} ", link.Name);
                        j++;
                    }
                }
                break;
            }
            }

            for (FieldType i = 0; i < FieldType.FRICTION; i++) {
                Field f = fMap.GetField(i);

                if (!f.Enabled) {
                    _writer.WriteLine("{0,-19} NO", f.Name);
                    continue;
                }

                _writer.WriteLine("{0,-19} PRECISION {1}", f.Name, f.Precision);

                if (f.GetRptLim(RangeType.LOW) < Constants.BIG)
                    _writer.WriteLine("{0,-19} BELOW {1:0.######}", f.Name, f.GetRptLim(RangeType.LOW));

                if (f.GetRptLim(RangeType.HI) > -Constants.BIG)
                    _writer.WriteLine("{0,-19} ABOVE {1:0.######}", f.Name, f.GetRptLim(RangeType.HI));
            }

            _writer.WriteLine();
        }

        private void ComposeCoordinates() {
            _writer.WriteLine(SectType.COORDINATES.ParseStr());
            _writer.WriteLine(COORDINATES_SUBTITLE);

            foreach (Node node  in  _net.Nodes) {
                if (!node.Coordinate.IsInvalid) {
                    _writer.WriteLine(
                            " {0,-16}\t{1,-12}\t{2,-12}",
                            node.Name,
                            node.Coordinate.X,
                            node.Coordinate.Y);
                }
            }
            _writer.WriteLine();
        }


        private void ComposeLabels() {
            _writer.WriteLine(SectType.LABELS.ParseStr());
            _writer.WriteLine(";X-Coord\tY-Coord\tLabel & Anchor Node");

            foreach (Label label  in  _net.Labels) {
                _writer.WriteLine(
                        " {0,-16}\t{1,-16}\t\"{2}\" {3,-16}",
                        label.Position.X,
                        label.Position.Y,
                        label.Text,
                        // label.AnchorNodeId // TODO: add AnchorNodeId property to label
                        ""
                    );

            }
            _writer.WriteLine();
        }

        private void ComposeVertices() {
            _writer.WriteLine(SectType.VERTICES.ParseStr());
            _writer.WriteLine(";Link\tX-Coord\tY-Coord");

            foreach (Link link  in  _net.Links) {
                if (link.Vertices.Count == 0)
                    continue;

                foreach (EnPoint p  in  link.Vertices) {
                    _writer.WriteLine(" {0,-16}\t{1,-16}\t{2,-16}", link.Name, p.X, p.Y);
                }
            }

            _writer.WriteLine();
        }

        private void ComposeRules() {
            _writer.WriteLine(SectType.RULES.ParseStr());
            _writer.WriteLine();

            foreach (Rule r  in  _net.Rules) {
                _writer.WriteLine("RULE " + r.Name);
                foreach (string s  in  r.Code)
                    _writer.WriteLine(s);
                _writer.WriteLine();
            }
            _writer.WriteLine();
        }
        
    }

}