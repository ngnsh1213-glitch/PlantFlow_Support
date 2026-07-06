using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PlantFlow_Support
{
    // 프리뷰 B(WebView2/three.js) 런처. A(정적 PNG)와 동일 진입점(Preview 버튼).
    // B 사용 가능 → WebView2 창, 초기화 실패 → A(PNG) 자동 폴백.
    public partial class PaletteTab
    {
        // 현재 파라미터 텍스트박스 값을 수집.
        private Dictionary<string, string> CollectCatParams()
        {
            var dict = new Dictionary<string, string>();
            foreach (var key in _currentParamKeys)
            {
                var tb = pnlCatParams.Controls.Find("tbParam_" + key, false).FirstOrDefault() as TextBox;
                if (tb != null) dict[key] = tb.Text;
            }
            return dict;
        }

        // B 우선 시도. 실패 시 PNG 폴백.
        private void ShowPreviewB(string template)
        {
            WebViewControl wv = null;
            Form form = null;
            try
            {
                wv = new WebViewControl { Dock = DockStyle.Fill };
                form = new Form
                {
                    Text = "3D Preview - " + template,
                    StartPosition = FormStartPosition.CenterParent,
                    Size = new Size(720, 720),
                    MinimizeBox = false,
                    MaximizeBox = true
                };
                var localForm = form;

                wv.Ready += (s, e) =>
                {
                    try { wv.LoadShape(template, CollectCatParams()); }
                    catch (Exception ex) { Log("ShowPreviewB LoadShape 실패: " + ex.Message); }
                };

                // 초기화 실패 → 창 닫고 A(PNG)로 폴백.
                wv.InitFailed += (s, reason) =>
                {
                    try
                    {
                        if (localForm != null && !localForm.IsDisposed)
                        {
                            localForm.BeginInvoke((Action)(() =>
                            {
                                localForm.Close();
                                FallbackToPng(template, reason);
                            }));
                        }
                    }
                    catch (Exception ex) { Log("InitFailed 폴백 실패: " + ex.Message); }
                };

                form.Controls.Add(wv);
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                Log("ShowPreviewB 예외 → PNG 폴백: " + ex.Message);
                try { form?.Dispose(); } catch { }
                FallbackToPng(template, ex.Message);
            }
        }

        // A(PNG) 폴백 — 기존 ResolveTypePng/ShowPreviewWindow 재사용.
        private void FallbackToPng(string template, string reason)
        {
            string png = ResolveTypePng(template);
            if (png == null)
            {
                MessageBox.Show(
                    "3D 미리보기 불가(" + reason + ")\n형상 이미지도 찾지 못했습니다.",
                    "Preview", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try { ShowPreviewWindow(template, png); }
            catch (Exception ex) { Log("PNG 폴백 표시 실패: " + ex.Message); }
        }
    }
}
