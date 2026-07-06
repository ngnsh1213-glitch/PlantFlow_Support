using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlantFlow_Support
{
    public static class AnnotationUtils
    {
        public static void CreateHorizontalDimension(
          Extents3d extent,
          Matrix3d transform,
          ObjectId dimstyle_id,
          double dim_line_point,
          out RotatedDimension dimension)
        {
          Point3d maxPoint = extent.MaxPoint;
          Point3d minPoint = extent.MinPoint;
          Point3d point3d2 = new Point3d(minPoint.X, maxPoint.Y, 0.0);
          Point3d point3d3 = new Point3d(maxPoint.X, dim_line_point + 50.0, 0.0);
          dimension = new RotatedDimension(0.0, maxPoint, point3d2, point3d3, "", dimstyle_id);
          ((Entity) dimension).TransformBy(transform);
          ((Entity) dimension).Layer = "AUTO_DIM";
        }

        public static RotatedDimension CreateHorizontalDimension(
          Point3d point1,
          Point3d point2,
          Point3d point3,
          Matrix3d transform,
          ObjectId dimstyle_id)
        {
          RotatedDimension horizontalDimension = new RotatedDimension(0.0, point1, point2, point3, "", dimstyle_id);
          ((Entity) horizontalDimension).TransformBy(transform);
          ((Entity) horizontalDimension).Layer = "AUTO_DIM";
          return horizontalDimension;
        }

        public static void CreateVerticalDimension(
          Extents3d extent,
          Matrix3d transform,
          ObjectId dimstyle_id,
          double dim_line_point,
          out RotatedDimension dimension)
        {
          Point3d maxPoint = extent.MaxPoint;
          Point3d minPoint = extent.MinPoint;
          Point3d point3d1 = new Point3d(maxPoint.X, minPoint.Y, 0.0);
          Point3d point3d3 = new Point3d(Math.Max(maxPoint.X, point3d1.X) + 50.0, maxPoint.Y, 0.0);
          dimension = new RotatedDimension(Math.PI / 2.0, maxPoint, point3d1, point3d3, "", dimstyle_id);
          ((Entity) dimension).TransformBy(transform);
          ((Entity) dimension).Layer = "AUTO_DIM";
        }

        public static RotatedDimension CreateVerticalDimension(
          Point3d point1,
          Point3d point2,
          Point3d point3,
          Matrix3d transform,
          ObjectId dimstyle_id)
        {
          RotatedDimension verticalDimension = new RotatedDimension(Math.PI / 2.0, point1, point2, point3, "", dimstyle_id);
          ((Entity) verticalDimension).TransformBy(transform);
          ((Entity) verticalDimension).Layer = "AUTO_DIM";
          return verticalDimension;
        }

        public static List<Point3d> AttaDPointArrangement(
            Extents3d frame_extents,
            List<Extents3d> atta_block_exts,
            Matrix3d ucs,
            out double dim_line_point,
            out double dim_line_point_vertical,
            out List<Point3d> points_vertical)
        {
            // Logic from PSUtil (simplified reconstruction)
            List<Point3d> source1 = new List<Point3d>();
            source1.Add(new Point3d(frame_extents.MinPoint.X, frame_extents.MaxPoint.Y, 0.0));
            source1.Add(new Point3d(frame_extents.MaxPoint.X, frame_extents.MaxPoint.Y, 0.0));

            points_vertical = new List<Point3d>();
            points_vertical.Add(new Point3d(frame_extents.MaxPoint.X, frame_extents.MinPoint.Y, 0.0));
            points_vertical.Add(new Point3d(frame_extents.MaxPoint.X, frame_extents.MaxPoint.Y, 0.0));

            foreach (Extents3d attaBlockExt in atta_block_exts)
            {
                Extents3d transformedExt = attaBlockExt;
                transformedExt.TransformBy(ucs); // Note: Extents transformation might be approximation if rotated. PSUtil did TransformBy directly.

                double num1 = Math.Abs(transformedExt.MaxPoint.X - transformedExt.MinPoint.X);
                double num2 = Math.Abs(transformedExt.MaxPoint.Y - transformedExt.MinPoint.Y);

                double maxY = Math.Max(Math.Abs(transformedExt.MaxPoint.Y), Math.Abs(transformedExt.MinPoint.Y));
                double maxX = Math.Max(Math.Abs(transformedExt.MaxPoint.X), Math.Abs(transformedExt.MinPoint.X));

                // Center points logic from PSUtil
                source1.Add(new Point3d(transformedExt.MaxPoint.X - num1 / 2.0, maxY, 0.0));
                points_vertical.Add(new Point3d(maxX, transformedExt.MaxPoint.Y - num2 / 2.0, 0.0));
            }

            source1.Sort((x, y) => x.X.CompareTo(y.X));
            dim_line_point = source1.Max(pos => pos.Y);
            points_vertical.Sort((x, y) => x.Y.CompareTo(y.Y));
            dim_line_point_vertical = points_vertical.Max(pos => pos.X);

            return source1;
        }

        public static List<RotatedDimension> CreateAttaDimension(
          List<Point3d> dpoints,
          double dim_line_point,
          Matrix3d ucs,
          ObjectId dimstyle_id)
        {
          List<RotatedDimension> attaDimension = new List<RotatedDimension>();
          for (int index = 1; index < dpoints.Count; ++index)
          {
            Point3d dpoint1 = dpoints[index - 1];
            Point3d dpoint2 = dpoints[index];
            Point3d point3 = new Point3d(dpoint1.X, dim_line_point + 50.0, 0.0);
            RotatedDimension horizontalDimension = CreateHorizontalDimension(dpoint1, dpoint2, point3, ucs, dimstyle_id);
            attaDimension.Add(horizontalDimension);
          }
          return attaDimension;
        }

        public static List<RotatedDimension> CreateAttaVerticalDimension(
          List<Point3d> dpoints,
          double dim_line_point,
          Matrix3d ucs,
          ObjectId dimstyle_id)
        {
          List<RotatedDimension> verticalDimension1 = new List<RotatedDimension>();
          for (int index = 1; index < dpoints.Count; ++index)
          {
            Point3d dpoint1 = dpoints[index - 1];
            Point3d dpoint2 = dpoints[index];
            Point3d point3 = new Point3d(dim_line_point + 50.0, dpoint1.Y, 0.0);
            RotatedDimension verticalDimension2 = CreateVerticalDimension(dpoint1, dpoint2, point3, ucs, dimstyle_id);
            verticalDimension1.Add(verticalDimension2);
          }
          return verticalDimension1;
        }

        public static MLeader CreateMLeader(
          Transaction trans,
          ObjectId block_content_id,
          ObjectId ml_style_id,
          Point3d[] tagging_points,
          string tag,
          Matrix3d ucs)
        {
          MLeader mleader = new MLeader();
          ((Entity) mleader).SetDatabaseDefaults();
          mleader.MLeaderStyle = ml_style_id;
          mleader.ContentType = (ContentType) 1;
          mleader.BlockContentId = block_content_id;
          mleader.BlockPosition = tagging_points[1];
          mleader.AddLeaderLine(tagging_points[0]);
          mleader.EnableDogleg = true;
          mleader.DoglegLength = 5.0;
          BlockTableRecord blockTableRecord = trans.GetObject(mleader.BlockContentId, (OpenMode) 0) as BlockTableRecord;
          Point3d blockPosition = mleader.BlockPosition;
          Matrix3d matrix3d = Matrix3d.Displacement(blockPosition.GetAsVector());
          foreach (ObjectId objectId in blockTableRecord)
          {
            AttributeDefinition attributeDefinition = trans.GetObject(objectId, (OpenMode) 0) as AttributeDefinition;
            if (attributeDefinition != null)
            {
              AttributeReference attributeReference1 = new AttributeReference();
              attributeReference1.SetAttributeFromBlock(attributeDefinition, matrix3d);
              AttributeReference attributeReference2 = attributeReference1;
              Point3d position = ((DBText) attributeDefinition).Position;
              Point3d point3d = position.TransformBy(matrix3d);
              ((DBText) attributeReference2).Position = point3d;
              ((DBText) attributeReference1).TextString = tag;
              mleader.SetBlockAttribute(objectId, attributeReference1);
            }
          }
          ((Entity) mleader).TransformBy(ucs);
          ((Entity) mleader).Layer = "AUTO_DIM";
          return mleader;
        }

        public static MLeader CreateMLeader(
          Point3d[] tagging_points,
          MText content,
          ObjectId ml_style_id,
          Matrix3d ucs)
        {
          MLeader mleader = new MLeader();
          ((Entity) mleader).SetDatabaseDefaults();
          mleader.MLeaderStyle = ml_style_id;
          mleader.AddFirstVertex(mleader.AddLeaderLine(tagging_points[1]), tagging_points[0]);
          mleader.EnableDogleg = true;
          mleader.DoglegLength = 5.0;
          mleader.MText = content;
          mleader.TextAlignmentType = (TextAlignmentType) 2;
          ((Entity) mleader).TransformBy(ucs);
          ((Entity) mleader).Layer = "AUTO_DIM";
          return mleader;
        }

        public static ObjectId GetArrowHeadObjectId(string var_type, string arrowhead_name, Transaction trans, Database db)
        {
            ObjectId arrowHeadObjectId = ObjectId.Null;
            string systemVariable = Application.GetSystemVariable(var_type) as string;
            Application.SetSystemVariable(var_type, (object)arrowhead_name);
            if (!string.IsNullOrEmpty(systemVariable))
                Application.SetSystemVariable(var_type, (object)systemVariable);
            
            // Note: 'using (trans)' is removed as transaction management should be external
            arrowHeadObjectId = ((SymbolTable)(trans.GetObject(db.BlockTableId, (OpenMode)0) as BlockTable))[arrowhead_name];
            // trans.Commit() removed
            return arrowHeadObjectId;
        }

        public static List<AttributeDefinition> CreateQuarterBlock(
          ObjectId datum_sym_id,
          Point3d[] line_starts,
          Point3d[] line_ends,
          ObjectId textstyle_id,
          Point3d[] text_position,
          string[] text_strings,
          ref BlockTableRecord datum_quarter)
        {
          BlockReference blockReference = new BlockReference(Point3d.Origin, datum_sym_id);
          datum_quarter.AppendEntity((Entity) blockReference);
          for (int index = 0; index < 6; ++index)
          {
            Line line = new Line(line_starts[index], line_ends[index]);
            ((Entity) line).Color = Color.FromColorIndex((ColorMethod) 195, (short) 1);
            datum_quarter.AppendEntity((Entity) line);
          }
          DBText dbText = new DBText();
          dbText.Position = new Point3d(2.02, -5.0, 0.0);
          dbText.TextString = "(P)";
          dbText.Height = 2.0;
          dbText.WidthFactor = 0.85;
          dbText.TextStyleId = textstyle_id;
          ((Entity) dbText).Color = Color.FromColorIndex((ColorMethod) 195, (short) 2);
          datum_quarter.AppendEntity((Entity) dbText);
          string[] strArray = new string[2]{ "X", "Y" };
          double[] numArray = new double[2] { 0.0, Math.PI / 2.0 };
          List<AttributeDefinition> quarterBlock = new List<AttributeDefinition>();
          for (int index = 0; index < 2; ++index)
          {
            string textString = text_strings[index];
            string str = strArray[index];
            Point3d point3d = text_position[index];
            double num = numArray[index];
            AttributeDefinition attributeDefinition = new AttributeDefinition(point3d, textString, str, str, textstyle_id);
            ((DBText) attributeDefinition).Justify = (AttachmentPoint) 5;
            ((DBText) attributeDefinition).AlignmentPoint = point3d;
            ((DBText) attributeDefinition).Height = 3.0;
            ((DBText) attributeDefinition).WidthFactor = 0.85;
            ((DBText) attributeDefinition).Rotation = num;
            ((Entity) attributeDefinition).Color = Color.FromColorIndex((ColorMethod) 195, (short) 3);
            datum_quarter.AppendEntity((Entity) attributeDefinition);
            quarterBlock.Add(attributeDefinition);
          }
          return quarterBlock;
        }
    }
}
