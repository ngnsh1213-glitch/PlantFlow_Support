using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlantFlow_Support
{
    internal static class CatalogBridge
    {
        private static readonly object CatalogLock = new object();
        private static readonly object DbLock = new object();
        private static CatalogManager _catalog;

        public static bool TryDispatch(WebViewControl owner, string message, Action<string> post, Action<string> log)
        {
            CatalogEnvelope request;
            try
            {
                request = JsonConvert.DeserializeObject<CatalogEnvelope>(message);
            }
            catch (Exception ex)
            {
                log("CatalogBridge parse 실패: " + ex.Message);
                return false;
            }

            if (request == null || request.Ch != "catalog")
                return false;

            if (request.Method == "preview")
            {
                DispatchPreview(owner, request, post, log);
                return true;
            }

            Task.Run(() =>
            {
                try
                {
                    object result = Execute(request.Method, request.Args ?? new JArray());
                    return Response.Ok(request.Id, result);
                }
                catch (Exception ex)
                {
                    log("CatalogBridge dispatch 실패: " + ex.Message);
                    return Response.Fail(request.Id, ex.Message);
                }
            }).ContinueWith(task =>
            {
                PostOnUi(owner, post, task.Result);
            });

            return true;
        }

        private static void DispatchPreview(WebViewControl owner, CatalogEnvelope request, Action<string> post, Action<string> log)
        {
            if (owner == null || owner.IsDisposed) return;
            Action run = () =>
            {
                try
                {
                    JObject payload = Arg<JObject>(request.Args, 0);
                    string type = payload.Value<string>("type") ?? "";
                    var parameters = payload["params"] is JObject paramObject
                        ? paramObject.Properties().ToDictionary(p => p.Name, p => Convert.ToString(p.Value, CultureInfo.InvariantCulture) ?? "", StringComparer.OrdinalIgnoreCase)
                        : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    owner.LoadSupportMesh(type, parameters);
                    post(JsonConvert.SerializeObject(Response.Ok(request.Id, new { ok = true, message = "Preview mesh request queued." })));
                }
                catch (Exception ex)
                {
                    log("CatalogBridge preview 실패: " + ex.Message);
                    post(JsonConvert.SerializeObject(Response.Fail(request.Id, ex.Message)));
                }
            };
            if (owner.InvokeRequired) owner.BeginInvoke(run);
            else run();
        }

        private static object Execute(string method, JArray args)
        {
            switch (method)
            {
                case "listTypes":
                    return GetCatalog().ListTypes().Select(ToCatalogType).ToList();
                case "listVariants":
                    return GetCatalog().ListVariants(Arg<string>(args, 0)).Select(ToVariantRow).ToList();
                case "getParamKeys":
                    return GetCatalog().GetParamKeys(Arg<string>(args, 0)).Select(ToParameterDef).ToList();
                case "addVariant":
                    lock (DbLock)
                        return ToResult(GetCatalog().AddVariant(Arg<string>(args, 0), ToVariantInput(Arg<JObject>(args, 1))));
                case "updateVariant":
                    lock (DbLock)
                        return ToResult(GetCatalog().UpdateVariant(Arg<long>(args, 0), Arg<string>(args, 1), ToVariantInput(Arg<JObject>(args, 2))));
                case "deleteVariant":
                    lock (DbLock)
                        return ToResult(GetCatalog().DeleteVariant(Arg<long>(args, 0)));
                case "loadAcat":
                    lock (CatalogLock)
                    {
                        _catalog = CreateCatalog();
                        return new { ok = true, message = "Catalog loaded: " + _catalog.AcatPath };
                    }
                default:
                    throw new InvalidOperationException("Unknown catalog method: " + method);
            }
        }

        private static CatalogManager GetCatalog()
        {
            lock (CatalogLock)
            {
                if (_catalog == null) _catalog = CreateCatalog();
                return _catalog;
            }
        }

        private static CatalogManager CreateCatalog()
        {
            string path = CatalogManager.ResolveAcatPath();
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException(".acat 경로를 찾을 수 없습니다.");
            return new CatalogManager(path);
        }

        private static object ToCatalogType(SupportTypeInfo type)
        {
            string range = type.MinDn > 0 || type.MaxDn > 0
                ? " DN " + type.MinDn.ToString("0.##", CultureInfo.InvariantCulture) + "-" + type.MaxDn.ToString("0.##", CultureInfo.InvariantCulture)
                : "";
            return new
            {
                id = type.Template ?? "",
                label = (type.Template ?? "") + " (" + type.VariantCount.ToString(CultureInfo.InvariantCulture) + ")",
                description = ((type.SampleLongDesc ?? "").Trim() + range).Trim()
            };
        }

        private static object ToVariantRow(VariantInfo variant)
        {
            return new
            {
                pnpId = variant.PnPID,
                dn = variant.NominalDiameter.ToString("0.##", CultureInfo.InvariantCulture),
                shortDescription = variant.ShortDescription ?? variant.PartSizeLongDesc ?? "",
                paramDefinition = ParseParamDefinition(variant.ParamDefinition)
            };
        }

        private static object ToParameterDef(string key)
        {
            bool number = !string.Equals(key, "DN", StringComparison.OrdinalIgnoreCase)
                       && !string.Equals(key, "Dn", StringComparison.OrdinalIgnoreCase);
            return new
            {
                key = key,
                label = key,
                unit = number ? "mm" : null,
                kind = number ? "number" : "text",
                required = string.Equals(key, "DN", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(key, "Dn", StringComparison.OrdinalIgnoreCase)
            };
        }

        private static Dictionary<string, string> ParseParamDefinition(string definition)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(definition)) return result;

            foreach (string part in definition.Split(','))
            {
                int idx = part.IndexOf('=');
                if (idx <= 0) continue;
                string key = part.Substring(0, idx).Trim();
                string value = part.Substring(idx + 1).Trim();
                if (key.Length > 0) result[key] = value;
            }
            return result;
        }

        private static VariantInput ToVariantInput(JObject input)
        {
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (input["params"] is JObject paramsObject)
            {
                foreach (JProperty property in paramsObject.Properties())
                {
                    parameters[property.Name] = Convert.ToString(property.Value, CultureInfo.InvariantCulture) ?? "";
                }
            }

            string dn = input.Value<string>("dn");
            if (!string.IsNullOrWhiteSpace(dn) && !parameters.ContainsKey("Dn"))
                parameters["Dn"] = dn;

            return new VariantInput
            {
                Params = parameters,
                ShortDescriptionOverride = input.Value<string>("shortDescription")
            };
        }

        private static object ToResult(AddVariantResult result)
        {
            return new
            {
                ok = result != null && result.Ok,
                message = result?.Message ?? ""
            };
        }

        private static T Arg<T>(JArray args, int index)
        {
            if (args == null || index >= args.Count)
                throw new ArgumentException("Catalog argument missing at index " + index.ToString(CultureInfo.InvariantCulture));
            return args[index].ToObject<T>();
        }

        private static void PostOnUi(Control owner, Action<string> post, object response)
        {
            if (owner == null || owner.IsDisposed) return;
            Action send = () => post(JsonConvert.SerializeObject(response));
            if (owner.InvokeRequired) owner.BeginInvoke(send);
            else send();
        }

        private sealed class CatalogEnvelope
        {
            [JsonProperty("ch")]
            public string Ch { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("method")]
            public string Method { get; set; }

            [JsonProperty("args")]
            public JArray Args { get; set; }
        }

        private static class Response
        {
            public static object Ok(string id, object result)
            {
                return new { ch = "catalog", id = id, ok = true, result = result };
            }

            public static object Fail(string id, string error)
            {
                return new { ch = "catalog", id = id, ok = false, error = error ?? "Catalog bridge failed." };
            }
        }
    }
}
