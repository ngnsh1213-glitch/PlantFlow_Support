using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PlantFlow_Support
{
    public class FormProjectScanResult : Form
    {
        private DataGridView dgvResults;
        private TextBox tbMaxUB;
        private TextBox tbMaxUBC;
        private Label lblTotalUB;
        private Label lblTotalUBC;
        private Button btSave;
        private Button btClose;

        public int MaxUB { get; private set; }
        public int MaxUBC { get; private set; }
        public bool Saved { get; private set; } = false;

        public FormProjectScanResult(List<DrawingScanResult> results, int currentMaxUB, int currentMaxUBC)
        {
            this.Text = "Project Scanning Result";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;

            InitializeUI(results);
            
            this.tbMaxUB.Text = currentMaxUB.ToString();
            this.tbMaxUBC.Text = currentMaxUBC.ToString();
            
            // Calculate Totals
            int totalUB = results.Sum(r => r.CountUB);
            int totalUBC = results.Sum(r => r.CountUBC);
            this.lblTotalUB.Text = $"Total UB: {totalUB}";
            this.lblTotalUBC.Text = $"Total UBC: {totalUBC}";
        }

        private void InitializeUI(List<DrawingScanResult> results)
        {
            // Grid
            dgvResults = new DataGridView();
            dgvResults.Dock = DockStyle.Top;
            dgvResults.Height = 350;
            dgvResults.AllowUserToAddRows = false;
            dgvResults.AllowUserToDeleteRows = false;
            dgvResults.ReadOnly = true;
            dgvResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            
            dgvResults.Columns.Add("Dwg", "Drawing Name");
            dgvResults.Columns.Add("UB", "UB Count");
            dgvResults.Columns.Add("UBC", "UBC Count");
            dgvResults.Columns.Add("Total", "Total");

            foreach(var r in results)
            {
                dgvResults.Rows.Add(r.DrawingName, r.CountUB, r.CountUBC, r.CountUB + r.CountUBC);
            }
            
            this.Controls.Add(dgvResults);

            // Bottom Panel
            Panel panelBottom = new Panel();
            panelBottom.Dock = DockStyle.Bottom;
            panelBottom.Height = 100;
            panelBottom.Padding = new Padding(10);
            
            // Layout Controls
            Label l1 = new Label() { Text = "Max UB Tag:", Location = new Point(20, 15), AutoSize = true };
            tbMaxUB = new TextBox() { Location = new Point(120, 12), Width = 80 };
            
            Label l2 = new Label() { Text = "Max UBC Tag:", Location = new Point(220, 15), AutoSize = true };
            tbMaxUBC = new TextBox() { Location = new Point(320, 12), Width = 80 };

            lblTotalUB = new Label() { Text = "Total UB: 0", Location = new Point(20, 50), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };
            lblTotalUBC = new Label() { Text = "Total UBC: 0", Location = new Point(220, 50), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };

            btSave = new Button() { Text = "Save", Location = new Point(400, 40), Size = new Size(80, 30), BackColor = SystemColors.Info };
            btSave.Click += BtSave_Click;

            // Export 2D Button
            Button btExport2D = new Button() { Text = "Export 2D", Location = new Point(20, 100), Size = new Size(100, 30), BackColor = Color.LightBlue };
            btExport2D.Location = new Point(310, 40); // Align with others
            btExport2D.Click += BtExport2D_Click;

            btClose = new Button() { Text = "Close", Location = new Point(490, 40), Size = new Size(80, 30) };
            btClose.Click += (s, e) => { this.Close(); };

            panelBottom.Controls.Add(l1);
            panelBottom.Controls.Add(tbMaxUB);
            panelBottom.Controls.Add(l2);
            panelBottom.Controls.Add(tbMaxUBC);
            panelBottom.Controls.Add(lblTotalUB);
            panelBottom.Controls.Add(lblTotalUBC);
            panelBottom.Controls.Add(btExport2D); // Add button
            panelBottom.Controls.Add(btSave);
            panelBottom.Controls.Add(btClose);

            this.Controls.Add(panelBottom);
        }

        public event EventHandler<List<string>> OnExport2DRequested;

        private void BtExport2D_Click(object sender, EventArgs e)
        {
            // Gather selected drawings or all if none selected
            List<string> drawingsToExport = new List<string>();
            if(dgvResults.SelectedRows.Count > 0)
            {
                foreach(DataGridViewRow r in dgvResults.SelectedRows)
                {
                    drawingsToExport.Add(r.Cells["Dwg"].Value.ToString());
                }
            }
            else
            {
                 // Ask if export all?
                 if(MessageBox.Show("Export 2D for ALL drawings in list?", "Confirm Export", MessageBoxButtons.YesNo) == DialogResult.Yes)
                 {
                     foreach(DataGridViewRow r in dgvResults.Rows)
                     {
                         drawingsToExport.Add(r.Cells["Dwg"].Value.ToString());
                     }
                 }
                 else return;
            }
            
            OnExport2DRequested?.Invoke(this, drawingsToExport);
        }

        private void BtSave_Click(object sender, EventArgs e)
        {
            if (int.TryParse(tbMaxUB.Text, out int ub) && int.TryParse(tbMaxUBC.Text, out int ubc))
            {
                this.MaxUB = ub;
                this.MaxUBC = ubc;
                this.Saved = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please enter valid numbers for Max Tags.");
            }
        }
    }

    public class DrawingScanResult
    {
        public string DrawingName { get; set; }
        public int CountUB { get; set; }
        public int CountUBC { get; set; }
        public int DwgId { get; set; } // Added DwgId to assist lookup
    }
}
