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
            string viewDir = (string)env.Args?[0] ?? "";
            var allowed = new[] { "Top", "Bottom", "Front", "Back", "Left", "Right" };
            if (Array.IndexOf(allowed, viewDir) < 0)
                return DrawingBridge.Fail(env.Id, "ER_BADVIEW", "뷰 방향을 먼저 선택하세요.");

            _drawingBusy = true;
            try
            {
                var docs = AcadApp.DocumentManager;
                var doc = docs.MdiActiveDocument;
                if (doc == null) return DrawingBridge.Fail(env.Id, "ER1000", "열린 도면이 없습니다.");

                string code = null, msg = null;
                object added = null;
                bool cancelled = false;
                await docs.ExecuteInCommandContextAsync(async _ =>
                {
                    AddSupportCore(viewDir, out code, out msg, out added, out cancelled);
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

                string code = null;
                await docs.ExecuteInCommandContextAsync(async _ =>
                {
                    Export2DCore(out code);
                    await Task.CompletedTask;
                }, null);

                if (code != null) return DrawingBridge.Fail(env.Id, code, "Export 2D 실패");
                return DrawingBridge.Ok(env.Id, new { ok = true });
            }
            catch (Exception ex)
            {
                Log("Export2D 실패: " + ex.Message);
                return DrawingBridge.Fail(env.Id, "ERX", ex.Message);
            }
            finally
            {
                _drawingBusy = false;
            }
        }
    }
}
