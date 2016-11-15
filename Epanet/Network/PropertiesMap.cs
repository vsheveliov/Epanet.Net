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
        
        private readonly Dictionary<string, object> _values;
        
        public PropertiesMap() {
            this._values = new Dictionary<string, object>(70, System.StringComparer.OrdinalIgnoreCase);
            this.LoadDefaults();
        }

        ///<summary>Get objects names in this map.</summary>
        /// <param name="excludeEpanet">Exclude Epanet objects.</param>
        public List<string> GetObjectsNames(bool excludeEpanet) {
            List<string> allObjs = new List<string>(this._values.Keys);

            if(excludeEpanet) {
                foreach(var s in EpanetObjectsNames) {
                    allObjs.Remove(s);
                }
            }

            return allObjs;
        }



        #region properties accessors/mutators

        public string AltReport {
            get { return (string)this._values[ALTREPORT]; }
            set { this._values[ALTREPORT] = value; }
        }

        /// <summary>Bulk flow reaction order.</summary>
        public double BulkOrder {
            get { return (double)this._values[BULKORDER]; }
            set { this._values[BULKORDER] = value; }
        }

        /// <summary>Hydraulics solver parameter.</summary>
        public int CheckFreq {
            get { return (int)this._values[CHECK_FREQ]; } 
            set { this._values[CHECK_FREQ] = value; }
        }

        /// <summary>Name of chemical.</summary>
        public string ChemName {
            get { return (string)this._values[CHEM_NAME]; }
            set { this._values[CHEM_NAME] = value; }
        }

        /// <summary>Units of chemical.</summary>
        public string ChemUnits {
            get { return (string)this._values[CHEM_UNITS]; } 
            set { this._values[CHEM_UNITS] = value; }
        }

        /// <summary>Limiting potential quality.</summary>
        public double Climit {
            get { return (double)this._values[CLIMIT]; } 
            set { this._values[CLIMIT] = value; }
        }

        /// <summary>Water quality tolerance.</summary>
        public double Ctol {
            get { return (double)this._values[CTOL]; } 
            set { this._values[CTOL] = value; }
        }

        /// <summary>Solution damping threshold.</summary>
        public double DampLimit {
            get { return (double)this._values[DAMP_LIMIT]; } 
            set { this._values[DAMP_LIMIT] = value; }
        }

        /// <summary>Energy demand charge/kw/day.</summary>
        public double Dcost {
            get { return (double)this._values[DCOST]; } 
            set { this._values[DCOST] = value; }
        }

        /// <summary>Default demand pattern ID.</summary>
        public string DefPatId {
            get { return (string)this._values[DEF_PAT_ID]; } 
            set { this._values[DEF_PAT_ID] = value; }
        }

        /// <summary>Diffusivity (sq ft/sec).</summary>
        public double Diffus {
            get { return (double)this._values[DIFFUS]; } 
            set { this._values[DIFFUS] = value; }
        }

        /// <summary>Demand multiplier.</summary>
        public double Dmult {
            get { return (double)this._values[DMULT]; }
            set { this._values[DMULT] = value; }
        }

        /// <summary>Duration of simulation (sec).</summary>
        public long Duration {
            get { return (long)this._values[DUR]; } 
            set { this._values[DUR] = value; }
        }

        /// <summary>Base energy cost per kwh.</summary>
        public double Ecost {
            get { return (double)this._values[ECOST]; } 
            set { this._values[ECOST] = value; }
        }

        /// <summary>Peak energy usage.</summary>
        public double Emax {
            get { return (double)this._values[EMAX]; } 
            set { this._values[EMAX] = value; }
        }

        /// <summary>Energy report flag.</summary>
        public bool Energyflag {
            get { return (bool)this._values[ENERGYFLAG]; } 
            set { this._values[ENERGYFLAG] = value; }
        }

        /// <summary>Energy cost time pattern.</summary>
        public string EpatId {
            get { return (string)this._values[EPAT_ID]; }
            set { this._values[EPAT_ID] = value; }
        }

        /// <summary>Global pump efficiency.</summary>
        public double Epump {
            get { return (double)this._values[EPUMP]; } 
            set { this._values[EPUMP] = value; }
        }

        /// <summary>Extra hydraulic trials.</summary>
        public int ExtraIter {
            get { return (int)this._values[EXTRA_ITER]; }
            set { this._values[EXTRA_ITER] = value; }
        }

        /// <summary>Flow units flag.</summary>
        public FlowUnitsType Flowflag {
            get { return (FlowUnitsType)this._values[FLOWFLAG]; }
            set { this._values[FLOWFLAG] = value; }
        }

        /// <summary>Hydraulic formula flag.</summary>
        public FormType Formflag {
            get { return (FormType)this._values[FORMFLAG]; } 
            set { this._values[FORMFLAG] = value; }
        }

        /// <summary>Hydraulics solution accuracy.</summary>
        public double Hacc {
            get { return (double)this._values[HACC]; } 
            set { this._values[HACC] = value; }
        }

        /// <summary>Exponent in headloss formula.</summary>
        public double Hexp {
            get { return (double)this._values[HEXP]; }
            set { this._values[HEXP] = value; }
        }

        /// <summary>Nominal hyd. time step (sec).</summary>
        public long Hstep {
            get { return (long)this._values[HSTEP]; } 
            set { this._values[HSTEP] = value; }
        }

        /// <summary>Hydraulic head tolerance.</summary>
        public double Htol {
            get { return (double)this._values[HTOL]; }
            set { this._values[HTOL] = value; }
        }

        /// <summary>Hydraulics flag.</summary>
        public Hydtype Hydflag {
            get { return (Hydtype)this._values[HYDFLAG]; } 
            set { this._values[HYDFLAG] = value; }
        }

        /// <summary>Hydraulics file name.</summary>
        public string HydFname {
            get { return (string)this._values[HYD_FNAME]; } 
            set { this._values[HYD_FNAME] = value; }
        }

        /// <summary>Global bulk reaction coeff.</summary>
        public double Kbulk {
            get { return (double)this._values[KBULK]; }
            set { this._values[KBULK] = value; }
        }

        /// <summary>Global wall reaction coeff.</summary>
        public double Kwall {
            get { return (double)this._values[KWALL]; }
            set { this._values[KWALL] = value; }
        }

        /// <summary>Link report flag.</summary>
        public ReportFlag Linkflag {
            get { return (ReportFlag)this._values[LINKFLAG]; } 
            set { this._values[LINKFLAG] = value; }
        }

        /// <summary>Map file name.</summary>
        public string MapFname {
            get { return (string)this._values[MAP_FNAME]; }
            set { this._values[MAP_FNAME] = value; }
        }

        /// <summary>Hydraulics solver parameter.</summary>
        public int MaxCheck {
            get { return (int)this._values[MAXCHECK]; }
            set { this._values[MAXCHECK] = value; }
        }

        /// <summary>Max. hydraulic trials.</summary>
        public int MaxIter {
            get { return (int)this._values[MAXITER]; }
            set { this._values[MAXITER] = value; }
        }

        /// <summary>Error/warning message flag.</summary>
        public bool Messageflag {
            get { return (bool)this._values[MESSAGEFLAG]; } 
            set { this._values[MESSAGEFLAG] = value; }
        }

        /// <summary>Node report flag.</summary>
        public ReportFlag Nodeflag {
            get { return (ReportFlag)this._values[NODEFLAG]; } 
            set { this._values[NODEFLAG] = value; }
        }

        /// <summary>Lines/page in output report.</summary>
        public int PageSize {
            get { return (int)this._values[PAGE_SIZE]; } 
            set { this._values[PAGE_SIZE] = value; }
        }

        /// <summary>Pressure units flag.</summary>
        public PressUnitsType Pressflag {
            get { return (PressUnitsType)this._values[PRESSFLAG]; }
            set { this._values[PRESSFLAG] = value; }
        }

        /// <summary>Starting pattern time (sec).</summary>
        public long Pstart {
            get { return (long)this._values[PSTART]; }
            set { this._values[PSTART] = value; }
        }

        /// <summary>Time pattern time step (sec).</summary>
        public long Pstep {
            get { return (long)this._values[PSTEP]; } 
            set { this._values[PSTEP] = value; }
        }

        /// <summary>Exponent in orifice formula.</summary>
        public double Qexp {
            get { return (double)this._values[QEXP]; } 
            set { this._values[QEXP] = value; }
        }

        /// <summary>Quality time step (sec).</summary>
        public long Qstep {
            get { return (long)this._values[QSTEP]; } 
            set { this._values[QSTEP] = value; }
        }

        /// <summary>Flow rate tolerance.</summary>
        public double Qtol {
            get { return (double)this._values[QTOL]; } 
            set { this._values[QTOL] = value; }
        }

        /// <summary>Water quality flag.</summary>
        public QualType Qualflag {
            get { return (QualType)this._values[QUALFLAG]; } 
            set { this._values[QUALFLAG] = value; }
        }

        /// <summary>Roughness-reaction factor.</summary>
        public double Rfactor {
            get { return (double)this._values[RFACTOR]; } 
            set { this._values[RFACTOR] = value; }
        }

        /// <summary>Flow resistance tolerance.</summary>
        public double RQtol {
            get { return (double)this._values[RQTOL]; }
            set { this._values[RQTOL] = value; }
        }

        /// <summary>Time when reporting starts.</summary>
        public long Rstart {
            get { return (long)this._values[RSTART]; }
            set { this._values[RSTART] = value; }
        }

        /// <summary>Reporting time step (sec).</summary>
        public long Rstep {
            get { return (long)this._values[RSTEP]; } 
            set { this._values[RSTEP] = value; }
        }

        /// <summary>Rule evaluation time step.</summary>
        public long Rulestep {
            get { return (long)this._values[RULESTEP]; }
            set { this._values[RULESTEP] = value; }
        }

        /// <summary>Specific gravity.</summary>
        public double SpGrav {
            get { return (double)this._values[SPGRAV]; } 
            set { this._values[SPGRAV] = value; }
        }

        /// <summary>Status report flag.</summary>
        public StatFlag Statflag {
            get { return (StatFlag)this._values[STATFLAG]; } 
            set { this._values[STATFLAG] = value; }
        }

        /// <summary>Report summary flag.</summary>
        public bool Summaryflag {
            get { return (bool)this._values[SUMMARYFLAG]; } 
            set { this._values[SUMMARYFLAG] = value; }
        }

        /// <summary>Tank reaction order.</summary>
        public double TankOrder {
            get { return (double)this._values[TANKORDER]; }
            set { this._values[TANKORDER] = value; }
        }

        /// <summary>Source node for flow tracing.</summary>
        public string TraceNode {
            get { return (string)this._values[TRACE_NODE]; } 
            set { this._values[TRACE_NODE] = value; }
        }

        /// <summary>Starting time of day (sec).</summary>
        public long Tstart {
            get { return (long)this._values[TSTART]; } 
            set { this._values[TSTART] = value; }
        }

        /// <summary>Time statistics flag.</summary>
        public TstatType Tstatflag {
            get { return (TstatType)this._values[TSTATFLAG]; }
            set { this._values[TSTATFLAG] = value; }
        }

        /// <summary>Unit system flag.</summary>
        public UnitsType Unitsflag {
            get { return (UnitsType)this._values[UNITSFLAG]; }
            set { this._values[UNITSFLAG] = value; }
        }

        /// <summary>Kin. viscosity (sq ft/sec).</summary>
        public double Viscos {
            get { return (double)this._values[VISCOS]; } 
            set { this._values[VISCOS] = value; }
        }

        /// <summary>Pipe wall reaction order.</summary>
        public double WallOrder {
            get { return (double)this._values[WALLORDER]; }
            set { this._values[WALLORDER] = value; }
        }

#endregion

        ///<summary>Init properties with default value.</summary>
        private void LoadDefaults() {
            this._values[BULKORDER] = 1.0d; // 1st-order bulk reaction rate
            this._values[TANKORDER] = 1.0d; // 1st-order tank reaction rate
            this._values[WALLORDER] = 1.0d; // 1st-order wall reaction rate
            this._values[RFACTOR] = 1.0d; // No roughness-reaction factor
            this._values[CLIMIT] = 0.0d; // No limiting potential quality
            this._values[KBULK] = 0.0d; // No global bulk reaction
            this._values[KWALL] = 0.0d; // No global wall reaction
            this._values[DCOST] = 0.0d; // Zero energy demand charge
            this._values[ECOST] = 0.0d; // Zero unit energy cost
            this._values[EPAT_ID] = ""; // No energy price pattern
            this._values[EPUMP] = Constants.EPUMP; // Default pump efficiency
            this._values[PAGE_SIZE] = Constants.PAGESIZE;
            this._values[STATFLAG] = StatFlag.NO;
            this._values[SUMMARYFLAG] = true;
            this._values[MESSAGEFLAG] = true;
            this._values[ENERGYFLAG] = false;
            this._values[NODEFLAG] = ReportFlag.FALSE;
            this._values[LINKFLAG] = ReportFlag.FALSE;
            this._values[TSTATFLAG] = TstatType.SERIES; // Generate time series output
            this._values[HSTEP] = 3600L; // 1 hr hydraulic time step
            this._values[DUR] = 0L; // 0 sec duration (steady state)
            this._values[QSTEP] = 0L; // No pre-set quality time step
            this._values[RULESTEP] = 0L; // No pre-set rule time step
            this._values[PSTEP] = 3600L; // 1 hr time pattern period
            this._values[PSTART] = 0L; // Starting pattern period
            this._values[RSTEP] = 3600L; // 1 hr reporting period
            this._values[RSTART] = 0L; // Start reporting at time 0
            this._values[TSTART] = 0L; // Starting time of day
            this._values[FLOWFLAG] = FlowUnitsType.GPM; // Flow units are gpm
            this._values[PRESSFLAG] = PressUnitsType.PSI; // Pressure units are psi
            this._values[FORMFLAG] = FormType.HW; // Use Hazen-Williams formula
            this._values[HYDFLAG] = Hydtype.SCRATCH; // No external hydraulics file
            this._values[QUALFLAG] = QualType.NONE; // No quality simulation
            this._values[UNITSFLAG] = UnitsType.US; // US unit system
            this._values[HYD_FNAME] = "";
            this._values[CHEM_NAME] = Keywords.t_CHEMICAL;
            this._values[CHEM_UNITS] = Keywords.u_MGperL; // mg/L
            this._values[DEF_PAT_ID] = Constants.DEFPATID; // Default demand pattern index
            this._values[MAP_FNAME] = "";
            this._values[ALTREPORT] = "";
            this._values[TRACE_NODE] = ""; // No source tracing
            this._values[EXTRA_ITER] = -1; // Stop if network unbalanced
            this._values[CTOL] = Constants.MISSING; // No pre-set quality tolerance
            this._values[DIFFUS] = Constants.MISSING; // Temporary diffusivity
            this._values[DAMP_LIMIT] = Constants.DAMPLIMIT;
            this._values[VISCOS] = Constants.MISSING; // Temporary viscosity
            this._values[SPGRAV] = Constants.SPGRAV; // Default specific gravity
            this._values[MAXITER] = Constants.MAXITER; // Default max. hydraulic trials
            this._values[HACC] = Constants.HACC; // Default hydraulic accuracy
            this._values[HTOL] = Constants.HTOL; // Default head tolerance
            this._values[QTOL] = Constants.QTOL; // Default flow tolerance
            this._values[RQTOL] = Constants.RQTOL; // Default hydraulics parameters
            this._values[HEXP] = 0.0d;
            this._values[QEXP] = 2.0d; // Flow exponent for emitters
            this._values[CHECK_FREQ] = Constants.CHECKFREQ;
            this._values[MAXCHECK] = Constants.MAXCHECK;
            this._values[DMULT] = 1.0d; // Demand multiplier
            this._values[EMAX] = 0.0d; // Zero peak energy usage


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
                return this._values.TryGetValue(name, out value) ? value : null;
            }
            set { this._values[name] = value; }
        }


    }

}