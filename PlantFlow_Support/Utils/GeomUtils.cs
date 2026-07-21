using Autodesk.AutoCAD.Geometry;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlantFlow_Support
{
    public static class GeomUtils
    {
        public static Dictionary<string, Vector3d> UpVectors()
        {
            return new Dictionary<string, Vector3d>()
            {
                { "Top", new Vector3d(0.0, 1.0, 0.0) },
                { "Bottom", new Vector3d(0.0, -1.0, 0.0) },
                { "Right", new Vector3d(0.0, 0.0, -1.0) },
                { "Left", new Vector3d(0.0, 0.0, -1.0) },
                { "Front", new Vector3d(0.0, 0.0, -1.0) },
                { "Back", new Vector3d(0.0, 0.0, -1.0) },
                { "NE", new Vector3d(0.0, 0.0, -1.0) }
            };
        }

        public static Dictionary<string, Vector3d> ViewDirection()
        {
            return new Dictionary<string, Vector3d>()
            {
                { "Top", new Vector3d(0.0, 0.0, 1.0) },
                { "Bottom", new Vector3d(0.0, 0.0, -1.0) },
                { "Right", new Vector3d(1.0, 0.0, 0.0) },
                { "Left", new Vector3d(-1.0, 0.0, 0.0) },
                { "Front", new Vector3d(0.0, -1.0, 0.0) },
                { "Back", new Vector3d(0.0, 1.0, 0.0) },
                { "NE", new Vector3d(1.0, 1.0, 1.0) }
            };
        }

        public static string TitleViewLabel(string view_type)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>()
            {
                { "Top", "PLAN VIEW" },
                { "Bottom", "LOOK FROM BOTTOM" },
                { "Right", "LOOK FROM EAST" },
                { "Left", "LOOK FROM WEST" },
                { "Front", "LOOK FROM SOUTH" },
                { "Back", "LOOK FROM NORTH" },
                { "NE", "ISO VIEW" }
            };
            return dict.ContainsKey(view_type) ? dict[view_type] : view_type;
        }

        public static Dictionary<string, Matrix3d> UCSs()
        {
             // Matrices constructed column-wise (or usually row-wise in constructor? ACAD Matrix3d constructor with double[] is Row-Major?)
             // Original code used `new double[16]` which implies implicit arrangement.
             // Verified original used explicit `new Matrix3d(double[])`.
             // I will duplicate exact matrices.
             Matrix3d mRight = new Matrix3d(new double[16] { 0,0,1,0, 1,0,0,0, 0,1,0,0, 0,0,0,1 });
             Matrix3d mLeft = new Matrix3d(new double[16] { 0,0,-1,0, -1,0,0,0, 0,1,0,0, 0,0,0,1 });
             Matrix3d mFront = new Matrix3d(new double[16] { 1,0,0,0, 0,0,-1,0, 0,1,0,0, 0,0,0,1 });
             Matrix3d mBack = new Matrix3d(new double[16] { -1,0,0,0, 0,0,1,0, 0,1,0,0, 0,0,0,1 });
             Matrix3d mTop = new Matrix3d(new double[16] { 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 });
             Matrix3d mBottom = new Matrix3d(new double[16] { -1,0,0,0, 0,1,0,0, 0,0,-1,0, 0,0,0,1 });
             Matrix3d mNE = mBottom; // Logic from original code seems to reuse matrix3d7 which is same as bottom? No, check original.
             // Original: NE -> matrix3d7.
             // matrix3d7: -1,0,0,0, 0,1,0,0, 0,0,-1,0 -> Same as Top inverted Z?
             // Let's explicitly define NE as in original (-1,0,0,0, 0,1,0,0, 0,0,-1,0).

             return new Dictionary<string, Matrix3d>()
             {
                 { "Top", mTop },
                 { "Bottom", mBottom },
                 { "Right", mRight },
                 { "Left", mLeft },
                 { "Front", mFront },
                 { "Back", mBack },
                 { "NE", mBottom } // Verified matrix3d7 == matrix3d6 (Bottom) in original snippet?
                 // matrix3d6: -1,0,0,0, 0,1,0,0, 0,0,-1,0
                 // matrix3d7: -1,0,0,0, 0,1,0,0, 0,0,-1,0
                 // Yes, they are identical in original provided code.
             };
        }

        public static double PipeSize(int norminal)
        {
            Dictionary<int, double> sizes = new Dictionary<int, double>()
            {
                {15, 21.3}, {20, 26.7}, {25, 33.4}, {40, 48.3}, {50, 60.3},
                {65, 73.0}, {80, 88.9}, {100, 114.3}, {125, 150.3}, {150, 168.3},
                {200, 219.1}, {250, 273.1}, {300, 323.9}, {350, 355.6}, {400, 406.4},
                {450, 457.2}, {500, 508.0}, {550, 558.8}, {600, 609.6}, {650, 660.0},
                {700, 711.0}, {750, 762.0}, {800, 813.0}, {850, 864.0}, {900, 914.0},
                {950, 965.0}, {1000, 1016.0}, {1050, 1066.8}, {1100, 1117.6},
                {1150, 1168.4}, {1250, 1271.4}, {1300, 1320.8}, {1350, 1371.8}
            };
            return sizes.ContainsKey(norminal) ? sizes[norminal] : 0.0;
        }

        public static string MetricImperial(string metric)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>()
            {
                {"15", "1/2\""}, {"20", "3/4\""}, {"25", "1\""}, {"32", "1 1/4\""},
                {"40", "1 1/2\""}, {"50", "2\""}, {"65", "2 1/2\""}, {"80", "3\""},
                {"100", "4\""}, {"125", "5\""}, {"150", "6\""}, {"200", "8\""},
                {"250", "10\""}, {"300", "12\""}, {"350", "14\""}, {"400", "16\""},
                {"450", "18\""}, {"500", "20\""}, {"550", "22\""}, {"600", "24\""},
                {"650", "26\""}, {"700", "28\""}, {"750", "30\""}, {"800", "32\""},
                {"850", "34\""}, {"900", "36\""}, {"950", "38\""}, {"1000", "40\""},
                {"1050", "42\""}, {"1100", "44\""}, {"1150", "46\""}, {"1250", "48\""},
                {"1300", "50\""}, {"1350", "52\""}
            };
            return dict.ContainsKey(metric) ? dict[metric] : metric;
        }

        public static List<Autodesk.AutoCAD.DatabaseServices.Line> GridLineSorting(List<Autodesk.AutoCAD.DatabaseServices.Line> list1, bool x_sort)
        {
            // Implementation from PSUtil
            List<Autodesk.AutoCAD.DatabaseServices.Line> source = new List<Autodesk.AutoCAD.DatabaseServices.Line>();
            foreach (Autodesk.AutoCAD.DatabaseServices.Line line1 in list1)
            {
                 double num1 = x_sort ? line1.StartPoint.X : line1.StartPoint.Y;
                 if (source.Count == 0) source.Add(line1);
                 else
                 {
                     double num3 = x_sort ? ((Autodesk.AutoCAD.DatabaseServices.Curve)source.Last()).StartPoint.X : ((Autodesk.AutoCAD.DatabaseServices.Curve)source.Last()).StartPoint.Y;
                     if (num1 > num3) source.Add(line1);
                     else
                     {
                         for (int i = 0; i < source.Count; ++i)
                         {
                             double num5 = x_sort ? ((Autodesk.AutoCAD.DatabaseServices.Curve)source[i]).StartPoint.X : ((Autodesk.AutoCAD.DatabaseServices.Curve)source[i]).StartPoint.Y;
                             if (num1 < num5) { source.Insert(i, line1); break; }
                         }
                     }
                 }
            }
            return source;
        }

        public static Dictionary<string, string> CheckDatumInfo(Point3d datum_point)
        {
            // 그리드 저장 위치는 활성 Plant 프로젝트 경로로 결정된다(레거시 경로는 1회 이관).
            JObject jobject1 = ProjectDataUtils.ReadJsonFile(ProjectDataUtils.ResolveGridSystemPathForRead());
            Point2d dPoint = new Point2d(datum_point.X, datum_point.Y);
            Point2d closestPoint = Point2d.Origin;
            double minDist = dPoint.GetDistanceTo(closestPoint);
            string gridName = "X-Y";

            foreach (KeyValuePair<string, JToken> keyValuePair in jobject1)
            {
                JObject jobject2 = keyValuePair.Value as JObject;
                Point2d iterPoint = new Point2d((double)jobject2["X"], (double)jobject2["Y"]);
                double dist = dPoint.GetDistanceTo(iterPoint);
                if (minDist >= dist)
                {
                    minDist = dist;
                    gridName = keyValuePair.Key;
                    closestPoint = iterPoint;
                }
            }

            string quarter = "1st QUARTER";
            if (datum_point.X >= closestPoint.X && datum_point.Y >= closestPoint.Y) quarter = "1st QUARTER";
            else if (datum_point.X < closestPoint.X && datum_point.Y > closestPoint.Y) quarter = "2nd QUARTER";
            else if (datum_point.X <= closestPoint.X && datum_point.Y <= closestPoint.Y) quarter = "3rd QUARTER";
            else if (datum_point.X > closestPoint.X && datum_point.Y < closestPoint.Y) quarter = "4th QUARTER";

            string ew = "", ns = "";
            Point3d origin = Point3d.Origin;
            if (datum_point.X >= origin.X && datum_point.Y >= origin.Y) { ew = "E: "; ns = "N: "; }
            else if (datum_point.X < origin.X && datum_point.Y > origin.Y) { ew = "W: "; ns = "N: "; }
            else if (datum_point.X <= origin.X && datum_point.Y <= origin.Y) { ew = "W: "; ns = "S: "; }
            else if (datum_point.X > origin.X && datum_point.Y < origin.Y) { ew = "E: "; ns = "S: "; }

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("QUARTER", quarter);
            dictionary.Add("ABS_X", ew + Math.Abs(Math.Round(datum_point.X)).ToString() + "mm");
            dictionary.Add("ABS_Y", ns + Math.Abs(Math.Round(datum_point.Y)).ToString() + "mm");
            dictionary.Add("UP", "U: " + Math.Abs(Math.Round(datum_point.Z)).ToString() + "mm");
            dictionary.Add("GRID", gridName);
            dictionary.Add("GRID_VALUE", Math.Abs(Math.Round(closestPoint.X)).ToString() + "," + Math.Abs(Math.Round(closestPoint.Y)).ToString());
            dictionary.Add("TRUE_X", Math.Round(Math.Abs(closestPoint.X - datum_point.X)).ToString() + "mm");
            dictionary.Add("TRUE_Y", Math.Round(Math.Abs(closestPoint.Y - datum_point.Y)).ToString() + "mm");
            return dictionary;
        }
    }
}
