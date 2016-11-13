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
using System.Globalization;
using System.IO;
using System.Text;
using Epanet;
using org.addition.epanet.log;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.network.io.input {


///<summary>INP parser class.</summary>
public class InpParser : InputParser {
    
    private int _lineNumber;
    private Rule.Rulewords ruleState;    // Last rule op
    private Rule currentRule;       // Current rule

    private static readonly string[] OPTION_VALUE_KEYWORDS = new string[]{
            Keywords.w_TOLERANCE, Keywords.w_DIFFUSIVITY, Keywords.w_DAMPLIMIT, Keywords.w_VISCOSITY, Keywords.w_SPECGRAV, Keywords.w_TRIALS, Keywords.w_ACCURACY,
            Keywords.w_HTOL, Keywords.w_QTOL, Keywords.w_RQTOL, Keywords.w_CHECKFREQ, Keywords.w_MAXCHECK, Keywords.w_EMITTER, Keywords.w_DEMAND
    };

    public InpParser(TraceSource log):base(log) {
        currentRule = null;
        ruleState = (Rule.Rulewords)(-1);
    }

    protected void LogException(Network.SectType section, ErrorCode err, string line, IList<string> tokens) {
        if(err == ErrorCode.Ok)
            return;

        string arg = section == Network.SectType.OPTIONS ? line : tokens[0];

        EpanetParseException parseException = new EpanetParseException(
            err,
            this._lineNumber,
            this.FileName,
            section.reportStr(),
            arg);

        base.Errors.Add(parseException);

        this.log.Error(parseException.ToString());

    }

    /// <summary>Parse demands and time patterns first.</summary>
    /// <param name="net"></param>
    /// <param name="f"></param>
 
    private void parsePC(Network net, string f)  {
        _lineNumber = 0;
        Network.SectType sectionType = (Network.SectType)(-1);
        StreamReader buffReader;

        try {
            buffReader = new StreamReader(f, Encoding.Default); // "ISO-8859-1"
        } catch (IOException e) {
            throw new ENException(ErrorCode.Err302);
        }

        try {
            string line;
            while ((line = buffReader.ReadLine()) != null) {
                _lineNumber++;

                line = line.Trim();

                if(string.IsNullOrEmpty(line))
                    continue;

                if (line[0] == '[') {
                    if (line.StartsWith("[PATTERNS]")) {
                        sectionType = Network.SectType.PATTERNS;
                    } else if (line.StartsWith("[CURVES]"))
                        sectionType = Network.SectType.CURVES;
                    else
                        sectionType = (Network.SectType)(-1);
                    continue;
                }

                if (sectionType != (Network.SectType)(-1)) {
                    if (line.IndexOf(';') >= 0)
                        line = line.Substring(0, line.IndexOf(';'));

                    if (line.Length == 0)
                        continue;

                    string[] tokens = Tokenize(line);

                    if (tokens.Length == 0) continue;

                    try {
                        switch (sectionType) {
                        case Network.SectType.PATTERNS:
                            this.parsePattern(net, tokens);
                            break;
                        case Network.SectType.CURVES:
                            this.parseCurve(net, tokens);
                            break;
                        }
                    } catch (ENException e) {
                        LogException(sectionType, e.getCodeID(), line, tokens);
                    }
                }

                if (this.Errors.Count == Constants.MAXERRS) break;
            }
        } catch (IOException) {
            throw new ENException(ErrorCode.Err302);
        }

        if(this.Errors.Count > 0)
            throw new ENException(ErrorCode.Err200);

        try {
            buffReader.Close();
        } catch (IOException) {
            throw new ENException(ErrorCode.Err302);
        }
    }

    // Parse INP file
    public override Network parse(Network net, string f) {
        this.FileName = Path.GetFullPath(f);

        parsePC(net, f);

        int errSum = 0;
        //int lineCount = 0;
        Network.SectType sectionType = (Network.SectType)(-1);
        TextReader buffReader;

        try {
            buffReader = new StreamReader(f, Encoding.Default); // "ISO-8859-1"
        } catch (IOException) {
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
                if(string.IsNullOrEmpty(line))
                    continue;

                string[] tokens = Tokenize(line);
                if (tokens.Length == 0) continue;

                try {

                    if (tokens[0].IndexOf('[') >= 0) {
                        Network.SectType type = findSectionType(tokens[0]);
                        if (type >= 0 )
                            sectionType = type;
                        else {
                            sectionType = (Network.SectType)(-1);
                            log.Error(null, null, string.Format("Unknown section type : {0}", tokens[0]));
                            //throw new ENException(201, lineCount);
                        }
                    } else if (sectionType >= 0) {

                        switch (sectionType) {
                            case Network.SectType.TITLE:
                                net.getTitleText().Add(line);
                                break;
                            case Network.SectType.JUNCTIONS:
                                parseJunction(net, tokens, comment);
                                break;

                            case Network.SectType.RESERVOIRS:
                            case Network.SectType.TANKS:
                                parseTank(net, tokens, comment);
                                break;

                            case Network.SectType.PIPES:
                                parsePipe(net, tokens, comment);
                                break;
                            case Network.SectType.PUMPS:
                                parsePump(net, tokens, comment);
                                break;
                            case Network.SectType.VALVES:
                                parseValve(net, tokens, comment);
                                break;
                            case Network.SectType.CONTROLS:
                                parseControl(net, tokens);
                                break;

                            case Network.SectType.RULES:
                                parseRule(net, tokens, line);
                                break;

                            case Network.SectType.DEMANDS:
                                parseDemand(net, tokens);
                                break;
                            case Network.SectType.SOURCES:
                                parseSource(net, tokens);
                                break;
                            case Network.SectType.EMITTERS:
                                parseEmitter(net, tokens);
                                break;
                            case Network.SectType.QUALITY:
                                parseQuality(net, tokens);
                                break;
                            case Network.SectType.STATUS:
                                parseStatus(net, tokens);
                                break;
                            case Network.SectType.ENERGY:
                                parseEnergy(net, tokens);
                                break;
                            case Network.SectType.REACTIONS:
                                parseReact(net, tokens);
                                break;
                            case Network.SectType.MIXING:
                                parseMixing(net, tokens);
                                break;
                            case Network.SectType.REPORT:
                                parseReport(net, tokens);
                                break;
                            case Network.SectType.TIMES:
                                parseTime(net, tokens);
                                break;
                            case Network.SectType.OPTIONS:
                                parseOption(net, tokens);
                                break;
                            case Network.SectType.COORDINATES:
                                parseCoordinate(net, tokens);
                                break;
                            case Network.SectType.VERTICES:
                                parseVertice(net, tokens);
                                break;
                            case Network.SectType.LABELS:
                                parseLabel(net, tokens);
                                break;
                        }
                    }
                } catch (ENException e) {
                    LogException(sectionType, e.getCodeID(), line, tokens);
                    errSum++;
                }
                if (errSum == Constants.MAXERRS) break;
            }
        } catch (IOException) {
            throw new ENException(ErrorCode.Err302);
        }

        if (errSum > 0) {
            throw new ENException(ErrorCode.Err200);
        }

        try {
            buffReader.Close();
        } catch (IOException) {
            throw new ENException(ErrorCode.Err302);
        }

        adjust(net);

        net.getFieldsMap().prepare(net.getPropertiesMap().getUnitsflag(),
                net.getPropertiesMap().getFlowflag(),
                net.getPropertiesMap().getPressflag(),
                net.getPropertiesMap().getQualflag(),
                net.getPropertiesMap().getChemUnits(),
                net.getPropertiesMap().getSpGrav(),
                net.getPropertiesMap().getHstep());

        convert(net);
        return net;
    }

    protected void parseJunction(Network net, string[] Tok, string comment) {
        int n = Tok.Length;
        double el, y = 0.0d;
        Pattern p = null;

        Node nodeRef = new Node();

        if (net.getNode(Tok[0]) != null)
            throw new ENException(ErrorCode.Err215, Network.SectType.JUNCTIONS, Tok[0]);

        net.addJunction(Tok[0], nodeRef);

        if (n < 2)
            throw new ENException(ErrorCode.Err201);

        if (!Tok[1].ToDouble(out el))
            throw new ENException(ErrorCode.Err202, Network.SectType.JUNCTIONS, Tok[0]);

        if (n >= 3 && !Tok[2].ToDouble(out y))
            throw new ENException(ErrorCode.Err202, Network.SectType.JUNCTIONS, Tok[0]);

        if (n >= 4) {
            p = net.getPattern(Tok[3]);
            if (p == null)
                throw new ENException(ErrorCode.Err205);
        }

        nodeRef.setElevation(el);
        nodeRef.setC0(new[]{0.0});
        nodeRef.setSource(null);
        nodeRef.setKe(0.0);
        nodeRef.setReportFlag(false);

        if (!string.IsNullOrEmpty(comment))
            nodeRef.setComment(comment);

        if (n >= 3) {
            Demand demand = new Demand(y, p);
            nodeRef.getDemand().Add(demand);

            nodeRef.setInitDemand(y);
        } else
            nodeRef.setInitDemand(Constants.MISSING);
    }


    protected void parseTank(Network net, string[] Tok, string comment) {
        int n = Tok.Length;
        Pattern p = null;
        Curve c = null;
        double el,
                initlevel = 0.0d,
                minlevel = 0.0d,
                maxlevel = 0.0d,
                minvol = 0.0d,
                diam = 0.0d,
                area;

        Tank tank = new Tank();
        if (comment.Length > 0)
            tank.setComment(comment);

        if (net.getNode(Tok[0]) != null)
            throw new ENException(ErrorCode.Err215);

        net.addTank(Tok[0], tank);

        if (n < 2)
            throw new ENException(ErrorCode.Err201);

        if (!Tok[1].ToDouble(out el))
            throw new ENException(ErrorCode.Err202);

        if (n <= 3) {
            if (n == 3) {
                p = net.getPattern(Tok[2]);
                if (p == null)
                    throw new ENException(ErrorCode.Err205);
            }
        } else if (n < 6)
            throw new ENException(ErrorCode.Err201);
        else {
            if (!Tok[2].ToDouble(out initlevel))
                throw new ENException(ErrorCode.Err202, Network.SectType.TANKS, Tok[0]);
           
            if (!Tok[3].ToDouble(out minlevel))
                throw new ENException(ErrorCode.Err202, Network.SectType.TANKS, Tok[0]);
          
            if (!Tok[4].ToDouble(out maxlevel))
                throw new ENException(ErrorCode.Err202, Network.SectType.TANKS, Tok[0]);
            if (!Tok[5].ToDouble(out diam))
                throw new ENException(ErrorCode.Err202, Network.SectType.TANKS, Tok[0]);

            if (diam < 0.0)
                throw new ENException(ErrorCode.Err202, Network.SectType.TANKS, Tok[0]);

            if (n >= 7
                    && !Tok[6].ToDouble(out minvol))
                throw new ENException(ErrorCode.Err202, Network.SectType.TANKS, Tok[0]);

            if (n == 8) {
                c = net.getCurve(Tok[7]);
                if(c == null)
                    throw new ENException(ErrorCode.Err202, Network.SectType.TANKS, Tok[0]);
            }
        }

        tank.setReportFlag(false);
        tank.setElevation(el);
        tank.setC0(new[]{0.0d});
        tank.setSource(null);
        tank.setKe(0.0);

        tank.setH0(initlevel);
        tank.setHmin(minlevel);
        tank.setHmax(maxlevel);
        tank.setArea(diam);
        tank.setPattern(p);
        tank.setKb(Constants.MISSING);

        area = Constants.PI * diam * diam / 4.0d;

        tank.setVmin(area * minlevel);
        if (minvol > 0.0)
            tank.setVmin(minvol);

        tank.setV0(tank.getVmin() + area * (initlevel - minlevel));
        tank.setVmax(tank.getVmin() + area * (maxlevel - minlevel));

        tank.setVcurve(c);
        tank.setMixModel(Tank.MixType.MIX1);
        tank.setV1max(1.0);
    }


    protected void parsePipe(Network net, string[] Tok, string comment) {
        Node j1, j2;
        int n = Tok.Length;
        Link.LinkType type = Link.LinkType.PIPE;
        Link.StatType status = Link.StatType.OPEN;
        double length, diam, rcoeff, lcoeff = 0.0d;

        Link link = new Link();

        if (net.getLink(Tok[0]) != null)
            throw new ENException(ErrorCode.Err215);

        net.addPipe(Tok[0], link);

        if (n < 6)
            throw new ENException(ErrorCode.Err201);

        if ((j1 = net.getNode(Tok[1])) == null ||
                (j2 = net.getNode(Tok[2])) == null
                ) throw new ENException(ErrorCode.Err203);


        if (j1 == j2) throw new ENException(ErrorCode.Err222);

        if (!Tok[3].ToDouble(out length) ||
                !Tok[4].ToDouble(out diam) ||
                !Tok[5].ToDouble(out rcoeff)
                ) throw new ENException(ErrorCode.Err202);


        if (length <= 0.0 || diam <= 0.0 || rcoeff <= 0.0) throw new ENException(ErrorCode.Err202);

        if (n == 7) {
            if (Tok[6].match(Link.LinkType.CV.ParseStr())) type = Link.LinkType.CV;
            else if (Tok[6].match(Link.StatType.CLOSED.ParseStr())) status = Link.StatType.CLOSED;
            else if (Tok[6].match(Link.StatType.OPEN.ParseStr())) status = Link.StatType.OPEN;
            else if (!Tok[6].ToDouble(out lcoeff)) throw new ENException(ErrorCode.Err202);
        }

        if (n == 8) {
            if (!Tok[6].ToDouble(out lcoeff)) throw new ENException(ErrorCode.Err202);
            if (Tok[7].match(Link.LinkType.CV.ParseStr())) type = Link.LinkType.CV;
            else if (Tok[7].match(Link.StatType.CLOSED.ParseStr())) status = Link.StatType.CLOSED;
            else if (Tok[7].match(Link.StatType.OPEN.ParseStr())) status = Link.StatType.OPEN;
            else
                throw new ENException(ErrorCode.Err202);
        }

        if (lcoeff < 0.0) throw new ENException(ErrorCode.Err202);

        link.setFirst(j1);
        link.setSecond(j2);
        link.setLenght(length);
        link.setDiameter(diam);
        link.setRoughness(rcoeff);
        link.setKm(lcoeff);
        link.setKb(Constants.MISSING);
        link.setKw(Constants.MISSING);
        link.setType(type);
        link.setStatus(status);
        link.setReportFlag(false);
        if (!string.IsNullOrEmpty(comment))
            link.setComment(comment);
    }


    protected void parsePump(Network net, string[] Tok, string comment) {
        int j, m, n = Tok.Length;
        Node j1, j2;
        double y;
        double[] X = new double[6];

        Pump pump = new Pump();

        if (net.getLink(Tok[0]) != null)
            throw new ENException(ErrorCode.Err215);

        net.addPump(Tok[0], pump);

        if (n < 4)
            throw new ENException(ErrorCode.Err201);
        if ((j1 = net.getNode(Tok[1])) == null || (j2 = net.getNode(Tok[2])) == null) throw new ENException(ErrorCode.Err203);
        if (j1 == j2) throw new ENException(ErrorCode.Err222);

        // Link attributes
        pump.setFirst(j1);
        pump.setSecond(j2);
        pump.setDiameter(0);
        pump.setLenght(0.0d);
        pump.setRoughness(1.0d);
        pump.setKm(0.0d);
        pump.setKb(0.0d);
        pump.setKw(0.0d);
        pump.setType(Link.LinkType.PUMP);
        pump.setStatus(Link.StatType.OPEN);
        pump.setReportFlag(false);

        // Pump attributes
        pump.setPtype(Pump.Type.NOCURVE);
        pump.setHcurve(null);
        pump.setEcurve(null);
        pump.setUpat(null);
        pump.setEcost(0.0d);
        pump.setEpat(null);
        if (comment.Length > 0) pump.setComment(comment);

        if (Tok[3].ToDouble(out X[0])) {

            m = 1;
            for (j = 4; j < n; j++) {
                if (!Tok[j].ToDouble(out X[m])) throw new ENException(ErrorCode.Err202);
                m++;
            }
            getpumpcurve(Tok, pump, m, X);
            return; /* If 4-th token is a number then input follows Version 1.x format  so retrieve pump curve parameters */

        }

        m = 4;
        while (m < n) {

            if (Tok[m - 1].match(Keywords.w_POWER)) {
                y = double.Parse(Tok[m]);
                if (y <= 0.0) throw new ENException(ErrorCode.Err202);
                pump.setPtype(Pump.Type.CONST_HP);
                pump.setKm(y);
            } else if (Tok[m - 1].match(Keywords.w_HEAD)) {
                Curve t = net.getCurve(Tok[m]);
                if (t == null) throw new ENException(ErrorCode.Err206);
                pump.setHcurve(t);
            } else if (Tok[m - 1].match(Keywords.w_PATTERN)) {
                Pattern p = net.getPattern(Tok[m]);
                if (p == null) throw new ENException(ErrorCode.Err205);
                pump.setUpat(p);
            } else if (Tok[m - 1].match(Keywords.w_SPEED)) {
                if (!Tok[m].ToDouble(out y)) throw new ENException(ErrorCode.Err202);
                if (y < 0.0) throw new ENException(ErrorCode.Err202);
                pump.setRoughness(y);
            } else
                throw new ENException(ErrorCode.Err201);
            m = m + 2;
        }
    }


    protected void parseValve(Network net, string[] Tok, string comment) {
        Node j1, j2;
        int n = Tok.length();
        Link.StatType status = Link.StatType.ACTIVE;
        Link.LinkType type;

        double diam, setting, lcoeff = 0.0;

        Valve valve = new Valve();

        if (net.getLink(Tok[0]) != null)
            throw new ENException(ErrorCode.Err215);

        net.addValve(Tok[0], valve);

        if (n < 6) throw new ENException(ErrorCode.Err201);
        if ((j1 = net.getNode(Tok[1])) == null ||
                (j2 = net.getNode(Tok[2])) == null
                ) throw new ENException(ErrorCode.Err203);

        if (j1 == j2)
            throw new ENException(ErrorCode.Err222);

        //if (Utilities.match(Tok[4], Keywords.w_PRV)) type = LinkType.PRV;
        //else if (Utilities.match(Tok[4], Keywords.w_PSV)) type = LinkType.PSV;
        //else if (Utilities.match(Tok[4], Keywords.w_PBV)) type = LinkType.PBV;
        //else if (Utilities.match(Tok[4], Keywords.w_FCV)) type = LinkType.FCV;
        //else if (Utilities.match(Tok[4], Keywords.w_TCV)) type = LinkType.TCV;
        //else if (Utilities.match(Tok[4], Keywords.w_GPV)) type = LinkType.GPV;
        
        if(!EnumsTxt.TryParse(Tok[4], out type))
            throw new ENException(ErrorCode.Err201);

        if (!Tok[3].ToDouble(out diam)) {
            throw new ENException(ErrorCode.Err202);
        }

        if (diam <= 0.0) throw new ENException(ErrorCode.Err202);

        if (type == Link.LinkType.GPV) {
            Curve t;
            if ((t = net.getCurve(Tok[5])) == null)
                throw new ENException(ErrorCode.Err206);

            List<Curve> curv = new List<Curve>(net.getCurves());
            setting = curv.IndexOf(t);
            log.Warning("GPV Valve, index as roughness !");
            valve.setCurve(t);
            status = Link.StatType.OPEN;
        } else if (!Tok[5].ToDouble(out setting)) {
            throw new ENException(ErrorCode.Err202);
        }

        if (n >= 7)

            if (!Tok[6].ToDouble(out lcoeff)) {
                throw new ENException(ErrorCode.Err202);
            }


        if ((j1 is Tank || j2 is Tank) &&
                (type == Link.LinkType.PRV || type == Link.LinkType.PSV || type == Link.LinkType.FCV))
            throw new ENException(ErrorCode.Err219);

        if (!valvecheck(net, type, j1, j2))
            throw new ENException(ErrorCode.Err220);


        valve.setFirst(j1);
        valve.setSecond(j2);
        valve.setDiameter(diam);
        valve.setLenght(0.0d);
        valve.setRoughness(setting);
        valve.setKm(lcoeff);
        valve.setKb(0.0d);
        valve.setKw(0.0d);
        valve.setType(type);
        valve.setStatus(status);
        valve.setReportFlag(false);
        if (comment.Length > 0)
            valve.setComment(comment);
    }

    private bool valvecheck(Network net, Link.LinkType type, Node j1, Node j2) {
        // Examine each existing valve
        foreach (Valve vk  in  net.getValves()) {
            Node vj1 = vk.getFirst();
            Node vj2 = vk.getSecond();
            Link.LinkType vtype = vk.getType();

            if (vtype == Link.LinkType.PRV && type == Link.LinkType.PRV) {
                if (vj2 == j2 ||
                        vj2 == j1 ||
                        vj1 == j2) return (false);
            }

            if (vtype == Link.LinkType.PSV && type == Link.LinkType.PSV) {
                if (vj1 == j1 ||
                        vj1 == j2 ||
                        vj2 == j1) return (false);
            }

            if (vtype == Link.LinkType.PSV && type == Link.LinkType.PRV && vj1 == j2) return (false);
            if (vtype == Link.LinkType.PRV && type == Link.LinkType.PSV && vj2 == j1) return (false);

            if (vtype == Link.LinkType.FCV && type == Link.LinkType.PSV && vj2 == j1) return (false);
            if (vtype == Link.LinkType.FCV && type == Link.LinkType.PRV && vj1 == j2) return (false);

            if (vtype == Link.LinkType.PSV && type == Link.LinkType.FCV && vj1 == j2) return (false);
            if (vtype == Link.LinkType.PRV && type == Link.LinkType.FCV && vj2 == j1) return (false);
        }
        return (true);
    }

    private void getpumpcurve(string[] Tok, Pump pump, int n, double[] X) {
        double h0, h1, h2, q1, q2;

        if (n == 1) {
            if (X[0] <= 0.0) throw new ENException(ErrorCode.Err202);
            pump.setPtype(Pump.Type.CONST_HP);
            pump.setKm(X[0]);
        } else {
            if (n == 2) {
                q1 = X[1];
                h1 = X[0];
                h0 = 1.33334 * h1;
                q2 = 2.0 * q1;
                h2 = 0.0;
            } else if (n >= 5) {
                h0 = X[0];
                h1 = X[1];
                q1 = X[2];
                h2 = X[3];
                q2 = X[4];
            } else throw new ENException(ErrorCode.Err202);
            pump.setPtype(Pump.Type.POWER_FUNC);
            double a, b, c;
            if (!Utilities.getPowerCurve(h0, h1, h2, q1, q2, out a, out b, out c))
                throw new ENException(ErrorCode.Err206);

            pump.setH0(-a);
            pump.setFlowCoefficient(-b);
            pump.setN(c);
            pump.setQ0(q1);
            pump.setQmax(Math.Pow(-a / b, 1.0 / c));
            pump.setHmax(h0);
        }
    }

    protected void parsePattern(Network net, string[] tok) {
        Pattern pat;

        if (net.getPatterns().size() == 0) {
            pat = new Pattern();
            net.addPattern(tok[0], pat);
        } else {
            pat = net.getPattern(tok[0]);

            if (pat == null) {
                pat = new Pattern();
                net.addPattern(tok[0], pat);
            }
        }

        for (int i = 1; i < tok.Length; i++) {
            double x;
           
            if(!tok[i].ToDouble(out x))
                throw new ENException(ErrorCode.Err202);

            pat.add(x);
        }
    }


    protected void parseCurve(Network net, string[] tok) {
        Curve cur;

        if (net.getCurves().size() == 0) {
            cur = new Curve();
            net.addCurve(tok[0], cur);
        } else {
            cur = net.getCurve(tok[0]);

            if (cur == null) {
                cur = new Curve();
                net.addCurve(tok[0], cur);
            }
        }

        double x, y;

        if (!tok[1].ToDouble(out x) || !tok[2].ToDouble(out y))
            throw new ENException(ErrorCode.Err202);

        cur.getX().Add(x);
        cur.getY().Add(y);
    }

    protected void parseCoordinate(Network net, string[] tok) {
        if (tok.Length < 3)
            throw new ENException(ErrorCode.Err201);

        Node nodeRef = net.getNode(tok[0]);

        if (nodeRef == null)
            throw new ENException(ErrorCode.Err203);

        double x, y;

        if (!tok[1].ToDouble(out x) || !tok[2].ToDouble(out y))
            throw new ENException(ErrorCode.Err202);

        nodeRef.setPosition(new Point(x, y));
    }

    protected void parseLabel(Network net, string[] tok) {
        if (tok.Length < 3)
            throw new ENException(ErrorCode.Err201);

        Label l = new Label();
        double x;
        double y;

        if (!tok[0].ToDouble(out x) || !tok[1].ToDouble(out y))
            throw new ENException(ErrorCode.Err202);

        l.setPosition(new Point(x, y));
        //if (tok[2].length() > 1)
        //    l.setText(tok[2].substring(1, tok[2].length() - 1));
        for (int i = 2; i < tok.Length; i++)
            if (l.getText().Length == 0)
                l.setText(tok[i].Replace("\"", ""));
            else
                l.setText(l.getText() + " " + tok[i].Replace("\"", ""));

        net.getLabels().Add(l);
    }

    protected void parseVertice(Network net, string[] tok) {
        if (tok.Length < 3)
            throw new ENException(ErrorCode.Err201);

        Link linkRef = net.getLink(tok[0]);

        if (linkRef == null)
            throw new ENException(ErrorCode.Err204);

        double x;
        double y;

        if(!tok[1].ToDouble(out x) || !tok[2].ToDouble(out y))
            throw new ENException(ErrorCode.Err202);

        linkRef.getVertices().Add(new Point(x, y));
    }

    protected void parseControl(Network net, string[] Tok) {
        int n = Tok.Length;
        Link.StatType status = Link.StatType.ACTIVE;

        double setting = Constants.MISSING, time = 0.0, level = 0.0;

        if (n < 6)
            throw new ENException(ErrorCode.Err201);

        Node nodeRef = null;
        Link linkRef = net.getLink(Tok[1]);

        if (linkRef == null) throw new ENException(ErrorCode.Err204);

        Link.LinkType ltype = linkRef.getType();

        if (ltype == Link.LinkType.CV) throw new ENException(ErrorCode.Err207);

        if (Tok[2].match(Link.StatType.OPEN.ParseStr())) {
            status = Link.StatType.OPEN;
            if (ltype == Link.LinkType.PUMP) setting = 1.0;
            if (ltype == Link.LinkType.GPV) setting = linkRef.getRoughness();
        } else if (Tok[2].match(Link.StatType.CLOSED.ParseStr())) {
            status = Link.StatType.CLOSED;
            if (ltype == Link.LinkType.PUMP) setting = 0.0;
            if (ltype == Link.LinkType.GPV) setting = linkRef.getRoughness();
        } else if (ltype == Link.LinkType.GPV)
            throw new ENException(ErrorCode.Err206);
        else if (!Tok[2].ToDouble(out setting))
            throw new ENException(ErrorCode.Err202);

        if (ltype == Link.LinkType.PUMP || ltype == Link.LinkType.PIPE) {
            if (setting != Constants.MISSING) {
                if (setting < 0.0) throw new ENException(ErrorCode.Err202);
                else if (setting == 0.0) status = Link.StatType.CLOSED;
                else status = Link.StatType.OPEN;
            }
        }

        Control.ControlType ctype;

        if (Tok[4].match(Keywords.w_TIME))
            ctype = Control.ControlType.TIMER;
        else if (Tok[4].match(Keywords.w_CLOCKTIME))
            ctype = Control.ControlType.TIMEOFDAY;
        else {
            if (n < 8)
                throw new ENException(ErrorCode.Err201);
            if ((nodeRef = net.getNode(Tok[5])) == null)
                throw new ENException(ErrorCode.Err203);
            if (Tok[6].match(Keywords.w_BELOW)) ctype = Control.ControlType.LOWLEVEL;
            else if (Tok[6].match(Keywords.w_ABOVE)) ctype = Control.ControlType.HILEVEL;
            else
                throw new ENException(ErrorCode.Err201);
        }

        switch (ctype) {
        case Control.ControlType.TIMER:
        case Control.ControlType.TIMEOFDAY:
            if (n == 6) time = Utilities.getHour(Tok[5], "");
            if (n == 7) time = Utilities.getHour(Tok[5], Tok[6]);
            if (time < 0.0) throw new ENException(ErrorCode.Err201);
            break;
        case Control.ControlType.LOWLEVEL:
        case Control.ControlType.HILEVEL:
            if (!Tok[7].ToDouble(out level)) {
                throw new ENException(ErrorCode.Err202);
            }
            break;
        }

        Control cntr = new Control();
        cntr.setLink(linkRef);
        cntr.setNode(nodeRef);
        cntr.setType(ctype);
        cntr.setStatus(status);
        cntr.setSetting(setting);
        cntr.setTime((long) (3600.0 * time));
        if (ctype == Control.ControlType.TIMEOFDAY)
            cntr.setTime(cntr.getTime() % Constants.SECperDAY);
        cntr.setGrade(level);

        net.addControl(cntr);
    }


    protected void parseSource(Network net, string[] Tok) {
        int n = Tok.Length;
        Source.Type type;
        double c0;
        Pattern pat = null;
        Node nodeRef;

        if (n < 2) throw new ENException(ErrorCode.Err201);
        if ((nodeRef = net.getNode(Tok[0])) == null) throw new ENException(ErrorCode.Err203);

        int i = 2;

        if (EnumsTxt.TryParse(Tok[1], out type))
            i = 1;

        //if (Utilities.match(Tok[1], Keywords.w_CONCEN)) type = Source.Type.CONCEN;
        //else if (Utilities.match(Tok[1], Keywords.w_MASS)) type = Source.Type.MASS;
        //else if (Utilities.match(Tok[1], Keywords.w_SETPOINT)) type = Source.Type.SETPOINT;
        //else if (Utilities.match(Tok[1], Keywords.w_FLOWPACED)) type = Source.Type.FLOWPACED;
        //else i = 1;

        if (!Tok[i].ToDouble(out c0)) {
            throw new ENException(ErrorCode.Err202);
        }

        if (n > i + 1 && Tok[i + 1].Length > 0 && !Tok[i + 1].Equals("*", StringComparison.Ordinal)) {
            pat = net.getPattern(Tok[i + 1]);
            if (pat == null) throw new ENException(ErrorCode.Err205);
        }

        Source src = new Source();

        src.setC0(c0);
        src.setPattern(pat);
        src.setType(type);

        nodeRef.setSource(src);
    }


    protected void parseEmitter(Network net, string[] Tok) {
        int n = Tok.Length;
        Node nodeRef;
        double k;

        if (n < 2) throw new ENException(ErrorCode.Err201);
        if ((nodeRef = net.getNode(Tok[0])) == null) throw new ENException(ErrorCode.Err203);
        if (nodeRef is Tank)
            throw new ENException(ErrorCode.Err209);

        if (!Tok[1].ToDouble(out k)) {
            throw new ENException(ErrorCode.Err202);
        }

        if (k < 0.0)
            throw new ENException(ErrorCode.Err202);

        nodeRef.setKe(k);

    }


    protected void parseQuality(Network net, string[] Tok) {
        int n = Tok.Length;
        long i0 = 0, i1 = 0;
        double c0;

        if (n < 2) return;
        if (n == 2) {
            Node nodeRef;
            if ((nodeRef = net.getNode(Tok[0])) == null) return;
            if (!Tok[1].ToDouble(out c0))
                throw new ENException(ErrorCode.Err209);
            nodeRef.setC0(new[]{c0});
        } else {
            if (!Tok[2].ToDouble(out c0)) {
                throw new ENException(ErrorCode.Err209);
            }

            try {
                i0 = long.Parse(Tok[0]);
                i1 = long.Parse(Tok[1]);
            } finally {
                if (i0 > 0 && i1 > 0) {
                    foreach (Node j  in  net.getNodes()) {
                        try {
                            long i = (long) double.Parse(j.getId());//Integer.parseInt(j.getId());
                            if (i >= i0 && i <= i1)
                                j.setC0(new[]{c0});
                        } catch (Exception) {
                        }
                    }
                } else {
                    foreach (Node j  in  net.getNodes()) {
                        if ((string.Compare(Tok[0], j.getId(), StringComparison.OrdinalIgnoreCase) <= 0) &&
                                (string.Compare(Tok[1], j.getId(), StringComparison.OrdinalIgnoreCase) >= 0))
                            j.setC0(new[]{c0});
                    }
                }
            }
        }
    }

    protected void parseReact(Network net, string[] Tok) {
        int item, n = Tok.Length;
        double y;

        if (n < 3) return;


        if (Tok[0].match(Keywords.w_ORDER)) {

            if (!Tok[n - 1].ToDouble(out y)) {
                throw new ENException(ErrorCode.Err213);
            }

            if (Tok[1].match(Keywords.w_BULK)) net.getPropertiesMap().setBulkOrder(y);
            else if (Tok[1].match(Keywords.w_TANK)) net.getPropertiesMap().setTankOrder(y);
            else if (Tok[1].match(Keywords.w_WALL)) {
                if (y == 0.0) net.getPropertiesMap().setWallOrder(0.0);
                else if (y == 1.0) net.getPropertiesMap().setWallOrder(1.0);
                else throw new ENException(ErrorCode.Err213);
            } else throw new ENException(ErrorCode.Err213);
            return;
        }

        if (Tok[0].match(Keywords.w_ROUGHNESS)) {
            if (!Tok[n - 1].ToDouble(out y)) {
                throw new ENException(ErrorCode.Err213);
            }
            net.getPropertiesMap().setRfactor(y);
            return;
        }

        if (Tok[0].match(Keywords.w_LIMITING)) {
            if (!Tok[n - 1].ToDouble(out y)) {
                throw new ENException(ErrorCode.Err213);
            }
            net.getPropertiesMap().setClimit(y);
            return;
        }

        if (Tok[0].match(Keywords.w_GLOBAL)) {
            if(!Tok[n - 1].ToDouble(out y)) {
                throw new ENException(ErrorCode.Err213);
            }
            if (Tok[1].match(Keywords.w_BULK)) net.getPropertiesMap().setKbulk(y);
            else if (Tok[1].match(Keywords.w_WALL)) net.getPropertiesMap().setKwall(y);
            else throw new ENException(ErrorCode.Err201);
            return;
        }

        if (Tok[0].match(Keywords.w_BULK)) item = 1;
        else if (Tok[0].match(Keywords.w_WALL)) item = 2;
        else if (Tok[0].match(Keywords.w_TANK)) item = 3;
        else throw new ENException(ErrorCode.Err201);

        Tok[0] = Tok[1];

        if (item == 3) {
            if (!Tok[n - 1].ToDouble(out y)) {
                throw new ENException(ErrorCode.Err209);
            }

            if (n == 3) {
                Node nodeRef;
                if ((nodeRef = net.getNode(Tok[1])) == null)
                    throw new ENException(ErrorCode.Err208); //if ((j = net.getNode(Tok[1])) <= juncsCount) return;
                if (!(nodeRef is Tank)) return;
                ((Tank) nodeRef).setKb(y);//net.getTanks()[j - juncsCount].setKb(y);
            } else {
                long i1 = 0, i2 = 0;
                try {
                    i1 = long.Parse(Tok[1]);
                    i2 = long.Parse(Tok[2]);
                } finally {
                    if (i1 > 0 && i2 > 0) {
                        foreach (Tank j  in  net.getTanks()) {
                            long i = long.Parse(j.getId());
                            if (i >= i1 && i <= i2)
                                j.setKb(y);
                        }
                    } else {
                        foreach (Tank j  in  net.getTanks()) {
                            if (string.Compare(Tok[1], j.getId(), StringComparison.Ordinal) <= 0 &&
                                    string.Compare(Tok[2], j.getId(), StringComparison.Ordinal) >= 0)
                                j.setKb(y);
                        }
                    }
                }
            }
        } else {
            if(!Tok[n - 1].ToDouble(out y)) {
                throw new ENException(ErrorCode.Err211);
            }

            if (net.getLinks().size() == 0) return;
            if (n == 3) {
                Link linkRef;
                if ((linkRef = net.getLink(Tok[1])) == null) return;
                if (item == 1)
                    linkRef.setKb(y);
                else
                    linkRef.setKw(y);
            } else {
                long i1 = 0, i2 = 0;
                try {
                    i1 = long.Parse(Tok[1]);
                    i2 = long.Parse(Tok[2]);
                } finally {
                    if (i1 > 0 && i2 > 0) {
                        foreach (Link j  in  net.getLinks()) {
                            try {
                                long i = long.Parse(j.getId());
                                if (i >= i1 && i <= i2) {
                                    if (item == 1)
                                        j.setKb(y);
                                    else
                                        j.setKw(y);
                                }
                            } catch (Exception) {
                            }
                        }
                    } else
                        foreach (Link j  in  net.getLinks()) {
                            if (string.Compare(Tok[1], j.getId(), StringComparison.Ordinal) <= 0 &&
                                    string.Compare(Tok[2], j.getId(), StringComparison.Ordinal) >= 0) {
                                if (item == 1)
                                    j.setKb(y);
                                else
                                    j.setKw(y);
                            }
                        }
                }
            }
        }
    }


    protected void parseMixing(Network net, string[] Tok) {
        int n = Tok.Length;
        Tank.MixType i;

        if (net.getNodes().size() == 0)
            throw new ENException(ErrorCode.Err208);

        if (n < 2) return;

        Node nodeRef = net.getNode(Tok[0]);
        if (nodeRef == null) throw new ENException(ErrorCode.Err208);
        if (!(nodeRef is Tank)) return;
        Tank tankRef = (Tank) nodeRef;

        if(!EnumsTxt.TryParse(Tok[1], out i))
            throw new ENException(ErrorCode.Err201);

        var v = 1.0;
        if (i == Tank.MixType.MIX2 && n == 3) {
            if (!Tok[2].ToDouble(out v)) {
                throw new ENException(ErrorCode.Err209);
            }
        }

        if (v == 0.0)
            v = 1.0;

        if (tankRef.getArea() == 0.0) return;
        tankRef.setMixModel(i);
        tankRef.setV1max(v);
    }


    protected void parseStatus(Network net, string[] Tok) {
        int n = Tok.Length - 1;
        double y = 0.0;
        Link.StatType status = Link.StatType.ACTIVE;

        if (net.getLinks().size() == 0) throw new ENException(ErrorCode.Err210);

        if (n < 1) throw new ENException(ErrorCode.Err201);

        if (Tok[n].match(Keywords.w_OPEN)) status = Link.StatType.OPEN;
        else if (Tok[n].match(Keywords.w_CLOSED)) status = Link.StatType.CLOSED;
        else if (!Tok[n].ToDouble(out y)) {
            throw new ENException(ErrorCode.Err211);
        }

        if (y < 0.0)
            throw new ENException(ErrorCode.Err211);

        if (n == 1) {
            Link linkRef;
            if ((linkRef = net.getLink(Tok[0])) == null) return;

            if (linkRef.getType() == Link.LinkType.CV) throw new ENException(ErrorCode.Err211);

            if (linkRef.getType() == Link.LinkType.GPV
                    && status == Link.StatType.ACTIVE) throw new ENException(ErrorCode.Err211);

            changeStatus(linkRef, status, y);
        } else {
            long i0 = 0, i1 = 0;
            try {
                i0 = long.Parse(Tok[0]);
                i1 = long.Parse(Tok[1]);
            } finally {
                if (i0 > 0 && i1 > 0) {
                    foreach (Link j  in  net.getLinks()) {
                        try {
                            long i = long.Parse(j.getId());
                            if (i >= i0 && i <= i1)
                                changeStatus(j, status, y);
                        } catch (Exception) {
                        }
                    }
                } else
                    foreach (Link j  in  net.getLinks())
                        if (string.Compare(Tok[0], j.getId(), StringComparison.Ordinal) <= 0 &&
                                string.Compare(Tok[1], j.getId(), StringComparison.Ordinal) >= 0)
                            changeStatus(j, status, y);
            }
        }
    }

    protected void changeStatus(Link lLink, Link.StatType status, double y) {
        if (lLink.getType() == Link.LinkType.PIPE || lLink.getType() == Link.LinkType.GPV) {
            if (status != Link.StatType.ACTIVE) lLink.setStatus(status);
        } else if (lLink.getType() == Link.LinkType.PUMP) {
            if (status == Link.StatType.ACTIVE) {
                lLink.setRoughness(y);//lLink.setKc(y);
                status = Link.StatType.OPEN;
                if (y == 0.0) status = Link.StatType.CLOSED;
            } else if (status == Link.StatType.OPEN) lLink.setRoughness(1.0); //lLink.setKc(1.0);
            lLink.setStatus(status);
        } else if (lLink.getType() >= Link.LinkType.PRV) {
            lLink.setRoughness(y);//lLink.setKc(y);
            lLink.setStatus(status);
            if (status != Link.StatType.ACTIVE) lLink.setRoughness(Constants.MISSING);//lLink.setKc(Constants.MISSING);
        }
    }

    protected void parseEnergy(Network net, string[] Tok) {
        int n = Tok.length();
        double y;

        if (n < 3) throw new ENException(ErrorCode.Err201);

        if (Tok[0].match(Keywords.w_DMNDCHARGE)) {
            if (!Tok[2].ToDouble(out y))
                throw new ENException(ErrorCode.Err213);
            net.getPropertiesMap().setDcost(y);
            return;
        }

        Pump pumpRef;
        if (Tok[0].match(Keywords.w_GLOBAL)) {
            pumpRef = null;
        } else if (Tok[0].match(Keywords.w_PUMP)) {
            if (n < 4) throw new ENException(ErrorCode.Err201);
            Link linkRef = net.getLink(Tok[1]);
            if (linkRef == null) throw new ENException(ErrorCode.Err216);
            if (linkRef.getType() != Link.LinkType.PUMP) throw new ENException(ErrorCode.Err216);
            pumpRef = (Pump) linkRef;
        } else throw new ENException(ErrorCode.Err201);


        if (Tok[n - 2].match(Keywords.w_PRICE)) {
            if (!Tok[n - 1].ToDouble(out y)) {
                if (pumpRef == null)
                    throw new ENException(ErrorCode.Err213);
                else
                    throw new ENException(ErrorCode.Err217);
            }

            if (pumpRef == null)
                net.getPropertiesMap().setEcost(y);
            else
                pumpRef.setEcost(y);

            return;
        } else if (Tok[n - 2].match(Keywords.w_PATTERN)) {
            Pattern t = net.getPattern(Tok[n - 1]);
            if (t == null) {
                if (pumpRef == null) throw new ENException(ErrorCode.Err213);
                else throw new ENException(ErrorCode.Err217);
            }
            if (pumpRef == null)
                net.getPropertiesMap().setEpatId(t.getId());
            else
                pumpRef.setEpat(t);
            return;
        } else if (Tok[n - 2].match(Keywords.w_EFFIC)) {
            if (pumpRef == null) {
                if (!Tok[n - 1].ToDouble(out y))
                    throw new ENException(ErrorCode.Err213);
                if (y <= 0.0)
                    throw new ENException(ErrorCode.Err213);
                net.getPropertiesMap().setEpump(y);
            } else {
                Curve t = net.getCurve(Tok[n - 1]);
                if (t == null) throw new ENException(ErrorCode.Err217);
                pumpRef.setEcurve(t);
            }
            return;
        }
        throw new ENException(ErrorCode.Err201);
    }


    protected void parseReport(Network net, string[] Tok) {
        int n = Tok.Length - 1;
        //FieldType i;
        double y;

        if (n < 1) throw new ENException(ErrorCode.Err201);

        if (Tok[0].match(Keywords.w_PAGE)) {
            if (!Tok[n].ToDouble(out y)) throw new ENException(ErrorCode.Err213);
            if (y < 0.0 || y > 255.0) throw new ENException(ErrorCode.Err213);
            net.getPropertiesMap().setPageSize((int)y);
            return;
        }


        if (Tok[0].match(Keywords.w_STATUS)) {
            PropertiesMap.StatFlag flag;
            if (EnumsTxt.TryParse(Tok[n], out flag)) {
                net.getPropertiesMap().setStatflag(flag);
            }
            else {
                // TODO: complete this    
            }
            
            //if (Utilities.match(Tok[n], Keywords.w_NO)) net.getPropertiesMap().setStatflag(PropertiesMap.StatFlag.FALSE);
            //if (Utilities.match(Tok[n], Keywords.w_YES)) net.getPropertiesMap().setStatflag(PropertiesMap.StatFlag.TRUE);
            //if (Utilities.match(Tok[n], Keywords.w_FULL)) net.getPropertiesMap().setStatflag(PropertiesMap.StatFlag.FULL);
            return;
        }

        if (Tok[0].match(Keywords.w_SUMMARY)) {
            if (Tok[n].match(Keywords.w_NO)) net.getPropertiesMap().setSummaryflag(false);
            if (Tok[n].match(Keywords.w_YES)) net.getPropertiesMap().setSummaryflag(true);
            return;
        }

        if (Tok[0].match(Keywords.w_MESSAGES)) {
            if (Tok[n].match(Keywords.w_NO)) net.getPropertiesMap().setMessageflag(false);
            if (Tok[n].match(Keywords.w_YES)) net.getPropertiesMap().setMessageflag(true);
            return;
        }

        if (Tok[0].match(Keywords.w_ENERGY)) {
            if (Tok[n].match(Keywords.w_NO)) net.getPropertiesMap().setEnergyflag(false);
            if (Tok[n].match(Keywords.w_YES)) net.getPropertiesMap().setEnergyflag(true);
            return;
        }

        if (Tok[0].match(Keywords.w_NODE)) {
            if (Tok[n].match(Keywords.w_NONE))
                net.getPropertiesMap().setNodeflag(PropertiesMap.ReportFlag.FALSE);
            else if (Tok[n].match(Keywords.w_ALL))
                net.getPropertiesMap().setNodeflag(PropertiesMap.ReportFlag.TRUE);
            else {
                if (net.getNodes().size() == 0) throw new ENException(ErrorCode.Err208);
                for (int ii = 1; ii <= n; ii++) {
                    Node nodeRef;
                    if ((nodeRef = net.getNode(Tok[n])) == null) throw new ENException(ErrorCode.Err208);
                    nodeRef.setReportFlag(true);
                }
                net.getPropertiesMap().setNodeflag(PropertiesMap.ReportFlag.SOME);
            }
            return;
        }

        if (Tok[0].match(Keywords.w_LINK)) {
            if (Tok[n].match(Keywords.w_NONE))
                net.getPropertiesMap().setLinkflag(PropertiesMap.ReportFlag.FALSE);
            else if (Tok[n].match(Keywords.w_ALL))
                net.getPropertiesMap().setLinkflag(PropertiesMap.ReportFlag.TRUE);
            else {
                if (net.getLinks().size() == 0) throw new ENException(ErrorCode.Err210);
                for (int ii = 1; ii <= n; ii++) {
                    Link linkRef = net.getLink(Tok[ii]);
                    if (linkRef == null) throw new ENException(ErrorCode.Err210);
                    linkRef.setReportFlag(true);
                }
                net.getPropertiesMap().setLinkflag(PropertiesMap.ReportFlag.SOME);
            }
            return;
        }

        FieldsMap.Type iFieldID;
        FieldsMap fMap = net.getFieldsMap();

        if (EnumsTxt.TryParse(Tok[0], out iFieldID)) {
            if(iFieldID > FieldsMap.Type.FRICTION)
                throw new ENException(ErrorCode.Err201);

            if (Tok.Length == 1 || Tok[1].match(Keywords.w_YES)) {
                fMap.getField(iFieldID).setEnabled(true);
                return;
            }

            if (Tok[1].match(Keywords.w_NO)) {
                fMap.getField(iFieldID).setEnabled(false);
                return;
            }

            Field.RangeType rj;

            if (Tok.Length < 3)
                throw new ENException(ErrorCode.Err201);

            if(!EnumsTxt.TryParse(Tok[1], out rj))
                throw new ENException(ErrorCode.Err201);

            if (!Tok[2].ToDouble(out y))
                throw new ENException(ErrorCode.Err201);

            if (rj == Field.RangeType.PREC) {
                fMap.getField(iFieldID).setEnabled(true);
                fMap.getField(iFieldID).setPrecision((int) Math.Round(y));//roundOff(y));
            } else
                fMap.getField(iFieldID).setRptLim(rj, y);

            return;
        }

        if (Tok[0].match(Keywords.w_FILE)) {
            net.getPropertiesMap().setAltReport(Tok[1]);
            return;
        }

        log.Information("Unknow section keyword "+Tok[0]+" value "+Tok[1]);
//        throw new ENException(ErrorCode.Err201);
    }

    protected void parseOption(Network net, string[] Tok) {
        int n = Tok.Length - 1;
        bool notHandled = optionChoice(net, Tok, n);
        if (notHandled)
            notHandled = optionValue(net, Tok, n);
        if (notHandled) {
            net.getPropertiesMap().put(Tok[0], Tok[1]);
        }
    }

    /**
     * Handles options that are choice values, such as quality type, for example.
     *
     * @param net - newtwork
     * @param Tok - token arry
     * @param n   - number of tokens
     * @return true is it didn't handle the option.
     * @throws ENException
     */
    protected bool optionChoice(Network net, string[] Tok, int n) {
        PropertiesMap map = net.getPropertiesMap();

        if (n < 0)
            throw new ENException(ErrorCode.Err201);

        if (Tok[0].match(Keywords.w_UNITS)) {
            PropertiesMap.FlowUnitsType type;
          
            if (n < 1)
                return false;
            else if (EnumsTxt.TryParse(Tok[1], out type))
                map.setFlowflag(type);
            else
                throw new ENException(ErrorCode.Err201);

        } else if (Tok[0].match(Keywords.w_PRESSURE)) {
            if (n < 1) return false;
            else if (Tok[1].match(Keywords.w_PSI)) map.setPressflag(PropertiesMap.PressUnitsType.PSI);
            else if (Tok[1].match(Keywords.w_KPA)) map.setPressflag(PropertiesMap.PressUnitsType.KPA);
            else if (Tok[1].match(Keywords.w_METERS)) map.setPressflag(PropertiesMap.PressUnitsType.METERS);
            else
                throw new ENException(ErrorCode.Err201);
        } else if (Tok[0].match(Keywords.w_HEADLOSS)) {
            if (n < 1) return false;
            else if (Tok[1].match(Keywords.w_HW)) map.setFormflag(PropertiesMap.FormType.HW);
            else if (Tok[1].match(Keywords.w_DW)) map.setFormflag(PropertiesMap.FormType.DW);
            else if (Tok[1].match(Keywords.w_CM)) map.setFormflag(PropertiesMap.FormType.CM);
            else throw new ENException(ErrorCode.Err201);
        } else if (Tok[0].match(Keywords.w_HYDRAULIC)) {
            if (n < 2)
                return false;
            else if (Tok[1].match(Keywords.w_USE)) map.setHydflag(PropertiesMap.Hydtype.USE);
            else if (Tok[1].match(Keywords.w_SAVE)) map.setHydflag(PropertiesMap.Hydtype.SAVE);
            else
                throw new ENException(ErrorCode.Err201);
            map.setHydFname(Tok[2]);
        } else if (Tok[0].match(Keywords.w_QUALITY)) {
            PropertiesMap.QualType type;
            
            if (n < 1)
                return false;
            else if (EnumsTxt.TryParse(Tok[1], out type))
                map.setQualflag(type);
                //else if (Utilities.match(Tok[1], Keywords.w_NONE)) net.setQualflag(QualType.NONE);
                //else if (Utilities.match(Tok[1], Keywords.w_CHEM)) net.setQualflag(QualType.CHEM);
                //else if (Utilities.match(Tok[1], Keywords.w_AGE)) net.setQualflag(QualType.AGE);
                //else if (Utilities.match(Tok[1], Keywords.w_TRACE)) net.setQualflag(QualType.TRACE);
            else {
                map.setQualflag(PropertiesMap.QualType.CHEM);
                map.setChemName(Tok[1]);
                if (n >= 2)
                    map.setChemUnits(Tok[2]);
            }
            if (map.getQualflag() == PropertiesMap.QualType.TRACE) {

                Tok[0] = "";
                if (n < 2)
                    throw new ENException(ErrorCode.Err212);
                Tok[0] = Tok[2];
                Node nodeRef = net.getNode(Tok[2]);
                if (nodeRef == null)
                    throw new ENException(ErrorCode.Err212);
                map.setTraceNode(nodeRef.getId());
                map.setChemName(Keywords.u_PERCENT);
                map.setChemUnits(Tok[2]);
            }
            if (map.getQualflag() == PropertiesMap.QualType.AGE) {
                map.setChemName(Keywords.w_AGE);
                map.setChemUnits(Keywords.u_HOURS);
            }
        } else if (Tok[0].match(Keywords.w_MAP)) {
            if (n < 1)
                return false;
            map.setMapFname(Tok[1]);
        } else if (Tok[0].match(Keywords.w_UNBALANCED)) {
            if (n < 1)
                return false;
            if (Tok[1].match(Keywords.w_STOP))
                map.setExtraIter(-1);
            else if (Tok[1].match(Keywords.w_CONTINUE)) {
                if (n >= 2) {
                    double d;
                    if (Tok[2].ToDouble(out d)) {
                        map.setExtraIter((int)d);
                    }
                    else {
                        throw new ENException(ErrorCode.Err201);
                    }
                }
                else
                    map.setExtraIter(0);
            } else throw new ENException(ErrorCode.Err201);
        } else if (Tok[0].match(Keywords.w_PATTERN)) {
            if (n < 1)
                return false;
            map.setDefPatId(Tok[1]);
        } else
            return true;
        return false;
    }

    protected bool optionValue(Network net, string[] Tok, int n) {
        int nvalue = 1;
        PropertiesMap map = net.getPropertiesMap();

        string name = Tok[0];


        if (name.match(Keywords.w_SPECGRAV) || name.match(Keywords.w_EMITTER)
                || name.match(Keywords.w_DEMAND)) nvalue = 2;

        string keyword = null;
        foreach (string k  in  OPTION_VALUE_KEYWORDS) {
            if (name.match(k)) {
                keyword = k;
                break;
            }
        }
        if (keyword == null) return true;
        name = keyword;

        double y;

        if (!Tok[nvalue].ToDouble(out y)) 
            throw new ENException(ErrorCode.Err213);

        if (name.match(Keywords.w_TOLERANCE)) {
            if (y < 0.0)
                throw new ENException(ErrorCode.Err213);
            map.setCtol(y);
            return false;
        }

        if (name.match(Keywords.w_DIFFUSIVITY)) {
            if (y < 0.0)
                throw new ENException(ErrorCode.Err213);
            map.setDiffus(y);
            return false;
        }

        if (name.match(Keywords.w_DAMPLIMIT)) {
            map.setDampLimit(y);
            return false;
        }

        if (y <= 0.0) throw new ENException(ErrorCode.Err213);

        if (name.match(Keywords.w_VISCOSITY)) map.setViscos(y);
        else if (name.match(Keywords.w_SPECGRAV)) map.setSpGrav(y);
        else if (name.match(Keywords.w_TRIALS)) map.setMaxIter((int)y);
        else if (name.match(Keywords.w_ACCURACY)) {
            y = Math.Max(y, 1e-5);
            y = Math.Min(y, 1e-1);
            map.setHacc(y);
        } else if (name.match(Keywords.w_HTOL)) map.setHtol(y);
        else if (name.match(Keywords.w_QTOL)) map.setQtol(y);
        else if (name.match(Keywords.w_RQTOL)) {
            if (y >= 1.0) throw new ENException(ErrorCode.Err213);
            map.setRQtol(y);
        } else if (name.match(Keywords.w_CHECKFREQ)) map.setCheckFreq((int)y);
        else if (name.match(Keywords.w_MAXCHECK)) map.setMaxCheck((int)y);
        else if (name.match(Keywords.w_EMITTER)) map.setQexp(1.0d / y);
        else if (name.match(Keywords.w_DEMAND)) map.setDmult(y);

        return false;
    }

    protected void parseTime(Network net, string[] Tok) {
        int n = Tok.Length - 1;
        double y;
        PropertiesMap map = net.getPropertiesMap();

        if (n < 1)
            throw new ENException(ErrorCode.Err201);

        if (Tok[0].match(Keywords.w_STATISTIC)) {
            if (Tok[n].match(Keywords.w_NONE)) map.setTstatflag(PropertiesMap.TstatType.SERIES);
            else if (Tok[n].match(Keywords.w_NO)) map.setTstatflag(PropertiesMap.TstatType.SERIES);
            else if (Tok[n].match(Keywords.w_AVG)) map.setTstatflag(PropertiesMap.TstatType.AVG);
            else if (Tok[n].match(Keywords.w_MIN)) map.setTstatflag(PropertiesMap.TstatType.MIN);
            else if (Tok[n].match(Keywords.w_MAX)) map.setTstatflag(PropertiesMap.TstatType.MAX);
            else if (Tok[n].match(Keywords.w_RANGE)) map.setTstatflag(PropertiesMap.TstatType.RANGE);
            else
                throw new ENException(ErrorCode.Err201);
            return;
        }

        if (!Tok[n].ToDouble(out y)) {
            if ((y = Utilities.getHour(Tok[n], "")) < 0.0) {
                if ((y = Utilities.getHour(Tok[n - 1], Tok[n])) < 0.0)
                    throw new ENException(ErrorCode.Err213);
            }
        }
        var t = (long) (3600.0 * y);

        if (Tok[0].match(Keywords.w_DURATION))
            map.setDuration(t);
        else if (Tok[0].match(Keywords.w_HYDRAULIC))
            map.setHstep(t);
        else if (Tok[0].match(Keywords.w_QUALITY))
            map.setQstep(t);
        else if (Tok[0].match(Keywords.w_RULE))
            map.setRulestep(t);
        else if (Tok[0].match(Keywords.w_MINIMUM))
            return;
        else if (Tok[0].match(Keywords.w_PATTERN)) {
            if (Tok[1].match(Keywords.w_TIME))
                map.setPstep(t);
            else if (Tok[1].match(Keywords.w_START))
                map.setPstart(t);
            else
                throw new ENException(ErrorCode.Err201);
        } else if (Tok[0].match(Keywords.w_REPORT)) {
            if (Tok[1].match(Keywords.w_TIME))
                map.setRstep(t);
            else if (Tok[1].match(Keywords.w_START))
                map.setRstart(t);
            else
                throw new ENException(ErrorCode.Err201);
        } else if (Tok[0].match(Keywords.w_START))
            map.setTstart(t % Constants.SECperDAY);
        else throw new ENException(ErrorCode.Err201);

    }

    private Network.SectType findSectionType(string line) {
        if(string.IsNullOrEmpty(line))
            return (Network.SectType)(-1);

        line = line.TrimStart();
        for(var type = Network.SectType.TITLE; type <= Network.SectType.END; type++) {
            string sectName = '[' + type.ToString() + ']';

            // if(line.Contains(type.parseStr())) return type;
            // if (line.IndexOf(type.parseStr(), StringComparison.OrdinalIgnoreCase) >= 0) {
            if(line.StartsWith(sectName, StringComparison.OrdinalIgnoreCase)) {
                return type;
            }
        }
        return (Network.SectType)(-1);

    }

    protected void parseDemand(Network net, string[] Tok) {
        int n = Tok.Length;
        double y;
        Demand demand = null;
        Pattern pat = null;

        if (n < 2)
            throw new ENException(ErrorCode.Err201);

        if (!Tok[1].ToDouble(out y)) {
            throw new ENException(ErrorCode.Err202);
        }

        if (Tok[0].match(Keywords.w_MULTIPLY)) {
            if (y <= 0.0)
                throw new ENException(ErrorCode.Err202);
            else
                net.getPropertiesMap().setDmult(y);
            return;
        }

        Node nodeRef;
        if ((nodeRef = net.getNode(Tok[0])) == null)
            throw new ENException(ErrorCode.Err208);

        if (nodeRef is Tank)
            throw new ENException(ErrorCode.Err208);

        if (n >= 3) {
            pat = net.getPattern(Tok[2]);
            if (pat == null)
                throw new ENException(ErrorCode.Err205);
        }

        if (nodeRef.getDemand().Count > 0)
            demand = nodeRef.getDemand()[0];

        if (demand != null && nodeRef.getInitDemand() != Constants.MISSING) {
            demand.setBase(y);
            demand.setPattern(pat);
            nodeRef.setInitDemand(Constants.MISSING);
        } else {
            demand = new Demand(y, pat);
            nodeRef.getDemand().Add(demand);
        }

    }

    protected void parseRule(Network net, string[] tok, string line) {
        Rule.Rulewords key;
        EnumsTxt.TryParse(tok[0], out key);
        if (key == Rule.Rulewords.r_RULE) {
            currentRule = new Rule();
            currentRule.setLabel(tok[1]);
            ruleState = Rule.Rulewords.r_RULE;
            net.addRule(currentRule);
        } else if (currentRule != null) {
            currentRule.setCode(currentRule.getCode() + line + "\n");
        }

    }

#if !COMMENTED

    protected void parseRule(Network net, string[] tok) {
        if (ruleState == Rule.Rulewords.r_ERROR)
            return;

        Rule.Rulewords key;

        if (!EnumsTxt.TryParse(tok[0], out key)) throw new ENException(ErrorCode.Err201);

        switch (key) {
        case Rule.Rulewords.r_RULE:
            currentRule = new Rule();
            currentRule.setLabel(tok[1]);
            ruleState = Rule.Rulewords.r_RULE;
            net.addRule(currentRule);
            break;

        case Rule.Rulewords.r_IF:
            if (this.ruleState != Rule.Rulewords.r_RULE)
                throw new ENException(ErrorCode.Err221);
            ruleState = Rule.Rulewords.r_IF;
            parsePremise(net, tok, Rule.Rulewords.r_AND);
            break;

        case Rule.Rulewords.r_AND:
            if (ruleState == Rule.Rulewords.r_IF)
                parsePremise(net, tok, Rule.Rulewords.r_AND);
            else if (ruleState == Rule.Rulewords.r_THEN || ruleState == Rule.Rulewords.r_ELSE)
                parseAction(net, ruleState, tok);
            else
                throw new ENException(ErrorCode.Err221);
            break;

        case Rule.Rulewords.r_OR:
            if (ruleState == Rule.Rulewords.r_IF)
                parsePremise(net, tok, Rule.Rulewords.r_OR);
            else
                throw new ENException(ErrorCode.Err221);
            break;

        case Rule.Rulewords.r_THEN:
            if (ruleState != Rule.Rulewords.r_IF)
                throw new ENException(ErrorCode.Err221);
            ruleState = Rule.Rulewords.r_THEN;
            parseAction(net, ruleState, tok);
            break;

        case Rule.Rulewords.r_ELSE:
            if (ruleState != Rule.Rulewords.r_THEN)
                throw new ENException(ErrorCode.Err221);
            ruleState = Rule.Rulewords.r_ELSE;
            parseAction(net, ruleState, tok);
            break;

        case Rule.Rulewords.r_PRIORITY:
            if (ruleState != Rule.Rulewords.r_THEN && ruleState != Rule.Rulewords.r_ELSE)
                throw new ENException(ErrorCode.Err221);
            ruleState = Rule.Rulewords.r_PRIORITY;
            parsePriority(net, tok);
            break;

        default:
            throw new ENException(ErrorCode.Err201);
        }
    }

    protected void parsePremise(Network net,string[]Tok,Rule.Rulewords logop) {
        Rule.Objects obj;
        Rule.Varwords v;

        org.addition.epanet.hydraulic.structures.SimulationRule.Premise p = new org.addition.epanet.hydraulic.structures.SimulationRule.Premise();

        if (Tok.length != 5 && Tok.length != 6)
            throw new ENException(ErrorCode.Err201);

        obj = Rule.Objects.parse(Tok[1]);

        p.setLogop(logop);

        if (obj == Rule.Objects.r_SYSTEM){
            v = Rule.Varwords.parse(Tok[2]);

            if (v != Rule.Varwords.r_DEMAND && v != Rule.Varwords.r_TIME && v != Rule.Varwords.r_CLOCKTIME)
                throw new ENException(ErrorCode.Err201);

            p.setObject(Rule.Objects.r_SYSTEM);
        }
        else
        {
            v = Rule.Varwords.parse(Tok[3]);
            if (v == null)
                throw new ENException(ErrorCode.Err201);
            switch (obj)
            {
                case r_NODE:
                case r_JUNC:
                case r_RESERV:
                case r_TANK:
                    obj = Rule.Objects.r_NODE;
                    break;
                case r_LINK:
                case r_PIPE:
                case r_PUMP:
                case r_VALVE:
                    obj = Rule.Objects.r_LINK;
                    break;
                default:
                    throw new ENException(ErrorCode.Err201);
            }

            if (obj == Rule.Objects.r_NODE)
            {
                Node nodeRef = net.getNode(Tok[2]);
                if (nodeRef == null)
                    throw new ENException(ErrorCode.Err203);
                switch (v){
                    case r_DEMAND:
                    case r_HEAD:
                    case r_GRADE:
                    case r_LEVEL:
                    case r_PRESSURE:
                        break;
                    case r_FILLTIME:
                    case r_DRAINTIME: if (nodeRef is Tank) throw new ENException(ErrorCode.Err201); break;

                    default:
                        throw new ENException(ErrorCode.Err201);
                }
                p.setObject(nodeRef);
            }
            else
            {
                Link linkRef = net.getLink(Tok[2]);
                if (linkRef == null)
                    throw new ENException(ErrorCode.Err204);
                switch (v){
                    case r_FLOW:
                    case r_STATUS:
                    case r_SETTING:
                        break;
                    default:
                        throw new ENException(ErrorCode.Err201);
                }
                p.setObject(linkRef);
            }
        }

        Rule.Operators op;

        if (obj == Rule.Objects.r_SYSTEM)
            op = Rule.Operators.parse(Tok[3]);
        else
            op = Rule.Operators.parse(Tok[4]);

        if (op == null)
            throw new ENException(ErrorCode.Err201);

        switch(op)
        {
            case IS:
                p.setRelop(Rule.Operators.EQ);
                break;
            case NOT:
                 p.setRelop(Rule.Operators.NE);
                break;
            case BELOW:
                p.setRelop(Rule.Operators.LT);
                break;
            case ABOVE:
                p.setRelop(Rule.Operators.GT);
                break;
            default:
                p.setRelop(op);
        }

        Rule.Values status = Rule.Values.IS_NUMBER;
        double value = Constants.MISSING;

        if (v == Rule.Varwords.r_TIME || v == Rule.Varwords.r_CLOCKTIME)
        {
            if (Tok.length == 6)
                value = Utilities.getHour(Tok[4],Tok[5])*3600.;
            else
                value = Utilities.getHour(Tok[4],"")*3600.;
            if (value < 0.0) throw new ENException(ErrorCode.Err202);
        }
        else{
            status = Rule.Values.parse(Tok[Tok.length-1]);
            if (status==null || status.id <= Rule.Values.IS_NUMBER.id){
                if ((value = Utilities.getDouble(Tok[Tok.length - 1]))==null)
                    throw new ENException(ErrorCode.Err202);
                if (v == Rule.Varwords.r_FILLTIME || v == Rule.Varwords.r_DRAINTIME)
                    value = value*3600.0;
            }
        }


        p.setVariable(v);
        p.setStatus(status);
        p.setValue(value);

        currentRule.addPremise(p);
    }

    protected void parseAction(Network net,Rule.Rulewords state, string[] tok) {
        int Ntokens = tok.Length;

        Rule.Values s,k;
        double x;

        if (Ntokens != 6)
            throw new ENException(ErrorCode.Err201);

        Link linkRef = net.getLink(tok[2]);

        if (linkRef == null)
            throw new ENException(ErrorCode.Err204);

        if (linkRef.getType() == Link.LinkType.CV)
            throw new ENException(ErrorCode.Err207);

        s = null;
        x = Constants.MISSING;
        k = Rule.Values.parse(tok[5]);

        if (k!=null && k.id > Rule.Values.IS_NUMBER.id)
            s = k;
        else
        {
            if ( (x = Utilities.getDouble(tok[5]))==null )
                throw new ENException(ErrorCode.Err202);
            if (x < 0.0)
                throw new ENException(ErrorCode.Err202);
        }

        if (x != Constants.MISSING && linkRef.getType() == Link.LinkType.GPV)
            throw new ENException(ErrorCode.Err202);

        if (x != Constants.MISSING && linkRef.getType() == Link.LinkType.PIPE){
            if (x == 0.0)
                s = Rule.Values.IS_CLOSED;
            else
                s = Rule.Values.IS_OPEN;
            x = Constants.MISSING;
        }

        Action a = new Action();
        a.setLink(linkRef);
        a.setStatus(s);
        a.setSetting(x);

        if (state == Rule.Rulewords.r_THEN)
            currentRule.addActionT(a);
        else
            currentRule.addActionF(a);
    }

    protected void parsePriority(Network net,string[] tok) {
        double x;
        if ( (x = Utilities.getDouble(tok[1]))==null)
            throw new ENException(ErrorCode.Err202);
        currentRule.setPriority(x);
    }  

#endif


}
}