using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using PlantFlow_Support.Properties;

namespace PlantFlow_Support
{
  public partial class PaletteTab
  {
    private IContainer components;
    private Button btExport;
    private Label lb1;
    private Button btAdd;
    private ListView lvSupportName;
    private ColumnHeader clSPName;
    private ColumnHeader clSPView;
    private ContextMenuStrip lvcMenuStrip;
    private ToolStripMenuItem tmsRemove;
    private ToolStripMenuItem tmsClearItem;
    private ComboBox cbbViewDirection;
    private Label label1;
    private Button btExportMTO;
    private TextBox tbSaveMTOAs;
    private Label label2;
    private TextBox tbTemplate;
    private Label label3;
    private Button btProccessing;
    private TextBox tbStartNumber;
    private Label label6;
    private Label label5;
    private ComboBox cbSupportType;
    private Button btSetGrid;
    private Label label7;
    private TextBox tbXlabel;
    private Label label4;
    private TextBox tbYlabel;
    private Button btGetXGrid;
    private Button btGetYGrid;
    private Button btPreProcess;

    private Label label8;
    private Label label9;
    private TextBox tbProjectNo;
    private Button btSetIdentifierCurrentSelection;
    private Label label10;
    private Label label11;
    private Label label14;
    private Label label13;
    private Button btAutoCodingForSelectedOnly;

    private TextBox tbDwgRevision;
    private TabControl tabMainUI;
    private TabPage tabSupportCatalog;
    private ComboBox cbCatType;
    private DataGridView dgvCatVariants;
    private Panel pnlCatParams;
    private TextBox tbCatDescOverride;
    private Button btCatAddVariant;
    private Button btCatUpdate;
    private Button btCatDelete;
    private Label lblCatStatus;
    private Label lblCatType;
    private Label lblCatDesc;
    private Button btCatBrowse;
    private Label lblCatPath;

    private TabPage tabSpanAutoPlace;
    private Button btSpanSelectPipes;
    private ComboBox cbSpanLoadCase;
    private NumericUpDown nudCollisionTol;
    private NumericUpDown nudMarkerRadius;
    private Button btSpanPreview;
    private DataGridView dgvSpanProposals;
    private ListBox lbSpanBlocked;
    private Button btSpanExport;
    private Button btSpanClear;
    private Label lblSpanSummary;
    private Label lblLoadCase;
    private Label lblColTol;
    private Label lblRadius;

    private TabPage tabAuto2D;
    private TabPage tabPSData;
    private Panel panel1;
    private Panel panel3;
    private Panel panel2;
    private Panel panel5;
    private DataGridView dgvPSDM;
    private ContextMenuStrip csmPSDM;
    private ToolStripMenuItem tsmRefresh;

    private Button btSetIdentifierCurrentList; // NEW
    private Button btAutoCodingCurrentList; 

    private Button btRefresh;
    private Button btProjectScan;

    protected override void Dispose(bool disposing)
    {
      PaletteTab_Dispose(disposing);
      if (disposing && this.components != null)
        this.components.Dispose();
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      Log("Init 1: Start");
      this.components = (IContainer) new System.ComponentModel.Container();
      this.btExport = new Button();
      this.lb1 = new Label();
      this.btAdd = new Button();
      this.lvSupportName = new ListView();
      Log("Init 2: ListView Created");
      this.clSPName = new ColumnHeader();
      this.clSPView = new ColumnHeader();
      this.lvcMenuStrip = new ContextMenuStrip(this.components);
      this.tmsRemove = new ToolStripMenuItem();
      this.tmsClearItem = new ToolStripMenuItem();
      this.cbbViewDirection = new ComboBox();
      this.label1 = new Label();
      this.btExportMTO = new Button();
      this.tbSaveMTOAs = new TextBox();
      this.label2 = new Label();
      this.tbTemplate = new TextBox();
      this.label3 = new Label();
      this.btProccessing = new Button();
      this.tbStartNumber = new TextBox();
      this.label5 = new Label();
      this.label6 = new Label();
      this.label10 = new Label();
      this.btSetIdentifierCurrentSelection = new Button();

      this.btPreProcess = new Button();
      this.btSetGrid = new Button();
      this.tbDwgRevision = new TextBox();
      this.tbProjectNo = new TextBox();
      this.label8 = new Label();
      this.label9 = new Label();
      this.btGetXGrid = new Button();
      this.btGetYGrid = new Button();
      this.tbYlabel = new TextBox();
      this.label7 = new Label();
      this.tbXlabel = new TextBox();
      this.label4 = new Label();
      this.tabMainUI = new TabControl();
      this.tabAuto2D = new TabPage();
      this.tabSupportCatalog = new TabPage();
      this.cbCatType = new ComboBox();
      this.dgvCatVariants = new DataGridView();
      this.pnlCatParams = new Panel();
      this.tbCatDescOverride = new TextBox();
      this.btCatAddVariant = new Button();
      this.btCatUpdate = new Button();
      this.btCatDelete = new Button();
      this.lblCatStatus = new Label();
      this.lblCatType = new Label();
      this.lblCatDesc = new Label();
      this.btCatBrowse = new Button();
      this.lblCatPath = new Label();

      this.tabSpanAutoPlace = new TabPage();
      this.btSpanSelectPipes = new Button();
      this.cbSpanLoadCase = new ComboBox();
      this.nudCollisionTol = new NumericUpDown();
      this.nudMarkerRadius = new NumericUpDown();
      this.btSpanPreview = new Button();
      this.dgvSpanProposals = new DataGridView();
      this.lbSpanBlocked = new ListBox();
      this.btSpanExport = new Button();
      this.btSpanClear = new Button();
      this.lblSpanSummary = new Label();
      this.lblLoadCase = new Label();
      this.lblColTol = new Label();
      this.lblRadius = new Label();

      this.label13 = new Label();
      this.panel3 = new Panel();
      this.panel2 = new Panel();
      this.panel1 = new Panel();
      this.tabPSData = new TabPage();

      this.dgvPSDM = new DataGridView();
      this.panel5 = new Panel();
      this.csmPSDM = new ContextMenuStrip(this.components);
      this.tsmRefresh = new ToolStripMenuItem();
      this.btRefresh = new Button();
      this.btProjectScan = new Button();
      this.btAutoCodingForSelectedOnly = new Button();

      this.label11 = new Label();
      this.label14 = new Label();

      this.label10.Text = "Set Support Identifier:";
      
      this.btSetIdentifierCurrentSelection.BackColor = SystemColors.Info;
      this.btSetIdentifierCurrentSelection.Location = new Point(330, 4); // Shifted Left
      this.btSetIdentifierCurrentSelection.Name = "btSetIdentifierCurrentSelection";
      this.btSetIdentifierCurrentSelection.Size = new Size(75, 23);
      this.btSetIdentifierCurrentSelection.TabIndex = 17;
      this.btSetIdentifierCurrentSelection.Text = "Selected Only";
      this.btSetIdentifierCurrentSelection.UseVisualStyleBackColor = false;
      this.btSetIdentifierCurrentSelection.Click += new EventHandler(this.btSetIdentifierCurrentSelection_Click);
      // NEW BUTTON: Current List
      this.btSetIdentifierCurrentList = new Button();
      this.btSetIdentifierCurrentList.BackColor = SystemColors.Info;
      this.btSetIdentifierCurrentList.Location = new Point(410, 4);
      this.btSetIdentifierCurrentList.Name = "btSetIdentifierCurrentList";
      this.btSetIdentifierCurrentList.Size = new Size(75, 23);
      this.btSetIdentifierCurrentList.TabIndex = 90;
      this.btSetIdentifierCurrentList.Text = "Current List";
      this.btSetIdentifierCurrentList.UseVisualStyleBackColor = false;
      this.btSetIdentifierCurrentList.Click += new EventHandler(new EventHandler(this.btSetIdentifierCurrentList_Click));

      this.btPreProcess = new Button();
      this.btSetGrid = new Button();
      this.tbDwgRevision = new TextBox();
      this.tbProjectNo = new TextBox();
      this.label8 = new Label();
      this.label9 = new Label();
      this.btGetXGrid = new Button();
      this.btGetYGrid = new Button();
      this.tbYlabel = new TextBox();
      this.label7 = new Label();
      this.tbXlabel = new TextBox();
      this.label4 = new Label();
      this.tabMainUI = new TabControl();
      this.tabAuto2D = new TabPage();
      this.tabSupportCatalog = new TabPage();
      this.cbCatType = new ComboBox();
      this.dgvCatVariants = new DataGridView();
      this.pnlCatParams = new Panel();
      this.tbCatDescOverride = new TextBox();
      this.btCatAddVariant = new Button();
      this.btCatUpdate = new Button();
      this.btCatDelete = new Button();
      this.lblCatStatus = new Label();
      this.lblCatType = new Label();
      this.lblCatDesc = new Label();
      this.btCatBrowse = new Button();
      this.lblCatPath = new Label();

      this.tabSpanAutoPlace = new TabPage();
      this.btSpanSelectPipes = new Button();
      this.cbSpanLoadCase = new ComboBox();
      this.nudCollisionTol = new NumericUpDown();
      this.nudMarkerRadius = new NumericUpDown();
      this.btSpanPreview = new Button();
      this.dgvSpanProposals = new DataGridView();
      this.lbSpanBlocked = new ListBox();
      this.btSpanExport = new Button();
      this.btSpanClear = new Button();
      this.lblSpanSummary = new Label();
      this.lblLoadCase = new Label();
      this.lblColTol = new Label();
      this.lblRadius = new Label();

      this.label13 = new Label();
      this.panel3 = new Panel();
      this.panel2 = new Panel();
      this.panel1 = new Panel();
      this.tabPSData = new TabPage();

      this.dgvPSDM = new DataGridView();
      this.panel5 = new Panel();
      this.csmPSDM = new ContextMenuStrip(this.components);
      this.tsmRefresh = new ToolStripMenuItem();
      this.btRefresh = new Button();

      this.lvcMenuStrip.SuspendLayout();
      this.tabMainUI.SuspendLayout();
      this.tabAuto2D.SuspendLayout();
      this.panel3.SuspendLayout();
      this.panel2.SuspendLayout();
      this.panel1.SuspendLayout();
      this.tabPSData.SuspendLayout();
      this.tabSupportCatalog.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.dgvCatVariants)).BeginInit();

      this.tabSpanAutoPlace.SuspendLayout();
      ((ISupportInitialize)(this.nudCollisionTol)).BeginInit();
      ((ISupportInitialize)(this.nudMarkerRadius)).BeginInit();
      ((ISupportInitialize)(this.dgvSpanProposals)).BeginInit();


      ((ISupportInitialize) this.dgvPSDM).BeginInit();
      this.panel5.SuspendLayout();
      this.csmPSDM.SuspendLayout();
      this.SuspendLayout();
      this.btExport.BackColor = SystemColors.Info;
      this.btExport.Location = new Point(8, 119);
      this.btExport.Name = "btExport";
      this.btExport.Size = new Size(75, 23);
      this.btExport.TabIndex = 0;
      this.btExport.Text = "Export 2D";
      this.btExport.UseVisualStyleBackColor = false;
      this.btExport.Click += new EventHandler(this.btExport_Click);
      this.lb1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      this.lb1.Location = new Point(209, 627);
      this.lb1.Name = "lb1";
      this.lb1.Size = new Size(173, 13);
      this.lb1.TabIndex = 3;
      this.lb1.Text = "Copyright (c) By PlantFlow";
      this.lb1.TextAlign = ContentAlignment.MiddleCenter;
      this.btAdd.BackColor = SystemColors.Info;
      this.btAdd.Location = new Point(8, 7);
      this.btAdd.Name = "btAdd";
      this.btAdd.Size = new Size(75, 23);
      this.btAdd.TabIndex = 5;
      this.btAdd.Text = "Add CE";
      this.btAdd.UseVisualStyleBackColor = false;
      this.btAdd.Click += new EventHandler(this.btAdd_Click);
      this.lvSupportName.Alignment = ListViewAlignment.Left;
      this.lvSupportName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      this.lvSupportName.BackColor = Color.Azure;
      this.lvSupportName.Columns.AddRange(new ColumnHeader[2]
      {
        this.clSPName,
        this.clSPView
      });
      this.lvSupportName.ContextMenuStrip = this.lvcMenuStrip;
      this.lvSupportName.FullRowSelect = true;
      this.lvSupportName.GridLines = true;
      this.lvSupportName.HideSelection = false;
      this.lvSupportName.Location = new Point(90, 3);
      this.lvSupportName.Name = "lvSupportName";
      this.lvSupportName.Size = new Size(485, 273);
      this.lvSupportName.TabIndex = 7;
      this.lvSupportName.TileSize = new Size(168, 30);
      this.lvSupportName.UseCompatibleStateImageBehavior = false;
      this.lvSupportName.View = View.Details;
      this.clSPName.Text = "Support Name";
      this.clSPName.Width = 126;
      this.clSPView.Text = "View";
      this.clSPView.Width = 161;
      this.lvcMenuStrip.ImageScalingSize = new Size(24, 24);
      this.lvcMenuStrip.Items.AddRange(new ToolStripItem[2]
      {
        (ToolStripItem) this.tmsRemove,
        (ToolStripItem) this.tmsClearItem
      });
      this.lvcMenuStrip.Name = "lvcMenuStrip";
      this.lvcMenuStrip.RenderMode = ToolStripRenderMode.Professional;
      this.lvcMenuStrip.Size = new Size(129, 48);
      this.lvcMenuStrip.Text = "Menu";
      this.tmsRemove.Name = "tmsRemove";
      this.tmsRemove.Size = new Size(128, 22);
      this.tmsRemove.Text = "Remove";
      this.tmsRemove.Click += new EventHandler(this.tmsRemove_Click);
      this.tmsClearItem.Name = "tmsClearItem";
      this.tmsClearItem.Size = new Size(128, 22);
      this.tmsClearItem.Text = "Clear Item";
      this.tmsClearItem.Click += new EventHandler(this.tmsClearItem_Click);
      this.cbbViewDirection.FormattingEnabled = true;
      Log("Init 5: Pre-ComboBox AddRange");
      this.cbbViewDirection.Items.AddRange(new object[6]
      {
        (object) "Top",
        (object) "Bottom",
        (object) "Front",
        (object) "Back",
        (object) "Left",
        (object) "Right"
      });
      this.cbbViewDirection.Location = new Point(8, 59);
      this.cbbViewDirection.Margin = new Padding(2);
      this.cbbViewDirection.Name = "cbbViewDirection";
      this.cbbViewDirection.Size = new Size(76, 21);
      this.cbbViewDirection.TabIndex = 10;
      this.label1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
      this.label1.AutoSize = true;
      this.label1.Location = new Point(8, 38);
      this.label1.Margin = new Padding(2, 0, 2, 0);
      this.label1.Name = "label1";
      this.label1.Size = new Size(78, 13);
      this.label1.TabIndex = 11;
      this.label1.Text = "View Direction:";
      this.btExportMTO.BackColor = SystemColors.Info;
      this.btExportMTO.Location = new Point(8, 88);
      this.btExportMTO.Name = "btExportMTO";
      this.btExportMTO.Size = new Size(75, 23);
      this.btExportMTO.TabIndex = 12;
      this.btExportMTO.Text = "Export MTO";
      this.btExportMTO.UseVisualStyleBackColor = false;
      this.btExportMTO.Click += new EventHandler(this.btExportMTO_Click);
      this.tbSaveMTOAs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      this.tbSaveMTOAs.Location = new Point(65, 29);
      this.tbSaveMTOAs.Name = "tbSaveMTOAs";
      this.tbSaveMTOAs.Size = new Size(500, 20);
      this.tbSaveMTOAs.TabIndex = 13;
      this.tbSaveMTOAs.TextChanged += new EventHandler(this.tbSaveMTOAs_TextChanged);
      this.label2.AutoSize = true;
      this.label2.Location = new Point(3, 33);
      this.label2.Name = "label2";
      this.label2.Size = new Size(62, 13);
      this.label2.TabIndex = 14;
      this.label2.Text = "Save MTO:";
      this.tbTemplate.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      this.tbTemplate.Location = new Point(65, 5);
      this.tbTemplate.Name = "tbTemplate";
      this.tbTemplate.Size = new Size(500, 20);
      this.tbTemplate.TabIndex = 13;
      this.tbTemplate.TextChanged += new EventHandler(this.tbTemplate_TextChanged);
      this.label3.AutoSize = true;
      this.label3.Location = new Point(3, 9);
      this.label3.Name = "label3";
      this.label3.Size = new Size(54, 13);
      this.label3.TabIndex = 14;
      this.label3.Text = "Template:";
      this.btProccessing.BackColor = SystemColors.Info;
      this.btProccessing.Location = new Point(330, 30);
      this.btProccessing.Name = "btProccessing";
      this.btProccessing.Size = new Size(75, 23);
      this.btProccessing.TabIndex = 5;
      this.btProccessing.Text = "Auto Tag";
      this.btProccessing.UseVisualStyleBackColor = false;
      this.btProccessing.Click += new EventHandler(this.btProcessing_Click_New);
      this.tbStartNumber.Location = new Point(95, 31);
      this.tbStartNumber.Name = "tbStartNumber";
      this.tbStartNumber.Size = new Size(107, 20);
      this.tbStartNumber.TabIndex = 13;

      this.label6.AutoSize = true;
      this.label6.Location = new Point(10, 35);
      this.label6.Name = "label6";
      this.label6.Size = new Size(72, 13);
      this.label6.TabIndex = 14;
      this.label6.Text = "Start Number:";
      this.label5.AutoSize = true;
      this.label5.Location = new Point(10, 9);
      this.label5.Name = "label5";
      this.label5.Size = new Size(74, 13);
      this.label5.TabIndex = 14;
      this.label5.Text = "Support Type:";
      this.label14.AutoSize = true;
      this.label14.Location = new Point(211, 61);
      this.label14.Name = "label14";
      this.label14.Size = new Size(102, 13);
      this.label14.TabIndex = 24;
      this.label14.Text = "Set Support Coding:";
      this.btAutoCodingForSelectedOnly.BackColor = SystemColors.Info;
      this.btAutoCodingForSelectedOnly.Location = new Point(330, 56); // Shifted Left
      this.btAutoCodingForSelectedOnly.Name = "btAutoCodingForSelectedOnly";
      this.btAutoCodingForSelectedOnly.Size = new Size(75, 23);
      this.btAutoCodingForSelectedOnly.TabIndex = 23;
      this.btAutoCodingForSelectedOnly.Text = "Selected Only";
      this.btAutoCodingForSelectedOnly.UseVisualStyleBackColor = false;
      this.btAutoCodingForSelectedOnly.Click += new EventHandler(this.btAutoCodingForSelectedOnly_Click);
      // NEW BUTTON: Current List
      this.btAutoCodingCurrentList = new Button();
      this.btAutoCodingCurrentList.BackColor = SystemColors.Info;
      this.btAutoCodingCurrentList.Location = new Point(410, 56);
      this.btAutoCodingCurrentList.Name = "btAutoCodingCurrentList";
      this.btAutoCodingCurrentList.Size = new Size(75, 23);
      this.btAutoCodingCurrentList.TabIndex = 91;
      this.btAutoCodingCurrentList.Text = "Current List";
      this.btAutoCodingCurrentList.UseVisualStyleBackColor = false;
      this.btAutoCodingCurrentList.Click += new EventHandler(new EventHandler(this.btAutoCodingCurrentList_Click));

      this.label11.AutoSize = true;
      this.label11.Location = new Point(211, 35);
      this.label11.Name = "label11";
      this.label11.Size = new Size(121, 13);
      this.label11.TabIndex = 19;
      this.label11.Text = "Set Pipe Support Tag:";
      this.label10.AutoSize = true;
      this.label10.Location = new Point(211, 9);
      this.label10.Name = "label10";
      this.label10.Size = new Size(109, 13);
      this.label10.TabIndex = 18;
      this.label10.Text = "Set Support Identifier:";
      this.btSetIdentifierCurrentSelection.BackColor = SystemColors.Info;
      this.btSetIdentifierCurrentSelection.Location = new Point(330, 4);
      this.btSetIdentifierCurrentSelection.Name = "btSetIdentifierCurrentSelection";
      this.btSetIdentifierCurrentSelection.Size = new Size(75, 23);
      this.btSetIdentifierCurrentSelection.TabIndex = 17;
      this.btSetIdentifierCurrentSelection.Text = "Selected Only";
      this.btSetIdentifierCurrentSelection.UseVisualStyleBackColor = false;
      this.btSetIdentifierCurrentSelection.Click += new EventHandler(this.btSetIdentifierCurrentSelection_Click);

      this.btPreProcess.BackColor = SystemColors.Info;
      this.btPreProcess.Location = new Point(490, 4); // Shifted Right
      this.btPreProcess.Name = "btPreProcess";
      this.btPreProcess.Size = new Size(80, 23);
      this.btPreProcess.TabIndex = 5;
      this.btPreProcess.Text = "Current Dwg";
      this.btPreProcess.UseVisualStyleBackColor = false;
      this.btPreProcess.Click += new EventHandler(this.btPreProcess_Click);
      this.btSetGrid.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      this.btSetGrid.BackColor = SystemColors.Info;
      this.btSetGrid.Location = new Point(490, 56);
      this.btSetGrid.Name = "btSetGrid";
      this.btSetGrid.Size = new Size(75, 23);
      this.btSetGrid.TabIndex = 17;
      this.btSetGrid.Text = "Set Grid";
      this.btSetGrid.UseVisualStyleBackColor = false;
      this.btSetGrid.Click += new EventHandler(this.btSetGrid_Click);
      this.tbDwgRevision.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      this.tbDwgRevision.Location = new Point(65, 29);
      this.tbDwgRevision.Margin = new Padding(2);
      this.tbDwgRevision.Name = "tbDwgRevision";
      this.tbDwgRevision.Size = new Size(500, 20);
      this.tbDwgRevision.TabIndex = 25;
      this.tbDwgRevision.TextChanged += new EventHandler(this.tbDwgRevision_TextChanged);
      this.tbProjectNo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      this.tbProjectNo.Location = new Point(65, 4);
      this.tbProjectNo.Margin = new Padding(2);
      this.tbProjectNo.Name = "tbProjectNo";
      this.tbProjectNo.Size = new Size(500, 20);
      this.tbProjectNo.TabIndex = 23;
      this.tbProjectNo.TextChanged += new EventHandler(this.tbProjectNo_TextChanged);
      this.label8.AutoSize = true;
      this.label8.BackColor = Color.LightSkyBlue;
      this.label8.Location = new Point(3, 33);
      this.label8.Margin = new Padding(2, 0, 2, 0);
      this.label8.Name = "label8";
      this.label8.Size = new Size(51, 13);
      this.label8.TabIndex = 21;
      this.label8.Text = "Revision:";
      this.label9.AutoSize = true;
      this.label9.BackColor = Color.LightSkyBlue;
      this.label9.Location = new Point(3, 8);
      this.label9.Margin = new Padding(2, 0, 2, 0);
      this.label9.Name = "label9";
      this.label9.Size = new Size(60, 13);
      this.label9.TabIndex = 19;
      this.label9.Text = "Project No:";
      this.btGetXGrid.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      this.btGetXGrid.BackColor = SystemColors.Info;
      this.btGetXGrid.Location = new Point(328, 56);
      this.btGetXGrid.Name = "btGetXGrid";
      this.btGetXGrid.Size = new Size(75, 23);
      this.btGetXGrid.TabIndex = 24;
      this.btGetXGrid.Text = "Get X Grid";
      this.btGetXGrid.UseVisualStyleBackColor = false;
      this.btGetXGrid.Click += new EventHandler(this.btGetXGrid_Click);
      this.btGetYGrid.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      this.btGetYGrid.BackColor = SystemColors.Info;
      this.btGetYGrid.Location = new Point(409, 56);
      this.btGetYGrid.Name = "btGetYGrid";
      this.btGetYGrid.Size = new Size(75, 23);
      this.btGetYGrid.TabIndex = 23;
      this.btGetYGrid.Text = "Get Y Grid";
      this.btGetYGrid.UseVisualStyleBackColor = false;
      this.btGetYGrid.Click += new EventHandler(this.btGetYGrid_Click);
      this.tbYlabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      this.tbYlabel.Location = new Point(65, 31);
      this.tbYlabel.Margin = new Padding(2);
      this.tbYlabel.Name = "tbYlabel";
      this.tbYlabel.Size = new Size(500, 20);
      this.tbYlabel.TabIndex = 18;
      this.tbYlabel.TextChanged += new EventHandler(this.tbYlabel_TextChanged);
      this.label7.AutoSize = true;
      this.label7.Location = new Point(3, 35);
      this.label7.Margin = new Padding(2, 0, 2, 0);
      this.label7.Name = "label7";
      this.label7.Size = new Size(46, 13);
      this.label7.TabIndex = 3;
      this.label7.Text = "Y Label:";
      this.tbXlabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      this.tbXlabel.Location = new Point(65, 5);
      this.tbXlabel.Margin = new Padding(2);
      this.tbXlabel.Name = "tbXlabel";
      this.tbXlabel.Size = new Size(500, 20);
      this.tbXlabel.TabIndex = 1;
      this.tbXlabel.TextChanged += new EventHandler(this.tbXlabel_TextChanged);
      this.label4.AutoSize = true;
      this.label4.Location = new Point(3, 9);
      this.label4.Margin = new Padding(2, 0, 2, 0);
      this.label4.Name = "label4";
      this.label4.Size = new Size(46, 13);
      this.label4.TabIndex = 0;
      this.label4.Text = "X Label:";
      this.tabMainUI.Alignment = TabAlignment.Left;
      this.tabMainUI.Controls.Add((Control) this.tabAuto2D);
      this.tabMainUI.Controls.Add((Control) this.tabPSData);
      this.tabMainUI.Controls.Add((Control) this.tabSupportCatalog);

      this.tabMainUI.Controls.Add((Control) this.tabSpanAutoPlace);

      this.tabMainUI.Dock = DockStyle.Fill;
      this.tabMainUI.Location = new Point(0, 0);
      this.tabMainUI.Multiline = true;
      this.tabMainUI.Name = "tabMainUI";
      
      // 
      // tabSpanAutoPlace
      // 
      this.tabSpanAutoPlace.BackColor = Color.LightSteelBlue;
      this.tabSpanAutoPlace.Controls.Add(this.btSpanSelectPipes);
      this.tabSpanAutoPlace.Controls.Add(this.lblLoadCase);
      this.tabSpanAutoPlace.Controls.Add(this.cbSpanLoadCase);
      this.tabSpanAutoPlace.Controls.Add(this.lblColTol);
      this.tabSpanAutoPlace.Controls.Add(this.nudCollisionTol);
      this.tabSpanAutoPlace.Controls.Add(this.lblRadius);
      this.tabSpanAutoPlace.Controls.Add(this.nudMarkerRadius);
      this.tabSpanAutoPlace.Controls.Add(this.btSpanPreview);
      this.tabSpanAutoPlace.Controls.Add(this.btSpanClear);
      this.tabSpanAutoPlace.Controls.Add(this.btSpanExport);
      this.tabSpanAutoPlace.Controls.Add(this.lblSpanSummary);
      this.tabSpanAutoPlace.Controls.Add(this.dgvSpanProposals);
      this.tabSpanAutoPlace.Controls.Add(this.lbSpanBlocked);
      this.tabSpanAutoPlace.Location = new Point(23, 4);
      this.tabSpanAutoPlace.Name = "tabSpanAutoPlace";
      this.tabSpanAutoPlace.Padding = new Padding(3);
      this.tabSpanAutoPlace.Size = new Size(578, 643);
      this.tabSpanAutoPlace.TabIndex = 2;
      this.tabSpanAutoPlace.Text = "Span Auto-Place";
      //
      // controls inside tabSpanAutoPlace
      //
      this.btSpanSelectPipes.Location = new Point(10, 10);
      this.btSpanSelectPipes.Size = new Size(100, 23);
      this.btSpanSelectPipes.Text = "Select Pipes";
      this.btSpanSelectPipes.Click += new EventHandler(this.btSpanSelectPipes_Click);
      
      this.lblLoadCase.Location = new Point(120, 15);
      this.lblLoadCase.Size = new Size(70, 15);
      this.lblLoadCase.Text = "Load Case:";
      
      this.cbSpanLoadCase.Location = new Point(190, 12);
      this.cbSpanLoadCase.Size = new Size(120, 21);
      this.cbSpanLoadCase.DropDownStyle = ComboBoxStyle.DropDownList;
      this.cbSpanLoadCase.Items.AddRange(new object[] { "Empty", "Full Water", "Empty+Ins", "FullWater+Ins" });
      this.cbSpanLoadCase.SelectedIndex = 1;

      this.lblColTol.Location = new Point(320, 15);
      this.lblColTol.Size = new Size(50, 15);
      this.lblColTol.Text = "Col. Tol:";

      this.nudCollisionTol.Location = new Point(370, 12);
      this.nudCollisionTol.Size = new Size(60, 20);
      this.nudCollisionTol.DecimalPlaces = 2;
      this.nudCollisionTol.Increment = 0.05M;
      this.nudCollisionTol.Value = 0.15M;

      this.lblRadius.Location = new Point(440, 15);
      this.lblRadius.Size = new Size(50, 15);
      this.lblRadius.Text = "Radius:";

      this.nudMarkerRadius.Location = new Point(490, 12);
      this.nudMarkerRadius.Size = new Size(60, 20);
      this.nudMarkerRadius.Maximum = 1000M;
      this.nudMarkerRadius.Value = 150M;

      this.btSpanPreview.Location = new Point(10, 40);
      this.btSpanPreview.Size = new Size(100, 30);
      this.btSpanPreview.Text = "Preview";
      this.btSpanPreview.Click += new EventHandler(this.btSpanPreview_Click);

      this.btSpanClear.Location = new Point(120, 40);
      this.btSpanClear.Size = new Size(100, 30);
      this.btSpanClear.Text = "Clear Preview";
      this.btSpanClear.Click += new EventHandler(this.btSpanClear_Click);

      this.btSpanExport.Location = new Point(230, 40);
      this.btSpanExport.Size = new Size(100, 30);
      this.btSpanExport.Text = "Export";
      this.btSpanExport.Click += new EventHandler(this.btSpanExport_Click);

      this.lblSpanSummary.Location = new Point(340, 48);
      this.lblSpanSummary.Size = new Size(200, 15);
      this.lblSpanSummary.Text = "Proposals: 0 | Blocked: 0";

      this.dgvSpanProposals.Location = new Point(10, 80);
      this.dgvSpanProposals.Size = new Size(550, 400);
      this.dgvSpanProposals.AllowUserToAddRows = false;
      this.dgvSpanProposals.ReadOnly = true;
      
      this.lbSpanBlocked.Location = new Point(10, 490);
      this.lbSpanBlocked.Size = new Size(550, 140);

      
      // 
      // tabSupportCatalog
      // 
      this.tabSupportCatalog.BackColor = Color.LightSteelBlue;
      this.tabSupportCatalog.Controls.Add(this.lblCatType);
      this.tabSupportCatalog.Controls.Add(this.cbCatType);
      this.tabSupportCatalog.Controls.Add(this.dgvCatVariants);
      this.tabSupportCatalog.Controls.Add(this.pnlCatParams);
      this.tabSupportCatalog.Controls.Add(this.lblCatDesc);
      this.tabSupportCatalog.Controls.Add(this.tbCatDescOverride);
      this.tabSupportCatalog.Controls.Add(this.btCatAddVariant);
      this.tabSupportCatalog.Controls.Add(this.btCatUpdate);
      this.tabSupportCatalog.Controls.Add(this.btCatDelete);
      this.tabSupportCatalog.Controls.Add(this.btCatBrowse);
      this.tabSupportCatalog.Controls.Add(this.lblCatPath);
      this.tabSupportCatalog.Controls.Add(this.lblCatStatus);
      this.tabSupportCatalog.Location = new Point(23, 4);
      this.tabSupportCatalog.Name = "tabSupportCatalog";
      this.tabSupportCatalog.Padding = new Padding(3);
      this.tabSupportCatalog.Size = new Size(578, 643);
      this.tabSupportCatalog.TabIndex = 3;
      this.tabSupportCatalog.Text = "Support Catalog";
      //
      // controls inside tabSupportCatalog
      //
      this.lblCatType.Location = new Point(10, 15);
      this.lblCatType.Size = new Size(100, 15);
      this.lblCatType.Text = "Support Type:";

      this.cbCatType.Location = new Point(110, 12);
      this.cbCatType.Size = new Size(200, 21);
      this.cbCatType.DropDownStyle = ComboBoxStyle.DropDownList;
      this.cbCatType.SelectedIndexChanged += new EventHandler(this.cbCatType_SelectedIndexChanged);

      this.btCatBrowse.Location = new Point(320, 11);
      this.btCatBrowse.Size = new Size(110, 23);
      this.btCatBrowse.Text = "Load .acat...";
      this.btCatBrowse.Click += new EventHandler(this.btCatBrowse_Click);

      this.lblCatPath.Location = new Point(10, 533);
      this.lblCatPath.Size = new Size(550, 15);
      this.lblCatPath.AutoEllipsis = true;
      this.lblCatPath.ForeColor = Color.DimGray;
      this.lblCatPath.Text = "";

      this.dgvCatVariants.Location = new Point(10, 40);
      this.dgvCatVariants.Size = new Size(550, 250);
      this.dgvCatVariants.AllowUserToAddRows = false;
      this.dgvCatVariants.ReadOnly = true;
      this.dgvCatVariants.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
      this.dgvCatVariants.SelectionChanged += new EventHandler(this.dgvCatVariants_SelectionChanged);

      this.pnlCatParams.Location = new Point(10, 300);
      this.pnlCatParams.Size = new Size(550, 150);
      this.pnlCatParams.BorderStyle = BorderStyle.FixedSingle;
      this.pnlCatParams.AutoScroll = true;

      this.lblCatDesc.Location = new Point(10, 460);
      this.lblCatDesc.Size = new Size(100, 15);
      this.lblCatDesc.Text = "Override Desc:";

      this.tbCatDescOverride.Location = new Point(110, 457);
      this.tbCatDescOverride.Size = new Size(300, 20);

      this.btCatAddVariant.Location = new Point(420, 455);
      this.btCatAddVariant.Size = new Size(140, 25);
      this.btCatAddVariant.Text = "Add Variant";
      this.btCatAddVariant.Click += new EventHandler(this.btCatAddVariant_Click);

      this.lblCatStatus.Location = new Point(10, 550);
      this.lblCatStatus.Size = new Size(550, 60);
      this.lblCatStatus.Text = "Status: Ready";

      this.btCatUpdate.Location = new Point(420, 485);
      this.btCatUpdate.Size = new Size(140, 25);
      this.btCatUpdate.Text = "Update Selected";
      this.btCatUpdate.Click += new EventHandler(this.btCatUpdate_Click);

      this.btCatDelete.Location = new Point(420, 515);
      this.btCatDelete.Size = new Size(140, 25);
      this.btCatDelete.Text = "Delete Selected";
      this.btCatDelete.Click += new EventHandler(this.btCatDelete_Click);


      this.tabMainUI.SelectedIndex = 0;
      this.tabMainUI.SelectedIndexChanged += new EventHandler(this.tabMainUI_SelectedIndexChanged);
      this.tabMainUI.Size = new Size(605, 651);
      this.tabMainUI.TabIndex = 17;
      this.tabAuto2D.BackColor = Color.LightSteelBlue;
      this.tabAuto2D.Controls.Add((Control) this.label13);
      this.tabAuto2D.Controls.Add((Control) this.panel3);
      this.tabAuto2D.Controls.Add((Control) this.panel2);
      this.tabAuto2D.Controls.Add((Control) this.btExportMTO);
      this.tabAuto2D.Controls.Add((Control) this.panel1);
      this.tabAuto2D.Controls.Add((Control) this.cbbViewDirection);
      this.tabAuto2D.Controls.Add((Control) this.label1);
      this.tabAuto2D.Controls.Add((Control) this.btAdd);
      this.tabAuto2D.Controls.Add((Control) this.lvSupportName);
      this.tabAuto2D.Controls.Add((Control) this.btExport);
      this.tabAuto2D.Location = new Point(23, 4);
      this.tabAuto2D.Name = "tabAuto2D";
      this.tabAuto2D.Padding = new Padding(3);
      this.tabAuto2D.Size = new Size(578, 643);
      this.tabAuto2D.TabIndex = 0;
      this.tabAuto2D.Text = "PS AUTO2D";
      this.label13.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      this.label13.Location = new Point(209, 627);
      this.label13.Name = "label13";
      this.label13.Size = new Size(173, 13);
      this.label13.TabIndex = 25;
      this.label13.Text = "Copyright By PlantFlow";
      this.label13.TextAlign = ContentAlignment.MiddleCenter;
      this.panel3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      this.panel3.BackColor = Color.LightSkyBlue;
      this.panel3.BorderStyle = BorderStyle.FixedSingle;
      this.panel3.Controls.Add((Control) this.tbXlabel);
      this.panel3.Controls.Add((Control) this.tbYlabel);
      this.panel3.Controls.Add((Control) this.label4);
      this.panel3.Controls.Add((Control) this.label7);
      this.panel3.Controls.Add((Control) this.btSetGrid);
      this.panel3.Controls.Add((Control) this.btGetYGrid);
      this.panel3.Controls.Add((Control) this.btGetXGrid);
      this.panel3.Location = new Point(3, 341);
      this.panel3.Name = "panel3";
      this.panel3.Size = new Size(572, 85);
      this.panel3.TabIndex = 30;
      this.panel2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      this.panel2.BackColor = Color.LightSkyBlue;
      this.panel2.BorderStyle = BorderStyle.FixedSingle;
      this.panel2.Controls.Add((Control) this.label3);
      this.panel2.Controls.Add((Control) this.label2);
      this.panel2.Controls.Add((Control) this.tbTemplate);
      this.panel2.Controls.Add((Control) this.tbSaveMTOAs);
      this.panel2.Location = new Point(3, 281);
      this.panel2.Name = "panel2";
      this.panel2.Size = new Size(572, 55);
      this.panel2.TabIndex = 29;
      this.panel1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      this.panel1.BackColor = Color.LightSkyBlue;
      this.panel1.BorderStyle = BorderStyle.FixedSingle;
      this.panel1.Controls.Add((Control) this.tbDwgRevision);
      this.panel1.Controls.Add((Control) this.tbProjectNo);
      this.panel1.Controls.Add((Control) this.label8);
      this.panel1.Controls.Add((Control) this.label9);
      this.panel1.Location = new Point(3, 431);
      this.panel1.Name = "panel1";
      this.panel1.Size = new Size(572, 55);
      this.panel1.TabIndex = 28;
      this.tabPSData.BackColor = Color.LightSteelBlue;


      this.tabPSData.Controls.Add((Control) this.btProjectScan);
      this.tabPSData.Controls.Add((Control) this.btRefresh);
      this.tabPSData.Controls.Add((Control) this.dgvPSDM);
      this.tabPSData.Controls.Add((Control) this.panel5);
      this.lb1.AutoSize = true;
      this.lb1.Location = new Point(430, 630);
      this.lb1.Name = "lb1";
      this.lb1.Size = new Size(100, 13);
      this.lb1.TabIndex = 26;
      this.lb1.Text = "Copyright (c) By PlantFlow";
      this.lb1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      this.tabPSData.Controls.Add((Control) this.lb1);
      this.tabPSData.Location = new Point(23, 4);
      this.tabPSData.Name = "tabPSData";
      this.tabPSData.Padding = new Padding(3);
      this.tabPSData.Size = new Size(578, 643);
      this.tabPSData.TabIndex = 1;
      this.tabPSData.Text = "PS DATA";

      this.dgvPSDM.AllowUserToAddRows = false;
      this.dgvPSDM.AllowUserToDeleteRows = false;
      this.dgvPSDM.AllowUserToResizeColumns = false;
      this.dgvPSDM.AllowUserToResizeRows = false;
      this.dgvPSDM.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      this.dgvPSDM.BackgroundColor = Color.LightSkyBlue;
      this.dgvPSDM.CellBorderStyle = DataGridViewCellBorderStyle.Sunken;
      this.dgvPSDM.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this.dgvPSDM.Location = new Point(3, 177);
      this.dgvPSDM.Name = "dgvPSDM";
      this.dgvPSDM.Size = new Size(572, 447);
      this.dgvPSDM.TabIndex = 30;
      this.panel5.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      this.panel5.BackColor = Color.LightSkyBlue;
      this.panel5.BorderStyle = BorderStyle.FixedSingle;
      this.panel5.Controls.Add((Control) this.btAutoCodingCurrentList); // Add to Panel

      this.panel5.Controls.Add((Control) this.btAutoCodingForSelectedOnly);
      this.panel5.Controls.Add((Control) this.label14);
      this.panel5.Controls.Add((Control) this.btProccessing);
      this.panel5.Controls.Add((Control) this.label11);
      this.panel5.Controls.Add((Control) this.btPreProcess);
      this.panel5.Controls.Add((Control) this.label10);
      this.panel5.Controls.Add((Control) this.label5);
      this.panel5.Controls.Add((Control) this.btSetIdentifierCurrentSelection);
      this.panel5.Controls.Add((Control) this.btSetIdentifierCurrentList); // Add to Panel
      this.panel5.Controls.Add((Control) this.label6);
      this.cbSupportType = new ComboBox(); // Restore context
      this.cbSupportType.Location = new Point(95, 9);
      this.cbSupportType.Name = "cbSupportType";
      this.cbSupportType.Size = new Size(107, 21);
      this.cbSupportType.TabIndex = 1;
      this.cbSupportType.DropDown += new EventHandler(this.cbSupportType_DropDown);
      this.cbSupportType.SelectedIndexChanged += new EventHandler(this.cbSupportType_SelectedIndexChanged);
      this.cbSupportType.DropDownStyle = ComboBoxStyle.DropDown;
      this.panel5.Controls.Add((Control) this.cbSupportType);
      this.panel5.Controls.Add((Control) this.tbStartNumber);

      this.panel5.Location = new Point(3, 59);
      this.panel5.Name = "panel5";
      this.panel5.Size = new Size(572, 85);
      this.panel5.TabIndex = 28;
      this.csmPSDM.Items.AddRange(new ToolStripItem[1]
      {
        (ToolStripItem) this.tsmRefresh
      });
      this.csmPSDM.Name = "csmPSDM";
      this.csmPSDM.Size = new Size(114, 26);
      this.tsmRefresh.Name = "tsmRefresh";
      this.tsmRefresh.Size = new Size(113, 22);
      this.tsmRefresh.Text = "Refresh";

      this.btRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      this.btRefresh.BackColor = SystemColors.Info;
      this.btRefresh.Location = new Point(445, 150);
      this.btRefresh.Name = "btRefresh";
      this.btRefresh.Size = new Size(100, 23);
      this.btRefresh.TabIndex = 32;
      this.btRefresh.Text = "Refresh";
      this.btRefresh.UseVisualStyleBackColor = false;
      this.btRefresh.Click += new EventHandler(this.btRefresh_Click);
      
      // btProjectScan
      this.btProjectScan.BackColor = SystemColors.Info;
      this.btProjectScan.Location = new Point(8, 8);
      this.btProjectScan.Name = "btProjectScan";
      this.btProjectScan.Size = new Size(120, 30);
      this.btProjectScan.TabIndex = 99;
      this.btProjectScan.Text = "Project Scan";
      this.btProjectScan.UseVisualStyleBackColor = false;
      this.btProjectScan.Click += new EventHandler(this.btProjectScan_Click);
      this.AutoScaleDimensions = new SizeF(6f, 13f);
      this.AutoScaleMode = AutoScaleMode.Font;
      this.BackColor = SystemColors.ControlLight;
      this.Controls.Add((Control) this.tabMainUI);
      this.Name = nameof (PaletteTab);
      this.Size = new Size(605, 651);
      this.lvcMenuStrip.ResumeLayout(false);
      this.tabMainUI.ResumeLayout(false);
      this.tabAuto2D.ResumeLayout(false);
      this.tabAuto2D.PerformLayout();
      this.panel3.ResumeLayout(false);
      this.panel3.PerformLayout();
      this.panel2.ResumeLayout(false);
      this.panel2.PerformLayout();
      this.panel1.ResumeLayout(false);
      this.panel1.PerformLayout();
      this.tabPSData.ResumeLayout(false);
      this.tabSupportCatalog.ResumeLayout(false);
      this.tabSupportCatalog.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)(this.dgvCatVariants)).EndInit();

      this.tabSpanAutoPlace.ResumeLayout(false);
      this.tabSpanAutoPlace.PerformLayout();
      ((ISupportInitialize)(this.nudCollisionTol)).EndInit();
      ((ISupportInitialize)(this.nudMarkerRadius)).EndInit();
      ((ISupportInitialize)(this.dgvSpanProposals)).EndInit();

      this.tabPSData.PerformLayout();

      ((ISupportInitialize) this.dgvPSDM).EndInit();
      this.panel5.ResumeLayout(false);
      this.panel5.PerformLayout();
      this.csmPSDM.ResumeLayout(false);
      this.ResumeLayout(false);
    }
  }
}
