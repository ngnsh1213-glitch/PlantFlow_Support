using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.ProcessPower.PnP3dObjects;

namespace PlantFlow_Support
{
    public partial class PaletteTab
    {
        private SpanPreviewRenderer _spanRenderer = new SpanPreviewRenderer();
        private List<ObjectId> _spanPipeIds = new List<ObjectId>();
        private List<SupportProposal> _lastProposals = new List<SupportProposal>();
        private List<EligibilityResult> _lastBlocked = new List<EligibilityResult>();

        private void btSpanSelectPipes_Click(object sender, EventArgs e)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = "\nSelect Pipes for Span Auto-Place: ";
            PromptSelectionResult sel = ed.GetSelection(pso);

            if (sel.Status == PromptStatus.OK && sel.Value != null)
            {
                _spanPipeIds.Clear();
                foreach (SelectedObject so in sel.Value)
                {
                    _spanPipeIds.Add(so.ObjectId);
                }
                lblSpanSummary.Text = $"Pipes selected: {_spanPipeIds.Count}";
                _spanRenderer.Clear(); 
            }
        }

        private void btSpanPreview_Click(object sender, EventArgs e)
        {
            if (_spanPipeIds == null || _spanPipeIds.Count == 0)
            {
                MessageBox.Show("Please select pipes first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;

            string jsonPath = SpanTable.ResolveDefaultPath();
            if (jsonPath == null)
            {
                MessageBox.Show("Cannot find span_table_JIS.json", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SpanTable table;
            try
            {
                table = SpanTable.Load(jsonPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load SpanTable: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LoadCase caseSelection = LoadCase.FullWater;
            string selectedCase = cbSpanLoadCase.SelectedItem?.ToString();
            if (selectedCase == "Empty") caseSelection = LoadCase.Empty;
            else if (selectedCase == "Empty+Ins") caseSelection = LoadCase.EmptyIns;
            else if (selectedCase == "FullWater+Ins") caseSelection = LoadCase.FullWaterIns;

            double collisionTol = (double)nudCollisionTol.Value;

            var placer = new SpanPlacer();
            var topo = new PlantTopologyResolver();

            _lastProposals.Clear();
            _lastBlocked.Clear();

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    _lastProposals = placer.ComputeProposals(_spanPipeIds, caseSelection, table, topo, tr, collisionTol, out _lastBlocked);
                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error computing proposals: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            dgvSpanProposals.DataSource = _lastProposals.Select((p, i) => new
            {
                No = i + 1,
                Dn = p.Dn,
                Symbol = p.Symbol,
                X = Math.Round(p.Position.X, 2),
                Y = Math.Round(p.Position.Y, 2),
                Z = Math.Round(p.Position.Z, 2),
                Confidence = p.Confidence
            }).ToList();

            lbSpanBlocked.Items.Clear();
            foreach (var b in _lastBlocked)
            {
                lbSpanBlocked.Items.Add($"Pipe {b.Source.Handle.Value.ToString("X")}: {b.Reason}");
            }

            lblSpanSummary.Text = $"Proposals: {_lastProposals.Count} | Blocked: {_lastBlocked.Count}";

            _spanRenderer.MarkerRadius = (double)nudMarkerRadius.Value;
            _spanRenderer.Show(_lastProposals);
        }

        private void btSpanClear_Click(object sender, EventArgs e)
        {
            _spanRenderer.Clear();
            _lastProposals.Clear();
            _lastBlocked.Clear();
            dgvSpanProposals.DataSource = null;
            lbSpanBlocked.Items.Clear();
            lblSpanSummary.Text = "Proposals: 0 | Blocked: 0";
        }

        private void btSpanExport_Click(object sender, EventArgs e)
        {
            if (_lastProposals.Count == 0 && _lastBlocked.Count == 0)
            {
                MessageBox.Show("No data to export. Run preview first.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV file|*.csv";
            sfd.Title = "Save Span Proposals";
            sfd.FileName = "SpanProposals";

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    sw.WriteLine("--- Proposals ---");
                    sw.WriteLine("No,Dn,Symbol,X,Y,Z,Confidence,LoadCase");
                    for (int i = 0; i < _lastProposals.Count; i++)
                    {
                        var p = _lastProposals[i];
                        sw.WriteLine($"{i + 1},{p.Dn},{p.Symbol},{p.Position.X},{p.Position.Y},{p.Position.Z},{p.Confidence},{p.Case}");
                    }
                    
                    sw.WriteLine();
                    sw.WriteLine("--- Blocked ---");
                    sw.WriteLine("Pipe Handle,Reason");
                    foreach (var b in _lastBlocked)
                    {
                        sw.WriteLine($"{b.Source.Handle.Value.ToString("X")},\"{b.Reason}\"");
                    }
                }

                MessageBox.Show("Export completed successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void tabMainUI_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.tabMainUI.SelectedTab != this.tabSpanAutoPlace)
            {
                _spanRenderer.Clear();
            }
        }

        private void PaletteTab_Dispose(bool disposing)
        {
            if (disposing)
            {
                _spanRenderer.Clear();
            }
        }
    }
}
