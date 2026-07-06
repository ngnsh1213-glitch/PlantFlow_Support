using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

#nullable disable
namespace PlantFlow_Support
{
  public class formExistedStdType : Form
  {
    private System.Data.DataTable data_table;
    private IContainer components;
    private Button btExportToText;
    private DataGridView dgvExistedType;

    public formExistedStdType() => this.InitializeComponent();

    private void btExportToText_Click(object sender, EventArgs e)
    {
      List<string> stringList = new List<string>();
      foreach (DataRow row in (InternalDataCollectionBase) this.data_table.Rows)
      {
        string str = row[0]?.ToString() + ";" + row[1]?.ToString();
        stringList.Add(str);
      }
      using (StreamWriter streamWriter = new StreamWriter("C:\\TEMP\\ExistedType.txt"))
      {
        foreach (string str in stringList)
          streamWriter.WriteLine(str);
      }
      int num = (int) MessageBox.Show("Successfull!", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
    }

    public void ShowForm(List<string> l1, List<string> l2)
    {
      this.data_table = new System.Data.DataTable();
      this.data_table.Columns.Add("Short Description", typeof (string));
      this.data_table.Columns.Add("Support Identifier", typeof (string));
      int num1 = ((IEnumerable<int>) new int[2]
      {
        l1.Count,
        l2.Count
      }).Max();
      for (int index = 0; index < num1; ++index)
      {
        string str1 = "";
        string str2 = "";
        if (index < l1.Count)
          str1 = l1[index];
        if (index < l2.Count)
          str2 = l2[index];
        this.data_table.Rows.Add((object) str1, (object) str2);
      }
      this.dgvExistedType.DataSource = (object) this.data_table;
      this.dgvExistedType.Columns[0].ReadOnly = true;
      this.dgvExistedType.Columns[1].ReadOnly = true;
      this.dgvExistedType.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
      this.dgvExistedType.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
      int num2 = (int) this.ShowDialog();
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && this.components != null)
        this.components.Dispose();
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      this.btExportToText = new Button();
      this.dgvExistedType = new DataGridView();
      ((ISupportInitialize) this.dgvExistedType).BeginInit();
      this.SuspendLayout();
      this.btExportToText.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      this.btExportToText.Location = new Point(286, 452);
      this.btExportToText.Name = "btExportToText";
      this.btExportToText.Size = new Size(130, 30);
      this.btExportToText.TabIndex = 0;
      this.btExportToText.Text = "Export To Text";
      this.btExportToText.UseVisualStyleBackColor = true;
      this.btExportToText.Click += new EventHandler(this.btExportToText_Click);
      this.dgvExistedType.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      this.dgvExistedType.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this.dgvExistedType.Location = new Point(13, 13);
      this.dgvExistedType.Name = "dgvExistedType";
      this.dgvExistedType.RowHeadersWidth = 62;
      this.dgvExistedType.RowTemplate.Height = 28;
      this.dgvExistedType.Size = new Size(403, 433);
      this.dgvExistedType.TabIndex = 1;
      this.AutoScaleDimensions = new SizeF(9f, 20f);
      this.AutoScaleMode = AutoScaleMode.Font;
      this.BackColor = SystemColors.Control;
      this.ClientSize = new Size(428, 494);
      this.Controls.Add((Control) this.dgvExistedType);
      this.Controls.Add((Control) this.btExportToText);
      this.FormBorderStyle = FormBorderStyle.FixedSingle;
      this.Name = nameof (formExistedStdType);
      this.Text = "Existed Standard Type";
      ((ISupportInitialize) this.dgvExistedType).EndInit();
      this.ResumeLayout(false);
    }
  }
}
