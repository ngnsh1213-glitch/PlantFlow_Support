using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

#nullable disable

namespace PlantFlow_Support
{
    public partial class Commands
    {
        private sealed class GeometryCandidate : IDisposable
        {
            public string SourcePath { get; set; }
            public string Kind { get; set; }
            public string EntityType { get; set; }
            public string Handle { get; set; }
            public Matrix3d WorldTransform { get; set; } = Matrix3d.Identity;
            public Entity Entity { get; set; }
            public bool OwnsEntity { get; set; }

            public void Dispose()
            {
                if (OwnsEntity && Entity != null)
                {
                    Entity.Dispose();
                }
            }
        }

        [CommandMethod("PFSMESHSPIKE", CommandFlags.Session)]
        public void RunServerSideMeshSpike()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            var report = new StringBuilder();
            string outDir = ResolveSpikeOutputDirectory();
            Directory.CreateDirectory(outDir);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string reportPath = Path.Combine(outDir, "spike_result_serverside_mesh_iter2_" + stamp + ".md");
            string meshPath = Path.Combine(outDir, "mesh_spike_iter2_" + stamp + ".json");

            report.AppendLine("# PFS server-side mesh spike result - iteration 2");
            report.AppendLine();
            report.AppendLine("- command: `PFSMESHSPIKE`");
            report.AppendLine("- drawing: `" + EscapeMarkdown(db.Filename) + "`");
            report.AppendLine("- output_dir: `" + EscapeMarkdown(outDir) + "`");
            report.AppendLine("- unit_contract: AutoCAD/Plant3D mm = three.js 1 unit");
            report.AppendLine("- up_axis_contract: Z-up");
            report.AppendLine("- tolerance: SubDMesh reflection probe defaults; extents-box only when real extraction fails");
            report.AppendLine();

            PromptStringOptions opt = new PromptStringOptions("\nPFS mesh spike 대상 handle 입력: ");
            opt.AllowSpaces = false;
            PromptResult pr = ed.GetString(opt);
            if (pr.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(pr.StringResult))
            {
                ed.WriteMessage("\n[PFSMESHSPIKE] 취소됨.\n");
                return;
            }

            string handleText = pr.StringResult.Trim();
            var candidates = new List<GeometryCandidate>();
            var retained = new HashSet<DBObject>();
            int modelCountBefore = -1;
            int paperCountBefore = -1;
            int modelCountAfter = -1;
            int paperCountAfter = -1;
            string modifiedBefore = "unknown";
            string modifiedAfter = "unknown";

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    modelCountBefore = CountSpaceEntities(db, tr, BlockTableRecord.ModelSpace);
                    paperCountBefore = CountSpaceEntities(db, tr, BlockTableRecord.PaperSpace);

                    report.AppendLine("## 1. handle resolution");
                    if (!TryResolveHandle(db, handleText, out ObjectId targetId, out string resolveReason))
                    {
                        report.AppendLine("- status: FAIL");
                        report.AppendLine("- reason: " + resolveReason);
                        tr.Abort();
                        WriteSpikeReport(reportPath, report.ToString(), ed);
                        return;
                    }

                    DBObject target = tr.GetObject(targetId, OpenMode.ForRead, false);
                    if (target == null || target.IsErased)
                    {
                        report.AppendLine("- status: FAIL");
                        report.AppendLine("- reason: resolved object is null or erased");
                        tr.Abort();
                        WriteSpikeReport(reportPath, report.ToString(), ed);
                        return;
                    }

                    modifiedBefore = ReadModifiedFlag(target);
                    report.AppendLine("- status: OK");
                    report.AppendLine("- handle: `" + EscapeMarkdown(handleText) + "`");
                    report.AppendLine("- object_id: `" + targetId.ToString() + "`");
                    report.AppendLine("- real_type: `" + EscapeMarkdown(target.GetType().FullName) + "`");
                    report.AppendLine("- erased: `" + target.IsErased.ToString(CultureInfo.InvariantCulture) + "`");
                    report.AppendLine("- modified_before: `" + EscapeMarkdown(modifiedBefore) + "`");
                    report.AppendLine();

                    report.AppendLine("## 2. SupportHelper cross-check");
                    ProbeSupportHelper(targetId, report);
                    report.AppendLine();

                    report.AppendLine("## 3. entity classification and explode inventory");
                    ClassifyAndCollectGeometry(target, tr, Matrix3d.Identity, "root", candidates, retained, report);
                    report.AppendLine("- collected_geometry_count: `" + candidates.Count.ToString(CultureInfo.InvariantCulture) + "`");
                    AppendCandidateInventory(candidates, report);
                    report.AppendLine();

                    report.AppendLine("## 4. tessellation probes");
                    var mesh = new MeshCapture();
                    foreach (GeometryCandidate candidate in candidates)
                    {
                        report.AppendLine("### " + candidate.SourcePath);
                        report.AppendLine("- kind: `" + EscapeMarkdown(candidate.Kind) + "`");
                        report.AppendLine("- entity_type: `" + EscapeMarkdown(candidate.EntityType) + "`");
                        report.AppendLine("- handle: `" + EscapeMarkdown(candidate.Handle) + "`");
                        report.AppendLine("- owns_entity: `" + candidate.OwnsEntity.ToString(CultureInfo.InvariantCulture) + "`");
                        report.AppendLine("- world_transform_applied: `" + (!candidate.WorldTransform.IsEqualTo(Matrix3d.Identity)).ToString(CultureInfo.InvariantCulture) + "`");
                        report.AppendLine("- volume_probe: `" + EscapeMarkdown(ProbeVolume(candidate.Entity)) + "`");

                        bool hasRealMesh = TryGetObjectMesh(candidate, mesh, report);
                        if (!hasRealMesh)
                        {
                            hasRealMesh = TryCaptureExistingMesh(candidate, tr, mesh, report);
                        }
                        if (!hasRealMesh && candidate.Entity is Solid3d solid)
                        {
                            TryBrepProbe(solid, report);
                            hasRealMesh = TrySubDMeshCreateFromSolid(solid, mesh, report);
                            TryAcisOutProbe(solid, outDir, stamp, report);
                        }
                        if (!hasRealMesh)
                        {
                            CaptureExtentsBox(candidate, mesh, report);
                        }
                        report.AppendLine();
                    }

                    File.WriteAllText(meshPath, mesh.ToJson(), Encoding.UTF8);
                    report.AppendLine("## 5. mesh output");
                    report.AppendLine("- mesh_json: `" + EscapeMarkdown(meshPath) + "`");
                    report.AppendLine("- vertices: `" + mesh.VertexCount.ToString(CultureInfo.InvariantCulture) + "`");
                    report.AppendLine("- triangles: `" + mesh.TriangleCount.ToString(CultureInfo.InvariantCulture) + "`");
                    report.AppendLine("- real_mesh_triangles: `" + mesh.RealTriangleCount.ToString(CultureInfo.InvariantCulture) + "`");
                    report.AppendLine("- diagnostic_triangles: `" + mesh.DiagnosticTriangleCount.ToString(CultureInfo.InvariantCulture) + "`");
                    report.AppendLine();

                    modifiedAfter = ReadModifiedFlag(target);
                    modelCountAfter = CountSpaceEntities(db, tr, BlockTableRecord.ModelSpace);
                    paperCountAfter = CountSpaceEntities(db, tr, BlockTableRecord.PaperSpace);
                    tr.Abort();
                }
            }
            catch (System.Exception ex)
            {
                report.AppendLine("## exception");
                report.AppendLine("- type: `" + EscapeMarkdown(ex.GetType().FullName) + "`");
                report.AppendLine("- message: `" + EscapeMarkdown(ex.Message) + "`");
                report.AppendLine("- stack: `" + EscapeMarkdown(ex.StackTrace ?? "") + "`");
            }
            finally
            {
                foreach (GeometryCandidate candidate in candidates)
                {
                    candidate.Dispose();
                }
            }

            report.AppendLine("## 6. side-effect observations");
            report.AppendLine("- transaction: `Abort()`");
            report.AppendLine("- document_lock: acquired only inside command scope and disposed");
            report.AppendLine("- target_modified_before: `" + EscapeMarkdown(modifiedBefore) + "`");
            report.AppendLine("- target_modified_after: `" + EscapeMarkdown(modifiedAfter) + "`");
            report.AppendLine("- modified_flag_unchanged: `" + string.Equals(modifiedBefore, modifiedAfter, StringComparison.Ordinal).ToString(CultureInfo.InvariantCulture) + "`");
            report.AppendLine("- model_entity_count_before: `" + modelCountBefore.ToString(CultureInfo.InvariantCulture) + "`");
            report.AppendLine("- model_entity_count_after: `" + modelCountAfter.ToString(CultureInfo.InvariantCulture) + "`");
            report.AppendLine("- paper_entity_count_before: `" + paperCountBefore.ToString(CultureInfo.InvariantCulture) + "`");
            report.AppendLine("- paper_entity_count_after: `" + paperCountAfter.ToString(CultureInfo.InvariantCulture) + "`");
            report.AppendLine("- remaining_risk: Plant3D object-enabler Explode and SubDMesh behavior must be judged in the live runtime.");
            report.AppendLine();
            report.AppendLine("## 7. gate");
            report.AppendLine("- verdict: `PENDING_RUNTIME_EVIDENCE`");
            report.AppendLine("- pass_condition: non-empty real_mesh_triangles and entity counts/Modified flag remain unchanged.");

            WriteSpikeReport(reportPath, report.ToString(), ed);
        }

        private static bool TryResolveHandle(Database db, string handleText, out ObjectId id, out string reason)
        {
            id = ObjectId.Null;
            reason = null;
            try
            {
                string normalized = handleText.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? handleText.Substring(2)
                    : handleText;
                long value = long.Parse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                id = db.GetObjectId(false, new Handle(value), 0);
                if (id == ObjectId.Null)
                {
                    reason = "Database.GetObjectId returned ObjectId.Null";
                    return false;
                }
                if (id.IsErased)
                {
                    reason = "resolved ObjectId is erased";
                    return false;
                }
                return true;
            }
            catch (System.Exception ex)
            {
                reason = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static void ClassifyAndCollectGeometry(
            DBObject obj,
            Transaction tr,
            Matrix3d transform,
            string path,
            List<GeometryCandidate> candidates,
            HashSet<DBObject> retained,
            StringBuilder report)
        {
            string typeName = obj.GetType().FullName;
            string handle = SafeHandle(obj);
            report.AppendLine("- `" + EscapeMarkdown(path) + "` type=`" + EscapeMarkdown(typeName) + "` handle=`" + EscapeMarkdown(handle) + "`");

            if (obj is Entity entity)
            {
                string kind = ClassifyGeometryKind(entity);
                if (kind != null)
                {
                    Entity retainedEntity = entity;
                    bool owns = false;
                    if (!transform.IsEqualTo(Matrix3d.Identity))
                    {
                        Entity transformed = null;
                        try
                        {
                            transformed = entity.GetTransformedCopy(transform);
                            retainedEntity = transformed;
                            owns = true;
                        }
                        catch (System.Exception ex)
                        {
                            report.AppendLine("  - transform_copy: FAIL `" + EscapeMarkdown(ex.Message) + "`");
                            transformed?.Dispose();
                            return;
                        }
                    }

                    retained.Add(retainedEntity);
                    candidates.Add(new GeometryCandidate
                    {
                        SourcePath = path,
                        Kind = kind,
                        EntityType = typeName,
                        Handle = handle,
                        WorldTransform = transform,
                        Entity = retainedEntity,
                        OwnsEntity = owns || !retainedEntity.ObjectId.IsValid
                    });
                    report.AppendLine("  - classification: `" + kind + "` collected");
                    return;
                }

                if (entity is BlockReference br)
                {
                    Matrix3d nested = br.BlockTransform * transform;
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead, false);
                    int index = 0;
                    foreach (ObjectId childId in btr)
                    {
                        DBObject child = tr.GetObject(childId, OpenMode.ForRead, false);
                        if (child == null || child.IsErased)
                        {
                            continue;
                        }
                        ClassifyAndCollectGeometry(child, tr, nested, path + "/block[" + index.ToString(CultureInfo.InvariantCulture) + "]", candidates, retained, report);
                        index++;
                    }
                    return;
                }

                TryExplodeAndRecurse(entity, tr, transform, path, candidates, retained, report);
                return;
            }

            report.AppendLine("  - classification: unsupported non-Entity; 분류 불가");
        }

        private static void TryExplodeAndRecurse(
            Entity entity,
            Transaction tr,
            Matrix3d transform,
            string path,
            List<GeometryCandidate> candidates,
            HashSet<DBObject> retained,
            StringBuilder report)
        {
            Entity explodeSource = entity;
            bool disposeExplodeSource = false;
            var explodedObjects = new DBObjectCollection();

            try
            {
                if (!transform.IsEqualTo(Matrix3d.Identity))
                {
                    explodeSource = entity.GetTransformedCopy(transform);
                    disposeExplodeSource = true;
                }

                explodeSource.Explode(explodedObjects);
                report.AppendLine("  - explode: OK count=`" + explodedObjects.Count.ToString(CultureInfo.InvariantCulture) + "`");
                int index = 0;
                foreach (DBObject child in explodedObjects)
                {
                    if (child == null)
                    {
                        continue;
                    }
                    ClassifyAndCollectGeometry(child, tr, Matrix3d.Identity, path + "/explode[" + index.ToString(CultureInfo.InvariantCulture) + "]", candidates, retained, report);
                    index++;
                }
            }
            catch (System.Exception ex)
            {
                report.AppendLine("  - classification: 분류 불가");
                report.AppendLine("  - explode: FAIL `" + EscapeMarkdown(ex.Message) + "`");
            }
            finally
            {
                foreach (DBObject child in explodedObjects)
                {
                    if (child != null && !retained.Contains(child))
                    {
                        child.Dispose();
                    }
                }
                if (disposeExplodeSource && explodeSource != null && !retained.Contains(explodeSource))
                {
                    explodeSource.Dispose();
                }
            }
        }

        private static string ClassifyGeometryKind(Entity entity)
        {
            string name = entity.GetType().Name;
            string fullName = entity.GetType().FullName ?? "";
            if (entity is Solid3d) return "Solid3d";
            if (name == "Region" || fullName.EndsWith(".Region", StringComparison.Ordinal)) return "Region";
            if (name.EndsWith("Surface", StringComparison.Ordinal) || fullName.IndexOf(".Surface", StringComparison.OrdinalIgnoreCase) >= 0) return "Surface";
            if (name == "Body" || fullName.EndsWith(".Body", StringComparison.Ordinal)) return "Body";
            if (name == "SubDMesh" || fullName.EndsWith(".SubDMesh", StringComparison.Ordinal)) return "SubDMesh";
            if (name == "PolyFaceMesh" || fullName.EndsWith(".PolyFaceMesh", StringComparison.Ordinal)) return "PolyFaceMesh";
            return null;
        }

        private static void ProbeSupportHelper(ObjectId id, StringBuilder report)
        {
            try
            {
                Type helperType = Type.GetType("Autodesk.ProcessPower.PnP3dPipeSupport.SupportHelper, PnP3dPipeSupport", false)
                    ?? Type.GetType("Autodesk.ProcessPower.PnP3dPipeSupport.SupportHelper, PnP3dPipeSupportsUI", false)
                    ?? FindLoadedType("Autodesk.ProcessPower.PnP3dPipeSupport.SupportHelper");
                if (helperType == null)
                {
                    report.AppendLine("- SupportHelper: FAIL type_not_found");
                    return;
                }

                report.AppendLine("- SupportHelper type: `" + EscapeMarkdown(helperType.AssemblyQualifiedName) + "`");
                foreach (MethodInfo method in helperType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (method.Name.IndexOf("Support", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        method.Name.IndexOf("Parameter", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        report.AppendLine("  - method: `" + EscapeMarkdown(method.ToString()) + "`");
                    }
                }

                MethodInfo getParameters = helperType.GetMethod("GetSupportParameters", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(ObjectId) }, null);
                if (getParameters == null)
                {
                    report.AppendLine("- GetSupportParameters: FAIL method_not_found");
                    return;
                }

                object result = getParameters.Invoke(null, new object[] { id });
                int count = 0;
                if (result is IEnumerable enumerable)
                {
                    foreach (object item in enumerable)
                    {
                        count++;
                        string name = ReadStringProperty(item, "Name");
                        string def = InvokeStringMethod(item, "ToDefinitionString");
                        report.AppendLine("  - parameter[" + count.ToString(CultureInfo.InvariantCulture) + "]: `" + EscapeMarkdown(name) + "` = `" + EscapeMarkdown(def) + "`");
                    }
                }
                report.AppendLine("- GetSupportParameters count: `" + count.ToString(CultureInfo.InvariantCulture) + "`");
            }
            catch (TargetInvocationException ex)
            {
                report.AppendLine("- SupportHelper: FAIL `" + EscapeMarkdown((ex.InnerException ?? ex).Message) + "`");
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- SupportHelper: FAIL `" + EscapeMarkdown(ex.Message) + "`");
            }
        }

        private static Type FindLoadedType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        private static bool TryCaptureExistingMesh(GeometryCandidate candidate, Transaction tr, MeshCapture mesh, StringBuilder report)
        {
            try
            {
                if (candidate.Kind == "PolyFaceMesh")
                {
                    bool ok = TryCapturePolyFaceMesh(candidate.Entity, tr, candidate.SourcePath, mesh, report);
                    if (ok)
                    {
                        return true;
                    }
                }

                if (candidate.Kind == "SubDMesh")
                {
                    bool ok = TryCaptureGenericMeshObject(candidate.Entity, candidate.SourcePath, mesh, report);
                    if (ok)
                    {
                        return true;
                    }
                }

                report.AppendLine("- real_mesh: not_available_for_kind");
                return false;
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- real_mesh: FAIL `" + EscapeMarkdown(ex.Message) + "`");
                return false;
            }
        }

        private static bool TryCapturePolyFaceMesh(Entity entity, Transaction tr, string sourcePath, MeshCapture mesh, StringBuilder report)
        {
            if (!(entity is IEnumerable enumerable))
            {
                report.AppendLine("- PolyFaceMesh: FAIL not_enumerable");
                return false;
            }

            var vertices = new List<Point3d>();
            int faces = 0;
            foreach (object item in enumerable)
            {
                DBObject sub = null;
                if (item is ObjectId id && id.IsValid && !id.IsNull)
                {
                    sub = tr.GetObject(id, OpenMode.ForRead, false);
                }
                else
                {
                    sub = item as DBObject;
                }

                if (sub == null)
                {
                    continue;
                }

                string typeName = sub.GetType().Name;
                if (typeName.IndexOf("Vertex", StringComparison.OrdinalIgnoreCase) >= 0 && TryReadPointProperty(sub, "Position", out Point3d point))
                {
                    vertices.Add(point);
                    continue;
                }

                if (typeName.IndexOf("Face", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    int[] indices = ReadFaceIndices(sub);
                    if (indices.Length >= 3)
                    {
                        faces += mesh.AddFaceFan(vertices, indices, sourcePath, false);
                    }
                }
            }

            report.AppendLine("- PolyFaceMesh: vertices=`" + vertices.Count.ToString(CultureInfo.InvariantCulture) + "` faces=`" + faces.ToString(CultureInfo.InvariantCulture) + "`");
            return faces > 0;
        }

        private static bool TryCaptureGenericMeshObject(object meshObject, string sourcePath, MeshCapture mesh, StringBuilder report)
        {
            int[] indices = TryReadIndexArray(meshObject);
            Point3d[] points = TryReadPointArray(meshObject);
            if (points.Length == 0 && indices.Length >= 3)
            {
                points = TryReadVerticesByIndex(meshObject, indices);
            }
            if (points.Length == 0 || indices.Length < 3)
            {
                DumpMeshLikeMethods(meshObject, report);
                report.AppendLine("- generic_mesh_extract: FAIL vertices_or_indices_not_found");
                return false;
            }

            int added = mesh.AddMeshIndexData(points, indices, sourcePath, false);
            report.AppendLine("- generic_mesh_extract: OK vertices=`" + points.Length.ToString(CultureInfo.InvariantCulture) + "` triangles=`" + added.ToString(CultureInfo.InvariantCulture) + "`");
            return added > 0;
        }

        private static bool TryGetObjectMesh(GeometryCandidate candidate, MeshCapture mesh, StringBuilder report)
        {
            object faceterData = null;
            object meshDataCollection = null;
            try
            {
                Type subDMeshType = typeof(SubDMesh);
                Type faceterType = subDMeshType.Assembly.GetType("Autodesk.AutoCAD.DatabaseServices.MeshFaceterData")
                    ?? FindLoadedType("Autodesk.AutoCAD.DatabaseServices.MeshFaceterData");
                if (faceterType == null)
                {
                    report.AppendLine("- GetObjectMesh: FAIL MeshFaceterData_type_not_found");
                    return false;
                }

                faceterData = Activator.CreateInstance(faceterType);
                string toleranceSummary = ConfigureMeshFaceterData(faceterData);
                report.AppendLine("- GetObjectMesh tolerance: `" + EscapeMarkdown(toleranceSummary) + "`");

                MethodInfo method = null;
                foreach (MethodInfo candidateMethod in subDMeshType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                {
                    if (!candidateMethod.Name.Equals("GetObjectMesh", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = candidateMethod.GetParameters();
                    if (parameters.Length == 2 &&
                        parameters[0].ParameterType.IsAssignableFrom(typeof(DBObject)) &&
                        parameters[1].ParameterType.IsAssignableFrom(faceterType))
                    {
                        method = candidateMethod;
                        break;
                    }
                }

                if (method == null)
                {
                    report.AppendLine("- GetObjectMesh: FAIL method_not_found");
                    DumpMeshLikeMethods(subDMeshType, report);
                    return false;
                }

                object target = method.IsStatic ? null : Activator.CreateInstance(subDMeshType);
                try
                {
                    meshDataCollection = method.Invoke(target, new object[] { candidate.Entity, faceterData });
                }
                finally
                {
                    if (target is IDisposable disposableTarget)
                    {
                        disposableTarget.Dispose();
                    }
                }

                if (meshDataCollection == null)
                {
                    report.AppendLine("- GetObjectMesh: FAIL returned_null");
                    return false;
                }

                int added = CaptureMeshDataCollection(meshDataCollection, candidate.SourcePath + "/GetObjectMesh", mesh, report);
                report.AppendLine("- GetObjectMesh: invoked `" + EscapeMarkdown(method.ToString()) + "`");
                report.AppendLine("- real_mesh: " + (added > 0 ? "available" : "FAIL no_triangles_extracted"));
                return added > 0;
            }
            catch (TargetInvocationException ex)
            {
                report.AppendLine("- GetObjectMesh: FAIL `" + EscapeMarkdown((ex.InnerException ?? ex).Message) + "`");
                return false;
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- GetObjectMesh: FAIL `" + EscapeMarkdown(ex.Message) + "`");
                return false;
            }
            finally
            {
                if (meshDataCollection is IDisposable disposableMeshData)
                {
                    disposableMeshData.Dispose();
                }
                if (faceterData is IDisposable disposableFaceter)
                {
                    disposableFaceter.Dispose();
                }
            }
        }

        private static string ConfigureMeshFaceterData(object faceterData)
        {
            var values = new List<string>();
            SetNumericPropertyIfExists(faceterData, "FacetMaxEdgeLength", 0.0d, values);
            SetNumericPropertyIfExists(faceterData, "FacetMaxGrid", 0, values);
            SetNumericPropertyIfExists(faceterData, "FacetMaxAngle", 0.0d, values);
            SetNumericPropertyIfExists(faceterData, "SurfaceTolerance", 0.1d, values);
            SetNumericPropertyIfExists(faceterData, "NormalTolerance", 0.0d, values);
            SetNumericPropertyIfExists(faceterData, "GridAspectRatio", 0.0d, values);
            return values.Count == 0 ? "defaults(no writable known properties)" : string.Join(", ", values);
        }

        private static void SetNumericPropertyIfExists(object obj, string propertyName, object desiredValue, List<string> values)
        {
            try
            {
                PropertyInfo property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property == null || !property.CanWrite)
                {
                    return;
                }

                object converted = Convert.ChangeType(desiredValue, property.PropertyType, CultureInfo.InvariantCulture);
                property.SetValue(obj, converted, null);
                values.Add(propertyName + "=" + Convert.ToString(converted, CultureInfo.InvariantCulture));
            }
            catch (System.Exception ex)
            {
                values.Add(propertyName + "=set_failed:" + ex.GetType().Name);
            }
        }

        private static int CaptureMeshDataCollection(object meshDataCollection, string sourcePath, MeshCapture mesh, StringBuilder report)
        {
            int added = 0;
            if (TryCaptureGenericMeshObject(meshDataCollection, sourcePath, mesh, report))
            {
                added += mesh.LastAddedTriangleCount;
            }

            if (meshDataCollection is IEnumerable enumerable)
            {
                int index = 0;
                foreach (object item in enumerable)
                {
                    if (item == null)
                    {
                        index++;
                        continue;
                    }

                    int before = mesh.TriangleCount;
                    if (TryCaptureGenericMeshObject(item, sourcePath + "[" + index.ToString(CultureInfo.InvariantCulture) + "]", mesh, report))
                    {
                        added += mesh.TriangleCount - before;
                    }
                    else
                    {
                        DumpMeshLikeMethods(item, report);
                    }
                    index++;
                }
            }

            report.AppendLine("- MeshDataCollection type: `" + EscapeMarkdown(meshDataCollection.GetType().FullName) + "`");
            report.AppendLine("- MeshDataCollection triangles_added: `" + added.ToString(CultureInfo.InvariantCulture) + "`");
            return added;
        }

        private static bool TrySubDMeshCreateFromSolid(Solid3d solid, MeshCapture mesh, StringBuilder report)
        {
            object subDMesh = null;
            try
            {
                Type meshType = typeof(SubDMesh);
                foreach (MethodInfo method in meshType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (!method.Name.Equals("CreateFrom", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    object[] args = BuildCreateFromArguments(parameters, solid);
                    if (args == null)
                    {
                        continue;
                    }

                    subDMesh = method.Invoke(null, args);
                    if (subDMesh == null)
                    {
                        continue;
                    }

                    bool ok = TryCaptureGenericMeshObject(subDMesh, "SubDMesh.CreateFrom(" + solid.Handle + ")", mesh, report);
                    report.AppendLine("- SubDMesh.CreateFrom: invoked `" + EscapeMarkdown(method.ToString()) + "`");
                    if (ok)
                    {
                        return true;
                    }
                }

                DumpMeshLikeMethods(meshType, report);
                report.AppendLine("- SubDMesh.CreateFrom: FAIL no_usable_overload_or_extractable_mesh");
                return false;
            }
            catch (TargetInvocationException ex)
            {
                report.AppendLine("- SubDMesh.CreateFrom: FAIL `" + EscapeMarkdown((ex.InnerException ?? ex).Message) + "`");
                return false;
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- SubDMesh.CreateFrom: FAIL `" + EscapeMarkdown(ex.Message) + "`");
                return false;
            }
            finally
            {
                if (subDMesh is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        private static object[] BuildCreateFromArguments(ParameterInfo[] parameters, Solid3d solid)
        {
            var args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                Type pt = parameters[i].ParameterType;
                if (pt.IsAssignableFrom(typeof(Solid3d)))
                {
                    args[i] = solid;
                }
                else if (pt == typeof(int))
                {
                    args[i] = 1;
                }
                else if (pt == typeof(short))
                {
                    args[i] = (short)1;
                }
                else if (pt == typeof(double))
                {
                    args[i] = 0.5d;
                }
                else if (pt == typeof(bool))
                {
                    args[i] = false;
                }
                else if (pt.IsEnum)
                {
                    args[i] = Enum.GetValues(pt).GetValue(0);
                }
                else if (pt.IsByRef)
                {
                    return null;
                }
                else
                {
                    return null;
                }
            }
            return args;
        }

        private static void TryBrepProbe(Solid3d solid, StringBuilder report)
        {
            object brep = null;
            try
            {
                Type brepType = typeof(Solid3d).Assembly.GetType("Autodesk.AutoCAD.BoundaryRepresentation.Brep")
                    ?? FindLoadedType("Autodesk.AutoCAD.BoundaryRepresentation.Brep");
                if (brepType == null)
                {
                    report.AppendLine("- Brep: FAIL type_not_found");
                    return;
                }
                brep = Activator.CreateInstance(brepType, solid);
                int faceCount = CountEnumerableProperty(brep, "Faces");
                int edgeCount = CountEnumerableProperty(brep, "Edges");
                report.AppendLine("- Brep: OK faces=`" + faceCount.ToString(CultureInfo.InvariantCulture) + "` edges=`" + edgeCount.ToString(CultureInfo.InvariantCulture) + "`");
            }
            catch (TargetInvocationException ex)
            {
                report.AppendLine("- Brep: FAIL `" + EscapeMarkdown((ex.InnerException ?? ex).Message) + "`");
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- Brep: FAIL `" + EscapeMarkdown(ex.Message) + "`");
            }
            finally
            {
                if (brep is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        private static void TryAcisOutProbe(Solid3d solid, string outDir, string stamp, StringBuilder report)
        {
            try
            {
                string satPath = Path.Combine(outDir, "mesh_spike_iter2_" + stamp + "_" + solid.Handle + ".sat");
                MethodInfo method = solid.GetType().GetMethod("AcisOut", new[] { typeof(string) });
                if (method == null)
                {
                    report.AppendLine("- SAT: FAIL AcisOut(string)_not_found");
                    return;
                }
                method.Invoke(solid, new object[] { satPath });
                report.AppendLine("- SAT: " + (File.Exists(satPath) ? "OK" : "FAIL file_not_created") + " path=`" + EscapeMarkdown(satPath) + "`");
            }
            catch (TargetInvocationException ex)
            {
                report.AppendLine("- SAT: FAIL `" + EscapeMarkdown((ex.InnerException ?? ex).Message) + "`");
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- SAT: FAIL `" + EscapeMarkdown(ex.Message) + "`");
            }
        }

        private static int CountEnumerableProperty(object obj, string propertyName)
        {
            object value = obj.GetType().GetProperty(propertyName)?.GetValue(obj, null);
            if (value == null)
            {
                return -1;
            }
            int count = 0;
            foreach (object ignored in (IEnumerable)value)
            {
                count++;
            }
            return count;
        }

        private static int CountSpaceEntities(Database db, Transaction tr, string spaceName)
        {
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead, false);
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[spaceName], OpenMode.ForRead, false);
            int count = 0;
            foreach (ObjectId ignored in btr)
            {
                count++;
            }
            return count;
        }

        private static void CaptureExtentsBox(GeometryCandidate candidate, MeshCapture mesh, StringBuilder report)
        {
            try
            {
                Extents3d ext = candidate.Entity.GeometricExtents;
                mesh.AddBox(ext.MinPoint, ext.MaxPoint, candidate.SourcePath);
                report.AppendLine("- diagnostic_extents_mesh: OK");
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- diagnostic_extents_mesh: FAIL `" + EscapeMarkdown(ex.Message) + "`");
            }
        }

        private static string ResolveSpikeOutputDirectory()
        {
            return Path.Combine(Path.GetTempPath(), "pfs_mesh_spike");
        }

        private static void WriteSpikeReport(string path, string text, Editor ed)
        {
            File.WriteAllText(path, text, Encoding.UTF8);
            ed.WriteMessage("\n[PFSMESHSPIKE] report: " + path + "\n");
        }

        private static string EscapeMarkdown(string value)
        {
            return (value ?? "").Replace("`", "'");
        }

        private static string SafeHandle(DBObject obj)
        {
            try
            {
                return obj.Handle.ToString();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PFSMESHSPIKE] SafeHandle failed: " + ex.Message);
                return "non-db-resident";
            }
        }

        private static string ReadModifiedFlag(DBObject obj)
        {
            try
            {
                PropertyInfo property = obj.GetType().GetProperty("IsModified", BindingFlags.Public | BindingFlags.Instance)
                    ?? typeof(DBObject).GetProperty("IsModified", BindingFlags.Public | BindingFlags.Instance);
                object value = property?.GetValue(obj, null);
                return value == null ? "property_not_found" : Convert.ToString(value, CultureInfo.InvariantCulture);
            }
            catch (System.Exception ex)
            {
                return "read_failed:" + ex.GetType().Name;
            }
        }

        private static string ProbeVolume(Entity entity)
        {
            try
            {
                object mass = entity.GetType().GetProperty("MassProperties", BindingFlags.Public | BindingFlags.Instance)?.GetValue(entity, null);
                object volume = mass?.GetType().GetProperty("Volume", BindingFlags.Public | BindingFlags.Instance)?.GetValue(mass, null);
                return volume == null ? "not_available" : Convert.ToString(volume, CultureInfo.InvariantCulture);
            }
            catch (System.Exception ex)
            {
                return "failed:" + ex.GetType().Name;
            }
        }

        private static void AppendCandidateInventory(List<GeometryCandidate> candidates, StringBuilder report)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (GeometryCandidate candidate in candidates)
            {
                counts.TryGetValue(candidate.Kind, out int count);
                counts[candidate.Kind] = count + 1;
            }

            report.AppendLine("- type_inventory:");
            foreach (KeyValuePair<string, int> pair in counts)
            {
                report.AppendLine("  - " + pair.Key + ": `" + pair.Value.ToString(CultureInfo.InvariantCulture) + "`");
            }
        }

        private static string ReadStringProperty(object obj, string propertyName)
        {
            try
            {
                object value = obj?.GetType().GetProperty(propertyName)?.GetValue(obj, null);
                return value == null ? "" : Convert.ToString(value, CultureInfo.InvariantCulture);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PFSMESHSPIKE] ReadStringProperty failed: " + ex.Message);
                return "";
            }
        }

        private static string InvokeStringMethod(object obj, string methodName)
        {
            try
            {
                object value = obj?.GetType().GetMethod(methodName, Type.EmptyTypes)?.Invoke(obj, null);
                return value == null ? "" : Convert.ToString(value, CultureInfo.InvariantCulture);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PFSMESHSPIKE] InvokeStringMethod failed: " + ex.Message);
                return "";
            }
        }

        private static bool TryReadPointProperty(object obj, string propertyName, out Point3d point)
        {
            point = Point3d.Origin;
            try
            {
                object value = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj, null);
                if (value is Point3d p)
                {
                    point = p;
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PFSMESHSPIKE] TryReadPointProperty failed: " + ex.Message);
            }
            return false;
        }

        private static int[] ReadFaceIndices(object face)
        {
            var indices = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                object value = InvokeMaybe(face, "GetVertexAt", new object[] { (short)i })
                    ?? ReadProperty(face, "Vertex" + i.ToString(CultureInfo.InvariantCulture))
                    ?? ReadProperty(face, "Vertex" + (i + 1).ToString(CultureInfo.InvariantCulture));
                if (value == null)
                {
                    continue;
                }

                int raw = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                if (raw != 0)
                {
                    indices.Add(Math.Abs(raw) - 1);
                }
            }
            return indices.ToArray();
        }

        private static Point3d[] TryReadPointArray(object obj)
        {
            foreach (string name in new[] { "Vertices", "VertexArray", "Points", "PointArray" })
            {
                object value = ReadProperty(obj, name) ?? InvokeMaybe(obj, "Get" + name, null);
                Point3d[] points = ConvertToPointArray(value);
                if (points.Length > 0)
                {
                    return points;
                }
            }
            return new Point3d[0];
        }

        private static Point3d[] TryReadVerticesByIndex(object obj, int[] indices)
        {
            int maxIndex = -1;
            for (int i = 0; i < indices.Length; i++)
            {
                int value = indices[i];
                if (value > maxIndex)
                {
                    maxIndex = value;
                }
            }

            var points = new List<Point3d>();
            for (int i = 0; i <= maxIndex; i++)
            {
                object value = InvokeMaybe(obj, "GetVertexAt", new object[] { i });
                if (value is Point3d point)
                {
                    points.Add(point);
                    continue;
                }
                break;
            }
            return points.ToArray();
        }

        private static int[] TryReadIndexArray(object obj)
        {
            foreach (string name in new[] { "FaceArray", "Faces", "FaceIndices", "Indices", "IndexArray" })
            {
                object value = ReadProperty(obj, name) ?? InvokeMaybe(obj, "Get" + name, null);
                int[] indices = ConvertToIntArray(value);
                if (indices.Length >= 3)
                {
                    return indices;
                }
            }
            return new int[0];
        }

        private static Point3d[] ConvertToPointArray(object value)
        {
            var points = new List<Point3d>();
            if (value is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    if (item is Point3d point)
                    {
                        points.Add(point);
                    }
                }
            }
            return points.ToArray();
        }

        private static int[] ConvertToIntArray(object value)
        {
            var indices = new List<int>();
            if (value is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    if (item == null)
                    {
                        continue;
                    }
                    if (item is int || item is short || item is long)
                    {
                        indices.Add(Convert.ToInt32(item, CultureInfo.InvariantCulture));
                    }
                }
            }
            return indices.ToArray();
        }

        private static object ReadProperty(object obj, string propertyName)
        {
            try
            {
                return obj?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj, null);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PFSMESHSPIKE] ReadProperty failed: " + ex.Message);
                return null;
            }
        }

        private static object InvokeMaybe(object obj, string methodName, object[] args)
        {
            try
            {
                Type[] argTypes = args == null ? Type.EmptyTypes : Array.ConvertAll(args, arg => arg.GetType());
                MethodInfo method = obj?.GetType().GetMethod(methodName, argTypes);
                return method?.Invoke(obj, args);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PFSMESHSPIKE] InvokeMaybe failed: " + ex.Message);
                return null;
            }
        }

        private static void DumpMeshLikeMethods(object objOrType, StringBuilder report)
        {
            Type type = objOrType as Type ?? objOrType?.GetType();
            if (type == null)
            {
                return;
            }
            int count = 0;
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                if (method.Name.IndexOf("Mesh", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    method.Name.IndexOf("Vertex", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    method.Name.IndexOf("Face", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    method.Name.IndexOf("Create", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    report.AppendLine("  - mesh_method: `" + EscapeMarkdown(method.ToString()) + "`");
                    count++;
                    if (count >= 25)
                    {
                        break;
                    }
                }
            }
        }

        private sealed class MeshCapture
        {
            private readonly List<Point3d> _vertices = new List<Point3d>();
            private readonly List<int[]> _triangles = new List<int[]>();
            private readonly List<string> _sources = new List<string>();
            private readonly List<bool> _diagnostic = new List<bool>();

            public int LastAddedTriangleCount { get; private set; }
            public int VertexCount => _vertices.Count;
            public int TriangleCount => _triangles.Count;
            public int RealTriangleCount
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < _diagnostic.Count; i++)
                    {
                        if (!_diagnostic[i]) count++;
                    }
                    return count;
                }
            }
            public int DiagnosticTriangleCount => TriangleCount - RealTriangleCount;

            public void AddBox(Point3d min, Point3d max, string source)
            {
                int baseIndex = _vertices.Count;
                _vertices.Add(new Point3d(min.X, min.Y, min.Z));
                _vertices.Add(new Point3d(max.X, min.Y, min.Z));
                _vertices.Add(new Point3d(max.X, max.Y, min.Z));
                _vertices.Add(new Point3d(min.X, max.Y, min.Z));
                _vertices.Add(new Point3d(min.X, min.Y, max.Z));
                _vertices.Add(new Point3d(max.X, min.Y, max.Z));
                _vertices.Add(new Point3d(max.X, max.Y, max.Z));
                _vertices.Add(new Point3d(min.X, max.Y, max.Z));

                AddTri(baseIndex, 0, 1, 2, source, true); AddTri(baseIndex, 0, 2, 3, source, true);
                AddTri(baseIndex, 4, 6, 5, source, true); AddTri(baseIndex, 4, 7, 6, source, true);
                AddTri(baseIndex, 0, 4, 5, source, true); AddTri(baseIndex, 0, 5, 1, source, true);
                AddTri(baseIndex, 1, 5, 6, source, true); AddTri(baseIndex, 1, 6, 2, source, true);
                AddTri(baseIndex, 2, 6, 7, source, true); AddTri(baseIndex, 2, 7, 3, source, true);
                AddTri(baseIndex, 3, 7, 4, source, true); AddTri(baseIndex, 3, 4, 0, source, true);
            }

            public int AddIndexedTriangles(Point3d[] points, int[] indices, string source, bool diagnostic)
            {
                int baseIndex = _vertices.Count;
                _vertices.AddRange(points);
                int added = 0;
                for (int i = 0; i + 2 < indices.Length; i += 3)
                {
                    if (indices[i] < 0 || indices[i + 1] < 0 || indices[i + 2] < 0 ||
                        indices[i] >= points.Length || indices[i + 1] >= points.Length || indices[i + 2] >= points.Length)
                    {
                        continue;
                    }
                    AddTri(baseIndex, indices[i], indices[i + 1], indices[i + 2], source, diagnostic);
                    added++;
                }
                LastAddedTriangleCount = added;
                return added;
            }

            public int AddMeshIndexData(Point3d[] points, int[] indices, string source, bool diagnostic)
            {
                int counted = AddCountPrefixedFaces(points, indices, source, diagnostic);
                if (counted > 0)
                {
                    LastAddedTriangleCount = counted;
                    return counted;
                }

                int triangles = AddIndexedTriangles(points, indices, source, diagnostic);
                LastAddedTriangleCount = triangles;
                return triangles;
            }

            private int AddCountPrefixedFaces(Point3d[] points, int[] indices, string source, bool diagnostic)
            {
                int cursor = 0;
                int added = 0;
                int baseIndex = _vertices.Count;
                var localTriangles = new List<int[]>();

                while (cursor < indices.Length)
                {
                    int faceSize = indices[cursor++];
                    if (faceSize < 3 || faceSize > 64 || cursor + faceSize > indices.Length)
                    {
                        return 0;
                    }

                    int first = indices[cursor];
                    for (int i = 1; i + 1 < faceSize; i++)
                    {
                        int b = indices[cursor + i];
                        int c = indices[cursor + i + 1];
                        if (first < 0 || b < 0 || c < 0 || first >= points.Length || b >= points.Length || c >= points.Length)
                        {
                            return 0;
                        }
                        localTriangles.Add(new[] { baseIndex + first, baseIndex + b, baseIndex + c });
                    }
                    cursor += faceSize;
                }

                if (localTriangles.Count == 0)
                {
                    return 0;
                }

                _vertices.AddRange(points);
                foreach (int[] triangle in localTriangles)
                {
                    _triangles.Add(triangle);
                    _sources.Add(source ?? "");
                    _diagnostic.Add(diagnostic);
                    added++;
                }
                return added;
            }

            public int AddFaceFan(List<Point3d> points, int[] faceIndices, string source, bool diagnostic)
            {
                if (points.Count == 0 || faceIndices.Length < 3)
                {
                    return 0;
                }

                int baseIndex = _vertices.Count;
                _vertices.AddRange(points);
                int added = 0;
                int a = faceIndices[0];
                for (int i = 1; i + 1 < faceIndices.Length; i++)
                {
                    int b = faceIndices[i];
                    int c = faceIndices[i + 1];
                    if (a < 0 || b < 0 || c < 0 || a >= points.Count || b >= points.Count || c >= points.Count)
                    {
                        continue;
                    }
                    AddTri(baseIndex, a, b, c, source, diagnostic);
                    added++;
                }
                LastAddedTriangleCount = added;
                return added;
            }

            public string ToJson()
            {
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"units\": \"mm\",");
                sb.AppendLine("  \"upAxis\": \"Z\",");
                sb.AppendLine("  \"diagnosticOnly\": " + (RealTriangleCount == 0 ? "true" : "false") + ",");
                sb.AppendLine("  \"vertices\": [");
                for (int i = 0; i < _vertices.Count; i++)
                {
                    Point3d p = _vertices[i];
                    sb.Append("    [")
                        .Append(p.X.ToString("R", CultureInfo.InvariantCulture)).Append(", ")
                        .Append(p.Y.ToString("R", CultureInfo.InvariantCulture)).Append(", ")
                        .Append(p.Z.ToString("R", CultureInfo.InvariantCulture)).Append("]");
                    sb.AppendLine(i == _vertices.Count - 1 ? "" : ",");
                }
                sb.AppendLine("  ],");
                sb.AppendLine("  \"triangles\": [");
                for (int i = 0; i < _triangles.Count; i++)
                {
                    int[] t = _triangles[i];
                    sb.Append("    { \"i\": [").Append(t[0]).Append(", ").Append(t[1]).Append(", ").Append(t[2]).Append("], ");
                    sb.Append("\"source\": \"").Append(JsonEscape(_sources[i])).Append("\", ");
                    sb.Append("\"diagnostic\": ").Append(_diagnostic[i] ? "true" : "false").Append(" }");
                    sb.AppendLine(i == _triangles.Count - 1 ? "" : ",");
                }
                sb.AppendLine("  ]");
                sb.AppendLine("}");
                return sb.ToString();
            }

            private void AddTri(int baseIndex, int a, int b, int c, string source, bool diagnostic)
            {
                _triangles.Add(new[] { baseIndex + a, baseIndex + b, baseIndex + c });
                _sources.Add(source ?? "");
                _diagnostic.Add(diagnostic);
            }

            private static string JsonEscape(string value)
            {
                return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
            }
        }
    }
}
