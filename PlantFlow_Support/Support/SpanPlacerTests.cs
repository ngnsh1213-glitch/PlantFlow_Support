using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace PlantFlow_Support
{
    // 지오메트리/DB 무관 순수 로직 테스트용 가짜 리졸버.
    public class FakeTopologyResolver : ITopologyResolver
    {
        public List<PipeSegment> SegmentsToReturn = new List<PipeSegment>();
        public List<Point3d> ObstaclesToReturn = new List<Point3d>();

        public List<PipeSegment> Resolve(IEnumerable<ObjectId> selectedPipes, Transaction tr) => SegmentsToReturn;
        public List<Point3d> GetObstacles(IEnumerable<ObjectId> selectedPipes, Transaction tr) => ObstaclesToReturn;
    }

    // 콘솔 실행형 단위테스트(프로젝트에 테스트 프레임워크 부재 → 자체 assert).
    // 호출: SpanPlacerTests.RunTests() → 결과 문자열 반환(통과/실패 요약).
    public static class SpanPlacerTests
    {
        private static int _pass;
        private static int _fail;
        private static readonly List<string> _log = new List<string>();

        // X축 직선 세그먼트 생성 헬퍼.
        private static PipeSegment Seg(double length, int dn, bool startFixed, bool endFixed,
            double temp = 20.0, double ins = 0.0, bool hasIns = false)
        {
            return new PipeSegment
            {
                PipeId = ObjectId.Null,
                Start = Point3d.Origin,
                End = new Point3d(length, 0, 0),
                Length = length,
                Dn = dn,
                DesignTemperature = temp,
                InsulationThickness = ins,
                HasInsulation = hasIns,
                StartFixed = startFixed,
                EndFixed = endFixed
            };
        }

        private static SpanTable Table()
        {
            // 100A: EMPTY=8000(테스트 단순화). 케이스 4개 동일하게 8000 + INS 케이스 구분용.
            return SpanTable.FromMemory(new Dictionary<string, List<double>>
            {
                { "100", new List<double> { 8000, 8000, 8000, 8000 } }
            });
        }

        private static List<SupportProposal> Run(PipeSegment seg, LoadCase c,
            out List<EligibilityResult> blocked, List<Point3d> obstacles = null, double tol = 0.15)
        {
            var topo = new FakeTopologyResolver { SegmentsToReturn = new List<PipeSegment> { seg } };
            if (obstacles != null) topo.ObstaclesToReturn = obstacles;
            return new SpanPlacer().ComputeProposals(
                new List<ObjectId> { ObjectId.Null }, c, Table(), topo, null, tol, out blocked);
        }

        private static void Check(bool cond, string name)
        {
            if (cond) { _pass++; _log.Add("PASS: " + name); }
            else { _fail++; _log.Add("FAIL: " + name); }
        }

        public static string RunTests()
        {
            _pass = 0; _fail = 0; _log.Clear();
            List<EligibilityResult> b;

            // 1) L=10000,S=8000,양단Fixed → n=1, 중앙 1개.
            var p1 = Run(Seg(10000, 100, true, true), LoadCase.Empty, out b);
            Check(p1.Count == 1 && Math.Abs(p1[0].Position.X - 5000) < 1e-6, "T1 L10000/S8000 -> 1@center");

            // 2) L=8000,S=8000 → n=0.
            var p2 = Run(Seg(8000, 100, true, true), LoadCase.Empty, out b);
            Check(p2.Count == 0, "T2 L8000/S8000 -> 0");

            // 3) L=25000,S=8000 → n=3, 각 간격 6250 ≤ S.
            var p3 = Run(Seg(25000, 100, true, true), LoadCase.Empty, out b);
            bool spans3 = p3.Count == 3
                && Math.Abs(p3[0].Position.X - 6250) < 1e-6
                && Math.Abs(p3[2].Position.X - 18750) < 1e-6;
            Check(spans3, "T3 L25000/S8000 -> 3@6250 step");

            // 4) 자유단 → blocked "free end".
            Run(Seg(10000, 100, true, false), LoadCase.Empty, out b);
            Check(b.Any(x => x.Reason.Contains("free end")), "T4 free end blocked");

            // 5) Dn=550 / temp=250 / ins=120 → 각각 blocked.
            Run(Seg(10000, 550, true, true), LoadCase.Empty, out b);
            Check(b.Any(x => x.Reason.Contains("out of allowed range")), "T5a Dn550 blocked");
            Run(Seg(10000, 100, true, true, temp: 250), LoadCase.Empty, out b);
            Check(b.Any(x => x.Reason.Contains("temperature")), "T5b temp250 blocked");
            Run(Seg(10000, 100, true, true, ins: 120, hasIns: true), LoadCase.EmptyIns, out b);
            Check(b.Any(x => x.Reason.Contains("insulation exceeds")), "T5c ins120 blocked");

            // 6) 충돌 이동 후 간격>S → 추가삽입으로 전 간격 ≤ S.
            // L=10000,S=8000 기본 1개(5000). 5000에 장애물 → 이동, 재검증으로 전 간격 ≤ 8000 유지.
            var obstacles = new List<Point3d> { new Point3d(5000, 0, 0) };
            var p6 = Run(Seg(10000, 100, true, true), LoadCase.Empty, out b, obstacles, tol: 200);
            bool allWithinS = true;
            var xs = p6.Select(p => p.Position.X).OrderBy(x => x).ToList();
            xs.Insert(0, 0); xs.Add(10000);
            for (int i = 0; i < xs.Count - 1; i++) if (xs[i + 1] - xs[i] > 8000 + 1e-6) allWithinS = false;
            Check(p6.Count >= 1 && allWithinS, "T6 collision -> all gaps <= S");

            string summary = $"SpanPlacerTests: {_pass} passed, {_fail} failed";
            return summary + Environment.NewLine + string.Join(Environment.NewLine, _log);
        }
    }
}
