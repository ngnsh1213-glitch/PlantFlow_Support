using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace PlantFlow_Support
{
    public class SpanPlacer
    {
        private const int INSERT_LIMIT = 100;       // 추가삽입 반복 상한(무한루프 방지)
        private const double GEOM_EPS = 1e-6;       // 간격 비교 허용오차
        private const double CONF_MOVED = 0.8;      // 충돌 이동 제안 신뢰도
        private const double CONF_INSERTED = 0.7;   // 추가삽입 제안 신뢰도

        public List<SupportProposal> ComputeProposals(
            IEnumerable<ObjectId> selectedPipes,
            LoadCase caseSelection,
            SpanTable table,
            ITopologyResolver topo,
            Transaction tr,
            double collisionTol,
            out List<EligibilityResult> blocked)
        {
            blocked = new List<EligibilityResult>();
            var proposals = new List<SupportProposal>();

            var segments = topo.Resolve(selectedPipes, tr);
            var obstacles = topo.GetObstacles(selectedPipes, tr) ?? new List<Point3d>();
            bool isInsCase = caseSelection == LoadCase.EmptyIns || caseSelection == LoadCase.FullWaterIns;

            foreach (var seg in segments)
            {
                // 4-0 적격성 게이트 (fail-closed)
                if (seg.Dn == 0)
                { blocked.Add(Block(seg, "Dn property missing")); continue; }
                if (!table.HasDn(seg.Dn))
                { blocked.Add(Block(seg, $"Dn {seg.Dn} is out of allowed range")); continue; }
                if (seg.DesignTemperature > 200.0)
                { blocked.Add(Block(seg, "temperature exceeds 200C")); continue; }
                if (seg.InsulationThickness >= 100.0)
                { blocked.Add(Block(seg, "insulation exceeds 100mm")); continue; }

                // D4: 케이스 모순(양방향)
                if (seg.HasInsulation && !isInsCase)
                { blocked.Add(Block(seg, "load case mismatch (insulated line needs INS case)")); continue; }
                if (!seg.HasInsulation && isInsCase)
                { blocked.Add(Block(seg, "load case mismatch (non-insulated line)")); continue; }

                // 4-1 자유단 차단
                if (!seg.StartFixed || !seg.EndFixed)
                { blocked.Add(Block(seg, "free end, manual review")); continue; }

                double s = table.MaxSpan(seg.Dn, caseSelection);
                double l = seg.Length;
                if (l <= s) continue; // 양단 고정 + 허용 이내 → 내부 서포트 불필요

                // 4-1 등간격 분배: 두 고정점 사이 n = max(0, ceil(L/S) - 1)
                int n = Math.Max(0, (int)Math.Ceiling(l / s) - 1);
                Vector3d dir = (seg.End - seg.Start).GetNormal();
                var positions = new List<Point3d>();
                double interval = l / (n + 1);
                for (int i = 1; i <= n; i++)
                    positions.Add(seg.Start + dir * (interval * i));

                // 4-2 충돌 회피
                bool anyMoved = AvoidCollisions(positions, seg, dir, s, obstacles, collisionTol);

                // 4-2 재검증 + 추가삽입 (이동으로 S 초과 시)
                bool unresolved;
                bool inserted = ReSpanAndInsert(positions, seg, s, out unresolved);
                if (unresolved)
                { blocked.Add(Block(seg, "collision unresolved")); continue; }

                double conf = inserted ? CONF_INSERTED : (anyMoved ? CONF_MOVED : 1.0);
                foreach (var p in positions)
                    proposals.Add(new SupportProposal
                    {
                        Position = p,
                        Dn = seg.Dn,
                        Symbol = "RS6",
                        Case = caseSelection,
                        Confidence = conf
                    });
            }

            return proposals;
        }

        private static EligibilityResult Block(PipeSegment seg, string reason)
            => new EligibilityResult { Ok = false, Reason = reason, Source = seg.PipeId };

        // 각 제안이 장애물 ±tol 내면 세그먼트 방향으로 가장 가까운 빈 지점으로 이동.
        // 세그먼트 경계를 벗어나거나 빈 지점을 못 찾으면 원위치 유지(재검증이 후처리).
        private static bool AvoidCollisions(List<Point3d> positions, PipeSegment seg, Vector3d dir,
                                            double s, List<Point3d> obstacles, double tol)
        {
            if (obstacles.Count == 0 || tol <= 0) return false;
            bool moved = false;
            double l = seg.Length;
            double step = Math.Max(tol, l / 100.0); // 탐색 보폭

            for (int i = 0; i < positions.Count; i++)
            {
                if (!HasObstacle(positions[i], obstacles, tol)) continue;

                Point3d? clear = null;
                // 양방향으로 점진 탐색하여 빈 지점 찾기(세그먼트 내부 한정).
                for (double off = step; off < l; off += step)
                {
                    double dAlong = Along(positions[i], seg.Start, dir);
                    foreach (double cand in new[] { dAlong + off, dAlong - off })
                    {
                        if (cand <= 0 || cand >= l) continue;
                        Point3d c = seg.Start + dir * cand;
                        if (!HasObstacle(c, obstacles, tol)) { clear = c; break; }
                    }
                    if (clear.HasValue) break;
                }

                if (clear.HasValue) { positions[i] = clear.Value; moved = true; }
            }

            if (moved) positions.Sort((a, b) => Along(a, seg.Start, dir).CompareTo(Along(b, seg.Start, dir)));
            return moved;
        }

        // 이동 후(또는 일반) 연속 간격 재검증. S 초과 구간 중점에 삽입, 전 간격 ≤ S 될 때까지 반복.
        private static bool ReSpanAndInsert(List<Point3d> positions, PipeSegment seg, double s, out bool unresolved)
        {
            unresolved = false;
            bool inserted = false;
            var pts = new List<Point3d> { seg.Start };
            pts.AddRange(positions);
            pts.Add(seg.End);

            int insertions = 0;
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    if (pts[i].DistanceTo(pts[i + 1]) > s + GEOM_EPS)
                    {
                        if (insertions >= INSERT_LIMIT) { unresolved = true; return inserted; }
                        Point3d mid = pts[i] + (pts[i + 1] - pts[i]) * 0.5;
                        pts.Insert(i + 1, mid);
                        insertions++;
                        inserted = true;
                        changed = true;
                        break;
                    }
                }
            }

            if (inserted)
            {
                positions.Clear();
                for (int i = 1; i < pts.Count - 1; i++) positions.Add(pts[i]);
            }
            return inserted;
        }

        private static bool HasObstacle(Point3d p, List<Point3d> obstacles, double tol)
        {
            foreach (var o in obstacles)
                if (p.DistanceTo(o) <= tol) return true;
            return false;
        }

        // start 기준 dir 축 위의 스칼라 위치.
        private static double Along(Point3d p, Point3d start, Vector3d dir)
            => (p - start).DotProduct(dir);
    }
}
