using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace PlantFlow_Support
{
    // 페인 #1: 템플릿 기반 서포트 변형 생성. .acat(SQLite) 카탈로그 직접 read/write.
    // 안전: 쓰기 전 온라인백업 + 배타잠금(BEGIN IMMEDIATE) + 단일 트랜잭션 + integrity_check.
    // PnP 상속체인 4테이블(PnPBase/EngineeringItems/PipeRunComponent/Support) 형제행 복제+override.
    // UI(Phase C)는 본 클래스의 public 메서드만 호출한다.
    public class CatalogManager
    {
        // 변형 1개가 걸치는 PnP 상속체인 테이블(실측). 포트/관계행은 RS류에 없음(타입별 동적확인은 향후).
        private static readonly string[] ChainTables = { "PnPBase", "EngineeringItems", "PipeRunComponent", "Support" };
        private const string AllocTable = "PnPSys_PnPBase_PnPID";

        public string AcatPath { get; }

        public CatalogManager(string acatPath)
        {
            if (string.IsNullOrEmpty(acatPath) || !File.Exists(acatPath))
                throw new FileNotFoundException("카탈로그(.acat) 파일을 찾을 수 없습니다: " + acatPath);
            AcatPath = acatPath;
        }

        // 라이브 Plant3D 콘텐츠 카탈로그 경로. [테스트] 사본으로 우선 검증.
        // ※ Plant3D가 실제로 읽는 .acat이어야 변형이 도면에 노출됨. 향후 UI에서 선택가능하게 확장.
        public static string ResolveAcatPath()
        {
            // 사용자가 UI에서 선택해 영구저장한 경로를 최우선 사용.
            string saved = LoadLastAcatPath();
            if (!string.IsNullOrEmpty(saved)) return saved;

            var cands = new List<string>
            {
                // [테스트] 라이브 콘텐츠 폴더의 사본
                @"D:\AutoCAD Plant 3D 2026 Content\Cpak Support\WH-PipeSupport - 복사본.acat",
                // 운영시 실제 카탈로그(테스트 통과 후 전환)
                @"D:\AutoCAD Plant 3D 2026 Content\Cpak Support\WH-PipeSupport.acat",
            };
            try
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                for (int i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
                {
                    cands.Add(Path.Combine(dir, @"Cpak Support\WH-PipeSupport.acat"));
                    dir = Path.GetDirectoryName(dir);
                }
            }
            catch { }
            foreach (var c in cands) { try { if (File.Exists(c)) return c; } catch { } }
            return null;
        }

        // 사용자가 선택한 .acat 경로의 영구저장 위치(%LOCALAPPDATA%\PlantFlow\support_catalog.path).
        private static string SettingsFilePath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PlantFlow");
            return Path.Combine(dir, "support_catalog.path");
        }

        // 마지막 선택 경로 로드. 파일/경로 부재 시 null. 손상 시 무시(기본 탐색으로 폴백).
        public static string LoadLastAcatPath()
        {
            try
            {
                string sf = SettingsFilePath();
                if (!File.Exists(sf)) return null;
                string p = File.ReadAllText(sf).Trim();
                return (!string.IsNullOrEmpty(p) && File.Exists(p)) ? p : null;
            }
            catch
            {
                // 설정 읽기 실패는 치명적이지 않음 → 기본 자동탐색으로 폴백.
                return null;
            }
        }

        // 선택 경로를 영구저장. 실패 시 호출자(UI)가 로깅하도록 예외 전파(No Silent Failures).
        public static void SaveLastAcatPath(string acatPath)
        {
            if (string.IsNullOrEmpty(acatPath))
                throw new ArgumentException("저장할 .acat 경로가 비었습니다.", nameof(acatPath));
            string sf = SettingsFilePath();
            string dir = Path.GetDirectoryName(sf);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(sf, acatPath);
        }

        private SQLiteConnection Open(bool write)
        {
            // ReadOnly 모드로 열어 Plant3D와의 충돌 최소화(읽기). 쓰기는 별도.
            string cs = $"Data Source={AcatPath};Version=3;{(write ? "" : "Read Only=True;")}";
            var conn = new SQLiteConnection(cs);
            conn.Open();
            return conn;
        }

        // ── 읽기 ──

        public List<SupportTypeInfo> ListTypes()
        {
            var list = new List<SupportTypeInfo>();
            using (var conn = Open(false))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT ContentGeometryTemplate, COUNT(*), MIN(NominalDiameter), MAX(NominalDiameter), " +
                    "       MIN(PartSizeLongDesc) " +
                    "FROM EngineeringItems WHERE ContentGeometryTemplate IS NOT NULL " +
                    "GROUP BY ContentGeometryTemplate ORDER BY ContentGeometryTemplate";
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new SupportTypeInfo
                        {
                            Template = r.GetString(0),
                            VariantCount = r.GetInt32(1),
                            MinDn = r.IsDBNull(2) ? 0 : Convert.ToDouble(r.GetValue(2)),
                            MaxDn = r.IsDBNull(3) ? 0 : Convert.ToDouble(r.GetValue(3)),
                            SampleLongDesc = r.IsDBNull(4) ? "" : r.GetString(4)
                        });
                    }
                }
            }
            return list;
        }

        public List<VariantInfo> ListVariants(string template)
        {
            var list = new List<VariantInfo>();
            using (var conn = Open(false))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT PnPID, NominalDiameter, ShortDescription, PartSizeLongDesc, ContentGeometryParamDefinition " +
                    "FROM EngineeringItems WHERE ContentGeometryTemplate=@t ORDER BY NominalDiameter";
                cmd.Parameters.AddWithValue("@t", template);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new VariantInfo
                        {
                            PnPID = Convert.ToInt64(r.GetValue(0)),
                            NominalDiameter = r.IsDBNull(1) ? 0 : Convert.ToDouble(r.GetValue(1)),
                            ShortDescription = r.IsDBNull(2) ? "" : r.GetString(2),
                            PartSizeLongDesc = r.IsDBNull(3) ? "" : r.GetString(3),
                            ParamDefinition = r.IsDBNull(4) ? "" : r.GetString(4)
                        });
                    }
                }
            }
            return list;
        }

        // 템플릿의 파라미터 키(현재 카탈로그/배포본 기준). UI 폼 필드 생성용.
        public List<string> GetParamKeys(string template)
        {
            var variants = ListVariants(template);
            var def = variants.FirstOrDefault(v => !string.IsNullOrEmpty(v.ParamDefinition));
            if (def == null) return new List<string>();
            return def.ParamDefinition.Split(',')
                .Select(kv => kv.Split('=')[0].Trim())
                .Where(k => k.Length > 0).ToList();
        }

        // ── 백업(온라인) ──

        public string BackupAcat()
        {
            string bak = AcatPath + ".bak." + DateTime.Now.ToString("yyyyMMddHHmmss");
            using (var src = Open(false))
            using (var dst = new SQLiteConnection($"Data Source={bak};Version=3;"))
            {
                dst.Open();
                src.BackupDatabase(dst, "main", "main", -1, null, 0); // SQLite 온라인 백업 API
            }
            return bak;
        }

        // ── 쓰기: 변형 추가 ──

        public AddVariantResult AddVariant(string template, VariantInput input)
        {
            var res = new AddVariantResult();
            string backup = null;
            try
            {
                if (input == null || input.Params == null || input.Params.Count == 0)
                { res.Message = "파라미터가 비었습니다."; return res; }

                double dnPre = ParseDn(input.Params);

                // 중복 가드(백업 전 조기 차단): 동일 template+Dn 존재 시 거부.
                using (var rc = Open(false))
                using (var dc = rc.CreateCommand())
                {
                    dc.CommandText = "SELECT COUNT(*) FROM EngineeringItems WHERE ContentGeometryTemplate=@t AND NominalDiameter=@dn";
                    dc.Parameters.AddWithValue("@t", template);
                    dc.Parameters.AddWithValue("@dn", dnPre);
                    if (Convert.ToInt64(dc.ExecuteScalar()) > 0)
                    { res.Message = $"이미 Dn={dnPre} 변형이 있습니다. 수정하려면 'Update Selected'를 사용하세요."; return res; }
                }

                // 카탈로그·스펙이 공유할 SizeRecordId(링크키). 단 1회 생성.
                byte[] sizeRec = Guid.NewGuid().ToByteArray();
                string specPath = SpecManager.ResolveSpecPath(AcatPath);
                var specElig = SpecManager.Evaluate(specPath, template, AcatPath);

                backup = BackupAcat();
                res.BackupPath = backup;

                // 스펙도 쓰기 전 백업(롤백 안전망). integrity_check+트랜잭션 외 최종 복원 수단.
                if (specElig.Ok)
                {
                    try { File.Copy(specPath, specPath + ".bak." + DateTime.Now.ToString("yyyyMMddHHmmss"), false); }
                    catch (Exception bex) { throw new IOException("스펙 백업 실패(쓰기 중단): " + bex.Message, bex); }
                }

                using (var conn = Open(true))
                {
                    // 스펙 적격 시 동일 트랜잭션으로 묶기 위해 ATTACH(교차DB 원자성, journal=delete 전제).
                    if (specElig.Ok)
                    {
                        using (var at = conn.CreateCommand())
                        {
                            at.CommandText = "ATTACH DATABASE @p AS spec";
                            at.Parameters.AddWithValue("@p", specPath);
                            at.ExecuteNonQuery();
                        }
                    }

                    // 배타잠금 조기획득(Plant3D가 .acat 열고있으면 SQLITE_BUSY).
                    using (var tx = conn.BeginTransaction(System.Data.IsolationLevel.Serializable))
                    {
                        // 형제 변형(대표 1행) PnPID
                        long siblingId = GetSiblingPnPID(conn, template);
                        if (siblingId < 0) { res.Message = $"템플릿 '{template}'의 기존 변형이 없습니다."; return res; }

                        long newId = AllocatePnPID(conn);

                        // 사이즈 파생값
                        double dn = ParseDn(input.Params);
                        double? matchingOd = input.MatchingPipeOd ?? LookupPipeOd(conn, dn);
                        string paramDef = BuildParamDefinition(conn, template, input.Params);
                        string longDesc = BuildPartSizeLongDesc(conn, siblingId, dn, input.ShortDescriptionOverride);

                        // 4테이블 형제행 복제 + override (카탈로그)
                        foreach (var tbl in ChainTables)
                        {
                            var overrides = new Dictionary<string, object> { { "PnPID", newId } };
                            if (tbl == "PnPBase")
                                overrides["PnPGuid"] = Guid.NewGuid().ToByteArray();
                            if (tbl == "EngineeringItems")
                            {
                                overrides["ContentGeometryParamDefinition"] = paramDef;
                                overrides["NominalDiameter"] = dn;
                                if (matchingOd.HasValue) overrides["MatchingPipeOd"] = matchingOd.Value;
                                overrides["SizeRecordId"] = sizeRec;
                                if (longDesc != null) overrides["PartSizeLongDesc"] = longDesc;
                            }
                            CloneRow(conn, tbl, siblingId, overrides);
                        }

                        // 스펙(.pspc) 동기화: 동일 SizeRecordId로 사이즈 행 추가(체크박스 체크 효과).
                        bool specWritten = false;
                        if (specElig.Ok)
                        {
                            AddSpecSize(conn, template, dn, sizeRec, matchingOd, paramDef, input.ShortDescriptionOverride);
                            specWritten = true;
                        }

                        IntegrityCheck(conn);
                        if (specWritten) IntegrityCheck(conn, "spec");
                        tx.Commit();

                        res.Ok = true;
                        res.NewPnPID = newId;
                        res.Message = $"변형 추가 완료 (PnPID={newId})."
                            + (specWritten ? " 스펙(.pspc) 사이즈 동기화 완료." : " 스펙 미동기화" + specElig.Reason)
                            + " Plant3D에서 'Check for Spec Updates' 후 Pipe Spec Viewer를 닫았다 여세요.";
                        return res;
                    }
                }
            }
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Busy || ex.ResultCode == SQLiteErrorCode.Locked)
            {
                res.Message = ".acat이 잠겨 있습니다(Plant3D가 사용 중일 수 있음). Spec Editor/카탈로그를 닫고 다시 시도하세요.";
                return res;
            }
            catch (Exception ex)
            {
                res.Message = "변형 추가 실패: " + ex.Message + (backup != null ? $"\n백업 보존: {backup}" : "");
                return res;
            }
        }

        // ── 쓰기: 기존 변형 수정(같은 PnPID, in-place) ──
        public AddVariantResult UpdateVariant(long pnpId, string template, VariantInput input)
        {
            var res = new AddVariantResult { NewPnPID = pnpId };
            string backup = null;
            try
            {
                if (input == null || input.Params == null || input.Params.Count == 0)
                { res.Message = "파라미터가 비었습니다."; return res; }

                backup = BackupAcat();
                res.BackupPath = backup;

                using (var conn = Open(true))
                using (var tx = conn.BeginTransaction(System.Data.IsolationLevel.Serializable))
                {
                    // 대상 행 존재 확인
                    using (var chk = conn.CreateCommand())
                    {
                        chk.CommandText = "SELECT COUNT(*) FROM EngineeringItems WHERE PnPID=@id";
                        chk.Parameters.AddWithValue("@id", pnpId);
                        if (Convert.ToInt64(chk.ExecuteScalar()) == 0)
                        { res.Message = $"PnPID {pnpId} 변형이 없습니다."; return res; }
                    }

                    double dn = ParseDn(input.Params);
                    double? matchingOd = input.MatchingPipeOd ?? LookupPipeOd(conn, dn);
                    string paramDef = BuildParamDefinition(conn, template, input.Params);
                    string longDesc = string.IsNullOrEmpty(input.ShortDescriptionOverride)
                        ? BuildPartSizeLongDescForDn(conn, pnpId, dn) : input.ShortDescriptionOverride;

                    // EngineeringItems의 파라미터/사이즈 필드만 갱신(PnPID/Guid 불변).
                    using (var up = conn.CreateCommand())
                    {
                        up.CommandText =
                            "UPDATE EngineeringItems SET ContentGeometryParamDefinition=@pd, NominalDiameter=@dn" +
                            (matchingOd.HasValue ? ", MatchingPipeOd=@od" : "") +
                            (longDesc != null ? ", PartSizeLongDesc=@ld" : "") +
                            " WHERE PnPID=@id";
                        up.Parameters.AddWithValue("@pd", paramDef);
                        up.Parameters.AddWithValue("@dn", dn);
                        if (matchingOd.HasValue) up.Parameters.AddWithValue("@od", matchingOd.Value);
                        if (longDesc != null) up.Parameters.AddWithValue("@ld", longDesc);
                        up.Parameters.AddWithValue("@id", pnpId);
                        up.ExecuteNonQuery();
                    }

                    // 변경 신호: PnPBase.PnPRevision +1 (Check for Spec Updates 감지 유도).
                    using (var rev = conn.CreateCommand())
                    {
                        rev.CommandText = "UPDATE PnPBase SET PnPRevision = COALESCE(PnPRevision,0)+1 WHERE PnPID=@id";
                        rev.Parameters.AddWithValue("@id", pnpId);
                        rev.ExecuteNonQuery();
                    }

                    IntegrityCheck(conn);
                    tx.Commit();
                    res.Ok = true;
                    res.Message = $"변형 수정 완료 (PnPID={pnpId}). 'Check for Spec Updates' 후 Pipe Spec Viewer를 닫았다 여세요.";
                    return res;
                }
            }
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Busy || ex.ResultCode == SQLiteErrorCode.Locked)
            { res.Message = ".acat이 잠겨 있습니다(Plant3D 사용 중). 닫고 다시 시도하세요."; return res; }
            catch (Exception ex)
            { res.Message = "변형 수정 실패: " + ex.Message + (backup != null ? $"\n백업: {backup}" : ""); return res; }
        }

        // ── 쓰기: 변형 삭제(4테이블 행 제거) ──
        public AddVariantResult DeleteVariant(long pnpId)
        {
            var res = new AddVariantResult { NewPnPID = pnpId };
            string backup = null;
            try
            {
                backup = BackupAcat();
                res.BackupPath = backup;
                using (var conn = Open(true))
                using (var tx = conn.BeginTransaction(System.Data.IsolationLevel.Serializable))
                {
                    // 자식→부모 순서로 삭제.
                    foreach (var tbl in new[] { "Support", "PipeRunComponent", "EngineeringItems", "PnPBase" })
                    {
                        using (var d = conn.CreateCommand())
                        {
                            d.CommandText = $"DELETE FROM \"{tbl}\" WHERE PnPID=@id";
                            d.Parameters.AddWithValue("@id", pnpId);
                            d.ExecuteNonQuery();
                        }
                    }
                    IntegrityCheck(conn);
                    tx.Commit();
                    res.Ok = true;
                    res.Message = $"변형 삭제 완료 (PnPID={pnpId}). 'Check for Spec Updates' 후 Viewer 재열기.";
                    return res;
                }
            }
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Busy || ex.ResultCode == SQLiteErrorCode.Locked)
            { res.Message = ".acat이 잠겨 있습니다(Plant3D 사용 중). 닫고 다시 시도하세요."; return res; }
            catch (Exception ex)
            { res.Message = "변형 삭제 실패: " + ex.Message + (backup != null ? $"\n백업: {backup}" : ""); return res; }
        }

        // ── 내부 ──

        // 대상 행의 현재 PartSizeLongDesc 기반으로 Dn 사이즈토큰 치환.
        private string BuildPartSizeLongDescForDn(SQLiteConnection conn, long pnpId, double dn)
        {
            string baseDesc = null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT PartSizeLongDesc FROM EngineeringItems WHERE PnPID=@id";
                cmd.Parameters.AddWithValue("@id", pnpId);
                baseDesc = cmd.ExecuteScalar() as string;
            }
            if (string.IsNullOrEmpty(baseDesc)) return null;
            string sizeTok = ((int)dn).ToString() + "A";
            if (Regex.IsMatch(baseDesc, @"\d+A\s*$")) return Regex.Replace(baseDesc, @"\d+A\s*$", sizeTok);
            return baseDesc;
        }

        private static long GetSiblingPnPID(SQLiteConnection conn, string template, string schema = "main")
        {
            string ei = schema == "main" ? "EngineeringItems" : schema + ".EngineeringItems";
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT PnPID FROM " + ei + " WHERE ContentGeometryTemplate=@t ORDER BY NominalDiameter LIMIT 1";
                cmd.Parameters.AddWithValue("@t", template);
                var o = cmd.ExecuteScalar();
                return o == null || o == DBNull.Value ? -1 : Convert.ToInt64(o);
            }
        }

        // PnPID 채번: 할당자 테이블 cnt와 실제 max PnPID 중 큰 값. 할당자/시퀀스 동반 갱신.
        private static long AllocatePnPID(SQLiteConnection conn, string schema = "main")
        {
            string alloc = schema == "main" ? $"\"{AllocTable}\"" : $"{schema}.\"{AllocTable}\"";
            string baseT = schema == "main" ? "PnPBase" : schema + ".PnPBase";
            string seqT = schema == "main" ? "sqlite_sequence" : schema + ".sqlite_sequence";
            long allocCnt = 0, maxId = 0;
            using (var c1 = conn.CreateCommand())
            {
                c1.CommandText = $"SELECT cnt FROM {alloc}";
                var o = c1.ExecuteScalar();
                if (o != null && o != DBNull.Value) allocCnt = Convert.ToInt64(o);
            }
            using (var c2 = conn.CreateCommand())
            {
                c2.CommandText = $"SELECT MAX(PnPID) FROM {baseT}";
                var o = c2.ExecuteScalar();
                if (o != null && o != DBNull.Value) maxId = Convert.ToInt64(o);
            }
            long newId = Math.Max(allocCnt, maxId + 1);
            long next = newId + 1;
            using (var u1 = conn.CreateCommand())
            { u1.CommandText = $"UPDATE {alloc} SET cnt=@n"; u1.Parameters.AddWithValue("@n", next); u1.ExecuteNonQuery(); }
            using (var u2 = conn.CreateCommand())
            { u2.CommandText = $"UPDATE {seqT} SET seq=@n WHERE name=@t"; u2.Parameters.AddWithValue("@n", next); u2.Parameters.AddWithValue("@t", AllocTable); u2.ExecuteNonQuery(); }
            return newId;
        }

        // 형제 PnPID 행을 전 컬럼 복제 후 overrides 적용해 INSERT.
        private static void CloneRow(SQLiteConnection conn, string table, long siblingId, Dictionary<string, object> overrides, string schema = "main")
        {
            string qt = schema == "main" ? $"\"{table}\"" : $"{schema}.\"{table}\"";
            string pragma = schema == "main" ? $"PRAGMA table_info(\"{table}\")" : $"PRAGMA {schema}.table_info(\"{table}\")";
            var cols = new List<string>();
            using (var pc = conn.CreateCommand())
            {
                pc.CommandText = pragma;
                using (var r = pc.ExecuteReader()) while (r.Read()) cols.Add(r.GetString(1));
            }
            object[] vals = new object[cols.Count];
            using (var sc = conn.CreateCommand())
            {
                sc.CommandText = $"SELECT * FROM {qt} WHERE PnPID=@id";
                sc.Parameters.AddWithValue("@id", siblingId);
                using (var r = sc.ExecuteReader())
                {
                    if (!r.Read()) throw new InvalidOperationException($"{table}에 형제행(PnPID={siblingId}) 없음");
                    for (int i = 0; i < cols.Count; i++) vals[i] = r.GetValue(i);
                }
            }
            for (int i = 0; i < cols.Count; i++)
                if (overrides.TryGetValue(cols[i], out var ov)) vals[i] = ov;

            var colList = string.Join(",", cols.Select(c => $"\"{c}\""));
            var parList = string.Join(",", cols.Select((c, i) => "@p" + i));
            using (var ic = conn.CreateCommand())
            {
                ic.CommandText = $"INSERT INTO {qt} ({colList}) VALUES ({parList})";
                for (int i = 0; i < cols.Count; i++) ic.Parameters.AddWithValue("@p" + i, vals[i] ?? DBNull.Value);
                ic.ExecuteNonQuery();
            }
        }

        // 스펙(ATTACH AS spec)에 동일 SizeRecordId로 사이즈 행 추가. 스펙 형제행 4테이블 클론.
        // 전제: 호출 전 SpecManager.Evaluate로 적격성 확인됨(타입 존재·포트 없음).
        private void AddSpecSize(SQLiteConnection conn, string template, double dn, byte[] sizeRec, double? matchingOd, string paramDef, string descOverride)
        {
            long sib = GetSiblingPnPID(conn, template, "spec");
            if (sib < 0) throw new InvalidOperationException("스펙 형제행 없음(template=" + template + ")");

            long newId = AllocatePnPID(conn, "spec");
            string longDesc = string.IsNullOrEmpty(descOverride) ? BuildSpecLongDesc(conn, sib, dn) : descOverride;

            foreach (var tbl in ChainTables)
            {
                var ov = new Dictionary<string, object> { { "PnPID", newId } };
                if (tbl == "PnPBase")
                    ov["PnPGuid"] = Guid.NewGuid().ToByteArray();
                if (tbl == "EngineeringItems")
                {
                    ov["ContentGeometryParamDefinition"] = paramDef;
                    ov["NominalDiameter"] = dn;
                    if (matchingOd.HasValue) ov["MatchingPipeOd"] = matchingOd.Value;
                    ov["SizeRecordId"] = sizeRec;        // ← 카탈로그와 동일값(.acat↔.pspc 링크)
                    if (longDesc != null) ov["PartSizeLongDesc"] = longDesc;
                }
                CloneRow(conn, tbl, sib, ov, "spec");
            }
        }

        // 스펙 형제 PartSizeLongDesc의 끝 "NNA" 사이즈 토큰을 새 Dn으로 치환(없으면 추가).
        private string BuildSpecLongDesc(SQLiteConnection conn, long sibId, double dn)
        {
            string baseDesc = null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT PartSizeLongDesc FROM spec.EngineeringItems WHERE PnPID=@id";
                cmd.Parameters.AddWithValue("@id", sibId);
                baseDesc = cmd.ExecuteScalar() as string;
            }
            if (string.IsNullOrEmpty(baseDesc)) return null;
            string tok = ((int)dn).ToString() + "A";
            if (Regex.IsMatch(baseDesc, @"\d+A\s*$"))
                return Regex.Replace(baseDesc, @"\d+A\s*$", tok);
            return baseDesc.TrimEnd() + " " + tok;
        }

        private static double ParseDn(Dictionary<string, string> p)
        {
            foreach (var k in new[] { "Dn", "DN", "dn" })
                if (p.TryGetValue(k, out var v) && double.TryParse(v, out var d)) return d;
            return 0;
        }

        private static double? LookupPipeOd(SQLiteConnection conn, double dn)
        {
            if (dn <= 0) return null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT MatchingPipeOd FROM EngineeringItems WHERE NominalDiameter=@dn AND MatchingPipeOd IS NOT NULL LIMIT 1";
                cmd.Parameters.AddWithValue("@dn", dn);
                var o = cmd.ExecuteScalar();
                return o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o);
            }
        }

        // 기존 키 순서 보존하여 K=V 조립(현재 카탈로그 키 기준 — 스테일 문자열 복제 금지).
        private string BuildParamDefinition(SQLiteConnection conn, string template, Dictionary<string, string> input)
        {
            var keys = GetParamKeysInternal(conn, template);
            if (keys.Count == 0) keys = input.Keys.ToList();
            var sb = new StringBuilder();
            foreach (var k in keys)
            {
                if (sb.Length > 0) sb.Append(',');
                string v = input.TryGetValue(k, out var iv) ? iv : "0";
                sb.Append(k).Append('=').Append(v);
            }
            return sb.ToString();
        }

        private static List<string> GetParamKeysInternal(SQLiteConnection conn, string template)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ContentGeometryParamDefinition FROM EngineeringItems WHERE ContentGeometryTemplate=@t AND ContentGeometryParamDefinition IS NOT NULL LIMIT 1";
                cmd.Parameters.AddWithValue("@t", template);
                var o = cmd.ExecuteScalar() as string;
                if (string.IsNullOrEmpty(o)) return new List<string>();
                return o.Split(',').Select(kv => kv.Split('=')[0].Trim()).Where(k => k.Length > 0).ToList();
            }
        }

        // PartSizeLongDesc: 형제 설명의 끝 "NNA" 사이즈 토큰을 새 Dn으로 치환(없으면 추가).
        private string BuildPartSizeLongDesc(SQLiteConnection conn, long siblingId, double dn, string overrideDesc)
        {
            if (!string.IsNullOrEmpty(overrideDesc)) return overrideDesc;
            string baseDesc = null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT PartSizeLongDesc FROM EngineeringItems WHERE PnPID=@id";
                cmd.Parameters.AddWithValue("@id", siblingId);
                baseDesc = cmd.ExecuteScalar() as string;
            }
            if (string.IsNullOrEmpty(baseDesc)) return null;
            string sizeTok = ((int)dn).ToString() + "A";
            if (Regex.IsMatch(baseDesc, @"\d+A\s*$"))
                return Regex.Replace(baseDesc, @"\d+A\s*$", sizeTok);
            return baseDesc.TrimEnd() + " " + sizeTok;
        }

        private static void IntegrityCheck(SQLiteConnection conn, string schema = "main")
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = schema == "main" ? "PRAGMA integrity_check" : $"PRAGMA {schema}.integrity_check";
                var o = cmd.ExecuteScalar() as string;
                if (o != null && o != "ok")
                    throw new InvalidOperationException($"integrity_check({schema}) 실패: " + o);
            }
        }
    }

    public class SupportTypeInfo
    {
        public string Template;       // 파이썬 함수명 = ContentGeometryTemplate
        public int VariantCount;
        public double MinDn;
        public double MaxDn;
        public string SampleLongDesc;
    }

    public class VariantInfo
    {
        public long PnPID;
        public double NominalDiameter;
        public string ShortDescription;
        public string PartSizeLongDesc;
        public string ParamDefinition;
    }

    public class VariantInput
    {
        public Dictionary<string, string> Params;       // 키=현재 카탈로그 파라미터 키(Dn,BI,A,...)
        public double? MatchingPipeOd;                  // null이면 동일 Dn 기존행에서 자동조회
        public string ShortDescriptionOverride;         // null이면 형제 설명에서 사이즈 치환
    }

    public class AddVariantResult
    {
        public bool Ok;
        public long NewPnPID;
        public string Message;
        public string BackupPath;
    }
}
