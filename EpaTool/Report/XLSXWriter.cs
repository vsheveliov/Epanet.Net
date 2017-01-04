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
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Xml;

namespace Epanet.Report {
    /// <summary>
    /// See also http://msdn.microsoft.com/en-us/library/documentformat.openxml.spreadsheet(v=office.14).aspx
    /// </summary>
    internal sealed class XLSXWriter : IDisposable {
        private const string SCHEMA_MAIN = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                                            
        private const string RELATIONSHIP_ROOT = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private const string NS_ROOT = "application/vnd.openxmlformats-officedocument.spreadsheetml";

        private const string NS_WORKBOOK = NS_ROOT + ".sheet.main+xml";
        private const string RELATIONSHIP_WORKBOOK = RELATIONSHIP_ROOT + "/officeDocument";

        private const string NS_WORKSHEET = NS_ROOT + ".worksheet+xml";
        private const string RELATIONSHIP_WORKSHEET = RELATIONSHIP_ROOT + "/worksheet";

        private const string NS_SHARED_STRINGS = NS_ROOT + ".sharedStrings+xml";
        private const string NS_STYLESHEET    = NS_ROOT + ".styles+xml";

        private const string RELATIONSHIP_SHAREDSTRINGS = RELATIONSHIP_ROOT + "/sharedStrings";
        private const string RELATIONSHIP_STYLESHEET = RELATIONSHIP_ROOT + "/styles";

        const int EXCEL_MAX_COLUMNS = 16384;

        private const bool PRESERVE_WHITESPACE = false;

        private readonly Dictionary<string, int> sharedStrings = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<Spreadsheet> sheets = new List<Spreadsheet>();

        private static readonly XmlWriterSettings XmlSettings = new XmlWriterSettings {
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = "\t",
            CloseOutput = true
        };

        public bool TransposedMode { get; set; }

        private void WriteWorksheet(Package package, int sheetIndex) {
            string sPos = (sheetIndex + 1).ToString(NumberFormatInfo.InvariantInfo);
            string name = "xl/worksheets/sheet" + sPos + ".xml";
            Uri uri = PackUriHelper.CreatePartUri(new Uri(name, UriKind.Relative));
            PackagePart wsPart = package.CreatePart(uri, NS_WORKSHEET, CompressionOption.Maximum);
           
            this.sheets[sheetIndex].Save(wsPart.GetStream(FileMode.Create, FileAccess.Write));
            
            //Create the relationship for the workbook part.
            Uri bookUri = PackUriHelper.CreatePartUri(new Uri("xl/workbook.xml", UriKind.Relative));
            PackagePart bookPart = package.GetPart(bookUri);
            bookPart.CreateRelationship(uri, TargetMode.Internal, RELATIONSHIP_WORKSHEET, "rId" + sPos);
        }

        private void WriteStyleSheetXml(Package package) {

            Uri uri = PackUriHelper.CreatePartUri(new Uri("xl/styles.xml", UriKind.Relative));

            //Create the workbook part.
            PackagePart part = package.CreatePart(uri, NS_STYLESHEET, CompressionOption.Maximum);

            using (var writer = XmlWriter.Create(part.GetStream(FileMode.Create, FileAccess.Write), XmlSettings)) {
                writer.WriteStartDocument(true);

                writer.WriteStartElement("styleSheet", SCHEMA_MAIN);
                writer.WriteStartElement("fonts");
                writer.WriteAttributeString("count", "2");
                    
                writer.WriteStartElement("font");
                writer.WriteEndElement();

                writer.WriteStartElement("font");
                writer.WriteStartElement("b");
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteEndElement(); // fonts

                writer.WriteStartElement("fills");
                writer.WriteAttributeString("count", "1");
                writer.WriteStartElement("fill");
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("borders");
                writer.WriteAttributeString("count", "1");
                writer.WriteStartElement("border");
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("cellStyleXfs");
                writer.WriteAttributeString("count", "1");
                writer.WriteStartElement("xf");
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("cellXfs");
                writer.WriteAttributeString("count", "2");
                writer.WriteStartElement("xf");
                writer.WriteEndElement();
                writer.WriteStartElement("xf");
                writer.WriteAttributeString("fontId", "1");
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteEndElement(); // styleSheet
            }

            //Create the relationship for the workbook part.
            Uri bookUri = PackUriHelper.CreatePartUri(new Uri("xl/workbook.xml", UriKind.Relative));
            PackagePart bookPart = package.GetPart(bookUri);
            bookPart.CreateRelationship(uri, TargetMode.Internal, RELATIONSHIP_STYLESHEET, "rId" + (this.sheets.Count + 2));

        }

        private void WriteSharedStringsXml(Package package) {
            int count = this.sheets.Sum(x => x.WordCount);

            Uri uri = PackUriHelper.CreatePartUri(new Uri("xl/sharedStrings.xml", UriKind.Relative));

            //Create the workbook part.
            PackagePart part = package.CreatePart(uri, NS_SHARED_STRINGS, CompressionOption.Maximum);

            using (var writer = new XmlTextWriter(part.GetStream(FileMode.Create, FileAccess.Write), Encoding.UTF8)) {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument(true);

                writer.WriteStartElement("sst");
                writer.WriteAttributeString("count", count.ToString(NumberFormatInfo.InvariantInfo));
                writer.WriteAttributeString(
                    "uniqueCount",
                    this.sharedStrings.Count.ToString(NumberFormatInfo.InvariantInfo));
                writer.WriteAttributeString("xmlns", SCHEMA_MAIN);


                foreach (var s in this.sharedStrings.OrderBy(x => x.Value)) {
                    writer.WriteStartElement("si");
                    writer.WriteStartElement("t");
                    writer.WriteString(s.Key);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

            }

            //Create the relationship for the workbook part.
            Uri bookUri = PackUriHelper.CreatePartUri(new Uri("xl/workbook.xml", UriKind.Relative));
            PackagePart bookPart = package.GetPart(bookUri);
            bookPart.CreateRelationship(uri, TargetMode.Internal, RELATIONSHIP_SHAREDSTRINGS, "rId" + (this.sheets.Count + 1));
        }

        private void WriteWorkbookXml(Package package) {

            Uri uri = PackUriHelper.CreatePartUri(new Uri("xl/workbook.xml", UriKind.Relative));
            var part = package.CreatePart(uri, NS_WORKBOOK, CompressionOption.Maximum);

            using(XmlWriter writer = XmlWriter.Create(part.GetStream(FileMode.Create, FileAccess.Write), XmlSettings)) {
                writer.WriteStartDocument(true);

                writer.WriteStartElement("workbook", SCHEMA_MAIN);
                writer.WriteAttributeString("xmlns", "r", null, RELATIONSHIP_ROOT);

                writer.WriteStartElement("sheets");

                for(int i = 1; i <= this.sheets.Count; i++) {
                    string sid = i.ToString(NumberFormatInfo.InvariantInfo);

                    //Create and append the sheet node to the sheets node.
                    writer.WriteStartElement("sheet");
                    writer.WriteAttributeString("name", this.sheets[i - 1].Name);
                    writer.WriteAttributeString("sheetId", sid);
                    writer.WriteAttributeString("r", "id", null, "rId" + sid);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

            }
            
            package.CreateRelationship(uri, TargetMode.Internal, RELATIONSHIP_WORKBOOK, "rId1");
        }

        public void Save(string outputFile) {


            using(var package = Package.Open(outputFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None)) {

                this.WriteWorkbookXml(package);
                this.WriteSharedStringsXml(package);
                this.WriteStyleSheetXml(package);

                for (int i = 0; i < this.sheets.Count; i++)
                    this.WriteWorksheet(package, i);

            }

        }

        public Spreadsheet NewSpreadsheet(string name) {
            var spreadsheet = new Spreadsheet(name, this.TransposedMode, this.sharedStrings);
            this.sheets.Add(spreadsheet);
            return spreadsheet;
        }

        public void Dispose() {
            this.sheets.Clear();
            GC.Collect();
        }

        private static string GetColumnName(int columnNumber) {
            if(columnNumber > EXCEL_MAX_COLUMNS || columnNumber < 0)
                throw new ArgumentOutOfRangeException("columnNumber");

            string columnName = string.Empty;

            for(int dividend = columnNumber; dividend > 0; ) {
                int modulo = (dividend - 1) % 26;
                columnName = (char)(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }

        // Note: memory hungry implementation
        public class Spreadsheet {
            private readonly string name;
            private readonly bool transposedMode;
            private int _rowsAdded = 1;
            private readonly XmlDocument xml;
            private readonly XmlElement sheetData;
            private readonly Dictionary<string, int> sharedStrings;

            internal Spreadsheet(string name, bool transposedMode, Dictionary<string, int> sharedStrings) {
                this.name = name;
                this.transposedMode = transposedMode;
                this.sharedStrings = sharedStrings;
                this.xml = new XmlDocument { PreserveWhitespace = PRESERVE_WHITESPACE };
                

                // Get a reference to the root node, and then add the XML declaration.
                XmlElement wsRoot = this.xml.DocumentElement;
                XmlDeclaration wsxmldecl = this.xml.CreateXmlDeclaration("1.0", "UTF-8", "yes");
                this.xml.InsertBefore(wsxmldecl, wsRoot);

                //Create and append the worksheet node to the document.
                var workSheet = this.xml.CreateElement("worksheet");
                workSheet.SetAttribute("xmlns", SCHEMA_MAIN);
                workSheet.SetAttribute("xmlns:r", RELATIONSHIP_ROOT);
                this.xml.AppendChild(workSheet);

                //Create and add the sheetData node.
                sheetData = (XmlElement)workSheet.AppendChild(this.xml.CreateElement("sheetData"));
            }

            public void Save(Stream st) { this.xml.Save(st); }

            public int WordCount { get; private set; }
            public string Name { get { return this.name; } }

            public void AddData(params object[] row) {
                if(this.transposedMode)
                    this.AddColumn(false, row);
                else
                    this.AddRow(false, row);

                this._rowsAdded++;
            }

            public void AddHeader(params object[] row) {
                if(this.transposedMode)
                    this.AddColumn(true, row);
                else
                    this.AddRow(true, row);

                this._rowsAdded++;
            }

            private void AddRow(bool bold, object[] row) {
                string rowName = this._rowsAdded.ToString(NumberFormatInfo.InvariantInfo);
                //Create and add the row node. 
                XmlElement rNode = this.xml.CreateElement("row");
                rNode.SetAttribute("r", rowName);
                rNode.SetAttribute("spans", "1:" + row.Length.ToString(NumberFormatInfo.InvariantInfo));

                // XmlElement sheetData = this.xml["worksheet"]["sheetData"];
                sheetData.AppendChild(rNode);

                int col = 0;

                foreach(object o in row) {
                    string cellAddr = GetColumnName(col + 1) + rowName;
                    this.AddCell(rNode, o, cellAddr, bold);

                    col++;
                }
            }

            private void AddColumn(bool bold, object[] row) {
                // XmlElement sheetData = this.xml["worksheet"]["sheetData"];
                XmlNodeList rows = sheetData.GetElementsByTagName("row");

                if(rows.Count < row.Length) {
                    for(int i = rows.Count; i < row.Length; i++) {
                        XmlElement rNode = this.xml.CreateElement("row");
                        rNode.SetAttribute("r", (i + 1).ToString(NumberFormatInfo.InvariantInfo));
                        if(bold) rNode.SetAttribute("s", "1");

                        // rNode.SetAttribute("spans", "1:" + colspan);
                        sheetData.AppendChild(rNode);
                    }

                    rows = sheetData.GetElementsByTagName("row");
                }

                int iRow = 1;
                string columnName = GetColumnName(this._rowsAdded);

                foreach(object o in row) {
                    XmlNode rNode = rows[iRow - 1];
                    string cellAddr = columnName + iRow.ToString(NumberFormatInfo.InvariantInfo);

                    this.AddCell(rNode, o, cellAddr, bold);

                    iRow++;
                }

            }


            private static string NumberToStringInvariant(object value) {
                // if (!(value is ValueType)) return false;

                var v = value as IConvertible;
                if (v == null)
                    return null;

                switch (v.GetTypeCode()) {
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
                    return v.ToString(NumberFormatInfo.InvariantInfo);

                default:
                    return null;
                }
            }

            private void AddCell(XmlNode rNode, object o, string cellAddr, bool bold) {

                string dataType;
                string dataValue = NumberToStringInvariant(o);

                if(!string.IsNullOrEmpty(dataValue)) {
                    dataType = "n";
                }
                else {   
                    dataType = "s";
                    string s = o == null ? string.Empty : o.ToString();

                    if (string.IsNullOrEmpty(s)) return;

                    int idx;

                    lock (this.sharedStrings) {
                        if (!this.sharedStrings.TryGetValue(s, out idx)) {
                            idx = this.sharedStrings.Count;
                            this.sharedStrings.Add(s, idx);
                        }
                    }

                    this.WordCount++;

                    dataValue = idx.ToString(NumberFormatInfo.InvariantInfo);
                }

                //Create and add the column node.
                XmlElement cNode = this.xml.CreateElement("c");

                cNode.SetAttribute("r", cellAddr);
                if(bold) cNode.SetAttribute("s", "1");
                cNode.SetAttribute("t", dataType);
               

                rNode.AppendChild(cNode);
                
                //Add the dataValue text to the worksheet.
                XmlElement vNode = this.xml.CreateElement("v");
                vNode.InnerText = dataValue;
                cNode.AppendChild(vNode);
               

            }

        }

        public Spreadsheet this[int i] {
            get { return this.sheets[i]; }
            set { this.sheets[i] = value; }
        }
    }

}
