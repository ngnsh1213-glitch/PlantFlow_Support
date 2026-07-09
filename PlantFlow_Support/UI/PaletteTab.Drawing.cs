using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PlantFlow_Support
{
    // Phase 1a: drawing 채널 연산(SupportSelection/lvSupportName 소유). UI 스레드 async, CAD만 command context.
    public partial class PaletteTab
    {
        private bool _drawingBusy;
        private string _captureDocName; // MDI 가드: 서포트 캡처 시 활성 문서명.

        // _shellWeb 생성 직후 배선(InitializeShell에서 호출).
        private void HookDrawingBridge(WebViewControl wv)
        {
            wv.ExternalMessageHandler = HandleDrawingMessage;
        }

        private bool HandleDrawingMessage(string msg)
        {
            var env = DrawingBridge.Parse(msg, Log);
            if (env == null) return false;
            _ = DispatchDrawingAsync(env);
            return true;
        }

        private async Task DispatchDrawingAsync(DrawingBridge.Envelope env)
        {
            string resp;
            try
            {
                switch (env.Method)
                {
                    case "getState": resp = DrawingBridge.Ok(env.Id, BuildDrawingState()); break;
                    case "getSettings": resp = DrawingBridge.Ok(env.Id, BuildSettings()); break;
                    case "setSetting": resp = SetSetting(env); break;
                    case "removeSupport": resp = RemoveSupport(env); break;
                    case "clearSupports": resp = ClearSupports(env); break;
                    case "addFromSelection": resp = await AddFromSelectionAsync(env); break;
                    case "export2D": resp = await Export2DAsync(env); break;
                    case "exportMTO": resp = DrawingBridge.Fail(env.Id, "ER_NOT_IMPL", "MTO는 현재 보류 상태입니다(재구현 예정)."); break;
                    default: resp = DrawingBridge.Fail(env.Id, "ERUNKNOWN", "Unknown drawing method: " + env.Method); break;
                }
            }
            catch (Exception ex)
            {
                Log("drawing dispatch 실패: " + ex.Message);
                resp = DrawingBridge.Fail(env.Id, "ERX", ex.Message);
            }

            try
            {
                if (_shellWeb != null && !_shellWeb.IsDisposed) _shellWeb.PostMessage(resp);
            }
            catch (Exception ex)
            {
                Log("drawing 응답 전송 실패: " + ex.Message);
            }
        }

        private object BuildDrawingState()
        {
            var supports = new List<object>();
            foreach (ListViewItem item in lvSupportName.Items)
            {
                string name = item.SubItems.Count > 0 ? item.SubItems[0].Text : "";
                string view = item.SubItems.Count > 1 ? item.SubItems[1].Text : "";
                supports.Add(new { name, view });
            }

            bool gridReady = System.IO.File.Exists(@"C:\TEMP\CADLIB\GridSystem.json");
            return new { supports, gridReady, captureDoc = _captureDocName ?? "" };
        }

        private object BuildSettings()
        {
            return new
            {
                template = tbTemplate?.Text ?? "",
                projectNo = tbProjectNo?.Text ?? "",
                revision = tbDwgRevision?.Text ?? ""
            };
        }

        private string SetSetting(DrawingBridge.Envelope env)
        {
            try
            {
                string key = (string)env.Args?[0];
                string val = (string)env.Args?[1] ?? "";
                switch (key)
                {
                    case "template": tbTemplate.Text = val; break;
                    case "projectNo": tbProjectNo.Text = val; break;
                    case "revision": tbDwgRevision.Text = val; break;
                    default: return DrawingBridge.Fail(env.Id, "ER_BADKEY", "Unknown setting: " + key);
                }

                return DrawingBridge.Ok(env.Id, new { ok = true });
            }
            catch (Exception ex)
            {
                Log("drawing setSetting 실패: " + ex.Message);
                return DrawingBridge.Fail(env.Id, "ERX", ex.Message);
            }
        }

        private string RemoveSupport(DrawingBridge.Envelope env)
        {
            try
            {
                string name = (string)env.Args?[0];
                if (string.IsNullOrEmpty(name)) return DrawingBridge.Fail(env.Id, "ER_BADARG", "name 누락");
                if (SupportSelection.ContainsKey(name)) SupportSelection.Remove(name);
                for (int i = lvSupportName.Items.Count - 1; i >= 0; i--)
                    if (lvSupportName.Items[i].SubItems[0].Text == name) lvSupportName.Items.RemoveAt(i);
                return DrawingBridge.Ok(env.Id, BuildDrawingState());
            }
            catch (Exception ex)
            {
                Log("drawing removeSupport 실패: " + ex.Message);
                return DrawingBridge.Fail(env.Id, "ERX", ex.Message);
            }
        }

        private string ClearSupports(DrawingBridge.Envelope env)
        {
            try
            {
                SupportSelection.Clear();
                lvSupportName.Items.Clear();
                _captureDocName = null;
                return DrawingBridge.Ok(env.Id, BuildDrawingState());
            }
            catch (Exception ex)
            {
                Log("drawing clearSupports 실패: " + ex.Message);
                return DrawingBridge.Fail(env.Id, "ERX", ex.Message);
            }
        }

        private async Task<string> AddFromSelectionAsync(DrawingBridge.Envelope env)
        {
            if (_drawingBusy) return DrawingBridge.Fail(env.Id, "ER_BUSY", "다른 작업이 진행 중입니다.");
            var allowed = new[] { "Main", "Top", "Bottom", "Front", "Back", "Left", "Right" };
            var viewDirs = new System.Collections.Generic.List<string>();
            try
            {
                var arr = env.Args?[0] as Newtonsoft.Json.Linq.JArray;
                if (arr != null)
                    foreach (var t in arr)
                    {
                        string v = (string)t;
                        if (!string.IsNullOrEmpty(v) && Array.IndexOf(allowed, v) >= 0 && !viewDirs.Contains(v))
                            viewDirs.Add(v);
                    }
                else
                {
                    // 하위호환: 단일 문자열도 허용
                    string single = (string)env.Args?[0];
                    if (!string.IsNullOrEmpty(single) && Array.IndexOf(allowed, single) >= 0)
                        viewDirs.Add(single);
                }
            }
            catch (Exception exView)
            {
                Log("addFromSelection views 파싱 실패: " + exView.Message);
                return DrawingBridge.Fail(env.Id, "ER_BADVIEW", "뷰 파싱 실패");
            }
            if (viewDirs.Count == 0)
                return DrawingBridge.Fail(env.Id, "ER_BADVIEW", "뷰 방향을 1개 이상 선택하세요.");

            _drawingBusy = true;
            try
            {
                PlantOrthoView.FileDiag("AddFromSelectionAsync: views=" + string.Join(",", viewDirs.ToArray()));
                var docs = AcadApp.DocumentManager;
                var doc = docs.MdiActiveDocument;
                if (doc == null) return DrawingBridge.Fail(env.Id, "ER1000", "열린 도면이 없습니다.");

                string code = null, msg = null;
                object added = null;
                bool cancelled = false;
                await docs.ExecuteInCommandContextAsync(async _ =>
                {
                    AddSupportCore(viewDirs, out code, out msg, out added, out cancelled);
                    await Task.CompletedTask;
                }, null);

                if (cancelled) return DrawingBridge.Fail(env.Id, "USER_CANCELLED", "선택이 취소되었습니다.");
                if (code != null) return DrawingBridge.Fail(env.Id, code, msg ?? "");
                _captureDocName = doc.Name;
                return DrawingBridge.Ok(env.Id, new { added, state = BuildDrawingState() });
            }
            catch (Exception ex)
            {
                Log("AddFromSelection 실패: " + ex.Message);
                return DrawingBridge.Fail(env.Id, "ERX", ex.Message);
            }
            finally
            {
                _drawingBusy = false;
            }
        }

        private async Task<string> Export2DAsync(DrawingBridge.Envelope env)
        {
            if (_drawingBusy) return DrawingBridge.Fail(env.Id, "ER_BUSY", "다른 작업이 진행 중입니다.");
            _drawingBusy = true;
            PlantOrthoView.FileDiag("Export2DAsync: busy=true 설정");
            try
            {
                if (lvSupportName.Items.Count == 0) return DrawingBridge.Fail(env.Id, "ER1004", "선택된 서포트가 없습니다.");
                if (!System.IO.File.Exists(@"C:\TEMP\CADLIB\GridSystem.json"))
                    return DrawingBridge.Fail(env.Id, "ER1005", "Grid System을 먼저 설정하세요(그리드는 Phase 1b).");

                var docs = AcadApp.DocumentManager;
                var doc = docs.MdiActiveDocument;
                if (doc == null) return DrawingBridge.Fail(env.Id, "ER1000", "열린 도면이 없습니다.");
                if (!string.IsNullOrEmpty(_captureDocName) &&
                    !string.Equals(doc.Name, _captureDocName, StringComparison.OrdinalIgnoreCase))
                    return DrawingBridge.Fail(env.Id, "ER_DOCMISMATCH", "서포트를 캡처한 도면과 현재 도면이 다릅니다. 목록을 지우고 다시 선택하세요.");

                // §9: item.Selected는 반드시 command context 콜백 밖(UI 스레드)에서 세팅.
                // 콜백 안 ListView 접근은 크로스스레드/데드락 위험이 있다.
                try
                {
                    var arr = env.Args != null && env.Args.Count > 0 ? env.Args[0] as Newtonsoft.Json.Linq.JArray : null;
                    var names = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (arr != null)
                        foreach (var t in arr)
                        {
                            string n = (string)t;
                            if (!string.IsNullOrEmpty(n))
                                names.Add(n);
                        }
                    if (names.Count > 0)
                    {
                        int sel = 0;
                        foreach (System.Windows.Forms.ListViewItem it in lvSupportName.Items)
                        {
                            bool on = names.Contains(it.SubItems[0].Text);
                            it.Selected = on;
                            if (on) sel++;
                        }
                        PlantOrthoView.FileDiag("Export2DAsync: 선택 필터 적용 selected=" + sel + "/" + lvSupportName.Items.Count);
                    }
                    else
                    {
                        foreach (System.Windows.Forms.ListViewItem it in lvSupportName.Items)
                            it.Selected = false;
                        PlantOrthoView.FileDiag("Export2DAsync: names 없음 → 전체 추출");
                    }
                }
                catch (Exception exSel)
                {
                    Log("Export2DAsync 선택 세팅 실패: " + exSel.Message);
                }

                string code = null;
                PlantOrthoView.FileDiag("Export2DAsync: ExecuteInCommandContextAsync 진입 직전");
                // phase2는 fire-and-forget Session 명령 체인이다. ExecuteInCommandContextAsync의
                // Task는 그 체인 전체가 끝나야 완료되는데, PFSORTHOCONTINUE가 실행 중 오쏘 문서를
                // CloseAndSave로 닫아 완료 신호가 유실 → await 영구 미완 → busy/스피너 stuck.
                // 해법: Export2DCore(스케줄링) 반환 시점에만 신호받고, 컨텍스트 Task는 백그라운드로 둔다.
                // ExecuteInCommandContextAsync는 ExecutionResult(awaitable)를 반환하므로 await하지 않고
                // fire-and-forget으로 두되, 콜백 내부에서 예외를 직접 잡아 로그(미관측 예외 방지, 삼킴 금지).
                var _schedDone = new TaskCompletionSource<bool>();
                docs.ExecuteInCommandContextAsync(async _ =>
                {
                    try
                    {
                        PlantOrthoView.FileDiag("Export2DAsync: Export2DCore 호출 직전");
                        Export2DCore(out code);
                        PlantOrthoView.FileDiag("Export2DAsync: Export2DCore 반환");
                    }
                    catch (Exception _cx)
                    {
                        PlantOrthoView.FileDiag("Export2DAsync: Export2DCore 예외(백그라운드): " + _cx.Message);
                    }
                    finally
                    {
                        _schedDone.TrySetResult(true);
                    }
                    await Task.CompletedTask;
                }, null);
                await _schedDone.Task;
                PlantOrthoView.FileDiag("Export2DAsync: 스케줄링 완료 신호 수신 (code=" + (code ?? "null") + ")");

                if (code != null) return DrawingBridge.Fail(env.Id, code, "Export 2D 실패");
                return DrawingBridge.Ok(env.Id, new { ok = true });
            }
            catch (Exception ex)
            {
                PlantOrthoView.FileDiag("Export2DAsync 예외: " + ex.Message);
                Log("Export2D 실패: " + ex.Message);
                return DrawingBridge.Fail(env.Id, "ERX", ex.Message);
            }
            finally
            {
                _drawingBusy = false;
                PlantOrthoView.FileDiag("Export2DAsync: busy=false (finally)");
            }
        }
    }
}
