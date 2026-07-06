using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Interop;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PlantFlow_Support
{
    public static class CadUtils
    {
        public static Document GetActiveDocument()
        {
            return Application.DocumentManager.MdiActiveDocument;
        }

        public static DocumentCollection GetDocumentCollection() => Application.DocumentManager;

        public static void ZoomToObject(Autodesk.AutoCAD.EditorInput.Editor ed, Entity ent)
        {
            Extents3d geometricExtents = ent.GeometricExtents;
            Matrix3d coordinateSystem = ed.CurrentUserCoordinateSystem;
            Matrix3d matrix3d = coordinateSystem.Inverse();
            geometricExtents.TransformBy(matrix3d);
            Point3d minPoint = geometricExtents.MinPoint;
            Point3d maxPoint = geometricExtents.MaxPoint;
            Point2d point2d1 = new Point2d(minPoint.X, minPoint.Y);
            Point2d point2d2 = new Point2d(maxPoint.X, maxPoint.Y);
            ViewTableRecord viewTableRecord = new ViewTableRecord();
            ((AbstractViewTableRecord)viewTableRecord).CenterPoint = point2d1 + (point2d2 - point2d1) / 2.0;
            viewTableRecord.Height = point2d2.Y - point2d1.Y;
            viewTableRecord.Width = point2d2.X - point2d1.X;
            ed.SetCurrentView(viewTableRecord);
        }

        public static void ZoomToObject(Autodesk.AutoCAD.EditorInput.Editor ed, Line line)
        {
            Extents3d geometricExtents = line.GeometricExtents;
            Matrix3d coordinateSystem = ed.CurrentUserCoordinateSystem;
            Matrix3d matrix3d = coordinateSystem.Inverse();
            geometricExtents.TransformBy(matrix3d);
            Point3d minPoint = geometricExtents.MinPoint;
            Point3d maxPoint = geometricExtents.MaxPoint;
            ((IAcadApplication)Application.AcadApplication).ZoomWindow((object)new double[3] { minPoint.X, minPoint.Y, minPoint.Z }, (object)new double[3] { maxPoint.X, maxPoint.Y, maxPoint.Z });
        }

        public static void LimitView(Dictionary<string, SupportInfo> supportSelection)
        {
            Document document = GetActiveDocument();
            if (document == null)
            {
                MessageBox.Show("No drawings are open, open project drawing and try again!", "Auto 2D", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
            Database database = document.Database;
            using (document.LockDocument())
            {
                using (Transaction transaction = database.TransactionManager.StartTransaction())
                {
                    // Need to iterate keys separately if we are modifying values?
                    // SupportSelection is Dict<string, SupportInfo>. SupportInfo is Class (Reference type). We modify property Extents.
                    foreach (KeyValuePair<string, SupportInfo> keyValuePair in supportSelection)
                    {
                        SupportInfo supportInfo = keyValuePair.Value;
                        // Checking if Ids exist...
                        if (supportInfo.Ids != null && supportInfo.Ids.Length > 0)
                        {
                            Entity ent = transaction.GetObject(supportInfo.Ids[0], (OpenMode)0) as Entity;
                            if (ent != null)
                            {
                                supportInfo.Extents = ent.GeometricExtents;
                            }
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        public static void LimitCube(ref SupportInfo sp_info)
        {
            Document document = GetActiveDocument();
            if (document == null)
            {
                MessageBox.Show("No drawings are open, open project drawing and try again!", "Auto 2D", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
            Database database = document.Database;
            ObjectId[] ids = sp_info.Ids;
            Point3d[] point3dArray = GeometryLimit(document, database, ids); // Pass doc/db
            
            using (document.LockDocument())
            {
                using (Transaction transaction = database.TransactionManager.StartTransaction())
                {
                    BlockTable blockTable = transaction.GetObject(database.BlockTableId, (OpenMode)0) as BlockTable;
                    BlockTableRecord blockTableRecord = transaction.GetObject(((SymbolTable)blockTable)[BlockTableRecord.ModelSpace], (OpenMode)1) as BlockTableRecord;
                    Solid3d solid3d1 = new Solid3d();
                    // point3dArray[0] contains dimensions (X, Y, Z lengths likely from GeometryLimit logic)
                    solid3d1.CreateBox(point3dArray[0].X, point3dArray[0].Y, point3dArray[0].Z);
                    blockTableRecord.AppendEntity((Entity)solid3d1);
                    transaction.AddNewlyCreatedDBObject((DBObject)solid3d1, true);
                    
                    // point3dArray[1] contains Center/Displacement?
                    // Checking Logic of GeometryLimit:
                    // It returns 2 points.
                    // Point 0: Dimensions (Width, Depth, Height)
                    // Point 1: Center Point
                    ((Entity)solid3d1).TransformBy(Matrix3d.Displacement(point3dArray[1] - Point3d.Origin));
                    sp_info.Extents = ((Entity)solid3d1).GeometricExtents;
                    ((DBObject)solid3d1).Erase();
                    transaction.Commit();
                }
            }
        }

        private static Point3d[] GeometryLimit(Document doc, Database database, ObjectId[] group_ids)
        {
            double num1 = 50.0; // Margin
            List<Point3d> source1 = new List<Point3d>();
            List<Point3d> source2 = new List<Point3d>();
            using (doc.LockDocument())
            {
                using (Transaction transaction = database.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId groupId in group_ids)
                    {
                        Extents3d geometricExtents = (transaction.GetObject(groupId, (OpenMode)0) as Entity).GeometricExtents;
                        source1.Add(geometricExtents.MaxPoint);
                        source2.Add(geometricExtents.MinPoint);
                    }
                    transaction.Commit();
                }
            }
            
            if (source1.Count == 0) return new Point3d[2]; // Safety

            double maxX = source1.Max(pos => pos.X) + num1;
            double maxY = source1.Max(pos => pos.Y) + num1;
            double maxZ = source1.Max(pos => pos.Z) + num1;
            Point3d pMax = new Point3d(maxX, maxY, maxZ);

            double minX = source2.Min(pos => pos.X) - num1;
            double minY = source2.Min(pos => pos.Y) - num1;
            double minZ = source2.Min(pos => pos.Z) - num1;
            Point3d pMin = new Point3d(minX, minY, minZ);

            double lenX = System.Math.Abs(maxX - minX);
            double lenY = System.Math.Abs(maxY - minY);
            double lenZ = System.Math.Abs(maxZ - minZ);
            Point3d dimensions = new Point3d(lenX, lenY, lenZ);

            Point3d center = new Point3d((pMin.X + pMax.X) / 2.0, (pMin.Y + pMax.Y) / 2.0, pMin.Z + lenZ / 2.0); // Z is likely bottom based + half height? Or center? 
            // Original: point3d2.Z + num10 / 2.0 (num10 is Z length). point3d2 is Min. So MinZ + Length/2 = CenterZ.
            
            return new Point3d[2] { dimensions, center };
        }
    }
}
