using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

#nullable disable

namespace PlantFlow_Support
{
    internal static class SupportPreviewBuilder
    {
        private const string PreviewLayerName = "_PFS_PREVIEW_MESH_ABORT";

        public static bool TryBuildMesh(Document doc, string type, IDictionary<string, string> parameters, out MeshData mesh, out string diag)
        {
            mesh = null;
            var lines = new List<string>();
            object nominalDiameter = null;
            object partSizeProperties = null;

            try
            {
                if (doc == null)
                {
                    diag = "document_null";
                    return false;
                }
                Database db = doc.Database;
                if (db == null)
                {
                    diag = "database_null";
                    return false;
                }
                if (ReadDatabaseReadOnlyFlag(db, lines))
                {
                    diag = "database_readonly; " + string.Join("; ", lines);
                    return false;
                }
                if (string.IsNullOrWhiteSpace(type))
                {
                    diag = "support_type_empty";
                    return false;
                }

                Type helperType = FindSupportHelperType();
                if (helperType == null)
                {
                    diag = "SupportHelper type_not_found";
                    return false;
                }

                object supportInfo = InvokeFindSupportInfo(helperType, type.Trim(), lines);
                if (supportInfo == null)
                {
                    diag = string.Join("; ", lines);
                    return false;
                }

                double dn = ReadDouble(parameters, "Dn", 80.0);
                nominalDiameter = CreateNominalDiameter(dn, lines);
                if (nominalDiameter == null)
                {
                    diag = string.Join("; ", lines);
                    return false;
                }

                if (!InvokeInitParameterList(supportInfo, nominalDiameter, lines))
                {
                    diag = string.Join("; ", lines);
                    return false;
                }

                partSizeProperties = InvokeMakePart(supportInfo, lines);
                if (partSizeProperties == null)
                {
                    diag = string.Join("; ", lines);
                    return false;
                }

                TrySetProperty(partSizeProperties, "NominalDiameter", nominalDiameter, lines);
                ApplyParameters(partSizeProperties, parameters, lines);

                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    int before = CountModelSpaceEntities(db, tr);
                    EnsurePreviewLayer(db, tr, lines);

                    object created = InvokeCreateSupportEntity(helperType, supportInfo, db, ref partSizeProperties, lines);
                    if (!(created is Entity entity))
                    {
                        TryDispose(created, lines, "created_non_entity");
                        tr.Abort();
                        diag = "CreateSupportEntity did not return Entity; " + string.Join("; ", lines);
                        return false;
                    }

                    ObjectId id = AppendEntity(db, tr, entity, lines);
                    lines.Add("appended=" + id.ToString());
                    int afterAppend = CountModelSpaceEntities(db, tr);
                    lines.Add("count_before=" + before.ToString(CultureInfo.InvariantCulture));
                    lines.Add("count_after_append=" + afterAppend.ToString(CultureInfo.InvariantCulture));

                    bool extracted = SupportMeshExtractor.TryExtract(entity, tr, out mesh, out string extractDiag);
                    lines.Add("extract=" + extracted.ToString(CultureInfo.InvariantCulture));
                    lines.Add(extractDiag);

                    tr.Abort();
                    lines.Add("transaction=Abort");

                    using (Transaction verify = db.TransactionManager.StartTransaction())
                    {
                        int afterAbort = CountModelSpaceEntities(db, verify);
                        lines.Add("count_after_abort=" + afterAbort.ToString(CultureInfo.InvariantCulture));
                        lines.Add("count_unchanged=" + (afterAbort == before).ToString(CultureInfo.InvariantCulture));
                        verify.Abort();
                        if (afterAbort != before)
                        {
                            diag = string.Join("; ", lines);
                            return false;
                        }
                    }

                    diag = string.Join("; ", lines);
                    return extracted && mesh != null && mesh.Triangles.Count > 0;
                }
            }
            catch (TargetInvocationException ex)
            {
                diag = "TargetInvocationException: " + (ex.InnerException ?? ex).Message + "; " + string.Join("; ", lines);
                return false;
            }
            catch (Exception ex)
            {
                diag = ex.GetType().Name + ": " + ex.Message + "; " + string.Join("; ", lines);
                return false;
            }
            finally
            {
                TryDispose(partSizeProperties, lines, "part_size_properties");
                TryDispose(nominalDiameter, lines, "nominal_diameter");
            }
        }

        private static void ApplyParameters(object partSizeProperties, IDictionary<string, string> parameters, List<string> lines)
        {
            var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Dn"] = "80",
                ["BI"] = "210",
                ["A"] = "800",
                ["A1"] = "150",
                ["F2"] = "900",
                ["P1"] = "0",
                ["TY"] = "-2"
            };

            if (parameters != null)
            {
                foreach (KeyValuePair<string, string> pair in parameters)
                {
                    // 빈/공백 값은 스킵 — 텍스트박스 미프리필 시 빈값이 유효 기본값을 덮어 서포트 생성이 폴백 형상이 되는 것 방지.
                    if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                    {
                        merged[pair.Key.Trim()] = pair.Value;
                    }
                }
            }

            foreach (KeyValuePair<string, string> pair in merged)
            {
                object value = ParseParameterValue(pair.Value);
                TrySetPropValue(partSizeProperties, pair.Key, value, lines);
            }
        }

        private static object ParseParameterValue(string text)
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            {
                return d;
            }
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out d))
            {
                return d;
            }
            return text ?? "";
        }

        private static double ReadDouble(IDictionary<string, string> parameters, string key, double fallback)
        {
            if (parameters != null && parameters.TryGetValue(key, out string text))
            {
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                {
                    return d;
                }
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out d))
                {
                    return d;
                }
            }
            return fallback;
        }

        private static Type FindSupportHelperType()
        {
            return Type.GetType("Autodesk.ProcessPower.PnP3dPipeSupport.SupportHelper, PnP3dPipeSupport", false)
                ?? Type.GetType("Autodesk.ProcessPower.PnP3dPipeSupport.SupportHelper, PnP3dPipeSupportsUI", false)
                ?? Type.GetType("Autodesk.ProcessPower.PnP3dPipeSupport.SupportHelper, PnP3dPipeUI", false)
                ?? FindLoadedType("Autodesk.ProcessPower.PnP3dPipeSupport.SupportHelper");
        }

        private static object InvokeFindSupportInfo(Type helperType, string type, List<string> lines)
        {
            try
            {
                MethodInfo method = helperType.GetMethod("FindSupportInfo", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                object result = method?.Invoke(null, new object[] { type });
                lines.Add("FindSupportInfo=" + (result == null ? "null" : "OK"));
                return result;
            }
            catch (TargetInvocationException ex)
            {
                lines.Add("FindSupportInfo FAIL " + (ex.InnerException ?? ex).Message);
                return null;
            }
            catch (Exception ex)
            {
                lines.Add("FindSupportInfo FAIL " + ex.Message);
                return null;
            }
        }

        private static object CreateNominalDiameter(double dn, List<string> lines)
        {
            Type type = FindLoadedType("Autodesk.ProcessPower.PartsRepository.NominalDiameter")
                ?? Type.GetType("Autodesk.ProcessPower.PartsRepository.NominalDiameter, PnP3dPartsRepository", false);
            if (type == null)
            {
                lines.Add("NominalDiameter type_not_found");
                return null;
            }

            foreach (object[] args in new[]
            {
                new object[] { dn },
                new object[] { Convert.ToInt32(dn, CultureInfo.InvariantCulture) },
                new object[] { dn.ToString(CultureInfo.InvariantCulture) }
            })
            {
                try
                {
                    object nd = Activator.CreateInstance(type, args);
                    lines.Add("NominalDiameter ctor OK " + args[0]);
                    return nd;
                }
                catch (Exception ex)
                {
                    lines.Add("NominalDiameter ctor FAIL " + ex.GetType().Name + ":" + ex.Message);
                }
            }
            return null;
        }

        private static bool InvokeInitParameterList(object supportInfo, object nominalDiameter, List<string> lines)
        {
            try
            {
                MethodInfo method = FindMethod(supportInfo.GetType(), "InitParameterList", 1);
                method?.Invoke(supportInfo, new[] { nominalDiameter });
                lines.Add("InitParameterList=" + (method == null ? "method_not_found" : "OK"));
                return method != null;
            }
            catch (TargetInvocationException ex)
            {
                lines.Add("InitParameterList FAIL " + (ex.InnerException ?? ex).Message);
                return false;
            }
            catch (Exception ex)
            {
                lines.Add("InitParameterList FAIL " + ex.Message);
                return false;
            }
        }

        private static object InvokeMakePart(object supportInfo, List<string> lines)
        {
            try
            {
                MethodInfo method = FindMethod(supportInfo.GetType(), "MakePart", 0);
                object result = method?.Invoke(supportInfo, null);
                lines.Add("MakePart=" + (result == null ? "null" : "OK"));
                return result;
            }
            catch (TargetInvocationException ex)
            {
                lines.Add("MakePart FAIL " + (ex.InnerException ?? ex).Message);
                return null;
            }
            catch (Exception ex)
            {
                lines.Add("MakePart FAIL " + ex.Message);
                return null;
            }
        }

        private static object InvokeCreateSupportEntity(Type helperType, object supportInfo, Database db, ref object partSizeProperties, List<string> lines)
        {
            MethodInfo primary = FindCreateSupportEntity(helperType, supportInfo.GetType(), typeof(Database), partSizeProperties.GetType(), true);
            if (primary != null)
            {
                try
                {
                    object[] args = { supportInfo, db, partSizeProperties };
                    object result = primary.Invoke(null, args);
                    partSizeProperties = args[2];
                    lines.Add("CreateSupportEntity primary OK");
                    return result;
                }
                catch (TargetInvocationException ex)
                {
                    lines.Add("CreateSupportEntity primary FAIL " + (ex.InnerException ?? ex).Message);
                }
                catch (Exception ex)
                {
                    lines.Add("CreateSupportEntity primary FAIL " + ex.Message);
                }
            }

            MethodInfo fallback = FindCreateSupportEntity(helperType, null, typeof(Database), partSizeProperties.GetType(), false);
            if (fallback == null)
            {
                lines.Add("CreateSupportEntity fallback method_not_found");
                return null;
            }

            try
            {
                object result = fallback.Invoke(null, new[] { partSizeProperties, db });
                lines.Add("CreateSupportEntity fallback OK");
                return result;
            }
            catch (TargetInvocationException ex)
            {
                lines.Add("CreateSupportEntity fallback FAIL " + (ex.InnerException ?? ex).Message);
                return null;
            }
            catch (Exception ex)
            {
                lines.Add("CreateSupportEntity fallback FAIL " + ex.Message);
                return null;
            }
        }

        private static MethodInfo FindCreateSupportEntity(Type helperType, Type supportInfoType, Type databaseType, Type pspType, bool supportInfoOverload)
        {
            foreach (MethodInfo method in helperType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (!method.Name.Equals("CreateSupportEntity", StringComparison.Ordinal))
                {
                    continue;
                }
                ParameterInfo[] ps = method.GetParameters();
                if (supportInfoOverload && ps.Length == 3 &&
                    IsAssignable(ps[0].ParameterType, supportInfoType) &&
                    IsAssignable(ps[1].ParameterType, databaseType) &&
                    IsPartSizeProperties(ps[2].ParameterType, pspType))
                {
                    return method;
                }
                if (!supportInfoOverload && ps.Length == 2 &&
                    IsPartSizeProperties(ps[0].ParameterType, pspType) &&
                    IsAssignable(ps[1].ParameterType, databaseType))
                {
                    return method;
                }
            }
            return null;
        }

        private static bool IsAssignable(Type parameterType, Type valueType)
        {
            if (parameterType == null || valueType == null)
            {
                return false;
            }
            Type effective = parameterType.IsByRef ? parameterType.GetElementType() : parameterType;
            return effective.IsAssignableFrom(valueType);
        }

        private static bool IsPartSizeProperties(Type parameterType, Type pspType)
        {
            Type effective = parameterType.IsByRef ? parameterType.GetElementType() : parameterType;
            return IsAssignable(effective, pspType) ||
                (effective.FullName ?? "").IndexOf("PartSizeProperties", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void TrySetPropValue(object partSizeProperties, string name, object value, List<string> lines)
        {
            try
            {
                MethodInfo method = FindMethod(partSizeProperties.GetType(), "SetPropValue", 2);
                method?.Invoke(partSizeProperties, new[] { name, value });
                lines.Add("SetPropValue." + name + "=" + (method == null ? "method_not_found" : "OK"));
            }
            catch (TargetInvocationException ex)
            {
                lines.Add("SetPropValue." + name + " FAIL " + (ex.InnerException ?? ex).Message);
            }
            catch (Exception ex)
            {
                lines.Add("SetPropValue." + name + " FAIL " + ex.Message);
            }
        }

        private static bool TrySetProperty(object obj, string propertyName, object value, List<string> lines)
        {
            try
            {
                PropertyInfo property = obj?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property == null || !property.CanWrite)
                {
                    return false;
                }
                property.SetValue(obj, value, null);
                lines.Add("property." + propertyName + "=OK");
                return true;
            }
            catch (Exception ex)
            {
                lines.Add("property." + propertyName + " FAIL " + ex.Message);
                return false;
            }
        }

        private static void EnsurePreviewLayer(Database db, Transaction tr, List<string> lines)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead, false);
            if (!lt.Has(PreviewLayerName))
            {
                lt.UpgradeOpen();
                var layer = new LayerTableRecord
                {
                    Name = PreviewLayerName,
                    IsOff = true,
                    IsFrozen = false,
                    IsLocked = false
                };
                lt.Add(layer);
                tr.AddNewlyCreatedDBObject(layer, true);
                lines.Add("preview_layer=created_off");
                return;
            }

            LayerTableRecord existing = (LayerTableRecord)tr.GetObject(lt[PreviewLayerName], OpenMode.ForWrite, false);
            existing.IsOff = true;
            existing.IsFrozen = false;
            lines.Add("preview_layer=reused_off");
        }

        private static ObjectId AppendEntity(Database db, Transaction tr, Entity entity, List<string> lines)
        {
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead, false);
            BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false);
            entity.Layer = PreviewLayerName;
            ObjectId id = ms.AppendEntity(entity);
            tr.AddNewlyCreatedDBObject(entity, true);
            lines.Add("append=OK");
            return id;
        }

        private static int CountModelSpaceEntities(Database db, Transaction tr)
        {
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead, false);
            BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead, false);
            int count = 0;
            foreach (ObjectId ignored in ms)
            {
                count++;
            }
            return count;
        }

        private static bool ReadDatabaseReadOnlyFlag(Database db, List<string> lines)
        {
            try
            {
                PropertyInfo property = db.GetType().GetProperty("IsReadOnly", BindingFlags.Public | BindingFlags.Instance);
                object value = property?.GetValue(db, null);
                if (value is bool readOnly)
                {
                    lines.Add("database_readonly=" + readOnly.ToString(CultureInfo.InvariantCulture));
                    return readOnly;
                }
            }
            catch (Exception ex)
            {
                lines.Add("database_readonly_probe FAIL " + ex.Message);
            }
            return false;
        }

        private static MethodInfo FindMethod(Type type, string name, int arity)
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

        private static void TryDispose(object obj, List<string> lines, string label)
        {
            try
            {
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                    lines.Add("dispose." + label + "=OK");
                }
            }
            catch (Exception ex)
            {
                lines.Add("dispose." + label + " FAIL " + ex.Message);
            }
        }
    }
}
