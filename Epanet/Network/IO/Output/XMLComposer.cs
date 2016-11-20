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
using Epanet.Network.Structures;
using Epanet.Util;

namespace Epanet.Network.IO.Output {

    public class XMLComposer:OutputComposer {
        private readonly bool gzip;
        private XmlWriter buffer;
        public XMLComposer(bool gzip) { this.gzip = gzip; }

        public override void Composer(Network net, string f) {
            try {
                // var fs = new FileStream(f, FileMode.Create, FileAccess.Write, FileShare.Read, 0x1000, FileOptions.SequentialScan);

                var settings = new XmlWriterSettings {
                    Indent = true,
                    Encoding = Encoding.UTF8
                };

                using (this.buffer = XmlWriter.Create(f, settings)) {
                    this.buffer.WriteStartDocument();
                    this.buffer.WriteStartElement("network");

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

                    this.buffer.WriteEndElement();
                    this.buffer.WriteEndDocument();
                }
            }
            catch (IOException e) {
                Debug.Print(e.ToString());
                throw new ENException(ErrorCode.Err308);
            }
        }

        private void ComposeTitle(Network net) {
            if (net.TitleText.Count <= 0) return;

            this.buffer.WriteStartElement(Network.SectType.TITLE.ToString());
            foreach(string s in net.TitleText) {
                this.buffer.WriteElementString("line", s);
            }
            this.buffer.WriteEndElement();
        }

        private void ComposeJunctions(Network net) {
            if (!net.Junctions.Any()) return;

            var fMap = net.FieldsMap;

            this.buffer.WriteStartElement(Network.SectType.JUNCTIONS.ToString());
          
            foreach(Node node in net.Junctions) {
                this.buffer.WriteStartElement("node");
                this.buffer.WriteAttributeString("id", node.Id);
                this.buffer.WriteAttributeString("elev", fMap.RevertUnit(FieldsMap.FieldType.ELEV, node.Elevation).ToString(NumberFormatInfo.InvariantInfo));
                
                if(node.Demand.Count > 0) {
                    Demand d = node.Demand[0];
                    var baseDemand = fMap.RevertUnit(FieldsMap.FieldType.DEMAND, d.Base);
                    this.buffer.WriteAttributeString("demand", baseDemand.ToString(NumberFormatInfo.InvariantInfo));

                    if(!string.IsNullOrEmpty(d.Pattern.Id)
                        && !net.PropertiesMap.DefPatId.Equals(d.Pattern.Id, StringComparison.OrdinalIgnoreCase))
                        this.buffer.WriteAttributeString("pattern", d.Pattern.Id);
                    
                    if (!string.IsNullOrEmpty(node.Comment)) {
                        this.buffer.WriteElementString("comment", node.Comment);
                        // writer.WriteCData(node.Comment);    
                    }
                    
                }

                this.buffer.WriteEndElement();

            }

            this.buffer.WriteEndElement();
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

            this.buffer.WriteStartElement(Network.SectType.RESERVOIRS.ToString());

            foreach(Tank r in reservoirs) {
                this.buffer.WriteStartElement("node");
                this.buffer.WriteAttributeString("id", r.Id);
                this.buffer.WriteAttributeString("head", fMap.RevertUnit(FieldsMap.FieldType.ELEV, r.Elevation).ToString(NumberFormatInfo.InvariantInfo));
                
                if(r.Pattern != null)
                    this.buffer.WriteAttributeString("pattern", r.Pattern.Id);

                if(!string.IsNullOrEmpty(r.Comment))
                    this.buffer.WriteElementString("comment", r.Comment);
                    
                this.buffer.WriteEndElement();
                
            }

            this.buffer.WriteEndElement();
            
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

            this.buffer.WriteStartElement(Network.SectType.TANKS.ToString());

            foreach(Tank tank in tanks) {
                double vmin = tank.Vmin;
                if(Math.Round(vmin / tank.Area) == Math.Round(tank.Hmin - tank.Elevation))
                    vmin = 0;

                double elev = fMap.RevertUnit(FieldsMap.FieldType.ELEV, tank.Elevation);
                double initlvl = fMap.RevertUnit(FieldsMap.FieldType.ELEV, tank.H0 - tank.Elevation);
                double minlvl = fMap.RevertUnit(FieldsMap.FieldType.ELEV, tank.Hmin - tank.Elevation);
                double maxlvl = fMap.RevertUnit(FieldsMap.FieldType.ELEV, tank.Hmax - tank.Elevation);
                double diam = fMap.RevertUnit(FieldsMap.FieldType.ELEV, 2 * Math.Sqrt(tank.Area / Math.PI));
                double minvol = fMap.RevertUnit(FieldsMap.FieldType.VOLUME, vmin);

                this.buffer.WriteStartElement("node");
                this.buffer.WriteAttributeString("id", tank.Id);
                this.buffer.WriteAttributeString("elev", elev.ToString(NumberFormatInfo.InvariantInfo));
                this.buffer.WriteAttributeString("initlvl", initlvl.ToString(NumberFormatInfo.InvariantInfo));
                this.buffer.WriteAttributeString("minlvl", minlvl.ToString(NumberFormatInfo.InvariantInfo));
                this.buffer.WriteAttributeString("maxlvl", maxlvl.ToString(NumberFormatInfo.InvariantInfo));
                this.buffer.WriteAttributeString("diam", diam.ToString(NumberFormatInfo.InvariantInfo));
                this.buffer.WriteAttributeString("minvol", minvol.ToString(NumberFormatInfo.InvariantInfo));

                if(tank.Vcurve != null)
                    this.buffer.WriteAttributeString("volcurve", tank.Vcurve.Id);

                if(!string.IsNullOrEmpty(tank.Comment))
                    this.buffer.WriteElementString("comment", tank.Comment);
                    

                this.buffer.WriteEndElement();
            }

            this.buffer.WriteEndElement();
        }


        private void ComposePipes(Network net) {
            FieldsMap fMap = net.FieldsMap;
            PropertiesMap pMap = net.PropertiesMap;

            if(net.Links.Count == 0)
                return;

            List<Link> pipes = new List<Link>();
            foreach(Link link in net.Links)
                if(link.Type == Link.LinkType.PIPE || link.Type == Link.LinkType.CV)
                    pipes.Add(link);


            this.buffer.WriteStartElement(Network.SectType.PIPES.ToString());
            
            foreach(Link link in pipes) {
                double d = link.Diameter;
                double kc = link.Roughness;
                if(pMap.FormFlag == PropertiesMap.FormType.DW)
                    kc = fMap.RevertUnit(FieldsMap.FieldType.ELEV, kc * 1000.0);

                double km = link.Km * Math.Pow(d, 4.0) / 0.02517;

                this.buffer.WriteStartElement("link");
                this.buffer.WriteAttributeString("id", link.Id);
                this.buffer.WriteAttributeString("node1", link.FirstNode.Id);
                this.buffer.WriteAttributeString("node2", link.SecondNode.Id);
                this.buffer.WriteAttributeString("length", fMap.RevertUnit(FieldsMap.FieldType.LENGTH, link.Lenght).ToString(NumberFormatInfo.InvariantInfo));
                this.buffer.WriteAttributeString("diam", fMap.RevertUnit(FieldsMap.FieldType.DIAM, d).ToString(NumberFormatInfo.InvariantInfo));
                this.buffer.WriteAttributeString("roughness", kc.ToString(NumberFormatInfo.InvariantInfo));
                this.buffer.WriteAttributeString("mloss", km.ToString(NumberFormatInfo.InvariantInfo));

                if(link.Type == Link.LinkType.CV)
                    this.buffer.WriteAttributeString("status", Link.LinkType.CV.ToString());
                else if(link.Status == Link.StatType.CLOSED)
                    this.buffer.WriteAttributeString("status", Link.StatType.CLOSED.ToString());
                else if(link.Status == Link.StatType.OPEN)
                    this.buffer.WriteAttributeString("status", Link.StatType.OPEN.ToString());

                if(!string.IsNullOrEmpty(link.Comment))
                    this.buffer.WriteElementString("comment", link.Comment);

                this.buffer.WriteEndElement();
            }

            this.buffer.WriteEndElement();
        }


        private void ComposePumps(Network net) {
            FieldsMap fMap = net.FieldsMap;

            if(!net.Pumps.Any())
                return;

            this.buffer.WriteStartElement(Network.SectType.PUMPS.ToString());
            
            foreach(Pump pump in net.Pumps) {
                this.buffer.WriteStartElement("link");
                this.buffer.WriteAttributeString("id", pump.Id);
                this.buffer.WriteAttributeString("node1", pump.FirstNode.Id);
                this.buffer.WriteAttributeString("node2", pump.SecondNode.Id);


                // Pump has constant power
                if(pump.Ptype == Pump.PumpType.CONST_HP)
                    this.buffer.WriteAttributeString("power", pump.Km.ToString(NumberFormatInfo.InvariantInfo));
                // Pump has a head curve
                else if(pump.HCurve != null)
                    this.buffer.WriteAttributeString("head", pump.HCurve.Id);
                // Old format used for pump curve
                else {
                    this.buffer.WriteAttributeString("h0", fMap.RevertUnit(FieldsMap.FieldType.HEAD, -pump.H0).ToString(NumberFormatInfo.InvariantInfo));
                    this.buffer.WriteAttributeString("h1", fMap.RevertUnit(FieldsMap.FieldType.HEAD, -pump.H0 - pump.FlowCoefficient * Math.Pow(pump.Q0, pump.N)).ToString(NumberFormatInfo.InvariantInfo));
                    this.buffer.WriteAttributeString("q1", fMap.RevertUnit(FieldsMap.FieldType.FLOW, pump.Q0).ToString(NumberFormatInfo.InvariantInfo));
                    this.buffer.WriteAttributeString("h2", 0.0.ToString(NumberFormatInfo.InvariantInfo));
                    this.buffer.WriteAttributeString("q2", fMap.RevertUnit(FieldsMap.FieldType.FLOW, pump.Qmax).ToString(NumberFormatInfo.InvariantInfo));

                    continue;
                }

                if(pump.UPat != null)
                    this.buffer.WriteAttributeString("pattern", pump.UPat.Id);

                if(pump.Roughness != 1.0)
                    this.buffer.WriteAttributeString("speed", pump.Roughness.ToString(NumberFormatInfo.InvariantInfo));

                if(!string.IsNullOrEmpty(pump.Comment))
                    this.buffer.WriteAttributeString("comment", pump.Comment);

                this.buffer.WriteEndElement();
            }

            this.buffer.WriteEndElement();
        }

        private void ComposeValves(Network net) {
            FieldsMap fMap = net.FieldsMap;

            if(!net.Valves.Any())
                return;

            this.buffer.WriteStartElement(Network.SectType.VALVES.ToString());
            
            foreach(Valve valve in net.Valves) {
                double d = valve.Diameter;
                double kc = valve.Roughness;
                if(kc.IsMissing())
                    kc = 0.0;

                switch(valve.Type) {
                case Link.LinkType.FCV:
                kc = fMap.RevertUnit(FieldsMap.FieldType.FLOW, kc);
                break;
                case Link.LinkType.PRV:
                case Link.LinkType.PSV:
                case Link.LinkType.PBV:
                kc = fMap.RevertUnit(FieldsMap.FieldType.PRESSURE, kc);
                break;
                }

                double km = valve.Km * Math.Pow(d, 4) / 0.02517;

                this.buffer.WriteStartElement("link");
                this.buffer.WriteAttributeString("id", valve.Id);
                this.buffer.WriteAttributeString("node1", valve.FirstNode.Id);
                this.buffer.WriteAttributeString("node2", valve.SecondNode.Id);
                this.buffer.WriteAttributeString("diameter", fMap.RevertUnit(FieldsMap.FieldType.DIAM, d).ToString(NumberFormatInfo.InvariantInfo));
                this.buffer.WriteAttributeString("type", valve.Type.ParseStr());

                if (valve.Type == Link.LinkType.GPV && valve.Curve != null) {
                    this.buffer.WriteAttributeString("setting", valve.Curve.Id);
                }
                else {
                    this.buffer.WriteAttributeString("setting", kc.ToString(NumberFormatInfo.InvariantInfo));
                }

                this.buffer.WriteAttributeString("minorloss", km.ToString(NumberFormatInfo.InvariantInfo));

                if(!string.IsNullOrEmpty(valve.Comment))
                    this.buffer.WriteElementString("comment", valve.Comment);

                this.buffer.WriteEndElement();
            }

            this.buffer.WriteEndElement();
        }

        private void ComposeDemands(Network net) {
            FieldsMap fMap = net.FieldsMap;

            if(!net.Junctions.Any())
                return;

            this.buffer.WriteStartElement(Network.SectType.DEMANDS.ToString());
            
            double ucf = fMap.GetUnits(FieldsMap.FieldType.DEMAND);

            foreach(Node node in net.Junctions) {
                if (node.Demand.Count > 1)
                    foreach (Demand demand in node.Demand) {
                        this.buffer.WriteStartElement("node");
                        this.buffer.WriteAttributeString("id", node.Id);
                        this.buffer.WriteAttributeString("demand", (ucf * demand.Base).ToString(NumberFormatInfo.InvariantInfo));
                        if (demand.Pattern != null)
                            this.buffer.WriteAttributeString("pattern", demand.Pattern.Id);

                        this.buffer.WriteEndElement();
                    }
            }

            this.buffer.WriteEndElement();
        }

        private void ComposeEmitters(Network net) {

            if(net.Nodes.Count == 0)
                return;

            this.buffer.WriteStartElement(Network.SectType.EMITTERS.ToString());
            
            double uflow = net.FieldsMap.GetUnits(FieldsMap.FieldType.FLOW);
            double upressure = net.FieldsMap.GetUnits(FieldsMap.FieldType.PRESSURE);
            double qexp = net.PropertiesMap.QExp;

            foreach(Node node in net.Junctions) {
                if(node.Ke == 0.0)
                    continue;
                double ke = uflow / Math.Pow(upressure * node.Ke, (1.0 / qexp));

                this.buffer.WriteStartElement("node");
                this.buffer.WriteAttributeString("id", node.Id);
                this.buffer.WriteAttributeString("coefficient", ke.ToString(NumberFormatInfo.InvariantInfo));
                this.buffer.WriteEndElement();
            }

            this.buffer.WriteEndElement();
        }


        private void ComposeStatus(Network net) {

            if(net.Links.Count == 0)
                return;

            this.buffer.WriteStartElement(Network.SectType.STATUS.ToString());
            
            foreach(Link link in net.Links) {
                if(link.Type <= Link.LinkType.PUMP) {
                    if (link.Status == Link.StatType.CLOSED) {
                        this.buffer.WriteStartElement("link");
                        this.buffer.WriteAttributeString("id", link.Id);
                        this.buffer.WriteAttributeString("setting", Link.StatType.CLOSED.ParseStr());
                        this.buffer.WriteEndElement();
                    }
                    else if(link.Type == Link.LinkType.PUMP) {
                        // Write pump speed here for pumps with old-style pump curve input
                        Pump pump = (Pump)link;
                        if (pump.HCurve == null &&
                            pump.Ptype != Pump.PumpType.CONST_HP &&
                            pump.Roughness != 1.0) {
                            this.buffer.WriteStartElement("link");
                            this.buffer.WriteAttributeString("id", link.Id);
                            this.buffer.WriteAttributeString("setting", link.Roughness.ToString(NumberFormatInfo.InvariantInfo));
                            this.buffer.WriteEndElement();
                        }
                    }
                }
                else if(link.Roughness.IsMissing()) {
                    // Write fixed-status PRVs & PSVs (setting = MISSING)
                    switch (link.Status) {
                    case Link.StatType.OPEN:
                        this.buffer.WriteStartElement("link");
                        this.buffer.WriteAttributeString("id", link.Id);
                        this.buffer.WriteAttributeString("setting", Link.StatType.OPEN.ParseStr());
                        this.buffer.WriteEndElement();
                        break;
                    case Link.StatType.CLOSED:
                        this.buffer.WriteStartElement("link");
                        this.buffer.WriteAttributeString("id", link.Id);
                        this.buffer.WriteAttributeString("setting", Link.StatType.CLOSED.ParseStr());
                        this.buffer.WriteEndElement();
                        break;
                    }

                }

            }

            this.buffer.WriteEndElement();
        }

        private void ComposePatterns(Network net) {

            var pats = net.Patterns;

            if(pats.Count <= 1)
                return;

            this.buffer.WriteStartElement(Network.SectType.PATTERNS.ToString());

            for(int i = 1; i < pats.Count; i++) {
                Pattern pat = pats[i];
                List<double> f = pat.FactorsList;

                this.buffer.WriteStartElement("pattern");
                this.buffer.WriteAttributeString("id", pat.Id);

                for(int j = 0; j < pats[i].Length; j++) {
                    this.buffer.WriteStartElement("factor");
                    this.buffer.WriteAttributeString("value", f[j].ToString(NumberFormatInfo.InvariantInfo));
                    this.buffer.WriteEndElement();
                }

                this.buffer.WriteEndElement();
            }

            this.buffer.WriteEndElement();
        }

        private void ComposeCurves(Network net) {

            var curves = net.Curves;

            if(curves.Count == 0)
                return;

            this.buffer.WriteStartElement(Network.SectType.CURVES.ToString());

            foreach(Curve c in curves) {
                this.buffer.WriteStartElement("curve");
                this.buffer.WriteAttributeString("id", c.Id);

                foreach(var pt in c.Points) {
                    this.buffer.WriteStartElement("point");
                    this.buffer.WriteAttributeString("x", pt.X.ToString(NumberFormatInfo.InvariantInfo));
                    this.buffer.WriteAttributeString("y", pt.Y.ToString(NumberFormatInfo.InvariantInfo));
                    this.buffer.WriteEndElement();
                }

                this.buffer.WriteEndElement();
            }

            this.buffer.WriteEndElement();
        }

        private void ComposeControls(Network net) {
            var controls = net.Controls;
            FieldsMap fmap = net.FieldsMap;

            if(controls.Count == 0)
                return;

            this.buffer.WriteStartElement(Network.SectType.CONTROLS.ToString());

            foreach(Control control in controls) {
                // Check that controlled link exists
                if(control.Link == null)
                    continue;

                this.buffer.WriteStartElement("control");

                // Get text of control's link status/setting
                if (control.Setting.IsMissing()) {
                    this.buffer.WriteAttributeString("link", control.Link.Id);
                    this.buffer.WriteAttributeString("status", control.Status.ParseStr());
                }
                else {
                    double kc = control.Setting;
                    switch(control.Link.Type) {
                    case Link.LinkType.PRV:
                    case Link.LinkType.PSV:
                    case Link.LinkType.PBV:
                    kc = fmap.RevertUnit(FieldsMap.FieldType.PRESSURE, kc);
                    break;
                    case Link.LinkType.FCV:
                    kc = fmap.RevertUnit(FieldsMap.FieldType.FLOW, kc);
                    break;
                    }

                    this.buffer.WriteAttributeString("link", control.Link.Id);
                    this.buffer.WriteAttributeString("status", kc.ToString(NumberFormatInfo.InvariantInfo));
                }


                switch(control.Type) {
                // Print level control
                case Control.ControlType.LOWLEVEL:
                case Control.ControlType.HILEVEL:
                double kc = control.Grade - control.Node.Elevation;
               
                if(control.Node is Tank)
                    kc = fmap.RevertUnit(FieldsMap.FieldType.HEAD, kc);
                else
                    kc = fmap.RevertUnit(FieldsMap.FieldType.PRESSURE, kc);


                this.buffer.WriteAttributeString("type", control.Type.ParseStr());
                this.buffer.WriteAttributeString("node", control.Node.Id);
                this.buffer.WriteAttributeString("value", kc.ToString(NumberFormatInfo.InvariantInfo));
                
                break;

                case Control.ControlType.TIMER:
                case Control.ControlType.TIMEOFDAY:
                // Print timer control
                // Print time-of-day control
                this.buffer.WriteAttributeString("type", control.Type.ParseStr());
                this.buffer.WriteAttributeString("value", control.Time.ToString(NumberFormatInfo.InvariantInfo));

                break;

                }

                this.buffer.WriteEndElement();
            }

            this.buffer.WriteEndElement();
        }

        private void ComposeQuality(Network net) {
            FieldsMap fmap = net.FieldsMap;

            if(net.Nodes.Count == 0)
                return;

            this.buffer.WriteStartElement(Network.SectType.QUALITY.ToString());
            
            foreach(Node node in net.Nodes) {
                if(node.C0.Length == 1) {
                    if(node.C0[0] == 0.0)
                        continue;

                    this.buffer.WriteStartElement("node");
                    this.buffer.WriteAttributeString("id", node.Id);
                    this.buffer.WriteAttributeString("value", fmap.RevertUnit(FieldsMap.FieldType.QUALITY, node.C0[0]).ToString(NumberFormatInfo.InvariantInfo));
                    this.buffer.WriteEndElement();
                    
                }
            }

            this.buffer.WriteEndElement();
        }

        private void ComposeSource(Network net) {

            if(net.Nodes.Count == 0)
                return;

            this.buffer.WriteStartElement(Network.SectType.SOURCES.ToString());
            
            foreach(Node node in net.Nodes) {
                Source source = node.Source;
                if(source == null)
                    continue;

                this.buffer.WriteStartElement("node");
                this.buffer.WriteAttributeString("id", node.Id);
                this.buffer.WriteAttributeString("type", source.Type.ParseStr());
                this.buffer.WriteAttributeString("quality", source.C0.ToString(NumberFormatInfo.InvariantInfo));


                if(source.Pattern != null)
                    this.buffer.WriteAttributeString("pattern", source.Pattern.Id);
                    
                this.buffer.WriteEndElement();
            }

            this.buffer.WriteEndElement();
        }


        private void ComposeMixing(Network net) {

            if(!net.Tanks.Any())
                return;

            this.buffer.WriteStartElement(Network.SectType.MIXING.ToString());
            
            foreach(Tank tank in net.Tanks) {
                if(tank.IsReservoir)
                    continue;

                this.buffer.WriteStartElement("node");
                this.buffer.WriteAttributeString("id", tank.Id);
                this.buffer.WriteAttributeString("model", tank.MixModel.ParseStr());
                this.buffer.WriteAttributeString("value", (tank.V1Max / tank.Vmax).ToString(NumberFormatInfo.InvariantInfo));
                this.buffer.WriteEndElement();
            }

            this.buffer.WriteEndElement();
        }

        private void ComposeReaction(Network net) {
            PropertiesMap pMap = net.PropertiesMap;

            this.buffer.WriteStartElement(Network.SectType.REACTIONS.ToString());

            WriteTag("order bulk", pMap.BulkOrder.ToString(NumberFormatInfo.InvariantInfo));
            WriteTag("order wall", pMap.WallOrder.ToString(NumberFormatInfo.InvariantInfo));
            WriteTag("order tank", pMap.TankOrder.ToString(NumberFormatInfo.InvariantInfo));
            WriteTag("global bulk", (pMap.KBulk * Constants.SECperDAY).ToString(NumberFormatInfo.InvariantInfo));
            WriteTag("global wall", (pMap.KWall * Constants.SECperDAY).ToString(NumberFormatInfo.InvariantInfo));
            WriteTag("limiting potential", pMap.CLimit.ToString(NumberFormatInfo.InvariantInfo));
            WriteTag("roughness correlation", pMap.RFactor.ToString(NumberFormatInfo.InvariantInfo));



            foreach(Link link in net.Links) {
                if(link.Type > Link.LinkType.PIPE)
                    continue;

                if (link.Kb != pMap.KBulk) {
                    this.buffer.WriteStartElement("option");
                    this.buffer.WriteAttributeString("name", "bulk");
                    this.buffer.WriteAttributeString("link", link.Id);
                    this.buffer.WriteAttributeString("value", (link.Kb * Constants.SECperDAY).ToString(NumberFormatInfo.InvariantInfo));
                    this.buffer.WriteEndElement();                    
                }

                if (link.Kw != pMap.KWall) {
                    this.buffer.WriteStartElement("option");
                    this.buffer.WriteAttributeString("name", "wall");
                    this.buffer.WriteAttributeString("link", link.Id);
                    this.buffer.WriteAttributeString("value", (link.Kw * Constants.SECperDAY).ToString(NumberFormatInfo.InvariantInfo));
                    this.buffer.WriteEndElement();
                }
            }

            foreach(Tank tank in net.Tanks) {
                if(tank.IsReservoir)
                    continue;
                if (tank.Kb != pMap.KBulk) {
                    this.buffer.WriteStartElement("option");
                    this.buffer.WriteAttributeString("name", "tank");
                    this.buffer.WriteAttributeString("id", tank.Id);
                    this.buffer.WriteAttributeString("value", (tank.Kb * Constants.SECperDAY).ToString(NumberFormatInfo.InvariantInfo));
                    this.buffer.WriteEndElement();
                }
            }

            this.buffer.WriteEndElement();
        }

        private void ComposeEnergy(Network net) {
            PropertiesMap pMap = net.PropertiesMap;

            this.buffer.WriteStartElement(Network.SectType.ENERGY.ToString());

            if (pMap.ECost != 0.0)
                WriteTag("global price", pMap.ECost.ToString(NumberFormatInfo.InvariantInfo));

            if (!string.IsNullOrEmpty(pMap.EPatId))
                WriteTag("global pattern", pMap.EPatId);

            WriteTag("global effic", pMap.EPump.ToString(NumberFormatInfo.InvariantInfo));
            WriteTag("demand charge", pMap.DCost.ToString(NumberFormatInfo.InvariantInfo));

            foreach(Pump pump in net.Pumps) {
                if (pump.ECost > 0.0) {
                    this.buffer.WriteStartElement("option");
                    this.buffer.WriteAttributeString("pump", pump.Id);
                    this.buffer.WriteAttributeString("price", pump.ECost.ToString(NumberFormatInfo.InvariantInfo));
                    this.buffer.WriteEndElement();
                }

                if (pump.EPat != null) {
                    this.buffer.WriteStartElement("option");
                    this.buffer.WriteAttributeString("pump", pump.Id);
                    this.buffer.WriteAttributeString("pattern", pump.EPat.Id);
                    this.buffer.WriteEndElement();
                }

                if (pump.ECurve != null) {
                    this.buffer.WriteStartElement("option");
                    this.buffer.WriteAttributeString("pump", pump.Id);
                    this.buffer.WriteAttributeString("effic", pump.ECurve.Id);
                    this.buffer.WriteEndElement();
                }
            }

            this.buffer.WriteEndElement();

        }

        private void WriteTag(string name, string value) {
            this.buffer.WriteStartElement("option");
            this.buffer.WriteAttributeString("name", name);
            this.buffer.WriteAttributeString("value", value);
            this.buffer.WriteEndElement();
        }

        private void ComposeTimes(Network net) {
            PropertiesMap pMap = net.PropertiesMap;
            this.buffer.WriteStartElement(Network.SectType.TIMES.ToString());

            WriteTag("DURATION", pMap.Duration.GetClockTime());
            WriteTag("HYDRAULIC_TIMESTEP", pMap.HStep.GetClockTime());
            WriteTag("QUALITY_TIMESTEP", pMap.QStep.GetClockTime());
            WriteTag("REPORT_TIMESTEP", pMap.RStep.GetClockTime());
            WriteTag("REPORT_START", pMap.RStart.GetClockTime());
            WriteTag("PATTERN_TIMESTEP", pMap.PStep.GetClockTime());
            WriteTag("PATTERN_START", pMap.PStart.GetClockTime());
            WriteTag("RULE_TIMESTEP", pMap.RuleStep.GetClockTime());
            WriteTag("START_CLOCKTIME", pMap.TStart.GetClockTime());
            WriteTag("STATISTIC", pMap.TStatFlag.ParseStr());

            this.buffer.WriteEndElement();
        }



        private void ComposeOptions(Network net) {
            PropertiesMap pMap = net.PropertiesMap;
            FieldsMap fMap = net.FieldsMap;

            this.buffer.WriteStartElement(Network.SectType.OPTIONS.ToString());

            this.WriteTag("units", pMap.FlowFlag.ParseStr());
            this.WriteTag("pressure", pMap.PressFlag.ParseStr());
            this.WriteTag("headloss", pMap.FormFlag.ParseStr());

            if(!string.IsNullOrEmpty(pMap.DefPatId))
                this.WriteTag("pattern", pMap.DefPatId);

            switch (pMap.HydFlag) {
            case PropertiesMap.HydType.USE:
                WriteTag("hydraulics use", pMap.HydFname);
                break;
            case PropertiesMap.HydType.SAVE:
                WriteTag("hydraulics save", pMap.HydFname);
                break;
            }

            if (pMap.ExtraIter == -1) {
                WriteTag("unbalanced stop", "");
            }
            else if (pMap.ExtraIter >= 0) {
                WriteTag("unbalanced continue", pMap.ExtraIter.ToString());
            }

            switch (pMap.QualFlag) {
            case PropertiesMap.QualType.CHEM:
            this.buffer.WriteStartElement("option");
                this.buffer.WriteAttributeString("name", "quality");
                this.buffer.WriteAttributeString("value", "chem");
                this.buffer.WriteAttributeString("chemname", pMap.ChemName);
                this.buffer.WriteAttributeString("chemunits", pMap.ChemUnits);
                this.buffer.WriteEndElement();
                break;
            case PropertiesMap.QualType.TRACE:
            this.buffer.WriteStartElement("option");
                this.buffer.WriteAttributeString("name", "quality");
                this.buffer.WriteAttributeString("value", "trace");
                this.buffer.WriteAttributeString("node", pMap.TraceNode);
                this.buffer.WriteEndElement();
                break;
            case PropertiesMap.QualType.AGE:
                WriteTag("quality", "age");
                break;
            case PropertiesMap.QualType.NONE:
                WriteTag("quality", "none");
                break;
            }

            this.WriteTag("demand_multiplier", pMap.DMult.ToString(NumberFormatInfo.InvariantInfo));
            this.WriteTag("emitter_exponent", (1.0 / pMap.QExp).ToString(NumberFormatInfo.InvariantInfo));
            this.WriteTag("viscosity", (pMap.Viscos / Constants.VISCOS).ToString(NumberFormatInfo.InvariantInfo));
            this.WriteTag("diffusivity", (pMap.Diffus / Constants.DIFFUS).ToString(NumberFormatInfo.InvariantInfo));
            this.WriteTag("specific_gravity", pMap.SpGrav.ToString(NumberFormatInfo.InvariantInfo));
            this.WriteTag("trials", pMap.MaxIter.ToString());
            this.WriteTag("accuracy", pMap.HAcc.ToString(CultureInfo.InvariantCulture));
            this.WriteTag("tolerance", fMap.RevertUnit(FieldsMap.FieldType.QUALITY, pMap.Ctol).ToString(CultureInfo.InvariantCulture));
            this.WriteTag("checkfreq", pMap.CheckFreq.ToString());
            this.WriteTag("maxcheck", pMap.MaxCheck.ToString());
            this.WriteTag("damplimit", pMap.DampLimit.ToString(CultureInfo.InvariantCulture));

            ComposeExtraOptions(net);

            this.buffer.WriteEndElement();
        }

        private void ComposeExtraOptions(Network net) {
            var extraOptions = net.PropertiesMap.ExtraOptions;

            if(extraOptions.Count == 0)
                return;

            foreach(var pair in extraOptions) {
                this.WriteTag(pair.Key, pair.Value);
            }

            this.buffer.WriteEndElement();
        }

        private void ComposeReport(Network net) {

            this.buffer.WriteStartElement(Network.SectType.REPORT.ToString());

            PropertiesMap pMap = net.PropertiesMap;
            FieldsMap fMap = net.FieldsMap;
            WriteTag("pagesize", pMap.PageSize.ToString());
            WriteTag("status", pMap.Stat_Flag.ParseStr());
            WriteTag("summary", (pMap.SummaryFlag ? Keywords.w_YES : Keywords.w_NO));
            WriteTag("energy", (pMap.EnergyFlag ? Keywords.w_YES : Keywords.w_NO));

            switch (pMap.NodeFlag) {
            case PropertiesMap.ReportFlag.FALSE:
                WriteTag("nodes", "none");
                break;
            case PropertiesMap.ReportFlag.TRUE:
                WriteTag("nodes", "all");
                break;
            case PropertiesMap.ReportFlag.SOME: {
                    this.buffer.WriteStartElement("option");
                    this.buffer.WriteAttributeString("name", "nodes");
                    this.buffer.WriteAttributeString("value", "some");

                foreach (Node node in net.Nodes) {
                    if (node.RptFlag) {
                        this.buffer.WriteStartElement("node");
                        this.buffer.WriteAttributeString("id", node.Id);
                        this.buffer.WriteEndElement();
                    }
                }

                this.buffer.WriteEndElement();
                break;
            }
            }

            switch (pMap.LinkFlag) {
            case PropertiesMap.ReportFlag.FALSE:
                WriteTag("links", "none");
                break;
            case PropertiesMap.ReportFlag.TRUE:
                WriteTag("links", "all");
                break;
            case PropertiesMap.ReportFlag.SOME:
                this.buffer.WriteStartElement("option");
                this.buffer.WriteAttributeString("name", "links");
                this.buffer.WriteAttributeString("value", "some");

                foreach (Link link in net.Links) {
                    if (link.RptFlag) {
                        this.buffer.WriteStartElement("link");
                        this.buffer.WriteAttributeString("id", link.Id);
                        this.buffer.WriteEndElement();
                    }
                }

                this.buffer.WriteEndElement();

                break;
            }

            for(FieldsMap.FieldType i = 0; i < FieldsMap.FieldType.FRICTION; i++) {
                Field f = fMap.GetField(i);

                if (!f.Enabled) {
                    this.buffer.WriteStartElement("field");
                    this.buffer.WriteAttributeString("name", f.Name);
                    this.buffer.WriteAttributeString("enabled", "false");
                    this.buffer.WriteEndElement();
                    continue;
                }

                this.buffer.WriteStartElement("field");
                this.buffer.WriteAttributeString("name", f.Name);
                this.buffer.WriteAttributeString("enabled", "true");
                this.buffer.WriteAttributeString("precision", f.Precision.ToString());

                if (f.GetRptLim(Field.RangeType.LOW) < Constants.BIG)
                    this.buffer.WriteAttributeString("below", f.GetRptLim(Field.RangeType.LOW).ToString(NumberFormatInfo.InvariantInfo));

                if (f.GetRptLim(Field.RangeType.HI) > -Constants.BIG)
                    this.buffer.WriteAttributeString("above", f.GetRptLim(Field.RangeType.HI).ToString(NumberFormatInfo.InvariantInfo));

                this.buffer.WriteEndElement();
            }

            this.buffer.WriteEndElement();
        }

        private void ComposeCoordinates(Network net) {

            this.buffer.WriteStartElement(Network.SectType.COORDINATES.ToString());

            foreach(Node node in net.Nodes) {
                if (node.Position.IsInvalid) continue;
                this.buffer.WriteStartElement("node");
                this.buffer.WriteAttributeString("id", node.Id);
                this.buffer.WriteAttributeString("x", node.Position.X.ToString(NumberFormatInfo.InvariantInfo));
                this.buffer.WriteAttributeString("y", node.Position.Y.ToString(NumberFormatInfo.InvariantInfo));
                this.buffer.WriteEndElement();
            }

            this.buffer.WriteEndElement();
        }


        private void ComposeLabels(Network net) {
            if (net.Labels.Count == 0)
                return;

            this.buffer.WriteStartElement(Network.SectType.LABELS.ToString());
            
            foreach(Label label in net.Labels) {
                this.buffer.WriteStartElement("label");
                this.buffer.WriteAttributeString("x", label.Position.X.ToString(NumberFormatInfo.InvariantInfo));
                this.buffer.WriteAttributeString("y", label.Position.Y.ToString(NumberFormatInfo.InvariantInfo));
                // this.buffer.WriteAttributeString("node", label.AnchorNodeId); // TODO: add AnchorNodeId property to label

                this.buffer.WriteElementString("text", label.Text);

                this.buffer.WriteEndElement();
            }

            this.buffer.WriteEndElement();
        }

        private void ComposeVertices(Network net) {
            this.buffer.WriteStartElement(Network.SectType.VERTICES.ToString());

            foreach(Link link in net.Links) {
                if(link.Vertices.Count == 0)
                    continue;

                this.buffer.WriteStartElement("link");
                this.buffer.WriteAttributeString("id", link.Id);
                
                foreach(EnPoint p in link.Vertices) {
                    this.buffer.WriteStartElement("point");
                    this.buffer.WriteAttributeString("x", p.X.ToString(NumberFormatInfo.InvariantInfo));
                    this.buffer.WriteAttributeString("y", p.Y.ToString(NumberFormatInfo.InvariantInfo));
                    this.buffer.WriteEndElement();
                }

                this.buffer.WriteEndElement();
            }

            this.buffer.WriteEndElement();
        }

        private void ComposeRules(Network net) {
            if(net.Rules.Count == 0)
                return;

            this.buffer.WriteStartElement(Network.SectType.RULES.ToString());
            
            foreach(Rule r in net.Rules) {
                this.buffer.WriteStartElement("rule");
                this.buffer.WriteAttributeString("label", r.Label);

                foreach(string s in r.Code)
                    this.buffer.WriteElementString("code", s);
                    
                this.buffer.WriteEndElement();
            }

            this.buffer.WriteEndElement();
        }
    }

}