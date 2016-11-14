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
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using org.addition.epanet.util;

namespace org.addition.epanet.network.io.input {

///<summary>Excel XLSX file parser.</summary>
public class ExcelParser : InpParser {


    public ExcelParser(TraceSource logger):base(logger) {
        this.Log = logger;
    }

    private string ConvertCell(ICell cell, Network.SectType section) {
        switch (cell.CellType) {
        case CellType.Numeric:
            return this._timeStyles.Contains(cell.CellStyle)
                ? ((long)Math.Round(cell.NumericCellValue * 86400)).GetClockTime()
                : cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);

        case CellType.String:
            return cell.StringCellValue;

        default:
            throw new EpanetParseException(
                ErrorCode.Err201,
                cell.RowIndex,
                this.FileName,
                section,
                cell.StringCellValue);
        }
    }

    readonly List<ICellStyle> _timeStyles = new List<ICellStyle>();

    private void FindTimeStyle(XSSFWorkbook workbook) {
        short[] validTimeFormats =
        {
            0x12, // "h:mm AM/PM"
            0x13, // "h:mm:ss AM/PM"
            0x14, // "h:mm"
            0x15, // "h:mm:ss"
            0x16, // "m/d/yy h:mm"
            0x2d, // "mm:ss"
            0x2e, // "[h]:mm:ss"
            0x2f, // "mm:ss.0"
        };

        var styleTable = workbook.GetStylesSource();
        
        for(int i = 0; i < styleTable.NumCellStyles; i++) {
            ICellStyle style = styleTable.GetStyleAt(i);

            //if(org.apache.poi.ss.usermodel.DateUtil.isInternalDateFormat(style.getDataFormat()))
            if (Array.IndexOf(validTimeFormats, style.DataFormat, 0) != -1)
                this._timeStyles.Add(style);
            else if (style.GetDataFormatString().ToLower().Contains("[h]:mm") ||
                        style.GetDataFormatString().ToLower().Contains("[hh]:mm"))
                this._timeStyles.Add(style);
        }
    }

    public override Network Parse(Network net, string f) {
        this.FileName = Path.GetFullPath(f);

        FileStream stream = null;
        try {
            stream = File.OpenRead(f);
            IWorkbook workbook = new XSSFWorkbook(stream);

            this.FindTimeStyle((XSSFWorkbook)workbook);

            Regex tagPattern = new Regex("\\[.*\\]");
        
            int errSum = 0;

            List<ISheet> sheetPC = new List<ISheet>();
            List<ISheet> sheetOthers = new List<ISheet>();
            List<ISheet> sheetNodes = new List<ISheet>();
            List<ISheet> sheetTanks = new List<ISheet>();

            for (int i = 0; i < workbook.NumberOfSheets; i++) {
                ISheet sh = workbook.GetSheetAt(i);

                if (sh.SheetName.Equals("Patterns", StringComparison.OrdinalIgnoreCase) ||
                    sh.SheetName.Equals("Curves", StringComparison.OrdinalIgnoreCase)) {
                    sheetPC.Add(sh);
                }
                else if (sh.SheetName.Equals("Junctions", StringComparison.OrdinalIgnoreCase))
                    sheetNodes.Add(sh);
                else if (sh.SheetName.Equals("Tanks", StringComparison.OrdinalIgnoreCase)
                         || sh.SheetName.Equals("Reservoirs", StringComparison.OrdinalIgnoreCase))
                    sheetTanks.Add(sh);
                else
                    sheetOthers.Add(sh);

            }
            errSum = this.ParseWorksheet(net, sheetPC, tagPattern, errSum); // parse the patterns and curves
            errSum = this.ParseWorksheet(net, sheetNodes, tagPattern, errSum); // parse the nodes
            errSum = this.ParseWorksheet(net, sheetTanks, tagPattern, errSum); // parse the nodes
            errSum = this.ParseWorksheet(net, sheetOthers, tagPattern, errSum); // parse other elements

            if (errSum != 0)
                throw new ENException(ErrorCode.Err200);

        }
        catch (IOException) {
            throw new ENException(ErrorCode.Err302);
        }
        finally {
            if (stream != null) {
                stream.Close();
            }
        }

        AdjustData(net);
        net.FieldsMap.Prepare(net.PropertiesMap.Unitsflag,
                net.PropertiesMap.Flowflag,
                net.PropertiesMap.Pressflag,
                net.PropertiesMap.Qualflag,
                net.PropertiesMap.ChemUnits,
                net.PropertiesMap.SpGrav,
                net.PropertiesMap.Hstep);

        this.Convert(net);
        return net;
    }

    private int ParseWorksheet(Network net, List<ISheet> sheets, Regex tagPattern, int errSum) {
        foreach (ISheet sheet  in  sheets) {

            bool lastRowNull = true;
            bool lastRowHeader = false;
            Network.SectType lastType = (Network.SectType)(-1);

            for (int rowCount = 0, tRowId = 0; rowCount < sheet.PhysicalNumberOfRows; tRowId++) {
                IRow row = sheet.GetRow(tRowId);

                if (row != null) {
                    List<string> tokens = new List<string>();

                    string comments = "";
                    bool allAreBold = true;

                    for (int cellCount = 0, tCellId = 0; cellCount < row.PhysicalNumberOfCells; tCellId++) {
                        ICell cell = row.GetCell(tCellId);
                        if (cell != null) {
                            string value = this.ConvertCell(cell, lastType);
                            if (value.StartsWith(";")) {
                                comments += value;
                            } else
                                tokens.Add(value);

                            allAreBold = allAreBold & ((XSSFCellStyle)cell.CellStyle).GetFont().IsBold; // TODO remover

                            cellCount++;
                        }
                    }

                    if (tokens.Count > 0) {
                        if (lastRowNull && tagPattern.IsMatch(tokens[0])) {
                            EnumsTxt.TryParse(tokens[0], out lastType);
                            lastRowHeader = true;
                        } else {
                            string[] tokArray = tokens.ToArray();

                            if (lastRowHeader && allAreBold) {
                                //System.out.println("Formating Header : " + tokens.toArray(new string[tokens.size()]));
                            } else {
                                try {
                                    this.ParseSect(net, lastType, comments, tokArray);
                                } catch (ENException e) {
                                    string line = "";
                                    foreach (string tk  in  tokArray)
                                        line += tk + " ";

                                    LogException(lastType, e.getCodeID(), line, tokArray);
                                    errSum++;
                                }
                            }
                        }
                    }

                    lastRowNull = false;
                    rowCount++;
                }

                if (row == null || row.PhysicalNumberOfCells == 0) {
                    lastRowNull = true;
                }

            }
        }
        return errSum;
    }

    private void ParseSect(Network net, Network.SectType type, string comments, string[] tokens) {
        switch (type) {

            case Network.SectType.TITLE:
                break;
            case Network.SectType.JUNCTIONS:
                this.ParseJunction(net, tokens, comments);
                break;
            case Network.SectType.RESERVOIRS:
            case Network.SectType.TANKS:
                this.ParseTank(net, tokens, comments);
                break;
            case Network.SectType.PIPES:
                this.ParsePipe(net, tokens, comments);
                break;
            case Network.SectType.PUMPS:
                this.ParsePump(net, tokens, comments);
                break;
            case Network.SectType.VALVES:
                this.ParseValve(net, tokens, comments);
                break;
            case Network.SectType.CONTROLS:
                this.ParseControl(net, tokens);
                break;
            case Network.SectType.RULES: {
                string line = "";
                foreach (string t  in  tokens)
                    line += t;
                this.ParseRule(net, tokens, line);
                break;
            }
            case Network.SectType.DEMANDS:
                this.ParseDemand(net, tokens);
                break;
            case Network.SectType.SOURCES:
                this.ParseSource(net, tokens);
                break;
            case Network.SectType.EMITTERS:
                this.ParseEmitter(net, tokens);
                break;
            case Network.SectType.PATTERNS:
                this.ParsePattern(net, tokens);
                break;
            case Network.SectType.CURVES:
                this.ParseCurve(net, tokens);
                break;
            case Network.SectType.QUALITY:
                this.ParseQuality(net, tokens);
                break;
            case Network.SectType.STATUS:
                this.ParseStatus(net, tokens);
                break;
            case Network.SectType.ROUGHNESS:
                break;
            case Network.SectType.ENERGY:
                this.ParseEnergy(net, tokens);
                break;
            case Network.SectType.REACTIONS:
                this.ParseReact(net, tokens);
                break;
            case Network.SectType.MIXING:
                this.ParseMixing(net, tokens);
                break;
            case Network.SectType.REPORT:
                this.ParseReport(net, tokens);
                break;
            case Network.SectType.TIMES:
                this.ParseTime(net, tokens);
                break;
            case Network.SectType.OPTIONS:
                this.ParseOption(net, tokens);
                break;
            case Network.SectType.COORDINATES:
                this.ParseCoordinate(net, tokens);
                break;
            case Network.SectType.VERTICES:
                this.ParseVertice(net, tokens);
                break;
            case Network.SectType.LABELS:
                this.ParseLabel(net, tokens);
                break;
            case Network.SectType.BACKDROP:
                break;
            case Network.SectType.TAGS:
                break;
            case Network.SectType.END:
                break;
        }
    }

}
}