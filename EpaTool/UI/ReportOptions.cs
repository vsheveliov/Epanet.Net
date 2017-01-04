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
        private readonly ENToolkit2 epanetTk;

        /// <summary>Epanet network config file.</summary>
        private readonly string fileInp;

        /// <summary>MSX config file.</summary>
        private readonly string fileMsx;

        private readonly TraceSource log;

        /// <summary>Loaded INP network.</summary>
        private readonly EpanetNetwork netInp;

        /// <summary>Hydraulic simulator.</summary>
        private HydraulicSim hydSim;

        /// <summary>Loaded MSX simulation.</summary>
        private EpanetMSX netMsx;

        private ReportOptions() {
            this.InitializeComponent();

            this.unitsBox.Items.AddRange(new object[] {"SI", "US"});

            var periods = Array.ConvertAll(TimeStep.Values, x => (object)x);
            this.reportPeriodBox.Items.AddRange(periods);
            this.hydComboBox.Items.AddRange(periods);
            this.qualComboBox.Items.AddRange(periods);

            this.hydVariables.Items.AddRange(Array.ConvertAll(ReportGenerator.HydVariable.Values, x => (object)x));

            for (int i = 0; i < this.hydVariables.Items.Count; i++) {
                this.hydVariables.SetItemChecked(i, true);
            }

            this.qualityVariables.Items.AddRange(Array.ConvertAll(ReportGenerator.QualVariable.Values, x => (object)x));

            for (int i = 0; i < this.qualityVariables.Items.Count; i++) {
                this.qualityVariables.SetItemChecked(i, true);
            }
        }

        /// <summary>Report options dialog constructor.</summary>
        public ReportOptions(string inpFile, string msxFile, TraceSource log) : this() {
            this.log = log;

            if (inpFile == null) return;

            this.fileInp = inpFile;
            this.netInp = new EpanetNetwork();

            try {
                InputParser inpParser;

                string extension = Path.GetExtension(inpFile);

                switch (extension.ToLowerInvariant()) {
                    case  ".xlsx":
                        inpParser = new ExcelParser(log);
                        break;
#if false
                    // TODO: implement this
                    case ".xml" :
                        inpParser = new XmlParser(log, false);
                        break;
                    case ".gz":
                        inpParser = new XmlParser(log, true);
                        break;
#endif
                    case ".inp":
                        inpParser = new InpParser(log);
                        break;
                    default:
                        inpParser = new InpParser(log);
                        break;
                }

                inpParser.Parse(this.netInp, inpFile);
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

            this.fileMsx = msxFile;
            this.epanetTk = new ENToolkit2(this.netInp);
            this.netMsx = new EpanetMSX(this.epanetTk);

            try {
                ErrorCodeType ret = this.netMsx.Load(this.fileMsx);
                if (ret != 0) {
                    MessageBox.Show("MSX parsing error " + ret, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.fileMsx = null;
                    this.netMsx = null;
                    this.epanetTk = null;
                }
            }
            catch (IOException) {
                MessageBox.Show(
                    "IO error while reading the MSX file",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                this.fileMsx = null;
                this.netMsx = null;
                this.epanetTk = null;
            }
        }

        private void ReportOptions_Closing(object sender, FormClosingEventArgs e) {
            if (this.hydSim != null) {
                try {
                    this.hydSim.StopRunning();
                }
                catch (ThreadInterruptedException ex) {
                    Debug.Print(ex.ToString());
                }
            }

            if (this.netMsx != null) {
                try {
                    this.netMsx.StopRunning();
                }
                catch (ThreadInterruptedException e1) {
                    Debug.Print(e1.ToString());
                }
            }
        }

        /// <summary>Create and show the window.</summary>
        private void ReportOptions_Load(object sender, EventArgs e) {
            // Adjust widgets before showing the window.

            if (this.netInp != null) {
                try {
                    this.unitsBox.SelectedIndex = this.netInp.UnitsFlag == UnitsType.SI ? 0 : 1;
                    this.reportPeriodBox.SelectedIndex = TimeStep.GetNearestStep(this.netInp.RStep);
                    this.hydComboBox.SelectedIndex = TimeStep.GetNearestStep(this.netInp.HStep);
                    this.qualComboBox.SelectedIndex = TimeStep.GetNearestStep(this.netInp.QStep);
                    this.textSimulationDuration.Text = this.netInp.Duration.GetClockTime();
                    this.textReportStart.Text = this.netInp.RStart.GetClockTime();
                    this.qualityCheckBox.Enabled = this.netInp.QualFlag != QualType.NONE;
                }
                catch (ENException ex) {
                    Debug.Print(ex.ToString());
                }
            }

            if (this.netMsx != null && this.netMsx.GetSpeciesNames().Length > 0) {
                var speciesNames = Array.ConvertAll(this.netMsx.GetSpeciesNames(), x => (object)x);
                this.speciesCheckList.Items.AddRange(speciesNames);
                //this.speciesCheckList.SelectionMode = SelectionMode.MultiExtended; // TODO: in designer
            }
            else {
                this.qualityMSXCheckBox.Enabled = false;
            }

            this.Visible = true;
            this.progressPanel.Visible = false;
            this.actions.Visible = true;
            this.UnlockInterface();
        }

        private void hydraulicsCheckBox_CheckedChanged(object sender, EventArgs e) {
            this.hydVariables.Enabled = this.hydraulicsCheckBox.Checked;
        }

        private void qualityCheckBox_CheckedChanged(object sender, EventArgs e) {
            this.qualityVariables.Enabled = this.qualityCheckBox.Checked;
        }

        private void qualityMSXCheckBox_CheckedChanged(object sender, EventArgs e) {
            this.speciesCheckList.Enabled = this.qualityMSXCheckBox.Checked;
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            //this.Close();
            this.Visible = false;
        }

        private void hydComboBox_SelectedIndexChanged(object sender, EventArgs eventArgs) {
            if (this.reportPeriodBox.SelectedIndex < this.hydComboBox.SelectedIndex) this.reportPeriodBox.SelectedIndex = this.hydComboBox.SelectedIndex;
        }

        private void reportPeriodBox_SelectedIndexChanged(object sender, EventArgs e) {
            if (this.reportPeriodBox.SelectedIndex < this.hydComboBox.SelectedIndex) this.reportPeriodBox.SelectedIndex = this.hydComboBox.SelectedIndex;
        }

        private void textReportStart_Validating(object sender, CancelEventArgs e) {
            double val = Utilities.GetHour(this.textReportStart.Text);
            if (double.IsNaN(val)) e.Cancel = true;
        }

        private void textSimulationDuration_Validating(object sender, CancelEventArgs e) {
            double val = Utilities.GetHour(this.textSimulationDuration.Text);
            if (double.IsNaN(val)) e.Cancel = true;
        }

        /// <summary>Lock the interface during the simulation.</summary>
        private void LockInterface() {
            this.hydraulicsCheckBox.Enabled = false;
            this.qualityCheckBox.Enabled = false;
            this.qualityMSXCheckBox.Enabled = false;
            this.hydVariables.Enabled = false;
            this.qualityVariables.Enabled = false;
            this.speciesCheckList.Enabled = false;
            this.showSummaryCheckBox.Enabled = false;
            this.showHydraulicSolverEventsCheckBox.Enabled = false;
            this.reportPeriodBox.Enabled = false;
            this.unitsBox.Enabled = false;
            this.hydComboBox.Enabled = false;
            this.textSimulationDuration.Enabled = false;
            this.qualComboBox.Enabled = false;
            this.textReportStart.Enabled = false;
            this.transposeResultsCheckBox.Enabled = false;
        }

        /// <summary>Unlock the interface during the simulation.</summary>
        private void UnlockInterface() {
            this.hydraulicsCheckBox.Enabled = true;
            this.transposeResultsCheckBox.Enabled = true;

            if (this.netMsx != null && this.netMsx.GetSpeciesNames().Length > 0) {
                this.qualityMSXCheckBox.Enabled = true;
                if (this.qualityMSXCheckBox.Checked) this.speciesCheckList.Enabled = true;
            }

            this.hydVariables.Enabled = true;
            try {
                if (this.netInp.QualFlag != QualType.NONE) {
                    if (this.qualityCheckBox.Checked) {
                        this.qualityVariables.Enabled = true;
                        this.qualityCheckBox.Enabled = true;
                    }
                }
            }
            catch (ENException e) {
                Debug.Print(e.ToString());
            }

            this.showSummaryCheckBox.Enabled = true;
            this.showHydraulicSolverEventsCheckBox.Enabled = true;
            this.reportPeriodBox.Enabled = true;
            this.unitsBox.Enabled = true;
            this.hydComboBox.Enabled = true;
            this.textSimulationDuration.Enabled = true;
            this.qualComboBox.Enabled = true;
            this.textReportStart.Enabled = true;
        }


        private void runButton_Click(object sender, EventArgs e) {

            if (this.Simulated) {
                MessageBox.Show("Simulation in progress! Can not start another one.");
                return;
            }

            if (Utilities.GetHour(this.textSimulationDuration.Text) < 0) {
                MessageBox.Show(
                    "Invalid time expression for simulation duration",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (Utilities.GetHour(this.textReportStart.Text) < 0) {
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
                FileName = "report_" + Path.GetFileNameWithoutExtension(this.fileInp)
            };

            if (fdialog.ShowDialog() != DialogResult.OK) return;

            this.RunSimulation(fdialog.FileName);

            this.Visible = false;
        }

        private bool simulated;

        private bool Simulated {
            get { return this.simulated; }
            set {
                this.simulated = value;

                if (value) {
                    this.LockInterface();
                    this.progressPanel.Visible = true;
                    this.runButton.Enabled = false;
                }
                else {
                    this.UnlockInterface();
                    this.progressPanel.Visible = false;
                    this.runButton.Enabled = true;
                }
            }
        }

        private bool canselSimulation;


        #region threading

        private ReportGenerator gen;

        private void RunSimulation(string fileName) {
            TraceListener simHandler = null;

            this.Simulated = true;
            this.canselSimulation = false;

            this.progressBar.Value = 0;
            this.progressBar.Maximum = 100;
            this.progressBar.Minimum = 0;

            try {

                if (this.showHydraulicSolverEventsCheckBox.Checked) {

                    string logFile = Path.Combine(Path.GetDirectoryName(fileName) ?? string.Empty, "hydEvents.log");

                    try {
                        simHandler = new EpanetTraceListener(logFile, false, logFile);
                        simHandler.TraceOutputOptions &= ~TraceOptions.DateTime;
                        this.log.Listeners.Add(simHandler);
                    }
                    catch (IOException ex) {
                        this.log.Error(ex);
                    }
                }

                int reportPeriod = ((TimeStep)this.reportPeriodBox.SelectedItem).Time;
                int reportStartTime = (int)(Utilities.GetHour(this.textReportStart.Text) * 3600);
                int hydTStep = ((TimeStep)this.hydComboBox.SelectedItem).Time;
                int qualTStep = ((TimeStep)this.qualComboBox.SelectedItem).Time;
                int durationTime = (int)(Utilities.GetHour(this.textSimulationDuration.Text) * 3600);

                this.netInp.RStart = reportStartTime;
                this.netInp.RStep = reportPeriod;
                this.netInp.HStep = hydTStep;
                this.netInp.Duration = durationTime;
                this.netInp.QStep = qualTStep;

                this.statusLabel.Text = "Simulating hydraulics";

                try {

                    this.hydSim = new HydraulicSim(this.netInp, this.log);

                    this.RunThread(
                        () => this.hydSim.Simulate("hydFile.bin"),
                        10,
                        30,
                        () => this.hydSim.Htime,
                        () => this.hydSim.Htime / (double)this.netInp.Duration);
                }
                catch (ENException ex) {
                    if (ex.getCodeID() == ErrorCode.Err1000)
                        throw new ThreadInterruptedException();

                    MessageBox.Show(
                        ex.Message + "\nCheck epanet.log for detailed error description",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                if (this.canselSimulation) return;


                if (this.fileMsx != null && this.qualityMSXCheckBox.Checked) {

                    this.statusLabel.Text = "Simulating MSX";

                    try {
                        // reload MSX
                        this.netMsx = new EpanetMSX(this.epanetTk);
                        this.netMsx.Load(this.fileMsx);

                        this.netMsx.Network.Rstep = reportPeriod;
                        this.netMsx.Network.Qstep = qualTStep;
                        this.netMsx.Network.Rstart = reportStartTime;
                        this.netMsx.Network.Dur = durationTime;

                        this.epanetTk.Open("hydFile.bin");

                        this.RunThread(
                            () => this.netMsx.Run("msxFile.bin"),
                            30,
                            50,
                            () => this.netMsx.QTime,
                            () => this.netMsx.QTime / (double)this.netInp.Duration);
                    }
                    catch (IOException) {}
                    catch (ENException) {
                        throw new ThreadInterruptedException();
                    }
                    finally {
                        this.epanetTk.Close();
                    }

                    // netMSX.getReport().MSXrpt_write(new "msxFile.bin");
                }

                if (this.canselSimulation) return;

                if (this.qualityCheckBox.Checked) {
                    try {
                        QualitySim qSim = new QualitySim(this.netInp, this.log);
                        this.statusLabel.Text = "Simulating Quality";

                        this.RunThread(
                            () => qSim.Simulate("hydFile.bin", "qualFile.bin"),
                            30,
                            50,
                            () => qSim.Qtime,
                            () => qSim.Qtime / (double)this.netInp.Duration);

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

                if (this.canselSimulation) return;

                this.progressBar.Value = 50;
                this.statusLabel.Text = "Writting XLSX";

                this.gen = new ReportGenerator(fileName);

                //log
                try {
                    this.log.Information("Starting xlsx write");
                    if (this.showSummaryCheckBox.Checked) this.gen.WriteSummary(this.fileInp, this.netInp, this.fileMsx, this.netMsx);

                    if (this.canselSimulation) return;

                    if (this.transposeResultsCheckBox.Checked) this.gen.TransposedMode = true;

                    if (this.hydraulicsCheckBox.Checked) {
                        // Write hydraulic spreadsheets
                        bool[] values = new bool[ReportGenerator.HydVariable.Values.Length];
                        for (int i = 0; i < ReportGenerator.HydVariable.Values.Length; i++) {
                            values[i] = this.hydVariables.GetItemChecked(i);
                        }



                        this.statusLabel.Text = "Writing hydraulic report";

                        this.RunThread(
                            () => this.gen.CreateHydReport("hydFile.bin", this.netInp, values),
                            50,
                            60,
                            () => this.gen.Rtime,
                            () => (this.gen.Rtime - this.netInp.RStart) / (double)this.netInp.Duration);
                    }

                    if (this.canselSimulation) return;

                    if (this.qualityCheckBox.Checked) {
                        this.statusLabel.Text = "Writing quality report";

                        bool nodes = this.qualityVariables.GetItemChecked(0);
                        bool links = this.qualityVariables.GetItemChecked(1);

                        this.RunThread(
                            () => this.gen.CreateQualReport("qualFile.bin", this.netInp, nodes, links),
                            60,
                            70,
                            () => this.gen.Rtime,
                            () => (this.gen.Rtime - this.netInp.RStart) / (double)this.netInp.Duration);
                    }

                    if (this.canselSimulation) return;

                    // Write MSX quality spreadsheets
                    if (this.fileMsx != null && this.qualityMSXCheckBox.Checked) {
                        bool[] valuesMSX = new bool[this.speciesCheckList.Items.Count];
                        for (int i = 0; i < this.speciesCheckList.Items.Count; i++) {
                            valuesMSX[i] = this.speciesCheckList.GetItemChecked(i);
                        }

                        this.gen.createMSXReport(
                            "msxFile.bin",
                            this.netInp,
                            this.netMsx,
                            this.epanetTk,
                            valuesMSX);

                        this.RunThread(
                            () =>
                                this.gen.createMSXReport(
                                    "msxFile.bin",
                                    this.netInp,
                                    this.netMsx,
                                    this.epanetTk,
                                    valuesMSX),
                            70,
                            80,
                            () => this.netMsx.QTime,
                            () => (this.gen.Rtime - this.netInp.RStart) / (double)this.netInp.Duration);

                    }

                    if (this.canselSimulation) return;

                    this.statusLabel.Text = "Writing workbook";
                    this.gen.writeWorksheet();
                    this.log.Information("Ending xlsx write");
                }
                catch (IOException ex) {
                    Debug.Print(ex.ToString());
                }
                catch (ENException ex) {
                    Debug.Print(ex.ToString());
                }
            }
            catch (ThreadInterruptedException) {
                this.log.Warning(0, "Simulation aborted!");
            }
            finally {
                if (simHandler != null) {
                    this.log.Listeners.Remove(simHandler);
                    simHandler.Close();
                }

                this.Simulated = false;
            }

        }

        private void RunThread(ThreadStart pts, int start, int end, Func<long> getTime, Func<double> getProgress) {
            string initName = this.statusLabel.Text;

            var thr = new Thread(pts);

            thr.Start();

            while (thr.IsAlive) {
                Debug.Print("Thresd state = {0}", thr.ThreadState);
                Thread.Sleep(200);

                if (this.canselSimulation) {
                    thr.Abort();
                    break;
                }

                long time = getTime();
                var value = getProgress();

                if (time != 0) this.statusLabel.Text = string.Format("{0} ({1})", initName, time.GetClockTime());

                this.progressBar.Value = (int)(start * (1.0f - value) + end * value);
                Application.DoEvents();

                if (getProgress() > 0.9) break;

                if (this.canselSimulation) break;

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
                this.time = time;
                this.name = name;
            }

            /// <summary>Entry timestep duration.</summary>
            public int Time { get { return this.time; } }

            /// <summary>Entry name</summary>
            private readonly string name;

            private readonly int time;

            /// <summary>Get the nearest timestep period.</summary>
            /// <returns>Nearest timestep, if the time is bigger than any timestep returns STEP_12_HOURS.</returns>
            public static int GetNearestStep(long time) {
                for (int i = 0; i < Values.Length; i++) {
                    TimeStep step = Values[i];
                    if (step.Time >= time) return i;
                }
                return Values.Length - 1;
            }

            public override string ToString() { return this.name; }

        }

    }

}
