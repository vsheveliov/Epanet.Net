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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Epanet.Log;
using Epanet.Network.IO.Input;
using Epanet.Network.IO.Output;
using Epanet.Util;

namespace Epanet.UI {

    public sealed partial class EpanetUI : Form {
        private const string WEBLINK = "https://github.com/vsheveliov/Epanet.Net";
        private const string LASTDOC = @"E:\LPRO\_WORK\EN_goefis.inp";

        /// <summary>Application title string.</summary>
        private const string APP_TITTLE = "Epanet.NET";

        private const string LOG_FILENAME = "epanet.log";

        /// <summary>Abstract representation of the network file(INP/XLSX/XML).</summary>
        private string inpFile;

        private Network.Network net;

        private TraceSource log;

        /// <summary>Reference to the report options window.</summary>
        private ReportOptions reportOptions;

        public EpanetUI() {
            this.InitializeComponent();
            this.InitLogger();
            this.log.Information(0, this.GetType().FullName + " started.");
            this.Text = APP_TITTLE;
            this.MinimumSize = new Size(848, 500);
            this.ClearInterface();
#if DEBUG
            this.DoOpen(LASTDOC);
#endif
        }


        /// <summary>Open the aware-p webpage in the browser.</summary>
        private void logoB_Click(object sender, EventArgs e) {
            try {
                Process.Start(WEBLINK);
            }
            catch (Exception ex) {
                MessageBox.Show(this, "Error opening browser:" + ex.Message);
            }
        }

        private void EpanetUI_FormClosing(object sender, FormClosingEventArgs e) {
            this.log.Flush();
            // Environment.Exit(0);
        }



        /// <summary>Reset the interface layout</summary>
        private void ClearInterface() {
            this.networkPanel.Net = null;
            this.inpFile = null;
            this.Text = APP_TITTLE;
            this.textReservoirs.Text = "0";
            this.textTanks.Text = ("0");
            this.textPipes.Text = ("0");
            this.textNodes.Text = ("0");
            this.textDuration.Text = ("00:00:00");
            this.textHydraulic.Text = ("00:00:00");
            this.textPattern.Text = ("00:00:00");
            this.textUnits.Text = ("NONE");
            this.textHeadloss.Text = ("NONE");
            this.textQuality.Text = ("NONE");
            this.textDemand.Text = ("0.0");

            if (this.reportOptions != null) {
                this.reportOptions.Close();
                this.reportOptions = null;
            }
            
            this.saveButton.Enabled = false;
            this.menuSave.Enabled = false;
            this.menuRun.Enabled = false;
            this.menuClose.Enabled = false;
            this.runSimulationButton.Enabled = false;
        }

        private void UnlockInterface() {

            int resrvCount = 0;
            int tanksCount = 0;

            foreach (var tank in this.Net.Tanks) {
                if (tank.IsReservoir)
                    resrvCount++;
                else
                    tanksCount++;
            }

            this.textReservoirs.Text = resrvCount.ToString(CultureInfo.CurrentCulture);
            this.textTanks.Text = tanksCount.ToString(CultureInfo.CurrentCulture);
            this.textPipes.Text = this.Net.Links.Count.ToString(CultureInfo.CurrentCulture);
            this.textNodes.Text = this.Net.Nodes.Count.ToString(CultureInfo.CurrentCulture);

            try {
                var pMap = this.Net.PropertiesMap;
                this.textDuration.Text = pMap.Duration.GetClockTime();
                this.textUnits.Text = pMap.UnitsFlag.ToString();
                this.textHeadloss.Text = pMap.FormFlag.ToString();
                this.textQuality.Text = pMap.QualFlag.ToString();
                this.textDemand.Text = pMap.DMult.ToString(CultureInfo.CurrentCulture);
                this.textHydraulic.Text = pMap.HStep.GetClockTime();
                this.textPattern.Text = pMap.PStep.GetClockTime();
            }
            catch (ENException) { }

            this.Text = APP_TITTLE + " - " + this.inpFile;
            this.inpName.Text = this.inpFile;
            this.networkPanel.Net = this.Net;

            
            if (this.reportOptions != null) {
                this.reportOptions.Close();
                this.reportOptions = null;
            }
            
            this.menuSave.Enabled = true;
            this.menuRun.Enabled = true;
            this.menuClose.Enabled = true;
            this.runSimulationButton.Enabled = true;            
            this.saveButton.Enabled = true;

            
        }

        private Network.Network Net {
            get { return this.net; }
            set {
                if (this.net == value) return;

                this.net = this.networkPanel.Net = value;

                if (this.net == null) {
                    this.ClearInterface();
                    return;
                }

                this.UnlockInterface();
            }
        }

        private void InitLogger() {
            this.log = new TraceSource(typeof(EpanetUI).FullName, SourceLevels.All);
            this.log.Listeners.Remove("Default");
            RollingFileStream stream = new RollingFileStream(LOG_FILENAME, 0x1000, 10, FileMode.Append, FileShare.Read);
            TextWriter writer = new StreamWriter(stream, Encoding.Default);
            TextWriterTraceListener listener = new EpanetTraceListener(writer, LOG_FILENAME);
            this.log.Listeners.Add(listener);
        }

        /// <summary>Show report options window to configure and run the simulation.</summary>
        private void RunSimulation(object sender, EventArgs e) {
            if (this.reportOptions == null)
                this.reportOptions = new ReportOptions(this.inpFile, null, this.log);

            this.reportOptions.ShowDialog(this);
        }

        /// <summary>Show the save dialog to save the network file.</summary>
        private void SaveEvent(object sender, EventArgs e) {
            if (this.Net == null) return;

            string initialDirectory = Path.GetDirectoryName(Path.GetFullPath(this.inpFile)) ?? string.Empty;

            var dlg = new SaveFileDialog {
                InitialDirectory = initialDirectory,
                OverwritePrompt = true,
                Filter =
                    "Epanet XLSX network file (*.xlsx)|*.xlsx|" + "Epanet XML network file (*.xml)|*.xml|"
                    + "Epanet GZIP'ped XML network file (*.xml.gz)|*.xml.gz|" + "Epanet INP network file (*.inp)|*.inp"
            };

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            OutputComposer compose;

            string fileName = Path.GetFullPath(dlg.FileName);
            string extension = Path.GetExtension(dlg.FileName) ?? string.Empty;

            switch (extension) {
                case ".inp":
                    compose = new InpComposer();
                    break;

                case ".xlsx":
                    compose = new ExcelComposer();
                    break;

                case ".xml":
                    compose = new XMLComposer(false);
                    break;

                case ".gz":
                    compose = new XMLComposer(true);
                    break;

                default:
                    extension = ".inp";
                    compose = new InpComposer();
                    break;
            }

            fileName = Path.ChangeExtension(fileName, extension);

            try {
                compose.Composer(this.networkPanel.Net, fileName);
            }
            catch (ENException ex) {
                MessageBox.Show(
                    ex.Message + "\nCheck epanet.log for detailed error description",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                this.log.Error("Unable to save network configuration file: {0}", ex);
            }
            catch (Exception ex) {
                MessageBox.Show(
                    "Unable to save network configuration file",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                this.log.Error(0, "Unable to save network configuration file: {0}", ex);
            }
        }

        //<summary>Show the open dialog and open the INP/XLSX and XML files.</summary>
        private void OpenEvent(object sender, EventArgs e) {
            //fileChooser = new FileDialog(frame);
            var fileChooser = new OpenFileDialog {
                Multiselect = false,
                Filter =
                    "Epanet XLSX network file (*.xlsx)|*.xlsx|" 
                    + "Epanet XML network file (*.xml)|*.xml|"
                    + "Epanet INP network file (*.inp)|*.inp|"
                    + "All supported files (*.inp, *.xlsx, *.xml)|*.inp *.xlsx *.xml",
                FilterIndex = 3
            };

            if (fileChooser.ShowDialog(this) != DialogResult.OK)
                return;

            string netFile = fileChooser.FileName;

            this.DoOpen(netFile);

            this.menuSave.Enabled = true;
            this.menuRun.Enabled = true;
        }

        private void DoOpen(string netFile) {
            string fileExtension = (Path.GetExtension(netFile) ?? string.Empty).ToLowerInvariant();

            if (  fileExtension != ".xlsx" && 
                fileExtension != ".inp" && fileExtension != ".xml" && fileExtension != ".gz") return;

            this.inpFile = netFile;

            InputParser inpParser;

            switch (fileExtension.ToLowerInvariant()) {                    
                case ".xlsx":
                    inpParser = new ExcelParser(this.log);
                    break;
                
                case ".xml":
                    inpParser = new XmlParser(this.log, false);
                    break;

                case ".gz":
                    inpParser = new XmlParser(this.log, true);
                    break;
                case ".inp":
                    inpParser = new InpParser(this.log);
                    break;
                default:
                    MessageBox.Show(
                        "Not supported file type: *" + fileExtension,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
            }

            var epanetNetwork = new Network.Network();

            try {
                inpParser.Parse(epanetNetwork, this.inpFile);
            }
            catch (ENException ex) {
                MessageBox.Show(
                    this,
                    ex + "\nCheck epanet.log for detailed error description",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                this.ClearInterface();
                this.inpFile = null;
                return;
            }
            catch (Exception ex) {
                MessageBox.Show(
                    this,
                    "Unable to parse network configuration file",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                this.log.Error("Unable to parse network configuration file: {0}", ex);
                this.ClearInterface();
                this.inpFile = null;

                return;
            }

            this.Net = epanetNetwork;

        }

        private void checks_CheckedChanged(object sender, EventArgs e) {
            this.networkPanel.DrawNodes = this.checkNodes.Checked;
            this.networkPanel.DrawPipes = this.checkPipes.Checked;
            this.networkPanel.DrawTanks = this.checkTanks.Checked;
            // this.networkPanel.Refresh();
            this.networkPanel.Invalidate();
        }

        

        private void networkPanel_MouseMove(object sender, MouseEventArgs e) {
            this.lblCoordinates.Text = string.Format("{0}/{1:P}", this.networkPanel.MousePoint, this.networkPanel.Zoom);

        }

        private void mnuZoomAll_Click(object sender, EventArgs e) { this.networkPanel.ZoomAll(); }
        private void mnuZoomIn_Click(object sender, EventArgs e) { this.networkPanel.ZoomStep(1); }
        private void mnuZoomOut_Click(object sender, EventArgs e) { this.networkPanel.ZoomStep(-1); }

        private void menuClose_Click(object sender, EventArgs e) {
            this.Net = null;
            
        }
    }

}
