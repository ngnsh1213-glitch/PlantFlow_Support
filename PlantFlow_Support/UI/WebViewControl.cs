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

        private bool _coreReady;
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

                // JS→C# 비동기 메시지(진단/ready/error).
                core.WebMessageReceived += (s, e) =>
                {
                    string msg;
                    try { msg = e.TryGetWebMessageAsString(); }
                    catch (Exception ex) { Log("WebMessage parse 실패: " + ex.Message); return; }
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
                        Log("Navigation 실패: " + e.WebErrorStatus);
                        return;
                    }
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
                    string cand = Path.Combine(dir, "frontend_support", "dist", "index.html");
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
            return "index.html";
        }

        private void Fail(string reason)
        {
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
