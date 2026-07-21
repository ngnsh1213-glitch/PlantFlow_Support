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
        // ── 그리드 시스템 저장 위치 ────────────────────────────────────────────
        // 과거에는 C:\TEMP\CADLIB\GridSystem.json을 하드코딩했다(원 개발 환경 경로).
        // 고객 PC에 그 경로가 없으면 동작하지 않으므로, 열린 Plant 프로젝트 경로를 키로
        // 사용자 로컬에 보관한다. PFO(BomConfigService)와 동일한 규약이다:
        //   %LocalAppData%\PlantFlow\projects\<sha1(정규화된 프로젝트 경로)>_gridsystem.json
        private const string LegacyGridSystemPath = @"C:\TEMP\CADLIB\GridSystem.json";

        // 활성 Plant 3D 프로젝트의 Piping 파트 디렉터리. 프로젝트가 없으면 null.
        public static string GetActiveProjectRoot()
        {
            try
            {
                Autodesk.ProcessPower.ProjectManager.PlantProject project =
                    Autodesk.ProcessPower.PlantInstance.PlantApplication.CurrentProject;
                if (project == null)
                    return null;
                Autodesk.ProcessPower.ProjectManager.Project piping =
                    project.ProjectParts["Piping"] as Autodesk.ProcessPower.ProjectManager.Project;
                if (piping == null)
                    return null;
                return piping.ProjectDirectory;
            }
            catch (System.Exception ex)
            {
                PlantOrthoView.FileDiag("ProjectDataUtils 활성 프로젝트 경로 취득 예외: " + ex.GetType().Name + ": " + ex.Message);
                return null;
            }
        }

        // 경로 표기 차이(대소문자·슬래시·후행 구분자)로 키가 갈라지지 않게 정규화한다.
        private static string NormalizeRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root)) return null;
            try
            {
                string full = Path.GetFullPath(root.Trim()).Replace('/', '\\').TrimEnd('\\');
                System.Text.StringBuilder sb = new System.Text.StringBuilder(full.Length);
                foreach (char ch in full) if (!char.IsControl(ch)) sb.Append(ch);
                return sb.ToString().ToLowerInvariant();
            }
            catch (System.Exception ex)
            {
                PlantOrthoView.FileDiag("ProjectDataUtils 경로 정규화 예외: " + ex.GetType().Name + ": " + ex.Message + " root=" + root);
                return root.Trim().ToLowerInvariant();
            }
        }

        // 활성 프로젝트의 그리드 파일 경로. 프로젝트가 없으면 null(호출부는 "미설정"으로 처리).
        // ensureDirectory=true면 저장 직전에 폴더를 만든다.
        public static string GetGridSystemPath(bool ensureDirectory)
        {
            string norm = NormalizeRoot(GetActiveProjectRoot());
            if (string.IsNullOrEmpty(norm))
            {
                PlantOrthoView.FileDiag("ProjectDataUtils 그리드 경로 확정 실패: 활성 Plant 프로젝트 없음");
                return null;
            }

            string hash;
            using (System.Security.Cryptography.SHA1 sha = System.Security.Cryptography.SHA1.Create())
            {
                byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(norm));
                System.Text.StringBuilder sb = new System.Text.StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                hash = sb.ToString();
            }

            string folder = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "PlantFlow", "projects");
            string path = Path.Combine(folder, hash + "_gridsystem.json");

            if (ensureDirectory)
            {
                try
                {
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                }
                catch (System.Exception ex)
                {
                    PlantOrthoView.FileDiag("ProjectDataUtils 그리드 폴더 생성 예외: " + ex.GetType().Name + ": " + ex.Message + " folder=" + folder);
                    return null;
                }
            }
            return path;
        }

        // 읽기용 경로. 새 위치에 없고 레거시 파일이 있으면 1회 이관한 뒤 새 위치를 쓴다.
        // 이관 실패는 치명적이지 않으므로 레거시 경로를 그대로 돌려주고 사유를 남긴다.
        public static string ResolveGridSystemPathForRead()
        {
            string path = GetGridSystemPath(false);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                return path;

            bool hasLegacy = false;
            try { hasLegacy = File.Exists(LegacyGridSystemPath); }
            catch (System.Exception ex)
            { PlantOrthoView.FileDiag("ProjectDataUtils 레거시 그리드 확인 예외: " + ex.GetType().Name + ": " + ex.Message); }

            if (!hasLegacy)
                return path;

            if (string.IsNullOrEmpty(path))
            {
                PlantOrthoView.FileDiag("ProjectDataUtils 그리드 레거시 사용(활성 프로젝트 없음) path=" + LegacyGridSystemPath);
                return LegacyGridSystemPath;
            }

            try
            {
                string folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder)) Directory.CreateDirectory(folder);
                File.Copy(LegacyGridSystemPath, path, false);
                PlantOrthoView.FileDiag("ProjectDataUtils 그리드 레거시 이관 " + LegacyGridSystemPath + " -> " + path);
                return path;
            }
            catch (System.Exception ex)
            {
                PlantOrthoView.FileDiag("ProjectDataUtils 그리드 레거시 이관 실패(레거시 사용): " + ex.GetType().Name + ": " + ex.Message);
                return LegacyGridSystemPath;
            }
        }

        // 그리드가 설정돼 있는가. 경로 확정 실패·파일 없음은 모두 "미설정"이다.
        public static bool GridSystemExists()
        {
            try
            {
                string path = ResolveGridSystemPathForRead();
                return !string.IsNullOrEmpty(path) && File.Exists(path);
            }
            catch (System.Exception ex)
            {
                PlantOrthoView.FileDiag("ProjectDataUtils 그리드 존재 확인 예외: " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

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
            string path = GetGridSystemPath(true);
            if (string.IsNullOrEmpty(path))
            {
                PlantOrthoView.FileDiag("ProjectDataUtils 그리드 저장 중단: 경로 확정 실패(활성 Plant 프로젝트 필요)");
                return;
            }
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
