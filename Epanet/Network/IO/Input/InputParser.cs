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
using Epanet.Log;
using Epanet.Network.Structures;
using Epanet.Util;

namespace Epanet.Network.IO.Input {


///<summary>Abstract input file parser.</summary>
public abstract class InputParser {

    protected string FileName;

    ///<summary>Reference to the error logger.</summary>
    protected TraceSource Log;
    protected readonly List<ENException> Errors = new List<ENException>();

    protected InputParser(TraceSource log) {
        this.Log = log;
    }

    public static InputParser Create(Network.FileType type, TraceSource log) {
        switch (type) {
            case Network.FileType.INP_FILE:
                return new InpParser(log);
            case Network.FileType.EXCEL_FILE:
                return new ExcelParser(log);
            case Network.FileType.XML_FILE:
                return new XmlParser(log,false);
            case Network.FileType.XML_GZ_FILE:
                return new XmlParser(log,true);
            case Network.FileType.NULL_FILE:
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

        foreach(char c in stringToSplit) {
            if(c == ';')
                break;

            if(c == '"') {
                // When we see a ", we need to decide whether we are
                // at the start or send of a quoted section...
                inQuote = !inQuote;
            }
            else if(!inQuote && char.IsWhiteSpace(c)) {
                // We've come to the end of a token, so we find the token,
                // trim it and add it to the collection of results...
                if(tok.Length > 0) {
                    string result = tok.ToString().Trim();
                    if(!string.IsNullOrEmpty(result))
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
        if(tok.Length > 0) {
            string lastResult = tok.ToString().Trim();
            if(!string.IsNullOrEmpty(lastResult))
                results.Add(lastResult);
        }

        return results.ToArray();
    }

   ///<summary>Prepare the hydraulic network for simulation.</summary>
    protected void Convert(Network net) {
        this.InitTanks(net);
        this.InitPumps(net);
        this.InitPatterns(net);
        this.CheckUnlinked(net);
        this.ConvertUnits(net);
    }

    ///<summary>Adjust simulation configurations.</summary>
    protected static void AdjustData(Network net) {
        PropertiesMap m = net.PropertiesMap;

        if (m.PStep <= 0) m.PStep = 3600;
        if (m.RStep == 0) m.RStep = m.PStep;
        if (m.HStep <= 0) m.HStep = 3600;
        if (m.HStep > m.PStep) m.HStep = m.PStep;
        if (m.HStep > m.RStep) m.HStep = m.RStep;
        if (m.RStart > m.Duration) m.RStart = 0;
        if (m.Duration == 0) m.QualFlag = PropertiesMap.QualType.NONE;
        if (m.QStep == 0) m.QStep = m.HStep / 10;
        if (m.RuleStep == 0) m.RuleStep = m.HStep / 10;

        m.RuleStep = Math.Min(m.RuleStep, m.HStep);
        m.QStep = Math.Min(m.QStep, m.HStep);

        if (m.Ctol == Constants.MISSING) {
            m.Ctol = m.QualFlag == PropertiesMap.QualType.AGE ? Constants.AGETOL : Constants.CHEMTOL;
        }

        switch (m.FlowFlag) {
            case PropertiesMap.FlowUnitsType.LPS:
            case PropertiesMap.FlowUnitsType.LPM:
            case PropertiesMap.FlowUnitsType.MLD:
            case PropertiesMap.FlowUnitsType.CMH:
            case PropertiesMap.FlowUnitsType.CMD:
                m.UnitsFlag = PropertiesMap.UnitsType.SI;
                break;
            default:
                m.UnitsFlag = PropertiesMap.UnitsType.US;
                break;
        }


        if (m.UnitsFlag != PropertiesMap.UnitsType.SI)
            m.PressFlag = PropertiesMap.PressUnitsType.PSI;
        else if (m.PressFlag == PropertiesMap.PressUnitsType.PSI)
            m.PressFlag = PropertiesMap.PressUnitsType.METERS;

        var ucf = 1.0;
        if (m.UnitsFlag == PropertiesMap.UnitsType.SI)
            ucf = Math.Pow(Constants.MperFT, 2);

        if (m.Viscos == Constants.MISSING)
            m.Viscos = Constants.VISCOS;
        else if (m.Viscos > 1e-3)
            m.Viscos = m.Viscos * Constants.VISCOS;
        else
            m.Viscos = m.Viscos / ucf;

        if (m.Diffus == Constants.MISSING)
            m.Diffus = Constants.DIFFUS;
        else if (m.Diffus > 1e-4)
            m.Diffus = m.Diffus * Constants.DIFFUS;
        else
            m.Diffus = m.Diffus / ucf;

        m.HExp = m.FormFlag == PropertiesMap.FormType.HW ? 1.852 : 2.0;

        double rfactor = m.RFactor;
        PropertiesMap.FormType formFlag = m.FormFlag;
        double kbulk = m.KBulk;

        foreach (Link link  in  net.Links) {
            if (link.Type > Link.LinkType.PIPE)
                continue;

            if (link.Kb == Constants.MISSING)
                link.Kb = kbulk;

            if (link.Kw == Constants.MISSING)
            {
                if (rfactor == 0.0)
                    link.Kw = m.KWall;
                else if ((link.Roughness > 0.0) && (link.Diameter > 0.0)) {
                    if (formFlag == PropertiesMap.FormType.HW)
                        link.Kw = rfactor / link.Roughness;
                    if (formFlag == PropertiesMap.FormType.DW)
                        link.Kw = rfactor / Math.Abs(Math.Log(link.Roughness / link.Diameter));
                    if (formFlag == PropertiesMap.FormType.CM)
                        link.Kw = rfactor * link.Roughness;
                }
                else
                    link.Kw = 0.0;
            }
        }

        foreach (Tank tank  in  net.Tanks)
            if (tank.Kb == Constants.MISSING)
                tank.Kb = kbulk;

        Pattern defpat = net.GetPattern(m.DefPatId) ?? net.GetPattern("");

        foreach (Node node  in  net.Nodes) {
            foreach (Demand d  in  node.Demand) {
                if (d.Pattern == null)
                    d.Pattern = defpat;
            }
        }

        if (m.QualFlag == PropertiesMap.QualType.NONE)
            net.FieldsMap.GetField(FieldsMap.FieldType.QUALITY).Enabled = false;

    }

    /**
     * Initialize tank properties.
     * @param net Hydraulic network reference.
     * @throws ENException
     */
    private void InitTanks(Network net) {
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
                n = curv.Npts - 1;
                if (tank.Hmin < curv.X[0] ||
                        tank.Hmax > curv.X[n])
                    levelerr = 1;
            }

            if (levelerr != 0) {
                throw new ENException(ErrorCode.Err225, tank.Id);
            } else if (curv != null) {

                tank.Vmin = curv.LinearInterpolator(tank.Hmin);
                tank.Vmax = curv.LinearInterpolator(tank.Hmax);
                tank.V0 = curv.LinearInterpolator(tank.H0);

                double a = (curv.Y[n] - curv.Y[0]) /
                           (curv.X[n] - curv.X[0]);
                tank.Area = Math.Sqrt(4.0 * a / Math.PI);
            }
        }
    }

    /**
     * Convert hydraulic structures values from user units to simulation system units.
     * @param net Hydraulic network reference.
     * @throws ENException
     */
    private void ConvertUnits(Network net) {
        FieldsMap fMap = net.FieldsMap;
        PropertiesMap pMap = net.PropertiesMap;

        foreach (Node node  in  net.Nodes) {
            node.Elevation = node.Elevation / fMap.GetUnits(FieldsMap.FieldType.ELEV);
            node.C0 = new[]{node.C0[0] / fMap.GetUnits(FieldsMap.FieldType.QUALITY)};
        }


        foreach (Node node  in  net.Nodes) {
            if (node is Tank)
                continue;

            foreach (Demand d  in  node.Demand) {
                d.Base = d.Base / fMap.GetUnits(FieldsMap.FieldType.DEMAND);
            }
        }


        double ucf = Math.Pow(fMap.GetUnits(FieldsMap.FieldType.FLOW), pMap.QExp) / fMap.GetUnits(FieldsMap.FieldType.PRESSURE);

        foreach (Node node  in  net.Nodes) {
            if (node is Tank)
                continue;

            if (node.Ke > 0.0)
                node.Ke = ucf / Math.Pow(node.Ke, pMap.QExp);
        }

        foreach (Tank tk  in  net.Tanks) {
            tk.H0 = tk.Elevation + tk.H0 / fMap.GetUnits(FieldsMap.FieldType.ELEV);
            tk.Hmin = tk.Elevation + tk.Hmin / fMap.GetUnits(FieldsMap.FieldType.ELEV);
            tk.Hmax = tk.Elevation + tk.Hmax / fMap.GetUnits(FieldsMap.FieldType.ELEV);
            tk.Area = Math.PI * Math.Pow(tk.Area / fMap.GetUnits(FieldsMap.FieldType.ELEV), 2) / 4.0;
            tk.V0 = tk.V0 / fMap.GetUnits(FieldsMap.FieldType.VOLUME);
            tk.Vmin = tk.Vmin / fMap.GetUnits(FieldsMap.FieldType.VOLUME);
            tk.Vmax = tk.Vmax / fMap.GetUnits(FieldsMap.FieldType.VOLUME);
            tk.Kb = tk.Kb / Constants.SECperDAY;
            //tk.setVolume(tk.getV0());
            tk.Concentration = tk.C0;
            tk.V1Max = tk.V1Max * tk.Vmax;
        }


        pMap.CLimit = pMap.CLimit / fMap.GetUnits(FieldsMap.FieldType.QUALITY);
        pMap.Ctol = pMap.Ctol / fMap.GetUnits(FieldsMap.FieldType.QUALITY);

        pMap.KBulk = pMap.KBulk / Constants.SECperDAY;
        pMap.KWall = pMap.KWall / Constants.SECperDAY;


        foreach (Link lk  in  net.Links) {

            if (lk.Type <= Link.LinkType.PIPE) {
                if (pMap.FormFlag == PropertiesMap.FormType.DW)
                    lk.Roughness = lk.Roughness / (1000.0 * fMap.GetUnits(FieldsMap.FieldType.ELEV));
                lk.Diameter = lk.Diameter / fMap.GetUnits(FieldsMap.FieldType.DIAM);
                lk.Lenght = lk.Lenght / fMap.GetUnits(FieldsMap.FieldType.LENGTH);

                lk.Km = 0.02517 * lk.Km / Math.Pow(lk.Diameter, 2) / Math.Pow(lk.Diameter, 2);

                lk.Kb = lk.Kb / Constants.SECperDAY;
                lk.Kw = lk.Kw / Constants.SECperDAY;
            } else if (lk is Pump) {
                Pump pump = (Pump) lk;

                if (pump.Ptype == Pump.PumpType.CONST_HP) {
                    if (pMap.UnitsFlag == PropertiesMap.UnitsType.SI)
                        pump.FlowCoefficient = pump.FlowCoefficient / fMap.GetUnits(FieldsMap.FieldType.POWER);
                } else {
                    if (pump.Ptype == Pump.PumpType.POWER_FUNC) {
                        pump.H0 = pump.H0 / fMap.GetUnits(FieldsMap.FieldType.HEAD);
                        pump.FlowCoefficient = pump.FlowCoefficient*
                                               (Math.Pow(fMap.GetUnits(FieldsMap.FieldType.FLOW), pump.N))/
                                               fMap.GetUnits(FieldsMap.FieldType.HEAD);
                    }

                    pump.Q0 = pump.Q0 / fMap.GetUnits(FieldsMap.FieldType.FLOW);
                    pump.Qmax = pump.Qmax / fMap.GetUnits(FieldsMap.FieldType.FLOW);
                    pump.Hmax = pump.Hmax / fMap.GetUnits(FieldsMap.FieldType.HEAD);
                }
            } else {
                lk.Diameter = lk.Diameter / fMap.GetUnits(FieldsMap.FieldType.DIAM);
                lk.Km = 0.02517 * lk.Km / Math.Pow(lk.Diameter, 2) / Math.Pow(lk.Diameter, 2);
                if (lk.Roughness != Constants.MISSING)
                    switch (lk.Type) {
                        case Link.LinkType.FCV:
                            lk.Roughness = lk.Roughness / fMap.GetUnits(FieldsMap.FieldType.FLOW);
                            break;
                        case Link.LinkType.PRV:
                        case Link.LinkType.PSV:
                        case Link.LinkType.PBV:
                            lk.Roughness = lk.Roughness / fMap.GetUnits(FieldsMap.FieldType.PRESSURE);
                            break;
                    }
            }

            lk.initResistance(net.PropertiesMap.FormFlag,net.PropertiesMap.HExp);
        }

        foreach (Control c_i  in  net.Controls) {


            if (c_i.Link == null) continue;
            if (c_i.Node != null) {
                Node node = c_i.Node;
                if (node is Tank)
                    c_i.Grade = node.Elevation +
                                c_i.Grade / fMap.GetUnits(FieldsMap.FieldType.ELEV);
                else
                    c_i.Grade = node.Elevation + c_i.Grade / fMap.GetUnits(FieldsMap.FieldType.PRESSURE);
            }

            if (c_i.Setting != Constants.MISSING)
                switch (c_i.Link.Type) {
                    case Link.LinkType.PRV:
                    case Link.LinkType.PSV:
                    case Link.LinkType.PBV:
                        c_i.Setting = c_i.Setting / fMap.GetUnits(FieldsMap.FieldType.PRESSURE);
                        break;
                    case Link.LinkType.FCV:
                        c_i.Setting = c_i.Setting / fMap.GetUnits(FieldsMap.FieldType.FLOW);
                        break;
                }
        }
    }



    /**
     * Initialize pump properties.
     * @param net Hydraulic network reference.
     * @throws ENException
     */
    private void InitPumps(Network net) {
        double h0 = 0.0, h1 = 0.0, h2 = 0.0, q1 = 0.0, q2 = 0.0;

        foreach (Pump pump  in  net.Pumps) {
            // Constant Hp pump
            if (pump.Ptype == Pump.PumpType.CONST_HP) {
                pump.H0 = 0.0;
                pump.FlowCoefficient = -8.814 * pump.Km;
                pump.N = -1.0;
                pump.Hmax = Constants.BIG;
                pump.Qmax = Constants.BIG;
                pump.Q0 = 1.0;
                continue;
            }

            // Set parameters for pump curves
            else if (pump.Ptype == Pump.PumpType.NOCURVE) {
                Curve curve = pump.Hcurve;
                if (curve == null) {
                    throw new ENException(ErrorCode.Err226, pump.Id);
                }
                int n = curve.Npts;
                if (n == 1) {
                    pump.Ptype = Pump.PumpType.POWER_FUNC;
                    q1 = curve.X[0];
                    h1 = curve.Y[0];
                    h0 = 1.33334 * h1;
                    q2 = 2.0 * q1;
                    h2 = 0.0;
                } else if (n == 3 && curve.X[0] == 0.0) {
                    pump.Ptype = Pump.PumpType.POWER_FUNC;
                    h0 = curve.Y[0];
                    q1 = curve.X[1];
                    h1 = curve.Y[1];
                    q2 = curve.X[2];
                    h2 = curve.Y[2];
                } else
                    pump.Ptype = Pump.PumpType.CUSTOM;

                // Compute shape factors & limits of power function pump curves
                if (pump.Ptype == Pump.PumpType.POWER_FUNC) {
                    double a, b, c;
                    if (!Utilities.GetPowerCurve(h0, h1, h2, q1, q2, out a, out b, out c))
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
            if (pump.Ptype == Pump.PumpType.CUSTOM) {
                Curve curve = pump.Hcurve;
                for (int m = 1; m < curve.Npts; m++) {
                    if (curve.Y[m] >= curve.Y[m - 1]) // Check for invalid curve
                    {
                        throw new ENException(ErrorCode.Err227, pump.Id);
                    }
                }
                pump.Qmax = curve.X[curve.Npts - 1];
                pump.Q0 = (curve.X[0] + pump.Qmax) / 2.0;
                pump.Hmax = curve.Y[0];
            }
        }

    }

    /**
     * Initialize patterns.
     * @param net Hydraulic network reference.
     * @throws ENException
     */
    private void InitPatterns(Network net) {
        foreach (Pattern par  in  net.Patterns) {
            if (par.FactorsList.Count == 0) {
                par.FactorsList.Add(1.0);
            }
        }
    }


    /**
     * Check for unlinked nodes.
     * @param net Hydraulic network reference.
     * @throws ENException
     */
    private void CheckUnlinked(Network net) {
        int[] marked = new int[net.Nodes.Count + 1];
        var nodes = net.Nodes;

        int err = 0;

        foreach(Link link in net.Links) {
            marked[nodes.IndexOf(link.FirstNode)]++;
            marked[nodes.IndexOf(link.SecondNode)]++;
        }

        int i = 0;
        foreach(Node node in nodes) {
            if (marked[i] == 0) {
                err++;
                this.Log.Error(new ENException(ErrorCode.Err233, node.Id));
            }

            if (err >= Constants.MAXERRS)
                break;

            i++;
        }

//        if (err > 0)
//            throw new ENException(200);
    }
}
}