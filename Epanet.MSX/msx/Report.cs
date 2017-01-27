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
using System.Globalization;
using System.IO;

namespace Epanet.MSX {

    public class Report {

        private const int SERIES_TABLE = 0;
        private const int STATS_TABLE = 1;

        private Network _msx;
        private EnToolkit2 _epanet;
        private Output _out;
        private InpReader _inpReader;

        public void LoadDependencies(EpanetMSX epa) {
            _msx = epa.Network;
            _epanet = epa.EnToolkit;
            _out = epa.Output;
            _inpReader = epa.Reader;
        }

        private static readonly string[] logo = {
            "******************************************************************",
            "*                      E P A N E T  -  M S X                     *",
            "*                   Multi-Species Water Quality                  *",
            "*                   Analysis for Pipe  Networks                  *",
            "*                           Version 1.0                          *",
            "******************************************************************"
        };

        private const string PAGE_HDR = "  Page %d                                    ";
        private static readonly string[] statsHdrs = {
            "", "Average Values  ", "Minimum Values  ",
            "Maximum Values  ", "Range of Values "
        };

        private static string _line;
        private static long _lineNum;
        private static long _pageNum;
        private static int[] _rptdSpecies;

        private class TableHeader {
            public string line1;
            public string line2;
            public string line3;
            public string line4;
            public string line5;
        }

        readonly TableHeader _tableHdr;

        public Report() { _tableHdr = new TableHeader(); }
        private string _dname;


        public ErrorCodeType MSXrpt_write(FileInfo outputFile) {
            BinaryReader raf;
            int magic;


            // check that results are available
            if (_msx.Nperiods < 1)
                return 0;

            try {
                raf = new BinaryReader(outputFile.OpenRead());
                raf.BaseStream.Seek(-sizeof(int), SeekOrigin.End);
                magic = raf.ReadInt32();
            }
            catch (IOException) {
                return ErrorCodeType.ERR_IO_OUT_FILE;
            }

            if (magic != Constants.MAGICNUMBER)
                return ErrorCodeType.ERR_IO_OUT_FILE;

            // write program logo & project title
            _pageNum = 1;
            _lineNum = 1;

            NewPage();
            foreach (string s in logo)
                WriteLine(s);

            WriteLine("");
            WriteLine(_msx.Title);

            // generate the appropriate type of table
            if (_msx.Statflag == TstatType.SERIES)
                CreateSeriesTables(raf);
            else
                CreateStatsTables(raf);

            WriteLine("");
            return 0;
        }

        void CreateSeriesTables(BinaryReader raf) {

            // Report on all requested nodes
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.NODE]; i++) {
                if (!_msx.Node[i].Rpt) continue;
                _dname = _epanet.ENgetnodeid(i);
                CreateTableHdr(ObjectTypes.NODE, SERIES_TABLE);
                WriteNodeTable(raf, i, SERIES_TABLE);
            }

            // Report on all requested links
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.LINK]; i++) {
                if (!_msx.Link[i].Rpt) continue;
                _dname = _epanet.ENgetlinkid(i);
                CreateTableHdr(ObjectTypes.LINK, SERIES_TABLE);
                WriteLinkTable(raf, i, SERIES_TABLE);
            }
        }



        void CreateStatsTables(BinaryReader raf) {
            // check if any nodes to be reported
            var count = 0;
            for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.NODE]; j++)
                count += _msx.Node[j].Rpt ? 1 : 0;

            // report on all requested nodes
            if (count > 0) {
                CreateTableHdr(ObjectTypes.NODE, STATS_TABLE);
                for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.NODE]; j++) {
                    if (_msx.Node[j].Rpt) WriteNodeTable(raf, j, STATS_TABLE);
                }
            }

            // Check if any links to be reported
            count = 0;
            for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.LINK]; j++)
                count += _msx.Link[j].Rpt ? 1 : 0;

            // Report on all requested links
            if (count > 0) {
                CreateTableHdr(ObjectTypes.LINK, STATS_TABLE);
                for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.LINK]; j++) {
                    if (_msx.Link[j].Rpt) WriteLinkTable(raf, j, STATS_TABLE);
                }
            }
        }

        void CreateTableHdr(ObjectTypes objType, int tableType) {
            if (tableType == SERIES_TABLE) {

                _tableHdr.line1 = objType == ObjectTypes.NODE
                    ? string.Format("<<< Node {0} >>>", _dname)
                    : string.Format("<<< Link {0} >>>", _dname);

                _tableHdr.line2 = "Time   ";
                _tableHdr.line3 = "hr:min ";
                _tableHdr.line4 = "-------";
            }

            if (tableType == STATS_TABLE) {
                _tableHdr.line1 = "";
                _tableHdr.line2 = string.Format("{0,-16}", statsHdrs[tableType]);
                _tableHdr.line3 = objType == ObjectTypes.NODE ? "for Node        " : "for Link        ";
                _tableHdr.line4 = "----------------";
            }
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; i++) {
                if (_msx.Species[i].Rpt == 0) continue;
                if (objType == ObjectTypes.NODE && _msx.Species[i].Type == SpeciesType.WALL)
                    continue;
                string s1 = string.Format("  {0,10}", _msx.Species[i].Id);
                _tableHdr.line2 += s1;
                _tableHdr.line4 += "  ----------";
                s1 = _inpReader.MSXinp_getSpeciesUnits(i);

                _tableHdr.line3 += string.Format("  {0,10}", s1);
            }
            if (_msx.PageSize > 0 && _msx.PageSize - _lineNum < 8) NewPage();
            else WriteTableHdr();
        }


        void WriteTableHdr() {
            if (_msx.PageSize > 0 && _msx.PageSize - _lineNum < 6) NewPage();
            WriteLine("");
            WriteLine(_tableHdr.line1);
            WriteLine("");
            WriteLine(_tableHdr.line2);
            WriteLine(_tableHdr.line3);
            WriteLine(_tableHdr.line4);
        }

        void WriteNodeTable(BinaryReader raf, int j, int tableType) {
            int[] hrs = new int[1], mins = new int[1];

            for (int i = 0; i < _msx.Nperiods; i++) {
                if (tableType == SERIES_TABLE) {
                    GetHrsMins(i, hrs, mins);
                    _line = string.Format("{0:4}:{1:00}", hrs[0], mins[0]);
                }
                if (tableType == STATS_TABLE) {
                    _dname = _epanet.ENgetnodeid(j);
                    _line = string.Format("{0,-16}", _dname);
                }

                for (int m = 1; m <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; m++) {
                    if (_msx.Species[m].Rpt == 0) continue;
                    if (_msx.Species[m].Type == SpeciesType.WALL) continue;
                    float c = _out.MSXout_getNodeQual(raf, i, j, m);
                    string fmt = "  {0,10:F"
                                 + _msx.Species[m].Precision.ToString(NumberFormatInfo.InvariantInfo) + "}";
                    _line += string.Format(fmt, c);
                }
                WriteLine(_line);
            }
        }

        void WriteLinkTable(BinaryReader raf, int j, int tableType) {
            int[] hrs = new int[1], mins = new int[1];

            for (int k = 0; k < _msx.Nperiods; k++) {
                if (tableType == SERIES_TABLE) {
                    GetHrsMins(k, hrs, mins);
                    _line = string.Format("{0,4}:{1:00}", hrs[0], mins[0]);
                }
                if (tableType == STATS_TABLE) {
                    _dname = _epanet.ENgetlinkid(j);
                    _line = string.Format("{0,-16}", _dname);
                }
                for (int m = 1; m <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; m++) {
                    if (_msx.Species[m].Rpt == 0) continue;
                    float c = _out.MSXout_getLinkQual(raf, k, j, m);
                    string fmt = "  {0,10:F"
                                 + _msx.Species[m].Precision.ToString(NumberFormatInfo.InvariantInfo) + "}";
                    _line += string.Format(fmt, c);
                }
                WriteLine(_line);
            }
        }

        void GetHrsMins(int k, int[] hrs, int[] mins) {
            long m = (_msx.Rstart + k * _msx.Rstep) / 60;
            long h = m / 60;
            m = m - 60 * h;
            hrs[0] = (int)h;
            mins[0] = (int)m;
        }


        void NewPage() {
            _lineNum = 1;
            WriteLine(
                    string.Format("\nPage {0,-3}                                             EPANET-MSX 1.0", _pageNum));
                //(modified, FS-01/07/08)
            WriteLine("");
            if (_pageNum > 1) WriteTableHdr();
            _pageNum++;
        }


        void WriteLine(string line) {
            if (_lineNum == _msx.PageSize)
                NewPage();

            //if ( MSX.RptFile.file ) fprintf(MSX.RptFile.file, "  %s\n", line);   //(modified, FS-01/07/2008)
            //if(MSX.RptFile.getFileIO()!=null){
            //    BufferedWriter din = (BufferedWriter)MSX.RptFile.getFileIO();
            //    try {
            //        din.write(string.format("  %s\n", line));
            //    } catch (IOException e) {
            //        return;
            //    }
            //}
            //else
            //    epanet.ENwriteline(line);
            Console.Out.WriteLine(line);

            _lineNum++;
        }

    }

}