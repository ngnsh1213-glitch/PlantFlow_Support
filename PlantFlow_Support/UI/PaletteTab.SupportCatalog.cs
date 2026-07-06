using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PlantFlow_Support
{
    public partial class PaletteTab
    {
        private CatalogManager _cat;
        private List<string> _currentParamKeys = new List<string>();
        private long _selectedPnPID = -1;

        private void InitializeSupportCatalog()
        {
            EnsureCatPreviewButton();
            string acatPath = CatalogManager.ResolveAcatPath();
            if (string.IsNullOrEmpty(acatPath))
            {
                lblCatStatus.Text = "Error: Cannot find WH-PipeSupport.acat. 'Load .acat...' 버튼으로 직접 선택하세요.";
                lblCatStatus.ForeColor = Color.Red;
                cbCatType.Enabled = false;
                btCatAddVariant.Enabled = false;
                return;
            }
            LoadCatalog(acatPath);
        }

        // 지정 .acat 경로로 카탈로그를 (재)로드한다. 타입 콤보를 다시 채운다.
        private void LoadCatalog(string acatPath)
        {
            try
            {
                _cat = new CatalogManager(acatPath);
                var types = _cat.ListTypes();
                cbCatType.Items.Clear();
                foreach (var t in types)
                {
                    cbCatType.Items.Add(t.Template);
                }
                cbCatType.Enabled = true;
                btCatAddVariant.Enabled = true;
                dgvCatVariants.DataSource = null;
                pnlCatParams.Controls.Clear();
                _selectedPnPID = -1;
                if (lblCatPath != null) lblCatPath.Text = acatPath;
                lblCatStatus.Text = $"Loaded catalog: {acatPath} ({types.Count} types)";
                lblCatStatus.ForeColor = Color.Black;
            }
            catch (Exception ex)
            {
                lblCatStatus.Text = "Error loading catalog: " + ex.Message;
                lblCatStatus.ForeColor = Color.Red;
                cbCatType.Enabled = false;
                btCatAddVariant.Enabled = false;
                Log("Error loading catalog: " + ex.Message);
            }
        }

        private void btCatBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Pipe Support Catalog (.acat)";
                dlg.Filter = "Pipe Support Catalog (*.acat)|*.acat|All files (*.*)|*.*";
                try
                {
                    string cur = _cat?.AcatPath;
                    if (!string.IsNullOrEmpty(cur))
                    {
                        string dir = System.IO.Path.GetDirectoryName(cur);
                        if (!string.IsNullOrEmpty(dir) && System.IO.Directory.Exists(dir))
                            dlg.InitialDirectory = dir;
                    }
                }
                catch { /* 초기 폴더 설정 실패는 무시 (다이얼로그는 기본 폴더로 열림) */ }

                if (dlg.ShowDialog() != DialogResult.OK) return;

                string chosen = dlg.FileName;
                LoadCatalog(chosen);

                // 로드 성공 시에만 경로를 영구저장한다.
                if (_cat != null && string.Equals(_cat.AcatPath, chosen, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        CatalogManager.SaveLastAcatPath(chosen);
                    }
                    catch (Exception ex)
                    {
                        Log("Failed to save .acat path: " + ex.Message);
                        lblCatStatus.Text += " (경로 저장 실패 — 이번 세션만 적용)";
                    }
                }
            }
        }

        private void cbCatType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cat == null) return;
            string template = cbCatType.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(template)) return;

            try
            {
                // Load Variants
                var variants = _cat.ListVariants(template);
                dgvCatVariants.DataSource = variants.Select(v => new
                {
                    v.PnPID,
                    Dn = v.NominalDiameter,
                    v.ShortDescription,
                    Params = v.ParamDefinition
                }).ToList();

                // Load Parameters for new variant
                _currentParamKeys = _cat.GetParamKeys(template);
                
                // Clear existing dynamic controls
                pnlCatParams.Controls.Clear();
                
                int yOffset = 10;
                int xLabel = 10;
                int xTextBox = 150;

                // Dynamic controls are created without prefilling here. 
                // Prefilling will be handled by dgvCatVariants_SelectionChanged.
                foreach (var key in _currentParamKeys)
                {
                    Label lbl = new Label();
                    lbl.Text = key + ":";
                    lbl.Location = new Point(xLabel, yOffset + 3);
                    lbl.Size = new Size(130, 20);

                    TextBox tb = new TextBox();
                    tb.Name = "tbParam_" + key;
                    tb.Location = new Point(xTextBox, yOffset);
                    tb.Size = new Size(200, 20);
                    
                    pnlCatParams.Controls.Add(lbl);
                    pnlCatParams.Controls.Add(tb);

                    yOffset += 30;
                }
                
                lblCatStatus.Text = $"Loaded {variants.Count} variants for {template}";
                lblCatStatus.ForeColor = Color.Black;
                tbCatDescOverride.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading variants: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log("Error loading variants: " + ex.Message);
            }
        }

        private void btCatAddVariant_Click(object sender, EventArgs e)
        {
            if (_cat == null) return;
            string template = cbCatType.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(template))
            {
                MessageBox.Show("Please select a Support Type first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Add a new variant to '{template}' catalog?\nThis will modify the SQLite database and create a backup.", "Confirm Add Variant", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            var input = new VariantInput();
            input.Params = new Dictionary<string, string>();

            foreach (var key in _currentParamKeys)
            {
                var tb = pnlCatParams.Controls.Find("tbParam_" + key, false).FirstOrDefault() as TextBox;
                if (tb != null)
                {
                    input.Params[key] = tb.Text;
                }
            }

            input.ShortDescriptionOverride = string.IsNullOrWhiteSpace(tbCatDescOverride.Text) ? null : tbCatDescOverride.Text.Trim();
            input.MatchingPipeOd = null; // Auto lookup

            try
            {
                var result = _cat.AddVariant(template, input);
                lblCatStatus.Text = result.Message;
                if (result.Ok)
                {
                    lblCatStatus.ForeColor = Color.Green;
                    MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // Refresh variants list
                    cbCatType_SelectedIndexChanged(null, null);
                }
                else
                {
                    lblCatStatus.ForeColor = Color.Red;
                    MessageBox.Show(result.Message, "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to add variant: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblCatStatus.Text = "Exception: " + ex.Message;
                lblCatStatus.ForeColor = Color.Red;
                Log("Failed to add variant: " + ex.Message);
            }
        }

        private void dgvCatVariants_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvCatVariants.SelectedRows.Count == 0)
            {
                _selectedPnPID = -1;
                return;
            }

            var row = dgvCatVariants.SelectedRows[0];
            if (row.Cells["PnPID"].Value != null)
            {
                _selectedPnPID = Convert.ToInt64(row.Cells["PnPID"].Value);
            }
            
            if (row.Cells["Params"].Value != null)
            {
                string paramDef = row.Cells["Params"].Value.ToString();
                var prefill = new Dictionary<string, string>();
                var pairs = paramDef.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var pair in pairs)
                {
                    var kv = pair.Split('=');
                    if (kv.Length == 2) prefill[kv[0].Trim()] = kv[1].Trim();
                }

                foreach (var key in _currentParamKeys)
                {
                    var tb = pnlCatParams.Controls.Find("tbParam_" + key, false).FirstOrDefault() as TextBox;
                    if (tb != null && prefill.ContainsKey(key))
                    {
                        tb.Text = prefill[key];
                    }
                }
            }
        }

        private void btCatUpdate_Click(object sender, EventArgs e)
        {
            if (_cat == null) return;
            string template = cbCatType.SelectedItem?.ToString();
            
            if (_selectedPnPID < 0)
            {
                MessageBox.Show("Please select a variant from the list first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Update variant PnPID {_selectedPnPID}?\nThis will modify the SQLite database and create a backup.", "Confirm Update Variant", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            var input = new VariantInput();
            input.Params = new Dictionary<string, string>();

            foreach (var key in _currentParamKeys)
            {
                var tb = pnlCatParams.Controls.Find("tbParam_" + key, false).FirstOrDefault() as TextBox;
                if (tb != null)
                {
                    input.Params[key] = tb.Text;
                }
            }

            input.ShortDescriptionOverride = string.IsNullOrWhiteSpace(tbCatDescOverride.Text) ? null : tbCatDescOverride.Text.Trim();
            input.MatchingPipeOd = null;

            try
            {
                var result = _cat.UpdateVariant(_selectedPnPID, template, input);
                lblCatStatus.Text = result.Message;
                if (result.Ok)
                {
                    lblCatStatus.ForeColor = Color.Green;
                    MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    cbCatType_SelectedIndexChanged(null, null);
                }
                else
                {
                    lblCatStatus.ForeColor = Color.Red;
                    MessageBox.Show(result.Message, "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update variant: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblCatStatus.Text = "Exception: " + ex.Message;
                lblCatStatus.ForeColor = Color.Red;
                Log("Failed to update variant: " + ex.Message);
            }
        }

        private void btCatDelete_Click(object sender, EventArgs e)
        {
            if (_cat == null) return;

            if (_selectedPnPID < 0)
            {
                MessageBox.Show("Please select a variant from the list first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Delete variant PnPID {_selectedPnPID}?\nThis will modify the SQLite database and create a backup.", "Confirm Delete Variant", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            try
            {
                var result = _cat.DeleteVariant(_selectedPnPID);
                lblCatStatus.Text = result.Message;
                if (result.Ok)
                {
                    lblCatStatus.ForeColor = Color.Green;
                    MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _selectedPnPID = -1;
                    cbCatType_SelectedIndexChanged(null, null);
                }
                else
                {
                    lblCatStatus.ForeColor = Color.Red;
                    MessageBox.Show(result.Message, "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to delete variant: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblCatStatus.Text = "Exception: " + ex.Message;
                lblCatStatus.ForeColor = Color.Red;
                Log("Failed to delete variant: " + ex.Message);
            }
        }
    }
}
