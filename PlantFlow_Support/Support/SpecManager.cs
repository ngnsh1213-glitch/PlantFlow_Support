using System;
using System.Data.SQLite;
using System.IO;

namespace PlantFlow_Support
{
    // 스펙(.pspc) 동기화 보조. CatalogManager가 .acat 쓰기 트랜잭션에 .pspc를 ATTACH해
    // 단일 트랜잭션으로 함께 기록하므로(원자성), 본 클래스는 경로 도출/적격성 판정만 담당한다.
    // 실측 근거: RS류 스펙 부품은 4테이블(PnPBase/EngineeringItems/PipeRunComponent/Support)만 사용,
    // Port/PartPort/관계행 없음. 포트 보유 타입은 자동추가 미지원으로 차단한다.
    public static class SpecManager
    {
        // .pspc 경로 = .acat 동일 폴더·동일 basename. 부재 시 null.
        public static string ResolveSpecPath(string acatPath)
        {
            try
            {
                if (string.IsNullOrEmpty(acatPath)) return null;
                string dir = Path.GetDirectoryName(acatPath);
                string baseName = Path.GetFileNameWithoutExtension(acatPath);
                if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(baseName)) return null;
                string p = Path.Combine(dir, baseName + ".pspc");
                return File.Exists(p) ? p : null;
            }
            catch
            {
                // 경로 도출 실패는 치명적 아님 → 스펙 동기화 스킵(호출자가 카탈로그만 반영).
                return null;
            }
        }

        // Plant3D가 파일을 열고 있으면 -wal/-journal/-shm 잔존. ATTACH 쓰기 충돌·손상 위험 차단용.
        public static bool HasSidecarJournals(string path)
        {
            try
            {
                return File.Exists(path + "-wal") || File.Exists(path + "-journal") || File.Exists(path + "-shm");
            }
            catch { return false; }
        }

        // 스펙 동기화 적격성 판정(쓰기 전, read-only). 부적격이면 카탈로그만 반영하도록 Reason 반환.
        public static SpecEval Evaluate(string specPath, string template, string acatPath)
        {
            if (string.IsNullOrEmpty(specPath))
                return SpecEval.Skip(" (.pspc 없음 — 스펙 수동 반영 필요)");
            if (HasSidecarJournals(specPath) || HasSidecarJournals(acatPath))
                return SpecEval.Skip(" (.acat/.pspc 저널 잔존 — Plant3D 닫고 재시도, 카탈로그만 반영)");
            try
            {
                using (var conn = new SQLiteConnection($"Data Source={specPath};Version=3;Read Only=True;"))
                {
                    conn.Open();
                    using (var c = conn.CreateCommand())
                    {
                        c.CommandText = "SELECT COUNT(*) FROM EngineeringItems WHERE ContentGeometryTemplate=@t";
                        c.Parameters.AddWithValue("@t", template);
                        if (Convert.ToInt64(c.ExecuteScalar()) == 0)
                            return SpecEval.Skip($" (스펙에 '{template}' 타입 없음 — 카탈로그만 반영)");
                    }
                    try
                    {
                        using (var c = conn.CreateCommand())
                        {
                            c.CommandText =
                                "SELECT COUNT(*) FROM PartPort WHERE Part IN " +
                                "(SELECT PnPID FROM EngineeringItems WHERE ContentGeometryTemplate=@t)";
                            c.Parameters.AddWithValue("@t", template);
                            if (Convert.ToInt64(c.ExecuteScalar()) > 0)
                                return SpecEval.Skip(" (포트/관계행 보유 타입 — 자동 스펙추가 미지원, 카탈로그만 반영)");
                        }
                    }
                    catch
                    {
                        // PartPort 테이블 부재 = 포트 없음으로 간주(계속 진행).
                    }
                }
            }
            catch (Exception ex)
            {
                return SpecEval.Skip(" (스펙 점검 실패: " + ex.Message + " — 카탈로그만 반영)");
            }
            return SpecEval.Pass();
        }
    }

    public struct SpecEval
    {
        public bool Ok;
        public string Reason;
        public static SpecEval Pass() => new SpecEval { Ok = true, Reason = "" };
        public static SpecEval Skip(string reason) => new SpecEval { Ok = false, Reason = reason };
    }
}
