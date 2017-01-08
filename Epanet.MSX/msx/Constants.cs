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

namespace Epanet.MSX {


public static class Constants {
    public const int MAXUNITS = 16;
    public const int MAXFNAME = 259; // Max. # characters in file name
    public const int CODEVERSION =20012;

    public const double TINY1 = 1.0e-20d;


    public const int MAGICNUMBER =516114521;
    public const int VERSION     =100000;
    public const int MAXMSG      =1024;            // Max. # characters in message text
    public const int MAXLINE     =1024;            // Max. # characters in input line
    //public const int TRUE        =1;
    //public const int FALSE       =0;
    public const double BIG      =1E10d;
    public const double TINY     =1E-6d;
    public const double MISSING  =-1E10d;
    public const double PI       =3.141592654d;
    public const double VISCOS   =1.1E-5d;          // Kinematic viscosity of water
    // @ 20 deg C (sq ft/sec)

    //-----------------------------------------------------------------------------
//  Various conversion factors
//-----------------------------------------------------------------------------
    public const double M2perFT2    =0.09290304d;
    public const double CM2perFT2   =929.0304d;
    public const double DAYperSEC   =1.1574E-5d;
    public const double HOURperSEC  =2.7778E-4d;
    public const double MINUTEperSEC=0.016667d;
    public const double GPMperCFS   =448.831d;
    public const double AFDperCFS   =1.9837d;
    public const double MGDperCFS   =0.64632d;
    public const double IMGDperCFS  =0.5382d;
    public const double LPSperCFS   =28.317d;
    public const double LPMperCFS   =1699.0d;
    public const double CMHperCFS   =101.94d;
    public const double CMDperCFS   =2446.6d;
    public const double MLDperCFS   =2.4466d;
    public const double M3perFT3    =0.028317d;
    public const double LperFT3     =28.317d;
    public const double MperFT      =0.3048d;
    public const double PSIperFT    =0.4333d;
    public const double KPAperPSI   =6.895d;
    public const double KWperHP     =0.7457d;
    public const double SECperDAY   =86400d;


    public static string [] Errmsg =
            {"unknown error code.",
                    "Error 501 - insufficient memory available.",
                    "Error 502 - no EPANET data file supplied.",
                    "Error 503 - could not open MSX input file.",
                    "Error 504 - could not open hydraulic results file.",
                    "Error 505 - could not read hydraulic results file.",
                    "Error 506 - could not read MSX input file.",
                    "Error 507 - too few pipe reaction expressions.",
                    "Error 508 - too few tank reaction expressions.",
                    "Error 509 - could not open differential equation solver.",
                    "Error 510 - could not open algebraic equation solver.",
                    "Error 511 - could not open binary results file.",
                    "Error 512 - read/write error on binary results file.",
                    "Error 513 - could not integrate reaction rate expressions.",
                    "Error 514 - could not solve reaction equilibrium expressions.",
                    "Error 515 - reference made to an unknown type of object.",
                    "Error 516 - reference made to an illegal object index.",
                    "Error 517 - reference made to an undefined object ID.",
                    "Error 518 - invalid property values were specified.",
                    "Error 519 - an MSX project was not opened.",
                    "Error 520 - an MSX project is already opened.",
                    "Error 521 - could not open MSX report file."};                           //(LR-11/20/07)


    public static readonly string [] MsxSectWords = {"[TITLE", "[SPECIE",  "[COEFF",  "[TERM",
            "[PIPE",  "[TANK",    "[SOURCE", "[QUALITY",
            "[PARAM", "[PATTERN", "[OPTION",
            "[REPORT"};

    public static readonly string [] ReportWords  = {"NODE", "LINK", "SPECIE", "FILE", "PAGESIZE"};

    public static readonly string [] OptionTypeWords = {"AREA_UNITS", "RATE_UNITS", "SOLVER", "COUPLING",
            "TIMESTEP", "RTOL", "ATOL"};

    public static readonly string [] SourceTypeWords = {"CONC", "MASS", "SETPOINT", "FLOW"};      //(FS-01/10/2008 To fix bug 11)
    static readonly string [] MixingTypeWords = {"MIXED", "2COMP", "FIFO", "LIFO"};
    static readonly string [] MassUnitsWords  = {"MG", "UG", "MOLE", "MMOL"};
    public static readonly string [] AreaUnitsWords  = {"FT2", "M2", "CM2"};
    public static readonly string [] TimeUnitsWords  = {"SEC", "MIN", "HR", "DAY"};
    public static readonly string [] SolverTypeWords = {"EUL", "RK5", "ROS2"};
    public static readonly string [] CouplingWords   = {"NONE", "FULL"};
    public static readonly string [] ExprTypeWords   = {"", "RATE", "FORMULA", "EQUIL"};
    public static readonly string [] HydVarWords     = {"", "D", "Q", "U", "Re","Us", "Ff", "Av", "Kc"};	/*Feng Shang 01/29/2008*/
    public static readonly string YES  = "YES";
    public static readonly string NO   = "NO";
    public static readonly string ALL  = "ALL";
    public static readonly string NONE = "NONE";
}
}