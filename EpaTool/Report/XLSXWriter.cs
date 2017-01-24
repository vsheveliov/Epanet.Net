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
    internal sealed class XLSXWriter:IDisposable {
        private const string SCHEMA_MAIN = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        private const string RELATIONSHIP_ROOT = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private const string NS_ROOT = "application/vnd.openxmlformats-officedocument.spreadsheetml";

        private const string NS_WORKBOOK = NS_ROOT + ".sheet.main+xml";
        private const string RELATIONSHIP_WORKBOOK = RELATIONSHIP_ROOT + "/officeDocument";

        private const string NS_WORKSHEET = NS_ROOT + ".worksheet+xml";
        private const string RELATIONSHIP_WORKSHEET = RELATIONSHIP_ROOT + "/worksheet";

        private const string NS_SHARED_STRINGS = NS_ROOT + ".sharedStrings+xml";
        private const string NS_STYLESHEET = NS_ROOT + ".styles+xml";

        private const string RELATIONSHIP_SHAREDSTRINGS = RELATIONSHIP_ROOT + "/sharedStrings";
        private const string RELATIONSHIP_STYLESHEET = RELATIONSHIP_ROOT + "/styles";

        private const int EXCEL_MAX_COLUMNS = 0x4000;

        private const bool PRESERVE_WHITESPACE = true;

        private static readonly XmlWriterSettings XmlSettings = new XmlWriterSettings {
            Encoding = Encoding.UTF8,
            Indent = PRESERVE_WHITESPACE,
            IndentChars = "\t",
            CloseOutput = true
        };

        private readonly Dictionary<string, int> sharedStrings = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<Spreadsheet> sheets = new List<Spreadsheet>();

        public bool TransposedMode { get; set; }

        public Spreadsheet this[int i] {
            get { return this.sheets[i]; }
            set { this.sheets[i] = value; }
        }

        public void Dispose() {
            this.sheets.Clear();
            GC.Collect();
        }

        private void WriteWorksheet(Package package, int sheetIndex) {
            string sPos = (sheetIndex + 1).ToString(NumberFormatInfo.InvariantInfo);
            string name = string.Format("xl/worksheets/sheet{0}.xml", sPos);
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
            bookPart.CreateRelationship(
                uri,
                TargetMode.Internal,
                RELATIONSHIP_STYLESHEET,
                "rId" + (this.sheets.Count + 2));
        }

        private void WriteSharedStringsXml(Package package) {
            int count = this.sheets.Sum(x => x.WordCount);

            Uri uri = PackUriHelper.CreatePartUri(new Uri("xl/sharedStrings.xml", UriKind.Relative));

            //Create the workbook part.
            PackagePart part = package.CreatePart(uri, NS_SHARED_STRINGS, CompressionOption.Maximum);

            using (var writer = XmlWriter.Create(part.GetStream(FileMode.Create, FileAccess.Write), XmlSettings)) {
                writer.WriteStartDocument(true);

                writer.WriteStartElement("sst", SCHEMA_MAIN);
                writer.WriteAttributeString("count", count.ToString(NumberFormatInfo.InvariantInfo));
                writer.WriteAttributeString(
                    "uniqueCount",
                    this.sharedStrings.Count.ToString(NumberFormatInfo.InvariantInfo));
                // writer.WriteAttributeString("xmlns", SCHEMA_MAIN);

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
            bookPart.CreateRelationship(
                uri,
                TargetMode.Internal,
                RELATIONSHIP_SHAREDSTRINGS,
                "rId" + (this.sheets.Count + 1));
        }

        private void WriteWorkbookXml(Package package) {
            Uri uri = PackUriHelper.CreatePartUri(new Uri("xl/workbook.xml", UriKind.Relative));
            // var part = package.CreatePart(uri, NS_WORKBOOK, CompressionOption.Maximum);
            var part = package.CreatePart(uri, "application/xml", CompressionOption.Normal);

            using (XmlWriter writer = XmlWriter.Create(part.GetStream(FileMode.Create, FileAccess.Write), XmlSettings)) {
                writer.WriteStartDocument(true);

                writer.WriteStartElement("workbook", SCHEMA_MAIN);
                writer.WriteAttributeString("xmlns", "r", null, RELATIONSHIP_ROOT);

                writer.WriteStartElement("bookViews");
                writer.WriteStartElement("workbookView");
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("sheets");

                for (int i = 1; i <= this.sheets.Count; i++) {
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
            using (var package = Package.Open(outputFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None)) {
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

        // Note: memory hungry implementation
        public class Spreadsheet {
            private readonly string name;
            private readonly bool transposedMode;
            private readonly XmlDocument xmlDoc;
            private readonly XmlElement sheetData;
            private readonly Dictionary<string, int> sharedStrings;
            private int _rowsAdded = 1;

            internal Spreadsheet(string name, bool transposedMode, Dictionary<string, int> sharedStrings) {
                this.name = name;
                this.transposedMode = transposedMode;
                this.sharedStrings = sharedStrings;

                this.xmlDoc = new XmlDocument {
                    PreserveWhitespace = PRESERVE_WHITESPACE
                };

                // Get a reference to the root node, and then add the XML declaration.
                XmlElement wsRoot = this.xmlDoc.DocumentElement;
                XmlDeclaration wsxmldecl = this.xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
                this.xmlDoc.InsertBefore(wsxmldecl, wsRoot);

                //Create and append the worksheet node to the document.
                var workSheet = this.xmlDoc.CreateElement("worksheet");
                workSheet.SetAttribute("xmlns", SCHEMA_MAIN);
                workSheet.SetAttribute("xmlns:r", RELATIONSHIP_ROOT);
                this.xmlDoc.AppendChild(workSheet);

                var sheetViews = (XmlElement)workSheet.AppendChild(this.xmlDoc.CreateElement("sheetViews"));
                var sheetView = (XmlElement)sheetViews.AppendChild(this.xmlDoc.CreateElement("sheetView"));
                sheetView.SetAttribute("workbookViewId", "0");

                var pane = (XmlElement)sheetView.AppendChild(this.xmlDoc.CreateElement("pane"));
                pane.SetAttribute("xSplit", "1");
                pane.SetAttribute("ySplit", "1");
                pane.SetAttribute("topLeftCell", "B2");
                pane.SetAttribute("state", "frozen");

                //Create and add the sheetData node.
                this.sheetData = (XmlElement)workSheet.AppendChild(this.xmlDoc.CreateElement("sheetData"));
            }

            public int WordCount { get; private set; }
            public string Name {
                get { return this.name; }
            }

            public void Save(Stream st) { this.xmlDoc.Save(st); }

            public void AddData(params object[] row) {
                if (this.transposedMode)
                    this.AddColumn(false, row);
                else
                    this.AddRow(false, row);

                this._rowsAdded++;
            }

            public void AddHeader(params object[] row) {
                if (this.transposedMode)
                    this.AddColumn(true, row);
                else
                    this.AddRow(true, row);

                this._rowsAdded++;
            }

            private static string GetColumnName(int columnNumber) {
                if ((columnNumber > EXCEL_MAX_COLUMNS) || (columnNumber < 0))
                    throw new ArgumentOutOfRangeException("columnNumber");

                string columnName = string.Empty;

                for (int dividend = columnNumber; dividend > 0;) {
                    int modulo = (dividend - 1) % 26;
                    columnName = (char)(65 + modulo) + columnName;
                    dividend = (dividend - modulo) / 26;
                }

                return columnName;
            }

            private void AddRow(bool bold, object[] row) {
                string rowName = this._rowsAdded.ToString(NumberFormatInfo.InvariantInfo);
                //Create and add the row node. 
                XmlElement rowNode = this.xmlDoc.CreateElement("row");
                rowNode.SetAttribute("r", rowName);
                rowNode.SetAttribute("spans", "1:" + row.Length.ToString(NumberFormatInfo.InvariantInfo));

                // XmlElement sheetData = this.xml["worksheet"]["sheetData"];

                for (int i = 0; i < row.Length; i++) {
                    object o = row[i];
                    string cellAddr = GetColumnName(i + 1) + rowName;
                    this.AddCell(rowNode, o, cellAddr, bold);
                }

                this.sheetData.AppendChild(rowNode);
            }

            private void AddColumn(bool bold, object[] row) {
                // XmlElement sheetData = this.xml["worksheet"]["sheetData"];
                XmlNodeList rows = this.sheetData.GetElementsByTagName("row");

                if (rows.Count < row.Length) {
                    for (int i = rows.Count; i < row.Length; i++) {
                        XmlElement rowNode = this.xmlDoc.CreateElement("row");
                        rowNode.SetAttribute("r", (i + 1).ToString(NumberFormatInfo.InvariantInfo));
                        if (bold) rowNode.SetAttribute("s", "1");

                        // rNode.SetAttribute("spans", "1:" + colspan);
                        this.sheetData.AppendChild(rowNode);
                    }

                    rows = this.sheetData.GetElementsByTagName("row");
                }

                int iRow = 1;
                string columnName = GetColumnName(this._rowsAdded);

                foreach (object o in row) {
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

                if (!string.IsNullOrEmpty(dataValue)) {
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
                XmlElement cNode = this.xmlDoc.CreateElement("c");

                cNode.SetAttribute("r", cellAddr);
                if (bold) cNode.SetAttribute("s", "1");
                cNode.SetAttribute("t", dataType);

                rNode.AppendChild(cNode);

                //Add the dataValue text to the worksheet.
                XmlElement vNode = this.xmlDoc.CreateElement("v");
                vNode.InnerText = dataValue;
                cNode.AppendChild(vNode);
            }
        }

        /*

        private void ExportDataSet(DataSet ds, string destination) {
            using (
                var workbook = SpreadsheetDocument.Create(
                    destination,
                    DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook)) {
                var workbookPart = workbook.AddWorkbookPart();
                workbook.WorkbookPart.Workbook = new DocumentFormat.OpenXml.Spreadsheet.Workbook();
                workbook.WorkbookPart.Workbook.Sheets = new DocumentFormat.OpenXml.Spreadsheet.Sheets();
                foreach (System.Data.DataTable table in ds.Tables) {
                    var sheetPart = workbook.WorkbookPart.AddNewPart<WorksheetPart>();
                    var sheetData = new DocumentFormat.OpenXml.Spreadsheet.SheetData();
                    sheetPart.Worksheet = new DocumentFormat.OpenXml.Spreadsheet.Worksheet(sheetData);
                
                    DocumentFormat.OpenXml.Spreadsheet.Sheets sheets =
                        workbook.WorkbookPart.Workbook.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.Sheets>();
                    string relationshipId = workbook.WorkbookPart.GetIdOfPart(sheetPart);
                    uint sheetId = 1;
                   
                    if (sheets.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>().Count() > 0) {
                        sheetId =
                            sheets.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>()
                                  .Select(s => s.SheetId.Value)
                                  .Max() + 1;
                    }

                    DocumentFormat.OpenXml.Spreadsheet.Sheet sheet = new DocumentFormat.OpenXml.Spreadsheet.Sheet() {
                        Id = relationshipId,
                        SheetId = sheetId,
                        Name = table.TableName
                    };

                    sheets.Append(sheet);
                    DocumentFormat.OpenXml.Spreadsheet.Row headerRow = new DocumentFormat.OpenXml.Spreadsheet.Row();
                    List<String> columns = new List<string>();
                    foreach (System.Data.DataColumn column in table.Columns) {
                        columns.Add(column.ColumnName);
                        DocumentFormat.OpenXml.Spreadsheet.Cell cell = new DocumentFormat.OpenXml.Spreadsheet.Cell();
                        cell.DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String;
                        cell.CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(column.ColumnName);
                        headerRow.AppendChild(cell);
                    }

                    sheetData.AppendChild(headerRow);
                    foreach (System.Data.DataRow dsrow in table.Rows) {
                        DocumentFormat.OpenXml.Spreadsheet.Row newRow = new DocumentFormat.OpenXml.Spreadsheet.Row();
                        foreach (String col in columns) {
                            DocumentFormat.OpenXml.Spreadsheet.Cell cell = new DocumentFormat.OpenXml.Spreadsheet.Cell();
                            cell.DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String;
                            cell.CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(dsrow[col].ToString());
                            //                             newRow.AppendChild(cell);    
                        }

                        sheetData.AppendChild(newRow);
                    }
                }
            }
        }

        */
    }

}
