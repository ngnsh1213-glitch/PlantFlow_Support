using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace PlantFlow_Support
{
    public enum LoadCase
    {
        Empty = 0,
        FullWater = 1,
        EmptyIns = 2,
        FullWaterIns = 3
    }

    public struct PipeSegment
    {
        public ObjectId PipeId;
        public Point3d Start;
        public Point3d End;
        public double Length;
        public int Dn;
        public double DesignTemperature;
        public double InsulationThickness;
        public bool HasInsulation;
        public bool StartFixed;
        public bool EndFixed;
    }

    public struct SupportProposal
    {
        public Point3d Position;
        public int Dn;
        public string Symbol;
        public LoadCase Case;
        public double Confidence;
    }

    public struct EligibilityResult
    {
        public bool Ok;
        public string Reason;
        public ObjectId Source;
    }

    public interface ITopologyResolver
    {
        List<PipeSegment> Resolve(IEnumerable<ObjectId> selectedPipes, Transaction tr);

        // D3 충돌 회피용: 배치 회피 대상(밸브/플랜지/기존 서포트 등 비-Pipe 부재) 위치.
        List<Point3d> GetObstacles(IEnumerable<ObjectId> selectedPipes, Transaction tr);
    }
}
