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
        private SectType _sect = (SectType)(-1);
        private readonly List<string> _tok = new List<string>(Constants.MAXTOKS);
        private string _comment;
        private string _line;

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

        public override Network Parse(Network nw, string fileName) {

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            net = nw ?? new Network();

            try {
                using (FileStream fs = File.OpenRead(fileName)) {
                    //Parse demands and time patterns first
                    ParsePc(fs);
                    fs.Position = 0;
                    Parse(fs);

                    if (errors.Count > 0)
                        throw new EnException(ErrorCode.Err200);
                    
                }
            }
            catch (IOException ex) {
                throw new EnException(ErrorCode.Err302, ex);
            }

            AdjustData(net);

            net.FieldsMap.Prepare(net.UnitsFlag, net.FlowFlag, net.PressFlag, net.QualFlag, net.ChemUnits, net.SpGrav, net.HStep);

            Convert();

            return net;
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
                        _sect = SectType.PATTERNS;
                    }
                    else if (_line.StartsWith("[CURVES]", StringComparison.OrdinalIgnoreCase))
                        _sect = SectType.CURVES;
                    else
                        _sect = (SectType)(-1);
                    
                    continue;
                }

                if (_sect != (SectType)(-1)) {
                    if (_line.IndexOf(';') >= 0)
                        _line = _line.Substring(0, _line.IndexOf(';'));

                    if (string.IsNullOrEmpty(_line))
                        continue;

                    if (GetTokens(_line, _tok) == 0)
                        continue;

                    try {
                        switch (_sect) {
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
            
            _sect = (SectType)(-1);
            TextReader reader = new StreamReader(stream, Encoding.Default); // "ISO-8859-1";
            
            while ((_line = reader.ReadLine()) != null) {

                if (_line.Length > Constants.MAXLINE)
                    LogException(new EnException(ErrorCode.Err214, _sect, _line));

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

                if (GetTokens(_line, _tok) == 0)
                    continue;

                if (_tok[0][0] == '[') {
                    _sect = FindSectionType(_tok[0]);

                    if (_sect < 0) {
                        log.Warning("Unknown section type: " + _tok[0]);
                    }

                    if (_sect == SectType.END)
                        break;

                    continue;
                }

                if (_sect < 0) continue;

                try {
                    NewLine();
                }
                catch (InputException ex) {
                    LogException(ex);
                }
            }

        }


        private void NewLine() {
            switch (_sect) {
                case SectType.TITLE:
                    ParseTitle();
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

                case SectType.PATTERNS:
                    //ParsePattern();
                    break;

                case SectType.CURVES:
                    //ParseCurve();
                    break;

                case SectType.DEMANDS:
                    ParseDemand();
                    break;

                case SectType.CONTROLS:
                    ParseControl();
                    break;

                case SectType.RULES:
                    ParseRule(); /* See RULES.C */
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

                case SectType.ROUGHNESS:
                    break;

                case SectType.ENERGY:
                    ParseEnergy();
                    break;

                case SectType.REACTIONS:
                    ParseReaction();
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

                /* Data in these sections are not used for any computations */
                case SectType.COORDINATES:
                    ParseCoordinate();
                    break;

                case SectType.LABELS:
                    ParseLabel();
                    break;

                case SectType.TAGS:
                    ParseTag();
                    break;

                case SectType.VERTICES:
                    ParseVertice();
                    break;

                case SectType.BACKDROP:
                    break;

                default:
                    throw new InputException(ErrorCode.Err201, _sect, _line);
            }
        }

        /// <summary>Mimics C atol function behavior</summary>
        private static bool Atol(string s, out int value) {
            if (string.IsNullOrEmpty(s)) {
                value = 0;
                return false;
            }
            
            int i = 0;

            foreach (char c in s) {
                if (c != '-' && c != '+' && !char.IsWhiteSpace(c)) break;
                i++;
            }

            while (i < s.Length) {

                if(!char.IsNumber(s[i]))   
                    break;

                i++;
            }

            return int.TryParse(s.Substring(0, i), out value);
        }

        private void ParseTitle() {
            if (net.Title.Count >= 3) return;

            string s = _line.Length > Constants.MAXMSG
                ? _line.Substring(0, Constants.MAXMSG)
                : _line;

            net.Title.Add(s);
        }

        private void ParseJunction() {
            int n = _tok.Count;
            double y = 0.0d;
            Pattern p = null;

            if (net.GetNode(_tok[0]) != null)
                throw new InputException(ErrorCode.Err215, SectType.JUNCTIONS, _tok[0]);

            Node node = new Junction(_tok[0]);

            net.Nodes.Add(node);

            if (n < 2)
                throw new InputException(ErrorCode.Err201, SectType.JUNCTIONS, _line);

            if (!_tok[1].ToDouble(out double el))
                throw new InputException(ErrorCode.Err202, SectType.JUNCTIONS, _tok[0]);

            if (n >= 3 && !_tok[2].ToDouble(out y))
                throw new InputException(ErrorCode.Err202, SectType.JUNCTIONS, _tok[0]);

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
                node.PrimaryDemand.Pattern = p;
            }
            
        }

        private void ParseTank() {
            int n = _tok.Count;
            Pattern p = null;
            Curve vcurve = null;
            double initlevel = 0.0d,
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

            if (!_tok[1].ToDouble(out double el))
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
                    diam < 0.0)
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
            tank.Kb = double.NaN;

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
            StatType status = StatType.OPEN;
            double lcoeff = 0.0d;

            if (net.GetLink(_tok[0]) != null)
                throw new InputException(ErrorCode.Err215, SectType.PIPES, _tok[0]);

            Pipe pipe = new Pipe(_tok[0]);
            net.Links.Add(pipe);
            
            if (_tok.Count < 6)
                throw new InputException(ErrorCode.Err201, SectType.PIPES, _line);


            Node j1 = net.GetNode(_tok[1]),
                 j2 = net.GetNode(_tok[2]);
            
            if (j1 == null || j2 == null)
                throw new InputException(ErrorCode.Err203, SectType.PIPES, _tok[0]);


            if (j1.Equals(j2))
                throw new InputException(ErrorCode.Err222, SectType.PIPES, _tok[0]);

            if (!_tok[3].ToDouble(out double length) || length <= 0.0 ||
                !_tok[4].ToDouble(out double diam) || diam <= 0.0 ||
                !_tok[5].ToDouble(out double rcoeff) || rcoeff <= 0.0)
                throw new InputException(ErrorCode.Err202, SectType.PIPES, _tok[0]);


            if (_tok.Count == 7) {
                if(_tok[6].Match(Keywords.w_CV)) pipe.HasCheckValve = true;
                else if(_tok[6].Match(Keywords.w_CLOSED)) status = StatType.CLOSED;
                else if(_tok[6].Match(Keywords.w_OPEN)) status = StatType.OPEN;
                else if (!_tok[6].ToDouble(out lcoeff) || lcoeff < 0)
                    throw new InputException(ErrorCode.Err202, SectType.PIPES, _tok[0]);
            }

            if (_tok.Count == 8) {
                if (!_tok[6].ToDouble(out lcoeff) || lcoeff < 0)
                    throw new InputException(ErrorCode.Err202, SectType.PIPES, _tok[0]);

                if(_tok[7].Match(Keywords.w_CV)) pipe.HasCheckValve = true;
                else if(_tok[7].Match(Keywords.w_CLOSED)) status = StatType.CLOSED;
                else if(_tok[7].Match(Keywords.w_OPEN)) status = StatType.OPEN;
                else
                    throw new InputException(ErrorCode.Err202, SectType.PIPES, _tok[0]); 
            }

            pipe.FirstNode = j1;
            pipe.SecondNode = j2;
            pipe.Lenght = length;
            pipe.Diameter = diam;
            pipe.Kc = rcoeff;
            pipe.Km = lcoeff;
            pipe.Kb = double.NaN;
            pipe.Kw = double.NaN;
            pipe.Status = status;
            pipe.RptFlag = false;

            if (!string.IsNullOrEmpty(_comment))
                pipe.Comment = _comment;
        }

        private void ParsePump() {
            int m;
            double[] x = new double[6];

            if (net.GetLink(_tok[0]) != null)
                throw new InputException(ErrorCode.Err215, SectType.PUMPS, _tok[0]);
            
            if (_tok.Count < 4)
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
                for (int j = 4; j < _tok.Count; j++) {
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
            while (m < _tok.Count) {
                if (_tok[m - 1].Match(Keywords.w_POWER)) { /* Const. HP curve       */
                    if (!_tok[m].ToDouble(out double y) || y <= 0.0)
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
                    if (!_tok[m].ToDouble(out double y) || y < 0.0)
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
            string key = _tok[0];

            double setting, lcoeff = 0.0;

            if (net.GetLink(key) != null)
                throw new InputException(ErrorCode.Err215, SectType.VALVES, _tok[0]);

            if (n < 6)
                throw new InputException(ErrorCode.Err201, SectType.VALVES, _line);

            Node j1 = net.GetNode(_tok[1]),
                 j2 = net.GetNode(_tok[2]);

            if (j1 == null || j2 == null)
                throw new InputException(ErrorCode.Err203, SectType.VALVES, _tok[0]);

            if (j1.Equals(j2))
                throw new InputException(ErrorCode.Err222, SectType.VALVES, _tok[0]);

            if (!_tok[4].TryParse(out ValveType type))
                throw new InputException(ErrorCode.Err201, SectType.VALVES, _line);

            if (!_tok[3].ToDouble(out double diam) || diam <= 0.0)
                throw new InputException(ErrorCode.Err202, SectType.VALVES, _tok[0]);

            Valve valve = new Valve(key, type);
            net.Links.Add(valve);

            if (type == ValveType.GPV) {
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

            if (j1.NodeType > NodeType.JUNC || j2.NodeType > NodeType.JUNC) {
                if (type == ValveType.PRV || type == ValveType.PSV || type == ValveType.FCV)
                    throw new InputException(ErrorCode.Err219, SectType.VALVES, _tok[0]);
            }

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
                if (!_tok[i].ToDouble(out double x))
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

            if (!_tok[1].ToDouble(out double x) || !_tok[2].ToDouble(out double y))
                throw new InputException(ErrorCode.Err202, SectType.CURVES, _tok[0]);

            cur.Add(x, y);
        }

        private void ParseCoordinate() {
            if (_tok.Count < 3)
                throw new InputException(ErrorCode.Err201, SectType.COORDINATES, _line);

            Node node = net.GetNode(_tok[0]);

            if (node == null)
                throw new InputException(ErrorCode.Err203, SectType.COORDINATES, _tok[0]);

            if (!_tok[1].ToDouble(out double x) || !_tok[2].ToDouble(out double y))
                throw new InputException(ErrorCode.Err202, SectType.COORDINATES, _tok[0]);

            node.Coordinate = new EnPoint(x, y);
        }

        private void ParseLabel() {
            if (_tok.Count < 3)
                throw new InputException(ErrorCode.Err201, SectType.LABELS, _line);

            if (!_tok[0].ToDouble(out double x) || !_tok[1].ToDouble(out double y))
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

            if (!_tok[1].ToDouble(out double x) || !_tok[2].ToDouble(out double y))
                throw new InputException(ErrorCode.Err202, SectType.VERTICES, _tok[0]);

            link.Vertices.Add(new EnPoint(x, y));
        }

        private void ParseControl() {
            StatType status = StatType.ACTIVE;

            double setting = double.NaN, level = 0.0;
            TimeSpan time = TimeSpan.Zero;

            if (_tok.Count < 6)
                throw new InputException(ErrorCode.Err201, SectType.CONTROLS, _line);

            Node node = null;
            Link link = net.GetLink(_tok[1]);

            if (link == null)
                throw new InputException(ErrorCode.Err204, SectType.CONTROLS, _line);

            LinkType ltype = link.LinkType;

            if (link.LinkType == LinkType.PIPE && ((Pipe)link).HasCheckValve)
                throw new InputException(ErrorCode.Err207, SectType.CONTROLS, _line);

            if (_tok[2].Match(StatType.OPEN.Keyword2())) {
                status = StatType.OPEN;
                switch (link.LinkType) {
                    case LinkType.PUMP:
                        setting = 1.0;
                        break;
                    case LinkType.VALVE:
                        if (((Valve)link).ValveType == ValveType.GPV)
                            setting = link.Kc;
                        break;
                }
            }
            else if (_tok[2].Match(StatType.CLOSED.Keyword2())) {
                status = StatType.CLOSED;
                switch (link.LinkType) {
                    case LinkType.PUMP:
                        setting = 0.0;
                        break;
                    case LinkType.VALVE:
                        if (((Valve)link).ValveType == ValveType.GPV)
                            setting = link.Kc;
                        break;
                }
            }
            else if (ltype == LinkType.VALVE && ((Valve)link).ValveType == ValveType.GPV) {
                throw new InputException(ErrorCode.Err206, SectType.CONTROLS, _line);
            }
            else if (!_tok[2].ToDouble(out setting)) {
                throw new InputException(ErrorCode.Err202, SectType.CONTROLS, _line);
            }

            if (ltype == LinkType.PUMP || ltype == LinkType.PIPE) {
                if (!double.IsNaN(setting)) {
                    if (setting < 0.0)
                        throw new InputException(ErrorCode.Err202, SectType.CONTROLS, _line);

                    status = setting.IsZero() ? StatType.CLOSED : StatType.OPEN;
                }
            }

            ControlType ctype;

            if (_tok[4].Match(Keywords.w_TIME))
                ctype = ControlType.TIMER;
            else if (_tok[4].Match(Keywords.w_CLOCKTIME))
                ctype = ControlType.TIMEOFDAY;
            else {
                if (_tok.Count < 8)
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
                time = _tok.Count > 6
                    ? Utilities.ToTimeSpan(_tok[5], _tok[6]) 
                    : Utilities.ToTimeSpan(_tok[5]);
                
                if(time == TimeSpan.MinValue) 
                    throw new InputException(ErrorCode.Err201, SectType.CONTROLS, _line);

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
                Time = ctype == ControlType.TIMEOFDAY ? time.TimeOfDay() : time,
                Grade = level
            };

            net.Controls.Add(cntr);
        }

        private void ParseSource() {
            int n = _tok.Count;

            Pattern pat = null;
            

            if (n < 2) 
                throw new InputException(ErrorCode.Err201, SectType.SOURCES, _line);

            Node node = net.GetNode(_tok[0]);

            if (node == null)
                throw new InputException(ErrorCode.Err203, SectType.SOURCES, _tok[0]);

            /* NOTE: Under old format, SourceType not supplied so let  */
            /*       i = index of token that contains quality value.   */
            int i = 2; /* Token with quality value */

            if (!_tok[1].TryParse(out SourceType type)) {
                i = 1;
                type = SourceType.CONCEN;
            }

            if (!_tok[i].ToDouble(out double c0)) {
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

            if (n < 2) throw
                new InputException(ErrorCode.Err201, SectType.EMITTERS, _line);
            
            if ((node = net.GetNode(_tok[0])) == null)
                throw new InputException(ErrorCode.Err203, SectType.EMITTERS, _tok[0]);

            if (node.NodeType != NodeType.JUNC)
                throw new InputException(ErrorCode.Err209, SectType.EMITTERS, _tok[0]);

            if (!_tok[1].ToDouble(out double k) || k < 0.0)
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
                if (Atol(_tok[0], out int i0)  && Atol(_tok[1], out int i1)) {
                    foreach (Node node  in  net.Nodes) {
                        if (Atol(node.Name, out int i) && i >= i0 && i <= i1)
                            node.C0 = c0;
                    }
                }
                else {
                    foreach (Node node  in  net.Nodes) {
                        if (string.Compare(_tok[0], node.Name, StringComparison.Ordinal) <= 0 &&
                            string.Compare(_tok[1], node.Name, StringComparison.Ordinal) >= 0)
                            node.C0 = c0;
                    }
                }
            }
        }

        private void ParseReaction() {
            int item, n = _tok.Count;
            double y;

            if (n < 3) return;


            if(_tok[0].Match(Keywords.w_ORDER)) { /* Reaction order */

                if (!_tok[n - 1].ToDouble(out y))
                    throw new InputException(ErrorCode.Err213, SectType.REACTIONS, _line);

                if (_tok[1].Match(Keywords.w_BULK)) net.BulkOrder = y;
                else if (_tok[1].Match(Keywords.w_TANK)) net.TankOrder = y;
                else if (_tok[1].Match(Keywords.w_WALL)) {
                    if (y.IsZero()) net.WallOrder = 0.0;
                    else if (y.EqualsTo(1.0)) net.WallOrder = 1.0;
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

            if(item == 3) {
                /* Tank rates */
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
                    /* If numerical range supplied, then use numerical comparison */
                    if (Atol(_tok[1], out int i1) && Atol(_tok[2], out int i2)) {
                        foreach (Tank tank  in  net.Tanks) {
                            if (Atol(tank.Name, out int i) && i >= i1 && i <= i2)
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
                else { 
                    /* Range of links */

                    /* If numerical range supplied, then use numerical comparison */
                    if (Atol(_tok[1], out int i1) && Atol(_tok[2], out int i2)) {
                        foreach (Link link  in  net.Links) {
                            if (Atol(link.Name, out int i) && i >= i1 && i <= i2) {
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

            if (net.Nodes.Count == 0)
                throw new InputException(ErrorCode.Err208, SectType.MIXING, _tok[0]);

            if (n < 2) return;

            Node node = net.GetNode(_tok[0]);

            if(node == null)
                throw new InputException(ErrorCode.Err208, SectType.MIXING, _tok[0]);

            if (node.NodeType != NodeType.JUNC) return;
            
            Tank tank = (Tank)node;

            if (!_tok[1].TryParse(out MixType type))
                throw new InputException(ErrorCode.Err201, SectType.MIXING, _line);

            var v = 1.0;
            if (type == MixType.MIX2 && n == 3) {
                if (!_tok[2].ToDouble(out v)) {
                    throw new InputException(ErrorCode.Err209, SectType.MIXING, _tok[0]);
                }
            }

            if (v.IsZero())
                v = 1.0;

            if (tank.NodeType == NodeType.RESERV) return;

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
            else if(!_tok[n].ToDouble(out y) || y < 0.0) 
                throw new InputException(ErrorCode.Err211, SectType.STATUS, _tok[0]);

            if (n == 1) {
                Link link = net.GetLink(_tok[0]);
                if (link == null) return;

                switch (link.LinkType) {
                    case LinkType.PIPE:
                        if (((Pipe)link).HasCheckValve)
                            throw new InputException(ErrorCode.Err211, SectType.STATUS, _tok[0]);

                        break;

                    case LinkType.VALVE:
                        if (((Valve)link).ValveType == ValveType.GPV && status == StatType.ACTIVE)
                            throw new InputException(ErrorCode.Err211, SectType.STATUS, _tok[0]);

                        break;
                }

                ChangeStatus(link, status, y);
            }
            else {
                /* If numerical range supplied, then use numerical comparison */
                if (Atol(_tok[0], out int i0) && Atol(_tok[1], out int i1)) {
                    foreach (Link link  in  net.Links) {
                        if (Atol(link.Name, out int i) && i >= i0 && i <= i1) 
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

                Link link = net.GetLink(_tok[1]);
                
                if(link == null || link.LinkType != LinkType.PUMP)
                    throw new InputException(ErrorCode.Err216, SectType.ENERGY, _tok[0]);

                pump = (Pump)link;
            }
            else
                throw new InputException(ErrorCode.Err201, SectType.ENERGY, _line);


            if (_tok[n - 2].Match(Keywords.w_PRICE)) {
                if (!_tok[n - 1].ToDouble(out y)) {
                    throw pump == null
                        ? new InputException(ErrorCode.Err213, SectType.ENERGY, _line)
                        : new InputException(ErrorCode.Err217, SectType.ENERGY, _tok[0]);
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
                if (_tok[n].TryParse(out StatFlag flag)) {
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

            FieldsMap fMap = net.FieldsMap;

            if (_tok[0].TryParse(out FieldType iFieldId)) {
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

                if (_tok.Count < 3)
                    throw new InputException(ErrorCode.Err201, SectType.REPORT, _line);

                if (!_tok[1].TryParse(out RangeType rj))
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
                if (n < 1)
                    return false;

                if (!_tok[1].TryParse(out FlowUnitsType type))
                    throw new InputException(ErrorCode.Err201, SectType.OPTIONS, _line);

                net.FlowFlag = type;
            }
            else if (_tok[0].Match(Keywords.w_PRESSURE)) {
                if (n < 1) return false;

                if (!_tok[1].TryParse(out PressUnitsType value)) {
                    throw new InputException(ErrorCode.Err201, SectType.OPTIONS, _line);
                }

                net.PressFlag = value;
            }
            else if (_tok[0].Match(Keywords.w_HEADLOSS)) {
                if (n < 1) return false;

                if (!_tok[1].TryParse(out FormType value))
                    throw new InputException(ErrorCode.Err201, SectType.OPTIONS, _line);

                net.FormFlag = value;
            }
            else if (_tok[0].Match(Keywords.w_HYDRAULIC)) {
                if (n < 2) return false;

                if (!_tok[1].TryParse(out HydType value))
                    throw new InputException(ErrorCode.Err201, SectType.OPTIONS, _line);

                net.HydFlag = value;
                net.HydFname = _tok[2];
            }
            else if (_tok[0].Match(Keywords.w_QUALITY)) {
                if (n < 1)
                    return false;

                net.QualFlag = _tok[1].TryParse(out QualType type) 
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
                        if (!_tok[2].ToDouble(out double d))
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

            if (!_tok[nvalue].ToDouble(out double y))
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

            if (n < 1)
                throw new InputException(ErrorCode.Err201, SectType.TIMES, _line);

            if (_tok[0].Match(Keywords.w_STATISTIC)) {
                if (!_tok[n].TryParse(out TimeStatType type))
                    throw new InputException(ErrorCode.Err201, SectType.TIMES, _line);

                net.TstatFlag = type;

                return;
            }

            TimeSpan t;

            if ((t = Utilities.ToTimeSpan(_tok[n])) == TimeSpan.MinValue &&
                (t = Utilities.ToTimeSpan(_tok[n - 1])) == TimeSpan.MinValue &&
                (t = Utilities.ToTimeSpan(_tok[n - 1], _tok[n])) == TimeSpan.MinValue)
                throw new InputException(ErrorCode.Err213, SectType.TIMES, _line);

            if (_tok[0].Match(Keywords.w_DURATION))
                net.Duration = t;
            else if (_tok[0].Match(Keywords.w_HYDRAULIC))
                net.HStep = t;
            else if (_tok[0].Match(Keywords.w_QUALITY))
                net.QStep = t;
            else if (_tok[0].Match(Keywords.w_RULE))
                net.RuleStep = t;
            else if (_tok[0].Match(Keywords.w_MINIMUM)) {}
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
                net.Tstart = t.TimeOfDay();

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

        private void ParseTag() {
            int n = _tok.Count;

            if (n < 3)
                throw new InputException(ErrorCode.Err201, SectType.DEMANDS, _line);

            Element el;

            if (_tok[0].Match(Keywords.w_NODE)) {
                if ((el = net.GetNode(_tok[1])) == null)
                    throw new InputException(ErrorCode.Err203, SectType.TAGS, _tok[1]);
            }
            else if (_tok[0].Match(Keywords.w_LINK)) {
                if ((el = net.GetLink(_tok[2])) == null)
                    throw new InputException(ErrorCode.Err204, SectType.TAGS, _tok[1]);
            }
            else {
                throw new InputException(ErrorCode.Err201, SectType.TAGS, _line);
            }

            el.Tag = _tok[3];

        }

        private void ParseDemand() {
            int n = _tok.Count;
            Pattern pat = null;

            if (n < 2)
                throw new InputException(ErrorCode.Err201, SectType.DEMANDS, _line);

            if (!_tok[1].ToDouble(out double y))
                throw new InputException(ErrorCode.Err202, SectType.DEMANDS, _tok[0]);

            if (_tok[0].Match(Keywords.w_MULTIPLY)) {
                if (y <= 0.0)
                    throw new InputException(ErrorCode.Err202, SectType.DEMANDS, _tok[0]);

                net.DMult = y;
                return;
            }

            Node node  = net.GetNode(_tok[0]);

            if(node == null || node.NodeType != NodeType.JUNC)
                throw new InputException(ErrorCode.Err208, SectType.DEMANDS, _tok[0]);

            if (n >= 3) {
                pat = net.GetPattern(_tok[2]);
                if (pat == null)
                    throw new InputException(ErrorCode.Err205, SectType.DEMANDS, _line);
            }

            node.Demands.Add(new Demand(y, pat));

        }

        private void ParseRule() {
            _tok[0].TryParse(out Rulewords key);
            if (key == Rulewords.RULE) {
                _currentRule = new Rule(_tok[1]);
                net.Rules.Add(_currentRule);
            }
            else {
                _currentRule?.Code.Add(_line);
            }
        }

    }

}