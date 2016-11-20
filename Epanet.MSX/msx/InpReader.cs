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
using System.Diagnostics;
using System.IO;
using Epanet.MSX.Structures;
using Epanet.Util;

namespace Epanet.MSX {

    public class InpReader {

        private TraceSource log;
        private const int MAXERRS = 100; // Max. input errors reported

        private Network msx;
        private ENToolkit2 epanet;
        private Project project;

        public void LoadDependencies(EpanetMSX epa) {
            this.msx = epa.Network;
            this.epanet = epa.EnToolkit;
            this.project = epa.Project;
        }

        /// <summary>Error codes (401 - 409)</summary>
        public enum InpErrorCodes {
            INP_ERR_FIRST = 400,
            ERR_LINE_LENGTH = 401,
            ERR_ITEMS = 402,
            ERR_KEYWORD = 403,
            ERR_NUMBER = 404,
            ERR_NAME = 405,
            ERR_RESERVED_NAME = 406,
            ERR_DUP_NAME = 407,
            ERR_DUP_EXPR = 408,
            ERR_MATH_EXPR = 409,
            INP_ERR_LAST = 410
        };

        /// <summary>Respective error messages.</summary>
        private static readonly string[] InpErrorTxt = {
            "",
            "Error 401 (too many characters)",
            "Error 402 (too few input items)",
            "Error 403 (invalid keyword)",
            "Error 404 (invalid numeric value)",
            "Error 405 (reference to undefined object)",
            "Error 406 (illegal use of a reserved name)",
            "Error 407 (name already used by another object)",
            "Error 408 (species already assigned an expression)",
            "Error 409 (illegal math expression)"
        };

        /// <summary>Reads multi-species input file to determine number of system objects.</summary>
        public EnumTypes.ErrorCodeType CountMsxObjects(TextReader reader) {
            EnumTypes.SectionType sect = (EnumTypes.SectionType)(-1); // input data sections
            InpErrorCodes errcode = 0; // error code
            int errsum = 0; // number of errors found
            long lineCount = 0;


            //BufferedReader reader = (BufferedReader)MSX.MsxFile.getFileIO();

            for (;;) {
                string line; // line from input data file
                try {
                    line = reader.ReadLine();
                }
                catch (IOException) {
                    break;
                }

                if (line == null)
                    break;

                errcode = 0;
                line = line.Trim();
                lineCount++;

                int comentPosition = line.IndexOf(';');
                if (comentPosition != -1)
                    line = line.Substring(0, comentPosition);

                if (line.Length == 0)
                    continue;

                string[] tok = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

                if (tok.Length == 0 || tok[0].Length > 0 && tok[0][0] == ';') continue;

                EnumTypes.SectionType sectTemp;
                if (GetNewSection(tok[0], Constants.MsxSectWords, out sectTemp) != 0) {
                    sect = sectTemp;
                    continue;
                }

                if (sect == EnumTypes.SectionType.s_SPECIES)
                    errcode = this.AddSpecies(tok);
                if (sect == EnumTypes.SectionType.s_COEFF)
                    errcode = this.AddCoeff(tok);
                if (sect == EnumTypes.SectionType.s_TERM)
                    errcode = this.AddTerm(tok);
                if (sect == EnumTypes.SectionType.s_PATTERN)
                    errcode = this.AddPattern(tok);


                if (errcode != 0) {
                    WriteInpErrMsg(errcode, Constants.MsxSectWords[(int)sect], line, (int)lineCount);
                    errsum++;
                    if (errsum >= MAXERRS) break;
                }
            }

            //return error code

            if (errsum > 0) return EnumTypes.ErrorCodeType.ERR_MSX_INPUT;
            return (EnumTypes.ErrorCodeType)errcode;
        }

        /// <summary>Queries EPANET database to determine number of network objects.</summary>
        public EnumTypes.ErrorCodeType CountNetObjects() {
            this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE] = this.epanet.ENgetcount(ENToolkit2.EN_NODECOUNT);
            this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TANK] = this.epanet.ENgetcount(ENToolkit2.EN_TANKCOUNT);
            this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK] = this.epanet.ENgetcount(ENToolkit2.EN_LINKCOUNT);
            return 0;
        }

        /// <summary>Retrieves required input data from the EPANET project data.</summary>
        public EnumTypes.ErrorCodeType ReadNetData() {
            // Get flow units & time parameters
            this.msx.Flowflag = this.epanet.ENgetflowunits();

            this.msx.Unitsflag = this.msx.Flowflag >= EnumTypes.FlowUnitsType.LPS
                ? EnumTypes.UnitSystemType.SI
                : EnumTypes.UnitSystemType.US;

            this.msx.Dur = this.epanet.ENgettimeparam(ENToolkit2.EN_DURATION);
            this.msx.Qstep = this.epanet.ENgettimeparam(ENToolkit2.EN_QUALSTEP);
            this.msx.Rstep = this.epanet.ENgettimeparam(ENToolkit2.EN_REPORTSTEP);
            this.msx.Rstart = this.epanet.ENgettimeparam(ENToolkit2.EN_REPORTSTART);
            this.msx.Pstep = this.epanet.ENgettimeparam(ENToolkit2.EN_PATTERNSTEP);
            this.msx.Pstart = this.epanet.ENgettimeparam(ENToolkit2.EN_PATTERNSTART);
            this.msx.Statflag = (EnumTypes.TstatType)this.epanet.ENgettimeparam(ENToolkit2.EN_STATISTIC);

            // Read tank/reservoir data
            int n = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE] - this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TANK];
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; i++) {
                int k = i - n;
                if (k <= 0) continue;

                int t;
                float v0;
                float xmix;
                float vmix;

                try {
                    t = this.epanet.ENgetnodetype(i);
                    v0 = this.epanet.ENgetnodevalue(i, ENToolkit2.EN_INITVOLUME);
                    xmix = this.epanet.ENgetnodevalue(i, ENToolkit2.EN_MIXMODEL);
                    vmix = this.epanet.ENgetnodevalue(i, ENToolkit2.EN_MIXZONEVOL);
                }
                catch (Exception e) {
                    return (EnumTypes.ErrorCodeType)int.Parse(e.Message);
                }

                this.msx.Node[i].Tank = k;
                this.msx.Tank[k].Node = i;
                this.msx.Tank[k].A = t == ENToolkit2.EN_RESERVOIR ? 0.0 : 1.0;
                this.msx.Tank[k].V0 = v0;
                this.msx.Tank[k].MixModel = (EnumTypes.MixType)(int)xmix;
                this.msx.Tank[k].VMix = vmix;
            }

            // Read link data
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; i++) {
                int n1, n2;

                try {
                    this.epanet.ENgetlinknodes(i, out n1, out n2);
                }
                catch (Exception e) {
                    return (EnumTypes.ErrorCodeType)int.Parse(e.Message);
                }

                
                float roughness;
                float diam;
                float len;
                try {
                    diam = this.epanet.ENgetlinkvalue(i, ENToolkit2.EN_DIAMETER);
                    len = this.epanet.ENgetlinkvalue(i, ENToolkit2.EN_LENGTH);
                    roughness = this.epanet.ENgetlinkvalue(i, ENToolkit2.EN_ROUGHNESS);
                }
                catch (Exception e) {
                    return (EnumTypes.ErrorCodeType)int.Parse(e.Message);
                }

                this.msx.Link[i].N1 = n1;
                this.msx.Link[i].N2 = n2;
                this.msx.Link[i].Diam = diam;
                this.msx.Link[i].Len = len;
                this.msx.Link[i].Roughness = roughness;
            }
            return 0;
        }

        /// <summary>Reads multi-species data from the EPANET-MSX input file.</summary>
        public EnumTypes.ErrorCodeType ReadMsxData(TextReader rin) {
            var sect = (EnumTypes.SectionType)(-1); // input data sections
            int errsum = 0; // number of errors found
            int lineCount = 0; // line count

            // rewind
            //MSX.MsxFile.close();
            //MSX.MsxFile.openAsTextReader();

            //BufferedReader rin = (BufferedReader)MSX.MsxFile.getFileIO();

            for (;;) {
                string line; // line from input data file
                try {
                    line = rin.ReadLine();
                }
                catch (IOException) {
                    break;
                }

                if (line == null)
                    break;

                lineCount++;
                line = line.Trim();

                int comentPosition = line.IndexOf(';');
                if (comentPosition != -1)
                    line = line.Substring(0, comentPosition);

                if (line.Length == 0)
                    continue;

                string[] tok = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

                if (tok.Length == 0) continue;

                InpErrorCodes inperr; // input error code
                if (GetLineLength(line) >= Constants.MAXLINE) {
                    inperr = InpErrorCodes.ERR_LINE_LENGTH;
                    WriteInpErrMsg(inperr, Constants.MsxSectWords[(int)sect], line, lineCount);
                    errsum++;
                }

                EnumTypes.SectionType sectTmp;
                if (GetNewSection(tok[0], Constants.MsxSectWords, out sectTmp) != 0) {
                    sect = sectTmp;
                    continue;
                }

                inperr = this.ParseLine(sect, line, tok);

                if (inperr > 0) {
                    errsum++;
                    WriteInpErrMsg(inperr, Constants.MsxSectWords[(int)sect], line, lineCount);
                }

                // Stop if reach end of file or max. error count
                if (errsum >= MAXERRS) break;
            }

            if (errsum > 0)
                return (EnumTypes.ErrorCodeType)200;

            return 0;
        }

        /// <summary>Reads multi-species data from the EPANET-MSX input file.</summary>
        public string MSXinp_getSpeciesUnits(int m) {
            string units = this.msx.Species[m].Units;
            units += "/";
            if (this.msx.Species[m].Type == EnumTypes.SpeciesType.BULK)
                units += "L";
            else
                units += Constants.AreaUnitsWords[(int)this.msx.AreaUnits];

            return units;
        }

        /// <summary>Determines number of characters of data in a line of input.</summary>
        private static int GetLineLength(string line) {
            int index = line.IndexOf(';');

            return index != -1 ? line.Substring(0, index).Length : line.Length;
        }

        /// <summary>Checks if a line begins a new section in the input file.</summary>
        private static int GetNewSection(string tok, string[] sectWords, out EnumTypes.SectionType sect) {
            sect = (EnumTypes.SectionType)(-1);
            if (tok.Length == 0)
                return 0;
            // --- check if line begins with a new section heading

            if (tok[0] == '[') {
                // --- look for section heading in list of section keywords

                int newsect = Utilities.MSXutils_findmatch(tok, sectWords);
                if (newsect >= 0) sect = (EnumTypes.SectionType)newsect;
                else
                    sect = (EnumTypes.SectionType)(-1);
                return 1;
            }
            return 0;
        }

        /// <summary>Adds a species ID name to the project.</summary>
        private InpErrorCodes AddSpecies(string[] tok) {
            if (tok.Length < 2) return InpErrorCodes.ERR_ITEMS;
            InpErrorCodes errcode = this.CheckId(tok[1]);
            if (errcode != 0) return errcode;
            if (this.project.MSXproj_addObject(
                        EnumTypes.ObjectTypes.SPECIES,
                        tok[1],
                        this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1) < 0)
                errcode = (InpErrorCodes)101;
            else this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]++;
            return errcode;
        }

        /// <summary>Adds a coefficient ID name to the project.</summary>
        private InpErrorCodes AddCoeff(string[] tok) {
            EnumTypes.ObjectTypes k;

            // determine the type of coeff.

            if (tok.Length < 2) return InpErrorCodes.ERR_ITEMS;
            if (Utilities.MSXutils_match(tok[0], "PARAM")) k = EnumTypes.ObjectTypes.PARAMETER;
            else if (Utilities.MSXutils_match(tok[0], "CONST")) k = EnumTypes.ObjectTypes.CONSTANT;
            else return InpErrorCodes.ERR_KEYWORD;

            // check for valid id name

            InpErrorCodes errcode = this.CheckId(tok[1]);
            if (errcode != 0) return errcode;
            if (this.project.MSXproj_addObject(k, tok[1], this.msx.Nobjects[(int)k] + 1) < 0)
                errcode = (InpErrorCodes)101;
            else this.msx.Nobjects[(int)k]++;
            return errcode;
        }


        /// <summary>Adds an intermediate expression term ID name to the project.</summary>
        private InpErrorCodes AddTerm(string[] id) {
            InpErrorCodes errcode = this.CheckId(id[0]);
            if (errcode == 0) {
                if (this.project.MSXproj_addObject(
                            EnumTypes.ObjectTypes.TERM,
                            id[0],
                            this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TERM] + 1) < 0)
                    errcode = (InpErrorCodes)101;
                else this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TERM]++;
            }
            return errcode;
        }


        /// <summary>Adds a time pattern ID name to the project.</summary>
        private InpErrorCodes AddPattern(string[] tok) {
            InpErrorCodes errcode = 0;

            // A time pattern can span several lines

            if (this.project.MSXproj_findObject(EnumTypes.ObjectTypes.PATTERN, tok[0]) <= 0) {
                if (this.project.MSXproj_addObject(
                            EnumTypes.ObjectTypes.PATTERN,
                            tok[0],
                            this.msx.Nobjects[(int)EnumTypes.ObjectTypes.PATTERN] + 1) < 0)
                    errcode = (InpErrorCodes)101;
                else this.msx.Nobjects[(int)EnumTypes.ObjectTypes.PATTERN]++;
            }
            return errcode;
        }


        /// <summary>Checks that an object's name is unique.</summary>
        private InpErrorCodes CheckId(string id) {
            // Check that id name is not a reserved word
            foreach (string word  in  Constants.HydVarWords) {
                if (string.Equals(id, word, StringComparison.OrdinalIgnoreCase)) 
                    return InpErrorCodes.ERR_RESERVED_NAME;
            }

            // Check that id name not used before

            if (this.project.MSXproj_findObject(EnumTypes.ObjectTypes.SPECIES, id) > 0
                || this.project.MSXproj_findObject(EnumTypes.ObjectTypes.TERM, id) > 0
                || this.project.MSXproj_findObject(EnumTypes.ObjectTypes.PARAMETER, id) > 0
                || this.project.MSXproj_findObject(EnumTypes.ObjectTypes.CONSTANT, id) > 0
            ) return InpErrorCodes.ERR_DUP_NAME;
            return 0;
        }


        /// <summary>Parses the contents of a line of input data.</summary>
        private InpErrorCodes ParseLine(EnumTypes.SectionType sect, string line, string[] tok) {
            switch (sect) {
            case EnumTypes.SectionType.s_TITLE:
                this.msx.Title = line;
                break;

            case EnumTypes.SectionType.s_OPTION:
                return this.ParseOption(tok);

            case EnumTypes.SectionType.s_SPECIES:
                return this.ParseSpecies(tok);

            case EnumTypes.SectionType.s_COEFF:
                return this.ParseCoeff(tok);

            case EnumTypes.SectionType.s_TERM:
                return this.ParseTerm(tok);

            case EnumTypes.SectionType.s_PIPE:
                return this.ParseExpression(EnumTypes.ObjectTypes.LINK, tok);

            case EnumTypes.SectionType.s_TANK:
                return this.ParseExpression(EnumTypes.ObjectTypes.TANK, tok);

            case EnumTypes.SectionType.s_SOURCE:
                return this.ParseSource(tok);

            case EnumTypes.SectionType.s_QUALITY:
                return this.ParseQuality(tok);

            case EnumTypes.SectionType.s_PARAMETER:
                return this.ParseParameter(tok);

            case EnumTypes.SectionType.s_PATTERN:
                return this.ParsePattern(tok);

            case EnumTypes.SectionType.s_REPORT:
                return this.ParseReport(tok);
            }
            return 0;
        }

        /// <summary>Parses an input line containing a project option.</summary>
        private InpErrorCodes ParseOption(string[] tok) {
            // Determine which option is being read

            if (tok.Length < 2) return 0;
            int k = Utilities.MSXutils_findmatch(tok[0], Constants.OptionTypeWords);
            if (k < 0) return InpErrorCodes.ERR_KEYWORD;

            // Parse the value for the given option
            switch ((EnumTypes.OptionType)k) {
            case EnumTypes.OptionType.AREA_UNITS_OPTION:
                k = Utilities.MSXutils_findmatch(tok[1], Constants.AreaUnitsWords);
                if (k < 0) return InpErrorCodes.ERR_KEYWORD;
                this.msx.AreaUnits = (EnumTypes.AreaUnitsType)k;
                break;

            case EnumTypes.OptionType.RATE_UNITS_OPTION:
                k = Utilities.MSXutils_findmatch(tok[1], Constants.TimeUnitsWords);
                if (k < 0) return InpErrorCodes.ERR_KEYWORD;
                this.msx.RateUnits = (EnumTypes.RateUnitsType)k;
                break;

            case EnumTypes.OptionType.SOLVER_OPTION:
                k = Utilities.MSXutils_findmatch(tok[1], Constants.SolverTypeWords);
                if (k < 0) return InpErrorCodes.ERR_KEYWORD;
                this.msx.Solver = (EnumTypes.SolverType)k;
                break;

            case EnumTypes.OptionType.COUPLING_OPTION:
                k = Utilities.MSXutils_findmatch(tok[1], Constants.CouplingWords);
                if (k < 0) return InpErrorCodes.ERR_KEYWORD;
                this.msx.Coupling = (EnumTypes.CouplingType)k;
                break;

            case EnumTypes.OptionType.TIMESTEP_OPTION:
                k = int.Parse(tok[1]);
                if (k <= 0) return InpErrorCodes.ERR_NUMBER;
                this.msx.Qstep = k;
                break;

            case EnumTypes.OptionType.RTOL_OPTION: {
                double tmp;
                if (!tok[1].ToDouble(out tmp)) return InpErrorCodes.ERR_NUMBER;
                this.msx.DefRtol = tmp;
                break;
            }
            case EnumTypes.OptionType.ATOL_OPTION: {
                double tmp;
                if (!tok[1].ToDouble(out tmp)) return InpErrorCodes.ERR_NUMBER;
                this.msx.DefAtol = tmp;
            }
                break;
            }
            return 0;
        }

        /// <summary>Parses an input line containing a species variable.</summary>
        private InpErrorCodes ParseSpecies(string[] tok) {
            // Get secies index
            if (tok.Length < 3) return InpErrorCodes.ERR_ITEMS;
            int i = this.project.MSXproj_findObject(EnumTypes.ObjectTypes.SPECIES, tok[1]);
            if (i <= 0) return InpErrorCodes.ERR_NAME;

            // Get pointer to Species name
            this.msx.Species[i].Id = this.project.MSXproj_findID(EnumTypes.ObjectTypes.SPECIES, tok[1]);

            // Get species type
            if (Utilities.MSXutils_match(tok[0], "BULK")) this.msx.Species[i].Type = EnumTypes.SpeciesType.BULK;
            else if (Utilities.MSXutils_match(tok[0], "WALL")) this.msx.Species[i].Type = EnumTypes.SpeciesType.WALL;
            else return InpErrorCodes.ERR_KEYWORD;

            // Get Species units
            this.msx.Species[i].Units = tok[2];

            // Get Species error tolerance
            this.msx.Species[i].ATol = 0.0;
            this.msx.Species[i].RTol = 0.0;
            if (tok.Length >= 4) {
                double tmp;
                // BUG: Baseform bug
                if (!tok[3].ToDouble(out tmp))
                    this.msx.Species[i].ATol = tmp;
                return InpErrorCodes.ERR_NUMBER;
            }
            if (tok.Length >= 5) {
                double tmp;
                // BUG: Baseform bug
                if (!tok[4].ToDouble(out tmp)) //&MSX.Species[i].rTol) )
                    this.msx.Species[i].RTol = tmp;
                return InpErrorCodes.ERR_NUMBER;
            }
            return 0;
        }

        /// <summary>Parses an input line containing a coefficient definition.</summary>
        private InpErrorCodes ParseCoeff(string[] tok) {
            
            // Check if variable is a Parameter
            if (tok.Length < 2) return 0;
       
            if (Utilities.MSXutils_match(tok[0], "PARAM")) {
                // Get Parameter's index
                int i = this.project.MSXproj_findObject(EnumTypes.ObjectTypes.PARAMETER, tok[1]);
                if (i <= 0) return InpErrorCodes.ERR_NAME;

                // Get Parameter's value
                this.msx.Param[i].Id = this.project.MSXproj_findID(EnumTypes.ObjectTypes.PARAMETER, tok[1]);
                if (tok.Length >= 3) {
                    // BUG: Baseform bug
                    double x;
                    if (tok[2].ToDouble(out x)) return InpErrorCodes.ERR_NUMBER;
                    this.msx.Param[i].Value = x;
                    
                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; j++)
                        this.msx.Link[j].Param[i] = x;
                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TANK]; j++)
                        this.msx.Tank[j].Param[i] = x;
                }
                return 0;
            }

            // Check if variable is a Constant
            else if (Utilities.MSXutils_match(tok[0], "CONST")) {
                // Get Constant's index
                int i = this.project.MSXproj_findObject(EnumTypes.ObjectTypes.CONSTANT, tok[1]);
                if (i <= 0) return InpErrorCodes.ERR_NAME;

                // Get constant's value
                this.msx.Const[i].Id = this.project.MSXproj_findID(EnumTypes.ObjectTypes.CONSTANT, tok[1]);
                this.msx.Const[i].Value = 0.0;
                if (tok.Length >= 3) {
                    double tmp;
                    if (!tok[2].ToDouble(out tmp)) //&MSX.Const[i].value) )
                        return InpErrorCodes.ERR_NUMBER;
                    this.msx.Const[i].Value = tmp;
                }
                return 0;
            }
            else
                return InpErrorCodes.ERR_KEYWORD;
        }

       /// <summary>Parses an input line containing an intermediate expression term .</summary>
        private InpErrorCodes ParseTerm(string[] tok) {
            string s = "";

           // --- get term's name

            if (tok.Length < 2) return 0;
            int i = this.project.MSXproj_findObject(EnumTypes.ObjectTypes.TERM, tok[0]);

            // --- reconstruct the expression string from its tokens

            for (int j = 1; j < tok.Length; j++) s += tok[j];

            // --- convert expression into a postfix stack of op codes

            //expr = mathexpr_create(s, getVariableCode);
            MathExpr expr = MathExpr.Create(s, this.GetVariableCode);
            if (expr == null) return InpErrorCodes.ERR_MATH_EXPR;

            // --- assign the expression to a Term object

            this.msx.Term[i].Expr = expr;
            return 0;
        }

        /// <summary>Parses an input line containing a math expression.</summary>
        private InpErrorCodes ParseExpression(EnumTypes.ObjectTypes classType, string[] tok) {
            string s = "";

            // --- determine expression type

            if (tok.Length < 3) return InpErrorCodes.ERR_ITEMS;
            int k = Utilities.MSXutils_findmatch(tok[0], Constants.ExprTypeWords);
            if (k < 0) return InpErrorCodes.ERR_KEYWORD;

            // --- determine species associated with expression

            int i = this.project.MSXproj_findObject(EnumTypes.ObjectTypes.SPECIES, tok[1]);
            if (i < 1) return InpErrorCodes.ERR_NAME;

            // --- check that species does not already have an expression

            if (classType == EnumTypes.ObjectTypes.LINK) {
                if (this.msx.Species[i].PipeExprType != EnumTypes.ExpressionType.NO_EXPR)
                    return InpErrorCodes.ERR_DUP_EXPR;
            }

            if (classType == EnumTypes.ObjectTypes.TANK) {
                if (this.msx.Species[i].TankExprType != EnumTypes.ExpressionType.NO_EXPR)
                    return InpErrorCodes.ERR_DUP_EXPR;
            }

            // --- reconstruct the expression string from its tokens

            for (int j = 2; j < tok.Length; j++) s += tok[j];

            // --- convert expression into a postfix stack of op codes

            //expr = mathexpr_create(s, getVariableCode);
            MathExpr expr = MathExpr.Create(s, this.GetVariableCode);

            if (expr == null) return InpErrorCodes.ERR_MATH_EXPR;

            // --- assign the expression to the species

            switch (classType) {
            case EnumTypes.ObjectTypes.LINK:
                this.msx.Species[i].PipeExpr = expr;
                this.msx.Species[i].PipeExprType = (EnumTypes.ExpressionType)k;
                break;
            case EnumTypes.ObjectTypes.TANK:
                this.msx.Species[i].TankExpr = expr;
                this.msx.Species[i].TankExprType = (EnumTypes.ExpressionType)k;
                break;
            }
            return 0;
        }

        /// <summary>Parses an input line containing initial species concentrations.</summary>
        private InpErrorCodes ParseQuality(string[] tok) {
            int err, i, j, k, m;
            double x;

            // --- determine if quality value is global or object-specific

            if (tok.Length < 3) return InpErrorCodes.ERR_ITEMS;
            if (Utilities.MSXutils_match(tok[0], "GLOBAL")) i = 1;
            else if (Utilities.MSXutils_match(tok[0], "NODE")) i = 2;
            else if (Utilities.MSXutils_match(tok[0], "LINK")) i = 3;
            else return InpErrorCodes.ERR_KEYWORD;

            // --- find species index

            k = 1;
            if (i >= 2) k = 2;
            m = this.project.MSXproj_findObject(EnumTypes.ObjectTypes.SPECIES, tok[k]);
            if (m <= 0) return InpErrorCodes.ERR_NAME;

            // --- get quality value

            if (i >= 2 && tok.Length < 4) return InpErrorCodes.ERR_ITEMS;
            k = 2;
            if (i >= 2) k = 3;
            if (!tok[k].ToDouble(out x)) return InpErrorCodes.ERR_NUMBER;

            // --- for global specification, set initial quality either for
            //     all nodes or links depending on type of species

            if (i == 1) {
                this.msx.C0[m] = x;
                if (this.msx.Species[m].Type == EnumTypes.SpeciesType.BULK) {
                    for (j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; j++)
                        this.msx.Node[j].C0[m] = x;
                }
                for (j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; j++)
                    this.msx.Link[j].C0[m] = x;
            }

            // --- for a specific node, get its index & set its initial quality

            else if (i == 2) {
                int tmp;
                err = this.epanet.ENgetnodeindex(tok[1], out tmp);
                j = tmp;
                if (err != 0) return InpErrorCodes.ERR_NAME;
                if (this.msx.Species[m].Type == EnumTypes.SpeciesType.BULK) this.msx.Node[j].C0[m] = x;
            }

            // --- for a specific link, get its index & set its initial quality

            else if (i == 3) {
                int tmp;
                err = this.epanet.ENgetlinkindex(tok[1], out tmp);
                j = tmp;
                if (err != 0)
                    return InpErrorCodes.ERR_NAME;

                this.msx.Link[j].C0[m] = x;
            }
            return 0;
        }

        /// <summary>Parses an input line containing a parameter data.</summary>
        private InpErrorCodes ParseParameter(string[] tok) {
            int err, j;

            // --- get parameter name

            if (tok.Length < 4) return 0;
            int i = this.project.MSXproj_findObject(EnumTypes.ObjectTypes.PARAMETER, tok[2]);

            // --- get parameter value

            double x;
            if (!tok[3].ToDouble(out x)) return InpErrorCodes.ERR_NUMBER;
            
            // --- for pipe parameter, get pipe index and update parameter's value

            if (Utilities.MSXutils_match(tok[0], "PIPE")) {
                err = this.epanet.ENgetlinkindex(tok[1], out j);
              
                if (err != 0) return InpErrorCodes.ERR_NAME;
                this.msx.Link[j].Param[i] = x;
            }

            // --- for tank parameter, get tank index and update parameter's value

            else if (Utilities.MSXutils_match(tok[0], "TANK")) {
                err = this.epanet.ENgetnodeindex(tok[1], out j);
                if (err != 0) return InpErrorCodes.ERR_NAME;
                j = this.msx.Node[j].Tank;
                if (j > 0) this.msx.Tank[j].Param[i] = x;
            }
            else return InpErrorCodes.ERR_KEYWORD;
            return 0;
        }

    /// <summary>Parses an input line containing a source input data.</summary>
        private InpErrorCodes ParseSource(string[] tok) {
           
            Source source = null;

            // --- get source type
            if (tok.Length < 4) return InpErrorCodes.ERR_ITEMS;
            int k = Utilities.MSXutils_findmatch(tok[0], Constants.SourceTypeWords);
            if (k < 0) return InpErrorCodes.ERR_KEYWORD;

            // --- get node index
            int j;
            int err = this.epanet.ENgetnodeindex(tok[1], out j);
            if (err != 0) return InpErrorCodes.ERR_NAME;

            //  --- get species index
            int m = this.project.MSXproj_findObject(EnumTypes.ObjectTypes.SPECIES, tok[2]);
            if (m <= 0) return InpErrorCodes.ERR_NAME;

            // --- check that species is a BULK species
            if (this.msx.Species[m].Type != EnumTypes.SpeciesType.BULK) return 0;

            // --- get base strength
            double x;
            if (!tok[3].ToDouble(out x)) return InpErrorCodes.ERR_NUMBER;
       
            // --- get time pattern if present
            var i = 0;
            if (tok.Length >= 5) {
                i = this.project.MSXproj_findObject(EnumTypes.ObjectTypes.PATTERN, tok[4]);
                if (i <= 0) return InpErrorCodes.ERR_NAME;
            }

            // --- check if a source for this species already exists
            foreach (Source src  in  this.msx.Node[j].Sources) {
                if (src.Species == m) {
                    source = src;
                    break;
                }

            }

            // --- otherwise create a new source object
            if (source == null) {
                source = new Source(); //(struct Ssource *) malloc(sizeof(struct Ssource));
                //if ( source == NULL ) return 101;
                //source->next = MSX.Node[j].sources;
                //MSX.Node[j].sources = source;
                this.msx.Node[j].Sources.Insert(0, source);
            }

            // --- save source's properties

            source.Type = (EnumTypes.SourceType)k;
            source.Species = m;
            source.C0 = x;
            source.Pattern = i;
            return 0;
        }

        /// <summary>Parses an input line containing a time pattern data.</summary>
        private InpErrorCodes ParsePattern(string[] tok) {

            // --- get time pattern index
            if (tok.Length < 2) return InpErrorCodes.ERR_ITEMS;
            int i = this.project.MSXproj_findObject(EnumTypes.ObjectTypes.PATTERN, tok[0]);
            if (i <= 0) return InpErrorCodes.ERR_NAME;
            this.msx.Pattern[i].Id = this.project.MSXproj_findID(EnumTypes.ObjectTypes.PATTERN, tok[0]);

            // --- begin reading pattern multipliers


            for (int k = 1; k < tok.Length; k++) //string token : Tok)
            {
                double x;
                if (!tok[k].ToDouble(out x)) return InpErrorCodes.ERR_NUMBER;

                this.msx.Pattern[i].Multipliers.Add(x);


                // k++;
            }
            return 0;
        }

        private InpErrorCodes ParseReport(string[] tok) {
            int err;

            // Get keyword
            if (tok.Length < 2)
                return 0;

            int k = Utilities.MSXutils_findmatch(tok[0], Constants.ReportWords);

            if (k < 0)
                return InpErrorCodes.ERR_KEYWORD;

            switch (k) {
            // Keyword is NODE; parse ID names of reported nodes
            case 0:
                if (string.Equals(tok[1], Constants.ALL, StringComparison.OrdinalIgnoreCase)) {
                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; j++)
                        this.msx.Node[j].Rpt = true;
                }
                else if (string.Equals(tok[1], Constants.NONE, StringComparison.OrdinalIgnoreCase)) {
                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; j++)
                        this.msx.Node[j].Rpt = false;
                }
                else
                    for (int i = 1; i < tok.Length; i++) {
                        int j;
                        err = this.epanet.ENgetnodeindex(tok[i], out j);

                        if (err != 0)
                            return InpErrorCodes.ERR_NAME;

                        this.msx.Node[j].Rpt = true;
                    }
                break;

            // Keyword is LINK: parse ID names of reported links
            case 1:
                if (string.Equals(tok[1], Constants.ALL, StringComparison.OrdinalIgnoreCase)) {
                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; j++)
                        this.msx.Link[j].Rpt = true;
                }
                else if (string.Equals(tok[1], Constants.NONE, StringComparison.OrdinalIgnoreCase)) {
                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; j++)
                        this.msx.Link[j].Rpt = false;
                }
                else
                    for (int i = 1; i < tok.Length; i++) {
                        int j;
                        err = this.epanet.ENgetlinkindex(tok[i], out j);
                        if (err != 0) return InpErrorCodes.ERR_NAME;
                        this.msx.Link[j].Rpt = true;
                    }
                break;

            // Keyword is SPECIES; get YES/NO & precision
            case 2: {
                int j = this.project.MSXproj_findObject(EnumTypes.ObjectTypes.SPECIES, tok[1]);
                if (j <= 0) return InpErrorCodes.ERR_NAME;

                if (tok.Length >= 3) {
                    if (string.Equals(tok[2], Constants.YES, StringComparison.OrdinalIgnoreCase)) this.msx.Species[j].Rpt = 1;
                    else if (string.Equals(tok[2], Constants.NO, StringComparison.OrdinalIgnoreCase)) this.msx.Species[j].Rpt = 0;
                    else return InpErrorCodes.ERR_KEYWORD;
                }

                if (tok.Length >= 4) {
                    int i;
                    // BUG: Baseform bug
                    if (!int.TryParse(tok[3], out i)) ;
                    this.msx.Species[j].Precision = i;
                    return InpErrorCodes.ERR_NUMBER;
                }
            }
                break;

            // Keyword is FILE: get name of report file
            case 3:
                this.msx.RptFilename = tok[1];
                break;

            // Keyword is PAGESIZE;
            case 4: {
                int i;
                if (!int.TryParse(tok[1], out i))
                    return InpErrorCodes.ERR_NUMBER;
                this.msx.PageSize = i;
            }
                break;
            }
            return 0;
        }

        /// <summary>
        ///  Finds the index assigned to a species, intermediate term, parameter, or constant that appears in a math expression.
        /// </summary>
        private int GetVariableCode(string id) {
            int j = this.project.MSXproj_findObject(EnumTypes.ObjectTypes.SPECIES, id);

            if (j >= 1) return j;

            j = this.project.MSXproj_findObject(EnumTypes.ObjectTypes.TERM, id);

            if (j >= 1) return this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + j;

            j = this.project.MSXproj_findObject(EnumTypes.ObjectTypes.PARAMETER, id);

            if (j >= 1)
                return this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]
                       + this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TERM] +
                       j;

            j = this.project.MSXproj_findObject(EnumTypes.ObjectTypes.CONSTANT, id);

            if (j >= 1)
                return this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]
                       + this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TERM]
                       + this.msx.Nobjects[(int)EnumTypes.ObjectTypes.PARAMETER] + j;

            j = Utilities.MSXutils_findmatch(id, Constants.HydVarWords);

            if (j >= 1)
                return this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]
                       + this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TERM]
                       + this.msx.Nobjects[(int)EnumTypes.ObjectTypes.PARAMETER]
                       + this.msx.Nobjects[(int)EnumTypes.ObjectTypes.CONSTANT] + j;
            return -1;
        }

        private static void WriteInpErrMsg(InpErrorCodes errcode, string sect, string line, int lineCount) {

            if (errcode >= InpErrorCodes.INP_ERR_LAST || errcode <= InpErrorCodes.INP_ERR_FIRST) {
                Console.Error.WriteLine("Error Code = {0}", (int)errcode);
            }
            else {
                Console.Error.WriteLine(
                           "{0} at line {1} of {2}] section:",
                           InpErrorTxt[errcode - InpErrorCodes.INP_ERR_FIRST],
                           lineCount,
                           sect);
            }

        }

    }

}
