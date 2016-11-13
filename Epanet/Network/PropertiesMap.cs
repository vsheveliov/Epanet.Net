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
using System.Collections.Generic;
using org.addition.epanet.network.io;

namespace org.addition.epanet.network {

    ///<summary>Simulation configuration properties map.</summary>
    public class PropertiesMap {

        /// <summary>Flow units.</summary>
        public enum FlowUnitsType {
            /// <summary>cubic feet per second</summary>
            CFS = 0,
            /// <summary>gallons per minute</summary>
            GPM = 1,
            /// <summary>million gallons per day</summary>
            MGD = 2,
            /// <summary>imperial million gal. per day</summary>
            IMGD = 3,
            /// <summary>acre-feet per day</summary>
            AFD = 4,
            /// <summary>liters per second</summary>
            LPS = 5,
            /// <summary>liters per minute</summary>
            LPM = 6,
            /// <summary>megaliters per day</summary>
            MLD = 7,
            /// <summary>cubic meters per hour</summary>
            CMH = 8,
            /// <summary>cubic meters per day</summary>
            CMD = 9
        }

        /// <summary>Head loss formula.</summary>
        public enum FormType {
            /// <summary>Hazen-Williams</summary>
            HW,
            /// <summary>Darcy-Weisbach</summary>
            DW,
            /// <summary>Chezy-Manning</summary>
            CM,
        }

        /// <summary>Hydraulics solution option.</summary>
        public enum Hydtype {
            /// <summary>Use from previous run.</summary>
            USE,
            /// <summary>Save after current run.</summary>
            SAVE,
            /// <summary>Use temporary file.</summary>
            SCRATCH
        }

        /// <summary>Pressure units.</summary>
        public enum PressUnitsType {
            /// <summary>pounds per square inch</summary>
            PSI,
            /// <summary>kiloPascals</summary>
            KPA,
            /// <summary>meters</summary>
            METERS
        }

        /// <summary>Water quality analysis option.</summary>
        public enum QualType {
            /// <summary>No quality analysis.</summary>
            NONE = 0,
            /// <summary>Analyze a chemical.</summary>
            CHEM = 1,
            /// <summary>Analyze water age.</summary>
            AGE = 2,
            /// <summary>Trace % of flow from a source</summary>
            TRACE = 3
        }

        /// <summary>Reporting flag.</summary>
        public enum ReportFlag {
            FALSE = 0,
            SOME = 2,
            TRUE = 1
        }

        /// <summary>Status report options.</summary>
        public enum StatFlag {
            NO = 0,
            YES = 1,
            FULL = 2
        }

        /// <summary>Time series statistics.</summary>
        public enum TstatType {
            /// <summary>none</summary>
            SERIES,
            /// <summary>time-averages</summary>
            AVG,
            /// <summary>minimum values</summary>
            MIN,
            /// <summary>maximum values</summary>
            MAX,
            /// <summary>max - min values</summary>
            RANGE
        }


        /// <summary>Unit system.</summary>
        public enum UnitsType {
            /// <summary>SI (metric)</summary>
            SI,
            /// <summary>US</summary>
            US
        }

        public const string ALTREPORT = "AltReport";
        public const string BULKORDER = "BulkOrder";
        public const string CHECK_FREQ = "CheckFreq";
        public const string CHEM_NAME = "ChemName";
        public const string CHEM_UNITS = "ChemUnits";
        public const string CLIMIT = "Climit";
        public const string CTOL = "Ctol";
        public const string DAMP_LIMIT = "DampLimit";
        public const string DCOST = "Dcost";
        public const string DEF_PAT_ID = "DefPatID";
        public const string DIFFUS = "Diffus";
        public const string DMULT = "Dmult";
        public const string DUR = "Dur";
        public const string ECOST = "Ecost";
        public const string EMAX = "Emax";
        public const string ENERGYFLAG = "Energyflag";
        public const string EPAT_ID = "EpatID";
        public const string EPUMP = "Epump";
        public const string EXTRA_ITER = "ExtraIter";
        public const string FLOWFLAG = "Flowflag";
        public const string FORMFLAG = "Formflag";
        public const string HACC = "Hacc";
        public const string HEXP = "Hexp";
        public const string HSTEP = "Hstep";
        public const string HTOL = "Htol";
        public const string HYD_FNAME = "HydFname";
        public const string HYDFLAG = "Hydflag";
        public const string KBULK = "Kbulk";
        public const string KWALL = "Kwall";
        public const string LINKFLAG = "Linkflag";
        public const string MAP_FNAME = "MapFname";
        public const string MAXCHECK = "MaxCheck";
        public const string MAXITER = "MaxIter";
        public const string MESSAGEFLAG = "Messageflag";
        public const string NODEFLAG = "Nodeflag";
        public const string PAGE_SIZE = "PageSize";
        public const string PRESSFLAG = "Pressflag";
        public const string PSTART = "Pstart";
        public const string PSTEP = "Pstep";
        public const string QEXP = "Qexp";
        public const string QSTEP = "Qstep";
        public const string QTOL = "Qtol";
        public const string QUALFLAG = "Qualflag";
        public const string RFACTOR = "Rfactor";
        public const string RQTOL = "RQtol";
        public const string RSTART = "Rstart";
        public const string RSTEP = "Rstep";
        public const string RULESTEP = "Rulestep";
        public const string SPGRAV = "SpGrav";
        public const string STATFLAG = "Statflag";
        public const string SUMMARYFLAG = "Summaryflag";
        public const string TANKORDER = "TankOrder";
        public const string TRACE_NODE = "TraceNode";
        public const string TSTART = "Tstart";
        public const string TSTATFLAG = "Tstatflag";
        public const string UNITSFLAG = "Unitsflag";
        public const string VISCOS = "Viscos";

        public const string WALLORDER = "WallOrder";
        public static readonly string[] EpanetObjectsNames = {
            TSTATFLAG, HSTEP, DUR, QSTEP, CHECK_FREQ,
            MAXCHECK, DMULT, ALTREPORT, QEXP, HEXP, RQTOL, QTOL, BULKORDER, TANKORDER, WALLORDER,
            RFACTOR, CLIMIT, KBULK, KWALL, DCOST, ECOST, EPAT_ID, EPUMP, PAGE_SIZE, STATFLAG, SUMMARYFLAG,
            MESSAGEFLAG, ENERGYFLAG, NODEFLAG, LINKFLAG, RULESTEP, PSTEP, PSTART, RSTEP, RSTART, TSTART,
            FLOWFLAG, PRESSFLAG, FORMFLAG, HYDFLAG, QUALFLAG, UNITSFLAG, HYD_FNAME, CHEM_NAME, CHEM_UNITS,
            DEF_PAT_ID, MAP_FNAME, TRACE_NODE, EXTRA_ITER, CTOL, DIFFUS, DAMP_LIMIT, VISCOS, SPGRAV, MAXITER,
            HACC, HTOL, EMAX
        };

        private Dictionary<string, object> values;



        public PropertiesMap() {
            values = new Dictionary<string, object>();
            loadDefaults();
        }

        /**
         * Get an object from the map.
         *
         * @param name Object name.
         * @return Object refernce.
         * @throws ENException If object name not found.
         */
        public object get(string name) { return values[name]; }

        public string getAltReport() { return (string)get(ALTREPORT); }

        public double getBulkOrder() { return (double)get(BULKORDER); }

        public int getCheckFreq() { return (int)get(CHECK_FREQ); }

        public string getChemName() { return (string)get(CHEM_NAME); }

        public string getChemUnits() { return (string)get(CHEM_UNITS); }

        public double getClimit() { return (double)get(CLIMIT); }

        public double getCtol() { return (double)get(CTOL); }


        public double getDampLimit() { return (double)get(DAMP_LIMIT); }


        public double getDcost() { return (double)get(DCOST); }


        public string getDefPatId() { return (string)get(DEF_PAT_ID); }

        public double getDiffus() { return (double)get(DIFFUS); }

        public double getDmult() { return (double)get(DMULT); }

        public long getDuration() { return (long)get(DUR); }

        public double getEcost() { return (double)get(ECOST); }

        public double getEmax() { return (double)get(EMAX); }

        public bool getEnergyflag() { return (bool)get(ENERGYFLAG); }

        public string getEpatId() { return (string)get(EPAT_ID); }

        public double getEpump() { return (double)get(EPUMP); }

        public int getExtraIter() { return (int)get(EXTRA_ITER); }

        public FlowUnitsType getFlowflag() { return (FlowUnitsType)get(FLOWFLAG); }

        public FormType getFormflag() { return (FormType)get(FORMFLAG); }

        public double getHacc() { return (double)get(HACC); }

        public double getHexp() { return (double)get(HEXP); }

        public long getHstep() { return (long)get(HSTEP); }

        public double getHtol() { return (double)get(HTOL); }

        public Hydtype getHydflag() { return (Hydtype)get(HYDFLAG); }

        public string getHydFname() { return (string)get(HYD_FNAME); }

        public double getKbulk() { return (double)get(KBULK); }

        public double getKwall() { return (double)get(KWALL); }

        public ReportFlag getLinkflag() { return (ReportFlag)get(LINKFLAG); }

        public string getMapFname() { return (string)get(MAP_FNAME); }

        public int getMaxCheck() { return (int)get(MAXCHECK); }

        public int getMaxIter() { return (int)get(MAXITER); }

        public bool getMessageflag() { return (bool)get(MESSAGEFLAG); }

        public ReportFlag getNodeflag() { return (ReportFlag)get(NODEFLAG); }

        /**
         * Get objects names in this map.
         *
         * @param exclude_epanet exclude Epanet objects.
         * @return List of objects names.
         */

        public List<string> getObjectsNames(bool exclude_epanet) {
            List<string> allObjs = new List<string>(values.Keys);

            if (exclude_epanet) {
                foreach (var s in EpanetObjectsNames) {
                    allObjs.Remove(s);
                }
            }

            return allObjs;
        }

        public int getPageSize() { return (int)get(PAGE_SIZE); }

        public PressUnitsType getPressflag() { return (PressUnitsType)get(PRESSFLAG); }

        public long getPstart() { return (long)get(PSTART); }

        public long getPstep() { return (long)get(PSTEP); }

        public double getQexp() { return (double)get(QEXP); }

        public long getQstep() { return (long)get(QSTEP); }

        public double getQtol() { return (double)get(QTOL); }

        public QualType getQualflag() { return (QualType)get(QUALFLAG); }

        public double getRfactor() { return (double)get(RFACTOR); }

        public double getRQtol() { return (double)get(RQTOL); }

        public long getRstart() { return (long)get(RSTART); }

        public long getRstep() { return (long)get(RSTEP); }

        public long getRulestep() { return (long)get(RULESTEP); }

        public double getSpGrav() { return (double)get(SPGRAV); }

        public StatFlag getStatflag() { return (StatFlag)get(STATFLAG); }

        public bool getSummaryflag() { return (bool)get(SUMMARYFLAG); }

        public double getTankOrder() { return (double)get(TANKORDER); }

        public string getTraceNode() { return (string)get(TRACE_NODE); }

        public long getTstart() { return (long)get(TSTART); }

        public TstatType getTstatflag() { return (TstatType)get(TSTATFLAG); }

        public UnitsType getUnitsflag() { return (UnitsType)get(UNITSFLAG); }


        public double getViscos() { return (double)get(VISCOS); }

        public double getWallOrder() { return (double)get(WALLORDER); }

        ///<summary>Init properties with default value.</summary>
        private void loadDefaults() {
            put(BULKORDER, (1.0d)); // 1st-order bulk reaction rate
            put(TANKORDER, (1.0d)); // 1st-order tank reaction rate
            put(WALLORDER, (1.0d)); // 1st-order wall reaction rate
            put(RFACTOR, (1.0d)); // No roughness-reaction factor
            put(CLIMIT, (0.0d)); // No limiting potential quality
            put(KBULK, (0.0d)); // No global bulk reaction
            put(KWALL, (0.0d)); // No global wall reaction
            put(DCOST, (0.0d)); // Zero energy demand charge
            put(ECOST, (0.0d)); // Zero unit energy cost
            put(EPAT_ID, ""); // No energy price pattern
            put(EPUMP, Constants.EPUMP); // Default pump efficiency
            put(PAGE_SIZE, Constants.PAGESIZE);
            put(STATFLAG, StatFlag.NO);
            put(SUMMARYFLAG, true);
            put(MESSAGEFLAG, true);
            put(ENERGYFLAG, false);
            put(NODEFLAG, ReportFlag.FALSE);
            put(LINKFLAG, ReportFlag.FALSE);
            put(TSTATFLAG, TstatType.SERIES); // Generate time series output
            put(HSTEP, (3600L)); // 1 hr hydraulic time step
            put(DUR, (0L)); // 0 sec duration (steady state)
            put(QSTEP, 0L); // No pre-set quality time step
            put(RULESTEP, 0L); // No pre-set rule time step
            put(PSTEP, 3600L); // 1 hr time pattern period
            put(PSTART, 0L); // Starting pattern period
            put(RSTEP, 3600L); // 1 hr reporting period
            put(RSTART, 0L); // Start reporting at time 0
            put(TSTART, 0L); // Starting time of day
            put(FLOWFLAG, FlowUnitsType.GPM); // Flow units are gpm
            put(PRESSFLAG, PressUnitsType.PSI); // Pressure units are psi
            put(FORMFLAG, FormType.HW); // Use Hazen-Williams formula
            put(HYDFLAG, Hydtype.SCRATCH); // No external hydraulics file
            put(QUALFLAG, QualType.NONE); // No quality simulation
            put(UNITSFLAG, UnitsType.US); // US unit system
            put(HYD_FNAME, "");
            put(CHEM_NAME, Keywords.t_CHEMICAL);
            put(CHEM_UNITS, Keywords.u_MGperL); // mg/L
            put(DEF_PAT_ID, Constants.DEFPATID); // Default demand pattern index
            put(MAP_FNAME, "");
            put(ALTREPORT, "");
            put(TRACE_NODE, ""); // No source tracing
            put(EXTRA_ITER, -1); // Stop if network unbalanced
            put(CTOL, Constants.MISSING); // No pre-set quality tolerance
            put(DIFFUS, Constants.MISSING); // Temporary diffusivity
            put(DAMP_LIMIT, Constants.DAMPLIMIT);
            put(VISCOS, Constants.MISSING); // Temporary viscosity
            put(SPGRAV, Constants.SPGRAV); // Default specific gravity
            put(MAXITER, Constants.MAXITER); // Default max. hydraulic trials
            put(HACC, Constants.HACC); // Default hydraulic accuracy
            put(HTOL, Constants.HTOL); // Default head tolerance
            put(QTOL, Constants.QTOL); // Default flow tolerance
            put(RQTOL, Constants.RQTOL); // Default hydraulics parameters
            put(HEXP, 0.0d);
            put(QEXP, 2.0d); // Flow exponent for emitters
            put(CHECK_FREQ, Constants.CHECKFREQ);
            put(MAXCHECK, Constants.MAXCHECK);
            put(DMULT, 1.0d); // Demand multiplier
            put(EMAX, 0.0d); // Zero peak energy usage


        }


        /**
         * Insert an object into the map.
         *
         * @param name Object name.
         * @param obj  Object reference.
         */
        public void put(string name, object obj) { values[name] = obj; }

        public void setAltReport(string str) { put(ALTREPORT, str); }

        public void setBulkOrder(double bulkOrder) { put(BULKORDER, bulkOrder); }

        public void setCheckFreq(int checkFreq) { put(CHECK_FREQ, checkFreq); }

        public void setChemName(string chemName) { put(CHEM_NAME, chemName); }

        public void setChemUnits(string chemUnits) { put(CHEM_UNITS, chemUnits); }

        public void setClimit(double climit) { put(CLIMIT, climit); }

        public void setCtol(double ctol) { put(CTOL, ctol); }

        public void setDampLimit(double dampLimit) { put(DAMP_LIMIT, dampLimit); }

        public void setDcost(double dcost) { put(DCOST, dcost); }

        public void setDefPatId(string defPatID) { put(DEF_PAT_ID, defPatID); }

        public void setDiffus(double diffus) { put(DIFFUS, diffus); }

        public void setDmult(double dmult) { put(DMULT, dmult); }

        public void setDuration(long dur) { put(DUR, dur); }

        public void setEcost(double ecost) { put(ECOST, ecost); }

        public void setEmax(double emax) { put(EMAX, emax); }

        public void setEnergyflag(bool energyflag) { put(ENERGYFLAG, energyflag); }

        public void setEpatId(string epat) { put(EPAT_ID, epat); }

        public void setEpump(double epump) { put(EPUMP, epump); }

        public void setExtraIter(int extraIter) { put(EXTRA_ITER, extraIter); }

        public void setFlowflag(FlowUnitsType flowflag) { put(FLOWFLAG, flowflag); }

        public void setFormflag(FormType formflag) { put(FORMFLAG, formflag); }

        public void setHacc(double hacc) { put(HACC, hacc); }

        public void setHexp(double hexp) { put(HEXP, hexp); }

        public void setHstep(long hstep) { put(HSTEP, hstep); }

        public void setHtol(double htol) { put(HTOL, htol); }

        public void setHydflag(Hydtype hydflag) { put(HYDFLAG, hydflag); }

        public void setHydFname(string hydFname) { put(HYD_FNAME, hydFname); }

        public void setKbulk(double kbulk) { put(KBULK, kbulk); }

        public void setKwall(double kwall) { put(KWALL, kwall); }

        public void setLinkflag(ReportFlag linkflag) { put(LINKFLAG, linkflag); }

        public void setMapFname(string mapFname) { put(MAP_FNAME, mapFname); }

        public void setMaxCheck(int maxCheck) { put(MAXCHECK, maxCheck); }

        public void setMaxIter(int maxIter) { put(MAXITER, maxIter); }

        public void setMessageflag(bool messageflag) { put(MESSAGEFLAG, messageflag); }

        public void setNodeflag(ReportFlag nodeflag) { put(NODEFLAG, nodeflag); }

        public void setPageSize(int pageSize) { put(PAGE_SIZE, pageSize); }

        public void setPressflag(PressUnitsType pressflag) { put(PRESSFLAG, pressflag); }

        public void setPstart(long pstart) { put(PSTART, pstart); }

        public void setPstep(long pstep) { put(PSTEP, pstep); }

        public void setQexp(double qexp) { put(QEXP, qexp); }

        public void setQstep(long qstep) { put(QSTEP, qstep); }

        public void setQtol(double qtol) { put(QTOL, qtol); }

        public void setQualflag(QualType qualflag) { put(QUALFLAG, qualflag); }

        public void setRfactor(double rfactor) { put(RFACTOR, rfactor); }

        public void setRQtol(double RQtol) { put(RQTOL, RQtol); }

        public void setRstart(long rstart) { put(RSTART, rstart); }

        public void setRstep(long rstep) { put(RSTEP, rstep); }

        public void setRulestep(long rulestep) { put(RULESTEP, rulestep); }

        public void setSpGrav(double spGrav) { put(SPGRAV, spGrav); }

        public void setStatflag(StatFlag statflag) { put(STATFLAG, statflag); }

        public void setSummaryflag(bool summaryflag) { put(SUMMARYFLAG, summaryflag); }

        public void setTankOrder(double tankOrder) { put(TANKORDER, tankOrder); }

        public void setTraceNode(string traceNode) { put(TRACE_NODE, traceNode); }

        public void setTstart(long tstart) { put(TSTART, tstart); }

        public void setTstatflag(TstatType tstatflag) { put(TSTATFLAG, tstatflag); }

        public void setUnitsflag(UnitsType unitsflag) { put(UNITSFLAG, unitsflag); }

        public void setViscos(double viscos) { put(VISCOS, viscos); }

        public void setWallOrder(double wallOrder) { put(WALLORDER, wallOrder); }

        public object this[string objName] {
            get {
                object value;
                return this.values.TryGetValue(objName, out value) ? value : null;
            }
        }
    }

}