using System;
using System.Windows.Forms;

namespace PlantFlow_Support
{
    // Phase 0: tabMainUI(TabControl) 전체를 app.html 단일 WebView 셸로 교체. 실패 시 순수 레거시 복원.
    // 계획서: plan_pfs_ui_shell_phase0_20260707.md
    public partial class PaletteTab
    {
        private WebViewControl _shellWeb;
        // 필드 기본값 true — 생성자 초기부터 P4 per-tab catalog 스왑을 선행 차단(셸이 catalog 흡수).
        internal bool _shellDesired = true;
        private bool _shellSwapFailed;

        // 생성자 말미에서 호출. tabMainUI를 덮는 단일 셸 부착.
        private void InitializeShell()
        {
            if (_shellWeb != null || _shellSwapFailed) return;
            if (tabMainUI == null) return;
            try
            {
                _shellWeb = new WebViewControl("app.html") { Dock = DockStyle.Fill };
                _shellWeb.InitFailed += (s, reason) => RestoreLegacyShell("InitFailed: " + reason);
                _shellWeb.MeshFailed += (s, reason) => Log("shell meshError: " + reason);
                HookCatalogWebFocus(_shellWeb); // P4-3 KeepFocus 토글 재사용
                tabMainUI.Visible = false;
                this.Controls.Add(_shellWeb);
                _shellWeb.BringToFront();
                Log("Phase0: tabMainUI → app.html 셸 교체 완료");
            }
            catch (Exception ex)
            {
                RestoreLegacyShell("swap 예외: " + ex.Message);
            }
        }

        // 1단 폴백 — 순수 레거시 TabControl 복원(catalog 탭도 WinForms, WebView 재시도 없음).
        private void RestoreLegacyShell(string reason)
        {
            Action run = () =>
            {
                try
                {
                    Log("Phase0 폴백(순수 레거시): " + reason);
                    _shellSwapFailed = true;
                    _shellDesired = false;      // P4 catalog 스왑 가드는 해제되지만…
                    _catalogSwapFailed = true;  // …이 플래그로 catalog WebView 재시도도 차단 → 순수 WinForms
                    SetCatalogKeepFocus(false);
                    if (_shellWeb != null)
                    {
                        try { this.Controls.Remove(_shellWeb); _shellWeb.Dispose(); }
                        catch (Exception dx) { Log("shellWeb dispose 실패: " + dx.Message); }
                        _shellWeb = null;
                    }
                    if (tabMainUI != null) tabMainUI.Visible = true;
                }
                catch (Exception ex) { Log("RestoreLegacyShell 실패: " + ex.Message); }
            };
            if (this.InvokeRequired) this.BeginInvoke(run);
            else run();
        }

        // 셸 도킹/리사이즈 재배치(CustomPaletteSet.RelayoutCatalogWeb에서 통합 호출).
        internal void RelayoutShellWeb()
        {
            try { _shellWeb?.RelayoutWebView(); }
            catch (Exception ex) { Log("RelayoutShellWeb 실패: " + ex.Message); }
        }
    }
}
