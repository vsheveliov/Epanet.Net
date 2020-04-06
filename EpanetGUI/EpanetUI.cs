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

using Epanet.Log;
using Epanet.Network.IO.Input;
using Epanet.Network.IO.Output;
using Epanet.Util;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet {

    public sealed partial class EpanetUi : Form {
        private const string WEBLINK = "https://github.com/vsheveliov/Epanet.Net";

        /// <summary>Application title string.</summary>
        private const string APP_TITTLE = "Epanet.NET";

        private const string LOG_FILENAME = "epanet.log";

        private const string SAVE_FILE_DIALOG_FILTER =
            "Epanet INP network file (*.inp)|*.inp|" +
            "Epanet XML network file (*.xml)|*.xml|" +
            "Epanet2 network project (*.net)|*.net|" +
            "Epanet GZIP'ped XML network file (*.xml.gz)|*.xml.gz";

        private const string OPEN_FILE_DIALOG_FILTER =
            "Epanet INP network file (*.inp)|*.inp|" +
            "Epanet XML network file (*.xml)|*.xml|" +
            "Epanet2 network project (*.net)|*.net|" +
            "All supported files (*.inp, *.net, *.xml)|*.inp *.net *.xml";

        /// <summary>Abstract representation of the network file(INP/NET/XML).</summary>
        private string _inpFile;

        private EpanetNetwork _net;

        private static readonly TraceSource log;

        /// <summary>Reference to the report options window.</summary>
        private ReportOptions _reportOptions;

        static EpanetUi() {
           // InitLogger
            log = new TraceSource("epanet", SourceLevels.All);
            log.Listeners.Remove("Default");
            log.Listeners.Add(new EpanetTraceListener(LOG_FILENAME, true));
        }

        public EpanetUi() {
            InitializeComponent();
            log.Information(0, GetType().FullName + " started.");
            Text = APP_TITTLE;
            MinimumSize = new Size(848, 500);
            ClearInterface();

            string[] args = Environment.GetCommandLineArgs();

            if (args.Length > 1) {
                DoOpen(args[1]);
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
            log.Close();
            // Environment.Exit(0);
        }



        /// <summary>Reset the interface layout</summary>
        private void ClearInterface() {
            networkPanel.Net = null;
            _inpFile = null;
            Text = APP_TITTLE;
            textReservoirs.Text = "0";
            textTanks.Text = "0";
            textPipes.Text = "0";
            textNodes.Text = "0";
            textDuration.Text = "00:00:00";
            textHydraulic.Text = "00:00:00";
            textPattern.Text = "00:00:00";
            textUnits.Text = "NONE";
            textHeadloss.Text = "NONE";
            textQuality.Text = "NONE";
            textDemand.Text = "0.0";

            if (_reportOptions != null) {
                _reportOptions.Close();
                _reportOptions = null;
            }
            
            saveButton.Enabled = false;
            menuSave.Enabled = false;
            menuRun.Enabled = false;
            menuClose.Enabled = false;
            runSimulationButton.Enabled = false;
        }

        private void UnlockInterface() {

            textReservoirs.Text = _net.Reservoirs.Count().ToString(CultureInfo.CurrentCulture);
            textTanks.Text = _net.Tanks.Count().ToString(CultureInfo.CurrentCulture);
            textPipes.Text = _net.Links.Count.ToString(CultureInfo.CurrentCulture);
            textNodes.Text = _net.Nodes.Count.ToString(CultureInfo.CurrentCulture);

            try {
                
                textDuration.Text = _net.Duration.GetClockTime();
                textUnits.Text = _net.UnitsFlag.ToString();
                textHeadloss.Text = _net.FormFlag.ToString();
                textQuality.Text = _net.QualFlag.ToString();
                textDemand.Text = _net.DMult.ToString(CultureInfo.CurrentCulture);
                textHydraulic.Text = _net.HStep.GetClockTime();
                textPattern.Text = _net.PStep.GetClockTime();
            }
            catch (EnException) { }

            Text = APP_TITTLE + " - " + _inpFile;
            inpName.Text = _inpFile;
            networkPanel.Net = Net;

            
            if (_reportOptions != null) {
                _reportOptions.Close();
                _reportOptions = null;
            }
            
            menuSave.Enabled = true;
            menuRun.Enabled = true;
            menuClose.Enabled = true;
            runSimulationButton.Enabled = true;            
            saveButton.Enabled = true;

            
        }

        private EpanetNetwork Net {
            get => _net;
            set {
                if (_net == value) return;

                _net = networkPanel.Net = value;

                if (_net == null) {
                    ClearInterface();
                    return;
                }

                UnlockInterface();
            }
        }



        /// <summary>Show report options window to configure and run the simulation.</summary>
        private void RunSimulation(object sender, EventArgs e) {
            if (_reportOptions == null)
                _reportOptions = new ReportOptions(_inpFile);

            _reportOptions.ShowDialog(this);
        }

        /// <summary>Show the save dialog to save the network file.</summary>
        private void SaveEvent(object sender, EventArgs e) {
            if (Net == null) return;

            // string initialDirectory = Path.GetDirectoryName(Path.GetFullPath(this.inpFile)) ?? string.Empty;

            var dlg = new SaveFileDialog {
                // InitialDirectory = initialDirectory,
                OverwritePrompt = true,
                FileName = Path.GetFileNameWithoutExtension(_inpFile),
                Filter = SAVE_FILE_DIALOG_FILTER
            };

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            OutputComposer composer;

            string fileName = Path.GetFullPath(dlg.FileName);
            string extension = Path.GetExtension(dlg.FileName);

            switch (extension) {
                case ".inp":
            composer = new InpComposer(networkPanel.Net);
                    break;

                case ".xml":
                    composer = new XmlComposer(networkPanel.Net, false);
                    break;

                case ".gz":
                    composer = new XmlComposer(networkPanel.Net, true);
                    break;

                default:
                    extension = ".inp";
                    composer = new InpComposer(networkPanel.Net);
                    break;
            }

            fileName = Path.ChangeExtension(fileName, extension);

            try {
                composer.Compose(fileName);
            }
            catch (EnException ex) {
                MessageBox.Show(
                    ex.Message + "\nCheck epanet.log for detailed error description",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                
                log.Error("Unable to save network configuration file: {0}", ex);
            }
            catch (Exception ex) {
                MessageBox.Show(
                    "Unable to save network configuration file",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                log.Error(0, "Unable to save network configuration file: {0}", ex);
            }
        }

        //<summary>Show the open dialog and open the INP/NET and XML files.</summary>
        private void OpenEvent(object sender, EventArgs e) {
            //fileChooser = new FileDialog(frame);

            var fileChooser = new OpenFileDialog {
                Multiselect = false,
                Filter = OPEN_FILE_DIALOG_FILTER,
                // FilterIndex = 0
            };

            if (fileChooser.ShowDialog(this) != DialogResult.OK)
                return;

            string netFile = fileChooser.FileName;

            DoOpen(netFile);

            menuSave.Enabled = true;
            menuRun.Enabled = true;
        }

        private void DoOpen(string netFile) {
            string fileExtension = (Path.GetExtension(netFile) ?? string.Empty).ToLowerInvariant();

            if (fileExtension != ".net" &&
                fileExtension != ".inp" &&
                fileExtension != ".xml" &&
                fileExtension != ".gz") return;

            _inpFile = netFile;

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

            EpanetNetwork net;

            try {
                net = inpParser.Parse(new EpanetNetwork(), _inpFile);
            }
            catch (EnException ex) {
                MessageBox.Show(
                    this,
                    ex + "\nCheck epanet.log for detailed error description",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                ClearInterface();
                _inpFile = null;
                return;
            }
            catch (Exception ex) {
                MessageBox.Show(
                    this,
                    "Unable to parse network configuration file",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                
                log.Error("Unable to parse network configuration file: {0}", ex);

                ClearInterface();
                _inpFile = null;

                return;
            }

            Net = net;

        }

        private void checks_CheckedChanged(object sender, EventArgs e) {
            networkPanel.DrawNodes = checkNodes.Checked;
            networkPanel.DrawPipes = checkPipes.Checked;
            networkPanel.DrawTanks = checkTanks.Checked;
            // this.networkPanel.Refresh();
            networkPanel.Invalidate();
        }

        

        private void networkPanel_MouseMove(object sender, MouseEventArgs e) {
            lblCoordinates.Text = string.Format("{0}/{1:P}", networkPanel.MousePoint, networkPanel.Zoom);

        }

        private void mnuZoomAll_Click(object sender, EventArgs e) { networkPanel.ZoomAll(); }
        private void mnuZoomIn_Click(object sender, EventArgs e) { networkPanel.ZoomStep(1); }
        private void mnuZoomOut_Click(object sender, EventArgs e) { networkPanel.ZoomStep(-1); }

        private void menuClose_Click(object sender, EventArgs e) {
            Net = null;
            
        }
    }

}
