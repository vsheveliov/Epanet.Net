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
using System.Globalization;
using System.IO;
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
        public XMLComposer(bool gzip) { this.gzip = gzip; }

        public override void Composer(Network net, string f) {
            try {
                // var fs = new FileStream(f, FileMode.Create, FileAccess.Write, FileShare.Read, 0x1000, FileOptions.SequentialScan);

                var settings = new XmlWriterSettings {
                    Indent = true,
                    Encoding = Encoding.UTF8
                };

                using (this.writer = XmlWriter.Create(f, settings)) {
                    this.writer.WriteStartDocument();
                    this.writer.WriteStartElement("network");

                    this.ComposeTitle(net);
                    this.ComposeJunctions(net);
                    this.ComposeReservoirs(net);
                    this.ComposeTanks(net);
                    this.ComposePipes(net);
                    this.ComposePumps(net);
                    this.ComposeValves(net);
                    this.ComposeDemands(net);
                    this.ComposeEmitters(net);
                    this.ComposeStatus(net);
                    this.ComposePatterns(net);
                    this.ComposeCurves(net);
                    this.ComposeControls(net);
                    this.ComposeQuality(net);
                    this.ComposeSource(net);
                    this.ComposeMixing(net);
                    this.ComposeReaction(net);
                    this.ComposeEnergy(net);
                    this.ComposeTimes(net);
                    this.ComposeOptions(net);
                    // this.ComposeExtraOptions(net);
                    this.ComposeReport(net);
                    this.ComposeLabels(net);
                    this.ComposeCoordinates(net);
                    this.ComposeVertices(net);
                    this.ComposeRules(net);

                    this.writer.WriteEndElement();
                    this.writer.WriteEndDocument();
                }
            }
            catch (IOException e) {
                Debug.Print(e.ToString());
                throw new ENException(ErrorCode.Err308);
            }
        }

        private void ComposeTitle(Network net) {
            if (net.TitleText.Count <= 0) return;

            this.writer.WriteStartElement(SectType.TITLE.ToString());
            foreach(string s in net.TitleText) {
                this.writer.WriteElementString("line", s);
            }
            this.writer.WriteEndElement();
        }

        private void ComposeJunctions(Network net) {
            if (!net.Junctions.Any()) return;

            var fMap = net.FieldsMap;

            this.writer.WriteStartElement(SectType.JUNCTIONS.ToString());
          
            foreach(Node node in net.Junctions) {
                this.writer.WriteStartElement("node");
                this.writer.WriteAttributeString("id", node.Id);
                this.writer.WriteAttributeString("elev", fMap.RevertUnit(FieldType.ELEV, node.Elevation).ToString(NumberFormatInfo.InvariantInfo));
                
                if(node.Demand.Count > 0) {
                    Demand d = node.Demand[0];
                    var baseDemand = fMap.RevertUnit(FieldType.DEMAND, d.Base);
                    this.writer.WriteAttributeString("demand", baseDemand.ToString(NumberFormatInfo.InvariantInfo));

                    if(!string.IsNullOrEmpty(d.Pattern.Id)
                        && !net.DefPatId.Equals(d.Pattern.Id, StringComparison.OrdinalIgnoreCase))
                        this.writer.WriteAttributeString("pattern", d.Pattern.Id);
                    
                    if (!string.IsNullOrEmpty(node.Comment)) {
                        this.writer.WriteElementString("comment", node.Comment);
                        // writer.WriteCData(node.Comment);    
                    }
                    
                }

                this.writer.WriteEndElement();

            }

            this.writer.WriteEndElement();
        }

        private void ComposeReservoirs(Network net) {
            
            if(!net.Tanks.Any())
                return;

            FieldsMap fMap = net.FieldsMap;
            List<Tank> reservoirs = new List<Tank>();

            foreach(Tank tank in net.Tanks)
                if(tank.IsReservoir)
                    reservoirs.Add(tank);

            if(reservoirs.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.RESERVOIRS.ToString());

            foreach(Tank r in reservoirs) {
                this.writer.WriteStartElement("node");
                this.writer.WriteAttributeString("id", r.Id);
                this.writer.WriteAttributeString("head", fMap.RevertUnit(FieldType.ELEV, r.Elevation).ToString(NumberFormatInfo.InvariantInfo));
                
                if(r.Pattern != null)
                    this.writer.WriteAttributeString("pattern", r.Pattern.Id);

                if(!string.IsNullOrEmpty(r.Comment))
                    this.writer.WriteElementString("comment", r.Comment);
                    
                this.writer.WriteEndElement();
                
            }

            this.writer.WriteEndElement();
            
        }


        private void ComposeTanks(Network net) {
            FieldsMap fMap = net.FieldsMap;

            List<Tank> tanks = new List<Tank>();
            foreach(Tank tank in net.Tanks)
                if(!tank.IsReservoir)
                    tanks.Add(tank);

           // var tanks2 = net.Nodes.OfType<Tank>().Where(x => x.IsReservoir).ToList();

            if(tanks.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.TANKS.ToString());

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
                this.writer.WriteAttributeString("id", tank.Id);
                this.writer.WriteAttributeString("elev", elev.ToString(NumberFormatInfo.InvariantInfo));
                this.writer.WriteAttributeString("initlvl", initlvl.ToString(NumberFormatInfo.InvariantInfo));
                this.writer.WriteAttributeString("minlvl", minlvl.ToString(NumberFormatInfo.InvariantInfo));
                this.writer.WriteAttributeString("maxlvl", maxlvl.ToString(NumberFormatInfo.InvariantInfo));
                this.writer.WriteAttributeString("diam", diam.ToString(NumberFormatInfo.InvariantInfo));
                this.writer.WriteAttributeString("minvol", minvol.ToString(NumberFormatInfo.InvariantInfo));

                if(tank.Vcurve != null)
                    this.writer.WriteAttributeString("volcurve", tank.Vcurve.Id);

                if(!string.IsNullOrEmpty(tank.Comment))
                    this.writer.WriteElementString("comment", tank.Comment);
                    

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }


        private void ComposePipes(Network net) {
            FieldsMap fMap = net.FieldsMap;
            
            if(net.Links.Count == 0)
                return;

            List<Link> pipes = new List<Link>();
            foreach(Link link in net.Links)
                if(link.Type == LinkType.PIPE || link.Type == LinkType.CV)
                    pipes.Add(link);


            this.writer.WriteStartElement(SectType.PIPES.ToString());
            
            foreach(Link link in pipes) {
                double d = link.Diameter;
                double kc = link.Roughness;
                if(net.FormFlag == FormType.DW)
                    kc = fMap.RevertUnit(FieldType.ELEV, kc * 1000.0);

                double km = link.Km * Math.Pow(d, 4.0) / 0.02517;

                this.writer.WriteStartElement("link");
                this.writer.WriteAttributeString("id", link.Id);
                this.writer.WriteAttributeString("node1", link.FirstNode.Id);
                this.writer.WriteAttributeString("node2", link.SecondNode.Id);
                this.writer.WriteAttributeString("length", fMap.RevertUnit(FieldType.LENGTH, link.Lenght).ToString(NumberFormatInfo.InvariantInfo));
                this.writer.WriteAttributeString("diam", fMap.RevertUnit(FieldType.DIAM, d).ToString(NumberFormatInfo.InvariantInfo));
                this.writer.WriteAttributeString("roughness", kc.ToString(NumberFormatInfo.InvariantInfo));
                this.writer.WriteAttributeString("mloss", km.ToString(NumberFormatInfo.InvariantInfo));

                if(link.Type == LinkType.CV)
                    this.writer.WriteAttributeString("status", LinkType.CV.ToString());
                else if(link.Status == StatType.CLOSED)
                    this.writer.WriteAttributeString("status", StatType.CLOSED.ToString());
                else if(link.Status == StatType.OPEN)
                    this.writer.WriteAttributeString("status", StatType.OPEN.ToString());

                if(!string.IsNullOrEmpty(link.Comment))
                    this.writer.WriteElementString("comment", link.Comment);

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }


        private void ComposePumps(Network net) {
            FieldsMap fMap = net.FieldsMap;

            if(!net.Pumps.Any())
                return;

            this.writer.WriteStartElement(SectType.PUMPS.ToString());
            
            foreach(Pump pump in net.Pumps) {
                this.writer.WriteStartElement("link");
                this.writer.WriteAttributeString("id", pump.Id);
                this.writer.WriteAttributeString("node1", pump.FirstNode.Id);
                this.writer.WriteAttributeString("node2", pump.SecondNode.Id);


                // Pump has constant power
                if(pump.Ptype == PumpType.CONST_HP)
                    this.writer.WriteAttributeString("power", pump.Km.ToString(NumberFormatInfo.InvariantInfo));
                // Pump has a head curve
                else if(pump.HCurve != null)
                    this.writer.WriteAttributeString("head", pump.HCurve.Id);
                // Old format used for pump curve
                else {
                    this.writer.WriteAttributeString("h0", fMap.RevertUnit(FieldType.HEAD, -pump.H0).ToString(NumberFormatInfo.InvariantInfo));
                    this.writer.WriteAttributeString("h1", fMap.RevertUnit(FieldType.HEAD, -pump.H0 - pump.FlowCoefficient * Math.Pow(pump.Q0, pump.N)).ToString(NumberFormatInfo.InvariantInfo));
                    this.writer.WriteAttributeString("q1", fMap.RevertUnit(FieldType.FLOW, pump.Q0).ToString(NumberFormatInfo.InvariantInfo));
                    this.writer.WriteAttributeString("h2", 0.0.ToString(NumberFormatInfo.InvariantInfo));
                    this.writer.WriteAttributeString("q2", fMap.RevertUnit(FieldType.FLOW, pump.Qmax).ToString(NumberFormatInfo.InvariantInfo));

                    continue;
                }

                if(pump.UPat != null)
                    this.writer.WriteAttributeString("pattern", pump.UPat.Id);

                if(pump.Roughness != 1.0)
                    this.writer.WriteAttributeString("speed", pump.Roughness.ToString(NumberFormatInfo.InvariantInfo));

                if(!string.IsNullOrEmpty(pump.Comment))
                    this.writer.WriteAttributeString("comment", pump.Comment);

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeValves(Network net) {
            FieldsMap fMap = net.FieldsMap;

            if(!net.Valves.Any())
                return;

            this.writer.WriteStartElement(SectType.VALVES.ToString());
            
            foreach(Valve valve in net.Valves) {
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
                this.writer.WriteAttributeString("id", valve.Id);
                this.writer.WriteAttributeString("node1", valve.FirstNode.Id);
                this.writer.WriteAttributeString("node2", valve.SecondNode.Id);
                this.writer.WriteAttributeString("diameter", fMap.RevertUnit(FieldType.DIAM, d).ToString(NumberFormatInfo.InvariantInfo));
                this.writer.WriteAttributeString("type", valve.Type.ParseStr());

                if (valve.Type == LinkType.GPV && valve.Curve != null) {
                    this.writer.WriteAttributeString("setting", valve.Curve.Id);
                }
                else {
                    this.writer.WriteAttributeString("setting", kc.ToString(NumberFormatInfo.InvariantInfo));
                }

                this.writer.WriteAttributeString("minorloss", km.ToString(NumberFormatInfo.InvariantInfo));

                if(!string.IsNullOrEmpty(valve.Comment))
                    this.writer.WriteElementString("comment", valve.Comment);

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeDemands(Network net) {
            FieldsMap fMap = net.FieldsMap;

            if(!net.Junctions.Any())
                return;

            this.writer.WriteStartElement(SectType.DEMANDS.ToString());
            
            double ucf = fMap.GetUnits(FieldType.DEMAND);

            foreach(Node node in net.Junctions) {
                if (node.Demand.Count > 1)
                    foreach (Demand demand in node.Demand) {
                        this.writer.WriteStartElement("node");
                        this.writer.WriteAttributeString("id", node.Id);
                        this.writer.WriteAttributeString("demand", (ucf * demand.Base).ToString(NumberFormatInfo.InvariantInfo));
                        if (demand.Pattern != null)
                            this.writer.WriteAttributeString("pattern", demand.Pattern.Id);

                        this.writer.WriteEndElement();
                    }
            }

            this.writer.WriteEndElement();
        }

        private void ComposeEmitters(Network net) {

            if(net.Nodes.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.EMITTERS.ToString());
            
            double uflow = net.FieldsMap.GetUnits(FieldType.FLOW);
            double upressure = net.FieldsMap.GetUnits(FieldType.PRESSURE);
            double qexp = net.QExp;

            foreach(Node node in net.Junctions) {
                if(node.Ke == 0.0)
                    continue;
                double ke = uflow / Math.Pow(upressure * node.Ke, (1.0 / qexp));

                this.writer.WriteStartElement("node");
                this.writer.WriteAttributeString("id", node.Id);
                this.writer.WriteAttributeString("coefficient", ke.ToString(NumberFormatInfo.InvariantInfo));
                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }


        private void ComposeStatus(Network net) {

            if(net.Links.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.STATUS.ToString());
            
            foreach(Link link in net.Links) {
                if(link.Type <= LinkType.PUMP) {
                    if (link.Status == StatType.CLOSED) {
                        this.writer.WriteStartElement("link");
                        this.writer.WriteAttributeString("id", link.Id);
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
                            this.writer.WriteAttributeString("id", link.Id);
                            this.writer.WriteAttributeString("setting", link.Roughness.ToString(NumberFormatInfo.InvariantInfo));
                            this.writer.WriteEndElement();
                        }
                    }
                }
                else if(link.Roughness.IsMissing()) {
                    // Write fixed-status PRVs & PSVs (setting = MISSING)
                    switch (link.Status) {
                    case StatType.OPEN:
                        this.writer.WriteStartElement("link");
                        this.writer.WriteAttributeString("id", link.Id);
                        this.writer.WriteAttributeString("setting", StatType.OPEN.ParseStr());
                        this.writer.WriteEndElement();
                        break;
                    case StatType.CLOSED:
                        this.writer.WriteStartElement("link");
                        this.writer.WriteAttributeString("id", link.Id);
                        this.writer.WriteAttributeString("setting", StatType.CLOSED.ParseStr());
                        this.writer.WriteEndElement();
                        break;
                    }

                }

            }

            this.writer.WriteEndElement();
        }

        private void ComposePatterns(Network net) {

            var pats = net.Patterns;

            if(pats.Count <= 1)
                return;

            this.writer.WriteStartElement(SectType.PATTERNS.ToString());

            for(int i = 1; i < pats.Count; i++) {
                Pattern pat = pats[i];
                List<double> f = pat.FactorsList;

                this.writer.WriteStartElement("pattern");
                this.writer.WriteAttributeString("id", pat.Id);

                for(int j = 0; j < pats[i].Length; j++) {
                    this.writer.WriteStartElement("factor");
                    this.writer.WriteAttributeString("value", f[j].ToString(NumberFormatInfo.InvariantInfo));
                    this.writer.WriteEndElement();
                }

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeCurves(Network net) {

            var curves = net.Curves;

            if(curves.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.CURVES.ToString());

            foreach(Curve c in curves) {
                this.writer.WriteStartElement("curve");
                this.writer.WriteAttributeString("id", c.Id);

                foreach(var pt in c.Points) {
                    this.writer.WriteStartElement("point");
                    this.writer.WriteAttributeString("x", pt.X.ToString(NumberFormatInfo.InvariantInfo));
                    this.writer.WriteAttributeString("y", pt.Y.ToString(NumberFormatInfo.InvariantInfo));
                    this.writer.WriteEndElement();
                }

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeControls(Network net) {
            var controls = net.Controls;
            FieldsMap fmap = net.FieldsMap;

            if(controls.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.CONTROLS.ToString());

            foreach(Control control in controls) {
                // Check that controlled link exists
                if(control.Link == null)
                    continue;

                this.writer.WriteStartElement("control");

                // Get text of control's link status/setting
                if (control.Setting.IsMissing()) {
                    this.writer.WriteAttributeString("link", control.Link.Id);
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

                    this.writer.WriteAttributeString("link", control.Link.Id);
                    this.writer.WriteAttributeString("status", kc.ToString(NumberFormatInfo.InvariantInfo));
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
                this.writer.WriteAttributeString("node", control.Node.Id);
                this.writer.WriteAttributeString("value", kc.ToString(NumberFormatInfo.InvariantInfo));
                
                break;

                case ControlType.TIMER:
                case ControlType.TIMEOFDAY:
                // Print timer control
                // Print time-of-day control
                this.writer.WriteAttributeString("type", control.Type.ParseStr());
                this.writer.WriteAttributeString("value", control.Time.ToString(NumberFormatInfo.InvariantInfo));

                break;

                }

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeQuality(Network net) {
            FieldsMap fmap = net.FieldsMap;

            if(net.Nodes.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.QUALITY.ToString());
            
            foreach(Node node in net.Nodes) {
                if (node.C0 == 0.0) continue;

                this.writer.WriteStartElement("node");
                this.writer.WriteAttributeString("id", node.Id);
                this.writer.WriteAttributeString("value", fmap.RevertUnit(FieldType.QUALITY, node.C0).ToString(NumberFormatInfo.InvariantInfo));
                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeSource(Network net) {

            if(net.Nodes.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.SOURCES.ToString());
            
            foreach(Node node in net.Nodes) {
                Source source = node.Source;
                if(source == null)
                    continue;

                this.writer.WriteStartElement("node");
                this.writer.WriteAttributeString("id", node.Id);
                this.writer.WriteAttributeString("type", source.Type.ParseStr());
                this.writer.WriteAttributeString("quality", source.C0.ToString(NumberFormatInfo.InvariantInfo));


                if(source.Pattern != null)
                    this.writer.WriteAttributeString("pattern", source.Pattern.Id);
                    
                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }


        private void ComposeMixing(Network net) {

            if(!net.Tanks.Any())
                return;

            this.writer.WriteStartElement(SectType.MIXING.ToString());
            
            foreach(Tank tank in net.Tanks) {
                if(tank.IsReservoir)
                    continue;

                this.writer.WriteStartElement("node");
                this.writer.WriteAttributeString("id", tank.Id);
                this.writer.WriteAttributeString("model", tank.MixModel.ParseStr());
                this.writer.WriteAttributeString("value", (tank.V1Max / tank.Vmax).ToString(NumberFormatInfo.InvariantInfo));
                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeReaction(Network net) {
            
            this.writer.WriteStartElement(SectType.REACTIONS.ToString());

            WriteTag("order bulk", net.BulkOrder.ToString(NumberFormatInfo.InvariantInfo));
            WriteTag("order wall", net.WallOrder.ToString(NumberFormatInfo.InvariantInfo));
            WriteTag("order tank", net.TankOrder.ToString(NumberFormatInfo.InvariantInfo));
            WriteTag("global bulk", (net.KBulk * Constants.SECperDAY).ToString(NumberFormatInfo.InvariantInfo));
            WriteTag("global wall", (net.KWall * Constants.SECperDAY).ToString(NumberFormatInfo.InvariantInfo));
            WriteTag("limiting potential", net.CLimit.ToString(NumberFormatInfo.InvariantInfo));
            WriteTag("roughness correlation", net.RFactor.ToString(NumberFormatInfo.InvariantInfo));



            foreach(Link link in net.Links) {
                if(link.Type > LinkType.PIPE)
                    continue;

                if (link.Kb != net.KBulk) {
                    this.writer.WriteStartElement("option");
                    this.writer.WriteAttributeString("name", "bulk");
                    this.writer.WriteAttributeString("link", link.Id);
                    this.writer.WriteAttributeString("value", (link.Kb * Constants.SECperDAY).ToString(NumberFormatInfo.InvariantInfo));
                    this.writer.WriteEndElement();                    
                }

                if (link.Kw != net.KWall) {
                    this.writer.WriteStartElement("option");
                    this.writer.WriteAttributeString("name", "wall");
                    this.writer.WriteAttributeString("link", link.Id);
                    this.writer.WriteAttributeString("value", (link.Kw * Constants.SECperDAY).ToString(NumberFormatInfo.InvariantInfo));
                    this.writer.WriteEndElement();
                }
            }

            foreach(Tank tank in net.Tanks) {
                if(tank.IsReservoir)
                    continue;
                if (tank.Kb != net.KBulk) {
                    this.writer.WriteStartElement("option");
                    this.writer.WriteAttributeString("name", "tank");
                    this.writer.WriteAttributeString("id", tank.Id);
                    this.writer.WriteAttributeString("value", (tank.Kb * Constants.SECperDAY).ToString(NumberFormatInfo.InvariantInfo));
                    this.writer.WriteEndElement();
                }
            }

            this.writer.WriteEndElement();
        }

        private void ComposeEnergy(Network net) {
            
            this.writer.WriteStartElement(SectType.ENERGY.ToString());

            if (net.ECost != 0.0)
                WriteTag("global price", net.ECost.ToString(NumberFormatInfo.InvariantInfo));

            if (!string.IsNullOrEmpty(net.EPatId))
                WriteTag("global pattern", net.EPatId);

            WriteTag("global effic", net.EPump.ToString(NumberFormatInfo.InvariantInfo));
            WriteTag("demand charge", net.DCost.ToString(NumberFormatInfo.InvariantInfo));

            foreach(Pump pump in net.Pumps) {
                if (pump.ECost > 0.0) {
                    this.writer.WriteStartElement("option");
                    this.writer.WriteAttributeString("pump", pump.Id);
                    this.writer.WriteAttributeString("price", pump.ECost.ToString(NumberFormatInfo.InvariantInfo));
                    this.writer.WriteEndElement();
                }

                if (pump.EPat != null) {
                    this.writer.WriteStartElement("option");
                    this.writer.WriteAttributeString("pump", pump.Id);
                    this.writer.WriteAttributeString("pattern", pump.EPat.Id);
                    this.writer.WriteEndElement();
                }

                if (pump.ECurve != null) {
                    this.writer.WriteStartElement("option");
                    this.writer.WriteAttributeString("pump", pump.Id);
                    this.writer.WriteAttributeString("effic", pump.ECurve.Id);
                    this.writer.WriteEndElement();
                }
            }

            this.writer.WriteEndElement();

        }

        private void WriteTag(string name, string value) {
            this.writer.WriteStartElement("option");
            this.writer.WriteAttributeString("name", name);
            this.writer.WriteAttributeString("value", value);
            this.writer.WriteEndElement();
        }

        private void ComposeTimes(Network net) {
            
            this.writer.WriteStartElement(SectType.TIMES.ToString());

            WriteTag("DURATION", net.Duration.GetClockTime());
            WriteTag("HYDRAULIC_TIMESTEP", net.HStep.GetClockTime());
            WriteTag("QUALITY_TIMESTEP", net.QStep.GetClockTime());
            WriteTag("REPORT_TIMESTEP", net.RStep.GetClockTime());
            WriteTag("REPORT_START", net.RStart.GetClockTime());
            WriteTag("PATTERN_TIMESTEP", net.PStep.GetClockTime());
            WriteTag("PATTERN_START", net.PStart.GetClockTime());
            WriteTag("RULE_TIMESTEP", net.RuleStep.GetClockTime());
            WriteTag("START_CLOCKTIME", net.TStart.GetClockTime());
            WriteTag("STATISTIC", net.TStatFlag.ParseStr());

            this.writer.WriteEndElement();
        }



        private void ComposeOptions(Network net) {
            FieldsMap fMap = net.FieldsMap;

            this.writer.WriteStartElement(SectType.OPTIONS.ToString());

            this.WriteTag("units", net.FlowFlag.ParseStr());
            this.WriteTag("pressure", net.PressFlag.ParseStr());
            this.WriteTag("headloss", net.FormFlag.ParseStr());

            if(!string.IsNullOrEmpty(net.DefPatId))
                this.WriteTag("pattern", net.DefPatId);

            switch (net.HydFlag) {
            case HydType.USE:
                WriteTag("hydraulics use", net.HydFname);
                break;
            case HydType.SAVE:
                WriteTag("hydraulics save", net.HydFname);
                break;
            }

            if (net.ExtraIter == -1) {
                WriteTag("unbalanced stop", "");
            }
            else if (net.ExtraIter >= 0) {
                WriteTag("unbalanced continue", net.ExtraIter.ToString());
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
                WriteTag("quality", "age");
                break;
            case QualType.NONE:
                WriteTag("quality", "none");
                break;
            }

            this.WriteTag("demand_multiplier", net.DMult.ToString(NumberFormatInfo.InvariantInfo));
            this.WriteTag("emitter_exponent", (1.0 / net.QExp).ToString(NumberFormatInfo.InvariantInfo));
            this.WriteTag("viscosity", (net.Viscos / Constants.VISCOS).ToString(NumberFormatInfo.InvariantInfo));
            this.WriteTag("diffusivity", (net.Diffus / Constants.DIFFUS).ToString(NumberFormatInfo.InvariantInfo));
            this.WriteTag("specific_gravity", net.SpGrav.ToString(NumberFormatInfo.InvariantInfo));
            this.WriteTag("trials", net.MaxIter.ToString());
            this.WriteTag("accuracy", net.HAcc.ToString(CultureInfo.InvariantCulture));
            this.WriteTag("tolerance", fMap.RevertUnit(FieldType.QUALITY, net.Ctol).ToString(CultureInfo.InvariantCulture));
            this.WriteTag("checkfreq", net.CheckFreq.ToString());
            this.WriteTag("maxcheck", net.MaxCheck.ToString());
            this.WriteTag("damplimit", net.DampLimit.ToString(CultureInfo.InvariantCulture));

            ComposeExtraOptions(net);

            this.writer.WriteEndElement();
        }

        private void ComposeExtraOptions(Network net) {
            var extraOptions = net.ExtraOptions;

            if(extraOptions.Count == 0)
                return;

            foreach(var pair in extraOptions) {
                this.WriteTag(pair.Key, pair.Value);
            }

            this.writer.WriteEndElement();
        }

        private void ComposeReport(Network net) {

            this.writer.WriteStartElement(SectType.REPORT.ToString());

            FieldsMap fMap = net.FieldsMap;
            WriteTag("pagesize", net.PageSize.ToString());
            WriteTag("status", net.Stat_Flag.ParseStr());
            WriteTag("summary", (net.SummaryFlag ? Keywords.w_YES : Keywords.w_NO));
            WriteTag("energy", (net.EnergyFlag ? Keywords.w_YES : Keywords.w_NO));

            switch (net.NodeFlag) {
            case ReportFlag.FALSE:
                WriteTag("nodes", "none");
                break;
            case ReportFlag.TRUE:
                WriteTag("nodes", "all");
                break;
            case ReportFlag.SOME: {
                    this.writer.WriteStartElement("option");
                    this.writer.WriteAttributeString("name", "nodes");
                    this.writer.WriteAttributeString("value", "some");

                foreach (Node node in net.Nodes) {
                    if (node.RptFlag) {
                        this.writer.WriteStartElement("node");
                        this.writer.WriteAttributeString("id", node.Id);
                        this.writer.WriteEndElement();
                    }
                }

                this.writer.WriteEndElement();
                break;
            }
            }

            switch (net.LinkFlag) {
            case ReportFlag.FALSE:
                WriteTag("links", "none");
                break;
            case ReportFlag.TRUE:
                WriteTag("links", "all");
                break;
            case ReportFlag.SOME:
                this.writer.WriteStartElement("option");
                this.writer.WriteAttributeString("name", "links");
                this.writer.WriteAttributeString("value", "some");

                foreach (Link link in net.Links) {
                    if (link.RptFlag) {
                        this.writer.WriteStartElement("link");
                        this.writer.WriteAttributeString("id", link.Id);
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
                    this.writer.WriteAttributeString("below", f.GetRptLim(RangeType.LOW).ToString(NumberFormatInfo.InvariantInfo));

                if (f.GetRptLim(RangeType.HI) > -Constants.BIG)
                    this.writer.WriteAttributeString("above", f.GetRptLim(RangeType.HI).ToString(NumberFormatInfo.InvariantInfo));

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeCoordinates(Network net) {

            this.writer.WriteStartElement(SectType.COORDINATES.ToString());

            foreach(Node node in net.Nodes) {
                if (node.Position.IsInvalid) continue;
                this.writer.WriteStartElement("node");
                this.writer.WriteAttributeString("id", node.Id);
                this.writer.WriteAttributeString("x", node.Position.X.ToString(NumberFormatInfo.InvariantInfo));
                this.writer.WriteAttributeString("y", node.Position.Y.ToString(NumberFormatInfo.InvariantInfo));
                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }


        private void ComposeLabels(Network net) {
            if (net.Labels.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.LABELS.ToString());
            
            foreach(Label label in net.Labels) {
                this.writer.WriteStartElement("label");
                this.writer.WriteAttributeString("x", label.Position.X.ToString(NumberFormatInfo.InvariantInfo));
                this.writer.WriteAttributeString("y", label.Position.Y.ToString(NumberFormatInfo.InvariantInfo));
                // this.buffer.WriteAttributeString("node", label.AnchorNodeId); // TODO: add AnchorNodeId property to label

                this.writer.WriteElementString("text", label.Text);

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeVertices(Network net) {
            this.writer.WriteStartElement(SectType.VERTICES.ToString());

            foreach(Link link in net.Links) {
                if(link.Vertices.Count == 0)
                    continue;

                this.writer.WriteStartElement("link");
                this.writer.WriteAttributeString("id", link.Id);
                
                foreach(EnPoint p in link.Vertices) {
                    this.writer.WriteStartElement("point");
                    this.writer.WriteAttributeString("x", p.X.ToString(NumberFormatInfo.InvariantInfo));
                    this.writer.WriteAttributeString("y", p.Y.ToString(NumberFormatInfo.InvariantInfo));
                    this.writer.WriteEndElement();
                }

                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }

        private void ComposeRules(Network net) {
            if(net.Rules.Count == 0)
                return;

            this.writer.WriteStartElement(SectType.RULES.ToString());
            
            foreach(Rule r in net.Rules) {
                this.writer.WriteStartElement("rule");
                this.writer.WriteAttributeString("label", r.Label);

                foreach(string s in r.Code)
                    this.writer.WriteElementString("code", s);
                    
                this.writer.WriteEndElement();
            }

            this.writer.WriteEndElement();
        }
    }

}