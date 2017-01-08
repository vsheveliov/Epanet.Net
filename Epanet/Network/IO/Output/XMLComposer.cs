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

    public class XMLComposer:OutputComposer {
        private readonly bool gzip;
        private XmlWriter writer;
        private Network net;

        public XMLComposer(bool gzip) {
            // this.gzip = gzip;
            this.gzip = true;
        }

        public override void Composer(Network net_, string f) {
            this.net = net_;

            try {
                Stream stream = new FileStream(f, FileMode.Create, FileAccess.Write, FileShare.Read, 0x1000, FileOptions.SequentialScan);
                if (this.gzip) stream = new GZipStream(stream, CompressionMode.Compress, false);

                var settings = new XmlWriterSettings {
                    Indent = true,
                    Encoding = Encoding.UTF8,
                    CloseOutput = true
                };

                using (this.writer = XmlWriter.Create(stream, settings)) {
                    this.writer.WriteStartDocument();
                    this.writer.WriteStartElement("network");

                    this.ComposeTitle();
                    this.ComposeJunctions();
                    this.ComposeReservoirs();
                    this.ComposeTanks();
                    this.ComposePipes();
                    this.ComposePumps();
                    this.ComposeValves();
                    this.ComposeStatus();
                    this.ComposePatterns();
                    this.ComposeCurves();
                    this.ComposeControls();
                    this.ComposeQuality();
                    this.ComposeSource();
                    this.ComposeReaction();
                    this.ComposeEnergy();
                    this.ComposeTimes();
                    this.ComposeOptions();
                    this.ComposeExtraOptions();
                    this.ComposeReport();
                    this.ComposeLabels();
                    this.ComposeRules();

                    this.writer.WriteEndElement();
                    this.writer.WriteEndDocument();
                }
            }
            catch (IOException e) {
                Debug.Print(e.ToString());
                throw new ENException(ErrorCode.Err308);
            }
        }

        private void ComposeTitle() {
            if (this.net.TitleText.Count <= 0) return;

            this.writer.WriteStartElement(SectType.TITLE.ToString().ToLower());
            foreach(string s in this.net.TitleText) {
                this.writer.WriteElementString("line", s);
            }
            this.writer.WriteEndElement();
        }

        private void ComposeElement(Element el) {
            if(!string.IsNullOrEmpty(el.Comment))
                this.writer.WriteElementString("comment", el.Comment);

            if(!string.IsNullOrEmpty(el.Tag))
                this.writer.WriteElementString("tag", el.Tag);            
        }

       
        private void ComposeJunctions() {
            if (!net.Junctions.Any()) return;

            var fMap = net.FieldsMap;

            double dUcf = fMap.GetUnits(FieldType.DEMAND);
            double fUcf = fMap.GetUnits(FieldType.FLOW);
            double pUcf = fMap.GetUnits(FieldType.PRESSURE);
            double eUcf = fMap.GetUnits(FieldType.ELEV);

            this.writer.WriteStartElement(SectType.JUNCTIONS.ToString().ToLower());
          
            foreach(Node node in net.Junctions) {
                this.writer.WriteStartElement("node");
                this.writer.WriteAttributeString("name", node.Name);
                this.writer.WriteAttributeString("elevation", XmlConvert.ToString(eUcf * node.Elevation));
                
                double ke = node.Ke;

                if(ke != 0.0) {
                    ke = fUcf / Math.Pow(pUcf * ke, (1.0 / net.QExp));
                    this.writer.WriteAttributeString("emmiter", XmlConvert.ToString(ke));

                }
                
                this.ComposeCoordinate(node);

                foreach(Demand d in node.Demand) {
                    this.writer.WriteStartElement("demand");

                    this.writer.WriteAttributeString("base", XmlConvert.ToString(dUcf * d.Base));
                    if(d.Pattern != null) {
                        var patName = d.Pattern.Name;
                        if (!string.IsNullOrEmpty(patName)
                            && !net.DefPatId.Equals(patName, StringComparison.OrdinalIgnoreCase))
                        {
                            this.writer.WriteAttributeString("pattern", patName);
                        }
                    }

                    this.writer.WriteEndElement();
                }
                


                this.ComposeElement(node);

                this.writer.WriteEndElement();

            }

            this.writer.WriteEndElement();
        }

        private void ComposeReservoirs() {
            
            var reservoirs = net.Tanks.Where(x => x.IsReservoir).ToArray();

            if(reservoirs.Length == 0)
                return;

            FieldsMap fMap = net.FieldsMap;

            this.writer.WriteStartElement(SectType.RESERVOIRS.ToString().ToLower());

            foreach(Tank r in reservoirs) {
                this.writer.WriteStartElement("node");
                this.writer.WriteAttributeString("name", r.Name);
                this.writer.WriteAttributeString("head", XmlConvert.ToString(fMap.RevertUnit(FieldType.ELEV, r.Elevation)));
                

                if(r.Pattern != null)
                    this.writer.WriteAttributeString("pattern", r.Pattern.Name);

                this.ComposeCoordinate(r);

                this.ComposeElement(r);

                this.writer.WriteEndElement();
                
            }

            this.writer.WriteEndElement();
            
        }

        private void ComposeTanks() {

            var tanks = net.Tanks.Where(x => !x.IsReservoir).ToArray();
        
           // var tanks2 = net.Nodes.OfType<Tank>().Where(x => !x.IsReservoir).ToList();

            if(tanks.Length == 0)
                return;

            FieldsMap fMap = net.FieldsMap;

            this.writer.WriteStartElement(SectType.TANKS.ToString().ToLower());

            foreach(Tank tank in tanks) {
                double vmin = tank.Vmin;
                if(Math.Round(vmin / tank.Area) == Math.Round(tank.Hmin - tank.Elevation))
                    vmin = 0;

                double elev = fMap.RevertUnit(FieldType.ELEV, tank.Elevation);
                double initlvl = fMap.RevertUnit(FieldType.ELEV, tank.H0 - tank.Elevation);
                double minlvl = fMap.RevertUnit(FieldType.ELEV, tank.Hmin - tank.Elevation);
                double maxlvl = fMap.RevertUnit(FieldType.ELEV, tank.Hmax - tank.Elevation);
                double diam = fMap.RevertUnit(FieldType.ELEV, 2 * Math.Sqrt(tank.Area / Math.PI));
                double minvol = fMap.RevertUnit(FieldType.VOLUME, vmin);

                this.writer.WriteStartElement("node");
                this.writer.WriteAttributeString("name", tank.Name);
                this.writer.WriteAttributeString("elevation", XmlConvert.ToString(elev));
                this.writer.WriteAttributeString("InitLevel", XmlConvert.ToString(initlvl));
                this.writer.WriteAttributeString("MinLevel", XmlConvert.ToString(minlvl));
                this.writer.WriteAttributeString("MaxLevel", XmlConvert.ToString(maxlvl));
                this.writer.WriteAttributeString("diameter", XmlConvert.ToString(diam));
                this.writer.WriteAttributeString("MinVolume", XmlConvert.ToString(minvol));

                if(tank.Vcurve != null)
                    this.writer.WriteAttributeString("volcurve", tank.Vcurve.Name);

                this.ComposeCoordinate(tank);

                if (tank.MixModel != MixType.MIX1) {
                    this.writer.WriteStartElement(SectType.MIXING.ToString().ToLower());
                    this.writer.WriteAttributeString("model", tank.MixModel.ParseStr());
                    if (tank.MixModel == MixType.MIX2)
                        this.writer.WriteAttributeString("compartment", XmlConvert.ToString(tank.V1Max / tank.Vmax));

                    this.writer.WriteEndElement();                    
                }

                this.ComposeElement(tank);

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }


        private void ComposePipes() {
            
            var pipes = net.Links.Where(x => x.Type == LinkType.PIPE || x.Type == LinkType.CV).ToArray();

            if(pipes.Length == 0)
                return;

            FieldsMap fMap = net.FieldsMap;

            this.writer.WriteStartElement(SectType.PIPES.ToString().ToLower());
            
            foreach(Link link in pipes) {
                double d = link.Diameter;
                double kc = link.Roughness;
                if(net.FormFlag == FormType.DW)
                    kc = fMap.RevertUnit(FieldType.ELEV, kc * 1000.0);

                double km = link.Km * Math.Pow(d, 4.0) / 0.02517;

                this.writer.WriteStartElement("link");
                this.writer.WriteAttributeString("name", link.Name);
                this.writer.WriteAttributeString("node1", link.FirstNode.Name);
                this.writer.WriteAttributeString("node2", link.SecondNode.Name);
                this.writer.WriteAttributeString("length", XmlConvert.ToString(fMap.RevertUnit(FieldType.LENGTH, link.Lenght)));
                this.writer.WriteAttributeString("diameter", XmlConvert.ToString(fMap.RevertUnit(FieldType.DIAM, d)));
                this.writer.WriteAttributeString("roughness", XmlConvert.ToString(kc));
                this.writer.WriteAttributeString("minorloss", XmlConvert.ToString(km));

                if(link.Type == LinkType.CV)
                    this.writer.WriteAttributeString("status", LinkType.CV.ToString());
                else if(link.Status == StatType.CLOSED)
                    this.writer.WriteAttributeString("status", StatType.CLOSED.ToString());
                else if(link.Status == StatType.OPEN)
                    this.writer.WriteAttributeString("status", StatType.OPEN.ToString());

                this.ComposeVertices(link);

                this.ComposeElement(link);

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }


        private void ComposePumps() {

            var pumps = net.Pumps.ToArray();

            if(pumps.Length == 0)
                return;
            
            FieldsMap fMap = net.FieldsMap;

            this.writer.WriteStartElement(SectType.PUMPS.ToString().ToLower());
            
            foreach(Pump pump in pumps) {
                this.writer.WriteStartElement("link");
                this.writer.WriteAttributeString("name", pump.Name);
                this.writer.WriteAttributeString("node1", pump.FirstNode.Name);
                this.writer.WriteAttributeString("node2", pump.SecondNode.Name);

                bool oldFormat = false;
                
                if (pump.Ptype == PumpType.CONST_HP) {
                    // Pump has constant power
                    this.writer.WriteAttributeString("power", XmlConvert.ToString(pump.Km));
                }
                else if (pump.HCurve != null) {
                    // Pump has a head curve
                    this.writer.WriteAttributeString("head", pump.HCurve.Name);
                }
                else {
                    // Old format used for pump curve
                    this.writer.WriteAttributeString("h0", XmlConvert.ToString(fMap.RevertUnit(FieldType.HEAD, -pump.H0)));
                    this.writer.WriteAttributeString("h1", XmlConvert.ToString(fMap.RevertUnit(FieldType.HEAD, -pump.H0 - pump.FlowCoefficient * Math.Pow(pump.Q0, pump.N))));
                    this.writer.WriteAttributeString("q1", XmlConvert.ToString(fMap.RevertUnit(FieldType.FLOW, pump.Q0)));
                    this.writer.WriteAttributeString("h2", XmlConvert.ToString(0.0));
                    this.writer.WriteAttributeString("q2", XmlConvert.ToString(fMap.RevertUnit(FieldType.FLOW, pump.Qmax)));

                    oldFormat = true;
                }

                if (!oldFormat) {
                    if (pump.UPat != null)
                        this.writer.WriteAttributeString("pattern", pump.UPat.Name);

                    if (pump.Roughness != 1.0)
                        this.writer.WriteAttributeString("speed", XmlConvert.ToString(pump.Roughness));
                }

                this.ComposeElement(pump);
                this.ComposeVertices(pump);
                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeVertices(Link link) {
            if (link.Vertices.Count == 0) return;

            foreach(EnPoint p in link.Vertices) {
                this.writer.WriteStartElement("point");
                this.writer.WriteAttributeString("x", XmlConvert.ToString(p.X));
                this.writer.WriteAttributeString("y", XmlConvert.ToString(p.Y));
                this.writer.WriteEndElement();
            }            
        }


        private void ComposeValves() {
            
            var valves = net.Valves.ToArray();

            if(valves.Length == 0)
                return;

            FieldsMap fMap = net.FieldsMap;
            
            this.writer.WriteStartElement(SectType.VALVES.ToString().ToLower());
            
            foreach(Valve valve in valves) {
                double d = valve.Diameter;
                double kc = valve.Roughness;
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

                this.writer.WriteStartElement("link");
                this.writer.WriteAttributeString("name", valve.Name);
                this.writer.WriteAttributeString("node1", valve.FirstNode.Name);
                this.writer.WriteAttributeString("node2", valve.SecondNode.Name);
                this.writer.WriteAttributeString("diameter", XmlConvert.ToString(fMap.RevertUnit(FieldType.DIAM, d)));
                this.writer.WriteAttributeString("type", valve.Type.ParseStr());

                if (valve.Type == LinkType.GPV && valve.Curve != null) {
                    this.writer.WriteAttributeString("setting", valve.Curve.Name);
                }
                else {
                    this.writer.WriteAttributeString("setting", XmlConvert.ToString(kc));
                }

                this.writer.WriteAttributeString("minorloss", XmlConvert.ToString(km));

                this.ComposeElement(valve);
                this.ComposeVertices(valve);

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

   


        private void ComposeStatus() {

            if(net.Links.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.STATUS.ToString().ToLower());
            
            foreach(Link link in net.Links) {
                if(link.Type <= LinkType.PUMP) {
                    if (link.Status == StatType.CLOSED) {
                        this.writer.WriteStartElement("link");
                        this.writer.WriteAttributeString("name", link.Name);
                        this.writer.WriteAttributeString("setting", StatType.CLOSED.ParseStr());
                        this.writer.WriteEndElement();
                    }
                    else if(link.Type == LinkType.PUMP) {
                        // Write pump speed here for pumps with old-style pump curve input
                        Pump pump = (Pump)link;
                        if (pump.HCurve == null &&
                            pump.Ptype != PumpType.CONST_HP &&
                            pump.Roughness != 1.0) {
                            this.writer.WriteStartElement("link");
                            this.writer.WriteAttributeString("name", link.Name);
                            this.writer.WriteAttributeString("setting", XmlConvert.ToString(link.Roughness));
                            this.writer.WriteEndElement();
                        }
                    }
                }
                else if(link.Roughness.IsMissing()) {
                    // Write fixed-status PRVs & PSVs (setting = MISSING)
                    switch (link.Status) {
                    case StatType.OPEN:
                        this.writer.WriteStartElement("link");
                        this.writer.WriteAttributeString("name", link.Name);
                        this.writer.WriteAttributeString("setting", StatType.OPEN.ParseStr());
                        this.writer.WriteEndElement();
                        break;
                    case StatType.CLOSED:
                        this.writer.WriteStartElement("link");
                        this.writer.WriteAttributeString("name", link.Name);
                        this.writer.WriteAttributeString("setting", StatType.CLOSED.ParseStr());
                        this.writer.WriteEndElement();
                        break;
                    }

                }

            }

            this.writer.WriteEndElement();
        }

        private void ComposePatterns() {

            var pats = net.Patterns;

            if(pats.Count <= 1)
                return;

            this.writer.WriteStartElement(SectType.PATTERNS.ToString().ToLower());

            for(int i = 1; i < pats.Count; i++) {
                Pattern pat = pats[i];

                this.writer.WriteStartElement("pattern");
                this.writer.WriteAttributeString("name", pat.Name);

                for(int j = 0; j < pats[i].Count; j++) {
                    this.writer.WriteStartElement("factor");
                    this.writer.WriteAttributeString("value", XmlConvert.ToString(pat[j]));
                    this.writer.WriteEndElement();
                }

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeCurves() {

            var curves = net.Curves;

            if(curves.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.CURVES.ToString().ToLower());

            foreach(Curve curve in curves) {
                this.writer.WriteStartElement("curve");
                this.writer.WriteAttributeString("name", curve.Name);

                foreach(var pt in curve) {
                    this.writer.WriteStartElement("point");
                    this.writer.WriteAttributeString("x", XmlConvert.ToString(pt.X));
                    this.writer.WriteAttributeString("y", XmlConvert.ToString(pt.Y));
                    this.writer.WriteEndElement();
                }

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeControls() {
            var controls = net.Controls;
            FieldsMap fmap = net.FieldsMap;

            if(controls.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.CONTROLS.ToString().ToLower());

            foreach(Control control in controls) {
                // Check that controlled link exists
                if(control.Link == null)
                    continue;

                this.writer.WriteStartElement("control");

                // Get text of control's link status/setting
                if (control.Setting.IsMissing()) {
                    this.writer.WriteAttributeString("link", control.Link.Name);
                    this.writer.WriteAttributeString("status", control.Status.ParseStr());
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

                    this.writer.WriteAttributeString("link", control.Link.Name);
                    this.writer.WriteAttributeString("status", XmlConvert.ToString(kc));
                }


                switch(control.Type) {
                // Print level control
                case ControlType.LOWLEVEL:
                case ControlType.HILEVEL:
                double kc = control.Grade - control.Node.Elevation;
               
                if(control.Node is Tank)
                    kc = fmap.RevertUnit(FieldType.HEAD, kc);
                else
                    kc = fmap.RevertUnit(FieldType.PRESSURE, kc);


                this.writer.WriteAttributeString("type", control.Type.ParseStr());
                this.writer.WriteAttributeString("node", control.Node.Name);
                this.writer.WriteAttributeString("value", XmlConvert.ToString(kc));
                
                break;

                case ControlType.TIMER:
                case ControlType.TIMEOFDAY:
                // Print timer control
                // Print time-of-day control
                this.writer.WriteAttributeString("type", control.Type.ParseStr());
                this.writer.WriteAttributeString("value", XmlConvert.ToString(control.Time));

                break;

                }

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeQuality() {
            FieldsMap fmap = net.FieldsMap;

            if(net.Nodes.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.QUALITY.ToString().ToLower());
            
            foreach(Node node in net.Nodes) {
                if (node.C0 == 0.0) continue;

                this.writer.WriteStartElement("node");
                this.writer.WriteAttributeString("name", node.Name);
                this.writer.WriteAttributeString("value", XmlConvert.ToString(fmap.RevertUnit(FieldType.QUALITY, node.C0)));
                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeSource() {

            if(net.Nodes.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.SOURCES.ToString().ToLower());
            
            foreach(Node node in net.Nodes) {
                QualSource source = node.QualSource;
                if(source == null)
                    continue;

                this.writer.WriteStartElement("node");
                this.writer.WriteAttributeString("name", node.Name);
                this.writer.WriteAttributeString("type", source.Type.ParseStr());
                this.writer.WriteAttributeString("quality", XmlConvert.ToString(source.C0));


                if(source.Pattern != null)
                    this.writer.WriteAttributeString("pattern", source.Pattern.Name);
                    
                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }


    

        private void ComposeReaction() {
            
            this.writer.WriteStartElement(SectType.REACTIONS.ToString().ToLower());

            this.WriteOption("order bulk", XmlConvert.ToString(net.BulkOrder));
            this.WriteOption("order wall", XmlConvert.ToString(net.WallOrder));
            this.WriteOption("order tank", XmlConvert.ToString(net.TankOrder));
            this.WriteOption("global bulk", XmlConvert.ToString(net.KBulk * Constants.SECperDAY));
            this.WriteOption("global wall", XmlConvert.ToString(net.KWall * Constants.SECperDAY));
            this.WriteOption("limiting potential", XmlConvert.ToString(net.CLimit));
            this.WriteOption("roughness correlation", XmlConvert.ToString(net.RFactor));



            foreach(Link link in net.Links) {
                if(link.Type > LinkType.PIPE)
                    continue;

                if (link.Kb != net.KBulk) {
                    this.writer.WriteStartElement("option");
                    this.writer.WriteAttributeString("name", "bulk");
                    this.writer.WriteAttributeString("link", link.Name);
                    this.writer.WriteAttributeString("value", XmlConvert.ToString(link.Kb * Constants.SECperDAY));
                    this.writer.WriteEndElement();                    
                }

                if (link.Kw != net.KWall) {
                    this.writer.WriteStartElement("option");
                    this.writer.WriteAttributeString("name", "wall");
                    this.writer.WriteAttributeString("link", link.Name);
                    this.writer.WriteAttributeString("value", XmlConvert.ToString(link.Kw * Constants.SECperDAY));
                    this.writer.WriteEndElement();
                }
            }

            foreach(Tank tank in net.Tanks) {
                if(tank.IsReservoir)
                    continue;
                if (tank.Kb != net.KBulk) {
                    this.writer.WriteStartElement("option");
                    this.writer.WriteAttributeString("name", "tank");
                    this.writer.WriteAttributeString("name", tank.Name);
                    this.writer.WriteAttributeString("value", XmlConvert.ToString(tank.Kb * Constants.SECperDAY));
                    this.writer.WriteEndElement();
                }
            }

            this.writer.WriteEndElement();
        }

        private void ComposeEnergy() {
            
            this.writer.WriteStartElement(SectType.ENERGY.ToString().ToLower());

            if (net.ECost != 0.0)
                this.WriteOption("global price", XmlConvert.ToString(net.ECost));

            if (!string.IsNullOrEmpty(net.EPatId))
                this.WriteOption("global pattern", net.EPatId);

            this.WriteOption("global effic", XmlConvert.ToString(net.EPump));
            this.WriteOption("demand charge", XmlConvert.ToString(net.DCost));

            foreach(Pump pump in net.Pumps) {
                if (pump.ECost > 0.0) {
                    this.writer.WriteStartElement("option");
                    this.writer.WriteAttributeString("pump", pump.Name);
                    this.writer.WriteAttributeString("price", XmlConvert.ToString(pump.ECost));
                    this.writer.WriteEndElement();
                }

                if (pump.EPat != null) {
                    this.writer.WriteStartElement("option");
                    this.writer.WriteAttributeString("pump", pump.Name);
                    this.writer.WriteAttributeString("pattern", pump.EPat.Name);
                    this.writer.WriteEndElement();
                }

                if (pump.ECurve != null) {
                    this.writer.WriteStartElement("option");
                    this.writer.WriteAttributeString("pump", pump.Name);
                    this.writer.WriteAttributeString("effic", pump.ECurve.Name);
                    this.writer.WriteEndElement();
                }
            }

            this.writer.WriteEndElement();

        }

        private void WriteOption(string name, string value) {
            this.writer.WriteStartElement("option");
            this.writer.WriteAttributeString("name", name);
            this.writer.WriteAttributeString("value", value);
            this.writer.WriteEndElement();
        }

        private void ComposeTimes() {
            
            this.writer.WriteStartElement(SectType.TIMES.ToString().ToLower());

            this.WriteOption("DURATION", net.Duration.GetClockTime());
            this.WriteOption("HYDRAULIC_TIMESTEP", net.HStep.GetClockTime());
            this.WriteOption("QUALITY_TIMESTEP", net.QStep.GetClockTime());
            this.WriteOption("REPORT_TIMESTEP", net.RStep.GetClockTime());
            this.WriteOption("REPORT_START", net.RStart.GetClockTime());
            this.WriteOption("PATTERN_TIMESTEP", net.PStep.GetClockTime());
            this.WriteOption("PATTERN_START", net.PStart.GetClockTime());
            this.WriteOption("RULE_TIMESTEP", net.RuleStep.GetClockTime());
            this.WriteOption("START_CLOCKTIME", net.TStart.GetClockTime());
            this.WriteOption("STATISTIC", net.TStatFlag.ParseStr());

            this.writer.WriteEndElement();
        }



        private void ComposeOptions() {
            FieldsMap fMap = net.FieldsMap;

            this.writer.WriteStartElement(SectType.OPTIONS.ToString().ToLower());

            this.WriteOption("units", net.FlowFlag.ParseStr());
            this.WriteOption("pressure", net.PressFlag.ParseStr());
            this.WriteOption("headloss", net.FormFlag.ParseStr());

            if(!string.IsNullOrEmpty(net.DefPatId))
                this.WriteOption("pattern", net.DefPatId);

            switch (net.HydFlag) {
            case HydType.USE:
                this.WriteOption("hydraulics use", net.HydFname);
                break;
            case HydType.SAVE:
                this.WriteOption("hydraulics save", net.HydFname);
                break;
            }

            if (net.ExtraIter == -1) {
                this.WriteOption("unbalanced stop", "");
            }
            else if (net.ExtraIter >= 0) {
                this.WriteOption("unbalanced continue", net.ExtraIter.ToString());
            }

            switch (net.QualFlag) {
            case QualType.CHEM:
            this.writer.WriteStartElement("option");
                this.writer.WriteAttributeString("name", "quality");
                this.writer.WriteAttributeString("value", "chem");
                this.writer.WriteAttributeString("chemname", net.ChemName);
                this.writer.WriteAttributeString("chemunits", net.ChemUnits);
                this.writer.WriteEndElement();
                break;
            case QualType.TRACE:
            this.writer.WriteStartElement("option");
                this.writer.WriteAttributeString("name", "quality");
                this.writer.WriteAttributeString("value", "trace");
                this.writer.WriteAttributeString("node", net.TraceNode);
                this.writer.WriteEndElement();
                break;
            case QualType.AGE:
                this.WriteOption("quality", "age");
                break;
            case QualType.NONE:
                this.WriteOption("quality", "none");
                break;
            }

            this.WriteOption("demand_multiplier", XmlConvert.ToString(net.DMult));
            this.WriteOption("emitter_exponent", XmlConvert.ToString(1.0 / net.QExp));
            this.WriteOption("viscosity", XmlConvert.ToString(net.Viscos / Constants.VISCOS));
            this.WriteOption("diffusivity", XmlConvert.ToString(net.Diffus / Constants.DIFFUS));
            this.WriteOption("specific_gravity", XmlConvert.ToString(net.SpGrav));
            this.WriteOption("trials", net.MaxIter.ToString());
            this.WriteOption("accuracy", net.HAcc.ToString(CultureInfo.InvariantCulture));
            this.WriteOption("tolerance", fMap.RevertUnit(FieldType.QUALITY, net.Ctol).ToString(CultureInfo.InvariantCulture));
            this.WriteOption("checkfreq", net.CheckFreq.ToString());
            this.WriteOption("maxcheck", net.MaxCheck.ToString());
            this.WriteOption("damplimit", net.DampLimit.ToString(CultureInfo.InvariantCulture));

            ComposeExtraOptions();

            this.writer.WriteEndElement();
        }

        private void ComposeExtraOptions() {
            var extraOptions = net.ExtraOptions;

            if(extraOptions.Count == 0)
                return;

            foreach(var pair in extraOptions) {
                this.WriteOption(pair.Key, pair.Value);
            }

            this.writer.WriteEndElement();
        }

        private void ComposeReport() {

            this.writer.WriteStartElement(SectType.REPORT.ToString().ToLower());

            FieldsMap fMap = net.FieldsMap;
            this.WriteOption("pagesize", net.PageSize.ToString());
            this.WriteOption("status", net.Stat_Flag.ParseStr());
            this.WriteOption("summary", (net.SummaryFlag ? Keywords.w_YES : Keywords.w_NO));
            this.WriteOption("energy", (net.EnergyFlag ? Keywords.w_YES : Keywords.w_NO));

            switch (net.NodeFlag) {
            case ReportFlag.FALSE:
                this.WriteOption("nodes", "none");
                break;
            case ReportFlag.TRUE:
                this.WriteOption("nodes", "all");
                break;
            case ReportFlag.SOME: {
                    this.writer.WriteStartElement("option");
                    this.writer.WriteAttributeString("name", "nodes");
                    this.writer.WriteAttributeString("value", "some");

                foreach (Node node in net.Nodes) {
                    if (node.RptFlag) {
                        this.writer.WriteStartElement("node");
                        this.writer.WriteAttributeString("name", node.Name);
                        this.writer.WriteEndElement();
                    }
                }

                this.writer.WriteEndElement();
                break;
            }
            }

            switch (net.LinkFlag) {
            case ReportFlag.FALSE:
                this.WriteOption("links", "none");
                break;
            case ReportFlag.TRUE:
                this.WriteOption("links", "all");
                break;
            case ReportFlag.SOME:
                this.writer.WriteStartElement("option");
                this.writer.WriteAttributeString("name", "links");
                this.writer.WriteAttributeString("value", "some");

                foreach (Link link in net.Links) {
                    if (link.RptFlag) {
                        this.writer.WriteStartElement("link");
                        this.writer.WriteAttributeString("name", link.Name);
                        this.writer.WriteEndElement();
                    }
                }

                this.writer.WriteEndElement();

                break;
            }

            for(FieldType i = 0; i < FieldType.FRICTION; i++) {
                Field f = fMap.GetField(i);

                if (!f.Enabled) {
                    this.writer.WriteStartElement("field");
                    this.writer.WriteAttributeString("name", f.Name);
                    this.writer.WriteAttributeString("enabled", "false");
                    this.writer.WriteEndElement();
                    continue;
                }

                this.writer.WriteStartElement("field");
                this.writer.WriteAttributeString("name", f.Name);
                this.writer.WriteAttributeString("enabled", "true");
                this.writer.WriteAttributeString("precision", f.Precision.ToString());

                if (f.GetRptLim(RangeType.LOW) < Constants.BIG)
                    this.writer.WriteAttributeString("below", XmlConvert.ToString(f.GetRptLim(RangeType.LOW)));

                if (f.GetRptLim(RangeType.HI) > -Constants.BIG)
                    this.writer.WriteAttributeString("above", XmlConvert.ToString(f.GetRptLim(RangeType.HI)));

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeCoordinate(Node node) {
            if(node.Position.IsInvalid) return;
            // this.writer.WriteStartElement("point");
            this.writer.WriteAttributeString("x", XmlConvert.ToString(node.Position.X));
            this.writer.WriteAttributeString("y", XmlConvert.ToString(node.Position.Y));
            // this.writer.WriteEndElement();            
        }

        private void ComposeLabels() {
            if (net.Labels.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.LABELS.ToString().ToLower());
            
            foreach(Label label in net.Labels) {
                this.writer.WriteStartElement("label");
                this.writer.WriteAttributeString("x", XmlConvert.ToString(label.Position.X));
                this.writer.WriteAttributeString("y", XmlConvert.ToString(label.Position.Y));
                // this.buffer.WriteAttributeString("node", label.AnchorNodeId); // TODO: add AnchorNodeId property to label

                this.writer.WriteElementString("text", label.Text);

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

       

        private void ComposeRules() {
            if(net.Rules.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.RULES.ToString().ToLower());
            
            foreach(Rule r in net.Rules) {
                this.writer.WriteStartElement("rule");
                this.writer.WriteAttributeString("name", r.Name);

                foreach(string s in r.Code)
                    this.writer.WriteElementString("code", s);
                    
                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }
    }

}