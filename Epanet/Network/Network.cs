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
using System.Linq;

using Epanet.Enums;
using Epanet.Network.Structures;

namespace Epanet.Network {


    ///<summary>Hydraulic network structure.</summary>
    public class Network {
        private readonly List<Control> _controls = new List<Control>();
        private readonly ElementCollection<Curve> _curves = new ElementCollection<Curve>();
        
        [NonSerialized]
        private readonly FieldsMap _fields = new FieldsMap();
        
        private readonly List<Label> _labels = new List<Label>();
        private readonly ElementCollection<Link> _links = new ElementCollection<Link>();
        private readonly ElementCollection<Node> _nodes = new ElementCollection<Node>();
        
        private readonly ElementCollection<Pattern> _patterns = new ElementCollection<Pattern> {
            new Pattern(string.Empty)
        };

        private readonly ElementCollection<Rule> _rules = new ElementCollection<Rule>();
        private readonly List<string> _title = new List<string>();

        public Network() {
            LoadDefaults();
        }
        
        public IList<Control> Controls { get { return _controls; } }

        public Curve GetCurve(string name) { return _curves.GetValueOrDefault(name); }

        public IList<Curve> Curves { get { return _curves; } }

        ///<summary>Fields map with report variables properties and conversion units.</summary>
        public FieldsMap FieldsMap { get { return _fields; } }

        ///<summary>Transient colleciton of junctions.</summary>
        public IEnumerable<Node> Junctions {
            get { return _nodes.Where(x => x.Type == NodeType.JUNC); }
        }

        public IList<Label> Labels { get { return _labels; } }

        public Link GetLink(string name) { return _links.GetValueOrDefault(name); }

        public IList<Link> Links { get { return _links; } }

        public Node GetNode(string name) { return _nodes.GetValueOrDefault(name); }

        public IList<Node> Nodes { get { return _nodes; } }

        public Pattern GetPattern(string name) { return _patterns.GetValueOrDefault(name); }

        public IList<Pattern> Patterns { get { return _patterns; } }

        ///<summary>Transient colleciton of pumps.</summary>
        public IEnumerable<Pump> Pumps {
            get { return _links.Where(x => x.Type == LinkType.PUMP).Cast<Pump>(); }
        }

        public Rule GetRule(string ruleName) { return _rules.GetValueOrDefault(ruleName); }

        public IList<Rule> Rules { get { return _rules; } }

        public IEnumerable<Tank> Tanks {
            get { return _nodes.Where(x => x.Type == NodeType.TANK).Cast<Tank>(); }
        }

        public IEnumerable<Tank> Reservoirs {
            get { return _nodes.Where(x => x.Type == NodeType.RESERV).Cast<Tank>(); }
        }

        public List<string> Title { get { return _title; } }

        ///<summary>Transient colleciton of valves.</summary>
        public IEnumerable<Valve> Valves {
            get { return _links.OfType<Valve>(); }
        }

        #region properties map


        private Dictionary<string, string> _extraOptions;

        
        #region properties accessors/mutators

        public string AltReport { get; set; }

        /// <summary>Bulk flow reaction order</summary>
        public double BulkOrder { get; set; }

        /// <summary>Hydraulics solver parameter.</summary>
        public int CheckFreq { get; set; }

        /// <summary>Name of chemical.</summary>
        public string ChemName { get; set; }

        /// <summary>Units of chemical.</summary>
        public string ChemUnits { get; set; }

        /// <summary>Limiting potential quality.</summary>
        public double CLimit { get; set; }

        /// <summary>Water quality tolerance.</summary>
        public double Ctol { get; set; }

        /// <summary>Solution damping threshold.</summary>
        public double DampLimit { get; set; }

        /// <summary>Energy demand charge/kw/day.</summary>
        public double DCost { get; set; }

        /// <summary>Default demand pattern ID.</summary>
        public string DefPatId { get; set; }

        /// <summary>Diffusivity (sq ft/sec).</summary>
        public double Diffus { get; set; }

        /// <summary>Demand multiplier.</summary>
        public double DMult { get; set; }

        /// <summary>Duration of simulation (sec).</summary>
        public long Duration { get; set; }

        /// <summary>Base energy cost per kwh.</summary>
        public double ECost { get; set; }

        /// <summary>Peak energy usage.</summary>
        public double EMax { get; set; }

        /// <summary>Energy report flag.</summary>
        public bool EnergyFlag { get; set; }

        /// <summary>Energy cost time pattern.</summary>
        public string EPatId { get; set; }

        /// <summary>Global pump efficiency.</summary>
        public double EPump { get; set; }

        /// <summary>Extra hydraulic trials.</summary>
        public int ExtraIter { get; set; }

        /// <summary>Flow units flag.</summary>
        public FlowUnitsType FlowFlag { get; set; }

        /// <summary>Hydraulic formula flag.</summary>
        public FormType FormFlag { get; set; }

        /// <summary>Hydraulics solution accuracy.</summary>
        public double HAcc { get; set; }

        /// <summary>Exponent in headloss formula.</summary>
        public double HExp { get; set; }

        /// <summary>Nominal hyd. time step (sec).</summary>
        public long HStep { get; set; }

        /// <summary>Hydraulic head tolerance.</summary>
        public double HTol { get; set; }

        /// <summary>Hydraulics flag.</summary>
        public HydType HydFlag { get; set; }

        /// <summary>Hydraulics file name.</summary>
        public string HydFname { get; set; }

        /// <summary>Global bulk reaction coeff.</summary>
        public double KBulk { get; set; }

        /// <summary>Global wall reaction coeff.</summary>
        public double KWall { get; set; }

        /// <summary>Link report flag.</summary>
        public ReportFlag LinkFlag { get; set; }

        /// <summary>Map file name.</summary>
        public string MapFname { get; set; }

        /// <summary>Hydraulics solver parameter.</summary>
        public int MaxCheck { get; set; }

        /// <summary>Max. hydraulic trials.</summary>
        public int MaxIter { get; set; }

        /// <summary>Error/warning message flag.</summary>
        public bool MessageFlag { get; set; }

        /// <summary>Node report flag.</summary>
        public ReportFlag NodeFlag { get; set; }

        /// <summary>Lines/page in output report.</summary>
        public int PageSize { get; set; }

        /// <summary>Pressure units flag.</summary>
        public PressUnitsType PressFlag { get; set; }

        /// <summary>Starting pattern time (sec).</summary>
        public long PStart { get; set; }

        /// <summary>Time pattern time step (sec).</summary>
        public long PStep { get; set; }

        /// <summary>Exponent in orifice formula.</summary>
        public double QExp { get; set; }

        /// <summary>Quality time step (sec).</summary>
        public long QStep { get; set; }

        /// <summary>Flow rate tolerance.</summary>
        public double QTol { get; set; }

        /// <summary>Water quality flag.</summary>
        public QualType QualFlag { get; set; }

        /// <summary>Roughness-reaction factor.</summary>
        public double RFactor { get; set; }

        /// <summary>Flow resistance tolerance.</summary>
        public double RQtol { get; set; }

        /// <summary>Time when reporting starts.</summary>
        public long RStart { get; set; }

        /// <summary>Reporting time step (sec).</summary>
        public long RStep { get; set; }

        /// <summary>Rule evaluation time step.</summary>
        public long RuleStep { get; set; }

        /// <summary>Specific gravity.</summary>
        public double SpGrav { get; set; }

        /// <summary>Status report flag.</summary>
        public StatFlag StatFlag { get; set; }

        /// <summary>Report summary flag.</summary>
        public bool SummaryFlag { get; set; }

        /// <summary>Tank reaction order.</summary>
        public double TankOrder { get; set; }

        /// <summary>Source node for flow tracing.</summary>
        public string TraceNode { get; set; }

        /// <summary>Starting time of day (sec).</summary>
        public long Tstart { get; set; }

        /// <summary>Time statistics flag.</summary>
        public TStatType TstatFlag { get; set; }

        /// <summary>Unit system flag.</summary>
        public UnitsType UnitsFlag { get; set; }

        /// <summary>Kin. viscosity (sq ft/sec).</summary>
        public double Viscos { get; set; }

        /// <summary>Pipe wall reaction order.</summary>
        public double WallOrder { get; set; }

        #endregion

        public Dictionary<string, string> ExtraOptions {
            get {
                return _extraOptions ?? (_extraOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            }
        }

        /// <summary>Init properties with default value.</summary>
        private void LoadDefaults() {
            BulkOrder = 1.0d; // 1st-order bulk reaction rate
            TankOrder = 1.0d; // 1st-order tank reaction rate
            WallOrder = 1.0d; // 1st-order wall reaction rate
            RFactor = 1.0d; // No roughness-reaction factor
            CLimit = 0.0d; // No limiting potential quality
            KBulk = 0.0d; // No global bulk reaction
            KWall = 0.0d; // No global wall reaction
            DCost = 0.0d; // Zero energy demand charge
            ECost = 0.0d; // Zero unit energy cost
            EPatId = string.Empty; // No energy price pattern
            EPump = Constants.EPUMP; // Default pump efficiency
            PageSize = Constants.PAGESIZE;
            StatFlag = StatFlag.NO;
            SummaryFlag = true;
            MessageFlag = true;
            EnergyFlag = false;
            NodeFlag = ReportFlag.FALSE;
            LinkFlag = ReportFlag.FALSE;
            TstatFlag = TStatType.SERIES; // Generate time series output
            HStep = 3600L; // 1 hr hydraulic time step
            Duration = 0L; // 0 sec duration (steady state)
            QStep = 0L; // No pre-set quality time step
            RuleStep = 0L; // No pre-set rule time step
            PStep = 3600L; // 1 hr time pattern period
            PStart = 0L; // Starting pattern period
            RStep = 3600L; // 1 hr reporting period
            RStart = 0L; // Start reporting at time 0
            Tstart = 0L; // Starting time of day
            FlowFlag = FlowUnitsType.GPM; // Flow units are gpm
            PressFlag = PressUnitsType.PSI; // Pressure units are psi
            FormFlag = FormType.HW; // Use Hazen-Williams formula
            HydFlag = HydType.SCRATCH; // No external hydraulics file
            QualFlag = QualType.NONE; // No quality simulation
            UnitsFlag = UnitsType.US; // US unit system
            HydFname = "";
            ChemName = Keywords.t_CHEMICAL;
            ChemUnits = Keywords.u_MGperL; // mg/L
            DefPatId = Constants.DEFPATID; // Default demand pattern index
            MapFname = "";
            AltReport = "";
            TraceNode = ""; // No source tracing
            ExtraIter = -1; // Stop if network unbalanced
            Ctol = Constants.MISSING; // No pre-set quality tolerance
            Diffus = Constants.MISSING; // Temporary diffusivity
            DampLimit = Constants.DAMPLIMIT;
            Viscos = Constants.MISSING; // Temporary viscosity
            SpGrav = Constants.SPGRAV; // Default specific gravity
            MaxIter = Constants.MAXITER; // Default max. hydraulic trials
            HAcc = Constants.HACC; // Default hydraulic accuracy
            HTol = Constants.HTOL; // Default head tolerance
            QTol = Constants.QTOL; // Default flow tolerance
            RQtol = Constants.RQTOL; // Default hydraulics parameters
            HExp = 0.0d;
            QExp = 2.0d; // Flow exponent for emitters
            CheckFreq = Constants.CHECKFREQ;
            MaxCheck = Constants.MAXCHECK;
            DMult = 1.0d; // Demand multiplier
            EMax = 0.0d; // Zero peak energy usage
        }


        #endregion

        public override string ToString() {
            
            return new System.Text.StringBuilder(0x200)
                .AppendLine(" Network")
                .Append("  Nodes      : ").Append(_nodes.Count).AppendLine()
                .Append("  Links      : ").Append(_links.Count).AppendLine()
                .Append("  Pattern    : ").Append(_patterns.Count).AppendLine()
                .Append("  Curves     : ").Append(_curves.Count).AppendLine()
                .Append("  Controls   : ").Append(_controls.Count).AppendLine()
                .Append("  Labels     : ").Append(_labels.Count).AppendLine()
                .Append("  Rules      : ").Append(_rules.Count).AppendLine()
                .Append("  Tanks      : ").Append(Tanks.Count()).AppendLine()
                .Append("  Reservoirs : ").Append(Reservoirs.Count()).AppendLine()
                .Append("  Pumps      : ").Append(Pumps.Count()).AppendLine()
                .Append("  Valves     : ").Append(Valves.Count()).AppendLine()
                .ToString();
            
        }

    }

}