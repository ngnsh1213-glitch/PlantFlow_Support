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
        // 고객 PC에 그 경로가 없으면 동작하지 않는다.
        //
        // [제품 데이터 저장 규약 2026-07-21] 프로젝트별 데이터는 활성 Plant 프로젝트 폴더 아래
        // 제품 전용 폴더에 둔다: <ProjectDirectory>\PlantFlow_PFS\GridSystem.json
        // 그리드는 도면 기준 데이터라 프로젝트와 함께 이동·백업되고 팀원과 공유되어야 한다.
        // (PFO도 같은 규약으로 <ProjectDirectory>\PlantFlow_PFO\bom.json을 쓴다.)
        //
        // 이관 순서: 새 위치 → (없으면) LocalAppData 해시 경로 → (없으면) CADLIB 레거시.
        // 앞선 위치에서 발견하면 새 위치로 1회 복사한다.
        private const string ProductFolderName = "PlantFlow_PFS";
        private const string GridSystemFileName = "GridSystem.json";
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
            string root = GetActiveProjectRoot();
            if (string.IsNullOrWhiteSpace(root))
            {
                PlantOrthoView.FileDiag("ProjectDataUtils 그리드 경로 확정 실패: 활성 Plant 프로젝트 없음");
                return null;
            }

            string folder;
            try
            {
                folder = Path.Combine(Path.GetFullPath(root.Trim()), ProductFolderName);
            }
            catch (System.Exception ex)
            {
                PlantOrthoView.FileDiag("ProjectDataUtils 제품 폴더 해석 예외: " + ex.GetType().Name + ": " + ex.Message + " root=" + root);
                return null;
            }

            if (ensureDirectory)
            {
                try
                {
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                }
                catch (System.Exception ex)
                {
                    // 공유 폴더 권한·Vault 체크아웃 등으로 실패할 수 있다. 호출부가 판단하도록 null.
                    PlantOrthoView.FileDiag("ProjectDataUtils 그리드 폴더 생성 예외: " + ex.GetType().Name + ": " + ex.Message + " folder=" + folder);
                    return null;
                }
            }
            return Path.Combine(folder, GridSystemFileName);
        }

        // [레거시] %LocalAppData%\PlantFlow\projects\<sha1(정규화된 프로젝트 경로)>_gridsystem.json
        // 2026-07-21 이전 방식. 이관 원본으로만 쓴다.
        private static string GetLegacyHashedGridPath()
        {
            string norm = NormalizeRoot(GetActiveProjectRoot());
            if (string.IsNullOrEmpty(norm)) return null;
            string hash;
            using (System.Security.Cryptography.SHA1 sha = System.Security.Cryptography.SHA1.Create())
            {
                byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(norm));
                System.Text.StringBuilder sb = new System.Text.StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                hash = sb.ToString();
            }
            return Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "PlantFlow", "projects", hash + "_gridsystem.json");
        }

        // 읽기용 경로. 새 위치에 없고 레거시 파일이 있으면 1회 이관한 뒤 새 위치를 쓴다.
        // 이관 실패는 치명적이지 않으므로 레거시 경로를 그대로 돌려주고 사유를 남긴다.
        public static string ResolveGridSystemPathForRead()
        {
            string path = GetGridSystemPath(false);
            if (!string.IsNullOrEmpty(path) && SafeFileExists(path))
                return path;

            // 이관 원본 우선순위: LocalAppData 해시(직전 방식) → CADLIB(원 개발 환경).
            string[] sources = new string[] { GetLegacyHashedGridPath(), LegacyGridSystemPath };
            for (int i = 0; i < sources.Length; i++)
            {
                string source = sources[i];
                if (string.IsNullOrEmpty(source) || !SafeFileExists(source)) continue;

                if (string.IsNullOrEmpty(path))
                {
                    PlantOrthoView.FileDiag("ProjectDataUtils 그리드 레거시 사용(새 경로 확정 실패) path=" + source);
                    return source;
                }

                try
                {
                    string folder = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder)) Directory.CreateDirectory(folder);
                    File.Copy(source, path, false);
                    PlantOrthoView.FileDiag("ProjectDataUtils 그리드 레거시 이관 " + source + " -> " + path);
                    return path;
                }
                catch (System.Exception ex)
                {
                    PlantOrthoView.FileDiag("ProjectDataUtils 그리드 레거시 이관 실패(레거시 사용): " + ex.GetType().Name + ": " + ex.Message + " source=" + source);
                    return source;
                }
            }
            return path;
        }

        private static bool SafeFileExists(string path)
        {
            try { return !string.IsNullOrEmpty(path) && File.Exists(path); }
            catch (System.Exception ex)
            {
                PlantOrthoView.FileDiag("ProjectDataUtils 파일 확인 예외: " + ex.GetType().Name + ": " + ex.Message + " path=" + path);
                return false;
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
