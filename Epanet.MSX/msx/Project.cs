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

using System.Collections.Generic;
using System.IO;
using org.addition.epanet.msx.Structures;

namespace org.addition.epanet.msx {

public class Project {


    public void loadDependencies(EpanetMSX epa)
    {
        MSX = epa.getNetwork();
        reader = epa.getReader();
    }

    Network MSX;

    Dictionary<string, int> [] Htable;

    InpReader reader;

    // Opens an EPANET-MSX project.
    public EnumTypes.ErrorCodeType  MSXproj_open(string msxFile) {
        EnumTypes.ErrorCodeType errcode = 0;

        //MSX.QualityOpened = false;

        StreamReader buffReader = File.OpenText(msxFile);
        // initialize data to default values
        setDefaults();

        // Open the MSX input file
        //MSX.MsxFile.setFilename(fname);
        //if(!MSX.MsxFile.openAsTextReader())
        //    return ErrorCodeType.ERR_OPEN_MSX_FILE.id;

        // create hash tables to look up object ID names
        errcode = Utilities.CALL(errcode, createHashTables());

        // allocate memory for the required number of objects
        errcode = Utilities.CALL(errcode, reader.countMsxObjects(buffReader));
        errcode = Utilities.CALL(errcode, reader.countNetObjects());
        errcode = Utilities.CALL(errcode, createObjects());

        buffReader.Close();
        buffReader = File.OpenText(msxFile);

        // Read in the EPANET and MSX object data

        errcode = Utilities.CALL(errcode, reader.readNetData());
        errcode = Utilities.CALL(errcode, reader.readMsxData(buffReader));

        //if (MSX.RptFile.getFilename().equals(""))
        //    errcode = Utilities.CALL(errcode, openRptFile());

        // Convert user's units to internal units
        errcode = Utilities.CALL(errcode, convertUnits());

        // Close input file
        //MSX.MsxFile.close();

        return errcode;
    }

    //=============================================================================
    // closes the current EPANET-MSX project.
    void MSXproj_close()
    {
        //if ( MSX.RptFile.file ) fclose(MSX.RptFile.file);                          //(LR-11/20/07, to fix bug 08)
        //MSX.RptFile.close();
        //if ( MSX.HydFile.file ) fclose(MSX.HydFile.file);
        //if ( MSX.HydFile.getMode() == FileModeType.SCRATCH_FILE ) //remove(MSX.HydFile.name);
        //    MSX.HydFile.remove();
        //if ( MSX.TmpOutFile.getFileIO() != null && MSX.TmpOutFile.getFileIO() != MSX.OutFile.getFileIO() )
        //{
        //    //fclose(MSX.TmpOutFile.file);
        //    MSX.TmpOutFile.close();
        //    MSX.TmpOutFile.remove();
        //    //remove(MSX.TmpOutFile.name);
        //}
        //if ( MSX.OutFile.file ) fclose(MSX.OutFile.file);
        //MSX.OutFile.close();
        //
        //if ( MSX.OutFile.getMode() == FileModeType.SCRATCH_FILE )// remove(MSX.OutFile.name);
        //    MSX.OutFile.close();
        //MSX.RptFile.file = null;                                                   //(LR-11/20/07, to fix bug 08)
        //MSX.HydFile.file = null;
        //MSX.OutFile.file = null;
        //MSX.TmpOutFile.file = null;
        deleteObjects();
        deleteHashTables();
        //MSX.ProjectOpened = false;
    }

    //=============================================================================
    // adds an object ID to the project's hash tables.
    public int   MSXproj_addObject(EnumTypes.ObjectTypes type, string id, int n)
    {
        int  result = 0;
        int  len;
        string newID = id;

// --- do nothing if object already exists in a hash table

        if ( MSXproj_findObject(type, id) > 0 ) return 0;

// --- insert object's ID into the hash table for that type of object

        //result = HTinsert(Htable[type], newID, n);

        if (Htable[(int)type].ContainsKey(newID)) {
            Htable[(int)type][newID] = n;
        }
        else {
            Htable[(int)type].Add(newID, n);
            result = 1;
        }

        return result == 0 ? -1 : result;
    }

    //=============================================================================
    // uses hash table to find index of an object with a given ID.
    public int   MSXproj_findObject(EnumTypes.ObjectTypes type, string id) {
        int val;
        return Htable[(int)type].TryGetValue(id, out val) ? val : -1;
    }

    //=============================================================================
    // uses hash table to find address of given string entry.
    public string MSXproj_findID(EnumTypes.ObjectTypes type, string id)
    {
        return Htable[(int)type].ContainsKey(id) ? id : string.Empty;
    }

    //=============================================================================
    // gets the text of an error message.
    string MSXproj_getErrmsg(EnumTypes.ErrorCodeType errcode)
    {
        if ( errcode <= EnumTypes.ErrorCodeType.ERR_FIRST || errcode >= EnumTypes.ErrorCodeType.ERR_MAX ) return Constants.Errmsg[0];
        else return Constants.Errmsg[errcode - EnumTypes.ErrorCodeType.ERR_FIRST];
    }

    // assigns default values to project variables.
    void setDefaults()
    {
        MSX.Title = "";
        MSX.Rptflag = false;
        for (int i=0; i<(int)EnumTypes.ObjectTypes.MAX_OBJECTS; i++)
            MSX.Nobjects[i] = 0;
        MSX.Unitsflag = EnumTypes.UnitSystemType.US;
        MSX.Flowflag = EnumTypes.FlowUnitsType.GPM;
        MSX.Statflag = EnumTypes.TstatType.SERIES;
        MSX.DefRtol = 0.001;
        MSX.DefAtol = 0.01;
        MSX.Solver = EnumTypes.SolverType.EUL;
        MSX.Coupling = EnumTypes.CouplingType.NO_COUPLING;
        MSX.AreaUnits = EnumTypes.AreaUnitsType.FT2;
        MSX.RateUnits = EnumTypes.RateUnitsType.DAYS;
        MSX.Qstep = 300;
        MSX.Rstep = 3600;
        MSX.Rstart = 0;
        MSX.Dur = 0;
        MSX.Node = null;
        MSX.Link = null;
        MSX.Tank = null;
        MSX.D = null;
        MSX.Q = null;
        MSX.H = null;
        MSX.Species = null;
        MSX.Term = null;
        MSX.Const = null;
        MSX.Pattern = null;
    }

    // Converts user's units to internal EPANET units.
    EnumTypes.ErrorCodeType convertUnits()
    {
        // Flow conversion factors (to cfs)
        double[] fcf = {1.0, Constants.GPMperCFS, Constants.MGDperCFS, Constants.IMGDperCFS, Constants.AFDperCFS,
                Constants.LPSperCFS, Constants.LPMperCFS, Constants.MLDperCFS, Constants.CMHperCFS, Constants.CMDperCFS};

        // Rate time units conversion factors (to sec)
        double[] rcf = {1.0, 60.0, 3600.0, 86400.0};

        int i, m;
        EnumTypes.ErrorCodeType errcode = 0;

        // Conversions for length & tank volume
        if ( MSX.Unitsflag == EnumTypes.UnitSystemType.US )
        {
            MSX.Ucf[(int)EnumTypes.UnitsType.LENGTH_UNITS] = 1.0;
            MSX.Ucf[(int)EnumTypes.UnitsType.DIAM_UNITS]   = 12.0;
            MSX.Ucf[(int)EnumTypes.UnitsType.VOL_UNITS]    = 1.0;
        }
        else
        {
            MSX.Ucf[(int)EnumTypes.UnitsType.LENGTH_UNITS] = Constants.MperFT;
            MSX.Ucf[(int)EnumTypes.UnitsType.DIAM_UNITS]   = 1000.0*Constants.MperFT;
            MSX.Ucf[(int)EnumTypes.UnitsType.VOL_UNITS]    = Constants.M3perFT3;
        }

        // Conversion for surface area
        MSX.Ucf[(int)EnumTypes.UnitsType.AREA_UNITS] = 1.0;
        switch (MSX.AreaUnits)
        {
            case EnumTypes.AreaUnitsType.M2:  
                MSX.Ucf[(int)EnumTypes.UnitsType.AREA_UNITS] = Constants.M2perFT2;  
                break;
            case EnumTypes.AreaUnitsType.CM2:
                MSX.Ucf[(int)EnumTypes.UnitsType.AREA_UNITS] = Constants.CM2perFT2; 
                break;
        }

        // Conversion for flow rate
        MSX.Ucf[(int)EnumTypes.UnitsType.FLOW_UNITS] = fcf[(int)MSX.Flowflag];
        MSX.Ucf[(int)EnumTypes.UnitsType.CONC_UNITS] = Constants.LperFT3;

        // Conversion for reaction rate time
        MSX.Ucf[(int)EnumTypes.UnitsType.RATE_UNITS] = rcf[(int)MSX.RateUnits];

        // Convert pipe diameter & length
        for (i=1; i<=MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; i++){
            MSX.Link[i].setDiam( MSX.Link[i].getDiam() / MSX.Ucf[(int)EnumTypes.UnitsType.DIAM_UNITS]);
            MSX.Link[i].setLen(MSX.Link[i].getLen() /  MSX.Ucf[(int)EnumTypes.UnitsType.LENGTH_UNITS]);
        }

        // Convert initial tank volumes
        for (i=1; i<=MSX.Nobjects[(int)EnumTypes.ObjectTypes.TANK]; i++){
            MSX.Tank[i].setV0(MSX.Tank[i].getV0() / MSX.Ucf[(int)EnumTypes.UnitsType.VOL_UNITS]);
            MSX.Tank[i].setvMix(MSX.Tank[i].getvMix() / MSX.Ucf[(int)EnumTypes.UnitsType.VOL_UNITS]);
        }

        // Assign default tolerances to species
        for (m=1; m<=MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++){
            if ( MSX.Species[m].getrTol() == 0.0 ) MSX.Species[m].setrTol(MSX.DefRtol);
            if ( MSX.Species[m].getaTol()  == 0.0 ) MSX.Species[m].setaTol(MSX.DefAtol);
        }

        return errcode;
    }

    // creates multi-species data objects.
    EnumTypes.ErrorCodeType createObjects()
    {
        // Create nodes, links, & tanks
        MSX.Node = new Node[MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]+1];
        MSX.Link = new Link[MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]+1];
        MSX.Tank = new Tank[MSX.Nobjects[(int)EnumTypes.ObjectTypes.TANK]+1];

        // Create species, terms, parameters, constants & time patterns
        MSX.Species = new Species[MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]+1];
        MSX.Term    = new Term[MSX.Nobjects[(int)EnumTypes.ObjectTypes.TERM]+1];
        MSX.Param   = new Param[MSX.Nobjects[(int)EnumTypes.ObjectTypes.PARAMETER]+1];
        MSX.Const   = new Const[MSX.Nobjects[(int)EnumTypes.ObjectTypes.CONSTANT]+1];
        MSX.Pattern = new Pattern[MSX.Nobjects[(int)EnumTypes.ObjectTypes.PATTERN]+1];

        for(int i = 0;i<=MSX.Nobjects[(int)EnumTypes.ObjectTypes.CONSTANT];i++)
            MSX.Const[i] = new Const();

        // Create arrays for demands, heads, & flows
        MSX.D = new float[MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]+1];
        MSX.H = new float[MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]+1];
        MSX.Q = new float[MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]+1];

        // create arrays for current & initial concen. of each species for each node
        MSX.C0 = new double[MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]+1];
        for (int i=1; i<=MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; i++)
        {
            MSX.Node[i] = new Node(MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]+1);

            //MSX.Node[i].c =  new double[MSX.Nobjects[SPECIES]+1, sizeof(double));
            //MSX.Node[i].c0 = new double[MSX.Nobjects[SPECIES]+1, sizeof(double));
            //MSX.Node[i].rpt = 0;
        }

        // Create arrays for init. concen. & kinetic parameter values for each link

        for (int i=1; i<=MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; i++)
        {
            MSX.Link[i] = new Link(MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]+1,MSX.Nobjects[(int)EnumTypes.ObjectTypes.PARAMETER]+1);
            //MSX.Link[i].c0 = (double *)
            //calloc(MSX.Nobjects[SPECIES]+1, sizeof(double));
            //MSX.Link[i].param = (double *)
            //calloc(MSX.Nobjects[PARAMETER]+1, sizeof(double));
            //MSX.Link[i].rpt = 0;
        }

        // Create arrays for kinetic parameter values & current concen. for each tank

        for (int i=1; i<=MSX.Nobjects[(int)EnumTypes.ObjectTypes.TANK]; i++)
        {
            MSX.Tank[i] = new Tank(MSX.Nobjects[(int)EnumTypes.ObjectTypes.PARAMETER]+1,MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]+1);

            //MSX.Tank[i].param = (double *)
            //calloc(MSX.Nobjects[PARAMETER]+1, sizeof(double));
            //MSX.Tank[i].c = (double *)
            //calloc(MSX.Nobjects[SPECIES]+1, sizeof(double));
        }

        // Initialize contents of each time pattern object

        for (int i=1; i<=MSX.Nobjects[(int)EnumTypes.ObjectTypes.PATTERN]; i++)
        {
            MSX.Pattern[i] = new Pattern();
            //MSX.Pattern[i].length = 0;
            //MSX.Pattern[i].first = null;
            //MSX.Pattern[i].current = null;
        }

        // Initialize reaction rate & equil. formulas for each species

        for (int i=1; i<=MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; i++)
        {
            MSX.Species[i] = new Species();
            //MSX.Species[i].pipeExpr     = null;
            //MSX.Species[i].tankExpr     = null;
            //MSX.Species[i].pipeExprType = NO_EXPR;
            //MSX.Species[i].tankExprType = NO_EXPR;
            //MSX.Species[i].precision    = 2;
            //MSX.Species[i].rpt = 0;
        }

        // Initialize math expressions for each intermediate term

        for (int i=1; i<=MSX.Nobjects[(int)EnumTypes.ObjectTypes.TERM]; i++){
            MSX.Term[i] = new Term();
            //MSX.Term[i].expr = null;
        }
        return 0;
    }

    //=============================================================================
    // Deletes multi-species data objects.
    void deleteObjects()
    {
        //int i;
        ////SnumList *listItem;
        //
// --- f//ree memory used by nodes, links, and tanks
        //
        //if (MSX.Node) for (i=1; i<=MSX.Nobjects[NODE]; i++)
        //{
        //    FREE(MSX.Node[i].c);
        //    FREE(MSX.Node[i].c0);
        //}
        //if (MSX.Link) for (i=1; i<=MSX.Nobjects[LINK]; i++)
        //{
        //    FREE(MSX.Link[i].c0);
        //    FREE(MSX.Link[i].param);
        //}
        //if (MSX.Tank) for (i=1; i<=MSX.Nobjects[TANK]; i++)
        //{
        //    FREE(MSX.Tank[i].param);
        //    FREE(MSX.Tank[i].c);
        //}
        //
// --- f//ree memory used by time patterns
        //
        //if (MSX.Pattern) for (i=1; i<=MSX.Nobjects[PATTERN]; i++)
        //{
        //    listItem = MSX.Pattern[i].first;
        //    while (listItem)
        //    {
        //        MSX.Pattern[i].first = listItem->next;
        //        free(listItem);
        //        listItem = MSX.Pattern[i].first;
        //    }
        //}
        //FREE(MSX.Pattern);
        //
// --- f//ree memory used for hydraulics results
        //
        //FREE(MSX.D);
        //FREE(MSX.H);
        //FREE(MSX.Q);
        //FREE(MSX.C0);
        //
// --- d//elete all nodes, links, and tanks
        //
        //FREE(MSX.Node);
        //FREE(MSX.Link);
        //FREE(MSX.Tank);
        //
// --- f//ree memory used by reaction rate & equilibrium expressions
        //
        //if (MSX.Species) for (i=1; i<=MSX.Nobjects[SPECIES]; i++)
        //{
        //    // --- free the species tank expression only if it doesn't
        //    //     already point to the species pipe expression
        //    if ( MSX.Species[i].tankExpr != MSX.Species[i].pipeExpr )
        //    {
        //        mathexpr_delete(MSX.Species[i].tankExpr);
        //    }
        //    mathexpr_delete(MSX.Species[i].pipeExpr);
        //}
        //
// --- d//elete all species, parameters, and constants
        //
        //FREE(MSX.Species);
        //FREE(MSX.Param);
        //FREE(MSX.Const);
        //
// --- f//ree memory used by intermediate terms
        //
        //if (MSX.Term) for (i=1; i<=MSX.Nobjects[TERM]; i++)
        //    mathexpr_delete(MSX.Term[i].expr);
        //FREE(MSX.Term);
    }

    // allocates memory for object ID hash tables.
    EnumTypes.ErrorCodeType createHashTables()
    {
      

        // create a hash table for each type of object
        Htable = new Dictionary<string, int>[(int)EnumTypes.ObjectTypes.MAX_OBJECTS];

        for (int j = 0; j < (int)EnumTypes.ObjectTypes.MAX_OBJECTS ; j++){
            Htable[j] = new Dictionary<string, int>();
        }

        return 0;
    }

    //=============================================================================
    // frees memory allocated for object ID hash tables.
    void deleteHashTables()
    {
        //int j;
        //
// --- f//ree the hash tables
        //
        //for (j = 0; j < MAX_OBJECTS; j++)
        //{
        //    if ( Htable[j] != null ) HTfree(Htable[j]);
        //}
        //
// --- f//ree the object ID memory pool
        //
        //if ( HashPool )
        //{
        //    AllocSetPool(HashPool);
        //    AllocFreePool();
        //}
    }

    // New function added (LR-11/20/07, to fix bug 08)
#if COMMENTED1
   int openRptFile()
   {
       if( MSX.RptFile.getFilename().equals(""))
           return 0;
   
       //if ( MSX.RptFile.file ) fclose(MSX.RptFile.file);
       MSX.RptFile.close();
       //MSX.RptFile.file = fopen(MSX.RptFile.name, "wt");
       if(!MSX.RptFile.openAsTextWriter())
           return (int)EnumTypes.ErrorCodeType.ERR_OPEN_RPT_FILE;
       //if ( MSX.RptFile.file == null ) return ErrorCodeType.ERR_OPEN_RPT_FILE;
       return 0;
   }
#endif

}
}