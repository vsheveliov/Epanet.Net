using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

using Epanet.Enums;
using Epanet.Log;
using Epanet.Network.Structures;
using Epanet.Util;

namespace Epanet.Network.IO.Input {

    public class NetParser:InputParser {
        
#region Data indexes
        //-------------------------
        // Generic property indexes
        //-------------------------
        private const int COMMENT_INDEX       = 0;  //Comment index
        private const int TAG_INDEX           = 1;  //Tag index

        //--------------------------
        // Junction property indexes
        //--------------------------
        private const int JUNC_ELEV_INDEX     = 2;  //Elevation
        private const int JUNC_DEMAND_INDEX   = 3;  //Demand
        private const int JUNC_PATTERN_INDEX  = 4;  //Demand pattern
        private const int JUNC_DMNDCAT_INDEX  = 5;  //Demand categories
        private const int JUNC_EMITTER_INDEX  = 6;  //Emitter coeff.
        private const int JUNC_INITQUAL_INDEX = 7;  //Init. quality
        private const int JUNC_SRCQUAL_INDEX  = 8;  //Source quality
        private const int JUNC_SRCPAT_INDEX   = 9;  //Source pattern
        private const int JUNC_SRCTYPE_INDEX  = 10; //Source type

        //---------------------------
        // Reservoir property indexes
        //---------------------------
        private const int RES_HEAD_INDEX      = 2;  //Head
        private const int RES_PATTERN_INDEX   = 3;  //Head pattern
        private const int RES_INITQUAL_INDEX  = 4;  //Init. quality
        private const int RES_SRCQUAL_INDEX   = 5;  //Source quality
        private const int RES_SRCPAT_INDEX    = 6;  //Source pattern
        private const int RES_SRCTYPE_INDEX   = 7;  //Source type

        //----------------------
        // Tank property indexes
        //----------------------
        private const int TANK_ELEV_INDEX     = 2;  //Elevation
        private const int TANK_INITLVL_INDEX  = 3;  //Init. level
        private const int TANK_MINLVL_INDEX   = 4;  //Min. level
        private const int TANK_MAXLVL_INDEX   = 5;  //Max. level
        private const int TANK_DIAM_INDEX     = 6;  //Diameter
        private const int TANK_MINVOL_INDEX   = 7;  //Min. volume
        private const int TANK_VCURVE_INDEX   = 8;  //Volume curve
        private const int TANK_MIXMODEL_INDEX = 9;  //Mixing model
        private const int TANK_MIXFRAC_INDEX  = 10; //Mixing fraction
        private const int TANK_KBULK_INDEX    = 11; //Bulk coeff.
        private const int TANK_INITQUAL_INDEX = 12; //Init. quality
        private const int TANK_SRCQUAL_INDEX  = 13; //Source quality
        private const int TANK_SRCPAT_INDEX   = 14; //Source pattern
        private const int TANK_SRCTYPE_INDEX  = 15; //Source type

        //----------------------
        // Pipe property indexes
        //----------------------
        private const int PIPE_LEN_INDEX      = 2;  //Length
        private const int PIPE_DIAM_INDEX     = 3;  //Diameter
        private const int PIPE_ROUGH_INDEX    = 4;  //Roughness coeff.
        private const int PIPE_MLOSS_INDEX    = 5;  //Minor loss coeff.
        private const int PIPE_STATUS_INDEX   = 6;  //Status
        private const int PIPE_KBULK_INDEX    = 7;  //Bulk coeff.
        private const int PIPE_KWALL_INDEX    = 8;  //Wall coeff.

        //----------------------
        // Pump property indexes
        //----------------------
        private const int PUMP_HCURVE_INDEX   = 2;  //Head curve
        private const int PUMP_HP_INDEX       = 3;  //Horsepower
        private const int PUMP_SPEED_INDEX    = 4;  //Speed
        private const int PUMP_PATTERN_INDEX  = 5;  //Speed pattern
        private const int PUMP_STATUS_INDEX   = 6;  //Status
        private const int PUMP_ECURVE_INDEX   = 7;  //Efficiency curve
        private const int PUMP_EPRICE_INDEX   = 8;  //Energy price
        private const int PUMP_PRICEPAT_INDEX = 9;  //Price pattern

        //-----------------------
        // Valve property indexes
        //-----------------------
        private const int VALVE_DIAM_INDEX    = 2;  //Diameter
        private const int VALVE_TYPE_INDEX    = 3;  //Type
        private const int VALVE_SETTING_INDEX = 4;  //Setting
        private const int VALVE_MLOSS_INDEX   = 5;  //Minor loss coeff.
        private const int VALVE_STATUS_INDEX  = 6;  //Status

        //---------------------------
        // Map Label property indexes
        //---------------------------
        private const int LABEL_TEXT_INDEX    = 0;
        private const int ANCHOR_NODE_INDEX   = 3;
        private const int METER_TYPE_INDEX    = 4;
        private const int METER_ID_INDEX      = 5;

        //------------------------
        // Analysis option indexes
        //------------------------
        private const int FLOW_UNITS_INDEX    = 0;  //Flow units
        private const int HLOSS_FORM_INDEX    = 1;  //Headloss formula
        private const int SPEC_GRAV_INDEX     = 2;  //Specific gravity
        private const int VISCOS_INDEX        = 3;  //Relative viscosity
        private const int TRIALS_INDEX        = 4;  //Max. trials
        private const int ACCURACY_INDEX      = 5;  //Hydraul. accuracy
        private const int UNBALANCED_INDEX    = 6;  //If unbalanced option
        private const int GLOBAL_PAT_INDEX    = 7;  //Default demand pattern
        private const int DEMAND_MULT_INDEX   = 8;  //Demand multiplier
        private const int EMITTER_EXP_INDEX   = 9;  //Emitter exponent
        private const int STATUS_RPT_INDEX    = 10; //Status report option
  
        private const int QUAL_PARAM_INDEX    = 11; //Quality parameter
        private const int QUAL_UNITS_INDEX    = 12; //Concen. units
        private const int DIFFUS_INDEX        = 13; //Diffusivity
        private const int TRACE_NODE_INDEX    = 14; //Trace node index
        private const int QUAL_TOL_INDEX      = 15; //Quality tolerance
        private const int MAX_SEGS_INDEX      = 16; //Max. pipe segments
  
        private const int BULK_ORDER_INDEX    = 17; //Bulk reaction order
        private const int WALL_ORDER_INDEX    = 18; //Wall reaction order
        private const int GLOBAL_KBULK_INDEX  = 19; //Default bulk react. coeff.
        private const int GLOBAL_KWALL_INDEX  = 20; //Default wall react. coeff.
        private const int LIMIT_QUAL_INDEX    = 21; //Limiting potential concen.
        private const int ROUGH_CORREL_INDEX  = 22; //Relation between Kwall & roughness
  
        private const int DURATION_INDEX      = 23; //Simulation duration
        private const int HYD_TSTEP_INDEX     = 24; //Hydraulic time step
        private const int QUAL_TSTEP_INDEX    = 25; //Quality time step
        private const int PAT_TSTEP_INDEX     = 26; //Pattern time step
        private const int PAT_START_INDEX     = 27; //Pattern start time
        private const int RPT_TSTEP_INDEX     = 28; //Reporting time step
        private const int RPT_START_INDEX     = 29; //Report start time
        private const int START_TIME_INDEX    = 30; //Starting time of day
        private const int TIME_STAT_INDEX     = 31; //Time statistic option
  
        private const int EFFIC_INDEX         = 32; //Default pump effic.
        private const int EPRICE_INDEX        = 33; //Default energy price
        private const int PRICE_PAT_INDEX     = 34; //Default price pattern
        private const int DMND_CHARGE_INDEX   = 35; //Energy demand charge
        private const int CHECK_FREQ_INDEX    = 36;
        private const int MAX_CHECK_INDEX     = 37;
        private const int DAMP_LIMIT_INDEX    = 38;

#endregion

        /// <summary>Object categories</summary>
        public enum ObjectCategories {
            JUNCS = 0,
            RESERVS = 1,
            TANKS = 2,
            PIPES = 3,
            PUMPS = 4,
            VALVES = 5,
            LABELS = 6,
            PATTERNS = 7,
            CURVES = 8,
            CNTRLS = 9,
            OPTS = 10,
            VERTICES = 11
        }

        /// <summary>EPANET2 *.net project file signature.</summary>
        private const string NETFILE_SIGNATURE = "<EPANET2>";

        /// <summary>EPANET2 *.net project file version ID.</summary>
        private const int VERSIONID1 = 20005;
        /// <summary>EPANET2 *.net project file version ID.</summary>
        private const int VERSIONID2 = 20008;

        /// <summary>Max. index for network options array.</summary>
        private const int MAXOPTIONS = 38;
        /// <summary>Max. index for node property array.</summary>
        private const int MAXNODEPROPS = 25;
        /// <summary>Max. index for link property array</summary>
        private const int MAXLINKPROPS = 25;

        private Reader _reader;

        public override Network Parse(Network nw, string fileName) {
            net = nw ?? new Network();

            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            
            using (fs) {
                _reader = new Reader(fs, Encoding.ASCII);

                string signature = _reader.ReadString();

                //Check for <EPANET2> marker at start of input file
                if (signature != NETFILE_SIGNATURE) throw new ENException(ErrorCode.Err200);

                //Read version ID
                int ver = _reader.ReadInteger();

                if ((ver < VERSIONID1) || (ver > VERSIONID2)) throw new ENException(ErrorCode.Err200);

                //Total up number of network components
                int ncomp = 0;

                for (int i = 0; i <= (int)ObjectCategories.CNTRLS; i++)
                    ncomp += _reader.ReadInteger();

                Debug.Print("Ncomp=" + ncomp);

                net.Title.Add(_reader.ReadString());
                net.Title.AddRange(_reader.ReadList());

                // TODO: move to end
                ReadOptions();

                //Read in time patterns.
                ReadPatterns();

                //Read in curves.
                ReadCurves();

                //Read in junctions.
                ReadJunctions();

                //Read in reservoirs
                ReadReservoirs();

                //Read in tanks
                ReadTanks();

                //Read in pipes, pumps, & valves
                ReadPipes();
                ReadPumps();
                ReadValves();

                //Read in control rules.
                ReadControls();
                ReadRules();
                
                //Read in map labels.
                ReadLabels();
                
            }

            AdjustData(net);

            net.FieldsMap.Prepare(net.UnitsFlag, net.FlowFlag, net.PressFlag, net.QualFlag, net.ChemUnits, net.SpGrav, net.HStep);

            Convert();

            return net;
        }

        private void ReadLabels() {
                int n = _reader.ReadInteger();

            if (n <= 0) return;

            for (int i = 0; i < n; i++) {
                Label mlabel = new Label(_reader.ReadString()) {
                    Position = _reader.ReadPoint(),
                    Anchor = net.GetNode(_reader.ReadString()),
                    FontName = _reader.ReadString(),
                    FontSize = _reader.ReadInteger(),
                    FontBold = _reader.ReadBoolean(),
                    FontItalic = _reader.ReadBoolean(),
                    MeterType = (Label.MeterTypes)_reader.ReadInteger(),
                    MeterId = _reader.ReadString()
                };

                net.Labels.Add(mlabel);
            }
        }

        private void ReadRules() {
            var lines = _reader.ReadList();
            List<string> tok = new List<string>(Constants.MAXTOKS);
            Rule currentRule = null;


            foreach (string line in lines) {
                if (string.IsNullOrEmpty(line))
                    continue;

                if(Tokenize(line, tok) == 0)
                    continue;

                Rulewords key;
                EnumsTxt.TryParse(tok[0], out key);
                if(key == Rulewords.RULE) {
                    currentRule = new Rule(tok[1]);
                    net.Rules.Add(currentRule);
                }
                else if(currentRule != null) {
                    currentRule.Code.Add(line);
                }

            }

        }

        private void ReadControls() {
            var lines = _reader.ReadList();
            List<string> tok = new List<string>(Constants.MAXTOKS);

            foreach (string line in lines) {
                if(string.IsNullOrEmpty(line))
                    continue;

                if(Tokenize(line, tok) == 0)
                    continue;

                try {

                    int n = tok.Count;
                    StatType status = StatType.ACTIVE;

                    double setting = Constants.MISSING, time = 0.0, level = 0.0;

                    if(n < 6)
                        throw new ENException(ErrorCode.Err201, SectType.CONTROLS, line);

                    Node node = null;
                    Link link = net.GetLink(tok[1]);

                    if(link == null)
                        throw new ENException(ErrorCode.Err204, SectType.CONTROLS, tok[0]);

                    LinkType ltype = link.Type;

                    if(ltype == LinkType.CV)
                        throw new ENException(ErrorCode.Err207, SectType.CONTROLS, tok[0]);

                    if(tok[2].Match(StatType.OPEN.ToString())) {
                        status = StatType.OPEN;
                        if(ltype == LinkType.PUMP)
                            setting = 1.0;
                        if(ltype == LinkType.GPV)
                            setting = link.Kc;
                    }
                    else if(tok[2].Match(StatType.CLOSED.ToString())) {
                        status = StatType.CLOSED;
                        if(ltype == LinkType.PUMP)
                            setting = 0.0;
                        if(ltype == LinkType.GPV)
                            setting = link.Kc;
                    }
                    else if(ltype == LinkType.GPV) {
                        throw new ENException(ErrorCode.Err206, SectType.CONTROLS, tok[0]);
                    }
                    else if(!tok[2].ToDouble(out setting)) {
                        throw new ENException(ErrorCode.Err202, SectType.CONTROLS, tok[0]);
                    }

                    if(ltype == LinkType.PUMP || ltype == LinkType.PIPE) {
                        if(!setting.IsMissing()) {
                            if(setting < 0.0)
                                throw new ENException(ErrorCode.Err202, SectType.CONTROLS, tok[0]);

                            status = setting == 0.0 ? StatType.CLOSED : StatType.OPEN;
                        }
                    }

                    ControlType ctype;

                    if(tok[4].Match(Keywords.w_TIME))
                        ctype = ControlType.TIMER;
                    else if(tok[4].Match(Keywords.w_CLOCKTIME))
                        ctype = ControlType.TIMEOFDAY;
                    else {
                        if(n < 8)
                            throw new ENException(ErrorCode.Err201, SectType.CONTROLS, line);

                        if((node = net.GetNode(tok[5])) == null)
                            throw new ENException(ErrorCode.Err203, SectType.CONTROLS, tok[0]);

                        if(tok[6].Match(Keywords.w_BELOW))
                            ctype = ControlType.LOWLEVEL;
                        else if(tok[6].Match(Keywords.w_ABOVE))
                            ctype = ControlType.HILEVEL;
                        else
                            throw new ENException(ErrorCode.Err201, SectType.CONTROLS, line);
                    }

                    switch(ctype) {
                    case ControlType.TIMER:
                    case ControlType.TIMEOFDAY:
                    if(n == 6)
                        time = Utilities.GetHour(tok[5]);
                    if(n == 7)
                        time = Utilities.GetHour(tok[5], tok[6]);
                    if(time < 0.0)
                        throw new ENException(ErrorCode.Err201, SectType.CONTROLS, line);
                    break;

                    case ControlType.LOWLEVEL:
                    case ControlType.HILEVEL:
                    if(!tok[7].ToDouble(out level))
                        throw new ENException(ErrorCode.Err202, SectType.CONTROLS, tok[0]);

                    break;
                    }

                    Control cntr = new Control {
                        Link = link,
                        Node = node,
                        Type = ctype,
                        Status = status,
                        Setting = setting,

                        Time = ctype == ControlType.TIMEOFDAY
                            ? (long)(3600.0 * time) % Constants.SECperDAY
                            : (long)(3600.0 * time),

                        Grade = level
                    };

                    net.Controls.Add(cntr);
                }
                catch (ENException ex) {
                    LogException(ex);
                }
            }
        }

        private void ReadPumps() {
            int n = _reader.ReadInteger();

            if(n <= 0)
                return;

            for(int i = 0; i < n; i++) {
                string name = _reader.ReadString();
                Node j1 = net.GetNode(_reader.ReadString());
                Node j2 = net.GetNode(_reader.ReadString());
                var points = _reader.ReadVertices();
                var data = _reader.ReadArray(MAXLINKPROPS);

                if(net.GetLink(name) != null) {
                    LogException(new ENException(ErrorCode.Err215, SectType.PUMPS, name));
                    continue;
                }

                if(j1 == null || j2 == null) {
                    LogException(new ENException(ErrorCode.Err203, SectType.PUMPS, name));
                    continue;
                }

                if(j1.Equals(j2)) {
                    LogException(new ENException(ErrorCode.Err222, SectType.PUMPS, name));
                    continue;
                }

                Pump pump = new Pump(name) {
                    FirstNode = j1,
                    SecondNode = j2,
                };

                if(points.Length > 0)
                    pump.Vertices.AddRange(points);

                if(!string.IsNullOrEmpty(data[COMMENT_INDEX]))
                    pump.Comment = data[COMMENT_INDEX];

                if(!string.IsNullOrEmpty(data[TAG_INDEX]))
                    pump.Tag = data[TAG_INDEX];

                //----------------------------------

                if(!string.IsNullOrEmpty(data[PUMP_HP_INDEX])) {
                    double y;
                    if (!data[PUMP_HP_INDEX].ToDouble(out y) || y <= 0.0) {
                        LogException(new ENException(ErrorCode.Err202, SectType.PUMPS, name));
                    }
                    else {
                        pump.Ptype = PumpType.CONST_HP;
                        pump.Km = y;
                    }
                }

                if (!string.IsNullOrEmpty(data[PUMP_HCURVE_INDEX])) {
                    Curve curve = net.GetCurve(data[PUMP_HCURVE_INDEX]);
                    if (curve == null) {
                        LogException(new ENException(ErrorCode.Err206, SectType.PUMPS, name));
                    }
                    else {
                        pump.HCurve = curve;
                    }
                }

                if(!string.IsNullOrEmpty(data[PUMP_PATTERN_INDEX])) {
                    Pattern p = net.GetPattern(data[PUMP_PATTERN_INDEX]);

                    if (p == null) {
                        LogException(new ENException(ErrorCode.Err205, SectType.PUMPS, name));
                    }
                    else {
                        pump.UPat = p;
                    }
                }


                if(!string.IsNullOrEmpty(data[PUMP_SPEED_INDEX])) {
                    double y;
                    if (!data[PUMP_SPEED_INDEX].ToDouble(out y) || y < 0.0) {
                        LogException(new ENException(ErrorCode.Err202, SectType.PUMPS, name));
                    }
                    else {
                        pump.Kc = y;
                    }                    
                }

                //------------------------------

                if (!string.IsNullOrEmpty(data[PUMP_ECURVE_INDEX])) {
                    Curve curve = net.GetCurve(data[PUMP_ECURVE_INDEX]);

                    if (curve == null) {
                        LogException(new ENException(ErrorCode.Err217, SectType.ENERGY, name));
                    }
                    else {
                        pump.ECurve = curve;
                    }
                }

                if(!string.IsNullOrEmpty(data[PUMP_EPRICE_INDEX])) {
                    double cost;

                    if (!data[PUMP_EPRICE_INDEX].ToDouble(out cost)) {
                        LogException(new ENException(ErrorCode.Err217, SectType.ENERGY, name));
                    }
                    else {
                        pump.ECost = cost;
                    }
                }

                if (!string.IsNullOrEmpty(data[PUMP_PRICEPAT_INDEX])) {
                    Pattern pattern = net.GetPattern(data[PUMP_PRICEPAT_INDEX]);

                    if (pattern == null) {
                        LogException(new ENException(ErrorCode.Err217, SectType.ENERGY, name));
                    }
                    else {
                        pump.EPat = pattern;
                    }
                }


                // FIXME: status changes
                if (!string.IsNullOrEmpty(data[PUMP_STATUS_INDEX])) {
                    StatType status = (StatType)(-1);
                    if (data[PUMP_STATUS_INDEX].Match(Keywords.w_OPEN)) {
                        status = StatType.OPEN;
                    }
                    else if (data[PUMP_STATUS_INDEX].Match(Keywords.w_CLOSED)) {
                        status = StatType.CLOSED;
                    }
                    else {
                        LogException(new ENException(ErrorCode.Err211, SectType.STATUS, name));
                    }

                    if (status >= 0)
                        ChangeStatus(pump, status, 0.0);    
                }

                net.Links.Add(pump);
            }            
        }

        private void ReadValves() {
            int n = _reader.ReadInteger();
            
            if(n <= 0)
                return;

            for(int i = 0; i < n; i++) {
                double length = 0, diam, rcoeff = 0, lcoeff, setting;
                LinkType type;
                StatType status = StatType.OPEN;

                string name = _reader.ReadString();
                Node j1 = net.GetNode(_reader.ReadString());
                Node j2 = net.GetNode(_reader.ReadString());
                var points = _reader.ReadVertices();
                var data = _reader.ReadArray(MAXLINKPROPS);


                if(net.GetLink(name) != null) {
                    LogException(new ENException(ErrorCode.Err215, SectType.VALVES, name));
                    continue;
                }

                if(j1 == null || j2 == null) {
                    LogException(new ENException(ErrorCode.Err203, SectType.VALVES, name));
                    continue;
                }

                if(j1.Equals(j2)) {
                    LogException(new ENException(ErrorCode.Err222, SectType.VALVES, name));
                    continue;
                }

                if (!EnumsTxt.TryParse(data[VALVE_TYPE_INDEX], out type)) {
                    LogException(new ENException(ErrorCode.Err201, SectType.VALVES, data[VALVE_TYPE_INDEX]));
                    continue;
                }

                if(!data[VALVE_DIAM_INDEX].ToDouble(out diam) || diam <= 0.0) {
                    LogException(new ENException(ErrorCode.Err202, SectType.VALVES, name));
                    continue;
                }

                Valve valve = new Valve(name) {
                    FirstNode = j1,
                    SecondNode = j2
                };

                if(type == LinkType.GPV) { /* Headloss curve for GPV */
                    Curve t = net.GetCurve(data[VALVE_SETTING_INDEX]); 
                    if(t == null) {
                        LogException(new ENException(ErrorCode.Err206, SectType.VALVES, name));
                        continue;
                    }

                    setting = net.Curves.IndexOf(t);
                    log.Warning("GPV Valve, index as roughness !");
                    valve.Curve = t;
                    status = StatType.OPEN;
                }
                else if(!data[VALVE_SETTING_INDEX].ToDouble(out setting)) {
                    LogException(new ENException(ErrorCode.Err202));
                    continue;
                }

                if(!data[VALVE_MLOSS_INDEX].ToDouble(out lcoeff) || lcoeff < 0.0) {
                    LogException(new ENException(ErrorCode.Err202, SectType.VALVES, name));
                    continue;
                }

                /* Check that PRV, PSV, or FCV not connected to a tank & */
                /* check for illegal connections between pairs of valves.*/
                if (j1.Type > NodeType.JUNC || j2.Type > NodeType.JUNC) {
                    switch (type) {
                    case LinkType.PRV:
                    case LinkType.PSV:
                    case LinkType.FCV:
                        LogException(new ENException(ErrorCode.Err219, SectType.VALVES, name));
                        continue;
                    }
                }

                if(!Valvecheck(net, type, j1, j2))
                    throw new ENException(ErrorCode.Err220);

                valve.FirstNode = j1;
                valve.SecondNode = j2;
                valve.Lenght = length;
                valve.Diameter = diam;
                valve.Kc = rcoeff;
                valve.Km = lcoeff;
                valve.Kb = Constants.MISSING;
                valve.Kw = Constants.MISSING;
                valve.Type = type;
                valve.Status = status;

                if(points.Length > 0)
                    valve.Vertices.AddRange(points);

                if(!string.IsNullOrEmpty(data[COMMENT_INDEX]))
                    valve.Comment = data[COMMENT_INDEX];

                if(!string.IsNullOrEmpty(data[TAG_INDEX]))
                    valve.Tag = data[TAG_INDEX];

                net.Links.Add(valve);
            }            
        }

        private void ReadPipes() {
            int n = _reader.ReadInteger();

            if (n <= 0) return;

            for (int i = 0; i < n; i++) {
                string name = _reader.ReadString();
                Node j1 = net.GetNode(_reader.ReadString());
                Node j2 = net.GetNode(_reader.ReadString());
                var points = _reader.ReadVertices();
                var data = _reader.ReadArray(MAXLINKPROPS);

                if (net.GetLink(name) != null) {
                    LogException(new ENException(ErrorCode.Err215, SectType.JUNCTIONS, name));
                    continue;
                }

                if (j1 == null || j2 == null) {
                    LogException(new ENException(ErrorCode.Err203, SectType.PIPES, name));
                    continue;
                }

                if (j1.Equals(j2)) {
                    LogException(new ENException(ErrorCode.Err222, SectType.PIPES, name));
                    continue;
                }

                double length, diam, rcoeff, lcoeff;

                if (!data[PIPE_LEN_INDEX].ToDouble(out length) || length <= 0.0 ||
                    !data[PIPE_DIAM_INDEX].ToDouble(out diam) || diam <= 0.0 ||
                    !data[PIPE_ROUGH_INDEX].ToDouble(out rcoeff) || rcoeff <= 0.0 ||
                    !data[PIPE_MLOSS_INDEX].ToDouble(out lcoeff) || lcoeff < 0.0
                ) {
                    LogException(new ENException(ErrorCode.Err202, SectType.PIPES, name));
                    continue;
                }

                LinkType type = LinkType.PIPE;
                StatType status = StatType.OPEN;

                if (data[PIPE_STATUS_INDEX].Match(Keywords.w_CV)) {
                    type = LinkType.CV;
                }
                else if(data[PIPE_STATUS_INDEX].Match(Keywords.w_CLOSED)) {
                    status = StatType.CLOSED;
                }
                else if(data[PIPE_STATUS_INDEX].Match(Keywords.w_OPEN)) {
                    status = StatType.OPEN;
                }
                else {
                    LogException(new ENException(ErrorCode.Err202, SectType.PIPES, name));
                    continue;                    
                }

                Link link = new Link(name) {
                    FirstNode = j1,
                    SecondNode = j2
                };

                link.FirstNode = j1;
                link.SecondNode = j2;
                link.Lenght = length;
                link.Diameter = diam;
                link.Kc = rcoeff;
                link.Km = lcoeff;
                link.Kb = Constants.MISSING;
                link.Kw = Constants.MISSING;
                link.Type = type;
                link.Status = status;
                
                if(points.Length > 0)
                    link.Vertices.AddRange(points);

                if(!string.IsNullOrEmpty(data[COMMENT_INDEX]))
                    link.Comment = data[COMMENT_INDEX];

                if(!string.IsNullOrEmpty(data[TAG_INDEX]))
                    link.Tag = data[TAG_INDEX];

                if (!string.IsNullOrEmpty(data[PIPE_KBULK_INDEX])) {
                    double kb;
                    if (!data[PIPE_KBULK_INDEX].ToDouble(out kb)) {
                        LogException(new ENException(ErrorCode.Err202, SectType.PIPES, name));
                    }
                    else {
                        link.Kb = kb;
                    }
                }

                if(!string.IsNullOrEmpty(data[PIPE_KWALL_INDEX])) {
                    double kw;
                    if(!data[PIPE_KWALL_INDEX].ToDouble(out kw)) {
                        LogException(new ENException(ErrorCode.Err202, SectType.PIPES, name));
                    }
                    else {
                        link.Kw = kw;
                    }
                }
                
                net.Links.Add(link);
            }
        }

        private void ReadTanks() {
            int n = _reader.ReadInteger();
            if (n <= 0) return;

            for(int i = 0; i < n; i++) {
                double el, initlevel, minlevel, maxlevel, minvol, diam;
                Curve vcurve = null;

                string name = _reader.ReadString();
                var point = _reader.ReadPoint();
                var data = _reader.ReadArray(MAXNODEPROPS);

                if (net.GetNode(name) != null) {
                    LogException(new ENException(ErrorCode.Err215, SectType.TANKS, name));
                    continue;
                }

                if(!data[TANK_ELEV_INDEX].ToDouble(out el)) {
                    LogException(new ENException(ErrorCode.Err202, SectType.TANKS, data[TANK_ELEV_INDEX]));
                    continue;
                }

                if(!data[TANK_INITLVL_INDEX].ToDouble(out initlevel)) {
                    LogException(new ENException(ErrorCode.Err202, SectType.TANKS, data[TANK_INITLVL_INDEX]));
                    continue;
                }

                if(!data[TANK_MINLVL_INDEX].ToDouble(out minlevel)) {
                    LogException(new ENException(ErrorCode.Err202, SectType.TANKS, data[TANK_MINLVL_INDEX]));
                    continue;
                }

                if(!data[TANK_MAXLVL_INDEX].ToDouble(out maxlevel)) {
                    LogException(new ENException(ErrorCode.Err202, SectType.TANKS, data[TANK_MAXLVL_INDEX]));
                    continue;
                }

                if(!data[TANK_DIAM_INDEX].ToDouble(out diam) || diam < 0) {
                    LogException(new ENException(ErrorCode.Err202, SectType.TANKS, data[TANK_DIAM_INDEX]));
                    continue;
                }

                if(!data[TANK_MINVOL_INDEX].ToDouble(out minvol) || minvol < 0) {
                    LogException(new ENException(ErrorCode.Err202, SectType.TANKS, data[TANK_MINVOL_INDEX]));
                    continue;
                }

                if (!string.IsNullOrEmpty(data[TANK_VCURVE_INDEX])) {
                    if((vcurve = net.GetCurve(data[TANK_VCURVE_INDEX])) == null) {
                        LogException(new ENException(ErrorCode.Err206, SectType.TANKS, data[TANK_VCURVE_INDEX]));
                    }
                }

                var tank = new Tank(name) { Position = point };

                if(!string.IsNullOrEmpty(data[COMMENT_INDEX]))
                    tank.Comment = data[COMMENT_INDEX];

                if(!string.IsNullOrEmpty(data[TAG_INDEX]))
                    tank.Tag = data[TAG_INDEX];

                tank.RptFlag = false;
                tank.Elevation = el;
                tank.C0 = 0.0;
                tank.QualSource = null;
                tank.Ke = 0.0;

                tank.H0 = initlevel;
                tank.Hmin = minlevel;
                tank.Hmax = maxlevel;
                tank.Area = diam;
                tank.Pattern = null;
                tank.Kb = Constants.MISSING;

                double area = Math.PI * diam * diam / 4.0d;

                tank.Vmin = area * minlevel;
                if(minvol > 0.0)
                    tank.Vmin = minvol;

                tank.V0 = tank.Vmin + area * (initlevel - minlevel);
                tank.Vmax = tank.Vmin + area * (maxlevel - minlevel);

                tank.Vcurve = vcurve;
                tank.MixModel = MixType.MIX1;
                tank.V1Max = 1.0;

                tank.Kb = 0;

                if (!string.IsNullOrEmpty(data[TANK_MIXMODEL_INDEX])) {
                    MixType type;

                    if (!EnumsTxt.TryParse(data[TANK_MIXMODEL_INDEX], out type)) {
                        LogException(new ENException(ErrorCode.Err201, SectType.MIXING, data[TANK_MIXMODEL_INDEX]));
                    }
                    else {
                        tank.MixModel = type;

                        if (type == MixType.MIX2 && !string.IsNullOrEmpty(data[TANK_MIXFRAC_INDEX])) {
                            double v;
                            if (!data[TANK_MIXFRAC_INDEX].ToDouble(out v)) {
                                LogException(new ENException(ErrorCode.Err209, SectType.MIXING, name));
                            }
                            else {
                                if (v == 0.0)
                                    v = 1.0;
                            
                                tank.V1Max = v;
                            }
                        }
                 
                    }
                }

                if (!string.IsNullOrEmpty(data[TANK_KBULK_INDEX])) {
                    double kb;
                    if (!data[TANK_KBULK_INDEX].ToDouble(out kb)) {
                        LogException(new ENException(ErrorCode.Err209, SectType.REACTIONS, name));
                    }
                    else {
                        tank.Kb = kb;
                    }
                }

                if(!string.IsNullOrEmpty(data[TANK_INITQUAL_INDEX])) {
                    double c0;

                    if(!data[TANK_INITQUAL_INDEX].ToDouble(out c0)) {
                        LogException(new ENException(ErrorCode.Err202, SectType.QUALITY, data[TANK_INITQUAL_INDEX]));
                    }
                    else {
                        tank.C0 = c0;
                    }
                }

                if(!string.IsNullOrEmpty(data[TANK_SRCQUAL_INDEX])) {
                    SourceType type;
                    double c0;
                    Pattern pat = null;

                    if(!data[TANK_SRCQUAL_INDEX].ToDouble(out c0)) {
                        LogException(new ENException(ErrorCode.Err202, SectType.SOURCES, data[TANK_SRCQUAL_INDEX]));
                    }
                    else if(!EnumsTxt.TryParse(data[TANK_SRCTYPE_INDEX], out type)) {
                        LogException(new ENException(ErrorCode.Err201, SectType.SOURCES, data[TANK_SRCTYPE_INDEX]));
                    }
                    else if(!string.IsNullOrEmpty(data[TANK_SRCPAT_INDEX]) && (pat = net.GetPattern(data[TANK_SRCPAT_INDEX])) == null) {
                        LogException(new ENException(ErrorCode.Err205, SectType.SOURCES, data[TANK_SRCPAT_INDEX]));
                    }
                    else {
                        tank.QualSource = new QualSource(type, c0, pat);
                    }
                }

                net.Nodes.Add(tank);
            }
        }

        private void ReadReservoirs() {

            int n = _reader.ReadInteger();

            if (n <= 0) return;

            for(int i = 0; i < n; i++) {
                string name = _reader.ReadString();
                var point = _reader.ReadPoint();
                var data = _reader.ReadArray(MAXNODEPROPS);

                if(net.GetNode(name) != null) {
                    LogException(new ENException(ErrorCode.Err215, SectType.RESERVOIRS, name));
                    continue;
                }

                var tank = new Tank(name) { Position = point };
                
                if(!string.IsNullOrEmpty(data[COMMENT_INDEX])) 
                    tank.Comment = data[COMMENT_INDEX];

                if (!string.IsNullOrEmpty(data[TAG_INDEX]))
                    tank.Tag = data[TAG_INDEX];

                double head;

                if (!data[RES_HEAD_INDEX].ToDouble(out head)) {
                    LogException(new ENException(ErrorCode.Err202, SectType.RESERVOIRS, data[RES_HEAD_INDEX]));
                    continue;
                }
                
                tank.Elevation = head;

                if (!string.IsNullOrEmpty(data[RES_PATTERN_INDEX])) {
                    Pattern pat = net.GetPattern(data[RES_PATTERN_INDEX]);

                    if (pat == null) {
                        LogException(new ENException(ErrorCode.Err205, tank.Name, data[RES_PATTERN_INDEX]));
                    }
                    
                    tank.Pattern = pat;

                }

                if (!string.IsNullOrEmpty(data[RES_INITQUAL_INDEX])) {
                    double c0;
                    if (!data[RES_INITQUAL_INDEX].ToDouble(out c0)) {
                        LogException(new ENException(ErrorCode.Err202, SectType.RESERVOIRS, data[RES_INITQUAL_INDEX]));
                    }
                    else {
                        tank.C0 = c0;
                    }
                }

                if(!string.IsNullOrEmpty(data[RES_SRCQUAL_INDEX])) {
                    SourceType type;
                    double c0;
                    Pattern pat = null;

                    if(!data[RES_SRCQUAL_INDEX].ToDouble(out c0)) {
                        LogException(new ENException(ErrorCode.Err202, SectType.SOURCES, data[RES_SRCQUAL_INDEX]));
                    }
                    else if(!EnumsTxt.TryParse(data[RES_SRCTYPE_INDEX], out type)) {
                        LogException(new ENException(ErrorCode.Err201, SectType.SOURCES, data[RES_SRCTYPE_INDEX]));
                    }
                    else if(!string.IsNullOrEmpty(data[RES_SRCPAT_INDEX]) && (pat = net.GetPattern(data[RES_SRCPAT_INDEX])) == null) {
                        LogException(new ENException(ErrorCode.Err205, SectType.SOURCES, data[RES_SRCPAT_INDEX]));
                    }
                    else {
                        tank.QualSource = new QualSource(type, c0, pat);
                    }
                }

                net.Nodes.Add(tank);
            }
        }

        private void ReadJunctions() {
            

            int n = _reader.ReadInteger();
            
            if (n <= 0) return;

            for(int i = 0; i < n; i++) {
                string name = _reader.ReadString();
                var position = _reader.ReadPoint();
                var data = _reader.ReadArray(MAXNODEPROPS);
                var demands = _reader.ReadList();

                if (net.GetNode(name) != null) {
                    LogException(new ENException(ErrorCode.Err215, SectType.JUNCTIONS, name));
                    continue;
                }                
                
                var node = new Node(name) { Position = position };

                
                if(!string.IsNullOrEmpty(data[COMMENT_INDEX])) 
                    node.Comment = data[COMMENT_INDEX];

                if (!string.IsNullOrEmpty(data[TAG_INDEX]))
                    node.Tag = data[TAG_INDEX];

                double el;

                if (!data[JUNC_ELEV_INDEX].ToDouble(out el)) {
                    LogException(new ENException(ErrorCode.Err202, SectType.JUNCTIONS, name));
                    continue;
                }

                node.Elevation = el;

                if (!string.IsNullOrEmpty(data[JUNC_DEMAND_INDEX])) {
                    double demand;
                   
                    if (!data[JUNC_DEMAND_INDEX].ToDouble(out demand)) {
                        LogException(new ENException(ErrorCode.Err202, SectType.JUNCTIONS, name));
                        continue;
                    }

                    node.PrimaryDemand.Base = demand;
                }
                else {
                    // node.PrimaryDemand.Base = 0;
                }

                if(!string.IsNullOrEmpty(data[JUNC_PATTERN_INDEX])) {
                    Pattern pattern = net.GetPattern(data[JUNC_PATTERN_INDEX]);
                    if (pattern == null) {
                        LogException(new ENException(ErrorCode.Err205, SectType.JUNCTIONS, data[JUNC_PATTERN_INDEX]));
                    }
                    else {
                        node.PrimaryDemand.pattern = pattern;
                    }
                }

                int demandCount;

                if (int.TryParse(data[JUNC_DMNDCAT_INDEX], out demandCount) && demandCount > 1) {
                    foreach (string s in demands) {
                        Pattern pat;
                        double @base;

                        string[] demandData = s.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

                        if (demandData.Length < 2) {
                            // TODO:ERROR?
                        }
                        else if(!demandData[0].ToDouble(out @base)) {
                            LogException(new ENException(ErrorCode.Err202, SectType.JUNCTIONS, name));
                        }
                        else if ((pat = net.GetPattern(demandData[1])) == null) {
                            LogException(new ENException(ErrorCode.Err205, SectType.JUNCTIONS, demandData[1]));
                        }
                        else {
                            node.Demands.Add(new Demand(@base, pat));
                        }

                    }
                }
                
                if(!string.IsNullOrEmpty(data[JUNC_EMITTER_INDEX])) {
                    double k;

                    if (node.Type == NodeType.TANK) {
                        LogException(new ENException(ErrorCode.Err209, data[JUNC_EMITTER_INDEX], name));
                    }
                    else if (!data[JUNC_EMITTER_INDEX].ToDouble(out k) || k < 0.0) {
                        LogException(new ENException(ErrorCode.Err202));
                    }
                    else {
                        node.Ke = k;
                    }
                }

                if (!string.IsNullOrEmpty(data[JUNC_INITQUAL_INDEX])) {
                    double c0;

                    if (!data[JUNC_INITQUAL_INDEX].ToDouble(out c0)) {
                        LogException(new ENException(ErrorCode.Err202));
                    }
                    else {
                        node.C0 = c0;
                    }
                }

                if (!string.IsNullOrEmpty(data[JUNC_SRCQUAL_INDEX])) {
                    SourceType type;
                    double c0;
                    Pattern pat = null;

                    if (!data[JUNC_SRCQUAL_INDEX].ToDouble(out c0)) {
                        LogException(new ENException(ErrorCode.Err202, SectType.SOURCES, data[JUNC_SRCQUAL_INDEX]));
                    }
                    else if(!EnumsTxt.TryParse(data[JUNC_SRCTYPE_INDEX], out type)) {
                        LogException(new ENException(ErrorCode.Err201, SectType.SOURCES, data[JUNC_SRCTYPE_INDEX]));
                    }
                    else if(!string.IsNullOrEmpty(data[JUNC_SRCPAT_INDEX]) && (pat = net.GetPattern(data[JUNC_SRCPAT_INDEX])) == null) {
                        LogException(new ENException(ErrorCode.Err205, SectType.SOURCES, data[JUNC_SRCPAT_INDEX]));
                    }
                    else {
                        node.QualSource = new QualSource(type, c0, pat);
                    }
                }
                
                net.Nodes.Add(node);
            }
        }

        private void ReadCurves() {
            int n = _reader.ReadInteger();

            if (n < 0) return;

            for(int i = 0; i < n; i++) {
                var curve = new Curve(_reader.ReadString()) {
                    Comment = _reader.ReadString()
                };

                string sCurveType = _reader.ReadString();
                
                try {
                    curve.Type = (CurveType)Enum.Parse(typeof(CurveType), sCurveType, true);
                }
                catch {

                }

                var xa = _reader.ReadList();
                var ya = _reader.ReadList();

                for(int j = 0; j < xa.Count; j++)
                    curve.Add(
                        double.Parse(xa[j], NumberFormatInfo.InvariantInfo),
                        double.Parse(ya[j], NumberFormatInfo.InvariantInfo));

                net.Curves.Add(curve);
            }
        }

        private void ReadPatterns() {
            int n = _reader.ReadInteger();

            if (n < 0) return;

            for(int i = 0; i < n; i++) {
                var pat = new Pattern(_reader.ReadString()) {
                    Comment = _reader.ReadString()
                };

                var multipliers = _reader.ReadList();

                foreach (string s in multipliers) {
                    double factor;
                    if (s.ToDouble(out factor)) {
                        pat.Add(factor);
                    }
                    else {
                        LogException(new ENException(ErrorCode.Err202, "PATTERNS", s));
                        // FIXME: what to do here?
                        break;
                    }
                }

                net.Patterns.Add(pat);
            }
        }

        private static long GetSeconds(string value) {
            value = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(value)) return -1;

            int index = value.LastIndexOfAny(new[] {' ', '\t'});

            if (index == -1) {
                return (long)(Utilities.GetHour(value) * 3600);
            }

            return (long)(Utilities.GetHour(value.Substring(0, index), value.Substring(index + 1)) * 3600);
        }

        private void ReadOptions() {
            double y;

            var options = _reader.ReadArray(MAXOPTIONS);

            bool autoLength = _reader.ReadBoolean();

            //-----------------------------------------------------------------------------------

            string line = options[FLOW_UNITS_INDEX];

            {
                FlowUnitsType flag;

                if (EnumsTxt.TryParse(line, out flag)) {
                    net.FlowFlag = flag;
                }
                else {
                    LogException(new ENException(ErrorCode.Err201, SectType.OPTIONS, "UNITS " + line));
                }
            }

            //-----------------------------------------------------------------------------------

            line = options[HLOSS_FORM_INDEX];
            {
                FormType flag;

                if (EnumsTxt.TryParse(line, out flag)) {
                    net.FormFlag = flag;
                }
                else {
                    LogException(new ENException(ErrorCode.Err201, SectType.OPTIONS, "HEADLOSS " + line));
                }
            }

            //-----------------------------------------------------------------------------------

            line = options[SPEC_GRAV_INDEX];

            if (line.ToDouble(out y) && y > 0) {
                net.SpGrav = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.OPTIONS, "SPECIFIC GRAVITY " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[VISCOS_INDEX];

            if (line.ToDouble(out y)) {
                net.Viscos = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.OPTIONS, "VISCOSITY " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[TRIALS_INDEX];

            if (line.ToDouble(out y) && y > 0) {
                net.MaxIter = (int)y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.OPTIONS, "TRIALS " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[ACCURACY_INDEX];

            if (line.ToDouble(out y) && y > 0) {
                y = Math.Max(y, 1e-5);
                y = Math.Min(y, 1e-1);
                net.HAcc = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.OPTIONS, "ACCURACY " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[UNBALANCED_INDEX];

            if (line.Match(Keywords.w_STOP)) {
                net.ExtraIter = -1;
            }
            else if (line.Match(Keywords.w_CONTINUE)) {
                net.ExtraIter = 0;
            }
            else {
                LogException(new ENException(ErrorCode.Err201, SectType.OPTIONS, "UNBALANCED " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[GLOBAL_PAT_INDEX];

            if (!string.IsNullOrEmpty(line)) {
                net.DefPatId = line;
            }

            //-----------------------------------------------------------------------------------

            line = options[DEMAND_MULT_INDEX];

            if (line.ToDouble(out y) && y > 0) {
                net.DMult = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.OPTIONS, "DEMAND MULTIPLIER " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[EMITTER_EXP_INDEX];

            if (line.ToDouble(out y) && y > 0) {
                net.QExp = 1 / y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.OPTIONS, "EMITTER EXPONENT " + line));
            }

            //-----------------------------------------------------------------------------------

            {
                line = options[STATUS_RPT_INDEX];
                StatFlag flag;

                if (EnumsTxt.TryParse(line, out flag) && y > 0) {
                    net.StatFlag = flag;
                }
                else {
                    LogException(new ENException(ErrorCode.Err201, SectType.REPORT, "STATUS " + line));
                }
            }

            //-----------------------------------------------------------------------------------

            line = options[QUAL_PARAM_INDEX];

            {
                QualType flag;
                net.QualFlag = EnumsTxt.TryParse(line, out flag) ? flag : QualType.CHEM;
                
                string units = options[QUAL_UNITS_INDEX];

                switch (net.QualFlag) {
                case QualType.CHEM:
                    net.ChemName = line;
                    if(!string.IsNullOrEmpty(units)) net.ChemUnits = units;
                    break;

                case QualType.TRACE:
                    // FIXME: parse nodes first!
                    Node nodeRef = net.GetNode(options[TRACE_NODE_INDEX]);
                
                    if (nodeRef == null) {
                        LogException(new ENException(ErrorCode.Err212, SectType.OPTIONS, "QUALITY TRACE " + line));
                    }
                    else {
                        net.TraceNode = nodeRef.Name;
                        net.ChemName = Keywords.u_PERCENT;
                        net.ChemUnits = units;
                    }

                    break;

                case QualType.AGE:
                    net.ChemName = Keywords.w_AGE;
                    net.ChemUnits = Keywords.u_HOURS;
                    break;
                }
            }

            //-----------------------------------------------------------------------------------

            line = options[DIFFUS_INDEX];

            if(line.ToDouble(out y) && y >= 0) {
                net.Diffus = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err201, SectType.OPTIONS, "DIFFUSIVITY " + line));
            }

            
            //-----------------------------------------------------------------------------------

            line = options[QUAL_TOL_INDEX];

            if(line.ToDouble(out y) && y > 0) {
                net.QTol = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err201, SectType.OPTIONS, "QTOL " + line));
            }

            //-----------------------------------------------------------------------------------
            
            line = options[BULK_ORDER_INDEX];

            if(line.ToDouble(out y)) {
                net.BulkOrder = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.REACTIONS, "ORDER BULK " + line));
            }

            //-----------------------------------------------------------------------------------
            
            line = options[WALL_ORDER_INDEX];

            if(string.Equals(line, "Zero",StringComparison.OrdinalIgnoreCase)) {
                net.WallOrder = 0;
            }
            else if (string.Equals(line, "First", StringComparison.OrdinalIgnoreCase)) {
                net.WallOrder = 1;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.REACTIONS, "ORDER WALL " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[GLOBAL_KBULK_INDEX];

            if(line.ToDouble(out y)) {
                net.KBulk = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.REACTIONS, "GLOBAL BULK " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[GLOBAL_KWALL_INDEX];

            if(line.ToDouble(out y)) {
                net.KWall = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.REACTIONS, "GLOBAL WALL " + line));
            }
            
            //-----------------------------------------------------------------------------------

            line = options[LIMIT_QUAL_INDEX];

            if(line.ToDouble(out y)) {
                net.CLimit = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.REACTIONS, "LIMITING POTENTIAL " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[ROUGH_CORREL_INDEX];

            if(line.ToDouble(out y)) {
                net.RFactor = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.REACTIONS, "ROUGHNESS CORRELATION " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[DURATION_INDEX];
            long t;

            if((t = GetSeconds(line)) >= 0) {
                net.Duration = t;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.TIMES, "DURATION " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[HYD_TSTEP_INDEX];
            
            if((t = GetSeconds(line)) >= 0) {
                net.HStep = t;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.TIMES, "HYDRAULIC TIMESTEP " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[QUAL_TSTEP_INDEX];

            if((t = GetSeconds(line)) >= 0) {
                //Check if Quality Time Step is in hours instead of minutes
                if(t > 3600)
                    net.QStep = t / 60;
                else
                    net.QStep = t;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.TIMES, "QUALITY TIMESTEP " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[PAT_TSTEP_INDEX];

            if((t = GetSeconds(line)) >= 0) {
                net.PStep = t;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.TIMES, "PATTERN TIMESTEP " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[PAT_START_INDEX];

            if((t = GetSeconds(line)) >= 0) {
                net.PStart = t;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.TIMES, "PATTERN START " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[RPT_TSTEP_INDEX];

            if((t = GetSeconds(line)) >= 0) {
                net.RStep = t;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.TIMES, "REPORT TIMESTEP " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[RPT_START_INDEX];

            if((t = GetSeconds(line)) >= 0) {
                net.RStart = t;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.TIMES, "REPORT START " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[START_TIME_INDEX];

            if((t = GetSeconds(line)) >= 0) {
                net.Tstart = t % Constants.SECperDAY;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.TIMES, "START CLOCKTIME " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[TIME_STAT_INDEX];
            {
                TStatType flag;

                if (EnumsTxt.TryParse(line, out flag)) {
                    net.TstatFlag = flag;
                }
                else {
                    LogException(new ENException(ErrorCode.Err213, SectType.TIMES, "STATISTIC " + line));
                }
            }

            //-----------------------------------------------------------------------------------

            line = options[EFFIC_INDEX];

            if(line.ToDouble(out y) && y > 0) {
                net.EPump = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.ENERGY, "GLOBAL EFFIC " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[EPRICE_INDEX];

            if(line.ToDouble(out y)) {
                net.ECost = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.ENERGY, "GLOBAL PRICE " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[EPRICE_INDEX];

            if(line.ToDouble(out y)) {
                net.ECost = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.ENERGY, "GLOBAL PRICE " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[PRICE_PAT_INDEX];
        
            if (!string.IsNullOrEmpty(line)) {
                Pattern pat = net.GetPattern(line);

                if(pat != null) {
                    net.EPatId = pat.Name;
                }
                else {
                    LogException(new ENException(ErrorCode.Err213, SectType.ENERGY, "GLOBAL PATTERN " + line));
                }
            }

            //-----------------------------------------------------------------------------------

            line = options[DMND_CHARGE_INDEX];

            if(line.ToDouble(out y)) {
                net.DCost = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.ENERGY, "DEMAND CHARGE " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[CHECK_FREQ_INDEX];

            if(line.ToDouble(out y) && y > 0) {
                net.CheckFreq = (int)y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.OPTIONS, "CHECKFREQ " + line));
            }
            
            //-----------------------------------------------------------------------------------

            line = options[MAX_CHECK_INDEX];

            if(line.ToDouble(out y) && y > 0) {
                net.MaxCheck = (int)y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.OPTIONS, "MAXCHECK " + line));
            }

            //-----------------------------------------------------------------------------------

            line = options[DAMP_LIMIT_INDEX];

            if(line.ToDouble(out y)) {
                net.DampLimit = y;
            }
            else {
                LogException(new ENException(ErrorCode.Err213, SectType.OPTIONS, "DAMPLIMIT " + line));
            }

        }
        
        /// <summary>Delphi TReader partial implementation.</summary>
        /// <remarks>TReader is a specialized filer that reads component data from an associated stream.</remarks>
        private class Reader {
            private readonly BinaryReader _br;

            public Reader(Stream stream, Encoding enc) { _br = new BinaryReader(stream, enc); }

            /// <summary>
            /// Reads the type of the next item on the reader object's stream and returns
            /// with the stream positioned after the value-type indicator.
            /// </summary>
            private TValueType ReadValue() { return (TValueType)_br.ReadByte(); }

            /// <summary>
            /// Returns the type of the next item in the reader object's stream without 
            /// moving the position of the stream.
            /// </summary>
            /// <returns></returns>
            private TValueType NextValue() { return (TValueType)unchecked((byte)_br.PeekChar()); }

            /// <summary>Reads a tagged string value written by WriteString from the reader object's stream and returns its contents.</summary>
            public string ReadString() {
                int len;

                switch (ReadValue()) {
                case TValueType.vaString:
                    // pascal short string
                    len = _br.ReadByte();
                    return Encoding.ASCII.GetString(_br.ReadBytes(len));

                case TValueType.vaLString:
                    len = _br.ReadInt32();
                    return Encoding.ASCII.GetString(_br.ReadBytes(len));

                case TValueType.vaWString:
                    len = _br.ReadInt32();
                    return Encoding.Unicode.GetString(_br.ReadBytes(len << 1));

                case TValueType.vaUTF8String:
                    len = _br.ReadInt32();
                    return Encoding.UTF8.GetString(_br.ReadBytes(len));

                default:
                    throw new InvalidDataException();
                }
            }

            /// <summary>
            /// Reads an integer-type number from the reader object's stream and returns its value.
            /// </summary>
            public int ReadInteger() {
                var vt = ReadValue();
                switch (vt) {
                case TValueType.vaInt8:
                    return _br.ReadSByte();
                case TValueType.vaInt16:
                    return _br.ReadInt16();
                case TValueType.vaInt32:
                    return _br.ReadInt32();
                default:
                    throw new InvalidDataException();
                }
            }

            public List<string> ReadList() {
                var result = new List<string>();

                var tv = ReadValue();
                if (tv != TValueType.vaList)
                    throw new InvalidDataException();

                while (NextValue() != TValueType.vaNull)
                    result.Add(ReadString());

                ReadValue();

                return result;
            }

            /// <summary>
            /// Reads a boolean from the reader object's stream and returns that boolean value.
            /// </summary>
            public bool ReadBoolean() { return ReadValue() == TValueType.vaTrue; }

            /// <summary>
            /// Reads 10-byte extended value from file.
            /// </summary>
            /// <returns></returns>
            private double ReadExtended() {
                ulong mantissa = _br.ReadUInt64();
                int expon = _br.ReadInt16();

                int sign = (expon & 0x8000) == 0x00 ? 1 : -1;
                expon &= 0x7FFF;

                switch (expon) {
                case 0:
                    return mantissa == 0 ? 0 : double.NaN;

                case 0x7FFF:
                    // Infinity or NaN 

                    // var integral = mantissa >> 63;
                    mantissa &= 0x7FFFFFFFFFFFFFFF;

                    if (mantissa == 0)
                        return sign / 0.0; // return double.PositiveInfinity * sign;

                    return double.NaN;

                default:
                    expon -= 16383;

                    double f = (mantissa >> 32) * Math.Pow(2, expon - 31);
                    f += (mantissa & 0xFFFFFFFF) * Math.Pow(2, expon - 63);
                    return sign < 0 ? -f : f;
                }
            }

            /// <summary>
            /// Reads a floating-point number from the reader object's stream and returns its value.
            /// </summary>
            public double ReadFloat() {
                switch (ReadValue()) {
                case TValueType.vaExtended:
                    return ReadExtended();

                default:
                    _br.BaseStream.Position--;
                    return ReadInt64();
                }
            }

            /// <summary>
            /// Reads an 64-bit integer from the reader object's stream and returns its value.
            /// </summary>
            public long ReadInt64() {
                switch (NextValue()) {
                case TValueType.vaInt64:
                    ReadValue();
                    return _br.ReadInt64();

                default:
                    return ReadInteger();
                }

                
            }

            public string[] ReadArray(int ubound) {
                ubound++;

                int len = ReadInteger();
                // if (len > maxLen) len = maxLen;

                string[] result = new string[ubound];

                for (int i = 0; i < len; i++)
                    if (i < ubound)
                        result[i] = ReadString();
                    else {
                        // skip remaining strings
                        ReadString();
                    }

                return result;
            }

            public EnPoint[] ReadVertices() {
                int n = ReadInteger();
                EnPoint[] points = new EnPoint[n];

                for (int i = 0; i < n; i++) 
                    points[i] = ReadPoint();

                // if (n > 1) Array.Reverse(points);
                return points;
            }

            public EnPoint ReadPoint() {
                return new EnPoint(ReadFloat(),ReadFloat());
            }

            // ReSharper disable InconsistentNaming
            /// <summary>TValueType defines the kinds of values written to and read from filer objects.</summary>
            private enum TValueType:byte {
                ///<summary>identifies the type as the const Variant Null.</summary>
                vaNull,

                ///<summary>identifies the type as a list of tagged items ending with a NULL pointer.</summary>
                
                vaList,
                
                ///<summary>identifies the type as an 8-bit integer.</summary>
                vaInt8,

                ///<summary>identifies the type as a 16-bit integer.</summary>
                vaInt16,

                ///<summary>identifies the type as a 32-bit integer.</summary>
                vaInt32,

                ///<summary>identifies the type as a float or long double.</summary>
                vaExtended,

                ///<summary>identifies the type as a short string</summary>
                vaString,

                ///<summary>identifies the type as an identifier string.</summary>
                vaIdent,

                ///<summary>identifies the type as the boolean false.</summary>
                vaFalse,

                ///<summary>identifies the type as the boolean true.</summary>
                vaTrue,

                ///<summary>identifies the type as a block of binary preceded by a length count.</summary>
                vaBinary,

                ///<summary>identifies the type as a set.</summary>
                vaSet,

                ///<summary>identifies the type as an AnsiString (long string).</summary>
                vaLString,

                ///<summary>identifies the type as nil (Delphi) or NULL (C++).</summary>
                vaNil,

                ///<summary>identifies the type as a TCollection.</summary>
                vaCollection,

                ///<summary>identifies the type as a Single (float).</summary>
                vaSingle,

                ///<summary>identifies the type as a Currency.</summary>
                vaCurrency,

                ///<summary>identifies the type as a Date.</summary>
                vaDate,

                ///<summary>identifies the type as a WideString.</summary>
                vaWString,

                ///<summary>identifies the type as a 64-bit integer.</summary>
                vaInt64,

                ///<summary>identifies the type as a UTF8String.</summary>
                vaUTF8String
            }
            // ReSharper restore InconsistentNaming
        }
    }

}
