using System.Diagnostics;
using System.IO;
using Epanet.Properties;
using org.addition.epanet.hydraulic.io;
using org.addition.epanet.msx;
using org.addition.epanet.msx.Structures;
using org.addition.epanet.network;
using org.addition.epanet.quality;
using org.addition.epanet.util;
using Network = org.addition.epanet.network.Network;
using Utilities = org.addition.epanet.util.Utilities;

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
            var nodes = net.getNodes();
            var links = net.getLinks();

            var nodesHead = new object[dseek.getNodes() + 1];
            nodesHead[0] = this._sheet.TransposedMode ? "Node/Time" : "Time/Node";
        
            for(int i = 0; i < nodes.Length; i++)
                nodesHead[i + 1] = nodes[i].getId();

            var linksHead = new object[dseek.getLinks() + 1];
            linksHead[0] = this._sheet.TransposedMode ? "Link/Time" : "Time/Link";
          
            for(int i = 0; i < links.Length; i++)
                linksHead[i + 1] = links[i].getId();

            XLSXWriter.Spreadsheet[] resultSheets = new XLSXWriter.Spreadsheet[HydVariable.Values.Length];

            for (int i = 0; i < resultSheets.Length; i++) {
                if (values != null && !values[i]) continue;
                resultSheets[i] = this._sheet.NewSpreadsheet(HydVariable.Values[i].Name);
                resultSheets[i].AddData(HydVariable.Values[i].IsNode ? nodesHead : linksHead);
            }

            var nodeRow = new object[dseek.getNodes() + 1];
            var linkRow = new object[dseek.getLinks() + 1];

            for(long time = net.getPropertiesMap().getRstart(); time <= net.getPropertiesMap().getDuration(); time += net.getPropertiesMap().getRstep()) {
                var step = dseek.getStep(time);
                if (step != null) {
                    nodeRow[0] = time.getClockTime();
                    linkRow[0] = time.getClockTime();

                    // NODES HEADS
                    if (resultSheets[(int)HydVariable.Type.Head] != null) {
                        for (int i = 0; i < nodes.Length; i++) {
                            nodeRow[i + 1] = step.getNodeHead(i, nodes[i], net.getFieldsMap());
                        }

                        resultSheets[(int)HydVariable.Type.Head].AddData(nodeRow);
                    }

                    // NODES DEMANDS
                    if (resultSheets[(int)HydVariable.Type.Demands] != null) {
                        for(int i = 0; i < nodes.Length; i++) {
                            nodeRow[i + 1] = step.getNodeDemand(i, nodes[i], net.getFieldsMap());
                        }
                        resultSheets[(int)HydVariable.Type.Demands].AddData(nodeRow);
                    }

                    // NODES PRESSURE
                    if (resultSheets[(int)HydVariable.Type.Pressure] != null) {
                        for(int i = 0; i < nodes.Length; i++) {
                            nodeRow[i + 1] = step.getNodePressure(i, nodes[i], net.getFieldsMap());
                        }
                        resultSheets[(int)HydVariable.Type.Pressure].AddData(nodeRow);
                    }

                    // LINK FLOW
                    if (resultSheets[(int)HydVariable.Type.Flows] != null) {
                        for(int i = 0; i < links.Length; i++) {
                            linkRow[i + 1] = step.getLinkFlow(i, links[i], net.getFieldsMap());
                        }
                        resultSheets[(int)HydVariable.Type.Flows].AddData(linkRow);
                    }

                    // LINK VELOCITY
                    if (resultSheets[(int)HydVariable.Type.Velocity] != null) {
                        for(int i = 0; i < links.Length; i++) {
                            linkRow[i + 1] = step.getLinkVelocity(i, links[i], net.getFieldsMap());
                        }
                        resultSheets[(int)HydVariable.Type.Velocity].AddData(linkRow);
                    }

                    // LINK HEADLOSS
                    if (resultSheets[(int)HydVariable.Type.Headloss] != null) {
                        for(int i = 0; i < links.Length; i++) {
                            linkRow[i + 1] = step.getLinkHeadLoss(i, links[i], net.getFieldsMap());
                        }
                        resultSheets[(int)HydVariable.Type.Headloss].AddData(linkRow);
                    }

                    // LINK FRICTION
                    if (resultSheets[(int)HydVariable.Type.Friction] != null) {
                        for(int i = 0; i < links.Length; i++) {
                            linkRow[i + 1] = step.getLinkFriction(i, links[i], net.getFieldsMap());
                        }
                        resultSheets[(int)HydVariable.Type.Friction].AddData(linkRow);
                    }
                }
                this.Rtime = time;
            }

            dseek.close();
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

            using (QualityReader dseek = new QualityReader(qualFile, net.getFieldsMap())) {

                var nodesHead = new object[dseek.getNodes() + 1];
                nodesHead[0] = this._sheet.TransposedMode ? "Node/Time" : "Time/Node";

                var netNodes = net.getNodes();
                var netLinks = net.getLinks();


                for(int i = 0; i < netNodes.Length; i++)
                    nodesHead[i + 1] = netNodes[i].getId();

                var linksHead = new object[dseek.getLinks() + 1];
                linksHead[0] = this._sheet.TransposedMode ? "Link/Time" : "Time/Link";

                for(int i = 0; i < netLinks.length(); i++)
                    linksHead[i + 1] = netLinks[i].getId();

                XLSXWriter.Spreadsheet[] resultSheets = new XLSXWriter.Spreadsheet[QualVariable.Values.Length];

                foreach (var v in QualVariable.Values) {

                    if ((!v.IsNode || !nodes) && (v.IsNode || !links))
                        continue;

                    resultSheets[v.ID] = this._sheet.NewSpreadsheet(v.Name);
                    resultSheets[v.ID].AddData(v.IsNode ? nodesHead : linksHead);
                }

                var nodeRow = new object[dseek.getNodes() + 1];
                var linkRow = new object[dseek.getLinks() + 1];

                
                using (var qIt = dseek.GetEnumerator())
                for(long time = net.getPropertiesMap().getRstart();
                    time <= net.getPropertiesMap().getDuration(); 
                    time += net.getPropertiesMap().getRstep())
                {
                    if (!qIt.MoveNext()) break;

                    var step = qIt.Current;
                    if (step == null) continue;

                    nodeRow[0] = Utilities.getClockTime(time);
                    linkRow[0] = Utilities.getClockTime(time);

                    if (resultSheets[(int)QualVariable.Type.Nodes] != null) {
                        for (int i = 0; i < dseek.getNodes(); i++) {
                            nodeRow[i + 1] = (double)step.getNodeQuality(i);
                        }
                        resultSheets[(int)QualVariable.Type.Nodes].AddData(nodeRow);
                    }

                    if (resultSheets[(int)QualVariable.Type.Links] != null) {
                        for (int i = 0; i < dseek.getLinks(); i++) {
                            linkRow[i + 1] = (double)step.getLinkQuality(i);
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

            var nodes = netMSX.getNetwork().getNodes();
            var links = netMSX.getNetwork().getLinks();

            string[] nSpecies = netMSX.getSpeciesNames();

            MsxReader reader = new MsxReader(
                nodes.Length - 1,
                links.Length - 1,
                nSpecies.Length,
                netMSX.getResultsOffset());

            int totalSpecies;

            if (values != null) {
                totalSpecies = 0;
                foreach (bool b  in  values) { if (b) totalSpecies++; }
            }
            else
                totalSpecies = nSpecies.Length;

            reader.open(msxBin);

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
                if (!nodes[i].getRpt()) continue;

                XLSXWriter.Spreadsheet spr =
                    this._sheet.NewSpreadsheet("Node&lt;&lt;" + tk2.ENgetnodeid(i) + "&gt;&gt;");
                spr.AddData(nodesHead);

                for (long time = net.getPropertiesMap().getRstart(), period = 0;
                     time <= net.getPropertiesMap().getDuration();
                     time += net.getPropertiesMap().getRstep(), period++) {
                    
                    nodeRow[0] = Utilities.getClockTime(time);

                    for (int j = 0, ji = 0; j < nSpecies.Length; j++) {
                        if (values == null || values[j])
                            nodeRow[ji++ + 1] = reader.getNodeQual((int)period, i, j + 1);
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

                for (long time = net.getPropertiesMap().getRstart(), period = 0;
                     time <= net.getPropertiesMap().getDuration();
                     time += net.getPropertiesMap().getRstep(), period++) 
                {
                    linkRow[0] = Utilities.getClockTime(time);

                    for (int j = 0, ji = 0; j < nSpecies.Length; j++) {
                        if (values == null || values[j])
                            linkRow[ji++ + 1] = reader.getLinkQual((int)period, i, j + 1);
                    }

                    spr.AddData(linkRow);
                }
            }

            reader.close();
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
                PropertiesMap pMap = net.getPropertiesMap();
                FieldsMap fMap = net.getFieldsMap();

                if (net.getTitleText() != null)
                    for (int i = 0; i < net.getTitleText().Count && i < 3; i++) {
                        if (!string.IsNullOrEmpty(net.getTitleText()[i])) {
                            if (net.getTitleText()[i].Length <= 70)
                                sh.AddData(net.getTitleText()[i]);
                            else {
                                sh.AddData(net.getTitleText()[i].Substring(0, 70));
                            }
                        }
                    }
                sh.AddData("\n");
                sh.AddData(Text.FMT19, inpFile);
                sh.AddData(Text.FMT20, net.getJunctions().Length);

                int nReservoirs = 0;
                int nTanks = 0;
                foreach (var tk  in  net.getTanks()) {
                    if (tk.IsReservoir)
                        nReservoirs++;
                    else
                        nTanks++;
                }

                int nValves = net.getValves().Length;
                int nPumps = net.getPumps().Length;
                int nPipes = net.getLinks().Length - nPumps - nValves;

                sh.AddData(Text.FMT21a, nReservoirs);
                sh.AddData(Text.FMT21b, nTanks);
                sh.AddData(Text.FMT22, nPipes);
                sh.AddData(Text.FMT23, nPumps);
                sh.AddData(Text.FMT24, nValves);
                sh.AddData(Text.FMT25, pMap.getFormflag().ParseStr());

                sh.AddData(Text.FMT26, Utilities.getClockTime(pMap.getHstep()));
                sh.AddData(Text.FMT27, pMap.getHacc());
                sh.AddData(Text.FMT27a, pMap.getCheckFreq());
                sh.AddData(Text.FMT27b, pMap.getMaxCheck());
                sh.AddData(Text.FMT27c, pMap.getDampLimit());
                sh.AddData(Text.FMT28, pMap.getMaxIter());

                switch (pMap.getDuration() == 0 ? PropertiesMap.QualType.NONE : pMap.getQualflag()) {
                case PropertiesMap.QualType.NONE:
                        sh.AddData(Text.FMT29, "None");
                        break;
                case PropertiesMap.QualType.CHEM:
                        sh.AddData(Text.FMT30, pMap.getChemName());
                        break;
                case PropertiesMap.QualType.TRACE:
                        sh.AddData(
                            Text.FMT31,
                            "Trace From Node",
                            net.getNode(pMap.getTraceNode()).getId());
                        break;
                case PropertiesMap.QualType.AGE:
                        sh.AddData(Text.FMT32, "Age");
                        break;
                }

                if(pMap.getQualflag() != PropertiesMap.QualType.NONE && pMap.getDuration() > 0) {
                    sh.AddData(Text.FMT33, "Time Step", Utilities.getClockTime(pMap.getQstep()));
                    sh.AddData(Text.FMT34, "Tolerance", fMap.revertUnit(FieldsMap.Type.QUALITY, pMap.getCtol()), fMap.getField(FieldsMap.Type.QUALITY).getUnits());
                }

                sh.AddData(Text.FMT36, pMap.getSpGrav());
                sh.AddData(Text.FMT37a, pMap.getViscos() / org.addition.epanet.Constants.VISCOS);
                sh.AddData(Text.FMT37b, pMap.getDiffus() / org.addition.epanet.Constants.DIFFUS);
                sh.AddData(Text.FMT38, pMap.getDmult());
                sh.AddData(Text.FMT39, fMap.revertUnit(FieldsMap.Type.TIME, pMap.getDuration()), fMap.getField(FieldsMap.Type.TIME).getUnits());

                if (msxFile != null && msx != null) {
                    sh.AddData("");
                    sh.AddData("MSX data file", msxFile);
                    sh.AddData("Species");
                    Species[] spe = msx.getNetwork().Species;
                    for (int i = 1; i < msx.getNetwork().Species.Length; i++) {
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