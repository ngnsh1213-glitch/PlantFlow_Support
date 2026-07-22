using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace PlantFlow_Support
{
    // Phase 1a: drawing 채널 연산(SupportSelection/lvSupportName 소유). UI 스레드 async, CAD만 command context.
    public partial class PaletteTab
    {
        private bool _drawingBusy;
        private string _captureDocName; // MDI 가드: 서포트 캡처 시 활성 문서명.
        private IntPtr _captureDbPtr = IntPtr.Zero; // Database 관리 래퍼는 재생성될 수 있어 네이티브 포인터로 비교.

        private sealed class NotabSelectionItem
        {
            internal string Tag;
            internal string Status;
            internal ObjectId Id;
        }

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
                    case "addBatchFromSelection": resp = await AddFromSelectionAsync(env); break;
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
                string status = item.SubItems.Count > 1 ? item.SubItems[1].Text : "";
                supports.Add(new { name, status });
            }
            return new { supports, captureDoc = _captureDocName ?? "" };
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
                _captureDbPtr = IntPtr.Zero;
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
            _drawingBusy = true;
            try
            {
                var docs = AcadApp.DocumentManager;
                var doc = docs.MdiActiveDocument;
                if (doc == null) return DrawingBridge.Fail(env.Id, "ER1000", "열린 도면이 없습니다.");

                string code = null;
                string message = null;
                bool cancelled = false;
                List<NotabSelectionItem> selected = null;
                await docs.ExecuteInCommandContextAsync(async _ =>
                {
                    try
                    {
                        AcadApp.MainWindow.Focus();
                        PromptSelectionResult result = doc.Editor.GetSelection();
                        if (result.Status == PromptStatus.Cancel) { cancelled = true; return; }
                        if (result.Status != PromptStatus.OK || result.Value == null || result.Value.Count == 0)
                        {
                            code = "ER1002";
                            message = "선택된 서포트가 없습니다.";
                            return;
                        }
                        selected = BuildNotabSelectionItems(doc.Database, new Commands().CollectSelectedSupportIdsForUi(doc.Database, result.Value.GetObjectIds()));
                        if (selected.Count == 0) { code = "ER1002"; message = "선택된 서포트가 없습니다."; }
                    }
                    catch (Exception ex)
                    {
                        PlantOrthoView.FileDiag("addBatchFromSelection 선택 수집 예외: " + ex.GetType().Name + ": " + ex.Message);
                        code = "ERX";
                        message = ex.Message;
                    }
                    await Task.CompletedTask;
                }, null);

                if (cancelled) return DrawingBridge.Fail(env.Id, "USER_CANCELLED", "선택이 취소되었습니다.");
                if (code != null) return DrawingBridge.Fail(env.Id, code, message ?? "");
                int duplicates = 0;
                foreach (NotabSelectionItem item in selected)
                {
                    if (SupportSelection.ContainsKey(item.Tag) || ListViewContainsSupport(item.Tag)) { duplicates++; continue; }
                    SupportSelection.Add(item.Tag, new SupportInfo { Name = item.Tag, Ids = new[] { item.Id } });
                    lvSupportName.Items.Add(new ListViewItem(new[] { item.Tag, item.Status ?? "" }));
                }
                _captureDocName = doc.Name;
                _captureDbPtr = doc.Database.UnmanagedObject;
                int addedCount = selected.Count - duplicates;
                PlantOrthoView.FileDiag("addBatchFromSelection done added=" + addedCount + " duplicates=" + duplicates);
                return DrawingBridge.Ok(env.Id, new { added = new { count = addedCount, duplicates, unnamed = 0, invalid = 0 }, state = BuildDrawingState() });
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

                var docs = AcadApp.DocumentManager;
                var doc = docs.MdiActiveDocument;
                if (doc == null) return DrawingBridge.Fail(env.Id, "ER1000", "열린 도면이 없습니다.");
                if (!string.IsNullOrEmpty(_captureDocName) &&
                    !string.Equals(doc.Name, _captureDocName, StringComparison.OrdinalIgnoreCase))
                    return DrawingBridge.Fail(env.Id, "ER_DOCMISMATCH", "서포트를 캡처한 도면과 현재 도면이 다릅니다. 목록을 지우고 다시 선택하세요.");
                if (_captureDbPtr != IntPtr.Zero && doc.Database.UnmanagedObject != _captureDbPtr)
                    return DrawingBridge.Fail(env.Id, "ER_DOCMISMATCH", "서포트를 캡처한 데이터베이스와 현재 도면이 다릅니다. 목록을 지우고 다시 선택하세요.");

                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var arr = env.Args != null && env.Args.Count > 0 ? env.Args[0] as Newtonsoft.Json.Linq.JArray : null;
                if (arr != null) foreach (var token in arr) { string name = (string)token; if (!string.IsNullOrEmpty(name)) names.Add(name); }
                var ids = new List<ObjectId>();
                foreach (string name in names)
                {
                    SupportInfo info;
                    if (SupportSelection.TryGetValue(name, out info) && info != null && info.Ids != null) ids.AddRange(info.Ids);
                }
                if (ids.Count == 0) return DrawingBridge.Fail(env.Id, "ER1004", "추출할 서포트를 선택하세요.");

                int ok = 0, fail = 0;
                List<string> failTags = null;
                bool entered = false;
                await docs.ExecuteInCommandContextAsync(async _ =>
                {
                    try
                    {
                        entered = new Commands().RunNotabBatch(doc, ids, out ok, out fail, out failTags);
                    }
                    catch (Exception ex)
                    {
                        PlantOrthoView.FileDiag("Export2DAsync 무탭 배치 예외: " + ex.GetType().Name + ": " + ex.Message);
                        fail = ids.Count;
                        failTags = names.ToList();
                    }
                    await Task.CompletedTask;
                }, null);
                if (!entered) return DrawingBridge.Fail(env.Id, "ER_BUSY", "다른 무탭 배치가 진행 중입니다.");
                return DrawingBridge.Ok(env.Id, new { ok, fail, failTags = failTags ?? new List<string>() });
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

        private List<NotabSelectionItem> BuildNotabSelectionItems(Database database, IList<ObjectId> ids)
        {
            var result = new List<NotabSelectionItem>();
            var util = new PSUtil();
            if (util.dl_manager == null) throw new InvalidOperationException("Plant 속성 관리자에 연결할 수 없습니다.");
            foreach (ObjectId id in ids)
            {
                try
                {
                    var names = new System.Collections.Specialized.StringCollection { "SupportName", "ShortDescription" };
                    var values = util.dl_manager.GetProperties(id, names, true);
                    string tag = values != null && values.Count > 0 ? values[0] : string.Empty;
                    if (string.IsNullOrWhiteSpace(tag)) { PlantOrthoView.FileDiag("addBatchFromSelection 무명 서포트 제외 id=" + id); continue; }
                    string status = values != null && values.Count > 1 ? values[1] : string.Empty;
                    result.Add(new NotabSelectionItem { Tag = tag, Status = status, Id = id });
                }
                catch (Exception ex) { PlantOrthoView.FileDiag("addBatchFromSelection 속성 조회 예외 id=" + id + ": " + ex.GetType().Name + ": " + ex.Message); }
            }
            return result;
        }
    }
}
