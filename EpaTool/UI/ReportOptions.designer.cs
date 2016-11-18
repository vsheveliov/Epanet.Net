namespace Epanet.UI {
    sealed partial class ReportOptions {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (this.components != null)) {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.hydraulicsCheckBox = new System.Windows.Forms.CheckBox();
            this.qualityMSXCheckBox = new System.Windows.Forms.CheckBox();
            this.qualityCheckBox = new System.Windows.Forms.CheckBox();
            this.hydPanel = new System.Windows.Forms.Panel();
            this.hydVariables = new System.Windows.Forms.CheckedListBox();
            this.speciesCheckList = new System.Windows.Forms.CheckedListBox();
            this.qualityPanel = new System.Windows.Forms.Panel();
            this.qualityVariables = new System.Windows.Forms.CheckedListBox();
            this.showSummaryCheckBox = new System.Windows.Forms.CheckBox();
            this.showHydraulicSolverEventsCheckBox = new System.Windows.Forms.CheckBox();
            this.transposeResultsCheckBox = new System.Windows.Forms.CheckBox();
            this.runButton = new System.Windows.Forms.Button();
            this.reportOptions2 = new System.Windows.Forms.Panel();
            this.qualComboBox = new System.Windows.Forms.ComboBox();
            this.reportPeriodBox = new System.Windows.Forms.ComboBox();
            this.unitsBox = new System.Windows.Forms.ComboBox();
            this.hydComboBox = new System.Windows.Forms.ComboBox();
            this.textSimulationDuration = new System.Windows.Forms.TextBox();
            this.textReportStart = new System.Windows.Forms.TextBox();
            this.dtSimulationDuration = new System.Windows.Forms.DateTimePicker();
            this.actions = new System.Windows.Forms.Panel();
            this.cancelButton = new System.Windows.Forms.Button();
            this.top = new System.Windows.Forms.TableLayoutPanel();
            this.qualityMSXPanel = new System.Windows.Forms.Panel();
            this.progressPanel = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.hydPanel.SuspendLayout();
            this.qualityPanel.SuspendLayout();
            this.reportOptions2.SuspendLayout();
            this.actions.SuspendLayout();
            this.top.SuspendLayout();
            this.qualityMSXPanel.SuspendLayout();
            this.progressPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 46);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(95, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Reporting timestep";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(233, 77);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(31, 13);
            this.label2.TabIndex = 17;
            this.label2.Text = "Units";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 15);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(93, 13);
            this.label3.TabIndex = 18;
            this.label3.Text = "Hydraulic timestep";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(233, 15);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(47, 13);
            this.label4.TabIndex = 28;
            this.label4.Text = "Duration";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(233, 46);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(62, 13);
            this.label6.TabIndex = 30;
            this.label6.Text = "Report start";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Dock = System.Windows.Forms.DockStyle.Top;
            this.label7.Location = new System.Drawing.Point(0, 0);
            this.label7.Name = "label7";
            this.label7.Padding = new System.Windows.Forms.Padding(2);
            this.label7.Size = new System.Drawing.Size(453, 17);
            this.label7.TabIndex = 24;
            this.label7.Text = "Running the simulation will generate a .xlsx workbook containing one result sheet" +
    " per variable.";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Dock = System.Windows.Forms.DockStyle.Top;
            this.label8.Location = new System.Drawing.Point(0, 17);
            this.label8.Name = "label8";
            this.label8.Padding = new System.Windows.Forms.Padding(2);
            this.label8.Size = new System.Drawing.Size(530, 17);
            this.label8.TabIndex = 25;
            this.label8.Text = "Each result worksheet displays the variable\'s values at all nodes or links, for a" +
    "ll reporting timesteps as selected.";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 77);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(81, 13);
            this.label5.TabIndex = 0;
            this.label5.Text = "Quality timestep";
            // 
            // hydraulicsCheckBox
            // 
            this.hydraulicsCheckBox.Checked = true;
            this.hydraulicsCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.hydraulicsCheckBox.Location = new System.Drawing.Point(6, 6);
            this.hydraulicsCheckBox.Name = "hydraulicsCheckBox";
            this.hydraulicsCheckBox.Size = new System.Drawing.Size(120, 18);
            this.hydraulicsCheckBox.TabIndex = 2;
            this.hydraulicsCheckBox.Text = "Hydraulics";
            this.hydraulicsCheckBox.CheckedChanged += new System.EventHandler(this.hydraulicsCheckBox_CheckedChanged);
            // 
            // qualityMSXCheckBox
            // 
            this.qualityMSXCheckBox.Location = new System.Drawing.Point(3, 6);
            this.qualityMSXCheckBox.Name = "qualityMSXCheckBox";
            this.qualityMSXCheckBox.Size = new System.Drawing.Size(120, 18);
            this.qualityMSXCheckBox.TabIndex = 0;
            this.qualityMSXCheckBox.Text = "Quality MSX";
            this.qualityMSXCheckBox.CheckedChanged += new System.EventHandler(this.qualityMSXCheckBox_CheckedChanged);
            // 
            // qualityCheckBox
            // 
            this.qualityCheckBox.Location = new System.Drawing.Point(6, 6);
            this.qualityCheckBox.Name = "qualityCheckBox";
            this.qualityCheckBox.Size = new System.Drawing.Size(120, 18);
            this.qualityCheckBox.TabIndex = 3;
            this.qualityCheckBox.Text = "Quality";
            this.qualityCheckBox.CheckedChanged += new System.EventHandler(this.qualityCheckBox_CheckedChanged);
            // 
            // hydPanel
            // 
            this.hydPanel.Controls.Add(this.hydraulicsCheckBox);
            this.hydPanel.Controls.Add(this.hydVariables);
            this.hydPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.hydPanel.Location = new System.Drawing.Point(3, 3);
            this.hydPanel.Name = "hydPanel";
            this.hydPanel.Size = new System.Drawing.Size(194, 245);
            this.hydPanel.TabIndex = 13;
            // 
            // hydVariables
            // 
            this.hydVariables.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.hydVariables.Location = new System.Drawing.Point(0, 31);
            this.hydVariables.Name = "hydVariables";
            this.hydVariables.Size = new System.Drawing.Size(194, 214);
            this.hydVariables.TabIndex = 0;
            // 
            // speciesCheckList
            // 
            this.speciesCheckList.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.speciesCheckList.Enabled = false;
            this.speciesCheckList.Location = new System.Drawing.Point(0, 31);
            this.speciesCheckList.Name = "speciesCheckList";
            this.speciesCheckList.Size = new System.Drawing.Size(219, 214);
            this.speciesCheckList.TabIndex = 1;
            // 
            // qualityPanel
            // 
            this.qualityPanel.Controls.Add(this.qualityCheckBox);
            this.qualityPanel.Controls.Add(this.qualityVariables);
            this.qualityPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.qualityPanel.Location = new System.Drawing.Point(203, 3);
            this.qualityPanel.Name = "qualityPanel";
            this.qualityPanel.Size = new System.Drawing.Size(194, 245);
            this.qualityPanel.TabIndex = 14;
            // 
            // qualityVariables
            // 
            this.qualityVariables.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.qualityVariables.Enabled = false;
            this.qualityVariables.Location = new System.Drawing.Point(0, 31);
            this.qualityVariables.Name = "qualityVariables";
            this.qualityVariables.Size = new System.Drawing.Size(194, 214);
            this.qualityVariables.TabIndex = 0;
            // 
            // showSummaryCheckBox
            // 
            this.showSummaryCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.showSummaryCheckBox.AutoSize = true;
            this.showSummaryCheckBox.Location = new System.Drawing.Point(446, 13);
            this.showSummaryCheckBox.Name = "showSummaryCheckBox";
            this.showSummaryCheckBox.Size = new System.Drawing.Size(97, 17);
            this.showSummaryCheckBox.TabIndex = 7;
            this.showSummaryCheckBox.Text = "Show summary";
            // 
            // showHydraulicSolverEventsCheckBox
            // 
            this.showHydraulicSolverEventsCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.showHydraulicSolverEventsCheckBox.AutoSize = true;
            this.showHydraulicSolverEventsCheckBox.Location = new System.Drawing.Point(446, 44);
            this.showHydraulicSolverEventsCheckBox.Name = "showHydraulicSolverEventsCheckBox";
            this.showHydraulicSolverEventsCheckBox.Size = new System.Drawing.Size(164, 17);
            this.showHydraulicSolverEventsCheckBox.TabIndex = 8;
            this.showHydraulicSolverEventsCheckBox.Text = "Show hydraulic solver events";
            // 
            // transposeResultsCheckBox
            // 
            this.transposeResultsCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.transposeResultsCheckBox.AutoSize = true;
            this.transposeResultsCheckBox.Location = new System.Drawing.Point(446, 75);
            this.transposeResultsCheckBox.Name = "transposeResultsCheckBox";
            this.transposeResultsCheckBox.Size = new System.Drawing.Size(114, 17);
            this.transposeResultsCheckBox.TabIndex = 29;
            this.transposeResultsCheckBox.Text = "Transpose Results";
            // 
            // runButton
            // 
            this.runButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.runButton.Location = new System.Drawing.Point(547, 8);
            this.runButton.Name = "runButton";
            this.runButton.Size = new System.Drawing.Size(75, 23);
            this.runButton.TabIndex = 9;
            this.runButton.Text = "Run";
            this.runButton.Click += new System.EventHandler(this.runButton_Click);
            // 
            // reportOptions2
            // 
            this.reportOptions2.Controls.Add(this.transposeResultsCheckBox);
            this.reportOptions2.Controls.Add(this.showHydraulicSolverEventsCheckBox);
            this.reportOptions2.Controls.Add(this.showSummaryCheckBox);
            this.reportOptions2.Controls.Add(this.label6);
            this.reportOptions2.Controls.Add(this.label4);
            this.reportOptions2.Controls.Add(this.label3);
            this.reportOptions2.Controls.Add(this.label2);
            this.reportOptions2.Controls.Add(this.label5);
            this.reportOptions2.Controls.Add(this.label1);
            this.reportOptions2.Controls.Add(this.qualComboBox);
            this.reportOptions2.Controls.Add(this.reportPeriodBox);
            this.reportOptions2.Controls.Add(this.unitsBox);
            this.reportOptions2.Controls.Add(this.hydComboBox);
            this.reportOptions2.Controls.Add(this.textSimulationDuration);
            this.reportOptions2.Controls.Add(this.textReportStart);
            this.reportOptions2.Dock = System.Windows.Forms.DockStyle.Top;
            this.reportOptions2.Location = new System.Drawing.Point(0, 285);
            this.reportOptions2.Name = "reportOptions2";
            this.reportOptions2.Size = new System.Drawing.Size(625, 100);
            this.reportOptions2.TabIndex = 2;
            // 
            // qualComboBox
            // 
            this.qualComboBox.Location = new System.Drawing.Point(104, 73);
            this.qualComboBox.Name = "qualComboBox";
            this.qualComboBox.Size = new System.Drawing.Size(121, 21);
            this.qualComboBox.TabIndex = 2;
            this.qualComboBox.SelectedIndexChanged += new System.EventHandler(this.reportPeriodBox_SelectedIndexChanged);
            // 
            // reportPeriodBox
            // 
            this.reportPeriodBox.Location = new System.Drawing.Point(104, 42);
            this.reportPeriodBox.Name = "reportPeriodBox";
            this.reportPeriodBox.Size = new System.Drawing.Size(121, 21);
            this.reportPeriodBox.TabIndex = 2;
            this.reportPeriodBox.SelectedIndexChanged += new System.EventHandler(this.reportPeriodBox_SelectedIndexChanged);
            // 
            // unitsBox
            // 
            this.unitsBox.Location = new System.Drawing.Point(301, 73);
            this.unitsBox.Name = "unitsBox";
            this.unitsBox.Size = new System.Drawing.Size(121, 21);
            this.unitsBox.TabIndex = 5;
            // 
            // hydComboBox
            // 
            this.hydComboBox.Location = new System.Drawing.Point(104, 11);
            this.hydComboBox.Name = "hydComboBox";
            this.hydComboBox.Size = new System.Drawing.Size(121, 21);
            this.hydComboBox.TabIndex = 0;
            this.hydComboBox.SelectedIndexChanged += new System.EventHandler(this.hydComboBox_SelectedIndexChanged);
            // 
            // textSimulationDuration
            // 
            this.textSimulationDuration.Location = new System.Drawing.Point(301, 11);
            this.textSimulationDuration.Name = "textSimulationDuration";
            this.textSimulationDuration.Size = new System.Drawing.Size(121, 20);
            this.textSimulationDuration.TabIndex = 3;
            this.textSimulationDuration.Validating += new System.ComponentModel.CancelEventHandler(this.textSimulationDuration_Validating);
            // 
            // textReportStart
            // 
            this.textReportStart.Location = new System.Drawing.Point(301, 42);
            this.textReportStart.Name = "textReportStart";
            this.textReportStart.Size = new System.Drawing.Size(121, 20);
            this.textReportStart.TabIndex = 4;
            this.textReportStart.Validating += new System.ComponentModel.CancelEventHandler(this.textReportStart_Validating);
            // 
            // dtSimulationDuration
            // 
            this.dtSimulationDuration.CustomFormat = "dd:HH:mm:ss";
            this.dtSimulationDuration.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtSimulationDuration.Location = new System.Drawing.Point(276, 12);
            this.dtSimulationDuration.Name = "dtSimulationDuration";
            this.dtSimulationDuration.ShowCheckBox = true;
            this.dtSimulationDuration.ShowUpDown = true;
            this.dtSimulationDuration.Size = new System.Drawing.Size(121, 20);
            this.dtSimulationDuration.TabIndex = 31;
            this.dtSimulationDuration.Value = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            // 
            // actions
            // 
            this.actions.Controls.Add(this.dtSimulationDuration);
            this.actions.Controls.Add(this.runButton);
            this.actions.Controls.Add(this.cancelButton);
            this.actions.Dock = System.Windows.Forms.DockStyle.Top;
            this.actions.Location = new System.Drawing.Point(0, 385);
            this.actions.Name = "actions";
            this.actions.Size = new System.Drawing.Size(625, 38);
            this.actions.TabIndex = 12;
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(3, 8);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 24;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // top
            // 
            this.top.ColumnCount = 3;
            this.top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.top.Controls.Add(this.qualityMSXPanel, 0, 0);
            this.top.Controls.Add(this.qualityPanel, 0, 0);
            this.top.Controls.Add(this.hydPanel, 0, 0);
            this.top.Dock = System.Windows.Forms.DockStyle.Top;
            this.top.Location = new System.Drawing.Point(0, 34);
            this.top.Name = "top";
            this.top.RowCount = 1;
            this.top.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.top.Size = new System.Drawing.Size(625, 251);
            this.top.TabIndex = 0;
            // 
            // qualityMSXPanel
            // 
            this.qualityMSXPanel.Controls.Add(this.qualityMSXCheckBox);
            this.qualityMSXPanel.Controls.Add(this.speciesCheckList);
            this.qualityMSXPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.qualityMSXPanel.Location = new System.Drawing.Point(403, 3);
            this.qualityMSXPanel.Name = "qualityMSXPanel";
            this.qualityMSXPanel.Size = new System.Drawing.Size(219, 245);
            this.qualityMSXPanel.TabIndex = 0;
            // 
            // progressPanel
            // 
            this.progressPanel.AutoSize = false;
            this.progressPanel.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel,
            this.progressBar});
            this.progressPanel.Location = new System.Drawing.Point(0, 425);
            this.progressPanel.Name = "progressPanel";
            this.progressPanel.Size = new System.Drawing.Size(625, 30);
            this.progressPanel.TabIndex = 23;
            this.progressPanel.Text = "statusStrip1";
            // 
            // statusLabel
            // 
            this.statusLabel.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(208, 25);
            this.statusLabel.Spring = true;
            this.statusLabel.Text = "Idle";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // progressBar
            // 
            this.progressBar.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(400, 24);
            // 
            // ReportOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(625, 455);
            this.Controls.Add(this.progressPanel);
            this.Controls.Add(this.actions);
            this.Controls.Add(this.reportOptions2);
            this.Controls.Add(this.top);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ReportOptions";
            this.Text = "Reporting options";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ReportOptions_Closing);
            this.Load += new System.EventHandler(this.ReportOptions_Load);
            this.hydPanel.ResumeLayout(false);
            this.qualityPanel.ResumeLayout(false);
            this.reportOptions2.ResumeLayout(false);
            this.reportOptions2.PerformLayout();
            this.actions.ResumeLayout(false);
            this.top.ResumeLayout(false);
            this.qualityMSXPanel.ResumeLayout(false);
            this.progressPanel.ResumeLayout(false);
            this.progressPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox hydraulicsCheckBox;
        private System.Windows.Forms.CheckBox qualityCheckBox;
        private System.Windows.Forms.CheckBox showSummaryCheckBox;
        private System.Windows.Forms.CheckBox showHydraulicSolverEventsCheckBox;
        private System.Windows.Forms.Button runButton;
        private System.Windows.Forms.Panel reportOptions2;
        private System.Windows.Forms.Panel actions;
        private System.Windows.Forms.Panel hydPanel;
        private System.Windows.Forms.Panel qualityPanel;
        private System.Windows.Forms.ComboBox reportPeriodBox;
        private System.Windows.Forms.ComboBox unitsBox;
        private System.Windows.Forms.CheckedListBox hydVariables;
        private System.Windows.Forms.CheckedListBox qualityVariables;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.TextBox textSimulationDuration;
        private System.Windows.Forms.ComboBox hydComboBox;
        private System.Windows.Forms.TextBox textReportStart;
        private System.Windows.Forms.CheckBox transposeResultsCheckBox;
        private System.Windows.Forms.DateTimePicker dtSimulationDuration;
        private System.Windows.Forms.ComboBox qualComboBox;

        private System.Windows.Forms.CheckBox qualityMSXCheckBox;
        private System.Windows.Forms.CheckedListBox speciesCheckList;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TableLayoutPanel top;
        private System.Windows.Forms.Panel qualityMSXPanel;
        private System.Windows.Forms.StatusStrip progressPanel;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.ToolStripProgressBar progressBar;


    }
}