/*
 * Copyright (C) 2012  Addition, Lda. (addition at addition dot pt)
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
using System.IO;
using System.Text;
using org.addition.epanet.hydraulic;
using org.addition.epanet.hydraulic.io;
using org.addition.epanet.log;
using org.addition.epanet.network;
using org.addition.epanet.network.io.input;
using org.addition.epanet.network.structures;
using org.addition.epanet.quality;
using org.addition.epanet.util;

namespace org.addition.epanet {

    public class EPATool {

        private static void consoleLog(string msg) {
            Console.WriteLine(msg + " " + DateTime.Now.ToString("HH:mm:ss"));
        }

        public static string convertToScientifcNotation(
            double value,
            double max_threshold,
            double min_threshold,
            int @decimal) {
            if (value == null)
                return null;

            if (value != 0.0 && (Math.Abs(value) > max_threshold || Math.Abs(value) < min_threshold))
                return string.Format("%." + @decimal + "e", value);

            return string.Format("%." + @decimal + "f", value);
        }

        private enum NodeVariableType {
            ELEVATION = 0,
            PRESSURE = 3,
            HEAD = 2,
            QUALITY = 4,
            INITQUALITY = 4 | 0x1000,
            BASEDEMAND = 1,
            DEMAND = 1 | 0x1000

        }

        private static FieldsMap.Type ToFieldType(NodeVariableType value) {
            return (FieldsMap.Type)((int)value & ~0x1000);
        }

        private static double GetNodeValue(NodeVariableType type, FieldsMap fmap, AwareStep step, Node node, int index) {
            switch (type) {
            case NodeVariableType.BASEDEMAND: {
                double dsum = 0;
                foreach (var demand in node.getDemand()) {
                    dsum += demand.getBase();
                }
                return fmap.revertUnit((FieldsMap.Type)type, dsum);
            }
            case NodeVariableType.ELEVATION:
                return fmap.revertUnit((FieldsMap.Type)type, node.getElevation());
            case NodeVariableType.DEMAND:
                return step != null ? step.getNodeDemand(index, node, fmap) : 0;
            case NodeVariableType.HEAD:
                return step != null ? step.getNodeHead(index, node, fmap) : 0;
            case NodeVariableType.INITQUALITY: {
                double dsum = 0;
                foreach (double d in node.getC0()) dsum += d;

                return fmap.revertUnit((FieldsMap.Type)type, dsum);
            }
            case NodeVariableType.PRESSURE:
                return step != null ? step.getNodePressure(index, node, fmap) : 0;
            case NodeVariableType.QUALITY:
                return step != null ? step.getNodeQuality(index) : 0;
            default:
                return 0.0;
            }
        }

        private enum LinkVariableType {
            LENGHT = FieldsMap.Type.LENGTH,
            DIAMETER = FieldsMap.Type.DIAM,
            ROUGHNESS = -1,
            FLOW = FieldsMap.Type.FLOW,
            VELOCITY = FieldsMap.Type.VELOCITY,
            UNITHEADLOSS = FieldsMap.Type.HEADLOSS,
            FRICTIONFACTOR = FieldsMap.Type.FRICTION,
            QUALITY = FieldsMap.Type.QUALITY
        }


        private static double GetLinkValue(
            LinkVariableType type,
            PropertiesMap.FormType formType,
            FieldsMap fmap,
            AwareStep step,
            Link link,
            int index) {
            switch (type) {
            case LinkVariableType.LENGHT:
                return fmap.revertUnit((FieldsMap.Type)type, link.getLenght());
            case LinkVariableType.DIAMETER:
                return fmap.revertUnit((FieldsMap.Type)type, link.getDiameter());
            case LinkVariableType.ROUGHNESS:
                return link.getType() == Link.LinkType.PIPE && formType == PropertiesMap.FormType.DW
                    ? fmap.revertUnit(FieldsMap.Type.DIAM, link.getRoughness())
                    : link.getRoughness();

            case LinkVariableType.FLOW:
                return step != null ? Math.Abs(step.getLinkFlow(index, link, fmap)) : 0;
            case LinkVariableType.VELOCITY:
                return step != null ? Math.Abs(step.getLinkVelocity(index, link, fmap)) : 0;
            case LinkVariableType.UNITHEADLOSS:
                return step != null ? step.getLinkHeadLoss(index, link, fmap) : 0;
            case LinkVariableType.FRICTIONFACTOR:
                return step != null ? step.getLinkFriction(index, link, fmap) : 0;
            case LinkVariableType.QUALITY:
                return step != null ? fmap.revertUnit((FieldsMap.Type)type, step.getLinkAvrQuality(index)) : 0;
            default:
                return 0.0;
            }
        }

/*
                                        enum NodeVariableType {
                                            ELEVATION("ELEVATION", FieldsMap.Type.ELEV),
                                            PRESSURE("PRESSURE", FieldsMap.Type.PRESSURE),
                                            HEAD("HEAD", FieldsMap.Type.HEAD),
                                            QUALITY("QUALITY", FieldsMap.Type.QUALITY),
                                            INITQUALITY("INITQUALITY", FieldsMap.Type.QUALITY),
                                            BASEDEMAND("BASEDEMAND", FieldsMap.Type.DEMAND),
                                            DEMAND("DEMAND", FieldsMap.Type.DEMAND);
                                    
                                            public readonly string name;
                                            public readonly FieldsMap.Type type;
                                    
                                            NodeVariableType(string name, FieldsMap.Type type) {
                                                this.name = name;
                                                this.type = type;
                                            }
                                    
                                            public double getValue(FieldsMap fmap, AwareStep step, Node node, int index) {
                                                switch (this) {
                                                    case BASEDEMAND: {
                                                        double dsum = 0;
                                                        foreach (Demand demand  in  node.getDemand()) {
                                                            dsum += demand.getBase();
                                                        }
                                                        return fmap.revertUnit(type, dsum);
                                                    }
                                                    case ELEVATION:
                                                        return fmap.revertUnit(type, node.getElevation());
                                                    case DEMAND:
                                                        return step != null ? step.getNodeDemand(index, node, fmap) : 0;
                                                    case HEAD:
                                                        return step != null ? step.getNodeHead(index, node, fmap) : 0;
                                                    case INITQUALITY: {
                                                        double dsum = 0;
                                                        foreach (double v  in  node.getC0()) {
                                                            dsum += v;
                                                        }
                                                        return dsum != 0 ? fmap.revertUnit(type, dsum / node.getC0().length) : fmap.revertUnit(type, dsum);
                                                    }
                                                    case PRESSURE:
                                                        return step != null ? step.getNodePressure(index, node, fmap) : 0;
                                                    case QUALITY:
                                                        return step != null ? step.getNodeQuality(index) : 0;
                                                    default:
                                                        return 0.0;
                                                }
                                            }
                                        }
                                        */

        /*
        static enum LinkVariableType {
            LENGHT("LENGHT", FieldsMap.Type.LENGTH),
            DIAMETER("DIAMETER", FieldsMap.Type.DIAM),
            ROUGHNESS("ROUGHNESS", null),
            FLOW("FLOW", FieldsMap.Type.FLOW),
            VELOCITY("VELOCITY", FieldsMap.Type.VELOCITY),
            UNITHEADLOSS("UNITHEADLOSS", FieldsMap.Type.HEADLOSS),
            FRICTIONFACTOR("FRICTIONFACTOR", FieldsMap.Type.FRICTION),
            QUALITY("QUALITY", FieldsMap.Type.QUALITY);
    
            public readonly string name;
            public readonly FieldsMap.Type type;
    
            LinkVariableType(string name, FieldsMap.Type type) {
                this.name = name;
                this.type = type;
            }
    
            public double getValue(PropertiesMap.FormType formType, FieldsMap fmap, AwareStep step, Link link, int index) {
                switch (this) {
                    case LENGHT:
                        return fmap.revertUnit(type, link.getLenght());
                    case DIAMETER:
                        return fmap.revertUnit(type, link.getDiameter());
                    case ROUGHNESS:
                        if (link.getType() == Link.LinkType.PIPE && formType == PropertiesMap.FormType.DW)
                            return fmap.revertUnit(FieldsMap.Type.DIAM, link.getRoughness());
                        else
                            return link.getRoughness();
                    case FLOW:
                        return step != null ? Math.Abs(step.getLinkFlow(index, link, fmap)) : 0;
                    case VELOCITY:
                        return step != null ? Math.Abs(step.getLinkVelocity(index, link, fmap)) : 0;
                    case UNITHEADLOSS:
                        return step != null ? step.getLinkHeadLoss(index, link, fmap) : 0;
                    case FRICTIONFACTOR:
                        return step != null ? step.getLinkFriction(index, link, fmap) : 0;
                    case QUALITY:
                        return step != null ? fmap.revertUnit(type, step.getLinkAvrQuality(index)) : 0;
                    default:
                        return 0.0;
                }
            }
        }
        */


        public static void main(string[] args) {
            TraceSource log = new TraceSource(typeof(EPATool).FullName, SourceLevels.All);

            string hydFile = null;
            string qualFile = null;
            Network net = new Network();

            List<NodeVariableType> nodesVariables = new List<NodeVariableType>();
            List<LinkVariableType> linksVariables = new List<LinkVariableType>();

            string inFile = "";
            List<long> targetTimes = new List<long>();
            List<string> targetNodes = new List<string>();
            List<string> targetLinks = new List<string>();

            int parseMode = 0;
            for (int i = 0; i < args.Length; i++) {
                if (args[i].EndsWith(".inp", StringComparison.OrdinalIgnoreCase)) {
                    parseMode = 0;
                    inFile = args[i];
                    if (!File.Exists(inFile)) {
                        consoleLog("END_RUN_ERR");
                        Console.Error.WriteLine("File not found !");
                        return;
                    }
                    continue;
                }

                switch (args[i]) {
                case "-T":
                case "-t":
                    parseMode = 1;
                    continue;
                case "-N":
                case "-n":
                    parseMode = 2;
                    continue;
                case "-L":
                case "-l":
                    parseMode = 3;
                    continue;
                }

                if (parseMode == 1) {
                    targetTimes.Add((long)(Utilities.getHour(args[i], "") * 3600));
                }
                else if (parseMode == 2) {
                    targetNodes.Add(args[i]);
                }
                else if (parseMode == 3) {
                    targetLinks.Add(args[i]);
                }
            }

            try {
                InputParser parserINP = InputParser.create(Network.FileType.INP_FILE, log);
                parserINP.parse(net, inFile);
                PropertiesMap pMap = net.getPropertiesMap();

                if (targetTimes.Count > 0) {
                    foreach (long time  in  targetTimes) {
                        string epanetTime = Utilities.getClockTime(time);
                        if (time < pMap.getRstart())
                            throw new Exception("Target time \"" + epanetTime + "\" smaller than simulation start time");

                        if (time > pMap.getDuration())
                            throw new Exception("Target time \"" + epanetTime + "\" bigger than simulation duration");

                        if ((time - pMap.getRstart()) % pMap.getRstep() != 0)
                            throw new Exception("Target time \"" + epanetTime + "\" not found");
                    }
                }

                foreach (string nodeName  in  targetNodes) {
                    if (net.getNode(nodeName) == null)
                        throw new Exception("Node \"" + nodeName + "\" not found");
                }

                foreach (string linkName  in  targetLinks) {
                    if (net.getLink(linkName) == null)
                        throw new Exception("Link \"" + linkName + "\" not found");
                }

                nodesVariables.Add(NodeVariableType.ELEVATION);
                nodesVariables.Add(NodeVariableType.BASEDEMAND);

                if (pMap.getQualflag() != PropertiesMap.QualType.NONE)
                    nodesVariables.Add(NodeVariableType.INITQUALITY);

                nodesVariables.Add(NodeVariableType.PRESSURE);
                nodesVariables.Add(NodeVariableType.HEAD);
                nodesVariables.Add(NodeVariableType.DEMAND);

                if (pMap.getQualflag() != (PropertiesMap.QualType.NONE))
                    nodesVariables.Add(NodeVariableType.QUALITY);

                linksVariables.Add(LinkVariableType.LENGHT);
                linksVariables.Add(LinkVariableType.DIAMETER);
                linksVariables.Add(LinkVariableType.ROUGHNESS);
                linksVariables.Add(LinkVariableType.FLOW);
                linksVariables.Add(LinkVariableType.VELOCITY);
                linksVariables.Add(LinkVariableType.UNITHEADLOSS);
                linksVariables.Add(LinkVariableType.FRICTIONFACTOR);

                if (pMap.getQualflag() != PropertiesMap.QualType.NONE)
                    linksVariables.Add(LinkVariableType.QUALITY);

                hydFile = Path.GetTempFileName(); // "hydSim.bin"

                consoleLog("START_RUNNING");

                HydraulicSim hydSim = new HydraulicSim(net, log);
                hydSim.simulate(hydFile);


                if (net.getPropertiesMap().getQualflag() != (PropertiesMap.QualType.NONE)) {
                    qualFile = Path.GetTempFileName(); // "qualSim.bin"

                    QualitySim q = new QualitySim(net, log);
                    q.simulate(hydFile, qualFile);
                }


                HydraulicReader hydReader = new HydraulicReader(new BinaryReader(File.OpenRead(hydFile)));

                StreamWriter nodesTextWriter = null;
                StreamWriter linksTextWriter = null;
                string nodesOutputFile = null;
                string linksOutputFile = null;

                if (targetNodes.Count == 0 && targetLinks.Count == 0 || targetNodes.Count > 0) {
                    nodesOutputFile = Path.GetFullPath(inFile) + ".nodes.out";
                    nodesTextWriter = new StreamWriter(nodesOutputFile, false, Encoding.UTF8);

                    nodesTextWriter.Write('\t');
                    foreach (NodeVariableType nodeVar  in  nodesVariables) {
                        nodesTextWriter.Write('\t');
                        nodesTextWriter.Write(nodeVar.ToString());
                    }
                    nodesTextWriter.Write("\n\t");

                    foreach (NodeVariableType nodeVar  in  nodesVariables) {
                        nodesTextWriter.Write('\t');
                        nodesTextWriter.Write(net.getFieldsMap().getField(ToFieldType(nodeVar)).getUnits());
                    }
                    nodesTextWriter.Write('\n');
                }


                if (targetNodes.Count == 0 && targetLinks.Count == 0 || targetLinks.Count > 0) {
                    linksOutputFile = Path.GetFullPath(inFile) + ".links.out";
                    linksTextWriter = new StreamWriter(linksOutputFile, false, Encoding.UTF8);

                    linksTextWriter.Write('\t');
                    foreach (LinkVariableType linkVar  in  linksVariables) {
                        linksTextWriter.Write('\t');
                        linksTextWriter.Write(linkVar.ToString());
                    }
                    linksTextWriter.Write("\n\t");

                    foreach (LinkVariableType linkVar  in  linksVariables) {
                        linksTextWriter.Write('\t');
                        if (linkVar < 0) {
                            continue;
                        }
                        linksTextWriter.Write(net.getFieldsMap().getField((FieldsMap.Type)linkVar).getUnits());
                    }
                    linksTextWriter.Write('\n');
                }


                for (long time = pMap.getRstart(); time <= pMap.getDuration(); time += pMap.getRstep()) {
                    AwareStep step = hydReader.getStep((int)time);

                    int i = 0;

                    if (targetTimes.Count > 0 && !targetTimes.Contains(time))
                        continue;

                    if (nodesTextWriter != null) {
                        foreach (Node node  in  net.getNodes()) {
                            if (targetNodes.Count > 0 && !targetNodes.Contains(node.getId()))
                                continue;

                            nodesTextWriter.Write(node.getId());

                            nodesTextWriter.Write('\t');
                            nodesTextWriter.Write(Utilities.getClockTime(time));

                            foreach (NodeVariableType nodeVar  in  nodesVariables) {
                                nodesTextWriter.Write('\t');
                                double val = GetNodeValue(nodeVar, net.getFieldsMap(), step, node, i);
                                nodesTextWriter.Write(convertToScientifcNotation(val, 1000, 0.01, 2));
                            }

                            nodesTextWriter.Write('\n');

                            i++;
                        }
                    }

                    i = 0;

                    if (linksTextWriter != null) {
                        foreach (Link link  in  net.getLinks()) {
                            if (targetLinks.Count > 0 && !targetLinks.Contains(link.getId()))
                                continue;

                            linksTextWriter.Write(link.getId());

                            linksTextWriter.Write('\t');
                            linksTextWriter.Write(Utilities.getClockTime(time));

                            foreach (LinkVariableType linkVar  in  linksVariables) {
                                linksTextWriter.Write('\t');
                                double val = GetLinkValue(linkVar, net.getPropertiesMap().getFormflag(), net.getFieldsMap(), step, link, i);
                                linksTextWriter.Write(convertToScientifcNotation(val, 1000, 0.01, 2));
                            }

                            linksTextWriter.Write('\n');

                            i++;
                        }
                    }
                }

                if (nodesTextWriter != null) {
                    nodesTextWriter.Close();
                    consoleLog("NODES FILE \"" + nodesOutputFile + "\"");
                }

                if (linksTextWriter != null) {
                    linksTextWriter.Close();
                    consoleLog("LINKS FILES \"" + nodesOutputFile + "\"");
                }

                consoleLog("END_RUN_OK");
            }
            catch (ENException e) {
                consoleLog("END_RUN_ERR");
                Debug.Print(e.ToString());
            }
            catch (IOException e) {
                consoleLog("END_RUN_ERR");
                Debug.Print(e.ToString());
            }
            catch (Exception e) {
                consoleLog("END_RUN_ERR");
                Debug.Print(e.ToString());
            }

            if (!string.IsNullOrEmpty(hydFile))
                File.Delete(hydFile);

            if (!string.IsNullOrEmpty(qualFile))
                File.Delete(qualFile);
        }
    }

}