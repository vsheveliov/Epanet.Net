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

using System.Diagnostics;
using System.IO;
using System.Linq;

using Epanet.Enums;
using Epanet.Hydraulic.IO;
using Epanet.MSX;
using Epanet.MSX.Structures;
using Epanet.Network;
using Epanet.Properties;
using Epanet.Quality;
using Epanet.Util;

namespace Epanet.Report {

    ///<summary>
    ///  This class handles the XLSX generation from the binary files created by Epanet and MSX simulations, the reported
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

            public readonly bool IsNode;
            public readonly string Name;

            private HydVariable(string text, bool node) {
                this.Name = text;
                this.IsNode = node;
            }

            public override string ToString() { return this.Name; }
        }

        /// <summary>Quality report fields.</summary>
        public struct QualVariable {
            public enum Type {
                Nodes = 0,
                Links = 1,
                // Rate = 2
            }

            public static readonly QualVariable[] Values = {
                new QualVariable("Node quality", true), // QualVariable.QUAL_VARIABLE_NODES
                new QualVariable("Link quality", false) // QualVariable.QUAL_VARIABLE_LINKS
                // new QualVariable(Type.Rate, "Link reaction rate", false) // QualVariable.QUAL_VARIABLE_RATE
            };

            public readonly string Name;
            public readonly bool IsNode;

            private QualVariable(string text, bool node) {
                this.Name = text;
                this.IsNode = node;
            }

            public override string ToString() { return this.Name; }
        }

        private readonly XLSXWriter sheet;
        private readonly string xlsxFile;

        public ReportGenerator(string xlsxFile) {
            this.xlsxFile = xlsxFile;
            this.sheet = new XLSXWriter();
        }

        ///<summary>Excel cells transposition mode.</summary>
        public bool TransposedMode {
            set { this.sheet.TransposedMode = value; }
            get { return this.sheet.TransposedMode; }
        }

        ///<summary>Current report time progress.</summary>
        public long Rtime { get; private set; }

        /// <summary>Generate hydraulic report.</summary>
        ///  <param name="hydBinFile">Name of the hydraulic simulation output file.</param>
        /// <param name="net">Hydraulic network.</param>
        /// <param name="values">Variables report flag.</param>
        public void CreateHydReport(string hydBinFile, Network.Network net, bool[] values) {
            this.Rtime = 0;
            HydraulicReader dseek = new HydraulicReader(new BinaryReader(File.OpenRead(hydBinFile)));
            int reportCount = (int)((net.PropertiesMap.Duration - net.PropertiesMap.RStart) / net.PropertiesMap.RStep) + 1;
            var nodes = net.Nodes;
            var links = net.Links;

            object[] nodesHead = new object[dseek.Nodes + 1];
            nodesHead[0] = this.sheet.TransposedMode ? "Node/Time" : "Time/Node";
            for (int i = 0; i < nodes.Count; i++)
                nodesHead[i + 1] = nodes[i].Id;

            var linksHead = new object[dseek.Links + 1];
            linksHead[0] = this.sheet.TransposedMode ? "Link/Time" : "Time/Link";
            for (int i = 0; i < links.Count; i++)
                linksHead[i + 1] = links[i].Id;

            XLSXWriter.Spreadsheet[] resultSheets = new XLSXWriter.Spreadsheet[HydVariable.Values.Length];
            // Array.Clear(resultSheets, 0, resultSheets.Length);

            for (int i = 0; i < resultSheets.Length; i++) {
                if (values != null && !values[i]) continue;
                resultSheets[i] = this.sheet.NewSpreadsheet(HydVariable.Values[i].Name);
                resultSheets[i].AddData(HydVariable.Values[i].IsNode ? nodesHead : linksHead);
            }

            var nodeRow = new object[dseek.Nodes + 1];
            var linkRow = new object[dseek.Links + 1];

            for (long time = net.PropertiesMap.RStart;
                 time <= net.PropertiesMap.Duration;
                 time += net.PropertiesMap.RStep) {

                var step = dseek.GetStep(time);

                if (step == null) {
                    this.Rtime = time;
                    continue;
                }

                nodeRow[0] = time.GetClockTime();
                linkRow[0] = time.GetClockTime();

                // NODES HEADS
                if (resultSheets[(int)HydVariable.Type.Head] != null) {
                    for (int i = 0; i < nodes.Count; i++)
                        nodeRow[i + 1] = step.GetNodeHead(i, nodes[i], net.FieldsMap);

                    resultSheets[(int)HydVariable.Type.Head].AddData(nodeRow);
                }

                // NODES DEMANDS
                if (resultSheets[(int)HydVariable.Type.Demands] != null) {
                    for (int i = 0; i < nodes.Count; i++) {
                        nodeRow[i + 1] = step.GetNodeDemand(i, nodes[i], net.FieldsMap);
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
                        linkRow[i + 1] = step.GetLinkFlow(i, links[i], net.FieldsMap);
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

                this.Rtime = time;
            }

            dseek.Close();
        }

        /// <summary>Generate quality report.</summary>
        ///  <param name="qualFile">Name of the quality simulation output file.</param>
        /// <param name="net">Hydraulic network.</param>
        /// <param name="nodes">Show nodes quality flag.</param>
        /// <param name="links">Show links quality flag.</param>
        public void CreateQualReport(string qualFile, Network.Network net, bool nodes, bool links) {
            this.Rtime = 0;

            int reportCount = (int)((net.PropertiesMap.Duration - net.PropertiesMap.RStart) / net.PropertiesMap.RStep) + 1;

            using (QualityReader dseek = new QualityReader(qualFile, net.FieldsMap)) {
                var netNodes = net.Nodes;
                var netLinks = net.Links;    
          
                var nodesHead = new object[dseek.Nodes + 1];
                nodesHead[0] = this.sheet.TransposedMode ? "Node/Time" : "Time/Node";
                for(int i = 0; i < netNodes.Count; i++)
                    nodesHead[i + 1] = netNodes[i].Id;

                var linksHead = new object[dseek.Links + 1];
                linksHead[0] = this.sheet.TransposedMode ? "Link/Time" : "Time/Link";
                for(int i = 0; i < netLinks.Count; i++)
                    linksHead[i + 1] = netLinks[i].Id;

                var resultSheets = new XLSXWriter.Spreadsheet[HydVariable.Values.Length];
               
                for (int i = 0; i < QualVariable.Values.Length; i++) {
                    var qvar = QualVariable.Values[i];
                    if ((!qvar.IsNode || !nodes) && (qvar.IsNode || !links))
                        continue;

                    resultSheets[i] = this.sheet.NewSpreadsheet(qvar.Name);
                    resultSheets[i].AddData(qvar.IsNode ? nodesHead : linksHead);
                }

                var nodeRow = new object[dseek.Nodes + 1];
                var linkRow = new object[dseek.Links + 1];

                using (var qIt = dseek.GetEnumerator())
                for (long time = net.PropertiesMap.RStart;
                     time <= net.PropertiesMap.Duration;
                     time += net.PropertiesMap.RStep)
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

                    this.Rtime = time;
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
            var nodes = netMSX.Network.Node;
            var links = netMSX.Network.Link;
            string[] nSpecies = netMSX.GetSpeciesNames();
            int reportCount = (int)((net.PropertiesMap.Duration - net.PropertiesMap.RStart) / net.PropertiesMap.RStep) + 1;

            var reader = new MsxReader(
                nodes.Length - 1,
                links.Length - 1,
                nSpecies.Length,
                netMSX.ResultsOffset);

            int totalSpecies = values == null ? nSpecies.Length : values.Count(b => b);

            reader.Open(msxBin);

            var nodesHead = new object[nSpecies.Length + 1];
            nodesHead[0] = this.sheet.TransposedMode ? "Node/Time" : "Time/Node";

            var linksHead = new object[nSpecies.Length + 1];
            linksHead[0] = this.sheet.TransposedMode ? "Link/Time" : "Time/Link";

            int count = 1;
            for (int i = 0; i < nSpecies.Length; i++)
                if (values == null || values[i]) {
                    nodesHead[count] = nSpecies[i];
                    linksHead[count++] = nSpecies[i];
                }

            var nodeRow = new object[totalSpecies + 1];

            for (int i = 1; i < nodes.Length; i++) {
                if (!nodes[i].Rpt) continue;

                var spr = this.sheet.NewSpreadsheet("Node&lt;&lt;" + tk2.ENgetnodeid(i) + "&gt;&gt;");
                spr.AddData(nodesHead);

                for (long time = net.PropertiesMap.RStart, period = 0;
                     time <= net.PropertiesMap.Duration;
                     time += net.PropertiesMap.RStep, period++) {

                    nodeRow[0] = time.GetClockTime();

                    for (int j = 0, ji = 0; j < nSpecies.Length; j++)
                        if (values == null || values[j])
                            nodeRow[ji++ + 1] = reader.GetNodeQual((int)period, i, j + 1);

                    spr.AddData(nodeRow);
                }
            }

            var linkRow = new object[totalSpecies + 1];

            for (int i = 1; i < links.Length; i++) {
                if (!links[i].Rpt) continue;

                var spr = this.sheet.NewSpreadsheet("Link&lt;&lt;" + tk2.ENgetlinkid(i) + "&gt;&gt;");
                spr.AddData(linksHead);

                for (long time = net.PropertiesMap.RStart, period = 0;
                     time <= net.PropertiesMap.Duration;
                     time += net.PropertiesMap.RStep, period++) 
                {
                    linkRow[0] = time.GetClockTime();

                    for (int j = 0, ji = 0; j < nSpecies.Length; j++) {
                        if (values == null || values[j])
                            linkRow[ji++ + 1] = reader.GetLinkQual((int)period, i, j + 1);
                    }

                    spr.AddData(linkRow);
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
        public void WriteSummary(string inpFile, Network.Network net, string msxFile, EpanetMSX msx) {
            var sh = this.sheet.NewSpreadsheet("Summary");

            try {
                PropertiesMap pMap = net.PropertiesMap;
                FieldsMap fMap = net.FieldsMap;

                if (net.TitleText != null)
                    for (int i = 0; i < net.TitleText.Count && i < 3; i++) {
                        if (!string.IsNullOrEmpty(net.TitleText[i])) {
                            if (net.TitleText[i].Length <= 70)
                                sh.AddData(net.TitleText[i]);
                            else {
                                sh.AddData(net.TitleText[i].Substring(0, 70));
                            }
                        }
                    }

                sh.AddData("\n");
                sh.AddData(Text.FMT19, inpFile);
                sh.AddData(Text.FMT20, net.Junctions.Count());

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

                sh.AddData(Text.FMT21a, nReservoirs);
                sh.AddData(Text.FMT21b, nTanks);
                sh.AddData(Text.FMT22, nPipes);
                sh.AddData(Text.FMT23, nPumps);
                sh.AddData(Text.FMT24, nValves);
                sh.AddData(Text.FMT25, pMap.FormFlag.ParseStr());

                sh.AddData(Text.FMT26, pMap.HStep.GetClockTime());
                sh.AddData(Text.FMT27, pMap.HAcc);
                sh.AddData(Text.FMT27a, pMap.CheckFreq);
                sh.AddData(Text.FMT27b, pMap.MaxCheck);
                sh.AddData(Text.FMT27c, pMap.DampLimit);
                sh.AddData(Text.FMT28, pMap.MaxIter);

                switch (pMap.Duration == 0 ? QualType.NONE : pMap.QualFlag) {
                case QualType.NONE:
                    sh.AddData(Text.FMT29, "None");
                    break;
                case QualType.CHEM:
                    sh.AddData(Text.FMT30, pMap.ChemName);
                    break;
                case QualType.TRACE:
                    sh.AddData(Text.FMT31, "Trace From Node", net.GetNode(pMap.TraceNode).Id);
                    break;
                case QualType.AGE:
                    sh.AddData(Text.FMT32, "Age");
                    break;
                }

                if (pMap.QualFlag != QualType.NONE && pMap.Duration > 0) {
                    sh.AddData(Text.FMT33, "Time Step", pMap.QStep.GetClockTime());
                    sh.AddData(
                        Text.FMT34,
                        "Tolerance",
                        fMap.RevertUnit(FieldType.QUALITY, pMap.Ctol),
                        fMap.GetField(FieldType.QUALITY).Units);
                }

                sh.AddData(Text.FMT36, pMap.SpGrav);
                sh.AddData(Text.FMT37a, pMap.Viscos / Constants.VISCOS);
                sh.AddData(Text.FMT37b, pMap.Diffus / Constants.DIFFUS);
                sh.AddData(Text.FMT38, pMap.DMult);
                sh.AddData(
                    Text.FMT39,
                    fMap.RevertUnit(FieldType.TIME, pMap.Duration),
                    fMap.GetField(FieldType.TIME).Units);

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
