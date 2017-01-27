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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

using Epanet.Enums;
using Epanet.Hydraulic;
using Epanet.Log;
using Epanet.MSX;
using Epanet.Network.IO.Input;
using Epanet.Quality;
using Epanet.Report;
using Epanet.Util;

using EpanetNetwork = Epanet.Network.Network;
using UnitsType = Epanet.Enums.UnitsType;
using Utilities = Epanet.Util.Utilities;

namespace Epanet.UI {

    /// <summary>Configures and executes the epanet (Hydraulic/Quality) and MSX quality simulation through the UI.</summary>
    public sealed partial class ReportOptions : Form {

        /// <summary>Epanet toolkit for the MSX.</summary>
        private readonly EnToolkit2 _epanetTk;

        /// <summary>Epanet network config file.</summary>
        private readonly string _fileInp;

        /// <summary>MSX config file.</summary>
        private readonly string _fileMsx;

        private static readonly TraceSource log = new TraceSource("epanet", SourceLevels.All);

        /// <summary>Loaded INP network.</summary>
        private readonly EpanetNetwork _net;

        /// <summary>Hydraulic simulator.</summary>
        private HydraulicSim _hydSim;

        /// <summary>Loaded MSX simulation.</summary>
        private EpanetMSX _netMsx;

        private ReportOptions() {
            InitializeComponent();

            unitsBox.Items.AddRange(new object[] {"SI", "US"});

            var periods = Array.ConvertAll(TimeStep.Values, x => (object)x);
            reportPeriodBox.Items.AddRange(periods);
            hydComboBox.Items.AddRange(periods);
            qualComboBox.Items.AddRange(periods);

            hydVariables.Items.AddRange(Array.ConvertAll(ReportGenerator.HydVariable.Values, x => (object)x));

            for (int i = 0; i < hydVariables.Items.Count; i++) {
                hydVariables.SetItemChecked(i, true);
            }

            qualityVariables.Items.AddRange(Array.ConvertAll(ReportGenerator.QualVariable.Values, x => (object)x));

            for (int i = 0; i < qualityVariables.Items.Count; i++) {
                qualityVariables.SetItemChecked(i, true);
            }
        }

        /// <summary>Report options dialog constructor.</summary>
        public ReportOptions(string inpFile, string msxFile) : this() {
            

            if (inpFile == null) return;

            _fileInp = inpFile;
            _net = new EpanetNetwork();

            try {
                InputParser inpParser;

                string extension = Path.GetExtension(inpFile);

                switch (extension.ToLowerInvariant()) {
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
                    inpParser = new InpParser();
                    break;
                }

                _net = inpParser.Parse(new EpanetNetwork(), inpFile);
            }
            catch (ENException ex) {
                MessageBox.Show(
                    ex.Message + "\nCheck epanet.log for detailed error description",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            if (msxFile == null) return;

            _fileMsx = msxFile;
            _epanetTk = new EnToolkit2(_net);
            _netMsx = new EpanetMSX(_epanetTk);

            try {
                ErrorCodeType ret = _netMsx.Load(_fileMsx);
                if (ret != 0) {
                    MessageBox.Show("MSX parsing error " + ret, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _fileMsx = null;
                    _netMsx = null;
                    _epanetTk = null;
                }
            }
            catch (IOException) {
                MessageBox.Show(
                    "IO error while reading the MSX file",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                _fileMsx = null;
                _netMsx = null;
                _epanetTk = null;
            }
        }

        private void ReportOptions_Closing(object sender, FormClosingEventArgs e) {
            if (_hydSim != null) {
                try {
                    _hydSim.StopRunning();
                }
                catch (ThreadInterruptedException ex) {
                    Debug.Print(ex.ToString());
                }
            }

            if (_netMsx != null) {
                try {
                    _netMsx.StopRunning();
                }
                catch (ThreadInterruptedException e1) {
                    Debug.Print(e1.ToString());
                }
            }
        }

        /// <summary>Create and show the window.</summary>
        private void ReportOptions_Load(object sender, EventArgs e) {
            // Adjust widgets before showing the window.

            if (_net != null) {
                try {
                    unitsBox.SelectedIndex = _net.UnitsFlag == UnitsType.SI ? 0 : 1;
                    reportPeriodBox.SelectedIndex = TimeStep.GetNearestStep(_net.RStep);
                    hydComboBox.SelectedIndex = TimeStep.GetNearestStep(_net.HStep);
                    qualComboBox.SelectedIndex = TimeStep.GetNearestStep(_net.QStep);
                    textSimulationDuration.Text = _net.Duration.GetClockTime();
                    textReportStart.Text = _net.RStart.GetClockTime();
                    qualityCheckBox.Enabled = _net.QualFlag != QualType.NONE;
                }
                catch (ENException ex) {
                    Debug.Print(ex.ToString());
                }
            }

            if (_netMsx != null && _netMsx.GetSpeciesNames().Length > 0) {
                var speciesNames = Array.ConvertAll(_netMsx.GetSpeciesNames(), x => (object)x);
                speciesCheckList.Items.AddRange(speciesNames);
                //this.speciesCheckList.SelectionMode = SelectionMode.MultiExtended; // TODO: in designer
            }
            else {
                qualityMSXCheckBox.Enabled = false;
            }

            Visible = true;
            progressPanel.Visible = false;
            actions.Visible = true;
            UnlockInterface();
        }

        private void hydraulicsCheckBox_CheckedChanged(object sender, EventArgs e) {
            hydVariables.Enabled = hydraulicsCheckBox.Checked;
        }

        private void qualityCheckBox_CheckedChanged(object sender, EventArgs e) {
            qualityVariables.Enabled = qualityCheckBox.Checked;
        }

        private void qualityMSXCheckBox_CheckedChanged(object sender, EventArgs e) {
            speciesCheckList.Enabled = qualityMSXCheckBox.Checked;
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            //this.Close();
            Visible = false;
        }

        private void hydComboBox_SelectedIndexChanged(object sender, EventArgs eventArgs) {
            if (reportPeriodBox.SelectedIndex < hydComboBox.SelectedIndex) reportPeriodBox.SelectedIndex = hydComboBox.SelectedIndex;
        }

        private void reportPeriodBox_SelectedIndexChanged(object sender, EventArgs e) {
            if (reportPeriodBox.SelectedIndex < hydComboBox.SelectedIndex) reportPeriodBox.SelectedIndex = hydComboBox.SelectedIndex;
        }

        private void textReportStart_Validating(object sender, CancelEventArgs e) {
            double val = Utilities.GetHour(textReportStart.Text);
            if (double.IsNaN(val)) e.Cancel = true;
        }

        private void textSimulationDuration_Validating(object sender, CancelEventArgs e) {
            double val = Utilities.GetHour(textSimulationDuration.Text);
            if (double.IsNaN(val)) e.Cancel = true;
        }

        /// <summary>Lock the interface during the simulation.</summary>
        private void LockInterface() {
            hydraulicsCheckBox.Enabled = false;
            qualityCheckBox.Enabled = false;
            qualityMSXCheckBox.Enabled = false;
            hydVariables.Enabled = false;
            qualityVariables.Enabled = false;
            speciesCheckList.Enabled = false;
            showSummaryCheckBox.Enabled = false;
            showHydraulicSolverEventsCheckBox.Enabled = false;
            reportPeriodBox.Enabled = false;
            unitsBox.Enabled = false;
            hydComboBox.Enabled = false;
            textSimulationDuration.Enabled = false;
            qualComboBox.Enabled = false;
            textReportStart.Enabled = false;
            transposeResultsCheckBox.Enabled = false;
        }

        /// <summary>Unlock the interface during the simulation.</summary>
        private void UnlockInterface() {
            hydraulicsCheckBox.Enabled = true;
            transposeResultsCheckBox.Enabled = true;

            if (_netMsx != null && _netMsx.GetSpeciesNames().Length > 0) {
                qualityMSXCheckBox.Enabled = true;
                if (qualityMSXCheckBox.Checked) speciesCheckList.Enabled = true;
            }

            hydVariables.Enabled = true;
            try {
                if (_net.QualFlag != QualType.NONE) {
                    if (qualityCheckBox.Checked) {
                        qualityVariables.Enabled = true;
                        qualityCheckBox.Enabled = true;
                    }
                }
            }
            catch (ENException e) {
                Debug.Print(e.ToString());
            }

            showSummaryCheckBox.Enabled = true;
            showHydraulicSolverEventsCheckBox.Enabled = true;
            reportPeriodBox.Enabled = true;
            unitsBox.Enabled = true;
            hydComboBox.Enabled = true;
            textSimulationDuration.Enabled = true;
            qualComboBox.Enabled = true;
            textReportStart.Enabled = true;
        }


        private void runButton_Click(object sender, EventArgs e) {

            if (Simulated) {
                MessageBox.Show("Simulation in progress! Can not start another one.");
                return;
            }

            if (Utilities.GetHour(textSimulationDuration.Text) < 0) {
                MessageBox.Show(
                    "Invalid time expression for simulation duration",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (Utilities.GetHour(textReportStart.Text) < 0) {
                MessageBox.Show(
                    this,
                    "Invalid time expression for report start time",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var fdialog = new SaveFileDialog {
                Title = "Save xlsx file",
                Filter = "Excel 2007 file (xlsx)|*.xlsx",
                FileName = "report_" + Path.GetFileNameWithoutExtension(_fileInp)
            };

            if (fdialog.ShowDialog() != DialogResult.OK) return;

            try {
                RunSimulation(fdialog.FileName);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Visible = false;
        }

        private bool _simulated;

        private bool Simulated {
            get { return _simulated; }
            set {
                _simulated = value;

                if (value) {
                    LockInterface();
                    progressPanel.Visible = true;
                    runButton.Enabled = false;
                }
                else {
                    UnlockInterface();
                    progressPanel.Visible = false;
                    runButton.Enabled = true;
                }
            }
        }

        private bool _canselSimulation;


        #region threading

        private ReportGenerator _gen;

        private void RunSimulation(string fileName) {
            TraceListener simHandler = null;

            Simulated = true;
            _canselSimulation = false;

            progressBar.Value = 0;
            progressBar.Maximum = 100;
            progressBar.Minimum = 0;

            try {
                if (showHydraulicSolverEventsCheckBox.Checked) {
                    string logFile = Path.Combine(Path.GetDirectoryName(fileName) ?? string.Empty, "hydEvents.log");

                    try {
                        simHandler = new EpanetTraceListener(logFile, false);
                        simHandler.TraceOutputOptions &= ~TraceOptions.DateTime;
                        log.Listeners.Add(simHandler);
                    }
                    catch (IOException ex) {
                        log.Error(ex);
                    }
                }

                int reportPeriod = ((TimeStep)reportPeriodBox.SelectedItem).Time;
                int reportStartTime = (int)(Utilities.GetHour(textReportStart.Text) * 3600);
                int hydTStep = ((TimeStep)hydComboBox.SelectedItem).Time;
                int qualTStep = ((TimeStep)qualComboBox.SelectedItem).Time;
                int durationTime = (int)(Utilities.GetHour(textSimulationDuration.Text) * 3600);

                _net.RStart = reportStartTime;
                _net.RStep = reportPeriod;
                _net.HStep = hydTStep;
                _net.Duration = durationTime;
                _net.QStep = qualTStep;

                statusLabel.Text = "Simulating hydraulics";

                try {
                    _hydSim = new HydraulicSim(_net, log);

                    RunThread(
                        () => _hydSim.Simulate("hydFile.bin"),
                        10,
                        30,
                        () => _hydSim.Htime,
                        () => _hydSim.Htime / (double)_net.Duration);
                }
                catch (ENException ex) {
                    if (ex.Code == ErrorCode.Err1000)
                        throw new ThreadInterruptedException();

                    MessageBox.Show(
                        ex.Message + "\nCheck epanet.log for detailed error description",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                if (_canselSimulation) return;

                if (_fileMsx != null && qualityMSXCheckBox.Checked) {
                    statusLabel.Text = "Simulating MSX";

                    try {
                        // reload MSX
                        _netMsx = new EpanetMSX(_epanetTk);
                        _netMsx.Load(_fileMsx);

                        _netMsx.Network.Rstep = reportPeriod;
                        _netMsx.Network.Qstep = qualTStep;
                        _netMsx.Network.Rstart = reportStartTime;
                        _netMsx.Network.Dur = durationTime;

                        _epanetTk.Open("hydFile.bin");

                        RunThread(
                            () => _netMsx.Run("msxFile.bin"),
                            30,
                            50,
                            () => _netMsx.QTime,
                            () => _netMsx.QTime / (double)_net.Duration);
                    }
                    catch (IOException) {}
                    catch (ENException) {
                        throw new ThreadInterruptedException();
                    }
                    finally {
                        _epanetTk.Close();
                    }

                    // netMSX.getReport().MSXrpt_write(new "msxFile.bin");
                }

                if (_canselSimulation) return;

                if (qualityCheckBox.Checked) {
                    try {
                        QualitySim qSim = new QualitySim(_net, log);
                        statusLabel.Text = "Simulating Quality";

                        RunThread(
                            () => qSim.Simulate("hydFile.bin", "qualFile.bin"),
                            30,
                            50,
                            () => qSim.Qtime,
                            () => qSim.Qtime / (double)_net.Duration);
                    }
                    catch (ENException ex) {
                        MessageBox.Show(
                            ex.Message + "\nCheck epanet.log for detailed error description",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    catch (IOException e1) {
                        Debug.Print(e1.ToString());
                    }
                }

                if (_canselSimulation) return;

                progressBar.Value = 50;
                statusLabel.Text = "Writting report";

                _gen = new ReportGenerator(fileName);

                //log

                log.Information("Starting report write");
                if (showSummaryCheckBox.Checked) _gen.WriteSummary(_fileInp, _net, _fileMsx, _netMsx);

                if (_canselSimulation) return;

                if (transposeResultsCheckBox.Checked) _gen.TransposedMode = true;

                if (hydraulicsCheckBox.Checked) {
                    // Write hydraulic spreadsheets
                    bool[] values = new bool[ReportGenerator.HydVariable.Values.Length];
                    for (int i = 0; i < ReportGenerator.HydVariable.Values.Length; i++) {
                        values[i] = hydVariables.GetItemChecked(i);
                    }

                    statusLabel.Text = "Writing hydraulic report";

                    RunThread(
                        () => _gen.CreateHydReport("hydFile.bin", _net, values),
                        50,
                        60,
                        () => _gen.Rtime,
                        () => (_gen.Rtime - _net.RStart) / (double)_net.Duration);
                }

                if (_canselSimulation) return;

                if (qualityCheckBox.Checked) {
                    statusLabel.Text = "Writing quality report";

                    bool nodes = qualityVariables.GetItemChecked(0);
                    bool links = qualityVariables.GetItemChecked(1);

                    RunThread(
                        () => _gen.CreateQualReport("qualFile.bin", _net, nodes, links),
                        60,
                        70,
                        () => _gen.Rtime,
                        () => (_gen.Rtime - _net.RStart) / (double)_net.Duration);
                }

                if (_canselSimulation) return;

                // Write MSX quality spreadsheets
                if (_fileMsx != null && qualityMSXCheckBox.Checked) {
                    bool[] valuesMsx = new bool[speciesCheckList.Items.Count];
                    for (int i = 0; i < speciesCheckList.Items.Count; i++) {
                        valuesMsx[i] = speciesCheckList.GetItemChecked(i);
                    }

                    _gen.CreateMsxReport(
                        "msxFile.bin",
                        _net,
                        _netMsx,
                        _epanetTk,
                        valuesMsx);

                    RunThread(
                        () =>
                            _gen.CreateMsxReport(
                                "msxFile.bin",
                                _net,
                                _netMsx,
                                _epanetTk,
                                valuesMsx),
                        70,
                        80,
                        () => _netMsx.QTime,
                        () => (_gen.Rtime - _net.RStart) / (double)_net.Duration);
                }

                if (_canselSimulation) return;

                statusLabel.Text = "Writing workbook";
                _gen.WriteWorksheet();
                log.Information("Ending report write");
            }
            catch (ThreadInterruptedException) {
                log.Warning(0, "Simulation aborted!");
            }
            finally {
                if (simHandler != null) {
                    log.Listeners.Remove(simHandler);
                    simHandler.Close();
                }

                Simulated = false;
            }

        }

        private void RunThread(ThreadStart pts, int start, int end, Func<long> getTime, Func<double> getProgress) {
            string initName = statusLabel.Text;

            var thr = new Thread(pts);

            thr.Start();

            while (thr.IsAlive) {
                Debug.Print("Thresd state = {0}", thr.ThreadState);
                Thread.Sleep(200);

                if (_canselSimulation) {
                    thr.Abort();
                    break;
                }

                long time = getTime();
                var value = getProgress();

                if (time != 0) statusLabel.Text = string.Format("{0} ({1})", initName, time.GetClockTime());

                progressBar.Value = (int)(start * (1.0f - value) + end * value);
                Application.DoEvents();

                if (getProgress() > 0.9) break;

                if (_canselSimulation) break;

            }

            try {
                thr.Join();
            }
            catch (ThreadAbortException) {}

        }

        #endregion


        /// <summary>Public enum to set the simulation time step duration.</summary>
        private struct TimeStep {

            public static readonly TimeStep[] Values = {
                new TimeStep(60, "1 minute"),
                new TimeStep(120, "2 minutes"),
                new TimeStep(180, "3 minutes"),
                new TimeStep(240, "4 minutes"),
                new TimeStep(300, "5 minutes"),
                new TimeStep(600, "10 minutes"),
                new TimeStep(900, "15 minutes"),
                new TimeStep(1800, "30 minutes"),
                new TimeStep(3600, "1 hour"),
                new TimeStep(7200, "2 hours"),
                new TimeStep(14400, "4 hours"),
                new TimeStep(21600, "6 hours"),
                new TimeStep(43200, "12 hours")
            };

            private TimeStep(int time, string name) {
                _time = time;
                _name = name;
            }

            /// <summary>Entry timestep duration.</summary>
            public int Time { get { return _time; } }

            /// <summary>Entry name</summary>
            private readonly string _name;

            private readonly int _time;

            /// <summary>Get the nearest timestep period.</summary>
            /// <returns>Nearest timestep, if the time is bigger than any timestep returns STEP_12_HOURS.</returns>
            public static int GetNearestStep(long time) {
                for (int i = 0; i < Values.Length; i++) {
                    TimeStep step = Values[i];
                    if (step.Time >= time) return i;
                }
                return Values.Length - 1;
            }

            public override string ToString() { return _name; }

        }

    }

}
