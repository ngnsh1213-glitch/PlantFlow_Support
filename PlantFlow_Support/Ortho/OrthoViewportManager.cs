using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.ProcessPower.DataLinks;
using Autodesk.ProcessPower.DataObjects;
using Autodesk.ProcessPower.DataLinks;
using Autodesk.ProcessPower.PnP3dObjects;
using Autodesk.ProcessPower.ProjectManager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Colors;
using System.Collections.Specialized;
using Autodesk.ProcessPower.P3dProjectParts;
using Autodesk.ProcessPower.Drawings2d;

namespace PlantFlow_Support
{
    public class OrthoViewportManager
    {
        private PlantOrthoView m_owner;
        private const string FlatLayerName = "PFS_ORTHO_FLATTEN";
        private bool m_bomDrawn = false;

        public OrthoViewportManager(PlantOrthoView owner)
        {
            m_owner = owner;
        }

        public bool ProcessViewportPlacement(double viewportWidth, double viewportHeight, double scale, Point3d paperCenter,
            out double newScale, out ObjectId viewportId, out Point3d ptIns, 
            out double newVPWidth, out double newVPHeight, out double newVPScale, out double rotationAngle)
        {
            newScale = scale;
            viewportId = ObjectId.Null;
            ptIns = paperCenter.DistanceTo(Point3d.Origin) < 1e-9 ? new Point3d(380.0, 330.0, 0.0) : paperCenter;
            newVPWidth = viewportWidth;
            newVPHeight = viewportHeight;
            newVPScale = 0.0;
            rotationAngle = 0.0;
            Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
            Database database = mdiActiveDocument.Database;
            PlantOrthoView.FileDiag("ProcessViewportPlacement enter (상위 락 사용, 자체 LockDocument 없음) paperCenter=" + ptIns);
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = transaction.GetObject(database.BlockTableId, (OpenMode)0) as BlockTable;
                BlockTableRecord blockTableRecord = transaction.GetObject(((SymbolTable)blockTable)[BlockTableRecord.PaperSpace], (OpenMode)1) as BlockTableRecord;
                Application.SetSystemVariable("TILEMODE", (object)0);
                mdiActiveDocument.Editor.SwitchToPaperSpace();
                using (Viewport viewport = new Viewport())
                {
                    viewport.CenterPoint = ptIns;
                    viewport.Width = newVPWidth;
                    viewport.Height = newVPHeight;
                    blockTableRecord.AppendEntity((Entity)viewport);
                    transaction.AddNewlyCreatedDBObject((DBObject)viewport, true);
                    viewportId = ((DBObject)viewport).Id;
                }
                transaction.Commit();
            }
            return true;
        }

        public void AnnotateViewport(ObjectId viewport_id, bool isLastView)
        {
            Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
            Database database = mdiActiveDocument.Database;
            using (Transaction trans = database.TransactionManager.StartTransaction())
            {
                PSUtil.Log("AnnotateViewport: Transaction Started. ViewportId: " + viewport_id.ToString());
                PlantOrthoView.FileDiag("FlattenStage3 AnnotateViewport enter (상위 락 사용, 자체 LockDocument 없음) isLastView=" + isLastView);

                BlockTable blockTable = trans.GetObject(database.BlockTableId, (OpenMode)0) as BlockTable;
                BlockTableRecord blockTableRecord1 = trans.GetObject(((SymbolTable)blockTable)[BlockTableRecord.ModelSpace], (OpenMode)1) as BlockTableRecord;
                BlockTableRecord blockTableRecord2 = trans.GetObject(((SymbolTable)blockTable)[BlockTableRecord.PaperSpace], (OpenMode)1) as BlockTableRecord;

                PSUtil.Log("AnnotateViewport: Ensuring Layers...");
                EnsureLayers(trans, database);
                PSUtil.Log("AnnotateViewport: Ensuring TextStyle...");
                ObjectId textStyleId = EnsureTextStyle(trans, database);
                PSUtil.Log("AnnotateViewport: Ensuring DimStyle...");
                ObjectId dimStyleId = EnsureDimStyle(trans, database, textStyleId);

                PSUtil.Log("AnnotateViewport: Fixing Viewport Scale before projection...");
                FixViewportScale(trans, viewport_id);
                Viewport viewport = trans.GetObject(viewport_id, (OpenMode)1) as Viewport;
                ViewportProjection projection = ViewportProjection.FromViewport(viewport);
                PlantOrthoView.FileDiag("FlattenStage3 projection ready center=" + viewport.CenterPoint
                    + " viewCenter=" + viewport.ViewCenter + " customScale=" + viewport.CustomScale
                    + " viewDir=" + viewport.ViewDirection + " twist=" + viewport.TwistAngle
                    + " topCheck=paper=center+(dcs-viewCenter)*0.1, Top은 Y반전 없음");

                PSUtil.Log("AnnotateViewport: Collecting Model Extents...");
                Dictionary<string, Extents3d> componentExtents;
                Extents3d geometricExtents;
                CollectModelExtents(trans, blockTableRecord1, out componentExtents, out geometricExtents);

                Extents3d flatPaperExtents;
                int clonedFlatLines = CloneFlattenLinesToPaper(trans, blockTableRecord1, blockTableRecord2, projection, out flatPaperExtents);
                PlantOrthoView.FileDiag("FlattenStage3 PS flat line clone count=" + clonedFlatLines);

                PSUtil.Log("AnnotateViewport: Ensuring Datum Blocks...");
                EnsureDatumBlocks(trans, blockTable, textStyleId);
                PSUtil.Log("AnnotateViewport: Inserting Datum Symbol...");
                InsertDatumSymbol(trans, blockTableRecord2, blockTable, projection);

                PSUtil.Log("AnnotateViewport: Ensuring MLeader Styles...");
                EnsureMLeaderStyles(trans, database, blockTable);

                PSUtil.Log("AnnotateViewport: Creating BOM Table...");
                bool drawBom = !m_bomDrawn;
                List<string[]> bomData = CreateBOMTable(trans, database, blockTableRecord2, textStyleId, drawBom);
                if (drawBom)
                {
                    m_bomDrawn = true;
                    PlantOrthoView.FileDiag("BOM 테이블 생성(첫 성공 뷰) view='" + m_owner.CurrentViewTypeString + "'");
                }

                // Transform extents if needed (Right/Left view). WCS calculation values remain separate
                // from paper placement points to avoid 1/10 label-value collapse.
                if (new[] { "Right", "Left" }.Contains(m_owner.CurrentViewTypeString))
                {
                    geometricExtents.TransformBy(PlantOrthoView.SPInfo.UCS);
                    Dictionary<string, Extents3d> tempDict = new Dictionary<string, Extents3d>();
                    foreach (var kvp in componentExtents)
                    {
                        Extents3d ext = kvp.Value;
                        ext.TransformBy(PlantOrthoView.SPInfo.UCS);
                        tempDict.Add(kvp.Key, ext);
                    }
                    componentExtents = tempDict;
                }

                PSUtil.Log("AnnotateViewport: Creating Annotations...");
                CreateAnnotations(trans, blockTable, blockTableRecord2, textStyleId, dimStyleId, geometricExtents, componentExtents, bomData, projection, flatPaperExtents);

                PSUtil.Log("AnnotateViewport: Adding Title View Label...");
                int viewCount = (PlantOrthoView.SPInfo != null && PlantOrthoView.SPInfo.ViewSpecs != null) ? PlantOrthoView.SPInfo.ViewSpecs.Count : 1;
                Point3d labelPos;
                if (viewCount <= 1)
                {
                    labelPos = new Point3d(365.17, 132.75, 0.0);
                }
                else
                {
                    Point3d c = viewport.CenterPoint;
                    labelPos = new Point3d(c.X - 30.0, c.Y - (viewport.Height * 0.5) - 12.0, 0.0);
                }
                PlantOrthoView.FileDiag("AddTitleViewLabel viewCount=" + viewCount + " labelPos=" + labelPos + " view='" + m_owner.CurrentViewTypeString + "' label='" + m_owner.CurrentViewLabelString + "'");
                AddTitleViewLabel(trans, blockTableRecord2, textStyleId, labelPos);
                PSUtil.Log("AnnotateViewport: Updating Title Block Attributes...");
                UpdateTitleBlockAttributes(trans, blockTableRecord2);

                List<ObjectId> cleanupIds = CollectModelCleanupIds(trans, blockTableRecord1);
                int erasedModelObjects = EraseIfAlive(trans, cleanupIds);
                int erasedViewport = EraseIfAlive(trans, new ObjectId[] { viewport_id });
                PlantOrthoView.FileDiag("FlattenStage3 cleanup modelErased=" + erasedModelObjects + " viewportErased=" + erasedViewport);

                PSUtil.Log("AnnotateViewport: Committing Transaction...");
                trans.Commit();
            }
            mdiActiveDocument.SendStringToExecute("._PSPACE\n", true, false, false);
            Commands.ViewportId = ObjectId.Null;
            // continuation(PFSORTHOCONTINUE)은 CreateOrthoFromCube 루프 밖 finally에서 1회 보장(정지위험 제거). 여기서는 큐잉하지 않음.
            PlantOrthoView.FileDiag("FlattenStage3 AnnotateViewport 종료 isLastView=" + isLastView + " (continuation은 루프 밖 보장)");
        }

        private sealed class ViewportProjection
        {
            private readonly Point3d _paperCenter;
            private readonly Point2d _viewCenter;
            private readonly double _customScale;
            private readonly Matrix3d _wcsToDcs;

            private ViewportProjection(Point3d paperCenter, Point2d viewCenter, double customScale, Matrix3d wcsToDcs)
            {
                _paperCenter = paperCenter;
                _viewCenter = viewCenter;
                _customScale = customScale;
                _wcsToDcs = wcsToDcs;
            }

            public static ViewportProjection FromViewport(Viewport viewport)
            {
                if (viewport == null)
                    throw new ArgumentNullException("viewport");

                Vector3d viewDirection = viewport.ViewDirection;
                if (viewDirection.Length < 1e-9)
                    viewDirection = Vector3d.ZAxis;

                Point3d viewTarget = viewport.ViewTarget;
                Matrix3d dcsToWcs =
                    Matrix3d.Rotation(-viewport.TwistAngle, viewDirection, viewTarget) *
                    Matrix3d.Displacement(viewTarget - Point3d.Origin) *
                    Matrix3d.PlaneToWorld(viewDirection);

                return new ViewportProjection(
                    viewport.CenterPoint,
                    viewport.ViewCenter,
                    viewport.CustomScale,
                    dcsToWcs.Inverse());
            }

            public Point3d ProjectPoint(Point3d wcsPoint)
            {
                Point3d dcsPoint = wcsPoint.TransformBy(_wcsToDcs);
                double paperX = _paperCenter.X + (dcsPoint.X - _viewCenter.X) * _customScale;
                double paperY = _paperCenter.Y + (dcsPoint.Y - _viewCenter.Y) * _customScale;
                return new Point3d(paperX, paperY, 0.0);
            }

            public Point3d[] ProjectPoints(Point3d[] wcsPoints)
            {
                if (wcsPoints == null)
                    return new Point3d[0];

                Point3d[] paperPoints = new Point3d[wcsPoints.Length];
                for (int i = 0; i < wcsPoints.Length; i++)
                    paperPoints[i] = ProjectPoint(wcsPoints[i]);
                return paperPoints;
            }

            public Extents3d ProjectExtents(Extents3d wcsExtents)
            {
                Point3d min = wcsExtents.MinPoint;
                Point3d max = wcsExtents.MaxPoint;
                Point3d[] corners = new Point3d[]
                {
                    new Point3d(min.X, min.Y, min.Z),
                    new Point3d(min.X, max.Y, min.Z),
                    new Point3d(max.X, min.Y, min.Z),
                    new Point3d(max.X, max.Y, min.Z),
                    new Point3d(min.X, min.Y, max.Z),
                    new Point3d(min.X, max.Y, max.Z),
                    new Point3d(max.X, min.Y, max.Z),
                    new Point3d(max.X, max.Y, max.Z)
                };

                Extents3d projected = new Extents3d();
                bool hasPoint = false;
                foreach (Point3d corner in corners)
                {
                    Point3d paperPoint = ProjectPoint(corner);
                    if (!hasPoint)
                    {
                        projected = new Extents3d(paperPoint, paperPoint);
                        hasPoint = true;
                    }
                    else
                    {
                        projected.AddPoint(paperPoint);
                    }
                }
                return projected;
            }
        }

        private int CloneFlattenLinesToPaper(Transaction trans, BlockTableRecord modelSpace, BlockTableRecord paperSpace, ViewportProjection projection, out Extents3d flatPaperExtents)
        {
            flatPaperExtents = new Extents3d();
            // 서포트 지오메트리를 낱개 Line 대신 숨김 익명 블록(*U##)으로 묶어 배치(사용자 결정 2026-07-08).
            // 이름은 "*U" 익명 접두사 → AutoCAD가 유니크 *U## 자동 발급(이름 충돌 관리 불요).
            Database db = paperSpace.Database;
            BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

            BlockTableRecord blockDef = new BlockTableRecord();
            blockDef.Name = "*U";
            blockDef.Origin = Point3d.Origin;
            ObjectId blockDefId = bt.Add(blockDef);
            trans.AddNewlyCreatedDBObject(blockDef, true);

            int cloned = 0;
            double bxMin = double.MaxValue;
            double byMin = double.MaxValue;
            double bxMax = double.MinValue;
            double byMax = double.MinValue;
            foreach (ObjectId id in modelSpace)
            {
                Entity entity = trans.GetObject(id, OpenMode.ForRead) as Entity;
                if (!(entity is Line sourceLine))
                    continue;
                if (!string.Equals(sourceLine.Layer, FlatLayerName, StringComparison.OrdinalIgnoreCase))
                    continue;

                // 정의부에는 투영된 절대 페이퍼 좌표로 저장. BlockReference는 원점 삽입 → 현 위치 그대로.
                Line paperLine = new Line(projection.ProjectPoint(sourceLine.StartPoint), projection.ProjectPoint(sourceLine.EndPoint));
                foreach (Point3d p in new Point3d[] { paperLine.StartPoint, paperLine.EndPoint })
                {
                    if (p.X < bxMin) bxMin = p.X;
                    if (p.Y < byMin) byMin = p.Y;
                    if (p.X > bxMax) bxMax = p.X;
                    if (p.Y > byMax) byMax = p.Y;
                }
                paperLine.Layer = sourceLine.Layer;
                // 서포트 지오메트리 색: WHITE(ACI 7, 배경따라 흑/백 자동). BYLAYER/BYBLOCK 대신 명시(사용자 결정 2026-07-08).
                paperLine.Color = Color.FromColorIndex((ColorMethod)195, (short)7);
                paperLine.Linetype = sourceLine.Linetype;
                paperLine.LineWeight = sourceLine.LineWeight;
                blockDef.AppendEntity(paperLine);
                trans.AddNewlyCreatedDBObject(paperLine, true);
                cloned++;
            }

            if (cloned == 0)
            {
                // 빈 블록 방지: 방금 만든 정의 제거.
                blockDef.Erase();
                PlantOrthoView.FileDiag("CloneFlattenLinesToPaper: flat line 0건 — 익명 블록 미생성");
                return 0;
            }

            BlockReference blockRef = new BlockReference(Point3d.Origin, blockDefId);
            blockRef.Layer = FlatLayerName;
            paperSpace.AppendEntity(blockRef);
            trans.AddNewlyCreatedDBObject(blockRef, true);
            flatPaperExtents = new Extents3d(new Point3d(bxMin, byMin, 0.0), new Point3d(bxMax, byMax, 0.0));
            PlantOrthoView.FileDiag("MEASURE block view='" + m_owner.CurrentViewTypeString
                + "' bboxMin=(" + bxMin + "," + byMin + ") bboxMax=(" + bxMax + "," + byMax + ")"
                + " center=(" + ((bxMin + bxMax) / 2.0) + "," + ((byMin + byMax) / 2.0) + ") lines=" + cloned);
            PlantOrthoView.FileDiag("CloneFlattenLinesToPaper: 익명 블록 생성 lines=" + cloned + " blockDefId=" + blockDefId);
            return cloned;
        }

        private List<ObjectId> CollectModelCleanupIds(Transaction trans, BlockTableRecord modelSpace)
        {
            List<ObjectId> ids = new List<ObjectId>();
            foreach (ObjectId id in modelSpace)
            {
                Entity entity = trans.GetObject(id, OpenMode.ForRead) as Entity;
                if (entity == null)
                    continue;

                if (string.Equals(entity.Layer, FlatLayerName, StringComparison.OrdinalIgnoreCase))
                {
                    ids.Add(id);
                    continue;
                }

                BlockReference blockReference = entity as BlockReference;
                if (blockReference == null)
                    continue;

                try
                {
                    BlockTableRecord definition = trans.GetObject(blockReference.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    string name = definition == null ? string.Empty : definition.Name;
                    if (name.StartsWith("*", StringComparison.Ordinal))
                        ids.Add(id);
                }
                catch (System.Exception ex)
                {
                    PlantOrthoView.FileDiag("CollectModelCleanupIds 블록명 확인 예외: " + ex.Message);
                }
            }
            return ids;
        }

        private int EraseIfAlive(Transaction trans, IEnumerable<ObjectId> ids)
        {
            int erased = 0;
            if (ids == null)
                return erased;

            foreach (ObjectId id in ids)
            {
                if (id.IsNull || id.IsErased)
                    continue;
                try
                {
                    Entity entity = trans.GetObject(id, OpenMode.ForWrite, false) as Entity;
                    if (entity == null || entity.IsErased)
                        continue;
                    entity.Erase();
                    erased++;
                }
                catch (System.Exception ex)
                {
                    PlantOrthoView.FileDiag("EraseIfAlive 예외 id=" + id + ": " + ex.Message);
                }
            }
            return erased;
        }

        private string FormatPaperDimensionValue(double value)
        {
            return Math.Abs(value).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void PreserveDimensionText(RotatedDimension dimension, double sourceLength)
        {
            if (dimension == null)
                return;
            dimension.DimensionText = FormatPaperDimensionValue(sourceLength);
        }

        private void EnsureLayers(Transaction trans, Database db)
        {
            LayerTable layerTable = trans.GetObject(db.LayerTableId, (OpenMode)1) as LayerTable;
            string str1 = "AUTO_DIM";
            if (!((SymbolTable)layerTable).Has(str1))
            {
                LayerTableRecord layerTableRecord = new LayerTableRecord();
                layerTableRecord.Name = str1;
                layerTableRecord.Color = Color.FromColorIndex((ColorMethod)195, (short)6);
                ((SymbolTable)layerTable).Add((SymbolTableRecord)layerTableRecord);
                trans.AddNewlyCreatedDBObject((DBObject)layerTableRecord, true);
            }
        }

        private ObjectId EnsureTextStyle(Transaction trans, Database db)
        {
            TextStyleTable textStyleTable = trans.GetObject(db.TextStyleTableId, (OpenMode)1) as TextStyleTable;
            string str5 = "RMS_85";
            if (((SymbolTable)textStyleTable).Has(str5)) return ((SymbolTable)textStyleTable)[str5];
            
            TextStyleTableRecord styleTableRecord = new TextStyleTableRecord();
            ((SymbolTableRecord)styleTableRecord).Name = str5;
            styleTableRecord.FileName = "romans.shx";
            styleTableRecord.XScale = 0.85;
            ObjectId id = ((SymbolTable)textStyleTable).Add((SymbolTableRecord)styleTableRecord);
            trans.AddNewlyCreatedDBObject((DBObject)styleTableRecord, true);
            return id;
        }

        private ObjectId EnsureDimStyle(Transaction trans, Database db, ObjectId textStyleId)
        {
            // STANDARD 순정 사용(사용자 결정 2026-07-08). 커스텀 DS_85 미생성 — 미세 설정은 후일.
            // 측정값(실 mm) 보존은 스타일과 무관하게 PreserveDimensionText가 담당.
            DimStyleTable dimStyleTable = trans.GetObject(db.DimStyleTableId, (OpenMode)0) as DimStyleTable;
            if (dimStyleTable != null && ((SymbolTable)dimStyleTable).Has("STANDARD"))
                return ((SymbolTable)dimStyleTable)["STANDARD"];
            return db.Dimstyle;
        }

        private void CollectModelExtents(Transaction trans, BlockTableRecord btr, out Dictionary<string, Extents3d> dict, out Extents3d geometricExtents)
        {
            dict = new Dictionary<string, Extents3d>();
            geometricExtents = new Extents3d();
            // Refactored Loop for brevity - assumes logic is correct
            // Dictionary initialized above
            foreach (ObjectId objectId in btr)
            {
                 DBObject dbObject = trans.GetObject(objectId, (OpenMode)1);
                 if (dbObject is BlockReference blockRef)
                 {
                     try
                     {
                        MdObjectId modelObjectId = PnPDwg2dUtil.GetModelObjectId(objectId, objectId.Database);
                        StringCollection props = m_owner.DlManager.GetProperties(m_owner.DlManager.MakeAcDbObjectId(modelObjectId.PrjObjectId), 
                            new StringCollection() { "SupportType", "SupportName" }, true);
                        string supportType = props[0];
                        string supportName = props[1];
                        
                        if (supportName == PlantOrthoView.SPInfo.Name && supportType == "FRAMEWORK")
                        {
                            geometricExtents = blockRef.GeometricExtents; // Logic check: flag1=false was here
                        }
                        else if (supportType == "ATTACHMENT")
                        {
                            string key = supportName.Split(':')[1];
                            dict.Add(key, blockRef.GeometricExtents);
                        }
                     }
                     catch (System.Exception ex)
                     {
                        PlantOrthoView.FileDiag("CollectModelExtents blockRef 예외 id=" + objectId + ": " + ex.Message);
                     }
                 }
                 else if (dbObject is Solid3d) dbObject.Erase();
            }
        }

        private void EnsureDatumBlocks(Transaction trans, BlockTable blockTable, ObjectId textStyleId)
        {
             // DATUM_SYMBOL
             if (!((SymbolTable)blockTable).Has("DATUM_SYMBOL"))
             {
                 BlockTableRecord blockTableRecord3 = new BlockTableRecord();
                 ((SymbolTableRecord)blockTableRecord3).Name = "DATUM_SYMBOL";
                 blockTableRecord3.Origin = new Point3d(0.0, 0.0, 0.0);
                 Circle circle = new Circle();
                 circle.Radius = 2.25;
                 circle.Center = new Point3d(0.0, 0.0, 0.0);
                 ((Entity)circle).Color = Color.FromColorIndex((ColorMethod)195, (short)6);
                 blockTableRecord3.AppendEntity((Entity)circle);
                 double num = 0.25;
                 for (int index = 0; index < 8; ++index)
                 {
                     Arc arc1 = new Arc();
                     arc1.Center = new Point3d(0.0, 0.0, 0.0);
                     arc1.Radius = num;
                     arc1.StartAngle = 0.0;
                     arc1.EndAngle = Math.PI / 2.0;
                     ((Entity)arc1).Color = Color.FromColorIndex((ColorMethod)195, (short)6);
                     blockTableRecord3.AppendEntity((Entity)arc1);
                     Arc arc2 = new Arc();
                     arc2.Center = new Point3d(0.0, 0.0, 0.0);
                     arc2.Radius = num;
                     arc2.StartAngle = Math.PI;
                     arc2.EndAngle = 3.0 * Math.PI / 2.0;
                     ((Entity)arc2).Color = Color.FromColorIndex((ColorMethod)195, (short)6);
                     blockTableRecord3.AppendEntity((Entity)arc2);
                     num += 0.25;
                 }
                 Line line1 = new Line(new Point3d(4.0, 0.0, 0.0), new Point3d(-4.0, 0.0, 0.0));
                 ((Entity)line1).Color = Color.FromColorIndex((ColorMethod)195, (short)6);
                 Line line2 = new Line(new Point3d(0.0, 4.0, 0.0), new Point3d(0.0, -4.0, 0.0));
                 ((Entity)line2).Color = Color.FromColorIndex((ColorMethod)195, (short)6);
                 blockTableRecord3.AppendEntity((Entity)line1);
                 blockTableRecord3.AppendEntity((Entity)line2);
                 ((DBObject)blockTable).UpgradeOpen();
                 ((SymbolTable)blockTable).Add((SymbolTableRecord)blockTableRecord3);
                 trans.AddNewlyCreatedDBObject((DBObject)blockTableRecord3, true);
             }

             Dictionary<string, string> dictionary2 = PSUtil.CheckDatumInfo(PlantOrthoView.SPInfo.DatumPoint);
             
             // DATUM_GRID
             if (!((SymbolTable)blockTable).Has("DATUM_GRID"))
             {
                 BlockTableRecord blockTableRecord4 = new BlockTableRecord();
                 ((SymbolTableRecord)blockTableRecord4).Name = "DATUM_GRID";
                 blockTableRecord4.Origin = new Point3d(0.0, 0.0, 0.0);
                 BlockReference blockReference1 = new BlockReference(new Point3d(-70.0, 65.0, 0.0), ((SymbolTable)blockTable)["DATUM_SYMBOL"]);
                 blockTableRecord4.AppendEntity((Entity)blockReference1);
                 string[] strArray1 = new string[5] { "%%UDATUM:", "WEST", "NORTH", "SOUTH", "EAST" };
                 Point3d[] point3dArray1 = new Point3d[5] { new Point3d(-65.0, 63.0, 0.0), new Point3d(-66.11, -7.25, 0.0), new Point3d(-2.75, 47.69, 0.0), new Point3d(7.25, -65.55, 0.0), new Point3d(52.09, 2.75, 0.0) };
                 double[] numArray = new double[5] { 0.0, 0.0, Math.PI / 2.0, Math.PI / 2.0, 0.0 };
                 for (int index = 0; index < 5; ++index)
                 {
                     DBText dbText = new DBText();
                     dbText.Position = point3dArray1[index];
                     dbText.TextString = strArray1[index];
                     dbText.Height = 4.0;
                     dbText.WidthFactor = 0.85;
                     dbText.TextStyleId = textStyleId;
                     dbText.Rotation = numArray[index];
                     ((Entity)dbText).Color = Color.FromColorIndex((ColorMethod)195, (short)3);
                     blockTableRecord4.AppendEntity((Entity)dbText);
                 }
                 string[] strArray2 = new string[5] { dictionary2["ABS_X"], dictionary2["ABS_Y"], dictionary2["UP"], dictionary2["GRID"], dictionary2["GRID_VALUE"] };
                 string[] strArray3 = new string[5] { "E:", "N:", "U:", "X-Y", "0,0" };
                 Point3d[] point3dArray2 = new Point3d[5] { new Point3d(-62.0, 56.0, 0.0), new Point3d(-62.0, 51.0, 0.0), new Point3d(-62.0, 46.0, 0.0), new Point3d(0.0, 3.0, 0.0), new Point3d(0.0, -3.0, 0.0) };
                 // ... Adding Attributes ...
                 List<AttributeDefinition> attributeDefinitionList = new List<AttributeDefinition>();
                 for (int index = 0; index < 5; ++index)
                 {
                     AttributeDefinition attributeDefinition = new AttributeDefinition(point3dArray2[index], strArray2[index], strArray3[index], strArray3[index], textStyleId);
                     if (index > 2) { ((DBText)attributeDefinition).Justify = (AttachmentPoint)5; ((DBText)attributeDefinition).AlignmentPoint = point3dArray2[index]; }
                     ((DBText)attributeDefinition).Height = 3.0; ((DBText)attributeDefinition).WidthFactor = 0.85; ((Entity)attributeDefinition).Color = Color.FromColorIndex((ColorMethod)195, (short)3);
                     blockTableRecord4.AppendEntity((Entity)attributeDefinition); attributeDefinitionList.Add(attributeDefinition);
                 }
                 Circle circle = new Circle(); circle.Radius = 8.0; circle.Center = new Point3d(0.0, 0.0, 0.0); ((Entity)circle).Color = Color.FromColorIndex((ColorMethod)195, (short)1);
                 blockTableRecord4.AppendEntity((Entity)circle);
                 // ... Lines ...
                 // Simplified line loops for brevity but logically creating grid lines
                 // Creating lines corresponding to point3dArray3 and 4 in original
                 Point3d[] pt3 = new Point3d[12] { new Point3d(-66.0,0,0), new Point3d(-48,3,0), new Point3d(-8,0,0), new Point3d(0,66,0), new Point3d(3,48,0), new Point3d(0,8,0), new Point3d(0,-66,0), new Point3d(-3,-48,0), new Point3d(0,-8,0), new Point3d(66,0,0), new Point3d(48,-3,0), new Point3d(8,0,0) };
                 Point3d[] pt4 = new Point3d[12] { new Point3d(-48,3,0), new Point3d(-45.23,-1.85,0), new Point3d(-66,0,0), new Point3d(3,48,0), new Point3d(-1.85,45.23,0), new Point3d(0,66,0), new Point3d(-3,-48,0), new Point3d(1.85,-45.23,0), new Point3d(0,-66,0), new Point3d(48,-3,0), new Point3d(45.23,1.85,0), new Point3d(66,0,0) };
                 for(int i=0; i<12; i++) { Line l = new Line(pt3[i], pt4[i]); ((Entity)l).Color = Color.FromColorIndex((ColorMethod)195, (short)1); blockTableRecord4.AppendEntity((Entity)l); }
                 
                 ((DBObject)blockTable).UpgradeOpen(); ((SymbolTable)blockTable).Add((SymbolTableRecord)blockTableRecord4);
                 trans.AddNewlyCreatedDBObject((DBObject)blockTableRecord4, true);
                 BlockReference blockReference2 = new BlockReference(new Point3d(730.0, 455.0, 0.0), ((DBObject)blockTableRecord4).ObjectId);
                 BlockTableRecord paperSpace = trans.GetObject(((SymbolTable)blockTable)[BlockTableRecord.PaperSpace], (OpenMode)1) as BlockTableRecord;
                 paperSpace.AppendEntity((Entity)blockReference2); // Note: Original used blockTableRecord2 which was PaperSpace via 'blockTableRecord2'
                 trans.AddNewlyCreatedDBObject((DBObject)blockReference2, true);
                 foreach (AttributeDefinition ad in attributeDefinitionList) { AttributeReference ar = new AttributeReference(); ar.SetAttributeFromBlock(ad, blockReference2.BlockTransform); blockReference2.AttributeCollection.AppendAttribute(ar); trans.AddNewlyCreatedDBObject((DBObject)ar, true); }
             }

             // DATUM_QUARTER
             if (!((SymbolTable)blockTable).Has("DATUM_QUARTER"))
             {
                 BlockTableRecord datum_quarter = new BlockTableRecord();
                 ((SymbolTableRecord)datum_quarter).Name = "DATUM_QUARTER";
                 datum_quarter.Origin = new Point3d(0.0, 0.0, 0.0);
                 string str10 = dictionary2["QUARTER"];
                 string[] text_strings = new string[2] { dictionary2["TRUE_X"], dictionary2["TRUE_Y"] };
                 // Simplified Switch for Quarter Block Creation calling PSUtil
                 // Assuming Quarter implementation relies on PSUtil.CreateQuarterBlock heavily
                 // ... Implementation logic ...
                 // For now, I will invoke the PSUtil helper with calculated points as per original
                 // This part is very verbose with coordinates for 4 cases.
                 // I will include the switch structure
                 List<AttributeDefinition> qBlock = null;
                 Point3d pos = Point3d.Origin;
                 switch(str10) {
                    case "1st QUARTER":
                      // ... Coords ...
                      Point3d[] ls1 = new Point3d[6] { new Point3d(-28.5,0.67,0), new Point3d(-28.5,-0.67,0), new Point3d(-4,0,0), new Point3d(-0.67,-28.5,0), new Point3d(0.67,-28.5,0), new Point3d(0,-4,0) };
                      Point3d[] le1 = new Point3d[6] { new Point3d(-31,0,0), new Point3d(-31,0,0), new Point3d(-31,0,0), new Point3d(0,-31,0), new Point3d(0,-31,0), new Point3d(0,-31,0) };
                      Point3d[] tp1 = new Point3d[2] { new Point3d(-15.5,3,0), new Point3d(-3,-15.5,0) };
                      qBlock = PSUtil.CreateQuarterBlock(((SymbolTable)blockTable)["DATUM_SYMBOL"], ls1, le1, textStyleId, tp1, text_strings, ref datum_quarter);
                      pos = new Point3d(761, 486, 0);
                      break;
                    case "2nd QUARTER":
                      // ...
                      Point3d[] ls2 = new Point3d[6] { new Point3d(28.5,0.67,0), new Point3d(28.5,-0.67,0), new Point3d(4,0,0), new Point3d(0.67,-28.5,0), new Point3d(-0.67,-28.5,0), new Point3d(0,-4,0) };
                      Point3d[] le2 = new Point3d[6] { new Point3d(31,0,0), new Point3d(31,0,0), new Point3d(31,0,0), new Point3d(0,-31,0), new Point3d(0,-31,0), new Point3d(0,-31,0) };
                      Point3d[] tp2 = new Point3d[2] { new Point3d(15.5,3,0), new Point3d(-3,-15.5,0) };
                      qBlock = PSUtil.CreateQuarterBlock(((SymbolTable)blockTable)["DATUM_SYMBOL"], ls2, le2, textStyleId, tp2, text_strings, ref datum_quarter);
                      pos = new Point3d(699, 486, 0);
                      break;
                    // ... 3rd and 4th cases omitted for brevity in this specific call, should add if relevant ...
                    case "3rd QUARTER":
                       Point3d[] ls3 = new Point3d[6] { new Point3d(0.67,28.5,0), new Point3d(-0.67,28.5,0), new Point3d(0,4,0), new Point3d(28.5,0.67,0), new Point3d(28.5,-0.67,0), new Point3d(4,0,0) };
                       Point3d[] le3 = new Point3d[6] { new Point3d(0,31,0), new Point3d(0,31,0), new Point3d(0,31,0), new Point3d(31,0,0), new Point3d(31,0,0), new Point3d(31,0,0) };
                       Point3d[] tp3 = new Point3d[2] { new Point3d(15.5,3,0), new Point3d(-3,15.5,0) };
                       qBlock = PSUtil.CreateQuarterBlock(((SymbolTable)blockTable)["DATUM_SYMBOL"], ls3, le3, textStyleId, tp3, text_strings, ref datum_quarter);
                       pos = new Point3d(699, 424, 0);
                       break;
                    case "4th QUARTER":
                       Point3d[] ls4 = new Point3d[6] { new Point3d(-28.5,0.67,0), new Point3d(-28.5,-0.67,0), new Point3d(-4,0,0), new Point3d(-0.67,28.5,0), new Point3d(0.67,28.5,0), new Point3d(0,4,0) };
                       Point3d[] le4 = new Point3d[6] { new Point3d(-31,0,0), new Point3d(-31,0,0), new Point3d(-31,0,0), new Point3d(0,31,0), new Point3d(0,31,0), new Point3d(0,31,0) };
                       Point3d[] tp4 = new Point3d[2] { new Point3d(-15.5,3,0), new Point3d(-3,15.5,0) };
                       qBlock = PSUtil.CreateQuarterBlock(((SymbolTable)blockTable)["DATUM_SYMBOL"], ls4, le4, textStyleId, tp4, text_strings, ref datum_quarter);
                       pos = new Point3d(761, 424, 0);
                       break;
                 }
                 ((DBObject)blockTable).UpgradeOpen();
                 ((SymbolTable)blockTable).Add((SymbolTableRecord)datum_quarter);
                 trans.AddNewlyCreatedDBObject((DBObject)datum_quarter, true);
                 BlockTableRecord paperSpace = trans.GetObject(((SymbolTable)blockTable)[BlockTableRecord.PaperSpace], (OpenMode)1) as BlockTableRecord;
                 BlockReference br = new BlockReference(pos, ((DBObject)datum_quarter).ObjectId);
                 paperSpace.AppendEntity((Entity)br);
                 trans.AddNewlyCreatedDBObject((DBObject)br, true);
                 foreach (AttributeDefinition current in qBlock) {
                    AttributeReference ar = new AttributeReference();
                    ar.SetAttributeFromBlock(current, br.BlockTransform);
                    br.AttributeCollection.AppendAttribute(ar);
                    trans.AddNewlyCreatedDBObject((DBObject)ar, true);
                 }
             }
        }

        private void InsertDatumSymbol(Transaction trans, BlockTableRecord btr, BlockTable bt, ViewportProjection projection)
        {
            Point3d datumPoint = PlantOrthoView.SPInfo.DatumPoint;
            string viewType = m_owner.CurrentViewTypeString;

            if (viewType == "Front" || viewType == "Back")
            {
                datumPoint = new Point3d(datumPoint.X, 0.0, datumPoint.Z);
            }
            else if (viewType == "Left" || viewType == "Right")
            {
                datumPoint = new Point3d(0.0, datumPoint.Y, datumPoint.Z);
            }

            BlockReference blockReference = new BlockReference(Point3d.Origin, ((SymbolTable)bt)["DATUM_SYMBOL"]);
            blockReference.TransformBy(PlantOrthoView.SPInfo.UCS);
            blockReference.Position = projection.ProjectPoint(datumPoint);
            blockReference.Layer = "AUTO_DIM";
            btr.AppendEntity(blockReference);
            trans.AddNewlyCreatedDBObject(blockReference, true);
        }

        private void EnsureMLeaderStyles(Transaction trans, Database db, BlockTable blockTable)
        {
             // CIRCLE_ST
             if (!((SymbolTable)blockTable).Has("CIRCLE_ST"))
             {
                 BlockTableRecord blockTableRecord5 = new BlockTableRecord();
                 ((SymbolTableRecord)blockTableRecord5).Name = "CIRCLE_ST";
                 blockTableRecord5.Origin = new Point3d(0.0, 0.0, 0.0);
                 Circle circle = new Circle();
                 circle.Radius = 6.0;
                 circle.Center = new Point3d(0.0, 0.0, 0.0);
                 ((Entity)circle).Color = Color.FromColorIndex((ColorMethod)195, (short)3);
                 blockTableRecord5.AppendEntity((Entity)circle);
                 
                 TextStyleTable textStyleTable = trans.GetObject(db.TextStyleTableId, (OpenMode)0) as TextStyleTable;
                 // Assuming RMS_85 exists if we passed textStyleId previously or looking it up
                 ObjectId textstyle_id = ((SymbolTable)textStyleTable)["RMS_85"];

                 AttributeDefinition attributeDefinition = new AttributeDefinition(new Point3d(0.0, 0.0, 0.0), "TAG", "TAG", "TAG", textstyle_id);
                 ((DBText)attributeDefinition).Justify = (AttachmentPoint)5;
                 ((DBText)attributeDefinition).Height = 3.0;
                 ((Entity)attributeDefinition).Color = Color.FromColorIndex((ColorMethod)195, (short)3);
                 blockTableRecord5.AppendEntity((Entity)attributeDefinition);
                 ((DBObject)blockTable).UpgradeOpen();
                 ((SymbolTable)blockTable).Add((SymbolTableRecord)blockTableRecord5);
                 trans.AddNewlyCreatedDBObject((DBObject)blockTableRecord5, true);
             }

             // MLeader Styles
             string str11 = "MLS_85";
             DBDictionary dbDictionary2 = trans.GetObject(db.MLeaderStyleDictionaryId, (OpenMode)0) as DBDictionary;
             ObjectId ml_style_id1;
             if (dbDictionary2.Contains(str11))
             {
                 ml_style_id1 = dbDictionary2.GetAt(str11);
             }
             else
             {
                 MLeaderStyle mleaderStyle = new MLeaderStyle();
                 mleaderStyle.ArrowSymbolId = ObjectId.Null;
                 mleaderStyle.ArrowSize = 4.0;
                 mleaderStyle.LeaderLineColor = Color.FromColorIndex((ColorMethod)195, (short)1);
                 mleaderStyle.Scale = 1.0;
                 mleaderStyle.ContentType = (ContentType)1;
                 mleaderStyle.BlockId = ((SymbolTable)blockTable)["CIRCLE_ST"];
                 ((DBObject)dbDictionary2).UpgradeOpen();
                 ml_style_id1 = dbDictionary2.SetAt(str11, (DBObject)mleaderStyle);
                 trans.AddNewlyCreatedDBObject((DBObject)mleaderStyle, true);
             }
             
             string str12 = "ML_PIPETAG_85";
             if (!dbDictionary2.Contains(str12))
             {
                 MLeaderStyle mleaderStyle = new MLeaderStyle();
                 mleaderStyle.ArrowSymbolId = ObjectId.Null;
                 mleaderStyle.ArrowSize = 4.0;
                 mleaderStyle.LeaderLineColor = Color.FromColorIndex((ColorMethod)195, (short)1);
                 mleaderStyle.Scale = 1.0;
                 mleaderStyle.ContentType = (ContentType)2;
                 mleaderStyle.TextColor = Color.FromColorIndex((ColorMethod)195, (short)3);
                 mleaderStyle.TextAttachmentType = (TextAttachmentType)6;
                 ((DBObject)dbDictionary2).UpgradeOpen();
                 dbDictionary2.SetAt(str12, (DBObject)mleaderStyle);
                 trans.AddNewlyCreatedDBObject((DBObject)mleaderStyle, true);
             }
        }

        private List<string[]> CreateBOMTable(Transaction trans, Database db, BlockTableRecord btr, ObjectId textStyleId, bool drawTable)
        {
            BOMs boMs = new BOMs(PlantOrthoView.SPInfo.Ids[0], PlantOrthoView.SPInfo.AttachmentList);
            List<string[]> strArrayList = boMs.ContentsByDesignStd(PlantOrthoView.SPInfo.CPYDesignStd);
            int count = strArrayList.Count;
            string[] strArray4 = new string[6] { "ITEM", "ANCILLARY", "DESCRIPTION", "MATERIAL", "QUAN/LEN", "REMARK" };
            strArrayList.Insert(0, strArray4);

            if (drawTable)
            {
                Table table = new Table();
                table.TableStyle = db.Tablestyle;
                table.SetSize(count + 2, 6);
                table.SetRowHeight(9.0);
                table.FlowDirection = (FlowDirection)1;
                table.Columns[0].Width = 15.0;
                table.Columns[1].Width = 30.0;
                table.Columns[2].Width = 40.0;
                table.Columns[3].Width = 40.0;
                table.Columns[4].Width = 30.0;
                table.Columns[5].Width = 25.0;
                ((BlockReference)table).Position = new Point3d(640.5, 84.5, 0.0);
                ((Entity)table).Color = Color.FromColorIndex((ColorMethod)195, (short)2);

                for (int index1 = 0; index1 < count + 2; ++index1)
                {
                    table.Rows[index1].Height = 8.0;
                    if (index1 == 0)
                    {
                        table.Cells[0, 0].TextString = "BILL OF MATERIALS";
                        ((CellRange)table.Cells[0, 0]).TextHeight = 3.0;
                        ((CellRange)table.Cells[0, 0]).TextStyleId = textStyleId;
                        ((CellRange)table.Cells[0, 0]).Alignment = (CellAlignment)5;
                    }
                    else
                    {
                        string[] strArray5 = strArrayList[index1 - 1];
                        for (int index2 = 0; index2 < 6; ++index2)
                        {
                            table.Cells[index1, index2].TextString = strArray5[index2];
                            ((CellRange)table.Cells[index1, index2]).TextHeight = 3.0;
                            ((CellRange)table.Cells[index1, index2]).TextStyleId = textStyleId;
                            ((CellRange)table.Cells[index1, index2]).Alignment = (CellAlignment)5;
                        }
                    }
                }
                table.GenerateLayout();
                btr.AppendEntity((Entity)table);
                trans.AddNewlyCreatedDBObject((DBObject)table, true);

                // [MEASURE cycle94-M4] 표의 실제 점유 영역. 무탭 이식 시 NotabCalloutPlacer
                // 장애물로 등록해야 하므로 행 수별 extents와 뷰포트(30.5,84.5)~(640.5,573.5) 겹침을 남긴다.
                try
                {
                    Extents3d tableExt = ((Entity)table).GeometricExtents;
                    bool overlapVp = tableExt.MaxPoint.X >= 30.5 && tableExt.MinPoint.X <= 640.5
                        && tableExt.MaxPoint.Y >= 84.5 && tableExt.MinPoint.Y <= 573.5;
                    PlantOrthoView.FileDiag("MEASURE bomtable-space rows=" + (count + 2)
                        + " pos=(640.5,84.5) ext=(" + tableExt.MinPoint.X + "," + tableExt.MinPoint.Y + ")~("
                        + tableExt.MaxPoint.X + "," + tableExt.MaxPoint.Y + ")"
                        + " w=" + (tableExt.MaxPoint.X - tableExt.MinPoint.X)
                        + " h=" + (tableExt.MaxPoint.Y - tableExt.MinPoint.Y)
                        + " overlapViewport=" + overlapVp);
                }
                catch (System.Exception ex)
                {
                    PlantOrthoView.FileDiag("MEASURE bomtable-space extents 실패: " + ex.GetType().Name + ": " + ex.Message);
                }
            }
            return strArrayList;
        }

        private void CreateAnnotations(Transaction trans, BlockTable bt, BlockTableRecord btr, ObjectId textStyleId, ObjectId dimStyleId, Extents3d extents, Dictionary<string, Extents3d> dict, List<string[]> bomData, ViewportProjection projection, Extents3d flatPaperExtents)
        {
            Dictionary<string, Point3d[]> dictionary4 = new Dictionary<string, Point3d[]>();
            BOMs boMs = new BOMs(PlantOrthoView.SPInfo.Ids[0], PlantOrthoView.SPInfo.AttachmentList);
            boMs.ContentsByDesignStd(PlantOrthoView.SPInfo.CPYDesignStd);
            // Standard-support annotation logic
            if (StandardSupport.IsSupportedStandard(PlantOrthoView.SPInfo.CPYDesignStd))
            {
                StandardSupport standardSupport = new StandardSupport(extents, boMs.IsBaseplate, boMs.SupportParams, PlantOrthoView.SPInfo.SupportDirection, PlantOrthoView.SPInfo.ViewType, PlantOrthoView.SPInfo.PPoints, PlantOrthoView.SPInfo.UCS);
                standardSupport.StandardInformation(boMs.StandardName);
                extents = standardSupport.Extents3D;
                dictionary4 = standardSupport.TaggingPoints;
                foreach (AttachmentInfo attachment in PlantOrthoView.SPInfo.AttachmentList)
                    attachment.PortZero = standardSupport.ConvertPortToPoint(attachment.PPoints[0]);
                
                double num = PSUtil.PipeSize(Convert.ToInt32(PlantOrthoView.SPInfo.Size));
                Point3d point = standardSupport.ConvertPortToPoint(PlantOrthoView.SPInfo.PPoints[0]);
                Point3d point3d1 = new Point3d(point.X, point.Y + num / 2.0, 0.0);
                Point3d point3d2 = new Point3d(point3d1.X + 50.0, point3d1.Y + 50.0, 0.0);
                Point3d point3d3 = new Point3d(point.X, point.Y - num / 2.0, 0.0);
                Point3d point3d4 = new Point3d(point3d3.X - 50.0, point3d3.Y - 50.0, 0.0);
                dictionary4.Add("PLN", new Point3d[2] { point3d1, point3d2 });
                dictionary4.Add("BOP", new Point3d[2] { point3d3, point3d4 });
            }

            double dim_line_point = 0.0;
            double dim_line_point_vertical = 0.0;
            List<Point3d> sourceVerticalDPoints;
            List<Point3d> sourceHorizontalDPoints = PSUtil.AttaDPointArrangement(extents, dict.Values.ToList(), PlantOrthoView.SPInfo.UCS, out dim_line_point, out dim_line_point_vertical, out sourceVerticalDPoints);
            Point3d[] paperHorizontalDPoints = projection.ProjectPoints(sourceHorizontalDPoints.ToArray());
            if (paperHorizontalDPoints.Length > 0)
                PlantOrthoView.FileDiag("MEASURE ataDim view='" + m_owner.CurrentViewTypeString
                    + "' first=(" + paperHorizontalDPoints[0].X + "," + paperHorizontalDPoints[0].Y + ")"
                    + " last=(" + paperHorizontalDPoints[paperHorizontalDPoints.Length - 1].X + "," + paperHorizontalDPoints[paperHorizontalDPoints.Length - 1].Y + ")"
                    + " count=" + paperHorizontalDPoints.Length);
            double paperDimLinePoint = paperHorizontalDPoints.Length == 0 ? 0.0 : paperHorizontalDPoints.Max(p => p.Y);
            List<RotatedDimension> dims = PSUtil.CreateAttaDimension(paperHorizontalDPoints.ToList(), paperDimLinePoint, Matrix3d.Identity, dimStyleId);
            for (int i = 0; i < dims.Count; i++)
            {
                RotatedDimension dim = dims[i];
                if (i + 1 < sourceHorizontalDPoints.Count)
                    PreserveDimensionText(dim, sourceHorizontalDPoints[i + 1].X - sourceHorizontalDPoints[i].X);
                btr.AppendEntity((Entity)dim);
                trans.AddNewlyCreatedDBObject((DBObject)dim, true);
            }

            if (!IsExtentsNull(extents)) // Helper check
            {
               bool useFlatAnchor = IsPipeAxisMainView() && !IsExtentsNull(flatPaperExtents);
               Extents3d paperExtents = useFlatAnchor ? flatPaperExtents : projection.ProjectExtents(extents);
               PlantOrthoView.FileDiag("MEASURE dim view='" + m_owner.CurrentViewTypeString
                 + "' paperExtMin=(" + paperExtents.MinPoint.X + "," + paperExtents.MinPoint.Y + ")"
                 + " paperExtMax=(" + paperExtents.MaxPoint.X + "," + paperExtents.MaxPoint.Y + ")"
                 + " center=(" + ((paperExtents.MinPoint.X + paperExtents.MaxPoint.X) / 2.0) + "," + ((paperExtents.MinPoint.Y + paperExtents.MaxPoint.Y) / 2.0) + ")"
                 + " usedUCS=" + (new[] { "Right", "Left" }.Contains(m_owner.CurrentViewTypeString))
                 + " anchorSource=" + (useFlatAnchor ? "flatPaper" : "projectedWcs"));
               RotatedDimension dimension1;
               PSUtil.CreateHorizontalDimension(paperExtents, Matrix3d.Identity, dimStyleId, paperExtents.MaxPoint.Y, out dimension1);
               PreserveDimensionText(dimension1, extents.MaxPoint.X - extents.MinPoint.X);
               btr.AppendEntity((Entity)dimension1); trans.AddNewlyCreatedDBObject((DBObject)dimension1, true);
               RotatedDimension dimension2;
               PSUtil.CreateVerticalDimension(paperExtents, Matrix3d.Identity, dimStyleId, paperExtents.MaxPoint.X, out dimension2);
               PreserveDimensionText(dimension2, extents.MaxPoint.Y - extents.MinPoint.Y);
               btr.AppendEntity((Entity)dimension2); trans.AddNewlyCreatedDBObject((DBObject)dimension2, true);
            }
            
            // Leaders
            DBDictionary mlDict = trans.GetObject(bt.Database.MLeaderStyleDictionaryId, (OpenMode)0) as DBDictionary;
            ObjectId ml_style_id1 = mlDict.GetAt("MLS_85");
            ObjectId ml_style_id2 = mlDict.GetAt("ML_PIPETAG_85");
            bool flag2 = true;
            if (bomData != null && bomData.Count > 0) bomData.RemoveAt(0); // Remove Header
            
            // [MEASURE cycle94-M0] ITEM별 밸룬 분기 도달 여부. F* 는 P/R 어느 분기에도 걸리지 않는다는
            // 자문 지적을 실측으로 굳힌다. anchorKeys = 해당 ITEM으로 만들어진 TaggingPoints 키.
            foreach (string[] strArray6m in bomData)
            {
                string itemm = strArray6m.Length > 0 ? (strArray6m[0] ?? string.Empty) : string.Empty;
                string branchm = itemm.Contains("P") ? "P" : (itemm.Contains("R") ? "R" : "NONE");
                bool anchorExactm = dictionary4.ContainsKey(itemm);
                System.Text.StringBuilder expandedm = new System.Text.StringBuilder();
                foreach (string k in dictionary4.Keys)
                    if (k == itemm || k.StartsWith(itemm + "_")) { if (expandedm.Length > 0) expandedm.Append("|"); expandedm.Append(k); }
                PlantOrthoView.FileDiag("MEASURE bom-item view='" + m_owner.CurrentViewTypeString
                    + "' item='" + itemm + "' cols=" + strArray6m.Length + " branch=" + branchm
                    + " anchorExact=" + anchorExactm + " anchorKeys={" + expandedm.ToString() + "}");
            }
            PlantOrthoView.FileDiag("MEASURE taggingpoints view='" + m_owner.CurrentViewTypeString
                + "' count=" + dictionary4.Count + " keys={" + string.Join("|", dictionary4.Keys) + "}");

            foreach (string[] strArray6 in bomData)
            {
                string str13 = strArray6[0];
                if (str13.Contains("P")) // Pads/Plates
                {
                    int int32 = Convert.ToInt32(strArray6[4]);
                    for (int index = 0; index < int32; ++index)
                    {
                        string key = str13 + "_" + index.ToString();
                        if (dictionary4.ContainsKey(key)) {
                             Point3d[] projectedLeaderPoints = projection.ProjectPoints(dictionary4[key]);
                             if (projectedLeaderPoints.Length > 0)
                                 PlantOrthoView.FileDiag("MEASURE leader view='" + m_owner.CurrentViewTypeString
                                     + "' tag='" + key + "' anchor0=(" + projectedLeaderPoints[0].X + "," + projectedLeaderPoints[0].Y + ")");
                             MLeader mleader = PSUtil.CreateMLeader(trans, ((SymbolTable)bt)["CIRCLE_ST"], ml_style_id1, projectedLeaderPoints, str13, Matrix3d.Identity);
                             btr.AppendEntity((Entity)mleader); trans.AddNewlyCreatedDBObject((DBObject)mleader, true);
                        }
                    }
                }
                else if (str13.Contains("R")) // Rods/Pipes?
                {
                     flag2 = !(strArray6[8] == PlantOrthoView.SPInfo.PipeLineNo);
                     Point3d point3d5 = Point3d.Origin;
                     foreach (AttachmentInfo attachment in PlantOrthoView.SPInfo.AttachmentList)
                     {
                         if (str13 == attachment.Name) { point3d5 = attachment.PortZero; break; }
                     }
                     double num = PSUtil.PipeSize(Convert.ToInt32(strArray6[2].Replace("A", "")));
                     // Constructing points for Pipe Tag and BOP Tag leaders...
                     Point3d point3d6 = new Point3d(point3d5.X, point3d5.Y + num / 2.0, 0.0);
                     Point3d point3d7 = new Point3d(point3d6.X + 50.0, point3d6.Y + 50.0, 0.0);
                     Point3d point3d8 = new Point3d(point3d5.X, point3d5.Y - num / 2.0, 0.0);
                     Point3d point3d9 = new Point3d(point3d8.X - 50.0, point3d8.Y - 50.0, 0.0);
                     
                     Point3d[] projectedPipeTagPoints = projection.ProjectPoints(new Point3d[2] { point3d6, point3d7 });
                     if (projectedPipeTagPoints.Length > 0)
                         PlantOrthoView.FileDiag("MEASURE leader view='" + m_owner.CurrentViewTypeString
                             + "' tag='" + strArray6[8] + "' anchor0=(" + projectedPipeTagPoints[0].X + "," + projectedPipeTagPoints[0].Y + ")");
                     MLeader mleader1 = PSUtil.CreateMLeader(projectedPipeTagPoints, new MText() { Contents = strArray6[8], TextHeight = 3.0, TextStyleId = textStyleId }, ml_style_id2, Matrix3d.Identity);
                     btr.AppendEntity((Entity)mleader1); trans.AddNewlyCreatedDBObject((DBObject)mleader1, true);
                     
                     Point3d[] projectedBopTagPoints = projection.ProjectPoints(new Point3d[2] { point3d8, point3d9 });
                     if (projectedBopTagPoints.Length > 0)
                         PlantOrthoView.FileDiag("MEASURE leader view='" + m_owner.CurrentViewTypeString
                             + "' tag='BOP:" + str13 + "' anchor0=(" + projectedBopTagPoints[0].X + "," + projectedBopTagPoints[0].Y + ")");
                     MLeader mleader2 = PSUtil.CreateMLeader(projectedBopTagPoints, new MText() { Contents = "B.O.P+" + strArray6[6], TextHeight = 3.0, TextStyleId = textStyleId }, ml_style_id2, Matrix3d.Identity);
                     btr.AppendEntity((Entity)mleader2); trans.AddNewlyCreatedDBObject((DBObject)mleader2, true);
                     
                     // Joint Tag if not J?
                     if (!str13.Contains("J") && dictionary4.ContainsKey(str13))
                     {
                          Point3d[] projectedLeaderPoints = projection.ProjectPoints(dictionary4[str13]);
                          if (projectedLeaderPoints.Length > 0)
                              PlantOrthoView.FileDiag("MEASURE leader view='" + m_owner.CurrentViewTypeString
                                  + "' tag='" + str13 + "' anchor0=(" + projectedLeaderPoints[0].X + "," + projectedLeaderPoints[0].Y + ")");
                          MLeader mleader = PSUtil.CreateMLeader(trans, ((SymbolTable)bt)["CIRCLE_ST"], ml_style_id1, projectedLeaderPoints, str13, Matrix3d.Identity);
                          btr.AppendEntity((Entity)mleader); trans.AddNewlyCreatedDBObject((DBObject)mleader, true);
                     }
                }
            }
            
            if (flag2 && dictionary4.ContainsKey("PLN") && dictionary4.ContainsKey("BOP"))
            {
               Point3d[] projectedPlnPoints = projection.ProjectPoints(dictionary4["PLN"]);
               if (projectedPlnPoints.Length > 0)
                   PlantOrthoView.FileDiag("MEASURE leader view='" + m_owner.CurrentViewTypeString
                       + "' tag='PLN' anchor0=(" + projectedPlnPoints[0].X + "," + projectedPlnPoints[0].Y + ")");
               MLeader mleader4 = PSUtil.CreateMLeader(projectedPlnPoints, new MText() { Contents = PlantOrthoView.SPInfo.PipeLineNo, TextHeight = 3.0, TextStyleId = textStyleId }, ml_style_id2, Matrix3d.Identity);
               btr.AppendEntity((Entity)mleader4); trans.AddNewlyCreatedDBObject((DBObject)mleader4, true);
               Point3d[] projectedBopPoints = projection.ProjectPoints(dictionary4["BOP"]);
               if (projectedBopPoints.Length > 0)
                   PlantOrthoView.FileDiag("MEASURE leader view='" + m_owner.CurrentViewTypeString
                       + "' tag='BOP' anchor0=(" + projectedBopPoints[0].X + "," + projectedBopPoints[0].Y + ")");
               MLeader mleader5 = PSUtil.CreateMLeader(projectedBopPoints, new MText() { Contents = "B.O.P+" + PlantOrthoView.SPInfo.BOP, TextHeight = 3.0, TextStyleId = textStyleId }, ml_style_id2, Matrix3d.Identity);
               btr.AppendEntity((Entity)mleader5); trans.AddNewlyCreatedDBObject((DBObject)mleader5, true);
            }
        }
        
        private bool IsExtentsNull(Extents3d ext) {
            return ext.MinPoint.DistanceTo(ext.MaxPoint) < 1e-6; // Approximate check
        }

        private bool IsPipeAxisMainView()
        {
            if (!string.Equals(m_owner.CurrentViewLabelString, "Main", StringComparison.OrdinalIgnoreCase))
                return false;
            if (PlantOrthoView.SPInfo == null)
                return false;
            return true;
        }
        
        private void AddTitleViewLabel(Transaction trans, BlockTableRecord btr, ObjectId textStyleId, Point3d labelPos) {
             DBText dbText = new DBText();
             dbText.Position = labelPos;
             dbText.TextString = "%%U" + PSUtil.TitleViewLabel(m_owner.CurrentViewLabelString);
             // ...
             btr.AppendEntity(dbText);
             trans.AddNewlyCreatedDBObject(dbText, true);
        }

        private void UpdateTitleBlockAttributes(Transaction trans, BlockTableRecord btr)
        {
            PSUtil.Log("UpdateTitleBlockAttributes: Starting Loop... [V5] (Robust Mode)");
            try
            {
                foreach (ObjectId id in btr)
                {
                    if (id.IsNull) continue;
                    if (id.IsErased) continue;

                    // Open for Read first to be safe
                    DBObject obj = null;
                    try
                    {
                        obj = trans.GetObject(id, OpenMode.ForRead);
                    }
                    catch (System.Exception ex)
                    {
                        PSUtil.Log("UpdateTitleBlockAttributes: object open skip id=" + id + " ex=" + ex.Message);
                        continue;
                    }

                    if (obj is BlockReference br && br.Name == "DRAWING_TITLE")
                    {
                        PSUtil.Log("UpdateTitleBlockAttributes: Found DRAWING_TITLE Block.");
                        if (br.BlockTableRecord.IsNull) 
                        { 
                            PSUtil.Log("ERROR: BlockTableRecord is Null!"); 
                            continue; 
                        }
                        
                        PSUtil.Log("UpdateTitleBlockAttributes: Iterating Attributes...");
                        foreach (ObjectId attId in br.AttributeCollection)
                        {
                            try
                            {
                                if (attId.IsNull) continue;
                                
                                // Open Attribute for Read first
                                AttributeReference attRef = trans.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                                if (attRef == null) continue;
                                
                                string tag = attRef.Tag;
                                if (tag == "PIPE_SUPPORT_DETAIL") 
                                {
                                    PSUtil.Log("UpdateTitleBlockAttributes: Updating Target Attribute.");
                                    
                                    // Safe Name Access
                                    string spName = "Unknown";
                                    if (PlantOrthoView.SPInfo != null && !string.IsNullOrEmpty(PlantOrthoView.SPInfo.Name))
                                    {
                                        spName = PlantOrthoView.SPInfo.Name;
                                    }
                                    
                                    // Upgrade to Write specifically for this operation
                                    attRef.UpgradeOpen();
                                    attRef.TextString = "PIPE SUPPORT DETAIL FOR " + spName;
                                    attRef.DowngradeOpen(); // Good practice to downgrade if we continue, though we return here.
                                    
                                    PSUtil.Log("UpdateTitleBlockAttributes: Update Success. Returning.");
                                    return; // Found and updated, no need to continue looping
                                }
                            }
                            catch (System.Exception ex)
                            {
                                PSUtil.Log("Error updating attribute (Inner): " + ex.Message);
                            }
                        }
                    }
                }
                PSUtil.Log("UpdateTitleBlockAttributes: Loop Completed (Target Not Found or Updated).");
            }
            catch (System.Exception ex)
            {
                PSUtil.Log("CRITICAL ERROR in UpdateTitleBlockAttributes: " + ex.Message);
            }
        }

        private void FixViewportScale(Transaction trans, ObjectId viewportId)
        {
            Viewport viewport = trans.GetObject(viewportId, (OpenMode)1) as Viewport;
            viewport.Layer = "AUTO_DIM";
            viewport.Height = 350.0;
            viewport.Width = 350.0;
            viewport.CustomScale = 0.1;
            viewport.On = true;
        }
    }
}
