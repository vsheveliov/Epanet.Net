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
using org.addition.epanet.log;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.network.io.input {


///<summary>Abstract input file parser.</summary>
public abstract class InputParser {

    protected string FileName;

    ///<summary>Reference to the error logger.</summary>
    protected TraceSource log;
    protected readonly List<ENException> Errors = new List<ENException>();

    protected InputParser(TraceSource log) {
        this.log = log;
    }

    public static InputParser create(Network.FileType type, TraceSource log) {
        switch (type) {
            case Network.FileType.INP_FILE:
                return new InpParser(log);
            case Network.FileType.EXCEL_FILE:
                return new ExcelParser(log);
            case Network.FileType.XML_FILE:
                return new XMLParser(log,false);
            case Network.FileType.XML_GZ_FILE:
                return new XMLParser(log,true);
            case Network.FileType.NULL_FILE:
                return new NullParser(log);
        }
        return null;
    }

    public abstract Network parse(Network net, string fileName);


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
    protected void convert(Network net) {
        initTanks(net);
        initPumps(net);
        initPatterns(net);
        checkUnlinked(net);
        convertUnits(net);
    }

    ///<summary>Adjust simulation configurations.</summary>
    public static void adjust(Network net)  {
        adjustData(net);
    }

    ///<summary>Adjust simulation configurations.</summary>
    private static void adjustData(Network net) {
        PropertiesMap m = net.getPropertiesMap();

        if (m.getPstep() <= 0) m.setPstep(3600);
        if (m.getRstep() == 0) m.setRstep(m.getPstep());
        if (m.getHstep() <= 0) m.setHstep(3600);
        if (m.getHstep() > m.getPstep()) m.setHstep(m.getPstep());
        if (m.getHstep() > m.getRstep()) m.setHstep(m.getRstep());
        if (m.getRstart() > m.getDuration()) m.setRstart(0);
        if (m.getDuration() == 0) m.setQualflag(PropertiesMap.QualType.NONE);
        if (m.getQstep() == 0) m.setQstep(m.getHstep() / 10);
        if (m.getRulestep() == 0) m.setRulestep(m.getHstep() / 10);

        m.setRulestep(Math.Min(m.getRulestep(), m.getHstep()));
        m.setQstep(Math.Min(m.getQstep(), m.getHstep()));

        if (m.getCtol() == Constants.MISSING) {
            if (m.getQualflag() == PropertiesMap.QualType.AGE)
                m.setCtol(Constants.AGETOL);
            else
                m.setCtol(Constants.CHEMTOL);
        }

        switch (m.getFlowflag()) {
            case PropertiesMap.FlowUnitsType.LPS:
            case PropertiesMap.FlowUnitsType.LPM:
            case PropertiesMap.FlowUnitsType.MLD:
            case PropertiesMap.FlowUnitsType.CMH:
            case PropertiesMap.FlowUnitsType.CMD:
                m.setUnitsflag(PropertiesMap.UnitsType.SI);
                break;
            default:
                m.setUnitsflag(PropertiesMap.UnitsType.US);
                break;
        }


        if (m.getUnitsflag() != PropertiesMap.UnitsType.SI)
            m.setPressflag(PropertiesMap.PressUnitsType.PSI);
        else if (m.getPressflag() == PropertiesMap.PressUnitsType.PSI)
            m.setPressflag(PropertiesMap.PressUnitsType.METERS);

        var ucf = 1.0;
        if (m.getUnitsflag() == PropertiesMap.UnitsType.SI)
            ucf = Math.Pow(Constants.MperFT, 2);

        if (m.getViscos() == Constants.MISSING)
            m.setViscos(Constants.VISCOS);
        else if (m.getViscos() > 1e-3)
            m.setViscos(m.getViscos() * Constants.VISCOS);
        else
            m.setViscos(m.getViscos() / ucf);

        if (m.getDiffus() == Constants.MISSING)
            m.setDiffus(Constants.DIFFUS);
        else if (m.getDiffus() > 1e-4)
            m.setDiffus(m.getDiffus() * Constants.DIFFUS);
        else
            m.setDiffus(m.getDiffus() / ucf);

        if (m.getFormflag() == PropertiesMap.FormType.HW)
            m.setHexp(1.852);
        else
            m.setHexp(2.0);

        double Rfactor = m.getRfactor();
        PropertiesMap.FormType formFlag = m.getFormflag();
        double Kbulk = m.getKbulk();

        foreach (Link link  in  net.getLinks()) {
            if (link.getType() > Link.LinkType.PIPE)
                continue;

            if (link.getKb() == Constants.MISSING)
                link.setKb(Kbulk);

            if (link.getKw() == Constants.MISSING)
            {
                if (Rfactor == 0.0)
                    link.setKw(m.getKwall());
                else if ((link.getRoughness() > 0.0) && (link.getDiameter() > 0.0)) {
                    if (formFlag == PropertiesMap.FormType.HW)
                        link.setKw(Rfactor / link.getRoughness());
                    if (formFlag == PropertiesMap.FormType.DW)
                        link.setKw(Rfactor / Math.Abs(Math.Log(link.getRoughness() / link.getDiameter())));
                    if (formFlag == PropertiesMap.FormType.CM)
                        link.setKw(Rfactor * link.getRoughness());
                }
                else
                    link.setKw(0.0);
            }
        }

        foreach (Tank tank  in  net.getTanks())
            if (tank.getKb() == Constants.MISSING)
                tank.setKb(Kbulk);

        Pattern defpat = net.getPattern(m.getDefPatId());
        if (defpat == null)
            defpat = net.getPattern("");

        if (defpat == null)
            defpat = net.getPattern("");

        foreach (Node node  in  net.getNodes()) {
            foreach (Demand d  in  node.getDemand()) {
                if (d.getPattern() == null)
                    d.setPattern(defpat);
            }
        }

        if (m.getQualflag() == PropertiesMap.QualType.NONE)
            net.getFieldsMap().getField(FieldsMap.Type.QUALITY).setEnabled(false);

    }

    /**
     * Initialize tank properties.
     * @param net Hydraulic network reference.
     * @throws ENException
     */
    private void initTanks(Network net) {
        int n = 0;
        double a;

        foreach (Tank tank  in  net.getTanks()) {
            if (tank.getArea() == 0.0)
                continue;

            int levelerr = 0;
            if (tank.getH0() > tank.getHmax() ||
                    tank.getHmin() > tank.getHmax() ||
                    tank.getH0() < tank.getHmin()
                    ) levelerr = 1;

            Curve curv = tank.getVcurve();

            if (curv != null) {
                n = curv.getNpts() - 1;
                if (tank.getHmin() < curv.getX()[0] ||
                        tank.getHmax() > curv.getX()[n])
                    levelerr = 1;
            }

            if (levelerr != 0) {
                throw new ENException(ErrorCode.Err225, tank.getId());
            } else if (curv != null) {

                tank.setVmin(Utilities.linearInterpolator(curv.getNpts(), curv.getX(),
                        curv.getY(), tank.getHmin()));
                tank.setVmax(Utilities.linearInterpolator(curv.getNpts(), curv.getX(),
                        curv.getY(), tank.getHmax()));
                tank.setV0(Utilities.linearInterpolator(curv.getNpts(), curv.getX(),
                        curv.getY(), tank.getH0()));

                a = (curv.getY()[n] - curv.getY()[0]) /
                        (curv.getX()[n] - curv.getX()[0]);
                tank.setArea(Math.Sqrt(4.0 * a / Constants.PI));
            }
        }
    }

    /**
     * Convert hydraulic structures values from user units to simulation system units.
     * @param net Hydraulic network reference.
     * @throws ENException
     */
    private void convertUnits(Network net) {
        FieldsMap fMap = net.getFieldsMap();
        PropertiesMap pMap = net.getPropertiesMap();

        foreach (Node node  in  net.getNodes()) {
            node.setElevation(node.getElevation() / fMap.getUnits(FieldsMap.Type.ELEV));
            node.setC0(new[]{node.getC0()[0] / fMap.getUnits(FieldsMap.Type.QUALITY)});
        }


        foreach (Node node  in  net.getNodes()) {
            if (node is Tank)
                continue;

            foreach (Demand d  in  node.getDemand()) {
                d.setBase(d.getBase() / fMap.getUnits(FieldsMap.Type.DEMAND));
            }
        }


        double ucf = Math.Pow(fMap.getUnits(FieldsMap.Type.FLOW), pMap.getQexp()) / fMap.getUnits(FieldsMap.Type.PRESSURE);

        foreach (Node node  in  net.getNodes()) {
            if (node is Tank)
                continue;

            if (node.getKe() > 0.0)
                node.setKe(ucf / Math.Pow(node.getKe(), pMap.getQexp()));
        }

        foreach (Tank tk  in  net.getTanks()) {
            tk.setH0(tk.getElevation() + tk.getH0() / fMap.getUnits(FieldsMap.Type.ELEV));
            tk.setHmin(tk.getElevation() + tk.getHmin() / fMap.getUnits(FieldsMap.Type.ELEV));
            tk.setHmax(tk.getElevation() + tk.getHmax() / fMap.getUnits(FieldsMap.Type.ELEV));
            tk.setArea(Constants.PI * Math.Pow(tk.getArea() / fMap.getUnits(FieldsMap.Type.ELEV), 2) / 4.0);
            tk.setV0(tk.getV0() / fMap.getUnits(FieldsMap.Type.VOLUME));
            tk.setVmin(tk.getVmin() / fMap.getUnits(FieldsMap.Type.VOLUME));
            tk.setVmax(tk.getVmax() / fMap.getUnits(FieldsMap.Type.VOLUME));
            tk.setKb(tk.getKb() / Constants.SECperDAY);
            //tk.setVolume(tk.getV0());
            tk.setConcentration(tk.getC0());
            tk.setV1max(tk.getV1max() * tk.getVmax());
        }


        pMap.setClimit(pMap.getClimit() / fMap.getUnits(FieldsMap.Type.QUALITY));
        pMap.setCtol(pMap.getCtol() / fMap.getUnits(FieldsMap.Type.QUALITY));

        pMap.setKbulk(pMap.getKbulk() / Constants.SECperDAY);
        pMap.setKwall(pMap.getKwall() / Constants.SECperDAY);


        foreach (Link lk  in  net.getLinks()) {

            if (lk.getType() <= Link.LinkType.PIPE) {
                if (pMap.getFormflag() == PropertiesMap.FormType.DW)
                    lk.setRoughness(lk.getRoughness() / (1000.0 * fMap.getUnits(FieldsMap.Type.ELEV)));
                lk.setDiameter(lk.getDiameter() / fMap.getUnits(FieldsMap.Type.DIAM));
                lk.setLenght(lk.getLenght() / fMap.getUnits(FieldsMap.Type.LENGTH));

                lk.setKm(0.02517 * lk.getKm() / Math.Pow(lk.getDiameter(), 2) / Math.Pow(lk.getDiameter(), 2));

                lk.setKb(lk.getKb() / Constants.SECperDAY);
                lk.setKw(lk.getKw() / Constants.SECperDAY);
            } else if (lk is Pump) {
                Pump pump = (Pump) lk;

                if (pump.getPtype() == Pump.Type.CONST_HP) {
                    if (pMap.getUnitsflag() == PropertiesMap.UnitsType.SI)
                        pump.setFlowCoefficient(pump.getFlowCoefficient() / fMap.getUnits(FieldsMap.Type.POWER));
                } else {
                    if (pump.getPtype() == Pump.Type.POWER_FUNC) {
                        pump.setH0(pump.getH0() / fMap.getUnits(FieldsMap.Type.HEAD));
                        pump.setFlowCoefficient(pump.getFlowCoefficient()*
                                                (Math.Pow(fMap.getUnits(FieldsMap.Type.FLOW), pump.getN()))/
                                                fMap.getUnits(FieldsMap.Type.HEAD));
                    }

                    pump.setQ0(pump.getQ0() / fMap.getUnits(FieldsMap.Type.FLOW));
                    pump.setQmax(pump.getQmax() / fMap.getUnits(FieldsMap.Type.FLOW));
                    pump.setHmax(pump.getHmax() / fMap.getUnits(FieldsMap.Type.HEAD));
                }
            } else {
                lk.setDiameter(lk.getDiameter() / fMap.getUnits(FieldsMap.Type.DIAM));
                lk.setKm(0.02517 * lk.getKm() / Math.Pow(lk.getDiameter(), 2) / Math.Pow(lk.getDiameter(), 2));
                if (lk.getRoughness() != Constants.MISSING)
                    switch (lk.getType()) {
                        case Link.LinkType.FCV:
                            lk.setRoughness(lk.getRoughness() / fMap.getUnits(FieldsMap.Type.FLOW));
                            break;
                        case Link.LinkType.PRV:
                        case Link.LinkType.PSV:
                        case Link.LinkType.PBV:
                            lk.setRoughness(lk.getRoughness() / fMap.getUnits(FieldsMap.Type.PRESSURE));
                            break;
                    }
            }

            lk.initResistance(net.getPropertiesMap().getFormflag(),net.getPropertiesMap().getHexp());
        }

        foreach (Control c_i  in  net.getControls()) {


            if (c_i.getLink() == null) continue;
            if (c_i.getNode() != null) {
                Node node = c_i.getNode();
                if (node is Tank)
                    c_i.setGrade(node.getElevation() +
                            c_i.getGrade() / fMap.getUnits(FieldsMap.Type.ELEV));
                else
                    c_i.setGrade(node.getElevation() + c_i.getGrade() / fMap.getUnits(FieldsMap.Type.PRESSURE));
            }

            if (c_i.getSetting() != Constants.MISSING)
                switch (c_i.getLink().getType()) {
                    case Link.LinkType.PRV:
                    case Link.LinkType.PSV:
                    case Link.LinkType.PBV:
                        c_i.setSetting(c_i.getSetting() / fMap.getUnits(FieldsMap.Type.PRESSURE));
                        break;
                    case Link.LinkType.FCV:
                        c_i.setSetting(c_i.getSetting() / fMap.getUnits(FieldsMap.Type.FLOW));
                        break;
                }
        }
    }



    /**
     * Initialize pump properties.
     * @param net Hydraulic network reference.
     * @throws ENException
     */
    private void initPumps(Network net) {
        double h0 = 0.0, h1 = 0.0, h2 = 0.0, q1 = 0.0, q2 = 0.0;

        foreach (Pump pump  in  net.getPumps()) {
            // Constant Hp pump
            if (pump.getPtype() == Pump.Type.CONST_HP) {
                pump.setH0(0.0);
                pump.setFlowCoefficient(-8.814 * pump.getKm());
                pump.setN(-1.0);
                pump.setHmax(Constants.BIG);
                pump.setQmax(Constants.BIG);
                pump.setQ0(1.0);
                continue;
            }

            // Set parameters for pump curves
            else if (pump.getPtype() == Pump.Type.NOCURVE) {
                Curve curve = pump.getHcurve();
                if (curve == null) {
                    throw new ENException(ErrorCode.Err226, pump.getId());
                }
                int n = curve.getNpts();
                if (n == 1) {
                    pump.setPtype(Pump.Type.POWER_FUNC);
                    q1 = curve.getX()[0];
                    h1 = curve.getY()[0];
                    h0 = 1.33334 * h1;
                    q2 = 2.0 * q1;
                    h2 = 0.0;
                } else if (n == 3 && curve.getX()[0] == 0.0) {
                    pump.setPtype(Pump.Type.POWER_FUNC);
                    h0 = curve.getY()[0];
                    q1 = curve.getX()[1];
                    h1 = curve.getY()[1];
                    q2 = curve.getX()[2];
                    h2 = curve.getY()[2];
                } else
                    pump.setPtype(Pump.Type.CUSTOM);

                // Compute shape factors & limits of power function pump curves
                if (pump.getPtype() == Pump.Type.POWER_FUNC) {
                    double a, b, c;
                    if (!Utilities.getPowerCurve(h0, h1, h2, q1, q2, out a, out b, out c))
                        throw new ENException(ErrorCode.Err227, pump.getId());


                    pump.setH0(-a);
                    pump.setFlowCoefficient(-b);
                    pump.setN(c);
                    pump.setQ0(q1);
                    pump.setQmax(Math.Pow(-a / b, (1.0 / c)));
                    pump.setHmax(h0);
                }
            }

            // Assign limits to custom pump curves
            if (pump.getPtype() == Pump.Type.CUSTOM) {
                Curve curve = pump.getHcurve();
                for (int m = 1; m < curve.getNpts(); m++) {
                    if (curve.getY()[m] >= curve.getY()[m - 1]) // Check for invalid curve
                    {
                        throw new ENException(ErrorCode.Err227, pump.getId());
                    }
                }
                pump.setQmax(curve.getX()[curve.getNpts() - 1]);
                pump.setQ0((curve.getX()[0] + pump.getQmax()) / 2.0);
                pump.setHmax(curve.getY()[0]);
            }
        }

    }

    /**
     * Initialize patterns.
     * @param net Hydraulic network reference.
     * @throws ENException
     */
    private void initPatterns(Network net) {
        foreach (Pattern par  in  net.getPatterns()) {
            if (par.getFactorsList().Count == 0) {
                par.getFactorsList().Add(1.0);
            }
        }
    }


    /**
     * Check for unlinked nodes.
     * @param net Hydraulic network reference.
     * @throws ENException
     */
    private void checkUnlinked(Network net) {
        int[] marked = new int[net.getNodes().Length + 1];
        List<Link> links = new List<Link>(net.getLinks());
        List<Node> nodes = new List<Node>(net.getNodes());

        int err = 0;

        foreach (Link link  in  links) {
            marked[nodes.IndexOf(link.getFirst())]++;
            marked[nodes.IndexOf(link.getSecond())]++;
        }

        int i = 0;
        foreach (Node node  in  nodes) {
            if (marked[i] == 0) {
                err++;
                log.Error(new ENException(ErrorCode.Err233, node.getId()));
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