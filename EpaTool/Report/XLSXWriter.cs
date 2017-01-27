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
    internal sealed class XlsxWriter:IDisposable {
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

        private static readonly XmlWriterSettings xmlSettings = new XmlWriterSettings {
            Encoding = Encoding.UTF8,
            Indent = PRESERVE_WHITESPACE,
            IndentChars = "\t",
            CloseOutput = true
        };

        private readonly Dictionary<string, int> _sharedStrings = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<Spreadsheet> _sheets = new List<Spreadsheet>();

        public bool TransposedMode { get; set; }

        public Spreadsheet this[int i] {
            get { return _sheets[i]; }
            set { _sheets[i] = value; }
        }

        public void Dispose() {
            _sheets.Clear();
            GC.Collect();
        }

        private void WriteWorksheet(Package package, int sheetIndex) {
            string sPos = (sheetIndex + 1).ToString(NumberFormatInfo.InvariantInfo);
            string name = string.Format("xl/worksheets/sheet{0}.xml", sPos);
            Uri uri = PackUriHelper.CreatePartUri(new Uri(name, UriKind.Relative));
            PackagePart wsPart = package.CreatePart(uri, NS_WORKSHEET, CompressionOption.Maximum);

            _sheets[sheetIndex].Save(wsPart.GetStream(FileMode.Create, FileAccess.Write));

            //Create the relationship for the workbook part.
            Uri bookUri = PackUriHelper.CreatePartUri(new Uri("xl/workbook.xml", UriKind.Relative));
            PackagePart bookPart = package.GetPart(bookUri);
            bookPart.CreateRelationship(uri, TargetMode.Internal, RELATIONSHIP_WORKSHEET, "rId" + sPos);
        }

        private void WriteStyleSheetXml(Package package) {
            Uri uri = PackUriHelper.CreatePartUri(new Uri("xl/styles.xml", UriKind.Relative));

            //Create the workbook part.
            PackagePart part = package.CreatePart(uri, NS_STYLESHEET, CompressionOption.Maximum);

            using (var writer = XmlWriter.Create(part.GetStream(FileMode.Create, FileAccess.Write), xmlSettings)) {
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
                "rId" + (_sheets.Count + 2));
        }

        private void WriteSharedStringsXml(Package package) {
            int count = _sheets.Sum(x => x.WordCount);

            Uri uri = PackUriHelper.CreatePartUri(new Uri("xl/sharedStrings.xml", UriKind.Relative));

            //Create the workbook part.
            PackagePart part = package.CreatePart(uri, NS_SHARED_STRINGS, CompressionOption.Maximum);

            using (var writer = XmlWriter.Create(part.GetStream(FileMode.Create, FileAccess.Write), xmlSettings)) {
                writer.WriteStartDocument(true);

                writer.WriteStartElement("sst", SCHEMA_MAIN);
                writer.WriteAttributeString("count", count.ToString(NumberFormatInfo.InvariantInfo));
                writer.WriteAttributeString(
                    "uniqueCount",
                    _sharedStrings.Count.ToString(NumberFormatInfo.InvariantInfo));
                // writer.WriteAttributeString("xmlns", SCHEMA_MAIN);

                foreach (var s in _sharedStrings.OrderBy(x => x.Value)) {
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
                "rId" + (_sheets.Count + 1));
        }

        private void WriteWorkbookXml(Package package) {
            Uri uri = PackUriHelper.CreatePartUri(new Uri("xl/workbook.xml", UriKind.Relative));
            var part = package.CreatePart(uri, NS_WORKBOOK, CompressionOption.Maximum);
            // var part = package.CreatePart(uri, "application/xml", CompressionOption.Normal);

            using (XmlWriter writer = XmlWriter.Create(part.GetStream(FileMode.Create, FileAccess.Write), xmlSettings)) {
                writer.WriteStartDocument(true);

                writer.WriteStartElement("workbook", SCHEMA_MAIN);
                writer.WriteAttributeString("xmlns", "r", null, RELATIONSHIP_ROOT);

                writer.WriteStartElement("bookViews");
                writer.WriteStartElement("workbookView");
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("sheets");

                for (int i = 1; i <= _sheets.Count; i++) {
                    string sid = i.ToString(NumberFormatInfo.InvariantInfo);

                    //Create and append the sheet node to the sheets node.
                    writer.WriteStartElement("sheet");
                    writer.WriteAttributeString("name", _sheets[i - 1].Name);
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
                WriteWorkbookXml(package);
                WriteSharedStringsXml(package);
                WriteStyleSheetXml(package);

                for (int i = 0; i < _sheets.Count; i++)
                    WriteWorksheet(package, i);
            }
        }

        public Spreadsheet NewSpreadsheet(string name) {
            var spreadsheet = new Spreadsheet(name, TransposedMode, _sharedStrings);
            _sheets.Add(spreadsheet);
            return spreadsheet;
        }

        // Note: memory hungry implementation
        public class Spreadsheet {
            private readonly string _name;
            private readonly bool _transposedMode;
            private readonly XmlDocument _xmlDoc;
            private readonly XmlElement _sheetData;
            private readonly Dictionary<string, int> __sharedStrings;
            private int _rowsAdded = 1;

            internal Spreadsheet(string name, bool transposedMode, Dictionary<string, int> sharedStrings) {
                _name = name;
                _transposedMode = transposedMode;
                __sharedStrings = sharedStrings;

                _xmlDoc = new XmlDocument {
                    PreserveWhitespace = PRESERVE_WHITESPACE
                };

                // Get a reference to the root node, and then add the XML declaration.
                XmlElement wsRoot = _xmlDoc.DocumentElement;
                XmlDeclaration wsxmldecl = _xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
                _xmlDoc.InsertBefore(wsxmldecl, wsRoot);

                //Create and append the worksheet node to the document.
                var workSheet = _xmlDoc.CreateElement("worksheet", SCHEMA_MAIN);
                workSheet.SetAttribute("xmlns:r", RELATIONSHIP_ROOT);
                _xmlDoc.AppendChild(workSheet);

                var sheetViews = (XmlElement)workSheet.AppendChild(_xmlDoc.CreateElement("sheetViews"));
                var sheetView = (XmlElement)sheetViews.AppendChild(_xmlDoc.CreateElement("sheetView"));
                sheetView.SetAttribute("workbookViewId", "0");

                var pane = (XmlElement)sheetView.AppendChild(_xmlDoc.CreateElement("pane"));
                pane.SetAttribute("xSplit", "1");
                pane.SetAttribute("ySplit", "1");
                pane.SetAttribute("topLeftCell", "B2");
                pane.SetAttribute("state", "frozen");

                //Create and add the sheetData node.
                _sheetData = _xmlDoc.CreateElement("sheetData");
                workSheet.AppendChild(_sheetData);
            }

            public int WordCount { get; private set; }
            public string Name {
                get { return _name; }
            }

            public void Save(Stream st) {
                using (XmlWriter writer = XmlWriter.Create(st, xmlSettings))
                    _xmlDoc.Save(writer);
            }

            public void AddData(params object[] row) {
                if (_transposedMode)
                    AddColumn(false, row);
                else
                    AddRow(false, row);

                _rowsAdded++;
            }

            public void AddHeader(params object[] row) {
                if (_transposedMode)
                    AddColumn(true, row);
                else
                    AddRow(true, row);

                _rowsAdded++;
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
                string rowName = _rowsAdded.ToString(NumberFormatInfo.InvariantInfo);
                //Create and add the row node. 
                XmlElement rowNode = _xmlDoc.CreateElement("row");
                rowNode.SetAttribute("r", rowName);
                rowNode.SetAttribute("spans", "1:" + row.Length.ToString(NumberFormatInfo.InvariantInfo));

                // XmlElement sheetData = this.xml["worksheet"]["sheetData"];

                for (int i = 0; i < row.Length; i++) {
                    object o = row[i];
                    string cellAddr = GetColumnName(i + 1) + rowName;
                    AddCell(rowNode, o, cellAddr, bold);
                }

                _sheetData.AppendChild(rowNode);
            }

            private void AddColumn(bool bold, object[] row) {
                // XmlElement sheetData = this.xml["worksheet"]["sheetData"];
                XmlNodeList rows = _sheetData.GetElementsByTagName("row");

                if (rows.Count < row.Length) {
                    for (int i = rows.Count; i < row.Length; i++) {
                        XmlElement rowNode = _xmlDoc.CreateElement("row");
                        rowNode.SetAttribute("r", (i + 1).ToString(NumberFormatInfo.InvariantInfo));
                        if (bold) rowNode.SetAttribute("s", "1");

                        // rNode.SetAttribute("spans", "1:" + colspan);
                        _sheetData.AppendChild(rowNode);
                    }

                    rows = _sheetData.GetElementsByTagName("row");
                }

                int iRow = 1;
                string columnName = GetColumnName(_rowsAdded);

                foreach (object o in row) {
                    XmlNode rNode = rows[iRow - 1];
                    string cellAddr = columnName + iRow.ToString(NumberFormatInfo.InvariantInfo);

                    AddCell(rNode, o, cellAddr, bold);

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
                    case TypeCode.Decimal:
                    case TypeCode.Single:
                    case TypeCode.Double:
                        double d = v.ToDouble(NumberFormatInfo.InvariantInfo);

                        if (double.IsNaN(d))
                            return "#NUM!";

                        if (double.IsPositiveInfinity(d) || double.IsNegativeInfinity(d))
                            return "#DIV/0!";

                        return v.ToString(NumberFormatInfo.InvariantInfo);

                    default:
                        return null;
                }
            }

            private void AddCell(XmlNode rNode, object o, string cellAddr, bool bold) {
                char dataType;
                string dataValue = NumberToStringInvariant(o);
                
                if (!string.IsNullOrEmpty(dataValue)) {
                    dataType = dataValue[0] == '#' ? 'e' : 'n';
                }
                else {
                    dataType = 's';
                    string s = o == null ? string.Empty : o.ToString();

                    if (string.IsNullOrEmpty(s)) return;

                    int idx;

                    lock (__sharedStrings) {
                        if (!__sharedStrings.TryGetValue(s, out idx)) {
                            idx = __sharedStrings.Count;
                            __sharedStrings.Add(s, idx);
                        }
                    }

                    WordCount++;

                    dataValue = idx.ToString(NumberFormatInfo.InvariantInfo);
                }

                //Create and add the column node.
                XmlElement cNode = _xmlDoc.CreateElement("c");

                cNode.SetAttribute("r", cellAddr);
                if (bold) cNode.SetAttribute("s", "1");
                cNode.SetAttribute("t", new string(dataType, 1));

                rNode.AppendChild(cNode);

                //Add the dataValue text to the worksheet.
                XmlElement vNode = _xmlDoc.CreateElement("v");
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
