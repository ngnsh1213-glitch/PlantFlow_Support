using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

// [임시] Phase 2 SpanPlacer 검증 전용 명령. 검증 후 이 파일 삭제 가능.
namespace PlantFlow_Support
{
    public partial class Commands
    {
        // ① 순수 로직 단위테스트 6종 실행. 도면/선택 불필요.
        [CommandMethod("PFSSPANTEST")]
        public void RunSpanUnitTests()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            try
            {
                string result = SpanPlacerTests.RunTests();
                ed.WriteMessage("\n========== PFS Span Unit Tests ==========\n");
                ed.WriteMessage(result + "\n");
                ed.WriteMessage("=========================================\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[PFSSPANTEST] 실패: " + ex.Message + "\n");
            }
        }

        // ② 선택한 파이프로 실제 제안 계산(도면 변경 없음). 제안 개수/위치 + blocked 사유 출력.
        [CommandMethod("PFSSPANTRY")]
        public void TrySpanProposals()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                string jsonPath = ResolveSpanJsonPath(ed);
                if (jsonPath == null)
                {
                    ed.WriteMessage("\n[PFSSPANTRY] span_table_JIS.json 경로를 찾지 못했습니다. (어셈블리 인근/개발경로 탐색 실패)\n");
                    return;
                }
                ed.WriteMessage("\n[PFSSPANTRY] 스팬표: " + jsonPath + "\n");
                SpanTable table = SpanTable.Load(jsonPath);

                ed.WriteMessage("\n파이프를 선택하세요 (제안 계산용, 도면 변경 없음):\n");
                PromptSelectionResult sel = ed.GetSelection();
                if (sel.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("[PFSSPANTRY] 선택 취소.\n");
                    return;
                }

                var pipeIds = new List<ObjectId>(sel.Value.GetObjectIds());

                // --- 진단: 선택 객체의 실제 타입/포트수 (Resolve가 왜 세그먼트를 안 만드는지 규명) ---
                using (doc.LockDocument())
                using (Transaction dtr = db.TransactionManager.StartTransaction())
                {
                    ed.WriteMessage("\n---------- 선택 객체 진단 ----------\n");
                    foreach (ObjectId oid in pipeIds)
                    {
                        try
                        {
                            DBObject o = dtr.GetObject(oid, OpenMode.ForRead);
                            string typeName = o.GetType().FullName;
                            bool isPipe = o is Autodesk.ProcessPower.PnP3dObjects.Pipe;
                            bool isPart = o is Autodesk.ProcessPower.PnP3dObjects.Part;
                            int portCount = -1;
                            try
                            {
                                if (isPart)
                                {
                                    var pc = ((Autodesk.ProcessPower.PnP3dObjects.Part)o)
                                        .GetPorts((Autodesk.ProcessPower.PnP3dObjects.PortType)7);
                                    portCount = pc != null ? pc.Count : 0;
                                }
                            }
                            catch (System.Exception pe) { ed.WriteMessage("    포트취득 예외: " + pe.Message + "\n"); }
                            ed.WriteMessage($"  type={typeName} | isPipe={isPipe} | isPart={isPart} | ports={portCount}\n");
                        }
                        catch (System.Exception oe) { ed.WriteMessage("  객체 열기 예외: " + oe.Message + "\n"); }
                    }
                    ed.WriteMessage("------------------------------------\n");
                    dtr.Commit();
                }

                var placer = new SpanPlacer();
                var topo = new PlantTopologyResolver();
                List<EligibilityResult> blocked;

                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 기본 하중케이스 = FULL WATER, collisionTol = 0.15(도면 단위 mm 가정시 150). 환경에 맞게 조정.
                    List<SupportProposal> proposals = placer.ComputeProposals(
                        pipeIds, LoadCase.FullWater, table, topo, tr, 0.15, out blocked);
                    tr.Commit();

                    ed.WriteMessage("\n========== PFS Span Proposals ==========\n");
                    ed.WriteMessage($"제안(Proposals): {proposals.Count}건\n");
                    int idx = 1;
                    foreach (var p in proposals)
                    {
                        ed.WriteMessage($"  {idx++}. Dn{p.Dn} {p.Symbol} @ ({p.Position.X:F1}, {p.Position.Y:F1}, {p.Position.Z:F1}) conf={p.Confidence:F2}\n");
                    }
                    ed.WriteMessage($"차단/진단(Blocked): {blocked.Count}건\n");
                    foreach (var b in blocked)
                    {
                        ed.WriteMessage($"  - {b.Reason}\n");
                    }
                    ed.WriteMessage("========================================\n");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[PFSSPANTRY] 실패: " + ex.Message + "\n");
            }
        }

        // span_table_JIS.json 위치 탐색: 어셈블리 인근 → 알려진 개발경로.
        private static string ResolveSpanJsonPath(Editor ed)
        {
            var candidates = new List<string>();
            try
            {
                string asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrEmpty(asmDir))
                {
                    candidates.Add(Path.Combine(asmDir, "Support", "library", "span_table_JIS.json"));
                    candidates.Add(Path.Combine(asmDir, "library", "span_table_JIS.json"));
                    candidates.Add(Path.Combine(asmDir, "span_table_JIS.json"));
                }
            }
            catch { /* 어셈블리 경로 취득 실패는 무시하고 개발경로로 폴백 */ }

            candidates.Add(@"D:\PlantFlow\PlantFlow_Support\PlantFlow_Support\Support\library\span_table_JIS.json");

            foreach (string c in candidates)
            {
                try { if (File.Exists(c)) return c; }
                catch { /* 개별 후보 접근 실패는 다음 후보로 */ }
            }
            return null;
        }
    }
}
