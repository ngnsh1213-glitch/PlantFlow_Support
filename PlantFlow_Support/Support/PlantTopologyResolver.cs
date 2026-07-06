using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.ProcessPower.PnP3dObjects;
using Autodesk.ProcessPower.DataLinks;

namespace PlantFlow_Support
{
    // D2/D3: Plant3D 토폴로지 식별 — 모델스페이스 Part 열거 + 포트 공간매칭.
    // 코드베이스 검증 패턴(런타임 타입 Pipe/Part, Part.GetPorts) 재사용.
    // ★빌드환경(AutoCAD2026+Plant3D) 확정 필요 항목은 아래 상수에 표시.
    public class PlantTopologyResolver : ITopologyResolver
    {
        // ── 빌드환경 확정 상수 (실 DB/IntelliSense로 검증) ──
        // PnP 프로퍼티 키명. 후보이며 실제 스키마에서 확인할 것.
        private const string PROP_SIZE = "Size";
        private const string PROP_TEMP = "DesignTemperature";
        private const string PROP_INS = "InsulationThickness";
        // 포트 공간매칭 허용오차(도면 단위, mm 가정). 너무 작으면 미스매치, 너무 크면 오접합.
        private const double PORT_MATCH_TOL = 1.0;

        // 모델스페이스 비-Pipe Part 캐시(한 Resolve 호출 1회 구성).
        private struct PartPorts
        {
            public ObjectId Id;
            public List<Point3d> Ports;
        }

        public List<PipeSegment> Resolve(IEnumerable<ObjectId> selectedPipes, Transaction tr)
        {
            var segments = new List<PipeSegment>();
            DataLinksManager dl_manager = SafeDlManager();

            // 비-Pipe Part(피팅/노즐/밸브/플랜지 등)의 포트 위치를 1회 수집.
            List<PartPorts> nonPipeParts = CollectNonPipeParts(tr);

            foreach (var pipeId in selectedPipes)
            {
                PipeSegment? seg = BuildSegment(pipeId, tr, dl_manager, nonPipeParts);
                if (seg.HasValue) segments.Add(seg.Value);
            }
            return segments;
        }

        public List<Point3d> GetObstacles(IEnumerable<ObjectId> selectedPipes, Transaction tr)
        {
            // v1: 모든 비-Pipe Part의 포트 위치를 회피 대상으로 본다(밸브/플랜지/피팅/노즐 + 보수적).
            // 기존 Support 엔티티도 포함.
            var obstacles = new List<Point3d>();
            foreach (var pp in CollectNonPipeParts(tr))
                obstacles.AddRange(pp.Ports);
            return obstacles;
        }

        // ── 내부 구현 ──

        private static DataLinksManager SafeDlManager()
        {
            try
            {
                return new PSUtil().dl_manager;
            }
            catch (System.Exception ex)
            {
                PSUtil.Print("[SpanPlacer] DataLinksManager 획득 실패: " + ex.Message);
                return null;
            }
        }

        private static Database ActiveDb()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            return doc != null ? doc.Database : null;
        }

        // 모델스페이스를 순회하여 Pipe가 아닌 Part들의 포트 위치를 수집.
        private List<PartPorts> CollectNonPipeParts(Transaction tr)
        {
            var result = new List<PartPorts>();
            Database db = ActiveDb();
            if (db == null) return result;

            try
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    DBObject o;
                    try { o = tr.GetObject(id, OpenMode.ForRead); }
                    catch { continue; }

                    if (o is Pipe) continue;          // 파이프는 세그먼트, 장애물 아님
                    if (!(o is Part part)) continue;  // Plant3D Part만 대상

                    List<Point3d> ports = GetPortPositions(part);
                    if (ports.Count > 0)
                        result.Add(new PartPorts { Id = id, Ports = ports });
                }
            }
            catch (System.Exception ex)
            {
                PSUtil.Print("[SpanPlacer] 모델스페이스 Part 수집 실패: " + ex.Message);
            }
            return result;
        }

        private static List<Point3d> GetPortPositions(Part part)
        {
            var pts = new List<Point3d>();
            try
            {
                PortCollection ports = part.GetPorts((PortType)7);
                if (ports != null)
                    for (int i = 0; i < ports.Count; i++)
                        pts.Add(ports[i].Position);
            }
            catch (System.Exception ex)
            {
                PSUtil.Print("[SpanPlacer] 포트 취득 실패: " + ex.Message);
            }
            return pts;
        }

        private PipeSegment? BuildSegment(ObjectId pipeId, Transaction tr, DataLinksManager dl, List<PartPorts> nonPipeParts)
        {
            DBObject obj;
            try { obj = tr.GetObject(pipeId, OpenMode.ForRead); }
            catch (System.Exception ex) { PSUtil.Print("[SpanPlacer] Pipe 열기 실패: " + ex.Message); return null; }

            if (!(obj is Pipe pipe)) return null;

            List<Point3d> pports = GetPortPositions(pipe);
            if (pports.Count < 2) return null; // 끝 포트 2개 필요(arc/compound 등 미지원은 자연 배제)

            Point3d start = pports[0];
            Point3d end = pports[pports.Count - 1];

            // 속성 취득(실패는 묵인하지 않고 0/false로 두어 SpanPlacer 게이트가 blocked 처리)
            int dn = 0; double designTemp = 0.0; double insThick = 0.0; bool hasIns = false;
            try
            {
                var props = dl != null
                    ? dl.GetProperties(pipeId, new StringCollection { PROP_SIZE, PROP_TEMP, PROP_INS }, true)
                    : null;
                if (props != null && props.Count >= 3)
                {
                    dn = ParseNominal(props[0]);
                    double.TryParse(props[1], out designTemp);
                    if (double.TryParse(props[2], out insThick) && insThick > 0.0)
                        hasIns = true;
                }
            }
            catch (System.Exception ex)
            {
                PSUtil.Print("[SpanPlacer] 속성 취득 실패(" + pipeId + "): " + ex.Message);
            }

            // Fixed 판정: 끝점이 비-Pipe Part 포트와 일치하면 고정.
            // fail-closed: 일치 없으면 false(자유단) → SpanPlacer가 manual review로 차단(거짓 고정 금지).
            bool startFixed = IsConnectedToNonPipe(start, nonPipeParts);
            bool endFixed = IsConnectedToNonPipe(end, nonPipeParts);

            return new PipeSegment
            {
                PipeId = pipeId,
                Start = start,
                End = end,
                Length = start.DistanceTo(end),
                Dn = dn,
                DesignTemperature = designTemp,
                InsulationThickness = insThick,
                HasInsulation = hasIns,
                StartFixed = startFixed,
                EndFixed = endFixed
            };
        }

        private static bool IsConnectedToNonPipe(Point3d endPoint, List<PartPorts> nonPipeParts)
        {
            foreach (var pp in nonPipeParts)
                foreach (var port in pp.Ports)
                    if (endPoint.DistanceTo(port) <= PORT_MATCH_TOL)
                        return true;
            return false;
        }

        // "100A", "100B", "100 mm" 등에서 선행 정수만 추출.
        private static int ParseNominal(string sizeStr)
        {
            if (string.IsNullOrEmpty(sizeStr)) return 0;
            var digits = new string(sizeStr.TrimStart().TakeWhile(char.IsDigit).ToArray());
            int v;
            return int.TryParse(digits, out v) ? v : 0;
        }
    }
}
