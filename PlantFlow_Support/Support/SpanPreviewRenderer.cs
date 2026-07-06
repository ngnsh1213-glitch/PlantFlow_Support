using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace PlantFlow_Support
{
    // Phase 3a: 제안 위치를 도면에 비파괴(임시 그래픽)로 표시.
    // TransientManager 사용 → DB에 엔티티를 쓰지 않으므로 도면 변경 없음(undo/저장 무영향).
    // 분담 경계: UI(WinForms 탭)는 Show/Clear만 호출한다.
    public class SpanPreviewRenderer
    {
        // 마커 기본 반경(도면 단위, mm 가정). UI에서 조정 가능.
        public double MarkerRadius { get; set; } = 150.0;

        private readonly List<Entity> _transients = new List<Entity>();

        // 제안 목록을 임시 마커로 표시. 호출 시 이전 마커는 자동 제거.
        // 반환: 표시된 마커 수.
        public int Show(IEnumerable<SupportProposal> proposals)
        {
            Clear();
            if (proposals == null) return 0;

            TransientManager tm = TransientManager.CurrentTransientManager;
            var ints = new IntegerCollection();
            int count = 0;

            foreach (var p in proposals)
            {
                Entity marker = null;
                try
                {
                    marker = BuildMarker(p);
                    if (marker == null) continue;
                    tm.AddTransient(marker, TransientDrawingMode.DirectShortTerm, 128, ints);
                    _transients.Add(marker);
                    count++;
                }
                catch (System.Exception ex)
                {
                    PSUtil.Print("[SpanPreview] 마커 생성 실패: " + ex.Message);
                    if (marker != null) { try { marker.Dispose(); } catch { } }
                }
            }

            RedrawView();
            return count;
        }

        // 모든 임시 마커 제거.
        public void Clear()
        {
            if (_transients.Count == 0) return;
            TransientManager tm = TransientManager.CurrentTransientManager;
            var ints = new IntegerCollection();
            foreach (var ent in _transients)
            {
                try { tm.EraseTransient(ent, ints); }
                catch (System.Exception ex) { PSUtil.Print("[SpanPreview] 마커 제거 실패: " + ex.Message); }
                finally { try { ent.Dispose(); } catch { } }
            }
            _transients.Clear();
            RedrawView();
        }

        // 신뢰도에 따라 색을 달리한 원형 마커 + 작은 축 십자(가시성).
        // 1.0=초록, 0.7~<1.0=노랑, 그 외=주황.
        private Entity BuildMarker(SupportProposal p)
        {
            Color color = p.Confidence >= 1.0
                ? Color.FromRgb(0, 200, 0)
                : (p.Confidence >= 0.7 ? Color.FromRgb(230, 230, 0) : Color.FromRgb(255, 140, 0));

            // 단일 엔티티만 transient로 관리하기 위해 Circle 사용(평면 노멀 Z).
            // 3D 가시성은 v1 충분. 필요 시 후속에서 다축 마커로 확장.
            var circle = new Circle(p.Position, Vector3d.ZAxis, MarkerRadius);
            circle.Color = color;
            return circle;
        }

        private static void RedrawView()
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null) doc.Editor.UpdateScreen();
            }
            catch { /* 화면 갱신 실패는 치명적 아님 */ }
        }
    }
}
