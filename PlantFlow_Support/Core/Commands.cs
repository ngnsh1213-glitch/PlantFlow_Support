using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Autodesk.ProcessPower.PnP3dObjects;

#nullable disable
namespace PlantFlow_Support
{

  public partial class Commands
  {
    private static CustomPaletteSet palette;
    private static System.Collections.Generic.HashSet<ObjectId> s_viewbaseBaseline;
    private const string ExportLayoutPath = @"C:\TEMP\pfs_vb_export.dwg";
    private static string LastIsoTempPath;
    private static Vector3d s_isoPipeAxis;
    private static Vector3d s_isoPipeUp;
    private static bool s_isoPipeAxisValid;
    private static ObjectId s_isoPipeId;
    private static Point3d s_isoPipeCenterWcs;
    private static bool s_isoPipeCenterValid;
    private static double s_isoPipeRadiusModel;
    private static double s_isoRealWidth;
    private static double s_isoRealHeight;
    private static bool s_isoRealSizeValid;
    private static string s_isoPipeLineNo;
    private static string s_isoBOP;
    private static string s_isoSize;
    private static string s_isoSupportTag;
    private static string s_isoDesignStd;
    private static string s_isoShortDesc;
    private static string s_isoSupportBI;
    private static string s_isoSupportProfile;
    private static string s_isoSupportDesignation;
    private static System.Collections.Generic.List<string> s_isoSupportDesignations = new System.Collections.Generic.List<string>();
    // CaptureIsoSupportProfile의 원본 3D 파라미터는 페이퍼 치수 작성 시점에도 필요하다.
    private static System.Collections.Generic.Dictionary<string, string> s_isoSupportParams = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    private static string s_isoSupportProfileHeight;
    // 원본 Plant DB에서만 취득한다. side DB로 복제된 Solid3d에는 포트 메타데이터가 없다.
    private sealed class NotabSupportPortSnapshot
    {
      public int Index;
      public string Name;
      public Point3d Wcs;
    }
    private static System.Collections.Generic.List<NotabSupportPortSnapshot> s_isoSupportPorts = new System.Collections.Generic.List<NotabSupportPortSnapshot>();
    private sealed class NotabUboltSnapshot
    {
      public string Tag;
      public Point3d WcsCenter;
    }
    // 자동 포함 원본에서만 확보한다. 상세 DB 복제본은 Plant 속성을 보존하지 않는다.
    private static System.Collections.Generic.List<NotabUboltSnapshot> s_isoUbolts = new System.Collections.Generic.List<NotabUboltSnapshot>();

    // BOMs/PSUtil은 Plant 프로젝트·DataLinksManager 의존이라 side DB에서 호출할 수 없다.
    // 원본 DB 단계에서 값만 복사해 두고, 상세 DB에서는 이 스냅샷만 참조한다.
    // 행 길이가 framework 6칸 / attachment 9칸으로 불균일하므로 명시 필드로 정규화한다.
    private sealed class NotabBomRow
    {
      public string Item;
      public string Category;
      public string Description;
      public string Material;
      public string QuantityOrLength;
      public string Remark;
    }
    private static System.Collections.Generic.List<NotabBomRow> s_isoBomRows = new System.Collections.Generic.List<NotabBomRow>();
    private sealed class NotabBalloonAnchor
    {
      public string Item;
      public Point3d WcsP0;
    }
    private static System.Collections.Generic.List<NotabBalloonAnchor> s_isoBalloonAnchors = new System.Collections.Generic.List<NotabBalloonAnchor>();
    private static Document s_isoOpenPendingTempDoc;
    private static Document s_isoOpenPendingOriginalDoc;
    private static int s_isoOpenSecondIdleAttempts;
    private static Document s_isoDoneSolidDoc;
    private static Document s_isoDoneOriginalDoc;
    private static int s_isoDoneBaseline;
    private static Document s_isoExportPendingSolidDoc;
    private static Document s_isoExportPendingOriginalDoc;
    private static string s_isoExportPath;
    private static Document s_isoClosePendingDoc;
    private static Document s_isoClosePendingOriginalDoc;
    private static Document s_isoRedrawPendingDoc;
    private static int s_isoRedrawAttempt;
    public static ObjectId ViewportId;
    public static string LayerName;
    public static int ItemNo;
    public static bool IsMoreThanOne;
    public static string SupportName;
    public static PSUtil PSUtil;
    public static string DocumentName;


    [CommandMethod("PFSNOTABN1", CommandFlags.Session)]
    public void NotabN1Command()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      Editor ed = doc.Editor;
      PromptSelectionResult psr = ed.GetSelection();
      if (psr.Status != PromptStatus.OK || psr.Value == null || psr.Value.Count == 0)
      {
        PlantOrthoView.FileDiag("PFSNOTABN1 선택 취소/빈 선택");
        ed.WriteMessage("\nPFSNOTABN1: 선택 없음");
        return;
      }

      ObjectId[] selectedIds = psr.Value.GetObjectIds();
      Database sourceDb = null;
      try
      {
        sourceDb = this.CloneSelectionToSideDatabase(doc.Database, selectedIds, "PFSNOTABN1");
        System.Collections.Generic.List<ObjectId> solidIds = this.CollectNotabN1SolidIds(sourceDb);
        string savedPath = this.CloneNotabN1SolidsToResult(sourceDb, solidIds);
        string extLog = this.GetSolidExtentsLog(sourceDb, solidIds);
        PlantOrthoView.FileDiag("PFSNOTABN1 done selected=" + selectedIds.Length + " solids=" + solidIds.Count + " ext=" + extLog + " saved=" + savedPath);
        ed.WriteMessage("\nPFSNOTABN1 solids=" + solidIds.Count + " saved=" + savedPath);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABN1 예외: " + ex.GetType().Name + ": " + ex.Message);
        ed.WriteMessage("\nPFSNOTABN1 error: " + ex.GetType().Name + ": " + ex.Message);
      }
      finally
      {
        if (sourceDb != null)
          sourceDb.Dispose();
      }
    }

    [CommandMethod("PFSNOTABDETAIL", CommandFlags.Session)]
    public void NotabDetailCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      PlantOrthoView.FileDiag("========== PFSNOTABDETAIL RUN START ==========");

      Editor ed = doc.Editor;
      PromptSelectionResult psr = ed.GetSelection();
      if (psr.Status != PromptStatus.OK || psr.Value == null || psr.Value.Count == 0)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL 선택 취소/빈 선택");
        ed.WriteMessage("\nPFSNOTABDETAIL: 선택 없음");
        return;
      }

      this.RunNotabDetailPipeline(doc, psr.Value.GetObjectIds());
    }

    [CommandMethod("PFSNOTABBATCH", CommandFlags.Session)]
    public void NotabDetailBatchCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      PlantOrthoView.FileDiag("========== PFSNOTABBATCH RUN START ==========");

      Editor ed = doc.Editor;
      PromptSelectionResult psr = ed.GetSelection();
      if (psr.Status != PromptStatus.OK || psr.Value == null || psr.Value.Count == 0)
      {
        PlantOrthoView.FileDiag("PFSNOTABBATCH 선택 취소/빈 선택");
        ed.WriteMessage("\nPFSNOTABBATCH: 선택 없음");
        return;
      }

      System.Collections.Generic.List<ObjectId> supportIds = this.CollectSelectedSupportIds(doc.Database, psr.Value.GetObjectIds());
      if (supportIds.Count == 0)
      {
        PlantOrthoView.FileDiag("PFSNOTABBATCH support 없음 selected=" + psr.Value.Count);
        ed.WriteMessage("\nPFSNOTABBATCH: 선택된 서포트 없음");
        return;
      }

      // 계측 전용 실행(cycle103 1단계). 도면을 뽑지 않고 선택 분류 결과만 보고한다.
      // 제외 규칙(2단계)의 근거값을 실측으로 확정하기 위한 것이다.
      if (this.GetEnvDouble("PFS_NOTAB_BATCH_DRYRUN", 0.0, 0.0, 1.0) >= 0.5)
      {
        System.Text.StringBuilder tagList = new System.Text.StringBuilder();
        for (int i = 0; i < supportIds.Count; i++)
        {
          if (tagList.Length > 0) tagList.Append(", ");
          tagList.Append(this.GetSupportNameForLog(doc.Database, supportIds[i]));
        }
        PlantOrthoView.FileDiag("PFSNOTABBATCH DRYRUN 도면 미추출 candidates=" + supportIds.Count + " [" + tagList.ToString() + "]");
        ed.WriteMessage("\nPFSNOTABBATCH: DRYRUN(계측 전용) — 도면을 추출하지 않았습니다. 후보 " + supportIds.Count + "건");
        ed.WriteMessage("\n  " + tagList.ToString());
        return;
      }

      int okCount;
      int failCount;
      System.Collections.Generic.List<string> failTags;
      if (!this.RunNotabBatch(doc, supportIds, out okCount, out failCount, out failTags))
      {
        PlantOrthoView.FileDiag("PFSNOTABBATCH 실행 거부: 다른 무탭 배치가 진행 중입니다.");
        ed.WriteMessage("\nPFSNOTABBATCH: 다른 무탭 배치가 진행 중입니다.");
        return;
      }

      string failLog = failTags.Count == 0 ? string.Empty : string.Join(",", failTags.ToArray());
      PlantOrthoView.FileDiag("PFSNOTABBATCH done total=" + supportIds.Count + " ok=" + okCount + " fail=" + failCount + " fails=[" + failLog + "]");
      ed.WriteMessage("\nPFSNOTABBATCH done total=" + supportIds.Count + " ok=" + okCount + " fail=" + failCount + (failTags.Count == 0 ? string.Empty : " fails=[" + failLog + "]"));
    }

    private static readonly object NotabBatchGate = new object();
    private static bool s_notabBatchRunning;

    // 명령과 팔레트가 공유하는 무탭 배치 파사드. 정적 파이프라인 상태 때문에 동시 실행은 금지한다.
    public bool RunNotabBatch(Document doc, System.Collections.Generic.IList<ObjectId> supportIds,
      out int ok, out int fail, out System.Collections.Generic.List<string> failTags)
    {
      ok = 0;
      fail = 0;
      failTags = new System.Collections.Generic.List<string>();
      if (doc == null || supportIds == null || supportIds.Count == 0)
      {
        PlantOrthoView.FileDiag("PFSNOTABBATCH facade 입력 없음");
        return true;
      }

      lock (NotabBatchGate)
      {
        if (s_notabBatchRunning)
        {
          PlantOrthoView.FileDiag("PFSNOTABBATCH facade 재진입 거부");
          return false;
        }
        s_notabBatchRunning = true;
      }

      try
      {
        for (int i = 0; i < supportIds.Count; i++)
        {
          ObjectId supportId = supportIds[i];
          string tag = this.GetSupportNameForLog(doc.Database, supportId);
          PlantOrthoView.FileDiag("PFSNOTABBATCH [" + (i + 1) + "/" + supportIds.Count + "] tag=" + tag + " 처리");
          try { doc.Editor.WriteMessage("\nPFSNOTABBATCH [" + (i + 1) + "/" + supportIds.Count + "] " + tag); }
          catch (System.Exception ex) { PlantOrthoView.FileDiag("PFSNOTABBATCH 진행 메시지 예외 tag=" + tag + ": " + ex.GetType().Name + ": " + ex.Message); }

          bool extracted = false;
          try { extracted = this.RunNotabDetailPipeline(doc, new ObjectId[] { supportId }); }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSNOTABBATCH 반복 예외 tag=" + tag + " id=" + supportId + ": " + ex.GetType().Name + ": " + ex.Message);
          }

          if (extracted) ok++;
          else { fail++; failTags.Add(tag); }
        }
      }
      finally
      {
        try
        {
          System.GC.Collect();
          System.GC.WaitForPendingFinalizers();
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSNOTABBATCH GC 예외: " + ex.GetType().Name + ": " + ex.Message);
        }
        finally
        {
          lock (NotabBatchGate) { s_notabBatchRunning = false; }
        }
      }
      return true;
    }

    // 자동 테스트(dev 원클릭): SupportName 태그로 서포트를 찾아 선택 없이 추출한다.
    // env PFS_NOTAB_TEST_TAG로 태그 지정, 기본 "GD1-001". 파이프/유볼트는 AutoIncludeRelatedParts가 자동 추가.
    [CommandMethod("PFSNOTABTEST", CommandFlags.Session)]
    public void NotabDetailTestCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      PlantOrthoView.FileDiag("========== PFSNOTABTEST RUN START ==========");
      string raw = System.Environment.GetEnvironmentVariable("PFS_NOTAB_TEST_TAG");
      if (string.IsNullOrWhiteSpace(raw)) raw = "GD1-001";

      string[] tags = raw.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
      int ok = 0;
      foreach (string t in tags)
      {
        string tag = t.Trim();
        if (tag.Length == 0) continue;

        ObjectId supId = this.FindSupportByTag(doc.Database, tag);
        if (supId == ObjectId.Null)
        {
          PlantOrthoView.FileDiag("PFSNOTABTEST 서포트 태그 미발견 tag=" + tag);
          doc.Editor.WriteMessage("\nPFSNOTABTEST: 서포트 '" + tag + "' 없음");
          continue;
        }
        PlantOrthoView.FileDiag("PFSNOTABTEST 서포트 발견 tag=" + tag + " id=" + supId);
        this.RunNotabDetailPipeline(doc, new ObjectId[] { supId });
        ok++;
      }
      PlantOrthoView.FileDiag("PFSNOTABTEST 완료 tags=" + tags.Length + " ok=" + ok);
    }

    private ObjectId FindSupportByTag(Database db, string tag)
    {
      try
      {
        PSUtil ps = Commands.PSUtil;
        if (ps == null) ps = new PSUtil();
        if (ps == null || ps.dl_manager == null)
        {
          PlantOrthoView.FileDiag("PFSNOTABTEST FindSupportByTag dl_manager null");
          return ObjectId.Null;
        }
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          ObjectId msId = this.GetModelSpaceId(db, tr);
          BlockTableRecord ms = msId == ObjectId.Null ? null : tr.GetObject(msId, OpenMode.ForRead) as BlockTableRecord;
          if (ms == null) { tr.Commit(); return ObjectId.Null; }
          foreach (ObjectId eid in ms)
          {
            string cls = eid.ObjectClass == null ? string.Empty : eid.ObjectClass.Name;
            if (cls.IndexOf("Support", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            try
            {
              System.Collections.Specialized.StringCollection names = new System.Collections.Specialized.StringCollection() { "SupportName" };
              System.Collections.Specialized.StringCollection props = ps.dl_manager.GetProperties(eid, names, true);
              string val = props != null && props.Count > 0 ? props[0] : string.Empty;
              if (string.Equals(val, tag, System.StringComparison.OrdinalIgnoreCase)) { tr.Commit(); return eid; }
            }
            catch { }
          }
          tr.Commit();
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABTEST FindSupportByTag 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
      return ObjectId.Null;
    }

    private System.Collections.Generic.List<ObjectId> CollectSelectedSupportIds(Database db, ObjectId[] selectedIds)
    {
      System.Collections.Generic.List<ObjectId> supportIds = new System.Collections.Generic.List<ObjectId>();
      if (db == null || selectedIds == null || selectedIds.Length == 0)
        return supportIds;

      int supportClassCount = 0, nonSupport = 0, probeFailed = 0, excludedCount = 0;
      System.Collections.Generic.Dictionary<string, int> nameCounts =
        new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

      // 부속(유볼트류)은 개별 도면 대상이 아니다. 본체 도면에는 AutoIncludeRelatedParts가 자동 포함한다.
      // 판별자 = Plant ShortDescription. 실측(cycle103): 유볼트 6건 전부 "UB",
      // 본체는 타입 코드 그대로(RC1/GD1/RS1/FS…)라 접두와 일치한다.
      // 유볼트 계열은 카탈로그에 UB / UB1 / UB2 / UBD 4종이 있다(Support 폴더 실재 확인).
      // 하드코딩 금지 — 카탈로그가 늘면 env로 추가한다.
      System.Collections.Generic.HashSet<string> excludeCodes =
        new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
      string excludeRaw = System.Environment.GetEnvironmentVariable("PFS_NOTAB_BATCH_EXCLUDE");
      if (string.IsNullOrWhiteSpace(excludeRaw)) excludeRaw = "UB,UB1,UB2,UBD";
      foreach (string part in excludeRaw.Split(','))
      {
        string code = (part ?? string.Empty).Trim();
        if (code.Length > 0) excludeCodes.Add(code);
      }

      try
      {
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          for (int i = 0; i < selectedIds.Length; i++)
          {
            ObjectId id = selectedIds[i];
            if (id == ObjectId.Null)
              continue;

            try
            {
              string cls = id.ObjectClass == null ? string.Empty : id.ObjectClass.Name;
              if (cls.IndexOf("Support", System.StringComparison.OrdinalIgnoreCase) < 0)
              {
                nonSupport++;
                continue;
              }
              supportClassCount++;

              DBObject obj = tr.GetObject(id, OpenMode.ForRead, false);
              if (obj == null)
                continue;

              // 유볼트도 AcPpDb3dSupport라 클래스만으로는 본체와 갈리지 않는다.
              // ShortDescription으로 부속을 걸러낸다(cycle103).
              string failReason;
              System.Collections.Generic.Dictionary<string, string> probe = this.GetNotabBatchProbeProperties(id, out failReason);
              if (probe.Count == 0) probeFailed++;
              System.Text.StringBuilder line = new System.Text.StringBuilder();
              for (int p = 0; p < NotabBatchProbeNames.Length; p++)
              {
                string key = NotabBatchProbeNames[p];
                string val;
                if (!probe.TryGetValue(key, out val) || string.IsNullOrWhiteSpace(val)) val = "(empty)";
                line.Append(key).Append('=').Append(val).Append(" | ");
              }
              string shortDesc;
              if (!probe.TryGetValue("ShortDescription", out shortDesc)) shortDesc = string.Empty;
              string nameForDup;
              if (!probe.TryGetValue("SupportName", out nameForDup) || string.IsNullOrWhiteSpace(nameForDup))
                nameForDup = "(noname)";
              int dupPrev;
              nameCounts.TryGetValue(nameForDup, out dupPrev);
              nameCounts[nameForDup] = dupPrev + 1;

              // 조회 실패·빈 값은 제외하지 않고 포함한다. 기본 제외로 두면 정상 본체가
              // 조용히 누락된다(자문 지적). 대신 경고로 남겨 운영자가 즉시 알 수 있게 한다.
              bool excluded = !string.IsNullOrWhiteSpace(shortDesc) && excludeCodes.Contains(shortDesc.Trim());
              if (excluded) excludedCount++;
              else supportIds.Add(id);

              PlantOrthoView.FileDiag("PFSNOTABBATCH probe handle=" + id.Handle + " class=" + cls
                + " shortDesc=" + (string.IsNullOrWhiteSpace(shortDesc) ? "(empty)" : shortDesc)
                + " verdict=" + (excluded ? "excluded(attachment)" : "extract")
                + (string.IsNullOrEmpty(failReason) ? string.Empty : " probeFail=" + failReason + " (포함 처리)")
                + (string.IsNullOrWhiteSpace(shortDesc) && string.IsNullOrEmpty(failReason) ? " WARN=shortDesc-empty (포함 처리)" : string.Empty)
                + " { " + line.ToString() + "}");
            }
            catch (System.Exception ex)
            {
              PlantOrthoView.FileDiag("PFSNOTABBATCH support filter 예외 id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
            }
          }
          tr.Commit();
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABBATCH support collect 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      // 중복 SupportName은 같은 서포트가 여러 솔리드로 잡혔다는 신호다(도면 중복 생성 위험).
      System.Text.StringBuilder dupSummary = new System.Text.StringBuilder();
      foreach (System.Collections.Generic.KeyValuePair<string, int> kv in nameCounts)
      { if (kv.Value > 1) { if (dupSummary.Length > 0) dupSummary.Append(","); dupSummary.Append(kv.Key).Append("=").Append(kv.Value); } }
      PlantOrthoView.FileDiag("PFSNOTABBATCH probe-summary selected=" + selectedIds.Length
        + " supportClass=" + supportClassCount + " nonSupport=" + nonSupport
        + " excluded=" + excludedCount + " candidates=" + supportIds.Count
        + " probeFailed=" + probeFailed
        + " excludeCodes=" + excludeRaw
        + " duplicateName{" + dupSummary.ToString() + "}");

      return supportIds;
    }

    // 팔레트 선택 경로도 명령과 동일한 부속 제외 규칙을 사용한다.
    public System.Collections.Generic.List<ObjectId> CollectSelectedSupportIdsForUi(Database db, ObjectId[] selectedIds)
    {
      return this.CollectSelectedSupportIds(db, selectedIds);
    }

    private string GetSupportNameForLog(Database db, ObjectId supportId)
    {
      if (db == null || supportId == ObjectId.Null)
        return "(invalid)";

      string fallback = "SUPPORT_" + supportId.Handle.ToString();
      try
      {
        PSUtil ps = Commands.PSUtil;
        if (ps == null) ps = new PSUtil();
        if (ps == null || ps.dl_manager == null)
        {
          PlantOrthoView.FileDiag("PFSNOTABBATCH SupportName skip: dl_manager null id=" + supportId);
          return fallback;
        }

        System.Collections.Specialized.StringCollection names = new System.Collections.Specialized.StringCollection() { "SupportName" };
        System.Collections.Specialized.StringCollection props = ps.dl_manager.GetProperties(supportId, names, true);
        string tag = props != null && props.Count > 0 ? props[0] : string.Empty;
        if (!string.IsNullOrWhiteSpace(tag))
          return tag;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABBATCH SupportName 예외 id=" + supportId + ": " + ex.GetType().Name + ": " + ex.Message);
      }

      return fallback;
    }

    // 일괄추출 계측용 속성 묶음. 인덱스 고정(values[1] 등)은 조회 목록이 바뀌면 의미가 바뀌므로
    // 반드시 이름→값으로 매핑해 쓴다(cycle103 자문).
    private static readonly string[] NotabBatchProbeNames = new string[]
    { "SupportName", "ShortDescription", "Tag", "TagName", "PartNumber", "SupportDetail", "Description" };

    // 선택 항목의 Plant 속성을 이름 기준으로 읽는다. 실패는 예외를 삼키지 않고 사유를 돌려준다.
    private System.Collections.Generic.Dictionary<string, string> GetNotabBatchProbeProperties(ObjectId id, out string failReason)
    {
      failReason = string.Empty;
      System.Collections.Generic.Dictionary<string, string> map =
        new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
      try
      {
        PSUtil ps = Commands.PSUtil;
        if (ps == null) ps = new PSUtil();
        if (ps == null || ps.dl_manager == null)
        {
          failReason = "dl_manager-null";
          return map;
        }
        System.Collections.Specialized.StringCollection names = new System.Collections.Specialized.StringCollection();
        for (int i = 0; i < NotabBatchProbeNames.Length; i++) names.Add(NotabBatchProbeNames[i]);
        System.Collections.Specialized.StringCollection values = ps.dl_manager.GetProperties(id, names, true);
        if (values == null)
        {
          failReason = "values-null";
          return map;
        }
        for (int i = 0; i < NotabBatchProbeNames.Length; i++)
          map[NotabBatchProbeNames[i]] = i < values.Count ? (values[i] ?? string.Empty) : string.Empty;
      }
      catch (System.Exception ex)
      {
        failReason = ex.GetType().Name + ":" + ex.Message;
      }
      return map;
    }

    private bool RunNotabDetailPipeline(Document doc, ObjectId[] selectedIds)
    {
      s_isoPipeAxisValid = false;
      s_isoPipeAxis = Vector3d.XAxis;
      s_isoPipeUp = Vector3d.ZAxis;
      s_isoPipeId = ObjectId.Null;
      s_isoPipeCenterWcs = Point3d.Origin;
      s_isoPipeCenterValid = false;
      s_isoPipeRadiusModel = double.NaN;
      s_isoRealWidth = 0.0;
      s_isoRealHeight = 0.0;
      s_isoRealSizeValid = false;
      s_isoPipeLineNo = string.Empty;
      s_isoBOP = string.Empty;
      s_isoSize = string.Empty;
      s_isoSupportTag = string.Empty;
      s_isoDesignStd = string.Empty;
      s_isoShortDesc = string.Empty;
      s_isoSupportBI = string.Empty;
      s_isoSupportProfile = string.Empty;
      s_isoSupportDesignation = string.Empty;
      s_isoSupportDesignations.Clear();
      s_isoSupportParams.Clear();
      s_isoSupportProfileHeight = string.Empty;
      s_isoSupportPorts.Clear();
      s_isoUbolts.Clear();
      s_isoBomRows.Clear();
      s_isoBalloonAnchors.Clear();
      // [MEASURE cycle94-M5] 배치 처리(PFSNOTABBATCH)에서 정적 상태가 대상 간 누출되지 않는지 검증.
      // 여기서 전부 0이어야 한다.
      PlantOrthoView.FileDiag("MEASURE state-reset ports=" + s_isoSupportPorts.Count
        + " ubolts=" + s_isoUbolts.Count + " params=" + s_isoSupportParams.Count
        + " designations=" + s_isoSupportDesignations.Count
        + " bomRows=" + s_isoBomRows.Count + " balloonAnchors=" + s_isoBalloonAnchors.Count
        + " designStd='" + (s_isoDesignStd ?? string.Empty) + "'");

      Editor ed = doc.Editor;
      selectedIds = this.AutoIncludeRelatedParts(doc.Database, selectedIds);
      Vector3d pipeAxis;
      ObjectId pipeId;
      s_isoPipeAxisValid = this.TryGetSelectionPipeAxis(doc.Database, selectedIds, out pipeAxis, out pipeId);
      s_isoPipeId = pipeId;
      s_isoPipeAxis = s_isoPipeAxisValid ? pipeAxis : Vector3d.XAxis;
      this.CaptureNotabPipeCenter(doc.Database, s_isoPipeId);
      this.CaptureIsoSelectionMetrics(doc.Database, selectedIds);

      // [방어] 파이프라인이 사이드DB/Plant explode 부작용으로 활성 뷰를 Perspective로 뒤집는
      // 현상 대비: 진입 시 PERSPECTIVE 저장 → finally에서 원복. 진입/종료값을 로그로 남겨
      // 어느 스텝이 뒤집는지 계측한다.
      object savedPerspective = null;
      try { savedPerspective = Application.GetSystemVariable("PERSPECTIVE"); }
      catch (System.Exception px) { PlantOrthoView.FileDiag("PFSNOTABDETAIL persp 읽기 예외: " + px.GetType().Name + ": " + px.Message); }
      PlantOrthoView.FileDiag("PFSNOTABDETAIL persp enter=" + (savedPerspective == null ? "(null)" : savedPerspective.ToString()) + " view={" + this.DescribeActiveViewPerspective(doc) + "}");
      Commands.ArmPerspGuard(savedPerspective);

      Database sourceDb = null;
      try
      {
        sourceDb = this.CloneSelectionToSideDatabase(doc.Database, selectedIds, "PFSNOTABDETAIL");
        System.Collections.Generic.List<ObjectId> solidIds = this.CollectNotabN1SolidIds(sourceDb);
        if (solidIds.Count == 0)
          throw new System.InvalidOperationException("solidIds 없음");

        string savedPath = this.CreateNotabDetailDrawing(ref sourceDb, solidIds);
        this.TryReopenSetNotabPaperSpace(savedPath);
        PlantOrthoView.FileDiag("PFSNOTABDETAIL done selected=" + selectedIds.Length + " solids=" + solidIds.Count + " axis=" + this.FormatVectorForCommand(s_isoPipeAxis) + " up=" + this.FormatVectorForCommand(s_isoPipeUp) + " saved=" + savedPath);
        ed.WriteMessage("\nPFSNOTABDETAIL saved=" + savedPath);
        return true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL 예외: " + ex.GetType().Name + ": " + ex.Message);
        ed.WriteMessage("\nPFSNOTABDETAIL error: " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
      finally
      {
        if (sourceDb != null)
          sourceDb.Dispose();

        // PERSPECTIVE 원복: sysvar 복원만으로는 활성 뷰포트 Gs 뷰가 즉시 갱신 안 될 수 있어
        // REGEN까지 병행한다(자문 근거). 종료값도 로그로 남겨 재현 확인.
        try
        {
          object nowPerspective = Application.GetSystemVariable("PERSPECTIVE");
          PlantOrthoView.FileDiag("PFSNOTABDETAIL persp exit=" + (nowPerspective == null ? "(null)" : nowPerspective.ToString()) + " restore=" + (savedPerspective == null ? "(null)" : savedPerspective.ToString()) + " view={" + this.DescribeActiveViewPerspective(doc) + "}");
          if (savedPerspective != null && !object.Equals(nowPerspective, savedPerspective))
          {
            Application.SetSystemVariable("PERSPECTIVE", savedPerspective);
            try { doc.Editor.Regen(); }
            catch (System.Exception rex) { PlantOrthoView.FileDiag("PFSNOTABDETAIL persp Regen 예외: " + rex.GetType().Name + ": " + rex.Message); }
            PlantOrthoView.FileDiag("PFSNOTABDETAIL persp 원복 적용 " + nowPerspective + " -> " + savedPerspective);
          }
        }
        catch (System.Exception cx) { PlantOrthoView.FileDiag("PFSNOTABDETAIL persp 원복 예외: " + cx.GetType().Name + ": " + cx.Message); }
      }
    }


    private Database CloneSelectionToSideDatabase(Database originalDb, ObjectId[] selectedIds, string logPrefix)
    {
      if (originalDb == null)
        throw new System.ArgumentNullException("originalDb");
      if (selectedIds == null || selectedIds.Length == 0)
        throw new System.ArgumentException("selectedIds 없음", "selectedIds");

      Database sideDb = null;
      try
      {
        sideDb = new Database(true, true);
        ObjectId sideMsId;
        using (Transaction tr = sideDb.TransactionManager.StartTransaction())
        {
          BlockTable bt = tr.GetObject(sideDb.BlockTableId, OpenMode.ForRead) as BlockTable;
          sideMsId = bt[BlockTableRecord.ModelSpace];
          tr.Commit();
        }

        ObjectIdCollection ids = new ObjectIdCollection();
        foreach (ObjectId id in selectedIds)
          ids.Add(id);

        Database oldWorking = HostApplicationServices.WorkingDatabase;
        try
        {
          HostApplicationServices.WorkingDatabase = sideDb;
          IdMapping idMap = new IdMapping();
          originalDb.WblockCloneObjects(ids, sideMsId, idMap, DuplicateRecordCloning.Replace, false);
        }
        finally
        {
          HostApplicationServices.WorkingDatabase = oldWorking;
        }

        int cloned = 0;
        using (Transaction tr = sideDb.TransactionManager.StartTransaction())
        {
          BlockTable bt = tr.GetObject(sideDb.BlockTableId, OpenMode.ForRead) as BlockTable;
          BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
          foreach (ObjectId id in ms)
          {
            cloned++;
            DBObject obj = tr.GetObject(id, OpenMode.ForRead, false);
            Entity ent = obj as Entity;
            string className = id.ObjectClass == null ? "null" : id.ObjectClass.Name;
            string typeName = obj == null ? "null" : obj.GetType().Name;
            string bounds = this.GetEntityExtentsLog(ent);
            PlantOrthoView.FileDiag(logPrefix + " input id=" + id + " class=" + className + " type=" + typeName + " bounds=" + bounds);
          }
          tr.Commit();
        }

        PlantOrthoView.FileDiag(logPrefix + " sideClone selected=" + selectedIds.Length + " cloned=" + cloned);
        return sideDb;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag(logPrefix + " sideClone 예외: " + ex.GetType().Name + ": " + ex.Message);
        if (sideDb != null)
          sideDb.Dispose();
        throw;
      }
    }

    private System.Collections.Generic.List<ObjectId> CollectNotabN1SolidIds(Database sourceDb)
    {
      var solidIds = new System.Collections.Generic.List<ObjectId>();
      if (sourceDb == null)
        return solidIds;

      using (Transaction tr = sourceDb.TransactionManager.StartTransaction())
      {
        BlockTable bt = tr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead) as BlockTable;
        BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
        var ids = new System.Collections.Generic.List<ObjectId>();
        foreach (ObjectId id in ms)
          ids.Add(id);

        foreach (ObjectId id in ids)
        {
          Entity ent = null;
          try
          {
            ent = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
          }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSNOTABN1 open 예외 id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
            continue;
          }

          if (ent == null || ent.IsErased)
            continue;

          this.CollectSolidsRecursive(tr, ms, ent, solidIds, 0);
        }
        tr.Commit();
      }

      PlantOrthoView.FileDiag("PFSNOTABN1 collect solids=" + solidIds.Count);
      return solidIds;
    }

    private void CollectSolidsRecursive(Transaction tr, BlockTableRecord modelSpace, Entity ent, System.Collections.Generic.List<ObjectId> solidIds, int depth)
    {
      if (tr == null || modelSpace == null || ent == null || solidIds == null)
        return;
      if (depth > 6)
      {
        PlantOrthoView.FileDiag("PFSNOTABN1 recurse depthLimit type=" + ent.GetType().Name + " depth=" + depth);
        return;
      }

      Solid3d existingSolid = ent as Solid3d;
      if (existingSolid != null)
      {
        if (existingSolid.ObjectId.IsNull)
        {
          modelSpace.AppendEntity(existingSolid);
          tr.AddNewlyCreatedDBObject(existingSolid, true);
        }
        if (!solidIds.Contains(existingSolid.ObjectId))
          solidIds.Add(existingSolid.ObjectId);
        PlantOrthoView.FileDiag("PFSNOTABN1 solid depth=" + depth + " id=" + existingSolid.ObjectId + " ext=" + this.GetEntityExtentsLog(existingSolid));
        return;
      }

      DBObjectCollection exploded = new DBObjectCollection();
      try
      {
        ent.Explode(exploded);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABN1 explode 예외 depth=" + depth + " type=" + ent.GetType().Name + ": " + ex.GetType().Name + ": " + ex.Message);
        return;
      }

      PlantOrthoView.FileDiag("PFSNOTABN1 explode depth=" + depth + " type=" + ent.GetType().Name + " produced=" + exploded.Count);
      if (exploded.Count == 0)
        return;

      foreach (DBObject child in exploded)
      {
        Entity childEntity = child as Entity;
        if (childEntity == null)
        {
          PlantOrthoView.FileDiag("PFSNOTABN1 child skip depth=" + depth + " type=" + (child == null ? "null" : child.GetType().Name));
          if (child != null)
            child.Dispose();
          continue;
        }

        try
        {
          this.CollectSolidsRecursive(tr, modelSpace, childEntity, solidIds, depth + 1);
        }
        finally
        {
          if (childEntity.ObjectId.IsNull)
            childEntity.Dispose();
        }
      }
    }

    private string CloneNotabN1SolidsToResult(Database sourceDb, System.Collections.Generic.List<ObjectId> solidIds)
    {
      if (sourceDb == null)
        throw new System.ArgumentNullException("sourceDb");
      if (solidIds == null || solidIds.Count == 0)
        throw new System.InvalidOperationException("solidIds 없음");

      string tempDir = @"C:\Temp";
      System.IO.Directory.CreateDirectory(tempDir);
      string path = System.IO.Path.Combine(tempDir, "notab_n1_solids_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".dwg");
      Database resultDb = null;
      try
      {
        resultDb = new Database(true, true);
        ObjectId resultMsId;
        using (Transaction tr = resultDb.TransactionManager.StartTransaction())
        {
          BlockTable bt = tr.GetObject(resultDb.BlockTableId, OpenMode.ForRead) as BlockTable;
          resultMsId = bt[BlockTableRecord.ModelSpace];
          tr.Commit();
        }

        ObjectIdCollection ids = new ObjectIdCollection();
        foreach (ObjectId id in solidIds)
          ids.Add(id);

        Database oldWorking = HostApplicationServices.WorkingDatabase;
        try
        {
          HostApplicationServices.WorkingDatabase = resultDb;
          IdMapping idMap = new IdMapping();
          sourceDb.WblockCloneObjects(ids, resultMsId, idMap, DuplicateRecordCloning.Replace, false);
        }
        finally
        {
          HostApplicationServices.WorkingDatabase = oldWorking;
        }

        string resultExt = this.GetModelSpaceSolidExtentsLog(resultDb);
        resultDb.SaveAs(path, DwgVersion.Current);
        PlantOrthoView.FileDiag("PFSNOTABN1 result solids=" + solidIds.Count + " ext=" + resultExt + " saved=" + path);
        return path;
      }
      finally
      {
        if (resultDb != null)
          resultDb.Dispose();
      }
    }

    private string GetModelSpaceSolidExtentsLog(Database db)
    {
      if (db == null)
        return "db=null";

      int solids = 0;
      Extents3d? ext = null;
      using (Transaction tr = db.TransactionManager.StartTransaction())
      {
        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
        BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
        foreach (ObjectId id in ms)
        {
          Solid3d solid = tr.GetObject(id, OpenMode.ForRead, false) as Solid3d;
          if (solid == null)
            continue;
          solids++;
          this.AddExtents(ref ext, solid.GeometricExtents);
        }
        tr.Commit();
      }

      return "solids=" + solids + " ext=" + this.FormatExtents(ext);
    }

    private string GetSolidExtentsLog(Database db, System.Collections.Generic.List<ObjectId> solidIds)
    {
      if (db == null || solidIds == null || solidIds.Count == 0)
        return "empty";

      Extents3d? ext = null;
      int ok = 0;
      using (Transaction tr = db.TransactionManager.StartTransaction())
      {
        foreach (ObjectId id in solidIds)
        {
          try
          {
            Solid3d solid = tr.GetObject(id, OpenMode.ForRead, false) as Solid3d;
            if (solid == null)
              continue;
            ok++;
            this.AddExtents(ref ext, solid.GeometricExtents);
          }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSNOTABN1 ext 예외 id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
          }
        }
        tr.Commit();
      }

      return "solids=" + ok + " ext=" + this.FormatExtents(ext);
    }

    private string GetEntityExtentsLog(Entity ent)
    {
      if (ent == null)
        return "entity=null";
      try
      {
        return this.FormatExtents(ent.GeometricExtents);
      }
      catch (System.Exception ex)
      {
        return "ERR " + ex.GetType().Name + ": " + ex.Message;
      }
    }

    private void AddExtents(ref Extents3d? target, Extents3d source)
    {
      if (!target.HasValue)
      {
        target = source;
        return;
      }

      Extents3d ext = target.Value;
      ext.AddExtents(source);
      target = ext;
    }

    private string FormatExtents(Extents3d? ext)
    {
      if (!ext.HasValue)
        return "none";
      return this.FormatExtents(ext.Value);
    }

    private string FormatExtents(Extents3d ext)
    {
      return "(" + this.FormatNumber(ext.MinPoint.X) + "," + this.FormatNumber(ext.MinPoint.Y) + "," + this.FormatNumber(ext.MinPoint.Z) + ")~(" + this.FormatNumber(ext.MaxPoint.X) + "," + this.FormatNumber(ext.MaxPoint.Y) + "," + this.FormatNumber(ext.MaxPoint.Z) + ")";
    }
    [CommandMethod("PFSVBISOCLONE")]
    public void VbIsoCloneCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;
      s_isoPipeAxisValid = false;
      s_isoPipeAxis = Vector3d.XAxis;
      s_isoPipeUp = Vector3d.ZAxis;
      s_isoPipeId = ObjectId.Null;
      s_isoPipeCenterWcs = Point3d.Origin;
      s_isoPipeCenterValid = false;
      s_isoPipeRadiusModel = double.NaN;
      s_isoRealWidth = 0.0;
      s_isoRealHeight = 0.0;
      s_isoRealSizeValid = false;
      s_isoPipeLineNo = string.Empty;
      s_isoBOP = string.Empty;
      s_isoSize = string.Empty;
      s_isoSupportTag = string.Empty;
      s_isoDesignStd = string.Empty;
      s_isoShortDesc = string.Empty;
      s_isoSupportBI = string.Empty;
      s_isoSupportProfile = string.Empty;
      s_isoSupportDesignation = string.Empty;
      s_isoSupportDesignations.Clear();
      s_isoSupportParams.Clear();
      s_isoSupportProfileHeight = string.Empty;
      Editor ed = doc.Editor;
      PromptSelectionResult psr = ed.GetSelection();
      if (psr.Status != PromptStatus.OK || psr.Value == null || psr.Value.Count == 0)
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE 선택 취소/빈 선택");
        ed.WriteMessage("\nPFSVBISOCLONE: 선택 없음");
        return;
      }

      ObjectId[] selectedIds = psr.Value.GetObjectIds();
      Vector3d pipeAxis;
      ObjectId pipeId;
      s_isoPipeAxisValid = this.TryGetSelectionPipeAxis(doc.Database, selectedIds, out pipeAxis, out pipeId);
      s_isoPipeId = pipeId;
      s_isoPipeAxis = s_isoPipeAxisValid ? pipeAxis : Vector3d.XAxis;
      PlantOrthoView.FileDiag("PFSVBISOCLONE pipeAxis=" + this.FormatVectorForCommand(s_isoPipeAxis) + " valid=" + s_isoPipeAxisValid);
      this.CaptureIsoSelectionMetrics(doc.Database, selectedIds);

      ObjectIdCollection ids = new ObjectIdCollection();
      foreach (ObjectId id in selectedIds)
        ids.Add(id);
      string tempDir = @"C:\Temp";
      string tempPath = System.IO.Path.Combine(tempDir, "pfs_iso_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".dwg");
      int cloned = 0;
      int proxy = 0;
      int noBounds = 0;

      Database tempDb = null;
      try
      {
        System.IO.Directory.CreateDirectory(tempDir);
        tempDb = new Database(true, true);
        ObjectId tempMsId;
        using (Transaction tr = tempDb.TransactionManager.StartTransaction())
        {
          BlockTable bt = tr.GetObject(tempDb.BlockTableId, OpenMode.ForRead) as BlockTable;
          tempMsId = bt[BlockTableRecord.ModelSpace];
          tr.Commit();
        }

        Database oldWorking = HostApplicationServices.WorkingDatabase;
        try
        {
          HostApplicationServices.WorkingDatabase = tempDb;
          IdMapping idMap = new IdMapping();
          doc.Database.WblockCloneObjects(ids, tempMsId, idMap, DuplicateRecordCloning.Replace, false);
        }
        finally
        {
          HostApplicationServices.WorkingDatabase = oldWorking;
        }

        using (Transaction tr = tempDb.TransactionManager.StartTransaction())
        {
          BlockTable bt = tr.GetObject(tempDb.BlockTableId, OpenMode.ForRead) as BlockTable;
          BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
          foreach (ObjectId id in ms)
          {
            cloned++;
            DBObject obj = tr.GetObject(id, OpenMode.ForRead, false);
            Entity ent = obj as Entity;
            string className = id.ObjectClass == null ? "null" : id.ObjectClass.Name;
            string typeName = obj == null ? "null" : obj.GetType().Name;
            bool isProxy = className.IndexOf("Proxy", System.StringComparison.OrdinalIgnoreCase) >= 0 || typeName.IndexOf("Proxy", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (isProxy)
              proxy++;
            string bounds = "n/a";
            if (ent != null)
            {
              try
              {
                Extents3d ext = ent.GeometricExtents;
                bounds = ext.MinPoint + "~" + ext.MaxPoint;
              }
              catch (System.Exception ex)
              {
                noBounds++;
                bounds = "ERR " + ex.GetType().Name + ": " + ex.Message;
              }
            }
            else
            {
              noBounds++;
            }
            PlantOrthoView.FileDiag("PFSVBISOCLONE ent class=" + className + " type=" + typeName + " proxy=" + isProxy + " bounds=" + bounds);
          }
          tr.Commit();
        }

        tempDb.SaveAs(tempPath, DwgVersion.Current);
        LastIsoTempPath = tempPath;
        PlantOrthoView.FileDiag("PFSVBISOCLONE cloned=" + cloned + " proxy=" + proxy + " noBounds=" + noBounds + " saved=" + tempPath);
        ed.WriteMessage("\nPFSVBISOCLONE cloned=" + cloned + " proxy=" + proxy + " noBounds=" + noBounds + " saved=" + tempPath);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE 예외: " + ex.GetType().Name + ": " + ex.Message);
        ed.WriteMessage("\nPFSVBISOCLONE error: " + ex.GetType().Name + ": " + ex.Message);
      }
      finally
      {
        if (tempDb != null)
          tempDb.Dispose();
      }
    }

    [CommandMethod("PFSVBISOOPEN", CommandFlags.Session)]
    public void VbIsoOpenCommand()
    {
      Document originalDoc = Application.DocumentManager.MdiActiveDocument;
      Editor originalEditor = originalDoc == null ? null : originalDoc.Editor;
      string path = LastIsoTempPath;
      if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
      {
        PlantOrthoView.FileDiag("PFSVBISOOPEN temp path 없음 path=" + (path ?? "null"));
        if (originalEditor != null)
          originalEditor.WriteMessage("\nPFSVBISOOPEN: temp dwg 없음");
        return;
      }

      Document tempDoc = null;
      try
      {
        tempDoc = Application.DocumentManager.Open(path, false);
        Application.DocumentManager.MdiActiveDocument = tempDoc;
        bool active = object.ReferenceEquals(Application.DocumentManager.MdiActiveDocument, tempDoc);
        PlantOrthoView.FileDiag("PFSVBISOOPEN opened path=" + path + " active=" + active);
        if (!active)
        {
          s_isoOpenPendingTempDoc = tempDoc;
          s_isoOpenPendingOriginalDoc = originalDoc;
          Application.Idle -= this.VbIsoOpenOnIdle;
          Application.Idle += this.VbIsoOpenOnIdle;
          PlantOrthoView.FileDiag("PFSVBISOOPEN active=false, Idle 위임");
          return;
        }

        this.RunIsoOpenViewbase(tempDoc, originalDoc, false);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOOPEN 예외: " + ex.GetType().Name + ": " + ex.Message);
        if (originalEditor != null)
          originalEditor.WriteMessage("\nPFSVBISOOPEN error: " + ex.GetType().Name + ": " + ex.Message);
        if (tempDoc != null)
          this.CloseIsoTempDocument(tempDoc, originalDoc);
      }
    }

    private void VbIsoOpenOnIdle(object sender, System.EventArgs e)
    {
      Application.Idle -= this.VbIsoOpenOnIdle;
      Document tempDoc = s_isoOpenPendingTempDoc;
      Document originalDoc = s_isoOpenPendingOriginalDoc;
      s_isoOpenPendingTempDoc = null;
      s_isoOpenPendingOriginalDoc = null;
      if (tempDoc == null)
      {
        PlantOrthoView.FileDiag("PFSVBISOOPEN Idle 중단: tempDoc null");
        return;
      }

      try
      {
        Application.DocumentManager.MdiActiveDocument = tempDoc;
        bool active = object.ReferenceEquals(Application.DocumentManager.MdiActiveDocument, tempDoc);
        PlantOrthoView.FileDiag("PFSVBISOOPEN Idle active=" + active);
        if (!active)
        {
          PlantOrthoView.FileDiag("PFSVBISOOPEN Idle 중단: tempDoc 활성화 실패");
          this.CloseIsoTempDocument(tempDoc, originalDoc);
          return;
        }
        this.RunIsoOpenViewbase(tempDoc, originalDoc, true);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOOPEN Idle 예외: " + ex.GetType().Name + ": " + ex.Message);
        this.CloseIsoTempDocument(tempDoc, originalDoc);
      }
    }

    private System.Collections.Generic.List<ObjectId> ExplodeModelSpaceToSolidIds(Document tempDoc)
    {
      var solidIds = new System.Collections.Generic.List<ObjectId>();
      if (tempDoc == null)
        return solidIds;

      using (tempDoc.LockDocument())
      using (Transaction tr = tempDoc.Database.TransactionManager.StartTransaction())
      {
        BlockTable bt = tr.GetObject(tempDoc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
        if (bt == null || !bt.Has(BlockTableRecord.ModelSpace))
        {
          PlantOrthoView.FileDiag("PFSVBISOOPEN explode ModelSpace 없음");
          tr.Commit();
          return solidIds;
        }

        BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
        if (ms == null)
        {
          PlantOrthoView.FileDiag("PFSVBISOOPEN explode ModelSpace BTR null");
          tr.Commit();
          return solidIds;
        }

        var ids = new System.Collections.Generic.List<ObjectId>();
        foreach (ObjectId id in ms)
          ids.Add(id);

        foreach (ObjectId id in ids)
        {
          Entity entity = null;
          try
          {
            entity = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
          }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSVBISOOPEN explode open 예외 id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
            continue;
          }

          if (entity == null || entity.IsErased)
            continue;

          if (entity is Solid3d)
          {
            solidIds.Add(id);
            continue;
          }

          this.AppendExplodedSolidIds(tr, ms, entity, solidIds);
        }

        tr.Commit();
      }

      PlantOrthoView.FileDiag("PFSVBISOOPEN explode solids=" + solidIds.Count);
      return solidIds;
    }

    private void AppendExplodedSolidIds(Transaction tr, BlockTableRecord modelSpace, Entity source, System.Collections.Generic.List<ObjectId> solidIds)
    {
      if (tr == null || modelSpace == null || source == null || solidIds == null)
        return;

      DBObjectCollection exploded = new DBObjectCollection();
      try
      {
        source.Explode(exploded);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOOPEN explode 예외 type=" + source.GetType().Name + ": " + ex.GetType().Name + ": " + ex.Message);
        return;
      }

      if (exploded.Count == 0)
      {
        PlantOrthoView.FileDiag("PFSVBISOOPEN explode skip type=" + source.GetType().Name + " produced=0");
        return;
      }

      foreach (DBObject child in exploded)
      {
        Entity childEntity = child as Entity;
        if (childEntity == null)
        {
          child.Dispose();
          continue;
        }

        Solid3d solid = childEntity as Solid3d;
        if (solid != null)
        {
          modelSpace.AppendEntity(solid);
          tr.AddNewlyCreatedDBObject(solid, true);
          solidIds.Add(solid.ObjectId);
          continue;
        }

        this.AppendExplodedSolidIds(tr, modelSpace, childEntity, solidIds);
        childEntity.Dispose();
      }
    }

    private string CloneSolidsToSecondTemp(Database sourceDb, System.Collections.Generic.List<ObjectId> solidIds)
    {
      if (sourceDb == null || solidIds == null || solidIds.Count == 0)
        throw new System.InvalidOperationException("solidIds 없음");

      string tempDir = @"C:\Temp";
      System.IO.Directory.CreateDirectory(tempDir);
      string path2 = System.IO.Path.Combine(tempDir, "pfs_iso_solids_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".dwg");
      Database tempDb2 = null;
      try
      {
        tempDb2 = new Database(true, true);
        ObjectId ms2Id;
        using (Transaction tr = tempDb2.TransactionManager.StartTransaction())
        {
          BlockTable bt = tr.GetObject(tempDb2.BlockTableId, OpenMode.ForRead) as BlockTable;
          ms2Id = bt[BlockTableRecord.ModelSpace];
          tr.Commit();
        }

        ObjectIdCollection ids = new ObjectIdCollection();
        foreach (ObjectId id in solidIds)
          ids.Add(id);

        Database oldWorking = HostApplicationServices.WorkingDatabase;
        try
        {
          HostApplicationServices.WorkingDatabase = tempDb2;
          IdMapping idMap = new IdMapping();
          sourceDb.WblockCloneObjects(ids, ms2Id, idMap, DuplicateRecordCloning.Replace, false);
        }
        finally
        {
          HostApplicationServices.WorkingDatabase = oldWorking;
        }

        tempDb2.SaveAs(path2, DwgVersion.Current);
        PlantOrthoView.FileDiag("PFSVBISOOPEN clone solids=" + solidIds.Count + " saved=" + path2);
        return path2;
      }
      finally
      {
        if (tempDb2 != null)
          tempDb2.Dispose();
      }
    }

    private void RunIsoOpenViewbase(Document tempDoc, Document originalDoc, bool fromIdle)
    {
      Editor ted = tempDoc.Editor;
      bool active = object.ReferenceEquals(Application.DocumentManager.MdiActiveDocument, tempDoc);
      string path2 = null;

      try
      {
        System.Collections.Generic.List<ObjectId> solidIds = this.ExplodeModelSpaceToSolidIds(tempDoc);
        if (solidIds.Count == 0)
        {
          PlantOrthoView.FileDiag("PFSVBISOOPEN VIEWBASE 중단: explode solids=0 active=" + active + " fromIdle=" + fromIdle);
          ted.WriteMessage("\nPFSVBISOOPEN stopped: explode solids=0");
          throw new System.InvalidOperationException("explode solids=0");
        }

        path2 = this.CloneSolidsToSecondTemp(tempDoc.Database, solidIds);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOOPEN M3 준비 예외: " + ex.GetType().Name + ": " + ex.Message + " fromIdle=" + fromIdle);
        ted.WriteMessage("\nPFSVBISOOPEN prepare error: " + ex.GetType().Name + ": " + ex.Message);
      }
      finally
      {
        this.CloseIsoTempDocument(tempDoc, originalDoc);
      }

      if (string.IsNullOrEmpty(path2) || !System.IO.File.Exists(path2))
        return;

      this.OpenSecondTempAndRunViewbase(path2, originalDoc, fromIdle);
    }

    private void OpenSecondTempAndRunViewbase(string path2, Document originalDoc, bool fromIdle)
    {
      Document solidDoc = null;
      try
      {
        solidDoc = Application.DocumentManager.Open(path2, false);
        Application.DocumentManager.MdiActiveDocument = solidDoc;
        bool active = object.ReferenceEquals(Application.DocumentManager.MdiActiveDocument, solidDoc);
        PlantOrthoView.FileDiag("PFSVBISOOPEN2 opened path=" + path2 + " active=" + active + " fromIdle=" + fromIdle + " -> Idle 위임");
        s_isoOpenPendingTempDoc = solidDoc;
        s_isoOpenPendingOriginalDoc = originalDoc;
        s_isoOpenSecondIdleAttempts = 0;
        Application.Idle -= this.VbIsoOpenSecondOnIdle;
        Application.Idle += this.VbIsoOpenSecondOnIdle;
        return;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOOPEN2 예외: " + ex.GetType().Name + ": " + ex.Message);
        if (solidDoc != null)
          this.CloseIsoTempDocument(solidDoc, originalDoc);
      }
    }
    private void VbIsoOpenSecondOnIdle(object sender, System.EventArgs e)
    {
      Application.Idle -= this.VbIsoOpenSecondOnIdle;
      Document solidDoc = s_isoOpenPendingTempDoc;
      Document originalDoc = s_isoOpenPendingOriginalDoc;
      s_isoOpenPendingTempDoc = null;
      s_isoOpenPendingOriginalDoc = null;
      if (solidDoc == null)
      {
        PlantOrthoView.FileDiag("PFSVBISOOPEN2 Idle 중단: solidDoc null");
        return;
      }

      try
      {
        Application.DocumentManager.MdiActiveDocument = solidDoc;
        bool active = object.ReferenceEquals(Application.DocumentManager.MdiActiveDocument, solidDoc);
        string cmdNames = (Application.GetSystemVariable("CMDNAMES") ?? "").ToString();
        string ctab = (Application.GetSystemVariable("CTAB") ?? "").ToString();
        short tileMode = (short)Application.GetSystemVariable("TILEMODE");
        PlantOrthoView.FileDiag("PFSVBISOOPEN2 state CTAB=" + ctab + " TILEMODE=" + tileMode + " CMDNAMES='" + cmdNames + "' active=" + active + " attempt=" + s_isoOpenSecondIdleAttempts);
        if (!active)
        {
          PlantOrthoView.FileDiag("PFSVBISOOPEN2 Idle 중단: solidDoc 활성화 실패");
          this.CloseIsoTempDocument(solidDoc, originalDoc);
          return;
        }
        if (!string.IsNullOrEmpty(cmdNames))
        {
          s_isoOpenSecondIdleAttempts++;
          if (s_isoOpenSecondIdleAttempts <= 5)
          {
            s_isoOpenPendingTempDoc = solidDoc;
            s_isoOpenPendingOriginalDoc = originalDoc;
            Application.Idle -= this.VbIsoOpenSecondOnIdle;
            Application.Idle += this.VbIsoOpenSecondOnIdle;
            PlantOrthoView.FileDiag("PFSVBISOOPEN2 Idle 재무장 CMDNAMES='" + cmdNames + "' attempt=" + s_isoOpenSecondIdleAttempts);
            return;
          }

          PlantOrthoView.FileDiag("PFSVBISOOPEN2 Idle 중단: CMDNAMES 미해소 limit=5 last='" + cmdNames + "'");
          this.CloseIsoTempDocument(solidDoc, originalDoc);
          return;
        }
        s_isoOpenSecondIdleAttempts = 0;
        this.RunViewbaseOnSolidTemp(solidDoc, originalDoc, true);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOOPEN2 Idle 예외: " + ex.GetType().Name + ": " + ex.Message);
        this.CloseIsoTempDocument(solidDoc, originalDoc);
      }
    }

    private void RunViewbaseOnSolidTemp(Document solidDoc, Document originalDoc, bool fromIdle)
    {
      Editor ted = solidDoc.Editor;
      int baseline = 0;
      bool active = object.ReferenceEquals(Application.DocumentManager.MdiActiveDocument, solidDoc);

      try
      {
        ObjectId layoutBtrId;
        using (Transaction tr = solidDoc.Database.TransactionManager.StartTransaction())
        {
          layoutBtrId = this.GetLayoutBlockTableRecordId(solidDoc.Database, tr, "Layout1");
          if (layoutBtrId.IsNull)
          {
            PlantOrthoView.FileDiag("PFSVBISOOPEN2 Layout1 없음 active=" + active + " fromIdle=" + fromIdle);
            tr.Commit();
            throw new System.InvalidOperationException("Layout1 없음");
          }
          BlockTableRecord btr = tr.GetObject(layoutBtrId, OpenMode.ForRead) as BlockTableRecord;
          if (btr != null)
            foreach (ObjectId id in btr)
              baseline++;
          tr.Commit();
        }

        s_isoDoneSolidDoc = solidDoc;
        s_isoDoneOriginalDoc = originalDoc;
        s_isoDoneBaseline = baseline;
        using (solidDoc.LockDocument())
        {
          ted.Regen();
          PlantOrthoView.FileDiag("PFSVBISOOPEN2 Regen 완료 fromIdle=" + fromIdle);
        }

        string vb;
        string dirLog;
        if (s_isoPipeAxisValid)
        {
          string vpoint = this.FormatVectorForCommand(s_isoPipeAxis);
          vb = "_.FILEDIA\n0\n_.CMDDIA\n0\n"
            + "_.-VPOINT\n" + vpoint + "\n"
            + "_.VIEWBASE\n_M\n_E\nLayout1\n_O\n_Current\n100,100\n\n\n"
            + "_.FILEDIA\n1\n_.CMDDIA\n1\n"
            + "PFSVBISODONE\n";
          dirLog = "pipeAxis vpoint=" + vpoint;
        }
        else
        {
          vb = "_.FILEDIA\n0\n_.CMDDIA\n0\n"
            + "_.VIEWBASE\n_M\n_E\n\n100,100\n\n\n"
            + "_.FILEDIA\n1\n_.CMDDIA\n1\n"
            + "PFSVBISODONE\n";
          dirLog = "default";
        }
        solidDoc.SendStringToExecute(vb, true, false, false);
        PlantOrthoView.FileDiag("PFSVBISOOPEN2 VIEWBASE dir=" + dirLog + " queued baseline=" + baseline + " fromIdle=" + fromIdle + " cmd=" + vb.Replace("\n", "|"));
        ted.WriteMessage("\nPFSVBISOOPEN2 VIEWBASE queued");
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOOPEN2 queue 예외: " + ex.GetType().Name + ": " + ex.Message + " fromIdle=" + fromIdle);
        ted.WriteMessage("\nPFSVBISOOPEN2 queue error: " + ex.GetType().Name + ": " + ex.Message);
        this.CloseIsoTempDocument(solidDoc, originalDoc);
      }
    }

    [CommandMethod("PFSVBISODONE", CommandFlags.Session)]
    public void VbIsoDoneCommand()
    {
      Document solidDoc = s_isoDoneSolidDoc;
      Document originalDoc = s_isoDoneOriginalDoc;
      int baseline = s_isoDoneBaseline;
      s_isoDoneSolidDoc = null;
      s_isoDoneOriginalDoc = null;
      s_isoDoneBaseline = 0;
      bool exportQueued = false;

      if (solidDoc == null)
      {
        PlantOrthoView.FileDiag("PFSVBISODONE 중단: solidDoc null");
        return;
      }

      Editor ed = solidDoc.Editor;
      try
      {
        Application.DocumentManager.MdiActiveDocument = solidDoc;
        int added = 0;
        using (Transaction tr = solidDoc.Database.TransactionManager.StartTransaction())
        {
          ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(solidDoc.Database, tr, "Layout1");
          if (!layoutBtrId.IsNull)
          {
            BlockTableRecord btr = tr.GetObject(layoutBtrId, OpenMode.ForRead) as BlockTableRecord;
            if (btr != null)
              foreach (ObjectId id in btr)
                added++;
          }
          tr.Commit();
        }

        int newEntities = added - baseline;
        PlantOrthoView.FileDiag("PFSVBISODONE newEntities=" + newEntities + " baseline=" + baseline + " added=" + added + " (VIEWBASE 완료판정)");
        ed.WriteMessage("\nPFSVBISODONE newEntities=" + newEntities);

        string directory = @"C:\Temp";
        if (!System.IO.Directory.Exists(directory))
          System.IO.Directory.CreateDirectory(directory);
        string exportPath = System.IO.Path.Combine(directory, "pfs_iso_export_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture) + ".dwg");
        if (System.IO.File.Exists(exportPath))
          System.IO.File.Delete(exportPath);

        string ctab = "unknown";
        try
        {
          object ctabValue = Application.GetSystemVariable("CTAB");
          if (ctabValue != null)
            ctab = ctabValue.ToString();
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSVBISODONE CTAB 조회 예외: " + ex.GetType().Name + ": " + ex.Message);
        }

        s_isoExportPendingSolidDoc = solidDoc;
        s_isoExportPendingOriginalDoc = originalDoc;
        s_isoExportPath = exportPath;
        string exportCmd = "_.CTAB\nLayout1\n"
          + "_.FILEDIA\n0\n_.CMDDIA\n0\n"
          + "_.EXPORTLAYOUT\n" + exportPath + "\n"
          + "_.FILEDIA\n1\n_.CMDDIA\n1\n"
          + "PFSVBISOEXPORTED\n";
        solidDoc.SendStringToExecute(exportCmd, true, false, false);
        exportQueued = true;
        PlantOrthoView.FileDiag("PFSVBISODONE newEntities=" + newEntities + " -> EXPORTLAYOUT queued path=" + exportPath + " ctab=" + ctab + " cmd=" + exportCmd.Replace("\n", "|"));
        ed.WriteMessage("\nPFSVBISODONE EXPORTLAYOUT queued -> " + exportPath);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISODONE export queue 예외: " + ex.GetType().Name + ": " + ex.Message);
        ed.WriteMessage("\nPFSVBISODONE export queue error: " + ex.GetType().Name + ": " + ex.Message);
      }
      finally
      {
        if (!exportQueued)
        {
          s_isoClosePendingDoc = solidDoc;
          s_isoClosePendingOriginalDoc = originalDoc;
          Application.Idle -= this.VbIsoDoneCloseOnIdle;
          Application.Idle += this.VbIsoDoneCloseOnIdle;
          PlantOrthoView.FileDiag("PFSVBISODONE close Idle 위임(export queue 실패)");
        }
      }
    }

    [CommandMethod("PFSVBISOEXPORTED", CommandFlags.Session)]
    public void VbIsoExportedCommand()
    {
      Document solidDoc = s_isoExportPendingSolidDoc;
      Document originalDoc = s_isoExportPendingOriginalDoc;
      string exportPath = s_isoExportPath;
      s_isoExportPendingSolidDoc = null;
      s_isoExportPendingOriginalDoc = null;
      s_isoExportPath = null;

      if (solidDoc == null)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED 중단: solidDoc null path=" + exportPath);
        return;
      }

      Editor ed = solidDoc.Editor;
      try
      {
        if (string.IsNullOrEmpty(exportPath) || !System.IO.File.Exists(exportPath))
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED export 파일 없음 path=" + exportPath);
          this.WriteEditorMessageSafe(ed, "\nPFSVBISOEXPORTED export 파일 없음 path=" + exportPath, "PFSVBISOEXPORTED missing export");
          return;
        }

        int total = 0;
        int line = 0;
        int circle = 0;
        int arc = 0;
        int poly = 0;
        int block = 0;
        int viewBorder = 0;
        ObjectIdCollection ids = new ObjectIdCollection();
        Database sideDb = null;
        try
        {
          sideDb = new Database(false, true);
          sideDb.ReadDwgFile(exportPath, System.IO.FileShare.ReadWrite, true, null);
          using (Transaction tr = sideDb.TransactionManager.StartTransaction())
          {
            BlockTable bt = tr.GetObject(sideDb.BlockTableId, OpenMode.ForRead) as BlockTable;
            if (bt != null && bt.Has(BlockTableRecord.ModelSpace))
            {
              BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
              if (ms != null)
              {
                foreach (ObjectId id in ms)
                {
                  Entity entity = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
                  if (entity == null)
                    continue;

                  ids.Add(id);
                  total++;
                  string typeName = entity.GetType().Name;
                  if (typeName.IndexOf("ViewBorder", System.StringComparison.OrdinalIgnoreCase) >= 0 || typeName.IndexOf("AcDbViewBorder", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    viewBorder++;
                  if (entity is Line)
                    line++;
                  else if (entity is Circle)
                    circle++;
                  else if (entity is Arc)
                    arc++;
                  else if (entity is Polyline || entity is Polyline2d || entity is Polyline3d)
                    poly++;
                  else if (entity is BlockReference)
                    block++;
                }
              }
            }
            tr.Commit();
          }

          PlantOrthoView.FileDiag("PFSVBISOEXPORTED exported2D total=" + total + " Line=" + line + " Circle=" + circle + " Arc=" + arc + " Poly=" + poly + " Block=" + block + " viewBorder=" + viewBorder + " path=" + exportPath);
          this.WriteEditorMessageSafe(ed, "\nPFSVBISOEXPORTED exported2D total=" + total + " Line=" + line + " Circle=" + circle + " Arc=" + arc + " Poly=" + poly + " Block=" + block + " viewBorder=" + viewBorder, "PFSVBISOEXPORTED exported2D");

          bool separateSaved = false;
          if (ids.Count > 0 && s_isoRealSizeValid && originalDoc != null)
          {
            string savedPath;
            bool separateOk = this.CreateIsoDetailDrawing(ids, sideDb, out savedPath);
            if (separateOk)
            {
              PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg 성공 path=" + savedPath);
              separateSaved = true;
            }
            else
            {
              PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg 실패 -> 원본 clone-back 폴백");
            }
          }

          if (separateSaved)
          {
            PlantOrthoView.FileDiag("PFSVBISOEXPORTED clone-back skip: separateDwg success");
          }
          else if (ids.Count == 0)
          {
            PlantOrthoView.FileDiag("PFSVBISOEXPORTED clone-back skip: export ModelSpace empty path=" + exportPath);
          }
          else if (originalDoc == null)
          {
            PlantOrthoView.FileDiag("PFSVBISOEXPORTED clone-back skip: originalDoc null path=" + exportPath);
          }
          else
          {
            this.CloneIsoExportToOriginal(ids, sideDb, originalDoc, exportPath);
          }
        }
        finally
        {
          if (sideDb != null)
            sideDb.Dispose();
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED 예외: " + ex.GetType().Name + ": " + ex.Message + " path=" + exportPath);
        this.WriteEditorMessageSafe(ed, "\nPFSVBISOEXPORTED error: " + ex.GetType().Name + ": " + ex.Message, "PFSVBISOEXPORTED error");
      }
      finally
      {
        s_isoClosePendingDoc = solidDoc;
        s_isoClosePendingOriginalDoc = originalDoc;
        Application.Idle -= this.VbIsoDoneCloseOnIdle;
        Application.Idle += this.VbIsoDoneCloseOnIdle;
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED close Idle 위임 path=" + exportPath);
      }
    }

    private void VbIsoDoneCloseOnIdle(object sender, System.EventArgs e)
    {
      Application.Idle -= this.VbIsoDoneCloseOnIdle;
      Document solidDoc = s_isoClosePendingDoc;
      Document originalDoc = s_isoClosePendingOriginalDoc;
      s_isoClosePendingDoc = null;
      s_isoClosePendingOriginalDoc = null;
      if (solidDoc == null)
      {
        PlantOrthoView.FileDiag("PFSVBISODONE close 중단: solidDoc null");
        return;
      }

      try
      {
        this.CloseIsoTempDocument(solidDoc, originalDoc);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISODONE close Idle 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private bool CloneIsoExportToOriginal(ObjectIdCollection ids, Database sideDb, Document originalDoc, string exportPath)
    {
      if (ids == null || ids.Count == 0)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED clone-back skip: export ModelSpace empty path=" + exportPath);
        return false;
      }
      if (sideDb == null)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED clone-back skip: sideDb null path=" + exportPath);
        return false;
      }
      if (originalDoc == null)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED clone-back skip: originalDoc null path=" + exportPath);
        return false;
      }

      int cloned = 0;
      Database originalDb = originalDoc.Database;
      using (DocumentLock dlk = originalDoc.LockDocument())
      using (Transaction ttr = originalDb.TransactionManager.StartTransaction())
      {
        BlockTable tbt = ttr.GetObject(originalDb.BlockTableId, OpenMode.ForRead) as BlockTable;
        if (tbt == null || !tbt.Has(BlockTableRecord.ModelSpace))
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED clone-back skip: original ModelSpace 없음 path=" + exportPath);
          ttr.Commit();
          return false;
        }

        ObjectId msId = tbt[BlockTableRecord.ModelSpace];
        BlockTableRecord targetMs = ttr.GetObject(msId, OpenMode.ForWrite) as BlockTableRecord;
        ObjectId detailLayerId = this.EnsureIsoDetailLayer(originalDb, ttr);
        ObjectId annotationLayerId;
        ObjectId textStyleId;
        ObjectId dimStyleId;
        this.EnsureIsoAnnotationResources(originalDb, ttr, out annotationLayerId, out textStyleId, out dimStyleId);
        this.PurgePriorIsoDetail(ttr, targetMs, detailLayerId, annotationLayerId);
        Database oldWorking = HostApplicationServices.WorkingDatabase;
        IdMapping idMap = new IdMapping();
        try
        {
          HostApplicationServices.WorkingDatabase = originalDb;
          sideDb.WblockCloneObjects(ids, msId, idMap, DuplicateRecordCloning.Ignore, false);
        }
        finally
        {
          HostApplicationServices.WorkingDatabase = oldWorking;
        }

        ObjectId detailBlockRefId = ObjectId.Null;
        ObjectId detailEntityId = ObjectId.Null;
        foreach (IdPair pair in idMap)
        {
          if (!pair.IsPrimary || !pair.IsCloned)
            continue;

          Entity e = ttr.GetObject(pair.Value, OpenMode.ForWrite, false) as Entity;
          if (e == null)
            continue;

          e.LayerId = detailLayerId;
          if (detailEntityId == ObjectId.Null)
            detailEntityId = pair.Value;
          if (detailBlockRefId == ObjectId.Null && e is BlockReference)
            detailBlockRefId = pair.Value;
          cloned++;
        }

        this.LogIsoBlockInnerTypes(originalDb, ttr, detailBlockRefId);
        if (targetMs != null && s_isoRealSizeValid)
        {
          ObjectId dimSourceId = detailBlockRefId != ObjectId.Null ? detailBlockRefId : detailEntityId;
          Database prevWdb = HostApplicationServices.WorkingDatabase;
          try
          {
            HostApplicationServices.WorkingDatabase = originalDb;
            this.AppendIsoBoundingDimensions(ttr, targetMs, dimSourceId, annotationLayerId, dimStyleId);
          }
          finally
          {
            HostApplicationServices.WorkingDatabase = prevWdb;
          }
        }
        else
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED dim skip realSizeValid=" + s_isoRealSizeValid + " targetMsNull=" + (targetMs == null));
        }

        ttr.Commit();
      }

      PlantOrthoView.FileDiag("PFSVBISOEXPORTED cloned2D=" + cloned + " -> 원본(PFS_ISO_DETAIL) path=" + exportPath);
      return cloned > 0;
    }

    private string CreateNotabDetailDrawing(ref Database sourceDb, System.Collections.Generic.List<ObjectId> solidIds)
    {
      if (sourceDb == null)
        throw new System.ArgumentNullException("sourceDb");
      if (solidIds == null || solidIds.Count == 0)
        throw new System.InvalidOperationException("solidIds 없음");

      string templatePath = this.ResolveIsoTemplatePath();
      if (string.IsNullOrWhiteSpace(templatePath) || !System.IO.File.Exists(templatePath))
        throw new System.IO.FileNotFoundException("template 없음", templatePath ?? string.Empty);

      string tag = s_isoSupportTag;
      if (string.IsNullOrWhiteSpace(tag) && !string.IsNullOrWhiteSpace(s_isoShortDesc))
        tag = s_isoShortDesc + "_" + this.GetNotabPrimarySupportHandle(sourceDb, solidIds);
      if (string.IsNullOrWhiteSpace(tag))
        tag = "SUPPORT_" + this.GetNotabPrimarySupportHandle(sourceDb, solidIds);
      string safeTag = this.SanitizeIsoFileName(tag);

      Database detailDb = null;
      Database oldWorking = HostApplicationServices.WorkingDatabase;
      string savedPath = null;
      try
      {
        detailDb = new Database(true, true);
        HostApplicationServices.WorkingDatabase = detailDb;
        this.TrySetTagDatabaseFacetres(detailDb, 10.0);

        Extents3d solidExt;
        Extents3d supportExt;
        string supportExtSource;
        int cloned;
        ObjectId viewportId = ObjectId.Null;
        bool skipViewport = string.Equals(System.Environment.GetEnvironmentVariable("PFS_NOTAB_SKIP_VIEWPORT"), "1", System.StringComparison.OrdinalIgnoreCase);
        bool viewportOk = false;
        int titleblockCloned = 0;
        ObjectId layoutBtrId = ObjectId.Null;
        using (Transaction tr = detailDb.TransactionManager.StartTransaction())
        {
          ObjectId msId = this.GetModelSpaceId(detailDb, tr);
          if (msId == ObjectId.Null)
            throw new System.InvalidOperationException("detail ModelSpace 없음");

          ObjectId detailLayerId = this.EnsureIsoDetailLayer(detailDb, tr);
          cloned = this.CopyCleanNotabSolids(sourceDb, solidIds, detailDb, tr, msId, detailLayerId, out solidExt, out supportExt, out supportExtSource);
          if (cloned == 0)
            throw new System.InvalidOperationException("cloned solid/extents 없음");

          sourceDb.Dispose();
          sourceDb = null;
          PlantOrthoView.FileDiag("PFSNOTABDETAIL sourceDb disposed(before-save) ok");

          layoutBtrId = this.EnsureNotabDetailLayout(detailDb, tr);
          if (layoutBtrId == ObjectId.Null)
            throw new System.InvalidOperationException("detail layout 없음");
          this.TryConfigureNotabA1Layout(tr, layoutBtrId);

          tr.Commit();
          PlantOrthoView.FileDiag("PFSNOTABDETAIL preTitle commit 완료");
        }

        string detailsDir = this.GetIsoDetailsDirectory();
        if (string.IsNullOrWhiteSpace(detailsDir))
          throw new System.InvalidOperationException("Details dir 없음");

        System.IO.Directory.CreateDirectory(detailsDir);
        savedPath = System.IO.Path.Combine(detailsDir, safeTag + "_notab.dwg");
        if (System.IO.File.Exists(savedPath))
          System.IO.File.Delete(savedPath);

        using (Transaction tr = detailDb.TransactionManager.StartTransaction())
        {
          layoutBtrId = this.GetNotabDetailLayoutBlockTableRecordId(detailDb, tr);
          if (layoutBtrId == ObjectId.Null)
            throw new System.InvalidOperationException("detail layout 없음");

          titleblockCloned = this.CloneTemplateTitleBlock2D(templatePath, detailDb, tr, layoutBtrId);

          if (skipViewport)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport skip(env)");
          }
          else
          {
            viewportOk = this.CreateNotabDetailViewport(tr, detailDb, out viewportId);
          }
          bool titleblockOk = this.UpdateIsoTitleBlockAttributesInLayout(tr, layoutBtrId, tag);
          PlantOrthoView.FileDiag("PFSNOTABDETAIL plainDb cloned=" + cloned + " viewport=" + (viewportOk ? "ok" : "skip") + " titleblockClone=" + titleblockCloned + " titleblockUpdate=" + (titleblockOk ? "ok" : "skip") + " ext=" + this.FormatExtents(solidExt));
          PlantOrthoView.FileDiag("PFSNOTABDETAIL commit 직전");
          tr.Commit();
          PlantOrthoView.FileDiag("PFSNOTABDETAIL commit 완료");
        }

        if (!skipViewport && viewportId != ObjectId.Null)
          this.ConfigureNotabDetailViewport(detailDb, viewportId, solidExt, supportExt, "clipped-solid");

        if (!skipViewport && viewportId != ObjectId.Null)
          this.AppendNotabPaperDimensions(detailDb, viewportId, supportExt);

        PlantOrthoView.FileDiag("PFSNOTABDETAIL saveAs 직전 path=" + savedPath);
        detailDb.SaveAs(savedPath, DwgVersion.Current);
        PlantOrthoView.FileDiag("PFSNOTABDETAIL saved path=" + savedPath + " tag=" + tag);
        return savedPath;
      }
      finally
      {
        HostApplicationServices.WorkingDatabase = oldWorking;
        if (detailDb != null)
          detailDb.Dispose();
      }
    }

    private string GetNotabPrimarySupportHandle(Database db, System.Collections.Generic.List<ObjectId> ids)
    {
      string fallback = System.DateTime.Now.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
      if (db == null || ids == null || ids.Count == 0)
        return fallback;

      try
      {
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          for (int i = 0; i < ids.Count; i++)
          {
            ObjectId id = ids[i];
            if (id == ObjectId.Null)
              continue;

            try
            {
              string cls = id.ObjectClass == null ? string.Empty : id.ObjectClass.Name;
              DBObject obj = tr.GetObject(id, OpenMode.ForRead, false);
              string typeName = obj == null ? string.Empty : obj.GetType().Name;
              if (cls.IndexOf("Support", System.StringComparison.OrdinalIgnoreCase) >= 0 || typeName.IndexOf("Support", System.StringComparison.OrdinalIgnoreCase) >= 0)
              {
                tr.Commit();
                return id.Handle.ToString();
              }
            }
            catch (System.Exception ex)
            {
              PlantOrthoView.FileDiag("PFSNOTABDETAIL support handle scan 예외 id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
            }
          }
          tr.Commit();
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL support handle fallback 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      return fallback;
    }

    private ObjectId EnsureNotabDetailLayout(Database db, Transaction tr)
    {
      if (db == null || tr == null)
        return ObjectId.Null;

      ObjectId titleId = this.GetLayoutBlockTableRecordId(db, tr, "Title Block");
      if (titleId != ObjectId.Null)
        return titleId;

      ObjectId layout1Id = this.GetLayoutBlockTableRecordId(db, tr, "Layout1");
      if (layout1Id == ObjectId.Null)
        return ObjectId.Null;

      try
      {
        DBDictionary layouts = tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
        if (layouts != null && layouts.Contains("Layout1") && !layouts.Contains("Title Block"))
        {
          Layout layout = tr.GetObject(layouts.GetAt("Layout1"), OpenMode.ForWrite, false) as Layout;
          if (layout != null)
          {
            layout.LayoutName = "Title Block";
            PlantOrthoView.FileDiag("PFSNOTABDETAIL layout renamed Layout1 -> Title Block");
          }
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL layout rename skip: " + ex.GetType().Name + ": " + ex.Message);
      }

      titleId = this.GetLayoutBlockTableRecordId(db, tr, "Title Block");
      return titleId != ObjectId.Null ? titleId : layout1Id;
    }

    private ObjectId GetNotabDetailLayoutBlockTableRecordId(Database db, Transaction tr)
    {
      ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(db, tr, "Title Block");
      if (layoutBtrId != ObjectId.Null)
        return layoutBtrId;
      return this.GetLayoutBlockTableRecordId(db, tr, "Layout1");
    }

    private bool TryConfigureNotabA1Layout(Transaction tr, ObjectId layoutBtrId)
    {
      if (tr == null || layoutBtrId == ObjectId.Null)
        return false;

      try
      {
        BlockTableRecord layoutBtr = tr.GetObject(layoutBtrId, OpenMode.ForRead, false) as BlockTableRecord;
        if (layoutBtr == null || layoutBtr.LayoutId == ObjectId.Null)
          return false;

        Layout layout = tr.GetObject(layoutBtr.LayoutId, OpenMode.ForWrite, false) as Layout;
        if (layout == null)
          return false;

        string mediaName = null;
        try
        {
          PlotSettings ps = new PlotSettings(layout.ModelType);
          ps.CopyFrom(layout);
          PlotSettingsValidator psv = PlotSettingsValidator.Current;
          try
          {
            psv.SetPlotConfigurationName(ps, "None", null);
          }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL A1 plot device None skip: " + ex.GetType().Name + ": " + ex.Message);
          }

          System.Collections.Specialized.StringCollection mediaNames = psv.GetCanonicalMediaNameList(ps);
          if (mediaNames != null)
          {
            foreach (string candidate in mediaNames)
            {
              if (candidate != null && candidate.IndexOf("A1", System.StringComparison.OrdinalIgnoreCase) >= 0)
              {
                mediaName = candidate;
                break;
              }
            }
          }

          if (!string.IsNullOrWhiteSpace(mediaName))
          {
            psv.SetCanonicalMediaName(ps, mediaName);
            layout.CopyFrom(ps);
          }
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL A1 PlotSettings skip: " + ex.GetType().Name + ": " + ex.Message);
        }

        try
        {
          System.Reflection.PropertyInfo limitsProperty = layout.GetType().GetProperty("Limits");
          if (limitsProperty != null && limitsProperty.CanWrite)
            limitsProperty.SetValue(layout, new Extents2d(new Point2d(0.0, 0.0), new Point2d(841.0, 594.0)), null);
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL A1 Limits reflection skip: " + ex.GetType().Name + ": " + ex.Message);
        }

        PlantOrthoView.FileDiag("PFSNOTABDETAIL A1 media=" + (mediaName ?? "none") + " plotArea=(0,0)~(841,594)");
        return true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL A1 layout config skip: " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
    }

    private int CloneTemplateTitleBlock2D(string templatePath, Database targetDb, Transaction targetTr, ObjectId targetLayoutBtrId)
    {
      if (string.IsNullOrWhiteSpace(templatePath) || targetDb == null || targetTr == null || targetLayoutBtrId == ObjectId.Null)
        return 0;

      Database templateDb = null;
      Database oldWorking = HostApplicationServices.WorkingDatabase;
      try
      {
        templateDb = new Database(false, true);
        templateDb.ReadDwgFile(templatePath, System.IO.FileShare.Read, true, null);
        templateDb.CloseInput(true);

        ObjectIdCollection sourceIds = new ObjectIdCollection();
        using (Transaction sourceTr = templateDb.TransactionManager.StartTransaction())
        {
          ObjectId sourceLayoutBtrId = this.GetLayoutBlockTableRecordId(templateDb, sourceTr, "Title Block");
          if (sourceLayoutBtrId == ObjectId.Null)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL titleblock clone skip: template Title Block layout 없음");
            sourceTr.Commit();
            return 0;
          }

          BlockTableRecord sourceLayoutBtr = sourceTr.GetObject(sourceLayoutBtrId, OpenMode.ForRead, false) as BlockTableRecord;
          if (sourceLayoutBtr == null)
          {
            sourceTr.Commit();
            return 0;
          }

          foreach (ObjectId id in sourceLayoutBtr)
          {
            Entity entity = sourceTr.GetObject(id, OpenMode.ForRead, false) as Entity;
            if (entity == null || entity is Viewport)
              continue;
            sourceIds.Add(id);
          }
          sourceTr.Commit();
        }

        if (sourceIds.Count == 0)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL titleblock clone skip: source 2D empty");
          return 0;
        }

        IdMapping idMap = new IdMapping();
        try
        {
          HostApplicationServices.WorkingDatabase = targetDb;
          templateDb.WblockCloneObjects(sourceIds, targetLayoutBtrId, idMap, DuplicateRecordCloning.Ignore, false);
        }
        finally
        {
          HostApplicationServices.WorkingDatabase = oldWorking;
        }

        int cloned = 0;
        foreach (IdPair pair in idMap)
        {
          if (pair.IsPrimary && pair.IsCloned)
            cloned++;
        }

        int normalized = this.NormalizeNotabClonedTitleBlockEntities(targetDb, targetTr, idMap);
        PlantOrthoView.FileDiag("PFSNOTABDETAIL titleblock clone cloned=" + cloned + " source=" + sourceIds.Count);
        PlantOrthoView.FileDiag("PFSNOTABDETAIL titleblock clone normalized=" + normalized);
        return cloned;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL titleblock clone 예외: " + ex.GetType().Name + ": " + ex.Message);
        return 0;
      }
      finally
      {
        HostApplicationServices.WorkingDatabase = oldWorking;
        if (templateDb != null)
          templateDb.Dispose();
      }
    }

    private int NormalizeNotabClonedTitleBlockEntities(Database db, Transaction tr, IdMapping idMap)
    {
      if (db == null || tr == null || idMap == null)
        return 0;

      int normalized = 0;
      try
      {
        ObjectId layer0Id = ObjectId.Null;
        ObjectId continuousId = ObjectId.Null;
        LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead, false) as LayerTable;
        if (layerTable != null && layerTable.Has("0"))
          layer0Id = layerTable["0"];
        LinetypeTable lineTypeTable = tr.GetObject(db.LinetypeTableId, OpenMode.ForRead, false) as LinetypeTable;
        if (lineTypeTable != null && lineTypeTable.Has("Continuous"))
          continuousId = lineTypeTable["Continuous"];

        foreach (IdPair pair in idMap)
        {
          if (!pair.IsPrimary || !pair.IsCloned)
            continue;

          Entity entity = tr.GetObject(pair.Value, OpenMode.ForWrite, false) as Entity;
          if (entity == null)
            continue;

          bool changed = false;
          if ((entity.LayerId == ObjectId.Null || entity.LayerId.IsErased) && layer0Id != ObjectId.Null)
          {
            entity.LayerId = layer0Id;
            changed = true;
          }
          if ((entity.LinetypeId == ObjectId.Null || entity.LinetypeId.IsErased) && continuousId != ObjectId.Null)
          {
            entity.LinetypeId = continuousId;
            changed = true;
          }

          if (changed)
            normalized++;
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL titleblock normalize 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      return normalized;
    }

    private void TryReopenSetNotabPaperSpace(string savedPath)
    {
      if (string.IsNullOrWhiteSpace(savedPath))
        return;

      Database reopenDb = null;
      try
      {
        reopenDb = new Database(false, true);
        reopenDb.ReadDwgFile(savedPath, System.IO.FileShare.ReadWrite, true, null);
        reopenDb.CloseInput(true);
        reopenDb.TileMode = false;
        this.TrySetNotabPaperZoomExtents(reopenDb);
        reopenDb.SaveAs(savedPath, true, DwgVersion.Current, reopenDb.SecurityParameters);
        PlantOrthoView.FileDiag("PFSNOTABDETAIL paper-reopen tilemode=0 ok");
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL paper-reopen 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
      finally
      {
        if (reopenDb != null)
          reopenDb.Dispose();
      }
    }

    // reopen DB(Plant 미부착)에서 Title Block 레이아웃의 오버올 뷰포트(Number 1) 뷰를 페이퍼 extents에 맞춘다.
    // 열 때 초기 화면이 도면 전체를 보이도록(zoom extents 상당). 실패해도 페이퍼 열림은 유지.
    private void TrySetNotabPaperZoomExtents(Database db)
    {
      try
      {
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          DBDictionary lays = tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
          if (lays == null || !lays.Contains("Title Block"))
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL paper-zoom skip: Title Block layout 없음");
            tr.Commit();
            return;
          }
          Layout lay = tr.GetObject(lays.GetAt("Title Block"), OpenMode.ForRead) as Layout;
          BlockTableRecord ps = lay == null ? null : tr.GetObject(lay.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
          if (ps == null) { tr.Commit(); return; }

          Extents3d ext = new Extents3d();
          bool has = false;
          Viewport overall = null;
          System.Text.StringBuilder vpNums = new System.Text.StringBuilder();
          System.Collections.Generic.List<Viewport> vps = new System.Collections.Generic.List<Viewport>();
          foreach (ObjectId eid in ps)
          {
            Entity e = tr.GetObject(eid, OpenMode.ForRead, false) as Entity;
            if (e == null) continue;
            Viewport v = e as Viewport;
            if (v != null) { vpNums.Append(v.Number).Append(","); vps.Add(v); continue; }
            try { Extents3d ex = e.GeometricExtents; if (!has) { ext = ex; has = true; } else ext.AddExtents(ex); }
            catch { }
          }
          // 오버올 뷰포트는 활성화 전 Number=-1이라 Number로 못 찾는다. dims로 판별:
          // 우리 디테일 뷰포트(610x489)가 아닌 것 = 오버올. 디테일 border는 extents에 포함.
          foreach (Viewport v in vps)
          {
            bool isDetail = System.Math.Abs(v.Width - 610.0) < 1.0 && System.Math.Abs(v.Height - 489.0) < 1.0;
            if (isDetail)
            {
              try { Extents3d ex = v.GeometricExtents; if (!has) { ext = ex; has = true; } else ext.AddExtents(ex); } catch { }
            }
            else overall = v;
          }
          PlantOrthoView.FileDiag("PFSNOTABDETAIL paper-zoom diag vpNumbers=[" + vpNums.ToString() + "] overallFound=" + (overall != null));

          if (has && overall != null)
          {
            overall.UpgradeOpen();
            double w = ext.MaxPoint.X - ext.MinPoint.X;
            double h = ext.MaxPoint.Y - ext.MinPoint.Y;
            double cx = (ext.MinPoint.X + ext.MaxPoint.X) / 2.0;
            double cy = (ext.MinPoint.Y + ext.MaxPoint.Y) / 2.0;
            overall.ViewCenter = new Point2d(cx, cy);
            double vpAspect = overall.Height > 1e-9 ? overall.Width / overall.Height : 1.0;
            double neededH = h * 1.05;
            double neededByW = vpAspect > 1e-9 ? (w * 1.05) / vpAspect : h * 1.05;
            overall.ViewHeight = System.Math.Max(neededH, neededByW);
            PlantOrthoView.FileDiag("PFSNOTABDETAIL paper-zoom ok center=(" + this.FormatNumber(cx) + "," + this.FormatNumber(cy) + ") viewH=" + this.FormatNumber(overall.ViewHeight));
          }
          else
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL paper-zoom skip: overall=" + (overall != null) + " hasExt=" + has);
          }
          tr.Commit();
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL paper-zoom 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private bool UpdateIsoTitleBlockAttributesInLayout(Transaction tr, ObjectId layoutBtrId, string supportTag)
    {
      if (tr == null || layoutBtrId == ObjectId.Null)
        return false;

      try
      {
        BlockTableRecord layoutBtr = tr.GetObject(layoutBtrId, OpenMode.ForRead) as BlockTableRecord;
        if (layoutBtr == null)
          return false;

        foreach (ObjectId id in layoutBtr)
        {
          BlockReference br = tr.GetObject(id, OpenMode.ForRead, false) as BlockReference;
          if (br == null)
            continue;

          string blockName = this.GetBlockReferenceName(tr, br);
          if (!string.Equals(blockName, "DRAWING_TITLE", System.StringComparison.OrdinalIgnoreCase))
            continue;

          bool changed = false;
          foreach (ObjectId attId in br.AttributeCollection)
          {
            AttributeReference att = tr.GetObject(attId, OpenMode.ForWrite, false) as AttributeReference;
            if (att == null)
              continue;

            string value = this.GetIsoTitleBlockValue(att.Tag, supportTag);
            if (string.IsNullOrWhiteSpace(value))
              continue;

            att.TextString = value;
            changed = true;
          }

          return changed;
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL titleblock update 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      return false;
    }

    private ObjectId GetModelSpaceId(Database db, Transaction tr)
    {
      if (db == null || tr == null)
        return ObjectId.Null;
      BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
      if (bt == null || !bt.Has(BlockTableRecord.ModelSpace))
        return ObjectId.Null;
      return bt[BlockTableRecord.ModelSpace];
    }

    private int CopyCleanNotabSolids(Database sourceDb, System.Collections.Generic.List<ObjectId> solidIds, Database targetDb, Transaction targetTr, ObjectId targetMsId, ObjectId layerId, out Extents3d solidExt, out Extents3d supportExt, out string supportExtSource)
    {
      solidExt = new Extents3d();
      supportExt = new Extents3d();
      supportExtSource = "fallback";
      bool hasExt = false;
      int copied = 0;
      if (sourceDb == null || solidIds == null || targetDb == null || targetTr == null || targetMsId == ObjectId.Null)
        return 0;

      BlockTableRecord targetMs = targetTr.GetObject(targetMsId, OpenMode.ForWrite, false) as BlockTableRecord;
      if (targetMs == null)
        return 0;

      using (Transaction sourceTr = sourceDb.TransactionManager.StartTransaction())
      {
        bool hasSupportExt = this.TryGetNotabSupportSolidExtents(sourceDb, sourceTr, out supportExt);
        if (hasSupportExt)
          supportExtSource = "support";
        NotabClipBox clipBox = null;
        if (hasSupportExt)
          clipBox = this.CreateNotabClipBox(supportExt);
        else
          PlantOrthoView.FileDiag("PFSNOTABDETAIL clip skip: supportExt 없음");

        int clipCandidates = 0;
        int clipKept = 0;
        int clipTrimmed = 0;
        int clipDropped = 0;
        int clipFallback = 0;

        foreach (ObjectId solidId in solidIds)
        {
          try
          {
            Solid3d src = sourceTr.GetObject(solidId, OpenMode.ForRead, false) as Solid3d;
            if (src == null)
            {
              PlantOrthoView.FileDiag("PFSNOTABDETAIL cleanSolid skip nonSolid id=" + solidId);
              continue;
            }

            Solid3d clean = new Solid3d();
            clean.SetDatabaseDefaults(targetDb);
            clean.CopyFrom(src);
            targetMs.AppendEntity(clean);
            targetTr.AddNewlyCreatedDBObject(clean, true);
            if (layerId != ObjectId.Null)
              clean.LayerId = layerId;
            this.TryStripCleanSolidMetadata(clean);

            bool keepSolid = true;
            bool clipped = false;
            if (clipBox != null)
            {
              clipCandidates++;
              keepSolid = this.TryClipNotabSolidToBox(clean, clipBox, out clipped);
              if (!keepSolid)
              {
                clipDropped++;
                try { clean.Erase(); }
                catch (System.Exception exErase) { PlantOrthoView.FileDiag("PFSNOTABDETAIL clip erase 예외 id=" + clean.ObjectId + ": " + exErase.GetType().Name + ": " + exErase.Message); }
                continue;
              }
              if (clipped)
                clipTrimmed++;
              else
                clipFallback++;
            }

            Extents3d ext = clean.GeometricExtents;
            if (!hasExt)
            {
              solidExt = ext;
              hasExt = true;
            }
            else
            {
              solidExt.AddExtents(ext);
            }
            copied++;
            clipKept++;
          }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL cleanSolid skip id=" + solidId + ": " + ex.GetType().Name + ": " + ex.Message);
          }
        }
        if (clipBox != null)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL clip solids=" + clipCandidates + " kept=" + clipKept + " trimmed=" + clipTrimmed + " dropped=" + clipDropped + " fallback=" + clipFallback + " margin=" + this.FormatNumber(clipBox.Margin) + " ext=" + (hasExt ? this.FormatExtents(solidExt) : "none"));
          clipBox.Dispose();
        }
        sourceTr.Commit();
      }

      if (hasExt && supportExtSource == "fallback")
        supportExt = solidExt;

      PlantOrthoView.FileDiag("PFSNOTABDETAIL cleanSolid copied=" + copied + " ext=" + (hasExt ? this.FormatExtents(solidExt) : "none") + " supportExt=" + (hasExt ? this.FormatExtents(supportExt) : "none") + " src=" + supportExtSource);
      return hasExt ? copied : 0;
    }

    private bool TryGetNotabSupportSolidExtents(Database sourceDb, Transaction tr, out Extents3d supportExt)
    {
      supportExt = new Extents3d();
      if (sourceDb == null || tr == null)
        return false;

      try
      {
        BlockTable bt = tr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead, false) as BlockTable;
        if (bt == null || !bt.Has(BlockTableRecord.ModelSpace))
          return false;

        BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead, false) as BlockTableRecord;
        if (ms == null)
          return false;

        bool hasSupportExt = false;
        int supportCount = 0;
        foreach (ObjectId id in ms)
        {
          try
          {
            DBObject obj = tr.GetObject(id, OpenMode.ForRead, false);
            Entity ent = obj as Entity;
            if (ent == null || ent.IsErased)
              continue;

            string className = id.ObjectClass == null ? string.Empty : id.ObjectClass.Name;
            string typeName = obj.GetType().Name;
            string dxfName = ent.GetRXClass() != null ? ent.GetRXClass().DxfName : string.Empty;
            bool isSupport = className.IndexOf("Support", System.StringComparison.OrdinalIgnoreCase) >= 0
              || typeName.IndexOf("Support", System.StringComparison.OrdinalIgnoreCase) >= 0
              || dxfName.IndexOf("Support", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isSupport)
              continue;

            supportCount++;
            this.AccumulateNotabSupportSolidExtents(ent, 0, ref hasSupportExt, ref supportExt);
          }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL supportExt scan skip id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
          }
        }

        PlantOrthoView.FileDiag("PFSNOTABDETAIL supportExt scan supports=" + supportCount + " ext=" + (hasSupportExt ? this.FormatExtents(supportExt) : "none"));
        return hasSupportExt;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL supportExt scan 예외: " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
    }

    private NotabClipBox CreateNotabClipBox(Extents3d supportExt)
    {
      double margin = this.GetEnvDouble("PFS_NOTAB_CLIP_MARGIN", 150.0, 0.0, 10000.0);
      Vector3d viewDir;
      Vector3d up;
      Vector3d right;
      this.GetNotabViewBasis(out viewDir, out up, out right);

      double minR = 0.0, maxR = 0.0, minU = 0.0, maxU = 0.0, minD = 0.0, maxD = 0.0;
      bool hasProjection = false;
      foreach (Point3d corner in this.GetExtentsCorners(supportExt))
      {
        Vector3d v = corner.GetAsVector();
        double r = v.DotProduct(right);
        double u = v.DotProduct(up);
        double d = v.DotProduct(viewDir);
        if (!hasProjection)
        {
          minR = maxR = r;
          minU = maxU = u;
          minD = maxD = d;
          hasProjection = true;
        }
        else
        {
          if (r < minR) minR = r;
          if (r > maxR) maxR = r;
          if (u < minU) minU = u;
          if (u > maxU) maxU = u;
          if (d < minD) minD = d;
          if (d > maxD) maxD = d;
        }
      }

      if (!hasProjection)
        return null;

      minR -= margin;
      maxR += margin;
      minU -= margin;
      maxU += margin;
      minD -= margin;
      maxD += margin;

      double width = System.Math.Max(maxR - minR, 1.0);
      double height = System.Math.Max(maxU - minU, 1.0);
      double depth = System.Math.Max(maxD - minD, 1.0);
      Point3d center = Point3d.Origin
        + right.MultiplyBy((minR + maxR) / 2.0)
        + up.MultiplyBy((minU + maxU) / 2.0)
        + viewDir.MultiplyBy((minD + maxD) / 2.0);

      Solid3d box = null;
      try
      {
        box = new Solid3d();
        box.CreateBox(width, height, depth);
        Matrix3d transform = Matrix3d.AlignCoordinateSystem(
          Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
          center, right, up, viewDir);
        box.TransformBy(transform);
        PlantOrthoView.FileDiag("PFSNOTABDETAIL clip box size=(" + this.FormatNumber(width) + "," + this.FormatNumber(height) + "," + this.FormatNumber(depth) + ") center=(" + this.FormatNumber(center.X) + "," + this.FormatNumber(center.Y) + "," + this.FormatNumber(center.Z) + ") margin=" + this.FormatNumber(margin));
        return new NotabClipBox(box, right, up, viewDir, minR, maxR, minU, maxU, minD, maxD, margin);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL clip box 예외: " + ex.GetType().Name + ": " + ex.Message);
        if (box != null)
          box.Dispose();
        return null;
      }
    }

    private bool TryClipNotabSolidToBox(Solid3d solid, NotabClipBox clipBox, out bool clipped)
    {
      clipped = false;
      if (solid == null || clipBox == null || clipBox.Box == null)
        return true;

      Extents3d beforeExt;
      try
      {
        beforeExt = solid.GeometricExtents;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL clip ext pre 예외 id=" + solid.ObjectId + ": " + ex.GetType().Name + ": " + ex.Message);
        return true;
      }

      if (!this.NotabProjectedExtentsIntersect(beforeExt, clipBox))
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL clip drop prefilter id=" + solid.ObjectId + " ext=" + this.FormatExtents(beforeExt));
        return false;
      }

      Solid3d clipCopy = null;
      try
      {
        clipCopy = clipBox.Box.Clone() as Solid3d;
        if (clipCopy == null)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL clip fallback: clip clone null id=" + solid.ObjectId);
          return true;
        }

        solid.BooleanOperation(BooleanOperationType.BoolIntersect, clipCopy);
        clipped = true;

        if (this.IsNotabSolidEmpty(solid))
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL clip drop empty id=" + solid.ObjectId);
          return false;
        }

        return true;
      }
      catch (Autodesk.AutoCAD.Runtime.Exception aex)
      {
        string status = aex.ErrorStatus.ToString();
        if (status.IndexOf("NoIntersection", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL clip drop boolean id=" + solid.ObjectId + ": " + status + " " + aex.Message);
          return false;
        }

        PlantOrthoView.FileDiag("PFSNOTABDETAIL clip fallback id=" + solid.ObjectId + ": " + status + " " + aex.Message);
        return true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL clip fallback id=" + solid.ObjectId + ": " + ex.GetType().Name + ": " + ex.Message);
        return true;
      }
      finally
      {
        if (clipCopy != null)
        {
          try { clipCopy.Dispose(); }
          catch (System.Exception ex) { PlantOrthoView.FileDiag("PFSNOTABDETAIL clip copy dispose 예외: " + ex.GetType().Name + ": " + ex.Message); }
        }
      }
    }

    private bool NotabProjectedExtentsIntersect(Extents3d ext, NotabClipBox clipBox)
    {
      double minR = 0.0, maxR = 0.0, minU = 0.0, maxU = 0.0, minD = 0.0, maxD = 0.0;
      bool hasProjection = false;
      foreach (Point3d corner in this.GetExtentsCorners(ext))
      {
        Vector3d v = corner.GetAsVector();
        double r = v.DotProduct(clipBox.Right);
        double u = v.DotProduct(clipBox.Up);
        double d = v.DotProduct(clipBox.ViewDir);
        if (!hasProjection)
        {
          minR = maxR = r;
          minU = maxU = u;
          minD = maxD = d;
          hasProjection = true;
        }
        else
        {
          if (r < minR) minR = r;
          if (r > maxR) maxR = r;
          if (u < minU) minU = u;
          if (u > maxU) maxU = u;
          if (d < minD) minD = d;
          if (d > maxD) maxD = d;
        }
      }

      if (!hasProjection)
        return false;
      if (maxR < clipBox.MinR || minR > clipBox.MaxR) return false;
      if (maxU < clipBox.MinU || minU > clipBox.MaxU) return false;
      if (maxD < clipBox.MinD || minD > clipBox.MaxD) return false;
      return true;
    }

    private bool IsNotabSolidEmpty(Solid3d solid)
    {
      if (solid == null)
        return true;

      try
      {
        double volume = solid.MassProperties.Volume;
        if (volume <= 1e-9)
          return true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL clip volume skip id=" + solid.ObjectId + ": " + ex.GetType().Name + ": " + ex.Message);
      }

      try
      {
        Extents3d ext = solid.GeometricExtents;
        return ext.MinPoint.DistanceTo(ext.MaxPoint) <= 1e-9;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL clip ext post 예외 id=" + solid.ObjectId + ": " + ex.GetType().Name + ": " + ex.Message);
        return true;
      }
    }

    private void AccumulateNotabSupportSolidExtents(Entity ent, int depth, ref bool hasExt, ref Extents3d supportExt)
    {
      if (ent == null)
        return;
      if (depth > 6)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL supportExt depthLimit type=" + ent.GetType().Name + " depth=" + depth);
        return;
      }

      Solid3d solid = ent as Solid3d;
      if (solid != null)
      {
        try
        {
          Extents3d ext = solid.GeometricExtents;
          if (!hasExt)
          {
            supportExt = ext;
            hasExt = true;
          }
          else
          {
            supportExt.AddExtents(ext);
          }
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL supportExt solid skip type=" + solid.GetType().Name + ": " + ex.GetType().Name + ": " + ex.Message);
        }

        return;
      }

      DBObjectCollection exploded = new DBObjectCollection();
      try
      {
        ent.Explode(exploded);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL supportExt explode skip depth=" + depth + " type=" + ent.GetType().Name + ": " + ex.GetType().Name + ": " + ex.Message);
        return;
      }

      foreach (DBObject child in exploded)
      {
        Entity childEntity = child as Entity;
        try
        {
          if (childEntity != null)
            this.AccumulateNotabSupportSolidExtents(childEntity, depth + 1, ref hasExt, ref supportExt);
        }
        finally
        {
          if (child != null)
            child.Dispose();
        }
      }
    }

    private void TryStripCleanSolidMetadata(Solid3d solid)
    {
      if (solid == null)
        return;

      try
      {
        if (!solid.ExtensionDictionary.IsNull)
        {
          System.Reflection.MethodInfo releaseMethod = solid.GetType().GetMethod("ReleaseExtensionDictionary", System.Type.EmptyTypes);
          if (releaseMethod != null)
            releaseMethod.Invoke(solid, null);
          else
            PlantOrthoView.FileDiag("PFSNOTABDETAIL cleanSolid dict remains: ReleaseExtensionDictionary 없음");
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL cleanSolid dict strip skip: " + ex.GetType().Name + ": " + ex.Message);
      }

      try
      {
        if (solid.XData != null)
          solid.XData = null;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL cleanSolid xdata strip skip: " + ex.GetType().Name + ": " + ex.Message);
      }

    }

    private int PrepareClonedNotabSolids(Transaction tr, IdMapping idMap, ObjectId layerId, out Extents3d solidExt)
    {
      solidExt = new Extents3d();
      bool hasExt = false;
      int cloned = 0;
      if (tr == null || idMap == null)
        return 0;

      foreach (IdPair pair in idMap)
      {
        if (!pair.IsPrimary || !pair.IsCloned)
          continue;

        Entity e = tr.GetObject(pair.Value, OpenMode.ForWrite, false) as Entity;
        if (e == null)
          continue;
        if (layerId != ObjectId.Null)
          e.LayerId = layerId;
        Solid3d solid = e as Solid3d;
        if (solid == null)
          continue;

        cloned++;
        try
        {
          Extents3d ext = solid.GeometricExtents;
          if (!hasExt)
          {
            solidExt = ext;
            hasExt = true;
          }
          else
          {
            solidExt.AddExtents(ext);
          }
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL solid ext skip id=" + pair.Value + ": " + ex.GetType().Name + ": " + ex.Message);
        }
      }

      if (!hasExt)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL solid ext 없음 cloned=" + cloned);
        return 0;
      }
      return cloned;
    }

    private bool CreateNotabDetailViewport(Transaction tr, Database db, out ObjectId viewportId)
    {
      viewportId = ObjectId.Null;
      if (tr == null || db == null)
        return false;

      try
      {
        ObjectId layoutBtrId = this.GetNotabDetailLayoutBlockTableRecordId(db, tr);
        if (layoutBtrId == ObjectId.Null)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport skip: Title Block/Layout1 layout 없음");
          return false;
        }

        BlockTableRecord layoutBtr = tr.GetObject(layoutBtrId, OpenMode.ForWrite) as BlockTableRecord;
        if (layoutBtr == null)
          return false;

        const double TargetMinX = 30.5;     // ID 실측: 도면영역 LL(30.5,84.5)~UR(640.5,573.5)
        const double TargetMinY = 84.5;
        const double TargetWidth = 610.0;
        const double TargetHeight = 489.0;
        double tcx = TargetMinX + (TargetWidth / 2.0);
        double tcy = TargetMinY + (TargetHeight / 2.0);

        Viewport vp = new Viewport();
        layoutBtr.AppendEntity(vp);
        tr.AddNewlyCreatedDBObject(vp, true);
        viewportId = vp.ObjectId;
        vp.CenterPoint = new Point3d(tcx, tcy, 0.0);
        vp.Width = TargetWidth;
        vp.Height = TargetHeight;
        vp.On = true;
        PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport rect LL=(" + this.FormatNumber(TargetMinX) + "," + this.FormatNumber(TargetMinY) + ") size=(" + this.FormatNumber(TargetWidth) + "," + this.FormatNumber(TargetHeight) + ") center=(" + this.FormatNumber(tcx) + "," + this.FormatNumber(tcy) + ")");
        PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport 1차 생성 완료 id=" + viewportId + " center=(" + this.FormatNumber(tcx) + "," + this.FormatNumber(tcy) + ") size=(" + this.FormatNumber(TargetWidth) + "," + this.FormatNumber(TargetHeight) + ") source=fixedRect");
        return true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      return false;
    }

    private bool ConfigureNotabDetailViewport(Database db, ObjectId viewportId, Extents3d targetExt, Extents3d supportExt, string targetSource)
    {
      if (db == null || viewportId == ObjectId.Null)
        return false;

      try
      {
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          Viewport vp = tr.GetObject(viewportId, OpenMode.ForWrite, false) as Viewport;
          if (vp == null)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport 2차 skip: viewport null id=" + viewportId);
            tr.Commit();
            return false;
          }

          Point3d target = new Point3d(
            (targetExt.MinPoint.X + targetExt.MaxPoint.X) / 2.0,
            (targetExt.MinPoint.Y + targetExt.MaxPoint.Y) / 2.0,
            (targetExt.MinPoint.Z + targetExt.MaxPoint.Z) / 2.0);
          Vector3d viewDir;
          Vector3d viewUp;
          Vector3d viewRight;
          this.GetNotabViewBasis(out viewDir, out viewUp, out viewRight);
          double twist = this.ComputeNotabViewportTwist(viewDir, viewUp);
          Point2d viewCenter;
          double viewHeight;
          double fitWidth;
          double fitHeight;
          double stdScale;
          double targetFill;
          double actualFill;
          this.ComputeNotabViewportFit(targetExt, target, viewRight, viewUp, vp.Width, vp.Height, out viewCenter, out viewHeight, out fitWidth, out fitHeight, out stdScale, out targetFill, out actualFill);

          vp.ViewTarget = target;
          vp.ViewDirection = viewDir;
          vp.ViewCenter = viewCenter;
          vp.ViewHeight = viewHeight;
          vp.CustomScale = stdScale;
          vp.TwistAngle = twist;
          // 기본=와이어프레임(0-A 육안 채택). Hidden 은선제거는 opt-in(PFS_NOTAB_USE_HIDDEN=1).
          // 미적용 시 뷰포트는 기본 2D 와이어프레임으로 렌더된다(VisualStyleId 미지정).
          bool useHidden = string.Equals(System.Environment.GetEnvironmentVariable("PFS_NOTAB_USE_HIDDEN"), "1", System.StringComparison.OrdinalIgnoreCase);
          bool hiddenOk = false;
          bool shadeOk = false;
          if (useHidden)
          {
            hiddenOk = this.TryApplyHiddenVisualStyle(db, tr, vp);
            shadeOk = this.TrySetViewportShadePlotHidden(vp);
          }
          else
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL wireframe(기본, hidden skip)");
          }

          PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport target=(" + this.FormatNumber(target.X) + "," + this.FormatNumber(target.Y) + "," + this.FormatNumber(target.Z) + ") src=" + (string.IsNullOrWhiteSpace(targetSource) ? "fallback" : targetSource));
          PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport fit center=(" + this.FormatNumber(viewCenter.X) + "," + this.FormatNumber(viewCenter.Y) + ") ViewHeight=" + this.FormatNumber(viewHeight) + " content=(" + this.FormatNumber(fitWidth) + "," + this.FormatNumber(fitHeight) + ") vp=(" + this.FormatNumber(vp.Width) + "," + this.FormatNumber(vp.Height) + ")");
          PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport scale std=1:" + this.FormatNumber(1.0 / stdScale) + " viewHeight=" + this.FormatNumber(viewHeight) + " fill=" + this.FormatNumber(actualFill) + " targetFill=" + this.FormatNumber(targetFill) + " content=(" + this.FormatNumber(fitWidth) + "," + this.FormatNumber(fitHeight) + ") vp=(" + this.FormatNumber(vp.Width) + "," + this.FormatNumber(vp.Height) + ")");
          PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport viewDir=" + this.FormatVectorForCommand(viewDir) + " target=(" + this.FormatNumber(target.X) + "," + this.FormatNumber(target.Y) + "," + this.FormatNumber(target.Z) + ") twist=" + this.FormatNumber(twist) + " hidden=" + hiddenOk + " shadePlot=" + shadeOk + " fit=dynamic");
          PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport 2차 view설정 직전");
          tr.Commit();
          PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport 2차 view설정 완료");
          this.LogNotabDimProbe(db, viewportId, supportExt, fitWidth, fitHeight);
          return true;
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport 2차 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      return false;
    }

    private double ComputeNotabViewportTwist(Vector3d viewDir, Vector3d desiredUp)
    {
      try
      {
        Vector3d dir = viewDir.Length > 1e-6 ? viewDir.GetNormal() : Vector3d.XAxis;
        Vector3d baseUp = Vector3d.ZAxis - dir.MultiplyBy(Vector3d.ZAxis.DotProduct(dir));
        if (baseUp.Length <= 1e-6)
          baseUp = Vector3d.YAxis - dir.MultiplyBy(Vector3d.YAxis.DotProduct(dir));
        Vector3d up = desiredUp - dir.MultiplyBy(desiredUp.DotProduct(dir));
        if (up.Length <= 1e-6)
          up = baseUp;
        if (baseUp.Length <= 1e-6 || up.Length <= 1e-6)
          return 0.0;

        baseUp = baseUp.GetNormal();
        up = up.GetNormal();
        double sin = baseUp.CrossProduct(up).DotProduct(dir);
        double cos = baseUp.DotProduct(up);
        return System.Math.Atan2(sin, cos);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL twist 계산 예외: " + ex.GetType().Name + ": " + ex.Message);
        return 0.0;
      }
    }

    private void ComputeNotabViewportFit(Extents3d ext, Point3d target, Vector3d right, Vector3d up, double viewportWidth, double viewportHeight, out Point2d viewCenter, out double viewHeight, out double contentWidth, out double contentHeight, out double stdScale, out double targetFill, out double actualFill)
    {
      viewCenter = new Point2d(0.0, 0.0);
      viewHeight = 100.0;
      contentWidth = 0.0;
      contentHeight = 0.0;
      stdScale = 1.0;
      targetFill = this.GetEnvDouble("PFS_NOTAB_TARGET_FILL", 0.4, 0.1, 0.95);
      actualFill = 0.0;

      try
      {
        double minR = 0.0, maxR = 0.0, minU = 0.0, maxU = 0.0;
        bool hasProjection = false;
        foreach (Point3d corner in this.GetExtentsCorners(ext))
        {
          Vector3d v = corner - target;
          double r = v.DotProduct(right);
          double u = v.DotProduct(up);
          if (!hasProjection)
          {
            minR = maxR = r;
            minU = maxU = u;
            hasProjection = true;
          }
          else
          {
            if (r < minR) minR = r;
            if (r > maxR) maxR = r;
            if (u < minU) minU = u;
            if (u > maxU) maxU = u;
          }
        }

        if (!hasProjection)
          return;

        contentWidth = System.Math.Max(maxR - minR, 1.0);
        contentHeight = System.Math.Max(maxU - minU, 1.0);
        double aspect = (viewportWidth > 1e-6 && viewportHeight > 1e-6) ? viewportWidth / viewportHeight : 1.0;
        viewCenter = new Point2d((minR + maxR) / 2.0, (minU + maxU) / 2.0);
        double requiredViewHeight = System.Math.Max(contentHeight / targetFill, (contentWidth / targetFill) / aspect);
        double maxScale = viewportHeight > 1e-6 && requiredViewHeight > 1e-6 ? viewportHeight / requiredViewHeight : 1.0;
        stdScale = this.SelectNotabStandardScale(maxScale);
        viewHeight = viewportHeight > 1e-6 && stdScale > 1e-9 ? viewportHeight / stdScale : requiredViewHeight;
        if (viewHeight <= 1e-6)
          viewHeight = 100.0;
        actualFill = System.Math.Max(contentHeight / viewHeight, contentWidth / (viewHeight * aspect));
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport fit 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private void CaptureNotabPipeCenter(Database db, ObjectId pipeId)
    {
      s_isoPipeCenterWcs = Point3d.Origin;
      s_isoPipeCenterValid = false;
      s_isoPipeRadiusModel = double.NaN;
      if (db == null || pipeId == ObjectId.Null)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL pipe center skip: db/id invalid id=" + pipeId);
        return;
      }

      try
      {
        PSUtil ps = Commands.PSUtil;
        if (ps == null) ps = new PSUtil();
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          Point3d p0;
          Vector3d dir;
          double radius;
          if (this.TryGetPipeAxisFromId(tr, pipeId, ps, out p0, out dir, out radius))
          {
            s_isoPipeCenterWcs = p0;
            s_isoPipeCenterValid = true;
            s_isoPipeRadiusModel = radius > 1e-6 ? radius : double.NaN;
            PlantOrthoView.FileDiag("PFSNOTABDETAIL pipe center captured wcs=(" + this.FormatNumber(p0.X) + "," + this.FormatNumber(p0.Y) + "," + this.FormatNumber(p0.Z) + ") id=" + pipeId + " rModel=" + this.FormatNumber(s_isoPipeRadiusModel));
          }
          else
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL pipe center invalid id=" + pipeId);
          }
          tr.Commit();
        }
      }
      catch (System.Exception ex)
      {
            s_isoPipeCenterWcs = Point3d.Origin;
            s_isoPipeCenterValid = false;
            s_isoPipeRadiusModel = double.NaN;
        PlantOrthoView.FileDiag("PFSNOTABDETAIL pipe center 예외 id=" + pipeId + ": " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private double SelectNotabStandardScale(double maxScale)
    {
      double[] scales = new double[] { 1.0, 0.5, 0.2, 0.1, 0.05, 0.04, 0.02 };
      if (maxScale <= 1e-9)
        return scales[scales.Length - 1];

      for (int i = 0; i < scales.Length; i++)
      {
        if (scales[i] <= maxScale + 1e-9)
          return scales[i];
      }

      return scales[scales.Length - 1];
    }

    private void LogNotabDimProbe(Database db, ObjectId viewportId, Extents3d supportExt, double realW, double realH)
    {
      if (db == null || viewportId == ObjectId.Null)
        return;

      try
      {
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          Viewport vp = tr.GetObject(viewportId, OpenMode.ForRead, false) as Viewport;
          if (vp == null)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL dim-probe skip: viewport null id=" + viewportId);
            tr.Commit();
            return;
          }

          Extents3d paperExt;
          double pipeCenterXPaper;
          double pipeCenterYPaper;
          bool hasPaper = this.TryComputeNotabDimPaperGeometry(vp, supportExt, out paperExt, out pipeCenterXPaper, out pipeCenterYPaper);
          double scale = this.GetNotabViewportScale(vp);

          PlantOrthoView.FileDiag("PFSNOTABDETAIL dim-probe support-paper=" + (hasPaper ? this.FormatExtents(paperExt) : "none") + " pipeCenterX(paper)=" + (double.IsNaN(pipeCenterXPaper) ? "NaN" : this.FormatNumber(pipeCenterXPaper)) + " pipeCenterY(paper)=" + (double.IsNaN(pipeCenterYPaper) ? "NaN" : this.FormatNumber(pipeCenterYPaper)) + " realW=" + this.FormatNumber(realW) + " realH=" + this.FormatNumber(realH) + " scale=" + this.FormatNumber(scale) + " vpCenter=(" + this.FormatNumber(vp.CenterPoint.X) + "," + this.FormatNumber(vp.CenterPoint.Y) + ") vpTarget=(" + this.FormatNumber(vp.ViewTarget.X) + "," + this.FormatNumber(vp.ViewTarget.Y) + "," + this.FormatNumber(vp.ViewTarget.Z) + ")");
          tr.Commit();
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL dim-probe 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private bool TryComputeNotabDimPaperGeometry(Viewport vp, Extents3d supportExt, out Extents3d paperExt, out double pipeCenterXPaper, out double pipeCenterYPaper)
    {
      paperExt = new Extents3d();
      pipeCenterXPaper = double.NaN;
      pipeCenterYPaper = double.NaN;
      if (vp == null)
        return false;

      try
      {
        Point3d[] corners = this.GetExtentsCorners(supportExt);
        bool hasPaper = false;
        for (int i = 0; i < corners.Length; i++)
        {
          Point3d paper = this.NotabProjectWcsToPaper(vp, corners[i]);
          if (!hasPaper)
          {
            paperExt = new Extents3d(paper, paper);
            hasPaper = true;
          }
          else
          {
            paperExt.AddPoint(paper);
          }
        }

        Point3d supportCenter = new Point3d(
          (supportExt.MinPoint.X + supportExt.MaxPoint.X) / 2.0,
          (supportExt.MinPoint.Y + supportExt.MaxPoint.Y) / 2.0,
          (supportExt.MinPoint.Z + supportExt.MaxPoint.Z) / 2.0);
        Point3d pipePaper = this.NotabProjectWcsToPaper(vp, supportCenter);
        if (s_isoPipeCenterValid)
        {
          try
          {
            pipePaper = this.NotabProjectWcsToPaper(vp, s_isoPipeCenterWcs);
          }
          catch (System.Exception px)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL pipe center project 예외: " + px.GetType().Name + ": " + px.Message);
            pipePaper = this.NotabProjectWcsToPaper(vp, supportCenter);
          }
        }
        pipeCenterXPaper = pipePaper.X;
        pipeCenterYPaper = pipePaper.Y;
        return hasPaper;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL dim-geometry 예외: " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
    }

    // 실패 시 Point3d.Origin을 반환하는 기존 함수는 정상값(원점)과 폴백을 구분할 수 없다.
    // 밸룬처럼 "실패하면 그리지 않아야" 하는 경로는 반드시 이쪽을 쓴다.
    private bool TryNotabProjectWcsToPaper(Viewport vp, Point3d wcs, out Point3d paper)
    {
      paper = Point3d.Origin;
      if (vp == null)
        return false;

      try
      {
        Vector3d viewDir = vp.ViewDirection;
        if (viewDir.Length < 1e-9)
          viewDir = Vector3d.ZAxis;

        Point3d viewTarget = vp.ViewTarget;
        Matrix3d dcsToWcs =
          Matrix3d.Rotation(-vp.TwistAngle, viewDir, viewTarget) *
          Matrix3d.Displacement(viewTarget - Point3d.Origin) *
          Matrix3d.PlaneToWorld(viewDir);
        Point3d dcs = wcs.TransformBy(dcsToWcs.Inverse());
        double scale = this.GetNotabViewportScale(vp);
        double px = vp.CenterPoint.X + (dcs.X - vp.ViewCenter.X) * scale;
        double py = vp.CenterPoint.Y + (dcs.Y - vp.ViewCenter.Y) * scale;
        if (double.IsNaN(px) || double.IsNaN(py) || double.IsInfinity(px) || double.IsInfinity(py))
          return false;
        paper = new Point3d(px, py, 0.0);
        return true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL try-project 예외: " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
    }

    private Point3d NotabProjectWcsToPaper(Viewport vp, Point3d wcs)
    {
      if (vp == null)
        return Point3d.Origin;

      try
      {
        Vector3d viewDir = vp.ViewDirection;
        if (viewDir.Length < 1e-9)
          viewDir = Vector3d.ZAxis;

        Point3d viewTarget = vp.ViewTarget;
        Matrix3d dcsToWcs =
          Matrix3d.Rotation(-vp.TwistAngle, viewDir, viewTarget) *
          Matrix3d.Displacement(viewTarget - Point3d.Origin) *
          Matrix3d.PlaneToWorld(viewDir);
        Matrix3d wcsToDcs = dcsToWcs.Inverse();

        Point3d dcs = wcs.TransformBy(wcsToDcs);
        double scale = this.GetNotabViewportScale(vp);
        double paperX = vp.CenterPoint.X + (dcs.X - vp.ViewCenter.X) * scale;
        double paperY = vp.CenterPoint.Y + (dcs.Y - vp.ViewCenter.Y) * scale;
        return new Point3d(paperX, paperY, 0.0);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL project 예외: " + ex.GetType().Name + ": " + ex.Message);
        return Point3d.Origin;
      }
    }

    private double GetNotabViewportScale(Viewport vp)
    {
      if (vp == null)
        return 1.0;
      if (vp.CustomScale > 1e-9)
        return vp.CustomScale;
      if (vp.Height > 1e-9 && vp.ViewHeight > 1e-9)
        return vp.Height / vp.ViewHeight;
      return 1.0;
    }

    private void AppendNotabPaperDimensions(Database db, ObjectId viewportId, Extents3d supportExt)
    {
      if (db == null || viewportId == ObjectId.Null)
        return;

      try
      {
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          Viewport vp = tr.GetObject(viewportId, OpenMode.ForRead, false) as Viewport;
          if (vp == null)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL dim append skip: viewport null id=" + viewportId);
            tr.Commit();
            return;
          }

          System.Collections.Generic.List<NotabMemberBox> memberBoxes = this.CollectNotabMemberBoxes(tr, db, vp);
          this.LogNotabSupportPortProjection(vp);

          Extents3d supportPaperExt;
          double pipeCenterXPaper;
          double pipeCenterYPaper;
          if (!this.TryComputeNotabDimPaperGeometry(vp, supportExt, out supportPaperExt, out pipeCenterXPaper, out pipeCenterYPaper))
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL dim append skip: paper geometry 실패");
            tr.Commit();
            return;
          }

          ObjectId layoutBtrId = this.GetNotabDetailLayoutBlockTableRecordId(db, tr);
          BlockTableRecord layoutBtr = layoutBtrId == ObjectId.Null ? null : tr.GetObject(layoutBtrId, OpenMode.ForWrite, false) as BlockTableRecord;
          if (layoutBtr == null)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL dim append skip: layout btr null");
            tr.Commit();
            return;
          }

          ObjectId layerId;
          ObjectId textStyleId;
          ObjectId dimStyleId;
          this.EnsureIsoAnnotationResources(db, tr, out layerId, out textStyleId, out dimStyleId);
          double pipePaperRadius = s_isoPipeRadiusModel > 1e-6
            ? s_isoPipeRadiusModel * this.GetNotabViewportScale(vp)
            : double.NaN;
          if (double.IsNaN(pipePaperRadius))
            pipePaperRadius = this.TryGetNotabPipePaperRadiusFromDetailSolids(tr, db, vp, pipeCenterXPaper, pipeCenterYPaper);
          PlantOrthoView.FileDiag("PFSNOTABDETAIL pipe paper radius=" + this.FormatNumber(pipePaperRadius) + " source=" + (s_isoPipeRadiusModel > 1e-6 ? "model" : "detail-solid"));
          Extents3d viewportPaperExt = new Extents3d(new Point3d(vp.CenterPoint.X - vp.Width / 2.0, vp.CenterPoint.Y - vp.Height / 2.0, 0.0), new Point3d(vp.CenterPoint.X + vp.Width / 2.0, vp.CenterPoint.Y + vp.Height / 2.0, 0.0));
          this.AppendNotabPaperDimensions(tr, layoutBtr, supportPaperExt, viewportPaperExt, pipeCenterXPaper, pipeCenterYPaper, pipePaperRadius, s_isoRealWidth, s_isoRealHeight, dimStyleId, layerId, textStyleId, db, memberBoxes, vp);
          tr.Commit();
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL dim append 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private struct NotabMemberBox
    {
      public ObjectId Id;
      public string Handle;
      public Extents3d PaperBox;
      public Vector3d WcsDims;
      public double Aspect;
      public double PipeDist;
    }

    private System.Collections.Generic.List<NotabMemberBox> CollectNotabMemberBoxes(Transaction tr, Database db, Viewport vp)
    {
      System.Collections.Generic.List<NotabMemberBox> result = new System.Collections.Generic.List<NotabMemberBox>();
      if (tr == null || db == null || vp == null)
        return result;

      try
      {
        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
        if (bt == null)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL member-spike skip: block table null");
          return result;
        }
        BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
        if (ms == null)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL member-spike skip: model space null");
          return result;
        }

        int i = 0;
        foreach (ObjectId id in ms)
        {
          Entity ent = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
          if (ent == null)
            continue;
          string kind = ent.GetType().Name;
          string layer = ent.Layer ?? string.Empty;
          int colorIndex = ent.ColorIndex;
          string handle = ent.Handle.ToString();
          Extents3d we;
          try { we = ent.GeometricExtents; }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL member-spike extents skip kind=" + kind + " 예외=" + ex.GetType().Name);
            continue;
          }
          double dx = we.MaxPoint.X - we.MinPoint.X, dy = we.MaxPoint.Y - we.MinPoint.Y, dz = we.MaxPoint.Z - we.MinPoint.Z;
          Point3d[] corners = this.GetExtentsCorners(we);
          double pminx = double.MaxValue, pminy = double.MaxValue, pmaxx = double.MinValue, pmaxy = double.MinValue;
          for (int c = 0; c < corners.Length; c++)
          {
            Point3d p = this.NotabProjectWcsToPaper(vp, corners[c]);
            pminx = System.Math.Min(pminx, p.X);
            pminy = System.Math.Min(pminy, p.Y);
            pmaxx = System.Math.Max(pmaxx, p.X);
            pmaxy = System.Math.Max(pmaxy, p.Y);
          }
          double[] dims = new double[] { dx, dy, dz };
          System.Array.Sort(dims);
          double aspectLongMid = dims[0] > 1e-6 ? dims[2] / dims[0] : -1;
          double pipeDist = -1;
          if (s_isoPipeCenterValid)
          {
            Point3d ec = new Point3d((we.MinPoint.X + we.MaxPoint.X) / 2.0, (we.MinPoint.Y + we.MaxPoint.Y) / 2.0, (we.MinPoint.Z + we.MaxPoint.Z) / 2.0);
            pipeDist = ec.DistanceTo(s_isoPipeCenterWcs);
          }
          PlantOrthoView.FileDiag("PFSNOTABDETAIL member-spike i=" + (i++) + " id=" + id + " handle=" + handle + " kind=" + kind + " layer=" + layer + " colorIndex=" + colorIndex
            + " wcsDims=(" + this.FormatNumber(dx) + "," + this.FormatNumber(dy) + "," + this.FormatNumber(dz) + ")"
            + " aspect=" + this.FormatNumber(aspectLongMid)
            + " paperBox=(" + this.FormatNumber(pminx) + "," + this.FormatNumber(pminy) + ")~(" + this.FormatNumber(pmaxx) + "," + this.FormatNumber(pmaxy) + ")"
            + " paperWH=(" + this.FormatNumber(pmaxx - pminx) + "," + this.FormatNumber(pmaxy - pminy) + ")"
            + " pipeDist=" + this.FormatNumber(pipeDist));
          result.Add(new NotabMemberBox { Id = id, Handle = handle, PaperBox = new Extents3d(new Point3d(pminx, pminy, 0.0), new Point3d(pmaxx, pmaxy, 0.0)), WcsDims = new Vector3d(dx, dy, dz), Aspect = aspectLongMid, PipeDist = pipeDist });
        }
        PlantOrthoView.FileDiag("PFSNOTABDETAIL member-spike done count=" + i + " std=" + this.GetNotabStandardName());
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL member-spike 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
      return result;
    }

    private bool TryGetNotabRcHorizontalParams(out double total, out double left, out double right)
    {
      total = left = right = double.NaN;
      string aRaw, a1Raw, a2Raw;
      double a, a1;
      // 단락 평가로 미할당 경로가 생기므로 선언 시 초기화한다.
      double a2 = double.NaN;
      if (!s_isoSupportParams.TryGetValue("A", out aRaw) || !s_isoSupportParams.TryGetValue("A1", out a1Raw)
        || !double.TryParse(aRaw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out a)
        || !double.TryParse(a1Raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out a1))
        return false;
      bool hasA2 = s_isoSupportParams.TryGetValue("A2", out a2Raw)
        && double.TryParse(a2Raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out a2);
      total = a + a1;
      right = hasA2 ? a2 : a1;
      left = total - right;
      return !double.IsNaN(total) && !double.IsInfinity(total) && !double.IsNaN(left) && !double.IsInfinity(left)
        && !double.IsNaN(right) && !double.IsInfinity(right) && total > 1e-6 && left > 1e-6 && right > 1e-6;
    }

    private bool TryGetNotabVerticalMemberBox(System.Collections.Generic.List<NotabMemberBox> boxes, out NotabMemberBox vertical, out string reason)
    {
      vertical = new NotabMemberBox();
      reason = "no-candidates";
      if (boxes == null || boxes.Count < 2)
        return false;
      double minPipeDist = double.MaxValue;
      for (int i = 0; i < boxes.Count; i++)
        if (boxes[i].PipeDist >= 0.0 && boxes[i].PipeDist < minPipeDist)
          minPipeDist = boxes[i].PipeDist;
      int best = -1;
      double bestAspect = double.NegativeInfinity;
      bool tie = false;
      for (int i = 0; i < boxes.Count; i++)
      {
        NotabMemberBox box = boxes[i];
        if (minPipeDist < double.MaxValue && System.Math.Abs(box.PipeDist - minPipeDist) <= 1e-6)
          continue;
        double w = box.PaperBox.MaxPoint.X - box.PaperBox.MinPoint.X;
        double h = box.PaperBox.MaxPoint.Y - box.PaperBox.MinPoint.Y;
        if (w <= 1e-6 || h <= 1e-6)
          continue;
        double aspect = h / w;
        if (aspect > bestAspect + 1e-6)
        {
          best = i;
          bestAspect = aspect;
          tie = false;
        }
        else if (System.Math.Abs(aspect - bestAspect) <= 1e-6)
          tie = true;
      }
      if (best < 0 || tie || bestAspect <= 1.0)
      {
        reason = best < 0 ? "no-nonclamp-candidate" : (tie ? "vertical-aspect-tie" : "not-vertical");
        return false;
      }
      vertical = boxes[best];
      reason = "relative-aspect-max, clamp-pipeDist-min-excluded";
      return true;
    }

    private void AppendNotabPaperDimensions(Transaction tr, BlockTableRecord layoutBtr, Extents3d supportPaperExt, Extents3d viewportPaperExt, double pipeCenterXPaper, double pipeCenterYPaper, double pipePaperRadius, double realW, double realH, ObjectId dimStyleId, ObjectId layerId, ObjectId textStyleId, Database db, System.Collections.Generic.List<NotabMemberBox> memberBoxes, Viewport vp)
    {
      if (tr == null || layoutBtr == null)
        return;
      if (realW <= 1e-6 || realH <= 1e-6)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL dim append skip: real size invalid W=" + this.FormatNumber(realW) + " H=" + this.FormatNumber(realH));
        return;
      }

      try
      {
        double minX = supportPaperExt.MinPoint.X;
        double maxX = supportPaperExt.MaxPoint.X;
        double minY = supportPaperExt.MinPoint.Y;
        double maxY = supportPaperExt.MaxPoint.Y;
        double paperW = maxX - minX;
        double paperH = maxY - minY;
        if (paperW <= 1e-6 || paperH <= 1e-6)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL dim append skip: paper ext degenerate " + this.FormatExtents(supportPaperExt));
          return;
        }

        string standardName = this.GetNotabStandardName();
        double paramTotal = double.NaN, paramLeft = double.NaN, paramRight = double.NaN;
        bool rcMemberGeometry = string.Equals(standardName, "RC1", System.StringComparison.OrdinalIgnoreCase)
          || string.Equals(standardName, "RC2", System.StringComparison.OrdinalIgnoreCase)
          || string.Equals(standardName, "RC3", System.StringComparison.OrdinalIgnoreCase)
          || string.Equals(standardName, "RC4", System.StringComparison.OrdinalIgnoreCase)
          || string.Equals(standardName, "RC5", System.StringComparison.OrdinalIgnoreCase)
          || string.Equals(standardName, "RC7", System.StringComparison.OrdinalIgnoreCase);
        string dimHSource = "fallback=legacy";
        string dimHFallback = "not-rc";
        if (rcMemberGeometry && this.TryGetNotabRcHorizontalParams(out paramTotal, out paramLeft, out paramRight))
        {
          // RC7은 공통 파라미터 산출(A/A1)을 그대로 쓰되, 도면 투영 좌우가 규격 좌우와 반대다.
          // 소비부에서만 교환해 다른 RC 타입과 파라미터 원본을 보존한다.
          if (string.Equals(standardName, "RC7", System.StringComparison.OrdinalIgnoreCase))
          {
            double rc7Left = paramLeft;
            paramLeft = paramRight;
            paramRight = rc7Left;
          }
          double paperPerModel = paperW / realW;
          if (paperPerModel > 1e-9 && memberBoxes != null && memberBoxes.Count > 0)
          {
            int horizontalIndex = -1;
            double widest = 0.0;
            for (int i = 0; i < memberBoxes.Count; i++)
            {
              double candidateWidth = memberBoxes[i].PaperBox.MaxPoint.X - memberBoxes[i].PaperBox.MinPoint.X;
              if (candidateWidth > widest)
              {
                widest = candidateWidth;
                horizontalIndex = i;
              }
            }
            if (horizontalIndex >= 0 && widest > 1e-6)
            {
              maxX = memberBoxes[horizontalIndex].PaperBox.MaxPoint.X;
              minX = maxX - paramTotal * paperPerModel;
              paperW = maxX - minX;
              dimHSource = "params(A+A1)";
              dimHFallback = string.Empty;
            }
            else
              dimHFallback = "horizontal-member-box-invalid";
          }
          else
            dimHFallback = "paper-scale-or-member-box-invalid";
        }
        else if (rcMemberGeometry)
          dimHFallback = "params-missing-or-invalid";
        PlantOrthoView.FileDiag("PFSNOTABDETAIL dimH source=" + dimHSource + " reason=" + (string.IsNullOrEmpty(dimHFallback) ? "none" : dimHFallback)
          + " total=" + this.FormatNumber(double.IsNaN(paramTotal) ? realW : paramTotal) + " left=" + this.FormatNumber(paramLeft) + " right=" + this.FormatNumber(paramRight)
          + " extX=(" + this.FormatNumber(minX) + "," + this.FormatNumber(maxX) + ")");

        double txt = this.GetEnvDouble("PFS_NOTAB_DIM_TXT", 8.0, 0.5, 50.0);
        // 간격은 글자 높이 비례. 고정값이면 글자 크기를 바꿀 때 치수선이 허공에 뜬다.
        double offset = this.GetEnvDouble("PFS_NOTAB_DIM_OFFSET", txt * 2.0, 1.0, 100.0);
        double stack = this.GetEnvDouble("PFS_NOTAB_DIM_STACK", txt * 2.0, 1.0, 100.0);
        double centerY = (minY + maxY) / 2.0;
        string horizontalSide = this.GetNotabHorizontalDimSide(standardName);
        bool horizontalBottom = string.Equals(horizontalSide, "bottom", System.StringComparison.OrdinalIgnoreCase) || (string.Equals(horizontalSide, "auto", System.StringComparison.OrdinalIgnoreCase) && !double.IsNaN(pipeCenterYPaper) && pipeCenterYPaper < centerY - 1e-6);
        double horizontalBaseY = horizontalBottom ? minY : maxY;
        // 치수를 부재에서 더 떼어내는 여유. 세로·가로 모두 같은 값으로 밀어 대칭을 맞춘다.
        double dimClear = this.GetEnvDouble("PFS_NOTAB_DIM_CLEAR", 10.0, 0.0, 200.0);
        double splitY = horizontalBottom ? minY - offset - dimClear : maxY + offset + dimClear;
        double totalY = horizontalBottom ? splitY - stack : splitY + stack;
        // 가로는 부재 폭, 세로는 기둥(F2)을 재므로 기준을 별도로 유지한다.
        double dimReferenceMinX = minX;
        string dimReferenceSource = dimHSource;
        double leftReal = double.NaN;
        double rightReal = double.NaN;
        double splitGuardLimit = System.Math.Min(txt * 2.0, txt * 1.6 * 2.0);
        bool splitGuard = false;

        bool paramsHorizontal = string.Equals(dimHSource, "params(A+A1)", System.StringComparison.Ordinal);
        if (paramsHorizontal)
          pipeCenterXPaper = minX + paperW * (paramLeft / paramTotal);
        bool splitOk = !double.IsNaN(pipeCenterXPaper) && pipeCenterXPaper > minX + 1e-6 && pipeCenterXPaper < maxX - 1e-6;
        if (splitOk)
        {
          leftReal = paramsHorizontal ? paramLeft : realW * (pipeCenterXPaper - minX) / paperW;
          rightReal = paramsHorizontal ? paramRight : realW - leftReal;
          double leftPaper = pipeCenterXPaper - minX;
          double rightPaper = maxX - pipeCenterXPaper;
          // A/A1은 파이프 중심이 아니라 규격 파라미터 경계다. 작은 A1도 유효한 분할이므로
          // 파이프 중심용 최소 간격 가드를 적용하지 않는다.
          splitGuard = !paramsHorizontal && (leftPaper < splitGuardLimit || rightPaper < splitGuardLimit);
          if (!splitGuard)
          {
            RotatedDimension dimLeft = PSUtil.CreateHorizontalDimension(new Point3d(minX, horizontalBaseY, 0.0), new Point3d(pipeCenterXPaper, horizontalBaseY, 0.0), new Point3d(minX, splitY, 0.0), Matrix3d.Identity, dimStyleId);
            RotatedDimension dimRight = PSUtil.CreateHorizontalDimension(new Point3d(pipeCenterXPaper, horizontalBaseY, 0.0), new Point3d(maxX, horizontalBaseY, 0.0), new Point3d(pipeCenterXPaper, splitY, 0.0), Matrix3d.Identity, dimStyleId);
            try
            {
              dimLeft.UsingDefaultTextPosition = false;
              dimLeft.TextPosition = new Point3d((minX + pipeCenterXPaper) / 2.0, splitY, 0.0);
              dimRight.UsingDefaultTextPosition = false;
              dimRight.TextPosition = new Point3d((pipeCenterXPaper + maxX) / 2.0, splitY, 0.0);
            }
            catch (System.Exception ex)
            {
              PlantOrthoView.FileDiag("PFSNOTABDETAIL dim split text position 예외: " + ex.GetType().Name + ": " + ex.Message + " leftMid=" + this.FormatNumber((minX + pipeCenterXPaper) / 2.0) + " rightMid=" + this.FormatNumber((pipeCenterXPaper + maxX) / 2.0) + " splitY=" + this.FormatNumber(splitY));
            }
            this.AppendNotabPaperDimensionEntity(tr, layoutBtr, dimLeft, dimStyleId, layerId, leftReal, txt, "dimSplitL");
            this.AppendNotabPaperDimensionEntity(tr, layoutBtr, dimRight, dimStyleId, layerId, rightReal, txt, "dimSplitR");
          }
          else
          {
            leftReal = double.NaN;
            rightReal = double.NaN;
            PlantOrthoView.FileDiag("PFSNOTABDETAIL dim append split guard centerX=" + this.FormatNumber(pipeCenterXPaper) + " leftPaper=" + this.FormatNumber(leftPaper) + " rightPaper=" + this.FormatNumber(rightPaper) + " limit=" + this.FormatNumber(splitGuardLimit));
          }
        }
        else
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL dim append split skip centerX=" + (double.IsNaN(pipeCenterXPaper) ? "NaN" : this.FormatNumber(pipeCenterXPaper)) + " paperMinX=" + this.FormatNumber(minX) + " paperMaxX=" + this.FormatNumber(maxX));
        }

        RotatedDimension dimTotal = PSUtil.CreateHorizontalDimension(new Point3d(minX, horizontalBaseY, 0.0), new Point3d(maxX, horizontalBaseY, 0.0), new Point3d(minX, totalY, 0.0), Matrix3d.Identity, dimStyleId);
        this.AppendNotabPaperDimensionEntity(tr, layoutBtr, dimTotal, dimStyleId, layerId, paramsHorizontal ? paramTotal : realW, txt, "dimH");

        double dimVTopY = maxY;
        double barRealH = double.NaN;
        double barPaperH = double.NaN;
        double vScale = paperH / realH;
        double dimVParamRealH = double.NaN;
        bool dimVBarSpan = false;
        string verticalMode = this.GetNotabVerticalDimMode(standardName);
        string dimVText = this.FormatNumber(realH);
        double verticalAnchorX = dimReferenceMinX;
        double verticalAnchorBaseY = minY;
        double verticalAnchorTopY = dimVTopY;
        string verticalAnchorSource = "fallback=" + dimReferenceSource;
        string verticalFallback = "not-rc";
        bool hasVerticalPortAnchor = false;
        if (string.Equals(verticalMode, "none", System.StringComparison.OrdinalIgnoreCase))
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL dimV skip: mode=none");
        }
        else if (string.Equals(verticalMode, "fheight", System.StringComparison.OrdinalIgnoreCase))
        {
          if (double.TryParse(s_isoSupportProfileHeight, out barRealH) && barRealH > 1e-6)
          {
            barPaperH = barRealH * vScale;
            if (barPaperH > 1e-6 && barPaperH <= paperH + 1e-6)
            {
              dimVTopY = minY + barPaperH;
              dimVBarSpan = true;
            }
            else
            {
              PlantOrthoView.FileDiag("PFSNOTABDETAIL dimV bar span fallback: barPaperH invalid barRealH=" + this.FormatNumber(barRealH) + " barPaperH=" + this.FormatNumber(barPaperH) + " paperH=" + this.FormatNumber(paperH) + " vScale=" + this.FormatNumber(vScale));
            }
          }
          else
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL dimV bar span fallback: profile height parse fail value=" + (string.IsNullOrWhiteSpace(s_isoSupportProfileHeight) ? "empty" : s_isoSupportProfileHeight) + " paperH=" + this.FormatNumber(paperH) + " realH=" + this.FormatNumber(realH));
          }

          dimVText = string.IsNullOrWhiteSpace(s_isoSupportProfileHeight) ? this.FormatNumber(realH) : s_isoSupportProfileHeight;
        }
        else if (string.Equals(verticalMode, "pipecenter", System.StringComparison.OrdinalIgnoreCase))
        {
          if (!double.IsNaN(pipeCenterYPaper) && pipeCenterYPaper > minY + 1e-6 && pipeCenterYPaper < maxY - 1e-6 && vScale > 1e-9)
          {
            dimVTopY = pipeCenterYPaper;
            dimVBarSpan = false;
            dimVText = this.FormatNumber((pipeCenterYPaper - minY) / vScale);
          }
          else
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL dimV pipecenter fallback: pipeCenterY=" + (double.IsNaN(pipeCenterYPaper) ? "NaN" : this.FormatNumber(pipeCenterYPaper)) + " minY=" + this.FormatNumber(minY) + " maxY=" + this.FormatNumber(maxY));
          }
        }
        else if (string.Equals(verticalMode, "param", System.StringComparison.OrdinalIgnoreCase))
        {
          string paramKey = this.GetNotabVerticalParamKey(standardName);
          string paramValue = string.Empty;
          double paramRealH = double.NaN;
          bool hasParam = !string.IsNullOrWhiteSpace(paramKey)
            && s_isoSupportParams.TryGetValue(paramKey, out paramValue)
            && double.TryParse(paramValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out paramRealH);
          if (!hasParam && !string.IsNullOrWhiteSpace(paramKey))
            hasParam = s_isoSupportParams.TryGetValue(paramKey, out paramValue)
              && double.TryParse(paramValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out paramRealH);

          if (hasParam && paramRealH > 1e-6 && paramRealH <= realH + 1e-6 && vScale > 1e-9)
          {
            dimVTopY = minY + paramRealH * vScale;
            dimVBarSpan = true;
            dimVParamRealH = paramRealH;
            dimVText = this.FormatNumber(paramRealH);
            PlantOrthoView.FileDiag("PFSNOTABDETAIL dimV param key=" + paramKey + " raw=" + paramValue + " real=" + this.FormatNumber(paramRealH) + " topY=" + this.FormatNumber(dimVTopY) + " minY=" + this.FormatNumber(minY) + " maxY=" + this.FormatNumber(maxY) + " realH=" + this.FormatNumber(realH));
          }
          else
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL dimV param fallback full: key=" + (string.IsNullOrWhiteSpace(paramKey) ? "empty" : paramKey) + " raw=" + (string.IsNullOrWhiteSpace(paramValue) ? "empty" : paramValue) + " parsed=" + this.FormatNumber(paramRealH) + " realH=" + this.FormatNumber(realH) + " minY=" + this.FormatNumber(minY) + " maxY=" + this.FormatNumber(maxY) + " vScale=" + this.FormatNumber(vScale));
          }
        }

        // 세로 S2 포트 앵커는 param+F2+vertical config를 가진 타입에서 활성(RC1~6,9). 가로 게이트와 분리.
        NotabTypeConfig cfgV = this.GetNotabTypeConfig(standardName);
        bool verticalPortGate = string.Equals(cfgV.VerticalMode, "param", System.StringComparison.OrdinalIgnoreCase)
          && string.Equals(cfgV.VerticalParamKey, "F2", System.StringComparison.OrdinalIgnoreCase)
          && string.Equals(cfgV.MemberAnchorSide, "vertical", System.StringComparison.OrdinalIgnoreCase);
        if (verticalPortGate)
        {
          NotabSupportPortSnapshot s2 = null;
          for (int i = 0; i < s_isoSupportPorts.Count; i++)
          {
            if (s_isoSupportPorts[i].Index == 1)
            {
              s2 = s_isoSupportPorts[i];
              break;
            }
          }
          if (s2 == null)
            verticalFallback = "S2-index-missing";
          else if (!string.Equals(s2.Name, "S2", System.StringComparison.OrdinalIgnoreCase))
            verticalFallback = "S2-name-mismatch:" + s2.Name;
          else if (!dimVBarSpan || double.IsNaN(dimVParamRealH) || dimVParamRealH <= 1e-6 || vScale <= 1e-9)
            verticalFallback = "F2-or-vScale-invalid";
          else
          {
            Point3d post = this.NotabProjectWcsToPaper(vp, s2.Wcs);
            double extMinY = supportPaperExt.MinPoint.Y;
            double extMaxY = supportPaperExt.MaxPoint.Y;
            // S2는 기둥의 자유단이고, 기둥이 뻗는 방향은 타입마다 반대다.
            // (RC1=S2가 상단→아래로, RC2/RC3=S2가 하단→위로) 범위 안에 들어오는 쪽을 채택한다.
            double spanUp = post.Y + dimVParamRealH * vScale;
            double spanDown = post.Y - dimVParamRealH * vScale;
            double other = double.NaN;
            if (spanUp <= extMaxY + 1e-6 && spanUp >= extMinY - 1e-6)
              other = spanUp;
            else if (spanDown <= extMaxY + 1e-6 && spanDown >= extMinY - 1e-6)
              other = spanDown;

            if (post.Y < extMinY - 1e-6 || post.Y > extMaxY + 1e-6)
              verticalFallback = "post-outside-support-extents";
            else if (double.IsNaN(other))
              verticalFallback = "post-span-outside-extents";
            else
            {
              verticalAnchorX = post.X;
              verticalAnchorBaseY = System.Math.Min(post.Y, other);
              verticalAnchorTopY = System.Math.Max(post.Y, other);
              verticalAnchorSource = "port-S2" + (other > post.Y ? "(up)" : "(down)");
              verticalFallback = string.Empty;
              hasVerticalPortAnchor = true;
            }
          }
        }
        if (!string.Equals(verticalMode, "none", System.StringComparison.OrdinalIgnoreCase))
        {
          double verticalX = verticalAnchorX - offset - dimClear;
          PlantOrthoView.FileDiag("PFSNOTABDETAIL dimH src=" + dimReferenceSource + " x=(" + this.FormatNumber(minX) + "," + this.FormatNumber(maxX) + ")"
            + " | dimV src=" + verticalAnchorSource + " x=" + this.FormatNumber(verticalAnchorX) + " baseY=" + this.FormatNumber(verticalAnchorBaseY) + " topY=" + this.FormatNumber(verticalAnchorTopY) + " lineX=" + this.FormatNumber(verticalX)
            + " | fallback=" + (string.IsNullOrEmpty(verticalFallback) ? "none" : verticalFallback));
          RotatedDimension dimV = PSUtil.CreateVerticalDimension(new Point3d(verticalAnchorX, verticalAnchorBaseY, 0.0), new Point3d(verticalAnchorX, verticalAnchorTopY, 0.0), new Point3d(verticalX, verticalAnchorBaseY, 0.0), Matrix3d.Identity, dimStyleId);
          double dimVRealValue = string.Equals(verticalMode, "param", System.StringComparison.OrdinalIgnoreCase) && dimVBarSpan ? dimVParamRealH : realH;
          this.AppendNotabPaperDimensionEntity(tr, layoutBtr, dimV, dimStyleId, layerId, dimVRealValue, txt, "dimV", dimVText);
        }
        // 치수가 먼저 자리를 확정하고, 모든 콜아웃이 같은 충돌 상태를 공유한다.
        // 콜아웃 허용영역은 실제 뷰포트 사각형이다. 서포트 extents ± 100은 폭 넓은
        // 라인넘버 텍스트가 좌우 고정 상태에서 전량 영역 밖으로 떨어지게 만든다.
        double calloutMargin = 100.0;
        bool viewportBoundsOk = viewportPaperExt.MaxPoint.X - viewportPaperExt.MinPoint.X > 1.0
          && viewportPaperExt.MaxPoint.Y - viewportPaperExt.MinPoint.Y > 1.0;
        NotabCalloutPlacer calloutPlacer = viewportBoundsOk
          ? new NotabCalloutPlacer(viewportPaperExt.MinPoint.X, viewportPaperExt.MinPoint.Y, viewportPaperExt.MaxPoint.X, viewportPaperExt.MaxPoint.Y)
          : new NotabCalloutPlacer(minX - calloutMargin, minY - calloutMargin, maxX + calloutMargin, maxY + calloutMargin);
        PlantOrthoView.FileDiag("PFSNOTABDETAIL callout-bounds src=" + (viewportBoundsOk ? "viewport" : "supportExt±100")
          + " ext=" + this.FormatExtents(viewportBoundsOk ? viewportPaperExt : supportPaperExt));
        // 소유자 태그를 달아 밸룬 배치 때 리더 검사만 면제할 수 있게 한다.
        calloutPlacer.AddObstacle(supportPaperExt, "support");
        // 병합 솔리드는 기둥 폭을 대표하지 못한다. 포트 기반 상자만 파이프 콜아웃의
        // 장애물로 등록한다. RC5의 F2는 S3(PPorts[2])이므로 치수 S2 축을 재사용하지 않는다.
        // 세로재 없는 타입은 기존 경로를 그대로 둔다.
        if (hasVerticalPortAnchor)
        {
          double verticalMemberX = verticalAnchorX;
          double verticalMemberBaseY = verticalAnchorBaseY;
          double verticalMemberTopY = verticalAnchorTopY;
          string verticalMemberSource = "dim-port";
          bool verticalMemberBoxAvailable = true;
          if (string.Equals(standardName, "RC5", System.StringComparison.OrdinalIgnoreCase))
          {
            NotabSupportPortSnapshot f2Port = null;
            for (int i = 0; i < s_isoSupportPorts.Count; i++)
            {
              if (this.IsNotabVerticalMemberPort(s_isoSupportPorts[i].Wcs))
              {
                f2Port = s_isoSupportPorts[i];
                break;
              }
            }
            if (f2Port != null)
            {
              Point3d f2Paper = this.NotabProjectWcsToPaper(vp, f2Port.Wcs);
              double span = verticalAnchorTopY - verticalAnchorBaseY;
              double spanUp = f2Paper.Y + span;
              double spanDown = f2Paper.Y - span;
              double other = double.NaN;
              if (spanUp <= supportPaperExt.MaxPoint.Y + 1e-6 && spanUp >= supportPaperExt.MinPoint.Y - 1e-6)
                other = spanUp;
              else if (spanDown <= supportPaperExt.MaxPoint.Y + 1e-6 && spanDown >= supportPaperExt.MinPoint.Y - 1e-6)
                other = spanDown;
              if (!double.IsNaN(other))
              {
                verticalMemberX = f2Paper.X;
                verticalMemberBaseY = System.Math.Min(f2Paper.Y, other);
                verticalMemberTopY = System.Math.Max(f2Paper.Y, other);
                verticalMemberSource = "RC5-F2-port=" + f2Port.Name;
              }
              else
              {
                verticalMemberBoxAvailable = false;
                PlantOrthoView.FileDiag("PFSNOTABDETAIL vertical-member RC5 skip: F2 span outside support extents");
              }
            }
            else
            {
              verticalMemberBoxAvailable = false;
              PlantOrthoView.FileDiag("PFSNOTABDETAIL vertical-member RC5 skip: F2 port missing");
            }
          }
          if (verticalMemberBoxAvailable)
          {
            double verticalHalfWidth = System.Math.Max(txt, this.GetEnvDouble("PFS_NOTAB_DIM_ARR", 10.0, 0.5, 50.0)) / 2.0;
            Extents3d verticalMemberBox = new Extents3d(
              new Point3d(verticalMemberX - verticalHalfWidth, verticalMemberBaseY, 0.0),
              new Point3d(verticalMemberX + verticalHalfWidth, verticalMemberTopY, 0.0));
            calloutPlacer.AddObstacle(verticalMemberBox, "vertical-member");
            PlantOrthoView.FileDiag("PFSNOTABDETAIL vertical-member source=" + verticalMemberSource
              + " x=" + this.FormatNumber(verticalMemberX) + " spanY=" + this.FormatNumber(verticalMemberBaseY)
              + "~" + this.FormatNumber(verticalMemberTopY));
          }
        }
        this.AddNotabDimensionObstacles(tr, layoutBtr, calloutPlacer, txt);
        this.AddNotabPipeObstacle(calloutPlacer, pipeCenterXPaper, pipeCenterYPaper, pipePaperRadius);
        // 표를 먼저 그리고 장애물로 등록해야 이후 콜아웃·밸룬이 표를 피한다.
        this.AppendNotabBomTable(tr, layoutBtr, db, textStyleId, layerId, calloutPlacer);
        Point3d calloutCostAnchor = (!double.IsNaN(pipeCenterXPaper) && !double.IsNaN(pipeCenterYPaper))
          ? new Point3d(pipeCenterXPaper, pipeCenterYPaper, 0.0)
          : new Point3d((minX + maxX) / 2.0, (minY + maxY) / 2.0, 0.0);
        calloutPlacer.SetCostReference(supportPaperExt, calloutCostAnchor);
        // 부재 밸룬(F 계열)은 부재 끝단 바깥이라는 확정 위치가 있으므로 콜아웃보다 먼저 잡는다.
        // 표·치수·파이프 장애물은 이미 등록돼 있어 밸룬이 그것들을 침범하지 않는다.
        this.AppendNotabBalloons(tr, layoutBtr, db, textStyleId, layerId, viewportPaperExt, txt, calloutPlacer, vp,
          hasVerticalPortAnchor, verticalAnchorX, verticalAnchorBaseY, verticalAnchorTopY, dimReferenceMinX, maxX,
          true);
        this.AppendNotabPipeCallout(tr, layoutBtr, db, textStyleId, layerId, pipeCenterXPaper, pipeCenterYPaper, supportPaperExt, viewportPaperExt, txt, calloutPlacer);
        Point3d verticalMemberAnchor = new Point3d(verticalAnchorX, verticalAnchorBaseY + (verticalAnchorTopY - verticalAnchorBaseY) * 0.5, 0.0);
        // 기본은 밸룬이 부재 표기를 대신한다(규격은 BOM 표에서 읽는다).
        // PFS_NOTAB_MEMBER_TEXT=1이면 기존 텍스트 콜아웃도 함께 그려 두 형태를 비교할 수 있다.
        bool memberText = this.GetEnvDouble("PFS_NOTAB_MEMBER_TEXT", 0.0, 0.0, 1.0) >= 0.5;
        bool balloonsAvailable = s_isoBalloonAnchors.Count > 0 && s_isoBomRows.Count > 0;
        if (memberText || !balloonsAvailable)
        {
          this.AppendNotabProfileCallout(tr, layoutBtr, db, textStyleId, layerId, supportPaperExt, viewportPaperExt, txt, calloutPlacer, memberBoxes, hasVerticalPortAnchor, verticalMemberAnchor);
          PlantOrthoView.FileDiag("PFSNOTABDETAIL member-text drawn reason=" + (memberText ? "env" : "no-balloon-source"));
        }
        else
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL member-text skip: balloon replaces (anchors=" + s_isoBalloonAnchors.Count + " bomRows=" + s_isoBomRows.Count + ")");
        }
        this.AppendNotabUboltCallouts(tr, layoutBtr, db, textStyleId, layerId, supportPaperExt, viewportPaperExt, txt, calloutPlacer, vp, memberBoxes);
        // 부재가 아닌 밸룬(P1 등)은 마지막. 기존 확정 배치의 회귀를 최소화한다.
        this.AppendNotabBalloons(tr, layoutBtr, db, textStyleId, layerId, viewportPaperExt, txt, calloutPlacer, vp,
          hasVerticalPortAnchor, verticalAnchorX, verticalAnchorBaseY, verticalAnchorTopY, dimReferenceMinX, maxX,
          false);

        PlantOrthoView.FileDiag("PFSNOTABDETAIL dim append H=" + this.FormatNumber(realW) + " V=" + this.FormatNumber(realH) + " dimV(F)=" + dimVText + " dimVBarSpan=" + dimVBarSpan + " vMode=" + verticalMode + " barRealH=" + (double.IsNaN(barPaperH) ? (string.IsNullOrWhiteSpace(s_isoSupportProfileHeight) ? "empty" : s_isoSupportProfileHeight) : this.FormatNumber(barRealH)) + " barPaperH=" + (double.IsNaN(barPaperH) ? "NaN" : this.FormatNumber(barPaperH)) + " paperH=" + this.FormatNumber(paperH) + " vScale=" + this.FormatNumber(vScale) + " callout=" + (string.IsNullOrWhiteSpace(s_isoSupportDesignation) ? "skip" : s_isoSupportDesignation) + " BI=" + (string.IsNullOrWhiteSpace(s_isoSupportBI) ? "skip" : s_isoSupportBI) + " split=(" + (double.IsNaN(leftReal) ? "skip" : this.FormatNumber(leftReal)) + "," + (double.IsNaN(rightReal) ? "skip" : this.FormatNumber(rightReal)) + ") side=" + (horizontalBottom ? "bottom" : "top") + " sideMode=" + horizontalSide + " type=" + (string.IsNullOrWhiteSpace(s_isoSupportTag) ? "unknown" : this.GetSupportTypePrefix(s_isoSupportTag)) + " std=" + standardName + " pipeCenterX(paper)=" + (double.IsNaN(pipeCenterXPaper) ? "NaN" : this.FormatNumber(pipeCenterXPaper)) + " pipeCenterY(paper)=" + (double.IsNaN(pipeCenterYPaper) ? "NaN" : this.FormatNumber(pipeCenterYPaper)) + " centerY=" + this.FormatNumber(centerY) + " splitGuard=" + splitGuard + " paperExt=" + this.FormatExtents(supportPaperExt) + " txt=" + this.FormatNumber(txt) + " offset=" + this.FormatNumber(offset) + " stack=" + this.FormatNumber(stack));
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL dim append entity 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private void AppendNotabPaperDimensionEntity(Transaction tr, BlockTableRecord layoutBtr, RotatedDimension dim, ObjectId dimStyleId, ObjectId layerId, double realValue, double txt, string label)
    {
      this.AppendNotabPaperDimensionEntity(tr, layoutBtr, dim, dimStyleId, layerId, realValue, txt, label, null);
    }

    private void AppendNotabPaperDimensionEntity(Transaction tr, BlockTableRecord layoutBtr, RotatedDimension dim, ObjectId dimStyleId, ObjectId layerId, double realValue, double txt, string label, string dimensionTextOverride)
    {
      if (tr == null || layoutBtr == null || dim == null)
        return;

      try
      {
        dim.DimensionStyle = dimStyleId;
        this.ApplyNotabPaperDimensionOverrides(dim, txt, label);
        dim.DimensionText = string.IsNullOrWhiteSpace(dimensionTextOverride) ? this.FormatDimNumber(realValue) : dimensionTextOverride;
        if (layerId != ObjectId.Null)
          dim.LayerId = layerId;
        layoutBtr.AppendEntity(dim);
        tr.AddNewlyCreatedDBObject(dim, true);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL " + label + " append 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private void ApplyNotabPaperDimensionOverrides(RotatedDimension dim, double txt, string label)
    {
      if (dim == null)
        return;

      try
      {
        double arr = this.GetEnvDouble("PFS_NOTAB_DIM_ARR", 10.0, 0.5, 50.0);
        dim.Dimtxt = txt;
        dim.Dimasz = arr;
        dim.Dimexe = 5.0;
        // 보조선이 부재에서 바로 시작하도록 원점 간격을 없앤다(치수-부재 연결).
        dim.Dimexo = 0.0;
        dim.Dimgap = txt * 0.6;
        dim.Dimscale = 1.0;
        dim.Dimtih = false;
        dim.Dimtoh = false;
        dim.Dimtad = 1;
        dim.Dimdec = 1;
        // Dim line forced=On. 보조선 간격이 좁아 문자가 밖으로 밀려도 치수선을 그린다.
        dim.Dimtofl = true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL " + label + " paper dim override 예외: " + ex.GetType().Name + ": " + ex.Message + " txt=" + this.FormatNumber(txt));
      }
    }

    // append 직후 치수 블록을 재계산해 실제 렌더 영역을 콜아웃 배치 장애물로 사용한다.
    private void AddNotabDimensionObstacles(Transaction tr, BlockTableRecord layoutBtr, NotabCalloutPlacer placer, double txt)
    {
      if (tr == null || layoutBtr == null || placer == null)
        return;

      int count = 0;
      foreach (ObjectId id in layoutBtr)
      {
        Dimension dim = tr.GetObject(id, OpenMode.ForRead, false) as Dimension;
        if (dim == null)
          continue;

        Extents3d extents;
        try
        {
          dim.UpgradeOpen();
          dim.RecomputeDimensionBlock(true);
          dim.DowngradeOpen();
          extents = dim.GeometricExtents;
        }
        catch (System.Exception dex)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL dim-obstacle 폴백 " + dex.GetType().Name + ": " + dex.Message);
          try
          {
            Point3d textPosition = dim.TextPosition;
            double pad = txt * 3.0;
            extents = new Extents3d(
              new Point3d(textPosition.X - pad, textPosition.Y - pad, 0.0),
              new Point3d(textPosition.X + pad, textPosition.Y + pad, 0.0));
          }
          catch (System.Exception fallbackEx)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL dim-obstacle 폴백 skip " + fallbackEx.GetType().Name + ": " + fallbackEx.Message);
            continue;
          }
        }

        Extents3d copied = new Extents3d(
          new Point3d(extents.MinPoint.X, extents.MinPoint.Y, 0.0),
          new Point3d(extents.MaxPoint.X, extents.MaxPoint.Y, 0.0));
        placer.AddObstacle(copied);
        count++;
        PlantOrthoView.FileDiag("PFSNOTABDETAIL dim-obstacle box=(" + this.FormatNumber(copied.MinPoint.X) + "," + this.FormatNumber(copied.MinPoint.Y) + ")~(" + this.FormatNumber(copied.MaxPoint.X) + "," + this.FormatNumber(copied.MaxPoint.Y) + ")");
      }
      PlantOrthoView.FileDiag("PFSNOTABDETAIL dim-obstacle count=" + count);
    }

    private void AddNotabPipeObstacle(NotabCalloutPlacer placer, double centerX, double centerY, double pipePaperRadius)
    {
      if (placer == null || double.IsNaN(centerX) || double.IsNaN(centerY) || double.IsNaN(pipePaperRadius) || pipePaperRadius <= 1e-6)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL pipe-obstacle skip: paper radius invalid");
        return;
      }
      try
      {
        Extents3d box = new Extents3d(new Point3d(centerX - pipePaperRadius, centerY - pipePaperRadius, 0.0), new Point3d(centerX + pipePaperRadius, centerY + pipePaperRadius, 0.0));
        placer.AddObstacle(box, "pipe");
        PlantOrthoView.FileDiag("PFSNOTABDETAIL pipe-obstacle box=(" + this.FormatNumber(box.MinPoint.X) + "," + this.FormatNumber(box.MinPoint.Y) + ")~(" + this.FormatNumber(box.MaxPoint.X) + "," + this.FormatNumber(box.MaxPoint.Y) + ") r=" + this.FormatNumber(pipePaperRadius));
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL pipe-obstacle 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private double TryGetNotabPipePaperRadiusFromDetailSolids(Transaction tr, Database db, Viewport vp, double centerX, double centerY)
    {
      if (tr == null || db == null || vp == null || double.IsNaN(centerX) || double.IsNaN(centerY))
        return double.NaN;
      try
      {
        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
        BlockTableRecord ms = bt == null ? null : tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
        if (ms == null)
          return double.NaN;
        double bestRadius = double.NaN;
        double bestDistance = double.MaxValue;
        foreach (ObjectId id in ms)
        {
          Solid3d solid = tr.GetObject(id, OpenMode.ForRead, false) as Solid3d;
          if (solid == null)
            continue;
          Extents3d ext = solid.GeometricExtents;
          Point3d[] corners = this.GetExtentsCorners(ext);
          double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
          for (int i = 0; i < corners.Length; i++)
          {
            Point3d paper = this.NotabProjectWcsToPaper(vp, corners[i]);
            minX = System.Math.Min(minX, paper.X); minY = System.Math.Min(minY, paper.Y);
            maxX = System.Math.Max(maxX, paper.X); maxY = System.Math.Max(maxY, paper.Y);
          }
          double w = maxX - minX, h = maxY - minY;
          if (w <= 1e-6 || h <= 1e-6 || System.Math.Abs(w - h) > System.Math.Min(w, h) * 0.2)
            continue;
          double candidateCenterX = (minX + maxX) / 2.0, candidateCenterY = (minY + maxY) / 2.0;
          double distance = new Point3d(candidateCenterX, candidateCenterY, 0.0).DistanceTo(new Point3d(centerX, centerY, 0.0));
          if (distance <= System.Math.Max(w, h) * 1.5 && distance < bestDistance)
          {
            bestDistance = distance;
            bestRadius = System.Math.Min(w, h) / 2.0;
          }
        }
        if (!double.IsNaN(bestRadius))
          PlantOrthoView.FileDiag("PFSNOTABDETAIL pipe radius detail-solid r=" + this.FormatNumber(bestRadius) + " dist=" + this.FormatNumber(bestDistance));
        return bestRadius;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL pipe radius detail-solid 예외: " + ex.GetType().Name + ": " + ex.Message);
        return double.NaN;
      }
    }

    private void AppendNotabDirectCallout(Transaction tr, BlockTableRecord layoutBtr, Database db, ObjectId textStyleId, ObjectId layerId, Extents3d supportPaperExt, double txt, NotabCalloutPlacer placer, string designation, Point3d anchor, double viewportCenterX, NotabCalloutPlacer.RequiredSide requiredSide, string sideSource, double gap, double minDx, string label)
    {
      this.AppendNotabDirectCallout(tr, layoutBtr, db, textStyleId, layerId, supportPaperExt, txt, placer, designation, anchor, viewportCenterX, requiredSide, sideSource, gap, minDx, label,
        false, new Extents3d(Point3d.Origin, Point3d.Origin), false, Point3d.Origin, new Extents3d(Point3d.Origin, Point3d.Origin));
    }

    // anchorBox를 주면 화살표를 대상 중심이 아니라 그 외곽(원형이면 quadrant)에 닿게 한다.
    private void AppendNotabDirectCallout(Transaction tr, BlockTableRecord layoutBtr, Database db, ObjectId textStyleId, ObjectId layerId, Extents3d supportPaperExt, double txt, NotabCalloutPlacer placer, string designation, Point3d anchor, double viewportCenterX, NotabCalloutPlacer.RequiredSide requiredSide, string sideSource, double gap, double minDx, string label,
      bool hasAnchorBox, Extents3d anchorBox, bool hasManualTextEdge, Point3d manualTextEdge, Extents3d manualBounds)
    {
      MText mt = new MText();
      try
      {
        mt.Contents = designation;
        mt.TextHeight = txt;
        mt.Attachment = AttachmentPoint.MiddleLeft;
        mt.Location = Point3d.Origin;
        if (textStyleId != ObjectId.Null) mt.TextStyleId = textStyleId;
        if (layerId != ObjectId.Null) mt.LayerId = layerId;
        mt.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex((Autodesk.AutoCAD.Colors.ColorMethod)195, (short)3);
        layoutBtr.AppendEntity(mt);
        tr.AddNewlyCreatedDBObject(mt, true);

        double actualWidth = System.Math.Max(1.0, mt.ActualWidth);
        double actualHeight = System.Math.Max(1.0, mt.ActualHeight);
        Point3d near = Point3d.Origin, p1 = Point3d.Origin, p2 = Point3d.Origin;
        double tailLength = 0.0;
        bool left = requiredSide == NotabCalloutPlacer.RequiredSide.Left;
        string diagnostic = "FAIL";
        bool smartPlaced = false;
        // 유볼트는 실제 화살표 시작점을 먼저 확정한 뒤, 동일한 점으로 배치 검사·등록·작도를 수행한다.
        Point3d leaderFrom = hasAnchorBox
          ? NotabCalloutPlacer.BoxVerticalEdgeMidpointToward(anchorBox,
            new Point3d(requiredSide == NotabCalloutPlacer.RequiredSide.Left ? anchor.X - 1.0 : anchor.X + 1.0, anchor.Y, 0.0))
          : anchor;
        double effectiveMinDx = minDx;
        if (hasAnchorBox)
        {
          double centerToEdge = System.Math.Abs(leaderFrom.X - anchor.X);
          effectiveMinDx = System.Math.Max(minDx, centerToEdge + gap);
        }
        if (placer != null)
        {
          bool isPipeCallout = label == "pipe callout";
          // 라인넘버(파이프)만 꺾임 1회. 유볼트·부재는 직선 1개를 유지한다(사용자 확정).
          tailLength = isPipeCallout ? this.GetEnvDouble("PFS_NOTAB_PIPE_LEADER_TAIL", txt * 2.0, 0.0, 200.0) : 0.0;
          // 부재 콜아웃은 하단 선호(치수가 항상 상단·좌측에 놓인다). 라인넘버는 상하 자유.
          if (hasManualTextEdge)
          {
            left = requiredSide == NotabCalloutPlacer.RequiredSide.Left;
            double boxLeft = left ? manualTextEdge.X - actualWidth : manualTextEdge.X;
            Extents3d manualBox = new Extents3d(
              new Point3d(boxLeft, manualTextEdge.Y - actualHeight / 2.0, 0.0),
              new Point3d(boxLeft + actualWidth, manualTextEdge.Y + actualHeight / 2.0, 0.0));
            bool manualOutOfBounds = manualBox.MinPoint.X < manualBounds.MinPoint.X
              || manualBox.MaxPoint.X > manualBounds.MaxPoint.X
              || manualBox.MinPoint.Y < manualBounds.MinPoint.Y
              || manualBox.MaxPoint.Y > manualBounds.MaxPoint.Y;
            if (manualOutOfBounds)
            {
              PlantOrthoView.FileDiag("PFSNOTABDETAIL callout-skip reason=env-pos-oob designation=" + designation
                + " end=" + this.FormatPoint(manualTextEdge) + " box=" + this.FormatExtents(manualBox));
            }
            else
            {
              near = manualTextEdge;
              p2 = manualTextEdge;
              p1 = tailLength > 1e-9
                ? new Point3d(left ? p2.X + tailLength : p2.X - tailLength, p2.Y, 0.0)
                : p2;
              diagnostic = "manual=env";
              smartPlaced = true;
            }
          }
          if (!smartPlaced)
            smartPlaced = placer.TryPlace(leaderFrom, requiredSide, actualWidth, actualHeight, gap, effectiveMinDx, isPipeCallout ? "pipe" : string.Empty, !isPipeCallout, tailLength, out near, out p1, out p2, out left, out diagnostic);
        }
        if (!smartPlaced)
        {
          mt.Erase();
          PlantOrthoView.FileDiag("PFSNOTABDETAIL callout-skip designation=" + designation + " requiredSide=" + (requiredSide == NotabCalloutPlacer.RequiredSide.Left ? "left" : "right") + " sideSrc=" + sideSource + " referenceX=" + this.FormatNumber(anchor.X) + " viewportCenterX=" + this.FormatNumber(viewportCenterX) + " " + diagnostic);
          return;
        }

        bool right = !left;
        double leftX = right ? System.Math.Max(near.X, leaderFrom.X + effectiveMinDx) : System.Math.Min(near.X, leaderFrom.X - effectiveMinDx) - actualWidth;
        double rightX = leftX + actualWidth;
        // placer가 확정한 정점을 그대로 사용해 충돌 모델과 작도를 일치시킨다.
        // 직선이면 p1==p2, 꺾임이면 p1=꺾임점 / p2=문자 접속점이다.
        double middleY = p2.Y;
        double nearX = p2.X;
        bool hasElbow = p1.DistanceTo(p2) > 1e-9;
        mt.Attachment = left ? AttachmentPoint.MiddleRight : AttachmentPoint.MiddleLeft;
        mt.Location = new Point3d(left ? rightX : leftX, middleY, 0.0);

        Leader leader = new Leader();
        leader.AppendVertex(leaderFrom);
        if (hasElbow) leader.AppendVertex(p1);
        leader.AppendVertex(new Point3d(nearX, middleY, 0.0));
        leader.HasArrowHead = true;
        leader.Dimasz = this.GetEnvDouble("PFS_NOTAB_DIM_ARR", 10.0, 0.5, 50.0);
        if (layerId != ObjectId.Null) leader.LayerId = layerId;
        leader.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex((Autodesk.AutoCAD.Colors.ColorMethod)195, (short)1);
        layoutBtr.AppendEntity(leader);
        tr.AddNewlyCreatedDBObject(leader, true);
        // 등록도 작도와 같은 선분 구성을 쓴다. 꺾임이면 두 선분, 직선이면 한 선분.
        if (placer != null)
        {
          if (hasElbow) placer.Commit(leaderFrom, p1, new Point3d(nearX, middleY, 0.0), near, actualWidth, actualHeight);
          else placer.Commit(leaderFrom, new Point3d(nearX, middleY, 0.0), near, actualWidth, actualHeight);
        }
        PlantOrthoView.FileDiag("PFSNOTABDETAIL callout-draw designation=" + designation + " anchor=" + anchor + " leaderFrom=" + this.FormatPoint(leaderFrom) + " elbow=" + (hasElbow ? this.FormatPoint(p1) : "none") + " tail=" + this.FormatNumber(tailLength) + " endX=" + this.FormatNumber(nearX) + " middleY=" + this.FormatNumber(middleY) + " effectiveMinDx=" + this.FormatNumber(effectiveMinDx) + " W=" + this.FormatNumber(actualWidth) + " H=" + this.FormatNumber(actualHeight) + " requiredSide=" + (requiredSide == NotabCalloutPlacer.RequiredSide.Left ? "left" : "right") + " sideSrc=" + sideSource + " referenceX=" + this.FormatNumber(anchor.X) + " viewportCenterX=" + this.FormatNumber(viewportCenterX) + " side=" + (right ? "right" : "left") + " sep=" + this.FormatNumber(right ? nearX - leaderFrom.X : leaderFrom.X - nearX) + " " + diagnostic);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL " + label + " direct draw 예외: " + ex.GetType().Name + ": " + ex.Message + " designation=" + designation);
      }
    }

    private NotabCalloutPlacer.RequiredSide ResolveNotabRequiredSide(string envKey, double referenceX, double viewportCenterX, out string source)
    {
      string raw = System.Environment.GetEnvironmentVariable(envKey);
      if (string.Equals(raw, "L", System.StringComparison.OrdinalIgnoreCase)) { source = "override"; return NotabCalloutPlacer.RequiredSide.Left; }
      if (string.Equals(raw, "R", System.StringComparison.OrdinalIgnoreCase)) { source = "override"; return NotabCalloutPlacer.RequiredSide.Right; }
      if (!string.IsNullOrWhiteSpace(raw))
        PlantOrthoView.FileDiag("PFSNOTABDETAIL deprecated direction env ignored key=" + envKey + " value=" + raw + " (use L/R)");
      source = "rule";
      return referenceX >= viewportCenterX ? NotabCalloutPlacer.RequiredSide.Right : NotabCalloutPlacer.RequiredSide.Left;
    }

    private void AppendNotabProfileCallout(Transaction tr, BlockTableRecord layoutBtr, Database db, ObjectId textStyleId, ObjectId layerId, Extents3d supportPaperExt, Extents3d viewportPaperExt, double txt, NotabCalloutPlacer placer, System.Collections.Generic.List<NotabMemberBox> memberBoxes, bool hasVerticalPortAnchor, Point3d verticalPortAnchor)
    {
      if (tr == null || layoutBtr == null || db == null)
        return;
      System.Collections.Generic.List<string> designations = new System.Collections.Generic.List<string>();
      if (s_isoSupportDesignations != null)
      {
        for (int i = 0; i < s_isoSupportDesignations.Count; i++)
        {
          if (!string.IsNullOrWhiteSpace(s_isoSupportDesignations[i]))
            designations.Add(s_isoSupportDesignations[i]);
        }
      }
      if (designations.Count == 0 && !string.IsNullOrWhiteSpace(s_isoSupportDesignation))
        designations.Add(s_isoSupportDesignation);
      if (designations.Count == 0)
        return;

      try
      {
        double offset = this.GetEnvDouble("PFS_NOTAB_DIM_OFFSET", 30.0, 1.0, 100.0);
        double arr = this.GetEnvDouble("PFS_NOTAB_DIM_ARR", 10.0, 0.5, 50.0);
        double gap = arr + txt * 0.6;
        double minDx = this.GetEnvDouble("PFS_NOTAB_CALLOUT_MIN_DX", 40.0, 0.0, 500.0);
        double mdx = this.GetEnvDouble("PFS_NOTAB_MEMBER_CALLOUT_DX", 0.0, -2000.0, 2000.0);
        double mdy = this.GetEnvDouble("PFS_NOTAB_MEMBER_CALLOUT_DY", 0.0, -2000.0, 2000.0);
        double minX = supportPaperExt.MinPoint.X;
        double maxX = supportPaperExt.MaxPoint.X;
        double centerX = (minX + maxX) / 2.0;
        double minY = supportPaperExt.MinPoint.Y;
        double h = supportPaperExt.MaxPoint.Y - minY;
        double width = maxX - minX;
        double vScale = s_isoRealHeight > 1e-6 ? h / s_isoRealHeight : 0.0;
        double barRealH;
        double barPaperH = h * 0.4;
        if (double.TryParse(s_isoSupportProfileHeight, out barRealH) && barRealH > 1e-6 && vScale > 0.0)
          barPaperH = barRealH * vScale;
        if (barPaperH <= 1e-6 || barPaperH > h)
          barPaperH = h * 0.4;
        bool multiDesignation = designations.Count > 1 && width > 1e-6;
        // GD 계열은 가로 부재가 하단, RC 계열은 상단에 있다. 기준면을 타입 설정으로 뒤집는다.
        string memberAnchorSide = this.GetNotabMemberAnchorSide(this.GetNotabStandardName());
        double maxYPaper = supportPaperExt.MaxPoint.Y;
        Point3d singleAnchor;
        if (string.Equals(memberAnchorSide, "vertical", System.StringComparison.OrdinalIgnoreCase))
        {
          NotabMemberBox verticalBox;
          string memberReason;
          if (hasVerticalPortAnchor)
          {
            singleAnchor = verticalPortAnchor;
            PlantOrthoView.FileDiag("PFSNOTABDETAIL member-anchor source=port-S2 anchor=(" + this.FormatNumber(singleAnchor.X) + "," + this.FormatNumber(singleAnchor.Y) + ")");
          }
          else if (this.TryGetNotabVerticalMemberBox(memberBoxes, out verticalBox, out memberReason))
          {
            singleAnchor = new Point3d((verticalBox.PaperBox.MinPoint.X + verticalBox.PaperBox.MaxPoint.X) / 2.0, (verticalBox.PaperBox.MinPoint.Y + verticalBox.PaperBox.MaxPoint.Y) / 2.0, 0.0);
            PlantOrthoView.FileDiag("PFSNOTABDETAIL member-anchor source=member-geometry reason=" + memberReason + " handle=" + verticalBox.Handle + " box=" + this.FormatExtents(verticalBox.PaperBox));
          }
          else
          {
            singleAnchor = new Point3d(minX + barPaperH * 0.5, maxYPaper - barPaperH * 1.5, 0.0);
            PlantOrthoView.FileDiag("PFSNOTABDETAIL member-anchor fallback=legacy reason=" + memberReason);
          }
        }
        else if (string.Equals(memberAnchorSide, "top", System.StringComparison.OrdinalIgnoreCase))
          singleAnchor = new Point3d(maxX, maxYPaper - barPaperH * 0.5, 0.0);
        else
          singleAnchor = new Point3d(maxX, minY + barPaperH * 0.5, 0.0);
        PlantOrthoView.FileDiag("PFSNOTABDETAIL member-anchor side=" + memberAnchorSide
          + " anchor=(" + this.FormatNumber(singleAnchor.X) + "," + this.FormatNumber(singleAnchor.Y) + ")"
          + " barPaperH=" + this.FormatNumber(barPaperH)
          + " minX=" + this.FormatNumber(minX) + " minY=" + this.FormatNumber(minY) + " maxY=" + this.FormatNumber(maxYPaper));

        for (int i = 0; i < designations.Count; i++)
        {
          string designation = designations[i];
          double fx = multiDesignation ? ((double)i + 0.5) / (double)designations.Count : 1.0;
          // GD2/GD3 2부재 전용 특수배치. 순서 역전은 아래 idx별 노브로 보정.
          string notabTypePrefix = this.GetSupportTypePrefix(s_isoSupportTag);
          bool twoMember = multiDesignation && designations.Count == 2;
          bool gd2Two = twoMember && string.Equals(notabTypePrefix, "GD2", System.StringComparison.OrdinalIgnoreCase);
          bool gd3Two = twoMember && string.Equals(notabTypePrefix, "GD3", System.StringComparison.OrdinalIgnoreCase);
          double anchorFx = fx;
          // 부재별 라이브 미세조정용 개별 노브(기본 0): DX0/DY0=idx0, DX1/DY1=idx1
          double mdxI = mdx + this.GetEnvDouble("PFS_NOTAB_MEMBER_CALLOUT_DX" + i, 0.0, -2000.0, 2000.0);
          double mdyI = mdy + this.GetEnvDouble("PFS_NOTAB_MEMBER_CALLOUT_DY" + i, 0.0, -2000.0, 2000.0);

          Point3d anchor;
          Point3d elbow;
          bool leftHalf;
          if (gd2Two && i == 0)
          {
            // idx0 = 중앙 수직재 몸통 지정, 텍스트는 좌측 하단 유지
            anchorFx = this.GetEnvDouble("PFS_NOTAB_GD2_ANCHOR_FX0", 0.5, 0.0, 1.0);
            anchor = new Point3d(minX + width * anchorFx, minY + h * 0.5, 0.0);
            elbow = new Point3d(minX - offset, minY - offset, 0.0);
            leftHalf = true;
          }
          else if (gd2Two && i == 1)
          {
            // idx1 = 하단 수평재 우측 지정, 텍스트 우측 하단
            anchorFx = this.GetEnvDouble("PFS_NOTAB_GD2_ANCHOR_FX1", 0.8, 0.0, 1.0);
            anchor = new Point3d(minX + width * anchorFx, minY, 0.0);
            elbow = new Point3d(maxX + offset, minY - offset * 2.0, 0.0);
            leftHalf = false;
          }
          else if (gd3Two && i == 0)
          {
            // GD3 납작기하: 좌측 1/4 지점의 수직재(앵글)를 지시, 텍스트 좌측 하단
            anchorFx = this.GetEnvDouble("PFS_NOTAB_GD3_ANCHOR_FX0", 0.305, 0.0, 1.0);
            anchor = new Point3d(minX + width * anchorFx, minY + h * 0.5, 0.0);
            elbow = new Point3d(minX - offset, minY - offset, 0.0);
            leftHalf = true;
          }
          else if (gd3Two && i == 1)
          {
            // GD3 채널: 하단 수평재 우측, 텍스트 far-right(파이프 콜아웃과 이격)
            anchorFx = this.GetEnvDouble("PFS_NOTAB_GD3_ANCHOR_FX1", 0.8, 0.0, 1.0);
            anchor = new Point3d(minX + width * anchorFx, minY, 0.0);
            elbow = new Point3d(maxX + offset, minY - offset * 2.0, 0.0);
            leftHalf = false;
          }
          else if (multiDesignation)
          {
            anchor = new Point3d(minX + width * fx, minY, 0.0);
            elbow = new Point3d(anchor.X, minY - offset, 0.0);
            leftHalf = anchor.X < centerX;
          }
          else
          {
            double stackDy = -(double)i * (txt * 1.8);
            anchor = singleAnchor;
            elbow = new Point3d(maxX + offset * 3.0, minY + barPaperH * 0.15 + stackDy, 0.0);
            leftHalf = false;
          }
          Point3d textPoint = leftHalf
            ? new Point3d(elbow.X - gap, elbow.Y, 0.0)
            : new Point3d(elbow.X + gap, elbow.Y, 0.0);
          // anchor(부재 지정점)는 노브 미적용으로 고정, elbow/text만 idx별 노브로 이동
          elbow = new Point3d(elbow.X + mdxI, elbow.Y + mdyI, 0.0);
          textPoint = new Point3d(textPoint.X + mdxI, textPoint.Y + mdyI, 0.0);
          string sideSource;
          NotabCalloutPlacer.RequiredSide requiredSide = this.ResolveNotabRequiredSide("PFS_NOTAB_DIR_" + notabTypePrefix + "_M" + i, anchor.X, (viewportPaperExt.MinPoint.X + viewportPaperExt.MaxPoint.X) / 2.0, out sideSource);
          // cycle83: MLeader 경로는 접속점을 확정할 수 없어 직접 MText+Leader 작도로 교체했다.
          #if false
          MText content = new MText();
          content.Contents = designation;
          content.TextHeight = txt;
          content.Attachment = leftHalf ? AttachmentPoint.MiddleRight : AttachmentPoint.MiddleLeft;
          content.Location = textPoint;
          if (textStyleId != ObjectId.Null)
            content.TextStyleId = textStyleId;

          MLeader leader = PSUtil.CreateMLeader(new Point3d[] { anchor, elbow, textPoint }, content, mlStyleId, Matrix3d.Identity);
          if (leader == null)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL callout skip: CreateMLeader null idx=" + i + " designation=" + designation);
            continue;
          }

          try
          {
            leader.TextLocation = textPoint;
            leader.TextAlignmentType = (TextAlignmentType)0;
            MText placed = leader.MText;
            if (placed != null)
            {
              placed.Attachment = leftHalf ? AttachmentPoint.MiddleRight : AttachmentPoint.MiddleLeft;
              placed.Location = textPoint;
              leader.MText = placed;
            }
            this.ApplyNotabCalloutNearEdgeAttachment(leader, gap, "callout", textPoint, leftHalf ? "RightLeader" : "LeftLeader");
          }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL callout text placement 예외: " + ex.GetType().Name + ": " + ex.Message + " idx=" + i + " text=" + textPoint);
          }

          if (layerId != ObjectId.Null)
            leader.LayerId = layerId;
          layoutBtr.AppendEntity(leader);
          tr.AddNewlyCreatedDBObject(leader, true);
          try
          {
            double textMinDx = this.GetEnvDouble("PFS_NOTAB_CALLOUT_MIN_DX", 40.0, 0.0, 500.0);
            MText mtFix = leader.MText;
            if (mtFix != null)
            {
              double aw = mtFix.ActualWidth, ah = mtFix.ActualHeight;
              if (aw > 1e-6)
              {
                Point3d loc = mtFix.Location;
                string att = mtFix.Attachment.ToString();
                bool extRight = att.EndsWith("Left", System.StringComparison.Ordinal);
                double curLeft = extRight ? loc.X : loc.X - aw;
                double curRight = extRight ? loc.X + aw : loc.X;
                bool extDown = att.StartsWith("Top", System.StringComparison.Ordinal);
                double curTop = extDown ? loc.Y : loc.Y + ah / 2.0;
                double curBottom = extDown ? loc.Y - ah : loc.Y - ah / 2.0;
                bool outwardRight = !(textPoint.X < anchor.X);
                double wantLeft, wantRight2;
                if (outwardRight)
                { wantLeft = System.Math.Max(textPoint.X, anchor.X + textMinDx); wantRight2 = wantLeft + aw; }
                else
                { wantRight2 = System.Math.Min(textPoint.X, anchor.X - textMinDx); wantLeft = wantRight2 - aw; }
                double curCenterY = (curTop + curBottom) / 2.0;
                double dx = outwardRight ? wantLeft - curLeft : wantRight2 - curRight;
                double dy = textPoint.Y - curCenterY;
                if (System.Math.Abs(dx) > 1e-6 || System.Math.Abs(dy) > 1e-6)
                {
                  leader.TextLocation = new Point3d(loc.X + dx, loc.Y + dy, 0.0);
                  PlantOrthoView.FileDiag("PFSNOTABDETAIL text-fit att=" + att + " aw=" + this.FormatNumber(aw) + " ah=" + this.FormatNumber(ah)
                    + " cur=[" + this.FormatNumber(curLeft) + "," + this.FormatNumber(curRight) + "]x[" + this.FormatNumber(curBottom) + "," + this.FormatNumber(curTop) + "]"
                    + " want=[" + this.FormatNumber(wantLeft) + "," + this.FormatNumber(wantRight2) + "]"
                    + " d=(" + this.FormatNumber(dx) + "," + this.FormatNumber(dy) + ")"
                    + " anchorX=" + this.FormatNumber(anchor.X) + " sep=" + this.FormatNumber(outwardRight ? wantLeft - anchor.X : anchor.X - wantRight2));
                }
              }
            }
          }
          catch (System.Exception sx)
          { PlantOrthoView.FileDiag("PFSNOTABDETAIL text-fit 예외: " + sx.GetType().Name + ": " + sx.Message); }
          try
          {
            MText mt = leader.MText;
            if (mt != null)
            {
              PlantOrthoView.FileDiag("PFSNOTABDETAIL render-check designation=" + designation
                + " calcTextX=" + this.FormatNumber(textPoint.X) + " calcSide=" + (textPoint.X < anchor.X ? "left" : "right")
                + " mtLoc=(" + this.FormatNumber(mt.Location.X) + "," + this.FormatNumber(mt.Location.Y) + ")"
                + " mtActualW=" + this.FormatNumber(mt.ActualWidth) + " mtActualH=" + this.FormatNumber(mt.ActualHeight) + " att=" + mt.Attachment.ToString()
                + " anchorX=" + this.FormatNumber(anchor.X));
            }
          }
          catch (System.Exception rex)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL render-check 예외: " + rex.GetType().Name);
          }
          if (smartPlaced)
            placer.Commit(anchor, elbow, textPoint, System.Math.Max(1.0, designation.Length * txt * 0.7), System.Math.Max(1.0, txt * 1.4));
          #endif
          this.AppendNotabDirectCallout(tr, layoutBtr, db, textStyleId, layerId, supportPaperExt, txt, placer, designation, anchor, (viewportPaperExt.MinPoint.X + viewportPaperExt.MaxPoint.X) / 2.0, requiredSide, sideSource, gap, minDx, "callout");
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL callout append 예외: " + ex.GetType().Name + ": " + ex.Message + " designation=" + (designations.Count > 0 ? designations[0] : s_isoSupportDesignation));
      }
    }

    private void AppendNotabUboltCallouts(Transaction tr, BlockTableRecord layoutBtr, Database db, ObjectId textStyleId, ObjectId layerId, Extents3d supportPaperExt, Extents3d viewportPaperExt, double txt, NotabCalloutPlacer placer, Viewport vp, System.Collections.Generic.List<NotabMemberBox> memberBoxes)
    {
      if (tr == null || layoutBtr == null || db == null || vp == null || s_isoUbolts.Count == 0)
        return;
      try
      {
        double arr = this.GetEnvDouble("PFS_NOTAB_DIM_ARR", 10.0, 0.5, 50.0);
        double gap = arr + txt * 0.6;
        double minDx = this.GetEnvDouble("PFS_NOTAB_UBOLT_MIN_DX", 10.0, 0.0, 500.0);
        double viewportCenterX = (viewportPaperExt.MinPoint.X + viewportPaperExt.MaxPoint.X) / 2.0;
        for (int i = 0; i < s_isoUbolts.Count; i++)
        {
          NotabUboltSnapshot snapshot = s_isoUbolts[i];
          if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.Tag))
            continue;
          Point3d anchor = this.NotabProjectWcsToPaper(vp, snapshot.WcsCenter);
          string sideSource;
          NotabCalloutPlacer.RequiredSide requiredSide = this.ResolveNotabRequiredSide("PFS_NOTAB_DIR_UBOLT_" + i, anchor.X, viewportCenterX, out sideSource);
          // 유볼트는 원형이라 중심을 찍으면 화살표가 안쪽에 박힌다. 실제 솔리드 박스를 찾아
          // 그 외곽(quadrant)에 닿게 한다.
          bool hasBox = false;
          Extents3d uboltBox = new Extents3d(Point3d.Origin, Point3d.Origin);
          // 유볼트는 세로기둥·가로재 박스 안에 완전히 들어앉는다. "첫 번째 포함 박스"를 쓰면
          // 큰 부재 박스가 잡혀 화살표가 엉뚱한 곳을 찍는다(cycle97 로그 확정: ubolt-box 없음 0건).
          // 포함하는 박스 중 최소 면적을 고르고, 서포트 대비 과대한 박스는 유볼트가 아니라고 본다.
          double supportW = System.Math.Max(1e-9, supportPaperExt.MaxPoint.X - supportPaperExt.MinPoint.X);
          double supportH = System.Math.Max(1e-9, supportPaperExt.MaxPoint.Y - supportPaperExt.MinPoint.Y);
          double maxRatio = this.GetEnvDouble("PFS_NOTAB_UBOLT_BOX_MAX_RATIO", 0.6, 0.05, 1.0);
          double bestArea = double.MaxValue;
          int rejectedOversize = 0;
          if (memberBoxes != null)
          {
            for (int m = 0; m < memberBoxes.Count; m++)
            {
              Extents3d pb = memberBoxes[m].PaperBox;
              if (anchor.X < pb.MinPoint.X || anchor.X > pb.MaxPoint.X
                || anchor.Y < pb.MinPoint.Y || anchor.Y > pb.MaxPoint.Y)
                continue;
              double bw = pb.MaxPoint.X - pb.MinPoint.X;
              double bh = pb.MaxPoint.Y - pb.MinPoint.Y;
              if (bw / supportW > maxRatio || bh / supportH > maxRatio)
              { rejectedOversize++; continue; }
              double area = System.Math.Max(0.0, bw) * System.Math.Max(0.0, bh);
              if (area < bestArea)
              { bestArea = area; uboltBox = pb; hasBox = true; }
            }
          }
          if (hasBox)
            PlantOrthoView.FileDiag("PFSNOTABDETAIL ubolt-box 채택 tag=" + snapshot.Tag
              + " W=" + this.FormatNumber(uboltBox.MaxPoint.X - uboltBox.MinPoint.X)
              + " H=" + this.FormatNumber(uboltBox.MaxPoint.Y - uboltBox.MinPoint.Y)
              + " oversizeRejected=" + rejectedOversize + " anchor=" + this.FormatPoint(anchor));
          else
            PlantOrthoView.FileDiag("PFSNOTABDETAIL ubolt-box 없음 tag=" + snapshot.Tag
              + " oversizeRejected=" + rejectedOversize + " anchor=" + this.FormatPoint(anchor));
          this.AppendNotabDirectCallout(tr, layoutBtr, db, textStyleId, layerId, supportPaperExt, txt, placer, snapshot.Tag, anchor, viewportCenterX, requiredSide, sideSource, gap, minDx, "ubolt callout", hasBox, uboltBox,
            false, Point3d.Origin, new Extents3d(Point3d.Origin, Point3d.Origin));
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL ubolt callout append 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private void AppendNotabPipeCallout(Transaction tr, BlockTableRecord layoutBtr, Database db, ObjectId textStyleId, ObjectId layerId, double pipeCenterXPaper, double pipeCenterYPaper, Extents3d supportPaperExt, Extents3d viewportPaperExt, double txt, NotabCalloutPlacer placer)
    {
      if (tr == null || layoutBtr == null || db == null)
        return;

      string pln = string.IsNullOrWhiteSpace(s_isoPipeLineNo) ? string.Empty : s_isoPipeLineNo.Trim();
      string bop = string.IsNullOrWhiteSpace(s_isoBOP) ? string.Empty : s_isoBOP.Trim();
      if (string.IsNullOrWhiteSpace(pln) && string.IsNullOrWhiteSpace(bop))
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL pipe callout skip: PLN/BOP empty");
        return;
      }

      try
      {
        double minX = supportPaperExt.MinPoint.X;
        double maxX = supportPaperExt.MaxPoint.X;
        double minY = supportPaperExt.MinPoint.Y;
        double maxY = supportPaperExt.MaxPoint.Y;
        string standardName = this.GetNotabStandardName();
        string pipeSide = this.GetNotabPipeCalloutSide(standardName);
        double offset = this.GetEnvDouble("PFS_NOTAB_DIM_OFFSET", 30.0, 1.0, 100.0);
        double arr = this.GetEnvDouble("PFS_NOTAB_DIM_ARR", 10.0, 0.5, 50.0);
        double gap = arr + txt * 0.6;
        double minDx = this.GetEnvDouble("PFS_NOTAB_CALLOUT_MIN_DX", 40.0, 0.0, 500.0);
        double pdx = this.GetEnvDouble("PFS_NOTAB_PIPE_CALLOUT_DX", 0.0, -2000.0, 2000.0);
        double pdy = this.GetEnvDouble("PFS_NOTAB_PIPE_CALLOUT_DY", 0.0, -2000.0, 2000.0);
        Point3d anchor = (!double.IsNaN(pipeCenterXPaper) && !double.IsNaN(pipeCenterYPaper))
          ? new Point3d(pipeCenterXPaper, pipeCenterYPaper, 0.0)
          : new Point3d((minX + maxX) / 2.0, maxY, 0.0);
        double elbowX = double.IsNaN(pipeCenterXPaper) ? anchor.X : pipeCenterXPaper;
        double elbowY = string.Equals(pipeSide, "bottom", System.StringComparison.OrdinalIgnoreCase) ? minY - offset : maxY + offset;
        Point3d elbow = new Point3d(elbowX, elbowY, 0.0);
        Point3d textPoint = new Point3d(elbow.X + gap, elbow.Y, 0.0);
        elbow = new Point3d(elbow.X + pdx, elbow.Y + pdy, 0.0);
        textPoint = new Point3d(textPoint.X + pdx, textPoint.Y + pdy, 0.0);
        string contents = pln;
        if (!string.IsNullOrWhiteSpace(bop))
          contents = string.IsNullOrWhiteSpace(contents) ? "B.O.P +" + bop : contents + "\\P" + "B.O.P +" + bop;

        int longestLine = 0;
        string[] lines = contents.Split(new string[] { "\\P" }, System.StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
          if (lines[i].Length > longestLine) longestLine = lines[i].Length;
        string sideSource;
        NotabCalloutPlacer.RequiredSide requiredSide = this.ResolveNotabRequiredSide("PFS_NOTAB_DIR_" + this.GetSupportTypePrefix(s_isoSupportTag) + "_PIPE", anchor.X, (viewportPaperExt.MinPoint.X + viewportPaperExt.MaxPoint.X) / 2.0, out sideSource);
        double manualDx, manualDy;
        bool hasManualPosition = this.TryGetNotabPipePosition(standardName, out manualDx, out manualDy);
        Point3d manualTextEdge = hasManualPosition ? new Point3d(anchor.X + manualDx, anchor.Y + manualDy, 0.0) : Point3d.Origin;
        if (hasManualPosition)
        {
          requiredSide = manualDx < 0.0 ? NotabCalloutPlacer.RequiredSide.Left : NotabCalloutPlacer.RequiredSide.Right;
          sideSource = "env";
        }

        // cycle83: MLeader 경로는 접속점을 확정할 수 없어 직접 MText+Leader 작도로 교체했다.
        #if false
        MText content = new MText();
        content.Contents = contents;
        content.TextHeight = txt;
        content.Attachment = textLeftOfAnchor ? AttachmentPoint.MiddleRight : AttachmentPoint.MiddleLeft;
        content.Location = textPoint;
        if (textStyleId != ObjectId.Null)
          content.TextStyleId = textStyleId;

        MLeader leader = PSUtil.CreateMLeader(new Point3d[] { anchor, elbow, textPoint }, content, mlStyleId, Matrix3d.Identity);
        if (leader == null)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL pipe callout skip: CreateMLeader null PLN=" + (string.IsNullOrWhiteSpace(pln) ? "skip" : pln) + " BOP=" + (string.IsNullOrWhiteSpace(bop) ? "skip" : bop));
          return;
        }

        try
        {
          leader.TextLocation = textPoint;
          leader.TextAlignmentType = (TextAlignmentType)0;
          MText placed = leader.MText;
          if (placed != null)
          {
            placed.Attachment = textLeftOfAnchor ? AttachmentPoint.MiddleRight : AttachmentPoint.MiddleLeft;
            placed.Location = textPoint;
            leader.MText = placed;
          }
          this.ApplyNotabCalloutNearEdgeAttachment(leader, gap, "pipe callout", textPoint, textLeftOfAnchor ? "RightLeader" : "LeftLeader");
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL pipe callout text placement 예외: " + ex.GetType().Name + ": " + ex.Message + " text=" + textPoint);
        }

        if (layerId != ObjectId.Null)
          leader.LayerId = layerId;
        layoutBtr.AppendEntity(leader);
        tr.AddNewlyCreatedDBObject(leader, true);
        try
        {
          double textMinDx = this.GetEnvDouble("PFS_NOTAB_CALLOUT_MIN_DX", 40.0, 0.0, 500.0);
          MText mtFix = leader.MText;
          if (mtFix != null)
          {
            double aw = mtFix.ActualWidth, ah = mtFix.ActualHeight;
            if (aw > 1e-6)
            {
              Point3d loc = mtFix.Location;
              string att = mtFix.Attachment.ToString();
              bool extRight = att.EndsWith("Left", System.StringComparison.Ordinal);
              double curLeft = extRight ? loc.X : loc.X - aw;
              double curRight = extRight ? loc.X + aw : loc.X;
              bool extDown = att.StartsWith("Top", System.StringComparison.Ordinal);
              double curTop = extDown ? loc.Y : loc.Y + ah / 2.0;
              double curBottom = extDown ? loc.Y - ah : loc.Y - ah / 2.0;
              bool outwardRight = !(textPoint.X < anchor.X);
              double wantLeft, wantRight2;
              if (outwardRight)
              { wantLeft = System.Math.Max(textPoint.X, anchor.X + textMinDx); wantRight2 = wantLeft + aw; }
              else
              { wantRight2 = System.Math.Min(textPoint.X, anchor.X - textMinDx); wantLeft = wantRight2 - aw; }
              double curCenterY = (curTop + curBottom) / 2.0;
              double dx = outwardRight ? wantLeft - curLeft : wantRight2 - curRight;
              double dy = textPoint.Y - curCenterY;
              if (System.Math.Abs(dx) > 1e-6 || System.Math.Abs(dy) > 1e-6)
              {
                leader.TextLocation = new Point3d(loc.X + dx, loc.Y + dy, 0.0);
                PlantOrthoView.FileDiag("PFSNOTABDETAIL text-fit att=" + att + " aw=" + this.FormatNumber(aw) + " ah=" + this.FormatNumber(ah)
                  + " cur=[" + this.FormatNumber(curLeft) + "," + this.FormatNumber(curRight) + "]x[" + this.FormatNumber(curBottom) + "," + this.FormatNumber(curTop) + "]"
                  + " want=[" + this.FormatNumber(wantLeft) + "," + this.FormatNumber(wantRight2) + "]"
                  + " d=(" + this.FormatNumber(dx) + "," + this.FormatNumber(dy) + ")"
                  + " anchorX=" + this.FormatNumber(anchor.X) + " sep=" + this.FormatNumber(outwardRight ? wantLeft - anchor.X : anchor.X - wantRight2));
              }
            }
          }
        }
        catch (System.Exception sx)
        { PlantOrthoView.FileDiag("PFSNOTABDETAIL text-fit 예외: " + sx.GetType().Name + ": " + sx.Message); }
        try
        {
          MText mt = leader.MText;
          if (mt != null)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL render-check PLN=" + (string.IsNullOrWhiteSpace(pln) ? "skip" : pln)
              + " calcTextX=" + this.FormatNumber(textPoint.X) + " calcSide=" + (textPoint.X < anchor.X ? "left" : "right")
              + " mtLoc=(" + this.FormatNumber(mt.Location.X) + "," + this.FormatNumber(mt.Location.Y) + ")"
              + " mtActualW=" + this.FormatNumber(mt.ActualWidth) + " mtActualH=" + this.FormatNumber(mt.ActualHeight) + " att=" + mt.Attachment.ToString()
              + " anchorX=" + this.FormatNumber(anchor.X));
          }
        }
        catch (System.Exception rex)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL render-check 예외: " + rex.GetType().Name);
        }
        if (smartPlaced)
          placer.Commit(anchor, elbow, textPoint, textWidth, textHeight);
        #endif
        this.AppendNotabDirectCallout(tr, layoutBtr, db, textStyleId, layerId, supportPaperExt, txt, placer, contents, anchor, (viewportPaperExt.MinPoint.X + viewportPaperExt.MaxPoint.X) / 2.0, requiredSide, sideSource, gap, minDx, "pipe callout",
          false, new Extents3d(Point3d.Origin, Point3d.Origin), hasManualPosition, manualTextEdge, viewportPaperExt);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL pipe callout append 예외: " + ex.GetType().Name + ": " + ex.Message + " PLN=" + (string.IsNullOrWhiteSpace(pln) ? "skip" : pln) + " BOP=" + (string.IsNullOrWhiteSpace(bop) ? "skip" : bop));
      }
    }

    private void ApplyNotabCalloutNearEdgeAttachment(MLeader leader, double gap, string label, Point3d textPoint, string leaderDir)
    {
      if (leader == null)
        return;

      try
      {
        double dogleg = Math.Max(1.0, Math.Min(5.0, gap * 0.3));
        leader.EnableDogleg = true;
        leader.DoglegLength = dogleg;

        Type leaderType = leader.GetType();
        Type directionType = leaderType.Assembly.GetType("Autodesk.AutoCAD.DatabaseServices.LeaderDirectionType");
        bool attachDirHorizontal = this.TrySetNotabTextAttachmentDirection(leader, "leader " + label, textPoint);
        string directionName = string.Equals(leaderDir, "RightLeader", System.StringComparison.Ordinal) ? "RightLeader" : "LeftLeader";
        TextAttachmentType attachType = (TextAttachmentType)6;
        if (directionType != null)
        {
          System.Reflection.MethodInfo method = leaderType.GetMethod(
            "SetTextAttachmentType",
            new Type[] { typeof(TextAttachmentType), directionType });
          if (method != null)
          {
            object leftLeader = Enum.Parse(directionType, "LeftLeader");
            object rightLeader = Enum.Parse(directionType, "RightLeader");
            method.Invoke(leader, new object[] { attachType, leftLeader });
            method.Invoke(leader, new object[] { attachType, rightLeader });
          }
          else
            PlantOrthoView.FileDiag("PFSNOTABDETAIL " + label + " near-edge attachment skip: SetTextAttachmentType overload missing text=" + textPoint);
        }
        else
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL " + label + " near-edge attachment skip: LeaderDirectionType missing text=" + textPoint);
        }

        PlantOrthoView.FileDiag("PFSNOTABDETAIL " + label + " near-edge attachment attachDir=" + (attachDirHorizontal ? "Horizontal" : "skip") + " attachType=" + ((int)attachType).ToString(System.Globalization.CultureInfo.InvariantCulture) + " dir=" + directionName + " dogleg=" + this.FormatNumber(dogleg) + " text=" + textPoint);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL " + label + " near-edge attachment 예외: " + ex.GetType().Name + ": " + ex.Message + " text=" + textPoint);
      }
    }

    private bool TrySetNotabTextAttachmentDirection(object target, string label, Point3d textPoint)
    {
      if (target == null)
        return false;

      try
      {
        Type targetType = target.GetType();
        Type directionType = targetType.Assembly.GetType("Autodesk.AutoCAD.DatabaseServices.TextAttachmentDirection");
        if (directionType == null)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL " + label + " attachDir skip: TextAttachmentDirection missing text=" + textPoint);
          return false;
        }

        System.Reflection.PropertyInfo property = targetType.GetProperty("TextAttachmentDirection");
        if (property == null || !property.CanWrite)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL " + label + " attachDir skip: TextAttachmentDirection property missing text=" + textPoint);
          return false;
        }

        object horizontal = Enum.Parse(directionType, "AttachmentHorizontal");
        property.SetValue(target, horizontal, null);
        return true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL " + label + " attachDir 예외: " + ex.GetType().Name + ": " + ex.Message + " text=" + textPoint);
        return false;
      }
    }

    private ObjectId EnsureNotabMLeaderStyle(Database db, Transaction tr)
    {
      if (db == null || tr == null)
        return ObjectId.Null;

      try
      {
        DBDictionary dict = tr.GetObject(db.MLeaderStyleDictionaryId, OpenMode.ForRead) as DBDictionary;
        if (dict == null)
          return ObjectId.Null;

        const string styleName = "ML_PIPETAG_85";
        if (dict.Contains(styleName))
        {
          ObjectId existingId = dict.GetAt(styleName);
          try
          {
            MLeaderStyle existingStyle = tr.GetObject(existingId, OpenMode.ForWrite, false) as MLeaderStyle;
            if (existingStyle != null)
              this.TrySetNotabTextAttachmentDirection(existingStyle, "mleader style existing", Point3d.Origin);
          }
          catch (System.Exception sx)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL mleader style existing attachDir 예외: " + sx.GetType().Name + ": " + sx.Message);
          }
          return existingId;
        }

        MLeaderStyle style = new MLeaderStyle();
        style.ArrowSymbolId = ObjectId.Null;
        style.ArrowSize = this.GetEnvDouble("PFS_NOTAB_DIM_ARR", 10.0, 0.5, 50.0);
        style.LeaderLineColor = Autodesk.AutoCAD.Colors.Color.FromColorIndex((Autodesk.AutoCAD.Colors.ColorMethod)195, (short)1);
        style.Scale = 1.0;
        style.ContentType = (ContentType)2;
        style.TextColor = Autodesk.AutoCAD.Colors.Color.FromColorIndex((Autodesk.AutoCAD.Colors.ColorMethod)195, (short)3);
        this.TrySetNotabTextAttachmentDirection(style, "mleader style new", Point3d.Origin);
        style.TextAttachmentType = (TextAttachmentType)6;
        dict.UpgradeOpen();
        ObjectId id = dict.SetAt(styleName, style);
        tr.AddNewlyCreatedDBObject(style, true);
        return id;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL mleader style 예외: " + ex.GetType().Name + ": " + ex.Message);
        return ObjectId.Null;
      }
    }

    private string GetSupportTypePrefix(string supportName)
    {
      if (string.IsNullOrWhiteSpace(supportName))
        return string.Empty;

      try
      {
        string trimmed = supportName.Trim();
        int dash = trimmed.IndexOf('-');
        return dash > 0 ? trimmed.Substring(0, dash) : trimmed;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL support type parse 예외 name=" + supportName + ": " + ex.GetType().Name + ": " + ex.Message);
        return string.Empty;
      }
    }

    private struct NotabTypeConfig
    {
      public string VerticalMode;
      public string VerticalParamKey;
      public string PipeCalloutSide;
      public string HorizontalSide;
      // 가로 부재가 도면 위아래 어디에 있는가. "bottom"(GD 계열, 기본) | "top"(RC 계열).
      // 부재 콜아웃 앵커의 상하 기준면을 결정한다.
      public string MemberAnchorSide;
      public string[] MemberBIs;
    }

    private string GetNotabStandardName()
    {
      try
      {
        string source = string.IsNullOrWhiteSpace(s_isoShortDesc) ? this.GetSupportTypePrefix(s_isoSupportTag) : s_isoShortDesc.Trim();
        if (string.IsNullOrWhiteSpace(source))
          return "unknown";

        string trimmed = source.Trim();
        int length = 0;
        while (length < trimmed.Length && System.Char.IsLetterOrDigit(trimmed[length]))
          length++;

        string token = length > 0 ? trimmed.Substring(0, length) : trimmed;
        return string.IsNullOrWhiteSpace(token) ? "unknown" : token;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL standard name parse 예외 shortDesc=" + (string.IsNullOrWhiteSpace(s_isoShortDesc) ? "empty" : s_isoShortDesc) + " supportTag=" + (string.IsNullOrWhiteSpace(s_isoSupportTag) ? "empty" : s_isoSupportTag) + ": " + ex.GetType().Name + ": " + ex.Message);
        return "unknown";
      }
    }

    private NotabTypeConfig GetNotabTypeConfig(string standardName)
    {
      // 全타입 실측하며 여기 행만 추가/수정. 향후 외부 설정화 가능.
      if (string.Equals(standardName, "GD1", System.StringComparison.OrdinalIgnoreCase))
        return new NotabTypeConfig { VerticalMode = "fheight", PipeCalloutSide = "top", HorizontalSide = "bottom" };
      if (string.Equals(standardName, "RC1", System.StringComparison.OrdinalIgnoreCase))
        return new NotabTypeConfig { VerticalMode = "param", VerticalParamKey = "F2", PipeCalloutSide = "top", HorizontalSide = "bottom", MemberAnchorSide = "vertical" };
      if (string.Equals(standardName, "RC2", System.StringComparison.OrdinalIgnoreCase))
        return new NotabTypeConfig { VerticalMode = "param", VerticalParamKey = "F2", PipeCalloutSide = "top", HorizontalSide = "auto", MemberAnchorSide = "vertical" };
      if (string.Equals(standardName, "RC3", System.StringComparison.OrdinalIgnoreCase))
        return new NotabTypeConfig { VerticalMode = "param", VerticalParamKey = "F2", PipeCalloutSide = "top", HorizontalSide = "auto", MemberAnchorSide = "vertical" };
      if (string.Equals(standardName, "RC4", System.StringComparison.OrdinalIgnoreCase))
        return new NotabTypeConfig { VerticalMode = "param", VerticalParamKey = "F2", PipeCalloutSide = "top", HorizontalSide = "auto", MemberAnchorSide = "vertical" };
      if (string.Equals(standardName, "RC5", System.StringComparison.OrdinalIgnoreCase))
        return new NotabTypeConfig { VerticalMode = "param", VerticalParamKey = "F2", PipeCalloutSide = "top", HorizontalSide = "auto", MemberAnchorSide = "vertical" };
      if (string.Equals(standardName, "RC6", System.StringComparison.OrdinalIgnoreCase))
        return new NotabTypeConfig { VerticalMode = "param", VerticalParamKey = "F2", PipeCalloutSide = "top", HorizontalSide = "auto", MemberAnchorSide = "vertical" };
      if (string.Equals(standardName, "RC7", System.StringComparison.OrdinalIgnoreCase))
        return new NotabTypeConfig { VerticalMode = "none", PipeCalloutSide = "top", HorizontalSide = "auto" };
      if (string.Equals(standardName, "RC8", System.StringComparison.OrdinalIgnoreCase))
        return new NotabTypeConfig { VerticalMode = "none", PipeCalloutSide = "top", HorizontalSide = "auto" };
      if (string.Equals(standardName, "RC9", System.StringComparison.OrdinalIgnoreCase))
        return new NotabTypeConfig { VerticalMode = "param", VerticalParamKey = "F2", PipeCalloutSide = "top", HorizontalSide = "auto", MemberAnchorSide = "vertical" };
      if (string.Equals(standardName, "GD2", System.StringComparison.OrdinalIgnoreCase))
        return new NotabTypeConfig { VerticalMode = "pipecenter", PipeCalloutSide = "top", HorizontalSide = "auto", MemberBIs = new string[] { "16", "215" } };
      if (string.Equals(standardName, "GD3", System.StringComparison.OrdinalIgnoreCase))
        return new NotabTypeConfig { VerticalMode = "full", PipeCalloutSide = "bottom", HorizontalSide = "auto" };

      return new NotabTypeConfig { VerticalMode = "fheight", PipeCalloutSide = "top", HorizontalSide = "auto" };
    }

    private string GetNotabHorizontalDimSide(string supportType)
    {
      return this.GetNotabTypeConfig(supportType).HorizontalSide;
    }

    private string GetNotabVerticalDimMode(string supportType)
    {
      return this.GetNotabTypeConfig(supportType).VerticalMode;
    }

    // 부재 콜아웃 앵커 기준. "bottom"(GD 계열, 기본) | "top" | "vertical"(RC 계열=세로 MEMBER 지시).
    private string GetNotabMemberAnchorSide(string supportType)
    {
      string side = this.GetNotabTypeConfig(supportType).MemberAnchorSide;
      return string.IsNullOrWhiteSpace(side) ? "bottom" : side;
    }

    private string GetNotabVerticalParamKey(string supportType)
    {
      return this.GetNotabTypeConfig(supportType).VerticalParamKey;
    }

    private string GetNotabPipeCalloutSide(string supportType)
    {
      return this.GetNotabTypeConfig(supportType).PipeCalloutSide;
    }

    private void GetNotabViewBasis(out Vector3d viewDir, out Vector3d up, out Vector3d right)
    {
      viewDir = s_isoPipeAxisValid && s_isoPipeAxis.Length > 1e-6 ? s_isoPipeAxis.GetNormal() : Vector3d.XAxis;
      up = s_isoPipeUp.Length > 1e-6 ? s_isoPipeUp.GetNormal() : Vector3d.ZAxis;
      up = up - viewDir.MultiplyBy(up.DotProduct(viewDir));
      if (up.Length <= 1e-6)
        up = Vector3d.ZAxis - viewDir.MultiplyBy(Vector3d.ZAxis.DotProduct(viewDir));
      if (up.Length <= 1e-6)
        up = Vector3d.YAxis - viewDir.MultiplyBy(Vector3d.YAxis.DotProduct(viewDir));
      if (up.Length <= 1e-6)
        up = Vector3d.ZAxis;
      up = up.GetNormal();

      right = up.CrossProduct(viewDir);
      if (right.Length <= 1e-6)
        right = Vector3d.XAxis;
      else
        right = right.GetNormal();

      up = viewDir.CrossProduct(right);
      if (up.Length > 1e-6)
        up = up.GetNormal();
    }

    private double GetEnvDouble(string name, double fallback, double minValue, double maxValue)
    {
      try
      {
        string raw = System.Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(raw))
          return fallback;

        double parsed;
        if (double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed) && parsed >= minValue && parsed <= maxValue)
          return parsed;

        PlantOrthoView.FileDiag("PFSNOTABDETAIL env 무시 " + name + "=" + raw + " range=" + this.FormatNumber(minValue) + "~" + this.FormatNumber(maxValue));
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL env 읽기 예외 " + name + ": " + ex.GetType().Name + ": " + ex.Message);
      }

      return fallback;
    }

    // 형식: dx,dy. 파이프 앵커 기준 paper 단위이며, 유효하지 않으면 자동배치로 폴백한다.
    private bool TryGetNotabPipePosition(string standardName, out double dx, out double dy)
    {
      dx = 0.0;
      dy = 0.0;
      string type = string.IsNullOrWhiteSpace(standardName) ? string.Empty : standardName.Trim().ToUpperInvariant();
      if (string.IsNullOrEmpty(type))
        return false;
      string name = "PFS_NOTAB_PIPE_POS_" + type;
      try
      {
        string raw = System.Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(raw))
          return false;
        string[] parts = raw.Split(',');
        if (parts.Length == 2
          && double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out dx)
          && double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out dy)
          && dx >= -2000.0 && dx <= 2000.0 && dy >= -2000.0 && dy <= 2000.0)
          return true;
        PlantOrthoView.FileDiag("PFSNOTABDETAIL env 무시 " + name + "=" + raw + " format=dx,dy range=-2000~2000");
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL env 읽기 예외 " + name + ": " + ex.GetType().Name + ": " + ex.Message);
      }
      dx = 0.0;
      dy = 0.0;
      return false;
    }

    private bool TryApplyHiddenVisualStyle(Database db, Transaction tr, Viewport vp)
    {
      try
      {
        DBDictionary vsDict = tr.GetObject(db.VisualStyleDictionaryId, OpenMode.ForRead) as DBDictionary;
        if (vsDict == null)
          return false;

        ObjectId fallbackHidden = ObjectId.Null;
        string fallbackHiddenName = null;
        ObjectId fallbackAnyHidden = ObjectId.Null;
        string fallbackAnyHiddenName = null;
        foreach (DBDictionaryEntry entry in vsDict)
        {
          string name = System.Convert.ToString(entry.Key, System.Globalization.CultureInfo.InvariantCulture);
          if (string.Equals(name, "Hidden", System.StringComparison.OrdinalIgnoreCase))
          {
            vp.VisualStyleId = entry.Value;
            PlantOrthoView.FileDiag("PFSNOTABDETAIL Hidden visualStyle=" + name + " id=" + entry.Value + " match=exact");
            return true;
          }

          if (name.IndexOf("Hidden", System.StringComparison.OrdinalIgnoreCase) < 0)
            continue;

          if (fallbackAnyHidden == ObjectId.Null)
          {
            fallbackAnyHidden = entry.Value;
            fallbackAnyHiddenName = name;
          }

          if (name.IndexOf("3D", System.StringComparison.OrdinalIgnoreCase) < 0 && fallbackHidden == ObjectId.Null)
          {
            fallbackHidden = entry.Value;
            fallbackHiddenName = name;
          }
        }

        if (fallbackHidden != ObjectId.Null)
        {
          vp.VisualStyleId = fallbackHidden;
          PlantOrthoView.FileDiag("PFSNOTABDETAIL Hidden visualStyle=" + fallbackHiddenName + " id=" + fallbackHidden + " match=fallback-no3d");
          return true;
        }

        if (fallbackAnyHidden != ObjectId.Null)
        {
          vp.VisualStyleId = fallbackAnyHidden;
          PlantOrthoView.FileDiag("PFSNOTABDETAIL Hidden visualStyle=" + fallbackAnyHiddenName + " id=" + fallbackAnyHidden + " match=fallback-any");
          return true;
        }

        PlantOrthoView.FileDiag("PFSNOTABDETAIL Hidden visualStyle not found");
        return false;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL Hidden style 예외: " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
    }

    private bool TrySetViewportShadePlotHidden(Viewport vp)
    {
      if (vp == null)
        return false;

      try
      {
        System.Reflection.PropertyInfo prop = vp.GetType().GetProperty("ShadePlot");
        if (prop == null || !prop.CanWrite)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL ShadePlot property 없음");
          return false;
        }

        object value = null;
        if (prop.PropertyType.IsEnum)
        {
          foreach (string name in System.Enum.GetNames(prop.PropertyType))
          {
            if (name.IndexOf("Hidden", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
              value = System.Enum.Parse(prop.PropertyType, name);
              break;
            }
          }
        }

        if (value == null)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL ShadePlot Hidden enum 없음 type=" + prop.PropertyType.FullName);
          return false;
        }

        prop.SetValue(vp, value, null);
        return true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL ShadePlot 예외: " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
    }

    private bool TrySetTagDatabaseFacetres(Database db, double value)
    {
      if (db == null)
        return false;

      try
      {
        System.Reflection.PropertyInfo prop = db.GetType().GetProperty("Facetres");
        if (prop == null || !prop.CanWrite)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL Facetres property 없음");
          return false;
        }

        object converted = System.Convert.ChangeType(value, prop.PropertyType, System.Globalization.CultureInfo.InvariantCulture);
        prop.SetValue(db, converted, null);
        PlantOrthoView.FileDiag("PFSNOTABDETAIL Facetres=" + this.FormatNumber(value));
        return true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL Facetres 예외: " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
    }

    private string SanitizeIsoFileName(string value)
    {
      string name = string.IsNullOrWhiteSpace(value) ? "SUPPORT_" + System.DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture) : value.Trim();
      char[] invalid = System.IO.Path.GetInvalidFileNameChars();
      foreach (char c in invalid)
        name = name.Replace(c, '_');
      return name;
    }

    private string GetIsoDetailsDirectory()
    {
      string projectDir = string.Empty;
      try
      {
        Autodesk.ProcessPower.ProjectManager.Project piping = Autodesk.ProcessPower.PlantInstance.PlantApplication.CurrentProject.ProjectParts["Piping"] as Autodesk.ProcessPower.ProjectManager.Project;
        if (piping != null)
          projectDir = piping.ProjectDwgDirectory;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg ProjectDwgDirectory 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      if (string.IsNullOrWhiteSpace(projectDir))
        return null;

      return System.IO.Path.Combine(projectDir, "Details");
    }

    private bool CreateIsoDetailDrawing(ObjectIdCollection exportIds, Database sideDb, out string savedPath)
    {
      savedPath = null;
      if (exportIds == null || exportIds.Count == 0 || sideDb == null)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg skip: exportIds/sideDb invalid");
        return false;
      }

      string templatePath = this.ResolveIsoTemplatePath();
      if (string.IsNullOrWhiteSpace(templatePath) || !System.IO.File.Exists(templatePath))
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg template 없음 path=" + (templatePath ?? "null"));
        return false;
      }

      string tag = s_isoSupportTag;
      if (string.IsNullOrWhiteSpace(tag) && !string.IsNullOrWhiteSpace(s_isoShortDesc))
        tag = s_isoShortDesc + "_" + System.DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
      if (string.IsNullOrWhiteSpace(tag))
        tag = "SUPPORT_" + System.DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
      string safeTag = this.SanitizeIsoFileName(tag);

      Database tagDb = null;
      Database oldWorking = HostApplicationServices.WorkingDatabase;
      int cloned = 0;
      bool viewportOk = false;
      bool titleblockOk = false;
      try
      {
        tagDb = new Database(false, true);
        tagDb.ReadDwgFile(templatePath, System.IO.FileShare.Read, true, null);
        HostApplicationServices.WorkingDatabase = tagDb;

        using (Transaction tr = tagDb.TransactionManager.StartTransaction())
        {
          BlockTable bt = tr.GetObject(tagDb.BlockTableId, OpenMode.ForRead) as BlockTable;
          if (bt == null || !bt.Has(BlockTableRecord.ModelSpace))
          {
            PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg skip: template ModelSpace 없음 path=" + templatePath);
            tr.Commit();
            return false;
          }

          ObjectId msId = bt[BlockTableRecord.ModelSpace];
          BlockTableRecord ms = tr.GetObject(msId, OpenMode.ForWrite) as BlockTableRecord;
          ObjectId detailLayerId = this.EnsureIsoDetailLayer(tagDb, tr);
          ObjectId annotationLayerId;
          ObjectId textStyleId;
          ObjectId dimStyleId;
          this.EnsureIsoAnnotationResources(tagDb, tr, out annotationLayerId, out textStyleId, out dimStyleId);

          IdMapping idMap = new IdMapping();
          sideDb.WblockCloneObjects(exportIds, msId, idMap, DuplicateRecordCloning.Ignore, false);

          ObjectId detailBlockRefId = ObjectId.Null;
          ObjectId detailEntityId = ObjectId.Null;
          foreach (IdPair pair in idMap)
          {
            if (!pair.IsPrimary || !pair.IsCloned)
              continue;

            Entity e = tr.GetObject(pair.Value, OpenMode.ForWrite, false) as Entity;
            if (e == null)
              continue;

            e.LayerId = detailLayerId;
            if (detailEntityId == ObjectId.Null)
              detailEntityId = pair.Value;
            if (detailBlockRefId == ObjectId.Null && e is BlockReference)
              detailBlockRefId = pair.Value;
            cloned++;
          }

          ObjectId dimSourceId = detailBlockRefId != ObjectId.Null ? detailBlockRefId : detailEntityId;
          this.LogIsoBlockInnerTypes(tagDb, tr, detailBlockRefId);
          if (ms != null && s_isoRealSizeValid)
            this.AppendIsoBoundingDimensions(tr, ms, dimSourceId, annotationLayerId, dimStyleId);
          else
            PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg dim skip realSizeValid=" + s_isoRealSizeValid + " msNull=" + (ms == null));

          viewportOk = this.FitIsoDetailViewport(tr, tagDb, dimSourceId);
          titleblockOk = this.UpdateIsoTitleBlockAttributes(tr, tagDb, tag);
          tr.Commit();
        }

        string detailsDir = this.GetIsoDetailsDirectory();
        if (string.IsNullOrWhiteSpace(detailsDir))
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg save skip: Details dir 없음");
          return false;
        }

        System.IO.Directory.CreateDirectory(detailsDir);
        savedPath = System.IO.Path.Combine(detailsDir, safeTag + ".dwg");
        if (System.IO.File.Exists(savedPath))
          System.IO.File.Delete(savedPath);
        tagDb.SaveAs(savedPath, DwgVersion.Current);
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg tag=" + tag + " path=" + savedPath + " cloned=" + cloned + " viewport=" + (viewportOk ? "ok" : "skip") + " titleblock=" + (titleblockOk ? "ok" : "skip"));
        return true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg 예외: " + ex.GetType().Name + ": " + ex.Message + " path=" + (savedPath ?? "null"));
        return false;
      }
      finally
      {
        HostApplicationServices.WorkingDatabase = oldWorking;
        if (tagDb != null)
          tagDb.Dispose();
      }
    }

    private bool FitIsoDetailViewport(Transaction tr, Database db, ObjectId dimSourceId)
    {
      if (tr == null || db == null || dimSourceId == ObjectId.Null)
        return false;

      try
      {
        Extents3d detailExt;
        if (!this.TryGetIsoModelSpaceExtents(tr, db, out detailExt))
        {
          Entity source = tr.GetObject(dimSourceId, OpenMode.ForRead, false) as Entity;
          if (source == null)
            return false;

          detailExt = source.GeometricExtents;
        }

        double detailW = detailExt.MaxPoint.X - detailExt.MinPoint.X;
        double detailH = detailExt.MaxPoint.Y - detailExt.MinPoint.Y;
        if (detailW <= 1e-9 || detailH <= 1e-9)
          return false;
        double dcx = (detailExt.MinPoint.X + detailExt.MaxPoint.X) / 2.0;
        double dcy = (detailExt.MinPoint.Y + detailExt.MaxPoint.Y) / 2.0;

        ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(db, tr, "Title Block");
        if (layoutBtrId == ObjectId.Null)
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg viewport skip: Title Block layout 없음");
          return false;
        }

        BlockTableRecord layoutBtr = tr.GetObject(layoutBtrId, OpenMode.ForWrite) as BlockTableRecord;
        if (layoutBtr == null)
          return false;

        double lminx = 0.0;
        double lminy = 0.0;
        double lmaxx = 841.0;
        double lmaxy = 594.0;
        string plotSource = "fallbackA1";
        try
        {
          Layout lay = tr.GetObject(layoutBtr.LayoutId, OpenMode.ForRead, false) as Layout;
          if (lay != null)
          {
            Extents2d limits = lay.Limits;
            double lwProbe = limits.MaxPoint.X - limits.MinPoint.X;
            double lhProbe = limits.MaxPoint.Y - limits.MinPoint.Y;
            if (lwProbe > 1e-6 && lhProbe > 1e-6)
            {
              lminx = limits.MinPoint.X;
              lminy = limits.MinPoint.Y;
              lmaxx = limits.MaxPoint.X;
              lmaxy = limits.MaxPoint.Y;
              plotSource = "Limits";
            }
          }
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg plotArea Limits 예외: " + ex.GetType().Name + ": " + ex.Message);
        }

        double lw = lmaxx - lminx;
        double lh = lmaxy - lminy;
        if (lw <= 1e-6 || lh <= 1e-6)
        {
          lminx = 0.0;
          lminy = 0.0;
          lmaxx = 841.0;
          lmaxy = 594.0;
          lw = 841.0;
          lh = 594.0;
          plotSource = "fallbackA1";
        }

        const double RightInset = 0.14;
        const double BottomInset = 0.10;
        const double Margin = 0.03;
        double targetMinX = lminx + lw * Margin;
        double targetMaxX = lmaxx - lw * RightInset;
        double targetMinY = lminy + lh * BottomInset;
        double targetMaxY = lmaxy - lh * Margin;
        double tw = targetMaxX - targetMinX;
        double th = targetMaxY - targetMinY;
        double tcx = (targetMinX + targetMaxX) / 2.0;
        double tcy = (targetMinY + targetMaxY) / 2.0;
        const double DetailViewScale = 0.5;

        Viewport nvp = new Viewport();
        layoutBtr.AppendEntity(nvp);
        tr.AddNewlyCreatedDBObject(nvp, true);
        nvp.CenterPoint = new Point3d(tcx, tcy, 0.0);
        nvp.Width = tw;
        nvp.Height = th;
        nvp.ViewCenter = new Point2d(dcx, dcy);
        nvp.CustomScale = DetailViewScale;
        nvp.On = true;
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg newViewport center=(" + this.FormatNumber(tcx) + "," + this.FormatNumber(tcy) + ") size=(" + this.FormatNumber(tw) + "," + this.FormatNumber(th) + ") viewCenter=(" + this.FormatNumber(dcx) + "," + this.FormatNumber(dcy) + ") customScale=1:2 detailExt=(" + this.FormatNumber(detailExt.MinPoint.X) + "," + this.FormatNumber(detailExt.MinPoint.Y) + ")~(" + this.FormatNumber(detailExt.MaxPoint.X) + "," + this.FormatNumber(detailExt.MaxPoint.Y) + ") plotArea=(" + this.FormatNumber(lminx) + "," + this.FormatNumber(lminy) + ")~(" + this.FormatNumber(lmaxx) + "," + this.FormatNumber(lmaxy) + ") source=" + plotSource);
        return true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg viewport 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      return false;
    }

    private bool TryGetIsoModelSpaceExtents(Transaction tr, Database db, out Extents3d extents)
    {
      extents = new Extents3d();
      if (tr == null || db == null)
        return false;

      bool has = false;
      try
      {
        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
        if (bt == null || !bt.Has(BlockTableRecord.ModelSpace))
          return false;

        BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
        if (ms == null)
          return false;

        foreach (ObjectId id in ms)
        {
          try
          {
            Entity entity = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
            if (entity == null)
              continue;

            Extents3d entityExt = entity.GeometricExtents;
            if (!has)
            {
              extents = entityExt;
              has = true;
            }
            else
            {
              extents.AddExtents(entityExt);
            }
          }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg detailExt entity skip id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
          }
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg detailExt 예외: " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }

      return has;
    }

    private bool UpdateIsoTitleBlockAttributes(Transaction tr, Database db, string supportTag)
    {
      if (tr == null || db == null)
        return false;

      try
      {
        ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(db, tr, "Title Block");
        if (layoutBtrId == ObjectId.Null)
          return false;

        BlockTableRecord layoutBtr = tr.GetObject(layoutBtrId, OpenMode.ForRead) as BlockTableRecord;
        if (layoutBtr == null)
          return false;

        foreach (ObjectId id in layoutBtr)
        {
          BlockReference br = tr.GetObject(id, OpenMode.ForRead, false) as BlockReference;
          if (br == null)
            continue;

          string blockName = this.GetBlockReferenceName(tr, br);
          if (!string.Equals(blockName, "DRAWING_TITLE", System.StringComparison.OrdinalIgnoreCase))
            continue;

          bool changed = false;
          foreach (ObjectId attId in br.AttributeCollection)
          {
            AttributeReference att = tr.GetObject(attId, OpenMode.ForWrite, false) as AttributeReference;
            if (att == null)
              continue;

            string value = this.GetIsoTitleBlockValue(att.Tag, supportTag);
            if (string.IsNullOrWhiteSpace(value))
              continue;

            att.TextString = value;
            changed = true;
          }

          return changed;
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED separateDwg titleblock 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      return false;
    }

    private string GetIsoTitleBlockValue(string tagName, string supportTag)
    {
      if (string.IsNullOrWhiteSpace(tagName))
        return null;

      string tag = tagName.Trim().ToUpperInvariant();
      if (tag == "SUPPORT_CODE")
        return supportTag;
      if (tag == "PIPE_NO")
        return s_isoPipeLineNo;
      if (tag == "STD")
        return s_isoDesignStd;
      if (tag == "DRAWING_NO")
        return supportTag;
      if (tag == "PIPE_SUPPORT_DETAIL")
        return s_isoShortDesc;
      return null;
    }

    private void PurgePriorIsoDetail(Transaction tr, BlockTableRecord targetMs, ObjectId detailLayerId, ObjectId annotationLayerId)
    {
      if (tr == null || targetMs == null)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED priorDetail purge skip: target null");
        return;
      }

      int erased = 0;
      System.Collections.Generic.List<ObjectId> eraseIds = new System.Collections.Generic.List<ObjectId>();
      foreach (ObjectId id in targetMs)
      {
        try
        {
          Entity ent = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
          if (ent == null)
            continue;
          if ((detailLayerId != ObjectId.Null && ent.LayerId == detailLayerId) || (annotationLayerId != ObjectId.Null && ent.LayerId == annotationLayerId))
            eraseIds.Add(id);
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED priorDetail purge scan skip id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
        }
      }

      foreach (ObjectId id in eraseIds)
      {
        try
        {
          Entity ent = tr.GetObject(id, OpenMode.ForWrite, false) as Entity;
          if (ent == null || ent.IsErased)
            continue;
          ent.Erase();
          erased++;
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED priorDetail purge erase skip id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
        }
      }

      PlantOrthoView.FileDiag("PFSVBISOEXPORTED priorDetail purge erased=" + erased);
    }

    private void CloseIsoTempDocument(Document tempDoc, Document originalDoc)
    {
      bool discarded = false;
      bool queueRedraw = false;
      try
      {
        if (originalDoc != null && !object.ReferenceEquals(originalDoc, tempDoc))
        {
          Application.DocumentManager.MdiActiveDocument = originalDoc;
          queueRedraw = true;
        }
        tempDoc.CloseAndDiscard();
        discarded = true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOOPEN CloseAndDiscard 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
      finally
      {
        if (queueRedraw && originalDoc != null)
        {
          s_isoRedrawPendingDoc = originalDoc;
          s_isoRedrawAttempt = 0;
          Application.Idle -= this.VbIsoRedrawOnIdle;
          Application.Idle += this.VbIsoRedrawOnIdle;
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED viewcube redraw Idle 예약 doc=" + originalDoc.Name + " discard=" + discarded);
        }
      }
      PlantOrthoView.FileDiag("PFSVBISOOPEN closed tempDoc discard=" + discarded);
    }


    private void VbIsoRedrawOnIdle(object sender, System.EventArgs e)
    {
      Application.Idle -= this.VbIsoRedrawOnIdle;
      try
      {
        Document doc = s_isoRedrawPendingDoc;
        if (doc == null)
          return;

        if (!object.ReferenceEquals(doc, Application.DocumentManager.MdiActiveDocument))
        {
          s_isoRedrawAttempt++;
          if (s_isoRedrawAttempt <= 2)
          {
            Application.Idle += this.VbIsoRedrawOnIdle;
            PlantOrthoView.FileDiag("PFSVBISOEXPORTED viewcube redraw 대기 attempt=" + s_isoRedrawAttempt + " doc=" + doc.Name);
            return;
          }

          PlantOrthoView.FileDiag("PFSVBISOEXPORTED viewcube redraw 포기 attempt=" + s_isoRedrawAttempt + " doc=" + doc.Name);
          s_isoRedrawPendingDoc = null;
          s_isoRedrawAttempt = 0;
          return;
        }

        s_isoRedrawPendingDoc = null;
        s_isoRedrawAttempt = 0;
        object navvcube = null;
        try
        {
          navvcube = Application.GetSystemVariable("NAVVCUBEDISPLAY");
        }
        catch (System.Exception ex)
        {
          navvcube = "read-error:" + ex.GetType().Name;
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED viewcube NAVVCUBEDISPLAY 조회 예외: " + ex.GetType().Name + ": " + ex.Message);
        }

        doc.SendStringToExecute("._REGENALL\n", true, false, false);
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED viewcube redraw(Idle) navvcube=" + navvcube + " REGENALL 큐잉 doc=" + doc.Name);
      }
      catch (System.Exception ex)
      {
        s_isoRedrawPendingDoc = null;
        s_isoRedrawAttempt = 0;
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED viewcube redraw Idle 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    [CommandMethod("PFSISOTBLPROBE")]
    public void ProbeIsoTitleBlockCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      Editor ed = doc != null ? doc.Editor : null;
      PlantOrthoView.FileDiag("PFSISOTBLPROBE start doc=" + (doc != null ? doc.Name : "null"));
      this.ProbeIsoTemplate();
      this.ProbeIsoSupportSelection(doc, ed);
      this.ProbeIsoOutputDirectory(doc);
      if (ed != null)
        ed.WriteMessage("\nPFSISOTBLPROBE done. Check FileDiag log.");
    }

    private string ResolveIsoTemplatePath()
    {
      // 1) PSUtil.OrthoTemplate static (UI AddSupport/MTO 흐름서만 세팅) 2) Settings.tbTemplate(영구 저장) 3) 하드코딩 폴백
      string fromStatic = PSUtil.OrthoTemplate;
      if (!string.IsNullOrWhiteSpace(fromStatic) && System.IO.File.Exists(fromStatic))
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE template 해석 source=OrthoTemplate path=" + fromStatic);
        return fromStatic;
      }

      string fromSettings = null;
      try
      {
        object v = PlantFlow_Support.Properties.Settings.Default["tbTemplate"];
        fromSettings = v as string;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE template Settings 조회 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
      if (!string.IsNullOrWhiteSpace(fromSettings) && System.IO.File.Exists(fromSettings))
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE template 해석 source=Settings path=" + fromSettings);
        return fromSettings;
      }

      const string hardcoded = @"C:\Temp\Template\Ortho_A1_Pipe Support.dwt";
      if (System.IO.File.Exists(hardcoded))
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE template 해석 source=하드코딩 path=" + hardcoded);
        return hardcoded;
      }

      PlantOrthoView.FileDiag("PFSISOTBLPROBE template 해석 실패 static=" + (fromStatic ?? "null") + " settings=" + (fromSettings ?? "null") + " hardcoded=" + hardcoded);
      return null;
    }

    private void ProbeIsoTemplate()
    {
      string templatePath = this.ResolveIsoTemplatePath();
      if (string.IsNullOrWhiteSpace(templatePath) || !System.IO.File.Exists(templatePath))
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE template 없음 path=" + (templatePath ?? "null"));
        return;
      }

      Database tdb = null;
      string openedBy = "direct";
      string tempPath = null;
      try
      {
        try
        {
          tdb = new Database(false, true);
          tdb.ReadDwgFile(templatePath, System.IO.FileShare.Read, true, null);
          PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl direct open ok path=" + templatePath);
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl direct open 예외: " + ex.GetType().Name + ": " + ex.Message + " path=" + templatePath);
          if (tdb != null)
          {
            tdb.Dispose();
            tdb = null;
          }

          try
          {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pfs_isotblprobe");
            System.IO.Directory.CreateDirectory(tempDir);
            tempPath = System.IO.Path.Combine(tempDir, "template_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture) + ".dwg");
            System.IO.File.Copy(templatePath, tempPath, true);
            tdb = new Database(false, true);
            tdb.ReadDwgFile(tempPath, System.IO.FileShare.Read, true, null);
            openedBy = "temp-copy";
            PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl temp-copy open ok path=" + tempPath);
          }
          catch (System.Exception ex2)
          {
            PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl temp-copy open 예외: " + ex2.GetType().Name + ": " + ex2.Message + " source=" + templatePath + " temp=" + tempPath);
            return;
          }
        }

        if (tdb != null)
          this.ProbeIsoTemplateDatabase(tdb, templatePath, openedBy);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl probe 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
      finally
      {
        if (tdb != null)
          tdb.Dispose();
        if (!string.IsNullOrEmpty(tempPath))
        {
          try
          {
            if (System.IO.File.Exists(tempPath))
              System.IO.File.Delete(tempPath);
          }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl temp delete 예외: " + ex.GetType().Name + ": " + ex.Message + " path=" + tempPath);
          }
        }
      }
    }

    private void ProbeIsoTemplateDatabase(Database tdb, string templatePath, string openedBy)
    {
      using (Transaction tr = tdb.TransactionManager.StartTransaction())
      {
        DBDictionary layouts = tr.GetObject(tdb.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
        BlockTable bt = tr.GetObject(tdb.BlockTableId, OpenMode.ForRead) as BlockTable;
        if (layouts != null)
        {
          foreach (DBDictionaryEntry entry in layouts)
          {
            try
            {
              Layout layout = tr.GetObject(entry.Value, OpenMode.ForRead, false) as Layout;
              if (layout == null)
                continue;
              BlockTableRecord btr = tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead, false) as BlockTableRecord;
              int entityCount = 0;
              int viewportCount = 0;
              if (btr != null)
              {
                foreach (ObjectId id in btr)
                {
                  entityCount++;
                  if (tr.GetObject(id, OpenMode.ForRead, false) is Viewport)
                    viewportCount++;
                }
              }
              PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl layout name=" + layout.LayoutName + " btr=" + layout.BlockTableRecordId + " entities=" + entityCount + " viewports=" + viewportCount + " openedBy=" + openedBy);
              this.ProbeIsoLayoutViewports(tr, btr, layout.LayoutName);
              this.ProbeIsoTitleBlocks(tr, btr, layout.LayoutName);
            }
            catch (System.Exception ex)
            {
              PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl layout 예외 key=" + entry.Key + ": " + ex.GetType().Name + ": " + ex.Message);
            }
          }
        }
        else
        {
          PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl layouts 없음 path=" + templatePath);
        }

        if (bt != null && bt.Has(BlockTableRecord.ModelSpace))
        {
          BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead, false) as BlockTableRecord;
          int entityCount = 0;
          int viewportCount = 0;
          if (ms != null)
          {
            foreach (ObjectId id in ms)
            {
              entityCount++;
              if (tr.GetObject(id, OpenMode.ForRead, false) is Viewport)
                viewportCount++;
            }
          }
          PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl modelspace entities=" + entityCount + " viewports=" + viewportCount);
          this.ProbeIsoTitleBlocks(tr, ms, "ModelSpace");
        }

        tr.Commit();
      }
    }

    private void ProbeIsoLayoutViewports(Transaction tr, BlockTableRecord btr, string layoutName)
    {
      if (tr == null || btr == null)
        return;
      foreach (ObjectId id in btr)
      {
        try
        {
          Viewport vp = tr.GetObject(id, OpenMode.ForRead, false) as Viewport;
          if (vp == null)
            continue;
          PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl viewport layout=" + layoutName + " id=" + id + " number=" + vp.Number + " center=" + vp.CenterPoint + " viewCenter=" + vp.ViewCenter + " customScale=" + vp.CustomScale + " locked=" + vp.Locked);
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl viewport 예외 layout=" + layoutName + " id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
        }
      }
    }

    private int ProbeIsoTitleBlocks(Transaction tr, BlockTableRecord btr, string ownerName)
    {
      if (tr == null || btr == null)
        return 0;
      int found = 0;
      foreach (ObjectId id in btr)
      {
        try
        {
          BlockReference br = tr.GetObject(id, OpenMode.ForRead, false) as BlockReference;
          if (br == null)
            continue;
          string blockName = this.GetBlockReferenceName(tr, br);
          if (!string.Equals(blockName, "DRAWING_TITLE", System.StringComparison.OrdinalIgnoreCase))
            continue;

          found++;
          System.Collections.Generic.List<string> tags = new System.Collections.Generic.List<string>();
          foreach (ObjectId attId in br.AttributeCollection)
          {
            AttributeReference ar = tr.GetObject(attId, OpenMode.ForRead, false) as AttributeReference;
            if (ar != null)
              tags.Add(ar.Tag + "=" + ar.TextString);
          }
          PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl titleBlock owner=" + ownerName + " id=" + id + " tags=" + string.Join(",", tags.ToArray()) + " hasPIPE_SUPPORT_DETAIL=" + tags.Exists(t => t.StartsWith("PIPE_SUPPORT_DETAIL=", System.StringComparison.OrdinalIgnoreCase)));
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl titleBlock 예외 owner=" + ownerName + " id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
        }
      }
      PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl titleBlock owner=" + ownerName + " count=" + found);
      return found;
    }

    private string GetBlockReferenceName(Transaction tr, BlockReference br)
    {
      try
      {
        if (!string.IsNullOrEmpty(br.Name))
          return br.Name;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl block name 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      try
      {
        BlockTableRecord btr = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead, false) as BlockTableRecord;
        return btr != null ? btr.Name : string.Empty;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE tmpl block btr name 예외: " + ex.GetType().Name + ": " + ex.Message);
        return string.Empty;
      }
    }

    private void ProbeIsoSupportSelection(Document doc, Editor ed)
    {
      if (doc == null || ed == null)
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE support skip: doc/editor null");
        return;
      }

      PromptSelectionResult psr = ed.GetSelection();
      if (psr.Status != PromptStatus.OK || psr.Value == null)
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE support selection skip status=" + psr.Status);
        return;
      }

      PSUtil ps = Commands.PSUtil;
      if (ps == null)
        ps = new PSUtil();
      if (ps == null || ps.dl_manager == null)
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE support skip: dl_manager null");
        return;
      }

      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        foreach (SelectedObject selected in psr.Value)
        {
          if (selected == null || selected.ObjectId == ObjectId.Null)
            continue;
          try
          {
            Entity ent = tr.GetObject(selected.ObjectId, OpenMode.ForRead, false) as Entity;
            if (ent == null)
              continue;
            string typeName = ent.GetType().Name;
            string dxfName = ent.GetRXClass() != null ? ent.GetRXClass().DxfName : string.Empty;
            bool isSupport = typeName.IndexOf("Support", System.StringComparison.OrdinalIgnoreCase) >= 0 || dxfName.IndexOf("Support", System.StringComparison.OrdinalIgnoreCase) >= 0;
            PlantOrthoView.FileDiag("PFSISOTBLPROBE support entity id=" + selected.ObjectId + " type=" + typeName + " dxf=" + dxfName + " isSupport=" + isSupport);
            if (!isSupport)
              continue;
            this.ProbeIsoSupportProperties(ps, selected.ObjectId);
          }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSISOTBLPROBE support entity 예외 id=" + selected.ObjectId + ": " + ex.GetType().Name + ": " + ex.Message);
          }
        }
        tr.Commit();
      }
    }

    private void ProbeIsoSupportProperties(PSUtil ps, ObjectId id)
    {
      bool loggedAny = false;
      try
      {
        System.Reflection.MethodInfo mi = ps.dl_manager.GetType().GetMethod("GetAllProperties", new System.Type[] { typeof(ObjectId), typeof(bool) });
        if (mi != null)
        {
          object result = mi.Invoke(ps.dl_manager, new object[] { id, true });
          System.Collections.IEnumerable seq = result as System.Collections.IEnumerable;
          if (seq != null && !(result is string))
          {
            foreach (object item in seq)
            {
              string name;
              string value;
              this.ExtractProbeProperty(item, out name, out value);
              PlantOrthoView.FileDiag("PFSISOTBLPROBE support prop " + name + "=" + value);
              loggedAny = true;
            }
          }
          else if (result != null)
          {
            PlantOrthoView.FileDiag("PFSISOTBLPROBE support prop GetAllProperties=" + result);
            loggedAny = true;
          }
        }
        else
        {
          PlantOrthoView.FileDiag("PFSISOTBLPROBE support prop GetAllProperties method 없음");
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE support prop GetAllProperties 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      string[] candidates = new string[] { "SupportName", "ShortDescription", "LineNumberTag", "Tag", "Number", "PnPTag", "SupportDetail" };
      foreach (string name in candidates)
      {
        try
        {
          System.Collections.Specialized.StringCollection names = new System.Collections.Specialized.StringCollection() { name };
          System.Collections.Specialized.StringCollection props = ps.dl_manager.GetProperties(id, names, true);
          string value = props != null && props.Count > 0 ? props[0] : string.Empty;
          PlantOrthoView.FileDiag("PFSISOTBLPROBE support prop candidate " + name + "=" + value);
          loggedAny = true;
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSISOTBLPROBE support prop candidate " + name + " 예외: " + ex.GetType().Name + ": " + ex.Message);
        }
      }

      if (!loggedAny)
        PlantOrthoView.FileDiag("PFSISOTBLPROBE support prop 없음 id=" + id);
    }

    private void ExtractProbeProperty(object item, out string name, out string value)
    {
      name = item != null ? item.ToString() : "null";
      value = string.Empty;
      if (item == null)
        return;

      try
      {
        System.Collections.DictionaryEntry de = item is System.Collections.DictionaryEntry ? (System.Collections.DictionaryEntry)item : new System.Collections.DictionaryEntry(null, null);
        if (de.Key != null || de.Value != null)
        {
          name = de.Key != null ? de.Key.ToString() : "null";
          value = de.Value != null ? de.Value.ToString() : string.Empty;
          return;
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE support prop DictionaryEntry 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      try
      {
        object key = this.GetProbePropertyValue(item, "Key") ?? this.GetProbePropertyValue(item, "Name") ?? this.GetProbePropertyValue(item, "PropertyName") ?? this.GetProbePropertyValue(item, "DisplayName");
        object val = this.GetProbePropertyValue(item, "Value") ?? this.GetProbePropertyValue(item, "Text") ?? this.GetProbePropertyValue(item, "StringValue");
        if (key != null)
          name = key.ToString();
        if (val != null)
          value = val.ToString();
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE support prop reflection 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private object GetProbePropertyValue(object item, string propertyName)
    {
      if (item == null || string.IsNullOrEmpty(propertyName))
        return null;
      System.Reflection.PropertyInfo pi = item.GetType().GetProperty(propertyName);
      return pi != null ? pi.GetValue(item, null) : null;
    }

    private void ProbeIsoOutputDirectory(Document doc)
    {
      string projectDir = string.Empty;
      try
      {
        Autodesk.ProcessPower.ProjectManager.Project piping = Autodesk.ProcessPower.PlantInstance.PlantApplication.CurrentProject.ProjectParts["Piping"] as Autodesk.ProcessPower.ProjectManager.Project;
        if (piping != null)
          projectDir = piping.ProjectDwgDirectory;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE dir project ProjectDwgDirectory 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      string docDir = string.Empty;
      try
      {
        if (doc != null && doc.Database != null && !string.IsNullOrWhiteSpace(doc.Database.Filename))
          docDir = System.IO.Path.GetDirectoryName(doc.Database.Filename);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE dir active doc folder 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      string baseDir = !string.IsNullOrWhiteSpace(projectDir) ? projectDir : docDir;
      string detailsDir = !string.IsNullOrWhiteSpace(baseDir) ? System.IO.Path.Combine(baseDir, "Details") : string.Empty;
      PlantOrthoView.FileDiag("PFSISOTBLPROBE dir project=" + projectDir + " doc=" + docDir + " details=" + detailsDir);
      if (string.IsNullOrWhiteSpace(detailsDir))
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE dir details skip: base dir empty");
        return;
      }

      try
      {
        bool exists = System.IO.Directory.Exists(detailsDir);
        string probeFile = System.IO.Path.Combine(exists ? detailsDir : baseDir, ".pfs_write_probe_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture) + ".tmp");
        System.IO.File.WriteAllText(probeFile, "probe");
        System.IO.File.Delete(probeFile);
        PlantOrthoView.FileDiag("PFSISOTBLPROBE dir details exists=" + exists + " writable=True path=" + detailsDir);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSISOTBLPROBE dir details writable=False 예외: " + ex.GetType().Name + ": " + ex.Message + " path=" + detailsDir);
      }
    }
    private void CaptureIsoSelectionMetrics(Database db, ObjectId[] selectedIds)
    {
      if (db == null || selectedIds == null || selectedIds.Length == 0)
        return;

      Vector3d axis = s_isoPipeAxisValid ? s_isoPipeAxis.GetNormal() : Vector3d.XAxis;
      Vector3d upSeed = System.Math.Abs(axis.DotProduct(Vector3d.ZAxis)) < 0.9 ? Vector3d.ZAxis : Vector3d.XAxis;
      Vector3d up = upSeed - axis.MultiplyBy(upSeed.DotProduct(axis));
      if (up.Length <= 1e-6)
        up = Vector3d.YAxis - axis.MultiplyBy(Vector3d.YAxis.DotProduct(axis));
      if (up.Length <= 1e-6)
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE metric skip: up basis degenerate axis=" + this.FormatVectorForCommand(axis));
        return;
      }
      up = up.GetNormal();
      s_isoPipeUp = up;
      Vector3d right = up.CrossProduct(axis);
      if (right.Length <= 1e-6)
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE metric skip: right basis degenerate axis=" + this.FormatVectorForCommand(axis));
        return;
      }
      right = right.GetNormal();

      bool hasProjection = false;
      double minR = 0.0;
      double maxR = 0.0;
      double minU = 0.0;
      double maxU = 0.0;
      ObjectId supportId = ObjectId.Null;

      try
      {
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          foreach (ObjectId id in selectedIds)
          {
            try
            {
              DBObject obj = tr.GetObject(id, OpenMode.ForRead, false);
              Entity ent = obj as Entity;
              if (ent == null)
                continue;

              string className = id.ObjectClass == null ? string.Empty : id.ObjectClass.Name;
              string typeName = obj.GetType().Name;
              if (className.IndexOf("Support", System.StringComparison.OrdinalIgnoreCase) < 0 && typeName.IndexOf("Support", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

              if (supportId == ObjectId.Null)
                supportId = id;
              Extents3d ext = ent.GeometricExtents;
              Point3d[] corners = this.GetExtentsCorners(ext);
              foreach (Point3d corner in corners)
              {
                double r = corner.GetAsVector().DotProduct(right);
                double u = corner.GetAsVector().DotProduct(up);
                if (!hasProjection)
                {
                  minR = maxR = r;
                  minU = maxU = u;
                  hasProjection = true;
                }
                else
                {
                  if (r < minR) minR = r;
                  if (r > maxR) maxR = r;
                  if (u < minU) minU = u;
                  if (u > maxU) maxU = u;
                }
              }
            }
            catch (System.Exception ex)
            {
              PlantOrthoView.FileDiag("PFSVBISOCLONE metric support skip id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
            }
          }
          tr.Commit();
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE metric scan 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      if (hasProjection)
      {
        s_isoRealWidth = System.Math.Abs(maxR - minR);
        s_isoRealHeight = System.Math.Abs(maxU - minU);
        s_isoRealSizeValid = s_isoRealWidth > 1e-6 && s_isoRealHeight > 1e-6;
      }
      else
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE metric support bounds 없음");
      }

      this.CaptureIsoSupportProperties(supportId, s_isoPipeId);
      this.CaptureIsoSupportProfile(supportId);
      this.CaptureIsoSupportPorts(supportId);
      this.AugmentDesignationsFromBom(supportId);
      // [MEASURE cycle94] 원본 DB 컨텍스트에서만 가능한 BOM·밸룬 원천을 계측한다.
      // BOMs/PSUtil은 Plant 프로젝트·DataLinksManager 의존이라 side DB에서 호출 불가.
      this.MeasureNotabBomBalloonSources(supportId);
      bool bomSpike = this.GetEnvDouble("PFS_NOTAB_BOM_SPIKE", 0.0, 0.0, 1.0) >= 0.5;
      if (bomSpike)
        this.NotabBomSpike(supportId);
      PlantOrthoView.FileDiag("PFSVBISOCLONE PLN=" + s_isoPipeLineNo + " BOP=" + s_isoBOP + " size=" + s_isoSize + " realW=" + this.FormatNumber(s_isoRealWidth) + " realH=" + this.FormatNumber(s_isoRealHeight) + " basisRight=" + this.FormatVectorForCommand(right) + " basisUp=" + this.FormatVectorForCommand(up) + " valid=" + s_isoRealSizeValid);
    }

    private void CaptureIsoSupportProperties(ObjectId supportId, ObjectId pipeId)
    {
      try
      {
        PSUtil ps = Commands.PSUtil;
        if (ps == null)
          ps = new PSUtil();
        if (ps == null || ps.dl_manager == null)
        {
          PlantOrthoView.FileDiag("PFSVBISOCLONE props skip: dl_manager null");
          return;
        }

        if (supportId != ObjectId.Null)
        {
          try
          {
            System.Collections.Specialized.StringCollection names = new System.Collections.Specialized.StringCollection()
            {
              "SupportName",
              "DesignStd",
              "LineNumberTag",
              "SupportDetail",
              "BOP",
              "Position Z",
              "Size",
              "ShortDescription"
            };
            System.Collections.Specialized.StringCollection props = ps.dl_manager.GetProperties(supportId, names, true);
            if (props == null)
            {
              PlantOrthoView.FileDiag("PFSVBISOCLONE props skip: GetProperties null");
            }
            else
            {
              if (props.Count > 0) s_isoSupportTag = props[0];
              if (props.Count > 1) s_isoDesignStd = props[1];
              if (props.Count > 2) s_isoPipeLineNo = props[2];
              if (props.Count > 4) s_isoBOP = this.RoundPropertyValue(props[4]);
              if (props.Count > 6) s_isoSize = props[6];
              if (props.Count > 7) s_isoShortDesc = props[7];
            }
          }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSVBISOCLONE support props 예외: " + ex.GetType().Name + ": " + ex.Message);
          }
        }
        else
        {
          PlantOrthoView.FileDiag("PFSVBISOCLONE props skip: supportId null");
        }

        if (string.IsNullOrWhiteSpace(s_isoPipeLineNo))
          this.CaptureIsoPipeLineNo(ps, pipeId);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE props 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    // 활성 원본 DB에서만 호출한다. 이후 상세 DB에서는 이 스냅샷을 투영만 한다.
    // BOM 표. 원본에서 캡처한 s_isoBomRows만 참조한다(side DB엔 Plant 컨텍스트가 없다).
    // 좌표·열폭은 오쏘 실측값에서 출발한다. 표는 뷰포트(30.5~640.5) 바깥 우측에 놓인다.
    private void AppendNotabBomTable(Transaction tr, BlockTableRecord layoutBtr, Database db,
      ObjectId textStyleId, ObjectId layerId, NotabCalloutPlacer placer)
    {
      if (tr == null || layoutBtr == null || db == null)
        return;
      if (s_isoBomRows.Count == 0)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL bom-table skip: rows 0");
        return;
      }

      try
      {
        double originX = this.GetEnvDouble("PFS_NOTAB_BOM_X", 640.5, -5000.0, 5000.0);
        double originY = this.GetEnvDouble("PFS_NOTAB_BOM_Y", 84.5, -5000.0, 5000.0);
        double rowH = this.GetEnvDouble("PFS_NOTAB_BOM_ROW_H", 8.0, 1.0, 50.0);
        double cellTxt = this.GetEnvDouble("PFS_NOTAB_BOM_TXT", 3.0, 0.5, 20.0);
        double[] colW = new double[] { 15.0, 30.0, 40.0, 40.0, 30.0, 25.0 };
        string[] header = new string[] { "ITEM", "ANCILLARY", "DESCRIPTION", "MATERIAL", "QUAN/LEN", "REMARK" };

        Table table = new Table();
        table.TableStyle = db.Tablestyle;
        table.SetSize(s_isoBomRows.Count + 2, colW.Length);
        table.SetRowHeight(rowH);
        // 행 증가 방향. 기본값이면 아래로 자라 타이틀블록에 묻힌다(라이브 실측 ext=(...,36.5)~(...,84.5)).
        table.FlowDirection = (FlowDirection)1;
        for (int c = 0; c < colW.Length; c++)
          table.Columns[c].Width = colW[c];

        table.Cells[0, 0].TextString = "BILL OF MATERIALS";
        for (int c = 0; c < colW.Length; c++)
          table.Cells[1, c].TextString = header[c];
        for (int i = 0; i < s_isoBomRows.Count; i++)
        {
          NotabBomRow row = s_isoBomRows[i];
          string[] vals = new string[] { row.Item, row.Category, row.Description, row.Material, row.QuantityOrLength, row.Remark };
          for (int c = 0; c < colW.Length; c++)
            table.Cells[i + 2, c].TextString = vals[c] ?? string.Empty;
        }
        for (int r = 0; r < s_isoBomRows.Count + 2; r++)
        {
          table.Rows[r].Height = rowH;
          for (int c = 0; c < colW.Length; c++)
          {
            table.Cells[r, c].TextHeight = cellTxt;
            // 기본값이면 문자가 셀 상단에 붙어 행 경계선과 겹친다. 세로 중앙 정렬로 맞춘다.
            table.Cells[r, c].Alignment = CellAlignment.MiddleCenter;
            if (textStyleId != ObjectId.Null)
              table.Cells[r, c].TextStyleId = textStyleId;
          }
        }

        ((BlockReference)table).Position = new Point3d(originX, originY, 0.0);
        if (layerId != ObjectId.Null)
          ((Entity)table).LayerId = layerId;

        // GenerateLayout은 반드시 AppendEntity 전에 호출한다.
        table.GenerateLayout();
        layoutBtr.AppendEntity(table);
        tr.AddNewlyCreatedDBObject(table, true);

        Extents3d ext;
        try { ext = ((Entity)table).GeometricExtents; }
        catch (System.Exception gx)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL bom-table extents 실패: " + gx.GetType().Name + ": " + gx.Message);
          return;
        }

        if (placer != null)
          placer.AddObstacle(ext, "bomtable");
        PlantOrthoView.FileDiag("PFSNOTABDETAIL bom-table rows=" + s_isoBomRows.Count
          + " pos=(" + this.FormatNumber(originX) + "," + this.FormatNumber(originY) + ")"
          + " ext=" + this.FormatExtents(ext)
          + " w=" + this.FormatNumber(ext.MaxPoint.X - ext.MinPoint.X)
          + " h=" + this.FormatNumber(ext.MaxPoint.Y - ext.MinPoint.Y));
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL bom-table 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    // 세로재 판정. 기본은 PPorts[1](=S2)이며, RC5의 F2만 PPorts[2](=S3) 자유단을 쓴다.
    // 이름 규칙이 아니라 WCS 일치로 판정해 다른 타입의 기존 매핑은 보존한다.
    private bool IsNotabVerticalMemberPort(Point3d wcs)
    {
      string standardName = this.GetNotabStandardName();
      int verticalPortIndex = string.Equals(standardName, "RC5", System.StringComparison.OrdinalIgnoreCase) ? 2 : 1;
      for (int i = 0; i < s_isoSupportPorts.Count; i++)
      {
        if (s_isoSupportPorts[i].Index != verticalPortIndex) continue;
        Point3d p = s_isoSupportPorts[i].Wcs;
        return System.Math.Abs(p.X - wcs.X) < 1e-6
          && System.Math.Abs(p.Y - wcs.Y) < 1e-6
          && System.Math.Abs(p.Z - wcs.Z) < 1e-6;
      }
      return false;
    }

    // 밸룬 아래 표기할 부재 코드. BOM DESCRIPTION "ANGLE A6" → "A6".
    // 코드 형태(영문1+숫자)가 없으면 표기를 생략한다(BASE PLATE 치수 등은 표에서 읽는다).
    private string GetNotabBalloonSubLabel(string description)
    {
      if (string.IsNullOrWhiteSpace(description))
        return string.Empty;
      try
      {
        string[] tokens = description.Trim().Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = tokens.Length - 1; i >= 0; i--)
        {
          if (System.Text.RegularExpressions.Regex.IsMatch(tokens[i], "^[A-Za-z]{1,2}[0-9]{1,3}$"))
            return tokens[i].ToUpperInvariant();
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL balloon sub-label 파싱 예외 desc=" + description + ": " + ex.GetType().Name + ": " + ex.Message);
      }
      return string.Empty;
    }

    // 밸룬(원 + 리더 + 번호). MLeader는 제어 불가로 폐기됐으므로 직접 작도한다.
    // 앵커는 원본에서 캡처한 TaggingPoints p0을 투영해 얻는다. 투영 실패·범위 이탈이면 그리지 않는다.
    private void AppendNotabBalloons(Transaction tr, BlockTableRecord layoutBtr, Database db,
      ObjectId textStyleId, ObjectId layerId, Extents3d viewportPaperExt,
      double txt, NotabCalloutPlacer placer, Viewport vp,
      // verticalX는 "치수선" X이지 부재 X가 아니다(cycle102 자문). 부재 형상 좌표로 쓰지 말 것.
      // 세로재 판정에만 쓰고, 밸룬 작도 좌표는 memberBoxes의 PaperBox에서 얻는다.
      bool hasVerticalAnchor, double verticalX, double verticalBaseY, double verticalTopY,
      double horizontalMemberMinX, double horizontalMemberMaxX,
      // true=부재 밸룬(F 계열)만, false=나머지(P1 등)만. 부재 밸룬을 콜아웃보다 먼저 배치하기 위한 분리.
      bool memberPass)
    {
      if (tr == null || layoutBtr == null || db == null || placer == null)
        return;
      if (s_isoBalloonAnchors.Count == 0)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL balloon skip: anchors 0");
        return;
      }

      // 밸룬 리더는 서포트 위를 지나는 것이 정상이다. 이 구간에서만 면제한다.
      placer.SetLeaderExemptOwner("support");
      try
      {
      string standardName = this.GetNotabStandardName();
      double radius = this.GetEnvDouble("PFS_NOTAB_BALLOON_R", txt * 1.2, 1.0, 100.0);
      double gap = this.GetEnvDouble("PFS_NOTAB_DIM_ARR", 10.0, 0.5, 50.0);
      // 밸룬은 작은 기호라 텍스트 콜아웃과 같은 이격(40)을 요구하면 멀리 밀려난다.
      foreach (NotabBalloonAnchor anchorInfo in s_isoBalloonAnchors)
      {
        // BOM 표에 없는 번호는 그리지 않는다. P1_0 처럼 확장된 키는 접두 Item으로 대응한다.
        string item = anchorInfo.Item ?? string.Empty;
        string bomItem = null;
        string bomDesc = string.Empty;
        foreach (NotabBomRow row in s_isoBomRows)
        {
          if (string.Equals(row.Item, item, System.StringComparison.OrdinalIgnoreCase)
            || item.StartsWith(row.Item + "_", System.StringComparison.OrdinalIgnoreCase))
          { bomItem = row.Item; bomDesc = row.Description ?? string.Empty; break; }
        }
        if (string.IsNullOrWhiteSpace(bomItem))
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL balloon-skip key=" + item + " reason=no-bom-row");
          continue;
        }

        Point3d rawAnchor;
        if (!this.TryNotabProjectWcsToPaper(vp, anchorInfo.WcsP0, out rawAnchor))
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL balloon-skip key=" + item + " reason=projection-failed");
          continue;
        }
        if (rawAnchor.X < viewportPaperExt.MinPoint.X || rawAnchor.X > viewportPaperExt.MaxPoint.X
          || rawAnchor.Y < viewportPaperExt.MinPoint.Y || rawAnchor.Y > viewportPaperExt.MaxPoint.Y)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL balloon-skip key=" + item + " reason=outside-viewport rawAnchor=" + this.FormatPoint(rawAnchor));
          continue;
        }

        Point3d anchor = rawAnchor;
        string anchorAdjust = "port";
        bool isVerticalMember = hasVerticalAnchor && this.IsNotabVerticalMemberPort(anchorInfo.WcsP0);
        bool isPlateItem = string.Equals(bomItem, "P1", System.StringComparison.OrdinalIgnoreCase);
        // RC7 F2는 RS2()의 PPorts[2]가 가리키는 대각 브레이스다. 가로재 끝단 규칙을 적용하지 않는다.
        bool isRc7DiagonalMember = string.Equals(standardName, "RC7", System.StringComparison.OrdinalIgnoreCase)
          && string.Equals(item, "F2", System.StringComparison.OrdinalIgnoreCase);
        // 부재 밸룬 = 세로재 또는 F 계열. P1(플레이트)은 포트가 곧 접속점이라 제외한다.
        bool isMemberItem = !isPlateItem
          && !isRc7DiagonalMember
          && (isVerticalMember || item.StartsWith("F", System.StringComparison.OrdinalIgnoreCase));
        bool isMemberPassItem = isMemberItem || isRc7DiagonalMember;
        // 부재 밸룬은 콜아웃보다 먼저 배치한다. 두 pass로 나뉘어 호출되므로 해당 없는 건 건너뛴다.
        if (isMemberPassItem != memberPass)
          continue;
        // 밸룬 옆 부재 코드(ANGLE A6 → A6). 표를 보지 않아도 규격을 알 수 있다.
        string subLabel = this.GetNotabBalloonSubLabel(bomDesc);
        double subGap = subLabel.Length == 0 ? 0.0 : txt * 0.4;
        double subW = subLabel.Length == 0 ? 0.0 : subLabel.Length * txt * 0.7;

        // ── 한적한 자리 찾기 ──────────────────────────────────────
        // 보정된 앵커에서 8방향으로 뻗어가며 "장애물까지 여유가 큰" 자리를 고른다.
        double[][] dirs = new double[][]
        {
          new double[] { 1, 0 }, new double[] { -1, 0 }, new double[] { 0, 1 }, new double[] { 0, -1 },
          new double[] { 0.7071, 0.7071 }, new double[] { -0.7071, 0.7071 },
          new double[] { 0.7071, -0.7071 }, new double[] { -0.7071, -0.7071 }
        };
        double step = System.Math.Max(radius, txt);
        bool placed = false;
        Point3d ballCenter = Point3d.Origin;
        Extents3d ballBox = new Extents3d(Point3d.Origin, Point3d.Origin);
        bool outwardLeft = false;
        string placeDiag = "none";
        double bestScore = double.MinValue;
        double leaderW = this.GetEnvDouble("PFS_NOTAB_BALLOON_LEADER_W", 2.0, 0.0, 10.0);
        double maxSteps = this.GetEnvDouble("PFS_NOTAB_BALLOON_MAX_STEPS", 4.0, 1.0, 16.0);
        double extSteps = this.GetEnvDouble("PFS_NOTAB_BALLOON_EXT_STEPS", 12.0, 1.0, 32.0);
        // 플레이트가 두 개인 RC9에서는 첫 P1 밸룬이 등록된 뒤 두 번째 P1이 기존 확장 범위를
        // 모두 소진한다. P1에만 탐색 반경을 넓혀 다른 품목의 확정 배치를 보존한다.
        if (isPlateItem)
          extSteps = this.GetEnvDouble("PFS_NOTAB_P1_EXT_STEPS", 24.0, 1.0, 48.0);
        extSteps = System.Math.Max(maxSteps, extSteps);
        int candidateCount = 0, freeCount = 0;
        double maxClearance = double.MinValue;
        Point3d bestAnchor = anchor;
        System.Collections.Generic.Dictionary<string, int> rejBy = new System.Collections.Generic.Dictionary<string, int>();
        string placementTier = "normal";
        bool isRc9P1DownOnly = string.Equals(standardName, "RC9", System.StringComparison.OrdinalIgnoreCase)
          && string.Equals(item, "P1_1", System.StringComparison.OrdinalIgnoreCase);

        // ── 부재 밸룬 전용 배치 (cycle102) ───────────────────────────
        // 사용자 확정 규칙: 부재의 "여유 있는 쪽 끝단" 바로 바깥. 화살표는 그 끝단에 닿고
        // 리더는 짧은 수평 직선 1개. 8방향 전역 탐색은 이 품목에 쓰지 않는다.
        if (isMemberItem)
        {
          double endMinX, endMaxX, endY;
          string geomSource;
          if (isVerticalMember)
          {
            // 세로 기둥은 독립 솔리드가 아니다(cycle102 실측: 7A가 기둥+가로재+플레이트 병합).
            // 따라서 솔리드 상자를 쓰면 서포트 전체 외곽을 찍어 허공이 된다. 포트 기반으로 잡는다.
            // rawAnchor = S2 포트(기둥 자유단) 투영값이라 기둥 축 X를 대표한다.
            // verticalBaseY/TopY = S2 + F2 파라미터×vScale로 재구성한 기둥 상하단이다.
            if (!hasVerticalAnchor)
            {
              PlantOrthoView.FileDiag("PFSNOTABDETAIL balloon-skip key=" + item
                + " reason=member-geometry-unavailable detail=no-vertical-port");
              continue;
            }
            // 실제 단면폭이 아니라 "얇은 기둥 위에 화살표가 닿게" 하는 가독성 하한이다.
            double halfThick = System.Math.Max(txt, radius) / 2.0;
            // 화살표 끝점만 기둥 축에 둔다. halfThick은 밸룬/문자 여백 계산에 계속 사용한다.
            endMinX = rawAnchor.X;
            endMaxX = rawAnchor.X;
            endY = (verticalBaseY + verticalTopY) / 2.0;
            geomSource = "vertical-port axisX=" + this.FormatNumber(rawAnchor.X)
              + " halfThick=" + this.FormatNumber(halfThick)
              + " spanY=" + this.FormatNumber(verticalBaseY) + "~" + this.FormatNumber(verticalTopY);
          }
          else
          {
            if (horizontalMemberMaxX - horizontalMemberMinX <= 1e-6)
            {
              PlantOrthoView.FileDiag("PFSNOTABDETAIL balloon-skip key=" + item
                + " reason=member-geometry-unavailable detail=horizontal-span-degenerate");
              continue;
            }
            endMinX = horizontalMemberMinX;
            endMaxX = horizontalMemberMaxX;
            endY = rawAnchor.Y;
            geomSource = "horizontal-span minX=" + this.FormatNumber(endMinX) + " maxX=" + this.FormatNumber(endMaxX);
          }

          double bestClear = double.MinValue;
          string endDiag = "none";
          // 기둥은 서포트 실루엣 안쪽에 있어 고정 거리로는 좌우 모두 막힌다(cycle102 실측).
          // 같은 수평 방향으로 단계적으로 밀어내되, 자유 후보가 나오는 첫 단계에서 멈춰
          // 리더가 필요 이상 길어지지 않게 한다. F1은 대개 k=0에서 끝난다.
          // 실측 필요 단계: 축 292.5는 우측 8, 311은 6, 342는 4 → 기본 12로 여유를 둔다.
          double memberSteps = this.GetEnvDouble("PFS_NOTAB_MEMBER_BALLOON_MAX_STEPS", 12.0, 0.0, 32.0);
          for (int k = 0; k <= (int)memberSteps && !placed; k++)
          {
            // 밸룬 위치는 기존 gap 기반으로 유지한다. 세로재 리더의 가시성은 아래 작도 시
            // 이 리더 전용 화살표를 축소해 확보하며, 중심을 더 멀리 밀지 않는다.
            double outDist = radius + gap + step * k;
            // 좌 끝단(바깥=왼쪽) / 우 끝단(바깥=오른쪽) 두 후보를 같은 단계에서 모두 평가한다.
            // 한쪽을 먼저 찾았다고 즉시 채택하면 "여유 큰 쪽" 규칙이 깨진다.
            for (int e = 0; e < 2; e++)
            {
              bool toLeftEnd = e == 0;
              double endX = toLeftEnd ? endMinX : endMaxX;
              // 화살표 시작점은 k와 무관하게 끝단에 고정된다. 멀어지는 것은 원 중심뿐이다.
              Point3d endAnchor = new Point3d(endX, endY, 0.0);
              double cx = toLeftEnd ? endX - outDist : endX + outDist;
              Point3d c = new Point3d(cx, endY, 0.0);

              double bMinX = toLeftEnd ? cx - radius - subGap - subW : cx - radius;
              double bMaxX = toLeftEnd ? cx + radius : cx + radius + subGap + subW;
              double half = System.Math.Max(radius, txt / 2.0);
              Extents3d box = new Extents3d(new Point3d(bMinX, endY - half, 0.0), new Point3d(bMaxX, endY + half, 0.0));
              candidateCount++;
              double clearance = placer.ClearanceTo(box, isVerticalMember, isVerticalMember);
              maxClearance = System.Math.Max(maxClearance, clearance);
              if (!placer.WithinBounds(box))
              {
                int oob;
                rejBy.TryGetValue("oob", out oob);
                rejBy["oob"] = oob + 1;
                continue;
              }
              Point3d touchPt = new Point3d(toLeftEnd ? cx + radius : cx - radius, endY, 0.0);
              string rejEnd;
              // 세로재 밸룬만 자신의 기둥 상자를 제외한다. support 외 상자·기존 콜아웃 충돌은 계속 금지한다.
              if (!placer.IsBalloonFree(box, endAnchor, touchPt, out rejEnd, isVerticalMember, false, isVerticalMember))
              {
                int rejected;
                rejBy.TryGetValue(rejEnd, out rejected);
                rejBy[rejEnd] = rejected + 1;
                continue;
              }
              freeCount++;
              // 같은 단계에서 여유가 큰 쪽 채택. 동점이면 우측.
              if (clearance > bestClear || (System.Math.Abs(clearance - bestClear) < 1e-9 && !toLeftEnd))
              {
                bestClear = clearance;
                ballCenter = c; ballBox = box; bestAnchor = endAnchor; outwardLeft = toLeftEnd; placed = true;
                placementTier = "member-end";
                endDiag = "tier=member-end end=" + (toLeftEnd ? "left" : "right")
                  + " k=" + k + " dist=" + this.FormatNumber(outDist)
                  + " clear=" + this.FormatNumber(clearance)
                  + " geom=" + geomSource;
              }
            }
          }
          placeDiag = endDiag;
          anchorAdjust = isVerticalMember ? "vertical-end" : "horizontal-end";

          if (!placed)
          {
            System.Text.StringBuilder endRej = new System.Text.StringBuilder();
            foreach (System.Collections.Generic.KeyValuePair<string, int> kv in rejBy)
            { if (endRej.Length > 0) endRej.Append(","); endRej.Append(kv.Key).Append("=").Append(kv.Value); }
            // 폴백 없이 생략한다(규칙 R2). 잘못된 방향의 밸룬보다 없는 편이 낫다.
            PlantOrthoView.FileDiag("PFSNOTABDETAIL balloon-skip key=" + item
              + " reason=member-end-no-space cand=" + candidateCount + " free=" + freeCount
              + " maxClear=" + this.FormatNumber(maxClearance)
              + " geom=" + geomSource + " rejBy{" + endRej.ToString() + "}");
            continue;
          }
        }

        // RC9 우측 P1은 F3·치수·기존 밸룬을 모두 피한 하향 배치만 허용한다.
        // 자유 후보가 없더라도 일반 탐색이나 리더 교차 폴백으로 되돌아가지 않는다.
        if (isRc9P1DownOnly)
        {
          // 먼저 순수 하향을 시도하고, 막힌 경우에만 우측 플레이트 바깥으로 조금씩 벗어난다.
          // 모든 후보는 기존의 엄격한 상자/리더 교차 검사를 그대로 적용한다.
          double[] outwardSteps = new double[] { 0.0, 0.25, 0.5, 0.75 };
          for (double stepIndex = 0.0; stepIndex <= maxSteps && !placed; stepIndex++)
          {
            double dist = gap + radius + step * stepIndex;
            for (int outwardIndex = 0; outwardIndex < outwardSteps.Length && !placed; outwardIndex++)
            {
              double outward = step * outwardSteps[outwardIndex];
              Point3d c = new Point3d(anchor.X + outward, anchor.Y - dist, 0.0);
              double bMinX = c.X - radius - subGap - subW;
              double bMaxX = c.X + radius;
              double half = System.Math.Max(radius, txt / 2.0);
              Extents3d box = new Extents3d(new Point3d(bMinX, c.Y - half, 0.0), new Point3d(bMaxX, c.Y + half, 0.0));
              candidateCount++;
              double clearance = placer.ClearanceTo(box);
              maxClearance = System.Math.Max(maxClearance, clearance);
              if (!placer.WithinBounds(box))
              {
                int oob;
                rejBy.TryGetValue("oob", out oob);
                rejBy["oob"] = oob + 1;
                continue;
              }

              Vector3d dir = c - anchor;
              Point3d touchPt = c - dir.GetNormal() * radius;
              string rej;
              if (!placer.IsBalloonFree(box, anchor, touchPt, out rej, false, true))
              {
                int rejected;
                rejBy.TryGetValue(rej, out rejected);
                rejBy[rej] = rejected + 1;
                continue;
              }

              freeCount++;
              ballCenter = c; ballBox = box; bestAnchor = anchor; outwardLeft = false; placed = true;
              placementTier = "rc9-p1-down";
              placeDiag = "tier=rc9-p1-down dir=(" + this.FormatNumber(outward) + ",-1) dist=" + this.FormatNumber(dir.Length)
                + " clear=" + this.FormatNumber(clearance);
            }
          }

          if (!placed)
          {
            System.Text.StringBuilder rc9Rej = new System.Text.StringBuilder();
            foreach (System.Collections.Generic.KeyValuePair<string, int> kv in rejBy)
            { if (rc9Rej.Length > 0) rc9Rej.Append(","); rc9Rej.Append(kv.Key).Append("=").Append(kv.Value); }
            PlantOrthoView.FileDiag("PFSNOTABDETAIL balloon-skip key=" + item
              + " reason=rc9-p1-down-no-space cand=" + candidateCount + " free=" + freeCount
              + " maxClear=" + this.FormatNumber(maxClearance) + " rejBy{" + rc9Rej.ToString() + "}");
            continue;
          }
        }

        // 1단계는 기존 거리 범위를 모두 평가해 짧은 리더 선호를 보존한다.
        // 2단계는 1단계가 완전히 실패했을 때만 시작하고, 처음 자유 후보가 나온 거리 링만
        // 평가한다. 따라서 clearance 점수가 먼 거리를 과도하게 선호하지 않는다.
        for (int tier = 0; tier < 2 && !placed && !isRc9P1DownOnly; tier++)
        {
          double firstStep = tier == 0 ? 0.0 : maxSteps + 1.0;
          double lastStep = tier == 0 ? maxSteps : extSteps;
          for (double stepIndex = firstStep; stepIndex <= lastStep && !(tier == 1 && placed); stepIndex++)
          {
            double dist = gap + radius + step * stepIndex;
            bool freeAtThisDistance = false;
            for (int d = 0; d < dirs.Length; d++)
            {
              double cx = anchor.X + dirs[d][0] * dist;
              double cy = anchor.Y + dirs[d][1] * dist;
              Point3d c = new Point3d(cx, cy, 0.0);
              bool toLeft = cx < anchor.X || (System.Math.Abs(cx - anchor.X) < 1e-9 && dirs[d][0] <= 0);

              double bMinX = toLeft ? cx - radius - subGap - subW : cx - radius;
              double bMaxX = toLeft ? cx + radius : cx + radius + subGap + subW;
              double half = System.Math.Max(radius, txt / 2.0);
              Extents3d box = new Extents3d(new Point3d(bMinX, cy - half, 0.0), new Point3d(bMaxX, cy + half, 0.0));
              candidateCount++;
              double clearance = placer.ClearanceTo(box);
              maxClearance = System.Math.Max(maxClearance, clearance);
              if (!placer.WithinBounds(box)) continue;

              Vector3d dir = c - anchor;
              if (dir.Length < 1e-9) continue;
              Point3d touchPt = c - dir.GetNormal() * radius;
              string rej;
              // RC9의 두 번째 P1만 정상 탐색이 완전히 막힌 뒤 기존 콜아웃 리더 교차를 허용한다.
              // 밸룬 상자·support·치수 상자 충돌은 이 경로에서도 계속 금지한다.
              bool p1LeaderFallback = isPlateItem && tier == 1 && freeCount == 0;
              if (!placer.IsBalloonFree(box, anchor, touchPt, out rej, false, p1LeaderFallback))
              {
                int rejected;
                rejBy.TryGetValue(rej, out rejected);
                rejBy[rej] = rejected + 1;
                continue;
              }
              freeCount++;
              freeAtThisDistance = true;

              double score = clearance - dir.Length * leaderW;
              if (score > bestScore)
              {
                bestScore = score; ballCenter = c; ballBox = box; bestAnchor = anchor; outwardLeft = toLeft; placed = true;
                placementTier = p1LeaderFallback ? "p1-leader-fallback" : (tier == 0 ? "normal" : "extended");
                placeDiag = "tier=" + placementTier + " dir=" + d + " dist=" + this.FormatNumber(dir.Length)
                  + " clear=" + this.FormatNumber(clearance)
                  + " score=" + this.FormatNumber(score);
              }
            }
            // 확장 단계는 최초 자유 거리 링의 후보만 비교한다.
            if (tier == 1 && freeAtThisDistance) break;
          }
        }

        if (!placed)
        {
          System.Text.StringBuilder rejSummary = new System.Text.StringBuilder();
          foreach (System.Collections.Generic.KeyValuePair<string, int> kv in rejBy)
          { if (rejSummary.Length > 0) rejSummary.Append(","); rejSummary.Append(kv.Key).Append("=").Append(kv.Value); }
          PlantOrthoView.FileDiag("PFSNOTABDETAIL balloon-skip key=" + item + " reason=no-space perimeter"
            + " cand=" + candidateCount + " free=" + freeCount
            + " maxClear=" + this.FormatNumber(maxClearance) + " tier=extended extended=true rejBy{" + rejSummary.ToString() + "}");
          continue;
        }

        anchor = bestAnchor;

        bool left = outwardLeft;
        string diag = placeDiag;
        // 화살표는 보정된 앵커에 고정하고, 리더는 꺾지 않고 원주까지 직선 1개.
        Vector3d toCenter = ballCenter - anchor;
        if (toCenter.Length < 1e-9) toCenter = Vector3d.XAxis;
        Point3d touch = ballCenter - toCenter.GetNormal() * radius;

        try
        {
          Circle circle = new Circle(ballCenter, Vector3d.ZAxis, radius);
          if (layerId != ObjectId.Null) circle.LayerId = layerId;
          circle.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex((Autodesk.AutoCAD.Colors.ColorMethod)195, (short)3);
          layoutBtr.AppendEntity(circle);
          tr.AddNewlyCreatedDBObject(circle, true);

          MText mt = new MText();
          mt.Contents = bomItem;
          mt.TextHeight = txt;
          mt.Attachment = AttachmentPoint.MiddleCenter;
          mt.Location = ballCenter;
          if (textStyleId != ObjectId.Null) mt.TextStyleId = textStyleId;
          if (layerId != ObjectId.Null) mt.LayerId = layerId;
          mt.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex((Autodesk.AutoCAD.Colors.ColorMethod)195, (short)3);
          layoutBtr.AppendEntity(mt);
          tr.AddNewlyCreatedDBObject(mt, true);

          if (subLabel.Length > 0)
          {
            MText sub = new MText();
            sub.Contents = subLabel;
            sub.TextHeight = txt;
            // 코드는 앵커 반대편(바깥)으로. 리더가 원을 지나 문자를 통과하지 않는다.
            sub.Attachment = left ? AttachmentPoint.MiddleRight : AttachmentPoint.MiddleLeft;
            sub.Location = new Point3d(
              left ? ballCenter.X - radius - subGap : ballCenter.X + radius + subGap,
              ballCenter.Y, 0.0);
            if (textStyleId != ObjectId.Null) sub.TextStyleId = textStyleId;
            if (layerId != ObjectId.Null) sub.LayerId = layerId;
            sub.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex((Autodesk.AutoCAD.Colors.ColorMethod)195, (short)3);
            layoutBtr.AppendEntity(sub);
            tr.AddNewlyCreatedDBObject(sub, true);
          }

          Leader leader = new Leader();
          leader.AppendVertex(anchor);
          leader.AppendVertex(touch);
          leader.HasArrowHead = true;
          double leaderArrowSize = this.GetEnvDouble("PFS_NOTAB_DIM_ARR", 10.0, 0.5, 50.0);
          if (isVerticalMember)
          {
            // 밸룬 중심은 유지하고, 화살촉이 원~기둥축 리더를 덮지 않게 노출 길이의 절반 미만으로 제한한다.
            leaderArrowSize = System.Math.Min(leaderArrowSize, System.Math.Max(0.5, anchor.DistanceTo(touch) * 0.45));
          }
          leader.Dimasz = leaderArrowSize;
          if (layerId != ObjectId.Null) leader.LayerId = layerId;
          leader.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex((Autodesk.AutoCAD.Colors.ColorMethod)195, (short)1);
          layoutBtr.AppendEntity(leader);
          tr.AddNewlyCreatedDBObject(leader, true);

          // 등록은 원만이 아니라 코드 문자까지 포함한 상자로 한다.
          placer.CommitBalloonBox(anchor, touch, ballBox);
          PlantOrthoView.FileDiag("PFSNOTABDETAIL balloon-draw key=" + item + " item=" + bomItem
            + " sub='" + subLabel + "' adjust=" + anchorAdjust
            + " rawAnchor=" + this.FormatPoint(rawAnchor) + " anchor=" + this.FormatPoint(anchor) + " center=" + this.FormatPoint(ballCenter)
            + " r=" + this.FormatNumber(radius)
            + " leaderArrow=" + this.FormatNumber(leaderArrowSize)
          + " box=" + this.FormatExtents(ballBox)
            + " side=" + (left ? "left" : "right") + " cand=" + candidateCount
            + " free=" + freeCount + " maxClear=" + this.FormatNumber(maxClearance) + " tier=" + placementTier + " extended=" + (placementTier == "extended" ? "true" : "false") + " " + diag);
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL balloon 작도 예외 key=" + item + ": " + ex.GetType().Name + ": " + ex.Message);
        }
      }
      }
      finally
      {
        placer.SetLeaderExemptOwner(string.Empty);
      }
    }

    // [MEASURE cycle94] 오쏘 BOM·밸룬 자산의 무탭 이식 가능성 계측 (로그 전용, 작도 변경 없음).
    // M1=BOM 행을 원본 DB에서 얻을 수 있는가 / M3=TaggingPoints 앵커 원천 / ITEM↔앵커 키 차집합.
    private void MeasureNotabBomBalloonSources(ObjectId supportId)
    {
      if (supportId == ObjectId.Null)
      {
        PlantOrthoView.FileDiag("MEASURE bom-source skip: supportId null");
        return;
      }

      System.Collections.Generic.List<string> itemKeys = new System.Collections.Generic.List<string>();
      try
      {
        BOMs bom = new BOMs(supportId, new System.Collections.Generic.List<AttachmentInfo>());
        System.Collections.Generic.List<string[]> rows = bom.ContentsByDesignStd(this.GetNotabBomDesignStd());
        PlantOrthoView.FileDiag("MEASURE bom-source std=" + (bom.StandardName ?? "null")
          + " designStd=" + (string.IsNullOrWhiteSpace(s_isoDesignStd) ? "(empty)" : s_isoDesignStd)
          + " rowCount=" + (rows == null ? -1 : rows.Count));
        if (rows != null)
        {
          for (int i = 0; i < rows.Count && i < 40; i++)
          {
            string[] r = rows[i] ?? new string[0];
            // 무탭 경로의 BOM 행에는 헤더가 없다(오쏘만 RemoveAt(0)로 제거). 행 0도 데이터다.
            if (r.Length > 0 && !string.IsNullOrWhiteSpace(r[0]))
            {
              itemKeys.Add(r[0].Trim());
              s_isoBomRows.Add(new NotabBomRow
              {
                Item = r[0].Trim(),
                Category = r.Length > 1 ? r[1] : string.Empty,
                Description = r.Length > 2 ? r[2] : string.Empty,
                Material = r.Length > 3 ? r[3] : string.Empty,
                QuantityOrLength = r.Length > 4 ? r[4] : string.Empty,
                Remark = r.Length > 5 ? r[5] : string.Empty
              });
            }
            PlantOrthoView.FileDiag("MEASURE bom-row i=" + i + " cols=" + r.Length + " { " + string.Join(" | ", r) + " }");
          }
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("MEASURE bom-source 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      // M3: TaggingPoints(밸룬 앵커)를 원본 DB에서 생성해 WCS로 스냅샷한다.
      // 포트 이름(S1~S4)과 마크(F1~F4)는 1:1이 아니며, 타입별 메서드가 포트 인덱스를 개별 지정한다.
      try
      {
        Database db = supportId.Database;
        if (db == null)
        {
          PlantOrthoView.FileDiag("MEASURE balloon-source skip: database null");
          return;
        }

        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          Part part = tr.GetObject(supportId, OpenMode.ForRead, false) as Part;
          if (part == null)
          {
            PlantOrthoView.FileDiag("MEASURE balloon-source skip: not Part id=" + supportId);
            tr.Commit();
            return;
          }

          PortCollection ports = part.GetPorts((PortType)7);
          Extents3d ext = ((Entity)part).GeometricExtents;
          BOMs bom2 = new BOMs(supportId, new System.Collections.Generic.List<AttachmentInfo>());
          StandardSupport ss = new StandardSupport(ext, bom2.IsBaseplate, bom2.SupportParams,
            s_isoPipeAxis, Commands.ViewType.Front, ports, Matrix3d.Identity);
          string stdName = this.GetNotabStandardName();
          ss.StandardInformation(stdName);

          System.Collections.Generic.Dictionary<string, Point3d[]> tp = ss.TaggingPoints;
          PlantOrthoView.FileDiag("MEASURE balloon-source std=" + stdName
            + " portCount=" + (ports == null ? -1 : ports.Count)
            + " tagCount=" + (tp == null ? -1 : tp.Count)
            + " keys={" + (tp == null ? string.Empty : string.Join("|", tp.Keys)) + "}");

          if (tp != null)
          {
            foreach (System.Collections.Generic.KeyValuePair<string, Point3d[]> kv in tp)
            {
              Point3d[] pts = kv.Value ?? new Point3d[0];
              // p0만 보관한다. p1은 p0+(50,50,z=0) 방향 힌트일 뿐 실좌표가 아니다(cycle94 실측).
              if (pts.Length > 0)
                s_isoBalloonAnchors.Add(new NotabBalloonAnchor { Item = kv.Key, WcsP0 = pts[0] });
              PlantOrthoView.FileDiag("MEASURE balloon-anchor key=" + kv.Key + " pts=" + pts.Length
                + (pts.Length > 0 ? " p0=" + this.FormatPoint(pts[0]) : string.Empty)
                + (pts.Length > 1 ? " p1=" + this.FormatPoint(pts[1]) : string.Empty));
            }

            // ITEM ↔ 앵커 키 차집합. 어느 쪽에만 있는지가 이식 설계의 입력이다.
            System.Collections.Generic.List<string> bomOnly = new System.Collections.Generic.List<string>();
            foreach (string item in itemKeys)
            {
              bool matched = false;
              foreach (string k in tp.Keys)
                if (k == item || k.StartsWith(item + "_")) { matched = true; break; }
              if (!matched) bomOnly.Add(item);
            }
            System.Collections.Generic.List<string> anchorOnly = new System.Collections.Generic.List<string>();
            foreach (string k in tp.Keys)
            {
              bool matched = false;
              foreach (string item in itemKeys)
                if (k == item || k.StartsWith(item + "_")) { matched = true; break; }
              if (!matched) anchorOnly.Add(k);
            }
            PlantOrthoView.FileDiag("MEASURE balloon-diff items={" + string.Join("|", itemKeys.ToArray()) + "}"
              + " bomOnly={" + string.Join("|", bomOnly.ToArray()) + "}"
              + " anchorOnly={" + string.Join("|", anchorOnly.ToArray()) + "}");
          }

          tr.Commit();
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("MEASURE balloon-source 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private void CaptureIsoSupportPorts(ObjectId supportId)
    {
      s_isoSupportPorts.Clear();
      if (supportId == ObjectId.Null)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL support-port skip: supportId null");
        return;
      }

      try
      {
        Database db = supportId.Database;
        if (db == null)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL support-port skip: database null id=" + supportId);
          return;
        }

        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          Part part = tr.GetObject(supportId, OpenMode.ForRead, false) as Part;
          if (part == null)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL support-port skip: not Part id=" + supportId);
            tr.Commit();
            return;
          }

          PortCollection ports = part.GetPorts((PortType)7);
          if (ports == null)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL support-port skip: ports null id=" + supportId);
            tr.Commit();
            return;
          }

          int index = 0;
          foreach (Port port in (PnP3dCollection)ports)
          {
            NotabSupportPortSnapshot snapshot = new NotabSupportPortSnapshot();
            snapshot.Index = index++;
            snapshot.Name = string.IsNullOrWhiteSpace(port.Name) ? "(unnamed)" : port.Name;
            snapshot.Wcs = port.Position;
            s_isoSupportPorts.Add(snapshot);
            PlantOrthoView.FileDiag("PFSNOTABDETAIL support-port wcs index=" + snapshot.Index + " name=" + snapshot.Name + " point=" + this.FormatPoint(snapshot.Wcs));
          }
          PlantOrthoView.FileDiag("PFSNOTABDETAIL support-port count=" + s_isoSupportPorts.Count + " id=" + supportId);
          tr.Commit();
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL support-port capture 예외 id=" + supportId + ": " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private void LogNotabSupportPortProjection(Viewport vp)
    {
      if (vp == null || s_isoSupportPorts.Count == 0)
        return;

      for (int i = 0; i < s_isoSupportPorts.Count; i++)
      {
        NotabSupportPortSnapshot snapshot = s_isoSupportPorts[i];
        Point3d paper = this.NotabProjectWcsToPaper(vp, snapshot.Wcs);
        PlantOrthoView.FileDiag("PFSNOTABDETAIL support-port paper index=" + snapshot.Index + " name=" + snapshot.Name
          + " wcs=" + this.FormatPoint(snapshot.Wcs) + " paper=" + this.FormatPoint(paper)
          + (snapshot.Index == 1 ? " role=F2-candidate" : string.Empty));
      }
    }

    private void CaptureIsoSupportProfile(ObjectId supportId)
    {
      s_isoSupportBI = string.Empty;
      s_isoSupportProfile = string.Empty;
      s_isoSupportDesignation = string.Empty;
      s_isoSupportDesignations.Clear();
      s_isoSupportParams.Clear();
      s_isoSupportProfileHeight = string.Empty;

      if (supportId == ObjectId.Null)
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE profile skip: supportId null");
        return;
      }

      try
      {
        PSUtil ps = Commands.PSUtil;
        if (ps == null)
          ps = new PSUtil();
        if (ps == null)
        {
          PlantOrthoView.FileDiag("PFSVBISOCLONE profile skip: PSUtil null");
          return;
        }

        System.Collections.Generic.Dictionary<string, string> dims = ps.GetSupportDimension(supportId);
        if (dims != null)
        {
          s_isoSupportParams = new System.Collections.Generic.Dictionary<string, string>(dims, System.StringComparer.OrdinalIgnoreCase);
          System.Text.StringBuilder sb = new System.Text.StringBuilder();
          int n = 0;
          foreach (System.Collections.Generic.KeyValuePair<string, string> kv in dims)
          {
            sb.Append(kv.Key).Append('=').Append(kv.Value).Append(" | ");
            if (++n >= 80)
            {
              sb.Append("...(truncated)");
              break;
            }
          }
          PlantOrthoView.FileDiag("PFSVBISOCLONE support params dump id=" + supportId + " count=" + dims.Count + " { " + sb.ToString() + "}");
        }
        if (dims == null || !dims.ContainsKey("BI") || string.IsNullOrWhiteSpace(dims["BI"]))
        {
          PlantOrthoView.FileDiag("PFSVBISOCLONE profile BI 없음 id=" + supportId + " config fallback 검사");
          this.CaptureIsoSupportProfileFromConfig();
          return;
        }

        string bi = dims["BI"].Trim().Replace("_", string.Empty);
        string profile;
        string height;
        string designation;
        if (!this.TryBuildNotabSupportDesignation(bi, out profile, out height, out designation))
          return;
        s_isoSupportBI = bi;
        s_isoSupportProfile = profile;
        s_isoSupportDesignation = designation;
        s_isoSupportDesignations.Add(s_isoSupportDesignation);
        s_isoSupportProfileHeight = height;
        PlantOrthoView.FileDiag("PFSVBISOCLONE profile BI=" + s_isoSupportBI + " profile=" + s_isoSupportProfile + " height=" + s_isoSupportProfileHeight + " designation=" + s_isoSupportDesignation + " designations count=" + s_isoSupportDesignations.Count + " { " + s_isoSupportDesignation + " }");
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE profile 예외 id=" + supportId + ": " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private void NotabBomSpike(ObjectId supportId)
    {
      try
      {
        BOMs bom = new BOMs(supportId, new System.Collections.Generic.List<AttachmentInfo>());
        System.Collections.Generic.List<string[]> rows = bom.ContentsByDesignStd(this.GetNotabBomDesignStd());
        PlantOrthoView.FileDiag("PFSNOTABDETAIL bom spike std=" + (bom.StandardName ?? "null") + " rowCount=" + (rows == null ? -1 : rows.Count));
        if (rows != null)
        {
          for (int i = 0; i < rows.Count && i < 40; i++)
            PlantOrthoView.FileDiag("PFSNOTABDETAIL bom row[" + i + "] cols=" + (rows[i] == null ? 0 : rows[i].Length) + " { " + (rows[i] == null ? "" : string.Join(" | ", rows[i])) + " }");

        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL bom spike 예외: " + ex.GetType().Name + ": " + ex.Message + " id=" + supportId);
      }
    }

    private string GetNotabBomDesignStd()
    {
      if (!string.IsNullOrWhiteSpace(s_isoDesignStd))
        return s_isoDesignStd;

      PlantOrthoView.FileDiag("PFSVBISOCLONE BOM DesignStd empty; legacy HANTEC fallback applied");
      return "HANTEC";
    }
    private void AugmentDesignationsFromBom(ObjectId supportId)
    {
      if (supportId == ObjectId.Null)
        return;

      try
      {
        BOMs bom = new BOMs(supportId, new System.Collections.Generic.List<AttachmentInfo>());
        System.Collections.Generic.List<string[]> rows = bom.ContentsByDesignStd(this.GetNotabBomDesignStd());
        if (rows == null || rows.Count == 0)
        {
          PlantOrthoView.FileDiag("PFSVBISOCLONE bom-augment skip: rows empty id=" + supportId);
          return;
        }

        System.Collections.Generic.List<string> angles = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> channels = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> others = new System.Collections.Generic.List<string>();
        System.Collections.Generic.HashSet<string> seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (string[] r in rows)
        {
          if (r == null || r.Length < 3 || string.IsNullOrWhiteSpace(r[2]))
            continue;

          string memberValue = System.Text.RegularExpressions.Regex.Replace(r[2].Trim(), "\\s+", " ");
          if (!seen.Add(memberValue))
            continue;

          string upper = memberValue.ToUpperInvariant();
          if (upper.StartsWith("ANGLE")) angles.Add(memberValue);
          else if (upper.StartsWith("CHANNEL")) channels.Add(memberValue);
          else others.Add(memberValue);
        }

        System.Collections.Generic.List<string> ordered = new System.Collections.Generic.List<string>();
        ordered.AddRange(angles);
        ordered.AddRange(channels);
        ordered.AddRange(others);

        System.Collections.Generic.List<string> bomDesignations = new System.Collections.Generic.List<string>();
        foreach (string memberValue in ordered)
        {
          string bi = StandardSupport.BeamProfileBI(memberValue);
          if (string.IsNullOrWhiteSpace(bi))
          {
            PlantOrthoView.FileDiag("PFSVBISOCLONE bom-augment 미매핑 member=" + memberValue);
            continue;
          }

          string profile;
          string height;
          string designation;
          if (this.TryBuildNotabSupportDesignation(bi, out profile, out height, out designation) && !string.IsNullOrWhiteSpace(designation))
            bomDesignations.Add(designation);
          else
            PlantOrthoView.FileDiag("PFSVBISOCLONE bom-augment build 실패 member=" + memberValue + " bi=" + bi);
        }

        if (bomDesignations.Count == 0)
        {
          PlantOrthoView.FileDiag("PFSVBISOCLONE bom-augment skip: designation 0 id=" + supportId);
          return;
        }

        System.Collections.Generic.HashSet<string> current = new System.Collections.Generic.HashSet<string>(s_isoSupportDesignations, System.StringComparer.OrdinalIgnoreCase);
        System.Collections.Generic.HashSet<string> replacement = new System.Collections.Generic.HashSet<string>(bomDesignations, System.StringComparer.OrdinalIgnoreCase);
        bool differ = bomDesignations.Count != s_isoSupportDesignations.Count || !current.SetEquals(replacement);
        if (differ)
        {
          string before = string.Join(" | ", s_isoSupportDesignations);
          s_isoSupportDesignations.Clear();
          s_isoSupportDesignations.AddRange(bomDesignations);
          PlantOrthoView.FileDiag("PFSVBISOCLONE bom-augment 교체 before={" + before + "} after={" + string.Join(" | ", bomDesignations) + "} (dim기준 BI=" + (string.IsNullOrWhiteSpace(s_isoSupportBI) ? "empty" : s_isoSupportBI) + " 불변)");
        }
        else
        {
          PlantOrthoView.FileDiag("PFSVBISOCLONE bom-augment 동일 유지 { " + string.Join(" | ", bomDesignations) + " }");
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE bom-augment 예외 id=" + supportId + ": " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private void CaptureIsoSupportProfileFromConfig()
    {
      try
      {
        string std = this.GetNotabStandardName();
        string[] bis = this.GetNotabTypeConfig(std).MemberBIs;
        if (bis == null || bis.Length == 0)
        {
          PlantOrthoView.FileDiag("PFSVBISOCLONE profile config skip: MemberBIs 없음 std=" + std);
          return;
        }

        System.Text.StringBuilder joined = new System.Text.StringBuilder();
        for (int i = 0; i < bis.Length; i++)
        {
          string profile;
          string height;
          string designation;
          if (!this.TryBuildNotabSupportDesignation(bis[i], out profile, out height, out designation))
          {
            PlantOrthoView.FileDiag("PFSVBISOCLONE profile config skip BI=" + (bis[i] ?? "null") + " std=" + std);
            continue;
          }

          if (string.IsNullOrWhiteSpace(s_isoSupportBI))
          {
            s_isoSupportBI = bis[i].Trim().Replace("_", string.Empty);
            s_isoSupportProfile = profile;
            s_isoSupportProfileHeight = height;
            s_isoSupportDesignation = designation;
          }
          s_isoSupportDesignations.Add(designation);
          if (joined.Length > 0)
            joined.Append(" | ");
          joined.Append(designation);
        }

        PlantOrthoView.FileDiag("PFSVBISOCLONE profile designations count=" + s_isoSupportDesignations.Count + " { " + joined.ToString() + " } std=" + std);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE profile config 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private bool TryBuildNotabSupportDesignation(string bi, out string profile, out string height, out string designation)
    {
      profile = string.Empty;
      height = string.Empty;
      designation = string.Empty;
      try
      {
        if (string.IsNullOrWhiteSpace(bi))
        {
          PlantOrthoView.FileDiag("PFSVBISOCLONE profile skip: BI empty");
          return false;
        }

        string normalizedBi = bi.Trim().Replace("_", string.Empty);
        profile = StandardSupport.DetailProfile(normalizedBi);
        if (string.IsNullOrWhiteSpace(profile))
        {
          PlantOrthoView.FileDiag("PFSVBISOCLONE profile skip: DetailProfile empty BI=" + normalizedBi);
          return false;
        }

        height = this.GetFirstProfileToken(profile);
        string prefix = this.GetSupportProfilePrefix(normalizedBi);
        designation = string.IsNullOrWhiteSpace(prefix) ? profile.Replace("x", "×") : prefix + "-" + profile.Replace("x", "×");
        return !string.IsNullOrWhiteSpace(designation);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE profile designation 예외 BI=" + (bi ?? "null") + ": " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
    }

    private string GetFirstProfileToken(string profile)
    {
      if (string.IsNullOrWhiteSpace(profile))
        return string.Empty;

      try
      {
        string normalized = profile.Replace('×', 'x');
        string[] parts = normalized.Split(new char[] { 'x', 'X' }, System.StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0].Trim() : string.Empty;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE profile height parse 예외 profile=" + profile + ": " + ex.GetType().Name + ": " + ex.Message);
        return string.Empty;
      }
    }

    private string GetSupportProfilePrefix(string bi)
    {
      if (string.IsNullOrWhiteSpace(bi))
        return string.Empty;

      switch (bi.Trim()[0])
      {
        case '1': return "L";
        case '2': return "C";
        case '3': return "H";
        case '4': return "FB";
        default:
          PlantOrthoView.FileDiag("PFSVBISOCLONE profile prefix unknown BI=" + bi);
          return string.Empty;
      }
    }

    private void CaptureIsoPipeLineNo(PSUtil ps, ObjectId pipeId)
    {
      if (pipeId == ObjectId.Null)
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE pipePLN skip: pipeId null");
        return;
      }
      if (ps == null || ps.dl_manager == null)
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE pipePLN skip: dl_manager null");
        return;
      }

      string[] candidates = new string[] { "LineNumberTag", "LineNumber", "Line Number" };
      foreach (string name in candidates)
      {
        try
        {
          System.Collections.Specialized.StringCollection names = new System.Collections.Specialized.StringCollection() { name };
          System.Collections.Specialized.StringCollection props = ps.dl_manager.GetProperties(pipeId, names, true);
          string value = props != null && props.Count > 0 ? props[0] : string.Empty;
          PlantOrthoView.FileDiag("PFSVBISOCLONE pipePLN candidate " + name + "=" + value);
          if (!string.IsNullOrWhiteSpace(value))
          {
            s_isoPipeLineNo = value;
            return;
          }
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSVBISOCLONE pipePLN candidate " + name + " 예외: " + ex.GetType().Name + ": " + ex.Message);
        }
      }
    }

    private Point3d[] GetExtentsCorners(Extents3d ext)
    {
      Point3d min = ext.MinPoint;
      Point3d max = ext.MaxPoint;
      return new Point3d[]
      {
        new Point3d(min.X, min.Y, min.Z),
        new Point3d(max.X, min.Y, min.Z),
        new Point3d(min.X, max.Y, min.Z),
        new Point3d(max.X, max.Y, min.Z),
        new Point3d(min.X, min.Y, max.Z),
        new Point3d(max.X, min.Y, max.Z),
        new Point3d(min.X, max.Y, max.Z),
        new Point3d(max.X, max.Y, max.Z)
      };
    }

    private string RoundPropertyValue(string value)
    {
      double parsed;
      if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed) || double.TryParse(value, out parsed))
        return System.Math.Round(parsed).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
      return value == null ? string.Empty : value;
    }

    private string FormatNumber(double value)
    {
      return System.Math.Abs(value).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }

    private string FormatPoint(Point3d point)
    {
      System.Globalization.CultureInfo ic = System.Globalization.CultureInfo.InvariantCulture;
      return "(" + point.X.ToString("0.##", ic) + "," + point.Y.ToString("0.##", ic) + "," + point.Z.ToString("0.##", ic) + ")";
    }

    private string FormatDimNumber(double value)
    {
      return System.Math.Abs(value).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
    }

    private void EnsureIsoAnnotationResources(Database db, Transaction tr, out ObjectId layerId, out ObjectId textStyleId, out ObjectId dimStyleId)
    {
      layerId = ObjectId.Null;
      textStyleId = ObjectId.Null;
      dimStyleId = db == null ? ObjectId.Null : db.Dimstyle;
      if (db == null || tr == null)
        return;

      LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
      const string layerName = "AUTO_DIM";
      if (lt != null && lt.Has(layerName))
      {
        layerId = lt[layerName];
      }
      else if (lt != null)
      {
        lt.UpgradeOpen();
        LayerTableRecord ltr = new LayerTableRecord();
        ltr.Name = layerName;
        ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 6);
        layerId = lt.Add(ltr);
        tr.AddNewlyCreatedDBObject(ltr, true);
      }

      TextStyleTable tst = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
      const string textStyleName = "RMS_85";
      if (tst != null && tst.Has(textStyleName))
      {
        textStyleId = tst[textStyleName];
      }
      else if (tst != null)
      {
        tst.UpgradeOpen();
        TextStyleTableRecord tstr = new TextStyleTableRecord();
        tstr.Name = textStyleName;
        tstr.FileName = "romans.shx";
        tstr.XScale = 0.85;
        textStyleId = tst.Add(tstr);
        tr.AddNewlyCreatedDBObject(tstr, true);
      }

      DimStyleTable dst = tr.GetObject(db.DimStyleTableId, OpenMode.ForRead) as DimStyleTable;
      if (dst != null && dst.Has("STANDARD"))
        dimStyleId = dst["STANDARD"];
      else
        dimStyleId = db.Dimstyle;
    }

    private void AppendIsoBoundingDimensions(Transaction tr, BlockTableRecord targetMs, ObjectId dimSourceId, ObjectId annotationLayerId, ObjectId dimStyleId)
    {
      if (tr == null || targetMs == null || dimSourceId == ObjectId.Null)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED dim skip: source null");
        return;
      }

      try
      {
        Entity source = tr.GetObject(dimSourceId, OpenMode.ForRead, false) as Entity;
        if (source == null)
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED dim skip: source entity null id=" + dimSourceId);
          return;
        }

        Extents3d paperExt = source.GeometricExtents;
        Extents3d verticalExt = paperExt;
        Extents3d supportExt;
        double pipeCenterX = double.NaN;
        if (this.TryGetIsoSupportPaperExtents(tr, source as BlockReference, out supportExt, out pipeCenterX))
        {
          verticalExt = supportExt;
        }
        else
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED supportExt fallback=paperExt " + paperExt.MinPoint + "~" + paperExt.MaxPoint);
        }
        double dimSize = this.ComputeIsoDimensionSize();
        RotatedDimension dimH;
        RotatedDimension dimV;
        PSUtil.CreateHorizontalDimension(paperExt, Matrix3d.Identity, dimStyleId, paperExt.MaxPoint.Y, out dimH);
        PSUtil.CreateVerticalDimension(verticalExt, Matrix3d.Identity, dimStyleId, verticalExt.MaxPoint.X, out dimV);
        if (dimH != null)
        {
          dimH.DimensionStyle = dimStyleId;
          this.ApplyIsoDimensionOverrides(dimH, dimSize, "dimH");
          dimH.DimensionText = this.FormatDimNumber(s_isoRealWidth);
          if (annotationLayerId != ObjectId.Null)
            dimH.LayerId = annotationLayerId;
          targetMs.AppendEntity(dimH);
          tr.AddNewlyCreatedDBObject(dimH, true);
          this.LogIsoDimensionExtents(dimH, "dimH", dimSize);
        }

        double baseMinX = verticalExt.MinPoint.X;
        double baseMaxX = verticalExt.MaxPoint.X;
        double baseMaxY = verticalExt.MaxPoint.Y;
        double baseWidth = baseMaxX - baseMinX;
        if (!double.IsNaN(pipeCenterX) && pipeCenterX > baseMinX + 1e-6 && pipeCenterX < baseMaxX - 1e-6 && baseWidth > 1e-6)
        {
          double leftReal = s_isoRealWidth * (pipeCenterX - baseMinX) / baseWidth;
          double rightReal = s_isoRealWidth - leftReal;
          double dimLineY = baseMaxY + 50.0 + dimSize * 2.0;
          RotatedDimension dimLeft = PSUtil.CreateHorizontalDimension(new Point3d(baseMinX, baseMaxY, 0.0), new Point3d(pipeCenterX, baseMaxY, 0.0), new Point3d(baseMinX, dimLineY, 0.0), Matrix3d.Identity, dimStyleId);
          RotatedDimension dimRight = PSUtil.CreateHorizontalDimension(new Point3d(pipeCenterX, baseMaxY, 0.0), new Point3d(baseMaxX, baseMaxY, 0.0), new Point3d(pipeCenterX, dimLineY, 0.0), Matrix3d.Identity, dimStyleId);
          if (dimLeft != null)
          {
            dimLeft.DimensionStyle = dimStyleId;
            this.ApplyIsoDimensionOverrides(dimLeft, dimSize, "dimSplitL");
            dimLeft.DimensionText = this.FormatDimNumber(leftReal);
            if (annotationLayerId != ObjectId.Null)
              dimLeft.LayerId = annotationLayerId;
            targetMs.AppendEntity(dimLeft);
            tr.AddNewlyCreatedDBObject(dimLeft, true);
            this.LogIsoDimensionExtents(dimLeft, "dimSplitL", dimSize);
          }
          if (dimRight != null)
          {
            dimRight.DimensionStyle = dimStyleId;
            this.ApplyIsoDimensionOverrides(dimRight, dimSize, "dimSplitR");
            dimRight.DimensionText = this.FormatDimNumber(rightReal);
            if (annotationLayerId != ObjectId.Null)
              dimRight.LayerId = annotationLayerId;
            targetMs.AppendEntity(dimRight);
            tr.AddNewlyCreatedDBObject(dimRight, true);
            this.LogIsoDimensionExtents(dimRight, "dimSplitR", dimSize);
          }
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED dimSplit centerX=" + this.FormatNumber(pipeCenterX) + " baseMinX=" + this.FormatNumber(baseMinX) + " baseMaxX=" + this.FormatNumber(baseMaxX) + " left=" + this.FormatNumber(leftReal) + " right=" + this.FormatNumber(rightReal) + " total=" + this.FormatNumber(s_isoRealWidth));
        }
        else
        {
          string reason = double.IsNaN(pipeCenterX) ? "centerX=NaN" : (baseWidth <= 1e-6 ? "baseWidth<=0" : "centerX outside base");
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED dimSplit skip: " + reason + " centerX=" + (double.IsNaN(pipeCenterX) ? "NaN" : this.FormatNumber(pipeCenterX)) + " baseMinX=" + this.FormatNumber(baseMinX) + " baseMaxX=" + this.FormatNumber(baseMaxX));
        }
        if (dimV != null)
        {
          dimV.DimensionStyle = dimStyleId;
          this.ApplyIsoDimensionOverrides(dimV, dimSize, "dimV");
          dimV.DimensionText = this.FormatDimNumber(s_isoRealHeight);
          if (annotationLayerId != ObjectId.Null)
            dimV.LayerId = annotationLayerId;
          targetMs.AppendEntity(dimV);
          tr.AddNewlyCreatedDBObject(dimV, true);
          this.LogIsoDimensionExtents(dimV, "dimV", dimSize);
        }
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED dim W=" + this.FormatNumber(s_isoRealWidth) + " V=" + this.FormatNumber(s_isoRealHeight) + " paperExt=" + paperExt.MinPoint + "~" + paperExt.MaxPoint + " blockRef=" + dimSourceId);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED dim 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private bool TryGetIsoSupportPaperExtents(Transaction tr, BlockReference br, out Extents3d supportExt, out double pipeCenterX)
    {
      supportExt = new Extents3d();
      pipeCenterX = double.NaN;
      if (br == null)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED supportExt skip: source not BlockReference");
        return false;
      }

      try
      {
        if (tr == null)
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED supportExt skip: transaction null");
          return false;
        }

        BlockTableRecord btr = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead, false) as BlockTableRecord;
        if (btr == null)
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED supportExt skip: block definition null br=" + br.ObjectId);
          return false;
        }

        Matrix3d transform = br.BlockTransform;
        System.Collections.Generic.List<IsoCircleCandidate> circles = new System.Collections.Generic.List<IsoCircleCandidate>();
        foreach (ObjectId id in btr)
        {
          Entity ent = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
          if (ent == null)
            continue;

          Circle circle = ent as Circle;
          if (circle != null)
          {
            double radius = this.GetTransformedCircleRadius(circle, transform);
            Point3d center = circle.Center.TransformBy(transform);
            circles.Add(new IsoCircleCandidate(id, radius, center.X, center.Y));
          }
        }

        double expectedRadius;
        bool hasExpected = this.TryGetExpectedIsoPipeRadius(out expectedRadius);
        ObjectId excludedCircleId = ObjectId.Null;
        int matched = 0;
        double bestCenterY = double.MinValue;
        double excludedCenterX = double.NaN;
        double maxRadius = -1.0;
        ObjectId maxCircleId = ObjectId.Null;
        double maxCircleCenterX = double.NaN;
        foreach (IsoCircleCandidate candidate in circles)
        {
          bool match = hasExpected && expectedRadius > 1e-6 && System.Math.Abs(candidate.Radius - expectedRadius) / expectedRadius < 0.15;
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED pipeCircle R=" + this.FormatNumber(candidate.Radius) + " expR=" + (hasExpected ? this.FormatNumber(expectedRadius) : "n/a") + " match=" + match + " centerX=" + this.FormatNumber(candidate.CenterX) + " centerY=" + this.FormatNumber(candidate.CenterY));
          if (candidate.Radius > maxRadius)
          {
            maxRadius = candidate.Radius;
            maxCircleId = candidate.Id;
            maxCircleCenterX = candidate.CenterX;
          }
          if (match)
          {
            matched++;
            if (candidate.CenterY > bestCenterY)
            {
              bestCenterY = candidate.CenterY;
              excludedCircleId = candidate.Id;
              excludedCenterX = candidate.CenterX;
            }
          }
        }

        if (excludedCircleId == ObjectId.Null && maxCircleId != ObjectId.Null)
        {
          excludedCircleId = maxCircleId;
          excludedCenterX = maxCircleCenterX;
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED pipeCircle fallback=maxCircle R=" + this.FormatNumber(maxRadius) + " centerX=" + this.FormatNumber(excludedCenterX));
        }
        pipeCenterX = excludedCenterX;

        bool hasSupport = false;
        Point3d min = Point3d.Origin;
        Point3d max = Point3d.Origin;
        int excludedCircles = 0;
        int includedEntities = 0;
        foreach (ObjectId id in btr)
        {
          Entity ent = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
          if (ent == null)
            continue;
          if (id == excludedCircleId)
          {
            excludedCircles++;
            continue;
          }

          try
          {
            Extents3d entExt = ent.GeometricExtents;
            Point3d[] corners = this.GetExtentsCorners(entExt);
            foreach (Point3d corner in corners)
              this.AccumulatePoint(corner.TransformBy(transform), ref hasSupport, ref min, ref max);
            includedEntities++;
          }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSVBISOEXPORTED supportExt entity skip id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
          }
        }

        if (!hasSupport || includedEntities == 0 || System.Math.Abs(max.X - min.X) <= 1e-6 || System.Math.Abs(max.Y - min.Y) <= 1e-6)
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED supportExt degenerate included=" + includedEntities + " excludedCircles=" + excludedCircles + " matched=" + matched);
          return false;
        }

        supportExt = new Extents3d(min, max);
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED supportExt=" + supportExt.MinPoint + "~" + supportExt.MaxPoint + " excludedCircles=" + excludedCircles + " matched=" + matched);
        return true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED supportExt 예외: " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
    }

    private bool TryGetExpectedIsoPipeRadius(out double radius)
    {
      radius = 0.0;
      try
      {
        if (string.IsNullOrWhiteSpace(s_isoSize))
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED pipeCircle expR skip: size empty");
          return false;
        }

        string digits = System.Text.RegularExpressions.Regex.Replace(s_isoSize, "[^0-9]", string.Empty);
        int nominal;
        if (string.IsNullOrWhiteSpace(digits) || !int.TryParse(digits, out nominal))
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED pipeCircle expR skip: parse size=" + s_isoSize);
          return false;
        }

        radius = PSUtil.PipeSize(nominal) / 2.0;
        return radius > 1e-6;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED pipeCircle expR 예외: " + ex.GetType().Name + ": " + ex.Message + " size=" + s_isoSize);
        return false;
      }
    }

    private double GetTransformedCircleRadius(Circle circle, Matrix3d transform)
    {
      Point3d center = circle.Center.TransformBy(transform);
      Point3d edge = (circle.Center + Vector3d.XAxis.MultiplyBy(circle.Radius)).TransformBy(transform);
      double radius = center.DistanceTo(edge);
      return radius > 1e-6 ? radius : circle.Radius;
    }

    private void AccumulatePoint(Point3d point, ref bool hasPoint, ref Point3d min, ref Point3d max)
    {
      if (!hasPoint)
      {
        min = point;
        max = point;
        hasPoint = true;
        return;
      }

      min = new Point3d(System.Math.Min(min.X, point.X), System.Math.Min(min.Y, point.Y), System.Math.Min(min.Z, point.Z));
      max = new Point3d(System.Math.Max(max.X, point.X), System.Math.Max(max.Y, point.Y), System.Math.Max(max.Z, point.Z));
    }

    private class IsoCircleCandidate
    {
      public IsoCircleCandidate(ObjectId id, double radius, double centerX, double centerY)
      {
        this.Id = id;
        this.Radius = radius;
        this.CenterX = centerX;
        this.CenterY = centerY;
      }

      public ObjectId Id;
      public double Radius;
      public double CenterX;
      public double CenterY;
    }

    private class NotabClipBox : System.IDisposable
    {
      public NotabClipBox(Solid3d box, Vector3d right, Vector3d up, Vector3d viewDir, double minR, double maxR, double minU, double maxU, double minD, double maxD, double margin)
      {
        this.Box = box;
        this.Right = right;
        this.Up = up;
        this.ViewDir = viewDir;
        this.MinR = minR;
        this.MaxR = maxR;
        this.MinU = minU;
        this.MaxU = maxU;
        this.MinD = minD;
        this.MaxD = maxD;
        this.Margin = margin;
      }

      public Solid3d Box;
      public Vector3d Right;
      public Vector3d Up;
      public Vector3d ViewDir;
      public double MinR;
      public double MaxR;
      public double MinU;
      public double MaxU;
      public double MinD;
      public double MaxD;
      public double Margin;

      public void Dispose()
      {
        if (this.Box != null)
        {
          this.Box.Dispose();
          this.Box = null;
        }
      }
    }

    private class NotabPipeIncludeCandidate
    {
      public NotabPipeIncludeCandidate(ObjectId id, double score, string projectionLog, double tolerance, double radius, string lineNo, double pipeCenterZ)
      {
        this.Id = id;
        this.Score = score;
        this.ProjectionLog = projectionLog == null ? string.Empty : projectionLog;
        this.Tolerance = tolerance;
        this.Radius = radius;
        this.LineNo = lineNo == null ? string.Empty : lineNo;
        this.PipeCenterZ = pipeCenterZ;
        this.BopError = double.MaxValue;
      }

      public ObjectId Id;
      public double Score;
      public string ProjectionLog;
      public double Tolerance;
      public double Radius;
      public string LineNo;
      public double PipeCenterZ;
      public double BopError;
    }

    private double ComputeIsoDimensionSize()
    {
      double baseSize = System.Math.Max(System.Math.Abs(s_isoRealWidth), System.Math.Abs(s_isoRealHeight));
      double h = baseSize > 1e-6 ? baseSize / 12.0 : 10.0;
      if (h < 10.0)
        h = 10.0;
      if (h > 500.0)
        h = 500.0;
      return h;
    }

    private void ApplyIsoDimensionOverrides(RotatedDimension dim, double h, string label)
    {
      if (dim == null)
        return;

      try
      {
        dim.Dimtxt = h;
        dim.Dimasz = h;
        dim.Dimexe = 5.0;
        dim.Dimexo = h * 0.3;
        dim.Dimgap = h * 0.3;
        dim.Dimtih = false;
        dim.Dimtoh = false;
        dim.Dimtad = 1;
        dim.Dimdec = 1;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED " + label + " dim override 예외: " + ex.GetType().Name + ": " + ex.Message + " h=" + this.FormatNumber(h));
      }
    }

    private void LogIsoDimensionExtents(RotatedDimension dim, string label, double h)
    {
      if (dim == null)
        return;

      try
      {
        Extents3d ext = dim.GeometricExtents;
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED " + label + " ext=" + ext.MinPoint + "~" + ext.MaxPoint + " h=" + this.FormatNumber(h));
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED " + label + " ext 예외: " + ex.GetType().Name + ": " + ex.Message + " h=" + this.FormatNumber(h));
      }
    }

    private void LogIsoBlockInnerTypes(Database db, Transaction tr, ObjectId blockRefId)
    {
      int line = 0;
      int circle = 0;
      int ellipse = 0;
      int spline = 0;
      int arc = 0;
      int poly = 0;
      if (db == null || tr == null || blockRefId == ObjectId.Null)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED blockInner skip blockRef=" + blockRefId);
        return;
      }

      try
      {
        BlockReference br = tr.GetObject(blockRefId, OpenMode.ForRead, false) as BlockReference;
        if (br == null)
        {
          PlantOrthoView.FileDiag("PFSVBISOEXPORTED blockInner skip: not BlockReference id=" + blockRefId);
          return;
        }
        this.CountBlockRecordEntityTypes(tr, br.BlockTableRecord, ref line, ref circle, ref ellipse, ref spline, ref arc, ref poly, 0);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOEXPORTED blockInner 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      PlantOrthoView.FileDiag("PFSVBISOEXPORTED blockInner Line=" + line + " Circle=" + circle + " Ellipse=" + ellipse + " Spline=" + spline + " Arc=" + arc + " Poly=" + poly);
    }

    private void CountBlockRecordEntityTypes(Transaction tr, ObjectId btrId, ref int line, ref int circle, ref int ellipse, ref int spline, ref int arc, ref int poly, int depth)
    {
      if (tr == null || btrId == ObjectId.Null || depth > 8)
        return;

      BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead, false) as BlockTableRecord;
      if (btr == null)
        return;

      foreach (ObjectId id in btr)
      {
        Entity ent = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
        if (ent == null)
          continue;
        if (ent is Line)
          line++;
        else if (ent is Circle)
          circle++;
        else if (ent is Ellipse)
          ellipse++;
        else if (ent is Spline)
          spline++;
        else if (ent is Arc)
          arc++;
        else if (ent is Polyline || ent is Polyline2d || ent is Polyline3d)
          poly++;

        BlockReference nested = ent as BlockReference;
        if (nested != null)
          this.CountBlockRecordEntityTypes(tr, nested.BlockTableRecord, ref line, ref circle, ref ellipse, ref spline, ref arc, ref poly, depth + 1);
      }
    }

    // 서포트만 선택해도 같은 위치의 관련 파트(관통 파이프 + 유볼트 등 co-located 서포트)를 자동 포함한다.
    // 선택된 서포트의 로컬 bbox와 교차하는 모델공간 Pipe/Support만 추가(다른 위치 서포트는 제외).
    private ObjectId[] AutoIncludeRelatedParts(Database db, ObjectId[] selectedIds)
    {
      if (db == null || selectedIds == null || selectedIds.Length == 0)
        return selectedIds;

      try
      {
        System.Collections.Generic.List<ObjectId> result = new System.Collections.Generic.List<ObjectId>(selectedIds);
        System.Collections.Generic.HashSet<ObjectId> existing = new System.Collections.Generic.HashSet<ObjectId>(selectedIds);

        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          // 1) 선택된 서포트들의 union bbox(앵커). 서포트 없으면 전체 선택 bbox 폴백.
          Extents3d anchor = new Extents3d();
          bool hasAnchor = false;
          foreach (ObjectId id in selectedIds)
          {
            string cls = id.ObjectClass == null ? string.Empty : id.ObjectClass.Name;
            if (cls.IndexOf("Support", System.StringComparison.OrdinalIgnoreCase) < 0)
              continue;
            Entity e = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
            if (e == null) continue;
            try { Extents3d ex = e.GeometricExtents; if (!hasAnchor) { anchor = ex; hasAnchor = true; } else anchor.AddExtents(ex); }
            catch (System.Exception exx) { PlantOrthoView.FileDiag("PFSNOTABDETAIL auto-include anchor ext 예외 id=" + id + ": " + exx.GetType().Name); }
          }
          if (!hasAnchor)
          {
            foreach (ObjectId id in selectedIds)
            {
              Entity e = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
              if (e == null) continue;
              try { Extents3d ex = e.GeometricExtents; if (!hasAnchor) { anchor = ex; hasAnchor = true; } else anchor.AddExtents(ex); }
              catch (System.Exception exx) { PlantOrthoView.FileDiag("PFSNOTABDETAIL auto-include fallback anchor ext 예외 id=" + id + ": " + exx.GetType().Name + ": " + exx.Message); }
            }
          }
          if (!hasAnchor) { tr.Commit(); return selectedIds; }

          double margin = this.GetEnvDouble("PFS_NOTAB_MARGIN", 150.0, 0.0, 10000.0);
          double contactAllowance = this.GetEnvDouble("PFS_NOTAB_CONTACT_TOL", 20.0, 0.0, 10000.0);
          Extents3d probe = new Extents3d(
            new Point3d(anchor.MinPoint.X - margin, anchor.MinPoint.Y - margin, anchor.MinPoint.Z - margin),
            new Point3d(anchor.MaxPoint.X + margin, anchor.MaxPoint.Y + margin, anchor.MaxPoint.Z + margin));
          double supTol = this.GetEnvDouble("PFS_NOTAB_SUPPORT_TOL", 50.0, 0.0, 10000.0);
          Extents3d supContactBox = new Extents3d(
            new Point3d(anchor.MinPoint.X - supTol, anchor.MinPoint.Y - supTol, anchor.MinPoint.Z - supTol),
            new Point3d(anchor.MaxPoint.X + supTol, anchor.MaxPoint.Y + supTol, anchor.MaxPoint.Z + supTol));
          double pipeReach = this.GetEnvDouble("PFS_NOTAB_PIPE_REACH", 300.0, 0.0, 100000.0);
          Extents3d pipeReachBox = new Extents3d(
            new Point3d(anchor.MinPoint.X - pipeReach, anchor.MinPoint.Y - pipeReach, anchor.MinPoint.Z - pipeReach),
            new Point3d(anchor.MaxPoint.X + pipeReach, anchor.MaxPoint.Y + pipeReach, anchor.MaxPoint.Z + pipeReach));
          Point3d supCenter = new Point3d(
            (anchor.MinPoint.X + anchor.MaxPoint.X) / 2.0,
            (anchor.MinPoint.Y + anchor.MaxPoint.Y) / 2.0,
            (anchor.MinPoint.Z + anchor.MaxPoint.Z) / 2.0);

          PSUtil ps = Commands.PSUtil;
          if (ps == null)
            ps = new PSUtil();
          string supTag = string.Empty;
          ObjectId supportForBop = ObjectId.Null;
          foreach (ObjectId id in selectedIds)
          {
            string cls = id.ObjectClass == null ? string.Empty : id.ObjectClass.Name;
            if (cls.IndexOf("Support", System.StringComparison.OrdinalIgnoreCase) < 0)
              continue;
            if (supportForBop == ObjectId.Null)
              supportForBop = id;
            if (this.TryGetPlantLineNumber(ps, id, out supTag))
              break;
          }
          double supportBop;
          bool hasBop = this.TryGetSupportBop(ps, supportForBop, out supportBop);
          double bopTol = this.GetEnvDouble("PFS_NOTAB_BOP_TOL", 10.0, 0.0, 1000.0);

          // 2) 모델공간 스캔: Pipe는 line tag + 축거리 접촉으로, Support는 anchor 접촉박스로만 포함한다.
          ObjectId msId = this.GetModelSpaceId(db, tr);
          BlockTableRecord ms = msId == ObjectId.Null ? null : tr.GetObject(msId, OpenMode.ForRead) as BlockTableRecord;
          int addedPipe = 0, addedSup = 0;
          int pipeCand = 0, pipeIncl = 0, pipeDropLine = 0, pipeDropDist = 0, pipeDropNoTag = 0, pipeFallback = 0;
          System.Collections.Generic.List<NotabPipeIncludeCandidate> pipeIncludes = new System.Collections.Generic.List<NotabPipeIncludeCandidate>();
          if (ms != null)
          {
            foreach (ObjectId eid in ms)
            {
              if (existing.Contains(eid)) continue;
              string cls = eid.ObjectClass == null ? string.Empty : eid.ObjectClass.Name;
              bool isPipe = cls.IndexOf("Pipe", System.StringComparison.OrdinalIgnoreCase) >= 0;
              bool isSup = cls.IndexOf("Support", System.StringComparison.OrdinalIgnoreCase) >= 0;
              bool isPlant = cls.IndexOf("AcPpDb3d", System.StringComparison.OrdinalIgnoreCase) >= 0;
              if (!isPipe && !isSup && !isPlant) continue;
              Entity e = tr.GetObject(eid, OpenMode.ForRead, false) as Entity;
              if (e == null) continue;
              Extents3d ex;
              try { ex = e.GeometricExtents; }
              catch (System.Exception exx)
              {
                PlantOrthoView.FileDiag("PFSNOTABDETAIL auto-include ext skip id=" + eid + ": " + exx.GetType().Name + ": " + exx.Message);
                continue;
              }

              if (isPipe)
              {
                pipeCand++;
                if (string.IsNullOrWhiteSpace(supTag))
                {
                  pipeDropNoTag++;
                  continue;
                }

                string pipeTag;
                if (!this.TryGetPlantLineNumber(ps, eid, out pipeTag) || !string.Equals(pipeTag, supTag, System.StringComparison.OrdinalIgnoreCase))
                {
                  pipeDropLine++;
                  continue;
                }

                if (!this.NotabExtentsIntersect(pipeReachBox, ex))
                {
                  pipeDropDist++;
                  PlantOrthoView.FileDiag("PFSNOTABDETAIL auto-include pipe dropFar id=" + eid + " ext=" + this.FormatExtents(ex) + " pipeReach=" + this.FormatNumber(pipeReach) + " line=" + pipeTag);
                  continue;
                }

                bool includePipe = false;
                Point3d p0;
                Vector3d dir;
                double radius;
                if (this.TryGetPipeAxisFromId(tr, eid, ps, out p0, out dir, out radius))
                {
                  double tol = radius + contactAllowance;
                  double score;
                  string projectionLog;
                  includePipe = this.DoesPipeAxisProjectThroughSupport(anchor, p0, dir, tol, out score, out projectionLog);
                  if (!includePipe)
                  {
                    pipeDropDist++;
                    PlantOrthoView.FileDiag("PFSNOTABDETAIL auto-include pipe dropDist id=" + eid + " " + projectionLog + " tol=" + this.FormatNumber(tol) + " radius=" + this.FormatNumber(radius) + " line=" + pipeTag);
                  }
                  else
                  {
                    double pipeCenterZ = p0.Z;
                    double candidateBop = pipeCenterZ - radius;
                    NotabPipeIncludeCandidate candidate = new NotabPipeIncludeCandidate(eid, score, projectionLog, tol, radius, pipeTag, pipeCenterZ);
                    if (hasBop && radius > 1e-6)
                      candidate.BopError = System.Math.Abs(candidateBop - supportBop);
                    pipeIncludes.Add(candidate);
                  }
                }
                else
                {
                  pipeFallback++;
                  double distance = this.DistancePointToExtents(supCenter, ex);
                  includePipe = distance <= contactAllowance;
                  if (!includePipe)
                  {
                    pipeDropDist++;
                    PlantOrthoView.FileDiag("PFSNOTABDETAIL auto-include pipe dropFallback id=" + eid + " d=" + this.FormatNumber(distance) + " tol=" + this.FormatNumber(contactAllowance) + " line=" + pipeTag);
                  }
                  else
                  {
                    pipeIncludes.Add(new NotabPipeIncludeCandidate(eid, distance, "fallback d=" + this.FormatNumber(distance), contactAllowance, 0.0, pipeTag, double.NaN));
                  }
                }

                if (!includePipe)
                  continue;
              }
              else if (isSup)
              {
                if (!this.NotabExtentsIntersect(supContactBox, ex))
                {
                  PlantOrthoView.FileDiag("PFSNOTABDETAIL auto-include support skip(far) id=" + eid + " ext=" + this.FormatExtents(ex) + " supTol=" + this.FormatNumber(supTol));
                  continue;
                }
                // class=RXClass 이름, type=DXF 이름. U-bolt 판별에 둘 다 필요하다.
                this.DumpNotabAutoIncludedSupportMetadata(ps, eid, cls,
                  eid.ObjectClass == null ? string.Empty : eid.ObjectClass.DxfName, ex);
                existing.Add(eid);
                result.Add(eid);
                addedSup++;
                PlantOrthoView.FileDiag("PFSNOTABDETAIL auto-include support add id=" + eid + " ext=" + this.FormatExtents(ex) + " supTol=" + this.FormatNumber(supTol));
              }
              else
              {
                PlantOrthoView.FileDiag("PFSNOTABDETAIL auto-include otherPart class=" + cls + " (미포함)");
              }
            }
          }

          if (pipeIncludes.Count > 0)
          {
            NotabPipeIncludeCandidate best = pipeIncludes[0];
            bool useBopTieBreak = false;
            for (int i = 0; i < pipeIncludes.Count; i++)
            {
              if (pipeIncludes[i].BopError < double.MaxValue)
              {
                useBopTieBreak = true;
                break;
              }
            }

            for (int i = 1; i < pipeIncludes.Count; i++)
            {
              if (useBopTieBreak)
              {
                if (pipeIncludes[i].BopError + bopTol < best.BopError ||
                    (System.Math.Abs(pipeIncludes[i].BopError - best.BopError) <= bopTol && pipeIncludes[i].Score < best.Score))
                  best = pipeIncludes[i];
              }
              else if (pipeIncludes[i].Score < best.Score)
              {
                best = pipeIncludes[i];
              }
            }

            existing.Add(best.Id);
            result.Add(best.Id);
            addedPipe++;
            pipeIncl = 1;
            if (!useBopTieBreak)
              PlantOrthoView.FileDiag("PFSNOTABDETAIL auto-include pipe BOP 부재 기하폴백 support=" + supportForBop);
            PlantOrthoView.FileDiag("PFSNOTABDETAIL auto-include pipe include id=" + best.Id + " reason=" + (useBopTieBreak ? "bop" : "fallback") + " score=" + this.FormatNumber(best.Score) + " bopErr=" + (best.BopError < double.MaxValue ? this.FormatNumber(best.BopError) : "n/a") + " pipeCenterZ=" + (double.IsNaN(best.PipeCenterZ) ? "n/a" : this.FormatNumber(best.PipeCenterZ)) + " radius=" + this.FormatNumber(best.Radius) + " bop=" + (hasBop ? this.FormatNumber(supportBop) : "n/a") + " " + best.ProjectionLog + " tol=" + this.FormatNumber(best.Tolerance) + " bopTol=" + this.FormatNumber(bopTol) + " line=" + best.LineNo + " passCount=" + pipeIncludes.Count);
            for (int i = 0; i < pipeIncludes.Count; i++)
            {
              if (pipeIncludes[i].Id == best.Id)
                continue;
              pipeDropDist++;
              PlantOrthoView.FileDiag("PFSNOTABDETAIL auto-include pipe dropAmbiguous id=" + pipeIncludes[i].Id + " score=" + this.FormatNumber(pipeIncludes[i].Score) + " bopErr=" + (pipeIncludes[i].BopError < double.MaxValue ? this.FormatNumber(pipeIncludes[i].BopError) : "n/a") + " pipeCenterZ=" + (double.IsNaN(pipeIncludes[i].PipeCenterZ) ? "n/a" : this.FormatNumber(pipeIncludes[i].PipeCenterZ)) + " radius=" + this.FormatNumber(pipeIncludes[i].Radius) + " bop=" + (hasBop ? this.FormatNumber(supportBop) : "n/a") + " " + pipeIncludes[i].ProjectionLog + " best=" + best.Id);
            }
          }
          tr.Commit();
          PlantOrthoView.FileDiag("PFSNOTABDETAIL auto-include anchor=" + this.FormatExtents(anchor) + " margin=" + this.FormatNumber(margin) + " supportTol=" + this.FormatNumber(supTol) + " pipeReach=" + this.FormatNumber(pipeReach) + " contactTol=" + this.FormatNumber(contactAllowance) + " line=" + (string.IsNullOrWhiteSpace(supTag) ? "(none)" : supTag) + " pipeCand=" + pipeCand + " incl=" + pipeIncl + " dropLine=" + pipeDropLine + " dropDist=" + pipeDropDist + " dropNoTag=" + pipeDropNoTag + " fallback=" + pipeFallback + " addedPipe=" + addedPipe + " addedSupport=" + addedSup);
        }
        return result.ToArray();
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL auto-include 예외: " + ex.GetType().Name + ": " + ex.Message);
        return selectedIds;
      }
    }

    private bool NotabExtentsIntersect(Extents3d a, Extents3d b)
    {
      if (a.MaxPoint.X < b.MinPoint.X || a.MinPoint.X > b.MaxPoint.X) return false;
      if (a.MaxPoint.Y < b.MinPoint.Y || a.MinPoint.Y > b.MaxPoint.Y) return false;
      if (a.MaxPoint.Z < b.MinPoint.Z || a.MinPoint.Z > b.MaxPoint.Z) return false;
      return true;
    }

    // 자동 포함된 원본 Support만 대상으로 한다. 상세도 복제본은 메타데이터가 제거되어 있다.
    private void DumpNotabAutoIncludedSupportMetadata(PSUtil ps, ObjectId id, string className, string typeName, Extents3d extents)
    {
      PlantOrthoView.FileDiag("PFSNOTABDETAIL ubolt-probe candidate id=" + id + " class=" + className + " type=" + typeName);
      if (ps == null)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL ubolt-probe skip: PSUtil null id=" + id);
        return;
      }

      try
      {
        System.Collections.Generic.Dictionary<string, string> dims = ps.GetSupportDimension(id);
        System.Text.StringBuilder dump = new System.Text.StringBuilder();
        if (dims != null)
        {
          foreach (System.Collections.Generic.KeyValuePair<string, string> kv in dims)
            dump.Append(kv.Key).Append('=').Append(kv.Value).Append(" | ");
        }
        PlantOrthoView.FileDiag("PFSNOTABDETAIL ubolt-probe support-dims id=" + id + " count=" + (dims == null ? 0 : dims.Count) + " { " + dump.ToString() + "}");
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL ubolt-probe support-dims 예외 id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
      }

      try
      {
        System.Collections.Specialized.StringCollection names = new System.Collections.Specialized.StringCollection()
        {
          "SupportName", "ShortDescription", "PartNumber", "Tag", "TagName", "SupportDetail", "Description"
        };
        System.Collections.Specialized.StringCollection values = ps.GetProperties(id, names);
        System.Text.StringBuilder dump = new System.Text.StringBuilder();
        for (int i = 0; i < names.Count; i++)
        {
          string value = values != null && i < values.Count ? values[i] : string.Empty;
          dump.Append(names[i]).Append('=').Append(string.IsNullOrWhiteSpace(value) ? "(empty)" : value).Append(" | ");
        }
        PlantOrthoView.FileDiag("PFSNOTABDETAIL ubolt-probe datalinks id=" + id + " { " + dump.ToString() + "}");
        string shortDescription = values != null && values.Count > 1 ? values[1] : string.Empty;
        string tag = values != null && values.Count > 3 ? values[3] : string.Empty;
        if (string.Equals(shortDescription, "UB", System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(tag))
        {
          tag = tag.Trim();
          bool duplicate = false;
          for (int i = 0; i < s_isoUbolts.Count; i++)
          {
            if (string.Equals(s_isoUbolts[i].Tag, tag, System.StringComparison.OrdinalIgnoreCase))
            {
              duplicate = true;
              break;
            }
          }
          if (!duplicate)
          {
            NotabUboltSnapshot snapshot = new NotabUboltSnapshot();
            snapshot.Tag = tag;
            snapshot.WcsCenter = new Point3d((extents.MinPoint.X + extents.MaxPoint.X) / 2.0, (extents.MinPoint.Y + extents.MaxPoint.Y) / 2.0, (extents.MinPoint.Z + extents.MaxPoint.Z) / 2.0);
            s_isoUbolts.Add(snapshot);
            PlantOrthoView.FileDiag("PFSNOTABDETAIL ubolt capture tag=" + tag + " wcs=" + this.FormatPoint(snapshot.WcsCenter));
          }
          else
            PlantOrthoView.FileDiag("PFSNOTABDETAIL ubolt skip=duplicate-tag tag=" + tag);
        }
        else
          PlantOrthoView.FileDiag("PFSNOTABDETAIL ubolt skip=" + (string.IsNullOrWhiteSpace(tag) ? "empty-tag" : "short-description-not-UB") + " id=" + id);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL ubolt-probe datalinks 예외 id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private bool TryGetPlantLineNumber(PSUtil ps, ObjectId id, out string lineNo)
    {
      lineNo = string.Empty;
      if (id == ObjectId.Null)
        return false;
      if (ps == null || ps.dl_manager == null)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL lineNo skip: dl_manager null id=" + id);
        return false;
      }

      string[] candidates = new string[] { "LineNumberTag", "LineNumber", "Line Number" };
      foreach (string name in candidates)
      {
        try
        {
          System.Collections.Specialized.StringCollection names = new System.Collections.Specialized.StringCollection() { name };
          System.Collections.Specialized.StringCollection props = ps.dl_manager.GetProperties(id, names, true);
          string value = props != null && props.Count > 0 ? props[0] : string.Empty;
          if (!string.IsNullOrWhiteSpace(value))
          {
            lineNo = value.Trim();
            return true;
          }
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL lineNo candidate " + name + " 예외 id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
        }
      }

      return false;
    }

    private bool TryGetSupportBop(PSUtil ps, ObjectId supId, out double bop)
    {
      bop = 0.0;
      if (supId == ObjectId.Null)
        return false;
      if (ps == null || ps.dl_manager == null)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL support BOP skip: dl_manager null id=" + supId);
        return false;
      }

      try
      {
        System.Collections.Specialized.StringCollection names = new System.Collections.Specialized.StringCollection() { "BOP" };
        System.Collections.Specialized.StringCollection props = ps.dl_manager.GetProperties(supId, names, true);
        string raw = props != null && props.Count > 0 ? props[0] : string.Empty;
        string rounded = this.RoundPropertyValue(raw);
        double parsed;
        if (double.TryParse(rounded, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed) ||
            double.TryParse(rounded, out parsed))
        {
          bop = parsed;
          return true;
        }

        PlantOrthoView.FileDiag("PFSNOTABDETAIL support BOP parse fail id=" + supId + " raw=" + raw);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL support BOP 예외 id=" + supId + ": " + ex.GetType().Name + ": " + ex.Message);
      }

      return false;
    }

    private bool TryGetPipeAxisFromId(Transaction tr, ObjectId pipeId, PSUtil ps, out Point3d p0, out Vector3d dir, out double radius)
    {
      p0 = Point3d.Origin;
      dir = Vector3d.XAxis;
      radius = 0.0;
      if (tr == null || pipeId == ObjectId.Null)
        return false;

      try
      {
        Part part = tr.GetObject(pipeId, OpenMode.ForRead, false) as Part;
        if (part == null)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL pipeAxis skip: not Part id=" + pipeId);
          return false;
        }

        PortCollection ports = part.GetPorts((PortType)7);
        if (ports == null || ports.Count < 2)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL pipeAxis skip: ports<2 id=" + pipeId);
          return false;
        }

        Point3d? first = null;
        Point3d? second = null;
        foreach (Port port in (PnP3dCollection)ports)
        {
          if (!first.HasValue)
          {
            first = port.Position;
            continue;
          }
          second = port.Position;
          break;
        }

        if (!first.HasValue || !second.HasValue)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL pipeAxis skip: port positions 없음 id=" + pipeId);
          return false;
        }

        Vector3d raw = second.Value - first.Value;
        if (raw.Length <= 1e-6)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL pipeAxis skip: degenerate id=" + pipeId);
          return false;
        }

        p0 = first.Value;
        dir = raw.GetNormal();
        radius = this.TryGetPipeRadiusFromProperties(ps, pipeId);
        if (radius <= 1e-6)
        {
          try
          {
            Extents3d ext = ((Entity)part).GeometricExtents;
            double dx = System.Math.Abs(ext.MaxPoint.X - ext.MinPoint.X);
            double dy = System.Math.Abs(ext.MaxPoint.Y - ext.MinPoint.Y);
            double dz = System.Math.Abs(ext.MaxPoint.Z - ext.MinPoint.Z);
            radius = System.Math.Max(1.0, System.Math.Min(dx, System.Math.Min(dy, dz)) / 2.0);
            PlantOrthoView.FileDiag("PFSNOTABDETAIL pipeAxis radius fallback bbox id=" + pipeId + " r=" + this.FormatNumber(radius));
          }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL pipeAxis radius fallback 예외 id=" + pipeId + ": " + ex.GetType().Name + ": " + ex.Message);
            radius = 0.0;
          }
        }

        return true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL pipeAxis 예외 id=" + pipeId + ": " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
    }

    private double TryGetPipeRadiusFromProperties(PSUtil ps, ObjectId pipeId)
    {
      if (ps == null || ps.dl_manager == null || pipeId == ObjectId.Null)
        return 0.0;

      string[] candidates = new string[] { "Size", "NominalDiameter", "Nominal Diameter", "Diameter" };
      foreach (string name in candidates)
      {
        try
        {
          System.Collections.Specialized.StringCollection names = new System.Collections.Specialized.StringCollection() { name };
          System.Collections.Specialized.StringCollection props = ps.dl_manager.GetProperties(pipeId, names, true);
          string value = props != null && props.Count > 0 ? props[0] : string.Empty;
          if (string.IsNullOrWhiteSpace(value))
            continue;

          string digits = System.Text.RegularExpressions.Regex.Replace(value, "[^0-9.]", string.Empty);
          double parsed;
          if (!string.IsNullOrWhiteSpace(digits) && double.TryParse(digits, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed) && parsed > 0.0)
          {
            if (string.Equals(name, "Size", System.StringComparison.OrdinalIgnoreCase))
            {
              int nominal = (int)System.Math.Round(parsed);
              return PSUtil.PipeSize(nominal) / 2.0;
            }

            return parsed / 2.0;
          }
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL pipeRadius candidate " + name + " 예외 id=" + pipeId + ": " + ex.GetType().Name + ": " + ex.Message);
        }
      }

      return 0.0;
    }

    private double DistancePointToPipeAxis(Point3d point, Point3d p0, Vector3d dir)
    {
      if (dir.Length <= 1e-6)
        return double.MaxValue;

      Vector3d n = dir.GetNormal();
      Vector3d v = point - p0;
      Vector3d perpendicular = v - n.MultiplyBy(v.DotProduct(n));
      return perpendicular.Length;
    }

    private bool DoesPipeAxisProjectThroughSupport(Extents3d supportExt, Point3d p0, Vector3d dir, double tol, out double score, out string projectionLog)
    {
      score = double.MaxValue;
      projectionLog = string.Empty;
      try
      {
        if (dir.Length <= 1e-6)
        {
          projectionLog = "projection skip: dir degenerate";
          return false;
        }

        Vector3d axis = dir.GetNormal();
        Vector3d right = axis.CrossProduct(Vector3d.ZAxis);
        if (right.Length <= 1e-6)
          right = axis.CrossProduct(Vector3d.YAxis);
        if (right.Length <= 1e-6)
        {
          projectionLog = "projection skip: right degenerate";
          return false;
        }
        right = right.GetNormal();

        Vector3d up = axis.CrossProduct(right);
        if (up.Length <= 1e-6)
        {
          projectionLog = "projection skip: up degenerate";
          return false;
        }
        up = up.GetNormal();

        double minR = 0.0, maxR = 0.0, minU = 0.0, maxU = 0.0;
        bool hasProjection = false;
        foreach (Point3d corner in this.GetExtentsCorners(supportExt))
        {
          Vector3d v = corner.GetAsVector();
          double r = v.DotProduct(right);
          double u = v.DotProduct(up);
          if (!hasProjection)
          {
            minR = maxR = r;
            minU = maxU = u;
            hasProjection = true;
          }
          else
          {
            if (r < minR) minR = r;
            if (r > maxR) maxR = r;
            if (u < minU) minU = u;
            if (u > maxU) maxU = u;
          }
        }

        if (!hasProjection)
        {
          projectionLog = "projection skip: support corners none";
          return false;
        }

        double pr = p0.GetAsVector().DotProduct(right);
        double pu = p0.GetAsVector().DotProduct(up);
        double centerR = (minR + maxR) / 2.0;
        double centerU = (minU + maxU) / 2.0;
        double dr = pr - centerR;
        double du = pu - centerU;
        score = System.Math.Sqrt((dr * dr) + (du * du));
        bool inside = pr >= minR - tol && pr <= maxR + tol && pu >= minU - tol && pu <= maxU + tol;
        projectionLog = "rect=(" + this.FormatSignedNumber(minR) + "," + this.FormatSignedNumber(maxR) + "," + this.FormatSignedNumber(minU) + "," + this.FormatSignedNumber(maxU) + ") p0proj=(" + this.FormatSignedNumber(pr) + "," + this.FormatSignedNumber(pu) + ")";
        return inside;
      }
      catch (System.Exception ex)
      {
        projectionLog = "projection 예외: " + ex.GetType().Name + ": " + ex.Message;
        PlantOrthoView.FileDiag("PFSNOTABDETAIL auto-include projection 예외: " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
    }

    private string FormatSignedNumber(double value)
    {
      return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }

    private double DistancePointToExtents(Point3d point, Extents3d ext)
    {
      double dx = 0.0;
      if (point.X < ext.MinPoint.X) dx = ext.MinPoint.X - point.X;
      else if (point.X > ext.MaxPoint.X) dx = point.X - ext.MaxPoint.X;
      double dy = 0.0;
      if (point.Y < ext.MinPoint.Y) dy = ext.MinPoint.Y - point.Y;
      else if (point.Y > ext.MaxPoint.Y) dy = point.Y - ext.MaxPoint.Y;
      double dz = 0.0;
      if (point.Z < ext.MinPoint.Z) dz = ext.MinPoint.Z - point.Z;
      else if (point.Z > ext.MaxPoint.Z) dz = point.Z - ext.MaxPoint.Z;
      return System.Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
    }

    private bool TryGetSelectionPipeAxis(Database db, ObjectId[] selectedIds, out Vector3d axis, out ObjectId pipeId)
    {
      axis = Vector3d.XAxis;
      pipeId = ObjectId.Null;
      if (db == null || selectedIds == null || selectedIds.Length == 0)
        return false;

      bool found = false;
      double bestLength = 0.0;
      try
      {
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          foreach (ObjectId id in selectedIds)
          {
            try
            {
              DBObject obj = tr.GetObject(id, OpenMode.ForRead, false);
              Part part = obj as Part;
              if (part == null)
                continue;

              string className = id.ObjectClass == null ? string.Empty : id.ObjectClass.Name;
              string typeName = obj.GetType().Name;
              if (className.IndexOf("Pipe", System.StringComparison.OrdinalIgnoreCase) < 0 && typeName.IndexOf("Pipe", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

              PortCollection ports = part.GetPorts((PortType)7);
              if (ports == null || ports.Count < 2)
                continue;

              Point3d? first = null;
              Point3d? second = null;
              foreach (Port port in (PnP3dCollection)ports)
              {
                if (!first.HasValue)
                {
                  first = port.Position;
                  continue;
                }
                second = port.Position;
                break;
              }

              if (!first.HasValue || !second.HasValue)
                continue;

              Vector3d rawAxis = second.Value - first.Value;
              double length = rawAxis.Length;
              if (length <= 1e-6 || length <= bestLength)
                continue;

              axis = rawAxis.GetNormal();
              pipeId = id;
              bestLength = length;
              found = true;
            }
            catch (System.Exception ex)
            {
              PlantOrthoView.FileDiag("PFSVBISOCLONE pipeAxis candidate 예외 id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
            }
          }
          tr.Commit();
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBISOCLONE pipeAxis scan 예외: " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }

      return found;
    }

    private string FormatVectorForCommand(Vector3d vector)
    {
      System.Globalization.CultureInfo ic = System.Globalization.CultureInfo.InvariantCulture;
      return vector.X.ToString("0.######", ic) + "," + vector.Y.ToString("0.######", ic) + "," + vector.Z.ToString("0.######", ic);
    }

    private ObjectId EnsureFlattenLayer(Database db, Transaction tr)
    {
      LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
      const string name = "PFS_ORTHO_FLATTEN";
      if (lt.Has(name))
        return lt[name];

      lt.UpgradeOpen();
      LayerTableRecord ltr = new LayerTableRecord();
      ltr.Name = name;
      ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
      ObjectId lid = lt.Add(ltr);
      tr.AddNewlyCreatedDBObject(ltr, true);
      return lid;
    }

    private ObjectId EnsureIsoDetailLayer(Database db, Transaction tr)
    {
      LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
      const string name = "PFS_ISO_DETAIL";
      if (lt.Has(name))
        return lt[name];

      lt.UpgradeOpen();
      LayerTableRecord ltr = new LayerTableRecord();
      ltr.Name = name;
      ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 3);
      ObjectId lid = lt.Add(ltr);
      tr.AddNewlyCreatedDBObject(ltr, true);
      return lid;
    }

    private void WriteEditorMessageSafe(Editor ed, string message, string context)
    {
      if (ed == null)
        return;

      try
      {
        ed.WriteMessage(message);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag(context + " WriteMessage skip: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    [CommandMethod("PFS")]
    public void CmdPalette()
    {
      if (Commands.palette == null)
        Commands.palette = new CustomPaletteSet();
      ((Window) Commands.palette).Visible = true;
    }
  }
}
