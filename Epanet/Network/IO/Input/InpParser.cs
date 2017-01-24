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
        private SectType sectionType = (SectType)(-1);
        private readonly List<string> Tok = new List<string>(Constants.MAXTOKS);
        private string comment;
        string line;

        private Rule currentRule; // Current rule

        private static readonly string[] OptionValueKeywords = {
            Keywords.w_TOLERANCE, Keywords.w_DIFFUSIVITY, Keywords.w_DAMPLIMIT, Keywords.w_VISCOSITY,
            Keywords.w_SPECGRAV,  Keywords.w_TRIALS,      Keywords.w_ACCURACY,  Keywords.w_HTOL,
            Keywords.w_QTOL,      Keywords.w_RQTOL,       Keywords.w_CHECKFREQ, Keywords.w_MAXCHECK, 
            Keywords.w_EMITTER,   Keywords.w_DEMAND
        };

        public InpParser() {
            this.currentRule = null;
        }
        
        public override Network Parse(Network net_, string fileName)
        {
            if(net_ == null)
                throw new ArgumentNullException("net_");

            if(string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            this.net = net_;
            this.FileName = Path.GetFullPath(fileName);

            try {
                using(FileStream fs = File.OpenRead(fileName)) {
                    this.ParsePc(fs);
                    this.Parse(fs);

                    if (this.Errors.Count > 0)
                        throw new ENException(ErrorCode.Err200);

                    return net_;
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

            while ((this.line = buffReader.ReadLine()) != null) {

                this.line = this.line.Trim();

                if (string.IsNullOrEmpty(this.line))
                    continue;

                if (this.line[0] == '[') {
                    if (this.line.StartsWith("[PATTERNS]", StringComparison.OrdinalIgnoreCase)) {
                        this.sectionType = SectType.PATTERNS;
                    }
                    else if (this.line.StartsWith("[CURVES]", StringComparison.OrdinalIgnoreCase))
                        this.sectionType = SectType.CURVES;
                    else
                        this.sectionType = (SectType)(-1);
                    continue;
                }

                if (this.sectionType != (SectType)(-1)) {
                    if (this.line.IndexOf(';') >= 0)
                        this.line = this.line.Substring(0, this.line.IndexOf(';'));

                    if (string.IsNullOrEmpty(this.line))
                        continue;

                    if (Tokenize(this.line, this.Tok) == 0)
                        continue;

                    try {
                        switch (this.sectionType) {
                        case SectType.PATTERNS:
                            this.ParsePattern();
                            break;
                        case SectType.CURVES:
                            this.ParseCurve();
                            break;
                        }
                    }
                    catch (InputException ex) {
                        this.LogException(ex);
                    }
                }
            }

        }

        // Parse INP file
        private void Parse(Stream stream) {
            
            this.sectionType = (SectType)(-1);
            TextReader buffReader = new StreamReader(stream, Encoding.Default); // "ISO-8859-1";

            while ((this.line = buffReader.ReadLine()) != null) {
                this.comment = null;

                int index = this.line.IndexOf(';');

                if (index >= 0) {
                    if (index > 0)
                        this.comment = this.line.Substring(index + 1).Trim();

                    this.line = this.line.Substring(0, index);
                }

                //lineCount++;
                this.line = this.line.Trim();
                if (string.IsNullOrEmpty(this.line))
                    continue;

                if (Tokenize(this.line, this.Tok) == 0)
                    continue;

                if (this.Tok[0][0] == '[') {
                    this.sectionType = FindSectionType(this.Tok[0]);

                    if (this.sectionType < 0) {
                        this.Log.Warning("Unknown section type : %s", this.Tok[0]);
                    }

                    continue;
                }

                if (this.sectionType < 0) continue;

                try {
                    switch (this.sectionType) {
                    case SectType.TITLE:
                        this.net.Title.Add(this.line);
                        break;
                    case SectType.JUNCTIONS:
                        this.ParseJunction();
                        break;

                    case SectType.RESERVOIRS:
                    case SectType.TANKS:
                        this.ParseTank();
                        break;

                    case SectType.PIPES:
                        this.ParsePipe();
                        break;
                    case SectType.PUMPS:
                        this.ParsePump();
                        break;
                    case SectType.VALVES:
                        this.ParseValve();
                        break;
                    case SectType.CONTROLS:
                        this.ParseControl();
                        break;

                    case SectType.RULES:
                        this.ParseRule();
                        break;

                    case SectType.DEMANDS:
                        this.ParseDemand();
                        break;
                    case SectType.SOURCES:
                        this.ParseSource();
                        break;
                    case SectType.EMITTERS:
                        this.ParseEmitter();
                        break;
                    case SectType.QUALITY:
                        this.ParseQuality();
                        break;
                    case SectType.STATUS:
                        this.ParseStatus();
                        break;
                    case SectType.ENERGY:
                        this.ParseEnergy();
                        break;
                    case SectType.REACTIONS:
                        this.ParseReact();
                        break;
                    case SectType.MIXING:
                        this.ParseMixing();
                        break;
                    case SectType.REPORT:
                        this.ParseReport();
                        break;
                    case SectType.TIMES:
                        this.ParseTime();
                        break;
                    case SectType.OPTIONS:
                        this.ParseOption();
                        break;
                    case SectType.COORDINATES:
                        this.ParseCoordinate();
                        break;
                    case SectType.VERTICES:
                        this.ParseVertice();
                        break;
                    case SectType.LABELS:
                        this.ParseLabel();
                        break;
                    }
                }
                catch (InputException ex) {
                    this.LogException(ex);
                }
            }

            AdjustData(this.net);

            this.net.FieldsMap.Prepare(this.net.UnitsFlag, this.net.FlowFlag, this.net.PressFlag, this.net.QualFlag, this.net.ChemUnits, this.net.SpGrav, this.net.HStep);

            this.Convert();

            return;
        }

        private static long atol(string value) {
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
            int n = this.Tok.Count;
            double el, y = 0.0d;
            Pattern p = null;

            if (this.net.GetNode(this.Tok[0]) != null)
                throw new InputException(ErrorCode.Err215, SectType.JUNCTIONS, this.Tok[0]);

            Node node = new Node(this.Tok[0]);

            this.net.Nodes.Add(node);

            if (n < 2)
                throw new InputException(ErrorCode.Err201, SectType.JUNCTIONS, this.line);

            if (!this.Tok[1].ToDouble(out el))
                throw new InputException(ErrorCode.Err202, SectType.JUNCTIONS, this.Tok[0]);

            if (n >= 3 && !this.Tok[2].ToDouble(out y)) {
                throw new InputException(ErrorCode.Err202, SectType.JUNCTIONS, this.Tok[0]);
            }

            if (n >= 4) {
                p = this.net.GetPattern(this.Tok[3]);
                if (p == null) {
                    throw new InputException(ErrorCode.Err205, SectType.JUNCTIONS, this.Tok[0]);
                }
            }

            node.Elevation = el;
            node.C0 = 0.0;
            node.QualSource = null;
            node.Ke = 0.0;
            node.RptFlag = false;

            if (!string.IsNullOrEmpty(this.comment))
                node.Comment = this.comment;

            if (n >= 3) {
                node.PrimaryDemand.Base = y;
                node.PrimaryDemand.Pattern = p;
            }
            
        }

        private void ParseTank() {
            int n = this.Tok.Count;
            Pattern p = null;
            Curve vcurve = null;
            double el,
                   initlevel = 0.0d,
                   minlevel = 0.0d,
                   maxlevel = 0.0d,
                   minvol = 0.0d,
                   diam = 0.0d;

            if (this.net.GetNode(this.Tok[0]) != null)
                throw new InputException(ErrorCode.Err215, SectType.TANKS, this.Tok[0]);

            Tank tank = new Tank(this.Tok[0]);
            
            if(!string.IsNullOrEmpty(this.comment))
                tank.Comment = this.comment;

            this.net.Nodes.Add(tank);

            if (n < 2)
                throw new InputException(ErrorCode.Err201, SectType.TANKS, this.line);

            if (!this.Tok[1].ToDouble(out el))
                throw new InputException(ErrorCode.Err202, SectType.TANKS, this.Tok[0]);

            if(n <= 3) {
                /* Tank is reservoir.*/
                if (n == 3) {
                    p = this.net.GetPattern(this.Tok[2]);
                    if (p == null) {
                        throw new InputException(ErrorCode.Err205, SectType.TANKS, this.Tok[0]);
                    }
                }
            }
            else if (n < 6) {
                throw new InputException(ErrorCode.Err201, SectType.TANKS, this.line);
            }
            else {

                /* Check for valid input data */
                if (!this.Tok[2].ToDouble(out initlevel) ||
                    !this.Tok[3].ToDouble(out minlevel) ||
                    !this.Tok[4].ToDouble(out maxlevel) ||
                    !this.Tok[5].ToDouble(out diam) ||
                    (diam < 0.0))
                    throw new InputException(ErrorCode.Err202, SectType.TANKS, this.Tok[0]);

                if (n >= 7 && !this.Tok[6].ToDouble(out minvol))
                    throw new InputException(ErrorCode.Err202, SectType.TANKS, this.Tok[0]);

                if (n == 8) {
                    vcurve = this.net.GetCurve(this.Tok[7]);

                    if (vcurve == null)
                        throw new InputException(ErrorCode.Err206, SectType.TANKS, this.Tok[0]);
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
            int n = this.Tok.Count;
            LinkType type = LinkType.PIPE;
            StatType status = StatType.OPEN;
            double length, diam, rcoeff, lcoeff = 0.0d;

            if (this.net.GetLink(this.Tok[0]) != null)
                throw new InputException(ErrorCode.Err215, SectType.PIPES, this.Tok[0]);

            Link link = new Link(this.Tok[0]);
            this.net.Links.Add(link);
            
            if (n < 6)
                throw new InputException(ErrorCode.Err201, SectType.PIPES, this.line);


            Node j1 = this.net.GetNode(this.Tok[1]),
                 j2 = this.net.GetNode(this.Tok[2]);
            
            if (j1 == null || j2 == null)
                throw new InputException(ErrorCode.Err203, SectType.PIPES, this.Tok[0]);


            if (j1.Equals(j2))
                throw new InputException(ErrorCode.Err222, SectType.PIPES, this.Tok[0]);

            if (!this.Tok[3].ToDouble(out length) || length <= 0.0 ||
                !this.Tok[4].ToDouble(out diam) || diam <= 0.0 ||
                !this.Tok[5].ToDouble(out rcoeff) || rcoeff <= 0.0)
                throw new InputException(ErrorCode.Err202, SectType.PIPES, this.Tok[0]);


            if (n == 7) {
                if(this.Tok[6].Match(Keywords.w_CV)) type = LinkType.CV;
                else if(this.Tok[6].Match(Keywords.w_CLOSED)) status = StatType.CLOSED;
                else if(this.Tok[6].Match(Keywords.w_OPEN)) status = StatType.OPEN;
                else if (!this.Tok[6].ToDouble(out lcoeff) || lcoeff < 0)
                    throw new InputException(ErrorCode.Err202, SectType.PIPES, this.Tok[0]);
            }

            if (n == 8) {
                if (!this.Tok[6].ToDouble(out lcoeff) || lcoeff < 0)
                    throw new InputException(ErrorCode.Err202, SectType.PIPES, this.Tok[0]);

                if(this.Tok[7].Match(Keywords.w_CV)) type = LinkType.CV;
                else if(this.Tok[7].Match(Keywords.w_CLOSED)) status = StatType.CLOSED;
                else if(this.Tok[7].Match(Keywords.w_OPEN)) status = StatType.OPEN;
                else
                    throw new InputException(ErrorCode.Err202, SectType.PIPES, this.Tok[0]); 
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

            if (!string.IsNullOrEmpty(this.comment))
                link.Comment = this.comment;
        }

        private void ParsePump() {
            int m, n = this.Tok.Count;
            double[] x = new double[6];

            if (this.net.GetLink(this.Tok[0]) != null)
                throw new InputException(ErrorCode.Err215, SectType.PUMPS, this.Tok[0]);
            
            if (n < 4)
                throw new InputException(ErrorCode.Err201, SectType.PUMPS, this.line);

            Node j1 = this.net.GetNode(this.Tok[1]),
                 j2 = this.net.GetNode(this.Tok[2]);

            if (j1 == null || j2 == null)
                throw new InputException(ErrorCode.Err203, SectType.PUMPS, this.Tok[0]);

            if (j1.Equals(j2))
                throw new InputException(ErrorCode.Err222, SectType.PUMPS, this.Tok[0]);

            Pump pump = new Pump(this.Tok[0]) {
                FirstNode = j1,
                SecondNode = j2,
            };
            
            this.net.Links.Add(pump);

            if (!string.IsNullOrEmpty(this.comment)) pump.Comment = this.comment;

            // If 4-th token is a number then input follows Version 1.x format 
            // so retrieve pump curve parameters
            if (this.Tok[3].ToDouble(out x[0])) {

                m = 1;
                for (int j = 4; j < n; j++) {
                    if (!this.Tok[j].ToDouble(out x[m]))
                        throw new InputException(ErrorCode.Err202, SectType.PUMPS, this.Tok[0]);
                    m++;
                }

                GetPumpCurve(pump, m, x); /* Get pump curve params */

                return;
            }

            /* Otherwise input follows Version 2 format */
            /* so retrieve keyword/value pairs.         */
            m = 4;
            while (m < n) {
                if (this.Tok[m - 1].Match(Keywords.w_POWER)) { /* Const. HP curve       */
                    double y;
                    if (!this.Tok[m].ToDouble(out y) || y <= 0.0)
                        throw new InputException(ErrorCode.Err202, SectType.PUMPS, this.Tok[0]);

                    pump.Ptype = PumpType.CONST_HP;
                    pump.Km = y;
                }
                else if (this.Tok[m - 1].Match(Keywords.w_HEAD)) { /* Custom pump curve      */
                    Curve t = this.net.GetCurve(this.Tok[m]);
                    if (t == null) 
                        throw new InputException(ErrorCode.Err206, SectType.PUMPS, this.Tok[0]);

                    pump.HCurve = t;
                }
                else if (this.Tok[m - 1].Match(Keywords.w_PATTERN)) { /* Speed/status pattern */
                    Pattern p = this.net.GetPattern(this.Tok[m]);
                    if (p == null) 
                        throw new InputException(ErrorCode.Err205, SectType.PUMPS, this.Tok[0]);

                    pump.UPat = p;
                }
                else if (this.Tok[m - 1].Match(Keywords.w_SPEED)) { /* Speed setting */
                    double y;
                    if (!this.Tok[m].ToDouble(out y) || y < 0.0)
                        throw new InputException(ErrorCode.Err202, SectType.PUMPS, this.Tok[0]);

                    pump.Kc = y;
                }
                else {
                    throw new InputException(ErrorCode.Err201, SectType.PUMPS, this.line);
                }

                m += 2;
            }
        }

        private void ParseValve() {
            int n = this.Tok.Count;
            StatType status = StatType.ACTIVE;
            LinkType type;

            double diam, setting, lcoeff = 0.0;

            if (this.net.GetLink(this.Tok[0]) != null)
                throw new InputException(ErrorCode.Err215, SectType.VALVES, this.Tok[0]);

            Valve valve = new Valve(this.Tok[0]);
            this.net.Links.Add(valve);

            if (n < 6)
                throw new InputException(ErrorCode.Err201, SectType.VALVES, this.line);

            Node j1 = this.net.GetNode(this.Tok[1]),
                 j2 = this.net.GetNode(this.Tok[2]);

            if (j1 == null || j2 == null)
                throw new InputException(ErrorCode.Err203, SectType.VALVES, this.Tok[0]);

            if (j1.Equals(j2))
                throw new InputException(ErrorCode.Err222, SectType.VALVES, this.Tok[0]);

            if (!EnumsTxt.TryParse(this.Tok[4], out type))
                throw new InputException(ErrorCode.Err201, SectType.VALVES, this.line);

            if (!this.Tok[3].ToDouble(out diam) || diam <= 0.0)
                throw new InputException(ErrorCode.Err202, SectType.VALVES, this.Tok[0]);

            if (type == LinkType.GPV) {
                Curve t = this.net.GetCurve(this.Tok[5]);
                
                if (t == null)
                    throw new InputException(ErrorCode.Err206, SectType.VALVES, this.Tok[0]);

                setting = this.net.Curves.IndexOf(t);
                this.Log.Warning("GPV Valve, index as roughness !");
                valve.Curve = t;
                status = StatType.OPEN;
            }
            else if (!this.Tok[5].ToDouble(out setting)) {
                throw new InputException(ErrorCode.Err202, SectType.VALVES, this.Tok[0]);
            }

            if (n >= 7) {
                if (!this.Tok[6].ToDouble(out lcoeff)) {
                    throw new InputException(ErrorCode.Err202, SectType.VALVES, this.Tok[0]);
                }
            }

            if ((j1.Type > NodeType.JUNC || j2.Type > NodeType.JUNC) &&
                (type == LinkType.PRV || type == LinkType.PSV || type == LinkType.FCV))
                throw new InputException(ErrorCode.Err219, SectType.VALVES, this.Tok[0]);

            if (!Valvecheck(this.net, type, j1, j2))
                throw new InputException(ErrorCode.Err220, SectType.VALVES, this.Tok[0]);


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

            if (!string.IsNullOrEmpty(this.comment))
                valve.Comment = this.comment;
        }
      
        private void ParsePattern() {
            Pattern pat = this.net.GetPattern(this.Tok[0]);

            if (pat == null) {
                pat = new Pattern(this.Tok[0]);
                this.net.Patterns.Add(pat);
            }

            for (int i = 1; i < this.Tok.Count; i++) {
                double x;

                if (!this.Tok[i].ToDouble(out x))
                    throw new InputException(ErrorCode.Err202, SectType.PATTERNS, this.Tok[0]);

                pat.Add(x);
            }
        }

        private void ParseCurve() {
            Curve cur = this.net.GetCurve(this.Tok[0]);

            if (cur == null) {
                cur = new Curve(this.Tok[0]);
                this.net.Curves.Add(cur);
            }

            double x, y;

            if (!this.Tok[1].ToDouble(out x) || !this.Tok[2].ToDouble(out y))
                throw new InputException(ErrorCode.Err202, SectType.CURVES, this.Tok[0]);

            cur.Add(x, y);
        }

        private void ParseCoordinate() {
            if (this.Tok.Count < 3)
                throw new InputException(ErrorCode.Err201, SectType.COORDINATES, this.line);

            Node node = this.net.GetNode(this.Tok[0]);

            if (node == null)
                throw new InputException(ErrorCode.Err203, SectType.COORDINATES, this.Tok[0]);

            double x, y;

            if (!this.Tok[1].ToDouble(out x) || !this.Tok[2].ToDouble(out y))
                throw new InputException(ErrorCode.Err202, SectType.COORDINATES, this.Tok[0]);

            node.Position = new EnPoint(x, y);
        }

        private void ParseLabel() {
            if (this.Tok.Count < 3)
                throw new InputException(ErrorCode.Err201, SectType.LABELS, this.line);
            
            double x, y;

            if (!this.Tok[0].ToDouble(out x) || !this.Tok[1].ToDouble(out y))
                throw new InputException(ErrorCode.Err201, SectType.LABELS, this.line);

            string text = this.Tok[2].Replace("\"", "");
            
            Label label = new Label(text) {
                Position = new EnPoint(x, y)
            };

            this.net.Labels.Add(label);
        }

        private void ParseVertice() {
            if (this.Tok.Count < 3)
                throw new InputException(ErrorCode.Err201, SectType.VERTICES, this.line);

            Link link = this.net.GetLink(this.Tok[0]);

            if (link == null)
                throw new InputException(ErrorCode.Err204, SectType.VERTICES, this.Tok[0]);

            double x, y;

            if (!this.Tok[1].ToDouble(out x) || !this.Tok[2].ToDouble(out y))
                throw new InputException(ErrorCode.Err202, SectType.VERTICES, this.Tok[0]);

            link.Vertices.Add(new EnPoint(x, y));
        }

        private void ParseControl() {
            int n = this.Tok.Count;
            StatType status = StatType.ACTIVE;

            double setting = Constants.MISSING, time = 0.0, level = 0.0;

            if (n < 6)
                throw new InputException(ErrorCode.Err201, SectType.CONTROLS, this.line);

            Node node = null;
            Link link = this.net.GetLink(this.Tok[1]);

            if (link == null)
                throw new InputException(ErrorCode.Err204, SectType.CONTROLS, this.line);

            LinkType ltype = link.Type;

            if (ltype == LinkType.CV)
                throw new InputException(ErrorCode.Err207, SectType.CONTROLS, this.line);

            if (this.Tok[2].Match(StatType.OPEN.ToString())) {
                status = StatType.OPEN;
                if (ltype == LinkType.PUMP) setting = 1.0;
                if (ltype == LinkType.GPV) setting = link.Kc;
            }
            else if (this.Tok[2].Match(StatType.CLOSED.ToString())) {
                status = StatType.CLOSED;
                if (ltype == LinkType.PUMP) setting = 0.0;
                if (ltype == LinkType.GPV) setting = link.Kc;
            }
            else if (ltype == LinkType.GPV) {
                throw new InputException(ErrorCode.Err206, SectType.CONTROLS, this.line);
            }
            else if (!this.Tok[2].ToDouble(out setting)) {
                throw new InputException(ErrorCode.Err202, SectType.CONTROLS, this.line);
            }

            if (ltype == LinkType.PUMP || ltype == LinkType.PIPE) {
                if (!setting.IsMissing()) {
                    if (setting < 0.0)
                        throw new InputException(ErrorCode.Err202, SectType.CONTROLS, this.line);

                    status = setting == 0.0 ? StatType.CLOSED : StatType.OPEN;
                }
            }

            ControlType ctype;

            if (this.Tok[4].Match(Keywords.w_TIME))
                ctype = ControlType.TIMER;
            else if (this.Tok[4].Match(Keywords.w_CLOCKTIME))
                ctype = ControlType.TIMEOFDAY;
            else {
                if (n < 8)
                    throw new InputException(ErrorCode.Err201, SectType.CONTROLS, this.line);

                if ((node = this.net.GetNode(this.Tok[5])) == null)
                    throw new InputException(ErrorCode.Err203, SectType.CONTROLS, this.line);

                if (this.Tok[6].Match(Keywords.w_BELOW)) ctype = ControlType.LOWLEVEL;
                else if (this.Tok[6].Match(Keywords.w_ABOVE)) ctype = ControlType.HILEVEL;
                else
                    throw new InputException(ErrorCode.Err201, SectType.CONTROLS, this.line);
            }

            switch (ctype) {
            case ControlType.TIMER:
            case ControlType.TIMEOFDAY:
                if (n == 6) time = Utilities.GetHour(this.Tok[5]);
                if (n == 7) time = Utilities.GetHour(this.Tok[5], this.Tok[6]);
                if(time < 0.0) throw new InputException(ErrorCode.Err201, SectType.CONTROLS, this.line);
                break;

            case ControlType.LOWLEVEL:
            case ControlType.HILEVEL:
                if (!this.Tok[7].ToDouble(out level))
                    throw new InputException(ErrorCode.Err202, SectType.CONTROLS, this.line);

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

            this.net.Controls.Add(cntr);
        }

        private void ParseSource() {
            int n = this.Tok.Count;

            SourceType type; /* Source type   */
            double c0; /* Init. quality */
            Pattern pat = null;
            

            if (n < 2) 
                throw new InputException(ErrorCode.Err201, SectType.SOURCES, this.line);

            Node node = this.net.GetNode(this.Tok[0]);

            if (node == null)
                throw new InputException(ErrorCode.Err203, SectType.SOURCES, this.Tok[0]);

            /* NOTE: Under old format, SourceType not supplied so let  */
            /*       i = index of token that contains quality value.   */
            int i = 2; /* Token with quality value */

            if (!EnumsTxt.TryParse(this.Tok[1], out type)) {
                i = 1;
                type = SourceType.CONCEN;
            }

            if (!this.Tok[i].ToDouble(out c0)) {
                /* Illegal WQ value */
                throw new InputException(ErrorCode.Err202, SectType.SOURCES, this.Tok[0]);
            }

            if (n > i + 1 && !string.IsNullOrEmpty(this.Tok[i + 1]) && this.Tok[i + 1] != "*") {
                if((pat = this.net.GetPattern(this.Tok[i + 1])) == null)
                    throw new InputException(ErrorCode.Err205, SectType.SOURCES, this.Tok[0]);
            }

            node.QualSource = new QualSource(type, c0, pat);
        }

        private void ParseEmitter() {
            int n = this.Tok.Count;
            Node node;
            double k;

            if (n < 2) throw
                new InputException(ErrorCode.Err201, SectType.EMITTERS, this.line);
            
            if ((node = this.net.GetNode(this.Tok[0])) == null)
                throw new InputException(ErrorCode.Err203, SectType.EMITTERS, this.Tok[0]);

            if (node.Type != NodeType.JUNC)
                throw new InputException(ErrorCode.Err209, SectType.EMITTERS, this.Tok[0]);

            if (!this.Tok[1].ToDouble(out k) || k < 0.0)
                throw new InputException(ErrorCode.Err202, SectType.EMITTERS, this.Tok[0]);

            node.Ke = k;

        }

        private void ParseQuality() {
            int n = this.Tok.Count;
            double c0;

            if (n < 2) return;

            /* Single node entered  */
            if (n == 2) {
                Node node = this.net.GetNode(this.Tok[0]);

                if (node == null) return;

                if (!this.Tok[1].ToDouble(out c0))
                    throw new InputException(ErrorCode.Err209, SectType.QUALITY, this.Tok[0]);

                node.C0 = c0;
            }
            else {

                /* Node range entered    */
                if (!this.Tok[2].ToDouble(out c0))
                    throw new InputException(ErrorCode.Err209, SectType.QUALITY, this.Tok[0]);

                /* If numerical range supplied, then use numerical comparison */
                long i0, i1;

                if ((i0 = atol(this.Tok[0])) > 0 && (i1 = atol(this.Tok[1])) > 0) {
                    foreach (Node node  in  this.net.Nodes) {
                        long i = atol(node.Name);
                        if (i >= i0 && i <= i1)
                            node.C0 = c0;
                    }
                }
                else {
                    foreach (Node node  in  this.net.Nodes) {
                        if ((string.Compare(this.Tok[0], node.Name, StringComparison.Ordinal) <= 0) &&
                            (string.Compare(this.Tok[1], node.Name, StringComparison.Ordinal) >= 0))
                            node.C0 = c0;
                    }
                }
            }
        }

        private void ParseReact() {
            int item, n = this.Tok.Count;
            double y;

            if (n < 3) return;


            if(this.Tok[0].Match(Keywords.w_ORDER)) { /* Reaction order */

                if (!this.Tok[n - 1].ToDouble(out y))
                    throw new InputException(ErrorCode.Err213, SectType.REACTIONS, this.line);

                if (this.Tok[1].Match(Keywords.w_BULK)) this.net.BulkOrder = y;
                else if (this.Tok[1].Match(Keywords.w_TANK)) this.net.TankOrder = y;
                else if (this.Tok[1].Match(Keywords.w_WALL)) {
                    if (y == 0.0) this.net.WallOrder = 0.0;
                    else if (y == 1.0) this.net.WallOrder = 1.0;
                    else throw new InputException(ErrorCode.Err213, SectType.REACTIONS, this.line);
                }
                else throw new InputException(ErrorCode.Err213, SectType.REACTIONS, this.line);

                return;
            }

            if(this.Tok[0].Match(Keywords.w_ROUGHNESS)) { /* Roughness factor */
                if (!this.Tok[n - 1].ToDouble(out y))
                    throw new InputException(ErrorCode.Err213, SectType.REACTIONS, this.line);

                this.net.RFactor = y;
                return;
            }

            if(this.Tok[0].Match(Keywords.w_LIMITING)) { /* Limiting potential */
                if (!this.Tok[n - 1].ToDouble(out y))
                    throw new InputException(ErrorCode.Err213, SectType.REACTIONS, this.line);

                this.net.CLimit = y;
                return;
            }

            if(this.Tok[0].Match(Keywords.w_GLOBAL)) { /* Global rates */
                if (!this.Tok[n - 1].ToDouble(out y))
                    throw new InputException(ErrorCode.Err213, SectType.REACTIONS, this.line);

                if (this.Tok[1].Match(Keywords.w_BULK)) this.net.KBulk = y;
                else if (this.Tok[1].Match(Keywords.w_WALL)) this.net.KWall = y;
                else throw new InputException(ErrorCode.Err201, SectType.REACTIONS, this.line);

                return;
            }

            /* Individual rates */
            if (this.Tok[0].Match(Keywords.w_BULK)) item = 1;
            else if (this.Tok[0].Match(Keywords.w_WALL)) item = 2;
            else if (this.Tok[0].Match(Keywords.w_TANK)) item = 3;
            else throw new InputException(ErrorCode.Err201, SectType.REACTIONS, this.line);

            this.Tok[0] = this.Tok[1];

            if(item == 3) { /* Tank rates */
                if (!this.Tok[n - 1].ToDouble(out y))
                    throw new InputException(ErrorCode.Err209, SectType.REACTIONS, this.Tok[1]);

                if (n == 3) {
                    Node node;
                    if ((node = this.net.GetNode(this.Tok[1])) == null)
                        throw new InputException(ErrorCode.Err208, SectType.REACTIONS, this.Tok[1]); 

                    Tank tank = node as Tank;
                    
                    if (tank == null) return;
                    tank.Kb = y; 
                }
                else {
                    long i1, i2;

                    /* If numerical range supplied, then use numerical comparison */
                    if ((i1 = atol(this.Tok[1])) > 0 && (i2 = atol(this.Tok[2])) > 0) {
                        foreach (Tank tank  in  this.net.Tanks) {
                            long i = atol(tank.Name);
                            if (i >= i1 && i <= i2)
                                tank.Kb = y;
                        }
                    }
                    else {
                        foreach (Tank tank  in  this.net.Tanks) {
                            if (string.Compare(this.Tok[1], tank.Name, StringComparison.Ordinal) <= 0 &&
                                string.Compare(this.Tok[2], tank.Name, StringComparison.Ordinal) >= 0)
                                tank.Kb = y;
                        }
                    }
                }
            }
            else { /* Link rates */
                if (!this.Tok[n - 1].ToDouble(out y))
                    throw new InputException(ErrorCode.Err211, SectType.REACTIONS, this.Tok[1]);

                if (this.net.Links.Count == 0) return;

                if(n == 3) { /* Single link */
                    Link link;
                    if ((link = this.net.GetLink(this.Tok[1])) == null) return;
                    if (item == 1) link.Kb = y;
                    else           link.Kw = y;
                }
                else { /* Range of links */
                    long i1, i2;

                    /* If numerical range supplied, then use numerical comparison */
                    if ((i1 = atol(this.Tok[1])) > 0 && (i2 = atol(this.Tok[2])) > 0) {
                        foreach (Link link  in  this.net.Links) {
                            long i = atol(link.Name);
                            if (i >= i1 && i <= i2) {
                                if (item == 1) link.Kb = y;
                                else link.Kw = y;
                            }
                        }
                    }
                    else
                        foreach (Link link  in  this.net.Links) {
                            if (string.Compare(this.Tok[1], link.Name, StringComparison.Ordinal) <= 0 &&
                                string.Compare(this.Tok[2], link.Name, StringComparison.Ordinal) >= 0) {
                                if (item == 1) link.Kb = y;
                                else           link.Kw = y;
                            }
                        }
                }
            }
        }

        private void ParseMixing() {
            int n = this.Tok.Count;
            MixType type;

            if (this.net.Nodes.Count == 0)
                throw new InputException(ErrorCode.Err208, SectType.MIXING, this.Tok[0]);

            if (n < 2) return;

            Node node = this.net.GetNode(this.Tok[0]);

            if(node == null)
                throw new InputException(ErrorCode.Err208, SectType.MIXING, this.Tok[0]);

            if (node.Type != NodeType.JUNC) return;
            
            Tank tank = (Tank)node;

            if (!EnumsTxt.TryParse(this.Tok[1], out type))
                throw new InputException(ErrorCode.Err201, SectType.MIXING, this.line);

            var v = 1.0;
            if (type == MixType.MIX2 && n == 3) {
                if (!this.Tok[2].ToDouble(out v)) {
                    throw new InputException(ErrorCode.Err209, SectType.MIXING, this.Tok[0]);
                }
            }

            if (v == 0.0)
                v = 1.0;

            if (tank.Type == NodeType.RESERV) return;

            tank.MixModel = type;
            tank.V1Max = v;
        }

        private void ParseStatus() {
            int n = this.Tok.Count - 1;
            double y = 0.0;
            StatType status = StatType.ACTIVE;

            if (this.net.Links.Count == 0)
                throw new InputException(ErrorCode.Err210, SectType.STATUS, this.Tok[0]);

            if(n < 1)
                throw new InputException(ErrorCode.Err201, SectType.STATUS, this.line);

            if (this.Tok[n].Match(Keywords.w_OPEN)) status = StatType.OPEN;
            else if (this.Tok[n].Match(Keywords.w_CLOSED)) status = StatType.CLOSED;
            else if (!this.Tok[n].ToDouble(out y)) 
                throw new InputException(ErrorCode.Err211, SectType.STATUS, this.Tok[0]);

            if (y < 0.0)
                throw new InputException(ErrorCode.Err211, SectType.STATUS, this.Tok[0]);

            if (n == 1) {
                Link link;
                if ((link = this.net.GetLink(this.Tok[0])) == null) return;

                if (link.Type == LinkType.CV)
                    throw new InputException(ErrorCode.Err211, SectType.STATUS, this.Tok[0]);

                if (link.Type == LinkType.GPV && status == StatType.ACTIVE)
                    throw new InputException(ErrorCode.Err211, SectType.STATUS, this.Tok[0]);

                ChangeStatus(link, status, y);
            }
            else {
                long i0, i1;

                /* If numerical range supplied, then use numerical comparison */
                if ((i0 = atol(this.Tok[0])) > 0 && (i1 = atol(this.Tok[1])) > 0) {
                    foreach (Link link  in  this.net.Links) {
                        long i = atol(link.Name);
                        if (i >= i0 && i <= i1) 
                            ChangeStatus(link, status, y);
                        
                        
                    }
                }
                else
                    foreach (Link j  in  this.net.Links)
                        if (string.Compare(this.Tok[0], j.Name, StringComparison.Ordinal) <= 0 &&
                            string.Compare(this.Tok[1], j.Name, StringComparison.Ordinal) >= 0)
                            ChangeStatus(j, status, y);
            }
        }


        private void ParseEnergy() {
            int n = this.Tok.Count;
            double y;

            if(n < 3)
                throw new InputException(ErrorCode.Err201, SectType.ENERGY, this.line);

            if (this.Tok[0].Match(Keywords.w_DMNDCHARGE)) {
                if (!this.Tok[2].ToDouble(out y))
                    throw new InputException(ErrorCode.Err213, SectType.ENERGY, this.line);

                this.net.DCost = y;
                return;
            }

            Pump pump;

            if (this.Tok[0].Match(Keywords.w_GLOBAL)) {
                pump = null;
            }
            else if (this.Tok[0].Match(Keywords.w_PUMP)) {
                if (n < 4)
                    throw new InputException(ErrorCode.Err201, SectType.ENERGY, this.line);

                Link linkRef = this.net.GetLink(this.Tok[1]);
                
                if(linkRef == null || linkRef.Type != LinkType.PUMP)
                    throw new InputException(ErrorCode.Err216, SectType.ENERGY, this.Tok[0]);

                pump = (Pump)linkRef;
            }
            else
                throw new InputException(ErrorCode.Err201, SectType.ENERGY, this.line);


            if (this.Tok[n - 2].Match(Keywords.w_PRICE)) {
                if (!this.Tok[n - 1].ToDouble(out y)) {
                    if (pump == null)
                        throw new InputException(ErrorCode.Err213, SectType.ENERGY, this.line);
                    else
                        throw new InputException(ErrorCode.Err217, SectType.ENERGY, this.Tok[0]);
                }

                if (pump == null)
                    this.net.ECost = y;
                else
                    pump.ECost = y;

                return;
            }

            if (this.Tok[n - 2].Match(Keywords.w_PATTERN)) {
                Pattern t = this.net.GetPattern(this.Tok[n - 1]);
                if (t == null) {
                    throw pump == null
                        ? new InputException(ErrorCode.Err213, SectType.ENERGY, this.line)
                        : new InputException(ErrorCode.Err217, SectType.ENERGY, this.Tok[0]);
                }

                if (pump == null)
                    this.net.EPatId = t.Name;
                else
                    pump.EPat = t;

                return;
            }

            if (this.Tok[n - 2].Match(Keywords.w_EFFIC)) {
                if (pump == null) {
                    if(!this.Tok[n - 1].ToDouble(out y) || y <= 0.0)
                        throw new InputException(ErrorCode.Err213, SectType.ENERGY, this.line);

                    this.net.EPump = y;
                }
                else {
                    Curve t = this.net.GetCurve(this.Tok[n - 1]);
                    if(t == null)
                        throw new InputException(ErrorCode.Err217, SectType.ENERGY, this.Tok[0]);

                    pump.ECurve = t;
                }
                return;
            }

            throw new InputException(ErrorCode.Err201, SectType.ENERGY, this.line);
        }

        private void ParseReport() {
            int n = this.Tok.Count - 1;
            //FieldType i;
            double y;

            if (n < 1) 
                throw new InputException(ErrorCode.Err201, SectType.REPORT, this.line);

            if (this.Tok[0].Match(Keywords.w_PAGE)) {
                if(!this.Tok[n].ToDouble(out y))
                    throw new InputException(ErrorCode.Err213, SectType.REPORT, this.line);

                if(y < 0.0 || y > 255.0)
                    throw new InputException(ErrorCode.Err213, SectType.REPORT, this.line);

                this.net.PageSize = (int)y;
                return;
            }


            if (this.Tok[0].Match(Keywords.w_STATUS)) {
                StatFlag flag;
                if (EnumsTxt.TryParse(this.Tok[n], out flag)) {
                    this.net.StatFlag = flag;
                }

                return;
            }

            if (this.Tok[0].Match(Keywords.w_SUMMARY)) {
                if (this.Tok[n].Match(Keywords.w_NO)) this.net.SummaryFlag = false;
                if (this.Tok[n].Match(Keywords.w_YES)) this.net.SummaryFlag = true;
                return;
            }

            if (this.Tok[0].Match(Keywords.w_MESSAGES)) {
                if (this.Tok[n].Match(Keywords.w_NO)) this.net.MessageFlag = false;
                if (this.Tok[n].Match(Keywords.w_YES)) this.net.MessageFlag = true;
                return;
            }

            if (this.Tok[0].Match(Keywords.w_ENERGY)) {
                if (this.Tok[n].Match(Keywords.w_NO)) this.net.EnergyFlag = false;
                if (this.Tok[n].Match(Keywords.w_YES)) this.net.EnergyFlag = true;
                return;
            }

            if (this.Tok[0].Match(Keywords.w_NODE)) {

                if (this.Tok[n].Match(Keywords.w_NONE)) {
                    this.net.NodeFlag = ReportFlag.FALSE;
                }
                else if (this.Tok[n].Match(Keywords.w_ALL)) {
                    this.net.NodeFlag = ReportFlag.TRUE;
                }
                else {

                    if (this.net.Nodes.Count == 0)
                        throw new InputException(ErrorCode.Err208, SectType.REPORT, this.Tok[1]);

                    for (int i = 1; i <= n; i++) {
                        Node node = this.net.GetNode(this.Tok[i]);
                       
                        if (node == null)
                            throw new InputException(ErrorCode.Err208, SectType.REPORT, this.Tok[i]);

                        node.RptFlag = true;
                    }

                    this.net.NodeFlag = ReportFlag.SOME;
                }
                return;
            }

            if (this.Tok[0].Match(Keywords.w_LINK)) {
                if (this.Tok[n].Match(Keywords.w_NONE))
                    this.net.LinkFlag = ReportFlag.FALSE;
                else if (this.Tok[n].Match(Keywords.w_ALL))
                    this.net.LinkFlag = ReportFlag.TRUE;
                else {
                   
                    if (this.net.Links.Count == 0)
                        throw new InputException(ErrorCode.Err210, SectType.REPORT, this.Tok[1]);
                    
                    for (int i = 1; i <= n; i++) {
                        Link link = this.net.GetLink(this.Tok[i]);
                        
                        if (link == null)
                            throw new InputException(ErrorCode.Err210, SectType.REPORT, this.Tok[i]);
                        
                        link.RptFlag = true;
                    }

                    this.net.LinkFlag = ReportFlag.SOME;
                }
                return;
            }

            FieldType iFieldID;
            FieldsMap fMap = this.net.FieldsMap;

            if (EnumsTxt.TryParse(this.Tok[0], out iFieldID)) {
                if (iFieldID > FieldType.FRICTION)
                    throw new InputException(ErrorCode.Err201, SectType.REPORT, this.line);

                if (this.Tok.Count == 1 || this.Tok[1].Match(Keywords.w_YES)) {
                    fMap.GetField(iFieldID).Enabled = true;
                    return;
                }

                if (this.Tok[1].Match(Keywords.w_NO)) {
                    fMap.GetField(iFieldID).Enabled = false;
                    return;
                }

                RangeType rj;

                if (this.Tok.Count < 3)
                    throw new InputException(ErrorCode.Err201, SectType.REPORT, this.line);

                if (!EnumsTxt.TryParse(this.Tok[1], out rj))
                    throw new InputException(ErrorCode.Err201, SectType.REPORT, this.line);

                if (!this.Tok[2].ToDouble(out y))
                    throw new InputException(ErrorCode.Err201, SectType.REPORT, this.line);

                if (rj == RangeType.PREC) {
                    fMap.GetField(iFieldID).Enabled = true;
                    fMap.GetField(iFieldID).Precision = (int)Math.Round(y); //roundOff(y));
                }
                else
                    fMap.GetField(iFieldID).SetRptLim(rj, y);

                return;
            }

            if (this.Tok[0].Match(Keywords.w_FILE)) {
                this.net.AltReport = this.Tok[1];
                return;
            }
          
            throw new InputException(ErrorCode.Err201, SectType.REPORT, this.line);

        }

        private void ParseOption() {
            int n = this.Tok.Count - 1;
            bool notHandled = this.OptionChoice(n);
            if (notHandled)
                notHandled = this.OptionValue(n);
            if (notHandled) {
                this.net.ExtraOptions[this.Tok[0]] = this.Tok[1];
            }
        }

        ///<summary>Handles options that are choice values, such as quality type, for example.</summary>
        /// <param name="n">number of tokens</param>
        /// <returns><c>true</c> is it didn't handle the option.</returns>
        private bool OptionChoice(int n) {
            
            if (n < 0)
                throw new InputException(ErrorCode.Err201, SectType.OPTIONS, this.line);

            if (this.Tok[0].Match(Keywords.w_UNITS)) {
                FlowUnitsType type;

                if (n < 1)
                    return false;

                if (!EnumsTxt.TryParse(this.Tok[1], out type))
                    throw new InputException(ErrorCode.Err201, SectType.OPTIONS, this.line);

                this.net.FlowFlag = type;
            }
            else if (this.Tok[0].Match(Keywords.w_PRESSURE)) {
                if (n < 1) return false;
                PressUnitsType value;

                if (!EnumsTxt.TryParse(this.Tok[1], out value)) {
                    throw new InputException(ErrorCode.Err201, SectType.OPTIONS, this.line);
                }

                this.net.PressFlag = value;
            }
            else if (this.Tok[0].Match(Keywords.w_HEADLOSS)) {
                if (n < 1) return false;
                FormType value;

                if (!EnumsTxt.TryParse(this.Tok[1], out value))
                    throw new InputException(ErrorCode.Err201, SectType.OPTIONS, this.line);

                this.net.FormFlag = value;
            }
            else if (this.Tok[0].Match(Keywords.w_HYDRAULIC)) {
                if (n < 2) return false;
                HydType value;

                if (!EnumsTxt.TryParse(this.Tok[1], out value))
                    throw new InputException(ErrorCode.Err201, SectType.OPTIONS, this.line);

                this.net.HydFlag = value;
                this.net.HydFname = this.Tok[2];
            }
            else if (this.Tok[0].Match(Keywords.w_QUALITY)) {
                QualType type;

                if (n < 1)
                    return false;

                this.net.QualFlag = EnumsTxt.TryParse(this.Tok[1], out type) 
                    ? type
                    : QualType.CHEM;

                switch (this.net.QualFlag) {
                case QualType.CHEM:
                    this.net.ChemName = this.Tok[1];
                    if(n >= 2)
                        this.net.ChemUnits = this.Tok[2];
                    break;

                case QualType.TRACE:

                    this.Tok[0] = "";
                    if (n < 2)
                        throw new InputException(ErrorCode.Err212, SectType.OPTIONS, this.line);

                    this.Tok[0] = this.Tok[2];
                    Node node = this.net.GetNode(this.Tok[2]);

                    if (node == null)
                        throw new InputException(ErrorCode.Err212, SectType.OPTIONS, this.line);

                    this.net.TraceNode = node.Name;
                    this.net.ChemName = Keywords.u_PERCENT;
                    this.net.ChemUnits = this.Tok[2];
                    break;

                case QualType.AGE:
                    this.net.ChemName = Keywords.w_AGE;
                    this.net.ChemUnits = Keywords.u_HOURS;
                    break;
                }

            }
            else if (this.Tok[0].Match(Keywords.w_MAP)) {
                if (n < 1)
                    return false;

                this.net.MapFname = this.Tok[1];
            }
            else if (this.Tok[0].Match(Keywords.w_UNBALANCED)) {
                if (n < 1)
                    return false;

                if (this.Tok[1].Match(Keywords.w_STOP)) {
                    this.net.ExtraIter = -1;
                }
                else if (this.Tok[1].Match(Keywords.w_CONTINUE)) {
                    if (n >= 2) {
                        double d;

                        if (!this.Tok[2].ToDouble(out d))
                            throw new InputException(ErrorCode.Err201, SectType.OPTIONS, this.line);

                        this.net.ExtraIter = (int)d;
                    }
                    else
                        this.net.ExtraIter = 0;
                }
                else {
                    throw new InputException(ErrorCode.Err201, SectType.OPTIONS, this.line);
                }
            }
            else if (this.Tok[0].Match(Keywords.w_PATTERN)) {
                if (n < 1)
                    return false;

                this.net.DefPatId = this.Tok[1];
            }
            else
                return true;
            return false;
        }

        private bool OptionValue(int n) {
            string name = this.Tok[0];

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
            foreach (string k  in  OptionValueKeywords) {
                if (name.Match(k)) {
                    keyword = k;
                    break;
                }
            }

            if (keyword == null) return true;
            name = keyword;

            double y;

            if (!this.Tok[nvalue].ToDouble(out y))
                throw new InputException(ErrorCode.Err213, SectType.OPTIONS, this.line);

            if (name.Match(Keywords.w_TOLERANCE)) {
                if (y < 0.0)
                    throw new InputException(ErrorCode.Err213, SectType.OPTIONS, this.line);

                this.net.Ctol = y;
                return false;
            }

            if (name.Match(Keywords.w_DIFFUSIVITY)) {
                if (y < 0.0)
                    throw new InputException(ErrorCode.Err213, SectType.OPTIONS, this.line);

                this.net.Diffus = y;
                return false;
            }

            if (name.Match(Keywords.w_DAMPLIMIT)) {
                this.net.DampLimit = y;
                return false;
            }

            if(y <= 0.0)
                throw new InputException(ErrorCode.Err213, SectType.OPTIONS, this.line);

            if (name.Match(Keywords.w_VISCOSITY)) this.net.Viscos = y;
            else if (name.Match(Keywords.w_SPECGRAV)) this.net.SpGrav = y;
            else if (name.Match(Keywords.w_TRIALS)) this.net.MaxIter = (int)y;
            else if (name.Match(Keywords.w_ACCURACY)) {
                y = Math.Max(y, 1e-5);
                y = Math.Min(y, 1e-1);
                this.net.HAcc = y;
            }
            else if (name.Match(Keywords.w_HTOL)) this.net.HTol = y;
            else if (name.Match(Keywords.w_QTOL)) this.net.QTol = y;
            else if (name.Match(Keywords.w_RQTOL)) {

                if(y >= 1.0)
                    throw new InputException(ErrorCode.Err213, SectType.OPTIONS, this.line);

                this.net.RQtol = y;
            }
            else if (name.Match(Keywords.w_CHECKFREQ)) this.net.CheckFreq = (int)y;
            else if (name.Match(Keywords.w_MAXCHECK)) this.net.MaxCheck = (int)y;
            else if (name.Match(Keywords.w_EMITTER)) this.net.QExp = 1.0 / y;
            else if (name.Match(Keywords.w_DEMAND)) this.net.DMult = y;

            return false;
        }

        private void ParseTime() {
            int n = this.Tok.Count - 1;
            double y;
            
            if (n < 1)
                throw new InputException(ErrorCode.Err201, SectType.TIMES, this.line);

            if (this.Tok[0].Match(Keywords.w_STATISTIC)) {
                TStatType type;

                if (!EnumsTxt.TryParse(this.Tok[n], out type))
                    throw new InputException(ErrorCode.Err201, SectType.TIMES, this.line);

                this.net.TStatFlag = type;
    
                return;
            }

            if (!this.Tok[n].ToDouble(out y)) {
                if ((y = Utilities.GetHour(this.Tok[n])) < 0.0) {
                    if ((y = Utilities.GetHour(this.Tok[n - 1], this.Tok[n])) < 0.0)
                        throw new InputException(ErrorCode.Err213, SectType.TIMES, this.line);
                }
            }

            var t = (long)(3600.0 * y);

            if (this.Tok[0].Match(Keywords.w_DURATION))
                this.net.Duration = t;
            else if (this.Tok[0].Match(Keywords.w_HYDRAULIC))
                this.net.HStep = t;
            else if (this.Tok[0].Match(Keywords.w_QUALITY))
                this.net.QStep = t;
            else if (this.Tok[0].Match(Keywords.w_RULE))
                this.net.RuleStep = t;
            else if (this.Tok[0].Match(Keywords.w_MINIMUM)) {
                
            }
            else if (this.Tok[0].Match(Keywords.w_PATTERN)) {
                if (this.Tok[1].Match(Keywords.w_TIME))
                    this.net.PStep = t;
                else if (this.Tok[1].Match(Keywords.w_START))
                    this.net.PStart = t;
                else
                    throw new InputException(ErrorCode.Err201, SectType.TIMES, this.line);
            }
            else if (this.Tok[0].Match(Keywords.w_REPORT)) {
                if (this.Tok[1].Match(Keywords.w_TIME))
                    this.net.RStep = t;
                else if (this.Tok[1].Match(Keywords.w_START))
                    this.net.RStart = t;
                else
                    throw new InputException(ErrorCode.Err201, SectType.TIMES, this.line);
            }
            else if (this.Tok[0].Match(Keywords.w_START))
                this.net.TStart = t % Constants.SECperDAY;

            else
                throw new InputException(ErrorCode.Err201, SectType.TIMES, this.line);

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
            int n = this.Tok.Count;
            double y;
            Pattern pat = null;

            if (n < 2)
                throw new InputException(ErrorCode.Err201, SectType.DEMANDS, this.line);

            if (!this.Tok[1].ToDouble(out y))
                throw new InputException(ErrorCode.Err202, SectType.DEMANDS, this.Tok[0]);

            if (this.Tok[0].Match(Keywords.w_MULTIPLY)) {
                if (y <= 0.0)
                    throw new InputException(ErrorCode.Err202, SectType.DEMANDS, this.Tok[0]);

                this.net.DMult = y;
                return;
            }

            Node node  = this.net.GetNode(this.Tok[0]);

            if(node == null || node.Type != NodeType.JUNC)
                throw new InputException(ErrorCode.Err208, SectType.DEMANDS, this.Tok[0]);

            if (n >= 3) {
                pat = this.net.GetPattern(this.Tok[2]);
                if (pat == null)
                    throw new InputException(ErrorCode.Err205, SectType.DEMANDS, this.line);
            }

            node.Demands.Add(new Demand(y, pat));

        }

        private void ParseRule() {
            Rulewords key;
            EnumsTxt.TryParse(this.Tok[0], out key);
            if (key == Rulewords.RULE) {
                this.currentRule = new Rule(this.Tok[1]);
                this.net.Rules.Add(this.currentRule);
            }
            else if (this.currentRule != null) {
                this.currentRule.Code.Add(this.line);
            }

        }

    }

}