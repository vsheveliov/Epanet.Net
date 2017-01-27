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
using System.IO;
using System.Text;

using Epanet.Enums;
using Epanet.Log;
using Epanet.Network.Structures;
using Epanet.Util;

namespace Epanet.Network.IO.Input {


    ///<summary>INP parser class.</summary>
    public class InpParser:InputParser {
        private SectType _sectionType = (SectType)(-1);
        private readonly List<string> _tok = new List<string>(Constants.MAXTOKS);
        private string _comment;
        string _line;

        private Rule _currentRule; // Current rule

        private static readonly string[] optionValueKeywords = {
            Keywords.w_TOLERANCE, Keywords.w_DIFFUSIVITY, Keywords.w_DAMPLIMIT, Keywords.w_VISCOSITY,
            Keywords.w_SPECGRAV,  Keywords.w_TRIALS,      Keywords.w_ACCURACY,  Keywords.w_HTOL,
            Keywords.w_QTOL,      Keywords.w_RQTOL,       Keywords.w_CHECKFREQ, Keywords.w_MAXCHECK, 
            Keywords.w_EMITTER,   Keywords.w_DEMAND
        };

        public InpParser() {
            _currentRule = null;
        }
        
        public override Network Parse(Network nw, string fileName)
        {

            if(string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            net = nw ?? new Network();

            try {
                using(FileStream fs = File.OpenRead(fileName)) {
                    ParsePc(fs);
                    fs.Position = 0;
                    Parse(fs);

                    if (errors.Count > 0)
                        throw new ENException(ErrorCode.Err200);

                    return net;
                }
            }
            catch(IOException ex) {
                throw new ENException(ErrorCode.Err302, ex);
            }
        }

        /// <summary>Parse demands and time patterns first.</summary>
        /// <param name="stream"></param>
        private void ParsePc(Stream stream) {
            var buffReader = new StreamReader(stream, Encoding.Default);

            while ((_line = buffReader.ReadLine()) != null) {

                _line = _line.Trim();

                if (string.IsNullOrEmpty(_line))
                    continue;

                if (_line[0] == '[') {
                    if (_line.StartsWith("[PATTERNS]", StringComparison.OrdinalIgnoreCase)) {
                        _sectionType = SectType.PATTERNS;
                    }
                    else if (_line.StartsWith("[CURVES]", StringComparison.OrdinalIgnoreCase))
                        _sectionType = SectType.CURVES;
                    else
                        _sectionType = (SectType)(-1);
                    continue;
                }

                if (_sectionType != (SectType)(-1)) {
                    if (_line.IndexOf(';') >= 0)
                        _line = _line.Substring(0, _line.IndexOf(';'));

                    if (string.IsNullOrEmpty(_line))
                        continue;

                    if (Tokenize(_line, _tok) == 0)
                        continue;

                    try {
                        switch (_sectionType) {
                        case SectType.PATTERNS:
                            ParsePattern();
                            break;
                        case SectType.CURVES:
                            ParseCurve();
                            break;
                        }
                    }
                    catch (InputException ex) {
                        LogException(ex);
                    }
                }
            }

        }

        // Parse INP file
        private void Parse(Stream stream) {
            
            _sectionType = (SectType)(-1);
            TextReader buffReader = new StreamReader(stream, Encoding.Default); // "ISO-8859-1";

            while ((_line = buffReader.ReadLine()) != null) {
                _comment = null;

                int index = _line.IndexOf(';');

                if (index >= 0) {
                    if (index > 0)
                        _comment = _line.Substring(index + 1).Trim();

                    _line = _line.Substring(0, index);
                }

                //lineCount++;
                _line = _line.Trim();
                if (string.IsNullOrEmpty(_line))
                    continue;

                if (Tokenize(_line, _tok) == 0)
                    continue;

                if (_tok[0][0] == '[') {
                    _sectionType = FindSectionType(_tok[0]);

                    if (_sectionType < 0) {
                        log.Warning("Unknown section type : %s", _tok[0]);
                    }

                    continue;
                }

                if (_sectionType < 0) continue;

                try {
                    switch (_sectionType) {
                    case SectType.TITLE:
                        net.Title.Add(_line);
                        break;
                    case SectType.JUNCTIONS:
                        ParseJunction();
                        break;

                    case SectType.RESERVOIRS:
                    case SectType.TANKS:
                        ParseTank();
                        break;

                    case SectType.PIPES:
                        ParsePipe();
                        break;
                    case SectType.PUMPS:
                        ParsePump();
                        break;
                    case SectType.VALVES:
                        ParseValve();
                        break;
                    case SectType.CONTROLS:
                        ParseControl();
                        break;

                    case SectType.RULES:
                        ParseRule();
                        break;

                    case SectType.DEMANDS:
                        ParseDemand();
                        break;
                    case SectType.SOURCES:
                        ParseSource();
                        break;
                    case SectType.EMITTERS:
                        ParseEmitter();
                        break;
                    case SectType.QUALITY:
                        ParseQuality();
                        break;
                    case SectType.STATUS:
                        ParseStatus();
                        break;
                    case SectType.ENERGY:
                        ParseEnergy();
                        break;
                    case SectType.REACTIONS:
                        ParseReact();
                        break;
                    case SectType.MIXING:
                        ParseMixing();
                        break;
                    case SectType.REPORT:
                        ParseReport();
                        break;
                    case SectType.TIMES:
                        ParseTime();
                        break;
                    case SectType.OPTIONS:
                        ParseOption();
                        break;
                    case SectType.COORDINATES:
                        ParseCoordinate();
                        break;
                    case SectType.VERTICES:
                        ParseVertice();
                        break;
                    case SectType.LABELS:
                        ParseLabel();
                        break;
                    }
                }
                catch (InputException ex) {
                    LogException(ex);
                }
            }

            AdjustData(net);

            net.FieldsMap.Prepare(net.UnitsFlag, net.FlowFlag, net.PressFlag, net.QualFlag, net.ChemUnits, net.SpGrav, net.HStep);

            Convert();
        }

        private static long Atol(string value) {
            value = (value ?? "").Trim();
            if (string.IsNullOrEmpty(value)) return 0;

            int i = 0;

            if(value[0] == '-' || value[0] == '+')
                i++;

            while (i < value.Length) {

                if(!char.IsNumber(value[i]))   
                    break;

                i++;
            }

            long result;

            return long.TryParse(value.Substring(0, i), out result) ? result : 0;
        }


        private void ParseJunction() {
            int n = _tok.Count;
            double el, y = 0.0d;
            Pattern p = null;

            if (net.GetNode(_tok[0]) != null)
                throw new InputException(ErrorCode.Err215, SectType.JUNCTIONS, _tok[0]);

            Node node = new Node(_tok[0]);

            net.Nodes.Add(node);

            if (n < 2)
                throw new InputException(ErrorCode.Err201, SectType.JUNCTIONS, _line);

            if (!_tok[1].ToDouble(out el))
                throw new InputException(ErrorCode.Err202, SectType.JUNCTIONS, _tok[0]);

            if (n >= 3 && !_tok[2].ToDouble(out y)) {
                throw new InputException(ErrorCode.Err202, SectType.JUNCTIONS, _tok[0]);
            }

            if (n >= 4) {
                p = net.GetPattern(_tok[3]);
                if (p == null) {
                    throw new InputException(ErrorCode.Err205, SectType.JUNCTIONS, _tok[0]);
                }
            }

            node.Elevation = el;
            node.C0 = 0.0;
            node.QualSource = null;
            node.Ke = 0.0;
            node.RptFlag = false;

            if (!string.IsNullOrEmpty(_comment))
                node.Comment = _comment;

            if (n >= 3) {
                node.PrimaryDemand.Base = y;
                node.PrimaryDemand.pattern = p;
            }
            
        }

        private void ParseTank() {
            int n = _tok.Count;
            Pattern p = null;
            Curve vcurve = null;
            double el,
                   initlevel = 0.0d,
                   minlevel = 0.0d,
                   maxlevel = 0.0d,
                   minvol = 0.0d,
                   diam = 0.0d;

            if (net.GetNode(_tok[0]) != null)
                throw new InputException(ErrorCode.Err215, SectType.TANKS, _tok[0]);

            Tank tank = new Tank(_tok[0]);
            
            if(!string.IsNullOrEmpty(_comment))
                tank.Comment = _comment;

            net.Nodes.Add(tank);

            if (n < 2)
                throw new InputException(ErrorCode.Err201, SectType.TANKS, _line);

            if (!_tok[1].ToDouble(out el))
                throw new InputException(ErrorCode.Err202, SectType.TANKS, _tok[0]);

            if(n <= 3) {
                /* Tank is reservoir.*/
                if (n == 3) {
                    p = net.GetPattern(_tok[2]);
                    if (p == null) {
                        throw new InputException(ErrorCode.Err205, SectType.TANKS, _tok[0]);
                    }
                }
            }
            else if (n < 6) {
                throw new InputException(ErrorCode.Err201, SectType.TANKS, _line);
            }
            else {

                /* Check for valid input data */
                if (!_tok[2].ToDouble(out initlevel) ||
                    !_tok[3].ToDouble(out minlevel) ||
                    !_tok[4].ToDouble(out maxlevel) ||
                    !_tok[5].ToDouble(out diam) ||
                    (diam < 0.0))
                    throw new InputException(ErrorCode.Err202, SectType.TANKS, _tok[0]);

                if (n >= 7 && !_tok[6].ToDouble(out minvol))
                    throw new InputException(ErrorCode.Err202, SectType.TANKS, _tok[0]);

                if (n == 8) {
                    vcurve = net.GetCurve(_tok[7]);

                    if (vcurve == null)
                        throw new InputException(ErrorCode.Err206, SectType.TANKS, _tok[0]);
                }
            }

            tank.RptFlag = false;
            tank.Elevation = el;
            tank.C0 = 0.0;
            tank.QualSource = null;
            tank.Ke = 0.0;

            tank.H0 = initlevel;
            tank.Hmin = minlevel;
            tank.Hmax = maxlevel;
            tank.Area = diam;
            tank.Pattern = p;
            tank.Kb = Constants.MISSING;

            /*
            *******************************************************************
             NOTE: The min, max, & initial volumes set here are based on a     
                nominal tank diameter. They will be modified in INPUT1.C if    
                a volume curve is supplied for this tank.                      
            *******************************************************************
            */

            double area = Math.PI * diam * diam / 4.0d;
            tank.Vmin = area * minlevel;
            if (minvol > 0.0) tank.Vmin = minvol;
            tank.V0 = tank.Vmin + area * (initlevel - minlevel);
            tank.Vmax = tank.Vmin + area * (maxlevel - minlevel);

            tank.Vcurve = vcurve;
            tank.MixModel = MixType.MIX1;
            tank.V1Max = 1.0;

        }

        private void ParsePipe() {
            int n = _tok.Count;
            LinkType type = LinkType.PIPE;
            StatType status = StatType.OPEN;
            double length, diam, rcoeff, lcoeff = 0.0d;

            if (net.GetLink(_tok[0]) != null)
                throw new InputException(ErrorCode.Err215, SectType.PIPES, _tok[0]);

            Link link = new Link(_tok[0]);
            net.Links.Add(link);
            
            if (n < 6)
                throw new InputException(ErrorCode.Err201, SectType.PIPES, _line);


            Node j1 = net.GetNode(_tok[1]),
                 j2 = net.GetNode(_tok[2]);
            
            if (j1 == null || j2 == null)
                throw new InputException(ErrorCode.Err203, SectType.PIPES, _tok[0]);


            if (j1.Equals(j2))
                throw new InputException(ErrorCode.Err222, SectType.PIPES, _tok[0]);

            if (!_tok[3].ToDouble(out length) || length <= 0.0 ||
                !_tok[4].ToDouble(out diam) || diam <= 0.0 ||
                !_tok[5].ToDouble(out rcoeff) || rcoeff <= 0.0)
                throw new InputException(ErrorCode.Err202, SectType.PIPES, _tok[0]);


            if (n == 7) {
                if(_tok[6].Match(Keywords.w_CV)) type = LinkType.CV;
                else if(_tok[6].Match(Keywords.w_CLOSED)) status = StatType.CLOSED;
                else if(_tok[6].Match(Keywords.w_OPEN)) status = StatType.OPEN;
                else if (!_tok[6].ToDouble(out lcoeff) || lcoeff < 0)
                    throw new InputException(ErrorCode.Err202, SectType.PIPES, _tok[0]);
            }

            if (n == 8) {
                if (!_tok[6].ToDouble(out lcoeff) || lcoeff < 0)
                    throw new InputException(ErrorCode.Err202, SectType.PIPES, _tok[0]);

                if(_tok[7].Match(Keywords.w_CV)) type = LinkType.CV;
                else if(_tok[7].Match(Keywords.w_CLOSED)) status = StatType.CLOSED;
                else if(_tok[7].Match(Keywords.w_OPEN)) status = StatType.OPEN;
                else
                    throw new InputException(ErrorCode.Err202, SectType.PIPES, _tok[0]); 
            }

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
            link.RptFlag = false;

            if (!string.IsNullOrEmpty(_comment))
                link.Comment = _comment;
        }

        private void ParsePump() {
            int m, n = _tok.Count;
            double[] x = new double[6];

            if (net.GetLink(_tok[0]) != null)
                throw new InputException(ErrorCode.Err215, SectType.PUMPS, _tok[0]);
            
            if (n < 4)
                throw new InputException(ErrorCode.Err201, SectType.PUMPS, _line);

            Node j1 = net.GetNode(_tok[1]),
                 j2 = net.GetNode(_tok[2]);

            if (j1 == null || j2 == null)
                throw new InputException(ErrorCode.Err203, SectType.PUMPS, _tok[0]);

            if (j1.Equals(j2))
                throw new InputException(ErrorCode.Err222, SectType.PUMPS, _tok[0]);

            Pump pump = new Pump(_tok[0]) {
                FirstNode = j1,
                SecondNode = j2,
            };
            
            net.Links.Add(pump);

            if (!string.IsNullOrEmpty(_comment)) pump.Comment = _comment;

            // If 4-th token is a number then input follows Version 1.x format 
            // so retrieve pump curve parameters
            if (_tok[3].ToDouble(out x[0])) {

                m = 1;
                for (int j = 4; j < n; j++) {
                    if (!_tok[j].ToDouble(out x[m]))
                        throw new InputException(ErrorCode.Err202, SectType.PUMPS, _tok[0]);
                    m++;
                }

                GetPumpCurve(pump, m, x); /* Get pump curve params */

                return;
            }

            /* Otherwise input follows Version 2 format */
            /* so retrieve keyword/value pairs.         */
            m = 4;
            while (m < n) {
                if (_tok[m - 1].Match(Keywords.w_POWER)) { /* Const. HP curve       */
                    double y;
                    if (!_tok[m].ToDouble(out y) || y <= 0.0)
                        throw new InputException(ErrorCode.Err202, SectType.PUMPS, _tok[0]);

                    pump.Ptype = PumpType.CONST_HP;
                    pump.Km = y;
                }
                else if (_tok[m - 1].Match(Keywords.w_HEAD)) { /* Custom pump curve      */
                    Curve t = net.GetCurve(_tok[m]);
                    if (t == null) 
                        throw new InputException(ErrorCode.Err206, SectType.PUMPS, _tok[0]);

                    pump.HCurve = t;
                }
                else if (_tok[m - 1].Match(Keywords.w_PATTERN)) { /* Speed/status pattern */
                    Pattern p = net.GetPattern(_tok[m]);
                    if (p == null) 
                        throw new InputException(ErrorCode.Err205, SectType.PUMPS, _tok[0]);

                    pump.UPat = p;
                }
                else if (_tok[m - 1].Match(Keywords.w_SPEED)) { /* Speed setting */
                    double y;
                    if (!_tok[m].ToDouble(out y) || y < 0.0)
                        throw new InputException(ErrorCode.Err202, SectType.PUMPS, _tok[0]);

                    pump.Kc = y;
                }
                else {
                    throw new InputException(ErrorCode.Err201, SectType.PUMPS, _line);
                }

                m += 2;
            }
        }

        private void ParseValve() {
            int n = _tok.Count;
            StatType status = StatType.ACTIVE;
            LinkType type;

            double diam, setting, lcoeff = 0.0;

            if (net.GetLink(_tok[0]) != null)
                throw new InputException(ErrorCode.Err215, SectType.VALVES, _tok[0]);

            Valve valve = new Valve(_tok[0]);
            net.Links.Add(valve);

            if (n < 6)
                throw new InputException(ErrorCode.Err201, SectType.VALVES, _line);

            Node j1 = net.GetNode(_tok[1]),
                 j2 = net.GetNode(_tok[2]);

            if (j1 == null || j2 == null)
                throw new InputException(ErrorCode.Err203, SectType.VALVES, _tok[0]);

            if (j1.Equals(j2))
                throw new InputException(ErrorCode.Err222, SectType.VALVES, _tok[0]);

            if (!EnumsTxt.TryParse(_tok[4], out type))
                throw new InputException(ErrorCode.Err201, SectType.VALVES, _line);

            if (!_tok[3].ToDouble(out diam) || diam <= 0.0)
                throw new InputException(ErrorCode.Err202, SectType.VALVES, _tok[0]);

            if (type == LinkType.GPV) {
                Curve t = net.GetCurve(_tok[5]);
                
                if (t == null)
                    throw new InputException(ErrorCode.Err206, SectType.VALVES, _tok[0]);

                setting = net.Curves.IndexOf(t);
                log.Warning("GPV Valve, index as roughness !");
                valve.Curve = t;
                status = StatType.OPEN;
            }
            else if (!_tok[5].ToDouble(out setting)) {
                throw new InputException(ErrorCode.Err202, SectType.VALVES, _tok[0]);
            }

            if (n >= 7) {
                if (!_tok[6].ToDouble(out lcoeff)) {
                    throw new InputException(ErrorCode.Err202, SectType.VALVES, _tok[0]);
                }
            }

            if ((j1.Type > NodeType.JUNC || j2.Type > NodeType.JUNC) &&
                (type == LinkType.PRV || type == LinkType.PSV || type == LinkType.FCV))
                throw new InputException(ErrorCode.Err219, SectType.VALVES, _tok[0]);

            if (!Valvecheck(net, type, j1, j2))
                throw new InputException(ErrorCode.Err220, SectType.VALVES, _tok[0]);


            valve.FirstNode = j1;
            valve.SecondNode = j2;
            valve.Diameter = diam;
            valve.Lenght = 0.0d;
            valve.Kc = setting;
            valve.Km = lcoeff;
            valve.Kb = 0.0d;
            valve.Kw = 0.0d;
            valve.Type = type;
            valve.Status = status;
            valve.RptFlag = false;

            if (!string.IsNullOrEmpty(_comment))
                valve.Comment = _comment;
        }
      
        private void ParsePattern() {
            Pattern pat = net.GetPattern(_tok[0]);

            if (pat == null) {
                pat = new Pattern(_tok[0]);
                net.Patterns.Add(pat);
            }

            for (int i = 1; i < _tok.Count; i++) {
                double x;

                if (!_tok[i].ToDouble(out x))
                    throw new InputException(ErrorCode.Err202, SectType.PATTERNS, _tok[0]);

                pat.Add(x);
            }
        }

        private void ParseCurve() {
            Curve cur = net.GetCurve(_tok[0]);

            if (cur == null) {
                cur = new Curve(_tok[0]);
                net.Curves.Add(cur);
            }

            double x, y;

            if (!_tok[1].ToDouble(out x) || !_tok[2].ToDouble(out y))
                throw new InputException(ErrorCode.Err202, SectType.CURVES, _tok[0]);

            cur.Add(x, y);
        }

        private void ParseCoordinate() {
            if (_tok.Count < 3)
                throw new InputException(ErrorCode.Err201, SectType.COORDINATES, _line);

            Node node = net.GetNode(_tok[0]);

            if (node == null)
                throw new InputException(ErrorCode.Err203, SectType.COORDINATES, _tok[0]);

            double x, y;

            if (!_tok[1].ToDouble(out x) || !_tok[2].ToDouble(out y))
                throw new InputException(ErrorCode.Err202, SectType.COORDINATES, _tok[0]);

            node.Position = new EnPoint(x, y);
        }

        private void ParseLabel() {
            if (_tok.Count < 3)
                throw new InputException(ErrorCode.Err201, SectType.LABELS, _line);
            
            double x, y;

            if (!_tok[0].ToDouble(out x) || !_tok[1].ToDouble(out y))
                throw new InputException(ErrorCode.Err201, SectType.LABELS, _line);

            string text = _tok[2].Replace("\"", "");
            
            Label label = new Label(text) {
                Position = new EnPoint(x, y)
            };

            net.Labels.Add(label);
        }

        private void ParseVertice() {
            if (_tok.Count < 3)
                throw new InputException(ErrorCode.Err201, SectType.VERTICES, _line);

            Link link = net.GetLink(_tok[0]);

            if (link == null)
                throw new InputException(ErrorCode.Err204, SectType.VERTICES, _tok[0]);

            double x, y;

            if (!_tok[1].ToDouble(out x) || !_tok[2].ToDouble(out y))
                throw new InputException(ErrorCode.Err202, SectType.VERTICES, _tok[0]);

            link.Vertices.Add(new EnPoint(x, y));
        }

        private void ParseControl() {
            int n = _tok.Count;
            StatType status = StatType.ACTIVE;

            double setting = Constants.MISSING, time = 0.0, level = 0.0;

            if (n < 6)
                throw new InputException(ErrorCode.Err201, SectType.CONTROLS, _line);

            Node node = null;
            Link link = net.GetLink(_tok[1]);

            if (link == null)
                throw new InputException(ErrorCode.Err204, SectType.CONTROLS, _line);

            LinkType ltype = link.Type;

            if (ltype == LinkType.CV)
                throw new InputException(ErrorCode.Err207, SectType.CONTROLS, _line);

            if (_tok[2].Match(StatType.OPEN.ToString())) {
                status = StatType.OPEN;
                if (ltype == LinkType.PUMP) setting = 1.0;
                if (ltype == LinkType.GPV) setting = link.Kc;
            }
            else if (_tok[2].Match(StatType.CLOSED.ToString())) {
                status = StatType.CLOSED;
                if (ltype == LinkType.PUMP) setting = 0.0;
                if (ltype == LinkType.GPV) setting = link.Kc;
            }
            else if (ltype == LinkType.GPV) {
                throw new InputException(ErrorCode.Err206, SectType.CONTROLS, _line);
            }
            else if (!_tok[2].ToDouble(out setting)) {
                throw new InputException(ErrorCode.Err202, SectType.CONTROLS, _line);
            }

            if (ltype == LinkType.PUMP || ltype == LinkType.PIPE) {
                if (!setting.IsMissing()) {
                    if (setting < 0.0)
                        throw new InputException(ErrorCode.Err202, SectType.CONTROLS, _line);

                    status = setting == 0.0 ? StatType.CLOSED : StatType.OPEN;
                }
            }

            ControlType ctype;

            if (_tok[4].Match(Keywords.w_TIME))
                ctype = ControlType.TIMER;
            else if (_tok[4].Match(Keywords.w_CLOCKTIME))
                ctype = ControlType.TIMEOFDAY;
            else {
                if (n < 8)
                    throw new InputException(ErrorCode.Err201, SectType.CONTROLS, _line);

                if ((node = net.GetNode(_tok[5])) == null)
                    throw new InputException(ErrorCode.Err203, SectType.CONTROLS, _line);

                if (_tok[6].Match(Keywords.w_BELOW)) ctype = ControlType.LOWLEVEL;
                else if (_tok[6].Match(Keywords.w_ABOVE)) ctype = ControlType.HILEVEL;
                else
                    throw new InputException(ErrorCode.Err201, SectType.CONTROLS, _line);
            }

            switch (ctype) {
            case ControlType.TIMER:
            case ControlType.TIMEOFDAY:
                if (n == 6) time = Utilities.GetHour(_tok[5]);
                if (n == 7) time = Utilities.GetHour(_tok[5], _tok[6]);
                if(time < 0.0) throw new InputException(ErrorCode.Err201, SectType.CONTROLS, _line);
                break;

            case ControlType.LOWLEVEL:
            case ControlType.HILEVEL:
                if (!_tok[7].ToDouble(out level))
                    throw new InputException(ErrorCode.Err202, SectType.CONTROLS, _line);

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

        private void ParseSource() {
            int n = _tok.Count;

            SourceType type; /* Source type   */
            double c0; /* Init. quality */
            Pattern pat = null;
            

            if (n < 2) 
                throw new InputException(ErrorCode.Err201, SectType.SOURCES, _line);

            Node node = net.GetNode(_tok[0]);

            if (node == null)
                throw new InputException(ErrorCode.Err203, SectType.SOURCES, _tok[0]);

            /* NOTE: Under old format, SourceType not supplied so let  */
            /*       i = index of token that contains quality value.   */
            int i = 2; /* Token with quality value */

            if (!EnumsTxt.TryParse(_tok[1], out type)) {
                i = 1;
                type = SourceType.CONCEN;
            }

            if (!_tok[i].ToDouble(out c0)) {
                /* Illegal WQ value */
                throw new InputException(ErrorCode.Err202, SectType.SOURCES, _tok[0]);
            }

            if (n > i + 1 && !string.IsNullOrEmpty(_tok[i + 1]) && _tok[i + 1] != "*") {
                if((pat = net.GetPattern(_tok[i + 1])) == null)
                    throw new InputException(ErrorCode.Err205, SectType.SOURCES, _tok[0]);
            }

            node.QualSource = new QualSource(type, c0, pat);
        }

        private void ParseEmitter() {
            int n = _tok.Count;
            Node node;
            double k;

            if (n < 2) throw
                new InputException(ErrorCode.Err201, SectType.EMITTERS, _line);
            
            if ((node = net.GetNode(_tok[0])) == null)
                throw new InputException(ErrorCode.Err203, SectType.EMITTERS, _tok[0]);

            if (node.Type != NodeType.JUNC)
                throw new InputException(ErrorCode.Err209, SectType.EMITTERS, _tok[0]);

            if (!_tok[1].ToDouble(out k) || k < 0.0)
                throw new InputException(ErrorCode.Err202, SectType.EMITTERS, _tok[0]);

            node.Ke = k;

        }

        private void ParseQuality() {
            int n = _tok.Count;
            double c0;

            if (n < 2) return;

            /* Single node entered  */
            if (n == 2) {
                Node node = net.GetNode(_tok[0]);

                if (node == null) return;

                if (!_tok[1].ToDouble(out c0))
                    throw new InputException(ErrorCode.Err209, SectType.QUALITY, _tok[0]);

                node.C0 = c0;
            }
            else {

                /* Node range entered    */
                if (!_tok[2].ToDouble(out c0))
                    throw new InputException(ErrorCode.Err209, SectType.QUALITY, _tok[0]);

                /* If numerical range supplied, then use numerical comparison */
                long i0, i1;

                if ((i0 = Atol(_tok[0])) > 0 && (i1 = Atol(_tok[1])) > 0) {
                    foreach (Node node  in  net.Nodes) {
                        long i = Atol(node.Name);
                        if (i >= i0 && i <= i1)
                            node.C0 = c0;
                    }
                }
                else {
                    foreach (Node node  in  net.Nodes) {
                        if ((string.Compare(_tok[0], node.Name, StringComparison.Ordinal) <= 0) &&
                            (string.Compare(_tok[1], node.Name, StringComparison.Ordinal) >= 0))
                            node.C0 = c0;
                    }
                }
            }
        }

        private void ParseReact() {
            int item, n = _tok.Count;
            double y;

            if (n < 3) return;


            if(_tok[0].Match(Keywords.w_ORDER)) { /* Reaction order */

                if (!_tok[n - 1].ToDouble(out y))
                    throw new InputException(ErrorCode.Err213, SectType.REACTIONS, _line);

                if (_tok[1].Match(Keywords.w_BULK)) net.BulkOrder = y;
                else if (_tok[1].Match(Keywords.w_TANK)) net.TankOrder = y;
                else if (_tok[1].Match(Keywords.w_WALL)) {
                    if (y == 0.0) net.WallOrder = 0.0;
                    else if (y == 1.0) net.WallOrder = 1.0;
                    else throw new InputException(ErrorCode.Err213, SectType.REACTIONS, _line);
                }
                else throw new InputException(ErrorCode.Err213, SectType.REACTIONS, _line);

                return;
            }

            if(_tok[0].Match(Keywords.w_ROUGHNESS)) { /* Roughness factor */
                if (!_tok[n - 1].ToDouble(out y))
                    throw new InputException(ErrorCode.Err213, SectType.REACTIONS, _line);

                net.RFactor = y;
                return;
            }

            if(_tok[0].Match(Keywords.w_LIMITING)) { /* Limiting potential */
                if (!_tok[n - 1].ToDouble(out y))
                    throw new InputException(ErrorCode.Err213, SectType.REACTIONS, _line);

                net.CLimit = y;
                return;
            }

            if(_tok[0].Match(Keywords.w_GLOBAL)) { /* Global rates */
                if (!_tok[n - 1].ToDouble(out y))
                    throw new InputException(ErrorCode.Err213, SectType.REACTIONS, _line);

                if (_tok[1].Match(Keywords.w_BULK)) net.KBulk = y;
                else if (_tok[1].Match(Keywords.w_WALL)) net.KWall = y;
                else throw new InputException(ErrorCode.Err201, SectType.REACTIONS, _line);

                return;
            }

            /* Individual rates */
            if (_tok[0].Match(Keywords.w_BULK)) item = 1;
            else if (_tok[0].Match(Keywords.w_WALL)) item = 2;
            else if (_tok[0].Match(Keywords.w_TANK)) item = 3;
            else throw new InputException(ErrorCode.Err201, SectType.REACTIONS, _line);

            _tok[0] = _tok[1];

            if(item == 3) { /* Tank rates */
                if (!_tok[n - 1].ToDouble(out y))
                    throw new InputException(ErrorCode.Err209, SectType.REACTIONS, _tok[1]);

                if (n == 3) {
                    Node node;
                    if ((node = net.GetNode(_tok[1])) == null)
                        throw new InputException(ErrorCode.Err208, SectType.REACTIONS, _tok[1]); 

                    Tank tank = node as Tank;
                    
                    if (tank == null) return;
                    tank.Kb = y; 
                }
                else {
                    long i1, i2;

                    /* If numerical range supplied, then use numerical comparison */
                    if ((i1 = Atol(_tok[1])) > 0 && (i2 = Atol(_tok[2])) > 0) {
                        foreach (Tank tank  in  net.Tanks) {
                            long i = Atol(tank.Name);
                            if (i >= i1 && i <= i2)
                                tank.Kb = y;
                        }
                    }
                    else {
                        foreach (Tank tank  in  net.Tanks) {
                            if (string.Compare(_tok[1], tank.Name, StringComparison.Ordinal) <= 0 &&
                                string.Compare(_tok[2], tank.Name, StringComparison.Ordinal) >= 0)
                                tank.Kb = y;
                        }
                    }
                }
            }
            else { /* Link rates */
                if (!_tok[n - 1].ToDouble(out y))
                    throw new InputException(ErrorCode.Err211, SectType.REACTIONS, _tok[1]);

                if (net.Links.Count == 0) return;

                if(n == 3) { /* Single link */
                    Link link;
                    if ((link = net.GetLink(_tok[1])) == null) return;
                    if (item == 1) link.Kb = y;
                    else           link.Kw = y;
                }
                else { /* Range of links */
                    long i1, i2;

                    /* If numerical range supplied, then use numerical comparison */
                    if ((i1 = Atol(_tok[1])) > 0 && (i2 = Atol(_tok[2])) > 0) {
                        foreach (Link link  in  net.Links) {
                            long i = Atol(link.Name);
                            if (i >= i1 && i <= i2) {
                                if (item == 1) link.Kb = y;
                                else link.Kw = y;
                            }
                        }
                    }
                    else
                        foreach (Link link  in  net.Links) {
                            if (string.Compare(_tok[1], link.Name, StringComparison.Ordinal) <= 0 &&
                                string.Compare(_tok[2], link.Name, StringComparison.Ordinal) >= 0) {
                                if (item == 1) link.Kb = y;
                                else           link.Kw = y;
                            }
                        }
                }
            }
        }

        private void ParseMixing() {
            int n = _tok.Count;
            MixType type;

            if (net.Nodes.Count == 0)
                throw new InputException(ErrorCode.Err208, SectType.MIXING, _tok[0]);

            if (n < 2) return;

            Node node = net.GetNode(_tok[0]);

            if(node == null)
                throw new InputException(ErrorCode.Err208, SectType.MIXING, _tok[0]);

            if (node.Type != NodeType.JUNC) return;
            
            Tank tank = (Tank)node;

            if (!EnumsTxt.TryParse(_tok[1], out type))
                throw new InputException(ErrorCode.Err201, SectType.MIXING, _line);

            var v = 1.0;
            if (type == MixType.MIX2 && n == 3) {
                if (!_tok[2].ToDouble(out v)) {
                    throw new InputException(ErrorCode.Err209, SectType.MIXING, _tok[0]);
                }
            }

            if (v == 0.0)
                v = 1.0;

            if (tank.Type == NodeType.RESERV) return;

            tank.MixModel = type;
            tank.V1Max = v;
        }

        private void ParseStatus() {
            int n = _tok.Count - 1;
            double y = 0.0;
            StatType status = StatType.ACTIVE;

            if (net.Links.Count == 0)
                throw new InputException(ErrorCode.Err210, SectType.STATUS, _tok[0]);

            if(n < 1)
                throw new InputException(ErrorCode.Err201, SectType.STATUS, _line);

            if (_tok[n].Match(Keywords.w_OPEN)) status = StatType.OPEN;
            else if (_tok[n].Match(Keywords.w_CLOSED)) status = StatType.CLOSED;
            else if (!_tok[n].ToDouble(out y)) 
                throw new InputException(ErrorCode.Err211, SectType.STATUS, _tok[0]);

            if (y < 0.0)
                throw new InputException(ErrorCode.Err211, SectType.STATUS, _tok[0]);

            if (n == 1) {
                Link link;
                if ((link = net.GetLink(_tok[0])) == null) return;

                if (link.Type == LinkType.CV)
                    throw new InputException(ErrorCode.Err211, SectType.STATUS, _tok[0]);

                if (link.Type == LinkType.GPV && status == StatType.ACTIVE)
                    throw new InputException(ErrorCode.Err211, SectType.STATUS, _tok[0]);

                ChangeStatus(link, status, y);
            }
            else {
                long i0, i1;

                /* If numerical range supplied, then use numerical comparison */
                if ((i0 = Atol(_tok[0])) > 0 && (i1 = Atol(_tok[1])) > 0) {
                    foreach (Link link  in  net.Links) {
                        long i = Atol(link.Name);
                        if (i >= i0 && i <= i1) 
                            ChangeStatus(link, status, y);
                        
                        
                    }
                }
                else
                    foreach (Link j  in  net.Links)
                        if (string.Compare(_tok[0], j.Name, StringComparison.Ordinal) <= 0 &&
                            string.Compare(_tok[1], j.Name, StringComparison.Ordinal) >= 0)
                            ChangeStatus(j, status, y);
            }
        }


        private void ParseEnergy() {
            int n = _tok.Count;
            double y;

            if(n < 3)
                throw new InputException(ErrorCode.Err201, SectType.ENERGY, _line);

            if (_tok[0].Match(Keywords.w_DMNDCHARGE)) {
                if (!_tok[2].ToDouble(out y))
                    throw new InputException(ErrorCode.Err213, SectType.ENERGY, _line);

                net.DCost = y;
                return;
            }

            Pump pump;

            if (_tok[0].Match(Keywords.w_GLOBAL)) {
                pump = null;
            }
            else if (_tok[0].Match(Keywords.w_PUMP)) {
                if (n < 4)
                    throw new InputException(ErrorCode.Err201, SectType.ENERGY, _line);

                Link linkRef = net.GetLink(_tok[1]);
                
                if(linkRef == null || linkRef.Type != LinkType.PUMP)
                    throw new InputException(ErrorCode.Err216, SectType.ENERGY, _tok[0]);

                pump = (Pump)linkRef;
            }
            else
                throw new InputException(ErrorCode.Err201, SectType.ENERGY, _line);


            if (_tok[n - 2].Match(Keywords.w_PRICE)) {
                if (!_tok[n - 1].ToDouble(out y)) {
                    if (pump == null)
                        throw new InputException(ErrorCode.Err213, SectType.ENERGY, _line);
                    else
                        throw new InputException(ErrorCode.Err217, SectType.ENERGY, _tok[0]);
                }

                if (pump == null)
                    net.ECost = y;
                else
                    pump.ECost = y;

                return;
            }

            if (_tok[n - 2].Match(Keywords.w_PATTERN)) {
                Pattern t = net.GetPattern(_tok[n - 1]);
                if (t == null) {
                    throw pump == null
                        ? new InputException(ErrorCode.Err213, SectType.ENERGY, _line)
                        : new InputException(ErrorCode.Err217, SectType.ENERGY, _tok[0]);
                }

                if (pump == null)
                    net.EPatId = t.Name;
                else
                    pump.EPat = t;

                return;
            }

            if (_tok[n - 2].Match(Keywords.w_EFFIC)) {
                if (pump == null) {
                    if(!_tok[n - 1].ToDouble(out y) || y <= 0.0)
                        throw new InputException(ErrorCode.Err213, SectType.ENERGY, _line);

                    net.EPump = y;
                }
                else {
                    Curve t = net.GetCurve(_tok[n - 1]);
                    if(t == null)
                        throw new InputException(ErrorCode.Err217, SectType.ENERGY, _tok[0]);

                    pump.ECurve = t;
                }
                return;
            }

            throw new InputException(ErrorCode.Err201, SectType.ENERGY, _line);
        }

        private void ParseReport() {
            int n = _tok.Count - 1;
            //FieldType i;
            double y;

            if (n < 1) 
                throw new InputException(ErrorCode.Err201, SectType.REPORT, _line);

            if (_tok[0].Match(Keywords.w_PAGE)) {
                if(!_tok[n].ToDouble(out y))
                    throw new InputException(ErrorCode.Err213, SectType.REPORT, _line);

                if(y < 0.0 || y > 255.0)
                    throw new InputException(ErrorCode.Err213, SectType.REPORT, _line);

                net.PageSize = (int)y;
                return;
            }


            if (_tok[0].Match(Keywords.w_STATUS)) {
                StatFlag flag;
                if (EnumsTxt.TryParse(_tok[n], out flag)) {
                    net.StatFlag = flag;
                }

                return;
            }

            if (_tok[0].Match(Keywords.w_SUMMARY)) {
                if (_tok[n].Match(Keywords.w_NO)) net.SummaryFlag = false;
                if (_tok[n].Match(Keywords.w_YES)) net.SummaryFlag = true;
                return;
            }

            if (_tok[0].Match(Keywords.w_MESSAGES)) {
                if (_tok[n].Match(Keywords.w_NO)) net.MessageFlag = false;
                if (_tok[n].Match(Keywords.w_YES)) net.MessageFlag = true;
                return;
            }

            if (_tok[0].Match(Keywords.w_ENERGY)) {
                if (_tok[n].Match(Keywords.w_NO)) net.EnergyFlag = false;
                if (_tok[n].Match(Keywords.w_YES)) net.EnergyFlag = true;
                return;
            }

            if (_tok[0].Match(Keywords.w_NODE)) {

                if (_tok[n].Match(Keywords.w_NONE)) {
                    net.NodeFlag = ReportFlag.FALSE;
                }
                else if (_tok[n].Match(Keywords.w_ALL)) {
                    net.NodeFlag = ReportFlag.TRUE;
                }
                else {

                    if (net.Nodes.Count == 0)
                        throw new InputException(ErrorCode.Err208, SectType.REPORT, _tok[1]);

                    for (int i = 1; i <= n; i++) {
                        Node node = net.GetNode(_tok[i]);
                       
                        if (node == null)
                            throw new InputException(ErrorCode.Err208, SectType.REPORT, _tok[i]);

                        node.RptFlag = true;
                    }

                    net.NodeFlag = ReportFlag.SOME;
                }
                return;
            }

            if (_tok[0].Match(Keywords.w_LINK)) {
                if (_tok[n].Match(Keywords.w_NONE))
                    net.LinkFlag = ReportFlag.FALSE;
                else if (_tok[n].Match(Keywords.w_ALL))
                    net.LinkFlag = ReportFlag.TRUE;
                else {
                   
                    if (net.Links.Count == 0)
                        throw new InputException(ErrorCode.Err210, SectType.REPORT, _tok[1]);
                    
                    for (int i = 1; i <= n; i++) {
                        Link link = net.GetLink(_tok[i]);
                        
                        if (link == null)
                            throw new InputException(ErrorCode.Err210, SectType.REPORT, _tok[i]);
                        
                        link.RptFlag = true;
                    }

                    net.LinkFlag = ReportFlag.SOME;
                }
                return;
            }

            FieldType iFieldId;
            FieldsMap fMap = net.FieldsMap;

            if (EnumsTxt.TryParse(_tok[0], out iFieldId)) {
                if (iFieldId > FieldType.FRICTION)
                    throw new InputException(ErrorCode.Err201, SectType.REPORT, _line);

                if (_tok.Count == 1 || _tok[1].Match(Keywords.w_YES)) {
                    fMap.GetField(iFieldId).Enabled = true;
                    return;
                }

                if (_tok[1].Match(Keywords.w_NO)) {
                    fMap.GetField(iFieldId).Enabled = false;
                    return;
                }

                RangeType rj;

                if (_tok.Count < 3)
                    throw new InputException(ErrorCode.Err201, SectType.REPORT, _line);

                if (!EnumsTxt.TryParse(_tok[1], out rj))
                    throw new InputException(ErrorCode.Err201, SectType.REPORT, _line);

                if (!_tok[2].ToDouble(out y))
                    throw new InputException(ErrorCode.Err201, SectType.REPORT, _line);

                if (rj == RangeType.PREC) {
                    fMap.GetField(iFieldId).Enabled = true;
                    fMap.GetField(iFieldId).Precision = (int)Math.Round(y); //roundOff(y));
                }
                else
                    fMap.GetField(iFieldId).SetRptLim(rj, y);

                return;
            }

            if (_tok[0].Match(Keywords.w_FILE)) {
                net.AltReport = _tok[1];
                return;
            }
          
            throw new InputException(ErrorCode.Err201, SectType.REPORT, _line);

        }

        private void ParseOption() {
            int n = _tok.Count - 1;
            bool notHandled = OptionChoice(n);
            if (notHandled)
                notHandled = OptionValue(n);
            if (notHandled) {
                net.ExtraOptions[_tok[0]] = _tok[1];
            }
        }

        ///<summary>Handles options that are choice values, such as quality type, for example.</summary>
        /// <param name="n">number of tokens</param>
        /// <returns><c>true</c> is it didn't handle the option.</returns>
        private bool OptionChoice(int n) {
            
            if (n < 0)
                throw new InputException(ErrorCode.Err201, SectType.OPTIONS, _line);

            if (_tok[0].Match(Keywords.w_UNITS)) {
                FlowUnitsType type;

                if (n < 1)
                    return false;

                if (!EnumsTxt.TryParse(_tok[1], out type))
                    throw new InputException(ErrorCode.Err201, SectType.OPTIONS, _line);

                net.FlowFlag = type;
            }
            else if (_tok[0].Match(Keywords.w_PRESSURE)) {
                if (n < 1) return false;
                PressUnitsType value;

                if (!EnumsTxt.TryParse(_tok[1], out value)) {
                    throw new InputException(ErrorCode.Err201, SectType.OPTIONS, _line);
                }

                net.PressFlag = value;
            }
            else if (_tok[0].Match(Keywords.w_HEADLOSS)) {
                if (n < 1) return false;
                FormType value;

                if (!EnumsTxt.TryParse(_tok[1], out value))
                    throw new InputException(ErrorCode.Err201, SectType.OPTIONS, _line);

                net.FormFlag = value;
            }
            else if (_tok[0].Match(Keywords.w_HYDRAULIC)) {
                if (n < 2) return false;
                HydType value;

                if (!EnumsTxt.TryParse(_tok[1], out value))
                    throw new InputException(ErrorCode.Err201, SectType.OPTIONS, _line);

                net.HydFlag = value;
                net.HydFname = _tok[2];
            }
            else if (_tok[0].Match(Keywords.w_QUALITY)) {
                QualType type;

                if (n < 1)
                    return false;

                net.QualFlag = EnumsTxt.TryParse(_tok[1], out type) 
                    ? type
                    : QualType.CHEM;

                switch (net.QualFlag) {
                case QualType.CHEM:
                    net.ChemName = _tok[1];
                    if(n >= 2)
                        net.ChemUnits = _tok[2];
                    break;

                case QualType.TRACE:

                    _tok[0] = "";
                    if (n < 2)
                        throw new InputException(ErrorCode.Err212, SectType.OPTIONS, _line);

                    _tok[0] = _tok[2];
                    Node node = net.GetNode(_tok[2]);

                    if (node == null)
                        throw new InputException(ErrorCode.Err212, SectType.OPTIONS, _line);

                    net.TraceNode = node.Name;
                    net.ChemName = Keywords.u_PERCENT;
                    net.ChemUnits = _tok[2];
                    break;

                case QualType.AGE:
                    net.ChemName = Keywords.w_AGE;
                    net.ChemUnits = Keywords.u_HOURS;
                    break;
                }

            }
            else if (_tok[0].Match(Keywords.w_MAP)) {
                if (n < 1)
                    return false;

                net.MapFname = _tok[1];
            }
            else if (_tok[0].Match(Keywords.w_UNBALANCED)) {
                if (n < 1)
                    return false;

                if (_tok[1].Match(Keywords.w_STOP)) {
                    net.ExtraIter = -1;
                }
                else if (_tok[1].Match(Keywords.w_CONTINUE)) {
                    if (n >= 2) {
                        double d;

                        if (!_tok[2].ToDouble(out d))
                            throw new InputException(ErrorCode.Err201, SectType.OPTIONS, _line);

                        net.ExtraIter = (int)d;
                    }
                    else
                        net.ExtraIter = 0;
                }
                else {
                    throw new InputException(ErrorCode.Err201, SectType.OPTIONS, _line);
                }
            }
            else if (_tok[0].Match(Keywords.w_PATTERN)) {
                if (n < 1)
                    return false;

                net.DefPatId = _tok[1];
            }
            else
                return true;
            return false;
        }

        private bool OptionValue(int n) {
            string name = _tok[0];

            /* Check for obsolete SEGMENTS keyword */
            if(name.Match(Keywords.w_SEGMENTS))
                return false;

            /* Check for missing value (which is permissible) */
            int nvalue = name.Match(Keywords.w_SPECGRAV) ||
                         name.Match(Keywords.w_EMITTER) ||
                         name.Match(Keywords.w_DEMAND)
                ? 2
                : 1;

            string keyword = null;
            foreach (string k  in  optionValueKeywords) {
                if (name.Match(k)) {
                    keyword = k;
                    break;
                }
            }

            if (keyword == null) return true;
            name = keyword;

            double y;

            if (!_tok[nvalue].ToDouble(out y))
                throw new InputException(ErrorCode.Err213, SectType.OPTIONS, _line);

            if (name.Match(Keywords.w_TOLERANCE)) {
                if (y < 0.0)
                    throw new InputException(ErrorCode.Err213, SectType.OPTIONS, _line);

                net.Ctol = y;
                return false;
            }

            if (name.Match(Keywords.w_DIFFUSIVITY)) {
                if (y < 0.0)
                    throw new InputException(ErrorCode.Err213, SectType.OPTIONS, _line);

                net.Diffus = y;
                return false;
            }

            if (name.Match(Keywords.w_DAMPLIMIT)) {
                net.DampLimit = y;
                return false;
            }

            if(y <= 0.0)
                throw new InputException(ErrorCode.Err213, SectType.OPTIONS, _line);

            if (name.Match(Keywords.w_VISCOSITY)) net.Viscos = y;
            else if (name.Match(Keywords.w_SPECGRAV)) net.SpGrav = y;
            else if (name.Match(Keywords.w_TRIALS)) net.MaxIter = (int)y;
            else if (name.Match(Keywords.w_ACCURACY)) {
                y = Math.Max(y, 1e-5);
                y = Math.Min(y, 1e-1);
                net.HAcc = y;
            }
            else if (name.Match(Keywords.w_HTOL)) net.HTol = y;
            else if (name.Match(Keywords.w_QTOL)) net.QTol = y;
            else if (name.Match(Keywords.w_RQTOL)) {

                if(y >= 1.0)
                    throw new InputException(ErrorCode.Err213, SectType.OPTIONS, _line);

                net.RQtol = y;
            }
            else if (name.Match(Keywords.w_CHECKFREQ)) net.CheckFreq = (int)y;
            else if (name.Match(Keywords.w_MAXCHECK)) net.MaxCheck = (int)y;
            else if (name.Match(Keywords.w_EMITTER)) net.QExp = 1.0 / y;
            else if (name.Match(Keywords.w_DEMAND)) net.DMult = y;

            return false;
        }

        private void ParseTime() {
            int n = _tok.Count - 1;
            double y;
            
            if (n < 1)
                throw new InputException(ErrorCode.Err201, SectType.TIMES, _line);

            if (_tok[0].Match(Keywords.w_STATISTIC)) {
                TStatType type;

                if (!EnumsTxt.TryParse(_tok[n], out type))
                    throw new InputException(ErrorCode.Err201, SectType.TIMES, _line);

                net.TstatFlag = type;
    
                return;
            }

            if (!_tok[n].ToDouble(out y)) {
                if ((y = Utilities.GetHour(_tok[n])) < 0.0) {
                    if ((y = Utilities.GetHour(_tok[n - 1], _tok[n])) < 0.0)
                        throw new InputException(ErrorCode.Err213, SectType.TIMES, _line);
                }
            }

            var t = (long)(3600.0 * y);

            if (_tok[0].Match(Keywords.w_DURATION))
                net.Duration = t;
            else if (_tok[0].Match(Keywords.w_HYDRAULIC))
                net.HStep = t;
            else if (_tok[0].Match(Keywords.w_QUALITY))
                net.QStep = t;
            else if (_tok[0].Match(Keywords.w_RULE))
                net.RuleStep = t;
            else if (_tok[0].Match(Keywords.w_MINIMUM)) {
                
            }
            else if (_tok[0].Match(Keywords.w_PATTERN)) {
                if (_tok[1].Match(Keywords.w_TIME))
                    net.PStep = t;
                else if (_tok[1].Match(Keywords.w_START))
                    net.PStart = t;
                else
                    throw new InputException(ErrorCode.Err201, SectType.TIMES, _line);
            }
            else if (_tok[0].Match(Keywords.w_REPORT)) {
                if (_tok[1].Match(Keywords.w_TIME))
                    net.RStep = t;
                else if (_tok[1].Match(Keywords.w_START))
                    net.RStart = t;
                else
                    throw new InputException(ErrorCode.Err201, SectType.TIMES, _line);
            }
            else if (_tok[0].Match(Keywords.w_START))
                net.Tstart = t % Constants.SECperDAY;

            else
                throw new InputException(ErrorCode.Err201, SectType.TIMES, _line);

        }

        private static SectType FindSectionType(string line) {
            if (string.IsNullOrEmpty(line)) return (SectType)(-1);

            for (var type = SectType.TITLE; type <= SectType.END; type++) {
                string sectName = '[' + type.ToString() + ']';

                // if(line.Contains(type.parseStr())) return type;
                // if (line.IndexOf(type.parseStr(), StringComparison.OrdinalIgnoreCase) >= 0) {
                if (line.StartsWith(sectName, StringComparison.OrdinalIgnoreCase)) {
                    return type;
                }
            }

            return (SectType)(-1);
        }

        private void ParseDemand() {
            int n = _tok.Count;
            double y;
            Pattern pat = null;

            if (n < 2)
                throw new InputException(ErrorCode.Err201, SectType.DEMANDS, _line);

            if (!_tok[1].ToDouble(out y))
                throw new InputException(ErrorCode.Err202, SectType.DEMANDS, _tok[0]);

            if (_tok[0].Match(Keywords.w_MULTIPLY)) {
                if (y <= 0.0)
                    throw new InputException(ErrorCode.Err202, SectType.DEMANDS, _tok[0]);

                net.DMult = y;
                return;
            }

            Node node  = net.GetNode(_tok[0]);

            if(node == null || node.Type != NodeType.JUNC)
                throw new InputException(ErrorCode.Err208, SectType.DEMANDS, _tok[0]);

            if (n >= 3) {
                pat = net.GetPattern(_tok[2]);
                if (pat == null)
                    throw new InputException(ErrorCode.Err205, SectType.DEMANDS, _line);
            }

            node.Demands.Add(new Demand(y, pat));

        }

        private void ParseRule() {
            Rulewords key;
            EnumsTxt.TryParse(_tok[0], out key);
            if (key == Rulewords.RULE) {
                _currentRule = new Rule(_tok[1]);
                net.Rules.Add(_currentRule);
            }
            else if (_currentRule != null) {
                _currentRule.Code.Add(_line);
            }

        }

    }

}