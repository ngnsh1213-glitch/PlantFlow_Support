using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;

namespace PlantFlow_Support
{
    // 프리뷰 B WebView2 호스트. PFO WebViewControl 패턴 이식 + §9 R2 강화:
    //  - JS↔C# 브리지는 postMessage/WebMessageReceived 비동기만(AddHostObjectToScript 미사용 → STA 데드락·공격면 회피)
    //  - NavigationStarting origin allowlist(외부 URL 차단)
    //  - 런타임/초기화 실패는 InitFailed 이벤트로 통지 → 호출측이 A(PNG)로 폴백
    //  - FindDistFolder: 설치경로 우선, 선택 경로 로깅
    public class WebViewControl : UserControl
    {
        private const string VirtualHost = "pfsupport.local";
        private const string AppOrigin = "http://" + VirtualHost + "/";
        private const string DevUrl = "http://localhost:5174/"; // PFO(5173)와 분리

        public WebView2 Browser { get; private set; }

        // 초기화/런타임 실패 시 발생. 구독측은 WebView 숨기고 PNG 폴백.
        public event EventHandler<string> InitFailed;
        public event EventHandler<string> MeshFailed;
        // 페이지(React/three.js) 로드 완료.
        public event EventHandler Ready;
        // Phase 1a: owner가 미처리 채널을 직접 처리하는 훅(true=처리됨). event 아님 - 단일 owner 대입.
        public Func<string, bool> ExternalMessageHandler { get; set; }
        // Phase 1a: owner가 응답을 전송(PostPayload 공개 래퍼).
        public void PostMessage(string json) => PostPayload(json);
        // 포커스 신호(주채널=JS ui 메시지, 보조=WinForms GotFocus/LostFocus). PaletteSet KeepFocus 토글용 (P4-1b).
        public event EventHandler WebFocusGained;
        public event EventHandler WebFocusLost;

        private bool _coreReady;
        private bool _failed;            // Fail() 1회 래치 — 폴백 이벤트 중복 발화 방지 (P4-1a)
        private bool _hasNavigatedOk;    // 최초 로드 성공 여부 — 이전 실패는 InitFailed로 승격 (P4-1c)
        private int _rendererFailCount;  // 렌더러 크래시 자동 Reload 횟수 (P4-1a)
        private string _pendingPayload; // CoreWebView2 준비 전 LoadShape 호출 시 보류.
        private readonly SemaphoreSlim _meshGate = new SemaphoreSlim(1, 1);
        private readonly Label _status;
        private readonly string _entryHtml;

        public WebViewControl() : this("index.html")
        {
        }

        public WebViewControl(string entryHtml)
        {
            _entryHtml = NormalizeEntryHtml(entryHtml);
            this.BackColor = SystemColors.Control;
            _status = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = "Preparing 3D preview...",
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.DimGray
            };
            this.Controls.Add(_status);
            // InitAsync는 OnHandleCreated에서 착수(생성자 시점엔 부모 폼 미소속 → 재부모화 시 핸들 파괴로 코어 dispose).
        }

        private bool _initStarted;
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_initStarted) return; // 재부모화로 핸들 재생성돼도 1회만 착수.
            _initStarted = true;
            InitAsync();
        }

        private async void InitAsync()
        {
            try
            {
                Browser = new WebView2 { Dock = DockStyle.Fill };
                Browser.DefaultBackgroundColor = SystemColors.Control;
                // 보조 포커스 신호(주채널=JS ui 메시지). 멱등 토글이라 중복 발화 무해 (P4-1b).
                Browser.GotFocus += (s, e) => WebFocusGained?.Invoke(this, EventArgs.Empty);
                Browser.LostFocus += (s, e) => WebFocusLost?.Invoke(this, EventArgs.Empty);
                this.Controls.Add(Browser);
                this.Controls.SetChildIndex(Browser, 0);

                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PlantFlow_Support_WebView2");

                CoreWebView2Environment env;
                try
                {
                    env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                }
                catch (Exception ex)
                {
                    Fail("WebView2 Runtime 생성 실패(런타임 미설치/정책 차단 가능): " + ex.Message);
                    return;
                }

                try
                {
                    await Browser.EnsureCoreWebView2Async(env);
                }
                catch (Exception ex)
                {
                    Fail("WebView2 코어 초기화 실패: " + ex.Message);
                    return;
                }

                var core = Browser.CoreWebView2;

                // 보안: 외부 탐색 차단(허용 origin만). 새창 차단.
                core.NavigationStarting += (s, e) =>
                {
                    string uri = e.Uri ?? string.Empty;
                    bool ok = uri.StartsWith(AppOrigin, StringComparison.OrdinalIgnoreCase)
                           || uri.StartsWith(DevUrl, StringComparison.OrdinalIgnoreCase);
                    if (!ok)
                    {
                        e.Cancel = true;
                        Log("Blocked navigation: " + uri);
                    }
                };
                core.NewWindowRequested += (s, e) => { e.Handled = true; };

                // 프로세스 크래시 복구: 렌더러 계열=1회 Reload, 그 외(브라우저 프로세스 등)=즉시 폴백 (P4-1a).
                // 브라우저 스레드에서 발생 가능 → UI 마샬링 필수. 크래시 즉시 브리지 차단(_coreReady=false).
                core.ProcessFailed += (s, e) =>
                {
                    _coreReady = false;
                    bool rendererClass =
                        e.ProcessFailedKind == CoreWebView2ProcessFailedKind.RenderProcessExited ||
                        e.ProcessFailedKind == CoreWebView2ProcessFailedKind.RenderProcessUnresponsive;
                    try
                    {
                        BeginInvoke((Action)(() =>
                        {
                            if (IsDisposed || !IsHandleCreated) return;
                            if (rendererClass && ++_rendererFailCount <= 1)
                            {
                                Log("ProcessFailed(" + e.ProcessFailedKind + ") → Reload 1회 시도");
                                try { Browser.Reload(); } // _coreReady 복구는 NavigationCompleted 성공 시에만.
                                catch (Exception rx) { Fail("Reload 실패: " + rx.Message); }
                            }
                            else
                            {
                                Fail("WebView2 프로세스 실패(" + e.ProcessFailedKind + ")");
                            }
                        }));
                    }
                    catch (Exception bx) { Log("ProcessFailed 마샬링 실패: " + bx.Message); }
                };

                // JS→C# 비동기 메시지(진단/ready/error).
                core.WebMessageReceived += (s, e) =>
                {
                    string msg;
                    try { msg = e.TryGetWebMessageAsString(); }
                    catch (Exception ex) { Log("WebMessage parse 실패: " + ex.Message); return; }
                    if (TryHandleUiMessage(msg))
                    {
                        return;
                    }
                    if (ExternalMessageHandler != null && ExternalMessageHandler(msg))
                    {
                        return;
                    }
                    if (CatalogBridge.TryDispatch(this, msg, PostPayload, Log))
                    {
                        return;
                    }
                    Log("JS->C#: " + msg);
                };

                core.NavigationCompleted += (s, e) =>
                {
                    if (!e.IsSuccess)
                    {
                        // 최초 로드 성공 전 실패 = 엔트리/서빙 문제 → InitFailed 승격(레거시 폴백) (P4-1c)
                        if (!_hasNavigatedOk) { Fail("Navigation 실패: " + e.WebErrorStatus); return; }
                        Log("Navigation 실패: " + e.WebErrorStatus);
                        return;
                    }
                    _hasNavigatedOk = true;
                    _coreReady = true;
                    if (_status != null) _status.Visible = false;
                    Ready?.Invoke(this, EventArgs.Empty);
                    if (_pendingPayload != null)
                    {
                        PostPayload(_pendingPayload);
                        _pendingPayload = null;
                    }
                };

                // dist 서빙 또는 dev 서버.
                string dist = FindDistFolder();
                if (dist != null)
                {
                    core.SetVirtualHostNameToFolderMapping(
                        VirtualHost, dist, CoreWebView2HostResourceAccessKind.Allow);
                    core.Navigate(AppOrigin + _entryHtml);
                }
                else
                {
                    Fail("Frontend 빌드(frontend_support/dist) 미발견. CleanAndBuild로 배포 필요.");
                }
            }
            catch (Exception ex)
            {
                Fail("WebView 초기화 예외: " + ex.Message);
            }
        }

        // 형상 데이터를 JS로 전송. type=함수명, paramKv=파라미터, sizeKv=사이즈필드.
        // 계약: {"type":..., "params":{...}, "sizeFields":{...}}
        public void LoadShape(string type, IDictionary<string, string> paramKv,
            IDictionary<string, string> sizeKv = null)
        {
            try
            {
                var payload = JsonConvert.SerializeObject(new
                {
                    type = type ?? string.Empty,
                    @params = paramKv ?? new Dictionary<string, string>(),
                    sizeFields = sizeKv ?? new Dictionary<string, string>()
                });
                if (_coreReady && Browser?.CoreWebView2 != null) PostPayload(payload);
                else _pendingPayload = payload;
            }
            catch (Exception ex)
            {
                Log("LoadShape 직렬화 실패: " + ex.Message);
            }
        }

        public async void LoadSupportMesh(string type, IDictionary<string, string> paramKv)
        {
            await LoadSupportMeshAsync(type, paramKv);
        }

        private async Task LoadSupportMeshAsync(string type, IDictionary<string, string> paramKv)
        {
            if (!_coreReady || Browser?.CoreWebView2 == null)
            {
                Log("LoadSupportMesh skipped: WebView not ready");
                return;
            }

            if (!await _meshGate.WaitAsync(0))
            {
                Log("LoadSupportMesh skipped: request already running");
                return;
            }

            try
            {
                var docs = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager;
                var doc = docs.MdiActiveDocument;
                if (doc == null)
                {
                    NotifyMeshFailed("활성 도면 없음");
                    return;
                }

                MeshData mesh = null;
                string diag = null;
                bool ok = false;
                var requestParams = paramKv == null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(paramKv, StringComparer.OrdinalIgnoreCase);

                await docs.ExecuteInCommandContextAsync(
                    async _ =>
                    {
                        ok = SupportPreviewBuilder.TryBuildMesh(doc, type, requestParams, out mesh, out diag);
                        await Task.CompletedTask;
                    },
                    null);

                if (!ok || mesh == null || mesh.Triangles.Count == 0)
                {
                    NotifyMeshFailed("mesh build failed: " + (diag ?? "no_diag"));
                    return;
                }

                var payload = JsonConvert.SerializeObject(new
                {
                    kind = "mesh",
                    units = mesh.Units,
                    upAxis = mesh.UpAxis,
                    vertices = mesh.Vertices,
                    triangles = mesh.Triangles
                });
                PostPayload(payload);
                Log("LoadSupportMesh OK triangles=" + mesh.Triangles.Count);
            }
            catch (Exception ex)
            {
                NotifyMeshFailed("LoadSupportMesh 예외: " + ex.Message);
            }
            finally
            {
                _meshGate.Release();
            }
        }

        private void PostPayload(string json)
        {
            try { Browser.CoreWebView2.PostWebMessageAsString(json); }
            catch (Exception ex) { Log("PostWebMessage 실패: " + ex.Message); }
        }

        private string FindDistFolder()
        {
            // 설치경로(어셈블리 폴더) 우선 상위 탐색. 선택 경로 로깅.
            try
            {
                string dir = Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
                for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
                {
                    string cand = Path.Combine(dir, "frontend_support", "dist", _entryHtml); // 엔트리별 존재 확인 (P4-1c)
                    if (File.Exists(cand))
                    {
                        string distDir = Path.Combine(dir, "frontend_support", "dist");
                        Log("dist 선택: " + distDir);
                        return distDir;
                    }
                    dir = Path.GetDirectoryName(dir);
                }
            }
            catch (Exception ex)
            {
                Log("FindDistFolder 예외: " + ex.Message);
            }
            return null;
        }

        private static string NormalizeEntryHtml(string entryHtml)
        {
            if (string.Equals(entryHtml, "catalog.html", StringComparison.OrdinalIgnoreCase))
                return "catalog.html";
            if (string.Equals(entryHtml, "app.html", StringComparison.OrdinalIgnoreCase))
                return "app.html";
            return "index.html";
        }

        private void Fail(string reason)
        {
            if (_failed) return; // 1회 래치 — 폴백 중복 실행 방지 (P4-1a)
            _failed = true;
            Log("InitFailed: " + reason);
            if (_status != null)
            {
                _status.Text = reason;
                _status.ForeColor = Color.FromArgb(217, 119, 6);
                _status.Visible = true;
            }
            InitFailed?.Invoke(this, reason);
        }

        private void NotifyMeshFailed(string reason)
        {
            Log(reason);
            // 도킹 탭엔 PNG 폴백이 없다 → React 3D 패널에 오류 표시 (P4-1d)
            try
            {
                if (_coreReady && Browser?.CoreWebView2 != null)
                    PostPayload(JsonConvert.SerializeObject(new { kind = "meshError", reason = reason }));
            }
            catch (Exception ex) { Log("meshError 통지 실패: " + ex.Message); }
            MeshFailed?.Invoke(this, reason);
        }

        private void Log(string message)
        {
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                doc?.Editor.WriteMessage("\n[PFS-WebView] " + message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PFS-WebView] Log failed: " + ex.Message);
            }
        }

        // JS focusin/focusout → KeepFocus 토글 신호. 계약: {"ch":"ui","event":"focus"|"blur"} (P4-1b)
        private bool TryHandleUiMessage(string msg)
        {
            try
            {
                if (string.IsNullOrEmpty(msg) || msg.IndexOf("\"ui\"", StringComparison.Ordinal) < 0) return false;
                var jo = Newtonsoft.Json.Linq.JObject.Parse(msg);
                if ((string)jo["ch"] != "ui") return false;
                string ev = (string)jo["event"];
                if (ev == "focus") WebFocusGained?.Invoke(this, EventArgs.Empty);
                else if (ev == "blur") WebFocusLost?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex) { Log("ui message parse 실패: " + ex.Message); return false; }
        }

        // 도킹↔플로팅 전환 등 호스트발 재배치 요청(PFO WebViewPaletteSet 패턴) (P4-4).
        public void RelayoutWebView()
        {
            try { if (IsHandleCreated && Browser != null) Browser.Bounds = this.ClientRectangle; }
            catch (Exception ex) { Log("RelayoutWebView 실패: " + ex.Message); }
        }

        // DPI/도킹 변경 시 재배치(PFO 패턴 축약).
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            const int WM_SIZE = 0x0005, WM_DPICHANGED = 0x02E0;
            if ((m.Msg == WM_SIZE || m.Msg == WM_DPICHANGED) && IsHandleCreated && Browser != null)
            {
                try { BeginInvoke((Action)(() => { if (Browser != null) Browser.Bounds = this.ClientRectangle; })); }
                catch (Exception ex) { Log("WndProc resize 실패: " + ex.Message); }
            }
        }
    }
}
