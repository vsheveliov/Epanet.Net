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
using org.addition.epanet.hydraulic.io;
using org.addition.epanet.msx;
using org.addition.epanet.msx.Structures;
using org.addition.epanet.network;
using org.addition.epanet.quality;
using org.addition.epanet.util;
using Link = org.addition.epanet.network.structures.Link;
using Network = org.addition.epanet.network.Network;
using Node = org.addition.epanet.network.structures.Node;
using Utilities = org.addition.epanet.util.Utilities;

namespace org.addition.epanet.ui {

/**
 * This class handles the XLSX generation from the binary files created by Epanet and MSX simulations, the reported
 * fields are configured via the ReportOptions class.
 */
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
        sheet = new XLSXWriter();

    }

    /**
     * Set excel cells transposition mode.
     *
     * @param value
     */
    public void setTransposedMode(bool value) {
        sheet.TransposedMode = value;
    }

    /**
     * Get current report time progress.
     *
     * @return
     */
    public long getRtime() {
        return Rtime;
    }

    /**
     * Generate hydraulic report.
     *
     * @param hydBinFile Abstract representation of the hydraulic simulation output file.
     * @param net        Hydraulic network.
     * @param values     Variables report flag.
     * @throws IOException
     * @throws org.addition.epanet.util.ENException
     *
     */
    public void createHydReport(string hydBinFile, Network net, bool[] values) {
        Rtime = 0;
        HydraulicReader dseek = new HydraulicReader(new BinaryReader(File.OpenRead(hydBinFile)));

        int reportCount = (int) ((net.getPropertiesMap().getDuration() - net.getPropertiesMap().getRstart()) / net.getPropertiesMap().getRstep()) + 1;
        var netNodes = net.getNodes();
        var netLinks = net.getLinks();

        object[] nodesHead = new object[dseek.getNodes() + 1];

        nodesHead[0] = this.sheet.TransposedMode ? "Node/Time" : "Time/Node";

        int count = 1;
        foreach(Node node in netNodes) {
            nodesHead[count++] = node.getId();
        }

        object[] linksHead = new object[dseek.getLinks() + 1];

        linksHead[0] = this.sheet.TransposedMode ? "Link/Time" : "Time/Link";

        count = 1;
        foreach(Link link in netLinks) {
            linksHead[count++] = link.getId();
        }

        XLSXWriter.Spreadsheet[] resultSheets = new XLSXWriter.Spreadsheet[HydVariable.Values.Length];
        // Array.Clear(resultSheets, 0, resultSheets.Length);

        for (int i = 0; i < resultSheets.Length; i++) {
            if (values != null && !values[i]) continue;
            resultSheets[i] = this.sheet.NewSpreadsheet(HydVariable.Values[i].Name);
            resultSheets[i].addRow(HydVariable.Values[i].IsNode ? nodesHead : linksHead);
        }

        object[] nodeRow = new object[dseek.getNodes() + 1];
        object[] linkRow = new object[dseek.getLinks() + 1];



        for (long time = net.getPropertiesMap().getRstart();
             time <= net.getPropertiesMap().getDuration();
             time += net.getPropertiesMap().getRstep()) {
            AwareStep step = dseek.getStep(time);

            if (step != null) {
                nodeRow[0] = time.getClockTime();
                linkRow[0] = time.getClockTime();

              

                // NODES HEADS
                if (resultSheets[(int)HydVariable.Type.Head] != null) {
                    for(int i = 0; i < netNodes.Length; i++) {
                        nodeRow[i + 1] = step.getNodeHead(i, netNodes[i], net.getFieldsMap());
                    }
                    resultSheets[(int)HydVariable.Type.Head].addRow(nodeRow);
                }

                // NODES DEMANDS
                if (resultSheets[(int)HydVariable.Type.Demands] != null) {
                    for(int i = 0; i < netNodes.Length; i++) {
                        nodeRow[i + 1] = step.getNodeDemand(i, netNodes[i], net.getFieldsMap());
                    }
                    resultSheets[(int)HydVariable.Type.Demands].addRow(nodeRow);
                }

                // NODES PRESSURE
                if (resultSheets[(int)HydVariable.Type.Pressure] != null) {
                    for (int i = 0; i < netNodes.Length; i++) {
                        nodeRow[i + 1] = step.getNodePressure(i, netNodes[i], net.getFieldsMap());
                    }
                    resultSheets[(int)HydVariable.Type.Pressure].addRow(nodeRow);
                }

                // LINK FLOW
                if(resultSheets[(int)HydVariable.Type.Flows] != null) {
                    for (int i = 0; i < netLinks.Length; i++) {
                        linkRow[i + 1] = step.getLinkFlow(i, netLinks[i], net.getFieldsMap());
                    }
                    resultSheets[(int)HydVariable.Type.Flows].addRow(linkRow);
                }

                // LINK VELOCITY
                if(resultSheets[(int)HydVariable.Type.Velocity] != null) {
                    for (int i = 0; i < netLinks.Length; i++) {
                        linkRow[i + 1] = step.getLinkVelocity(i, netLinks[i], net.getFieldsMap());
                    }
                    resultSheets[(int)HydVariable.Type.Velocity].addRow(linkRow);
                }

                // LINK HEADLOSS
                if(resultSheets[(int)HydVariable.Type.Headloss] != null) {
                    for (int i = 0; i < netLinks.Length; i++) {
                        linkRow[i + 1] = step.getLinkHeadLoss(i, netLinks[i], net.getFieldsMap());
                    }
                    resultSheets[(int)HydVariable.Type.Headloss].addRow(linkRow);
                }

                // LINK FRICTION
                if(resultSheets[(int)HydVariable.Type.Friction] != null) {
                    for (int i = 0; i < netLinks.Length; i++) {
                        linkRow[i + 1] = step.getLinkFriction(i, netLinks[i], net.getFieldsMap());
                    }
                    resultSheets[(int)HydVariable.Type.Friction].addRow(linkRow);
                }
            }
            Rtime = time;
        }


        dseek.close();
    }

    /**
     * Generate quality report.
     *
     * @param qualFile Abstract representation of the quality simulation output file.
     * @param net      Hydraulic network.
     * @param nodes    Show nodes quality flag.
     * @param links    Show links quality flag.
     * @throws IOException
     * @throws ENException
     */
    public void createQualReport(string qualFile, Network net, bool nodes, bool links) {
        Rtime = 0;
        

        int reportCount = (int) ((net.getPropertiesMap().getDuration() - net.getPropertiesMap().getRstart()) / net.getPropertiesMap().getRstep()) + 1;

        using (QualityReader dseek = new QualityReader(qualFile, net.getFieldsMap())) {
           
            object[] nodesHead = new object[dseek.getNodes() + 1];
            nodesHead[0] = this.sheet.TransposedMode ? "Node/Time" : "Time/Node";
            for (int i = 0; i < net.getNodes().Length; i++) {
                nodesHead[i + 1] = net.getNodes()[i].getId();
            }

            object[] linksHead = new object[dseek.getLinks() + 1];
            linksHead[0] = this.sheet.TransposedMode ? "Link/Time" : "Time/Link";
            for (int i = 0; i < net.getLinks().Length; i++) {
                linksHead[i + 1] = net.getLinks()[i].getId();
            }

            XLSXWriter.Spreadsheet[] resultSheets = new XLSXWriter.Spreadsheet[hydVariableName.Length];
            // Array.Clear(resultSheets, 0, resultSheets.Length);

            foreach (int var  in  Enum.GetValues(typeof(QualVariable))) {
                
                if ((!QualVariableIsNode[var] || !nodes) && (QualVariableIsNode[var] || !links))
                    continue;

                resultSheets[var] = this.sheet.NewSpreadsheet(QualVariableName[var]);
                resultSheets[var].addRow(QualVariableIsNode[var] ? nodesHead : linksHead);
            }

            object[] nodeRow = new object[dseek.getNodes() + 1];
            object[] linkRow = new object[dseek.getLinks() + 1];

            using (var qIt = dseek.GetEnumerator())
            for (long time = net.getPropertiesMap().getRstart();
                 time <= net.getPropertiesMap().getDuration();
                 time += net.getPropertiesMap().getRstep()) {
               
                if (!qIt.MoveNext())
                    return;

                QualityReader.Step step = qIt.Current;
                if (step != null) {
                    nodeRow[0] = time.getClockTime();
                    linkRow[0] = time.getClockTime();


                    if (resultSheets[(int)QualVariable.Type.Nodes] != null) {
                        for (int i = 0; i < dseek.getNodes(); i++)
                            nodeRow[i + 1] = (double)step.getNodeQuality(i);
                        resultSheets[(int)HydVariable.Type.Head].addRow(nodeRow);
                    }


                    if(resultSheets[(int)QualVariable.Type.Links] != null) {
                        for (int i = 0; i < dseek.getLinks(); i++)
                            linkRow[i + 1] = (double)step.getLinkQuality(i);
                        resultSheets[(int)HydVariable.Type.Demands].addRow(linkRow);
                    }
                    Rtime = time;

                }
            }

        }
    }

    /**
     * Generate multi-species quality report.
     *
     * @param msxBin Abstract representation of the MSX simulation output file.
     * @param net    Hydraulic network.
     * @param netMSX MSX network.
     * @param tk2    Hydraulic network - MSX bridge.
     * @param values Species report flag.
     * @throws IOException
     * @throws ENException
     */
    public void createMSXReport(string msxBin, Network net, EpanetMSX netMSX, ENToolkit2 tk2, bool[] values) {
        Rtime = 0;
        msx.Structures.Node[] nodes = netMSX.getNetwork().getNodes();
        msx.Structures.Link[] links = netMSX.getNetwork().getLinks();
        string[] nSpecies = netMSX.getSpeciesNames();

        int reportCount = (int) ((net.getPropertiesMap().getDuration() - net.getPropertiesMap().getRstart()) / net.getPropertiesMap().getRstep()) + 1;

        MsxReader reader = new MsxReader(nodes.Length - 1, links.Length - 1, nSpecies.Length, netMSX.getResultsOffset());

        int totalSpecies;

        if (values != null) {
            totalSpecies = 0;
            foreach (bool b  in  values)
                if (b)
                    totalSpecies++;
        } else
            totalSpecies = nSpecies.Length;

        reader.open(msxBin);


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
            if (nodes[i].getRpt()) {
                var spr = sheet.NewSpreadsheet("Node&lt;&lt;" + tk2.ENgetnodeid(i) + "&gt;&gt;");
                spr.addRow(nodesHead);

                for (long time = net.getPropertiesMap().getRstart(), period = 0;
                     time <= net.getPropertiesMap().getDuration();
                     time += net.getPropertiesMap().getRstep(), period++) {

                    nodeRow[0] = time.getClockTime();

                    for (int j = 0, ji = 0; j < nSpecies.Length; j++) {
                        if (values == null || values[j])
                            nodeRow[ji++ + 1] = reader.getNodeQual((int) period, i, j + 1);
                    }

                    spr.addRow(nodeRow);
                }
            }
        }

        object[] linkRow = new object[totalSpecies + 1];
        for (int i = 1; i < links.Length; i++) {
            if (links[i].getRpt()) {
                XLSXWriter.Spreadsheet spr = sheet.NewSpreadsheet("Link&lt;&lt;" + tk2.ENgetlinkid(i) + "&gt;&gt;");
                spr.addRow(linksHead);

                for (long time = net.getPropertiesMap().getRstart(), period = 0;
                     time <= net.getPropertiesMap().getDuration();
                     time += net.getPropertiesMap().getRstep(), period++) {

                    linkRow[0] = time.getClockTime();

                    for (int j = 0, ji = 0; j < nSpecies.Length; j++) {
                        if (values == null || values[j])
                            linkRow[ji++ + 1] = reader.getLinkQual((int) period, i, j + 1);
                    }

                    spr.addRow(linkRow);
                }
            }

        }

        reader.close();
    }

    /**
     * Write the final worksheet.
     *
     * @throws IOException
     */
    public void writeWorksheet() {
        this.sheet.Save(xlsxFile);
    }

    /**
     * Write simulation summary to one worksheet.
     *
     * @param inpFile Hydraulic network file.
     * @param net     Hydraulic network.
     * @param msxFile MSX file.
     * @param msx     MSX solver.
     * @throws IOException
     */
    public void writeSummary(string inpFile, Network net, string msxFile, EpanetMSX msx) {
        XLSXWriter.Spreadsheet sh = sheet.NewSpreadsheet("Summary");

        try {
            PropertiesMap pMap = net.getPropertiesMap();
            FieldsMap fMap = net.getFieldsMap();

            if (net.getTitleText() != null)
                for (int i = 0; i < net.getTitleText().Count && i < 3; i++) {
                    if (net.getTitleText()[i].Length > 0) {
                        if (net.getTitleText()[i].Length <= 70)
                            sh.addRow(net.getTitleText()[i]);
                    }
                }
            sh.addRow("\n");
            sh.addRow(Utilities.getText("FMT19"), inpFile);
            sh.addRow(Utilities.getText("FMT20"), net.getJunctions().Length);

            int nReservoirs = 0;
            int nTanks = 0;
            foreach (org.addition.epanet.network.structures.Tank tk  in  net.getTanks())
                if (tk.getArea() == 0)
                    nReservoirs++;
                else
                    nTanks++;

            int nValves = net.getValves().size();
            int nPumps = net.getPumps().size();
            int nPipes = net.getLinks().size() - nPumps - nValves;

            sh.addRow(Utilities.getText("FMT21a"), nReservoirs);
            sh.addRow(Utilities.getText("FMT21b"), nTanks);
            sh.addRow(Utilities.getText("FMT22"), nPipes);
            sh.addRow(Utilities.getText("FMT23"), nPumps);
            sh.addRow(Utilities.getText("FMT24"), nValves);
            sh.addRow(Utilities.getText("FMT25"), pMap.getFormflag().ParseStr());

            sh.addRow(Utilities.getText("FMT26"), pMap.getHstep().getClockTime());
            sh.addRow(Utilities.getText("FMT27"), pMap.getHacc());
            sh.addRow(Utilities.getText("FMT27a"), pMap.getCheckFreq());
            sh.addRow(Utilities.getText("FMT27b"), pMap.getMaxCheck());
            sh.addRow(Utilities.getText("FMT27c"), pMap.getDampLimit());
            sh.addRow(Utilities.getText("FMT28"), pMap.getMaxIter());

            if (pMap.getQualflag() == PropertiesMap.QualType.NONE || pMap.getDuration() == 0.0)
                sh.addRow(Utilities.getText("FMT29"), "None");
            else if (pMap.getQualflag() == PropertiesMap.QualType.CHEM)
                sh.addRow(Utilities.getText("FMT30"), pMap.getChemName());
            else if (pMap.getQualflag() == PropertiesMap.QualType.TRACE)
                sh.addRow(Utilities.getText("FMT31"), "Trace From Node", net.getNode(pMap.getTraceNode()).getId());
            else if (pMap.getQualflag() == PropertiesMap.QualType.AGE)
                sh.addRow(Utilities.getText("FMT32"), "Age");

            if (pMap.getQualflag() != PropertiesMap.QualType.NONE && pMap.getDuration() > 0) {
                sh.addRow(Utilities.getText("FMT33"), "Time Step", pMap.getQstep().getClockTime());
                sh.addRow(Utilities.getText("FMT34"), "Tolerance", fMap.revertUnit(FieldsMap.Type.QUALITY, pMap.getCtol()),
                        fMap.getField(FieldsMap.Type.QUALITY).getUnits());
            }

            sh.addRow(Utilities.getText("FMT36"), pMap.getSpGrav());
            sh.addRow(Utilities.getText("FMT37a"), pMap.getViscos() / Constants.VISCOS);
            sh.addRow(Utilities.getText("FMT37b"), pMap.getDiffus() / Constants.DIFFUS);
            sh.addRow(Utilities.getText("FMT38"), pMap.getDmult());
            sh.addRow(Utilities.getText("FMT39"), fMap.revertUnit(FieldsMap.Type.TIME, pMap.getDuration()), fMap.getField(FieldsMap.Type.TIME).getUnits());

            if (msxFile != null && msx != null) {
                sh.addRow("");
                sh.addRow("MSX data file", msxFile);
                sh.addRow("Species");
                Species[] spe = msx.getNetwork().getSpecies();
                for (int i = 1; i < msx.getNetwork().getSpecies().Length; i++)
                    sh.addRow(spe[i].getId(), spe[i].getUnits());
            }
        } catch (IOException) {

        } catch (ENException e) {
            Debug.Print(e.ToString());
        }
    }


}
}