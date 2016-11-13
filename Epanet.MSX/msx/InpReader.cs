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
using System.Diagnostics;
using System.IO;
using org.addition.epanet.msx.Structures;
using org.addition.epanet.util;

namespace org.addition.epanet.msx {

    public class InpReader {

        private TraceSource log;
        private const int MAXERRS = 100; // Max. input errors reported

        private Network MSX;
        private ENToolkit2 epanet;
        private Project project;

        public void loadDependencies(EpanetMSX epa) {
            this.MSX = epa.getNetwork();
            this.epanet = epa.getENToolkit();
            this.project = epa.getProject();
        }

        // Error codes (401 - 409)
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

        // Respective error messages.
        private static string[] InpErrorTxt = {
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

        // Reads multi-species input file to determine number of system objects.
        public EnumTypes.ErrorCodeType countMsxObjects(TextReader reader) {
            string line; // line from input data file
            EnumTypes.SectionType sect = (EnumTypes.SectionType)(-1); // input data sections
            InpErrorCodes errcode = 0; // error code
            int errsum = 0; // number of errors found
            long lineCount = 0;


            //MSX.Msg+=MSX.MsxFile.getFilename();
            //epanet.ENwriteline(MSX.Msg);
            //epanet.ENwriteline("");

            //BufferedReader reader = (BufferedReader)MSX.MsxFile.getFileIO();

            for (;;) {
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

                EnumTypes.SectionType sect_temp;
                if (getNewSection(tok[0], Constants.MsxSectWords, out sect_temp) != 0) {
                    sect = sect_temp;
                    continue;
                }

                if (sect == EnumTypes.SectionType.s_SPECIES)
                    errcode = addSpecies(tok);
                if (sect == EnumTypes.SectionType.s_COEFF)
                    errcode = addCoeff(tok);
                if (sect == EnumTypes.SectionType.s_TERM)
                    errcode = addTerm(tok);
                if (sect == EnumTypes.SectionType.s_PATTERN)
                    errcode = addPattern(tok);


                if (errcode != 0) {
                    writeInpErrMsg(errcode, Constants.MsxSectWords[(int)sect], line, (int)lineCount);
                    errsum++;
                    if (errsum >= MAXERRS) break;
                }
            }

            //return error code

            if (errsum > 0) return EnumTypes.ErrorCodeType.ERR_MSX_INPUT;
            return (EnumTypes.ErrorCodeType)errcode;
        }

        // Queries EPANET database to determine number of network objects.
        public EnumTypes.ErrorCodeType countNetObjects() {
            MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE] = epanet.ENgetcount(ENToolkit2.EN_NODECOUNT);
            MSX.Nobjects[(int)EnumTypes.ObjectTypes.TANK] = epanet.ENgetcount(ENToolkit2.EN_TANKCOUNT);
            MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK] = epanet.ENgetcount(ENToolkit2.EN_LINKCOUNT);
            return 0;
        }

        // retrieves required input data from the EPANET project data.
        public EnumTypes.ErrorCodeType readNetData() {
            int i, k, n, t = 0;
            int n1 = 0, n2 = 0;
            float diam, len, v0, xmix, vmix;
            float roughness = 0.0f;

            // Get flow units & time parameters
            MSX.Flowflag = epanet.ENgetflowunits();

            MSX.Unitsflag = MSX.Flowflag >= EnumTypes.FlowUnitsType.LPS
                ? EnumTypes.UnitSystemType.SI
                : EnumTypes.UnitSystemType.US;

            MSX.Dur = epanet.ENgettimeparam(ENToolkit2.EN_DURATION);
            MSX.Qstep = epanet.ENgettimeparam(ENToolkit2.EN_QUALSTEP);
            MSX.Rstep = epanet.ENgettimeparam(ENToolkit2.EN_REPORTSTEP);
            MSX.Rstart = epanet.ENgettimeparam(ENToolkit2.EN_REPORTSTART);
            MSX.Pstep = epanet.ENgettimeparam(ENToolkit2.EN_PATTERNSTEP);
            MSX.Pstart = epanet.ENgettimeparam(ENToolkit2.EN_PATTERNSTART);
            MSX.Statflag = (EnumTypes.TstatType)epanet.ENgettimeparam(ENToolkit2.EN_STATISTIC);

            // Read tank/reservoir data
            n = MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE] - MSX.Nobjects[(int)EnumTypes.ObjectTypes.TANK];
            for (i = 1; i <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; i++) {
                k = i - n;
                if (k > 0) {
                    try {
                        t = epanet.ENgetnodetype(i);
                        v0 = epanet.ENgetnodevalue(i, ENToolkit2.EN_INITVOLUME);
                        xmix = epanet.ENgetnodevalue(i, ENToolkit2.EN_MIXMODEL);
                        vmix = epanet.ENgetnodevalue(i, ENToolkit2.EN_MIXZONEVOL);
                    }
                    catch (Exception e) {
                        return (EnumTypes.ErrorCodeType)int.Parse(e.Message);
                    }

                    MSX.Node[i].setTank(k);
                    MSX.Tank[k].setNode(i);
                    if (t == ENToolkit2.EN_RESERVOIR)
                        MSX.Tank[k].setA(0.0);
                    else
                        MSX.Tank[k].setA(1.0);
                    MSX.Tank[k].setV0(v0);
                    MSX.Tank[k].setMixModel((EnumTypes.MixType)(int)xmix);
                    MSX.Tank[k].setvMix(vmix);
                }
            }

            // Read link data
            for (i = 1; i <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; i++) {
                int[] n_temp;
                try {
                    n_temp = epanet.ENgetlinknodes(i);
                }
                catch (Exception e) {
                    return (EnumTypes.ErrorCodeType)int.Parse(e.Message);
                }
                n1 = n_temp[0];
                n2 = n_temp[1];
                try {
                    diam = epanet.ENgetlinkvalue(i, ENToolkit2.EN_DIAMETER);
                    len = epanet.ENgetlinkvalue(i, ENToolkit2.EN_LENGTH);
                    roughness = epanet.ENgetlinkvalue(i, ENToolkit2.EN_ROUGHNESS);
                }
                catch (Exception e) {
                    return (EnumTypes.ErrorCodeType)int.Parse(e.Message);
                }

                MSX.Link[i].setN1(n1);
                MSX.Link[i].setN2(n2);
                MSX.Link[i].setDiam(diam);
                MSX.Link[i].setLen(len);
                MSX.Link[i].setRoughness(roughness);
            }
            return 0;
        }

        // Reads multi-species data from the EPANET-MSX input file.
        public EnumTypes.ErrorCodeType readMsxData(TextReader rin) {
            string line; // line from input data file
            var sect = (EnumTypes.SectionType)(-1); // input data sections
            int errsum = 0; // number of errors found
            InpErrorCodes inperr = 0; // input error code
            int lineCount = 0; // line count

            // rewind
            //MSX.MsxFile.close();
            //MSX.MsxFile.openAsTextReader();

            //BufferedReader rin = (BufferedReader)MSX.MsxFile.getFileIO();

            for (;;) {

                try {
                    line = rin.ReadLine();
                }
                catch (IOException e) {
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

                if (getLineLength(line) >= Constants.MAXLINE) {
                    inperr = InpErrorCodes.ERR_LINE_LENGTH;
                    writeInpErrMsg(inperr, Constants.MsxSectWords[(int)sect], line, lineCount);
                    errsum++;
                }

                EnumTypes.SectionType sect_tmp;
                if (getNewSection(tok[0], Constants.MsxSectWords, out sect_tmp) != 0) {
                    sect = sect_tmp;
                    continue;
                }

                inperr = parseLine(sect, line, tok);

                if (inperr > 0) {
                    errsum++;
                    writeInpErrMsg(inperr, Constants.MsxSectWords[(int)sect], line, lineCount);
                }

                // Stop if reach end of file or max. error count
                if (errsum >= MAXERRS) break;
            }

            if (errsum > 0)
                return (EnumTypes.ErrorCodeType)200;

            return 0;
        }

        //  reads multi-species data from the EPANET-MSX input file.
        public string MSXinp_getSpeciesUnits(int m) {
            string units = MSX.Species[m].getUnits();
            units += "/";
            if (MSX.Species[m].getType() == EnumTypes.SpeciesType.BULK)
                units += "L";
            else
                units += Constants.AreaUnitsWords[(int)MSX.AreaUnits];

            return units;
        }

        // determines number of characters of data in a line of input.
        private int getLineLength(string line) {
            int index = line.IndexOf(';');

            if (index != -1) {
                return line.Substring(0, index).Length;
            }

            return line.Length;
        }

        // checks if a line begins a new section in the input file.
        private int getNewSection(string tok, string[] sectWords, out EnumTypes.SectionType sect) {
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

        // adds a species ID name to the project.
        private InpErrorCodes addSpecies(string[] Tok) {
            InpErrorCodes errcode;
            if (Tok.Length < 2) return InpErrorCodes.ERR_ITEMS;
            errcode = checkID(Tok[1]);
            if (errcode != 0) return errcode;
            if (
                project.MSXproj_addObject(EnumTypes.ObjectTypes.SPECIES, Tok[1],
                    MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1) < 0)
                errcode = (InpErrorCodes)101;
            else MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]++;
            return errcode;
        }

        // adds a coefficient ID name to the project.
        private InpErrorCodes addCoeff(string[] Tok) {
            EnumTypes.ObjectTypes k;
            InpErrorCodes errcode;

            // determine the type of coeff.

            if (Tok.Length < 2) return InpErrorCodes.ERR_ITEMS;
            if (Utilities.MSXutils_match(Tok[0], "PARAM")) k = EnumTypes.ObjectTypes.PARAMETER;
            else if (Utilities.MSXutils_match(Tok[0], "CONST")) k = EnumTypes.ObjectTypes.CONSTANT;
            else return InpErrorCodes.ERR_KEYWORD;

            // check for valid id name

            errcode = checkID(Tok[1]);
            if (errcode != 0) return errcode;
            if (project.MSXproj_addObject(k, Tok[1], MSX.Nobjects[(int)k] + 1) < 0)
                errcode = (InpErrorCodes)101;
            else MSX.Nobjects[(int)k]++;
            return errcode;
        }


        // adds an intermediate expression term ID name to the project.
        private InpErrorCodes addTerm(string[] id) {
            InpErrorCodes errcode = checkID(id[0]);
            if (errcode == 0) {
                if (
                    project.MSXproj_addObject(EnumTypes.ObjectTypes.TERM, id[0],
                        MSX.Nobjects[(int)EnumTypes.ObjectTypes.TERM] + 1) < 0)
                    errcode = (InpErrorCodes)101;
                else MSX.Nobjects[(int)EnumTypes.ObjectTypes.TERM]++;
            }
            return errcode;
        }


        // adds a time pattern ID name to the project.
        private InpErrorCodes addPattern(string[] tok) {
            InpErrorCodes errcode = 0;

            // A time pattern can span several lines

            if (project.MSXproj_findObject(EnumTypes.ObjectTypes.PATTERN, tok[0]) <= 0) {
                if (
                    project.MSXproj_addObject(EnumTypes.ObjectTypes.PATTERN, tok[0],
                        MSX.Nobjects[(int)EnumTypes.ObjectTypes.PATTERN] + 1) < 0)
                    errcode = (InpErrorCodes)101;
                else MSX.Nobjects[(int)EnumTypes.ObjectTypes.PATTERN]++;
            }
            return errcode;
        }


        // checks that an object's name is unique
        private InpErrorCodes checkID(string id) {
            // Check that id name is not a reserved word
            int i = 1;
            //while (HydVarWords[i] != NULL)
            foreach (string word  in  Constants.HydVarWords) {
                if (Utilities.MSXutils_strcomp(id, word)) return InpErrorCodes.ERR_RESERVED_NAME;
                i++;
            }

            // Check that id name not used before

            if (project.MSXproj_findObject(EnumTypes.ObjectTypes.SPECIES, id) > 0 ||
                project.MSXproj_findObject(EnumTypes.ObjectTypes.TERM, id) > 0 ||
                project.MSXproj_findObject(EnumTypes.ObjectTypes.PARAMETER, id) > 0 ||
                project.MSXproj_findObject(EnumTypes.ObjectTypes.CONSTANT, id) > 0
                ) return InpErrorCodes.ERR_DUP_NAME;
            return 0;
        }


        // parses the contents of a line of input data.
        private InpErrorCodes parseLine(EnumTypes.SectionType sect, string line, string[] Tok) {
            switch (sect) {
                case EnumTypes.SectionType.s_TITLE:
                    MSX.Title = line;
                    break;

                case EnumTypes.SectionType.s_OPTION:
                    return parseOption(Tok);

                case EnumTypes.SectionType.s_SPECIES:
                    return parseSpecies(Tok);

                case EnumTypes.SectionType.s_COEFF:
                    return parseCoeff(Tok);

                case EnumTypes.SectionType.s_TERM:
                    return parseTerm(Tok);

                case EnumTypes.SectionType.s_PIPE:
                    return parseExpression(EnumTypes.ObjectTypes.LINK, Tok);

                case EnumTypes.SectionType.s_TANK:
                    return parseExpression(EnumTypes.ObjectTypes.TANK, Tok);

                case EnumTypes.SectionType.s_SOURCE:
                    return parseSource(Tok);

                case EnumTypes.SectionType.s_QUALITY:
                    return parseQuality(Tok);

                case EnumTypes.SectionType.s_PARAMETER:
                    return parseParameter(Tok);

                case EnumTypes.SectionType.s_PATTERN:
                    return parsePattern(Tok);

                case EnumTypes.SectionType.s_REPORT:
                    return parseReport(Tok);
            }
            return 0;
        }

        // parses an input line containing a project option.
        private InpErrorCodes parseOption(string[] Tok) {
            int k;

            // Determine which option is being read

            if (Tok.Length < 2) return 0;
            k = Utilities.MSXutils_findmatch(Tok[0], Constants.OptionTypeWords);
            if (k < 0) return InpErrorCodes.ERR_KEYWORD;

            // Parse the value for the given option
            switch ((EnumTypes.OptionType)k) {
                case EnumTypes.OptionType.AREA_UNITS_OPTION:
                    k = Utilities.MSXutils_findmatch(Tok[1], Constants.AreaUnitsWords);
                    if (k < 0) return InpErrorCodes.ERR_KEYWORD;
                    MSX.AreaUnits = (EnumTypes.AreaUnitsType)k;
                    break;

                case EnumTypes.OptionType.RATE_UNITS_OPTION:
                    k = Utilities.MSXutils_findmatch(Tok[1], Constants.TimeUnitsWords);
                    if (k < 0) return InpErrorCodes.ERR_KEYWORD;
                    MSX.RateUnits = (EnumTypes.RateUnitsType)k;
                    break;

                case EnumTypes.OptionType.SOLVER_OPTION:
                    k = Utilities.MSXutils_findmatch(Tok[1], Constants.SolverTypeWords);
                    if (k < 0) return InpErrorCodes.ERR_KEYWORD;
                    MSX.Solver = (EnumTypes.SolverType)k;
                    break;

                case EnumTypes.OptionType.COUPLING_OPTION:
                    k = Utilities.MSXutils_findmatch(Tok[1], Constants.CouplingWords);
                    if (k < 0) return InpErrorCodes.ERR_KEYWORD;
                    MSX.Coupling = (EnumTypes.CouplingType)k;
                    break;

                case EnumTypes.OptionType.TIMESTEP_OPTION:
                    k = int.Parse(Tok[1]);
                    if (k <= 0) return InpErrorCodes.ERR_NUMBER;
                    MSX.Qstep = k;
                    break;

                case EnumTypes.OptionType.RTOL_OPTION: {
                    double tmp;
                    if (!Tok[1].ToDouble(out tmp)) return InpErrorCodes.ERR_NUMBER;
                    MSX.DefRtol = tmp;
                    break;
                }
                case EnumTypes.OptionType.ATOL_OPTION: {
                    double tmp;
                    if (!Tok[1].ToDouble(out tmp)) return InpErrorCodes.ERR_NUMBER;
                    MSX.DefAtol = tmp;
                }
                    break;
            }
            return 0;
        }

        // Parses an input line containing a species variable.
        private InpErrorCodes parseSpecies(string[] Tok) {
            int i;

            // Get secies index
            if (Tok.Length < 3) return InpErrorCodes.ERR_ITEMS;
            i = project.MSXproj_findObject(EnumTypes.ObjectTypes.SPECIES, Tok[1]);
            if (i <= 0) return InpErrorCodes.ERR_NAME;

            // Get pointer to Species name
            MSX.Species[i].setId(project.MSXproj_findID(EnumTypes.ObjectTypes.SPECIES, Tok[1]));

            // Get species type
            if (Utilities.MSXutils_match(Tok[0], "BULK")) MSX.Species[i].setType(EnumTypes.SpeciesType.BULK);
            else if (Utilities.MSXutils_match(Tok[0], "WALL")) MSX.Species[i].setType(EnumTypes.SpeciesType.WALL);
            else return InpErrorCodes.ERR_KEYWORD;

            // Get Species units
            MSX.Species[i].setUnits(Tok[2]);

            // Get Species error tolerance
            MSX.Species[i].setaTol(0.0);
            MSX.Species[i].setrTol(0.0);
            if (Tok.Length >= 4) {
                double tmp;
                // BUG: Baseform bug
                if (!Tok[3].ToDouble(out tmp))
                    MSX.Species[i].setaTol(tmp);
                return InpErrorCodes.ERR_NUMBER;
            }
            if (Tok.Length >= 5) {
                double tmp;
                // BUG: Baseform bug
                if (!Tok[4].ToDouble(out tmp)) //&MSX.Species[i].rTol) )
                    MSX.Species[i].setrTol(tmp);
                return InpErrorCodes.ERR_NUMBER;
            }
            return 0;
        }

        // parses an input line containing a coefficient definition.
        private InpErrorCodes parseCoeff(string[] Tok) {
            int i, j;
            double x;

            // Check if variable is a Parameter
            if (Tok.Length < 2) return 0;
            if (Utilities.MSXutils_match(Tok[0], "PARAM")) {
                // Get Parameter's index
                i = project.MSXproj_findObject(EnumTypes.ObjectTypes.PARAMETER, Tok[1]);
                if (i <= 0) return InpErrorCodes.ERR_NAME;

                // Get Parameter's value
                MSX.Param[i].setId(project.MSXproj_findID(EnumTypes.ObjectTypes.PARAMETER, Tok[1]));
                if (Tok.Length >= 3) {
                    // BUG: Baseform bug
                    if (Tok[2].ToDouble(out x)) return InpErrorCodes.ERR_NUMBER;
                    MSX.Param[i].setValue(x);
                    for (j = 1; j <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; j++) MSX.Link[j].getParam()[i] = x;
                    for (j = 1; j <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.TANK]; j++) MSX.Tank[j].getParam()[i] = x;
                }
                return 0;
            }

                // Check if variable is a Constant
            else if (Utilities.MSXutils_match(Tok[0], "CONST")) {
                // Get Constant's index
                i = project.MSXproj_findObject(EnumTypes.ObjectTypes.CONSTANT, Tok[1]);
                if (i <= 0) return InpErrorCodes.ERR_NAME;

                // Get constant's value
                MSX.Const[i].setId(project.MSXproj_findID(EnumTypes.ObjectTypes.CONSTANT, Tok[1]));
                MSX.Const[i].setValue(0.0);
                if (Tok.Length >= 3) {
                    double tmp;
                    if (!Tok[2].ToDouble(out tmp)) //&MSX.Const[i].value) )
                        return InpErrorCodes.ERR_NUMBER;
                    MSX.Const[i].setValue(tmp);
                }
                return 0;
            }
            else
                return InpErrorCodes.ERR_KEYWORD;
        }

        //=============================================================================
        // parses an input line containing an intermediate expression term .
        private InpErrorCodes parseTerm(string[] Tok) {
            int i, j;
            string s = "";
            MathExpr expr;

            // --- get term's name

            if (Tok.Length < 2) return 0;
            i = project.MSXproj_findObject(EnumTypes.ObjectTypes.TERM, Tok[0]);

            // --- reconstruct the expression string from its tokens

            for (j = 1; j < Tok.Length; j++) s += Tok[j];

            // --- convert expression into a postfix stack of op codes

            //expr = mathexpr_create(s, getVariableCode);
            expr = MathExpr.create(s, new VariableContainer(id => 0, this.getVariableCode));
            if (expr == null) return InpErrorCodes.ERR_MATH_EXPR;

            // --- assign the expression to a Term object

            MSX.Term[i].setExpr(expr);
            return 0;
        }

        //=============================================================================
        // parses an input line containing a math expression.
        private InpErrorCodes parseExpression(EnumTypes.ObjectTypes classType, string[] Tok) {
            int i, j, k;
            string s = "";
            MathExpr expr;

            // --- determine expression type

            if (Tok.Length < 3) return InpErrorCodes.ERR_ITEMS;
            k = Utilities.MSXutils_findmatch(Tok[0], Constants.ExprTypeWords);
            if (k < 0) return InpErrorCodes.ERR_KEYWORD;

            // --- determine species associated with expression

            i = project.MSXproj_findObject(EnumTypes.ObjectTypes.SPECIES, Tok[1]);
            if (i < 1) return InpErrorCodes.ERR_NAME;

            // --- check that species does not already have an expression

            if (classType == EnumTypes.ObjectTypes.LINK) {
                if (MSX.Species[i].getPipeExprType() != EnumTypes.ExpressionType.NO_EXPR)
                    return InpErrorCodes.ERR_DUP_EXPR;
            }

            if (classType == EnumTypes.ObjectTypes.TANK) {
                if (MSX.Species[i].getTankExprType() != EnumTypes.ExpressionType.NO_EXPR)
                    return InpErrorCodes.ERR_DUP_EXPR;
            }

            // --- reconstruct the expression string from its tokens

            for (j = 2; j < Tok.Length; j++) s += Tok[j];

            // --- convert expression into a postfix stack of op codes

            //expr = mathexpr_create(s, getVariableCode);
            expr = MathExpr.create(s, new VariableContainer(id => 0, this.getVariableCode)); //createMathExpr()

            if (expr == null) return InpErrorCodes.ERR_MATH_EXPR;

            // --- assign the expression to the species

            switch (classType) {
                case EnumTypes.ObjectTypes.LINK:
                    MSX.Species[i].setPipeExpr(expr);
                    MSX.Species[i].setPipeExprType((EnumTypes.ExpressionType)k);
                    break;
                case EnumTypes.ObjectTypes.TANK:
                    MSX.Species[i].setTankExpr(expr);
                    MSX.Species[i].setTankExprType((EnumTypes.ExpressionType)k);
                    break;
            }
            return 0;
        }

        //=============================================================================
        // parses an input line containing initial species concentrations.
        private InpErrorCodes parseQuality(string[] Tok) {
            int err, i, j, k, m;
            double x;

            // --- determine if quality value is global or object-specific

            if (Tok.Length < 3) return InpErrorCodes.ERR_ITEMS;
            if (Utilities.MSXutils_match(Tok[0], "GLOBAL")) i = 1;
            else if (Utilities.MSXutils_match(Tok[0], "NODE")) i = 2;
            else if (Utilities.MSXutils_match(Tok[0], "LINK")) i = 3;
            else return InpErrorCodes.ERR_KEYWORD;

            // --- find species index

            k = 1;
            if (i >= 2) k = 2;
            m = project.MSXproj_findObject(EnumTypes.ObjectTypes.SPECIES, Tok[k]);
            if (m <= 0) return InpErrorCodes.ERR_NAME;

            // --- get quality value

            if (i >= 2 && Tok.Length < 4) return InpErrorCodes.ERR_ITEMS;
            k = 2;
            if (i >= 2) k = 3;
            if (!Tok[k].ToDouble(out x)) return InpErrorCodes.ERR_NUMBER;

            // --- for global specification, set initial quality either for
            //     all nodes or links depending on type of species

            if (i == 1) {
                MSX.C0[m] = x;
                if (MSX.Species[m].getType() == EnumTypes.SpeciesType.BULK) {
                    for (j = 1; j <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; j++) MSX.Node[j].getC0()[m] = x;
                }
                for (j = 1; j <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; j++) MSX.Link[j].getC0()[m] = x;
            }

                // --- for a specific node, get its index & set its initial quality

            else if (i == 2) {
                int tmp;
                err = epanet.ENgetnodeindex(Tok[1], out tmp);
                j = tmp;
                if (err != 0) return InpErrorCodes.ERR_NAME;
                if (MSX.Species[m].getType() == EnumTypes.SpeciesType.BULK) MSX.Node[j].getC0()[m] = x;
            }

                // --- for a specific link, get its index & set its initial quality

            else if (i == 3) {
                int tmp;
                err = epanet.ENgetlinkindex(Tok[1], out tmp);
                j = tmp;
                if (err != 0)
                    return InpErrorCodes.ERR_NAME;

                MSX.Link[j].getC0()[m] = x;
            }
            return 0;
        }

        //=============================================================================
        // parses an input line containing a parameter data.
        private InpErrorCodes parseParameter(string[] Tok) {
            int err, i, j;
            double x;

            // --- get parameter name

            if (Tok.Length < 4) return 0;
            i = project.MSXproj_findObject(EnumTypes.ObjectTypes.PARAMETER, Tok[2]);

            // --- get parameter value

            double x_tmp;
            if (!Tok[3].ToDouble(out x_tmp)) return InpErrorCodes.ERR_NUMBER;
            x = x_tmp;
            // --- for pipe parameter, get pipe index and update parameter's value

            if (Utilities.MSXutils_match(Tok[0], "PIPE")) {
                int j_tmp;
                err = epanet.ENgetlinkindex(Tok[1], out j_tmp);
                j = j_tmp;
                if (err != 0) return InpErrorCodes.ERR_NAME;
                MSX.Link[j].getParam()[i] = x;
            }

                // --- for tank parameter, get tank index and update parameter's value

            else if (Utilities.MSXutils_match(Tok[0], "TANK")) {
                int j_temp;
                err = epanet.ENgetnodeindex(Tok[1], out j_temp);
                j = j_temp;
                if (err != 0) return InpErrorCodes.ERR_NAME;
                j = MSX.Node[j].getTank();
                if (j > 0) MSX.Tank[j].getParam()[i] = x;
            }
            else return InpErrorCodes.ERR_KEYWORD;
            return 0;
        }

        //=============================================================================
        // parses an input line containing a source input data.
        private InpErrorCodes parseSource(string[] Tok) {
            int err, i, j, k, m;
            double x;
            Source source = null;

            // --- get source type

            if (Tok.Length < 4) return InpErrorCodes.ERR_ITEMS;
            k = Utilities.MSXutils_findmatch(Tok[0], Constants.SourceTypeWords);
            if (k < 0) return InpErrorCodes.ERR_KEYWORD;

            // --- get node index

            int j_tmp;
            err = epanet.ENgetnodeindex(Tok[1], out j_tmp);
            j = j_tmp;
            if (err != 0) return InpErrorCodes.ERR_NAME;

            //  --- get species index

            m = project.MSXproj_findObject(EnumTypes.ObjectTypes.SPECIES, Tok[2]);
            if (m <= 0) return InpErrorCodes.ERR_NAME;

            // --- check that species is a BULK species

            if (MSX.Species[m].getType() != EnumTypes.SpeciesType.BULK) return 0;

            // --- get base strength

            double x_tmp;
            if (!Tok[3].ToDouble(out x_tmp)) return InpErrorCodes.ERR_NUMBER;
            x = x_tmp;
            // --- get time pattern if present

            i = 0;
            if (Tok.Length >= 5) {
                i = project.MSXproj_findObject(EnumTypes.ObjectTypes.PATTERN, Tok[4]);
                if (i <= 0) return InpErrorCodes.ERR_NAME;
            }

            // --- check if a source for this species already exists

            /*source = MSX.Node[j].sources;
        while ( source )
        {
            if ( source->species == m ) break;
            source = source->next;
        }*/

            foreach (Source src  in  MSX.Node[j].getSources()) {
                if (src.getSpecies() == m) {
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
                MSX.Node[j].getSources().Insert(0, source);
            }

            // --- save source's properties

            source.setType((EnumTypes.SourceType)k);
            source.setSpecies(m);
            source.setC0(x);
            source.setPattern(i);
            return 0;
        }

        //=============================================================================
        // parses an input line containing a time pattern data.
        private InpErrorCodes parsePattern(string[] Tok) {
            int i;
            double x;
            //List<Double> listItem = new ArrayList<Double>();
            //SnumList *listItem;

            // --- get time pattern index

            if (Tok.Length < 2) return InpErrorCodes.ERR_ITEMS;
            i = project.MSXproj_findObject(EnumTypes.ObjectTypes.PATTERN, Tok[0]);
            if (i <= 0) return InpErrorCodes.ERR_NAME;
            MSX.Pattern[i].setId(project.MSXproj_findID(EnumTypes.ObjectTypes.PATTERN, Tok[0]));

            // --- begin reading pattern multipliers

            //k = 1;
            //while ( k < Tok.length )
            for (int k = 1; k < Tok.Length; k++) //string token : Tok)
            {

                if (!Tok[k].ToDouble(out x)) return InpErrorCodes.ERR_NUMBER;

                MSX.Pattern[i].getMultipliers().Add(x);
                /*listItem = (SnumList *) malloc(sizeof(SnumList));
            if ( listItem == NULL ) return 101;
            listItem->value = x;
            listItem->next = NULL;
            if ( MSX.Pattern[i].first == NULL )
            {
                MSX.Pattern[i].current = listItem;
                MSX.Pattern[i].first = listItem;
            }
            else
            {
                MSX.Pattern[i].current->next = listItem;
                MSX.Pattern[i].current = listItem;
            } */

                // k++;
            }
            return 0;
        }

        private InpErrorCodes parseReport(string[] Tok) {
            int i, j, k, err;

            // Get keyword
            if (Tok.Length < 2)
                return 0;

            k = Utilities.MSXutils_findmatch(Tok[0], Constants.ReportWords);

            if (k < 0)
                return InpErrorCodes.ERR_KEYWORD;

            switch (k) {
                    // Keyword is NODE; parse ID names of reported nodes
                case 0:
                    if (Utilities.MSXutils_strcomp(Tok[1], Constants.ALL)) {
                        for (j = 1; j <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; j++) MSX.Node[j].setRpt(true);
                    }
                    else if (Utilities.MSXutils_strcomp(Tok[1], Constants.NONE)) {
                        for (j = 1; j <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; j++) MSX.Node[j].setRpt(false);
                    }
                    else
                        for (i = 1; i < Tok.Length; i++) {
                            int j_tmp;
                            err = epanet.ENgetnodeindex(Tok[i], out j_tmp);
                            j = j_tmp;
                            if (err != 0)
                                return InpErrorCodes.ERR_NAME;
                            MSX.Node[j].setRpt(true);
                        }
                    break;

                    // Keyword is LINK: parse ID names of reported links
                case 1:
                    if (Utilities.MSXutils_strcomp(Tok[1], Constants.ALL)) {
                        for (j = 1; j <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; j++) MSX.Link[j].setRpt(true);
                    }
                    else if (Utilities.MSXutils_strcomp(Tok[1], Constants.NONE)) {
                        for (j = 1; j <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; j++) MSX.Link[j].setRpt(false);
                    }
                    else
                        for (i = 1; i < Tok.Length; i++) {
                            int j_temp;
                            err = epanet.ENgetlinkindex(Tok[i], out j_temp);
                            j = j_temp;
                            if (err != 0) return InpErrorCodes.ERR_NAME;
                            MSX.Link[j].setRpt(true);
                        }
                    break;

                    // Keyword is SPECIES; get YES/NO & precision
                case 2:
                    j = project.MSXproj_findObject(EnumTypes.ObjectTypes.SPECIES, Tok[1]);
                    if (j <= 0) return InpErrorCodes.ERR_NAME;
               
                    if (Tok.Length >= 3) {
                        if (Utilities.MSXutils_strcomp(Tok[2], Constants.YES)) MSX.Species[j].setRpt(1);
                        else if (Utilities.MSXutils_strcomp(Tok[2], Constants.NO)) MSX.Species[j].setRpt(0);
                        else return InpErrorCodes.ERR_KEYWORD;
                    }

                    if (Tok.Length >= 4) {
                        int precision_tmp;
                        // BUG: Baseform bug
                        if (!int.TryParse(Tok[3], out precision_tmp)) ;
                        MSX.Species[j].setPrecision(precision_tmp);
                        return InpErrorCodes.ERR_NUMBER;
                    }

                    break;

                    // Keyword is FILE: get name of report file
                case 3:
                    MSX.rptFilename = Tok[1];
                    break;

                    // Keyword is PAGESIZE;
                case 4:
                    int pagesize_tmp;
                    if (!Utilities.MSXutils_getInt(Tok[1], out pagesize_tmp))
                        return InpErrorCodes.ERR_NUMBER;
                    MSX.PageSize = pagesize_tmp;
                    break;
            }
            return 0;
        }

        //=============================================================================
        // Finds the index assigned to a species, intermediate term, parameter, or constant that appears in a math expression.
        private int getVariableCode(string id) {
            int j = project.MSXproj_findObject(EnumTypes.ObjectTypes.SPECIES, id);

            if (j >= 1) return j;

            j = project.MSXproj_findObject(EnumTypes.ObjectTypes.TERM, id);

            if (j >= 1) return MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + j;

            j = project.MSXproj_findObject(EnumTypes.ObjectTypes.PARAMETER, id);

            if (j >= 1)
                return MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + MSX.Nobjects[(int)EnumTypes.ObjectTypes.TERM] +
                       j;

            j = project.MSXproj_findObject(EnumTypes.ObjectTypes.CONSTANT, id);

            if (j >= 1)
                return MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + MSX.Nobjects[(int)EnumTypes.ObjectTypes.TERM] +
                       MSX.Nobjects[(int)EnumTypes.ObjectTypes.PARAMETER] + j;

            j = Utilities.MSXutils_findmatch(id, Constants.HydVarWords);

            if (j >= 1)
                return MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + MSX.Nobjects[(int)EnumTypes.ObjectTypes.TERM] +
                       MSX.Nobjects[(int)EnumTypes.ObjectTypes.PARAMETER] +
                       MSX.Nobjects[(int)EnumTypes.ObjectTypes.CONSTANT] + j;
            return -1;
        }

        //=============================================================================
        // Scans a string for tokens, saving pointers to them
        //in shared variable Tok[].
        //int  getTokens(string s)
        //{
        //    int  len, m, n;
        //    string c;
        //
        //    // --- begin with no tokens
        //
        //    for (n = 0; n < MAXTOKS; n++) Tok[n] = NULL;
        //    n = 0;
        //
        //    // --- truncate s at start of comment
        //
        //    c = strchr(s,';');
        //    if (c) *c = '\0';
        //    len = strlen(s);
        //
        //    // --- scan s for tokens until nothing left
        //
        //    while (len > 0 && n < MAXTOKS)
        //    {
        //        m = strcspn(s,SEPSTR);              // find token length
        //        if (m == 0) s++;                    // no token found
        //        else
        //        {
        //            if (*s == '"')                  // token begins with quote
        //            {
        //                s++;                        // start token after quote
        //                len--;                      // reduce length of s
        //                m = strcspn(s,"\"\n");      // find end quote or new line
        //            }
        //            s[m] = '\0';                    // null-terminate the token
        //            Tok[n] = s;                     // save pointer to token
        //            n++;                            // update token count
        //            s += m+1;                       // begin next token
        //        }
        //        len -= m+1;                         // update length of s
        //    }
        //    return(n);
        //}

        //=============================================================================

        private void writeInpErrMsg(InpErrorCodes errcode, string sect, string line, int lineCount) {

            string msg;
            if (errcode >= InpErrorCodes.INP_ERR_LAST || errcode <= InpErrorCodes.INP_ERR_FIRST) {
                Console.Error.WriteLine("Error Code = {0}", (int)errcode);
            }
            else {
                Console.Error.WriteLine("{0} at line {1} of {2}] section:",
                    InpErrorTxt[errcode - InpErrorCodes.INP_ERR_FIRST], lineCount, sect);
            }
            //epanet.ENwriteline("");
            //epanet.ENwriteline(msg);
            //epanet.ENwriteline(line);
        }

    }
}
