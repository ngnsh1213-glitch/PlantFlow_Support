using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PlantFlow_Support
{
    // P4: tabSupportCatalog을 catalog.html WebView로 교체. 레거시 컨트롤은 숨김 보존 → 실패 시 원상 복원 폴백.
    // 계획서: plan_pfs_catalog_p4_webview_tab_20260707.md
    public partial class PaletteTab
    {
        private WebViewControl _catalogWeb;
        private List<KeyValuePair<Control, bool>> _legacyCatSnapshot; // (컨트롤, 원래 Visible)
        private bool _catalogSwapFailed; // 폴백 후 재스왑 루프 방지 — 세션당 1회만 시도

        // LoadCatalog 성공 경로에서만 호출(.acat 미발견 시 레거시 browse UX 보존).
        private void TrySwapCatalogTabToWeb()
        {
            if (_catalogWeb != null || _catalogSwapFailed || tabSupportCatalog == null) return;
            try
            {
                // 1) 레거시 스냅샷(원래 Visible 보존) + 숨김. _btCatPreview 포함(EnsureCatPreviewButton 이후 전제).
                _legacyCatSnapshot = tabSupportCatalog.Controls.Cast<Control>()
                    .Select(c => new KeyValuePair<Control, bool>(c, c.Visible)).ToList();
                foreach (var kv in _legacyCatSnapshot) kv.Key.Visible = false;

                // 2) WebView 부착. 초기화는 TabPage 핸들 생성(탭 첫 표시) 시점에 지연 착수.
                _catalogWeb = new WebViewControl("catalog.html") { Dock = DockStyle.Fill };
                _catalogWeb.InitFailed += (s, reason) => RestoreLegacyCatalogTab("InitFailed: " + reason);
                _catalogWeb.MeshFailed += (s, reason) => Log("catalog meshError: " + reason); // UI 표시는 JS(meshError) 측
                HookCatalogWebFocus(_catalogWeb);
                tabSupportCatalog.Controls.Add(_catalogWeb);
                _catalogWeb.BringToFront();
                Log("P4: catalog tab → WebView 교체 완료");
            }
            catch (Exception ex)
            {
                RestoreLegacyCatalogTab("swap 예외: " + ex.Message);
            }
        }

        // WebView 실패 → 레거시 WinForms UI 원상 복원. UI 스레드 마샬링 포함.
        private void RestoreLegacyCatalogTab(string reason)
        {
            Action run = () =>
            {
                try
                {
                    Log("P4 폴백: " + reason);
                    _catalogSwapFailed = true;
                    SetCatalogKeepFocus(false); // KeepFocus=true 잔류 방지
                    if (_catalogWeb != null)
                    {
                        try { tabSupportCatalog.Controls.Remove(_catalogWeb); _catalogWeb.Dispose(); }
                        catch (Exception dx) { Log("catalogWeb dispose 실패: " + dx.Message); }
                        _catalogWeb = null;
                    }
                    if (_legacyCatSnapshot != null)
                        foreach (var kv in _legacyCatSnapshot) kv.Key.Visible = kv.Value; // 원상 복원
                }
                catch (Exception ex) { Log("RestoreLegacyCatalogTab 실패: " + ex.Message); }
            };
            if (this.InvokeRequired) this.BeginInvoke(run);
            else run();
        }

        // WebView 포커스 ↔ PaletteSet.KeepFocus 동적 토글 (P4-3).
        private void HookCatalogWebFocus(WebViewControl wv)
        {
            wv.WebFocusGained += (s, e) => SetCatalogKeepFocus(true);
            wv.WebFocusLost += (s, e) => SetCatalogKeepFocus(false);
        }

        private void SetCatalogKeepFocus(bool on)
        {
            // Instance가 disposed여도 예외는 여기서 흡수(정적 참조 stale 가드).
            try { var ps = CustomPaletteSet.Instance; if (ps != null) ps.KeepFocus = on; }
            catch (Exception ex) { Log("KeepFocus 토글 실패: " + ex.Message); }
        }

        // CustomPaletteSet StateChanged/SizeChanged → WebView 재배치 위임 (P4-4).
        internal void RelayoutCatalogWeb()
        {
            try { _catalogWeb?.RelayoutWebView(); }
            catch (Exception ex) { Log("RelayoutCatalogWeb 실패: " + ex.Message); }
        }
    }
}
