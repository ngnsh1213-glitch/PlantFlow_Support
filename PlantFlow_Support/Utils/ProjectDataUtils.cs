using Autodesk.ProcessPower.DataLinks;
using Autodesk.ProcessPower.DataObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace PlantFlow_Support
{
    public static class ProjectDataUtils
    {
        public static JObject ReadJson(string json_file)
        {
            using (StreamReader streamReader = File.OpenText(json_file))
            {
                using (JsonTextReader jsonTextReader = new JsonTextReader((TextReader)streamReader))
                    return (JObject)JToken.ReadFrom((JsonReader)jsonTextReader);
            }
        }

        public static JObject ReadJsonFile(string json_file)
        {
            return ReadJson(json_file);
        }

        public static void WriteJsonFile(Dictionary<string, Autodesk.AutoCAD.Geometry.Point2d> Grid)
        {
            string path = "C:\\TEMP\\CADLIB\\GridSystem.json";
            JObject jobject1 = new JObject();
            foreach (KeyValuePair<string, Autodesk.AutoCAD.Geometry.Point2d> keyValuePair in Grid)
            {
                JObject jobject2 = new JObject();
                JObject jobject3 = jobject2;
                Autodesk.AutoCAD.Geometry.Point2d point2d = keyValuePair.Value;
                JToken jtoken1 = (JToken)System.Math.Round(point2d.X);
                jobject3.Add("X", jtoken1);
                JObject jobject4 = jobject2;
                point2d = keyValuePair.Value;
                JToken jtoken2 = (JToken)System.Math.Round(point2d.Y);
                jobject4.Add("Y", jtoken2);
                jobject1.Add(keyValuePair.Key, (JToken)jobject2);
            }
            using (StreamWriter text = File.CreateText(path))
            {
                using (JsonTextWriter jsonTextWriter = new JsonTextWriter((TextWriter)text))
                    ((JToken)jobject1).WriteTo((JsonWriter)jsonTextWriter, System.Array.Empty<JsonConverter>());
            }
        }

        public static string IndividualDrawingPath(PnPDatabase db, string drawing_title)
        {
            // Note: Accessing PnPDatabase tables.
            return ((PnPAbstractRow)db.Tables["PnPDrawings"].Select(string.Format("\"Dwg Name\"='{0}'", (object)(drawing_title + ".dwg")))[0])["Path"] as string;
        }
    }
}
