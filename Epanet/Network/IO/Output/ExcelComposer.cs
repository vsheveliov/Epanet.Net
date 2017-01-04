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

using Epanet.Enums;
using Epanet.Network.Structures;
using Epanet.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Epanet.Network.IO.Output {

    ///<summary>EXCEL XLSX composer class.</summary>
    public class ExcelComposer:OutputComposer {
        private class ExcelWriter {
            private readonly XSSFWorkbook workbook;
            private IRow activeRow;
            private ISheet activeSheet;
            private int cellCount;
            private int rowCount;

            private readonly ICellStyle timeStyle;
            private readonly ICellStyle topBold;

            public ExcelWriter(XSSFWorkbook workbook) {
                this.workbook = workbook;
                this.topBold = workbook.CreateCellStyle();
                IFont newFont = workbook.CreateFont();
                newFont.Boldweight = (short)FontBoldWeight.Bold;
                this.topBold.SetFont(newFont);
                this.timeStyle = workbook.CreateCellStyle();
                this.timeStyle.DataFormat = 46;
            }

            public void NewLine() {
                this.activeRow = this.activeSheet.CreateRow(this.rowCount++);
                this.cellCount = 0;
            }

            public void NewSpreadsheet(string name) {
                this.rowCount = 0;
                this.cellCount = 0;
                this.activeSheet = this.workbook.CreateSheet(name);
                this.activeRow = null;
            }

            public static bool IsNumber(object value) {
                if (value == null)
                    return false;
                var code = Type.GetTypeCode(value.GetType());

                switch (code) {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
                }

                // return code >= TypeCode.SByte && code <= TypeCode.Decimal;

            }

            public void Write(params object[] args) {
                if (this.activeRow == null) {
                    this.activeRow = this.activeSheet.CreateRow(this.rowCount++);
                    this.cellCount = 0;
                }

                foreach (object obj  in  args) {
                    if (obj is string && obj.Equals(NEWLINE)) {
                        this.activeRow = this.activeSheet.CreateRow(this.rowCount++);
                        this.cellCount = 0;
                        continue;
                    }

                    var c = this.activeRow.CreateCell(this.cellCount++);

                    if (obj == null)
                        c.SetCellType(CellType.Blank);
                    else if (obj is DateTime) {
                        DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
                            //Getting UTC DATE since epoch
                        TimeSpan ts = (DateTime)obj - epochStart;
                            //get the current timestamp between now and january 1970
                        c.SetCellValue(ts.TotalSeconds / 86400.0d);
                        c.CellStyle = this.timeStyle;
                    }
                    else if (obj is bool)
                        c.SetCellValue(((bool)obj));
                    else if (IsNumber(obj))
                        c.SetCellValue(Convert.ToDouble(obj));
                    else
                        c.SetCellValue(obj.ToString());
                }
            }

            public void WriteHeader(string str) {
                string[] sections = str.Split('\t');
                //for (int i = 0; i < sections.length; i++)
                //    sections[i] = ";" + sections[i];
                //if(sections.length>0)
                //    sections[0] = ";" + sections[0];

                //write(sections);
                if (this.activeRow == null) {
                    this.activeRow = this.activeSheet.CreateRow(this.rowCount++);
                    this.cellCount = 0;
                }

                foreach (string obj  in  sections) {
                    ICell c = this.activeRow.CreateCell(this.cellCount++);
                    c.CellStyle = this.topBold;
                    c.SetCellValue(obj);
                }

                this.NewLine();
            }
        }

        private const string COORDINATES_SUBTITLE = "Node\tX-Coord\tY-Coord";
        private const string CURVE_SUBTITLE = "ID\tX-Value\tY-Value";
        private const string DEMANDS_SUBTITLE = "Junction\tDemand\tPattern\tCategory";
        private const string EMITTERS_SUBTITLE = "Junction\tCoefficient";
        private const string JUNCS_SUBTITLE = "ID\tElev\tDemand\tPattern\tComment";
        private const string LABELS_SUBTITLE = "X-Coord\tY-Coord\tLabel & Anchor Node";
        private const string MIXING_SUBTITLE = "Tank\tModel";
        private const string NEWLINE = "\n";
        private const string PATTERNS_SUBTITLE = "ID\tMultipliers";
        private const string PIPES_SUBTITLE =
            "ID\tNode1\tNode2\tLength\tDiameter\tRoughness\tMinorLoss\tStatus\tComment";
        private const string PUMPS_SUBTITLE = "ID\tNode1\tNode2\tParameters\tValue\tComment";
        private const string QUALITY_SUBTITLE = "Node\tInitQual";
        private const string REACTIONS_SUBTITLE = "Type\tPipe/Tank";
        private const string RESERVOIRS_SUBTITLE = "ID\tHead\tPattern\tComment";
        private const string SOURCE_SUBTITLE = "Node\tType\tQuality\tPattern";
        private const string STATUS_SUBTITLE = "ID\tStatus/Setting";
        private const string TANK_SUBTITLE =
            "ID\tElevation\tInitLevel\tMinLevel\tMaxLevel\tDiameter\tMinVol\tVolCurve\tComment";
        private const string TITLE_SUBTITLE = "Text";
        private const string VALVES_SUBTITLE = "ID\tNode1\tNode2\tDiameter\tType\tSetting\tMinorLoss\tComment";

        //private const string TITLE_TAG       = "[TITLE]";
        //private const string JUNCTIONS_TAG   = "[JUNCTIONS]";
        //private const string TANKS_TAG       = "[TANKS]";
        //private const string RESERVOIRS_TAG  = "[RESERVOIRS]";
        //private const string PIPES_TAG       = "[PIPES]";
        //private const string PUMPS_TAG       = "[PUMPS]";
        //private const string VALVES_TAG      = "[VALVES]";
        //private const string DEMANDS_TAG     = "[DEMANDS]";
        //private const string EMITTERS_TAG    = "[EMITTERS]";
        //private const string STATUS_TAG      = "[STATUS]";
        //private const string PATTERNS_TAG    = "[PATTERNS]";
        //private const string CURVES_TAG      = "[CURVES]";
        //private const string CONTROLS_TAG    = "[CONTROLS]";
        //private const string QUALITY_TAG     = "[QUALITY]";
        //private const string SOURCE_TAG      = "[SOURCE]";
        //private const string MIXING_TAG      = "[MIXING]";
        //private const string REACTIONS_TAG   = "[REACTIONS]";
        //private const string ENERGY_TAG      = "[ENERGY]";
        //private const string TIMES_TAG       = "[TIMES]";
        //private const string OPTIONS_TAG     = "[OPTIONS]";
        //private const string REPORT_TAG      = "[REPORT]";
        //private const string COORDINATES_TAG = "[COORDINATES]";
        //private const string RULES_TAG       = "[RULES]";
        //private const string VERTICES_TAG    = "[VERTICES]";
        //private const string LABELS_TAG      = "[LABELS]";


        private const string VERTICES_SUBTITLE = "Link\tX-Coord\tY-Coord";

        private XSSFWorkbook workbook;

        private ExcelWriter writer;

        private void ComposeControls(Network net) {
            FieldsMap fmap = net.FieldsMap;

            this.writer.Write(SectType.CONTROLS.ParseStr(), NEWLINE);
            this.writer.WriteHeader("Code");

            foreach (Control control in net.Controls) {
                // Check that controlled link exists
                if (control.Link == null) continue;

                // Get text of control's link status/setting
                if (control.Setting.IsMissing())
                    this.writer.Write("LINK", control.Link.Id, control.Status.ParseStr());
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
                    this.writer.Write("LINK", control.Link.Id, kc);
                }


                switch (control.Type) {
                // Print level control
                case ControlType.LOWLEVEL:
                case ControlType.HILEVEL:
                    double kc = control.Grade - control.Node.Elevation;
                    if (control.Node is Tank) kc = fmap.RevertUnit(FieldType.HEAD, kc);
                    else
                        kc = fmap.RevertUnit(FieldType.PRESSURE, kc);
                    this.writer.Write("IF", "NODE", control.Node.Id, control.Type.ParseStr(), kc);
                    break;

                // Print timer control
                case ControlType.TIMER:
                    this.writer.Write("AT", ControlType.TIMER.ParseStr(), control.Time / 3600.0f, "HOURS");
                    break;

                // Print time-of-day control
                case ControlType.TIMEOFDAY:
                    this.writer.Write("AT", ControlType.TIMEOFDAY.ParseStr(), control.Time.GetClockTime());
                    break;
                }
                this.writer.NewLine();
            }
            this.writer.NewLine();
        }

        private void ComposeCoordinates(Network net) {
            this.writer.Write(SectType.COORDINATES.ParseStr(), NEWLINE);
            this.writer.WriteHeader(COORDINATES_SUBTITLE);

            foreach (Node node  in  net.Nodes) {
                if (!node.Position.IsInvalid) {
                    this.writer.Write(node.Id, node.Position.X, node.Position.Y, NEWLINE);
                }
            }
            this.writer.NewLine();
        }


        private void ComposeCurves(Network net) {

            List<Curve> curves = new List<Curve>(net.Curves);

            this.writer.Write(SectType.CURVES.ParseStr(), NEWLINE);
            this.writer.WriteHeader(CURVE_SUBTITLE);

            foreach (Curve c  in  curves) {
                foreach (var pt in c.Points) {
                    this.writer.Write(c.Id, pt.X, pt.Y);
                    this.writer.NewLine();
                }
            }

            this.writer.NewLine();
        }

        private void ComposeDemands(Network net) {
            FieldsMap fMap = net.FieldsMap;

            this.writer.Write(SectType.DEMANDS.ParseStr(), NEWLINE);
            this.writer.WriteHeader(DEMANDS_SUBTITLE);

            double ucf = fMap.GetUnits(FieldType.DEMAND);

            foreach (Node node  in  net.Junctions) {

                if (node.Demand.Count > 1)
                    for (int i = 1; i < node.Demand.Count; i++) {
                        Demand demand = node.Demand[i];
                        this.writer.Write(node.Id, ucf * demand.Base);
                        if (demand.Pattern != null && !string.IsNullOrEmpty(demand.Pattern.Id))
                            this.writer.Write(demand.Pattern.Id);
                        this.writer.NewLine();
                    }
            }

            this.writer.NewLine();
        }

        private void ComposeEmitters(Network net) {
            this.writer.Write(SectType.EMITTERS.ParseStr(), NEWLINE);
            this.writer.WriteHeader(EMITTERS_SUBTITLE);

            double uflow = net.FieldsMap.GetUnits(FieldType.FLOW);
            double upressure = net.FieldsMap.GetUnits(FieldType.PRESSURE);
            double qexp = net.QExp;

            foreach (Node node  in  net.Junctions) {
                if (node.Ke == 0.0) continue;
                double ke = uflow / Math.Pow(upressure * node.Ke, (1.0 / qexp));
                this.writer.Write(node.Id, ke);
                this.writer.NewLine();
            }

            this.writer.NewLine();
        }

        private void ComposeEnergy(Network net) {
            
            this.writer.Write(SectType.ENERGY.ParseStr(), NEWLINE);

            if (net.ECost != 0.0)
                this.writer.Write("GLOBAL", "PRICE", net.ECost, NEWLINE);

            if (!string.IsNullOrEmpty(net.EPatId))
                this.writer.Write("GLOBAL", "PATTERN", net.EPatId, NEWLINE);

            this.writer.Write("GLOBAL", "EFFIC", net.EPump, NEWLINE);
            this.writer.Write("DEMAND", "CHARGE", net.DCost, NEWLINE);

            foreach (Pump p  in  net.Pumps) {
                if (p.ECost > 0.0)
                    this.writer.Write("PUMP", p.Id, "PRICE", p.ECost, NEWLINE);
                if (p.EPat != null)
                    this.writer.Write("PUMP", p.Id, "PATTERN", p.EPat.Id, NEWLINE);
                if (p.ECurve != null)
                    this.writer.Write("PUMP", p.Id, "EFFIC", p.Id, p.ECurve.Id, NEWLINE);
            }
            this.writer.NewLine();

        }

        public void ComposeHeader(Network net) {
            if (net.TitleText.Count == 0)
                return;

            this.writer.Write(SectType.TITLE.ParseStr(), NEWLINE);
            this.writer.WriteHeader(TITLE_SUBTITLE);

            foreach (string str  in  net.TitleText) {
                this.writer.Write(str);
                this.writer.NewLine();
            }

            this.writer.NewLine();
        }

        private void ComposeJunctions(Network net) {
            FieldsMap fMap = net.FieldsMap;
           
            this.writer.Write(SectType.JUNCTIONS.ParseStr(), NEWLINE);
            this.writer.WriteHeader(JUNCS_SUBTITLE);

            foreach (Node node  in  net.Junctions) {
                this.writer.Write(node.Id, fMap.RevertUnit(FieldType.ELEV, node.Elevation));

                if (node.Demand.Count > 0) {
                    Demand d = node.Demand[0];
                    this.writer.Write(fMap.RevertUnit(FieldType.DEMAND, d.Base));

                    if (!string.IsNullOrEmpty(d.Pattern.Id)
                        && !net.DefPatId.Equals(d.Pattern.Id, StringComparison.OrdinalIgnoreCase))
                        this.writer.Write(d.Pattern.Id);
                }

                if (!string.IsNullOrEmpty(node.Comment))
                    this.writer.Write(";" + node.Comment);

                this.writer.NewLine();
            }


            this.writer.NewLine();
        }

        private void ComposeLabels(Network net) {
            this.writer.Write(SectType.LABELS.ParseStr(), NEWLINE);
            this.writer.WriteHeader(LABELS_SUBTITLE);

            foreach (Label label  in  net.Labels) {
                this.writer.Write(label.Position.X, label.Position.Y, label.Text, NEWLINE);
            }
            this.writer.NewLine();
        }

        private void ComposeMixing(Network net) {
            this.writer.Write(SectType.MIXING.ParseStr(), NEWLINE);
            this.writer.WriteHeader(MIXING_SUBTITLE);

            foreach (Tank tank  in  net.Tanks) {
                if (tank.IsReservoir) continue;
                this.writer.Write(tank.Id, tank.MixModel.ParseStr(), (tank.V1Max / tank.Vmax));
                this.writer.NewLine();
            }
            this.writer.NewLine();
        }

        private void ComposeOptions(Network net) {
            this.writer.Write(SectType.OPTIONS.ParseStr(), NEWLINE);

            FieldsMap fMap = net.FieldsMap;

            this.writer.Write("UNITS", net.FlowFlag.ParseStr(), NEWLINE);
            this.writer.Write("PRESSURE", net.PressFlag.ParseStr(), NEWLINE);
            this.writer.Write("HEADLOSS", net.FormFlag.ParseStr(), NEWLINE);

            if (!string.IsNullOrEmpty(net.DefPatId))
                this.writer.Write("PATTERN", net.DefPatId, NEWLINE);

            if (net.HydFlag == HydType.USE)
                this.writer.Write("HYDRAULICS USE", net.HydFname, NEWLINE);

            if (net.HydFlag == HydType.SAVE)
                this.writer.Write("HYDRAULICS SAVE", net.HydFname, NEWLINE);

            if (net.ExtraIter == -1)
                this.writer.Write("UNBALANCED", "STOP", NEWLINE);

            if (net.ExtraIter >= 0)
                this.writer.Write("UNBALANCED", "CONTINUE", net.ExtraIter, NEWLINE);

            if (net.QualFlag == QualType.CHEM)
                this.writer.Write("QUALITY", net.ChemName, net.ChemUnits, NEWLINE);

            if (net.QualFlag == QualType.TRACE)
                this.writer.Write("QUALITY", "TRACE", net.TraceNode, NEWLINE);

            if (net.QualFlag == QualType.AGE)
                this.writer.Write("QUALITY", "AGE", NEWLINE);

            if (net.QualFlag == QualType.NONE)
                this.writer.Write("QUALITY", "NONE", NEWLINE);

            this.writer.Write("DEMAND", "MULTIPLIER", net.DMult, NEWLINE);

            this.writer.Write("EMITTER", "EXPONENT", 1.0 / net.QExp, NEWLINE);

            this.writer.Write("VISCOSITY", net.Viscos / Constants.VISCOS, NEWLINE);

            this.writer.Write("DIFFUSIVITY", net.Diffus / Constants.DIFFUS, NEWLINE);

            this.writer.Write("SPECIFIC", "GRAVITY", net.SpGrav, NEWLINE);

            this.writer.Write("TRIALS", net.MaxIter, NEWLINE);

            this.writer.Write("ACCURACY", net.HAcc, NEWLINE);

            this.writer.Write("TOLERANCE", fMap.RevertUnit(FieldType.QUALITY, net.Ctol), NEWLINE);

            this.writer.Write("CHECKFREQ", net.CheckFreq, NEWLINE);

            this.writer.Write("MAXCHECK", net.MaxCheck, NEWLINE);

            this.writer.Write("DAMPLIMIT", net.DampLimit, NEWLINE);

            this.writer.NewLine();
        }

        private void ComposePatterns(Network net) {

            List<Pattern> pats = new List<Pattern>(net.Patterns);

            this.writer.Write(SectType.PATTERNS.ParseStr(), NEWLINE);
            this.writer.WriteHeader(PATTERNS_SUBTITLE);

            for (int i = 1; i < pats.Count; i++) {
                Pattern pat = pats[i];
                List<double> f = pat.FactorsList;
                for (int j = 0; j < pats[i].Length; j++) {
                    if (j % 6 == 0)
                        this.writer.Write(pat.Id);
                    this.writer.Write(f[j]);

                    if (j % 6 == 5)
                        this.writer.NewLine();
                }
                this.writer.NewLine();
            }

            this.writer.NewLine();
        }

        private void ComposePipes(Network net) {
            FieldsMap fMap = net.FieldsMap;
            List<Link> pipes = new List<Link>();

            foreach (Link link  in  net.Links)
                if (link.Type <= LinkType.PIPE)
                    pipes.Add(link);

            this.writer.Write(SectType.PIPES.ParseStr(), NEWLINE);
            this.writer.WriteHeader(PIPES_SUBTITLE);

            foreach (Link link  in  pipes) {
                double d = link.Diameter;
                double kc = link.Roughness;
                if (net.FormFlag == FormType.DW)
                    kc = fMap.RevertUnit(FieldType.ELEV, kc * 1000.0);

                double km = link.Km * Math.Pow(d, 4.0) / 0.02517;

                this.writer.Write(
                        link.Id,
                        link.FirstNode.Id,
                        link.SecondNode.Id,
                        fMap.RevertUnit(FieldType.LENGTH, link.Lenght),
                        fMap.RevertUnit(FieldType.DIAM, d));

                // if (net.FormFlag == FormType.DW)
                this.writer.Write(kc, km);

                if (link.Type == LinkType.CV)
                    this.writer.Write("CV");
                else if (link.Status == StatType.CLOSED)
                    this.writer.Write("CLOSED");
                else if (link.Status == StatType.OPEN)
                    this.writer.Write("OPEN");

                if (!string.IsNullOrEmpty(link.Comment))
                    this.writer.Write(";" + link.Comment);

                this.writer.NewLine();
            }

            this.writer.NewLine();
        }

        private void ComposePumps(Network net) {
            FieldsMap fMap = net.FieldsMap;

            this.writer.Write(SectType.PUMPS.ParseStr(), NEWLINE);
            this.writer.WriteHeader(PUMPS_SUBTITLE);

            foreach (Pump pump in net.Pumps) {
                this.writer.Write(
                        pump.Id,
                        pump.FirstNode.Id,
                        pump.SecondNode.Id);


                // Pump has constant power
                if (pump.Ptype == PumpType.CONST_HP)
                    this.writer.Write("POWER", pump.Km);
                // Pump has a head curve
                else if (pump.HCurve != null)
                    this.writer.Write("HEAD", pump.HCurve.Id);
                // Old format used for pump curve
                else {
                    this.writer.Write(
                            fMap.RevertUnit(FieldType.HEAD, -pump.H0),
                            fMap.RevertUnit(
                                FieldType.HEAD,
                                -pump.H0 - pump.FlowCoefficient * Math.Pow(pump.Q0, pump.N)),
                            fMap.RevertUnit(FieldType.FLOW, pump.Q0),
                            0.0,
                            fMap.RevertUnit(
                                FieldType.FLOW,
                                pump.Qmax
                            ));
                    continue;
                }

                if (pump.UPat != null)
                    this.writer.Write("PATTERN", pump.UPat.Id);

                if (pump.Roughness != 1.0)
                    this.writer.Write("SPEED", pump.Roughness);

                if (!string.IsNullOrEmpty(pump.Comment))
                    this.writer.Write(";" + pump.Comment);

                this.writer.NewLine();
            }

            this.writer.NewLine();
        }

        private void ComposeQuality(Network net) {
            FieldsMap fmap = net.FieldsMap;

            this.writer.Write(SectType.QUALITY.ParseStr(), NEWLINE);
            this.writer.WriteHeader(QUALITY_SUBTITLE);

            foreach (Node node in net.Nodes) {
                if (node.C0 == 0.0) continue;

                this.writer.Write(node.Id, fmap.RevertUnit(FieldType.QUALITY, node.C0));
                this.writer.NewLine();
            }

            this.writer.NewLine();
        }

        public override void Composer(Network net, string fileName) {

            this.workbook = new XSSFWorkbook();
            this.writer = new ExcelWriter(this.workbook);

            try {


                this.writer.NewSpreadsheet("Junctions");
                this.ComposeJunctions(net);

                this.writer.NewSpreadsheet("Tanks");
                this.ComposeReservoirs(net);
                this.ComposeTanks(net);

                this.writer.NewSpreadsheet("Pipes");
                this.ComposePipes(net);

                this.writer.NewSpreadsheet("Pumps");
                this.ComposePumps(net);
                this.ComposeEnergy(net);

                this.writer.NewSpreadsheet("Valves");
                this.ComposeValves(net);

                this.writer.NewSpreadsheet("Demands");
                this.ComposeDemands(net);

                this.writer.NewSpreadsheet("Patterns");
                this.ComposePatterns(net);
                this.writer.NewSpreadsheet("Curves");
                this.ComposeCurves(net);

                this.writer.NewSpreadsheet("Script");
                this.ComposeControls(net);
                this.ComposeRules(net);

                this.writer.NewSpreadsheet("Quality");
                this.ComposeQuality(net);
                this.ComposeSource(net);
                this.ComposeMixing(net);
                this.ComposeReaction(net);


                this.writer.NewSpreadsheet("Config");
                this.ComposeHeader(net);
                this.ComposeTimes(net);
                this.ComposeOptions(net);
                this.ComposeReport(net);
                this.ComposeEmitters(net);
                this.ComposeStatus(net);

                this.writer.NewSpreadsheet("GIS");
                this.ComposeLabels(net);
                this.ComposeCoordinates(net);
                this.ComposeVertices(net);



                this.workbook.Write(File.OpenWrite(fileName));
            }
            catch (IOException) {}
        }


        private void ComposeReaction(Network net) {
            
            this.writer.Write(SectType.REACTIONS.ParseStr(), NEWLINE);
            this.writer.WriteHeader(REACTIONS_SUBTITLE);

            this.writer.Write("ORDER", "BULK", net.BulkOrder, NEWLINE);
            this.writer.Write("ORDER", "WALL", net.WallOrder, NEWLINE);
            this.writer.Write("ORDER", "TANK", net.TankOrder, NEWLINE);
            this.writer.Write("GLOBAL", "BULK", net.KBulk * Constants.SECperDAY, NEWLINE);
            this.writer.Write("GLOBAL", "WALL", net.KWall * Constants.SECperDAY, NEWLINE);
            // if (net.CLimit > 0.0)
            this.writer.Write("LIMITING", "POTENTIAL", net.CLimit, NEWLINE);

            // if (!net.RFactor.IsMissing() && net.RFactor != 0.0)
            this.writer.Write("ROUGHNESS", "CORRELATION", net.RFactor, NEWLINE);


            foreach (Link link  in  net.Links) {
                if (link.Type > LinkType.PIPE)
                    continue;

                if (link.Kb != net.KBulk)
                    this.writer.Write("BULK", link.Id, link.Kb * Constants.SECperDAY, NEWLINE);
                if (link.Kw != net.KWall)
                    this.writer.Write("WALL", link.Id, link.Kw * Constants.SECperDAY, NEWLINE);
            }

            foreach (Tank tank  in  net.Tanks) {
                if (tank.IsReservoir) continue;
                if (tank.Kb != net.KBulk)
                    this.writer.Write("TANK", tank.Id, tank.Kb * Constants.SECperDAY, NEWLINE);
            }
            this.writer.NewLine();
        }

        private void ComposeReport(Network net) {
            this.writer.Write(SectType.REPORT.ParseStr(), NEWLINE);

            FieldsMap fMap = net.FieldsMap;
            this.writer.Write("PAGESIZE", net.PageSize, NEWLINE);
            this.writer.Write("STATUS", net.Stat_Flag.ToString(), NEWLINE);
            this.writer.Write("SUMMARY", net.SummaryFlag ? Keywords.w_YES : Keywords.w_NO, NEWLINE);
            this.writer.Write("ENERGY", net.EnergyFlag ? Keywords.w_YES : Keywords.w_NO, NEWLINE);

            switch (net.NodeFlag) {
            case ReportFlag.FALSE:
                this.writer.Write("NODES", "NONE", NEWLINE);
                break;
            case ReportFlag.TRUE:
                this.writer.Write("NODES", "ALL", NEWLINE);
                break;
            case ReportFlag.SOME: {
                int j = 0;
                foreach (Node node  in  net.Nodes) {
                    if (node.RptFlag) {
                        if (j % 5 == 0) this.writer.Write("NODES", NEWLINE);
                        this.writer.Write(node.Id);
                        j++;
                    }
                }
                break;
            }
            }

            switch (net.LinkFlag) {
            case ReportFlag.FALSE:
                this.writer.Write("LINKS", "NONE", NEWLINE);
                break;
            case ReportFlag.TRUE:
                this.writer.Write("LINKS", "ALL", NEWLINE);
                break;
            case ReportFlag.SOME: {
                int j = 0;
                foreach (Link link  in  net.Links) {
                    if (link.RptFlag) {
                        if (j % 5 == 0) this.writer.Write("LINKS", NEWLINE);
                        this.writer.Write(link.Id);
                        j++;
                    }
                }
                break;
            }
            }

            for (FieldType i = 0; i < FieldType.FRICTION; i++) {
                Field f = fMap.GetField(i);
                if (f.Enabled) {
                    this.writer.Write(f.Name, "PRECISION", f.Precision, NEWLINE);
                    if (f.GetRptLim(RangeType.LOW) < Constants.BIG)
                        this.writer.Write(f.Name, "BELOW", f.GetRptLim(RangeType.LOW), NEWLINE);
                    if (f.GetRptLim(RangeType.HI) > -Constants.BIG)
                        this.writer.Write(f.Name, "ABOVE", f.GetRptLim(RangeType.HI), NEWLINE);
                }
                else
                    this.writer.Write(f.Name, "NO", NEWLINE);
            }

            this.writer.NewLine();
        }

        private void ComposeReservoirs(Network net) {
            FieldsMap fMap = net.FieldsMap;

            if (!net.Tanks.Any())
                return;

            List<Tank> reservoirs = new List<Tank>();
            foreach (Tank tank in net.Tanks)
                if (tank.IsReservoir)
                    reservoirs.Add(tank);

            this.writer.Write(SectType.RESERVOIRS.ParseStr(), NEWLINE);
            this.writer.WriteHeader(RESERVOIRS_SUBTITLE);

            foreach (Tank r  in  reservoirs) {
                this.writer.Write(r.Id, fMap.RevertUnit(FieldType.ELEV, r.Elevation));

                if (r.Pattern != null)
                    this.writer.Write(r.Pattern.Id);

                if (!string.IsNullOrEmpty(r.Comment))
                    this.writer.Write(";" + r.Comment);

                this.writer.NewLine();
            }

            this.writer.NewLine();
        }

        private void ComposeRules(Network net) {
            this.writer.Write(SectType.RULES.ParseStr(), NEWLINE);
            foreach (Rule r  in  net.Rules) {
                this.writer.Write("RULE ", r.Label, NEWLINE);

                foreach (string s  in  r.Code)
                    this.writer.Write(s, NEWLINE);

                this.writer.NewLine();
            }
            this.writer.NewLine();
        }

        private void ComposeSource(Network net) {
            this.writer.Write(SectType.SOURCES.ParseStr(), NEWLINE);
            this.writer.WriteHeader(SOURCE_SUBTITLE);

            foreach (Node node in net.Nodes) {
                Source source = node.Source;
                if (source == null)
                    continue;
                this.writer.Write(
                        node.Id,
                        source.Type.ParseStr(),
                        source.C0);
                if (source.Pattern != null)
                    this.writer.Write(source.Pattern.Id);
                this.writer.NewLine();
            }
            this.writer.NewLine();
        }

        private void ComposeStatus(Network net) {

            this.writer.Write(SectType.STATUS.ParseStr(), NEWLINE);
            this.writer.WriteHeader(STATUS_SUBTITLE);

            foreach (Link link  in  net.Links) {
                if (link.Type <= LinkType.PUMP) {
                    if (link.Status == StatType.CLOSED)
                        this.writer.Write(link.Id, StatType.CLOSED.ParseStr());
                    else if (link.Type == LinkType.PUMP) { // Write pump speed here for pumps with old-style pump curve input
                        Pump pump = (Pump)link;
                        if (pump.HCurve == null &&
                            pump.Ptype != PumpType.CONST_HP &&
                            pump.Roughness != 1.0)
                            this.writer.Write(link.Id, link.Roughness);
                    }
                }
                // Write fixed-status PRVs & PSVs (setting = MISSING)
                else if (link.Roughness.IsMissing()) {
                    if (link.Status == StatType.OPEN)
                        this.writer.Write(link.Id, StatType.OPEN.ParseStr());

                    if (link.Status == StatType.CLOSED)
                        this.writer.Write(link.Id, StatType.CLOSED.ParseStr());

                }

                this.writer.NewLine();
            }

            this.writer.NewLine();
        }

        private void ComposeTanks(Network net) {
            FieldsMap fMap = net.FieldsMap;

            this.writer.Write(SectType.TANKS.ParseStr(), NEWLINE);
            this.writer.WriteHeader(TANK_SUBTITLE);

            foreach (Tank tank in net.Tanks) {
                if (tank.IsReservoir) continue;

                double vmin = tank.Vmin;
                if (Math.Abs(vmin / tank.Area - (tank.Hmin - tank.Elevation)) < 0.1)
                    vmin = 0;

                this.writer.Write(
                        tank.Id,
                        fMap.RevertUnit(FieldType.ELEV, tank.Elevation),
                        fMap.RevertUnit(FieldType.ELEV, tank.H0 - tank.Elevation),
                        fMap.RevertUnit(FieldType.ELEV, tank.Hmin - tank.Elevation),
                        fMap.RevertUnit(FieldType.ELEV, tank.Hmax - tank.Elevation),
                        fMap.RevertUnit(FieldType.ELEV, 2 * Math.Sqrt(tank.Area / Math.PI)),
                        fMap.RevertUnit(FieldType.VOLUME, vmin));

                if (tank.Vcurve != null)
                    this.writer.Write(tank.Vcurve.Id);

                if (!string.IsNullOrEmpty(tank.Comment))
                    this.writer.Write(";" + tank.Comment);

                this.writer.NewLine();
            }

            this.writer.NewLine();
        }


        private void ComposeTimes(Network net) {
            this.writer.Write(SectType.TIMES.ParseStr(), NEWLINE);
            
            this.writer.Write("DURATION", TimeSpan.FromSeconds(net.Duration), NEWLINE);
            this.writer.Write("HYDRAULIC", "TIMESTEP", TimeSpan.FromSeconds(net.HStep), NEWLINE);
            this.writer.Write("QUALITY", "TIMESTEP", TimeSpan.FromSeconds(net.QStep), NEWLINE);
            this.writer.Write("REPORT", "TIMESTEP", TimeSpan.FromSeconds(net.RStep), NEWLINE);
            this.writer.Write("REPORT", "START", TimeSpan.FromSeconds(net.RStart), NEWLINE);
            this.writer.Write("PATTERN", "TIMESTEP", TimeSpan.FromSeconds(net.PStep), NEWLINE);
            this.writer.Write("PATTERN", "START", TimeSpan.FromSeconds(net.PStart), NEWLINE);
            this.writer.Write("RULE", "TIMESTEP", TimeSpan.FromSeconds(net.RuleStep), NEWLINE);
            this.writer.Write("START", "CLOCKTIME", TimeSpan.FromSeconds(net.TStart), NEWLINE);
            this.writer.Write("STATISTIC", net.TStatFlag.ParseStr(), NEWLINE);
            this.writer.NewLine();
        }

        private void ComposeValves(Network net) {
            FieldsMap fMap = net.FieldsMap;

            this.writer.Write(SectType.VALVES.ParseStr(), NEWLINE);
            this.writer.WriteHeader(VALVES_SUBTITLE);

            foreach (Valve valve in net.Valves) {
                double d = valve.Diameter;
                double kc = valve.Roughness;
                if (kc.IsMissing())
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

                this.writer.Write(
                        valve.Id,
                        valve.FirstNode.Id,
                        valve.SecondNode.Id,
                        fMap.RevertUnit(FieldType.DIAM, d),
                        valve.Type.ParseStr());

                if (valve.Type == LinkType.GPV && valve.Curve != null)
                    this.writer.Write(valve.Curve.Id, km);
                else
                    this.writer.Write(kc, km);

                if (!string.IsNullOrEmpty(valve.Comment))
                    this.writer.Write(";" + valve.Comment);

                this.writer.NewLine();
            }
            this.writer.NewLine();
        }

        private void ComposeVertices(Network net) {
            this.writer.Write(SectType.VERTICES.ParseStr(), NEWLINE);
            this.writer.WriteHeader(VERTICES_SUBTITLE);

            foreach (Link link  in  net.Links) {
                foreach (EnPoint p  in  link.Vertices) {
                    this.writer.Write(link.Id, p.X, p.Y, NEWLINE);
                }
            }

            this.writer.NewLine();
        }


    }

}