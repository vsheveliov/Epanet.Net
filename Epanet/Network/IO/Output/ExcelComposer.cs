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
using NPOI.HSSF.UserModel;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.network.io.output {

///<summary>EXCEL XLSX composer class.</summary>
public class ExcelComposer : OutputComposer {


    class ExcelWriter {
        private readonly XSSFWorkbook workbook;
        IRow activeRow;
        ISheet activeSheet;
        int cellCount;
        int rowCount;

        readonly ICellStyle timeStyle;
        readonly ICellStyle topBold;

        public ExcelWriter(XSSFWorkbook workbook) {
            this.workbook = workbook;
            topBold = workbook.CreateCellStyle();
            IFont newFont = workbook.CreateFont();
            newFont.Boldweight = (short)FontBoldWeight.Bold;
            topBold.SetFont(newFont);
            timeStyle = workbook.CreateCellStyle();
            timeStyle.DataFormat = 46;
        }

        public void newLine() {
            activeRow = activeSheet.CreateRow(rowCount++);
            cellCount = 0;
        }

        public void newSpreadsheet(string name) {
            rowCount = 0;
            cellCount = 0;
            activeSheet = workbook.CreateSheet(name);
            activeRow = null;
        }

        public void write(params object[] args) {
            if (activeRow == null) {
                activeRow = activeSheet.CreateRow(rowCount++);
                cellCount = 0;
            }

            foreach (object obj  in  args) {
                if (obj is string && obj.Equals(NEWLINE)) {
                    activeRow = activeSheet.CreateRow(rowCount++);
                    cellCount = 0;
                    continue;
                }

                var c = activeRow.CreateCell(cellCount++);

                if(obj==null)
                    c.SetCellType(CellType.Blank);
                else if(obj is DateTime){
                    DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc); //Getting UTC DATE since epoch
                    TimeSpan ts = (DateTime)obj - epochStart; //get the current timestamp between now and january 1970
                    c.SetCellValue(ts.TotalSeconds / 86400.0d);
                    c.CellStyle = timeStyle;
                }
                else if (obj is bool)
                    c.SetCellValue(((bool) obj));
                else if (obj.IsNumber())
                    c.SetCellValue(Convert.ToDouble(obj));
                else
                    c.SetCellValue( obj.ToString());
            }
        }

        public void writeHeader(string str) {
            string[] sections = str.Split('\t');
            //for (int i = 0; i < sections.length; i++)
            //    sections[i] = ";" + sections[i];
            //if(sections.length>0)
            //    sections[0] = ";" + sections[0];

            //write(sections);
            if (activeRow == null) {
                activeRow = activeSheet.CreateRow(rowCount++);
                cellCount = 0;
            }

            foreach (string obj  in  sections) {
                ICell c = activeRow.CreateCell(cellCount++);
                c.CellStyle = topBold;
                c.SetCellValue( obj);
            }

            newLine();
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

    //private const String TITLE_TAG       = "[TITLE]";
    //private const String JUNCTIONS_TAG   = "[JUNCTIONS]";
    //private const String TANKS_TAG       = "[TANKS]";
    //private const String RESERVOIRS_TAG  = "[RESERVOIRS]";
    //private const String PIPES_TAG       = "[PIPES]";
    //private const String PUMPS_TAG       = "[PUMPS]";
    //private const String VALVES_TAG      = "[VALVES]";
    //private const String DEMANDS_TAG     = "[DEMANDS]";
    //private const String EMITTERS_TAG    = "[EMITTERS]";
    //private const String STATUS_TAG      = "[STATUS]";
    //private const String PATTERNS_TAG    = "[PATTERNS]";
    //private const String CURVES_TAG      = "[CURVES]";
    //private const String CONTROLS_TAG    = "[CONTROLS]";
    //private const String QUALITY_TAG     = "[QUALITY]";
    //private const String SOURCE_TAG      = "[SOURCE]";
    //private const String MIXING_TAG      = "[MIXING]";
    //private const String REACTIONS_TAG   = "[REACTIONS]";
    //private const String ENERGY_TAG      = "[ENERGY]";
    //private const String TIMES_TAG       = "[TIMES]";
    //private const String OPTIONS_TAG     = "[OPTIONS]";
    //private const String REPORT_TAG      = "[REPORT]";
    //private const String COORDINATES_TAG = "[COORDINATES]";
    //private const String RULES_TAG       = "[RULES]";
    //private const String VERTICES_TAG    = "[VERTICES]";
    //private const String LABELS_TAG      = "[LABELS]";


    private const string VERTICES_SUBTITLE = "Link\tX-Coord\tY-Coord";

    XSSFWorkbook workbook;

    ExcelWriter writer;

    private void composeControls(Network net) {
        Control[] controls = net.getControls();
        FieldsMap fmap = net.getFieldsMap();

        writer.write(Network.SectType.CONTROLS.parseStr(),NEWLINE);
        writer.writeHeader("Code");

        foreach (Control control  in  controls) {
            // Check that controlled link exists
            if (control.getLink() == null) continue;

            // Get text of control's link status/setting
            if (control.getSetting() == Constants.MISSING)
                writer.write("LINK", control.getLink().getId(), control.getStatus().ParseStr());
            else {
                double kc = control.getSetting();
                switch (control.getLink().getType()) {
                    case Link.LinkType.PRV:
                    case Link.LinkType.PSV:
                    case Link.LinkType.PBV:
                        kc = fmap.revertUnit(FieldsMap.Type.PRESSURE, kc);
                        break;
                    case Link.LinkType.FCV:
                        kc = fmap.revertUnit(FieldsMap.Type.FLOW, kc);
                        break;
                }
                writer.write("LINK", control.getLink().getId(), kc);
            }


            switch (control.getType()) {
                // Print level control
                case Control.ControlType.LOWLEVEL:
                case Control.ControlType.HILEVEL:
                    double kc = control.getGrade() - control.getNode().getElevation();
                    if (control.getNode() is Tank) kc = fmap.revertUnit(FieldsMap.Type.HEAD, kc);
                    else
                        kc = fmap.revertUnit(FieldsMap.Type.PRESSURE, kc);
                    writer.write("IF", "NODE", control.getNode().getId(), control.getType().ParseStr(), kc);
                    break;

                // Print timer control
                case Control.ControlType.TIMER:
                writer.write("AT", Control.ControlType.TIMER.ParseStr(), control.getTime() / 3600.0f, "HOURS");
                    break;

                // Print time-of-day control
                case Control.ControlType.TIMEOFDAY:
                    writer.write("AT", Control.ControlType.TIMEOFDAY.ParseStr(), Utilities.getClockTime(control.getTime()));
                    break;
            }
            writer.newLine();
        }
        writer.newLine();
    }

    private void composeCoordinates(Network net) {
        writer.write(Network.SectType.COORDINATES.parseStr(),NEWLINE);
        writer.writeHeader(COORDINATES_SUBTITLE);

        foreach (Node node  in  net.getNodes()) {
            if (node.getPosition() != null) {
                writer.write(node.getId(), node.getPosition().getX(), node.getPosition().getY(), NEWLINE);
            }
        }
        writer.newLine();
    }


    private void composeCurves(Network net) {

        List<Curve> curves = new List<Curve>(net.getCurves());

        writer.write(Network.SectType.CURVES.parseStr(),NEWLINE);
        writer.writeHeader(CURVE_SUBTITLE);

        foreach (Curve c  in  curves) {
            for (int i = 0; i < c.getNpts(); i++) {
                writer.write(c.getId(), c.getX()[i], c.getY()[i]);
                writer.newLine();
            }
        }

        writer.newLine();
    }

    private void composeDemands(Network net) {
        FieldsMap fMap = net.getFieldsMap();

        writer.write(Network.SectType.DEMANDS.parseStr(),NEWLINE);
        writer.writeHeader(DEMANDS_SUBTITLE);

        double ucf = fMap.getUnits(FieldsMap.Type.DEMAND);

        foreach (Node node  in  net.getJunctions()) {

            if (node.getDemand().Count > 1)
                for(int i = 1;i<node.getDemand().Count;i++){
                    Demand demand = node.getDemand()[i];
                    writer.write(node.getId(), ucf * demand.getBase());
                    if (demand.getPattern() != null && !string.IsNullOrEmpty(demand.getPattern().getId()))
                        writer.write(demand.getPattern().getId());
                    writer.newLine();
                }
        }

        writer.newLine();
    }

    private void composeEmitters(Network net) {
        writer.write(Network.SectType.EMITTERS.parseStr(),NEWLINE);
        writer.writeHeader(EMITTERS_SUBTITLE);

        double uflow = net.getFieldsMap().getUnits(FieldsMap.Type.FLOW);
        double upressure = net.getFieldsMap().getUnits(FieldsMap.Type.PRESSURE);
        double Qexp = net.getPropertiesMap().getQexp();

        foreach (Node node  in  net.getJunctions()) {
            if (node.getKe() == 0.0) continue;
            double ke = uflow / Math.Pow(upressure * node.getKe(), (1.0 / Qexp));
            writer.write(node.getId(), ke);
            writer.newLine();
        }

        writer.newLine();
    }

    private void composeEnergy(Network net) {
        PropertiesMap pMap = net.getPropertiesMap();

        writer.write(Network.SectType.ENERGY.parseStr(),NEWLINE);

        if (pMap.getEcost() != 0.0)
            writer.write("GLOBAL", "PRICE", pMap.getEcost(), NEWLINE);

        if (!string.IsNullOrEmpty(pMap.getEpatId()))
            writer.write("GLOBAL", "PATTERN", pMap.getEpatId(), NEWLINE);

        writer.write("GLOBAL", "EFFIC", pMap.getEpump(), NEWLINE);
        writer.write("DEMAND", "CHARGE", pMap.getDcost(), NEWLINE);

        foreach (Pump p  in  net.getPumps()) {
            if (p.getEcost() > 0.0)
                writer.write("PUMP", p.getId(), "PRICE", p.getEcost(), NEWLINE);
            if (p.getEpat() != null)
                writer.write("PUMP", p.getId(), "PATTERN", p.getEpat().getId(), NEWLINE);
            if (p.getEcurve() != null)
                writer.write("PUMP", p.getId(), "EFFIC", p.getId(), p.getEcurve().getId(), NEWLINE);
        }
        writer.newLine();

    }

    public void composeHeader(Network net) {
        if (net.getTitleText().Count == 0)
            return;

        writer.write(Network.SectType.TITLE.parseStr(),NEWLINE);
        writer.writeHeader(TITLE_SUBTITLE);

        foreach (string str  in  net.getTitleText()) {
            writer.write(str);
            writer.newLine();
        }

        writer.newLine();
    }

    private void composeJunctions(Network net) {
        FieldsMap fMap = net.getFieldsMap();
        PropertiesMap pMap = net.getPropertiesMap();

        writer.write(Network.SectType.JUNCTIONS.parseStr(),NEWLINE);
        writer.writeHeader(JUNCS_SUBTITLE);

        foreach (Node node  in  net.getJunctions()) {
            writer.write(node.getId(), fMap.revertUnit(FieldsMap.Type.ELEV, node.getElevation()));

            if( node.getDemand().Count>0){
                Demand d = node.getDemand()[0];
                writer.write(fMap.revertUnit(FieldsMap.Type.DEMAND, d.getBase()));

                if (!string.IsNullOrEmpty(d.getPattern().getId()) 
                    && !pMap.getDefPatId().Equals(d.getPattern().getId(), StringComparison.OrdinalIgnoreCase))
                    writer.write(d.getPattern().getId());
            }

            if (!string.IsNullOrEmpty(node.getComment()))
                writer.write(";" + node.getComment());

            writer.newLine();
        }


        writer.newLine();
    }

    private void composeLabels(Network net) {
        writer.write(Network.SectType.LABELS.parseStr(),NEWLINE);
        writer.writeHeader(LABELS_SUBTITLE);

        foreach (Label label  in  net.getLabels()) {
            writer.write(label.getPosition().getX(), label.getPosition().getY(),label.getText(), NEWLINE);
        }
        writer.newLine();
    }

    private void composeMixing(Network net) {
        writer.write(Network.SectType.MIXING.parseStr(),NEWLINE);
        writer.writeHeader(MIXING_SUBTITLE);

        foreach (Tank tank  in  net.getTanks()) {
            if (tank.getArea() == 0.0) continue;
            writer.write(tank.getId(), tank.getMixModel().ParseStr(),
                    (tank.getV1max() / tank.getVmax()));
            writer.newLine();
        }
        writer.newLine();
    }

    private void composeOptions(Network net) {
        writer.write(Network.SectType.OPTIONS.parseStr(),NEWLINE);

        PropertiesMap pMap = net.getPropertiesMap();
        FieldsMap fMap = net.getFieldsMap();

        writer.write("UNITS", pMap.getFlowflag().ParseStr(), NEWLINE);
        writer.write("PRESSURE", pMap.getPressflag().ParseStr(), NEWLINE);
        writer.write("HEADLOSS", pMap.getFormflag().ParseStr(), NEWLINE);

        if (!string.IsNullOrEmpty(pMap.getDefPatId()))
            writer.write("PATTERN", pMap.getDefPatId(), NEWLINE);

        if (pMap.getHydflag() == PropertiesMap.Hydtype.USE)
            writer.write("HYDRAULICS USE", pMap.getHydFname(), NEWLINE);

        if (pMap.getHydflag() == PropertiesMap.Hydtype.SAVE)
            writer.write("HYDRAULICS SAVE", pMap.getHydFname(), NEWLINE);

        if (pMap.getExtraIter() == -1)
            writer.write("UNBALANCED", "STOP", NEWLINE);

        if (pMap.getExtraIter() >= 0)
            writer.write("UNBALANCED", "CONTINUE", pMap.getExtraIter(), NEWLINE);

        if (pMap.getQualflag() == PropertiesMap.QualType.CHEM)
            writer.write("QUALITY", pMap.getChemName(), pMap.getChemUnits(), NEWLINE);

        if (pMap.getQualflag() == PropertiesMap.QualType.TRACE)
            writer.write("QUALITY", "TRACE", pMap.getTraceNode(), NEWLINE);

        if (pMap.getQualflag() == PropertiesMap.QualType.AGE)
            writer.write("QUALITY", "AGE", NEWLINE);

        if (pMap.getQualflag() == PropertiesMap.QualType.NONE)
            writer.write("QUALITY", "NONE", NEWLINE);

        writer.write("DEMAND", "MULTIPLIER", pMap.getDmult(), NEWLINE);

        writer.write("EMITTER", "EXPONENT", 1.0 / pMap.getQexp(), NEWLINE);

        writer.write("VISCOSITY", pMap.getViscos() / Constants.VISCOS, NEWLINE);

        writer.write("DIFFUSIVITY", pMap.getDiffus() / Constants.DIFFUS, NEWLINE);

        writer.write("SPECIFIC", "GRAVITY", pMap.getSpGrav(), NEWLINE);

        writer.write("TRIALS", pMap.getMaxIter(), NEWLINE);

        writer.write("ACCURACY", pMap.getHacc(), NEWLINE);

        writer.write("TOLERANCE", fMap.revertUnit(FieldsMap.Type.QUALITY, pMap.getCtol()), NEWLINE);

        writer.write("CHECKFREQ", pMap.getCheckFreq(), NEWLINE);

        writer.write("MAXCHECK", pMap.getMaxCheck(), NEWLINE);

        writer.write("DAMPLIMIT", pMap.getDampLimit(), NEWLINE);

        writer.newLine();
    }

    private void composePatterns(Network net) {

        List<Pattern> pats = new List<Pattern>(net.getPatterns());

        writer.write(Network.SectType.PATTERNS.parseStr(),NEWLINE);
        writer.writeHeader(PATTERNS_SUBTITLE);

        for (int i = 1; i < pats.Count; i++) {
            Pattern pat = pats[i];
            List<double> F = pat.getFactorsList();
            for (int j = 0; j < pats[i].getLength(); j++) {
                if (j % 6 == 0)
                    writer.write(pat.getId());
                writer.write(F[j]);

                if (j % 6 == 5)
                    writer.newLine();
            }
            writer.newLine();
        }

        writer.newLine();
    }

    private void composePipes(Network net) {
        FieldsMap fMap = net.getFieldsMap();
        PropertiesMap pMap = net.getPropertiesMap();

        List<Link> pipes = new List<Link>();
        foreach (Link link  in  net.getLinks())
            if (link.getType() <= Link.LinkType.PIPE)
                pipes.Add(link);

        writer.write(Network.SectType.PIPES.parseStr(),NEWLINE);
        writer.writeHeader(PIPES_SUBTITLE);

        foreach (Link link  in  pipes) {
            double d = link.getDiameter();
            double kc = link.getRoughness();
            if (pMap.getFormflag() == PropertiesMap.FormType.DW)
                kc = fMap.revertUnit(FieldsMap.Type.ELEV, kc * 1000.0);

            double km = link.getKm() * Math.Pow(d, 4.0) / 0.02517;

            writer.write(link.getId(),
                    link.getFirst().getId(),
                    link.getSecond().getId(),
                    fMap.revertUnit(FieldsMap.Type.LENGTH, link.getLenght()),
                    fMap.revertUnit(FieldsMap.Type.DIAM, d));

            //if (pMap.getFormflag() == FormType.DW)
            writer.write(kc, km);

            if (link.getType() == Link.LinkType.CV)
                writer.write("CV");
            else if (link.getStat() == Link.StatType.CLOSED)
                writer.write("CLOSED");
            else if (link.getStat() == Link.StatType.OPEN)
                writer.write("OPEN");

            if (!string.IsNullOrEmpty(link.getComment()))
                writer.write(";" + link.getComment());

            writer.newLine();
        }

        writer.newLine();
    }

    private void composePumps(Network net) {
        FieldsMap fMap = net.getFieldsMap();
        List<Pump> pumps = new List<Pump>(net.getPumps());

        writer.write(Network.SectType.PUMPS.parseStr(),NEWLINE);
        writer.writeHeader(PUMPS_SUBTITLE);

        foreach (Pump pump  in  pumps) {
            writer.write(pump.getId(),
                    pump.getFirst().getId(), pump.getSecond().getId());


            // Pump has constant power
            if (pump.getPtype() == Pump.Type.CONST_HP)
                writer.write("POWER", pump.getKm());
                // Pump has a head curve
            else if (pump.getHcurve() != null)
                writer.write("HEAD", pump.getHcurve().getId());
                // Old format used for pump curve
            else {
                writer.write(
                        fMap.revertUnit(FieldsMap.Type.HEAD, -pump.getH0()),
                        fMap.revertUnit(FieldsMap.Type.HEAD, -pump.getH0() - pump.getFlowCoefficient() * Math.Pow(pump.getQ0(), pump.getN())),
                        fMap.revertUnit(FieldsMap.Type.FLOW, pump.getQ0()), 0.0,
                        fMap.revertUnit(FieldsMap.Type.FLOW, pump.getQmax()
                        ));
                continue;
            }

            if (pump.getUpat() != null)
                writer.write("PATTERN", pump.getUpat().getId());

            if (pump.getRoughness() != 1.0)
                writer.write("SPEED", pump.getRoughness());

            if (!string.IsNullOrEmpty(pump.getComment()))
                writer.write(";" + pump.getComment());

            writer.newLine();
        }

        writer.newLine();
    }

    private void composeQuality(Network net) {
        Node[] nodes = net.getNodes();
        FieldsMap fmap = net.getFieldsMap();

        writer.write(Network.SectType.QUALITY.parseStr(),NEWLINE);
        writer.writeHeader(QUALITY_SUBTITLE);

        foreach (Node node  in  nodes) {
            if (node.getC0().Length == 1) {
                if (node.getC0()[0] == 0.0) continue;
                writer.write(node.getId(), fmap.revertUnit(FieldsMap.Type.QUALITY, node.getC0()[0]));
            }
            writer.newLine();
        }
        writer.newLine();
    }

    public override void composer(Network net, string fileName) {

        workbook = new XSSFWorkbook();
        writer = new ExcelWriter(workbook);

        try {


            writer.newSpreadsheet("Junctions");
            composeJunctions(net);

            writer.newSpreadsheet("Tanks");
            composeReservoirs(net);
            composeTanks(net);

            writer.newSpreadsheet("Pipes");
            composePipes(net);

            writer.newSpreadsheet("Pumps");
            composePumps(net);
            composeEnergy(net);

            writer.newSpreadsheet("Valves");
            composeValves(net);

            writer.newSpreadsheet("Demands");
            composeDemands(net);

            writer.newSpreadsheet("Patterns");
            composePatterns(net);
            writer.newSpreadsheet("Curves");
            composeCurves(net);

            writer.newSpreadsheet("Script");
            composeControls(net);
            composeRules(net);

            writer.newSpreadsheet("Quality");
            composeQuality(net);
            composeSource(net);
            composeMixing(net);
            composeReaction(net);


            writer.newSpreadsheet("Config");
            composeHeader(net);
            composeTimes(net);
            composeOptions(net);
            composeReport(net);
            composeEmitters(net);
            composeStatus(net);

            writer.newSpreadsheet("GIS");
            composeLabels(net);
            composeCoordinates(net);
            composeVertices(net);



            workbook.Write(File.OpenWrite(fileName));
        } catch (IOException) {

        }
    }


    private void composeReaction(Network net) {
        PropertiesMap pMap = net.getPropertiesMap();

        writer.write(Network.SectType.REACTIONS.parseStr(),NEWLINE);
        writer.writeHeader(REACTIONS_SUBTITLE);

        writer.write("ORDER", "BULK", pMap.getBulkOrder(), NEWLINE);
        writer.write("ORDER", "WALL", pMap.getWallOrder(), NEWLINE);
        writer.write("ORDER", "TANK", pMap.getTankOrder(), NEWLINE);
        writer.write("GLOBAL", "BULK", pMap.getKbulk() * Constants.SECperDAY, NEWLINE);
        writer.write("GLOBAL", "WALL", pMap.getKwall() * Constants.SECperDAY, NEWLINE);
        //if (pMap.getClimit() > 0.0)
        writer.write("LIMITING", "POTENTIAL", pMap.getClimit(), NEWLINE);

        //if (pMap.getRfactor() != Constants.MISSING && pMap.getRfactor() != 0.0)
        writer.write("ROUGHNESS", "CORRELATION", pMap.getRfactor(), NEWLINE);


        foreach (Link link  in  net.getLinks()) {
            if (link.getType() > Link.LinkType.PIPE)
                continue;

            if (link.getKb() != pMap.getKbulk())
                writer.write("BULK", link.getId(), link.getKb() * Constants.SECperDAY, NEWLINE);
            if (link.getKw() != pMap.getKwall())
                writer.write("WALL", link.getId(), link.getKw() * Constants.SECperDAY, NEWLINE);
        }

        foreach (Tank tank  in  net.getTanks()) {
            if (tank.getArea() == 0.0) continue;
            if (tank.getKb() != pMap.getKbulk())
                writer.write("TANK", tank.getId(), tank.getKb() * Constants.SECperDAY, NEWLINE);
        }
        writer.newLine();
    }

    private void composeReport(Network net) {
        writer.write(Network.SectType.REPORT.parseStr(),NEWLINE);

        PropertiesMap pMap = net.getPropertiesMap();
        FieldsMap fMap = net.getFieldsMap();
        writer.write("PAGESIZE", pMap.getPageSize(), NEWLINE);
        writer.write("STATUS", pMap.getStatflag().ToString(), NEWLINE);
        writer.write("SUMMARY", pMap.getSummaryflag() ? Keywords.w_YES : Keywords.w_NO, NEWLINE);
        writer.write("ENERGY", pMap.getEnergyflag() ? Keywords.w_YES : Keywords.w_NO, NEWLINE);

        switch (pMap.getNodeflag()) {
            case PropertiesMap.ReportFlag.FALSE:
                writer.write("NODES", "NONE", NEWLINE);
                break;
            case PropertiesMap.ReportFlag.TRUE:
                writer.write("NODES", "ALL", NEWLINE);
                break;
            case PropertiesMap.ReportFlag.SOME: {
                int j = 0;
                foreach (Node node  in  net.getNodes()) {
                    if (node.isRptFlag()) {
                        if (j % 5 == 0) writer.write("NODES", NEWLINE);
                        writer.write(node.getId());
                        j++;
                    }
                }
                break;
            }
        }

        switch (pMap.getLinkflag()) {
            case PropertiesMap.ReportFlag.FALSE:
                writer.write("LINKS", "NONE", NEWLINE);
                break;
            case PropertiesMap.ReportFlag.TRUE:
                writer.write("LINKS", "ALL", NEWLINE);
                break;
            case PropertiesMap.ReportFlag.SOME: {
                int j = 0;
                foreach (Link link  in  net.getLinks()) {
                    if (link.isRptFlag()) {
                        if (j % 5 == 0) writer.write("LINKS", NEWLINE);
                        writer.write(link.getId());
                        j++;
                    }
                }
                break;
            }
        }

        for (FieldsMap.Type i = 0; i < FieldsMap.Type.FRICTION; i++)
        {
            Field f = fMap.getField(i);
            if (f.isEnabled()) {
                writer.write(f.getName(), "PRECISION", f.getPrecision(), NEWLINE);
                if (f.getRptLim(Field.RangeType.LOW) < Constants.BIG)
                    writer.write(f.getName(), "BELOW", f.getRptLim(Field.RangeType.LOW), NEWLINE);
                if (f.getRptLim(Field.RangeType.HI) > -Constants.BIG)
                    writer.write(f.getName(), "ABOVE", f.getRptLim(Field.RangeType.HI), NEWLINE);
            } else
                writer.write(f.getName(), "NO", NEWLINE);
        }

        writer.newLine();
    }

    private void composeReservoirs(Network net) {
        FieldsMap fMap = net.getFieldsMap();
        if (net.getTanks().Length == 0)
            return;

        List<Tank> reservoirs = new List<Tank>();
        foreach (Tank tank  in  net.getTanks())
            if (tank.getArea() == 0)
                reservoirs.Add(tank);

        writer.write(Network.SectType.RESERVOIRS.parseStr(),NEWLINE);
        writer.writeHeader(RESERVOIRS_SUBTITLE);

        foreach (Tank r  in  reservoirs) {
            writer.write(r.getId(), fMap.revertUnit(FieldsMap.Type.ELEV, r.getElevation()));

            if (r.getPattern()!=null)
                writer.write(r.getPattern().getId());

            if (!string.IsNullOrEmpty(r.getComment()))
                writer.write(";" + r.getComment());

            writer.newLine();
        }

        writer.newLine();
    }

    private void composeRules(Network net) {
        writer.write(Network.SectType.RULES.parseStr(),NEWLINE);
        foreach (Rule r  in  net.getRules()) {
            writer.write("RULE ",r.getLabel(), NEWLINE);
            foreach (string s  in  r.getCode().Split('\n'))
                writer.write(s, NEWLINE);
            writer.newLine();
        }
        writer.newLine();
    }

    private void composeSource(Network net) {
        var nodes = net.getNodes();

        writer.write(Network.SectType.SOURCES.parseStr(),NEWLINE);
        writer.writeHeader(SOURCE_SUBTITLE);

        foreach (Node node  in  nodes) {
            Source source = node.getSource();
            if (source == null)
                continue;
            writer.write(node.getId(),
                    source.getType().ParseStr(),
                    source.getC0());
            if (source.getPattern() != null)
                writer.write(source.getPattern().getId());
            writer.newLine();
        }
        writer.newLine();
    }

    private void composeStatus(Network net) {

        writer.write(Network.SectType.STATUS.parseStr(),NEWLINE);
        writer.writeHeader(STATUS_SUBTITLE);

        foreach (Link link  in  net.getLinks()) {
            if (link.getType() <= Link.LinkType.PUMP) {
                if (link.getStat() == Link.StatType.CLOSED)
                    writer.write(link.getId(), Link.StatType.CLOSED.ParseStr());
                else if (link.getType() == Link.LinkType.PUMP) {  // Write pump speed here for pumps with old-style pump curve input
                    Pump pump = (Pump) link;
                    if (pump.getHcurve() == null &&
                            pump.getPtype() != Pump.Type.CONST_HP &&
                            pump.getRoughness() != 1.0)
                        writer.write(link.getId(), link.getRoughness());
                }
            } else if (link.getRoughness() == Constants.MISSING)  // Write fixed-status PRVs & PSVs (setting = MISSING)
            {
                if (link.getStat() == Link.StatType.OPEN)
                    writer.write(link.getId(), Link.StatType.OPEN.ParseStr());

                if (link.getStat() == Link.StatType.CLOSED)
                    writer.write(link.getId(), Link.StatType.CLOSED.ParseStr());

            }

            writer.newLine();
        }

        writer.newLine();
    }

    private void composeTanks(Network net) {
        FieldsMap fMap = net.getFieldsMap();

        List<Tank> tanks = new List<Tank>();
        foreach (Tank tank  in  net.getTanks())
            if (tank.getArea() != 0)
                tanks.Add(tank);

        writer.write(Network.SectType.TANKS.parseStr(),NEWLINE);
        writer.writeHeader(TANK_SUBTITLE);

        foreach (Tank tank  in  tanks) {
            double Vmin = tank.getVmin();
            if(Math.Abs(Vmin/tank.getArea() - (tank.getHmin()-tank.getElevation()))<0.1)
                Vmin = 0;

            writer.write(tank.getId(),
                    fMap.revertUnit(FieldsMap.Type.ELEV, tank.getElevation()),
                    fMap.revertUnit(FieldsMap.Type.ELEV, tank.getH0() - tank.getElevation()),
                    fMap.revertUnit(FieldsMap.Type.ELEV, tank.getHmin() - tank.getElevation()),
                    fMap.revertUnit(FieldsMap.Type.ELEV, tank.getHmax() - tank.getElevation()),
                    fMap.revertUnit(FieldsMap.Type.ELEV, 2 * Math.Sqrt(tank.getArea() / Constants.PI)),
                    fMap.revertUnit(FieldsMap.Type.VOLUME, Vmin));

            if (tank.getVcurve() != null)
                writer.write(tank.getVcurve().getId());

            if (!string.IsNullOrEmpty(tank.getComment()))
                writer.write(";" + tank.getComment());

            writer.newLine();
        }

        writer.newLine();
    }


    private void composeTimes(Network net) {
        writer.write(Network.SectType.TIMES.parseStr(),NEWLINE);
        PropertiesMap pMap = net.getPropertiesMap();
        
        //writer.write("DURATION", Utilities.getClockTime(pMap.getDuration()), NEWLINE);
        //writer.write("HYDRAULIC", "TIMESTEP", Utilities.getClockTime(pMap.getHstep()), NEWLINE);
        //writer.write("QUALITY", "TIMESTEP", Utilities.getClockTime(pMap.getQstep()), NEWLINE);
        //writer.write("REPORT", "TIMESTEP", Utilities.getClockTime(pMap.getRstep()), NEWLINE);
        //writer.write("REPORT", "START", Utilities.getClockTime(pMap.getRstart()), NEWLINE);
        //writer.write("PATTERN", "TIMESTEP", Utilities.getClockTime(pMap.getPstep()), NEWLINE);
        //writer.write("PATTERN", "START", Utilities.getClockTime(pMap.getPstart()), NEWLINE);
        //writer.write("RULE", "TIMESTEP", Utilities.getClockTime(pMap.getRulestep()), NEWLINE);
        //writer.write("START", "CLOCKTIME", Utilities.getClockTime(pMap.getTstart()), NEWLINE);

        writer.write("DURATION", TimeSpan.FromSeconds(pMap.getDuration()), NEWLINE);
        writer.write("HYDRAULIC", "TIMESTEP", TimeSpan.FromSeconds(pMap.getHstep()), NEWLINE);
        writer.write("QUALITY", "TIMESTEP", TimeSpan.FromSeconds(pMap.getQstep()), NEWLINE);
        writer.write("REPORT", "TIMESTEP", TimeSpan.FromSeconds(pMap.getRstep()), NEWLINE);
        writer.write("REPORT", "START", TimeSpan.FromSeconds(pMap.getRstart()), NEWLINE);
        writer.write("PATTERN", "TIMESTEP", TimeSpan.FromSeconds(pMap.getPstep()), NEWLINE);
        writer.write("PATTERN", "START", TimeSpan.FromSeconds(pMap.getPstart()), NEWLINE);
        writer.write("RULE", "TIMESTEP", TimeSpan.FromSeconds(pMap.getRulestep()), NEWLINE);
        writer.write("START", "CLOCKTIME", TimeSpan.FromSeconds(pMap.getTstart()), NEWLINE);
        writer.write("STATISTIC", pMap.getTstatflag().ParseStr(), NEWLINE);
        writer.newLine();
    }

    private void composeValves(Network net) {
        FieldsMap fMap = net.getFieldsMap();
        List<Valve> valves = new List<Valve>(net.getValves());

        writer.write(Network.SectType.VALVES.parseStr(),NEWLINE);
        writer.writeHeader(VALVES_SUBTITLE);

        foreach (Valve valve  in  valves) {
            double d = valve.getDiameter();
            double kc = valve.getRoughness();
            if (kc == Constants.MISSING)
                kc = 0.0;

            switch (valve.getType()) {
                case Link.LinkType.FCV:
                    kc = fMap.revertUnit(FieldsMap.Type.FLOW, kc);
                    break;
                case Link.LinkType.PRV:
                case Link.LinkType.PSV:
                case Link.LinkType.PBV:
                    kc = fMap.revertUnit(FieldsMap.Type.PRESSURE, kc);
                    break;
            }

            double km = valve.getKm() * Math.Pow(d, 4) / 0.02517;

            writer.write(valve.getId(),
                    valve.getFirst().getId(),
                    valve.getSecond().getId(),
                    fMap.revertUnit(FieldsMap.Type.DIAM, d),
                    valve.getType().ParseStr());

            if (valve.getType() == Link.LinkType.GPV && valve.getCurve() != null)
                writer.write(valve.getCurve().getId(), km);
            else
                writer.write(kc, km);

            if (!string.IsNullOrEmpty(valve.getComment()))
                writer.write(";" + valve.getComment());

            writer.newLine();
        }
        writer.newLine();
    }

    private void composeVertices(Network net) {
        writer.write(Network.SectType.VERTICES.parseStr(),NEWLINE);
        writer.writeHeader(VERTICES_SUBTITLE);

        foreach (Link link  in  net.getLinks()) {
            foreach (Point p  in  link.getVertices()) {
                writer.write(link.getId(), p.getX(), p.getY(), NEWLINE);
            }
        }

        writer.newLine();
    }


}
}