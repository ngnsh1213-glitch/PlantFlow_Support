using Autodesk.ProcessPower.PlantInstance;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

#nullable disable
namespace PlantFlow_Support
{
  public class formDBSetting : Form
  {
    private IContainer components;
    private Label label3;
    private CheckBox cbAdmin;
    private Label lbSuccessful;
    private Label label4;
    private TextBox tbProjectName;
    private Label lbGetProjectName;
    private Button btRevTableSet;
    private TextBox tbTableDirectory;
    private Label lbTableDirectory;

    public formDBSetting() => this.InitializeComponent();

    private void btRevTableSet_Click(object sender, EventArgs e)
    {
      string sourceFileName = "C:\\TEMP\\SUPPORT\\Misc\\DBTable.db";
      string destFileName = this.tbTableDirectory.Text + "\\" + this.tbProjectName.Text + "_PSDBTable.db";
      if (!File.Exists(destFileName.ToLower()))
      {
        File.Copy(sourceFileName, destFileName);
        this.lbSuccessful.Visible = true;
      }
      else
      {
        int num = (int) MessageBox.Show("Revision table for this project already existed, please check with your admin!!!", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
    }

    private void cbAdmin_CheckedChanged(object sender, EventArgs e)
    {
      if (this.cbAdmin.Checked)
      {
        this.lbTableDirectory.Enabled = true;
        this.lbGetProjectName.Enabled = true;
        this.tbTableDirectory.Enabled = true;
        this.tbProjectName.Enabled = true;
        this.btRevTableSet.Enabled = true;
        this.btRevTableSet.BackColor = Color.Ivory;
      }
      else
      {
        this.lbTableDirectory.Enabled = false;
        this.lbGetProjectName.Enabled = false;
        this.tbTableDirectory.Enabled = false;
        this.tbProjectName.Enabled = false;
        this.btRevTableSet.Enabled = false;
        this.btRevTableSet.BackColor = SystemColors.Menu;
      }
    }

    private void lbTableDirectory_Click(object sender, EventArgs e)
    {
      FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
      if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
        return;
      this.tbTableDirectory.Text = folderBrowserDialog.SelectedPath;
    }

    private void lbGetProjectName_Click(object sender, EventArgs e)
    {
      this.tbProjectName.Text = PlantApplication.CurrentProject.Name;
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && this.components != null)
        this.components.Dispose();
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      this.label3 = new Label();
      this.cbAdmin = new CheckBox();
      this.lbSuccessful = new Label();
      this.label4 = new Label();
      this.tbProjectName = new TextBox();
      this.lbGetProjectName = new Label();
      this.btRevTableSet = new Button();
      this.tbTableDirectory = new TextBox();
      this.lbTableDirectory = new Label();
      this.SuspendLayout();
      this.label3.AutoSize = true;
      this.label3.FlatStyle = FlatStyle.System;
      this.label3.Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, (byte) 0);
      this.label3.ForeColor = Color.IndianRed;
      this.label3.Location = new Point(11, 9);
      this.label3.Margin = new Padding(2, 0, 2, 0);
      this.label3.Name = "label3";
      this.label3.Size = new Size(467, 26);
      this.label3.TabIndex = 7;
      this.label3.Text = "WARNING!!!\r\nThis field for Admin only, please tick to checkbox if you are admin of this project.";
      this.cbAdmin.AutoSize = true;
      this.cbAdmin.Location = new Point(11, 39);
      this.cbAdmin.Margin = new Padding(2);
      this.cbAdmin.Name = "cbAdmin";
      this.cbAdmin.Size = new Size(137, 17);
      this.cbAdmin.TabIndex = 8;
      this.cbAdmin.Text = "I'm Admin of this project";
      this.cbAdmin.UseVisualStyleBackColor = true;
      this.cbAdmin.CheckedChanged += new EventHandler(this.cbAdmin_CheckedChanged);
      this.lbSuccessful.AutoSize = true;
      this.lbSuccessful.FlatStyle = FlatStyle.System;
      this.lbSuccessful.Font = new Font("Microsoft Sans Serif", 10f, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, (byte) 0);
      this.lbSuccessful.ForeColor = Color.DodgerBlue;
      this.lbSuccessful.Location = new Point(11, 112);
      this.lbSuccessful.Margin = new Padding(2, 0, 2, 0);
      this.lbSuccessful.Name = "lbSuccessful";
      this.lbSuccessful.Size = new Size(98, 17);
      this.lbSuccessful.TabIndex = 15;
      this.lbSuccessful.Text = "Successful!!!";
      this.lbSuccessful.Visible = false;
      this.label4.AutoSize = true;
      this.label4.Location = new Point(174, 134);
      this.label4.Margin = new Padding(2, 0, 2, 0);
      this.label4.Name = "label4";
      this.label4.Size = new Size(152, 13);
      this.label4.TabIndex = 14;
      this.label4.Text = "Copyright (c) by NghiaBT 2025";
      this.tbProjectName.Enabled = false;
      this.tbProjectName.Location = new Point(98, 85);
      this.tbProjectName.Margin = new Padding(2);
      this.tbProjectName.Name = "tbProjectName";
      this.tbProjectName.Size = new Size(386, 20);
      this.tbProjectName.TabIndex = 13;
      this.lbGetProjectName.AutoSize = true;
      this.lbGetProjectName.BorderStyle = BorderStyle.Fixed3D;
      this.lbGetProjectName.Cursor = Cursors.Hand;
      this.lbGetProjectName.Enabled = false;
      this.lbGetProjectName.FlatStyle = FlatStyle.Popup;
      this.lbGetProjectName.Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Underline, GraphicsUnit.Point, (byte) 0);
      this.lbGetProjectName.ForeColor = SystemColors.HotTrack;
      this.lbGetProjectName.Location = new Point(11, 88);
      this.lbGetProjectName.Margin = new Padding(2, 0, 2, 0);
      this.lbGetProjectName.Name = "lbGetProjectName";
      this.lbGetProjectName.Size = new Size(76, 15);
      this.lbGetProjectName.TabIndex = 12;
      this.lbGetProjectName.Text = "Project Name:";
      this.lbGetProjectName.Click += new EventHandler(this.lbGetProjectName_Click);
      this.btRevTableSet.BackColor = SystemColors.Menu;
      this.btRevTableSet.Enabled = false;
      this.btRevTableSet.FlatStyle = FlatStyle.Popup;
      this.btRevTableSet.Location = new Point(401, 112);
      this.btRevTableSet.Margin = new Padding(2);
      this.btRevTableSet.Name = "btRevTableSet";
      this.btRevTableSet.Size = new Size(83, 21);
      this.btRevTableSet.TabIndex = 11;
      this.btRevTableSet.Text = "Set Table";
      this.btRevTableSet.UseVisualStyleBackColor = false;
      this.btRevTableSet.Click += new EventHandler(this.btRevTableSet_Click);
      this.tbTableDirectory.Enabled = false;
      this.tbTableDirectory.Location = new Point(98, 59);
      this.tbTableDirectory.Margin = new Padding(2);
      this.tbTableDirectory.Name = "tbTableDirectory";
      this.tbTableDirectory.Size = new Size(386, 20);
      this.tbTableDirectory.TabIndex = 10;
      this.lbTableDirectory.AutoSize = true;
      this.lbTableDirectory.BorderStyle = BorderStyle.Fixed3D;
      this.lbTableDirectory.Cursor = Cursors.Hand;
      this.lbTableDirectory.Enabled = false;
      this.lbTableDirectory.FlatStyle = FlatStyle.Popup;
      this.lbTableDirectory.Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Underline, GraphicsUnit.Point, (byte) 0);
      this.lbTableDirectory.ForeColor = SystemColors.HotTrack;
      this.lbTableDirectory.Location = new Point(11, 62);
      this.lbTableDirectory.Margin = new Padding(2, 0, 2, 0);
      this.lbTableDirectory.Name = "lbTableDirectory";
      this.lbTableDirectory.Size = new Size(73, 15);
      this.lbTableDirectory.TabIndex = 9;
      this.lbTableDirectory.Text = "File Directory:";
      this.lbTableDirectory.Click += new EventHandler(this.lbTableDirectory_Click);
      this.AutoScaleDimensions = new SizeF(6f, 13f);
      this.AutoScaleMode = AutoScaleMode.Font;
      this.ClientSize = new Size(497, 153);
      this.Controls.Add((Control) this.lbSuccessful);
      this.Controls.Add((Control) this.label4);
      this.Controls.Add((Control) this.tbProjectName);
      this.Controls.Add((Control) this.lbGetProjectName);
      this.Controls.Add((Control) this.btRevTableSet);
      this.Controls.Add((Control) this.tbTableDirectory);
      this.Controls.Add((Control) this.lbTableDirectory);
      this.Controls.Add((Control) this.cbAdmin);
      this.Controls.Add((Control) this.label3);
      this.ForeColor = SystemColors.ControlText;
      this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
      this.Name = nameof (formDBSetting);
      this.Text = "Pipe Support DB Setting";
      this.ResumeLayout(false);
      this.PerformLayout();
    }
  }
}
