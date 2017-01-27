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
using System.Linq;
using System.Text;

using Epanet.Enums;
using Epanet.Hydraulic;
using Epanet.Hydraulic.IO;
using Epanet.Network;
using Epanet.Network.IO.Input;
using Epanet.Network.Structures;
using Epanet.Quality;
using Epanet.Util;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet {

    public class EPATool {

        private static void ConsoleLog(string msg) { Console.WriteLine(msg + " " + DateTime.Now.ToString("HH:mm:ss")); }

        private static string ConvertToScientifcNotation(
            double value,
            double maxThreshold,
            double minThreshold,
            int @decimal) {
            if (double.IsNaN(value))
                return null;

            if (Math.Abs(value) > double.Epsilon && (Math.Abs(value) > maxThreshold || Math.Abs(value) < minThreshold))
                return value.ToString("E" + @decimal.ToString(CultureInfo.InvariantCulture));

            return value.ToString("F" + @decimal.ToString(CultureInfo.InvariantCulture));
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

        private static FieldType ToFieldType(NodeVariableType value) {
            return (FieldType)((int)value & ~0x1000);
        }

        private static double GetNodeValue(NodeVariableType type, FieldsMap fmap, AwareStep step, Node node, int index) {
            switch (type) {
            case NodeVariableType.BASEDEMAND: {
                double dsum = node.Demands.Sum(demand => demand.Base);
                return fmap.RevertUnit((FieldType)type, dsum);
            }
            case NodeVariableType.ELEVATION:
                return fmap.RevertUnit((FieldType)type, node.Elevation);
            case NodeVariableType.DEMAND:
                return step != null ? step.GetNodeDemand(index, node, fmap) : 0;
            case NodeVariableType.HEAD:
                return step != null ? step.GetNodeHead(index, node, fmap) : 0;
            case NodeVariableType.INITQUALITY:
                return fmap.RevertUnit((FieldType)type, node.C0);
            case NodeVariableType.PRESSURE:
                return step != null ? step.GetNodePressure(index, node, fmap) : 0;
            case NodeVariableType.QUALITY:
                return step != null ? step.GetNodeQuality(index) : 0;
            default:
                return 0.0;
            }
        }

        private enum LinkVariableType {
            LENGHT = FieldType.LENGTH,
            DIAMETER = FieldType.DIAM,
            ROUGHNESS = -1,
            FLOW = FieldType.FLOW,
            VELOCITY = FieldType.VELOCITY,
            UNITHEADLOSS = FieldType.HEADLOSS,
            FRICTIONFACTOR = FieldType.FRICTION,
            QUALITY = FieldType.QUALITY
        }


        private static double GetLinkValue(
            LinkVariableType type,
            FormType formType,
            FieldsMap fmap,
            AwareStep step,
            Link link,
            int index) {

            switch (type) {
            case LinkVariableType.LENGHT:
                return fmap.RevertUnit((FieldType)type, link.Lenght);
        
            case LinkVariableType.DIAMETER:
                return fmap.RevertUnit((FieldType)type, link.Diameter);
         
            case LinkVariableType.ROUGHNESS:
                return link.Type == LinkType.PIPE && formType == FormType.DW
                    ? fmap.RevertUnit(FieldType.DIAM, link.Kc)
                    : link.Kc;

            case LinkVariableType.FLOW:
                return step != null ? Math.Abs(step.GetLinkFlow(index, link, fmap)) : 0;
         
            case LinkVariableType.VELOCITY:
                return step != null ? Math.Abs(step.GetLinkVelocity(index, link, fmap)) : 0;
          
            case LinkVariableType.UNITHEADLOSS:
                return step != null ? step.GetLinkHeadLoss(index, link, fmap) : 0;
          
            case LinkVariableType.FRICTIONFACTOR:
                return step != null ? step.GetLinkFriction(index, link, fmap) : 0;
        
            case LinkVariableType.QUALITY:
                return step != null ? fmap.RevertUnit((FieldType)type, step.GetLinkAvrQuality(index)) : 0;
         
            default:
                return 0.0;
            }
        }

        public static void main(string[] args) {
            TraceSource log = new TraceSource(typeof(EPATool).FullName, SourceLevels.All);

            string hydFile = null;
            string qualFile = null;
            var net = new EpanetNetwork();

            List<NodeVariableType> nodesVariables = new List<NodeVariableType>();
            List<LinkVariableType> linksVariables = new List<LinkVariableType>();

            string inFile = "";
            List<long> targetTimes = new List<long>();
            List<string> targetNodes = new List<string>();
            List<string> targetLinks = new List<string>();

            int parseMode = 0;
            foreach (string arg in args) {
                if (arg.EndsWith(".inp", StringComparison.OrdinalIgnoreCase)) {
                    parseMode = 0;
                    inFile = arg;
                    if (!File.Exists(inFile)) {
                        ConsoleLog("END_RUN_ERR");
                        Console.Error.WriteLine("File not found !");
                        return;
                    }
                    continue;
                }

                switch (arg) {
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

                switch (parseMode) {
                case 1:
                    targetTimes.Add((long)(Utilities.GetHour(arg) * 3600));
                    break;
                case 2:
                    targetNodes.Add(arg);
                    break;
                case 3:
                    targetLinks.Add(arg);
                    break;
                }
            }

            try {
                InputParser parserInp = InputParser.Create(FileType.INP_FILE);
                net = parserInp.Parse(new EpanetNetwork(), inFile);
                

                if (targetTimes.Count > 0) {
                    foreach (long time  in  targetTimes) {
                        string epanetTime = time.GetClockTime();
                        if (time < net.RStart)
                            throw new Exception("Target time \"" + epanetTime + "\" smaller than simulation start time");

                        if (time > net.Duration)
                            throw new Exception("Target time \"" + epanetTime + "\" bigger than simulation duration");

                        if ((time - net.RStart) % net.RStep != 0)
                            throw new Exception("Target time \"" + epanetTime + "\" not found");
                    }
                }

                foreach (string nodeName  in  targetNodes) {
                    if (net.GetNode(nodeName) == null)
                        throw new Exception("Node \"" + nodeName + "\" not found");
                }

                foreach (string linkName  in  targetLinks) {
                    if (net.GetLink(linkName) == null)
                        throw new Exception("Link \"" + linkName + "\" not found");
                }

                nodesVariables.Add(NodeVariableType.ELEVATION);
                nodesVariables.Add(NodeVariableType.BASEDEMAND);

                if (net.QualFlag != QualType.NONE)
                    nodesVariables.Add(NodeVariableType.INITQUALITY);

                nodesVariables.Add(NodeVariableType.PRESSURE);
                nodesVariables.Add(NodeVariableType.HEAD);
                nodesVariables.Add(NodeVariableType.DEMAND);

                if (net.QualFlag != (QualType.NONE))
                    nodesVariables.Add(NodeVariableType.QUALITY);

                linksVariables.Add(LinkVariableType.LENGHT);
                linksVariables.Add(LinkVariableType.DIAMETER);
                linksVariables.Add(LinkVariableType.ROUGHNESS);
                linksVariables.Add(LinkVariableType.FLOW);
                linksVariables.Add(LinkVariableType.VELOCITY);
                linksVariables.Add(LinkVariableType.UNITHEADLOSS);
                linksVariables.Add(LinkVariableType.FRICTIONFACTOR);

                if (net.QualFlag != QualType.NONE)
                    linksVariables.Add(LinkVariableType.QUALITY);

                hydFile = Path.GetTempFileName(); // "hydSim.bin"

                ConsoleLog("START_RUNNING");

                HydraulicSim hydSim = new HydraulicSim(net, log);
                hydSim.Simulate(hydFile);


                if (net.QualFlag != QualType.NONE) {
                    qualFile = Path.GetTempFileName(); // "qualSim.bin"

                    QualitySim q = new QualitySim(net, log);
                    q.Simulate(hydFile, qualFile);
                }


                HydraulicReader hydReader = new HydraulicReader(new BinaryReader(File.OpenRead(hydFile)));

                StreamWriter nodesTextWriter = null;
                StreamWriter linksTextWriter = null;
                string nodesOutputFile = null;

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
                        nodesTextWriter.Write(net.FieldsMap.GetField(ToFieldType(nodeVar)).Units);
                    }
                    nodesTextWriter.Write('\n');
                }


                if (targetNodes.Count == 0 && targetLinks.Count == 0 || targetLinks.Count > 0) {
                    string linksOutputFile = Path.GetFullPath(inFile) + ".links.out";
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
                        linksTextWriter.Write(net.FieldsMap.GetField((FieldType)linkVar).Units);
                    }
                    linksTextWriter.Write('\n');
                }


                for (long time = net.RStart; time <= net.Duration; time += net.RStep) {
                    AwareStep step = hydReader.GetStep((int)time);

                    int i = 0;

                    if (targetTimes.Count > 0 && !targetTimes.Contains(time))
                        continue;

                    if (nodesTextWriter != null) {
                        foreach (Node node  in  net.Nodes) {
                            if (targetNodes.Count > 0 && !targetNodes.Contains(node.Name))
                                continue;

                            nodesTextWriter.Write(node.Name);

                            nodesTextWriter.Write('\t');
                            nodesTextWriter.Write(time.GetClockTime());

                            foreach (NodeVariableType nodeVar  in  nodesVariables) {
                                nodesTextWriter.Write('\t');
                                double val = GetNodeValue(nodeVar, net.FieldsMap, step, node, i);
                                nodesTextWriter.Write(ConvertToScientifcNotation(val, 1000, 0.01, 2));
                            }

                            nodesTextWriter.Write('\n');

                            i++;
                        }
                    }

                    i = 0;

                    if (linksTextWriter != null) {
                        foreach (Link link  in  net.Links) {
                            if (targetLinks.Count > 0 && !targetLinks.Contains(link.Name))
                                continue;

                            linksTextWriter.Write(link.Name);

                            linksTextWriter.Write('\t');
                            linksTextWriter.Write(time.GetClockTime());

                            foreach (LinkVariableType linkVar  in  linksVariables) {
                                linksTextWriter.Write('\t');
                                double val = GetLinkValue(
                                    linkVar,
                                    net.FormFlag,
                                    net.FieldsMap,
                                    step,
                                    link,
                                    i);
                                linksTextWriter.Write(ConvertToScientifcNotation(val, 1000, 0.01, 2));
                            }

                            linksTextWriter.Write('\n');

                            i++;
                        }
                    }
                }

                if (nodesTextWriter != null) {
                    nodesTextWriter.Close();
                    ConsoleLog("NODES FILE \"" + nodesOutputFile + "\"");
                }

                if (linksTextWriter != null) {
                    linksTextWriter.Close();
                    ConsoleLog("LINKS FILES \"" + nodesOutputFile + "\"");
                }

                ConsoleLog("END_RUN_OK");
            }
            catch (ENException e) {
                ConsoleLog("END_RUN_ERR");
                Debug.Print(e.ToString());
            }
            catch (IOException e) {
                ConsoleLog("END_RUN_ERR");
                Debug.Print(e.ToString());
            }
            catch (Exception e) {
                ConsoleLog("END_RUN_ERR");
                Debug.Print(e.ToString());
            }

            if (!string.IsNullOrEmpty(hydFile))
                File.Delete(hydFile);

            if (!string.IsNullOrEmpty(qualFile))
                File.Delete(qualFile);
        }
    }

}