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

using System.Collections.Generic;
using System.IO;
using Epanet.MSX.Structures;

namespace Epanet.MSX {

    public class Project {
        public void LoadDependencies(EpanetMSX epa) {
            this.msx = epa.Network;
            this.reader = epa.Reader;
        }

        private Network msx;
        private Dictionary<string, int>[] htable;
        private InpReader reader;

        /// <summary>Opens an EPANET-MSX project.</summary>
        public EnumTypes.ErrorCodeType MSXproj_open(string msxFile) {
            EnumTypes.ErrorCodeType errcode = 0;

            //MSX.QualityOpened = false;

            StreamReader buffReader = File.OpenText(msxFile);
            // initialize data to default values
            this.msx.SetDefaults();

            // Open the MSX input file
            //MSX.MsxFile.setFilename(fname);
            //if(!MSX.MsxFile.openAsTextReader())
            //    return ErrorCodeType.ERR_OPEN_MSX_FILE.id;

            // create hash tables to look up object ID names
            errcode = Utilities.Call(errcode, this.CreateHashTables());

            // allocate memory for the required number of objects
            errcode = Utilities.Call(errcode, this.reader.CountMsxObjects(buffReader));
            errcode = Utilities.Call(errcode, this.reader.CountNetObjects());
            errcode = Utilities.Call(errcode, this.CreateObjects());

            buffReader.Close();
            buffReader = File.OpenText(msxFile);

            // Read in the EPANET and MSX object data

            errcode = Utilities.Call(errcode, this.reader.ReadNetData());
            errcode = Utilities.Call(errcode, this.reader.ReadMsxData(buffReader));

            //if (MSX.RptFile.getFilename().equals(""))
            //    errcode = Utilities.CALL(errcode, openRptFile());

            // Convert user's units to internal units
            errcode = Utilities.Call(errcode, this.ConvertUnits());

            // Close input file
            //MSX.MsxFile.close();

            return errcode;
        }

        /// <summary>Closes the current EPANET-MSX project.</summary>
        private void MSXproj_close() {
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
            //MSX.ProjectOpened = false;
        }

        /// <summary>Adds an object ID to the project's hash tables.</summary>
        public int MSXproj_addObject(EnumTypes.ObjectTypes type, string id, int n) {
            int result = 0;
            string newId = id;

// --- do nothing if object already exists in a hash table

            if (this.MSXproj_findObject(type, id) > 0) return 0;

// --- insert object's ID into the hash table for that type of object

            //result = HTinsert(Htable[type], newID, n);

            if (this.htable[(int)type].ContainsKey(newId)) {
                this.htable[(int)type][newId] = n;
            }
            else {
                this.htable[(int)type].Add(newId, n);
                result = 1;
            }

            return result == 0 ? -1 : result;
        }

        /// <summary>Uses hash table to find index of an object with a given ID.</summary>
        public int MSXproj_findObject(EnumTypes.ObjectTypes type, string id) {
            int val;
            return this.htable[(int)type].TryGetValue(id, out val) ? val : -1;
        }

        /// <summary>Uses hash table to find address of given string entry.</summary>
        public string MSXproj_findID(EnumTypes.ObjectTypes type, string id) {
            return this.htable[(int)type].ContainsKey(id) ? id : string.Empty;
        }


        /// <summary>Converts user's units to internal EPANET units.</summary>
        private EnumTypes.ErrorCodeType ConvertUnits() {
            // Flow conversion factors (to cfs)
            double[] fcf = {
                1.0, Constants.GPMperCFS, Constants.MGDperCFS, Constants.IMGDperCFS, Constants.AFDperCFS,
                Constants.LPSperCFS, Constants.LPMperCFS, Constants.MLDperCFS, Constants.CMHperCFS, Constants.CMDperCFS
            };

            // Rate time units conversion factors (to sec)
            double[] rcf = {1.0, 60.0, 3600.0, 86400.0};

            EnumTypes.ErrorCodeType errcode = 0;

            // Conversions for length & tank volume
            if (this.msx.Unitsflag == EnumTypes.UnitSystemType.US) {
                this.msx.Ucf[(int)EnumTypes.UnitsType.LENGTH_UNITS] = 1.0;
                this.msx.Ucf[(int)EnumTypes.UnitsType.DIAM_UNITS] = 12.0;
                this.msx.Ucf[(int)EnumTypes.UnitsType.VOL_UNITS] = 1.0;
            }
            else {
                this.msx.Ucf[(int)EnumTypes.UnitsType.LENGTH_UNITS] = Constants.MperFT;
                this.msx.Ucf[(int)EnumTypes.UnitsType.DIAM_UNITS] = 1000.0 * Constants.MperFT;
                this.msx.Ucf[(int)EnumTypes.UnitsType.VOL_UNITS] = Constants.M3perFT3;
            }

            // Conversion for surface area
            this.msx.Ucf[(int)EnumTypes.UnitsType.AREA_UNITS] = 1.0;
            switch (this.msx.AreaUnits) {
            case EnumTypes.AreaUnitsType.M2:
                this.msx.Ucf[(int)EnumTypes.UnitsType.AREA_UNITS] = Constants.M2perFT2;
                break;
            case EnumTypes.AreaUnitsType.CM2:
                this.msx.Ucf[(int)EnumTypes.UnitsType.AREA_UNITS] = Constants.CM2perFT2;
                break;
            }

            // Conversion for flow rate
            this.msx.Ucf[(int)EnumTypes.UnitsType.FLOW_UNITS] = fcf[(int)this.msx.Flowflag];
            this.msx.Ucf[(int)EnumTypes.UnitsType.CONC_UNITS] = Constants.LperFT3;

            // Conversion for reaction rate time
            this.msx.Ucf[(int)EnumTypes.UnitsType.RATE_UNITS] = rcf[(int)this.msx.RateUnits];

            // Convert pipe diameter & length
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; i++) {
                this.msx.Link[i].Diam = this.msx.Link[i].Diam / this.msx.Ucf[(int)EnumTypes.UnitsType.DIAM_UNITS];
                this.msx.Link[i].Len = this.msx.Link[i].Len / this.msx.Ucf[(int)EnumTypes.UnitsType.LENGTH_UNITS];
            }

            // Convert initial tank volumes
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TANK]; i++) {
                this.msx.Tank[i].V0 = this.msx.Tank[i].V0 / this.msx.Ucf[(int)EnumTypes.UnitsType.VOL_UNITS];
                this.msx.Tank[i].VMix = this.msx.Tank[i].VMix / this.msx.Ucf[(int)EnumTypes.UnitsType.VOL_UNITS];
            }

            // Assign default tolerances to species
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; i++) {
                if (this.msx.Species[i].RTol == 0.0) this.msx.Species[i].RTol = this.msx.DefRtol;
                if (this.msx.Species[i].ATol == 0.0) this.msx.Species[i].ATol = this.msx.DefAtol;
            }

            return errcode;
        }

        /// <summary>Creates multi-species data objects.</summary>
        private EnumTypes.ErrorCodeType CreateObjects() {
            // Create nodes, links, & tanks
            this.msx.Node = new Node[this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE] + 1];
            this.msx.Link = new Link[this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + 1];
            this.msx.Tank = new Tank[this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TANK] + 1];

            // Create species, terms, parameters, constants & time patterns
            this.msx.Species = new Species[this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1];
            this.msx.Term = new Term[this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TERM] + 1];
            this.msx.Param = new Param[this.msx.Nobjects[(int)EnumTypes.ObjectTypes.PARAMETER] + 1];
            this.msx.Const = new Const[this.msx.Nobjects[(int)EnumTypes.ObjectTypes.CONSTANT] + 1];
            this.msx.Pattern = new Pattern[this.msx.Nobjects[(int)EnumTypes.ObjectTypes.PATTERN] + 1];

            for (int i = 0; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.CONSTANT]; i++)
                this.msx.Const[i] = new Const();

            // Create arrays for demands, heads, & flows
            this.msx.D = new float[this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE] + 1];
            this.msx.H = new float[this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE] + 1];
            this.msx.Q = new float[this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + 1];

            // create arrays for current & initial concen. of each species for each node
            this.msx.C0 = new double[this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1];
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; i++) {
                this.msx.Node[i] = new Node(this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1);

                //MSX.Node[i].c =  new double[MSX.Nobjects[SPECIES]+1, sizeof(double));
                //MSX.Node[i].c0 = new double[MSX.Nobjects[SPECIES]+1, sizeof(double));
                //MSX.Node[i].rpt = 0;
            }

            // Create arrays for init. concen. & kinetic parameter values for each link

            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; i++) {
                this.msx.Link[i] = new Link(
                    this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1,
                    this.msx.Nobjects[(int)EnumTypes.ObjectTypes.PARAMETER] + 1);
                //MSX.Link[i].c0 = (double *)
                //calloc(MSX.Nobjects[SPECIES]+1, sizeof(double));
                //MSX.Link[i].param = (double *)
                //calloc(MSX.Nobjects[PARAMETER]+1, sizeof(double));
                //MSX.Link[i].rpt = 0;
            }

            // Create arrays for kinetic parameter values & current concen. for each tank

            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TANK]; i++) {
                this.msx.Tank[i] = new Tank(
                    this.msx.Nobjects[(int)EnumTypes.ObjectTypes.PARAMETER] + 1,
                    this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1);

                //MSX.Tank[i].param = (double *)
                //calloc(MSX.Nobjects[PARAMETER]+1, sizeof(double));
                //MSX.Tank[i].c = (double *)
                //calloc(MSX.Nobjects[SPECIES]+1, sizeof(double));
            }

            // Initialize contents of each time pattern object

            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.PATTERN]; i++) {
                this.msx.Pattern[i] = new Pattern();
                //MSX.Pattern[i].length = 0;
                //MSX.Pattern[i].first = null;
                //MSX.Pattern[i].current = null;
            }

            // Initialize reaction rate & equil. formulas for each species

            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; i++) {
                this.msx.Species[i] = new Species();
                //MSX.Species[i].pipeExpr     = null;
                //MSX.Species[i].tankExpr     = null;
                //MSX.Species[i].pipeExprType = NO_EXPR;
                //MSX.Species[i].tankExprType = NO_EXPR;
                //MSX.Species[i].precision    = 2;
                //MSX.Species[i].rpt = 0;
            }

            // Initialize math expressions for each intermediate term

            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TERM]; i++) {
                this.msx.Term[i] = new Term();
                //MSX.Term[i].expr = null;
            }
            return 0;
        }


        /// <summary>Allocates memory for object ID hash tables.</summary>
        private EnumTypes.ErrorCodeType CreateHashTables() {


            // create a hash table for each type of object
            this.htable = new Dictionary<string, int>[(int)EnumTypes.ObjectTypes.MAX_OBJECTS];

            for (int j = 0; j < (int)EnumTypes.ObjectTypes.MAX_OBJECTS; j++) {
                this.htable[j] = new Dictionary<string, int>();
            }

            return 0;
        }

    }

}