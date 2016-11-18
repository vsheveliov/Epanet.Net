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
using Epanet.Network.IO;
using Epanet.Util;

namespace Epanet.Network {

    ///<summary>Simulation configuration properties map.</summary>
    public class PropertiesMap {

        #region enums
        // ReSharper disable InconsistentNaming

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
        public enum HydType {
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
        public enum TStatType {
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

        // ReSharper restore InconsistentNaming

        #endregion
        
        #region constants
        
        private const string ALTREPORT = "AltReport";
        private const string BULKORDER = "BulkOrder";
        private const string CHECK_FREQ = "CheckFreq";
        private const string CHEM_NAME = "ChemName";
        private const string CHEM_UNITS = "ChemUnits";
        private const string CLIMIT = "Climit";
        private const string CTOL = "Ctol";
        private const string DAMP_LIMIT = "DampLimit";
        private const string DCOST = "Dcost";
        private const string DEF_PAT_ID = "DefPatID";
        private const string DIFFUS = "Diffus";
        private const string DMULT = "Dmult";
        private const string DUR = "Dur";
        private const string ECOST = "Ecost";
        private const string EMAX = "Emax";
        private const string ENERGYFLAG = "Energyflag";
        private const string EPAT_ID = "EpatID";
        private const string EPUMP = "Epump";
        private const string EXTRA_ITER = "ExtraIter";
        private const string FLOWFLAG = "Flowflag";
        private const string FORMFLAG = "Formflag";
        private const string HACC = "Hacc";
        private const string HEXP = "Hexp";
        private const string HSTEP = "Hstep";
        private const string HTOL = "Htol";
        private const string HYD_FNAME = "HydFname";
        private const string HYDFLAG = "Hydflag";
        private const string KBULK = "Kbulk";
        private const string KWALL = "Kwall";
        private const string LINKFLAG = "Linkflag";
        private const string MAP_FNAME = "MapFname";
        private const string MAXCHECK = "MaxCheck";
        private const string MAXITER = "MaxIter";
        private const string MESSAGEFLAG = "Messageflag";
        private const string NODEFLAG = "Nodeflag";
        private const string PAGE_SIZE = "PageSize";
        private const string PRESSFLAG = "Pressflag";
        private const string PSTART = "Pstart";
        private const string PSTEP = "Pstep";
        private const string QEXP = "Qexp";
        private const string QSTEP = "Qstep";
        private const string QTOL = "Qtol";
        private const string QUALFLAG = "Qualflag";
        private const string RFACTOR = "Rfactor";
        private const string RQTOL = "RQtol";
        private const string RSTART = "Rstart";
        private const string RSTEP = "Rstep";
        private const string RULESTEP = "Rulestep";
        private const string SPGRAV = "SpGrav";
        private const string STATFLAG = "Statflag";
        private const string SUMMARYFLAG = "Summaryflag";
        private const string TANKORDER = "TankOrder";
        private const string TRACE_NODE = "TraceNode";
        private const string TSTART = "Tstart";
        private const string TSTATFLAG = "Tstatflag";
        private const string UNITSFLAG = "Unitsflag";
        private const string VISCOS = "Viscos";

        private const string WALLORDER = "WallOrder";

        private static readonly string[] EpanetObjectsNames = {
            TSTATFLAG, HSTEP, DUR, QSTEP, CHECK_FREQ,
            MAXCHECK, DMULT, ALTREPORT, QEXP, HEXP, RQTOL, QTOL, BULKORDER, TANKORDER, WALLORDER,
            RFACTOR, CLIMIT, KBULK, KWALL, DCOST, ECOST, EPAT_ID, EPUMP, PAGE_SIZE, STATFLAG, SUMMARYFLAG,
            MESSAGEFLAG, ENERGYFLAG, NODEFLAG, LINKFLAG, RULESTEP, PSTEP, PSTART, RSTEP, RSTART, TSTART,
            FLOWFLAG, PRESSFLAG, FORMFLAG, HYDFLAG, QUALFLAG, UNITSFLAG, HYD_FNAME, CHEM_NAME, CHEM_UNITS,
            DEF_PAT_ID, MAP_FNAME, TRACE_NODE, EXTRA_ITER, CTOL, DIFFUS, DAMP_LIMIT, VISCOS, SPGRAV, MAXITER,
            HACC, HTOL, EMAX
        };

        #endregion
        
        private readonly Dictionary<string, object> values;
        
        public PropertiesMap() {
            this.values = new Dictionary<string, object>(70, System.StringComparer.OrdinalIgnoreCase);
            this.LoadDefaults();
        }

        ///<summary>Get objects names in this map.</summary>
        /// <param name="excludeEpanet">Exclude Epanet objects.</param>
        public List<string> GetObjectsNames(bool excludeEpanet) {
            List<string> allObjs = new List<string>(this.values.Keys);

            if(excludeEpanet) {
                foreach(var s in EpanetObjectsNames) {
                    allObjs.Remove(s);
                }
            }

            return allObjs;
        }



        #region properties accessors/mutators

        public string AltReport {
            get { return (string)this.values[ALTREPORT]; }
            set { this.values[ALTREPORT] = value; }
        }

        /// <summary>Bulk flow reaction order.</summary>
        public double BulkOrder {
            get { return (double)this.values[BULKORDER]; }
            set { this.values[BULKORDER] = value; }
        }

        /// <summary>Hydraulics solver parameter.</summary>
        public int CheckFreq {
            get { return (int)this.values[CHECK_FREQ]; } 
            set { this.values[CHECK_FREQ] = value; }
        }

        /// <summary>Name of chemical.</summary>
        public string ChemName {
            get { return (string)this.values[CHEM_NAME]; }
            set { this.values[CHEM_NAME] = value; }
        }

        /// <summary>Units of chemical.</summary>
        public string ChemUnits {
            get { return (string)this.values[CHEM_UNITS]; } 
            set { this.values[CHEM_UNITS] = value; }
        }

        /// <summary>Limiting potential quality.</summary>
        public double CLimit {
            get { return (double)this.values[CLIMIT]; } 
            set { this.values[CLIMIT] = value; }
        }

        /// <summary>Water quality tolerance.</summary>
        public double Ctol {
            get { return (double)this.values[CTOL]; } 
            set { this.values[CTOL] = value; }
        }

        /// <summary>Solution damping threshold.</summary>
        public double DampLimit {
            get { return (double)this.values[DAMP_LIMIT]; } 
            set { this.values[DAMP_LIMIT] = value; }
        }

        /// <summary>Energy demand charge/kw/day.</summary>
        public double DCost {
            get { return (double)this.values[DCOST]; } 
            set { this.values[DCOST] = value; }
        }

        /// <summary>Default demand pattern ID.</summary>
        public string DefPatId {
            get { return (string)this.values[DEF_PAT_ID]; } 
            set { this.values[DEF_PAT_ID] = value; }
        }

        /// <summary>Diffusivity (sq ft/sec).</summary>
        public double Diffus {
            get { return (double)this.values[DIFFUS]; } 
            set { this.values[DIFFUS] = value; }
        }

        /// <summary>Demand multiplier.</summary>
        public double DMult {
            get { return (double)this.values[DMULT]; }
            set { this.values[DMULT] = value; }
        }

        /// <summary>Duration of simulation (sec).</summary>
        public long Duration {
            get { return (long)this.values[DUR]; } 
            set { this.values[DUR] = value; }
        }

        /// <summary>Base energy cost per kwh.</summary>
        public double ECost {
            get { return (double)this.values[ECOST]; } 
            set { this.values[ECOST] = value; }
        }

        /// <summary>Peak energy usage.</summary>
        public double EMax {
            get { return (double)this.values[EMAX]; } 
            set { this.values[EMAX] = value; }
        }

        /// <summary>Energy report flag.</summary>
        public bool EnergyFlag {
            get { return (bool)this.values[ENERGYFLAG]; } 
            set { this.values[ENERGYFLAG] = value; }
        }

        /// <summary>Energy cost time pattern.</summary>
        public string EPatId {
            get { return (string)this.values[EPAT_ID]; }
            set { this.values[EPAT_ID] = value; }
        }

        /// <summary>Global pump efficiency.</summary>
        public double EPump {
            get { return (double)this.values[EPUMP]; } 
            set { this.values[EPUMP] = value; }
        }

        /// <summary>Extra hydraulic trials.</summary>
        public int ExtraIter {
            get { return (int)this.values[EXTRA_ITER]; }
            set { this.values[EXTRA_ITER] = value; }
        }

        /// <summary>Flow units flag.</summary>
        public FlowUnitsType FlowFlag {
            get { return (FlowUnitsType)this.values[FLOWFLAG]; }
            set { this.values[FLOWFLAG] = value; }
        }

        /// <summary>Hydraulic formula flag.</summary>
        public FormType FormFlag {
            get { return (FormType)this.values[FORMFLAG]; } 
            set { this.values[FORMFLAG] = value; }
        }

        /// <summary>Hydraulics solution accuracy.</summary>
        public double HAcc {
            get { return (double)this.values[HACC]; } 
            set { this.values[HACC] = value; }
        }

        /// <summary>Exponent in headloss formula.</summary>
        public double HExp {
            get { return (double)this.values[HEXP]; }
            set { this.values[HEXP] = value; }
        }

        /// <summary>Nominal hyd. time step (sec).</summary>
        public long HStep {
            get { return (long)this.values[HSTEP]; } 
            set { this.values[HSTEP] = value; }
        }

        /// <summary>Hydraulic head tolerance.</summary>
        public double HTol {
            get { return (double)this.values[HTOL]; }
            set { this.values[HTOL] = value; }
        }

        /// <summary>Hydraulics flag.</summary>
        public HydType HydFlag {
            get { return (HydType)this.values[HYDFLAG]; } 
            set { this.values[HYDFLAG] = value; }
        }

        /// <summary>Hydraulics file name.</summary>
        public string HydFname {
            get { return (string)this.values[HYD_FNAME]; } 
            set { this.values[HYD_FNAME] = value; }
        }

        /// <summary>Global bulk reaction coeff.</summary>
        public double KBulk {
            get { return (double)this.values[KBULK]; }
            set { this.values[KBULK] = value; }
        }

        /// <summary>Global wall reaction coeff.</summary>
        public double KWall {
            get { return (double)this.values[KWALL]; }
            set { this.values[KWALL] = value; }
        }

        /// <summary>Link report flag.</summary>
        public ReportFlag LinkFlag {
            get { return (ReportFlag)this.values[LINKFLAG]; } 
            set { this.values[LINKFLAG] = value; }
        }

        /// <summary>Map file name.</summary>
        public string MapFname {
            get { return (string)this.values[MAP_FNAME]; }
            set { this.values[MAP_FNAME] = value; }
        }

        /// <summary>Hydraulics solver parameter.</summary>
        public int MaxCheck {
            get { return (int)this.values[MAXCHECK]; }
            set { this.values[MAXCHECK] = value; }
        }

        /// <summary>Max. hydraulic trials.</summary>
        public int MaxIter {
            get { return (int)this.values[MAXITER]; }
            set { this.values[MAXITER] = value; }
        }

        /// <summary>Error/warning message flag.</summary>
        public bool MessageFlag {
            get { return (bool)this.values[MESSAGEFLAG]; } 
            set { this.values[MESSAGEFLAG] = value; }
        }

        /// <summary>Node report flag.</summary>
        public ReportFlag NodeFlag {
            get { return (ReportFlag)this.values[NODEFLAG]; } 
            set { this.values[NODEFLAG] = value; }
        }

        /// <summary>Lines/page in output report.</summary>
        public int PageSize {
            get { return (int)this.values[PAGE_SIZE]; } 
            set { this.values[PAGE_SIZE] = value; }
        }

        /// <summary>Pressure units flag.</summary>
        public PressUnitsType PressFlag {
            get { return (PressUnitsType)this.values[PRESSFLAG]; }
            set { this.values[PRESSFLAG] = value; }
        }

        /// <summary>Starting pattern time (sec).</summary>
        public long PStart {
            get { return (long)this.values[PSTART]; }
            set { this.values[PSTART] = value; }
        }

        /// <summary>Time pattern time step (sec).</summary>
        public long PStep {
            get { return (long)this.values[PSTEP]; } 
            set { this.values[PSTEP] = value; }
        }

        /// <summary>Exponent in orifice formula.</summary>
        public double QExp {
            get { return (double)this.values[QEXP]; } 
            set { this.values[QEXP] = value; }
        }

        /// <summary>Quality time step (sec).</summary>
        public long QStep {
            get { return (long)this.values[QSTEP]; } 
            set { this.values[QSTEP] = value; }
        }

        /// <summary>Flow rate tolerance.</summary>
        public double QTol {
            get { return (double)this.values[QTOL]; } 
            set { this.values[QTOL] = value; }
        }

        /// <summary>Water quality flag.</summary>
        public QualType QualFlag {
            get { return (QualType)this.values[QUALFLAG]; } 
            set { this.values[QUALFLAG] = value; }
        }

        /// <summary>Roughness-reaction factor.</summary>
        public double RFactor {
            get { return (double)this.values[RFACTOR]; } 
            set { this.values[RFACTOR] = value; }
        }

        /// <summary>Flow resistance tolerance.</summary>
        public double RQtol {
            get { return (double)this.values[RQTOL]; }
            set { this.values[RQTOL] = value; }
        }

        /// <summary>Time when reporting starts.</summary>
        public long RStart {
            get { return (long)this.values[RSTART]; }
            set { this.values[RSTART] = value; }
        }

        /// <summary>Reporting time step (sec).</summary>
        public long RStep {
            get { return (long)this.values[RSTEP]; } 
            set { this.values[RSTEP] = value; }
        }

        /// <summary>Rule evaluation time step.</summary>
        public long RuleStep {
            get { return (long)this.values[RULESTEP]; }
            set { this.values[RULESTEP] = value; }
        }

        /// <summary>Specific gravity.</summary>
        public double SpGrav {
            get { return (double)this.values[SPGRAV]; } 
            set { this.values[SPGRAV] = value; }
        }

        /// <summary>Status report flag.</summary>
        public StatFlag Stat_Flag {
            get { return (StatFlag)this.values[STATFLAG]; } 
            set { this.values[STATFLAG] = value; }
        }

        /// <summary>Report summary flag.</summary>
        public bool SummaryFlag {
            get { return (bool)this.values[SUMMARYFLAG]; } 
            set { this.values[SUMMARYFLAG] = value; }
        }

        /// <summary>Tank reaction order.</summary>
        public double TankOrder {
            get { return (double)this.values[TANKORDER]; }
            set { this.values[TANKORDER] = value; }
        }

        /// <summary>Source node for flow tracing.</summary>
        public string TraceNode {
            get { return (string)this.values[TRACE_NODE]; } 
            set { this.values[TRACE_NODE] = value; }
        }

        /// <summary>Starting time of day (sec).</summary>
        public long TStart {
            get { return (long)this.values[TSTART]; } 
            set { this.values[TSTART] = value; }
        }

        /// <summary>Time statistics flag.</summary>
        public TStatType TStatFlag {
            get { return (TStatType)this.values[TSTATFLAG]; }
            set { this.values[TSTATFLAG] = value; }
        }

        /// <summary>Unit system flag.</summary>
        public UnitsType UnitsFlag {
            get { return (UnitsType)this.values[UNITSFLAG]; }
            set { this.values[UNITSFLAG] = value; }
        }

        /// <summary>Kin. viscosity (sq ft/sec).</summary>
        public double Viscos {
            get { return (double)this.values[VISCOS]; } 
            set { this.values[VISCOS] = value; }
        }

        /// <summary>Pipe wall reaction order.</summary>
        public double WallOrder {
            get { return (double)this.values[WALLORDER]; }
            set { this.values[WALLORDER] = value; }
        }

#endregion

        ///<summary>Init properties with default value.</summary>
        private void LoadDefaults() {
            this.values[BULKORDER] = 1.0d; // 1st-order bulk reaction rate
            this.values[TANKORDER] = 1.0d; // 1st-order tank reaction rate
            this.values[WALLORDER] = 1.0d; // 1st-order wall reaction rate
            this.values[RFACTOR] = 1.0d; // No roughness-reaction factor
            this.values[CLIMIT] = 0.0d; // No limiting potential quality
            this.values[KBULK] = 0.0d; // No global bulk reaction
            this.values[KWALL] = 0.0d; // No global wall reaction
            this.values[DCOST] = 0.0d; // Zero energy demand charge
            this.values[ECOST] = 0.0d; // Zero unit energy cost
            this.values[EPAT_ID] = ""; // No energy price pattern
            this.values[EPUMP] = Constants.EPUMP; // Default pump efficiency
            this.values[PAGE_SIZE] = Constants.PAGESIZE;
            this.values[STATFLAG] = StatFlag.NO;
            this.values[SUMMARYFLAG] = true;
            this.values[MESSAGEFLAG] = true;
            this.values[ENERGYFLAG] = false;
            this.values[NODEFLAG] = ReportFlag.FALSE;
            this.values[LINKFLAG] = ReportFlag.FALSE;
            this.values[TSTATFLAG] = TStatType.SERIES; // Generate time series output
            this.values[HSTEP] = 3600L; // 1 hr hydraulic time step
            this.values[DUR] = 0L; // 0 sec duration (steady state)
            this.values[QSTEP] = 0L; // No pre-set quality time step
            this.values[RULESTEP] = 0L; // No pre-set rule time step
            this.values[PSTEP] = 3600L; // 1 hr time pattern period
            this.values[PSTART] = 0L; // Starting pattern period
            this.values[RSTEP] = 3600L; // 1 hr reporting period
            this.values[RSTART] = 0L; // Start reporting at time 0
            this.values[TSTART] = 0L; // Starting time of day
            this.values[FLOWFLAG] = FlowUnitsType.GPM; // Flow units are gpm
            this.values[PRESSFLAG] = PressUnitsType.PSI; // Pressure units are psi
            this.values[FORMFLAG] = FormType.HW; // Use Hazen-Williams formula
            this.values[HYDFLAG] = HydType.SCRATCH; // No external hydraulics file
            this.values[QUALFLAG] = QualType.NONE; // No quality simulation
            this.values[UNITSFLAG] = UnitsType.US; // US unit system
            this.values[HYD_FNAME] = "";
            this.values[CHEM_NAME] = Keywords.t_CHEMICAL;
            this.values[CHEM_UNITS] = Keywords.u_MGperL; // mg/L
            this.values[DEF_PAT_ID] = Constants.DEFPATID; // Default demand pattern index
            this.values[MAP_FNAME] = "";
            this.values[ALTREPORT] = "";
            this.values[TRACE_NODE] = ""; // No source tracing
            this.values[EXTRA_ITER] = -1; // Stop if network unbalanced
            this.values[CTOL] = Constants.MISSING; // No pre-set quality tolerance
            this.values[DIFFUS] = Constants.MISSING; // Temporary diffusivity
            this.values[DAMP_LIMIT] = Constants.DAMPLIMIT;
            this.values[VISCOS] = Constants.MISSING; // Temporary viscosity
            this.values[SPGRAV] = Constants.SPGRAV; // Default specific gravity
            this.values[MAXITER] = Constants.MAXITER; // Default max. hydraulic trials
            this.values[HACC] = Constants.HACC; // Default hydraulic accuracy
            this.values[HTOL] = Constants.HTOL; // Default head tolerance
            this.values[QTOL] = Constants.QTOL; // Default flow tolerance
            this.values[RQTOL] = Constants.RQTOL; // Default hydraulics parameters
            this.values[HEXP] = 0.0d;
            this.values[QEXP] = 2.0d; // Flow exponent for emitters
            this.values[CHECK_FREQ] = Constants.CHECKFREQ;
            this.values[MAXCHECK] = Constants.MAXCHECK;
            this.values[DMULT] = 1.0d; // Demand multiplier
            this.values[EMAX] = 0.0d; // Zero peak energy usage


        }

        /// <summary>
        /// Insert an object into the map. / Get an object from the map.
        /// </summary>
        /// <param name="name">Object name.</param>
        /// <returns>Object refernce.</returns>
        /// <remarks>Throws <see cref="ENException"/> if object name not found.</remarks>
        public object this[string name] {
            get {
                object value;
                return this.values.TryGetValue(name, out value) ? value : null;
            }
            set { this.values[name] = value; }
        }


    }

}