using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

#nullable disable

namespace PlantFlow_Support
{
    public sealed class MeshData
    {
        public List<double[]> Vertices { get; } = new List<double[]>();
        public List<int[]> Triangles { get; } = new List<int[]>();
        public string Units { get; set; } = "mm";
        public string UpAxis { get; set; } = "Z";
    }

    internal static class SupportMeshExtractor
    {
        public static bool TryExtract(Entity entity, Transaction tr, out MeshData mesh, out string diag)
        {
            mesh = new MeshData();
            var lines = new List<string>();
            var candidates = new List<Entity>();
            var owned = new HashSet<Entity>();

            try
            {
                if (entity == null)
                {
                    diag = "entity_null";
                    return false;
                }
                if (tr == null)
                {
                    diag = "transaction_null";
                    return false;
                }

                CollectGeometry(entity, tr, "root", candidates, owned, lines);
                lines.Add("candidates=" + candidates.Count.ToString(CultureInfo.InvariantCulture));

                foreach (Entity candidate in candidates)
                {
                    int before = mesh.Triangles.Count;
                    if (TryGetObjectMesh(candidate, mesh, lines))
                    {
                        lines.Add("GetObjectMesh triangles=" + (mesh.Triangles.Count - before).ToString(CultureInfo.InvariantCulture));
                        continue;
                    }
                    if (TryCaptureExistingMesh(candidate, mesh, lines))
                    {
                        lines.Add("existing mesh triangles=" + (mesh.Triangles.Count - before).ToString(CultureInfo.InvariantCulture));
                    }
                }

                diag = string.Join("; ", lines);
                return mesh.Triangles.Count > 0;
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
                foreach (Entity ownedEntity in owned)
                {
                    try
                    {
                        ownedEntity.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("[PFS-SupportMeshExtractor] Dispose failed: " + ex.Message);
                    }
                }
            }
        }

        private static void CollectGeometry(Entity entity, Transaction tr, string path, List<Entity> candidates, HashSet<Entity> owned, List<string> lines)
        {
            if (IsMeshCandidate(entity))
            {
                candidates.Add(entity);
                lines.Add(path + "=" + entity.GetType().Name);
                return;
            }

            if (entity is BlockReference br)
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead, false);
                int index = 0;
                foreach (ObjectId childId in btr)
                {
                    DBObject child = tr.GetObject(childId, OpenMode.ForRead, false);
                    if (child is Entity childEntity && !childEntity.IsErased)
                    {
                        Entity transformed = null;
                        try
                        {
                            transformed = childEntity.GetTransformedCopy(br.BlockTransform);
                            owned.Add(transformed);
                            CollectGeometry(transformed, tr, path + "/block[" + index.ToString(CultureInfo.InvariantCulture) + "]", candidates, owned, lines);
                        }
                        catch (Exception ex)
                        {
                            lines.Add(path + "/block[" + index.ToString(CultureInfo.InvariantCulture) + "] transform FAIL " + ex.Message);
                            if (transformed != null && !owned.Contains(transformed))
                            {
                                transformed.Dispose();
                            }
                        }
                    }
                    index++;
                }
                return;
            }

            var exploded = new DBObjectCollection();
            try
            {
                entity.Explode(exploded);
                lines.Add(path + " explode=" + exploded.Count.ToString(CultureInfo.InvariantCulture));
                int index = 0;
                foreach (DBObject child in exploded)
                {
                    if (child is Entity childEntity)
                    {
                        owned.Add(childEntity);
                        CollectGeometry(childEntity, tr, path + "/explode[" + index.ToString(CultureInfo.InvariantCulture) + "]", candidates, owned, lines);
                    }
                    else if (child != null)
                    {
                        child.Dispose();
                    }
                    index++;
                }
            }
            catch (Exception ex)
            {
                lines.Add(path + " explode FAIL " + ex.Message);
                foreach (DBObject child in exploded)
                {
                    if (child != null && !owned.Contains(child as Entity))
                    {
                        child.Dispose();
                    }
                }
            }
        }

        private static bool IsMeshCandidate(Entity entity)
        {
            string name = entity.GetType().Name;
            string fullName = entity.GetType().FullName ?? "";
            return entity is Solid3d
                || name == "SubDMesh"
                || name == "PolyFaceMesh"
                || name == "Region"
                || name == "Body"
                || name.EndsWith("Surface", StringComparison.Ordinal)
                || fullName.IndexOf(".Surface", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TryGetObjectMesh(Entity entity, MeshData mesh, List<string> lines)
        {
            try
            {
                Type subDMeshType = typeof(SubDMesh);
                Type faceterType = Type.GetType("Autodesk.AutoCAD.DatabaseServices.MeshFaceterData, AcDbMgd", false)
                    ?? FindLoadedType("Autodesk.AutoCAD.DatabaseServices.MeshFaceterData");
                if (faceterType == null)
                {
                    lines.Add("MeshFaceterData type_not_found");
                    return false;
                }

                MethodInfo method = null;
                foreach (MethodInfo candidate in subDMeshType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (!candidate.Name.Equals("GetObjectMesh", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    ParameterInfo[] ps = candidate.GetParameters();
                    if (ps.Length == 2 && ps[0].ParameterType.IsAssignableFrom(typeof(DBObject)) && ps[1].ParameterType == faceterType)
                    {
                        method = candidate;
                        break;
                    }
                }
                if (method == null)
                {
                    lines.Add("GetObjectMesh method_not_found");
                    return false;
                }

                object faceter = Activator.CreateInstance(faceterType);
                object target = method.IsStatic ? null : Activator.CreateInstance(subDMeshType);
                object result = method.Invoke(target, new[] { entity, faceter });
                int added = CaptureMeshObject(result, mesh);
                TryDispose(faceter, lines, "faceter");
                TryDispose(target, lines, "subdmesh_target");
                TryDispose(result, lines, "mesh_result");
                return added > 0;
            }
            catch (TargetInvocationException ex)
            {
                lines.Add("GetObjectMesh FAIL " + (ex.InnerException ?? ex).Message);
                return false;
            }
            catch (Exception ex)
            {
                lines.Add("GetObjectMesh FAIL " + ex.Message);
                return false;
            }
        }

        private static bool TryCaptureExistingMesh(Entity entity, MeshData mesh, List<string> lines)
        {
            try
            {
                int added = CaptureMeshObject(entity, mesh);
                return added > 0;
            }
            catch (Exception ex)
            {
                lines.Add("existing mesh capture FAIL " + ex.Message);
                return false;
            }
        }

        private static int CaptureMeshObject(object meshLike, MeshData mesh)
        {
            if (meshLike == null)
            {
                return 0;
            }

            int total = 0;
            if (meshLike is IEnumerable enumerable && !(meshLike is string))
            {
                foreach (object item in enumerable)
                {
                    total += CaptureSingleMeshObject(item, mesh);
                }
                return total;
            }
            return CaptureSingleMeshObject(meshLike, mesh);
        }

        private static int CaptureSingleMeshObject(object meshLike, MeshData mesh)
        {
            Point3d[] points = ReadPointArray(meshLike);
            int[] indices = ReadIndexArray(meshLike);
            if (points.Length == 0 || indices.Length < 3)
            {
                return 0;
            }
            return AddMeshIndexData(mesh, points, indices);
        }

        private static Point3d[] ReadPointArray(object obj)
        {
            foreach (string name in new[] { "VertexArray", "Vertices", "PointArray", "Points" })
            {
                Point3d[] points = ConvertToPointArray(ReadProperty(obj, name) ?? InvokeMaybe(obj, "Get" + name, null));
                if (points.Length > 0)
                {
                    return points;
                }
            }

            var indexed = new List<Point3d>();
            for (int i = 0; i < 100000; i++)
            {
                object value = InvokeMaybe(obj, "GetVertexAt", new object[] { i });
                if (value is Point3d point)
                {
                    indexed.Add(point);
                    continue;
                }
                break;
            }
            return indexed.ToArray();
        }

        private static int[] ReadIndexArray(object obj)
        {
            foreach (string name in new[] { "FaceArray", "Faces", "FaceIndices", "Indices", "IndexArray" })
            {
                int[] indices = ConvertToIntArray(ReadProperty(obj, name) ?? InvokeMaybe(obj, "Get" + name, null));
                if (indices.Length >= 3)
                {
                    return indices;
                }
            }
            return Array.Empty<int>();
        }

        private static int AddMeshIndexData(MeshData mesh, Point3d[] points, int[] indices)
        {
            int counted = AddCountPrefixedFaces(mesh, points, indices);
            if (counted > 0)
            {
                return counted;
            }

            int baseIndex = mesh.Vertices.Count;
            foreach (Point3d point in points)
            {
                mesh.Vertices.Add(new[] { point.X, point.Y, point.Z });
            }

            int added = 0;
            for (int i = 0; i + 2 < indices.Length; i += 3)
            {
                int a = indices[i];
                int b = indices[i + 1];
                int c = indices[i + 2];
                if (a < 0 || b < 0 || c < 0 || a >= points.Length || b >= points.Length || c >= points.Length)
                {
                    continue;
                }
                mesh.Triangles.Add(new[] { baseIndex + a, baseIndex + b, baseIndex + c });
                added++;
            }
            return added;
        }

        private static int AddCountPrefixedFaces(MeshData mesh, Point3d[] points, int[] indices)
        {
            int cursor = 0;
            int baseIndex = mesh.Vertices.Count;
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
            foreach (Point3d point in points)
            {
                mesh.Vertices.Add(new[] { point.X, point.Y, point.Z });
            }
            mesh.Triangles.AddRange(localTriangles);
            return localTriangles.Count;
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PFS-SupportMeshExtractor] ReadProperty failed: " + ex.Message);
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PFS-SupportMeshExtractor] InvokeMaybe failed: " + ex.Message);
                return null;
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

        private static void TryDispose(object obj, List<string> lines, string label)
        {
            try
            {
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                lines.Add(label + " dispose FAIL " + ex.Message);
            }
        }
    }
}
