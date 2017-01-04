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

        ///<summary>Reference to the error logger.</summary>
        protected readonly TraceSource Log;
        protected readonly List<ENException> Errors = new List<ENException>();

        protected InputParser(TraceSource log) { this.Log = log; }

        public static InputParser Create(FileType type, TraceSource log) {
            switch (type) {
            case FileType.INP_FILE:
                return new InpParser(log);
            case FileType.EXCEL_FILE:
                return new ExcelParser(log);
            case FileType.XML_FILE:
                return new XmlParser(log, false);
            case FileType.XML_GZ_FILE:
                return new XmlParser(log, true);
            case FileType.NULL_FILE:
                return new NullParser(log);
            }
            return null;
        }

        public abstract Network Parse(Network net, string fileName);


        /// <summary>
        ///     Parses string S into N tokens stored in T.
        ///     Words between " " are stored 7as a single token.
        ///     Characters to right of ';' are ignored.
        /// </summary>
        protected static string[] Tokenize(string stringToSplit) {
            // const string SEPARATORS = " \t,\r\n";
            // char[] SEPARATORS = { ' ', '\t', ',', '\r', '\n' };
            bool inQuote = false;
            var tok = new StringBuilder(Constants.MAXLINE);
            List<string> results = new List<string>();

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

            return results.ToArray();
        }

        ///<summary>Prepare the hydraulic network for simulation.</summary>
        protected void Convert(Network net) {
            InitTanks(net);
            InitPumps(net);
            InitPatterns(net);
            this.CheckUnlinked(net);
            ConvertUnits(net);
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
                    else if ((link.Roughness > 0.0) && (link.Diameter > 0.0)) {
                        if (formFlag == FormType.HW)
                            link.Kw = rfactor / link.Roughness;
                        if (formFlag == FormType.DW)
                            link.Kw = rfactor / Math.Abs(Math.Log(link.Roughness / link.Diameter));
                        if (formFlag == FormType.CM)
                            link.Kw = rfactor * link.Roughness;
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
                foreach (Demand d  in  node.Demand) {
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
                if (tank.IsReservoir)
                    continue;

                int levelerr = 0;
                if (tank.H0 > tank.Hmax ||
                    tank.Hmin > tank.Hmax ||
                    tank.H0 < tank.Hmin
                ) levelerr = 1;

                Curve curv = tank.Vcurve;

                if (curv != null) {
                    n = curv.Points.Count - 1;
                    if (tank.Hmin < curv.Points[0].X ||
                        tank.Hmax > curv.Points[n].X)

                        levelerr = 1;
                }

                if (levelerr != 0) {
                    throw new ENException(ErrorCode.Err225, tank.Id);
                }

                if (curv != null) {

                    tank.Vmin = curv[tank.Hmin];
                    tank.Vmax = curv[tank.Hmax];
                    tank.V0 = curv[tank.H0];

                    var p = curv.Points;
                    double a = (p[n].Y - p[0].Y) / (p[n].X - p[0].X);

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


            foreach (Node node  in  net.Nodes) {
                if(node.Type == NodeType.TANK)
                    continue;

                foreach (Demand d  in  node.Demand) {
                    d.Base = d.Base / fMap.GetUnits(FieldType.DEMAND);
                }
            }


            double ucf = Math.Pow(fMap.GetUnits(FieldType.FLOW), net.QExp)
                         / fMap.GetUnits(FieldType.PRESSURE);

            foreach (Node node  in  net.Nodes) {
                if (!(node is Tank))
                    if (node.Ke > 0.0)
                        node.Ke = ucf / Math.Pow(node.Ke, net.QExp);
            }

            foreach (Tank tk  in  net.Tanks) {
                tk.H0 = tk.Elevation + tk.H0 / fMap.GetUnits(FieldType.ELEV);
                tk.Hmin = tk.Elevation + tk.Hmin / fMap.GetUnits(FieldType.ELEV);
                tk.Hmax = tk.Elevation + tk.Hmax / fMap.GetUnits(FieldType.ELEV);
                tk.Area = Math.PI * Math.Pow(tk.Area / fMap.GetUnits(FieldType.ELEV), 2) / 4.0;
                tk.V0 = tk.V0 / fMap.GetUnits(FieldType.VOLUME);
                tk.Vmin = tk.Vmin / fMap.GetUnits(FieldType.VOLUME);
                tk.Vmax = tk.Vmax / fMap.GetUnits(FieldType.VOLUME);
                tk.Kb = tk.Kb / Constants.SECperDAY;
                //tk.setVolume(tk.getV0());
                tk.Concentration = tk.C0;
                tk.V1Max = tk.V1Max * tk.Vmax;
            }


            net.CLimit = net.CLimit / fMap.GetUnits(FieldType.QUALITY);
            net.Ctol = net.Ctol / fMap.GetUnits(FieldType.QUALITY);

            net.KBulk = net.KBulk / Constants.SECperDAY;
            net.KWall = net.KWall / Constants.SECperDAY;


            foreach (Link lk  in  net.Links) {

                if (lk.Type <= LinkType.PIPE) {
                    if (net.FormFlag == FormType.DW)
                        lk.Roughness = lk.Roughness / (1000.0 * fMap.GetUnits(FieldType.ELEV));
                    lk.Diameter = lk.Diameter / fMap.GetUnits(FieldType.DIAM);
                    lk.Lenght = lk.Lenght / fMap.GetUnits(FieldType.LENGTH);

                    lk.Km = 0.02517 * lk.Km / Math.Pow(lk.Diameter, 2) / Math.Pow(lk.Diameter, 2);

                    lk.Kb = lk.Kb / Constants.SECperDAY;
                    lk.Kw = lk.Kw / Constants.SECperDAY;
                }
                else if (lk is Pump) {
                    Pump pump = (Pump)lk;

                    if (pump.Ptype == PumpType.CONST_HP) {
                        if (net.UnitsFlag == UnitsType.SI)
                            pump.FlowCoefficient = pump.FlowCoefficient / fMap.GetUnits(FieldType.POWER);
                    }
                    else {
                        if (pump.Ptype == PumpType.POWER_FUNC) {
                            pump.H0 = pump.H0 / fMap.GetUnits(FieldType.HEAD);
                            pump.FlowCoefficient = pump.FlowCoefficient *
                                                   (Math.Pow(fMap.GetUnits(FieldType.FLOW), pump.N)) /
                                                   fMap.GetUnits(FieldType.HEAD);
                        }

                        pump.Q0 = pump.Q0 / fMap.GetUnits(FieldType.FLOW);
                        pump.Qmax = pump.Qmax / fMap.GetUnits(FieldType.FLOW);
                        pump.Hmax = pump.Hmax / fMap.GetUnits(FieldType.HEAD);
                    }
                }
                else {
                    lk.Diameter = lk.Diameter / fMap.GetUnits(FieldType.DIAM);
                    lk.Km = 0.02517 * lk.Km / Math.Pow(lk.Diameter, 2) / Math.Pow(lk.Diameter, 2);
                   
                    if (!lk.Roughness.IsMissing())
                        switch (lk.Type) {
                        case LinkType.FCV:
                            lk.Roughness = lk.Roughness / fMap.GetUnits(FieldType.FLOW);
                            break;
                        case LinkType.PRV:
                        case LinkType.PSV:
                        case LinkType.PBV:
                            lk.Roughness = lk.Roughness / fMap.GetUnits(FieldType.PRESSURE);
                            break;
                        }
                }

                lk.initResistance(net.FormFlag, net.HExp);
            }

            foreach (Control c  in  net.Controls) {


                if (c.Link == null) continue;
                if (c.Node != null) {
                    Node node = c.Node;
                    if (node is Tank)
                        c.Grade = node.Elevation +
                                  c.Grade / fMap.GetUnits(FieldType.ELEV);
                    else
                        c.Grade = node.Elevation + c.Grade / fMap.GetUnits(FieldType.PRESSURE);
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
                        throw new ENException(ErrorCode.Err226, pump.Id);
                    }

                    int n = curve.Points.Count;

                    if (n == 1) {
                        pump.Ptype = PumpType.POWER_FUNC;
                        var pt = curve.Points[0];
                        q1 = pt.X;
                        h1 = pt.Y;
                        h0 = 1.33334 * h1;
                        q2 = 2.0 * q1;
                        h2 = 0.0;
                    }
                    else if (n == 3 && curve.Points[0].X == 0.0) {
                        pump.Ptype = PumpType.POWER_FUNC;
                        var poinst = curve.Points;
                        h0 = poinst[0].Y;
                        q1 = poinst[1].X;
                        h1 = poinst[1].Y;
                        q2 = poinst[2].X;
                        h2 = poinst[2].Y;
                    }
                    else
                        pump.Ptype = PumpType.CUSTOM;

                    // Compute shape factors & limits of power function pump curves
                    if (pump.Ptype == PumpType.POWER_FUNC) {
                        double a, b, c;
                        if (!GetPowerCurve(h0, h1, h2, q1, q2, out a, out b, out c))
                            throw new ENException(ErrorCode.Err227, pump.Id);


                        pump.H0 = -a;
                        pump.FlowCoefficient = -b;
                        pump.N = c;
                        pump.Q0 = q1;
                        pump.Qmax = Math.Pow(-a / b, (1.0 / c));
                        pump.Hmax = h0;
                    }
                }

                // Assign limits to custom pump curves
                if (pump.Ptype == PumpType.CUSTOM) {
                    Curve curve = pump.HCurve;
                    var points = curve.Points;

                    for (int i = 1; i < points.Count; i++) {
                        // Check for invalid curve
                        if (points[i].Y >= points[i - 1].Y) {
                            throw new ENException(ErrorCode.Err227, pump.Id);
                        }
                    }

                    pump.Qmax = points[curve.Points.Count - 1].X;
                    pump.Q0 = (points[0].X + pump.Qmax) / 2.0;
                    pump.Hmax = points[0].Y;
                }
            }

        }

        ///<summary>Initialize patterns.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        private static void InitPatterns(Network net) {
            foreach (Pattern par  in  net.Patterns) {
                if (par.FactorsList.Count == 0) {
                    par.FactorsList.Add(1.0);
                }
            }
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
                    this.Log.Error(new ENException(ErrorCode.Err233, node.Id));
                }

                if (err >= Constants.MAXERRS)
                    break;
            }

            //        if (err > 0)
            //            throw new ENException(200);
        }

        ///<summary>Check for unlinked nodes.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        private void CheckUnlinked2(Network net) {
            //int[] marked = new int[net.Nodes.Count + 1];
            var nodes = net.Nodes;
            Dictionary<string, int> marked = new Dictionary<string, int>(
                nodes.Count + 1,
                StringComparer.OrdinalIgnoreCase);

            int err = 0;

            foreach (Link link in net.Links) {
                marked[link.FirstNode.Id] = 1;
                marked[link.SecondNode.Id] = 1;
            }

            foreach (Node node in nodes) {
                if (marked[node.Id] == 0) {
                    err++;
                    this.Log.Error(new ENException(ErrorCode.Err233, node.Id));
                }

                if (err >= Constants.MAXERRS)
                    break;
            }

            //        if (err > 0)
            //            throw new ENException(200);
        }

        ///<summary>Check for unlinked nodes.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        private void checkUnlinked(Network net) {
            int[] marked = new int[net.Nodes.Count + 1];
            List<Link> links = new List<Link>(net.Links);
            List<Node> nodes = new List<Node>(net.Nodes);

            int err = 0;

            foreach (Link link in links) {
                marked[nodes.IndexOf(link.FirstNode)]++;
                marked[nodes.IndexOf(link.SecondNode)]++;
            }

            int i = 0;
            foreach (Node node in nodes) {
                if (marked[i] == 0) {
                    err++;
                    this.Log.Error("checkUnlinked", new ENException(ErrorCode.Err233, node.Id));
                }

                if (err >= Constants.MAXERRS)
                    break;

                i++;
            }

//        if (err > 0)
//            throw new ENException(200);
        }

        ///<summary>Check for unlinked nodes.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        private void CheckUnlinked(Network net) {
            int[] marked = new int[net.Nodes.Count + 1];
            var nodes = net.Nodes;

            int err = 0;

            foreach (Link link in net.Links) {
                marked[nodes.IndexOf(link.FirstNode)]++;
                marked[nodes.IndexOf(link.SecondNode)]++;
            }

            for (int i = 0; i < nodes.Count; i++) {
                if (marked[i] == 0) {
                    err++;
                    this.Log.Error(new ENException(ErrorCode.Err233, nodes[i].Id));
                }

                if (err >= Constants.MAXERRS)
                    break;
            }

//        if (err > 0)
//            throw new ENException(200);
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
        protected static bool GetPowerCurve(
            double h0,
            double h1,
            double h2,
            double q1,
            double q2,
            out double a,
            out double b,
            out double c) {
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
    }

}



