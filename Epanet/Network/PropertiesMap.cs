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
using System.Collections.Generic;
using Epanet.Network.IO;

namespace Epanet.Network {

    /// <summary>Simulation configuration configuration.</summary>
    public sealed class PropertiesMap {

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

        private Dictionary<string, string> extraOptions;

        public PropertiesMap() { this.LoadDefaults(); }

        #region properties accessors/mutators

        [System.ComponentModel.DefaultValue(null)]
        public string AltReport { get; set; }

        /// <summary>Bulk flow reaction order</summary>
        [System.ComponentModel.DefaultValue(0.0)] // 1st-order bulk reaction rate
        public double BulkOrder { get; set; }

        /// <summary>Hydraulics solver parameter.</summary>
        [System.ComponentModel.DefaultValue(Constants.CHECKFREQ)]
        public int CheckFreq { get; set; }

        /// <summary>Name of chemical.</summary>
        [System.ComponentModel.DefaultValue(Keywords.t_CHEMICAL)]
        public string ChemName { get; set; }

        /// <summary>Units of chemical.</summary>
        [System.ComponentModel.DefaultValue(Keywords.u_MGperL)]
        public string ChemUnits { get; set; }

        /// <summary>Limiting potential quality.</summary>
        [System.ComponentModel.DefaultValue(0.0)] // No limiting potential quality
        public double CLimit { get; set; }

        /// <summary>Water quality tolerance.</summary>
        [System.ComponentModel.DefaultValue(double.NaN)] // No pre-set quality tolerance
        public double Ctol { get; set; }

        /// <summary>Solution damping threshold.</summary>
        [System.ComponentModel.DefaultValue(Constants.DAMPLIMIT)]
        public double DampLimit { get; set; }

        /// <summary>Energy demand charge/kw/day.</summary>
        [System.ComponentModel.DefaultValue(0.0)] // Zero energy demand charge
        public double DCost { get; set; }

        /// <summary>Default demand pattern ID.</summary>
        [System.ComponentModel.DefaultValue(Constants.DEFPATID)]
        public string DefPatId { get; set; }

        /// <summary>Diffusivity (sq ft/sec).</summary>
        [System.ComponentModel.DefaultValue(double.NaN)]
        public double Diffus { get; set; }

        /// <summary>Demand multiplier.</summary>
        [System.ComponentModel.DefaultValue(1.0)]
        public double DMult { get; set; }

        /// <summary>Duration of simulation (sec).</summary>
        [System.ComponentModel.DefaultValue(0)] // 0 sec duration (steady state)
        public long Duration { get; set; }

        /// <summary>Base energy cost per kwh.</summary>
        [System.ComponentModel.DefaultValue(0.0)] // Zero unit energy cost
        public double ECost { get; set; }

        /// <summary>Peak energy usage.</summary>
        [System.ComponentModel.DefaultValue(0.0)]
        public double EMax { get; set; }

        /// <summary>Energy report flag.</summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool EnergyFlag { get; set; }

        /// <summary>Energy cost time pattern.</summary>
        [System.ComponentModel.DefaultValue("")] // No energy price pattern
        public string EPatId { get; set; }

        /// <summary>Global pump efficiency.</summary>
        [System.ComponentModel.DefaultValue(Constants.EPUMP)] // Default pump efficiency
        public double EPump { get; set; }

        /// <summary>Extra hydraulic trials.</summary>
        [System.ComponentModel.DefaultValue(-1)] // Stop if network unbalanced
        public int ExtraIter { get; set; }

        /// <summary>Flow units flag.</summary>
        [System.ComponentModel.DefaultValue(FlowUnitsType.GPM)]
        public FlowUnitsType FlowFlag { get; set; }

        /// <summary>Hydraulic formula flag.</summary>
        [System.ComponentModel.DefaultValue(FormType.HW)] // Use Hazen-Williams formula
        public FormType FormFlag { get; set; }

        /// <summary>Hydraulics solution accuracy.</summary>
        [System.ComponentModel.DefaultValue(Constants.HACC)]
        public double HAcc { get; set; }

        /// <summary>Exponent in headloss formula.</summary>
        [System.ComponentModel.DefaultValue(0.0)]
        public double HExp { get; set; }

        /// <summary>Nominal hyd. time step (sec).</summary>
        [System.ComponentModel.DefaultValue(3600)] // 1 hr hydraulic time step
        public long HStep { get; set; }

        /// <summary>Hydraulic head tolerance.</summary>
        [System.ComponentModel.DefaultValue(Constants.HTOL)]
        public double HTol { get; set; }

        /// <summary>Hydraulics flag.</summary>
        [System.ComponentModel.DefaultValue(HydType.SCRATCH)] // No external hydraulics file
        public HydType HydFlag { get; set; }

        /// <summary>Hydraulics file name.</summary>
        [System.ComponentModel.DefaultValue(null)]
        public string HydFname { get; set; }

        /// <summary>Global bulk reaction coeff.</summary>
        [System.ComponentModel.DefaultValue(0.0)] // No global bulk reaction
        public double KBulk { get; set; }

        /// <summary>Global wall reaction coeff.</summary>
        [System.ComponentModel.DefaultValue(0.0)] // No global wall reaction
        public double KWall { get; set; }

        /// <summary>Link report flag.</summary>
        [System.ComponentModel.DefaultValue(ReportFlag.FALSE)]
        public ReportFlag LinkFlag { get; set; }

        /// <summary>Map file name.</summary>
        [System.ComponentModel.DefaultValue(null)]
        public string MapFname { get; set; }

        /// <summary>Hydraulics solver parameter.</summary>
        [System.ComponentModel.DefaultValue(Constants.MAXCHECK)]
        public int MaxCheck { get; set; }

        /// <summary>Max. hydraulic trials.</summary>
        [System.ComponentModel.DefaultValue(Constants.MAXITER)]
        public int MaxIter { get; set; }

        /// <summary>Error/warning message flag.</summary>
        [System.ComponentModel.DefaultValue(true)]
        public bool MessageFlag { get; set; }

        /// <summary>Node report flag.</summary>
        [System.ComponentModel.DefaultValue(ReportFlag.FALSE)]
        public ReportFlag NodeFlag { get; set; }

        /// <summary>Lines/page in output report.</summary>
        [System.ComponentModel.DefaultValue(Constants.PAGESIZE)]
        public int PageSize { get; set; }

        /// <summary>Pressure units flag.</summary>
        [System.ComponentModel.DefaultValue(PressUnitsType.PSI)]
        public PressUnitsType PressFlag { get; set; }

        /// <summary>Starting pattern time (sec).</summary>
        [System.ComponentModel.DefaultValue(0)] // Starting pattern period
        public long PStart { get; set; }

        /// <summary>Time pattern time step (sec).</summary>
        [System.ComponentModel.DefaultValue(3600)] // 1 hr time pattern period
        public long PStep { get; set; }

        /// <summary>Exponent in orifice formula.</summary>
        [System.ComponentModel.DefaultValue(2.0)] // Flow exponent for emitters
        public double QExp { get; set; }

        /// <summary>Quality time step (sec).</summary>
        [System.ComponentModel.DefaultValue(0)] // No pre-set quality time step
        public long QStep { get; set; }

        /// <summary>Flow rate tolerance.</summary>
        [System.ComponentModel.DefaultValue(Constants.QTOL)]
        public double QTol { get; set; }

        /// <summary>Water quality flag.</summary>
        [System.ComponentModel.DefaultValue(QualType.NONE)]
        public QualType QualFlag { get; set; }

        /// <summary>Roughness-reaction factor.</summary>
        [System.ComponentModel.DefaultValue(1.0)] // No roughness-reaction factor
        public double RFactor { get; set; }

        /// <summary>Flow resistance tolerance.</summary>
        [System.ComponentModel.DefaultValue(Constants.RQTOL)]
        public double RQtol { get; set; }

        /// <summary>Time when reporting starts.</summary>
        [System.ComponentModel.DefaultValue(0)] // Start reporting at time 0
        public long RStart { get; set; }

        /// <summary>Reporting time step (sec).</summary>
        [System.ComponentModel.DefaultValue(3600)] // 1 hr reporting period
        public long RStep { get; set; }

        /// <summary>Rule evaluation time step.</summary>
        [System.ComponentModel.DefaultValue(0)] // No pre-set rule time step
        public long RuleStep { get; set; }

        /// <summary>Specific gravity.</summary>
        [System.ComponentModel.DefaultValue(Constants.SPGRAV)]
        public double SpGrav { get; set; }

        /// <summary>Status report flag.</summary>
        [System.ComponentModel.DefaultValue(StatFlag.NO)]
        public StatFlag Stat_Flag { get; set; }

        /// <summary>Report summary flag.</summary>
        [System.ComponentModel.DefaultValue(true)]
        public bool SummaryFlag { get; set; }

        /// <summary>Tank reaction order.</summary>
        [System.ComponentModel.DefaultValue(1.0)] // 1st-order tank reaction rate
        public double TankOrder { get; set; }

        /// <summary>Source node for flow tracing.</summary>
        [System.ComponentModel.DefaultValue(null)] // No source tracing
        public string TraceNode { get; set; }

        /// <summary>Starting time of day (sec).</summary>
        [System.ComponentModel.DefaultValue(0)] // Starting time of day
        public long TStart { get; set; }

        /// <summary>Time statistics flag.</summary>
        [System.ComponentModel.DefaultValue(TStatType.SERIES)] // Generate time series output
        public TStatType TStatFlag { get; set; }

        /// <summary>Unit system flag.</summary>
        [System.ComponentModel.DefaultValue(UnitsType.US)] // US unit system
        public UnitsType UnitsFlag { get; set; }

        /// <summary>Kin. viscosity (sq ft/sec).</summary>
        [System.ComponentModel.DefaultValue(double.NaN)]
        public double Viscos { get; set; }

        /// <summary>Pipe wall reaction order.</summary>
        [System.ComponentModel.DefaultValue(1.0)] // 1st-order wall reaction rate
        public double WallOrder { get; set; }

        #endregion

        public Dictionary<string, string> ExtraOptions {
            get {
                return this.extraOptions ?? (this.extraOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            }
        }

#if false

        /// <summary>Init properties with default value.</summary>
        private void LoadDefaults() {
            var props = typeof(PropertiesMap).GetProperties(System.Reflection.BindingFlags.Instance 
                                                          | System.Reflection.BindingFlags.Public
                                                          | System.Reflection.BindingFlags.DeclaredOnly);

            foreach(System.Reflection.PropertyInfo pi in props) {
                var attrs = pi.GetCustomAttributes(false);
                foreach(object att in attrs) {
                    var dv = att as System.ComponentModel.DefaultValueAttribute;
                    if(dv == null)
                        continue;
                    pi.SetValue(this, dv.Value, null);
                    break;
                }
            }
        }

#else
        /// <summary>Init properties with default value.</summary>
        private void LoadDefaults() {
            this.BulkOrder = 1.0d; // 1st-order bulk reaction rate
            this.TankOrder = 1.0d; // 1st-order tank reaction rate
            this.WallOrder = 1.0d; // 1st-order wall reaction rate
            this.RFactor = 1.0d; // No roughness-reaction factor
            this.CLimit = 0.0d; // No limiting potential quality
            this.KBulk = 0.0d; // No global bulk reaction
            this.KWall = 0.0d; // No global wall reaction
            this.DCost = 0.0d; // Zero energy demand charge
            this.ECost = 0.0d; // Zero unit energy cost
            this.EPatId = string.Empty; // No energy price pattern
            this.EPump = Constants.EPUMP; // Default pump efficiency
            this.PageSize = Constants.PAGESIZE;
            this.Stat_Flag = StatFlag.NO;
            this.SummaryFlag = true;
            this.MessageFlag = true;
            this.EnergyFlag = false;
            this.NodeFlag = ReportFlag.FALSE;
            this.LinkFlag = ReportFlag.FALSE;
            this.TStatFlag = TStatType.SERIES; // Generate time series output
            this.HStep = 3600L; // 1 hr hydraulic time step
            this.Duration = 0L; // 0 sec duration (steady state)
            this.QStep = 0L; // No pre-set quality time step
            this.RuleStep = 0L; // No pre-set rule time step
            this.PStep = 3600L; // 1 hr time pattern period
            this.PStart = 0L; // Starting pattern period
            this.RStep = 3600L; // 1 hr reporting period
            this.RStart = 0L; // Start reporting at time 0
            this.TStart = 0L; // Starting time of day
            this.FlowFlag = FlowUnitsType.GPM; // Flow units are gpm
            this.PressFlag = PressUnitsType.PSI; // Pressure units are psi
            this.FormFlag = FormType.HW; // Use Hazen-Williams formula
            this.HydFlag = HydType.SCRATCH; // No external hydraulics file
            this.QualFlag = QualType.NONE; // No quality simulation
            this.UnitsFlag = UnitsType.US; // US unit system
            this.HydFname = "";
            this.ChemName = Keywords.t_CHEMICAL;
            this.ChemUnits = Keywords.u_MGperL; // mg/L
            this.DefPatId = Constants.DEFPATID; // Default demand pattern index
            this.MapFname = "";
            this.AltReport = "";
            this.TraceNode = ""; // No source tracing
            this.ExtraIter = -1; // Stop if network unbalanced
            this.Ctol = double.NaN; // No pre-set quality tolerance
            this.Diffus = double.NaN; // Temporary diffusivity
            this.DampLimit = Constants.DAMPLIMIT;
            this.Viscos = double.NaN; // Temporary viscosity
            this.SpGrav = Constants.SPGRAV; // Default specific gravity
            this.MaxIter = Constants.MAXITER; // Default max. hydraulic trials
            this.HAcc = Constants.HACC; // Default hydraulic accuracy
            this.HTol = Constants.HTOL; // Default head tolerance
            this.QTol = Constants.QTOL; // Default flow tolerance
            this.RQtol = Constants.RQTOL; // Default hydraulics parameters
            this.HExp = 0.0d;
            this.QExp = 2.0d; // Flow exponent for emitters
            this.CheckFreq = Constants.CHECKFREQ;
            this.MaxCheck = Constants.MAXCHECK;
            this.DMult = 1.0d; // Demand multiplier
            this.EMax = 0.0d; // Zero peak energy usage
        }

#endif

    }    

}