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

namespace Epanet.Enums
{

    ///<summary>Parse and report keywords.</summary>
    public static class Keywords {
// ReSharper disable InconsistentNaming
        public const string s_BACKDROP = "[BACKDROP]";
        public const string s_CONTROLS = "[CONTROLS]";

        public const string s_COORDS = "[COORDINATES]";
        public const string s_CURVES = "[CURVES]";

        public const string s_DEMANDS = "[DEMANDS]";
        public const string s_EMITTERS = "[EMITTERS]";
        public const string s_END = "[END]";

        public const string s_ENERGY = "[ENERGY]";
        public const string s_JUNCTIONS = "[JUNCTIONS]";
        public const string s_LABELS = "[LABELS]";
        public const string s_MIXING = "[MIXING]";
        public const string s_OPTIONS = "[OPTIONS]";
        public const string s_PATTERNS = "[PATTERNS]";
        public const string s_PIPES = "[PIPES]";
        public const string s_PUMPS = "[PUMPS]";
        public const string s_QUALITY = "[QUALITY]";
        public const string s_REACTIONS = "[REACTIONS]";
        public const string s_REPORT = "[REPORT]";
        public const string s_RESERVOIRS = "[RESERVOIRS]";
        public const string s_ROUGHNESS = "[ROUGHNESS]";
        public const string s_RULES = "[RULES]";

        public const string s_SOURCES = "[SOURCES]";
        public const string s_STATUS = "[STATUS]";
        public const string s_TAGS = "[TAGS]";
        public const string s_TANKS = "[TANKS]";
        public const string s_TIMES = "[TIMES]";
        
        // INP sections types strings
        //public const string s_TITLE =      "[TITL";
        //public const string s_JUNCTIONS =  "[JUNC";
        //public const string s_RESERVOIRS = "[RESE";
        //public const string s_TANKS =      "[TANK";
        //public const string s_PIPES =      "[PIPE";
        //public const string s_PUMPS =      "[PUMP";
        //public const string s_VALVES =     "[VALV";
        //public const string s_CONTROLS =   "[CONT";
        //public const string s_RULES =      "[RULE";
        //public const string s_DEMANDS =    "[DEMA";
        //public const string s_SOURCES =    "[SOUR";
        //public const string s_EMITTERS =   "[EMIT";
        //public const string s_PATTERNS =   "[PATT";
        //public const string s_CURVES =     "[CURV";
        //public const string s_QUALITY =    "[QUAL";
        //public const string s_STATUS =     "[STAT";
        //public const string s_ROUGHNESS =  "[ROUG";
        //public const string s_ENERGY =     "[ENER";
        //public const string s_REACTIONS =  "[REAC";
        //public const string s_MIXING =     "[MIXI";
        //public const string s_REPORT =     "[REPO";
        //public const string s_TIMES =      "[TIME";
        //public const string s_OPTIONS =    "[OPTI";
        //public const string s_COORDS =     "[COOR";
        //public const string s_VERTICES =   "[VERT";
        //public const string s_LABELS =     "[LABE";
        //public const string s_BACKDROP =   "[BACK";
        //public const string s_TAGS =       "[TAGS";
        //public const string s_END =        "[END";

        public const string s_TITLE = "[TITLE]";
        public const string s_VALVES = "[VALVES]";
        public const string s_VERTICES = "[VERTICES]";
        public const string t_ABOVE = "above";

        public const string t_ACTIVE = "active";
        public const string t_BACKDROP = "Backdrop";

        public const string t_BELOW = "below";
        public const string t_CHEMICAL = "Chemical";
        public const string t_CLOSED = "closed";
        public const string t_CM = "Chezy-Manning";
        public const string t_CONTINUED = " (continued)";

        public const string t_CONTROL = "Control";
        public const string t_COORD = "Coordinate";
        public const string t_CURVE = "Curve";
        public const string t_DEMAND = "Demand";
        public const string t_DEMANDFOR = "Demand for Node";
        public const string t_DIAM = "Diameter";
        public const string t_DIFFER = "DIFFERENTIAL";
        public const string t_DW = "Darcy-Weisbach";
        public const string t_ELEV = "Elevation";
        public const string t_EMITTER = "Emitter";
        public const string t_EMPTYING = "emptying";

        public const string t_END = "End";
        public const string t_ENERGY = "Energy";
        public const string t_FILLING = "filling";

        public const string t_FLOW = "Flow";
        public const string t_FRICTION = "F-Factor";
        public const string t_FUNCCALL = "function call";
        public const string t_HALTED = " EXECUTION HALTED.";
        public const string t_HEAD = "Head";
        public const string t_HEADLOSS = "Headloss";
        public const string t_HW = "Hazen-Williams";
        public const string t_JUNCTION = "Junction";
        public const string t_LABEL = "Label";
        public const string t_LENGTH = "Length";
        public const string t_LINKID = "Link";

        public const string t_LINKQUAL = "Quality";
        public const string t_LINKSTATUS = "State";
        public const string t_MIXING = "Mixing";

        public const string t_NODEID = "Node";
        public const string t_OPEN = "open";
        public const string t_OPTION = "Options";
        public const string t_PATTERN = "Pattern";

        public const string t_PERDAY = "/day";
        public const string t_perM3 = "  /m3";
        public const string t_perMGAL = "/Mgal";
        public const string t_PIPE = "Pipe";
        public const string t_PRESSURE = "Pressure";
        public const string t_PUMP = "Pump";
        public const string t_QUALITY = "Quality";
        public const string t_REACTION = "Reaction";
        public const string t_REACTRATE = "Reaction";
        public const string t_REPORT = "Report";
        public const string t_RESERVOIR = "Reservoir";
        public const string t_ROUGHNESS = "Roughness";

        public const string t_RULE = "Rule";
        public const string t_RULES_SECT = "[RULES] section";
        public const string t_SETTING = "Setting";
        public const string t_SOURCE = "Source";
        public const string t_STATUS = "Status";

        public const string t_TAG = "Tag";
        public const string t_TANK = "Tank";
        public const string t_TEMPCLOSED = "temporarily closed";
        public const string t_TIME = "Times";
        public const string t_TITLE = "Title";
        public const string t_VALVE = "Valve";
        public const string t_VELOCITY = "Velocity";
        public const string t_VERTICE = "Vertice";
        public const string t_XFCV = "open but cannot deliver flow";
        public const string t_XFLOW = "open but exceeds maximum flow";
        public const string t_XHEAD = "closed because cannot deliver head";

        public const string t_XPRESSURE = "open but cannot deliver pressure";
        public const string u_AFD = "a-f/d";

        // Units
        public const string u_CFS = "cfs";
        public const string u_CMD = "m3/d";
        public const string u_CMH = "m3/h";
        public const string u_FEET = "ft";
        public const string u_FTperSEC = "fps";
        public const string u_GPM = "gpm";

        public const string u_HOURS = "hrs";
        public const string u_HP = "hp";
        public const string u_IMGD = "Imgd";
        public const string u_INCHES = "in";
        public const string u_KPA = "kPa";
        public const string u_KW = "kw";

        public const string u_LPM = "Lpm";
        public const string u_LPS = "L/s";
        public const string u_METERS = "m";
        public const string u_MGD = "mgd";

        public const string u_MGperL = "mg/L";
        public const string u_MINUTES = "min";

        public const string u_MLD = "ML/d";
        public const string u_MMETERS = "mm";
        public const string u_MperSEC = "m/s";
        public const string u_per1000FT = "/kft";

        public const string u_per1000M = "/km";
        public const string u_PERCENT = "%";
        public const string u_PSI = "psi";
        public const string u_SQFTperSEC = "sq ft/sec";

        public const string u_SQMperSEC = "sq m/sec";
        public const string u_UGperL = "ug/L";
        public const string w_2COMP = "2COMP";
        public const string w_ABOVE = "ABOVE";
        public const string w_ACCURACY = "ACCU";

        public const string w_ACTIVE = "ACTIVE";
        public const string w_ADD = "ADD";
        public const string w_AFD = "AFD";

        public const string w_AGE = "AGE";

        public const string w_ALL = "ALL";
        public const string w_AM = "AM";
        public const string w_AVG = "AVERAGE";
        public const string w_BELOW = "BELOW";
        public const string w_BULK = "BULK";
        public const string w_CFS = "CFS";
        public const string w_CHECKFREQ = "CHECKFREQ";
        public const string w_CHEM = "CHEM";
        public const string w_CLOCKTIME = "CLOCKTIME";
        public const string w_CLOSED = "CLOSED";
        public const string w_CM = "C-M";
        public const string w_CMD = "CMD";
        public const string w_CMH = "CMH";
        public const string w_CONCEN = "CONCEN";
        public const string w_CONTINUE = "CONT";
        public const string w_CURVE = "CURV";
        public const string w_CV = "CV";
        public const string w_DAMPLIMIT = "DAMPLIMIT";
        public const string w_DAYS = "DAY";
        public const string w_DEMAND = "DEMA";
        public const string w_DIAM = "DIAM";
        public const string w_DIFFUSIVITY = "DIFF";
        public const string w_DMNDCHARGE = "DEMAN";
        public const string w_DRAINTIME = "DRAI";
        public const string w_DURATION = "DURA";
        public const string w_DW = "D-W";
        public const string w_EFFIC = "EFFI";
        public const string w_ELEV = "ELEV";
        public const string w_EMITTER = "EMIT";

        public const string w_ENERGY = "ENER";
        public const string w_FCV = "FCV";
        public const string w_FIFO = "FIFO";

        public const string w_FILE = "FILE";
        public const string w_FILLTIME = "FILL";
        public const string w_FLOW = "FLOW";
        public const string w_FLOWPACED = "FLOWPACED";
        public const string w_FULL = "FULL";
        public const string w_GLOBAL = "GLOB";
        public const string w_GPM = "GPM";
        public const string w_GPV = "GPV";
        public const string w_GRADE = "GRADE";
        public const string w_HEAD = "HEAD";
        public const string w_HEADLOSS = "HEADL";
        public const string w_HOURS = "HOU";
        public const string w_HTOL = "HTOL";
        public const string w_HW = "H-W";
        public const string w_HYDRAULIC = "HYDR";

        public const string w_IMGD = "IMGD";
        public const string w_IS = "IS";
        public const string w_JUNC = "Junc";
        public const string w_KPA = "KPA";
        public const string w_LEVEL = "LEVEL";
        public const string w_LIFO = "LIFO";
        public const string w_LIMITING = "LIMIT";
        public const string w_LINK = "LINK";
        public const string w_LPM = "LPM";
        public const string w_LPS = "LPS";
        public const string w_MAP = "MAP";
        public const string w_MASS = "MASS";
        public const string w_MAX = "MAXIMUM";
        public const string w_MAXCHECK = "MAXCHECK";
        public const string w_MESSAGES = "MESS";
        public const string w_METERS = "METERS";
        public const string w_MGD = "MGD";
        public const string w_MIN = "MINIMUM";
        public const string w_MINIMUM = "MINI";
        public const string w_MINUTES = "MIN";
        public const string w_MIXED = "MIXED";
        public const string w_MLD = "MLD";
        public const string w_MULTIPLY = "MULT";
        public const string w_NO = "NO";
        public const string w_NODE = "NODE";
        public const string w_NONE = "NONE";
        public const string w_NOT = "NOT";
        public const string w_OPEN = "OPEN";
        public const string w_ORDER = "ORDER";
        public const string w_PAGE = "PAGE";
        public const string w_PATTERN = "PATT";
        public const string w_PBV = "PBV";
        public const string w_PIPE = "Pipe";
        public const string w_PM = "PM";
        public const string w_POWER = "POWE";

        public const string w_PRECISION = "PREC";
        public const string w_PRESSURE = "PRES";
        public const string w_PRICE = "PRICE";
        public const string w_PRV = "PRV";
        public const string w_PSI = "PSI";
        public const string w_PSV = "PSV";
        public const string w_PUMP = "Pump";
        public const string w_QTOL = "QTOL";
        public const string w_QUALITY = "QUAL";
        public const string w_RANGE = "RANGE";
        public const string w_REPORT = "REPO";
        public const string w_RESERV = "Reser";
        public const string w_ROUGHNESS = "ROUG";
        public const string w_RQTOL = "RQTOL";
        public const string w_RULE = "RULE";
        public const string w_SAVE = "SAVE";
        public const string w_SECONDS = "SEC";
        public const string w_SEGMENTS = "SEGM";
        public const string w_SETPOINT = "SETPOINT";
        public const string w_SETTING = "SETT";
        public const string w_SI = "SI";
        public const string w_SPECGRAV = "SPEC";
        public const string w_SPEED = "SPEE";
        public const string w_START = "STAR";
        public const string w_STATISTIC = "STAT";
        public const string w_STATUS = "STATUS";
        public const string w_STOP = "STOP";
        public const string w_SUMMARY = "SUMM";
        public const string w_SYSTEM = "SYST";

        public const string w_TANK = "Tank";
        public const string w_TCV = "TCV";
        public const string w_TIME = "TIME";

        public const string w_TOLERANCE = "TOLER";
        public const string w_TRACE = "TRACE";
        public const string w_TRIALS = "TRIAL";

        public const string w_UNBALANCED = "UNBA";
        public const string w_UNITS = "UNIT";
        public const string w_USE = "USE";
        public const string w_VALVE = "Valve";
        public const string w_VELOCITY = "VELO";
        public const string w_VERIFY = "VERI";
        public const string w_VISCOSITY = "VISC";
        public const string w_VOLUME = "VOLU";
        public const string w_WALL = "WALL";
        public const string w_YES = "YES";

        public const string wr_ABOVE = "ABOVE";
        public const string wr_ACTIVE = "ACTIVE";
        public const string wr_AND = "AND";
        public const string wr_BELOW = "BELOW";
        public const string wr_CLOCKTIME = "CLOCKTIME";
        public const string wr_CLOSED = "CLOSED";
        public const string wr_DEMAND = "DEMA";
        public const string wr_DRAINTIME = "DRAI";
        public const string wr_ELSE = "ELSE";
        public const string wr_FILLTIME = "FILL";
        public const string wr_FLOW = "FLOW";
        public const string wr_GRADE = "GRADE";
        public const string wr_HEAD = "HEAD";
        public const string wr_IF = "IF";
        public const string wr_IS = "IS";
        public const string wr_JUNC = "Junc";
        public const string wr_LEVEL = "LEVEL";
        public const string wr_LINK = "LINK";
        public const string wr_NODE = "NODE";
        public const string wr_NOT = "NOT";
        public const string wr_OPEN = "OPEN";
        public const string wr_OR = "OR";
        public const string wr_PIPE = "Pipe";
        public const string wr_POWER = "POWE";
        public const string wr_PRESSURE = "PRES";
        public const string wr_PRIORITY = "PRIO";
        public const string wr_PUMP = "Pump";
        public const string wr_RESERV = "Reser";
        public const string wr_RULE = "RULE";
        public const string wr_SETTING = "SETT";
        public const string wr_STATUS = "STATUS";
        public const string wr_SYSTEM = "SYST";
        public const string wr_TANK = "Tank";
        public const string wr_THEN = "THEN";
        public const string wr_TIME = "TIME";
        public const string wr_VALVE = "Valve";

    }

    // ReSharper restore InconsistentNaming
}