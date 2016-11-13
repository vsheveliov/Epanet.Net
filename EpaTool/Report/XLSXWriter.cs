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
using System.Xml;

namespace EpaTool.Report {
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
        private const string RELATIONSHIP_SHAREDSTRINGS = RELATIONSHIP_ROOT + "/sharedStrings";

        const int EXCEL_MAX_COLUMNS = 16384;

        private const bool PRESERVE_WHITESPACE = false;

        private readonly Dictionary<string, int> _sharedStrings = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<Spreadsheet> _sheets = new List<Spreadsheet>();
        private Package _package;
        public bool TransposedMode { get; set; }

        private void WriteWorksheet(Spreadsheet sheet, int pos) {
            string sPos = pos.ToString(NumberFormatInfo.InvariantInfo);
            string name = string.Format("xl/worksheets/sheet{0}.xml", sPos);
            Uri uri = PackUriHelper.CreatePartUri(new Uri(name, UriKind.Relative));
            PackagePart wsPart = this._package.CreatePart(uri, NS_WORKSHEET, CompressionOption.Maximum);
            sheet.Save(wsPart.GetStream(FileMode.Create, FileAccess.Write));
            //Create the relationship for the workbook part.
            Uri bookUri = PackUriHelper.CreatePartUri(new Uri("xl/workbook.xml", UriKind.Relative));
            PackagePart bookPart = this._package.GetPart(bookUri);
            bookPart.CreateRelationship(uri, TargetMode.Internal, RELATIONSHIP_WORKSHEET, "rId" + sPos);
        }

        private void WriteSharedStringsXml() {
            int count = 0;
            
            foreach (var x in this._sheets) {
                count += x.WordCount;
            }


            //Create a new XML document for the sharedStrings.
            XmlDocument sharedStringsDoc = new XmlDocument { PreserveWhitespace = PRESERVE_WHITESPACE };

            //Get a reference to the root node, and then add the XML declaration.
            XmlElement ssRoot = sharedStringsDoc.DocumentElement;
            XmlDeclaration ssxmldecl = sharedStringsDoc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            sharedStringsDoc.InsertBefore(ssxmldecl, ssRoot);

            //Create and append the sst node.
            XmlElement sstNode = sharedStringsDoc.CreateElement("sst");
            sstNode.SetAttribute("xmlns", SCHEMA_MAIN);
            sstNode.SetAttribute("count", count.ToString(NumberFormatInfo.InvariantInfo));
            sstNode.SetAttribute("uniqueCount", this._sharedStrings.Count.ToString(NumberFormatInfo.InvariantInfo));
            sharedStringsDoc.AppendChild(sstNode);

            var words = new List<KeyValuePair<string,int>>(this._sharedStrings);
            words.Sort((a,b) => a.Value.CompareTo(b.Value));

            foreach (KeyValuePair<string, int> kvp in words) {
                
                //Create and append the si node.
                XmlElement siNode = sharedStringsDoc.CreateElement("si");
                sstNode.AppendChild(siNode);

                //Create and append the t node.
                XmlElement tNode = sharedStringsDoc.CreateElement("t");
                tNode.InnerText = kvp.Key;
                siNode.AppendChild(tNode);
            }

            Uri uri = PackUriHelper.CreatePartUri(new Uri("xl/sharedStrings.xml", UriKind.Relative));

            //Create the workbook part.
            PackagePart part = this._package.CreatePart(uri, NS_SHARED_STRINGS, CompressionOption.Maximum);

            //Write the workbook XML to the workbook part.
            sharedStringsDoc.Save(part.GetStream(FileMode.Create, FileAccess.Write));

            //Create the relationship for the workbook part.
            Uri bookUri = PackUriHelper.CreatePartUri(new Uri("xl/workbook.xml", UriKind.Relative));
            PackagePart bookPart = this._package.GetPart(bookUri);
            bookPart.CreateRelationship(uri, TargetMode.Internal, RELATIONSHIP_SHAREDSTRINGS, "rId" + (this._sheets.Count + 2));
        }


        private void WriteWorkbookXml() {

            //Create a new XML document for the workbook.
            var workbookDoc = new XmlDocument { PreserveWhitespace = PRESERVE_WHITESPACE };

            //Obtain a reference to the root node, and then add the XML declaration.
            XmlElement wbRoot = workbookDoc.DocumentElement;
            XmlDeclaration wbxmldecl = workbookDoc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            workbookDoc.InsertBefore(wbxmldecl, wbRoot);

            //Create and append the workbook node to the document.
            XmlElement workBook = workbookDoc.CreateElement("workbook");
            workBook.SetAttribute("xmlns", SCHEMA_MAIN);
            workBook.SetAttribute("xmlns:r", RELATIONSHIP_ROOT);
            workbookDoc.AppendChild(workBook);
            
            //Create and append the sheets node to the workBook node.
            XmlElement sheets = workbookDoc.CreateElement("sheets");
            workBook.AppendChild(sheets);

            for (int i = 1; i <= this._sheets.Count; i++) {
                string sid = i.ToString(NumberFormatInfo.InvariantInfo);
                //Create and append the sheet node to the sheets node.
                XmlElement sheet = workbookDoc.CreateElement("sheet");
                sheet.SetAttribute("name", this._sheets[i - 1].Name);
                sheet.SetAttribute("sheetId", sid);
                sheet.SetAttribute("id", RELATIONSHIP_ROOT, "rId" + sid);
                sheets.AppendChild(sheet);
            }

            Uri uri = PackUriHelper.CreatePartUri(new Uri("xl/workbook.xml", UriKind.Relative));
            var part = this._package.CreatePart(uri, NS_WORKBOOK, CompressionOption.Maximum);
            workbookDoc.Save(part.GetStream(FileMode.Create, FileAccess.Write));
     
            this._package.CreateRelationship(uri, TargetMode.Internal, RELATIONSHIP_WORKBOOK, "rId1" );
        }

        public void Save(string outputFile) {
            this._package = Package.Open(outputFile, FileMode.Create);
            
            this.WriteWorkbookXml();
            this.WriteSharedStringsXml();

            for (int i = 0; i < this._sheets.Count; i++) {
                this.WriteWorksheet(this._sheets[i], i + 1);
            }

            this._package.Close();
            this._package = null;
        }

        public Spreadsheet NewSpreadsheet(string name) {
            var spreadsheet = new Spreadsheet(name, this.TransposedMode, this._sharedStrings);
            this._sheets.Add(spreadsheet);
            return spreadsheet;
        }

        public void Dispose() {
            if (this._package != null) {
                this._package.Close();
                this._package = null;
            }

            this._sheets.Clear();
        }

        private static string GetColumnName(int columnNumber) {
            if (columnNumber > EXCEL_MAX_COLUMNS || columnNumber < 0)
                throw new ArgumentOutOfRangeException("columnNumber");
           
            string columnName = string.Empty;
                       
            for (int dividend = columnNumber; dividend > 0; ) {
                int modulo = (dividend - 1) % 26;
                columnName = (char)(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }

        // Note: memory hungry implementation
        public class Spreadsheet {
            private readonly string _name;
            private readonly bool _transposedMode;
            private int _rowsAdded = 1;
            private readonly XmlDocument _xml;
            private readonly Dictionary<string, int> _sharedStrings;

            internal Spreadsheet(string name, bool transposedMode, Dictionary<string, int> sharedStrings) {
                this._name = name;
                this._transposedMode = transposedMode;
                this._sharedStrings = sharedStrings;
                this._xml = new XmlDocument { PreserveWhitespace = PRESERVE_WHITESPACE };
                
                // Get a reference to the root node, and then add the XML declaration.
                XmlElement wsRoot = this._xml.DocumentElement;
                XmlDeclaration wsxmldecl = this._xml.CreateXmlDeclaration("1.0", "UTF-8", "yes");
                this._xml.InsertBefore(wsxmldecl, wsRoot);

                //Create and append the worksheet node to the document.
                XmlElement workSheet = this._xml.CreateElement("worksheet");
                workSheet.SetAttribute("xmlns", SCHEMA_MAIN);
                workSheet.SetAttribute("xmlns:r", RELATIONSHIP_ROOT);
                this._xml.AppendChild(workSheet);

                //Create and add the sheetData node.
                XmlElement sheetData = this._xml.CreateElement("sheetData");
                workSheet.AppendChild(sheetData);
            }

            public void Save(Stream st) { this._xml.Save(st); }

            public int WordCount { get; private set; }
            public string Name { get { return this._name; } }

            public void AddData(params object[] row) {
                if (this._transposedMode)
                    this.AddColumn(row);
                else
                    this.AddRow(row);

                this._rowsAdded++;
            }

            private void AddRow(params object[] row) {
                string rowName = this._rowsAdded.ToString(NumberFormatInfo.InvariantInfo);
                //Create and add the row node. 
                XmlElement rNode = this._xml.CreateElement("row");
                rNode.SetAttribute("r", rowName);
                rNode.SetAttribute("spans", "1:" + row.Length.ToString(NumberFormatInfo.InvariantInfo));

                XmlElement sheetData = this._xml["worksheet"]["sheetData"];
                sheetData.AppendChild(rNode);

                int col = 0;

                foreach (object o in row) {
                    string cellAddr = GetColumnName(col + 1) + rowName;
                    this.AddCell(rNode, o, cellAddr);

                    col++;
                }
            }

            private void AddColumn(params object[] row) {
                XmlElement sheetData = this._xml["worksheet"]["sheetData"];
                XmlNodeList rows = sheetData.GetElementsByTagName("row");

                if (rows.Count < row.Length) {
                    for (int i = rows.Count; i < row.Length; i++) {
                        XmlElement rNode = this._xml.CreateElement("row");
                        rNode.SetAttribute("r", (i+1).ToString(NumberFormatInfo.InvariantInfo));
                        // rNode.SetAttribute("spans", "1:" + colspan);
                        sheetData.AppendChild(rNode);
                    }

                    rows = sheetData.GetElementsByTagName("row");
                }

                int iRow = 1;
                string columnName = GetColumnName(this._rowsAdded);

                foreach (object o in row) {
                    XmlNode rNode = rows[iRow - 1];
                    string cellAddr = columnName + iRow.ToString(NumberFormatInfo.InvariantInfo);

                    this.AddCell(rNode, o, cellAddr);

                    iRow++;
                }

            }


            private static string NumberToStringInvariant(object value) {
                // if (!(value is ValueType)) return false;

                var v = value as IConvertible;
                if (v == null) return null;

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

            private void AddCell(XmlNode rNode, object o, string cellAddr) {
      
                string dataType;
                string dataValue = NumberToStringInvariant(o);

                if (!string.IsNullOrEmpty(dataValue)) {
                    dataType = "n";
                }
                else {
                    string s = o == null ? string.Empty : o.ToString();

                    int idx;

                    lock(this._sharedStrings) {
                        if (!this._sharedStrings.TryGetValue(s, out idx)) {
                            idx = this._sharedStrings.Count;
                            this._sharedStrings.Add(s, idx);
                        }
                    }

                    this.WordCount++;

                    dataType = "s";
                    dataValue = idx.ToString(NumberFormatInfo.InvariantInfo);
                }

                //Create and add the column node.
                XmlElement cNode = this._xml.CreateElement("c");
                
                cNode.SetAttribute("r", cellAddr);
                cNode.SetAttribute("t", dataType);
                rNode.AppendChild(cNode);

                //Add the dataValue text to the worksheet.
                XmlElement vNode = this._xml.CreateElement("v");
                vNode.InnerText = dataValue;
                cNode.AppendChild(vNode);

            }

        }

        public Spreadsheet this[int i] { 
            get { return this._sheets[i]; } 
            set { this._sheets[i] = value; } }
    }

}
