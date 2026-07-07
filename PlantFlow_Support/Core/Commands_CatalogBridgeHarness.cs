using System;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;

namespace PlantFlow_Support
{
    public partial class Commands
    {
        [CommandMethod("PFSCATALOGBRIDGE", CommandFlags.Session)]
        public void RunCatalogBridgeHarness()
        {
            try
            {
                var form = new Form
                {
                    Text = "PFS Catalog Bridge Harness",
                    StartPosition = FormStartPosition.CenterScreen,
                    Size = new Size(1280, 820)
                };
                var webView = new WebViewControl("catalog.html") { Dock = DockStyle.Fill };
                form.Controls.Add(webView);
                // 모드리스 창 닫힘 시 Dispose — 유령 WebView 누수 방지(Phase 2b 패턴 일치).
                form.FormClosed += (s, e) =>
                {
                    try { form.Dispose(); }
                    catch (Exception disposeEx)
                    {
                        Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?
                            .Editor.WriteMessage("\n[PFSCATALOGBRIDGE] dispose 실패: " + disposeEx.Message + "\n");
                    }
                };
                Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessDialog(form);
            }
            catch (Exception ex)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?
                    .Editor.WriteMessage("\n[PFSCATALOGBRIDGE] failed: " + ex.Message + "\n");
            }
        }
    }
}
