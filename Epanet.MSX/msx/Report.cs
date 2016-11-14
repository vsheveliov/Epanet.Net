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

namespace org.addition.epanet.msx {

    public class Report {




        private const int SERIES_TABLE = 0;
        private const int STATS_TABLE = 1;


        private Network msx;
        private ENToolkit2 epanet;
        private Output @out;
        private InpReader inpReader;

        public void LoadDependencies(EpanetMSX epa) {
            this.msx = epa.Network;
            epanet = epa.EnToolkit;
            @out = epa.Output;
            inpReader = epa.Reader;
        }

        private static readonly string[] Logo = {
            "******************************************************************",
            "*                      E P A N E T  -  M S X                     *",
            "*                   Multi-Species Water Quality                  *",
            "*                   Analysis for Pipe  Networks                  *",
            "*                           Version 1.0                          *",
            "******************************************************************"
        };

        private const string PAGE_HDR = "  Page %d                                    ";
        private static readonly string[] StatsHdrs = {
            "", "Average Values  ", "Minimum Values  ",
            "Maximum Values  ", "Range of Values "
        };
        private static string _line;
        private static long _lineNum;
        private static long _pageNum;
        private static int[] _rptdSpecies;

        private class TableHeader {
            public string Line1;
            public string Line2;
            public string Line3;
            public string Line4;
            public string Line5;
        }

        readonly TableHeader tableHdr;

        public Report() { this.tableHdr = new TableHeader(); }
        private string dname;


        public EnumTypes.ErrorCodeType MSXrpt_write(FileInfo outputFile) {
            BinaryReader raf;
            int magic = 0;


            // check that results are available
            if (this.msx.Nperiods < 1)
                return 0;

            try {
                raf = new BinaryReader(outputFile.OpenRead());
                raf.BaseStream.Seek(-sizeof(int), SeekOrigin.End);
                magic = raf.ReadInt32();
            }
            catch (IOException) {
                return EnumTypes.ErrorCodeType.ERR_IO_OUT_FILE;
            }

            if (magic != Constants.MAGICNUMBER)
                return EnumTypes.ErrorCodeType.ERR_IO_OUT_FILE;

            // write program logo & project title
            _pageNum = 1;
            _lineNum = 1;

            this.NewPage();
            foreach (string s in Logo)
                this.WriteLine(s);

            this.WriteLine("");
            this.WriteLine(this.msx.Title);

            // generate the appropriate type of table
            if (this.msx.Statflag == EnumTypes.TstatType.SERIES)
                this.CreateSeriesTables(raf);
            else
                this.CreateStatsTables(raf);

            this.WriteLine("");
            return 0;
        }

        void CreateSeriesTables(BinaryReader raf) {

            // Report on all requested nodes
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; i++) {
                if (!this.msx.Node[i].Rpt) continue;
                this.dname = epanet.ENgetnodeid(i);
                this.CreateTableHdr(EnumTypes.ObjectTypes.NODE, SERIES_TABLE);
                this.WriteNodeTable(raf, i, SERIES_TABLE);
            }

            // Report on all requested links
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; i++) {
                if (!this.msx.Link[i].getRpt()) continue;
                this.dname = epanet.ENgetlinkid(i);
                this.CreateTableHdr(EnumTypes.ObjectTypes.LINK, SERIES_TABLE);
                this.WriteLinkTable(raf, i, SERIES_TABLE);
            }
        }



        void CreateStatsTables(BinaryReader raf) {
            // check if any nodes to be reported
            var count = 0;
            for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; j++)
                count += this.msx.Node[j].Rpt ? 1 : 0;

            // report on all requested nodes
            if (count > 0) {
                this.CreateTableHdr(EnumTypes.ObjectTypes.NODE, STATS_TABLE);
                for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; j++) {
                    if (this.msx.Node[j].Rpt) this.WriteNodeTable(raf, j, STATS_TABLE);
                }
            }

            // Check if any links to be reported
            count = 0;
            for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; j++)
                count += this.msx.Link[j].getRpt() ? 1 : 0;

            // Report on all requested links
            if (count > 0) {
                this.CreateTableHdr(EnumTypes.ObjectTypes.LINK, STATS_TABLE);
                for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; j++) {
                    if (this.msx.Link[j].getRpt()) this.WriteLinkTable(raf, j, STATS_TABLE);
                }
            }
        }

        void CreateTableHdr(EnumTypes.ObjectTypes objType, int tableType) {
            if (tableType == SERIES_TABLE) {

                this.tableHdr.Line1 = objType == EnumTypes.ObjectTypes.NODE
                    ? string.Format("<<< Node {0} >>>", this.dname)
                    : string.Format("<<< Link {0} >>>", this.dname);

                this.tableHdr.Line2 = "Time   ";
                this.tableHdr.Line3 = "hr:min ";
                this.tableHdr.Line4 = "-------";
            }

            if (tableType == STATS_TABLE) {
                this.tableHdr.Line1 = "";
                this.tableHdr.Line2 = string.Format("%-16s", StatsHdrs[tableType]);
                if (objType == EnumTypes.ObjectTypes.NODE) this.tableHdr.Line3 = "for Node        ";
                else this.tableHdr.Line3 = "for Link        ";
                this.tableHdr.Line4 = "----------------";
            }
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; i++) {
                if (this.msx.Species[i].getRpt() == 0) continue;
                if (objType == EnumTypes.ObjectTypes.NODE && this.msx.Species[i].getType() == EnumTypes.SpeciesType.WALL)
                    continue;
                string s1 = string.Format("  {0,10}", this.msx.Species[i].getId());
                this.tableHdr.Line2 += s1;
                this.tableHdr.Line4 += "  ----------";
                s1 = inpReader.MSXinp_getSpeciesUnits(i);

                this.tableHdr.Line3 += string.Format("  {0,10}", s1);
            }
            if (this.msx.PageSize > 0 && this.msx.PageSize - _lineNum < 8) this.NewPage();
            else this.WriteTableHdr();
        }


        void WriteTableHdr() {
            if (this.msx.PageSize > 0 && this.msx.PageSize - _lineNum < 6) this.NewPage();
            this.WriteLine("");
            this.WriteLine(this.tableHdr.Line1);
            this.WriteLine("");
            this.WriteLine(this.tableHdr.Line2);
            this.WriteLine(this.tableHdr.Line3);
            this.WriteLine(this.tableHdr.Line4);
        }

        void WriteNodeTable(BinaryReader raf, int j, int tableType) {
            int[] hrs = new int[1], mins = new int[1];

            for (int i = 0; i < this.msx.Nperiods; i++) {
                if (tableType == SERIES_TABLE) {
                    this.GetHrsMins(i, hrs, mins);
                    _line = string.Format("{0:4}:{1:00}", hrs[0], mins[0]);
                }
                if (tableType == STATS_TABLE) {
                    this.dname = epanet.ENgetnodeid(j);
                    _line = string.Format("{0,-16}", this.dname);
                }

                for (int m = 1; m <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++) {
                    if (this.msx.Species[m].getRpt() == 0) continue;
                    if (this.msx.Species[m].getType() == EnumTypes.SpeciesType.WALL) continue;
                    float c = this.@out.MSXout_getNodeQual(raf, i, j, m);
                    string fmt = "  {0,10:F"
                                 + this.msx.Species[m].getPrecision().ToString(NumberFormatInfo.InvariantInfo) + "}";
                    _line += string.Format(fmt, c);
                }
                this.WriteLine(_line);
            }
        }

        void WriteLinkTable(BinaryReader raf, int j, int tableType) {
            int[] hrs = new int[1], mins = new int[1];

            for (int k = 0; k < this.msx.Nperiods; k++) {
                if (tableType == SERIES_TABLE) {
                    this.GetHrsMins(k, hrs, mins);
                    _line = string.Format("{0,4}:{1:00}", hrs[0], mins[0]);
                }
                if (tableType == STATS_TABLE) {
                    this.dname = epanet.ENgetlinkid(j);
                    _line = string.Format("{0,-16}", this.dname);
                }
                for (int m = 1; m <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++) {
                    if (this.msx.Species[m].getRpt() == 0) continue;
                    float c = this.@out.MSXout_getLinkQual(raf, k, j, m);
                    string fmt = "  {0,10:F"
                                 + this.msx.Species[m].getPrecision().ToString(NumberFormatInfo.InvariantInfo) + "}";
                    _line += string.Format(fmt, c);
                }
                this.WriteLine(_line);
            }
        }

        void GetHrsMins(int k, int[] hrs, int[] mins) {
            long m = (this.msx.Rstart + k * this.msx.Rstep) / 60;
            long h = m / 60;
            m = m - 60 * h;
            hrs[0] = (int)h;
            mins[0] = (int)m;
        }


        void NewPage() {
            _lineNum = 1;
            this.WriteLine(
                    string.Format("\nPage {0,-3}                                             EPANET-MSX 1.0", _pageNum));
                //(modified, FS-01/07/08)
            this.WriteLine("");
            if (_pageNum > 1) this.WriteTableHdr();
            _pageNum++;
        }


        void WriteLine(string line) {
            if (_lineNum == this.msx.PageSize)
                this.NewPage();

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