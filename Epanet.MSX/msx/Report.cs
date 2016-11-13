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




    private const int SERIES_TABLE  =0;
    private const int STATS_TABLE   =1;


    private Network     MSX;
    private ENToolkit2  epanet;
    private Output      @out;
    private InpReader   inpReader;

    public void loadDependencies(EpanetMSX epa){
        MSX=epa.getNetwork();
        epanet=epa.getENToolkit() ;
        @out=epa.getOutput();
        inpReader = epa.getReader();
    }

    private static string [] Logo =
            {   "******************************************************************",
                    "*                      E P A N E T  -  M S X                     *",
                    "*                   Multi-Species Water Quality                  *",
                    "*                   Analysis for Pipe  Networks                  *",
                    "*                           Version 1.0                          *",
                    "******************************************************************"};

    private readonly static string PageHdr = "  Page %d                                    ";
    private static string [] StatsHdrs = {"", "Average Values  ", "Minimum Values  ",
            "Maximum Values  ", "Range of Values "};
    private static string Line;
    private static long LineNum;
    private static long PageNum;
    private static int  [] RptdSpecies;

    private class  TableHeader{
        public string Line1;
        public string Line2;
        public string Line3;
        public string Line4;
        public string Line5;
    }
    TableHeader TableHdr;

    public Report(){
        TableHdr = new TableHeader();
    }
    private string IDname;


    public EnumTypes.ErrorCodeType  MSXrpt_write(FileInfo outputFile)
    {
        BinaryReader raf;
        int  magic = 0;
    

        // check that results are available
        if ( MSX.Nperiods < 1 )
            return 0;

        try{
            raf = new BinaryReader(outputFile.OpenRead());
            raf.BaseStream.Seek(-sizeof(int), SeekOrigin.End);
            magic = raf.ReadInt32();
        }
        catch (IOException){
            return  EnumTypes.ErrorCodeType.ERR_IO_OUT_FILE;
        }

        if ( magic != Constants.MAGICNUMBER )
            return EnumTypes.ErrorCodeType.ERR_IO_OUT_FILE;

        // write program logo & project title
        PageNum = 1;
        LineNum = 1;

        newPage();
        foreach (string s in Logo)
            this.writeLine(s);

        writeLine("");
        writeLine(MSX.Title);

        // generate the appropriate type of table
        if ( MSX.Statflag == EnumTypes.TstatType.SERIES )
            createSeriesTables(raf);
        else
            createStatsTables(raf);

        writeLine("");
        return 0;
    }

    void createSeriesTables(BinaryReader raf){
        int  j;

        // Report on all requested nodes
        for (j=1; j<=MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; j++)
        {
            if ( !MSX.Node[j].getRpt() ) continue;
            IDname = epanet.ENgetnodeid(j);
            createTableHdr(EnumTypes.ObjectTypes.NODE, SERIES_TABLE);
            writeNodeTable(raf,j, SERIES_TABLE);
        }

        // Report on all requested links
        for (j=1; j<=MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; j++)
        {
            if ( !MSX.Link[j].getRpt() ) continue;
            IDname = epanet.ENgetlinkid(j);
            createTableHdr(EnumTypes.ObjectTypes.LINK, SERIES_TABLE);
            writeLinkTable(raf,j, SERIES_TABLE);
        }
    }



    void createStatsTables(BinaryReader raf){
        int  j;
        int  count;

        // check if any nodes to be reported
        count = 0;
        for (j = 1; j <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; j++) count += MSX.Node[j].getRpt()?1:0;

        // report on all requested nodes
        if ( count > 0 )
        {
            createTableHdr(EnumTypes.ObjectTypes.NODE, STATS_TABLE);
            for (j = 1; j <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; j++)
            {
                if ( MSX.Node[j].getRpt()) writeNodeTable(raf,j, STATS_TABLE);
            }
        }

        // Check if any links to be reported
        count = 0;
        for (j = 1; j <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; j++) count += MSX.Link[j].getRpt()?1:0;

        // Report on all requested links
        if ( count > 0 )
        {
            createTableHdr(EnumTypes.ObjectTypes.LINK, STATS_TABLE);
            for (j = 1; j <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; j++)
            {
                if ( MSX.Link[j].getRpt() ) writeLinkTable(raf,j, STATS_TABLE);
            }
        }
    }

    void createTableHdr(EnumTypes.ObjectTypes objType, int tableType)
    {
        int   m;
        string s1;

        if ( tableType == SERIES_TABLE ) {

            TableHdr.Line1 = objType == EnumTypes.ObjectTypes.NODE
                ? string.Format("<<< Node {0} >>>", IDname)
                : string.Format("<<< Link {0} >>>", IDname);

            TableHdr.Line2 = "Time   ";
            TableHdr.Line3 = "hr:min ";
            TableHdr.Line4 = "-------";
        }

        if ( tableType == STATS_TABLE )
        {
            TableHdr.Line1 = "";
            TableHdr.Line2 = string.Format("%-16s", StatsHdrs[tableType]);
            if ( objType == EnumTypes.ObjectTypes.NODE ) TableHdr.Line3 = "for Node        ";
            else                   TableHdr.Line3 = "for Link        ";
            TableHdr.Line4 = "----------------";
        }
        for (m=1; m<=MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
        {
            if ( MSX.Species[m].getRpt()==0 ) continue;
            if ( objType == EnumTypes.ObjectTypes.NODE && MSX.Species[m].getType() == EnumTypes.SpeciesType.WALL ) continue;
            s1 = string.Format("  {0,10}", MSX.Species[m].getId());
            TableHdr.Line2 += s1;
            TableHdr.Line4 += "  ----------";
            s1 = inpReader.MSXinp_getSpeciesUnits(m);

            TableHdr.Line3 +=  string.Format("  {0,10}", s1);
        }
        if ( MSX.PageSize > 0 && MSX.PageSize - LineNum < 8 ) newPage();
        else writeTableHdr();
    }


    void  writeTableHdr()
    {
        if ( MSX.PageSize > 0 && MSX.PageSize - LineNum < 6 ) newPage();
        writeLine("");
        writeLine(TableHdr.Line1);
        writeLine("");
        writeLine(TableHdr.Line2);
        writeLine(TableHdr.Line3);
        writeLine(TableHdr.Line4);
    }

    void  writeNodeTable(BinaryReader raf,int j, int tableType)
    {
        int   k, m;
        int [] hrs = new int[1], mins = new int[1];
        float c;

        for (k=0; k<MSX.Nperiods; k++)
        {
            if ( tableType == SERIES_TABLE )
            {
                getHrsMins(k, hrs, mins);
                Line = string.Format("{0:4}:{1:00}", hrs[0], mins[0]);
            }
            if ( tableType == STATS_TABLE )
            {
                IDname = epanet.ENgetnodeid(j);
                Line = string.Format("{0,-16}", IDname);
            }
            for (m=1; m<=MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
            {
                if ( MSX.Species[m].getRpt() == 0 ) continue;
                if ( MSX.Species[m].getType() == EnumTypes.SpeciesType.WALL ) continue;
                c = @out.MSXout_getNodeQual(raf,k, j, m);
                string fmt = "  {0,10:F" + this.MSX.Species[m].getPrecision().ToString(NumberFormatInfo.InvariantInfo) + "}";
                Line += string.Format(fmt, c);
            }
            writeLine(Line);
        }
    }

    void  writeLinkTable(BinaryReader raf,int j, int tableType)
    {
        int   k, m;
        int [] hrs = new int [1], mins = new int [1];
        float c;

        for (k=0; k<MSX.Nperiods; k++)
        {
            if ( tableType == SERIES_TABLE )
            {
                getHrsMins(k, hrs, mins);
                Line = string.Format("{0,4}:{1:00}", hrs[0], mins[0]);
            }
            if ( tableType == STATS_TABLE )
            {
                IDname = epanet.ENgetlinkid(j);
                Line = string.Format("{0,-16}", IDname);
            }
            for (m=1; m<=MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
            {
                if ( MSX.Species[m].getRpt() ==0) continue;
                c = @out.MSXout_getLinkQual(raf,k, j, m);
                string fmt = "  {0,10:F" + this.MSX.Species[m].getPrecision().ToString(NumberFormatInfo.InvariantInfo) + "}";
                Line += string.Format(fmt, c);
            }
            writeLine(Line);
        }
    }

    void getHrsMins(int k, int [] hrs, int [] mins)
    {
        long m, h;

        m = (MSX.Rstart + k*MSX.Rstep) / 60;
        h = m / 60;
        m = m - 60*h;
        hrs[0] = (int)h;
        mins[0] = (int)m;
    }


    void  newPage()
    {
        string  s;
        LineNum = 1;
        writeLine(string.Format("\nPage {0,-3}                                             EPANET-MSX 1.0", PageNum)); //(modified, FS-01/07/08)
        writeLine("");
        if ( PageNum > 1 ) writeTableHdr();
        PageNum++;
    }


    void  writeLine(string line)
    {
        if ( LineNum == MSX.PageSize )
            newPage();

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

        LineNum++;
    }

}
}