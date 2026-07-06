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
        [CommandMethod("PFSCREATESPIKE", CommandFlags.Session)]
        public void RunCreateSupportSpike()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc?.Editor;
            Database db = doc?.Database;
            var report = new StringBuilder();
            string outDir = ResolveSpikeOutputDirectory();
            Directory.CreateDirectory(outDir);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string reportPath = Path.Combine(outDir, "spike_result_create_step2_" + stamp + ".md");

            report.AppendLine("# PFS create support spike result - Step 2 create probe");
            report.AppendLine();
            report.AppendLine("- command: `PFSCREATESPIKE`");
            report.AppendLine("- step: `inventory-plus-create-probe`");
            report.AppendLine("- create_support_entity_called: `pending`");
            report.AppendLine("- drawing: `" + EscapeMarkdown(db?.Filename ?? "no_active_document") + "`");
            report.AppendLine("- output_dir: `" + EscapeMarkdown(outDir) + "`");
            report.AppendLine("- safety: append inside active DWG transaction, mandatory Abort rollback");
            report.AppendLine("- undo_check: manual-only; no forced API undo implementation");
            report.AppendLine();

            string supportType = PromptSupportType(ed);
            if (string.IsNullOrWhiteSpace(supportType))
            {
                report.AppendLine("## 0. prompt");
                report.AppendLine("- status: `cancelled`");
                WriteCreateSpikeReport(reportPath, report.ToString(), ed);
                return;
            }

            report.AppendLine("## 0. prompt");
            report.AppendLine("- support_type: `" + EscapeMarkdown(supportType) + "`");
            report.AppendLine();

            Type helperType = FindSupportHelperType(report);
            report.AppendLine();

            Type partSizePropertiesType = FindLoadedType("Autodesk.ProcessPower.PnP3dObjects.PartSizeProperties");
            report.AppendLine("## 2. PartSizeProperties type inventory");
            DumpTypeInventory(partSizePropertiesType, report, "PartSizeProperties", true);
            report.AppendLine();

            object supportInfo = null;
            if (helperType != null)
            {
                report.AppendLine("## 3. FindSupportInfo probe");
                supportInfo = InvokeFindSupportInfo(helperType, supportType, report);
                report.AppendLine();
            }

            report.AppendLine("## 4. SupportInfo inventory");
            DumpObjectInventory(supportInfo, report, "SupportInfo", true);
            report.AppendLine();

            report.AppendLine("## 5. PartSizeProperties source candidates");
            AppendPartSizePropertiesSourceCandidates(helperType, supportInfo, partSizePropertiesType, report);
            report.AppendLine();

            report.AppendLine("## 6. Step 1 verdict");
            report.AppendLine("- status: `" + (helperType != null && supportInfo != null ? "inventory_collected" : "partial_inventory") + "`");
            report.AppendLine("- next_step: `Step 2 create probe appended below`");
            report.AppendLine();

            AppendCreateSupportStep2(doc, db, helperType, supportInfo, supportType, report);

            WriteCreateSpikeReport(reportPath, report.ToString(), ed);
        }

        private static string PromptSupportType(Editor ed)
        {
            if (ed == null)
            {
                return "RS2";
            }

            var opt = new PromptStringOptions("\nPFS create spike Support Type 입력 <RS2>: ");
            opt.AllowSpaces = false;
            opt.DefaultValue = "RS2";
            opt.UseDefaultValue = true;
            PromptResult pr = ed.GetString(opt);
            if (pr.Status != PromptStatus.OK)
            {
                return null;
            }
            return string.IsNullOrWhiteSpace(pr.StringResult) ? "RS2" : pr.StringResult.Trim();
        }

        private static Type FindSupportHelperType(StringBuilder report)
        {
            report.AppendLine("## 1. SupportHelper inventory");
            Type helperType = Type.GetType("Autodesk.ProcessPower.PnP3dPipeSupport.SupportHelper, PnP3dPipeSupport", false)
                ?? Type.GetType("Autodesk.ProcessPower.PnP3dPipeSupport.SupportHelper, PnP3dPipeSupportsUI", false)
                ?? Type.GetType("Autodesk.ProcessPower.PnP3dPipeSupport.SupportHelper, PnP3dPipeUI", false)
                ?? FindLoadedType("Autodesk.ProcessPower.PnP3dPipeSupport.SupportHelper");

            if (helperType == null)
            {
                report.AppendLine("- status: `FAIL type_not_found`");
                return null;
            }

            report.AppendLine("- status: `OK`");
            report.AppendLine("- type: `" + EscapeMarkdown(helperType.AssemblyQualifiedName) + "`");
            DumpTypeInventory(helperType, report, "SupportHelper", false);
            return helperType;
        }

        private static object InvokeFindSupportInfo(Type helperType, string supportType, StringBuilder report)
        {
            try
            {
                MethodInfo method = helperType.GetMethod("FindSupportInfo", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (method == null)
                {
                    report.AppendLine("- status: `FAIL method_not_found`");
                    return null;
                }

                object supportInfo = method.Invoke(null, new object[] { supportType });
                if (supportInfo == null)
                {
                    report.AppendLine("- status: `FAIL returned_null`");
                    return null;
                }

                report.AppendLine("- status: `OK`");
                report.AppendLine("- result_type: `" + EscapeMarkdown(supportInfo.GetType().AssemblyQualifiedName) + "`");
                AppendReadableScalarProperties(supportInfo, report, 30);
                return supportInfo;
            }
            catch (TargetInvocationException ex)
            {
                report.AppendLine("- status: `FAIL TargetInvocationException`");
                report.AppendLine("- reason: `" + EscapeMarkdown((ex.InnerException ?? ex).Message) + "`");
                return null;
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- status: `FAIL " + EscapeMarkdown(ex.GetType().Name) + "`");
                report.AppendLine("- reason: `" + EscapeMarkdown(ex.Message) + "`");
                return null;
            }
        }

        private static void DumpObjectInventory(object obj, StringBuilder report, string label, bool includeAll)
        {
            if (obj == null)
            {
                report.AppendLine("- " + label + ": `null`");
                return;
            }
            report.AppendLine("- " + label + "_type: `" + EscapeMarkdown(obj.GetType().AssemblyQualifiedName) + "`");
            DumpTypeInventory(obj.GetType(), report, label, includeAll);
            AppendReadableScalarProperties(obj, report, 50);
        }

        private static void DumpTypeInventory(Type type, StringBuilder report, string label, bool includeAll)
        {
            if (type == null)
            {
                report.AppendLine("- " + label + ": `type_not_found`");
                return;
            }

            report.AppendLine("- " + label + "_assembly: `" + EscapeMarkdown(type.Assembly.FullName) + "`");

            int propertyCount = 0;
            foreach (PropertyInfo property in SafeGetProperties(type, report, label))
            {
                if (!includeAll && !IsRelevantMember(property.Name, property.PropertyType))
                {
                    continue;
                }
                report.AppendLine("  - property: `" + EscapeMarkdown(DescribeProperty(property)) + "`");
                propertyCount++;
                if (propertyCount >= 80)
                {
                    report.AppendLine("  - property: `truncated_after_80`");
                    break;
                }
            }

            int methodCount = 0;
            foreach (MethodInfo method in SafeGetMethods(type, report, label))
            {
                if (method.IsSpecialName)
                {
                    continue;
                }
                if (!includeAll && !IsRelevantMember(method.Name, method.ReturnType) && !HasRelevantParameter(method))
                {
                    continue;
                }
                report.AppendLine("  - method: `" + EscapeMarkdown(method.ToString()) + "`");
                methodCount++;
                if (methodCount >= 120)
                {
                    report.AppendLine("  - method: `truncated_after_120`");
                    break;
                }
            }

            foreach (ConstructorInfo ctor in SafeGetConstructors(type, report, label))
            {
                report.AppendLine("  - constructor: `" + EscapeMarkdown(ctor.ToString()) + "`");
            }
        }

        private static PropertyInfo[] SafeGetProperties(Type type, StringBuilder report, string label)
        {
            try
            {
                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- " + label + "_properties: `read_failed:" + EscapeMarkdown(ex.GetType().Name) + ":" + EscapeMarkdown(ex.Message) + "`");
                return Array.Empty<PropertyInfo>();
            }
        }

        private static MethodInfo[] SafeGetMethods(Type type, StringBuilder report, string label)
        {
            try
            {
                return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- " + label + "_methods: `read_failed:" + EscapeMarkdown(ex.GetType().Name) + ":" + EscapeMarkdown(ex.Message) + "`");
                return Array.Empty<MethodInfo>();
            }
        }

        private static ConstructorInfo[] SafeGetConstructors(Type type, StringBuilder report, string label)
        {
            try
            {
                return type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- " + label + "_constructors: `read_failed:" + EscapeMarkdown(ex.GetType().Name) + ":" + EscapeMarkdown(ex.Message) + "`");
                return Array.Empty<ConstructorInfo>();
            }
        }

        private static bool IsRelevantMember(string name, Type type)
        {
            string haystack = ((name ?? "") + " " + (type?.FullName ?? "")).ToLowerInvariant();
            return haystack.Contains("support")
                || haystack.Contains("partsize")
                || haystack.Contains("properties")
                || haystack.Contains("parameter")
                || haystack.Contains("catalog")
                || haystack.Contains("spec")
                || haystack.Contains("create")
                || haystack.Contains("find");
        }

        private static bool HasRelevantParameter(MethodInfo method)
        {
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                if (IsRelevantMember(parameter.Name, parameter.ParameterType))
                {
                    return true;
                }
            }
            return false;
        }

        private static string DescribeProperty(PropertyInfo property)
        {
            string access = (property.CanRead ? "get;" : "") + (property.CanWrite ? " set;" : "");
            return property.PropertyType.FullName + " " + property.Name + " { " + access.Trim() + " }";
        }

        private static void AppendReadableScalarProperties(object obj, StringBuilder report, int limit)
        {
            int count = 0;
            foreach (PropertyInfo property in SafeGetProperties(obj.GetType(), report, obj.GetType().Name))
            {
                if (!property.CanRead || property.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                try
                {
                    object value = property.GetValue(obj, null);
                    if (value == null || IsScalar(value.GetType()))
                    {
                        report.AppendLine("  - value." + property.Name + ": `" + EscapeMarkdown(Convert.ToString(value, CultureInfo.InvariantCulture)) + "`");
                        count++;
                    }
                    else if (value is IEnumerable && !(value is string))
                    {
                        report.AppendLine("  - value." + property.Name + ": `" + EscapeMarkdown(value.GetType().FullName) + " enumerable`");
                        count++;
                    }
                    else if (IsRelevantMember(property.Name, value.GetType()))
                    {
                        report.AppendLine("  - value." + property.Name + ": `" + EscapeMarkdown(value.GetType().FullName) + "`");
                        count++;
                    }
                }
                catch (System.Exception ex)
                {
                    report.AppendLine("  - value." + property.Name + ": `read_failed:" + EscapeMarkdown(ex.GetType().Name) + "`");
                    count++;
                }

                if (count >= limit)
                {
                    report.AppendLine("  - value: `truncated_after_" + limit.ToString(CultureInfo.InvariantCulture) + "`");
                    break;
                }
            }
        }

        private static bool IsScalar(Type type)
        {
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(Guid);
        }

        private static void AppendPartSizePropertiesSourceCandidates(Type helperType, object supportInfo, Type partSizePropertiesType, StringBuilder report)
        {
            int candidates = 0;
            if (helperType != null)
            {
                foreach (MethodInfo method in SafeGetMethods(helperType, report, "SupportHelper"))
                {
                    if (IsPartSizePropertiesCandidate(method.ReturnType, partSizePropertiesType) || HasPartSizePropertiesOutParameter(method, partSizePropertiesType))
                    {
                        report.AppendLine("- helper_method_candidate: `" + EscapeMarkdown(method.ToString()) + "`");
                        candidates++;
                    }
                }
            }

            if (supportInfo != null)
            {
                foreach (PropertyInfo property in SafeGetProperties(supportInfo.GetType(), report, "SupportInfo"))
                {
                    if (IsPartSizePropertiesCandidate(property.PropertyType, partSizePropertiesType) || IsRelevantMember(property.Name, property.PropertyType))
                    {
                        report.AppendLine("- support_info_property_candidate: `" + EscapeMarkdown(DescribeProperty(property)) + "`");
                        candidates++;
                    }
                }

                foreach (MethodInfo method in SafeGetMethods(supportInfo.GetType(), report, "SupportInfo"))
                {
                    if (method.IsSpecialName)
                    {
                        continue;
                    }
                    if (IsPartSizePropertiesCandidate(method.ReturnType, partSizePropertiesType) || IsRelevantMember(method.Name, method.ReturnType))
                    {
                        report.AppendLine("- support_info_method_candidate: `" + EscapeMarkdown(method.ToString()) + "`");
                        candidates++;
                    }
                }
            }

            report.AppendLine("- candidate_count: `" + candidates.ToString(CultureInfo.InvariantCulture) + "`");
            report.AppendLine("- create_support_entity_called: `False`");
        }

        private static bool IsPartSizePropertiesCandidate(Type type, Type knownPartSizePropertiesType)
        {
            if (type == null)
            {
                return false;
            }
            if (knownPartSizePropertiesType != null && knownPartSizePropertiesType.IsAssignableFrom(type))
            {
                return true;
            }
            return (type.FullName ?? "").IndexOf("PartSizeProperties", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasPartSizePropertiesOutParameter(MethodInfo method, Type knownPartSizePropertiesType)
        {
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                Type parameterType = parameter.ParameterType;
                Type elementType = parameterType.IsByRef ? parameterType.GetElementType() : parameterType;
                if ((parameter.IsOut || parameterType.IsByRef) && IsPartSizePropertiesCandidate(elementType, knownPartSizePropertiesType))
                {
                    return true;
                }
            }
            return false;
        }

        private static void AppendCreateSupportStep2(Document doc, Database db, Type helperType, object supportInfo, string supportType, StringBuilder report)
        {
            report.AppendLine("## 7. Step 2 create support entity probe");
            report.AppendLine("- support_type: `" + EscapeMarkdown(supportType) + "`");

            if (doc == null || db == null)
            {
                report.AppendLine("- status: `FAIL no_active_document`");
                AppendCreateStep2Gate(false, 0, false, report);
                return;
            }
            if (helperType == null || supportInfo == null)
            {
                report.AppendLine("- status: `FAIL missing_support_helper_or_info`");
                AppendCreateStep2Gate(false, 0, false, report);
                return;
            }

            object nominalDiameter = null;
            object partSizeProperties = null;
            var candidates = new List<GeometryCandidate>();
            var retained = new HashSet<DBObject>();
            int modelCountBefore = -1;
            int modelCountAfterAppend = -1;
            int modelCountAfterAbort = -1;
            bool explicitAbort = false;
            bool createSuccess = false;
            string createOverload = "";
            var mesh = new MeshCapture();

            try
            {
                report.AppendLine();
                report.AppendLine("### 7-A. PartSizeProperties preparation");
                nominalDiameter = CreateNominalDiameterForSpike(80.0, report);
                if (nominalDiameter == null)
                {
                    report.AppendLine("- prepare_status: `FAIL nominal_diameter_unavailable`");
                    AppendCreateStep2Gate(false, 0, false, report);
                    return;
                }

                if (!InvokeInitParameterList(supportInfo, nominalDiameter, report))
                {
                    report.AppendLine("- prepare_status: `FAIL InitParameterList`");
                    AppendCreateStep2Gate(false, 0, false, report);
                    return;
                }

                partSizeProperties = InvokeMakePart(supportInfo, report);
                if (partSizeProperties == null)
                {
                    report.AppendLine("- prepare_status: `FAIL MakePart returned null`");
                    AppendCreateStep2Gate(false, 0, false, report);
                    return;
                }

                TrySetPartSizePropertyObject(partSizeProperties, "NominalDiameter", nominalDiameter, report);
                TrySetCreateSpikePropValue(partSizeProperties, "Dn", 80.0, report);
                TrySetCreateSpikePropValue(partSizeProperties, "BI", 210.0, report);
                TrySetCreateSpikePropValue(partSizeProperties, "A", 800.0, report);
                TrySetCreateSpikePropValue(partSizeProperties, "A1", 150.0, report);
                TrySetCreateSpikePropValue(partSizeProperties, "F2", 900.0, report);
                TrySetCreateSpikePropValue(partSizeProperties, "P1", 0.0, report);
                TrySetCreateSpikePropValue(partSizeProperties, "TY", -2.0, report);
                report.AppendLine("- prepare_status: `OK`");
                report.AppendLine();

                using (doc.LockDocument())
                {
                    Transaction tr = null;
                    try
                    {
                        tr = db.TransactionManager.StartTransaction();
                        modelCountBefore = CountSpaceEntities(db, tr, BlockTableRecord.ModelSpace);

                        report.AppendLine("### 7-B. CreateSupportEntity and append");
                        object created = InvokeCreateSupportEntity(helperType, supportInfo, db, ref partSizeProperties, out createOverload, report);
                        if (created == null)
                        {
                            report.AppendLine("- create_status: `FAIL returned_null`");
                            tr.Abort();
                            explicitAbort = true;
                            throw new InvalidOperationException("CreateSupportEntity returned null after transaction abort.");
                        }

                        createSuccess = true;
                        report.AppendLine("- create_status: `OK`");
                        report.AppendLine("- create_overload: `" + EscapeMarkdown(createOverload) + "`");
                        report.AppendLine("- created_type: `" + EscapeMarkdown(created.GetType().AssemblyQualifiedName) + "`");

                        Entity createdEntity = created as Entity;
                        if (createdEntity == null)
                        {
                            report.AppendLine("- append_status: `FAIL created_object_is_not_entity`");
                            TryDisposeObject(created, report, "created_non_entity");
                            tr.Abort();
                            explicitAbort = true;
                            throw new InvalidOperationException("CreateSupportEntity returned a non-Entity object after transaction abort.");
                        }

                        ObjectId createdId = AppendEntityToModelSpace(db, tr, createdEntity, report);
                        modelCountAfterAppend = CountSpaceEntities(db, tr, BlockTableRecord.ModelSpace);
                        report.AppendLine("- appended_object_id: `" + createdId.ToString() + "`");
                        report.AppendLine("- model_entity_count_before: `" + modelCountBefore.ToString(CultureInfo.InvariantCulture) + "`");
                        report.AppendLine("- model_entity_count_after_append: `" + modelCountAfterAppend.ToString(CultureInfo.InvariantCulture) + "`");
                        report.AppendLine();

                        report.AppendLine("### 7-C. Mesh extraction");
                        ClassifyAndCollectGeometry(createdEntity, tr, Matrix3d.Identity, "created_support", candidates, retained, report);
                        report.AppendLine("- collected_geometry_count: `" + candidates.Count.ToString(CultureInfo.InvariantCulture) + "`");
                        AppendCandidateInventory(candidates, report);
                        foreach (GeometryCandidate candidate in candidates)
                        {
                            report.AppendLine("#### " + candidate.SourcePath);
                            report.AppendLine("- kind: `" + EscapeMarkdown(candidate.Kind) + "`");
                            report.AppendLine("- entity_type: `" + EscapeMarkdown(candidate.EntityType) + "`");
                            bool hasRealMesh = TryGetObjectMesh(candidate, mesh, report);
                            if (!hasRealMesh)
                            {
                                hasRealMesh = TryCaptureExistingMesh(candidate, tr, mesh, report);
                            }
                            if (!hasRealMesh && candidate.Entity is Solid3d solid)
                            {
                                TryBrepProbe(solid, report);
                                hasRealMesh = TrySubDMeshCreateFromSolid(solid, mesh, report);
                            }
                            if (!hasRealMesh)
                            {
                                report.AppendLine("- diagnostic_extents_mesh: skipped_for_create_gate");
                            }
                            report.AppendLine();
                        }
                        report.AppendLine("- vertices: `" + mesh.VertexCount.ToString(CultureInfo.InvariantCulture) + "`");
                        report.AppendLine("- triangles: `" + mesh.TriangleCount.ToString(CultureInfo.InvariantCulture) + "`");
                        report.AppendLine("- real_mesh_triangles: `" + mesh.RealTriangleCount.ToString(CultureInfo.InvariantCulture) + "`");
                        report.AppendLine("- diagnostic_triangles: `" + mesh.DiagnosticTriangleCount.ToString(CultureInfo.InvariantCulture) + "`");
                        report.AppendLine();

                        report.AppendLine("### 7-D. Rollback");
                        tr.Abort();
                        explicitAbort = true;
                        report.AppendLine("- transaction: `Abort()`");
                    }
                    catch (TargetInvocationException ex)
                    {
                        report.AppendLine("- exception_type: `TargetInvocationException`");
                        report.AppendLine("- exception_message: `" + EscapeMarkdown((ex.InnerException ?? ex).Message) + "`");
                        report.AppendLine("- stack: `" + EscapeMarkdown((ex.InnerException ?? ex).StackTrace ?? ex.StackTrace ?? "") + "`");
                        if (!explicitAbort && tr != null)
                        {
                            tr.Abort();
                            explicitAbort = true;
                            report.AppendLine("- transaction: `Abort()`");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        report.AppendLine("- exception_type: `" + EscapeMarkdown(ex.GetType().FullName) + "`");
                        report.AppendLine("- exception_message: `" + EscapeMarkdown(ex.Message) + "`");
                        report.AppendLine("- stack: `" + EscapeMarkdown(ex.StackTrace ?? "") + "`");
                        if (!explicitAbort && tr != null)
                        {
                            tr.Abort();
                            explicitAbort = true;
                            report.AppendLine("- transaction: `Abort()`");
                        }
                    }
                    finally
                    {
                        tr?.Dispose();
                    }
                }

                using (Transaction verifyTr = db.TransactionManager.StartTransaction())
                {
                    modelCountAfterAbort = CountSpaceEntities(db, verifyTr, BlockTableRecord.ModelSpace);
                    verifyTr.Abort();
                }
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- outer_exception_type: `" + EscapeMarkdown(ex.GetType().FullName) + "`");
                report.AppendLine("- outer_exception_message: `" + EscapeMarkdown(ex.Message) + "`");
                report.AppendLine("- outer_stack: `" + EscapeMarkdown(ex.StackTrace ?? "") + "`");
            }
            finally
            {
                foreach (GeometryCandidate candidate in candidates)
                {
                    candidate.Dispose();
                }
                TryDisposeObject(partSizeProperties, report, "part_size_properties");
                TryDisposeObject(nominalDiameter, report, "nominal_diameter");
            }

            bool countUnchanged = modelCountBefore >= 0 && modelCountAfterAbort == modelCountBefore;
            report.AppendLine("- model_entity_count_after_abort: `" + modelCountAfterAbort.ToString(CultureInfo.InvariantCulture) + "`");
            report.AppendLine("- abort_count_unchanged: `" + countUnchanged.ToString(CultureInfo.InvariantCulture) + "`");
            report.AppendLine("- project_bom_change_expected: `False`");
            report.AppendLine("- undo_check: `manual_only`");
            AppendCreateStep2Gate(createSuccess, mesh.RealTriangleCount, countUnchanged, report);
        }

        private static object CreateNominalDiameterForSpike(double dn, StringBuilder report)
        {
            Type nominalDiameterType = FindLoadedType("Autodesk.ProcessPower.PartsRepository.NominalDiameter")
                ?? Type.GetType("Autodesk.ProcessPower.PartsRepository.NominalDiameter, PnP3dObjectsMgd", false)
                ?? Type.GetType("Autodesk.ProcessPower.PartsRepository.NominalDiameter, PnPProjectPartsMgd", false);
            if (nominalDiameterType == null)
            {
                report.AppendLine("- nominal_diameter: `FAIL type_not_found`");
                return null;
            }

            report.AppendLine("- nominal_diameter_type: `" + EscapeMarkdown(nominalDiameterType.AssemblyQualifiedName) + "`");
            foreach (object[] args in BuildNominalDiameterConstructorArgs(dn))
            {
                try
                {
                    object nd = Activator.CreateInstance(nominalDiameterType, args);
                    if (nd != null)
                    {
                        report.AppendLine("- nominal_diameter_ctor: `OK " + EscapeMarkdown(DescribeArgs(args)) + "`");
                        return nd;
                    }
                }
                catch (System.Exception ex)
                {
                    report.AppendLine("- nominal_diameter_ctor: `FAIL " + EscapeMarkdown(DescribeArgs(args)) + " " + EscapeMarkdown(ex.GetType().Name) + ":" + EscapeMarkdown(ex.Message) + "`");
                }
            }

            try
            {
                object nd = Activator.CreateInstance(nominalDiameterType);
                if (TrySetPartSizePropertyObject(nd, "Value", dn, report) ||
                    TrySetPartSizePropertyObject(nd, "NominalDiameter", dn, report) ||
                    TrySetPartSizePropertyObject(nd, "ND", dn, report))
                {
                    report.AppendLine("- nominal_diameter_default_ctor: `OK property_set`");
                    return nd;
                }
                TryDisposeObject(nd, report, "nominal_diameter_default_ctor_unused");
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- nominal_diameter_default_ctor: `FAIL " + EscapeMarkdown(ex.GetType().Name) + ":" + EscapeMarkdown(ex.Message) + "`");
            }

            return null;
        }

        private static List<object[]> BuildNominalDiameterConstructorArgs(double dn)
        {
            return new List<object[]>
            {
                new object[] { dn },
                new object[] { Convert.ToInt32(dn, CultureInfo.InvariantCulture) },
                new object[] { dn.ToString(CultureInfo.InvariantCulture) },
                new object[] { dn, "mm" },
                new object[] { dn.ToString(CultureInfo.InvariantCulture), "mm" }
            };
        }

        private static bool InvokeInitParameterList(object supportInfo, object nominalDiameter, StringBuilder report)
        {
            try
            {
                MethodInfo method = FindMethodByNameAndArity(supportInfo.GetType(), "InitParameterList", 1);
                if (method == null)
                {
                    report.AppendLine("- InitParameterList: `FAIL method_not_found`");
                    return false;
                }
                method.Invoke(supportInfo, new[] { nominalDiameter });
                report.AppendLine("- InitParameterList: `OK " + EscapeMarkdown(method.ToString()) + "`");
                return true;
            }
            catch (TargetInvocationException ex)
            {
                report.AppendLine("- InitParameterList: `FAIL TargetInvocationException:" + EscapeMarkdown((ex.InnerException ?? ex).Message) + "`");
                return false;
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- InitParameterList: `FAIL " + EscapeMarkdown(ex.GetType().Name) + ":" + EscapeMarkdown(ex.Message) + "`");
                return false;
            }
        }

        private static object InvokeMakePart(object supportInfo, StringBuilder report)
        {
            try
            {
                MethodInfo method = FindMethodByNameAndArity(supportInfo.GetType(), "MakePart", 0);
                if (method == null)
                {
                    report.AppendLine("- MakePart: `FAIL method_not_found`");
                    return null;
                }
                object psp = method.Invoke(supportInfo, null);
                report.AppendLine("- MakePart: `" + (psp == null ? "FAIL returned_null" : "OK " + EscapeMarkdown(psp.GetType().AssemblyQualifiedName)) + "`");
                return psp;
            }
            catch (TargetInvocationException ex)
            {
                report.AppendLine("- MakePart: `FAIL TargetInvocationException:" + EscapeMarkdown((ex.InnerException ?? ex).Message) + "`");
                return null;
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- MakePart: `FAIL " + EscapeMarkdown(ex.GetType().Name) + ":" + EscapeMarkdown(ex.Message) + "`");
                return null;
            }
        }

        private static bool TrySetCreateSpikePropValue(object partSizeProperties, string name, object value, StringBuilder report)
        {
            try
            {
                MethodInfo method = partSizeProperties.GetType().GetMethod("SetPropValue", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(object) }, null)
                    ?? FindMethodByNameAndArity(partSizeProperties.GetType(), "SetPropValue", 2);
                if (method == null)
                {
                    report.AppendLine("- SetPropValue." + name + ": `FAIL method_not_found`");
                    return false;
                }
                method.Invoke(partSizeProperties, new[] { name, value });
                report.AppendLine("- SetPropValue." + name + ": `OK " + EscapeMarkdown(Convert.ToString(value, CultureInfo.InvariantCulture)) + "`");
                return true;
            }
            catch (TargetInvocationException ex)
            {
                report.AppendLine("- SetPropValue." + name + ": `FAIL TargetInvocationException:" + EscapeMarkdown((ex.InnerException ?? ex).Message) + "`");
                return false;
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- SetPropValue." + name + ": `FAIL " + EscapeMarkdown(ex.GetType().Name) + ":" + EscapeMarkdown(ex.Message) + "`");
                return false;
            }
        }

        private static bool TrySetPartSizePropertyObject(object obj, string propertyName, object value, StringBuilder report)
        {
            try
            {
                PropertyInfo property = obj?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property == null || !property.CanWrite)
                {
                    return false;
                }
                property.SetValue(obj, value, null);
                report.AppendLine("- property." + propertyName + ": `OK`");
                return true;
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- property." + propertyName + ": `FAIL " + EscapeMarkdown(ex.GetType().Name) + ":" + EscapeMarkdown(ex.Message) + "`");
                return false;
            }
        }

        private static object InvokeCreateSupportEntity(Type helperType, object supportInfo, Database db, ref object partSizeProperties, out string overload, StringBuilder report)
        {
            overload = "";
            MethodInfo primary = FindCreateSupportEntity(helperType, supportInfo.GetType(), typeof(Database), partSizeProperties.GetType(), true);
            if (primary != null)
            {
                try
                {
                    object[] args = { supportInfo, db, partSizeProperties };
                    object result = primary.Invoke(null, args);
                    partSizeProperties = args[2];
                    overload = primary.ToString();
                    return result;
                }
                catch (TargetInvocationException ex)
                {
                    report.AppendLine("- CreateSupportEntity.primary: `FAIL TargetInvocationException:" + EscapeMarkdown((ex.InnerException ?? ex).Message) + "`");
                }
                catch (System.Exception ex)
                {
                    report.AppendLine("- CreateSupportEntity.primary: `FAIL " + EscapeMarkdown(ex.GetType().Name) + ":" + EscapeMarkdown(ex.Message) + "`");
                }
            }
            else
            {
                report.AppendLine("- CreateSupportEntity.primary: `FAIL method_not_found`");
            }

            MethodInfo fallback = FindCreateSupportEntity(helperType, null, typeof(Database), partSizeProperties.GetType(), false);
            if (fallback == null)
            {
                report.AppendLine("- CreateSupportEntity.fallback: `FAIL method_not_found`");
                return null;
            }

            try
            {
                object result = fallback.Invoke(null, new[] { partSizeProperties, db });
                overload = fallback.ToString();
                return result;
            }
            catch (TargetInvocationException ex)
            {
                report.AppendLine("- CreateSupportEntity.fallback: `FAIL TargetInvocationException:" + EscapeMarkdown((ex.InnerException ?? ex).Message) + "`");
                return null;
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- CreateSupportEntity.fallback: `FAIL " + EscapeMarkdown(ex.GetType().Name) + ":" + EscapeMarkdown(ex.Message) + "`");
                return null;
            }
        }

        private static MethodInfo FindCreateSupportEntity(Type helperType, Type supportInfoType, Type databaseType, Type pspType, bool supportInfoOverload)
        {
            foreach (MethodInfo method in SafeGetMethods(helperType, new StringBuilder(), "SupportHelper"))
            {
                if (!method.Name.Equals("CreateSupportEntity", StringComparison.Ordinal))
                {
                    continue;
                }
                ParameterInfo[] parameters = method.GetParameters();
                if (supportInfoOverload)
                {
                    if (parameters.Length == 3 &&
                        ParametersAssignable(parameters[0].ParameterType, supportInfoType) &&
                        ParametersAssignable(parameters[1].ParameterType, databaseType) &&
                        IsPartSizePropertiesParameter(parameters[2].ParameterType, pspType))
                    {
                        return method;
                    }
                }
                else if (parameters.Length == 2 &&
                    IsPartSizePropertiesParameter(parameters[0].ParameterType, pspType) &&
                    ParametersAssignable(parameters[1].ParameterType, databaseType))
                {
                    return method;
                }
            }
            return null;
        }

        private static bool ParametersAssignable(Type parameterType, Type valueType)
        {
            if (parameterType == null || valueType == null)
            {
                return false;
            }
            Type effective = parameterType.IsByRef ? parameterType.GetElementType() : parameterType;
            return effective.IsAssignableFrom(valueType);
        }

        private static bool IsPartSizePropertiesParameter(Type parameterType, Type pspType)
        {
            Type effective = parameterType.IsByRef ? parameterType.GetElementType() : parameterType;
            return ParametersAssignable(effective, pspType) ||
                (effective.FullName ?? "").IndexOf("PartSizeProperties", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static ObjectId AppendEntityToModelSpace(Database db, Transaction tr, Entity entity, StringBuilder report)
        {
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead, false);
            BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false);
            ObjectId id = modelSpace.AppendEntity(entity);
            tr.AddNewlyCreatedDBObject(entity, true);
            report.AppendLine("- append_status: `OK`");
            return id;
        }

        private static MethodInfo FindMethodByNameAndArity(Type type, string name, int arity)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                if (method.Name.Equals(name, StringComparison.Ordinal) && method.GetParameters().Length == arity)
                {
                    return method;
                }
            }
            return null;
        }

        private static string DescribeArgs(object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return "no_args";
            }
            var parts = new List<string>();
            foreach (object arg in args)
            {
                parts.Add(arg == null ? "null" : arg.GetType().Name + "=" + Convert.ToString(arg, CultureInfo.InvariantCulture));
            }
            return string.Join(",", parts);
        }

        private static void TryDisposeObject(object obj, StringBuilder report, string label)
        {
            if (obj == null)
            {
                return;
            }
            try
            {
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                    report.AppendLine("- dispose." + label + ": `OK`");
                }
            }
            catch (System.Exception ex)
            {
                report.AppendLine("- dispose." + label + ": `FAIL " + EscapeMarkdown(ex.GetType().Name) + ":" + EscapeMarkdown(ex.Message) + "`");
            }
        }

        private static void AppendCreateStep2Gate(bool createSuccess, int realMeshTriangles, bool abortCountUnchanged, StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("## 8. Step 2 gate");
            report.AppendLine("- create_success: `" + createSuccess.ToString(CultureInfo.InvariantCulture) + "`");
            report.AppendLine("- real_mesh_triangles: `" + realMeshTriangles.ToString(CultureInfo.InvariantCulture) + "`");
            report.AppendLine("- abort_after_entity_count_unchanged: `" + abortCountUnchanged.ToString(CultureInfo.InvariantCulture) + "`");
            string verdict = createSuccess && realMeshTriangles > 0 && abortCountUnchanged ? "PASS" : "PENDING_OR_FAIL";
            report.AppendLine("- verdict: `" + verdict + "`");
            report.AppendLine("- pass_condition: `create_success && real_mesh_triangles > 0 && abort_after_entity_count_unchanged`");
        }

        private static void WriteCreateSpikeReport(string path, string text, Editor ed)
        {
            File.WriteAllText(path, text, Encoding.UTF8);
            ed?.WriteMessage("\n[PFSCREATESPIKE] report: " + path + "\n");
        }
    }
}
