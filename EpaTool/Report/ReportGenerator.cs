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
using Epanet.Properties;
using org.addition.epanet.hydraulic.io;
using org.addition.epanet.msx;
using org.addition.epanet.msx.Structures;
using org.addition.epanet.network;
using org.addition.epanet.quality;
using org.addition.epanet.util;
using Network = org.addition.epanet.network.Network;

namespace EpaTool.Report {

    /// <summary>
    ///     This class handles the XLSX generation from the binary files created by Epanet and MSX simulations, 
    ///     the reported fields are configured via the ReportOptions class.
    /// </summary>
    public class ReportGenerator {

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
                new HydVariable(Type.Head,"Node head", true), // HydVariable.HYDR_VARIABLE_HEAD
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

        private readonly XLSXWriter _sheet;
        private readonly string _xlsxFile;

        public ReportGenerator(string xlsxFile) {
            this._xlsxFile = xlsxFile;
            this._sheet = new XLSXWriter();
        }

        /// <summary>Set excel cells transposition mode.</summary>
        public bool TransposedMode {
            set { this._sheet.TransposedMode = value; }
            get { return this._sheet.TransposedMode; }
        }

        /// <summary>Get current report time progress.</summary>
        public long Rtime { get; private set; }

        /// <summary>Generate hydraulic report.</summary>
        /// <param name="hydFile">Abstract representation of the hydraulic simulation output file.</param>
        /// <param name="net">Hydraulic network.</param>
        /// <param name="values">Variables report flag.</param>
        /// <throws>IOException</throws>
        /// <throws>org.addition.epanet.util.ENException</throws>
        public void CreateHydReport(string hydFile, Network net, bool[] values) {
            this.Rtime = 0;
            HydraulicReader dseek = new HydraulicReader(hydFile);
            var nodes = net.Nodes;
            var links = net.Links;

            var nodesHead = new object[dseek.Nodes + 1];
            nodesHead[0] = this._sheet.TransposedMode ? "Node/Time" : "Time/Node";

            for(int i = 0; i < nodes.Count; i++)
                nodesHead[i + 1] = nodes[i].Id;

            var linksHead = new object[dseek.Links + 1];
            linksHead[0] = this._sheet.TransposedMode ? "Link/Time" : "Time/Link";
          
            for(int i = 0; i < links.Count; i++)
                linksHead[i + 1] = links[i].Id;

            XLSXWriter.Spreadsheet[] resultSheets = new XLSXWriter.Spreadsheet[HydVariable.Values.Length];

            for (int i = 0; i < resultSheets.Length; i++) {
                if (values != null && !values[i]) continue;
                resultSheets[i] = this._sheet.NewSpreadsheet(HydVariable.Values[i].Name);
                resultSheets[i].AddData(HydVariable.Values[i].IsNode ? nodesHead : linksHead);
            }

            var nodeRow = new object[dseek.Nodes + 1];
            var linkRow = new object[dseek.Links + 1];

            for(long time = net.PropertiesMap.Rstart; time <= net.PropertiesMap.Duration; time += net.PropertiesMap.Rstep) {
                var step = dseek.getStep(time);
                if (step != null) {
                    nodeRow[0] = time.GetClockTime();
                    linkRow[0] = time.GetClockTime();

                    // NODES HEADS
                    if (resultSheets[(int)HydVariable.Type.Head] != null) {
                        for (int i = 0; i < nodes.Count; i++) {
                            nodeRow[i + 1] = step.getNodeHead(i, nodes[i], net.FieldsMap);
                        }

                        resultSheets[(int)HydVariable.Type.Head].AddData(nodeRow);
                    }

                    // NODES DEMANDS
                    if (resultSheets[(int)HydVariable.Type.Demands] != null) {
                        for(int i = 0; i < nodes.Count; i++) {
                            nodeRow[i + 1] = step.getNodeDemand(i, nodes[i], net.FieldsMap);
                        }
                        resultSheets[(int)HydVariable.Type.Demands].AddData(nodeRow);
                    }

                    // NODES PRESSURE
                    if (resultSheets[(int)HydVariable.Type.Pressure] != null) {
                        for(int i = 0; i < nodes.Count; i++) {
                            nodeRow[i + 1] = step.getNodePressure(i, nodes[i], net.FieldsMap);
                        }
                        resultSheets[(int)HydVariable.Type.Pressure].AddData(nodeRow);
                    }

                    // LINK FLOW
                    if (resultSheets[(int)HydVariable.Type.Flows] != null) {
                        for(int i = 0; i < links.Count; i++) {
                            linkRow[i + 1] = step.getLinkFlow(i, links[i], net.FieldsMap);
                        }
                        resultSheets[(int)HydVariable.Type.Flows].AddData(linkRow);
                    }

                    // LINK VELOCITY
                    if (resultSheets[(int)HydVariable.Type.Velocity] != null) {
                        for(int i = 0; i < links.Count; i++) {
                            linkRow[i + 1] = step.getLinkVelocity(i, links[i], net.FieldsMap);
                        }
                        resultSheets[(int)HydVariable.Type.Velocity].AddData(linkRow);
                    }

                    // LINK HEADLOSS
                    if (resultSheets[(int)HydVariable.Type.Headloss] != null) {
                        for(int i = 0; i < links.Count; i++) {
                            linkRow[i + 1] = step.getLinkHeadLoss(i, links[i], net.FieldsMap);
                        }
                        resultSheets[(int)HydVariable.Type.Headloss].AddData(linkRow);
                    }

                    // LINK FRICTION
                    if (resultSheets[(int)HydVariable.Type.Friction] != null) {
                        for(int i = 0; i < links.Count; i++) {
                            linkRow[i + 1] = step.getLinkFriction(i, links[i], net.FieldsMap);
                        }
                        resultSheets[(int)HydVariable.Type.Friction].AddData(linkRow);
                    }
                }
                this.Rtime = time;
            }

            dseek.Close();
        }

        /// <summary>Generate quality report.</summary>
        /// <param name="qualFile">Abstract representation of the quality simulation output file.</param>
        /// <param name="net">Hydraulic network.</param>
        /// <param name="nodes">Show nodes quality flag.</param>
        /// <param name="links">Show links quality flag.</param>
        /// <throws>IOException</throws>
        /// <throws>ENException</throws>
        public void CreateQualReport(string qualFile, Network net, bool nodes, bool links) {
            this.Rtime = 0;

            using (QualityReader dseek = new QualityReader(qualFile, net.FieldsMap)) {

                var nodesHead = new object[dseek.Nodes + 1];
                nodesHead[0] = this._sheet.TransposedMode ? "Node/Time" : "Time/Node";

                var netNodes = net.Nodes;
                var netLinks = net.Links;


                for(int i = 0; i < netNodes.Count; i++)
                    nodesHead[i + 1] = netNodes[i].Id;

                var linksHead = new object[dseek.Links + 1];
                linksHead[0] = this._sheet.TransposedMode ? "Link/Time" : "Time/Link";

                for(int i = 0; i < netLinks.Count; i++)
                    linksHead[i + 1] = netLinks[i].Id;

                XLSXWriter.Spreadsheet[] resultSheets = new XLSXWriter.Spreadsheet[QualVariable.Values.Length];

                foreach (var v in QualVariable.Values) {

                    if ((!v.IsNode || !nodes) && (v.IsNode || !links))
                        continue;

                    resultSheets[v.ID] = this._sheet.NewSpreadsheet(v.Name);
                    resultSheets[v.ID].AddData(v.IsNode ? nodesHead : linksHead);
                }

                var nodeRow = new object[dseek.Nodes + 1];
                var linkRow = new object[dseek.Links + 1];

                
                using (var qIt = dseek.GetEnumerator())
                for(long time = net.PropertiesMap.Rstart;
                    time <= net.PropertiesMap.Duration; 
                    time += net.PropertiesMap.Rstep)
                {
                    if (!qIt.MoveNext()) break;

                    var step = qIt.Current;
                    if (step == null) continue;

                    nodeRow[0] = time.GetClockTime();
                    linkRow[0] = time.GetClockTime();

                    if (resultSheets[(int)QualVariable.Type.Nodes] != null) {
                        for (int i = 0; i < dseek.Nodes; i++) {
                            nodeRow[i + 1] = (double)step.GetNodeQuality(i);
                        }
                        resultSheets[(int)QualVariable.Type.Nodes].AddData(nodeRow);
                    }

                    if (resultSheets[(int)QualVariable.Type.Links] != null) {
                        for (int i = 0; i < dseek.Links; i++) {
                            linkRow[i + 1] = (double)step.GetLinkQuality(i);
                        }
                        resultSheets[(int)QualVariable.Type.Links].AddData(linkRow);
                    }
                    this.Rtime = time;
                }
            }
        }

        /// <summary>Generate multi-species quality report.</summary>
        /// <param name="msxBin">Abstract representation of the MSX simulation output file.</param>
        /// <param name="net">Hydraulic network.</param>
        /// <param name="netMSX">MSX network.</param>
        /// <param name="tk2">Hydraulic network - MSX bridge.</param>
        /// <param name="values">Species report flag.</param>
        /// <throws>IOException</throws>
        /// <throws>ENException</throws>
        public void CreateMSXReport(string msxBin, Network net, EpanetMSX netMSX, ENToolkit2 tk2, bool[] values) {
            this.Rtime = 0;

            var nodes = netMSX.Network.Node;
            var links = netMSX.Network.Link;

            string[] nSpecies = netMSX.GetSpeciesNames();

            MsxReader reader = new MsxReader(
                nodes.Length - 1,
                links.Length - 1,
                nSpecies.Length,
                netMSX.ResultsOffset);

            int totalSpecies;

            if (values != null) {
                totalSpecies = 0;
                foreach (bool b  in  values) { if (b) totalSpecies++; }
            }
            else
                totalSpecies = nSpecies.Length;

            reader.Open(msxBin);

            var nodesHead = new object[nSpecies.Length + 1];
            nodesHead[0] = this._sheet.TransposedMode ? "Node/Time" : "Time/Node";

            var linksHead = new object[nSpecies.Length + 1];
            linksHead[0] = this._sheet.TransposedMode ? "Link/Time" : "Time/Link";

            int count = 1;
            for (int i = 0; i < nSpecies.Length; i++) {
                if (values == null || values[i]) {
                    nodesHead[count] = nSpecies[i];
                    linksHead[count++] = nSpecies[i];
                }
            }

            var nodeRow = new object[totalSpecies + 1];

            for (int i = 1; i < nodes.Length; i++) {
                if (!nodes[i].Rpt) continue;

                XLSXWriter.Spreadsheet spr =
                    this._sheet.NewSpreadsheet("Node&lt;&lt;" + tk2.ENgetnodeid(i) + "&gt;&gt;");
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

            var linkRow = new object[totalSpecies + 1];

            for (int i = 1; i < links.Length; i++) {
                if (!links[i].getRpt()) continue;
                XLSXWriter.Spreadsheet spr =
                    this._sheet.NewSpreadsheet("Link&lt;&lt;" + tk2.ENgetlinkid(i) + "&gt;&gt;");
                    
                spr.AddData(linksHead);

                for (long time = net.PropertiesMap.Rstart, period = 0;
                     time <= net.PropertiesMap.Duration;
                     time += net.PropertiesMap.Rstep, period++) 
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
        /// <throws>IOException</throws>
        public void WriteWorksheet() {
            this._sheet.Save(this._xlsxFile);
        }

        /// <summary>Write simulation summary to one worksheet.</summary>
        /// <param name="inpFile">Hydraulic network file.</param>
        /// <param name="net">Hydraulic network.</param>
        /// <param name="msxFile">MSX file.</param>
        /// <param name="msx">MSX solver.</param>
        /// <throws>IOException</throws>
        public void WriteSummary(string inpFile, Network net, string msxFile, EpanetMSX msx) {
            XLSXWriter.Spreadsheet sh = this._sheet.NewSpreadsheet("Summary");

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
                foreach (var tk  in  net.Tanks) {
                    if (tk.IsReservoir)
                        nReservoirs++;
                    else
                        nTanks++;
                }

                int nValves = net.Valves.Count();
                int nPumps = net.Pumps.Count();
                int nPipes = net.Links.Count - nPumps - nValves;

                sh.AddData(Text.FMT21a, nReservoirs);
                sh.AddData(Text.FMT21b, nTanks);
                sh.AddData(Text.FMT22, nPipes);
                sh.AddData(Text.FMT23, nPumps);
                sh.AddData(Text.FMT24, nValves);
                sh.AddData(Text.FMT25, pMap.Formflag.ParseStr());

                sh.AddData(Text.FMT26, pMap.Hstep.GetClockTime());
                sh.AddData(Text.FMT27, pMap.Hacc);
                sh.AddData(Text.FMT27a, pMap.CheckFreq);
                sh.AddData(Text.FMT27b, pMap.MaxCheck);
                sh.AddData(Text.FMT27c, pMap.DampLimit);
                sh.AddData(Text.FMT28, pMap.MaxIter);

                switch (pMap.Duration == 0 ? PropertiesMap.QualType.NONE : pMap.Qualflag) {
                case PropertiesMap.QualType.NONE:
                        sh.AddData(Text.FMT29, "None");
                        break;
                case PropertiesMap.QualType.CHEM:
                        sh.AddData(Text.FMT30, pMap.ChemName);
                        break;
                case PropertiesMap.QualType.TRACE:
                        sh.AddData(
                            Text.FMT31,
                            "Trace From Node",
                            net.GetNode(pMap.TraceNode).Id);
                        break;
                case PropertiesMap.QualType.AGE:
                        sh.AddData(Text.FMT32, "Age");
                        break;
                }

                if(pMap.Qualflag != PropertiesMap.QualType.NONE && pMap.Duration > 0) {
                    sh.AddData(Text.FMT33, "Time Step", pMap.Qstep.GetClockTime());
                    sh.AddData(Text.FMT34, "Tolerance", fMap.RevertUnit(FieldsMap.FieldType.QUALITY, pMap.Ctol), fMap.GetField(FieldsMap.FieldType.QUALITY).Units);
                }

                sh.AddData(Text.FMT36, pMap.SpGrav);
                sh.AddData(Text.FMT37a, pMap.Viscos / org.addition.epanet.Constants.VISCOS);
                sh.AddData(Text.FMT37b, pMap.Diffus / org.addition.epanet.Constants.DIFFUS);
                sh.AddData(Text.FMT38, pMap.Dmult);
                sh.AddData(Text.FMT39, fMap.RevertUnit(FieldsMap.FieldType.TIME, pMap.Duration), fMap.GetField(FieldsMap.FieldType.TIME).Units);

                if (msxFile != null && msx != null) {
                    sh.AddData("");
                    sh.AddData("MSX data file", msxFile);
                    sh.AddData("Species");
                    Species[] spe = msx.Network.Species;
                    for (int i = 1; i < msx.Network.Species.Length; i++) {
                        sh.AddData(spe[i].getId(), spe[i].getUnits());
                    }
                }
            }
            catch (IOException) {}
            catch (ENException e) {
                Debug.Print(e.ToString());
            }
        }
    }

}