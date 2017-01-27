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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

using Epanet.Enums;
using Epanet.Network.Structures;
using Epanet.Util;

namespace Epanet.Network.IO.Output {

    public class XmlComposer:OutputComposer {
        private readonly bool _gzip;
        private XmlWriter _writer;
        private Network _net;

        public XmlComposer(bool gzip) { _gzip = gzip; }

        public override void Composer(Network net, string f) {
            _net = net;

            try {
                Stream stream = new FileStream(f, FileMode.Create, FileAccess.Write, FileShare.Read, 0x1000, FileOptions.SequentialScan);
                if (_gzip) stream = new GZipStream(stream, CompressionMode.Compress, false);

                var settings = new XmlWriterSettings {
                    Indent = true,
                    Encoding = Encoding.UTF8,
                    CloseOutput = true
                };

                using (_writer = XmlWriter.Create(stream, settings)) {
                    _writer.WriteStartDocument();
                    _writer.WriteStartElement("network");

                    ComposeTitle();
                    ComposeJunctions();
                    ComposeReservoirs();
                    ComposeTanks();
                    ComposePipes();
                    ComposePumps();
                    ComposeValves();
                    ComposeStatus();
                    ComposePatterns();
                    ComposeCurves();
                    ComposeControls();
                    ComposeQuality();
                    ComposeSource();
                    ComposeReaction();
                    ComposeEnergy();
                    ComposeTimes();
                    ComposeOptions();
                    ComposeExtraOptions();
                    ComposeReport();
                    ComposeLabels();
                    ComposeRules();

                    _writer.WriteEndElement();
                    _writer.WriteEndDocument();
                }
            }
            catch (IOException e) {
                Debug.Print(e.ToString());
                throw new ENException(ErrorCode.Err308);
            }
        }

        private void ComposeTitle() {
            if (_net.Title.Count <= 0) return;

            _writer.WriteStartElement(SectType.TITLE.ToString().ToLower());
            foreach(string s in _net.Title) {
                _writer.WriteElementString("line", s);
            }
            _writer.WriteEndElement();
        }

        private void ComposeElement(Element el) {
            if (!string.IsNullOrEmpty(el.Comment))
                _writer.WriteComment(el.Comment);

            if(!string.IsNullOrEmpty(el.Tag))
                _writer.WriteElementString("tag", el.Tag);            
        }

       
        private void ComposeJunctions() {
            if (!_net.Junctions.Any()) return;

            var fMap = _net.FieldsMap;

            double dUcf = fMap.GetUnits(FieldType.DEMAND);
            double fUcf = fMap.GetUnits(FieldType.FLOW);
            double pUcf = fMap.GetUnits(FieldType.PRESSURE);
            double eUcf = fMap.GetUnits(FieldType.ELEV);

            _writer.WriteStartElement(SectType.JUNCTIONS.ToString().ToLower());
          
            foreach(Node node in _net.Junctions) {
                _writer.WriteStartElement("node");
                _writer.WriteAttributeString("name", node.Name);
                _writer.WriteAttributeString("elevation", XmlConvert.ToString(eUcf * node.Elevation));
                
                double ke = node.Ke;

                if(ke != 0.0) {
                    ke = fUcf / Math.Pow(pUcf * ke, (1.0 / _net.QExp));
                    _writer.WriteAttributeString("emmiter", XmlConvert.ToString(ke));
                }

                if (!node.Position.IsInvalid) {
                    _writer.WriteAttributeString("x", XmlConvert.ToString(node.Position.X));
                    _writer.WriteAttributeString("y", XmlConvert.ToString(node.Position.Y));
                }

                foreach(Demand d in node.Demands) {
                    _writer.WriteStartElement("demand");

                    _writer.WriteAttributeString("base", XmlConvert.ToString(dUcf * d.Base));
                    if(d.pattern != null) {
                        var patName = d.pattern.Name;
                        if (!string.IsNullOrEmpty(patName)
                            && !_net.DefPatId.Equals(patName, StringComparison.OrdinalIgnoreCase))
                        {
                            _writer.WriteAttributeString("pattern", patName);
                        }
                    }

                    _writer.WriteEndElement();
                }
                


                ComposeElement(node);

                _writer.WriteEndElement();

            }

            _writer.WriteEndElement();
        }

        private void ComposeReservoirs() {
            
            if(!_net.Reservoirs.Any())
                return;

            FieldsMap fMap = _net.FieldsMap;

            _writer.WriteStartElement(SectType.RESERVOIRS.ToString().ToLower());

            foreach(Tank r in _net.Reservoirs) {
                _writer.WriteStartElement("node");
                _writer.WriteAttributeString("name", r.Name);
                _writer.WriteAttributeString("head", XmlConvert.ToString(fMap.RevertUnit(FieldType.ELEV, r.Elevation)));
                

                if(r.Pattern != null)
                    _writer.WriteAttributeString("pattern", r.Pattern.Name);

                if (!r.Position.IsInvalid) {
                    _writer.WriteAttributeString("x", XmlConvert.ToString(r.Position.X));
                    _writer.WriteAttributeString("y", XmlConvert.ToString(r.Position.Y));
                }

                ComposeElement(r);

                _writer.WriteEndElement();
                
            }

            _writer.WriteEndElement();
            
        }

        private void ComposeTanks() {

            if(!_net.Tanks.Any())
                return;

            FieldsMap fMap = _net.FieldsMap;

            _writer.WriteStartElement(SectType.TANKS.ToString().ToLower());

            foreach(Tank tank in _net.Tanks) {
                double vmin = tank.Vmin;
                if(Math.Round(vmin / tank.Area) == Math.Round(tank.Hmin - tank.Elevation))
                    vmin = 0;

                double elev = fMap.RevertUnit(FieldType.ELEV, tank.Elevation);
                double initlvl = fMap.RevertUnit(FieldType.ELEV, tank.H0 - tank.Elevation);
                double minlvl = fMap.RevertUnit(FieldType.ELEV, tank.Hmin - tank.Elevation);
                double maxlvl = fMap.RevertUnit(FieldType.ELEV, tank.Hmax - tank.Elevation);
                double diam = fMap.RevertUnit(FieldType.ELEV, 2 * Math.Sqrt(tank.Area / Math.PI));
                double minvol = fMap.RevertUnit(FieldType.VOLUME, vmin);

                _writer.WriteStartElement("node");
                _writer.WriteAttributeString("name", tank.Name);
                _writer.WriteAttributeString("elevation", XmlConvert.ToString(elev));
                _writer.WriteAttributeString("InitLevel", XmlConvert.ToString(initlvl));
                _writer.WriteAttributeString("MinLevel", XmlConvert.ToString(minlvl));
                _writer.WriteAttributeString("MaxLevel", XmlConvert.ToString(maxlvl));
                _writer.WriteAttributeString("diameter", XmlConvert.ToString(diam));
                _writer.WriteAttributeString("MinVolume", XmlConvert.ToString(minvol));

                if(tank.Vcurve != null)
                    _writer.WriteAttributeString("volcurve", tank.Vcurve.Name);

                if (!tank.Position.IsInvalid) {
                    _writer.WriteAttributeString("x", XmlConvert.ToString(tank.Position.X));
                    _writer.WriteAttributeString("y", XmlConvert.ToString(tank.Position.Y));
                }

                if (tank.MixModel != MixType.MIX1) {
                    _writer.WriteStartElement(SectType.MIXING.ToString().ToLower());
                    _writer.WriteAttributeString("model", tank.MixModel.ParseStr());
                    if (tank.MixModel == MixType.MIX2)
                        _writer.WriteAttributeString("compartment", XmlConvert.ToString(tank.V1Max / tank.Vmax));

                    _writer.WriteEndElement();                    
                }

                ComposeElement(tank);

                _writer.WriteEndElement();
            }

            _writer.WriteEndElement();
        }


        private void ComposePipes() {
            
            var pipes = _net.Links.Where(x => x.Type == LinkType.PIPE || x.Type == LinkType.CV).ToArray();

            if(pipes.Length == 0)
                return;

            FieldsMap fMap = _net.FieldsMap;

            _writer.WriteStartElement(SectType.PIPES.ToString().ToLower());
            
            foreach(Link link in pipes) {
                double d = link.Diameter;
                double kc = link.Kc;
                if(_net.FormFlag == FormType.DW)
                    kc = fMap.RevertUnit(FieldType.ELEV, kc * 1000.0);

                double km = link.Km * Math.Pow(d, 4.0) / 0.02517;

                _writer.WriteStartElement("link");
                _writer.WriteAttributeString("name", link.Name);
                _writer.WriteAttributeString("node1", link.FirstNode.Name);
                _writer.WriteAttributeString("node2", link.SecondNode.Name);
                _writer.WriteAttributeString("length", XmlConvert.ToString(fMap.RevertUnit(FieldType.LENGTH, link.Lenght)));
                _writer.WriteAttributeString("diameter", XmlConvert.ToString(fMap.RevertUnit(FieldType.DIAM, d)));
                _writer.WriteAttributeString("roughness", XmlConvert.ToString(kc));
                _writer.WriteAttributeString("minorloss", XmlConvert.ToString(km));

                if(link.Type == LinkType.CV)
                    _writer.WriteAttributeString("status", LinkType.CV.ToString());
                else if(link.Status == StatType.CLOSED)
                    _writer.WriteAttributeString("status", StatType.CLOSED.ToString());
                else if(link.Status == StatType.OPEN)
                    _writer.WriteAttributeString("status", StatType.OPEN.ToString());

                ComposeVertices(link);

                ComposeElement(link);

                _writer.WriteEndElement();
            }

            _writer.WriteEndElement();
        }


        private void ComposePumps() {

            var pumps = _net.Pumps.ToArray();

            if(pumps.Length == 0)
                return;
            
            FieldsMap fMap = _net.FieldsMap;

            _writer.WriteStartElement(SectType.PUMPS.ToString().ToLower());
            
            foreach(Pump pump in pumps) {
                _writer.WriteStartElement("link");
                _writer.WriteAttributeString("name", pump.Name);
                _writer.WriteAttributeString("node1", pump.FirstNode.Name);
                _writer.WriteAttributeString("node2", pump.SecondNode.Name);

                bool oldFormat = false;
                
                if (pump.Ptype == PumpType.CONST_HP) {
                    // Pump has constant power
                    _writer.WriteAttributeString("power", XmlConvert.ToString(pump.Km));
                }
                else if (pump.HCurve != null) {
                    // Pump has a head curve
                    _writer.WriteAttributeString("head", pump.HCurve.Name);
                }
                else {
                    // Old format used for pump curve
                    _writer.WriteAttributeString("h0", XmlConvert.ToString(fMap.RevertUnit(FieldType.HEAD, -pump.H0)));
                    _writer.WriteAttributeString("h1", XmlConvert.ToString(fMap.RevertUnit(FieldType.HEAD, -pump.H0 - pump.FlowCoefficient * Math.Pow(pump.Q0, pump.N))));
                    _writer.WriteAttributeString("q1", XmlConvert.ToString(fMap.RevertUnit(FieldType.FLOW, pump.Q0)));
                    _writer.WriteAttributeString("h2", XmlConvert.ToString(0.0));
                    _writer.WriteAttributeString("q2", XmlConvert.ToString(fMap.RevertUnit(FieldType.FLOW, pump.Qmax)));

                    oldFormat = true;
                }

                if (!oldFormat) {
                    if (pump.UPat != null)
                        _writer.WriteAttributeString("pattern", pump.UPat.Name);

                    if (pump.Kc != 1.0)
                        _writer.WriteAttributeString("speed", XmlConvert.ToString(pump.Kc));
                }

                ComposeElement(pump);
                ComposeVertices(pump);
                _writer.WriteEndElement();
            }

            _writer.WriteEndElement();
        }

        private void ComposeVertices(Link link) {
            if (link.Vertices.Count == 0) return;

            foreach(EnPoint p in link.Vertices) {
                _writer.WriteStartElement("point");
                _writer.WriteAttributeString("x", XmlConvert.ToString(p.X));
                _writer.WriteAttributeString("y", XmlConvert.ToString(p.Y));
                _writer.WriteEndElement();
            }            
        }


        private void ComposeValves() {
            
            var valves = _net.Valves.ToArray();

            if(valves.Length == 0)
                return;

            FieldsMap fMap = _net.FieldsMap;
            
            _writer.WriteStartElement(SectType.VALVES.ToString().ToLower());
            
            foreach(Valve valve in valves) {
                double d = valve.Diameter;
                double kc = valve.Kc;
                if(kc.IsMissing())
                    kc = 0.0;
                
                switch(valve.Type) {
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

                _writer.WriteStartElement("link");
                _writer.WriteAttributeString("name", valve.Name);
                _writer.WriteAttributeString("node1", valve.FirstNode.Name);
                _writer.WriteAttributeString("node2", valve.SecondNode.Name);
                _writer.WriteAttributeString("diameter", XmlConvert.ToString(fMap.RevertUnit(FieldType.DIAM, d)));
                _writer.WriteAttributeString("type", valve.Type.ParseStr());

                if (valve.Type == LinkType.GPV && valve.Curve != null) {
                    _writer.WriteAttributeString("setting", valve.Curve.Name);
                }
                else {
                    _writer.WriteAttributeString("setting", XmlConvert.ToString(kc));
                }

                _writer.WriteAttributeString("minorloss", XmlConvert.ToString(km));

                ComposeElement(valve);
                ComposeVertices(valve);

                _writer.WriteEndElement();
            }

            _writer.WriteEndElement();
        }

   


        private void ComposeStatus() {

            if(_net.Links.Count == 0)
                return;

            _writer.WriteStartElement(SectType.STATUS.ToString().ToLower());
            
            foreach(Link link in _net.Links) {
                if(link.Type <= LinkType.PUMP) {
                    if (link.Status == StatType.CLOSED) {
                        _writer.WriteStartElement("link");
                        _writer.WriteAttributeString("name", link.Name);
                        _writer.WriteAttributeString("setting", link.Status.ToString());
                        _writer.WriteEndElement();
                    }
                    else if(link.Type == LinkType.PUMP) {
                        // Write pump speed here for pumps with old-style pump curve input
                        Pump pump = (Pump)link;
                        if (pump.HCurve == null &&
                            pump.Ptype != PumpType.CONST_HP &&
                            pump.Kc != 1.0) {
                            _writer.WriteStartElement("link");
                            _writer.WriteAttributeString("name", link.Name);
                            _writer.WriteAttributeString("setting", XmlConvert.ToString(link.Kc));
                            _writer.WriteEndElement();
                        }
                    }
                }
                else if(link.Kc.IsMissing()) {
                    // Write fixed-status PRVs & PSVs (setting = MISSING)
                    switch (link.Status) {
                    case StatType.OPEN:
                    case StatType.CLOSED:
                        _writer.WriteStartElement("link");
                        _writer.WriteAttributeString("name", link.Name);
                        _writer.WriteAttributeString("setting", link.Status.ToString());
                        _writer.WriteEndElement();
                        break;
                    }

                }

            }

            _writer.WriteEndElement();
        }

        private void ComposePatterns() {

            var pats = _net.Patterns;

            if(pats.Count <= 1)
                return;

            _writer.WriteStartElement(SectType.PATTERNS.ToString().ToLower());

            for(int i = 1; i < pats.Count; i++) {
                Pattern pat = pats[i];

                _writer.WriteStartElement("pattern");
                _writer.WriteAttributeString("name", pat.Name);

                for(int j = 0; j < pats[i].Count; j++) {
                    _writer.WriteStartElement("factor");
                    _writer.WriteAttributeString("value", XmlConvert.ToString(pat[j]));
                    _writer.WriteEndElement();
                }

                _writer.WriteEndElement();
            }

            _writer.WriteEndElement();
        }

        private void ComposeCurves() {

            var curves = _net.Curves;

            if(curves.Count == 0)
                return;

            _writer.WriteStartElement(SectType.CURVES.ToString().ToLower());

            foreach(Curve curve in curves) {
                _writer.WriteStartElement("curve");
                _writer.WriteAttributeString("name", curve.Name);

                foreach(var pt in curve) {
                    _writer.WriteStartElement("point");
                    _writer.WriteAttributeString("x", XmlConvert.ToString(pt.X));
                    _writer.WriteAttributeString("y", XmlConvert.ToString(pt.Y));
                    _writer.WriteEndElement();
                }

                _writer.WriteEndElement();
            }

            _writer.WriteEndElement();
        }

        private void ComposeControls() {
            var controls = _net.Controls;
            FieldsMap fmap = _net.FieldsMap;

            if(controls.Count == 0)
                return;

            _writer.WriteStartElement(SectType.CONTROLS.ToString().ToLower());

            foreach(Control control in controls) {
                // Check that controlled link exists
                if(control.Link == null)
                    continue;

                _writer.WriteStartElement("control");

                // Get text of control's link status/setting
                if (control.Setting.IsMissing()) {
                    _writer.WriteAttributeString("link", control.Link.Name);
                    _writer.WriteAttributeString("status", control.Status.ToString());
                }
                else {
                    double kc = control.Setting;
                    switch(control.Link.Type) {
                    case LinkType.PRV:
                    case LinkType.PSV:
                    case LinkType.PBV:
                    kc = fmap.RevertUnit(FieldType.PRESSURE, kc);
                    break;
                    case LinkType.FCV:
                    kc = fmap.RevertUnit(FieldType.FLOW, kc);
                    break;
                    }

                    _writer.WriteAttributeString("link", control.Link.Name);
                    _writer.WriteAttributeString("status", XmlConvert.ToString(kc));
                }


                switch(control.Type) {
                // Print level control
                case ControlType.LOWLEVEL:
                case ControlType.HILEVEL:
                double kc = control.Grade - control.Node.Elevation;
                kc = fmap.RevertUnit(control.Node.Type == NodeType.JUNC ? FieldType.PRESSURE : FieldType.HEAD, kc);
                
                _writer.WriteAttributeString("type", control.Type.ParseStr());
                _writer.WriteAttributeString("node", control.Node.Name);
                _writer.WriteAttributeString("value", XmlConvert.ToString(kc));
                
                break;

                case ControlType.TIMER:
                case ControlType.TIMEOFDAY:
                // Print timer control
                // Print time-of-day control
                _writer.WriteAttributeString("type", control.Type.ParseStr());
                _writer.WriteAttributeString("value", XmlConvert.ToString(control.Time));

                break;

                }

                _writer.WriteEndElement();
            }

            _writer.WriteEndElement();
        }

        private void ComposeQuality() {
            FieldsMap fmap = _net.FieldsMap;

            if(_net.Nodes.Count == 0)
                return;

            _writer.WriteStartElement(SectType.QUALITY.ToString().ToLower());
            
            foreach(Node node in _net.Nodes) {
                if (node.C0 == 0.0) continue;

                _writer.WriteStartElement("node");
                _writer.WriteAttributeString("name", node.Name);
                _writer.WriteAttributeString("value", XmlConvert.ToString(fmap.RevertUnit(FieldType.QUALITY, node.C0)));
                _writer.WriteEndElement();
            }

            _writer.WriteEndElement();
        }

        private void ComposeSource() {

            if(_net.Nodes.Count == 0)
                return;

            _writer.WriteStartElement(SectType.SOURCES.ToString().ToLower());
            
            foreach(Node node in _net.Nodes) {
                QualSource source = node.QualSource;
                if(source == null)
                    continue;

                _writer.WriteStartElement("node");
                _writer.WriteAttributeString("name", node.Name);
                _writer.WriteAttributeString("type", source.Type.ToString());
                _writer.WriteAttributeString("quality", XmlConvert.ToString(source.C0));


                if(source.Pattern != null)
                    _writer.WriteAttributeString("pattern", source.Pattern.Name);
                    
                _writer.WriteEndElement();
            }

            _writer.WriteEndElement();
        }


    

        private void ComposeReaction() {
            
            _writer.WriteStartElement(SectType.REACTIONS.ToString().ToLower());

            WriteOption("order bulk", XmlConvert.ToString(_net.BulkOrder));
            WriteOption("order wall", XmlConvert.ToString(_net.WallOrder));
            WriteOption("order tank", XmlConvert.ToString(_net.TankOrder));
            WriteOption("global bulk", XmlConvert.ToString(_net.KBulk * Constants.SECperDAY));
            WriteOption("global wall", XmlConvert.ToString(_net.KWall * Constants.SECperDAY));
            WriteOption("limiting potential", XmlConvert.ToString(_net.CLimit));
            WriteOption("roughness correlation", XmlConvert.ToString(_net.RFactor));



            foreach(Link link in _net.Links) {
                if(link.Type > LinkType.PIPE)
                    continue;

                if (link.Kb != _net.KBulk) {
                    _writer.WriteStartElement("option");
                    _writer.WriteAttributeString("name", "bulk");
                    _writer.WriteAttributeString("link", link.Name);
                    _writer.WriteAttributeString("value", XmlConvert.ToString(link.Kb * Constants.SECperDAY));
                    _writer.WriteEndElement();                    
                }

                if (link.Kw != _net.KWall) {
                    _writer.WriteStartElement("option");
                    _writer.WriteAttributeString("name", "wall");
                    _writer.WriteAttributeString("link", link.Name);
                    _writer.WriteAttributeString("value", XmlConvert.ToString(link.Kw * Constants.SECperDAY));
                    _writer.WriteEndElement();
                }
            }

            foreach(Tank tank in _net.Tanks) {
                if (tank.Kb != _net.KBulk) {
                    _writer.WriteStartElement("option");
                    _writer.WriteAttributeString("name", "tank");
                    _writer.WriteAttributeString("name", tank.Name);
                    _writer.WriteAttributeString("value", XmlConvert.ToString(tank.Kb * Constants.SECperDAY));
                    _writer.WriteEndElement();
                }
            }

            _writer.WriteEndElement();
        }

        private void ComposeEnergy() {
            
            _writer.WriteStartElement(SectType.ENERGY.ToString().ToLower());

            if (_net.ECost != 0.0)
                WriteOption("global price", XmlConvert.ToString(_net.ECost));

            if (!string.IsNullOrEmpty(_net.EPatId))
                WriteOption("global pattern", _net.EPatId);

            WriteOption("global effic", XmlConvert.ToString(_net.EPump));
            WriteOption("demand charge", XmlConvert.ToString(_net.DCost));

            foreach(Pump pump in _net.Pumps) {
                if (pump.ECost > 0.0) {
                    _writer.WriteStartElement("option");
                    _writer.WriteAttributeString("pump", pump.Name);
                    _writer.WriteAttributeString("price", XmlConvert.ToString(pump.ECost));
                    _writer.WriteEndElement();
                }

                if (pump.EPat != null) {
                    _writer.WriteStartElement("option");
                    _writer.WriteAttributeString("pump", pump.Name);
                    _writer.WriteAttributeString("pattern", pump.EPat.Name);
                    _writer.WriteEndElement();
                }

                if (pump.ECurve != null) {
                    _writer.WriteStartElement("option");
                    _writer.WriteAttributeString("pump", pump.Name);
                    _writer.WriteAttributeString("effic", pump.ECurve.Name);
                    _writer.WriteEndElement();
                }
            }

            _writer.WriteEndElement();

        }

        private void WriteOption(string name, string value) {
            _writer.WriteStartElement("option");
            _writer.WriteAttributeString("name", name);
            _writer.WriteAttributeString("value", value);
            _writer.WriteEndElement();
        }

        private void ComposeTimes() {
            
            _writer.WriteStartElement(SectType.TIMES.ToString().ToLower());

            WriteOption("DURATION", _net.Duration.GetClockTime());
            WriteOption("HYDRAULIC_TIMESTEP", _net.HStep.GetClockTime());
            WriteOption("QUALITY_TIMESTEP", _net.QStep.GetClockTime());
            WriteOption("REPORT_TIMESTEP", _net.RStep.GetClockTime());
            WriteOption("REPORT_START", _net.RStart.GetClockTime());
            WriteOption("PATTERN_TIMESTEP", _net.PStep.GetClockTime());
            WriteOption("PATTERN_START", _net.PStart.GetClockTime());
            WriteOption("RULE_TIMESTEP", _net.RuleStep.GetClockTime());
            WriteOption("START_CLOCKTIME", _net.Tstart.GetClockTime());
            WriteOption("STATISTIC", _net.TstatFlag.ParseStr());

            _writer.WriteEndElement();
        }



        private void ComposeOptions() {
            FieldsMap fMap = _net.FieldsMap;

            _writer.WriteStartElement(SectType.OPTIONS.ToString().ToLower());

            WriteOption("units", _net.FlowFlag.ParseStr());
            WriteOption("pressure", _net.PressFlag.ParseStr());
            WriteOption("headloss", _net.FormFlag.ParseStr());

            if(!string.IsNullOrEmpty(_net.DefPatId))
                WriteOption("pattern", _net.DefPatId);

            switch (_net.HydFlag) {
            case HydType.USE:
                WriteOption("hydraulics use", _net.HydFname);
                break;
            case HydType.SAVE:
                WriteOption("hydraulics save", _net.HydFname);
                break;
            }

            if (_net.ExtraIter == -1) {
                WriteOption("unbalanced stop", "");
            }
            else if (_net.ExtraIter >= 0) {
                WriteOption("unbalanced continue", _net.ExtraIter.ToString());
            }

            switch (_net.QualFlag) {
            case QualType.CHEM:
            _writer.WriteStartElement("option");
                _writer.WriteAttributeString("name", "quality");
                _writer.WriteAttributeString("value", "chem");
                _writer.WriteAttributeString("chemname", _net.ChemName);
                _writer.WriteAttributeString("chemunits", _net.ChemUnits);
                _writer.WriteEndElement();
                break;
            case QualType.TRACE:
            _writer.WriteStartElement("option");
                _writer.WriteAttributeString("name", "quality");
                _writer.WriteAttributeString("value", "trace");
                _writer.WriteAttributeString("node", _net.TraceNode);
                _writer.WriteEndElement();
                break;
            case QualType.AGE:
                WriteOption("quality", "age");
                break;
            case QualType.NONE:
                WriteOption("quality", "none");
                break;
            }

            WriteOption("demand_multiplier", XmlConvert.ToString(_net.DMult));
            WriteOption("emitter_exponent", XmlConvert.ToString(1.0 / _net.QExp));
            WriteOption("viscosity", XmlConvert.ToString(_net.Viscos / Constants.VISCOS));
            WriteOption("diffusivity", XmlConvert.ToString(_net.Diffus / Constants.DIFFUS));
            WriteOption("specific_gravity", XmlConvert.ToString(_net.SpGrav));
            WriteOption("trials", _net.MaxIter.ToString());
            WriteOption("accuracy", _net.HAcc.ToString(CultureInfo.InvariantCulture));
            WriteOption("tolerance", fMap.RevertUnit(FieldType.QUALITY, _net.Ctol).ToString(CultureInfo.InvariantCulture));
            WriteOption("checkfreq", _net.CheckFreq.ToString());
            WriteOption("maxcheck", _net.MaxCheck.ToString());
            WriteOption("damplimit", _net.DampLimit.ToString(CultureInfo.InvariantCulture));

            ComposeExtraOptions();

            _writer.WriteEndElement();
        }

        private void ComposeExtraOptions() {
            var extraOptions = _net.ExtraOptions;

            if(extraOptions.Count == 0)
                return;

            foreach(var pair in extraOptions) {
                WriteOption(pair.Key, pair.Value);
            }

            _writer.WriteEndElement();
        }

        private void ComposeReport() {

            _writer.WriteStartElement(SectType.REPORT.ToString().ToLower());

            FieldsMap fMap = _net.FieldsMap;
            WriteOption("pagesize", _net.PageSize.ToString());
            WriteOption("status", _net.StatFlag.ParseStr());
            WriteOption("summary", (_net.SummaryFlag ? Keywords.w_YES : Keywords.w_NO));
            WriteOption("energy", (_net.EnergyFlag ? Keywords.w_YES : Keywords.w_NO));

            switch (_net.NodeFlag) {
            case ReportFlag.FALSE:
                WriteOption("nodes", "none");
                break;
            case ReportFlag.TRUE:
                WriteOption("nodes", "all");
                break;
            case ReportFlag.SOME: {
                    _writer.WriteStartElement("option");
                    _writer.WriteAttributeString("name", "nodes");
                    _writer.WriteAttributeString("value", "some");

                foreach (Node node in _net.Nodes) {
                    if (node.RptFlag) {
                        _writer.WriteStartElement("node");
                        _writer.WriteAttributeString("name", node.Name);
                        _writer.WriteEndElement();
                    }
                }

                _writer.WriteEndElement();
                break;
            }
            }

            switch (_net.LinkFlag) {
            case ReportFlag.FALSE:
                WriteOption("links", "none");
                break;
            case ReportFlag.TRUE:
                WriteOption("links", "all");
                break;
            case ReportFlag.SOME:
                _writer.WriteStartElement("option");
                _writer.WriteAttributeString("name", "links");
                _writer.WriteAttributeString("value", "some");

                foreach (Link link in _net.Links) {
                    if (link.RptFlag) {
                        _writer.WriteStartElement("link");
                        _writer.WriteAttributeString("name", link.Name);
                        _writer.WriteEndElement();
                    }
                }

                _writer.WriteEndElement();

                break;
            }

            for(FieldType i = 0; i < FieldType.FRICTION; i++) {
                Field f = fMap.GetField(i);

                if (!f.Enabled) {
                    _writer.WriteStartElement("field");
                    _writer.WriteAttributeString("name", f.Name);
                    _writer.WriteAttributeString("enabled", "false");
                    _writer.WriteEndElement();
                    continue;
                }

                _writer.WriteStartElement("field");
                _writer.WriteAttributeString("name", f.Name);
                _writer.WriteAttributeString("enabled", "true");
                _writer.WriteAttributeString("precision", f.Precision.ToString());

                if (f.GetRptLim(RangeType.LOW) < Constants.BIG)
                    _writer.WriteAttributeString("below", XmlConvert.ToString(f.GetRptLim(RangeType.LOW)));

                if (f.GetRptLim(RangeType.HI) > -Constants.BIG)
                    _writer.WriteAttributeString("above", XmlConvert.ToString(f.GetRptLim(RangeType.HI)));

                _writer.WriteEndElement();
            }

            _writer.WriteEndElement();
        }

        private void ComposeLabels() {
            if (_net.Labels.Count == 0)
                return;

            _writer.WriteStartElement(SectType.LABELS.ToString().ToLower());
            
            foreach(Label label in _net.Labels) {
                _writer.WriteStartElement("label");
                _writer.WriteAttributeString("x", XmlConvert.ToString(label.Position.X));
                _writer.WriteAttributeString("y", XmlConvert.ToString(label.Position.Y));
                // this.buffer.WriteAttributeString("node", label.AnchorNodeId); // TODO: add AnchorNodeId property to label

                _writer.WriteElementString("text", label.Text);

                _writer.WriteEndElement();
            }

            _writer.WriteEndElement();
        }

       

        private void ComposeRules() {
            if(_net.Rules.Count == 0)
                return;

            _writer.WriteStartElement(SectType.RULES.ToString().ToLower());
            
            foreach(Rule r in _net.Rules) {
                _writer.WriteStartElement("rule");
                _writer.WriteAttributeString("name", r.Name);

                foreach(string s in r.Code)
                    _writer.WriteElementString("code", s);
                    
                _writer.WriteEndElement();
            }

            _writer.WriteEndElement();
        }
    }

}