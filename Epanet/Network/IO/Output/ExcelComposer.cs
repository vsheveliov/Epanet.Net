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
using Epanet.Network.Structures;
using Epanet.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Epanet.Network.IO.Output {

///<summary>EXCEL XLSX composer class.</summary>
public class ExcelComposer : OutputComposer {
    private class ExcelWriter {
        private readonly XSSFWorkbook _workbook;
        private IRow _activeRow;
        private ISheet _activeSheet;
        private int _cellCount;
        private int _rowCount;

        private readonly ICellStyle _timeStyle;
        private readonly ICellStyle _topBold;

        public ExcelWriter(XSSFWorkbook workbook) {
            this._workbook = workbook;
            this._topBold = workbook.CreateCellStyle();
            IFont newFont = workbook.CreateFont();
            newFont.Boldweight = (short)FontBoldWeight.Bold;
            this._topBold.SetFont(newFont);
            this._timeStyle = workbook.CreateCellStyle();
            this._timeStyle.DataFormat = 46;
        }

        public void NewLine() {
            this._activeRow = this._activeSheet.CreateRow(this._rowCount++);
            this._cellCount = 0;
        }

        public void NewSpreadsheet(string name) {
            this._rowCount = 0;
            this._cellCount = 0;
            this._activeSheet = this._workbook.CreateSheet(name);
            this._activeRow = null;
        }

        public void Write(params object[] args) {
            if (this._activeRow == null) {
                this._activeRow = this._activeSheet.CreateRow(this._rowCount++);
                this._cellCount = 0;
            }

            foreach (object obj  in  args) {
                if (obj is string && obj.Equals(NEWLINE)) {
                    this._activeRow = this._activeSheet.CreateRow(this._rowCount++);
                    this._cellCount = 0;
                    continue;
                }

                var c = this._activeRow.CreateCell(this._cellCount++);

                if(obj==null)
                    c.SetCellType(CellType.Blank);
                else if(obj is DateTime){
                    DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc); //Getting UTC DATE since epoch
                    TimeSpan ts = (DateTime)obj - epochStart; //get the current timestamp between now and january 1970
                    c.SetCellValue(ts.TotalSeconds / 86400.0d);
                    c.CellStyle = this._timeStyle;
                }
                else if (obj is bool)
                    c.SetCellValue(((bool) obj));
                else if (obj.IsNumber())
                    c.SetCellValue(Convert.ToDouble(obj));
                else
                    c.SetCellValue( obj.ToString());
            }
        }

        public void WriteHeader(string str) {
            string[] sections = str.Split('\t');
            //for (int i = 0; i < sections.length; i++)
            //    sections[i] = ";" + sections[i];
            //if(sections.length>0)
            //    sections[0] = ";" + sections[0];

            //write(sections);
            if (this._activeRow == null) {
                this._activeRow = this._activeSheet.CreateRow(this._rowCount++);
                this._cellCount = 0;
            }

            foreach (string obj  in  sections) {
                ICell c = this._activeRow.CreateCell(this._cellCount++);
                c.CellStyle = this._topBold;
                c.SetCellValue( obj);
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
    private const string PIPES_SUBTITLE = "ID\tNode1\tNode2\tLength\tDiameter\tRoughness\tMinorLoss\tStatus\tComment";
    private const string PUMPS_SUBTITLE = "ID\tNode1\tNode2\tParameters\tValue\tComment";
    private const string QUALITY_SUBTITLE = "Node\tInitQual";
    private const string REACTIONS_SUBTITLE = "Type\tPipe/Tank";
    private const string RESERVOIRS_SUBTITLE = "ID\tHead\tPattern\tComment";
    private const string SOURCE_SUBTITLE = "Node\tType\tQuality\tPattern";
    private const string STATUS_SUBTITLE = "ID\tStatus/Setting";
    private const string TANK_SUBTITLE = "ID\tElevation\tInitLevel\tMinLevel\tMaxLevel\tDiameter\tMinVol\tVolCurve\tComment";
    private const string TITLE_SUBTITLE  = "Text";
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

    private XSSFWorkbook _workbook;

    private ExcelWriter _writer;

    private void ComposeControls(Network net) {
        FieldsMap fmap = net.FieldsMap;

        this._writer.Write(Network.SectType.CONTROLS.parseStr(),NEWLINE);
        this._writer.WriteHeader("Code");

        foreach(Control control in net.Controls) {
            // Check that controlled link exists
            if (control.Link == null) continue;

            // Get text of control's link status/setting
            if (control.Setting == Constants.MISSING)
                this._writer.Write("LINK", control.Link.Id, control.Status.ParseStr());
            else {
                double kc = control.Setting;
                switch (control.Link.Type) {
                    case Link.LinkType.PRV:
                    case Link.LinkType.PSV:
                    case Link.LinkType.PBV:
                        kc = fmap.RevertUnit(FieldsMap.FieldType.PRESSURE, kc);
                        break;
                    case Link.LinkType.FCV:
                        kc = fmap.RevertUnit(FieldsMap.FieldType.FLOW, kc);
                        break;
                }
                this._writer.Write("LINK", control.Link.Id, kc);
            }


            switch (control.Type) {
                // Print level control
                case Control.ControlType.LOWLEVEL:
                case Control.ControlType.HILEVEL:
                    double kc = control.Grade - control.Node.Elevation;
                    if (control.Node is Tank) kc = fmap.RevertUnit(FieldsMap.FieldType.HEAD, kc);
                    else
                        kc = fmap.RevertUnit(FieldsMap.FieldType.PRESSURE, kc);
                    this._writer.Write("IF", "NODE", control.Node.Id, control.Type.ParseStr(), kc);
                    break;

                // Print timer control
                case Control.ControlType.TIMER:
                this._writer.Write("AT", Control.ControlType.TIMER.ParseStr(), control.Time / 3600.0f, "HOURS");
                    break;

                // Print time-of-day control
                case Control.ControlType.TIMEOFDAY:
                    this._writer.Write("AT", Control.ControlType.TIMEOFDAY.ParseStr(), control.Time.GetClockTime());
                    break;
            }
            this._writer.NewLine();
        }
        this._writer.NewLine();
    }

    private void ComposeCoordinates(Network net) {
        this._writer.Write(Network.SectType.COORDINATES.parseStr(),NEWLINE);
        this._writer.WriteHeader(COORDINATES_SUBTITLE);

        foreach (Node node  in  net.Nodes) {
            if (!node.Position.IsInvalid) {
                this._writer.Write(node.Id, node.Position.X, node.Position.Y, NEWLINE);
            }
        }
        this._writer.NewLine();
    }


    private void ComposeCurves(Network net) {

        List<Curve> curves = new List<Curve>(net.Curves);

        this._writer.Write(Network.SectType.CURVES.parseStr(),NEWLINE);
        this._writer.WriteHeader(CURVE_SUBTITLE);

        foreach (Curve c  in  curves) {
            for (int i = 0; i < c.Npts; i++) {
                this._writer.Write(c.Id, c.X[i], c.Y[i]);
                this._writer.NewLine();
            }
        }

        this._writer.NewLine();
    }

    private void ComposeDemands(Network net) {
        FieldsMap fMap = net.FieldsMap;

        this._writer.Write(Network.SectType.DEMANDS.parseStr(),NEWLINE);
        this._writer.WriteHeader(DEMANDS_SUBTITLE);

        double ucf = fMap.GetUnits(FieldsMap.FieldType.DEMAND);

        foreach (Node node  in  net.Junctions) {

            if (node.Demand.Count > 1)
                for(int i = 1;i<node.Demand.Count;i++){
                    Demand demand = node.Demand[i];
                    this._writer.Write(node.Id, ucf * demand.Base);
                    if (demand.Pattern != null && !string.IsNullOrEmpty(demand.Pattern.Id))
                        this._writer.Write(demand.Pattern.Id);
                    this._writer.NewLine();
                }
        }

        this._writer.NewLine();
    }

    private void ComposeEmitters(Network net) {
        this._writer.Write(Network.SectType.EMITTERS.parseStr(),NEWLINE);
        this._writer.WriteHeader(EMITTERS_SUBTITLE);

        double uflow = net.FieldsMap.GetUnits(FieldsMap.FieldType.FLOW);
        double upressure = net.FieldsMap.GetUnits(FieldsMap.FieldType.PRESSURE);
        double qexp = net.PropertiesMap.Qexp;

        foreach (Node node  in  net.Junctions) {
            if (node.Ke == 0.0) continue;
            double ke = uflow / Math.Pow(upressure * node.Ke, (1.0 / qexp));
            this._writer.Write(node.Id, ke);
            this._writer.NewLine();
        }

        this._writer.NewLine();
    }

    private void ComposeEnergy(Network net) {
        PropertiesMap pMap = net.PropertiesMap;

        this._writer.Write(Network.SectType.ENERGY.parseStr(),NEWLINE);

        if (pMap.Ecost != 0.0)
            this._writer.Write("GLOBAL", "PRICE", pMap.Ecost, NEWLINE);

        if (!string.IsNullOrEmpty(pMap.EpatId))
            this._writer.Write("GLOBAL", "PATTERN", pMap.EpatId, NEWLINE);

        this._writer.Write("GLOBAL", "EFFIC", pMap.Epump, NEWLINE);
        this._writer.Write("DEMAND", "CHARGE", pMap.Dcost, NEWLINE);

        foreach (Pump p  in  net.Pumps) {
            if (p.Ecost > 0.0)
                this._writer.Write("PUMP", p.Id, "PRICE", p.Ecost, NEWLINE);
            if (p.Epat != null)
                this._writer.Write("PUMP", p.Id, "PATTERN", p.Epat.Id, NEWLINE);
            if (p.Ecurve != null)
                this._writer.Write("PUMP", p.Id, "EFFIC", p.Id, p.Ecurve.Id, NEWLINE);
        }
        this._writer.NewLine();

    }

    public void ComposeHeader(Network net) {
        if (net.TitleText.Count == 0)
            return;

        this._writer.Write(Network.SectType.TITLE.parseStr(),NEWLINE);
        this._writer.WriteHeader(TITLE_SUBTITLE);

        foreach (string str  in  net.TitleText) {
            this._writer.Write(str);
            this._writer.NewLine();
        }

        this._writer.NewLine();
    }

    private void ComposeJunctions(Network net) {
        FieldsMap fMap = net.FieldsMap;
        PropertiesMap pMap = net.PropertiesMap;

        this._writer.Write(Network.SectType.JUNCTIONS.parseStr(),NEWLINE);
        this._writer.WriteHeader(JUNCS_SUBTITLE);

        foreach (Node node  in  net.Junctions) {
            this._writer.Write(node.Id, fMap.RevertUnit(FieldsMap.FieldType.ELEV, node.Elevation));

            if( node.Demand.Count>0){
                Demand d = node.Demand[0];
                this._writer.Write(fMap.RevertUnit(FieldsMap.FieldType.DEMAND, d.Base));

                if (!string.IsNullOrEmpty(d.Pattern.Id) 
                    && !pMap.DefPatId.Equals(d.Pattern.Id, StringComparison.OrdinalIgnoreCase))
                    this._writer.Write(d.Pattern.Id);
            }

            if (!string.IsNullOrEmpty(node.Comment))
                this._writer.Write(";" + node.Comment);

            this._writer.NewLine();
        }


        this._writer.NewLine();
    }

    private void ComposeLabels(Network net) {
        this._writer.Write(Network.SectType.LABELS.parseStr(),NEWLINE);
        this._writer.WriteHeader(LABELS_SUBTITLE);

        foreach (Label label  in  net.Labels) {
            this._writer.Write(label.Position.X, label.Position.Y,label.Text, NEWLINE);
        }
        this._writer.NewLine();
    }

    private void ComposeMixing(Network net) {
        this._writer.Write(Network.SectType.MIXING.parseStr(),NEWLINE);
        this._writer.WriteHeader(MIXING_SUBTITLE);

        foreach (Tank tank  in  net.Tanks) {
            if (tank.IsReservoir) continue;
            this._writer.Write(tank.Id, tank.MixModel.ParseStr(), (tank.V1Max / tank.Vmax));
            this._writer.NewLine();
        }
        this._writer.NewLine();
    }

    private void ComposeOptions(Network net) {
        this._writer.Write(Network.SectType.OPTIONS.parseStr(),NEWLINE);

        PropertiesMap pMap = net.PropertiesMap;
        FieldsMap fMap = net.FieldsMap;

        this._writer.Write("UNITS", pMap.Flowflag.ParseStr(), NEWLINE);
        this._writer.Write("PRESSURE", pMap.Pressflag.ParseStr(), NEWLINE);
        this._writer.Write("HEADLOSS", pMap.Formflag.ParseStr(), NEWLINE);

        if (!string.IsNullOrEmpty(pMap.DefPatId))
            this._writer.Write("PATTERN", pMap.DefPatId, NEWLINE);

        if (pMap.Hydflag == PropertiesMap.Hydtype.USE)
            this._writer.Write("HYDRAULICS USE", pMap.HydFname, NEWLINE);

        if (pMap.Hydflag == PropertiesMap.Hydtype.SAVE)
            this._writer.Write("HYDRAULICS SAVE", pMap.HydFname, NEWLINE);

        if (pMap.ExtraIter == -1)
            this._writer.Write("UNBALANCED", "STOP", NEWLINE);

        if (pMap.ExtraIter >= 0)
            this._writer.Write("UNBALANCED", "CONTINUE", pMap.ExtraIter, NEWLINE);

        if (pMap.Qualflag == PropertiesMap.QualType.CHEM)
            this._writer.Write("QUALITY", pMap.ChemName, pMap.ChemUnits, NEWLINE);

        if (pMap.Qualflag == PropertiesMap.QualType.TRACE)
            this._writer.Write("QUALITY", "TRACE", pMap.TraceNode, NEWLINE);

        if (pMap.Qualflag == PropertiesMap.QualType.AGE)
            this._writer.Write("QUALITY", "AGE", NEWLINE);

        if (pMap.Qualflag == PropertiesMap.QualType.NONE)
            this._writer.Write("QUALITY", "NONE", NEWLINE);

        this._writer.Write("DEMAND", "MULTIPLIER", pMap.Dmult, NEWLINE);

        this._writer.Write("EMITTER", "EXPONENT", 1.0 / pMap.Qexp, NEWLINE);

        this._writer.Write("VISCOSITY", pMap.Viscos / Constants.VISCOS, NEWLINE);

        this._writer.Write("DIFFUSIVITY", pMap.Diffus / Constants.DIFFUS, NEWLINE);

        this._writer.Write("SPECIFIC", "GRAVITY", pMap.SpGrav, NEWLINE);

        this._writer.Write("TRIALS", pMap.MaxIter, NEWLINE);

        this._writer.Write("ACCURACY", pMap.Hacc, NEWLINE);

        this._writer.Write("TOLERANCE", fMap.RevertUnit(FieldsMap.FieldType.QUALITY, pMap.Ctol), NEWLINE);

        this._writer.Write("CHECKFREQ", pMap.CheckFreq, NEWLINE);

        this._writer.Write("MAXCHECK", pMap.MaxCheck, NEWLINE);

        this._writer.Write("DAMPLIMIT", pMap.DampLimit, NEWLINE);

        this._writer.NewLine();
    }

    private void ComposePatterns(Network net) {

        List<Pattern> pats = new List<Pattern>(net.Patterns);

        this._writer.Write(Network.SectType.PATTERNS.parseStr(),NEWLINE);
        this._writer.WriteHeader(PATTERNS_SUBTITLE);

        for (int i = 1; i < pats.Count; i++) {
            Pattern pat = pats[i];
            List<double> f = pat.FactorsList;
            for (int j = 0; j < pats[i].Length; j++) {
                if (j % 6 == 0)
                    this._writer.Write(pat.Id);
                this._writer.Write(f[j]);

                if (j % 6 == 5)
                    this._writer.NewLine();
            }
            this._writer.NewLine();
        }

        this._writer.NewLine();
    }

    private void ComposePipes(Network net) {
        FieldsMap fMap = net.FieldsMap;
        PropertiesMap pMap = net.PropertiesMap;

        List<Link> pipes = new List<Link>();
        foreach (Link link  in  net.Links)
            if (link.Type <= Link.LinkType.PIPE)
                pipes.Add(link);

        this._writer.Write(Network.SectType.PIPES.parseStr(),NEWLINE);
        this._writer.WriteHeader(PIPES_SUBTITLE);

        foreach (Link link  in  pipes) {
            double d = link.Diameter;
            double kc = link.Roughness;
            if (pMap.Formflag == PropertiesMap.FormType.DW)
                kc = fMap.RevertUnit(FieldsMap.FieldType.ELEV, kc * 1000.0);

            double km = link.Km * Math.Pow(d, 4.0) / 0.02517;

            this._writer.Write(link.Id,
                    link.FirstNode.Id,
                    link.SecondNode.Id,
                    fMap.RevertUnit(FieldsMap.FieldType.LENGTH, link.Lenght),
                    fMap.RevertUnit(FieldsMap.FieldType.DIAM, d));

            //if (pMap.getFormflag() == FormType.DW)
            this._writer.Write(kc, km);

            if (link.Type == Link.LinkType.CV)
                this._writer.Write("CV");
            else if (link.Status == Link.StatType.CLOSED)
                this._writer.Write("CLOSED");
            else if (link.Status == Link.StatType.OPEN)
                this._writer.Write("OPEN");

            if (!string.IsNullOrEmpty(link.Comment))
                this._writer.Write(";" + link.Comment);

            this._writer.NewLine();
        }

        this._writer.NewLine();
    }

    private void ComposePumps(Network net) {
        FieldsMap fMap = net.FieldsMap;
        
        this._writer.Write(Network.SectType.PUMPS.parseStr(),NEWLINE);
        this._writer.WriteHeader(PUMPS_SUBTITLE);

        foreach(Pump pump in net.Pumps) {
            this._writer.Write(pump.Id,
                    pump.FirstNode.Id, pump.SecondNode.Id);


            // Pump has constant power
            if (pump.Ptype == Pump.PumpType.CONST_HP)
                this._writer.Write("POWER", pump.Km);
                // Pump has a head curve
            else if (pump.Hcurve != null)
                this._writer.Write("HEAD", pump.Hcurve.Id);
                // Old format used for pump curve
            else {
                this._writer.Write(
                        fMap.RevertUnit(FieldsMap.FieldType.HEAD, -pump.H0),
                        fMap.RevertUnit(FieldsMap.FieldType.HEAD, -pump.H0 - pump.FlowCoefficient * Math.Pow(pump.Q0, pump.N)),
                        fMap.RevertUnit(FieldsMap.FieldType.FLOW, pump.Q0), 0.0,
                        fMap.RevertUnit(FieldsMap.FieldType.FLOW, pump.Qmax
                        ));
                continue;
            }

            if (pump.Upat != null)
                this._writer.Write("PATTERN", pump.Upat.Id);

            if (pump.Roughness != 1.0)
                this._writer.Write("SPEED", pump.Roughness);

            if (!string.IsNullOrEmpty(pump.Comment))
                this._writer.Write(";" + pump.Comment);

            this._writer.NewLine();
        }

        this._writer.NewLine();
    }

    private void ComposeQuality(Network net) {
        FieldsMap fmap = net.FieldsMap;

        this._writer.Write(Network.SectType.QUALITY.parseStr(),NEWLINE);
        this._writer.WriteHeader(QUALITY_SUBTITLE);

        foreach(Node node in net.Nodes) {
            if (node.C0.Length == 1) {
                if (node.C0[0] == 0.0) continue;
                this._writer.Write(node.Id, fmap.RevertUnit(FieldsMap.FieldType.QUALITY, node.C0[0]));
            }
            this._writer.NewLine();
        }
        this._writer.NewLine();
    }

    public override void Composer(Network net, string fileName) {

        this._workbook = new XSSFWorkbook();
        this._writer = new ExcelWriter(this._workbook);

        try {


            this._writer.NewSpreadsheet("Junctions");
            this.ComposeJunctions(net);

            this._writer.NewSpreadsheet("Tanks");
            this.ComposeReservoirs(net);
            this.ComposeTanks(net);

            this._writer.NewSpreadsheet("Pipes");
            this.ComposePipes(net);

            this._writer.NewSpreadsheet("Pumps");
            this.ComposePumps(net);
            this.ComposeEnergy(net);

            this._writer.NewSpreadsheet("Valves");
            this.ComposeValves(net);

            this._writer.NewSpreadsheet("Demands");
            this.ComposeDemands(net);

            this._writer.NewSpreadsheet("Patterns");
            this.ComposePatterns(net);
            this._writer.NewSpreadsheet("Curves");
            this.ComposeCurves(net);

            this._writer.NewSpreadsheet("Script");
            this.ComposeControls(net);
            this.ComposeRules(net);

            this._writer.NewSpreadsheet("Quality");
            this.ComposeQuality(net);
            this.ComposeSource(net);
            this.ComposeMixing(net);
            this.ComposeReaction(net);


            this._writer.NewSpreadsheet("Config");
            this.ComposeHeader(net);
            this.ComposeTimes(net);
            this.ComposeOptions(net);
            this.ComposeReport(net);
            this.ComposeEmitters(net);
            this.ComposeStatus(net);

            this._writer.NewSpreadsheet("GIS");
            this.ComposeLabels(net);
            this.ComposeCoordinates(net);
            this.ComposeVertices(net);



            this._workbook.Write(File.OpenWrite(fileName));
        } catch (IOException) {

        }
    }


    private void ComposeReaction(Network net) {
        PropertiesMap pMap = net.PropertiesMap;

        this._writer.Write(Network.SectType.REACTIONS.parseStr(),NEWLINE);
        this._writer.WriteHeader(REACTIONS_SUBTITLE);

        this._writer.Write("ORDER", "BULK", pMap.BulkOrder, NEWLINE);
        this._writer.Write("ORDER", "WALL", pMap.WallOrder, NEWLINE);
        this._writer.Write("ORDER", "TANK", pMap.TankOrder, NEWLINE);
        this._writer.Write("GLOBAL", "BULK", pMap.Kbulk * Constants.SECperDAY, NEWLINE);
        this._writer.Write("GLOBAL", "WALL", pMap.Kwall * Constants.SECperDAY, NEWLINE);
        //if (pMap.getClimit() > 0.0)
        this._writer.Write("LIMITING", "POTENTIAL", pMap.Climit, NEWLINE);

        //if (pMap.getRfactor() != Constants.MISSING && pMap.getRfactor() != 0.0)
        this._writer.Write("ROUGHNESS", "CORRELATION", pMap.Rfactor, NEWLINE);


        foreach (Link link  in  net.Links) {
            if (link.Type > Link.LinkType.PIPE)
                continue;

            if (link.Kb != pMap.Kbulk)
                this._writer.Write("BULK", link.Id, link.Kb * Constants.SECperDAY, NEWLINE);
            if (link.Kw != pMap.Kwall)
                this._writer.Write("WALL", link.Id, link.Kw * Constants.SECperDAY, NEWLINE);
        }

        foreach (Tank tank  in  net.Tanks) {
            if (tank.IsReservoir) continue;
            if (tank.Kb != pMap.Kbulk)
                this._writer.Write("TANK", tank.Id, tank.Kb * Constants.SECperDAY, NEWLINE);
        }
        this._writer.NewLine();
    }

    private void ComposeReport(Network net) {
        this._writer.Write(Network.SectType.REPORT.parseStr(),NEWLINE);

        PropertiesMap pMap = net.PropertiesMap;
        FieldsMap fMap = net.FieldsMap;
        this._writer.Write("PAGESIZE", pMap.PageSize, NEWLINE);
        this._writer.Write("STATUS", pMap.Statflag.ToString(), NEWLINE);
        this._writer.Write("SUMMARY", pMap.Summaryflag ? Keywords.w_YES : Keywords.w_NO, NEWLINE);
        this._writer.Write("ENERGY", pMap.Energyflag ? Keywords.w_YES : Keywords.w_NO, NEWLINE);

        switch (pMap.Nodeflag) {
            case PropertiesMap.ReportFlag.FALSE:
                this._writer.Write("NODES", "NONE", NEWLINE);
                break;
            case PropertiesMap.ReportFlag.TRUE:
                this._writer.Write("NODES", "ALL", NEWLINE);
                break;
            case PropertiesMap.ReportFlag.SOME: {
                int j = 0;
                foreach (Node node  in  net.Nodes) {
                    if (node.RptFlag) {
                        if (j % 5 == 0) this._writer.Write("NODES", NEWLINE);
                        this._writer.Write(node.Id);
                        j++;
                    }
                }
                break;
            }
        }

        switch (pMap.Linkflag) {
            case PropertiesMap.ReportFlag.FALSE:
                this._writer.Write("LINKS", "NONE", NEWLINE);
                break;
            case PropertiesMap.ReportFlag.TRUE:
                this._writer.Write("LINKS", "ALL", NEWLINE);
                break;
            case PropertiesMap.ReportFlag.SOME: {
                int j = 0;
                foreach (Link link  in  net.Links) {
                    if (link.RptFlag) {
                        if (j % 5 == 0) this._writer.Write("LINKS", NEWLINE);
                        this._writer.Write(link.Id);
                        j++;
                    }
                }
                break;
            }
        }

        for (FieldsMap.FieldType i = 0; i < FieldsMap.FieldType.FRICTION; i++)
        {
            Field f = fMap.GetField(i);
            if (f.Enabled) {
                this._writer.Write(f.Name, "PRECISION", f.Precision, NEWLINE);
                if (f.GetRptLim(Field.RangeType.LOW) < Constants.BIG)
                    this._writer.Write(f.Name, "BELOW", f.GetRptLim(Field.RangeType.LOW), NEWLINE);
                if (f.GetRptLim(Field.RangeType.HI) > -Constants.BIG)
                    this._writer.Write(f.Name, "ABOVE", f.GetRptLim(Field.RangeType.HI), NEWLINE);
            } else
                this._writer.Write(f.Name, "NO", NEWLINE);
        }

        this._writer.NewLine();
    }

    private void ComposeReservoirs(Network net) {
        FieldsMap fMap = net.FieldsMap;
        
        if(!net.Tanks.Any())
            return;

        List<Tank> reservoirs = new List<Tank>();
        foreach(Tank tank in net.Tanks)
            if (tank.IsReservoir)
                reservoirs.Add(tank);

        this._writer.Write(Network.SectType.RESERVOIRS.parseStr(),NEWLINE);
        this._writer.WriteHeader(RESERVOIRS_SUBTITLE);

        foreach (Tank r  in  reservoirs) {
            this._writer.Write(r.Id, fMap.RevertUnit(FieldsMap.FieldType.ELEV, r.Elevation));

            if (r.Pattern!=null)
                this._writer.Write(r.Pattern.Id);

            if (!string.IsNullOrEmpty(r.Comment))
                this._writer.Write(";" + r.Comment);

            this._writer.NewLine();
        }

        this._writer.NewLine();
    }

    private void ComposeRules(Network net) {
        this._writer.Write(Network.SectType.RULES.parseStr(),NEWLINE);
        foreach (Rule r  in  net.Rules) {
            this._writer.Write("RULE ",r.Label, NEWLINE);

            foreach (string s  in  r.Code)
                this._writer.Write(s, NEWLINE);

            this._writer.NewLine();
        }
        this._writer.NewLine();
    }

    private void ComposeSource(Network net) {
        this._writer.Write(Network.SectType.SOURCES.parseStr(),NEWLINE);
        this._writer.WriteHeader(SOURCE_SUBTITLE);

        foreach(Node node in net.Nodes) {
            Source source = node.Source;
            if (source == null)
                continue;
            this._writer.Write(node.Id,
                    source.Type.ParseStr(),
                    source.C0);
            if (source.Pattern != null)
                this._writer.Write(source.Pattern.Id);
            this._writer.NewLine();
        }
        this._writer.NewLine();
    }

    private void ComposeStatus(Network net) {

        this._writer.Write(Network.SectType.STATUS.parseStr(),NEWLINE);
        this._writer.WriteHeader(STATUS_SUBTITLE);

        foreach (Link link  in  net.Links) {
            if (link.Type <= Link.LinkType.PUMP) {
                if (link.Status == Link.StatType.CLOSED)
                    this._writer.Write(link.Id, Link.StatType.CLOSED.ParseStr());
                else if (link.Type == Link.LinkType.PUMP) {  // Write pump speed here for pumps with old-style pump curve input
                    Pump pump = (Pump) link;
                    if (pump.Hcurve == null &&
                            pump.Ptype != Pump.PumpType.CONST_HP &&
                            pump.Roughness != 1.0)
                        this._writer.Write(link.Id, link.Roughness);
                }
            } else if (link.Roughness == Constants.MISSING)  // Write fixed-status PRVs & PSVs (setting = MISSING)
            {
                if (link.Status == Link.StatType.OPEN)
                    this._writer.Write(link.Id, Link.StatType.OPEN.ParseStr());

                if (link.Status == Link.StatType.CLOSED)
                    this._writer.Write(link.Id, Link.StatType.CLOSED.ParseStr());

            }

            this._writer.NewLine();
        }

        this._writer.NewLine();
    }

    private void ComposeTanks(Network net) {
        FieldsMap fMap = net.FieldsMap;

        this._writer.Write(Network.SectType.TANKS.parseStr(),NEWLINE);
        this._writer.WriteHeader(TANK_SUBTITLE);

        foreach(Tank tank in net.Tanks) {
            if (tank.IsReservoir) continue;

            double vmin = tank.Vmin;
            if(Math.Abs(vmin/tank.Area - (tank.Hmin-tank.Elevation))<0.1)
                vmin = 0;

            this._writer.Write(tank.Id,
                    fMap.RevertUnit(FieldsMap.FieldType.ELEV, tank.Elevation),
                    fMap.RevertUnit(FieldsMap.FieldType.ELEV, tank.H0 - tank.Elevation),
                    fMap.RevertUnit(FieldsMap.FieldType.ELEV, tank.Hmin - tank.Elevation),
                    fMap.RevertUnit(FieldsMap.FieldType.ELEV, tank.Hmax - tank.Elevation),
                    fMap.RevertUnit(FieldsMap.FieldType.ELEV, 2 * Math.Sqrt(tank.Area / Math.PI)),
                    fMap.RevertUnit(FieldsMap.FieldType.VOLUME, vmin));

            if (tank.Vcurve != null)
                this._writer.Write(tank.Vcurve.Id);

            if (!string.IsNullOrEmpty(tank.Comment))
                this._writer.Write(";" + tank.Comment);

            this._writer.NewLine();
        }

        this._writer.NewLine();
    }


    private void ComposeTimes(Network net) {
        this._writer.Write(Network.SectType.TIMES.parseStr(),NEWLINE);
        PropertiesMap pMap = net.PropertiesMap;
        
        this._writer.Write("DURATION", TimeSpan.FromSeconds(pMap.Duration), NEWLINE);
        this._writer.Write("HYDRAULIC", "TIMESTEP", TimeSpan.FromSeconds(pMap.Hstep), NEWLINE);
        this._writer.Write("QUALITY", "TIMESTEP", TimeSpan.FromSeconds(pMap.Qstep), NEWLINE);
        this._writer.Write("REPORT", "TIMESTEP", TimeSpan.FromSeconds(pMap.Rstep), NEWLINE);
        this._writer.Write("REPORT", "START", TimeSpan.FromSeconds(pMap.Rstart), NEWLINE);
        this._writer.Write("PATTERN", "TIMESTEP", TimeSpan.FromSeconds(pMap.Pstep), NEWLINE);
        this._writer.Write("PATTERN", "START", TimeSpan.FromSeconds(pMap.Pstart), NEWLINE);
        this._writer.Write("RULE", "TIMESTEP", TimeSpan.FromSeconds(pMap.Rulestep), NEWLINE);
        this._writer.Write("START", "CLOCKTIME", TimeSpan.FromSeconds(pMap.Tstart), NEWLINE);
        this._writer.Write("STATISTIC", pMap.Tstatflag.ParseStr(), NEWLINE);
        this._writer.NewLine();
    }

    private void ComposeValves(Network net) {
        FieldsMap fMap = net.FieldsMap;
        
        this._writer.Write(Network.SectType.VALVES.parseStr(),NEWLINE);
        this._writer.WriteHeader(VALVES_SUBTITLE);

        foreach(Valve valve in net.Valves) {
            double d = valve.Diameter;
            double kc = valve.Roughness;
            if (kc == Constants.MISSING)
                kc = 0.0;

            switch (valve.Type) {
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

            this._writer.Write(valve.Id,
                    valve.FirstNode.Id,
                    valve.SecondNode.Id,
                    fMap.RevertUnit(FieldsMap.FieldType.DIAM, d),
                    valve.Type.ParseStr());

            if (valve.Type == Link.LinkType.GPV && valve.Curve != null)
                this._writer.Write(valve.Curve.Id, km);
            else
                this._writer.Write(kc, km);

            if (!string.IsNullOrEmpty(valve.Comment))
                this._writer.Write(";" + valve.Comment);

            this._writer.NewLine();
        }
        this._writer.NewLine();
    }

    private void ComposeVertices(Network net) {
        this._writer.Write(Network.SectType.VERTICES.parseStr(),NEWLINE);
        this._writer.WriteHeader(VERTICES_SUBTITLE);

        foreach (Link link  in  net.Links) {
            foreach (EnPoint p  in  link.Vertices) {
                this._writer.Write(link.Id, p.X, p.Y, NEWLINE);
            }
        }

        this._writer.NewLine();
    }


}
}