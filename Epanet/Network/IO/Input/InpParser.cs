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

        private int lineNumber;
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

        protected void LogException(SectType section, ErrorCode err, string line, IList<string> tokens) {
            if(err == ErrorCode.Ok)
                return;

            string arg = section == SectType.OPTIONS ? line : tokens[0];

            EpanetParseException parseException = new EpanetParseException(
                err,
                this.lineNumber,
                this.FileName,
                section.ReportStr(),
                arg);

            base.Errors.Add(parseException);

            this.Log.Error(parseException.ToString());

        }

        /// <summary>Parse demands and time patterns first.</summary>
        /// <param name="net"></param>
        /// <param name="f"></param>

        private void ParsePc(Network net, string f) {
            this.lineNumber = 0;
            SectType sectionType = (SectType)(-1);
            StreamReader buffReader;

            try {
                buffReader = new StreamReader(f, Encoding.Default); // "ISO-8859-1"
            }
            catch (IOException) {
                throw new ENException(ErrorCode.Err302);
            }

            try {
                string line;
                while ((line = buffReader.ReadLine()) != null) {
                    this.lineNumber++;

                    line = line.Trim();

                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (line[0] == '[') {
                        if (line.StartsWith("[PATTERNS]")) {
                            sectionType = SectType.PATTERNS;
                        }
                        else if (line.StartsWith("[CURVES]"))
                            sectionType = SectType.CURVES;
                        else
                            sectionType = (SectType)(-1);
                        continue;
                    }

                    if (sectionType != (SectType)(-1)) {
                        if (line.IndexOf(';') >= 0)
                            line = line.Substring(0, line.IndexOf(';'));

                        if (line.Length == 0)
                            continue;

                        string[] tokens = Tokenize(line);

                        if (tokens.Length == 0) continue;

                        try {
                            switch (sectionType) {
                            case SectType.PATTERNS:
                                this.ParsePattern(net, tokens);
                                break;
                            case SectType.CURVES:
                                this.ParseCurve(net, tokens);
                                break;
                            }
                        }
                        catch (ENException e) {
                            this.LogException(sectionType, e.Code, line, tokens);
                        }
                    }

                    if (this.Errors.Count == Constants.MAXERRS) break;
                }
            }
            catch (IOException) {
                throw new ENException(ErrorCode.Err302);
            }

            if (this.Errors.Count > 0)
                throw new ENException(ErrorCode.Err200);

            try {
                buffReader.Close();
            }
            catch (IOException) {
                throw new ENException(ErrorCode.Err302);
            }
        }

        // Parse INP file
        public override Network Parse(Network net, string f) {
            this.FileName = Path.GetFullPath(f);

            this.ParsePc(net, f);

            int errSum = 0;
            //int lineCount = 0;
            SectType sectionType = (SectType)(-1);
            TextReader buffReader;

            try {
                buffReader = new StreamReader(f, Encoding.Default); // "ISO-8859-1"
            }
            catch (IOException) {
                throw new ENException(ErrorCode.Err302);
            }

            try {
                string line;
                while ((line = buffReader.ReadLine()) != null) {
                    string comment = "";

                    int index = line.IndexOf(';');

                    if (index >= 0) {
                        if (index > 0)
                            comment = line.Substring(index + 1).Trim();

                        line = line.Substring(0, index);
                    }


                    //lineCount++;
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    string[] tokens = Tokenize(line);
                    if (tokens.Length == 0) continue;

                    try {

                        if (tokens[0].IndexOf('[') >= 0) {
                            SectType type = FindSectionType(tokens[0]);
                            if (type >= 0)
                                sectionType = type;
                            else {
                                sectionType = (SectType)(-1);
                                this.Log.Error(null, null, string.Format("Unknown section type : {0}", tokens[0]));
                                //throw new ENException(201, lineCount);
                            }
                        }
                        else if (sectionType >= 0) {

                            switch (sectionType) {
                            case SectType.TITLE:
                                net.TitleText.Add(line);
                                break;
                            case SectType.JUNCTIONS:
                                ParseJunction(net, tokens, comment);
                                break;

                            case SectType.RESERVOIRS:
                            case SectType.TANKS:
                                ParseTank(net, tokens, comment);
                                break;

                            case SectType.PIPES:
                                this.ParsePipe(net, tokens, comment);
                                break;
                            case SectType.PUMPS:
                                this.ParsePump(net, tokens, comment);
                                break;
                            case SectType.VALVES:
                                this.ParseValve(net, tokens, comment);
                                break;
                            case SectType.CONTROLS:
                                this.ParseControl(net, tokens);
                                break;

                            case SectType.RULES:
                                this.ParseRule(net, tokens, line);
                                break;

                            case SectType.DEMANDS:
                                this.ParseDemand(net, tokens);
                                break;
                            case SectType.SOURCES:
                                this.ParseSource(net, tokens);
                                break;
                            case SectType.EMITTERS:
                                this.ParseEmitter(net, tokens);
                                break;
                            case SectType.QUALITY:
                                ParseQuality(net, tokens);
                                break;
                            case SectType.STATUS:
                                this.ParseStatus(net, tokens);
                                break;
                            case SectType.ENERGY:
                                this.ParseEnergy(net, tokens);
                                break;
                            case SectType.REACTIONS:
                                this.ParseReact(net, tokens);
                                break;
                            case SectType.MIXING:
                                this.ParseMixing(net, tokens);
                                break;
                            case SectType.REPORT:
                                this.ParseReport(net, tokens);
                                break;
                            case SectType.TIMES:
                                this.ParseTime(net, tokens);
                                break;
                            case SectType.OPTIONS:
                                this.ParseOption(net, tokens);
                                break;
                            case SectType.COORDINATES:
                                this.ParseCoordinate(net, tokens);
                                break;
                            case SectType.VERTICES:
                                this.ParseVertice(net, tokens);
                                break;
                            case SectType.LABELS:
                                this.ParseLabel(net, tokens);
                                break;
                            }
                        }
                    }
                    catch (ENException e) {
                        this.LogException(sectionType, e.Code, line, tokens);
                        errSum++;
                    }
                    if (errSum == Constants.MAXERRS) break;
                }
            }
            catch (IOException) {
                throw new ENException(ErrorCode.Err302);
            }

            if (errSum > 0) {
                throw new ENException(ErrorCode.Err200);
            }

            try {
                buffReader.Close();
            }
            catch (IOException) {
                throw new ENException(ErrorCode.Err302);
            }

            AdjustData(net);

            net.FieldsMap.Prepare(
                   net.UnitsFlag,
                   net.FlowFlag,
                   net.PressFlag,
                   net.QualFlag,
                   net.ChemUnits,
                   net.SpGrav,
                   net.HStep);

            this.Convert(net);
            return net;
        }

        protected static void ParseJunction(Network net, string[] tok, string comment) {
            int n = tok.Length;
            double el, y = 0.0d;
            Pattern p = null;

            if (net.GetNode(tok[0]) != null)
                throw new ENException(ErrorCode.Err215, SectType.JUNCTIONS, tok[0]);

            Node nodeRef = new Node(tok[0]);

            net.Nodes.Add(nodeRef);

            if (n < 2)
                throw new ENException(ErrorCode.Err201);

            if (!tok[1].ToDouble(out el))
                throw new ENException(ErrorCode.Err202, SectType.JUNCTIONS, tok[0]);

            if (n >= 3 && !tok[2].ToDouble(out y))
                throw new ENException(ErrorCode.Err202, SectType.JUNCTIONS, tok[0]);

            if (n >= 4) {
                p = net.GetPattern(tok[3]);
                if (p == null)
                    throw new ENException(ErrorCode.Err205);
            }

            nodeRef.Elevation = el;
            nodeRef.C0 = 0.0;
            nodeRef.QualSource = null;
            nodeRef.Ke = 0.0;
            nodeRef.RptFlag = false;

            if (!string.IsNullOrEmpty(comment))
                nodeRef.Comment = comment;

            if (n >= 3) {
                Demand demand = new Demand(y, p);
                nodeRef.Demand.Add(demand);

                nodeRef.InitDemand = y;
            }
            else
                nodeRef.InitDemand = Constants.MISSING;
        }


        protected static void ParseTank(Network net, string[] tok, string comment) {
            int n = tok.Length;
            Pattern p = null;
            Curve c = null;
            double el,
                   initlevel = 0.0d,
                   minlevel = 0.0d,
                   maxlevel = 0.0d,
                   minvol = 0.0d,
                   diam = 0.0d,
                   area;

            if (net.GetNode(tok[0]) != null)
                throw new ENException(ErrorCode.Err215);

            Tank tank = new Tank(tok[0]);
            
            if(comment.Length > 0)
                tank.Comment = comment;

            net.Nodes.Add(tank);

            if (n < 2)
                throw new ENException(ErrorCode.Err201);

            if (!tok[1].ToDouble(out el))
                throw new ENException(ErrorCode.Err202);

            if (n <= 3) {
                if (n == 3) {
                    p = net.GetPattern(tok[2]);
                    if (p == null)
                        throw new ENException(ErrorCode.Err205);
                }
            }
            else if (n < 6)
                throw new ENException(ErrorCode.Err201);
            else {
                if (!tok[2].ToDouble(out initlevel))
                    throw new ENException(ErrorCode.Err202, SectType.TANKS, tok[0]);

                if (!tok[3].ToDouble(out minlevel))
                    throw new ENException(ErrorCode.Err202, SectType.TANKS, tok[0]);

                if (!tok[4].ToDouble(out maxlevel))
                    throw new ENException(ErrorCode.Err202, SectType.TANKS, tok[0]);
                if (!tok[5].ToDouble(out diam))
                    throw new ENException(ErrorCode.Err202, SectType.TANKS, tok[0]);

                if (diam < 0.0)
                    throw new ENException(ErrorCode.Err202, SectType.TANKS, tok[0]);

                if (n >= 7
                    && !tok[6].ToDouble(out minvol))
                    throw new ENException(ErrorCode.Err202, SectType.TANKS, tok[0]);

                if (n == 8) {
                    c = net.GetCurve(tok[7]);
                    if (c == null)
                        throw new ENException(ErrorCode.Err202, SectType.TANKS, tok[0]);
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

            area = Math.PI * diam * diam / 4.0d;

            tank.Vmin = area * minlevel;
            if (minvol > 0.0)
                tank.Vmin = minvol;

            tank.V0 = tank.Vmin + area * (initlevel - minlevel);
            tank.Vmax = tank.Vmin + area * (maxlevel - minlevel);

            tank.Vcurve = c;
            tank.MixModel = MixType.MIX1;
            tank.V1Max = 1.0;
        }


        protected void ParsePipe(Network net, string[] tok, string comment) {
            Node j1, j2;
            int n = tok.Length;
            LinkType type = LinkType.PIPE;
            StatType status = StatType.OPEN;
            double length, diam, rcoeff, lcoeff = 0.0d;

            if (net.GetLink(tok[0]) != null)
                throw new ENException(ErrorCode.Err215);

            Link link = new Link(tok[0]);
            net.Links.Add(link);
            
            if (n < 6)
                throw new ENException(ErrorCode.Err201);

            if ((j1 = net.GetNode(tok[1])) == null ||
                (j2 = net.GetNode(tok[2])) == null
            ) throw new ENException(ErrorCode.Err203);


            if (j1.Equals(j2)) throw new ENException(ErrorCode.Err222);

            if (!tok[3].ToDouble(out length) ||
                !tok[4].ToDouble(out diam) ||
                !tok[5].ToDouble(out rcoeff)
            ) throw new ENException(ErrorCode.Err202);


            if (length <= 0.0 || diam <= 0.0 || rcoeff <= 0.0) throw new ENException(ErrorCode.Err202);

            if (n == 7) {
                if (tok[6].Match(LinkType.CV.ParseStr())) type = LinkType.CV;
                else if (tok[6].Match(StatType.CLOSED.ParseStr())) status = StatType.CLOSED;
                else if (tok[6].Match(StatType.OPEN.ParseStr())) status = StatType.OPEN;
                else if (!tok[6].ToDouble(out lcoeff)) throw new ENException(ErrorCode.Err202);
            }

            if (n == 8) {
                if (!tok[6].ToDouble(out lcoeff)) throw new ENException(ErrorCode.Err202);
                if (tok[7].Match(LinkType.CV.ParseStr())) type = LinkType.CV;
                else if (tok[7].Match(StatType.CLOSED.ParseStr())) status = StatType.CLOSED;
                else if (tok[7].Match(StatType.OPEN.ParseStr())) status = StatType.OPEN;
                else
                    throw new ENException(ErrorCode.Err202);
            }

            if (lcoeff < 0.0) throw new ENException(ErrorCode.Err202);

            link.FirstNode = j1;
            link.SecondNode = j2;
            link.Lenght = length;
            link.Diameter = diam;
            link.Roughness = rcoeff;
            link.Km = lcoeff;
            link.Kb = Constants.MISSING;
            link.Kw = Constants.MISSING;
            link.Type = type;
            link.Status = status;
            link.RptFlag = false;

            if (!string.IsNullOrEmpty(comment))
                link.Comment = comment;
        }


        protected void ParsePump(Network net, string[] tok, string comment) {
            int m, n = tok.Length;
            Node j1, j2;
            double[] x = new double[6];

            if (net.GetLink(tok[0]) != null)
                throw new ENException(ErrorCode.Err215);

            Pump pump = new Pump(tok[0]);

            net.Links.Add(pump);

            if (n < 4) throw new ENException(ErrorCode.Err201);

            if ((j1 = net.GetNode(tok[1])) == null || (j2 = net.GetNode(tok[2])) == null)
                throw new ENException(ErrorCode.Err203);

            if (j1.Equals(j2)) throw new ENException(ErrorCode.Err222);

            // Link attributes
            pump.FirstNode = j1;
            pump.SecondNode = j2;
            pump.Diameter = 0;
            pump.Lenght = 0.0d;
            pump.Roughness = 1.0d;
            pump.Km = 0.0d;
            pump.Kb = 0.0d;
            pump.Kw = 0.0d;
            pump.Type = LinkType.PUMP;
            pump.Status = StatType.OPEN;
            pump.RptFlag = false;

            // Pump attributes
            pump.Ptype = PumpType.NOCURVE;
            pump.HCurve = null;
            pump.ECurve = null;
            pump.UPat = null;
            pump.ECost = 0.0d;
            pump.EPat = null;

            if (comment.Length > 0) pump.Comment = comment;

            if (tok[3].ToDouble(out x[0])) {

                m = 1;
                for (int j = 4; j < n; j++) {
                    if (!tok[j].ToDouble(out x[m])) throw new ENException(ErrorCode.Err202);
                    m++;
                }
                GetPumpCurve(tok, pump, m, x);
                return;
                    /* If 4-th token is a number then input follows Version 1.x format  so retrieve pump curve parameters */

            }

            m = 4;
            while (m < n) {
                if (tok[m - 1].Match(Keywords.w_POWER)) {
                    double y;
                    if (!tok[m].ToDouble(out y) || y <= 0.0) throw new ENException(ErrorCode.Err202);
                    pump.Ptype = PumpType.CONST_HP;
                    pump.Km = y;
                }
                else if (tok[m - 1].Match(Keywords.w_HEAD)) {
                    Curve t = net.GetCurve(tok[m]);
                    if (t == null) throw new ENException(ErrorCode.Err206);
                    pump.HCurve = t;
                }
                else if (tok[m - 1].Match(Keywords.w_PATTERN)) {
                    Pattern p = net.GetPattern(tok[m]);
                    if (p == null) throw new ENException(ErrorCode.Err205);
                    pump.UPat = p;
                }
                else if (tok[m - 1].Match(Keywords.w_SPEED)) {
                    double y;
                    if (!tok[m].ToDouble(out y)) throw new ENException(ErrorCode.Err202);
                    if (y < 0.0) throw new ENException(ErrorCode.Err202);
                    pump.Roughness = y;
                }
                else
                    throw new ENException(ErrorCode.Err201);

                m = m + 2;
            }
        }


        protected void ParseValve(Network net, string[] tok, string comment) {
            Node j1, j2;
            int n = tok.Length;
            StatType status = StatType.ACTIVE;
            LinkType type;

            double diam, setting, lcoeff = 0.0;

            if (net.GetLink(tok[0]) != null)
                throw new ENException(ErrorCode.Err215);

            Valve valve = new Valve(tok[0]);
            net.Links.Add(valve);

            if (n < 6) throw new ENException(ErrorCode.Err201);
            if ((j1 = net.GetNode(tok[1])) == null ||
                (j2 = net.GetNode(tok[2])) == null
            ) throw new ENException(ErrorCode.Err203);

            if (j1.Equals(j2))
                throw new ENException(ErrorCode.Err222);

            if (!EnumsTxt.TryParse(tok[4], out type))
                throw new ENException(ErrorCode.Err201);

            if(!tok[3].ToDouble(out diam) || diam <= 0.0) {
                throw new ENException(ErrorCode.Err202);
            }

            if (type == LinkType.GPV) {
                Curve t = net.GetCurve(tok[5]);
                if (t == null) throw new ENException(ErrorCode.Err206);

                setting = net.Curves.IndexOf(t);
                this.Log.Warning("GPV Valve, index as roughness !");
                valve.Curve = t;
                status = StatType.OPEN;
            }
            else if (!tok[5].ToDouble(out setting)) {
                throw new ENException(ErrorCode.Err202);
            }

            if (n >= 7) {
                if (!tok[6].ToDouble(out lcoeff)) {
                    throw new ENException(ErrorCode.Err202);
                }
            }

            if ((j1 is Tank || j2 is Tank) &&
                (type == LinkType.PRV || type == LinkType.PSV || type == LinkType.FCV))
                throw new ENException(ErrorCode.Err219);

            if (!Valvecheck(net, type, j1, j2))
                throw new ENException(ErrorCode.Err220);


            valve.FirstNode = j1;
            valve.SecondNode = j2;
            valve.Diameter = diam;
            valve.Lenght = 0.0d;
            valve.Roughness = setting;
            valve.Km = lcoeff;
            valve.Kb = 0.0d;
            valve.Kw = 0.0d;
            valve.Type = type;
            valve.Status = status;
            valve.RptFlag = false;
            if (comment.Length > 0)
                valve.Comment = comment;
        }

        private static bool Valvecheck(Network net, LinkType type, Node j1, Node j2) {
            // Examine each existing valve
            foreach (Valve vk  in  net.Valves) {
                Node vj1 = vk.FirstNode;
                Node vj2 = vk.SecondNode;
                LinkType vtype = vk.Type;

                if (vtype == LinkType.PRV && type == LinkType.PRV) {
                    if (Equals(vj2, j2) ||
                        Equals(vj2, j1) ||
                        Equals(vj1, j2)) return (false);
                }

                if (vtype == LinkType.PSV && type == LinkType.PSV) {
                    if (Equals(vj1, j1) ||
                        Equals(vj1, j2) ||
                        Equals(vj2, j1)) return (false);
                }

                if (vtype == LinkType.PSV && type == LinkType.PRV && vj1 == j2) return (false);
                if (vtype == LinkType.PRV && type == LinkType.PSV && vj2 == j1) return (false);

                if (vtype == LinkType.FCV && type == LinkType.PSV && vj2 == j1) return (false);
                if (vtype == LinkType.FCV && type == LinkType.PRV && vj1 == j2) return (false);

                if (vtype == LinkType.PSV && type == LinkType.FCV && vj1 == j2) return (false);
                if (vtype == LinkType.PRV && type == LinkType.FCV && vj2 == j1) return (false);
            }
            return (true);
        }

        private static void GetPumpCurve(string[] tok, Pump pump, int n, double[] x) {
            

            if (n == 1) {
                if (x[0] <= 0.0) throw new ENException(ErrorCode.Err202);
                pump.Ptype = PumpType.CONST_HP;
                pump.Km = x[0];
            }
            else {
                double h0, h1, h2, q1, q2;

                if (n == 2) {
                    q1 = x[1];
                    h1 = x[0];
                    h0 = 1.33334 * h1;
                    q2 = 2.0 * q1;
                    h2 = 0.0;
                }
                else if (n >= 5) {
                    h0 = x[0];
                    h1 = x[1];
                    q1 = x[2];
                    h2 = x[3];
                    q2 = x[4];
                }
                else 
                    throw new ENException(ErrorCode.Err202);

                pump.Ptype = PumpType.POWER_FUNC;
                double a, b, c;
              
                if (!GetPowerCurve(h0, h1, h2, q1, q2, out a, out b, out c))
                    throw new ENException(ErrorCode.Err206);

                pump.H0 = -a;
                pump.FlowCoefficient = -b;
                pump.N = c;
                pump.Q0 = q1;
                pump.Qmax = Math.Pow(-a / b, 1.0 / c);
                pump.Hmax = h0;
            }
        }

        protected void ParsePattern(Network net, string[] tok) {
            Pattern pat = net.GetPattern(tok[0]);

            if (pat == null) {
                pat = new Pattern(tok[0]);
                net.Patterns.Add(pat);
            }

            for (int i = 1; i < tok.Length; i++) {
                double x;

                if (!tok[i].ToDouble(out x))
                    throw new ENException(ErrorCode.Err202);

                pat.Add(x);
            }
        }

        protected void ParseCurve(Network net, string[] tok) {
            Curve cur = net.GetCurve(tok[0]);

            if (cur == null) {
                cur = new Curve(tok[0]);
                net.Curves.Add(cur);
            }

            double x, y;

            if (!tok[1].ToDouble(out x) || !tok[2].ToDouble(out y))
                throw new ENException(ErrorCode.Err202);

            cur.Add(x, y);
        }

        protected void ParseCoordinate(Network net, string[] tok) {
            if (tok.Length < 3)
                throw new ENException(ErrorCode.Err201);

            Node nodeRef = net.GetNode(tok[0]);

            if (nodeRef == null)
                throw new ENException(ErrorCode.Err203);

            double x, y;

            if (!tok[1].ToDouble(out x) || !tok[2].ToDouble(out y))
                throw new ENException(ErrorCode.Err202);

            nodeRef.Position = new EnPoint(x, y);
        }

        protected void ParseLabel(Network net, string[] tok) {
            if (tok.Length < 3)
                throw new ENException(ErrorCode.Err201);

            Label l = new Label();
            double x;
            double y;

            if (!tok[0].ToDouble(out x) || !tok[1].ToDouble(out y))
                throw new ENException(ErrorCode.Err202);

            l.Position = new EnPoint(x, y);
            //if (tok[2].length() > 1)
            //    l.setText(tok[2].substring(1, tok[2].length() - 1));
            for (int i = 2; i < tok.Length; i++)
                if (l.Text.Length == 0)
                    l.Text = tok[i].Replace("\"", "");
                else
                    l.Text = l.Text + " " + tok[i].Replace("\"", "");

            net.Labels.Add(l);
        }

        protected void ParseVertice(Network net, string[] tok) {
            if (tok.Length < 3)
                throw new ENException(ErrorCode.Err201);

            Link linkRef = net.GetLink(tok[0]);

            if (linkRef == null)
                throw new ENException(ErrorCode.Err204);

            double x;
            double y;

            if (!tok[1].ToDouble(out x) || !tok[2].ToDouble(out y))
                throw new ENException(ErrorCode.Err202);

            linkRef.Vertices.Add(new EnPoint(x, y));
        }

        protected void ParseControl(Network net, string[] tok) {
            int n = tok.Length;
            StatType status = StatType.ACTIVE;

            double setting = Constants.MISSING, time = 0.0, level = 0.0;

            if (n < 6)
                throw new ENException(ErrorCode.Err201);

            Node nodeRef = null;
            Link linkRef = net.GetLink(tok[1]);

            if (linkRef == null) throw new ENException(ErrorCode.Err204);

            LinkType ltype = linkRef.Type;

            if (ltype == LinkType.CV) throw new ENException(ErrorCode.Err207);

            if (tok[2].Match(StatType.OPEN.ParseStr())) {
                status = StatType.OPEN;
                if (ltype == LinkType.PUMP) setting = 1.0;
                if (ltype == LinkType.GPV) setting = linkRef.Roughness;
            }
            else if (tok[2].Match(StatType.CLOSED.ParseStr())) {
                status = StatType.CLOSED;
                if (ltype == LinkType.PUMP) setting = 0.0;
                if (ltype == LinkType.GPV) setting = linkRef.Roughness;
            }
            else if (ltype == LinkType.GPV)
                throw new ENException(ErrorCode.Err206);
            else if (!tok[2].ToDouble(out setting))
                throw new ENException(ErrorCode.Err202);

            if (ltype == LinkType.PUMP || ltype == LinkType.PIPE) {
                if (!setting.IsMissing()) {
                    if (setting < 0.0) throw new ENException(ErrorCode.Err202);
                    else if (setting == 0.0) status = StatType.CLOSED;
                    else status = StatType.OPEN;
                }
            }

            ControlType ctype;

            if (tok[4].Match(Keywords.w_TIME))
                ctype = ControlType.TIMER;
            else if (tok[4].Match(Keywords.w_CLOCKTIME))
                ctype = ControlType.TIMEOFDAY;
            else {
                if (n < 8)
                    throw new ENException(ErrorCode.Err201);
                if ((nodeRef = net.GetNode(tok[5])) == null)
                    throw new ENException(ErrorCode.Err203);
                if (tok[6].Match(Keywords.w_BELOW)) ctype = ControlType.LOWLEVEL;
                else if (tok[6].Match(Keywords.w_ABOVE)) ctype = ControlType.HILEVEL;
                else
                    throw new ENException(ErrorCode.Err201);
            }

            switch (ctype) {
            case ControlType.TIMER:
            case ControlType.TIMEOFDAY:
                if (n == 6) time = Utilities.GetHour(tok[5]);
                if (n == 7) time = Utilities.GetHour(tok[5], tok[6]);
                if (time < 0.0) throw new ENException(ErrorCode.Err201);
                break;
            case ControlType.LOWLEVEL:
            case ControlType.HILEVEL:
                if (!tok[7].ToDouble(out level)) {
                    throw new ENException(ErrorCode.Err202);
                }
                break;
            }

            Control cntr = new Control();
            cntr.Link = linkRef;
            cntr.Node = nodeRef;
            cntr.Type = ctype;
            cntr.Status = status;
            cntr.Setting = setting;
            cntr.Time = (long)(3600.0 * time);
            if (ctype == ControlType.TIMEOFDAY)
                cntr.Time = cntr.Time % Constants.SECperDAY;
            cntr.Grade = level;

            net.Controls.Add(cntr);
        }


        protected void ParseSource(Network net, string[] tok) {
            int n = tok.Length;
            SourceType type;
            double c0;
            Pattern pat = null;
            Node nodeRef;

            if (n < 2) throw new ENException(ErrorCode.Err201);
            if ((nodeRef = net.GetNode(tok[0])) == null) throw new ENException(ErrorCode.Err203);

            int i = 2;

            if (!EnumsTxt.TryParse(tok[1], out type))
                i = 1;

            //if (Utilities.match(Tok[1], Keywords.w_CONCEN)) type = Source.Type.CONCEN;
            //else if (Utilities.match(Tok[1], Keywords.w_MASS)) type = Source.Type.MASS;
            //else if (Utilities.match(Tok[1], Keywords.w_SETPOINT)) type = Source.Type.SETPOINT;
            //else if (Utilities.match(Tok[1], Keywords.w_FLOWPACED)) type = Source.Type.FLOWPACED;
            //else i = 1;

            if (!tok[i].ToDouble(out c0)) {
                throw new ENException(ErrorCode.Err202);
            }

            if (n > i + 1 && tok[i + 1].Length > 0 && !tok[i + 1].Equals("*", StringComparison.Ordinal)) {
                pat = net.GetPattern(tok[i + 1]);
                if (pat == null) throw new ENException(ErrorCode.Err205);
            }

            QualSource src = new QualSource();

            src.C0 = c0;
            src.Pattern = pat;
            src.Type = type;

            nodeRef.QualSource = src;
        }


        protected void ParseEmitter(Network net, string[] tok) {
            int n = tok.Length;
            Node nodeRef;
            double k;

            if (n < 2) throw new ENException(ErrorCode.Err201);
            if ((nodeRef = net.GetNode(tok[0])) == null) throw new ENException(ErrorCode.Err203);
            if (nodeRef is Tank)
                throw new ENException(ErrorCode.Err209);

            if (!tok[1].ToDouble(out k)) {
                throw new ENException(ErrorCode.Err202);
            }

            if (k < 0.0)
                throw new ENException(ErrorCode.Err202);

            nodeRef.Ke = k;

        }


        protected static void ParseQuality(Network net, string[] tok) {
            int n = tok.Length;
            long i0 = 0, i1 = 0;
            double c0;

            if (n < 2) return;

            if (n == 2) {
                Node nodeRef;
                if ((nodeRef = net.GetNode(tok[0])) == null) return;
                if (!tok[1].ToDouble(out c0))
                    throw new ENException(ErrorCode.Err209);
                nodeRef.C0 = c0;
            }
            else {
                if (!tok[2].ToDouble(out c0)) {
                    throw new ENException(ErrorCode.Err209);
                }

                try {
                    i0 = long.Parse(tok[0]);
                    i1 = long.Parse(tok[1]);
                }
                finally {
                    if (i0 > 0 && i1 > 0) {
                        foreach (Node j  in  net.Nodes) {
                            double d;
                            if (double.TryParse(j.Name, out d)) {
                                long i = (long)d;
                                if (i >= i0 && i <= i1)
                                    j.C0 = c0;
                            }
                        }
                    }
                    else {
                        foreach (Node j  in  net.Nodes) {
                            if ((string.Compare(tok[0], j.Name, StringComparison.OrdinalIgnoreCase) <= 0) &&
                                (string.Compare(tok[1], j.Name, StringComparison.OrdinalIgnoreCase) >= 0))
                                j.C0 = c0;
                        }
                    }
                }
            }
        }

        protected void ParseReact(Network net, string[] tok) {
            int item, n = tok.Length;
            double y;

            if (n < 3) return;


            if (tok[0].Match(Keywords.w_ORDER)) {

                if (!tok[n - 1].ToDouble(out y)) {
                    throw new ENException(ErrorCode.Err213);
                }

                if (tok[1].Match(Keywords.w_BULK)) net.BulkOrder = y;
                else if (tok[1].Match(Keywords.w_TANK)) net.TankOrder = y;
                else if (tok[1].Match(Keywords.w_WALL)) {
                    if (y == 0.0) net.WallOrder = 0.0;
                    else if (y == 1.0) net.WallOrder = 1.0;
                    else throw new ENException(ErrorCode.Err213);
                }
                else throw new ENException(ErrorCode.Err213);
                return;
            }

            if (tok[0].Match(Keywords.w_ROUGHNESS)) {
                if (!tok[n - 1].ToDouble(out y)) {
                    throw new ENException(ErrorCode.Err213);
                }
                net.RFactor = y;
                return;
            }

            if (tok[0].Match(Keywords.w_LIMITING)) {
                if (!tok[n - 1].ToDouble(out y)) {
                    throw new ENException(ErrorCode.Err213);
                }
                net.CLimit = y;
                return;
            }

            if (tok[0].Match(Keywords.w_GLOBAL)) {
                if (!tok[n - 1].ToDouble(out y)) {
                    throw new ENException(ErrorCode.Err213);
                }
                if (tok[1].Match(Keywords.w_BULK)) net.KBulk = y;
                else if (tok[1].Match(Keywords.w_WALL)) net.KWall = y;
                else throw new ENException(ErrorCode.Err201);
                return;
            }

            if (tok[0].Match(Keywords.w_BULK)) item = 1;
            else if (tok[0].Match(Keywords.w_WALL)) item = 2;
            else if (tok[0].Match(Keywords.w_TANK)) item = 3;
            else throw new ENException(ErrorCode.Err201);

            tok[0] = tok[1];

            if (item == 3) {
                if (!tok[n - 1].ToDouble(out y)) {
                    throw new ENException(ErrorCode.Err209);
                }

                if (n == 3) {
                    Node nodeRef;
                    if ((nodeRef = net.GetNode(tok[1])) == null)
                        throw new ENException(ErrorCode.Err208); //if ((j = net.getNode(Tok[1])) <= juncsCount) return;
                    if (!(nodeRef is Tank)) return;
                    ((Tank)nodeRef).Kb = y; //net.getTanks()[j - juncsCount].setKb(y);
                }
                else {
                    long i1 = 0, i2 = 0;
                    try {
                        i1 = long.Parse(tok[1]);
                        i2 = long.Parse(tok[2]);
                    }
                    finally {
                        if (i1 > 0 && i2 > 0) {
                            foreach (Tank j  in  net.Tanks) {
                                long i = long.Parse(j.Name);
                                if (i >= i1 && i <= i2)
                                    j.Kb = y;
                            }
                        }
                        else {
                            foreach (Tank j  in  net.Tanks) {
                                if (string.Compare(tok[1], j.Name, StringComparison.Ordinal) <= 0 &&
                                    string.Compare(tok[2], j.Name, StringComparison.Ordinal) >= 0)
                                    j.Kb = y;
                            }
                        }
                    }
                }
            }
            else {
                if (!tok[n - 1].ToDouble(out y)) {
                    throw new ENException(ErrorCode.Err211);
                }

                if (net.Links.Count == 0) return;
                if (n == 3) {
                    Link linkRef;
                    if ((linkRef = net.GetLink(tok[1])) == null) return;
                    if (item == 1)
                        linkRef.Kb = y;
                    else
                        linkRef.Kw = y;
                }
                else {
                    long i1 = 0, i2 = 0;
                    try {
                        i1 = long.Parse(tok[1]);
                        i2 = long.Parse(tok[2]);
                    }
                    finally {
                        if (i1 > 0 && i2 > 0) {
                            foreach (Link j  in  net.Links) {
                                try {
                                    long i = long.Parse(j.Name);
                                    if (i >= i1 && i <= i2) {
                                        if (item == 1)
                                            j.Kb = y;
                                        else
                                            j.Kw = y;
                                    }
                                }
                                catch (Exception) {}
                            }
                        }
                        else
                            foreach (Link j  in  net.Links) {
                                if (string.Compare(tok[1], j.Name, StringComparison.Ordinal) <= 0 &&
                                    string.Compare(tok[2], j.Name, StringComparison.Ordinal) >= 0) {
                                    if (item == 1)
                                        j.Kb = y;
                                    else
                                        j.Kw = y;
                                }
                            }
                    }
                }
            }
        }


        protected void ParseMixing(Network net, string[] tok) {
            int n = tok.Length;
            MixType i;

            if (net.Nodes.Count == 0)
                throw new ENException(ErrorCode.Err208);

            if (n < 2) return;

            Node nodeRef = net.GetNode(tok[0]);
            if (nodeRef == null) throw new ENException(ErrorCode.Err208);
            if (!(nodeRef is Tank)) return;
            Tank tankRef = (Tank)nodeRef;

            if (!EnumsTxt.TryParse(tok[1], out i))
                throw new ENException(ErrorCode.Err201);

            var v = 1.0;
            if (i == MixType.MIX2 && n == 3) {
                if (!tok[2].ToDouble(out v)) {
                    throw new ENException(ErrorCode.Err209);
                }
            }

            if (v == 0.0)
                v = 1.0;

            if (tankRef.IsReservoir) return;
            tankRef.MixModel = i;
            tankRef.V1Max = v;
        }


        protected void ParseStatus(Network net, string[] tok) {
            int n = tok.Length - 1;
            double y = 0.0;
            StatType status = StatType.ACTIVE;

            if (net.Links.Count == 0) throw new ENException(ErrorCode.Err210);

            if (n < 1) throw new ENException(ErrorCode.Err201);

            if (tok[n].Match(Keywords.w_OPEN)) status = StatType.OPEN;
            else if (tok[n].Match(Keywords.w_CLOSED)) status = StatType.CLOSED;
            else if (!tok[n].ToDouble(out y)) {
                throw new ENException(ErrorCode.Err211);
            }

            if (y < 0.0)
                throw new ENException(ErrorCode.Err211);

            if (n == 1) {
                Link linkRef;
                if ((linkRef = net.GetLink(tok[0])) == null) return;

                if (linkRef.Type == LinkType.CV) throw new ENException(ErrorCode.Err211);

                if (linkRef.Type == LinkType.GPV
                    && status == StatType.ACTIVE) throw new ENException(ErrorCode.Err211);

                this.ChangeStatus(linkRef, status, y);
            }
            else {
                long i0 = 0, i1 = 0;
                try {
                    i0 = long.Parse(tok[0]);
                    i1 = long.Parse(tok[1]);
                }
                finally {
                    if (i0 > 0 && i1 > 0) {
                        foreach (Link j  in  net.Links) {
                            try {
                                long i = long.Parse(j.Name);
                                if (i >= i0 && i <= i1)
                                    this.ChangeStatus(j, status, y);
                            }
                            catch (Exception) {}
                        }
                    }
                    else
                        foreach (Link j  in  net.Links)
                            if (string.Compare(tok[0], j.Name, StringComparison.Ordinal) <= 0 &&
                                string.Compare(tok[1], j.Name, StringComparison.Ordinal) >= 0)
                                this.ChangeStatus(j, status, y);
                }
            }
        }

        protected void ChangeStatus(Link lLink, StatType status, double y) {
            if (lLink.Type == LinkType.PIPE || lLink.Type == LinkType.GPV) {
                if (status != StatType.ACTIVE) lLink.Status = status;
            }
            else if (lLink.Type == LinkType.PUMP) {
                if (status == StatType.ACTIVE) {
                    lLink.Roughness = y; //lLink.setKc(y);
                    status = StatType.OPEN;
                    if (y == 0.0) status = StatType.CLOSED;
                }
                else if (status == StatType.OPEN) lLink.Roughness = 1.0; //lLink.setKc(1.0);
                lLink.Status = status;
            }
            else if (lLink.Type >= LinkType.PRV) {
                lLink.Roughness = y; //lLink.setKc(y);
                lLink.Status = status;
                if (status != StatType.ACTIVE)
                    lLink.Roughness = Constants.MISSING; //lLink.setKc(Constants.MISSING);
            }
        }

        protected void ParseEnergy(Network net, string[] tok) {
            int n = tok.Length;
            double y;

            if (n < 3) throw new ENException(ErrorCode.Err201);

            if (tok[0].Match(Keywords.w_DMNDCHARGE)) {
                if (!tok[2].ToDouble(out y))
                    throw new ENException(ErrorCode.Err213);
                net.DCost = y;
                return;
            }

            Pump pumpRef;
            if (tok[0].Match(Keywords.w_GLOBAL)) {
                pumpRef = null;
            }
            else if (tok[0].Match(Keywords.w_PUMP)) {
                if (n < 4) throw new ENException(ErrorCode.Err201);
                Link linkRef = net.GetLink(tok[1]);
                if (linkRef == null) throw new ENException(ErrorCode.Err216);
                if (linkRef.Type != LinkType.PUMP) throw new ENException(ErrorCode.Err216);
                pumpRef = (Pump)linkRef;
            }
            else throw new ENException(ErrorCode.Err201);


            if (tok[n - 2].Match(Keywords.w_PRICE)) {
                if (!tok[n - 1].ToDouble(out y)) {
                    if (pumpRef == null)
                        throw new ENException(ErrorCode.Err213);
                    else
                        throw new ENException(ErrorCode.Err217);
                }

                if (pumpRef == null)
                    net.ECost = y;
                else
                    pumpRef.ECost = y;

                return;
            }
            else if (tok[n - 2].Match(Keywords.w_PATTERN)) {
                Pattern t = net.GetPattern(tok[n - 1]);
                if (t == null) {
                    if (pumpRef == null) throw new ENException(ErrorCode.Err213);
                    else throw new ENException(ErrorCode.Err217);
                }
                if (pumpRef == null)
                    net.EPatId = t.Name;
                else
                    pumpRef.EPat = t;
                return;
            }
            else if (tok[n - 2].Match(Keywords.w_EFFIC)) {
                if (pumpRef == null) {
                    if (!tok[n - 1].ToDouble(out y))
                        throw new ENException(ErrorCode.Err213);
                    if (y <= 0.0)
                        throw new ENException(ErrorCode.Err213);
                    net.EPump = y;
                }
                else {
                    Curve t = net.GetCurve(tok[n - 1]);
                    if (t == null) throw new ENException(ErrorCode.Err217);
                    pumpRef.ECurve = t;
                }
                return;
            }
            throw new ENException(ErrorCode.Err201);
        }


        protected void ParseReport(Network net, string[] tok) {
            int n = tok.Length - 1;
            //FieldType i;
            double y;

            if (n < 1) throw new ENException(ErrorCode.Err201);

            if (tok[0].Match(Keywords.w_PAGE)) {
                if (!tok[n].ToDouble(out y)) throw new ENException(ErrorCode.Err213);
                if (y < 0.0 || y > 255.0) throw new ENException(ErrorCode.Err213);
                net.PageSize = (int)y;
                return;
            }


            if (tok[0].Match(Keywords.w_STATUS)) {
                StatFlag flag;
                if (EnumsTxt.TryParse(tok[n], out flag)) {
                    net.Stat_Flag = flag;
                }
                else {
                    // TODO: complete this    
                }

                //if (Utilities.match(Tok[n], Keywords.w_NO)) net.getPropertiesMap().setStatflag(PropertiesMap.StatFlag.FALSE);
                //if (Utilities.match(Tok[n], Keywords.w_YES)) net.getPropertiesMap().setStatflag(PropertiesMap.StatFlag.TRUE);
                //if (Utilities.match(Tok[n], Keywords.w_FULL)) net.getPropertiesMap().setStatflag(PropertiesMap.StatFlag.FULL);
                return;
            }

            if (tok[0].Match(Keywords.w_SUMMARY)) {
                if (tok[n].Match(Keywords.w_NO)) net.SummaryFlag = false;
                if (tok[n].Match(Keywords.w_YES)) net.SummaryFlag = true;
                return;
            }

            if (tok[0].Match(Keywords.w_MESSAGES)) {
                if (tok[n].Match(Keywords.w_NO)) net.MessageFlag = false;
                if (tok[n].Match(Keywords.w_YES)) net.MessageFlag = true;
                return;
            }

            if (tok[0].Match(Keywords.w_ENERGY)) {
                if (tok[n].Match(Keywords.w_NO)) net.EnergyFlag = false;
                if (tok[n].Match(Keywords.w_YES)) net.EnergyFlag = true;
                return;
            }

            if (tok[0].Match(Keywords.w_NODE)) {
                if (tok[n].Match(Keywords.w_NONE))
                    net.NodeFlag = ReportFlag.FALSE;
                else if (tok[n].Match(Keywords.w_ALL))
                    net.NodeFlag = ReportFlag.TRUE;
                else {
                    if (net.Nodes.Count == 0) throw new ENException(ErrorCode.Err208);
                    for (int ii = 1; ii <= n; ii++) {
                        Node nodeRef;
                        if ((nodeRef = net.GetNode(tok[n])) == null) throw new ENException(ErrorCode.Err208);
                        nodeRef.RptFlag = true;
                    }
                    net.NodeFlag = ReportFlag.SOME;
                }
                return;
            }

            if (tok[0].Match(Keywords.w_LINK)) {
                if (tok[n].Match(Keywords.w_NONE))
                    net.LinkFlag = ReportFlag.FALSE;
                else if (tok[n].Match(Keywords.w_ALL))
                    net.LinkFlag = ReportFlag.TRUE;
                else {
                    if (net.Links.Count == 0) throw new ENException(ErrorCode.Err210);
                    for (int ii = 1; ii <= n; ii++) {
                        Link linkRef = net.GetLink(tok[ii]);
                        if (linkRef == null) throw new ENException(ErrorCode.Err210);
                        linkRef.RptFlag = true;
                    }
                    net.LinkFlag = ReportFlag.SOME;
                }
                return;
            }

            FieldType iFieldID;
            FieldsMap fMap = net.FieldsMap;

            if (EnumsTxt.TryParse(tok[0], out iFieldID)) {
                if (iFieldID > FieldType.FRICTION)
                    throw new ENException(ErrorCode.Err201);

                if (tok.Length == 1 || tok[1].Match(Keywords.w_YES)) {
                    fMap.GetField(iFieldID).Enabled = true;
                    return;
                }

                if (tok[1].Match(Keywords.w_NO)) {
                    fMap.GetField(iFieldID).Enabled = false;
                    return;
                }

                RangeType rj;

                if (tok.Length < 3)
                    throw new ENException(ErrorCode.Err201);

                if (!EnumsTxt.TryParse(tok[1], out rj))
                    throw new ENException(ErrorCode.Err201);

                if (!tok[2].ToDouble(out y))
                    throw new ENException(ErrorCode.Err201);

                if (rj == RangeType.PREC) {
                    fMap.GetField(iFieldID).Enabled = true;
                    fMap.GetField(iFieldID).Precision = (int)Math.Round(y); //roundOff(y));
                }
                else
                    fMap.GetField(iFieldID).SetRptLim(rj, y);

                return;
            }

            if (tok[0].Match(Keywords.w_FILE)) {
                net.AltReport = tok[1];
                return;
            }

            this.Log.Information("Unknow section keyword " + tok[0] + " value " + tok[1]);
//        throw new ENException(ErrorCode.Err201);
        }

        protected void ParseOption(Network net, string[] tok) {
            int n = tok.Length - 1;
            bool notHandled = this.OptionChoice(net, tok, n);
            if (notHandled)
                notHandled = this.OptionValue(net, tok, n);
            if (notHandled) {
                net.ExtraOptions[tok[0]] = tok[1];
            }
        }

        ///<summary>Handles options that are choice values, such as quality type, for example.</summary>
        /// <param name="net">newtwork</param>
        /// <param name="tok">token arry</param>
        /// <param name="n">number of tokens</param>
        /// <returns><c>true</c> is it didn't handle the option.</returns>
        protected bool OptionChoice(Network net, string[] tok, int n) {
            
            if (n < 0)
                throw new ENException(ErrorCode.Err201);

            if (tok[0].Match(Keywords.w_UNITS)) {
                FlowUnitsType type;

                if (n < 1)
                    return false;
                else if (EnumsTxt.TryParse(tok[1], out type))
                    net.FlowFlag = type;
                else
                    throw new ENException(ErrorCode.Err201);

            }
            else if (tok[0].Match(Keywords.w_PRESSURE)) {
                if (n < 1) return false;
                PressUnitsType value;

                if (!EnumsTxt.TryParse(tok[1], out value)) {
                    throw new ENException(ErrorCode.Err201);
                }

                net.PressFlag = value;
            }
            else if (tok[0].Match(Keywords.w_HEADLOSS)) {
                if (n < 1) return false;
                FormType value;

                if (!EnumsTxt.TryParse(tok[1], out value)) {
                    throw new ENException(ErrorCode.Err201);
                }
                
                net.FormFlag = value;
            }
            else if (tok[0].Match(Keywords.w_HYDRAULIC)) {
                if (n < 2) return false;
                HydType value;

                if (!EnumsTxt.TryParse(tok[1], out value)) {
                    throw new ENException(ErrorCode.Err201);
                }

                net.HydFlag = value;
                net.HydFname = tok[2];
            }
            else if (tok[0].Match(Keywords.w_QUALITY)) {
                QualType type;

                if (n < 1)
                    return false;
                else if (EnumsTxt.TryParse(tok[1], out type))
                    net.QualFlag = type;
                else {
                    net.QualFlag = QualType.CHEM;
                    net.ChemName = tok[1];
                    if (n >= 2)
                        net.ChemUnits = tok[2];
                }
                if (net.QualFlag == QualType.TRACE) {

                    tok[0] = "";
                    if (n < 2)
                        throw new ENException(ErrorCode.Err212);
                    tok[0] = tok[2];
                    Node nodeRef = net.GetNode(tok[2]);
                    if (nodeRef == null)
                        throw new ENException(ErrorCode.Err212);
                    net.TraceNode = nodeRef.Name;
                    net.ChemName = Keywords.u_PERCENT;
                    net.ChemUnits = tok[2];
                }
                if (net.QualFlag == QualType.AGE) {
                    net.ChemName = Keywords.w_AGE;
                    net.ChemUnits = Keywords.u_HOURS;
                }
            }
            else if (tok[0].Match(Keywords.w_MAP)) {
                if (n < 1)
                    return false;
                net.MapFname = tok[1];
            }
            else if (tok[0].Match(Keywords.w_UNBALANCED)) {
                if (n < 1)
                    return false;
                if (tok[1].Match(Keywords.w_STOP))
                    net.ExtraIter = -1;
                else if (tok[1].Match(Keywords.w_CONTINUE)) {
                    if (n >= 2) {
                        double d;
                        if (tok[2].ToDouble(out d)) {
                            net.ExtraIter = (int)d;
                        }
                        else {
                            throw new ENException(ErrorCode.Err201);
                        }
                    }
                    else
                        net.ExtraIter = 0;
                }
                else throw new ENException(ErrorCode.Err201);
            }
            else if (tok[0].Match(Keywords.w_PATTERN)) {
                if (n < 1)
                    return false;
                net.DefPatId = tok[1];
            }
            else
                return true;
            return false;
        }

        protected bool OptionValue(Network net, string[] tok, int n) {
            int nvalue = 1;
            
            string name = tok[0];

            if (name.Match(Keywords.w_SPECGRAV) || name.Match(Keywords.w_EMITTER)
                || name.Match(Keywords.w_DEMAND)) nvalue = 2;

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

            if (!tok[nvalue].ToDouble(out y))
                throw new ENException(ErrorCode.Err213);

            if (name.Match(Keywords.w_TOLERANCE)) {
                if (y < 0.0)
                    throw new ENException(ErrorCode.Err213);
                net.Ctol = y;
                return false;
            }

            if (name.Match(Keywords.w_DIFFUSIVITY)) {
                if (y < 0.0)
                    throw new ENException(ErrorCode.Err213);
                net.Diffus = y;
                return false;
            }

            if (name.Match(Keywords.w_DAMPLIMIT)) {
                net.DampLimit = y;
                return false;
            }

            if (y <= 0.0) throw new ENException(ErrorCode.Err213);

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
                if (y >= 1.0) throw new ENException(ErrorCode.Err213);
                net.RQtol = y;
            }
            else if (name.Match(Keywords.w_CHECKFREQ)) net.CheckFreq = (int)y;
            else if (name.Match(Keywords.w_MAXCHECK)) net.MaxCheck = (int)y;
            else if (name.Match(Keywords.w_EMITTER)) net.QExp = 1.0d / y;
            else if (name.Match(Keywords.w_DEMAND)) net.DMult = y;

            return false;
        }

        protected void ParseTime(Network net, string[] tok) {
            int n = tok.Length - 1;
            double y;
            
            if (n < 1)
                throw new ENException(ErrorCode.Err201);

            if (tok[0].Match(Keywords.w_STATISTIC)) {
                TStatType type;

                if (!EnumsTxt.TryParse(tok[n], out type))
                    throw new ENException(ErrorCode.Err201);

                net.TStatFlag = type;
    
                return;
            }

            if (!tok[n].ToDouble(out y)) {
                if ((y = Utilities.GetHour(tok[n])) < 0.0) {
                    if ((y = Utilities.GetHour(tok[n - 1], tok[n])) < 0.0)
                        throw new ENException(ErrorCode.Err213);
                }
            }

            var t = (long)(3600.0 * y);

            if (tok[0].Match(Keywords.w_DURATION))
                net.Duration = t;
            else if (tok[0].Match(Keywords.w_HYDRAULIC))
                net.HStep = t;
            else if (tok[0].Match(Keywords.w_QUALITY))
                net.QStep = t;
            else if (tok[0].Match(Keywords.w_RULE))
                net.RuleStep = t;
            else if (tok[0].Match(Keywords.w_MINIMUM))
                return;
            else if (tok[0].Match(Keywords.w_PATTERN)) {
                if (tok[1].Match(Keywords.w_TIME))
                    net.PStep = t;
                else if (tok[1].Match(Keywords.w_START))
                    net.PStart = t;
                else
                    throw new ENException(ErrorCode.Err201);
            }
            else if (tok[0].Match(Keywords.w_REPORT)) {
                if (tok[1].Match(Keywords.w_TIME))
                    net.RStep = t;
                else if (tok[1].Match(Keywords.w_START))
                    net.RStart = t;
                else
                    throw new ENException(ErrorCode.Err201);
            }
            else if (tok[0].Match(Keywords.w_START))
                net.TStart = t % Constants.SECperDAY;
            else throw new ENException(ErrorCode.Err201);

        }

        private static SectType FindSectionType(string line) {
            if (string.IsNullOrEmpty(line))
                return (SectType)(-1);

            line = line.TrimStart();
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

        protected void ParseDemand(Network net, string[] tok) {
            int n = tok.Length;
            double y;
            Demand demand = null;
            Pattern pat = null;

            if (n < 2)
                throw new ENException(ErrorCode.Err201);

            if (!tok[1].ToDouble(out y)) {
                throw new ENException(ErrorCode.Err202);
            }

            if (tok[0].Match(Keywords.w_MULTIPLY)) {
                if (y <= 0.0)
                    throw new ENException(ErrorCode.Err202);
                else
                    net.DMult = y;
                return;
            }

            Node nodeRef;
            if ((nodeRef = net.GetNode(tok[0])) == null)
                throw new ENException(ErrorCode.Err208);

            if (nodeRef is Tank)
                throw new ENException(ErrorCode.Err208);

            if (n >= 3) {
                pat = net.GetPattern(tok[2]);
                if (pat == null)
                    throw new ENException(ErrorCode.Err205);
            }

            if (nodeRef.Demand.Count > 0)
                demand = nodeRef.Demand[0];

            if (demand != null && !nodeRef.InitDemand.IsMissing()) {
                demand.Base = y;
                demand.Pattern = pat;
                nodeRef.InitDemand = Constants.MISSING;
            }
            else {
                demand = new Demand(y, pat);
                nodeRef.Demand.Add(demand);
            }

        }

        protected void ParseRule(Network net, string[] tok, string line) {
            Rulewords key;
            EnumsTxt.TryParse(tok[0], out key);
            if (key == Rulewords.RULE) {
                this.currentRule = new Rule(tok[1]);
                net.Rules.Add(this.currentRule);
            }
            else if (this.currentRule != null) {
                this.currentRule.Code.Add(line);
            }

        }

    }

}