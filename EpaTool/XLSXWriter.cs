/*
 * Copyright (C) 2012  Addition, Lda. (addition at addition dot pt)
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace org.addition.epanet.util {

public class XLSXWriter {

    private bool transposedMode;

    public void setTransposedMode(bool transposedMode) {
        this.transposedMode = transposedMode;
    }

    public bool getTransposedMode() {
        return transposedMode;
    }

    private static string ColumnName(int index) {
        index -= 1;

        int quotient = index / 26;
        if (quotient > 0)
            return ColumnName(quotient) + chars[index % 26];
        else
            return char.ToString(chars[index % 26]);
    }

    private static readonly char[] chars = {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
        'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'
    };


    public class Spreadsheet {

        private const int CELL_RECORD_WIDTH = 75;
        private string tmpFile;
        private TextWriter tmpWriter;
        private string name;
        private int rowNo = 1;
        private int wordCount;

        private int maxColumns;
        private FileStream rndWriter;

        private readonly List<string> _sharedStrings;
        private readonly bool _transposedMode;

        private void fillArray(char[] a) {
            for (int i = 0; i < a.Length; i++) {
                a[i] = ' ';
            }            
        }

        public void prepareTranspose(int rows, int columns) {
            tmpWriter.Close();

            char[] cleanString = new char[CELL_RECORD_WIDTH];
            fillArray(cleanString);
            
            maxColumns = columns;
            tmpWriter = new StreamWriter(tmpFile,false, Encoding.UTF8); 

            for (int i = 0; i < rows; i++) {

                char[] title = string.Format("<row r=\"{0}\" spans=\"1:{1}\">", i + 1, columns).ToCharArray();
                fillArray(cleanString);
                Array.Copy(title, 0, cleanString, 0, title.Length);
                tmpWriter.Write(cleanString, 0, CELL_RECORD_WIDTH);

                fillArray(cleanString);
                for (int j = 0; j < columns; j++) {
                    tmpWriter.Write(cleanString, 0, CELL_RECORD_WIDTH);
                }

                char[] end = "</row>".ToCharArray();
                fillArray(cleanString);
                Array.Copy(end, 0, cleanString, 0, end.Length);
                cleanString[49] = '\n';
                tmpWriter.Write(cleanString, 0, CELL_RECORD_WIDTH);
            }

            tmpWriter.Close();
            tmpWriter = null;
            rndWriter = new FileStream(this.tmpFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        }

        public int getWordCount() {
            return wordCount;
        }

        public string getTmpFile() {
            return tmpFile;
        }

        public TextWriter getTmpWriter() {
            return tmpWriter;
        }

        public Spreadsheet(string name, List<string> sharedStrings, bool transposedMode) {
            this.name = name;
            this._sharedStrings = sharedStrings;
            this._transposedMode = transposedMode;

            tmpFile = Path.GetTempFileName();// File.createTempFile(Spreadsheet.class.getSimpleName(), name);
            tmpWriter = new StreamWriter(File.OpenWrite(tmpFile), Encoding.UTF8);
        }

        private void addRowNormal(params object[] row) {
            tmpWriter.WriteLine("\t<row r=\"{0}\" spans=\"1:%d\">", rowNo, row.Length);
            
            int columnCount = 0;

            foreach (object cell  in  row) {
                if (cell.IsNumber())
                {
                    tmpWriter.Write("<c r=\"" + ColumnName(columnCount + 1) + rowNo + "\" t=\"n\">\n<v>" + cell + "</v>\n</c>");
                } else {
                    var o = (cell ?? "").ToString();
                    int idx = _sharedStrings.IndexOf(o);
                    if (idx < 0) {
                        _sharedStrings.Add(o);
                        idx = _sharedStrings.IndexOf(o);
                    }
                    wordCount++;
                    tmpWriter.Write("\t\t<c r=\"{0}{1}\" t=\"s\"><v>{2}</v></c>", ColumnName(columnCount + 1), rowNo, idx);

                }

                columnCount++;
            }
            tmpWriter.WriteLine("\t</row>");
            rowNo++;
        }

        public void addRow(params object[] row) {
            if (this._transposedMode)
                addRowTranspose2(row);
            else
                addRowNormal(row);
        }


        private void addRowTranspose2(params object[] row) {
            int newRowId = 1;
            int newColumnId = rowNo;
            long pos1 = CELL_RECORD_WIDTH + CELL_RECORD_WIDTH * (rowNo - 1);


            foreach (object cell  in  row) {
                rndWriter.Position = pos1 + CELL_RECORD_WIDTH * (2 + maxColumns) * (newRowId - 1); //50*(maxColumns+2)*(newRowId-1));
                if (cell.IsNumber())
                {
                    byte[] buff = Encoding.UTF8.GetBytes("<c r=\"" + ColumnName(newColumnId) + newRowId + "\" t=\"n\"><v>" + cell + "</v> </c>");
                    rndWriter.Write(buff, 0, buff.Length);

                } else {
                    var o = (cell ?? "").ToString();
                   int idx = _sharedStrings.IndexOf(o);
                    if (idx < 0) {
                        _sharedStrings.Add(o);
                        idx = _sharedStrings.IndexOf(o);
                    }
                    wordCount++;
                    byte[] buff = Encoding.UTF8.GetBytes(string.Format("<c r=\"{0}{1}\" t=\"s\"><v>{2}</v></c>", ColumnName(newColumnId), newRowId, idx));
                    rndWriter.Write(buff, 0, buff.Length);


                }

                newRowId++;
            }
            rowNo++;
        }

        public void addRow(List<object> row) {
            addRow(row.ToArray());
        }

        public string getName() {
            return name;
        }

        public void finish() {
            try {
                if (tmpWriter != null) tmpWriter.Close();
                if (rndWriter != null) rndWriter.Close();
                tmpWriter = null;
                File.Delete(tmpFile);
            } catch (Exception) {
            }
        }

        public void close() {
            try {
                if (tmpWriter != null) {
                    tmpWriter.Flush();
                    tmpWriter.Close();
                }
                if (rndWriter != null) {
                    rndWriter.Close();
                }
            } catch (IOException e) {
                Debug.Print(e.ToString());
            }
        }

    }

    private readonly List<string> sharedStrings = new List<string>();

    public XLSXWriter() {
        sheets = new List<Spreadsheet>();
    }


    private void createWorksheet(Spreadsheet sheet, int pos) {
        sheet.close();

        ZipEntry entry = new ZipEntry("xl/worksheets/sheet" + pos + ".xml");
        zos.PutNextEntry(entry);
        byte[] buff = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                                             "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">\n" +
                                             "<sheetData>\n");
        zos.Write(buff, 0, buff.Length);


        FileStream bis = File.OpenWrite(sheet.getTmpFile());
        CopyStream(bis, this.zos);
        bis.Close();

        buff = Encoding.UTF8.GetBytes("</sheetData>\n</worksheet>\n");
        zos.Write(buff, 0, buff.Length);

        zos.CloseEntry();
    }

    public static void CopyStream(Stream input, Stream output) {
        byte[] buffer = new byte[32768];
        int read;
        while((read = input.Read(buffer, 0, buffer.Length)) > 0) {
            output.Write(buffer, 0, read);
        }
    }

    private void createSharedStringsXML() {
        ZipEntry entry = new ZipEntry("xl/sharedStrings.xml");
        zos.PutNextEntry(entry);

        int count = 0;
        foreach (Spreadsheet s  in  sheets) {
            count += s.getWordCount();
        }

        writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        writer.WriteLine("<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"" + count + "\" uniqueCount=\"" + sharedStrings.Count + "\">");
        
        for (int i = 0; i < sharedStrings.Count; i++) {
            string s = sharedStrings[i];
            //replace all & with &amp;
            s = s.Replace("&", "&amp;");
            //replace all < with &lt;
            s = s.Replace("<", "&lt;");
            //replace all > with &gt;
            s = s.Replace(">", "&gt;");
            writer.WriteLine("\t<si><t>" + s + "</t></si>");
        }
        writer.WriteLine("</sst>");
        
        writer.Flush();
        zos.CloseEntry();
    }


    private void createWorkbookXML() {
        ZipEntry entry = new ZipEntry("xl/workbook.xml");
        zos.PutNextEntry(entry);
        writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        writer.WriteLine("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
        writer.WriteLine("\t<sheets>");

        int id = 1;
        foreach (Spreadsheet sheet  in  sheets) {
            string data = "<sheet name=\"" + sheet.getName() + "\" sheetId=\"" + id + "\" r:id=\"rId" + id + "\"/>";
            writer.WriteLine("\t\t" + data);
            id++;
        }

        writer.WriteLine("\t</sheets>");
        writer.WriteLine("</workbook>");
        writer.Flush();
        zos.CloseEntry();
    }


    private void createXL_rel() {
        ZipEntry entry = new ZipEntry("xl/_rels/workbook.xml.rels");
        zos.PutNextEntry(entry);
        writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        writer.WriteLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");

        int id;

        for (id = 1; id <= this.sheets.Count; id++) {
            string data = "<Relationship Id=\"rId" + id
                          + "\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet"
                          + id + ".xml\"/>";
            this.writer.WriteLine("\t" + data);
        }
        {
            string data = "<Relationship Id=\"rId" + id
                          + "\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings\" Target=\"sharedStrings.xml\"/>";
            writer.WriteLine("\t" + data);
        }

        writer.WriteLine("</Relationships>");
        writer.Flush();
        zos.CloseEntry();
    }

    private void creatContentType() {
        ZipEntry entry = new ZipEntry("[Content_Types].xml");
        zos.PutNextEntry(entry);

        writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        writer.WriteLine("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
        writer.WriteLine("\t<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
        writer.WriteLine("\t<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
        writer.WriteLine("\t<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
        
        int id = 1;
        foreach (Spreadsheet sheet  in  sheets) {
            string data = "<Override PartName=\"/xl/worksheets/sheet" + id + ".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>";
            writer.WriteLine('\t' + data);
            id++;
        }

        writer.WriteLine("\t<Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/>");
        writer.WriteLine("</Types>");
        writer.Flush();
        zos.CloseEntry();
    }

    private void createRels() {
        ZipEntry entry = new ZipEntry("_rels/.rels");
        zos.PutNextEntry(entry);
        writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        writer.WriteLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
        writer.WriteLine("\t<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>");
        writer.WriteLine("</Relationships>");
        writer.Flush();
        zos.CloseEntry();
    }

    public void save(Stream outputStream) {
        zos = new ZipOutputStream(outputStream);
        zos.SetLevel(1);
        writer = new StreamWriter(zos, Encoding.UTF8);
        createRels();

        creatContentType();

        createXL_rel();
        createWorkbookXML();
        createSharedStringsXML();
        for (int i = 0; i < sheets.Count; i++) {
            Spreadsheet sheet = sheets[i];
            createWorksheet(sheet, i + 1);
        }
    }


    private List<Spreadsheet> sheets;
    private ZipOutputStream zos;
    private TextWriter writer;

    public Spreadsheet newSpreadsheet(string name) {
        Spreadsheet spreadsheet = new Spreadsheet(name, this.sharedStrings, this.transposedMode);
        sheets.Add(spreadsheet);
        return spreadsheet;
    }

    public void finish() {
        zos.Finish();
        foreach (Spreadsheet sheet  in  sheets) {
            sheet.finish();
        }
        zos.Close();
    }


}
}