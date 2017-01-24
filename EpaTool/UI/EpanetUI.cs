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
using System.Linq;
using System.Windows.Forms;

using Epanet.Enums;
using Epanet.Log;
using Epanet.Network.IO.Input;
using Epanet.Network.IO.Output;
using Epanet.Util;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet.UI {

    public sealed partial class EpanetUI : Form {
        private const string WEBLINK = "https://github.com/vsheveliov/Epanet.Net";

        /// <summary>Application title string.</summary>
        private const string APP_TITTLE = "Epanet.NET";

        private const string LOG_FILENAME = "epanet.log";

        private const string SaveFileDialogFilter =
            "Epanet INP network file (*.inp)|*.inp|" +
            "Epanet XML network file (*.xml)|*.xml|" +
            "Epanet2 network project (*.net)|*.net|" +
            "Epanet GZIP'ped XML network file (*.xml.gz)|*.xml.gz";

        private const string OpenFileDialogFilter =
            "Epanet INP network file (*.inp)|*.inp|" +
            "Epanet XML network file (*.xml)|*.xml|" +
            "Epanet2 network project (*.net)|*.net|" +
            "All supported files (*.inp, *.net, *.xml)|*.inp *.net *.xml";

        /// <summary>Abstract representation of the network file(INP/NET/XML).</summary>
        private string inpFile;

        private EpanetNetwork net;

        private static readonly TraceSource Log;

        /// <summary>Reference to the report options window.</summary>
        private ReportOptions reportOptions;

        static EpanetUI() {
           // InitLogger
            Log = new TraceSource("epanet", SourceLevels.All);
            Log.Listeners.Remove("Default");
            Log.Listeners.Add(new EpanetTraceListener(LOG_FILENAME, true));
        }

        public EpanetUI() {
            this.InitializeComponent();
            Log.Information(0, this.GetType().FullName + " started.");
            this.Text = APP_TITTLE;
            this.MinimumSize = new Size(848, 500);
            this.ClearInterface();

            string[] args = Environment.GetCommandLineArgs();

            if (args.Length > 1) {
                this.DoOpen(args[1]);
            }

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
            // Log.Flush();
            Log.Close();
            // Environment.Exit(0);
        }



        /// <summary>Reset the interface layout</summary>
        private void ClearInterface() {
            this.networkPanel.Net = null;
            this.inpFile = null;
            this.Text = APP_TITTLE;
            this.textReservoirs.Text = "0";
            this.textTanks.Text = "0";
            this.textPipes.Text = "0";
            this.textNodes.Text = "0";
            this.textDuration.Text = "00:00:00";
            this.textHydraulic.Text = "00:00:00";
            this.textPattern.Text = "00:00:00";
            this.textUnits.Text = "NONE";
            this.textHeadloss.Text = "NONE";
            this.textQuality.Text = "NONE";
            this.textDemand.Text = "0.0";

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

            this.textReservoirs.Text = this.net.Reservoirs.Count().ToString(CultureInfo.CurrentCulture);
            this.textTanks.Text = this.net.Tanks.Count().ToString(CultureInfo.CurrentCulture);
            this.textPipes.Text = this.net.Links.Count.ToString(CultureInfo.CurrentCulture);
            this.textNodes.Text = this.net.Nodes.Count.ToString(CultureInfo.CurrentCulture);

            try {
                
                this.textDuration.Text = this.net.Duration.GetClockTime();
                this.textUnits.Text = this.net.UnitsFlag.ToString();
                this.textHeadloss.Text = this.net.FormFlag.ToString();
                this.textQuality.Text = this.net.QualFlag.ToString();
                this.textDemand.Text = this.net.DMult.ToString(CultureInfo.CurrentCulture);
                this.textHydraulic.Text = this.net.HStep.GetClockTime();
                this.textPattern.Text = this.net.PStep.GetClockTime();
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

        private EpanetNetwork Net {
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



        /// <summary>Show report options window to configure and run the simulation.</summary>
        private void RunSimulation(object sender, EventArgs e) {
            if (this.reportOptions == null)
                this.reportOptions = new ReportOptions(this.inpFile, null);

            this.reportOptions.ShowDialog(this);
        }

        /// <summary>Show the save dialog to save the network file.</summary>
        private void SaveEvent(object sender, EventArgs e) {
            if (this.Net == null) return;

            // string initialDirectory = Path.GetDirectoryName(Path.GetFullPath(this.inpFile)) ?? string.Empty;

            var dlg = new SaveFileDialog {
                // InitialDirectory = initialDirectory,
                OverwritePrompt = true,
                FileName = Path.GetFileNameWithoutExtension(this.inpFile),
                Filter = SaveFileDialogFilter
            };

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            OutputComposer composer;

            string fileName = Path.GetFullPath(dlg.FileName);
            string extension = Path.GetExtension(dlg.FileName);

            switch (extension) {
                case ".inp":
                    composer = new InpComposer();
                    break;

                case ".xml":
                    composer = new XMLComposer(false);
                    break;

                case ".gz":
                    composer = new XMLComposer(true);
                    break;

                default:
                    extension = ".inp";
                    composer = new InpComposer();
                    break;
            }

            fileName = Path.ChangeExtension(fileName, extension);

            try {
                composer.Composer(this.networkPanel.Net, fileName);
            }
            catch (ENException ex) {
                MessageBox.Show(
                    ex.Message + "\nCheck epanet.log for detailed error description",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                
                Log.Error("Unable to save network configuration file: {0}", ex);
            }
            catch (Exception ex) {
                MessageBox.Show(
                    "Unable to save network configuration file",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Log.Error(0, "Unable to save network configuration file: {0}", ex);
            }
        }

        //<summary>Show the open dialog and open the INP/NET and XML files.</summary>
        private void OpenEvent(object sender, EventArgs e) {
            //fileChooser = new FileDialog(frame);

            var fileChooser = new OpenFileDialog {
                Multiselect = false,
                Filter = OpenFileDialogFilter,
                // FilterIndex = 0
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

            if (fileExtension != ".net" &&
                fileExtension != ".inp" &&
                fileExtension != ".xml" &&
                fileExtension != ".gz") return;

            this.inpFile = netFile;

            InputParser inpParser;

            switch (fileExtension.ToLowerInvariant()) {
            case ".inp":
                inpParser = new InpParser();
                break;

            case ".net":
                inpParser = new NetParser();
                break;

            case ".xml":
                inpParser = new XmlParser(false);
                break;

            case ".gz":
                inpParser = new XmlParser(true);
                break;

            default:
                MessageBox.Show(
                    "Not supported file type: *" + fileExtension,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var epanetNetwork = new EpanetNetwork();

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
                
                Log.Error("Unable to parse network configuration file: {0}", ex);

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
