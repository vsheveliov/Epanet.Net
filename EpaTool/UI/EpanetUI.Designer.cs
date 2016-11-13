namespace EpaTool {

    sealed partial class EpanetUI {
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
            System.Windows.Forms.Label label2;
            System.Windows.Forms.Label label4;
            System.Windows.Forms.Label label5;
            System.Windows.Forms.Label label6;
            System.Windows.Forms.Label label7;
            System.Windows.Forms.Label label8;
            System.Windows.Forms.Label label9;
            System.Windows.Forms.Label label10;
            System.Windows.Forms.Label label11;
            System.Windows.Forms.Label label12;
            System.Windows.Forms.Label label13;
            System.Windows.Forms.Label label14;
            System.Windows.Forms.Label logoB;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EpanetUI));
            System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem menuOpen;
            this.menuRun = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSave = new System.Windows.Forms.ToolStripMenuItem();
            this.bottom = new System.Windows.Forms.Panel();
            this.textDuration = new System.Windows.Forms.Label();
            this.textQuality = new System.Windows.Forms.Label();
            this.textHeadloss = new System.Windows.Forms.Label();
            this.textUnits = new System.Windows.Forms.Label();
            this.textDemand = new System.Windows.Forms.Label();
            this.textPattern = new System.Windows.Forms.Label();
            this.textHydraulic = new System.Windows.Forms.Label();
            this.middle = new System.Windows.Forms.Panel();
            this.textNodes = new System.Windows.Forms.Label();
            this.textTanks = new System.Windows.Forms.Label();
            this.textReservoirs = new System.Windows.Forms.Label();
            this.textPipes = new System.Windows.Forms.Label();
            this.properties = new System.Windows.Forms.Panel();
            this.inpName = new System.Windows.Forms.Label();
            this.top = new System.Windows.Forms.Panel();
            this.openINPButton = new System.Windows.Forms.Button();
            this.runSimulationButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.checks = new System.Windows.Forms.Panel();
            this.lblCoordinates = new System.Windows.Forms.Label();
            this.checkPipes = new System.Windows.Forms.CheckBox();
            this.checkNodes = new System.Windows.Forms.CheckBox();
            this.checkTanks = new System.Windows.Forms.CheckBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuZoomAll = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuZoomIn = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuZoomOut = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.drawNodesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.drawTanksReservoirsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.drawPipesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.networkPanel = new NetworkPanel();
            this.menuClose = new System.Windows.Forms.ToolStripMenuItem();
            label2 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            label9 = new System.Windows.Forms.Label();
            label10 = new System.Windows.Forms.Label();
            label11 = new System.Windows.Forms.Label();
            label12 = new System.Windows.Forms.Label();
            label13 = new System.Windows.Forms.Label();
            label14 = new System.Windows.Forms.Label();
            logoB = new System.Windows.Forms.Label();
            fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            menuOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.bottom.SuspendLayout();
            this.middle.SuspendLayout();
            this.properties.SuspendLayout();
            this.top.SuspendLayout();
            this.checks.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label2
            // 
            label2.Location = new System.Drawing.Point(2, 2);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(120, 23);
            label2.TabIndex = 0;
            label2.Text = "INP file name:";
            label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            label4.Location = new System.Drawing.Point(2, 71);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(120, 23);
            label4.TabIndex = 0;
            label4.Text = "Reservoirs";
            label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            label5.Location = new System.Drawing.Point(2, 2);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(120, 23);
            label5.TabIndex = 0;
            label5.Text = "Pipes";
            label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label6
            // 
            label6.Location = new System.Drawing.Point(2, 25);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(120, 23);
            label6.TabIndex = 0;
            label6.Text = "Nodes";
            label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label7
            // 
            label7.Location = new System.Drawing.Point(2, 48);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(120, 23);
            label7.TabIndex = 0;
            label7.Text = "Tanks";
            label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label8
            // 
            label8.Location = new System.Drawing.Point(2, 2);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(120, 23);
            label8.TabIndex = 0;
            label8.Text = "Simulation Duration";
            label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label9
            // 
            label9.Location = new System.Drawing.Point(2, 71);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(120, 23);
            label9.TabIndex = 0;
            label9.Text = "Units";
            label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label10
            // 
            label10.Location = new System.Drawing.Point(2, 94);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(120, 23);
            label10.TabIndex = 0;
            label10.Text = "Headloss Formula";
            label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label11
            // 
            label11.Location = new System.Drawing.Point(2, 117);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(120, 23);
            label11.TabIndex = 0;
            label11.Text = "Demand multiplier";
            label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label12
            // 
            label12.Location = new System.Drawing.Point(2, 140);
            label12.Name = "label12";
            label12.Size = new System.Drawing.Size(120, 23);
            label12.TabIndex = 0;
            label12.Text = "Quality";
            label12.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label13
            // 
            label13.Location = new System.Drawing.Point(2, 25);
            label13.Name = "label13";
            label13.Size = new System.Drawing.Size(120, 23);
            label13.TabIndex = 0;
            label13.Text = "Hydraulic timestep";
            label13.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label14
            // 
            label14.Location = new System.Drawing.Point(2, 48);
            label14.Name = "label14";
            label14.Size = new System.Drawing.Size(120, 23);
            label14.TabIndex = 0;
            label14.Text = "Pattern timestep";
            label14.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // logoB
            // 
            logoB.Cursor = System.Windows.Forms.Cursors.Hand;
            logoB.Dock = System.Windows.Forms.DockStyle.Top;
            logoB.Image = ((System.Drawing.Image)(resources.GetObject("logoB.Image")));
            logoB.Location = new System.Drawing.Point(10, 10);
            logoB.Name = "logoB";
            logoB.Size = new System.Drawing.Size(261, 52);
            logoB.TabIndex = 5;
            logoB.Click += new System.EventHandler(this.logoB_Click);
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            menuOpen,
            this.menuSave,
            this.menuClose,
            this.menuRun});
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            fileToolStripMenuItem.Text = "&File";
            // 
            // menuOpen
            // 
            menuOpen.Name = "menuOpen";
            menuOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            menuOpen.Size = new System.Drawing.Size(163, 22);
            menuOpen.Text = "&Open";
            menuOpen.Click += new System.EventHandler(this.OpenEvent);
            // 
            // menuRun
            // 
            this.menuRun.Name = "menuRun";
            this.menuRun.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.menuRun.Size = new System.Drawing.Size(163, 22);
            this.menuRun.Text = "&Run";
            this.menuRun.Click += new System.EventHandler(this.RunSimulation);
            // 
            // menuSave
            // 
            this.menuSave.Name = "menuSave";
            this.menuSave.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.menuSave.Size = new System.Drawing.Size(163, 22);
            this.menuSave.Text = "&Save As...";
            this.menuSave.Click += new System.EventHandler(this.SaveEvent);
            // 
            // bottom
            // 
            this.bottom.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.bottom.Controls.Add(this.textDuration);
            this.bottom.Controls.Add(this.textQuality);
            this.bottom.Controls.Add(label14);
            this.bottom.Controls.Add(this.textHeadloss);
            this.bottom.Controls.Add(this.textUnits);
            this.bottom.Controls.Add(this.textDemand);
            this.bottom.Controls.Add(label13);
            this.bottom.Controls.Add(this.textPattern);
            this.bottom.Controls.Add(label12);
            this.bottom.Controls.Add(label11);
            this.bottom.Controls.Add(this.textHydraulic);
            this.bottom.Controls.Add(label10);
            this.bottom.Controls.Add(label9);
            this.bottom.Controls.Add(label8);
            this.bottom.Dock = System.Windows.Forms.DockStyle.Top;
            this.bottom.Location = new System.Drawing.Point(10, 275);
            this.bottom.Name = "bottom";
            this.bottom.Size = new System.Drawing.Size(261, 167);
            this.bottom.TabIndex = 28;
            // 
            // textDuration
            // 
            this.textDuration.Location = new System.Drawing.Point(123, 2);
            this.textDuration.Name = "textDuration";
            this.textDuration.Size = new System.Drawing.Size(135, 23);
            this.textDuration.TabIndex = 13;
            this.textDuration.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textQuality
            // 
            this.textQuality.Location = new System.Drawing.Point(123, 140);
            this.textQuality.Name = "textQuality";
            this.textQuality.Size = new System.Drawing.Size(135, 23);
            this.textQuality.TabIndex = 6;
            this.textQuality.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textHeadloss
            // 
            this.textHeadloss.Location = new System.Drawing.Point(123, 94);
            this.textHeadloss.Name = "textHeadloss";
            this.textHeadloss.Size = new System.Drawing.Size(135, 23);
            this.textHeadloss.TabIndex = 15;
            this.textHeadloss.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textUnits
            // 
            this.textUnits.Location = new System.Drawing.Point(123, 71);
            this.textUnits.Name = "textUnits";
            this.textUnits.Size = new System.Drawing.Size(135, 23);
            this.textUnits.TabIndex = 14;
            this.textUnits.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textDemand
            // 
            this.textDemand.Location = new System.Drawing.Point(123, 117);
            this.textDemand.Name = "textDemand";
            this.textDemand.Size = new System.Drawing.Size(135, 23);
            this.textDemand.TabIndex = 5;
            this.textDemand.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textPattern
            // 
            this.textPattern.Location = new System.Drawing.Point(123, 48);
            this.textPattern.Name = "textPattern";
            this.textPattern.Size = new System.Drawing.Size(135, 23);
            this.textPattern.TabIndex = 29;
            this.textPattern.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textHydraulic
            // 
            this.textHydraulic.Location = new System.Drawing.Point(123, 25);
            this.textHydraulic.Name = "textHydraulic";
            this.textHydraulic.Size = new System.Drawing.Size(135, 23);
            this.textHydraulic.TabIndex = 25;
            this.textHydraulic.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // middle
            // 
            this.middle.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.middle.Controls.Add(this.textNodes);
            this.middle.Controls.Add(label7);
            this.middle.Controls.Add(label6);
            this.middle.Controls.Add(label5);
            this.middle.Controls.Add(label4);
            this.middle.Controls.Add(this.textTanks);
            this.middle.Controls.Add(this.textReservoirs);
            this.middle.Controls.Add(this.textPipes);
            this.middle.Dock = System.Windows.Forms.DockStyle.Top;
            this.middle.Location = new System.Drawing.Point(10, 124);
            this.middle.Name = "middle";
            this.middle.Size = new System.Drawing.Size(261, 98);
            this.middle.TabIndex = 27;
            // 
            // textNodes
            // 
            this.textNodes.Location = new System.Drawing.Point(123, 25);
            this.textNodes.Name = "textNodes";
            this.textNodes.Size = new System.Drawing.Size(135, 23);
            this.textNodes.TabIndex = 12;
            this.textNodes.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textTanks
            // 
            this.textTanks.Location = new System.Drawing.Point(123, 48);
            this.textTanks.Name = "textTanks";
            this.textTanks.Size = new System.Drawing.Size(135, 23);
            this.textTanks.TabIndex = 10;
            this.textTanks.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textReservoirs
            // 
            this.textReservoirs.Location = new System.Drawing.Point(123, 71);
            this.textReservoirs.Name = "textReservoirs";
            this.textReservoirs.Size = new System.Drawing.Size(135, 23);
            this.textReservoirs.TabIndex = 9;
            this.textReservoirs.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textPipes
            // 
            this.textPipes.Location = new System.Drawing.Point(123, 2);
            this.textPipes.Name = "textPipes";
            this.textPipes.Size = new System.Drawing.Size(135, 23);
            this.textPipes.TabIndex = 11;
            this.textPipes.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // properties
            // 
            this.properties.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.properties.Controls.Add(label2);
            this.properties.Controls.Add(this.inpName);
            this.properties.Dock = System.Windows.Forms.DockStyle.Top;
            this.properties.Location = new System.Drawing.Point(10, 222);
            this.properties.MinimumSize = new System.Drawing.Size(240, 4);
            this.properties.Name = "properties";
            this.properties.Size = new System.Drawing.Size(261, 53);
            this.properties.TabIndex = 7;
            // 
            // inpName
            // 
            this.inpName.Location = new System.Drawing.Point(3, 25);
            this.inpName.Name = "inpName";
            this.inpName.Size = new System.Drawing.Size(247, 23);
            this.inpName.TabIndex = 24;
            this.inpName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // top
            // 
            this.top.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.top.Controls.Add(this.openINPButton);
            this.top.Controls.Add(this.runSimulationButton);
            this.top.Controls.Add(this.saveButton);
            this.top.Dock = System.Windows.Forms.DockStyle.Top;
            this.top.Location = new System.Drawing.Point(10, 62);
            this.top.MinimumSize = new System.Drawing.Size(240, 4);
            this.top.Name = "top";
            this.top.Size = new System.Drawing.Size(261, 62);
            this.top.TabIndex = 3;
            // 
            // openINPButton
            // 
            this.openINPButton.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.openINPButton.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.ButtonFace;
            this.openINPButton.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ControlDark;
            this.openINPButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.openINPButton.Location = new System.Drawing.Point(14, 3);
            this.openINPButton.Name = "openINPButton";
            this.openINPButton.Size = new System.Drawing.Size(100, 23);
            this.openINPButton.TabIndex = 1;
            this.openINPButton.Text = "Open";
            this.openINPButton.UseVisualStyleBackColor = false;
            this.openINPButton.Click += new System.EventHandler(this.OpenEvent);
            // 
            // runSimulationButton
            // 
            this.runSimulationButton.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.runSimulationButton.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.ButtonFace;
            this.runSimulationButton.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ControlDark;
            this.runSimulationButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.runSimulationButton.Location = new System.Drawing.Point(150, 3);
            this.runSimulationButton.Name = "runSimulationButton";
            this.runSimulationButton.Size = new System.Drawing.Size(100, 23);
            this.runSimulationButton.TabIndex = 2;
            this.runSimulationButton.Text = "Run Simulation";
            this.runSimulationButton.UseVisualStyleBackColor = false;
            this.runSimulationButton.Click += new System.EventHandler(this.RunSimulation);
            // 
            // saveButton
            // 
            this.saveButton.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.saveButton.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.ButtonFace;
            this.saveButton.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ControlDark;
            this.saveButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.saveButton.Location = new System.Drawing.Point(14, 32);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(100, 23);
            this.saveButton.TabIndex = 30;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = false;
            this.saveButton.Click += new System.EventHandler(this.SaveEvent);
            // 
            // checks
            // 
            this.checks.Controls.Add(this.lblCoordinates);
            this.checks.Controls.Add(this.checkPipes);
            this.checks.Controls.Add(this.checkNodes);
            this.checks.Controls.Add(this.checkTanks);
            this.checks.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.checks.Location = new System.Drawing.Point(0, 497);
            this.checks.Name = "checks";
            this.checks.Size = new System.Drawing.Size(556, 39);
            this.checks.TabIndex = 21;
            // 
            // lblCoordinates
            // 
            this.lblCoordinates.AutoSize = true;
            this.lblCoordinates.Location = new System.Drawing.Point(379, 12);
            this.lblCoordinates.Name = "lblCoordinates";
            this.lblCoordinates.Size = new System.Drawing.Size(42, 13);
            this.lblCoordinates.TabIndex = 19;
            this.lblCoordinates.Text = "X:0;Y:0";
            // 
            // checkPipes
            // 
            this.checkPipes.AutoSize = true;
            this.checkPipes.Checked = true;
            this.checkPipes.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkPipes.Location = new System.Drawing.Point(294, 11);
            this.checkPipes.Name = "checkPipes";
            this.checkPipes.Size = new System.Drawing.Size(80, 17);
            this.checkPipes.TabIndex = 18;
            this.checkPipes.Text = "Draw Pipes";
            this.checkPipes.CheckedChanged += new System.EventHandler(this.checks_CheckedChanged);
            // 
            // checkNodes
            // 
            this.checkNodes.AutoSize = true;
            this.checkNodes.Checked = true;
            this.checkNodes.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkNodes.Location = new System.Drawing.Point(19, 11);
            this.checkNodes.Name = "checkNodes";
            this.checkNodes.Size = new System.Drawing.Size(85, 17);
            this.checkNodes.TabIndex = 16;
            this.checkNodes.Text = "Draw Nodes";
            this.checkNodes.CheckedChanged += new System.EventHandler(this.checks_CheckedChanged);
            // 
            // checkTanks
            // 
            this.checkTanks.AutoSize = true;
            this.checkTanks.Checked = true;
            this.checkTanks.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkTanks.Location = new System.Drawing.Point(120, 11);
            this.checkTanks.Name = "checkTanks";
            this.checkTanks.Size = new System.Drawing.Size(158, 17);
            this.checkTanks.TabIndex = 17;
            this.checkTanks.Text = "Draw Tanks and Reservoirs";
            this.checkTanks.CheckedChanged += new System.EventHandler(this.checks_CheckedChanged);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            fileToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(841, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuZoomAll,
            this.mnuZoomIn,
            this.mnuZoomOut,
            this.toolStripMenuItem1,
            this.drawNodesToolStripMenuItem,
            this.drawTanksReservoirsToolStripMenuItem,
            this.drawPipesToolStripMenuItem,
            this.toolStripMenuItem2});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(41, 20);
            this.viewToolStripMenuItem.Text = "&View";
            // 
            // mnuZoomAll
            // 
            this.mnuZoomAll.Name = "mnuZoomAll";
            this.mnuZoomAll.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Multiply)));
            this.mnuZoomAll.Size = new System.Drawing.Size(194, 22);
            this.mnuZoomAll.Text = "Zoom &All";
            this.mnuZoomAll.Click += new System.EventHandler(this.mnuZoomAll_Click);
            // 
            // mnuZoomIn
            // 
            this.mnuZoomIn.Name = "mnuZoomIn";
            this.mnuZoomIn.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Add)));
            this.mnuZoomIn.Size = new System.Drawing.Size(194, 22);
            this.mnuZoomIn.Text = "Zoom &In";
            this.mnuZoomIn.Click += new System.EventHandler(this.mnuZoomIn_Click);
            // 
            // mnuZoomOut
            // 
            this.mnuZoomOut.Name = "mnuZoomOut";
            this.mnuZoomOut.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Subtract)));
            this.mnuZoomOut.Size = new System.Drawing.Size(194, 22);
            this.mnuZoomOut.Text = "Zoom &Out";
            this.mnuZoomOut.Click += new System.EventHandler(this.mnuZoomOut_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(191, 6);
            // 
            // drawNodesToolStripMenuItem
            // 
            this.drawNodesToolStripMenuItem.Name = "drawNodesToolStripMenuItem";
            this.drawNodesToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.drawNodesToolStripMenuItem.Text = "Draw &Nodes";
            this.drawNodesToolStripMenuItem.CheckedChanged += new System.EventHandler(this.checks_CheckedChanged);
            // 
            // drawTanksReservoirsToolStripMenuItem
            // 
            this.drawTanksReservoirsToolStripMenuItem.Name = "drawTanksReservoirsToolStripMenuItem";
            this.drawTanksReservoirsToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.drawTanksReservoirsToolStripMenuItem.Text = "Draw &Tanks && Reservoirs";
            this.drawTanksReservoirsToolStripMenuItem.CheckedChanged += new System.EventHandler(this.checks_CheckedChanged);
            // 
            // drawPipesToolStripMenuItem
            // 
            this.drawPipesToolStripMenuItem.Name = "drawPipesToolStripMenuItem";
            this.drawPipesToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.drawPipesToolStripMenuItem.Text = "Draw &Pipes";
            this.drawPipesToolStripMenuItem.CheckedChanged += new System.EventHandler(this.checks_CheckedChanged);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(191, 6);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(225)))), ((int)(((byte)(225)))));
            this.splitContainer1.Panel1.Controls.Add(this.bottom);
            this.splitContainer1.Panel1.Controls.Add(this.properties);
            this.splitContainer1.Panel1.Controls.Add(this.middle);
            this.splitContainer1.Panel1.Controls.Add(this.top);
            this.splitContainer1.Panel1.Controls.Add(logoB);
            this.splitContainer1.Panel1.Padding = new System.Windows.Forms.Padding(10);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.networkPanel);
            this.splitContainer1.Panel2.Controls.Add(this.checks);
            this.splitContainer1.Size = new System.Drawing.Size(841, 536);
            this.splitContainer1.SplitterDistance = 281;
            this.splitContainer1.TabIndex = 21;
            // 
            // networkPanel
            // 
            this.networkPanel.BackColor = System.Drawing.Color.Black;
            this.networkPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.networkPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.networkPanel.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.networkPanel.Location = new System.Drawing.Point(0, 0);
            this.networkPanel.Name = "networkPanel";
            this.networkPanel.Size = new System.Drawing.Size(556, 497);
            this.networkPanel.TabIndex = 8;
            this.networkPanel.TabStop = true;
            this.networkPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.networkPanel_MouseMove);
            // 
            // menuClose
            // 
            this.menuClose.Name = "menuClose";
            this.menuClose.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F4)));
            this.menuClose.Size = new System.Drawing.Size(163, 22);
            this.menuClose.Text = "&Close";
            this.menuClose.Click += new System.EventHandler(this.menuClose_Click);
            // 
            // EpanetUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(841, 560);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "EpanetUI";
            this.Text = "EpanetUI2";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.EpanetUI_FormClosing);
            this.bottom.ResumeLayout(false);
            this.middle.ResumeLayout(false);
            this.properties.ResumeLayout(false);
            this.top.ResumeLayout(false);
            this.checks.ResumeLayout(false);
            this.checks.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button openINPButton;
        private System.Windows.Forms.Button runSimulationButton;
        private System.Windows.Forms.Panel top;
        private System.Windows.Forms.Label textDemand;
        private System.Windows.Forms.Label textQuality;
        private System.Windows.Forms.Panel properties;
        private NetworkPanel networkPanel;
        private System.Windows.Forms.Label textReservoirs;
        private System.Windows.Forms.Label textTanks;
        private System.Windows.Forms.Label textPipes;
        private System.Windows.Forms.Label textNodes;
        private System.Windows.Forms.Label textDuration;
        private System.Windows.Forms.Label textUnits;
        private System.Windows.Forms.Label textHeadloss;
        private System.Windows.Forms.CheckBox checkNodes;
        private System.Windows.Forms.CheckBox checkTanks;
        private System.Windows.Forms.CheckBox checkPipes;
        private System.Windows.Forms.Panel checks;
        private System.Windows.Forms.Label inpName;
        private System.Windows.Forms.Label textHydraulic;
        private System.Windows.Forms.Panel middle;
        private System.Windows.Forms.Panel bottom;
        private System.Windows.Forms.Label textPattern;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menuRun;
        private System.Windows.Forms.ToolStripMenuItem menuSave;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mnuZoomAll;
        private System.Windows.Forms.ToolStripMenuItem mnuZoomIn;
        private System.Windows.Forms.ToolStripMenuItem mnuZoomOut;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem drawNodesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem drawTanksReservoirsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem drawPipesToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.Label lblCoordinates;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStripMenuItem menuClose;
    }
}