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
using System.Diagnostics;
using System.IO;
using System.Linq;

using Epanet.Enums;
using Epanet.Hydraulic.IO;
using Epanet.Network;
using Epanet.Properties;
using Epanet.Quality;
using Epanet.Util;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet {

    ///<summary>
    ///  This class handles the XLSX generation from the binary files created by Epanet simulation, the reported
    ///  fields are configured via the ReportOptions class.
    /// </summary>
    public class ReportGenerator {
        ///<summary>Hydraulic report fields.</summary>
        /// <summary>Hydraulic report fields.</summary>
        public struct HydVariable {
            public enum Type {
                Head = 0,
                Demands = 1,
                Pressure = 2,
                Flows = 3,
                Velocity = 4,
                Headloss = 5,
                Friction = 6
            }

            public static readonly HydVariable[] Values = {
                new HydVariable("Node head", true), // HydVariable.HYDR_VARIABLE_HEAD
                new HydVariable("Node actual demand", true), // HydVariable.HYDR_VARIABLE_DEMANDS
                new HydVariable("Node pressure", true), // HydVariable.HYDR_VARIABLE_PRESSURE
                new HydVariable("Link flows", false), // HydVariable.HYDR_VARIABLE_FLOWS
                new HydVariable("Link velocity", false), // HydVariable.HYDR_VARIABLE_VELOCITY
                new HydVariable("Link unit headloss", false), // HydVariable.HYDR_VARIABLE_HEADLOSS
                new HydVariable("Link friction factor", false) // HydVariable.HYDR_VARIABLE_FRICTION
            };

            public readonly bool isNode;
            public readonly string name;

            private HydVariable(string text, bool node) {
                name = text;
                isNode = node;
            }

            public override string ToString() { return name; }
        }

        /// <summary>Quality report fields.</summary>
        public struct QualVariable {
            public enum Type {
                Nodes = 0,
                Links = 1
                // Rate = 2
            }

            public static readonly QualVariable[] Values = {
                new QualVariable("Node quality", true), // QualVariable.QUAL_VARIABLE_NODES
                new QualVariable("Link quality", false) // QualVariable.QUAL_VARIABLE_LINKS
                // new QualVariable(Type.Rate, "Link reaction rate", false) // QualVariable.QUAL_VARIABLE_RATE
            };

            public readonly string name;
            public readonly bool isNode;

            private QualVariable(string text, bool node) {
                name = text;
                isNode = node;
            }

            public override string ToString() { return name; }
        }

        private readonly XlsxWriter _sheet;
        private readonly string _xlsxFile;

        public ReportGenerator(string xlsxFile) {
            _xlsxFile = xlsxFile;
            _sheet = new XlsxWriter();
        }

        ///<summary>Excel cells transposition mode.</summary>
        public bool TransposedMode {
            set => _sheet.TransposedMode = value;
            get => _sheet.TransposedMode;
        }

        ///<summary>Current report time progress.</summary>
        public TimeSpan Rtime { get; private set; }

        /// <summary>Generate hydraulic report.</summary>
        ///  <param name="hydBinFile">Name of the hydraulic simulation output file.</param>
        /// <param name="net">Hydraulic network.</param>
        /// <param name="values">Variables report flag.</param>
        public void CreateHydReport(string hydBinFile, EpanetNetwork net, bool[] values) {
            Rtime = TimeSpan.Zero;
            HydraulicReader dseek = new HydraulicReader(new BinaryReader(File.OpenRead(hydBinFile)));
            // int reportCount = (int)((net.Duration.Ticks - net.RStart.Ticks) / net.RStep.Ticks + 1);
            var nodes = net.Nodes;
            var links = net.Links;

            object[] nodesHead = new object[dseek.Nodes + 1];
            nodesHead[0] = _sheet.TransposedMode ? "Node/Time" : "Time/Node";
            for (int i = 0; i < nodes.Count; i++)
                nodesHead[i + 1] = nodes[i].Name;

            var linksHead = new object[dseek.Links + 1];
            linksHead[0] = _sheet.TransposedMode ? "Link/Time" : "Time/Link";
            for (int i = 0; i < links.Count; i++)
                linksHead[i + 1] = links[i].Name;

            XlsxWriter.Spreadsheet[] resultSheets = new XlsxWriter.Spreadsheet[HydVariable.Values.Length];
            // Array.Clear(resultSheets, 0, resultSheets.Length);

            for (int i = 0; i < resultSheets.Length; i++) {
                if (values != null && !values[i]) continue;
                resultSheets[i] = _sheet.NewSpreadsheet(HydVariable.Values[i].name);
                resultSheets[i].AddHeader(HydVariable.Values[i].isNode ? nodesHead : linksHead);
            }

            var nodeRow = new object[dseek.Nodes + 1];
            var linkRow = new object[dseek.Links + 1];

            for (TimeSpan time = net.RStart; time <= net.Duration; time += net.RStep) {

                var step = dseek.GetStep(time);

                if (step == null) {
                    Rtime = time;
                    continue;
                }

                nodeRow[0] = time.GetClockTime();
                linkRow[0] = time.GetClockTime();

                // NODES HEADS
                if (resultSheets[(int)HydVariable.Type.Head] != null) {
                    for (int i = 0; i < nodes.Count; i++)
                        nodeRow[i + 1] = step.GetNodeHead(i, net.FieldsMap);

                    resultSheets[(int)HydVariable.Type.Head].AddData(nodeRow);
                }

                // NODES DEMANDS
                if (resultSheets[(int)HydVariable.Type.Demands] != null) {
                    for (int i = 0; i < nodes.Count; i++) {
                        nodeRow[i + 1] = step.GetNodeDemand(i, net.FieldsMap);
                    }

                    resultSheets[(int)HydVariable.Type.Demands].AddData(nodeRow);
                }

                // NODES PRESSURE
                if (resultSheets[(int)HydVariable.Type.Pressure] != null) {
                    for (int i = 0; i < nodes.Count; i++) {
                        nodeRow[i + 1] = step.GetNodePressure(i, nodes[i], net.FieldsMap);
                    }

                    resultSheets[(int)HydVariable.Type.Pressure].AddData(nodeRow);
                }

                // LINK FLOW
                if (resultSheets[(int)HydVariable.Type.Flows] != null) {
                    for (int i = 0; i < links.Count; i++) {
                        linkRow[i + 1] = step.GetLinkFlow(i, net.FieldsMap);
                    }

                    resultSheets[(int)HydVariable.Type.Flows].AddData(linkRow);
                }

                // LINK VELOCITY
                if (resultSheets[(int)HydVariable.Type.Velocity] != null) {
                    for (int i = 0; i < links.Count; i++) {
                        linkRow[i + 1] = step.GetLinkVelocity(i, links[i], net.FieldsMap);
                    }

                    resultSheets[(int)HydVariable.Type.Velocity].AddData(linkRow);
                }

                // LINK HEADLOSS
                if (resultSheets[(int)HydVariable.Type.Headloss] != null) {
                    for (int i = 0; i < links.Count; i++) {
                        linkRow[i + 1] = step.GetLinkHeadLoss(i, links[i], net.FieldsMap);
                    }

                    resultSheets[(int)HydVariable.Type.Headloss].AddData(linkRow);
                }

                // LINK FRICTION
                if (resultSheets[(int)HydVariable.Type.Friction] != null) {
                    for (int i = 0; i < links.Count; i++) {
                        linkRow[i + 1] = step.GetLinkFriction(i, links[i], net.FieldsMap);
                    }

                    resultSheets[(int)HydVariable.Type.Friction].AddData(linkRow);
                }

                Rtime = time;
            }

            dseek.Close();
        }

        /// <summary>Generate quality report.</summary>
        ///  <param name="qualFile">Name of the quality simulation output file.</param>
        /// <param name="net">Hydraulic network.</param>
        /// <param name="nodes">Show nodes quality flag.</param>
        /// <param name="links">Show links quality flag.</param>
        public void CreateQualReport(string qualFile, EpanetNetwork net, bool nodes, bool links) {
            Rtime = TimeSpan.Zero;

            // int reportCount = (int)((net.Duration.Ticks - net.RStart.Ticks) / net.RStep.Ticks + 1);

            using (QualityReader dseek = new QualityReader(qualFile, net.FieldsMap)) {
                var netNodes = net.Nodes;
                var netLinks = net.Links;    
          
                var nodesHead = new object[dseek.Nodes + 1];
                nodesHead[0] = _sheet.TransposedMode ? "Node/Time" : "Time/Node";
                for(int i = 0; i < netNodes.Count; i++)
                    nodesHead[i + 1] = netNodes[i].Name;

                var linksHead = new object[dseek.Links + 1];
                linksHead[0] = _sheet.TransposedMode ? "Link/Time" : "Time/Link";
                for(int i = 0; i < netLinks.Count; i++)
                    linksHead[i + 1] = netLinks[i].Name;

                var resultSheets = new XlsxWriter.Spreadsheet[HydVariable.Values.Length];
               
                for (int i = 0; i < QualVariable.Values.Length; i++) {
                    var qvar = QualVariable.Values[i];
                    if ((!qvar.isNode || !nodes) && (qvar.isNode || !links))
                        continue;

                    resultSheets[i] = _sheet.NewSpreadsheet(qvar.name);
                    resultSheets[i].AddHeader(qvar.isNode ? nodesHead : linksHead);
                }

                var nodeRow = new object[dseek.Nodes + 1];
                var linkRow = new object[dseek.Links + 1];

                using (var qIt = dseek.GetEnumerator())
                    for (TimeSpan time = net.RStart; time <= net.Duration; time += net.RStep)
                    {
                        if (!qIt.MoveNext()) return;

                        var step = qIt.Current;
                        if (step == null) continue;

                        nodeRow[0] = time.GetClockTime();
                        linkRow[0] = time.GetClockTime();

                        if (resultSheets[(int)QualVariable.Type.Nodes] != null) {
                            for (int i = 0; i < dseek.Nodes; i++)
                                nodeRow[i + 1] = (double)step.GetNodeQuality(i);

                            resultSheets[(int)QualVariable.Type.Nodes].AddData(nodeRow);
                        }

                        if (resultSheets[(int)QualVariable.Type.Links] != null) {
                            for (int i = 0; i < dseek.Links; i++)
                                linkRow[i + 1] = (double)step.GetLinkQuality(i);

                            resultSheets[(int)QualVariable.Type.Links].AddData(linkRow);
                        }

                        Rtime = time;
                    }
            }
        }

        /// <summary>Write the final worksheet.</summary>
        public void WriteWorksheet() {
            _sheet.Save(_xlsxFile);
        }

        /// <summary>Write simulation summary to one worksheet.</summary>
        ///  <param name="inpFile">Hydraulic network file name.</param>
        /// <param name="net">Hydraulic network.</param>
        public void WriteSummary(string inpFile, EpanetNetwork net) {
            var sh = _sheet.NewSpreadsheet("Summary");

            try {
                FieldsMap fMap = net.FieldsMap;

                if (net.Title != null)
                    for (int i = 0; i < net.Title.Count && i < 3; i++) {
                        if (!string.IsNullOrEmpty(net.Title[i])) {
                            sh.AddData(
                                net.Title[i].Length > 70
                                    ? net.Title[i].Substring(0, 70)
                                    : net.Title[i]);
                        }
                    }

                sh.AddData(string.Empty);
                sh.AddData(Text.FMT19, inpFile);
                sh.AddData(Text.FMT20, net.Junctions.Count());

                int nReservoirs = net.Reservoirs.Count();
                int nTanks = net.Tanks.Count();
                int nValves = net.Valves.Count();
                int nPumps = net.Pumps.Count();
                int nPipes = net.Links.Count - nPumps - nValves;

                sh.AddData(Text.FMT21a, nReservoirs);
                sh.AddData(Text.FMT21b, nTanks);
                sh.AddData(Text.FMT22, nPipes);
                sh.AddData(Text.FMT23, nPumps);
                sh.AddData(Text.FMT24, nValves);
                sh.AddData(Text.FMT25, net.FormFlag.ParseStr());

                sh.AddData(Text.FMT26, net.HStep.GetClockTime());
                sh.AddData(Text.FMT27, net.HAcc);
                sh.AddData(Text.FMT27a, net.CheckFreq);
                sh.AddData(Text.FMT27b, net.MaxCheck);
                sh.AddData(Text.FMT27c, net.DampLimit);
                sh.AddData(Text.FMT28, net.MaxIter);

                switch (net.Duration.IsZero() ? QualType.NONE : net.QualFlag) {
                    case QualType.NONE:
                        sh.AddData(Text.FMT29, "None");
                        break;
                    case QualType.CHEM:
                        sh.AddData(Text.FMT30, net.ChemName);
                        break;
                    case QualType.TRACE:
                        sh.AddData(Text.FMT31, "Trace From Node", net.GetNode(net.TraceNode).Name);
                        break;
                    case QualType.AGE:
                        sh.AddData(Text.FMT32, "Age");
                        break;
                }

                if (net.QualFlag != QualType.NONE && net.Duration.Ticks > 0) {
                    sh.AddData(Text.FMT33, "Time Step", net.QStep.GetClockTime());
                    sh.AddData(
                        Text.FMT34,
                        "Tolerance",
                        fMap.RevertUnit(FieldType.QUALITY, net.Ctol),
                        fMap.GetField(FieldType.QUALITY).Units);
                }

                sh.AddData(Text.FMT36, net.SpGrav);
                sh.AddData(Text.FMT37a, net.Viscos / Constants.VISCOS);
                sh.AddData(Text.FMT37b, net.Diffus / Constants.DIFFUS);
                sh.AddData(Text.FMT38, net.DMult);

                // FIXME: wrong convertion?
                sh.AddData(Text.FMT39, 
                    fMap.RevertUnit(FieldType.TIME, net.Duration.TotalSeconds), 
                    fMap.GetField(FieldType.TIME).Units);

            }
            catch (IOException) {
                //TODO: error handling
            }
            catch (EnException e) {
                Debug.Print(e.ToString());
            }
        }
    }

}
