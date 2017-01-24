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
using System.Diagnostics;
using System.Text;

using Epanet.Enums;
using Epanet.Log;
using Epanet.Network.Structures;
using Epanet.Util;

namespace Epanet.Network.IO.Input {


    ///<summary>Abstract input file parser.</summary>
    public abstract class InputParser {
        protected string FileName;
        protected Network net;

        ///<summary>Reference to the error logger.</summary>
        protected readonly TraceSource Log = new TraceSource("epanet", SourceLevels.All);
        protected readonly List<ENException> Errors = new List<ENException>(Constants.MAXERRS + 1);

        protected ErrorCode LogException(ENException ex) {
            this.Errors.Add(ex);
            
            this.Log.Error(ex);

            if(this.Errors.Count > Constants.MAXERRS)
                throw new ENException(ErrorCode.Err200);

            return ex.Code;
        }

        public static InputParser Create(FileType type) {
            switch (type) {
            case FileType.INP_FILE:
                return new InpParser();
            case FileType.NET_FILE:
                return new NetParser();
            case FileType.XML_FILE:
                return new XmlParser(false);
            case FileType.XML_GZ_FILE:
                return new XmlParser(true);
            case FileType.NULL_FILE:
                return new NullParser();
            }
            return null;
        }

        public abstract Network Parse(Network net, string fileName);




        /// <summary>
        ///     Parses string S into N tokens stored in T.
        ///     Words between " " are stored 7as a single token.
        ///     Characters to right of ';' are ignored.
        /// </summary>
        protected static int Tokenize(string stringToSplit, List<string> results) {
            // const string SEPARATORS = " \t,\r\n";
            // char[] SEPARATORS = { ' ', '\t', ',', '\r', '\n' };

            if(results == null)
                throw new ArgumentNullException("results");

            bool inQuote = false;
            var tok = new StringBuilder(Constants.MAXLINE);

            results.Clear();


            foreach (char c in stringToSplit) {
                if (c == ';')
                    break;

                if (c == '"') {
                    // When we see a ", we need to decide whether we are
                    // at the start or send of a quoted section...
                    inQuote = !inQuote;
                }
                else if (!inQuote && char.IsWhiteSpace(c)) {
                    // We've come to the end of a token, so we find the token,
                    // trim it and add it to the collection of results...
                    if (tok.Length > 0) {
                        string result = tok.ToString().Trim();
                        if (!string.IsNullOrEmpty(result))
                            results.Add(result);
                    }

                    // We start a new token...
                    tok.Length = 0;
                }
                else {
                    // We've got a 'normal' character, so we add it to the curent token...
                    tok.Append(c);
                }
            }

            // We've come to the end of the string, so we add the last token...
            if (tok.Length > 0) {
                string lastResult = tok.ToString().Trim();
                if (!string.IsNullOrEmpty(lastResult))
                    results.Add(lastResult);
            }

            return results.Count;
        }

        ///<summary>Prepare the hydraulic network for simulation.</summary>
        protected void Convert() {
            InitTanks(this.net);
            InitPumps(this.net);
            InitPatterns(this.net);
            this.CheckUnlinked();
            ConvertUnits(this.net);
        }

        ///<summary>Adjust simulation configurations.</summary>
        protected static void AdjustData(Network net) {
            
            if (net.PStep <= 0) net.PStep = 3600;
            if (net.RStep == 0) net.RStep = net.PStep;
            if (net.HStep <= 0) net.HStep = 3600;
            if (net.HStep > net.PStep) net.HStep = net.PStep;
            if (net.HStep > net.RStep) net.HStep = net.RStep;
            if (net.RStart > net.Duration) net.RStart = 0;
            if (net.Duration == 0) net.QualFlag = QualType.NONE;
            if (net.QStep == 0) net.QStep = net.HStep / 10;
            if (net.RuleStep == 0) net.RuleStep = net.HStep / 10;

            net.RuleStep = Math.Min(net.RuleStep, net.HStep);
            net.QStep = Math.Min(net.QStep, net.HStep);

            if (net.Ctol.IsMissing()) {
                net.Ctol = net.QualFlag == QualType.AGE ? Constants.AGETOL : Constants.CHEMTOL;
            }

            switch (net.FlowFlag) {
            case FlowUnitsType.LPS:
            case FlowUnitsType.LPM:
            case FlowUnitsType.MLD:
            case FlowUnitsType.CMH:
            case FlowUnitsType.CMD:
                net.UnitsFlag = UnitsType.SI;
                break;
            default:
                net.UnitsFlag = UnitsType.US;
                break;
            }


            if (net.UnitsFlag != UnitsType.SI)
                net.PressFlag = PressUnitsType.PSI;
            else if (net.PressFlag == PressUnitsType.PSI)
                net.PressFlag = PressUnitsType.METERS;

            var ucf = 1.0;
            if (net.UnitsFlag == UnitsType.SI)
                ucf = Math.Pow(Constants.MperFT, 2);

            if (net.Viscos.IsMissing())
                net.Viscos = Constants.VISCOS;
            else if (net.Viscos > 1e-3)
                net.Viscos = net.Viscos * Constants.VISCOS;
            else
                net.Viscos = net.Viscos / ucf;

            if (net.Diffus.IsMissing())
                net.Diffus = Constants.DIFFUS;
            else if (net.Diffus > 1e-4)
                net.Diffus = net.Diffus * Constants.DIFFUS;
            else
                net.Diffus = net.Diffus / ucf;

            net.HExp = net.FormFlag == FormType.HW ? 1.852 : 2.0;

            double rfactor = net.RFactor;
            FormType formFlag = net.FormFlag;
            double kbulk = net.KBulk;

            foreach (Link link  in  net.Links) {
                if (link.Type > LinkType.PIPE)
                    continue;

                if (link.Kb.IsMissing())
                    link.Kb = kbulk;

                if (link.Kw.IsMissing()) {
                    if (rfactor == 0.0)
                        link.Kw = net.KWall;
                    else if ((link.Kc > 0.0) && (link.Diameter > 0.0)) {
                        if (formFlag == FormType.HW)
                            link.Kw = rfactor / link.Kc;
                        if (formFlag == FormType.DW)
                            link.Kw = rfactor / Math.Abs(Math.Log(link.Kc / link.Diameter));
                        if (formFlag == FormType.CM)
                            link.Kw = rfactor * link.Kc;
                    }
                    else
                        link.Kw = 0.0;
                }
            }

            foreach (Tank tank  in  net.Tanks)
                if (tank.Kb.IsMissing())
                    tank.Kb = kbulk;

            Pattern defpat = net.GetPattern(net.DefPatId) ?? net.GetPattern("");

            foreach (Node node  in  net.Nodes) {
                if (node.Demands.Count == 0) 
                    node.Demands.Add(node.PrimaryDemand);

                foreach (Demand d  in  node.Demands) {
                    if (d.Pattern == null)
                        d.Pattern = defpat;
                }
            }

            if (net.QualFlag == QualType.NONE)
                net.FieldsMap.GetField(FieldType.QUALITY).Enabled = false;

        }

        ///<summary>Initialize tank properties.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        private static void InitTanks(Network net) {
            int n = 0;

            foreach (Tank tank  in  net.Tanks) {
                int levelerr = 0;

                if (tank.H0 > tank.Hmax || tank.Hmin > tank.Hmax || tank.H0 < tank.Hmin) 
                    levelerr = 1;

                Curve curv = tank.Vcurve;

                if (curv != null) {
                    n = curv.Count - 1;
                    if (tank.Hmin < curv[0].X ||
                        tank.Hmax > curv[n].X)

                        levelerr = 1;
                }

                if (levelerr != 0) {
                    throw new ENException(ErrorCode.Err225, tank.Name);
                }

                if (curv != null) {

                    tank.Vmin = curv.Interpolate(tank.Hmin);
                    tank.Vmax = curv.Interpolate(tank.Hmax);
                    tank.V0 = curv.Interpolate(tank.H0);
                   
                    double a = (curv[n].Y - curv[0].Y) / (curv[n].X - curv[0].X);

                    tank.Area = Math.Sqrt(4.0 * a / Math.PI);
                }
            }
        }

        ///<summary>Convert hydraulic structures values from user units to simulation system units.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        private static void ConvertUnits(Network net) {
            FieldsMap fMap = net.FieldsMap;
            
            foreach (Node node  in  net.Nodes) {
                node.Elevation /= fMap.GetUnits(FieldType.ELEV);
                node.C0 /= fMap.GetUnits(FieldType.QUALITY);
            }


            foreach (Node node  in  net.Junctions) {
                foreach (Demand d  in  node.Demands) {
                    d.Base /= fMap.GetUnits(FieldType.DEMAND);
                }
            }


            double ucf = Math.Pow(fMap.GetUnits(FieldType.FLOW), net.QExp)
                         / fMap.GetUnits(FieldType.PRESSURE);

            foreach (Node node  in  net.Junctions) {
                if (node.Ke > 0.0)
                    node.Ke = ucf / Math.Pow(node.Ke, net.QExp);
            }


            // FIXME:Tanks and reservoirs here?
            foreach (Tank tk  in  net.Tanks) {
                tk.H0 = tk.Elevation + tk.H0 / fMap.GetUnits(FieldType.ELEV);
                tk.Hmin = tk.Elevation + tk.Hmin / fMap.GetUnits(FieldType.ELEV);
                tk.Hmax = tk.Elevation + tk.Hmax / fMap.GetUnits(FieldType.ELEV);
                tk.Area = Math.PI * Math.Pow(tk.Area / fMap.GetUnits(FieldType.ELEV), 2) / 4.0;
                tk.V0 = tk.V0 / fMap.GetUnits(FieldType.VOLUME);
                tk.Vmin = tk.Vmin / fMap.GetUnits(FieldType.VOLUME);
                tk.Vmax = tk.Vmax / fMap.GetUnits(FieldType.VOLUME);
                tk.Kb = tk.Kb / Constants.SECperDAY;
                // tk.Volume = tk.V0;
                tk.C = tk.C0;
                tk.V1Max = tk.V1Max * tk.Vmax;
            }
            
            net.CLimit = net.CLimit / fMap.GetUnits(FieldType.QUALITY);
            net.Ctol = net.Ctol / fMap.GetUnits(FieldType.QUALITY);

            net.KBulk = net.KBulk / Constants.SECperDAY;
            net.KWall = net.KWall / Constants.SECperDAY;

            foreach (Link lk  in  net.Links) {
                switch (lk.Type) {
                case LinkType.CV:
                case LinkType.PIPE:
                    if (net.FormFlag == FormType.DW)
                        lk.Kc = lk.Kc / (1000.0 * fMap.GetUnits(FieldType.ELEV));
                    lk.Diameter = lk.Diameter / fMap.GetUnits(FieldType.DIAM);
                    lk.Lenght = lk.Lenght / fMap.GetUnits(FieldType.LENGTH);

                    lk.Km = 0.02517 * lk.Km / Math.Pow(lk.Diameter, 2) / Math.Pow(lk.Diameter, 2);

                    lk.Kb = lk.Kb / Constants.SECperDAY;
                    lk.Kw = lk.Kw / Constants.SECperDAY;
                    break;

                case LinkType.PUMP:
                    Pump pump = (Pump)lk;

                    if (pump.Ptype == PumpType.CONST_HP) {
                        if (net.UnitsFlag == UnitsType.SI)
                            pump.FlowCoefficient = pump.FlowCoefficient / fMap.GetUnits(FieldType.POWER);
                    }
                    else {
                        if (pump.Ptype == PumpType.POWER_FUNC) {
                            pump.H0 = pump.H0 / fMap.GetUnits(FieldType.HEAD);
                            pump.FlowCoefficient = pump.FlowCoefficient *
                                                   Math.Pow(fMap.GetUnits(FieldType.FLOW), pump.N) /
                                                   fMap.GetUnits(FieldType.HEAD);
                        }

                        pump.Q0 = pump.Q0 / fMap.GetUnits(FieldType.FLOW);
                        pump.Qmax = pump.Qmax / fMap.GetUnits(FieldType.FLOW);
                        pump.Hmax = pump.Hmax / fMap.GetUnits(FieldType.HEAD);
                    }
                    break;

                default:
                    lk.Diameter = lk.Diameter / fMap.GetUnits(FieldType.DIAM);
                    lk.Km = 0.02517 * lk.Km / Math.Pow(lk.Diameter, 2) / Math.Pow(lk.Diameter, 2);
                   
                    if (!lk.Kc.IsMissing())
                        switch (lk.Type) {
                        case LinkType.FCV:
                            lk.Kc = lk.Kc / fMap.GetUnits(FieldType.FLOW);
                            break;
                        case LinkType.PRV:
                        case LinkType.PSV:
                        case LinkType.PBV:
                            lk.Kc = lk.Kc / fMap.GetUnits(FieldType.PRESSURE);
                            break;
                        }

                    break;
                }

                lk.initResistance(net.FormFlag, net.HExp);
            }

            foreach (Control c  in  net.Controls) {


                if (c.Link == null) continue;
                if (c.Node != null) {
                    c.Grade = c.Node.Type == NodeType.JUNC
                        ? c.Node.Elevation + c.Grade / fMap.GetUnits(FieldType.PRESSURE)
                        : c.Node.Elevation + c.Grade / fMap.GetUnits(FieldType.ELEV);
                }

                if (!c.Setting.IsMissing())
                    switch (c.Link.Type) {
                    case LinkType.PRV:
                    case LinkType.PSV:
                    case LinkType.PBV:
                        c.Setting = c.Setting / fMap.GetUnits(FieldType.PRESSURE);
                        break;
                    case LinkType.FCV:
                        c.Setting = c.Setting / fMap.GetUnits(FieldType.FLOW);
                        break;
                    }
            }
        }

        ///<summary>Initialize pump properties.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        private static void InitPumps(Network net) {
            double h0 = 0.0, h1 = 0.0, h2 = 0.0, q1 = 0.0, q2 = 0.0;

            foreach (Pump pump  in  net.Pumps) {
                // Constant Hp pump
                if (pump.Ptype == PumpType.CONST_HP) {
                    pump.H0 = 0.0;
                    pump.FlowCoefficient = -8.814 * pump.Km;
                    pump.N = -1.0;
                    pump.Hmax = Constants.BIG;
                    pump.Qmax = Constants.BIG;
                    pump.Q0 = 1.0;
                    continue;
                }

                // Set parameters for pump curves
                else if (pump.Ptype == PumpType.NOCURVE) {
                    Curve curve = pump.HCurve;
                    if (curve == null) {
                        throw new ENException(ErrorCode.Err226, pump.Name);
                    }

                    int n = curve.Count;

                    if (n == 1) {
                        pump.Ptype = PumpType.POWER_FUNC;
                        var pt = curve[0];
                        q1 = pt.X;
                        h1 = pt.Y;
                        h0 = 1.33334 * h1;
                        q2 = 2.0 * q1;
                        h2 = 0.0;
                    }
                    else if (n == 3 && curve[0].X == 0.0) {
                        pump.Ptype = PumpType.POWER_FUNC;
                        h0 = curve[0].Y;
                        q1 = curve[1].X;
                        h1 = curve[1].Y;
                        q2 = curve[2].X;
                        h2 = curve[2].Y;
                    }
                    else
                        pump.Ptype = PumpType.CUSTOM;

                    // Compute shape factors & limits of power function pump curves
                    if (pump.Ptype == PumpType.POWER_FUNC) {
                        double a, b, c;
                        if (!GetPowerCurve(h0, h1, h2, q1, q2, out a, out b, out c))
                            throw new ENException(ErrorCode.Err227, pump.Name);


                        pump.H0 = -a;
                        pump.FlowCoefficient = -b;
                        pump.N = c;
                        pump.Q0 = q1;
                        pump.Qmax = Math.Pow(-a / b, 1.0 / c);
                        pump.Hmax = h0;
                    }
                }

                // Assign limits to custom pump curves
                if (pump.Ptype == PumpType.CUSTOM) {
                    Curve curve = pump.HCurve;

                    for(int i = 1; i < curve.Count; i++) {
                        // Check for invalid curve
                        if(curve[i].Y >= curve[i - 1].Y) {
                            throw new ENException(ErrorCode.Err227, pump.Name);
                        }
                    }

                    pump.Qmax = curve[curve.Count - 1].X;
                    pump.Q0 = (curve[0].X + pump.Qmax) / 2.0;
                    pump.Hmax = curve[0].Y;
                }
            }

        }

        ///<summary>Initialize patterns.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        private static void InitPatterns(Network net) {
            foreach (Pattern par  in  net.Patterns)
                if (par.Count == 0)
                    par.Add(1.0);
        }

        // TODO: performance testing

        ///<summary>Check for unlinked nodes.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        private void CheckUnlinked1(Network net) {
            //int[] marked = new int[net.Nodes.Count + 1];
            var nodes = net.Nodes;
            Dictionary<Node, int> marked = new Dictionary<Node, int>(nodes.Count + 1);

            int err = 0;

            foreach (Link link in net.Links) {
                marked[link.FirstNode] = 1;
                marked[link.SecondNode] = 1;
            }

            foreach (Node node in nodes) {
                if (marked[node] == 0) {
                    err++;
                    this.LogException(new ENException(ErrorCode.Err233, node.Name));
                }

                if (err >= Constants.MAXERRS)
                    break;
            }

            //        if (err > 0)
            //            throw new ENException(200);
        }

        ///<summary>Check for unlinked nodes.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        private void CheckUnlinked2() {
            //int[] marked = new int[net.Nodes.Count + 1];
            var nodes = net.Nodes;
            Dictionary<string, int> marked = new Dictionary<string, int>(
                nodes.Count + 1,
                StringComparer.OrdinalIgnoreCase);

            foreach (Link link in net.Links) {
                marked[link.FirstNode.Name] = 1;
                marked[link.SecondNode.Name] = 1;
            }

            foreach (Node node in nodes) {
                if (marked[node.Name] == 0) {
                    this.LogException(new ENException(ErrorCode.Err233, node.Name));
                }

            }

        }

        ///<summary>Check for unlinked nodes.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        private void checkUnlinked() {
            int[] marked = new int[net.Nodes.Count + 1];
            var nodes = net.Nodes;

            foreach(Link link in net.Links) {
                marked[nodes.IndexOf(link.FirstNode)]++;
                marked[nodes.IndexOf(link.SecondNode)]++;
            }

            for (int i = 0; i < nodes.Count; i++) {
                if (marked[i] == 0) {
                    this.LogException(new ENException(ErrorCode.Err233, nodes[i].Name));
                }
            }

        }

        ///<summary>Check for unlinked nodes.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        private void CheckUnlinked() {
            int[] marked = new int[this.net.Nodes.Count + 1];
            var nodes = this.net.Nodes;

            foreach (Link link in this.net.Links) {
                marked[nodes.IndexOf(link.FirstNode)]++;
                marked[nodes.IndexOf(link.SecondNode)]++;
            }

            for (int i = 0; i < nodes.Count; i++) {
                if (marked[i] == 0) {
                    this.LogException(new ENException(ErrorCode.Err233, nodes[i].Name));
                }
            }

        }

        /// <summary>Computes coeffs. for pump curve.</summary>
        ///  <param name="h0">shutoff head</param>
        /// <param name="h1">design head</param>
        /// <param name="h2">head at max. flow</param>
        /// <param name="q1">design flow</param>
        /// <param name="q2">max. flow</param>
        /// <param name="a">pump curve coeffs. (H = a-bQ^c)</param>
        /// <param name="b">pump curve coeffs. (H = a-bQ^c)</param>
        /// <param name="c">pump curve coeffs. (H = a-bQ^c)</param>
        ///  <returns>Returns true if sucessful, false otherwise.</returns>
        protected static bool GetPowerCurve(double h0, double h1, double h2, double q1, double q2, out double a, out double b, out double c) 
        {
            a = b = c = 0;

            if (
                h0 < Constants.TINY ||
                h0 - h1 < Constants.TINY ||
                h1 - h2 < Constants.TINY ||
                q1 < Constants.TINY ||
                q2 - q1 < Constants.TINY
            ) return false;

            a = h0;
            double h4 = h0 - h1;
            double h5 = h0 - h2;
            c = Math.Log(h5 / h4) / Math.Log(q2 / q1);
            if (c <= 0.0 || c > 20.0) return false;
            b = -h4 / Math.Pow(q1, c);

            return !(b >= 0.0);
        }

        /// <summary>checks for legal connections between PRVs & PSVs</summary>
        /// <param name="net"></param>
        /// <param name="type">valve type</param>
        /// <param name="j1">upstream node</param>
        /// <param name="j2">downstream node</param>
        /// <returns>returns true for legal connection, false otherwise</returns>
        protected static bool Valvecheck(Network net, LinkType type, Node j1, Node j2) {
            // Examine each existing valve
            foreach(Valve vk in net.Valves) {
                Node vj1 = vk.FirstNode;
                Node vj2 = vk.SecondNode;
                LinkType vtype = vk.Type;

                /* Cannot have two PRVs sharing downstream nodes or in series */
                if(vtype == LinkType.PRV && type == LinkType.PRV) {
                    if(Equals(vj2, j2) ||
                        Equals(vj2, j1) ||
                        Equals(vj1, j2))
                        return false;
                }

                /* Cannot have two PSVs sharing upstream nodes or in series */
                if(vtype == LinkType.PSV && type == LinkType.PSV) {
                    if(Equals(vj1, j1) ||
                        Equals(vj1, j2) ||
                        Equals(vj2, j1))
                        return false;
                }

                /* Cannot have PSV connected to downstream node of PRV */
                if(vtype == LinkType.PSV && type == LinkType.PRV && vj1 == j2)
                    return false;
                if(vtype == LinkType.PRV && type == LinkType.PSV && vj2 == j1)
                    return false;

                /* Cannot have PSV connected to downstream node of FCV */
                /* nor have PRV connected to upstream node of FCV */
                if(vtype == LinkType.FCV && type == LinkType.PSV && vj2 == j1)
                    return false;
                if(vtype == LinkType.FCV && type == LinkType.PRV && vj1 == j2)
                    return false;

                if(vtype == LinkType.PSV && type == LinkType.FCV && vj1 == j2)
                    return false;
                if(vtype == LinkType.PRV && type == LinkType.FCV && vj2 == j1)
                    return false;
            }

            return true;
        }

        protected static void GetPumpCurve(Pump pump, int n, double[] x) {


            if(n == 1) {
                if(x[0] <= 0.0)
                    throw new ENException(ErrorCode.Err202);
                pump.Ptype = PumpType.CONST_HP;
                pump.Km = x[0];
            }
            else {
                double h0, h1, h2, q1, q2;

                if(n == 2) {
                    q1 = x[1];
                    h1 = x[0];
                    h0 = 1.33334 * h1;
                    q2 = 2.0 * q1;
                    h2 = 0.0;
                }
                else if(n >= 5) {
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

                if(!GetPowerCurve(h0, h1, h2, q1, q2, out a, out b, out c))
                    throw new ENException(ErrorCode.Err206);

                pump.H0 = -a;
                pump.FlowCoefficient = -b;
                pump.N = c;
                pump.Q0 = q1;
                pump.Qmax = Math.Pow(-a / b, 1.0 / c);
                pump.Hmax = h0;
            }
        }

        // TODO: move to link class
        protected static void ChangeStatus(Link link, StatType status, double y) {

            switch (link.Type) {
            case LinkType.PIPE:
            case LinkType.GPV:
                if (status != StatType.ACTIVE)
                    link.Status = status;
                break;

            case LinkType.PUMP:

                switch (status) {
                case StatType.ACTIVE:
                    link.Kc = y;
                    link.Status = y == 0.0 ? StatType.CLOSED : StatType.OPEN;
                    break;
                case StatType.OPEN:
                    link.Kc = 1.0; 
                    break;
                }

                link.Status = status;
                break;

            case LinkType.CV:
                break;

            case LinkType.PRV:
            case LinkType.PSV:
            case LinkType.PBV:
            case LinkType.FCV:
            case LinkType.TCV:
                link.Status = status;
                link.Kc = status == StatType.ACTIVE
                    ? y //lLink.setKc(y);
                    : Constants.MISSING; //lLink.setKc(Constants.MISSING);

                break;
            }
        }

    }





}



