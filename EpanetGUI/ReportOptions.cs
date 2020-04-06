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
using Epanet.Network.IO.Input;
using Epanet.Quality;
using Epanet.Util;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet {

    /// <summary>Configures and executes the epanet (Hydraulic/Quality) simulation through the UI.</summary>
    public sealed partial class ReportOptions : Form {

        /// <summary>Epanet network config file.</summary>
        private readonly string _fileInp;

        private static readonly TraceSource log = new TraceSource("epanet", SourceLevels.All);

        /// <summary>Loaded INP network.</summary>
        private readonly EpanetNetwork _net;

        /// <summary>Hydraulic simulator.</summary>
        private HydraulicSim _hydSim;

        private ReportOptions() {
            InitializeComponent();

            unitsBox.Items.AddRange(new object[] { UnitsType.SI, UnitsType.US});

            reportPeriodBox.DataSource = TimeStep.Values;
            hydComboBox.DataSource = TimeStep.Values;
            qualComboBox.DataSource = TimeStep.Values;

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
        public ReportOptions(string inpFile) : this() {
            

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
            catch (EnException ex) {
                MessageBox.Show(
                    ex.Message + "\nCheck epanet.log for detailed error description",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

        }

        /// <summary>Create and show the window.</summary>
        private void ReportOptions_Load(object sender, EventArgs e) {
            // Adjust widgets before showing the window.

            if (_net != null) {
                unitsBox.SelectedIndex = (int)_net.UnitsFlag;
                reportPeriodBox.SelectedIndex = TimeStep.GetNearestStep(_net.RStep);
                hydComboBox.SelectedIndex = TimeStep.GetNearestStep(_net.HStep);
                qualComboBox.SelectedIndex = TimeStep.GetNearestStep(_net.QStep);
                textSimulationDuration.Text = _net.Duration.GetClockTime();
                textReportStart.Text = _net.RStart.GetClockTime();
                qualityCheckBox.Enabled = _net.QualFlag != QualType.NONE;
            }

            Visible = true;
            cancelButton.Visible = true;
            runButton.Visible = true;
            UnlockInterface();
        }

        private void hydraulicsCheckBox_CheckedChanged(object sender, EventArgs e) {
            hydVariables.Enabled = hydraulicsCheckBox.Checked;
        }

        private void qualityCheckBox_CheckedChanged(object sender, EventArgs e) {
            qualityVariables.Enabled = qualityCheckBox.Checked;
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            //this.Close();
            Visible = false;
        }

        private void hydComboBox_SelectedIndexChanged(object sender, EventArgs eventArgs) {
            if (reportPeriodBox.SelectedIndex < hydComboBox.SelectedIndex) 
                reportPeriodBox.SelectedIndex = hydComboBox.SelectedIndex;
        }

        private void reportPeriodBox_SelectedIndexChanged(object sender, EventArgs e) {
            if (reportPeriodBox.SelectedIndex < hydComboBox.SelectedIndex)
                reportPeriodBox.SelectedIndex = hydComboBox.SelectedIndex;
        }

        private void Timespan_Validating(object sender, CancelEventArgs e) {
            TextBox box = sender as TextBox;
            if (box == null) return;

            double val = Utilities.GetHour(box.Text);
            if (val < 0) {
                errorProvider.SetError(box, "Invalid value"); 
                e.Cancel = true;
            }
            else {
                errorProvider.Clear();
            }
        }

        /// <summary>Lock the interface during the simulation.</summary>
        private void LockInterface() {
            hydraulicsCheckBox.Enabled = false;
            qualityCheckBox.Enabled = false;
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
            runButton.Enabled = false;
        }

        /// <summary>Unlock the interface during the simulation.</summary>
        private void UnlockInterface() {
            hydraulicsCheckBox.Enabled = true;
            transposeResultsCheckBox.Enabled = true;

            hydVariables.Enabled = true;
            
            if (_net.QualFlag != QualType.NONE) {
                if (qualityCheckBox.Checked) {
                    qualityVariables.Enabled = true;
                    qualityCheckBox.Enabled = true;
                }
            }

            showSummaryCheckBox.Enabled = true;
            showHydraulicSolverEventsCheckBox.Enabled = true;
            reportPeriodBox.Enabled = true;
            unitsBox.Enabled = true;
            hydComboBox.Enabled = true;
            textSimulationDuration.Enabled = true;
            qualComboBox.Enabled = true;
            textReportStart.Enabled = true;
            runButton.Enabled = true;
            statusLabel.Text = "Idle";
            progressBar.Value = 0;
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
                Filter = "Excel 2007 file|*.xlsx",
                FileName = Path.GetFileNameWithoutExtension(_fileInp)
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
            get => _simulated;
            set {
                _simulated = value;

                if (value) {
                    LockInterface();
                }
                else {
                    UnlockInterface();
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

                _net.RStart = Utilities.ToTimeSpan(textReportStart.Text);
                _net.RStep = ((TimeStep)reportPeriodBox.SelectedItem).Time;
                _net.HStep = ((TimeStep)hydComboBox.SelectedItem).Time;
                _net.Duration = Utilities.ToTimeSpan(textSimulationDuration.Text);
                _net.QStep = ((TimeStep)qualComboBox.SelectedItem).Time;
                _net.UnitsFlag = (UnitsType)unitsBox.SelectedItem;

                statusLabel.Text = "Simulating hydraulics";

                try {
                    _hydSim = new HydraulicSim(_net, log);

                    RunThread(
                        () => _hydSim.Simulate("hydFile.bin"),
                        10,
                        30,
                        () => _hydSim.Htime,
                        () => _hydSim.Htime.TotalSeconds / (double)_net.Duration.TotalSeconds);
                }
                catch (EnException ex) {
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

                if (qualityCheckBox.Checked) {
                    try {
                        QualitySim qSim = new QualitySim(_net, log);
                        statusLabel.Text = "Simulating Quality";

                        RunThread(
                            () => qSim.Simulate("hydFile.bin", "qualFile.bin"),
                            30,
                            50,
                            () => qSim.Qtime,
                            () => qSim.Qtime.TotalSeconds / _net.Duration.TotalSeconds);
                    }
                    catch (EnException ex) {
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
                if (showSummaryCheckBox.Checked) _gen.WriteSummary(_fileInp, _net);

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
                        () => (_gen.Rtime - _net.RStart).Ticks / (double)_net.Duration.Ticks);
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
                        () => (_gen.Rtime - _net.RStart).Ticks / (double)_net.Duration.Ticks);
                }

                if (_canselSimulation) return;
                
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

        private void RunThread(ThreadStart pts, int start, int end, Func<TimeSpan> getTime, Func<double> getProgress) {

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

                TimeSpan time = getTime();
                var value = getProgress();

                if (!time.IsZero())
                    statusLabel.Text = string.Format("{0} ({1})", initName, time.GetClockTime());

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
                new TimeStep(TimeSpan.FromMinutes(1), "1 minute"),
                new TimeStep(TimeSpan.FromMinutes(2), "2 minutes"),
                new TimeStep(TimeSpan.FromMinutes(3), "3 minutes"),
                new TimeStep(TimeSpan.FromMinutes(4), "4 minutes"),
                new TimeStep(TimeSpan.FromMinutes(5), "5 minutes"),
                new TimeStep(TimeSpan.FromMinutes(10), "10 minutes"),
                new TimeStep(TimeSpan.FromMinutes(15), "15 minutes"),
                new TimeStep(TimeSpan.FromMinutes(30), "30 minutes"),
                new TimeStep(TimeSpan.FromHours(1), "1 hour"),
                new TimeStep(TimeSpan.FromHours(2), "2 hours"),
                new TimeStep(TimeSpan.FromHours(4), "4 hours"),
                new TimeStep(TimeSpan.FromHours(6), "6 hours"),
                new TimeStep(TimeSpan.FromHours(12), "12 hours")
            };

            private TimeStep(TimeSpan time, string name) {
                Time = time;
                _name = name;
            }

            /// <summary>Entry timestep duration.</summary>
            public TimeSpan Time { get; }

            /// <summary>Entry name</summary>
            private readonly string _name;

            /// <summary>Get the nearest timestep period.</summary>
            /// <returns>Nearest timestep, if the time is bigger than any timestep returns STEP_12_HOURS.</returns>
            public static int GetNearestStep(TimeSpan time) {
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
