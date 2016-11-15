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
using Epanet.Hydraulic.IO;
using Epanet.MSX;
using Epanet.MSX.Structures;
using Epanet.Network;
using Epanet.Properties;
using Epanet.Quality;
using Epanet.Report;
using Epanet.Util;
using Link = Epanet.Network.Structures.Link;
using Node = Epanet.Network.Structures.Node;

namespace Epanet.UI {

    ///<summary>
    ///  This class handles the XLSX generation from the binary files created by Epanet and MSX simulations, the reported
    ///  fields are configured via the ReportOptions class.
    /// </summary>
    public class ReportGenerator {
        private static readonly string[] hydVariableName = {
            "Node head", "Node actual demand", "Node pressure",
            "Link flows", "Link velocity", "Link unit headloss", "Link friction factor"
        };

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
                new HydVariable(Type.Head, "Node head", true), // HydVariable.HYDR_VARIABLE_HEAD
                new HydVariable(Type.Demands, "Node actual demand", true), // HydVariable.HYDR_VARIABLE_DEMANDS
                new HydVariable(Type.Pressure, "Node pressure", true), // HydVariable.HYDR_VARIABLE_PRESSURE
                new HydVariable(Type.Flows, "Link flows", false), // HydVariable.HYDR_VARIABLE_FLOWS
                new HydVariable(Type.Velocity, "Link velocity", false), // HydVariable.HYDR_VARIABLE_VELOCITY
                new HydVariable(Type.Headloss, "Link unit headloss", false), // HydVariable.HYDR_VARIABLE_HEADLOSS
                new HydVariable(Type.Friction, "Link friction factor", false) // HydVariable.HYDR_VARIABLE_FRICTION
            };

            public readonly bool IsNode;
            public readonly string Name;
            public readonly int ID;

            private HydVariable(Type id, string text, bool node) {
                this.Name = text;
                this.IsNode = node;
                this.ID = (int)id;
            }

            public override string ToString() { return this.Name; }
        }

        private static readonly string[] QualVariableName = {"Node quality", "Link quality", "Link reaction rate"};
        private static readonly bool[] QualVariableIsNode = {true, false, false};

        /// <summary>Quality report fields.</summary>
        public struct QualVariable {
            public enum Type {
                Nodes = 0,
                Links = 1,
                // Rate = 2
            }

            public static readonly QualVariable[] Values = {
                new QualVariable(Type.Nodes, "Node quality", true), // QualVariable.QUAL_VARIABLE_NODES
                new QualVariable(Type.Links, "Link quality", false) // QualVariable.QUAL_VARIABLE_LINKS
                // new QualVariable(Type.Rate, "Link reaction rate", false) // QualVariable.QUAL_VARIABLE_RATE
            };

            public readonly int ID;
            public readonly string Name;
            public readonly bool IsNode;

            private QualVariable(Type id, string text, bool node) {
                this.Name = text;
                this.IsNode = node;
                this.ID = (int)id;
            }

            public override string ToString() { return this.Name; }
        }

        private readonly XLSXWriter sheet;
        private readonly string xlsxFile;

        ///<summary>Current report time.</summary>
        private long Rtime;

        public ReportGenerator(string xlsxFile) {
            this.xlsxFile = xlsxFile;
            this.sheet = new XLSXWriter();
        }

        ///<summary>Set excel cells transposition mode.</summary>
        public void setTransposedMode(bool value) {
            this.sheet.TransposedMode = value;
        }

        ///<summary>Get current report time progress.</summary>
        public long getRtime() {
            return this.Rtime;
        }

        /// <summary>Generate hydraulic report.</summary>
        ///  <param name="hydBinFile">Name of the hydraulic simulation output file.</param>
        /// <param name="net">Hydraulic network.</param>
        /// <param name="values">Variables report flag.</param>
        public void CreateHydReport(string hydBinFile, Network.Network net, bool[] values) {
            this.Rtime = 0;
            HydraulicReader dseek = new HydraulicReader(new BinaryReader(File.OpenRead(hydBinFile)));

            int reportCount = (int)((net.PropertiesMap.Duration - net.PropertiesMap.Rstart) / net.PropertiesMap.Rstep)
                              + 1;
            var netNodes = net.Nodes;
            var netLinks = net.Links;

            object[] nodesHead = new object[dseek.Nodes + 1];

            nodesHead[0] = this.sheet.TransposedMode ? "Node/Time" : "Time/Node";

            int count = 1;
            foreach (Node node in netNodes) {
                nodesHead[count++] = node.Id;
            }

            object[] linksHead = new object[dseek.Links + 1];

            linksHead[0] = this.sheet.TransposedMode ? "Link/Time" : "Time/Link";

            count = 1;
            foreach (Link link in netLinks) {
                linksHead[count++] = link.Id;
            }

            XLSXWriter.Spreadsheet[] resultSheets = new XLSXWriter.Spreadsheet[HydVariable.Values.Length];
            // Array.Clear(resultSheets, 0, resultSheets.Length);

            for (int i = 0; i < resultSheets.Length; i++) {
                if (values != null && !values[i]) continue;
                resultSheets[i] = this.sheet.NewSpreadsheet(HydVariable.Values[i].Name);
                resultSheets[i].AddData(HydVariable.Values[i].IsNode ? nodesHead : linksHead);
            }

            object[] nodeRow = new object[dseek.Nodes + 1];
            object[] linkRow = new object[dseek.Links + 1];

            for (long time = net.PropertiesMap.Rstart;
                 time <= net.PropertiesMap.Duration;
                 time += net.PropertiesMap.Rstep) {
                AwareStep step = dseek.getStep(time);

                if (step != null) {
                    nodeRow[0] = time.GetClockTime();
                    linkRow[0] = time.GetClockTime();

                    // NODES HEADS
                    if (resultSheets[(int)HydVariable.Type.Head] != null) {
                        for (int i = 0; i < netNodes.Count; i++) {
                            nodeRow[i + 1] = step.GetNodeHead(i, netNodes[i], net.FieldsMap);
                        }
                        resultSheets[(int)HydVariable.Type.Head].AddData(nodeRow);
                    }

                    // NODES DEMANDS
                    if (resultSheets[(int)HydVariable.Type.Demands] != null) {
                        for (int i = 0; i < netNodes.Count; i++) {
                            nodeRow[i + 1] = step.GetNodeDemand(i, netNodes[i], net.FieldsMap);
                        }
                        resultSheets[(int)HydVariable.Type.Demands].AddData(nodeRow);
                    }

                    // NODES PRESSURE
                    if (resultSheets[(int)HydVariable.Type.Pressure] != null) {
                        for (int i = 0; i < netNodes.Count; i++) {
                            nodeRow[i + 1] = step.GetNodePressure(i, netNodes[i], net.FieldsMap);
                        }
                        resultSheets[(int)HydVariable.Type.Pressure].AddData(nodeRow);
                    }

                    // LINK FLOW
                    if (resultSheets[(int)HydVariable.Type.Flows] != null) {
                        for (int i = 0; i < netLinks.Count; i++) {
                            linkRow[i + 1] = step.GetLinkFlow(i, netLinks[i], net.FieldsMap);
                        }
                        resultSheets[(int)HydVariable.Type.Flows].AddData(linkRow);
                    }

                    // LINK VELOCITY
                    if (resultSheets[(int)HydVariable.Type.Velocity] != null) {
                        for (int i = 0; i < netLinks.Count; i++) {
                            linkRow[i + 1] = step.GetLinkVelocity(i, netLinks[i], net.FieldsMap);
                        }
                        resultSheets[(int)HydVariable.Type.Velocity].AddData(linkRow);
                    }

                    // LINK HEADLOSS
                    if (resultSheets[(int)HydVariable.Type.Headloss] != null) {
                        for (int i = 0; i < netLinks.Count; i++) {
                            linkRow[i + 1] = step.GetLinkHeadLoss(i, netLinks[i], net.FieldsMap);
                        }
                        resultSheets[(int)HydVariable.Type.Headloss].AddData(linkRow);
                    }

                    // LINK FRICTION
                    if (resultSheets[(int)HydVariable.Type.Friction] != null) {
                        for (int i = 0; i < netLinks.Count; i++) {
                            linkRow[i + 1] = step.GetLinkFriction(i, netLinks[i], net.FieldsMap);
                        }
                        resultSheets[(int)HydVariable.Type.Friction].AddData(linkRow);
                    }
                }
                this.Rtime = time;
            }

            dseek.Close();
        }

        /// <summary>Generate quality report.</summary>
        ///  <param name="qualFile">Name of the quality simulation output file.</param>
        /// <param name="net">Hydraulic network.</param>
        /// <param name="nodes">Show nodes quality flag.</param>
        /// <param name="links">Show links quality flag.</param>
        public void createQualReport(string qualFile, Network.Network net, bool nodes, bool links) {
            this.Rtime = 0;

            int reportCount = (int)((net.PropertiesMap.Duration - net.PropertiesMap.Rstart) / net.PropertiesMap.Rstep)
                              + 1;

            using (QualityReader dseek = new QualityReader(qualFile, net.FieldsMap)) {
                object[] nodesHead = new object[dseek.Nodes + 1];
                nodesHead[0] = this.sheet.TransposedMode ? "Node/Time" : "Time/Node";
                for (int i = 0; i < net.Nodes.Count; i++) {
                    nodesHead[i + 1] = net.Nodes[i].Id;
                }

                object[] linksHead = new object[dseek.Links + 1];
                linksHead[0] = this.sheet.TransposedMode ? "Link/Time" : "Time/Link";
                for (int i = 0; i < net.Links.Count; i++) {
                    linksHead[i + 1] = net.Links[i].Id;
                }

                XLSXWriter.Spreadsheet[] resultSheets = new XLSXWriter.Spreadsheet[hydVariableName.Length];
                // Array.Clear(resultSheets, 0, resultSheets.Length);

                foreach (int var  in  Enum.GetValues(typeof(QualVariable))) {
                    if ((!QualVariableIsNode[var] || !nodes) && (QualVariableIsNode[var] || !links))
                        continue;

                    resultSheets[var] = this.sheet.NewSpreadsheet(QualVariableName[var]);
                    resultSheets[var].AddData(QualVariableIsNode[var] ? nodesHead : linksHead);
                }

                object[] nodeRow = new object[dseek.Nodes + 1];
                object[] linkRow = new object[dseek.Links + 1];

                using (var qIt = dseek.GetEnumerator())
                    for (long time = net.PropertiesMap.Rstart;
                         time <= net.PropertiesMap.Duration;
                         time += net.PropertiesMap.Rstep) {
                        if (!qIt.MoveNext())
                            return;

                        QualityReader.Step step = qIt.Current;
                        if (step != null) {
                            nodeRow[0] = time.GetClockTime();
                            linkRow[0] = time.GetClockTime();

                            if (resultSheets[(int)QualVariable.Type.Nodes] != null) {
                                for (int i = 0; i < dseek.Nodes; i++)
                                    nodeRow[i + 1] = (double)step.GetNodeQuality(i);
                                resultSheets[(int)HydVariable.Type.Head].AddData(nodeRow);
                            }

                            if (resultSheets[(int)QualVariable.Type.Links] != null) {
                                for (int i = 0; i < dseek.Links; i++)
                                    linkRow[i + 1] = (double)step.GetLinkQuality(i);
                                resultSheets[(int)HydVariable.Type.Demands].AddData(linkRow);
                            }
                            this.Rtime = time;
                        }
                    }
            }
        }

        /// <summary>Generate multi-species quality report.</summary>
        ///  <param name="msxBin">Name of the MSX simulation output file.</param>
        /// <param name="net">Hydraulic network.</param>
        /// <param name="netMSX">MSX network.</param>
        /// <param name="tk2">Hydraulic network - MSX bridge.</param>
        /// <param name="values">Species report flag.</param>
        public void createMSXReport(string msxBin, Network.Network net, EpanetMSX netMSX, ENToolkit2 tk2, bool[] values) {
            this.Rtime = 0;
            MSX.Structures.Node[] nodes = netMSX.Network.Node;
            MSX.Structures.Link[] links = netMSX.Network.Link;
            string[] nSpecies = netMSX.GetSpeciesNames();

            int reportCount = (int)((net.PropertiesMap.Duration - net.PropertiesMap.Rstart) / net.PropertiesMap.Rstep)
                              + 1;

            MsxReader reader = new MsxReader(nodes.Length - 1, links.Length - 1, nSpecies.Length, netMSX.ResultsOffset);

            int totalSpecies;

            if (values != null) {
                totalSpecies = 0;
                foreach (bool b  in  values)
                    if (b)
                        totalSpecies++;
            }
            else
                totalSpecies = nSpecies.Length;

            reader.Open(msxBin);

            object[] nodesHead = new object[nSpecies.Length + 1];
            nodesHead[0] = this.sheet.TransposedMode ? "Node/Time" : "Time/Node";

            object[] linksHead = new object[nSpecies.Length + 1];
            linksHead[0] = this.sheet.TransposedMode ? "Link/Time" : "Time/Link";

            int count = 1;
            for (int i = 0; i < nSpecies.Length; i++)
                if (values == null || values[i]) {
                    nodesHead[count] = nSpecies[i];
                    linksHead[count++] = nSpecies[i];
                }

            object[] nodeRow = new object[totalSpecies + 1];
            for (int i = 1; i < nodes.Length; i++) {
                if (nodes[i].Rpt) {
                    var spr = this.sheet.NewSpreadsheet("Node&lt;&lt;" + tk2.ENgetnodeid(i) + "&gt;&gt;");
                    spr.AddData(nodesHead);

                    for (long time = net.PropertiesMap.Rstart, period = 0;
                         time <= net.PropertiesMap.Duration;
                         time += net.PropertiesMap.Rstep, period++) {
                        nodeRow[0] = time.GetClockTime();

                        for (int j = 0, ji = 0; j < nSpecies.Length; j++) {
                            if (values == null || values[j])
                                nodeRow[ji++ + 1] = reader.GetNodeQual((int)period, i, j + 1);
                        }

                        spr.AddData(nodeRow);
                    }
                }
            }

            object[] linkRow = new object[totalSpecies + 1];
            for (int i = 1; i < links.Length; i++) {
                if (links[i].Rpt) {
                    XLSXWriter.Spreadsheet spr =
                        this.sheet.NewSpreadsheet("Link&lt;&lt;" + tk2.ENgetlinkid(i) + "&gt;&gt;");
                    spr.AddData(linksHead);

                    for (long time = net.PropertiesMap.Rstart, period = 0;
                         time <= net.PropertiesMap.Duration;
                         time += net.PropertiesMap.Rstep, period++) {
                        linkRow[0] = time.GetClockTime();

                        for (int j = 0, ji = 0; j < nSpecies.Length; j++) {
                            if (values == null || values[j])
                                linkRow[ji++ + 1] = reader.GetLinkQual((int)period, i, j + 1);
                        }

                        spr.AddData(linkRow);
                    }
                }
            }

            reader.Close();
        }

        /// <summary>Write the final worksheet.</summary>
        public void writeWorksheet() {
            this.sheet.Save(this.xlsxFile);
        }

        /// <summary>Write simulation summary to one worksheet.</summary>
        ///  <param name="inpFile">Hydraulic network file name.</param>
        /// <param name="net">Hydraulic network.</param>
        /// <param name="msxFile">MSX file.</param>
        /// <param name="msx">MSX solver.</param>
        public void writeSummary(string inpFile, Network.Network net, string msxFile, EpanetMSX msx) {
            XLSXWriter.Spreadsheet sh = this.sheet.NewSpreadsheet("Summary");

            try {
                PropertiesMap pMap = net.PropertiesMap;
                FieldsMap fMap = net.FieldsMap;

                if (net.TitleText != null)
                    for (int i = 0; i < net.TitleText.Count && i < 3; i++) {
                        if (net.TitleText[i].Length > 0) {
                            if (net.TitleText[i].Length <= 70)
                                sh.AddData(net.TitleText[i]);
                        }
                    }
                sh.AddData("\n");
                sh.AddData(Text.ResourceManager.GetString("FMT19"), inpFile);
                sh.AddData(Text.ResourceManager.GetString("FMT20"), net.Junctions.Count());

                int nReservoirs = 0;
                int nTanks = 0;
                foreach (var tk  in  net.Tanks)
                    if (tk.IsReservoir)
                        nReservoirs++;
                    else
                        nTanks++;

                int nValves = net.Valves.Count();
                int nPumps = net.Pumps.Count();
                int nPipes = net.Links.Count - nPumps - nValves;

                sh.AddData(Text.ResourceManager.GetString("FMT21a"), nReservoirs);
                sh.AddData(Text.ResourceManager.GetString("FMT21b"), nTanks);
                sh.AddData(Text.ResourceManager.GetString("FMT22"), nPipes);
                sh.AddData(Text.ResourceManager.GetString("FMT23"), nPumps);
                sh.AddData(Text.ResourceManager.GetString("FMT24"), nValves);
                sh.AddData(Text.ResourceManager.GetString("FMT25"), pMap.Formflag.ParseStr());

                sh.AddData(Text.ResourceManager.GetString("FMT26"), pMap.Hstep.GetClockTime());
                sh.AddData(Text.ResourceManager.GetString("FMT27"), pMap.Hacc);
                sh.AddData(Text.ResourceManager.GetString("FMT27a"), pMap.CheckFreq);
                sh.AddData(Text.ResourceManager.GetString("FMT27b"), pMap.MaxCheck);
                sh.AddData(Text.ResourceManager.GetString("FMT27c"), pMap.DampLimit);
                sh.AddData(Text.ResourceManager.GetString("FMT28"), pMap.MaxIter);

                if (pMap.Qualflag == PropertiesMap.QualType.NONE || pMap.Duration == 0.0)
                    sh.AddData(Text.ResourceManager.GetString("FMT29"), "None");
                else if (pMap.Qualflag == PropertiesMap.QualType.CHEM)
                    sh.AddData(Text.ResourceManager.GetString("FMT30"), pMap.ChemName);
                else if (pMap.Qualflag == PropertiesMap.QualType.TRACE)
                    sh.AddData(
                        Text.ResourceManager.GetString("FMT31"),
                        "Trace From Node",
                        net.GetNode(pMap.TraceNode).Id);
                else if (pMap.Qualflag == PropertiesMap.QualType.AGE)
                    sh.AddData(Text.ResourceManager.GetString("FMT32"), "Age");

                if (pMap.Qualflag != PropertiesMap.QualType.NONE && pMap.Duration > 0) {
                    sh.AddData(Text.ResourceManager.GetString("FMT33"), "Time Step", pMap.Qstep.GetClockTime());
                    sh.AddData(
                        Text.ResourceManager.GetString("FMT34"),
                        "Tolerance",
                        fMap.RevertUnit(FieldsMap.FieldType.QUALITY, pMap.Ctol),
                        fMap.GetField(FieldsMap.FieldType.QUALITY).Units);
                }

                sh.AddData(Text.ResourceManager.GetString("FMT36"), pMap.SpGrav);
                sh.AddData(Text.ResourceManager.GetString("FMT37a"), pMap.Viscos / Constants.VISCOS);
                sh.AddData(Text.ResourceManager.GetString("FMT37b"), pMap.Diffus / Constants.DIFFUS);
                sh.AddData(Text.ResourceManager.GetString("FMT38"), pMap.Dmult);
                sh.AddData(
                    Text.ResourceManager.GetString("FMT39"),
                    fMap.RevertUnit(FieldsMap.FieldType.TIME, pMap.Duration),
                    fMap.GetField(FieldsMap.FieldType.TIME).Units);

                if (msxFile != null && msx != null) {
                    sh.AddData("");
                    sh.AddData("MSX data file", msxFile);
                    sh.AddData("Species");
                    Species[] spe = msx.Network.Species;
                    for (int i = 1; i < msx.Network.Species.Length; i++)
                        sh.AddData(spe[i].Id, spe[i].Units);
                }
            }
            catch (IOException) {}
            catch (ENException e) {
                Debug.Print(e.ToString());
            }
        }
    }

}
