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
    private static double s_isoRealWidth;
    private static double s_isoRealHeight;
    private static bool s_isoRealSizeValid;
    private static string s_isoPipeLineNo;
    private static string s_isoBOP;
    private static string s_isoSize;
    private static string s_isoSupportTag;
    private static string s_isoDesignStd;
    private static string s_isoShortDesc;
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

    [CommandMethod("PFSORTHOUPDATESCALE")]
    public void UpdateStandardScale()
    {
      Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
      if (mdiActiveDocument == null)
      {
        PlantOrthoView.FileDiag("PFSORTHOUPDATESCALE no-op: active doc null");
        return;
      }

      PlantOrthoView.FileDiag("PFSORTHOUPDATESCALE no-op: Stage3 paper-space annotations already use real text height; 10x 보정 생략 doc='" + mdiActiveDocument.Name + "'");
      mdiActiveDocument.SendStringToExecute("._PFSORTHOCONTINUE\n", true, false, false);
    }

    [CommandMethod("PFSORTHOCONTINUE", CommandFlags.Session)]
    public void SaveAndExitCurrentDrawingAndContinueToExecute()
    {
      Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
      Database database = mdiActiveDocument.Database;
      string filename = database.Filename;
      PlantOrthoView.FileDiag("PFSORTHOCONTINUE 진입 doc='" + mdiActiveDocument.Name + "' IsMoreThanOne=" + Commands.IsMoreThanOne + " ItemNo=" + Commands.ItemNo);
      using (mdiActiveDocument.LockDocument())
      {
        using (Transaction transaction = database.TransactionManager.StartTransaction())
        {
          if (Commands.ViewportId != ObjectId.Null)
          {
             try
             {
                (transaction.GetObject(Commands.ViewportId, (OpenMode) 1) as Viewport).Locked = true;
             }
             catch (System.Exception ex)
             {
                PlantOrthoView.FileDiag("PFSORTHOCONTINUE viewport lock skip: " + ex.Message);
             }
          }
          transaction.Commit();
        }
      }
      PlantOrthoView.FileDiag("PFSORTHOCONTINUE: CloseAndSave 호출 직전 filename='" + filename + "'");
      DocumentExtension.CloseAndSave(mdiActiveDocument, filename);
      PlantOrthoView.FileDiag("PFSORTHOCONTINUE: CloseAndSave 반환, PIPE 재활성 직전");
      Document reactivatedDoc = null;
      foreach (Document document in Application.DocumentManager)
      {
        if (string.Compare(document.Name, Commands.DocumentName, true) == 0)
        {
          Application.DocumentManager.MdiActiveDocument = document;
          reactivatedDoc = document;
        }
      }
      // 큐브를 원본(PIPE) DB에서 지웠으나 삭제 시점 비활성이라 화면 잔상이 남음(REGENALL 전까지).
      // 재활성 후 REGENALL 큐잉으로 표시 갱신 → 초록 clip cube 잔상 자동 제거(사용자 확인 case A).
      if (reactivatedDoc != null)
      {
        PlantOrthoView.FileDiag("PFSORTHOCONTINUE: PIPE 재활성 후 REGENALL 큐잉 doc='" + reactivatedDoc.Name + "'");
        reactivatedDoc.SendStringToExecute("._REGENALL\n", true, false, false);
      }
      if (!Commands.IsMoreThanOne)
      {
        PlantOrthoView.FileDiag("PFSORTHOCONTINUE 종료: 단일 서포트(IsMoreThanOne=false) return");
        return;
      }
      int itemNo = Commands.ItemNo;
      if (itemNo >= Commands.PSUtil.lvSupportName.Items.Count - 1)
      {
        PlantOrthoView.FileDiag("PFSORTHOCONTINUE 종료: 마지막 item(itemNo=" + itemNo + ") return");
        return;
      }
      PlantOrthoView.FileDiag("PFSORTHOCONTINUE: 다음 item PSUtil.run(" + itemNo + ") 호출");
      Commands.PSUtil.run(itemNo);
    }

    [CommandMethod("PFSORTHOPHASE2", CommandFlags.Session)]
    public void ContinueOrthoCreateInOrthoDocument()
    {
      Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
      Editor editor = mdiActiveDocument == null ? null : mdiActiveDocument.Editor;
      PlantOrthoView.FileDiag("PFSORTHOPHASE2 진입 doc='" + (mdiActiveDocument == null ? "<null>" : mdiActiveDocument.Name) + "'");
      editor?.WriteMessage("\n[PFS-DIAG] PFSORTHOPHASE2 진입 doc='" + (mdiActiveDocument == null ? "<null>" : mdiActiveDocument.Name) + "'");

      object rawOrthoView = Commands.OrthoView;
      PlantOrthoView orthoView = rawOrthoView as PlantOrthoView;
      if (orthoView == null)
      {
        PlantOrthoView.FileDiag("PFSORTHOPHASE2 실패: OrthoView 인스턴스 없음");
        editor?.WriteMessage("\n[PFS-DIAG] PFSORTHOPHASE2 실패: OrthoView 인스턴스 없음");
        Commands.ContinueBatchAfterFailure("PFSORTHOPHASE2.OrthoViewNull", null);
        return;
      }

      try
      {
        orthoView.CreateOrthoFromCube();
        PlantOrthoView.FileDiag("PFSORTHOPHASE2: CreateOrthoFromCube 반환(정상)");
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSORTHOPHASE2 예외: " + ex.GetType().Name + ": " + ex.Message);
        editor?.WriteMessage("\n[PFS-DIAG] PFSORTHOPHASE2 예외: " + ex.GetType().Name + ": " + ex.Message);
        Commands.ContinueBatchAfterFailure("PFSORTHOPHASE2", ex);
      }
      finally
      {
        try { orthoView.RestoreSpikePipesAfterPhase2(); }
        catch (System.Exception rex) { PlantOrthoView.FileDiag("PFSORTHOPHASE2 spike-restore 예외: " + rex.GetType().Name + ": " + rex.Message); }
      }
      PlantOrthoView.FileDiag("PFSORTHOPHASE2 종료");
    }

    internal static void ContinueBatchAfterFailure(string stage, System.Exception ex)
    {
      try
      {
        PlantOrthoView.FileDiag("ContinueBatchAfterFailure stage=" + stage + " IsMoreThanOne=" + Commands.IsMoreThanOne + " ItemNo=" + Commands.ItemNo
          + (ex == null ? "" : " error=" + ex.GetType().Name + ": " + ex.Message));
        if (!Commands.IsMoreThanOne || Commands.PSUtil == null || Commands.PSUtil.lvSupportName == null)
          return;

        int itemNo = Commands.ItemNo;
        if (itemNo >= Commands.PSUtil.lvSupportName.Items.Count - 1)
        {
          PlantOrthoView.FileDiag("ContinueBatchAfterFailure 종료: 마지막 item(itemNo=" + itemNo + ")");
          return;
        }

        Document activeDoc = Application.DocumentManager.MdiActiveDocument;
        Document sourceDoc = null;
        foreach (Document document in Application.DocumentManager)
        {
          if (string.Compare(document.Name, Commands.DocumentName, true) == 0)
          {
            sourceDoc = document;
            break;
          }
        }

        if (activeDoc != null && (sourceDoc == null || string.Compare(activeDoc.Name, sourceDoc.Name, true) != 0))
        {
          PlantOrthoView.FileDiag("ContinueBatchAfterFailure: Ortho/비원본 doc 정리 위해 PFSORTHOCONTINUE 큐잉 doc='" + activeDoc.Name + "'");
          activeDoc.SendStringToExecute("._PFSORTHOCONTINUE\n", true, false, false);
          return;
        }

        if (sourceDoc != null)
        {
          Application.DocumentManager.MdiActiveDocument = sourceDoc;
          PlantOrthoView.FileDiag("ContinueBatchAfterFailure: 원본 doc 재활성 후 다음 item PSUtil.run(" + itemNo + ")");
        }
        else
        {
          PlantOrthoView.FileDiag("ContinueBatchAfterFailure: 원본 doc 찾지 못함 DocumentName='" + Commands.DocumentName + "'");
        }

        Commands.PSUtil.run(itemNo);
      }
      catch (System.Exception continueEx)
      {
        PlantOrthoView.FileDiag("ContinueBatchAfterFailure 자체 실패: " + continueEx.GetType().Name + ": " + continueEx.Message);
      }
    }

    public static void Export2DSession()
    {
      if (Commands.PSUtil == null || Commands.PSUtil.SupportSelection == null || Commands.PSUtil.SupportSelection.Count == 0)
      {
        Application.ShowAlertDialog("No supports selected for export.");
        return;
      }

      // Initialize for batch processing
      // PSUtil.run increments ItemNo at the beginning, so starting at -1 ensures the first item (index 0) is processed.
      Commands.ItemNo = -1; 
      Commands.IsMoreThanOne = Commands.PSUtil.SupportSelection.Count > 1;

      // Start the export process
      Commands.PSUtil.run(Commands.ItemNo);
    }

    public static dynamic OrthoView { get; set; }

    [CommandMethod("PFSTESTFLATSHOT")]
    public void TestFlatshotCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      Editor ed = doc.Editor;
      PlantOrthoView.FileDiag("PFSTESTFLATSHOT enter doc='" + doc.Name + "' tilemode=" + Application.GetSystemVariable("TILEMODE"));

      var before = new System.Collections.Generic.HashSet<ObjectId>();
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
        BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
        foreach (ObjectId id in ms)
          before.Add(id);
        tr.Commit();
      }

      try
      {
        ed.Command(
          "_.-FLATSHOT",
          "_Insert",
          "_ByBlock",
          "_ByBlock",
          "_No",
          "_No",
          "0,0,0",
          1.0,
          1.0,
          0.0);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSTESTFLATSHOT command 예외: " + ex.GetType().Name + ": " + ex.Message);
        ed.WriteMessage("\nPFSTESTFLATSHOT command error: " + ex.GetType().Name + ": " + ex.Message);
        return;
      }

      int line = 0;
      int circle = 0;
      int arc = 0;
      bool blockCreated = false;
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
        BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
        foreach (ObjectId id in ms)
        {
          if (before.Contains(id))
            continue;

          BlockReference br = tr.GetObject(id, OpenMode.ForRead, false) as BlockReference;
          if (br == null)
            continue;

          blockCreated = true;
          BlockTableRecord btr = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
          if (btr == null)
            continue;

          foreach (ObjectId eid in btr)
          {
            Entity entity = tr.GetObject(eid, OpenMode.ForRead, false) as Entity;
            if (entity is Line)
              line++;
            else if (entity is Circle)
              circle++;
            else if (entity is Arc)
              arc++;
          }
        }
        tr.Commit();
      }

      PlantOrthoView.FileDiag("PFSTESTFLATSHOT block created=" + (blockCreated ? "T" : "F") + " Line=" + line + " Circle=" + circle + " Arc=" + arc);
      ed.WriteMessage("\nPFSTESTFLATSHOT block=" + (blockCreated ? "T" : "F") + " Line=" + line + " Circle=" + circle + " Arc=" + arc);
    }
    [CommandMethod("PFSTESTVIEWBASE")]
    public void TestViewbaseCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      Editor ed = doc.Editor;
      PlantOrthoView.FileDiag("PFSTESTVIEWBASE enter doc='" + doc.Name + "'");

      s_viewbaseBaseline = new System.Collections.Generic.HashSet<ObjectId>();
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(doc.Database, tr, "Layout1");
        if (!layoutBtrId.IsNull)
          this.SnapshotBlockTableRecordIds(tr, layoutBtrId, s_viewbaseBaseline);
        tr.Commit();
      }

      doc.SendStringToExecute("._VIEWBASE\n_M\n_E\nLayout1\n100,100\n\n\n", true, false, false);
      doc.SendStringToExecute("PFSCOUNTVIEW\n", true, false, false);
      PlantOrthoView.FileDiag("PFSTESTVIEWBASE posted VIEWBASE + PFSCOUNTVIEW");
      ed.WriteMessage("\nPFSTESTVIEWBASE posted VIEWBASE + PFSCOUNTVIEW");
    }

    private void RunViewBaseOriented(string orientKeyword)
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      Editor ed = doc.Editor;
      PlantOrthoView.FileDiag("PFSVBORIENT enter orient=" + orientKeyword + " doc='" + doc.Name + "'");

      s_viewbaseBaseline = new System.Collections.Generic.HashSet<ObjectId>();
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(doc.Database, tr, "Layout1");
        if (!layoutBtrId.IsNull)
          this.SnapshotBlockTableRecordIds(tr, layoutBtrId, s_viewbaseBaseline);
        tr.Commit();
      }

      string cmd = "._VIEWBASE\n_M\n_E\nLayout1\n_O\n" + orientKeyword + "\n100,100\n\n\n";
      doc.SendStringToExecute(cmd, true, false, false);
      PlantOrthoView.FileDiag("PFSVBORIENT posted orient=" + orientKeyword + " cmd=" + cmd.Replace("\n", "|"));
      ed.WriteMessage("\nPFSVBORIENT posted orient=" + orientKeyword);
    }

    [CommandMethod("PFSVBFRONT")]
    public void VbFront()
    {
      this.RunViewBaseOriented("_Front");
    }

    [CommandMethod("PFSVBTOP")]
    public void VbTop()
    {
      this.RunViewBaseOriented("_Top");
    }

    [CommandMethod("PFSVBLEFT")]
    public void VbLeft()
    {
      this.RunViewBaseOriented("_Left");
    }

    [CommandMethod("PFSVBRIGHT")]
    public void VbRight()
    {
      this.RunViewBaseOriented("_Right");
    }

    [CommandMethod("PFSVBCUR")]
    public void VbCur()
    {
      this.RunViewBaseOriented("_Current");
    }

    private void RunVpointThenCurrent(string vpoint)
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      Editor ed = doc.Editor;
      PlantOrthoView.FileDiag("PFSVBVP enter vpoint=" + vpoint + " doc='" + doc.Name + "'");

      s_viewbaseBaseline = new System.Collections.Generic.HashSet<ObjectId>();
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(doc.Database, tr, "Layout1");
        if (!layoutBtrId.IsNull)
          this.SnapshotBlockTableRecordIds(tr, layoutBtrId, s_viewbaseBaseline);
        tr.Commit();
      }

      string cmd = "_.-VPOINT\n" + vpoint + "\n._VIEWBASE\n_M\n_E\nLayout1\n_O\n_Current\n100,100\n\n\n";
      doc.SendStringToExecute(cmd, true, false, false);
      PlantOrthoView.FileDiag("PFSVBVP posted vpoint=" + vpoint + " cmd=" + cmd.Replace("\n", "|"));
      ed.WriteMessage("\nPFSVBVP posted vpoint=" + vpoint);
    }

    [CommandMethod("PFSVBVPX")]
    public void VbVpX()
    {
      this.RunVpointThenCurrent("1,0,0");
    }

    [CommandMethod("PFSVBVPTOP")]
    public void VbVpTop()
    {
      this.RunVpointThenCurrent("0,0,1");
    }

    [CommandMethod("PFSVBMAIN")]
    public void VbMainCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      Editor ed = doc.Editor;
      PromptPointResult p1 = ed.GetPoint("\n파이프 축 시작점 지정: ");
      if (p1.Status != PromptStatus.OK)
      {
        PlantOrthoView.FileDiag("PFSVBMAIN: p1 취소");
        return;
      }

      PromptPointOptions opt2 = new PromptPointOptions("\n파이프 축 끝점 지정: ");
      opt2.UseBasePoint = true;
      opt2.BasePoint = p1.Value;
      PromptPointResult p2 = ed.GetPoint(opt2);
      if (p2.Status != PromptStatus.OK)
      {
        PlantOrthoView.FileDiag("PFSVBMAIN: p2 취소");
        return;
      }

      Vector3d axis = p2.Value - p1.Value;
      if (axis.Length < 1e-6)
      {
        PlantOrthoView.FileDiag("PFSVBMAIN: 축 길이 0");
        ed.WriteMessage("\n두 점이 동일");
        return;
      }

      axis = axis.GetNormal();
      string vp = axis.X.ToString("0.######") + "," + axis.Y.ToString("0.######") + "," + axis.Z.ToString("0.######");

      s_viewbaseBaseline = new System.Collections.Generic.HashSet<ObjectId>();
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(doc.Database, tr, "Layout1");
        if (!layoutBtrId.IsNull)
          this.SnapshotBlockTableRecordIds(tr, layoutBtrId, s_viewbaseBaseline);
        tr.Commit();
      }

      string cmd = "_.-VPOINT\n" + vp + "\n._VIEWBASE\n_M\n_E\nLayout1\n_O\n_Current\n100,100\n\n\n";
      doc.SendStringToExecute(cmd, true, false, false);
      PlantOrthoView.FileDiag("PFSVBMAIN axis=" + vp + " cmd=" + cmd.Replace("\n", "|"));
      ed.WriteMessage("\nPFSVBMAIN axis=" + vp);
    }

    [CommandMethod("PFSVB2")]
    public void Vb2Command()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      Editor ed = doc.Editor;

      s_viewbaseBaseline = new System.Collections.Generic.HashSet<ObjectId>();
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(doc.Database, tr, "Layout1");
        if (!layoutBtrId.IsNull)
          this.SnapshotBlockTableRecordIds(tr, layoutBtrId, s_viewbaseBaseline);
        tr.Commit();
      }

      string v1 = "_.-VPOINT\n1,0,0\n._VIEWBASE\n_M\n_E\nLayout1\n_O\n_Current\n100,100\n\n\n";
      string v2 = "_.-VPOINT\n0,1,0\n._VIEWBASE\n_M\n_E\nLayout1\n_O\n_Current\n400,100\n\n\n";
      string cmd = v1 + v2;
      doc.SendStringToExecute(cmd, true, false, false);
      PlantOrthoView.FileDiag("PFSVB2 posted bundle cmd=" + cmd.Replace("\n", "|"));
      ed.WriteMessage("\nPFSVB2 posted 2-view bundle");
    }

    [CommandMethod("PFSVB2M")]
    public void Vb2ModelCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      Editor ed = doc.Editor;

      s_viewbaseBaseline = new System.Collections.Generic.HashSet<ObjectId>();
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(doc.Database, tr, "Layout1");
        if (!layoutBtrId.IsNull)
          this.SnapshotBlockTableRecordIds(tr, layoutBtrId, s_viewbaseBaseline);
        tr.Commit();
      }

      string v1 = "_.MODEL\n_.-VPOINT\n1,0,0\n._VIEWBASE\n_M\n_E\nLayout1\n_O\n_Current\n100,100\n\n\n";
      string v2 = "_.MODEL\n_.-VPOINT\n0,1,0\n._VIEWBASE\n_M\n_E\nLayout1\n_O\n_Current\n400,100\n\n\n";
      string cmd = v1 + v2;
      doc.SendStringToExecute(cmd, true, false, false);
      PlantOrthoView.FileDiag("PFSVB2M posted bundle cmd=" + cmd.Replace("\n", "|"));
      ed.WriteMessage("\nPFSVB2M posted 2-view bundle (MODEL switch)");
    }

    [CommandMethod("PFSVBMULTI")]
    public void VbMultiCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      Editor ed = doc.Editor;
      PromptPointResult p1 = ed.GetPoint("\n파이프 축 시작점 지정: ");
      if (p1.Status != PromptStatus.OK)
      {
        PlantOrthoView.FileDiag("PFSVBMULTI: p1 취소");
        return;
      }

      PromptPointOptions opt2 = new PromptPointOptions("\n파이프 축 끝점 지정: ");
      opt2.UseBasePoint = true;
      opt2.BasePoint = p1.Value;
      PromptPointResult p2 = ed.GetPoint(opt2);
      if (p2.Status != PromptStatus.OK)
      {
        PlantOrthoView.FileDiag("PFSVBMULTI: p2 취소");
        return;
      }

      Vector3d v = p2.Value - p1.Value;
      if (v.Length < 1e-6)
      {
        PlantOrthoView.FileDiag("PFSVBMULTI: 축 길이 0");
        ed.WriteMessage("\n두 점 동일");
        return;
      }

      v = v.GetNormal();
      Vector3d g = Vector3d.ZAxis;
      Vector3d upMain = g - (g.DotProduct(v)) * v;
      if (upMain.Length < 1e-6)
      {
        g = Vector3d.XAxis;
        upMain = g - (g.DotProduct(v)) * v;
      }
      if (upMain.Length < 1e-6)
      {
        g = Vector3d.YAxis;
        upMain = g - (g.DotProduct(v)) * v;
      }
      upMain = upMain.GetNormal();

      var ic = System.Globalization.CultureInfo.InvariantCulture;
      System.Func<Vector3d, string> fmt = vec =>
        vec.X.ToString("0.######", ic) + "," + vec.Y.ToString("0.######", ic) + "," + vec.Z.ToString("0.######", ic);
      string vpMain = fmt(v);
      string vpTop = fmt(upMain);

      s_viewbaseBaseline = new System.Collections.Generic.HashSet<ObjectId>();
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(doc.Database, tr, "Layout1");
        if (!layoutBtrId.IsNull)
          this.SnapshotBlockTableRecordIds(tr, layoutBtrId, s_viewbaseBaseline);
        tr.Commit();
      }

      string main = "_.MODEL\n_.-VPOINT\n" + vpMain + "\n._VIEWBASE\n_M\n_E\nLayout1\n_O\n_Current\n100,100\n\n\n";
      string top = "_.MODEL\n_.-VPOINT\n" + vpTop + "\n._VIEWBASE\n_M\n_E\nLayout1\n_O\n_Current\n400,100\n\n\n";
      string iso = "_.MODEL\n._VIEWBASE\n_M\n_E\nLayout1\n_O\n_SE\n700,100\n\n\n";
      string cmd = main + top + iso;
      doc.SendStringToExecute(cmd, true, false, false);
      PlantOrthoView.FileDiag("PFSVBMULTI vMain=" + vpMain + " vTop=" + vpTop + " cmd=" + cmd.Replace("\n", "|"));
      ed.WriteMessage("\nPFSVBMULTI posted 3-view bundle (Main/Top/ISO)");
    }

    [CommandMethod("PFSVBSUPPORT")]
    public void VbSupportCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      Editor ed = doc.Editor;
      PromptEntityOptions peo = new PromptEntityOptions("\n서포트 선택: ");
      peo.SetRejectMessage("\nSupport 객체가 아닙니다.");
      peo.AddAllowedClass(typeof(Support), false);
      PromptEntityResult per = ed.GetEntity(peo);
      if (per.Status != PromptStatus.OK)
      {
        PlantOrthoView.FileDiag("PFSVBSUPPORT: 선택 취소");
        return;
      }

      ed.SetImpliedSelection(new ObjectId[0]);

      Vector3d s1 = Vector3d.ZAxis;
      bool found = false;
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        DBObject obj = tr.GetObject(per.ObjectId, OpenMode.ForRead);
        Support sup = obj as Support;
        if (sup != null)
        {
          PortCollection ports = ((Part)sup).GetPorts((PortType)7);
          foreach (Port port in (PnP3dCollection)ports)
          {
            if (port.Name == "S1")
            {
              s1 = port.Direction;
              found = true;
              break;
            }
          }
        }
        tr.Commit();
      }

      if (!found)
      {
        PlantOrthoView.FileDiag("PFSVBSUPPORT: S1 포트 없음");
        ed.WriteMessage("\nS1 포트 없음");
        return;
      }
      if (s1.Length < 1e-6)
      {
        PlantOrthoView.FileDiag("PFSVBSUPPORT: S1 길이 0");
        return;
      }

      s1 = s1.GetNormal();
      var ic = System.Globalization.CultureInfo.InvariantCulture;
      string vp = s1.X.ToString("0.######", ic) + "," + s1.Y.ToString("0.######", ic) + "," + s1.Z.ToString("0.######", ic);

      s_viewbaseBaseline = new System.Collections.Generic.HashSet<ObjectId>();
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(doc.Database, tr, "Layout1");
        if (!layoutBtrId.IsNull)
          this.SnapshotBlockTableRecordIds(tr, layoutBtrId, s_viewbaseBaseline);
        tr.Commit();
      }

      string cmd = "_.MODEL\n_.-VPOINT\n" + vp + "\n._VIEWBASE\n_M\n_E\nLayout1\n_O\n_Current\n100,100\n\n\n";
      doc.SendStringToExecute(cmd, true, false, false);
      PlantOrthoView.FileDiag("PFSVBSUPPORT s1=" + vp + " cmd=" + cmd.Replace("\n", "|"));
      ed.WriteMessage("\nPFSVBSUPPORT s1=" + vp);
    }

    // ---- Stage A 게이트 공용 헬퍼 ----
    // 선택 획득 + Layout1 baseline 스냅샷. 실패 시 false.
    private bool VbIsoPrepare(Document doc, Editor ed, string tag, out ObjectId[] ids)
    {
      ids = null;
      PromptSelectionResult psr = ed.GetSelection();
      if (psr.Status != PromptStatus.OK || psr.Value == null || psr.Value.Count == 0)
      {
        PlantOrthoView.FileDiag(tag + ": 선택 취소/빈 선택");
        return false;
      }
      ids = psr.Value.GetObjectIds();
      // 공간 상태 로그: TILEMODE 1=Model 탭, 0=Paper/Layout. 판정 오염 감지용.
      short tileMode = (short)Application.GetSystemVariable("TILEMODE");
      PlantOrthoView.FileDiag(tag + " space TILEMODE=" + tileMode + (tileMode == 1 ? " (Model)" : " (Paper/Layout)"));
      s_viewbaseBaseline = new System.Collections.Generic.HashSet<ObjectId>();
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(doc.Database, tr, "Layout1");
        if (!layoutBtrId.IsNull)
          this.SnapshotBlockTableRecordIds(tr, layoutBtrId, s_viewbaseBaseline);
        tr.Commit();
      }
      return true;
    }

    // Stage A-1: _.MODEL 없음 = 순수 pickfirst 소비 판정(사용자가 Model 탭 상태여야 함).
    [CommandMethod("PFSVBISO")]
    public void VbIsoCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;
      Editor ed = doc.Editor;
      ObjectId[] ids;
      if (!this.VbIsoPrepare(doc, ed, "PFSVBISO", out ids))
        return;
      // PFSVBISO는 _.MODEL 미포함 → Model 탭 전제. Paper/Layout이면 판정 오염 경고(중단은 안 함, 판정 로그로 분리).
      if ((short)Application.GetSystemVariable("TILEMODE") != 1)
      {
        PlantOrthoView.FileDiag("PFSVBISO 경고: Model 탭 아님(TILEMODE!=1) → pickfirst 판정 오염 가능. PFSVBISOM 사용 권장.");
        ed.WriteMessage("\n[경고] Model 탭이 아님 → PFSVBISO 판정 오염 가능. Model 탭에서 재실행하거나 PFSVBISOM 사용.");
      }

      ed.SetImpliedSelection(ids);
      // _.MODEL·_E 모두 제거: MODEL 전환 confound 없이 pickfirst 소비만 판정.
      string cmd = "._VIEWBASE\n_M\nLayout1\n_O\n_Current\n100,100\n\n\n";
      doc.SendStringToExecute(cmd, true, false, false);
      PlantOrthoView.FileDiag("PFSVBISO ids=" + ids.Length + " modelSwitch=NO cmd=" + cmd.Replace("\n", "|"));
      ed.WriteMessage("\nPFSVBISO posted (no _.MODEL, no _E) ids=" + ids.Length);
    }

    // Stage A-1M: _.MODEL 포함 = 모델 복귀 후 pickfirst 소비 판정. A-1과 대조해 confound 격리.
    [CommandMethod("PFSVBISOM")]
    public void VbIsoModelCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;
      Editor ed = doc.Editor;
      ObjectId[] ids;
      if (!this.VbIsoPrepare(doc, ed, "PFSVBISOM", out ids))
        return;

      ed.SetImpliedSelection(ids);
      string cmd = "_.MODEL\n._VIEWBASE\n_M\nLayout1\n_O\n_Current\n100,100\n\n\n";
      doc.SendStringToExecute(cmd, true, false, false);
      PlantOrthoView.FileDiag("PFSVBISOM ids=" + ids.Length + " modelSwitch=YES cmd=" + cmd.Replace("\n", "|"));
      ed.WriteMessage("\nPFSVBISOM posted (_.MODEL, no _E) ids=" + ids.Length);
    }

    // Stage A-2: 명시 선택 경로. _S(Select objects) → _P(Previous). 런타임 의존(비확정).
    [CommandMethod("PFSVBISOS")]
    public void VbIsoSelectCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;
      Editor ed = doc.Editor;
      ObjectId[] ids;
      if (!this.VbIsoPrepare(doc, ed, "PFSVBISOS", out ids))
        return;

      ed.SetImpliedSelection(ids);
      string cmd = "_.MODEL\n._VIEWBASE\n_M\n_S\n_P\n\nLayout1\n_O\n_Current\n100,100\n\n\n";
      doc.SendStringToExecute(cmd, true, false, false);
      PlantOrthoView.FileDiag("PFSVBISOS ids=" + ids.Length + " modelSwitch=YES cmd=" + cmd.Replace("\n", "|"));
      ed.WriteMessage("\nPFSVBISOS posted (_M _S _P) ids=" + ids.Length);
    }

    // Stage A-fix: 견고화 probe. §9(Gemini+Codex) 수렴 반영.
    // Session+Idle 클린 컨텍스트 / _M 제거(model source 기본 Enter 수락, MOVE 붕괴 차단) /
    // 트레일링 Enter 정리 / CMDNAMES 가드 / Idle서 SetImpliedSelection 재확정.
    private static ObjectId[] s_vbIso2Ids;

    [CommandMethod("PFSVBISO2", CommandFlags.Session)]
    public void VbIso2Command()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;
      Editor ed = doc.Editor;
      ObjectId[] ids;
      if (!this.VbIsoPrepare(doc, ed, "PFSVBISO2", out ids))
        return;
      ed.SetImpliedSelection(ids);
      s_vbIso2Ids = ids;
      Application.Idle += this.VbIso2OnIdle;
      PlantOrthoView.FileDiag("PFSVBISO2 armed(Idle 대기) ids=" + ids.Length);
      ed.WriteMessage("\nPFSVBISO2 armed(Idle) ids=" + ids.Length);
    }

    private void VbIso2OnIdle(object sender, System.EventArgs e)
    {
      Application.Idle -= this.VbIso2OnIdle; // 일회성 보장
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;
      // 클린 컨텍스트 확인: 진행 중 명령 없어야 함.
      object cmdNames = Application.GetSystemVariable("CMDNAMES");
      string cmdStr = cmdNames == null ? "" : cmdNames.ToString();
      short grips = (short)Application.GetSystemVariable("GRIPS");
      short pickfirst = (short)Application.GetSystemVariable("PICKFIRST");
      short tileMode = (short)Application.GetSystemVariable("TILEMODE");
      PlantOrthoView.FileDiag("PFSVBISO2 Idle 발화 CMDNAMES='" + cmdStr + "' GRIPS=" + grips + " PICKFIRST=" + pickfirst + " TILEMODE=" + tileMode);
      // CMDNAMES는 Idle 발화 시점에 자기 자신(PFSVBISO2, Session 명령)을 포함할 수 있음(정상).
      // SendStringToExecute는 큐잉되어 PFSVBISO2 종료 후 실행되므로 자기 이름은 클린으로 간주.
      // 외부 명령(MOVE 등)이 진행 중일 때만 desync 방지 차단.
      if (!string.IsNullOrEmpty(cmdStr) && cmdStr.IndexOf("PFSVBISO2", System.StringComparison.OrdinalIgnoreCase) < 0)
      {
        PlantOrthoView.FileDiag("PFSVBISO2 중단: 외부 명령 진행 중(CMDNAMES='" + cmdStr + "') - desync 방지, 미실행");
        return;
      }
      // Idle 사이 pickfirst 유실 대비 재확정(+Previous 등록).
      if (s_vbIso2Ids != null && s_vbIso2Ids.Length > 0)
        doc.Editor.SetImpliedSelection(s_vbIso2Ids);
      // 스트림 = run1 검증형 복원. VIEWBASE가 Session+Idle로 안정 기동하므로 _M은 안전하게
      // model-source 옵션으로 소비(MOVE 붕괴는 미기동 시에만 발생, 이제 해소). pickfirst 자동소비
      // ("N found") → 선택셋만 투영. 트레일링 Enter는 run1 통과형 유지.
      string cmd = "._VIEWBASE\n_M\nLayout1\n_O\n_Current\n100,100\n\n\n";
      doc.SendStringToExecute(cmd, true, false, false);
      PlantOrthoView.FileDiag("PFSVBISO2 posted(Idle) cmd=" + cmd.Replace("\n", "|"));
    }

    // Stage B1: ed.Command(동기)로 VIEWBASE _E 반복 결정성 프로브. SendStringToExecute 미사용.
    // 현재 문서·전체모델 대상(격리 아직 아님). FILEDIA/CMDDIA=0 복구, viewrep 생성 카운트.
    [CommandMethod("PFSVBCMD")]
    public void VbCmdCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;
      Editor ed = doc.Editor;

      // baseline(Layout1) 스냅샷
      var baseline = new System.Collections.Generic.HashSet<ObjectId>();
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(doc.Database, tr, "Layout1");
        if (!layoutBtrId.IsNull)
          this.SnapshotBlockTableRecordIds(tr, layoutBtrId, baseline);
        tr.Commit();
      }

      object oldFiledia = Application.GetSystemVariable("FILEDIA");
      object oldCmddia = Application.GetSystemVariable("CMDDIA");
      short tileMode = (short)Application.GetSystemVariable("TILEMODE");
      PlantOrthoView.FileDiag("PFSVBCMD enter TILEMODE=" + tileMode + " baseline=" + baseline.Count);
      try
      {
        Application.SetSystemVariable("FILEDIA", 0);
        Application.SetSystemVariable("CMDDIA", 0);
        using (doc.LockDocument())
        {
          // ed.Command 동기: VIEWBASE _M(Model space) _E(Entire model) Layout1 _O(Orientation) _Current, 위치, 종료.
          // 주의: VIEWBASE 프롬프트 시퀀스에 맞춰 인자 수 정확히. base view 배치 후 projected view는 빈 Enter로 eXit.
          ed.Command("_.VIEWBASE", "_M", "_E", "Layout1", "_O", "_Current", new Point3d(100.0, 100.0, 0.0), "", "");
        }
        PlantOrthoView.FileDiag("PFSVBCMD ed.Command VIEWBASE 반환(동기 완주)");
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSVBCMD ed.Command 예외: " + ex.GetType().Name + ": " + ex.Message);
        ed.WriteMessage("\nPFSVBCMD error: " + ex.GetType().Name + ": " + ex.Message);
      }
      finally
      {
        Application.SetSystemVariable("FILEDIA", oldFiledia);
        Application.SetSystemVariable("CMDDIA", oldCmddia);
      }

      // 신규 엔티티 카운트(생성 확인)
      int added = 0;
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(doc.Database, tr, "Layout1");
        if (!layoutBtrId.IsNull)
        {
          BlockTableRecord btr = tr.GetObject(layoutBtrId, OpenMode.ForRead) as BlockTableRecord;
          if (btr != null)
            foreach (ObjectId id in btr)
              if (!baseline.Contains(id))
                added++;
        }
        tr.Commit();
      }
      PlantOrthoView.FileDiag("PFSVBCMD 신규 Layout1 엔티티=" + added);
      ed.WriteMessage("\nPFSVBCMD done, new Layout1 entities=" + added);
    }

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

      s_isoPipeAxisValid = false;
      s_isoPipeAxis = Vector3d.XAxis;
      s_isoPipeUp = Vector3d.ZAxis;
      s_isoPipeId = ObjectId.Null;
      s_isoRealWidth = 0.0;
      s_isoRealHeight = 0.0;
      s_isoRealSizeValid = false;
      s_isoPipeLineNo = string.Empty;
      s_isoBOP = string.Empty;
      s_isoSize = string.Empty;
      s_isoSupportTag = string.Empty;
      s_isoDesignStd = string.Empty;
      s_isoShortDesc = string.Empty;

      Editor ed = doc.Editor;
      PromptSelectionResult psr = ed.GetSelection();
      if (psr.Status != PromptStatus.OK || psr.Value == null || psr.Value.Count == 0)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL 선택 취소/빈 선택");
        ed.WriteMessage("\nPFSNOTABDETAIL: 선택 없음");
        return;
      }

      ObjectId[] selectedIds = psr.Value.GetObjectIds();
      Vector3d pipeAxis;
      ObjectId pipeId;
      s_isoPipeAxisValid = this.TryGetSelectionPipeAxis(doc.Database, selectedIds, out pipeAxis, out pipeId);
      s_isoPipeId = pipeId;
      s_isoPipeAxis = s_isoPipeAxisValid ? pipeAxis : Vector3d.XAxis;
      this.CaptureIsoSelectionMetrics(doc.Database, selectedIds);

      Database sourceDb = null;
      try
      {
        sourceDb = this.CloneSelectionToSideDatabase(doc.Database, selectedIds, "PFSNOTABDETAIL");
        System.Collections.Generic.List<ObjectId> solidIds = this.CollectNotabN1SolidIds(sourceDb);
        if (solidIds.Count == 0)
          throw new System.InvalidOperationException("solidIds 없음");

        string savedPath = this.CreateNotabDetailDrawing(sourceDb, solidIds);
        PlantOrthoView.FileDiag("PFSNOTABDETAIL done selected=" + selectedIds.Length + " solids=" + solidIds.Count + " axis=" + this.FormatVectorForCommand(s_isoPipeAxis) + " up=" + this.FormatVectorForCommand(s_isoPipeUp) + " saved=" + savedPath);
        ed.WriteMessage("\nPFSNOTABDETAIL saved=" + savedPath);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL 예외: " + ex.GetType().Name + ": " + ex.Message);
        ed.WriteMessage("\nPFSNOTABDETAIL error: " + ex.GetType().Name + ": " + ex.Message);
      }
      finally
      {
        if (sourceDb != null)
          sourceDb.Dispose();
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
      s_isoRealWidth = 0.0;
      s_isoRealHeight = 0.0;
      s_isoRealSizeValid = false;
      s_isoPipeLineNo = string.Empty;
      s_isoBOP = string.Empty;
      s_isoSize = string.Empty;
      s_isoSupportTag = string.Empty;
      s_isoDesignStd = string.Empty;
      s_isoShortDesc = string.Empty;
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

    private string CreateNotabDetailDrawing(Database sourceDb, System.Collections.Generic.List<ObjectId> solidIds)
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
        tag = s_isoShortDesc + "_" + System.DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
      if (string.IsNullOrWhiteSpace(tag))
        tag = "SUPPORT_" + System.DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
      string safeTag = this.SanitizeIsoFileName(tag);

      Database tagDb = null;
      Database oldWorking = HostApplicationServices.WorkingDatabase;
      string savedPath = null;
      try
      {
        tagDb = new Database(false, true);
        tagDb.ReadDwgFile(templatePath, System.IO.FileShare.Read, true, null);
        HostApplicationServices.WorkingDatabase = tagDb;
        this.TrySetTagDatabaseFacetres(tagDb, 10.0);

        Extents3d solidExt;
        int cloned;
        using (Transaction tr = tagDb.TransactionManager.StartTransaction())
        {
          ObjectId msId = this.GetModelSpaceId(tagDb, tr);
          if (msId == ObjectId.Null)
            throw new System.InvalidOperationException("template ModelSpace 없음");

          ObjectId detailLayerId = this.EnsureIsoDetailLayer(tagDb, tr);
          IdMapping idMap = new IdMapping();
          ObjectIdCollection ids = new ObjectIdCollection();
          foreach (ObjectId id in solidIds)
            ids.Add(id);
          sourceDb.WblockCloneObjects(ids, msId, idMap, DuplicateRecordCloning.Ignore, false);

          cloned = this.PrepareClonedNotabSolids(tr, idMap, detailLayerId, out solidExt);
          if (cloned == 0)
            throw new System.InvalidOperationException("cloned solid/extents 없음");

          bool viewportOk = this.CreateNotabDetailViewport(tr, tagDb, solidExt);
          bool titleblockOk = this.UpdateIsoTitleBlockAttributes(tr, tagDb, tag);
          PlantOrthoView.FileDiag("PFSNOTABDETAIL template cloned=" + cloned + " viewport=" + (viewportOk ? "ok" : "skip") + " titleblock=" + (titleblockOk ? "ok" : "skip") + " ext=" + this.FormatExtents(solidExt));
          tr.Commit();
        }

        string detailsDir = this.GetIsoDetailsDirectory();
        if (string.IsNullOrWhiteSpace(detailsDir))
          throw new System.InvalidOperationException("Details dir 없음");

        System.IO.Directory.CreateDirectory(detailsDir);
        savedPath = System.IO.Path.Combine(detailsDir, safeTag + "_notab.dwg");
        if (System.IO.File.Exists(savedPath))
          System.IO.File.Delete(savedPath);
        tagDb.SaveAs(savedPath, DwgVersion.Current);
        PlantOrthoView.FileDiag("PFSNOTABDETAIL saved path=" + savedPath + " tag=" + tag);
        return savedPath;
      }
      finally
      {
        HostApplicationServices.WorkingDatabase = oldWorking;
        if (tagDb != null)
          tagDb.Dispose();
      }
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

    private bool CreateNotabDetailViewport(Transaction tr, Database db, Extents3d solidExt)
    {
      if (tr == null || db == null)
        return false;

      try
      {
        ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(db, tr, "Title Block");
        if (layoutBtrId == ObjectId.Null)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport skip: Title Block layout 없음");
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
          PlantOrthoView.FileDiag("PFSNOTABDETAIL plotArea Limits 예외: " + ex.GetType().Name + ": " + ex.Message);
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

        Point3d target = new Point3d(
          (solidExt.MinPoint.X + solidExt.MaxPoint.X) / 2.0,
          (solidExt.MinPoint.Y + solidExt.MaxPoint.Y) / 2.0,
          (solidExt.MinPoint.Z + solidExt.MaxPoint.Z) / 2.0);
        Vector3d viewDir = s_isoPipeAxisValid && s_isoPipeAxis.Length > 1e-6 ? s_isoPipeAxis.GetNormal() : Vector3d.XAxis;
        Vector3d viewUp = s_isoPipeUp.Length > 1e-6 ? s_isoPipeUp.GetNormal() : Vector3d.ZAxis;
        double twist = this.ComputeNotabViewportTwist(viewDir, viewUp);

        Viewport vp = new Viewport();
        layoutBtr.AppendEntity(vp);
        tr.AddNewlyCreatedDBObject(vp, true);
        vp.CenterPoint = new Point3d(tcx, tcy, 0.0);
        vp.Width = tw;
        vp.Height = th;
        vp.ViewTarget = target;
        vp.ViewDirection = viewDir;
        vp.ViewCenter = new Point2d(0.0, 0.0);
        vp.TwistAngle = twist;
        vp.CustomScale = DetailViewScale;
        vp.On = true;
        bool hiddenOk = this.TryApplyHiddenVisualStyle(db, tr, vp);
        bool shadeOk = this.TrySetViewportShadePlotHidden(vp);
        PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport viewDir=" + this.FormatVectorForCommand(viewDir) + " target=(" + this.FormatNumber(target.X) + "," + this.FormatNumber(target.Y) + "," + this.FormatNumber(target.Z) + ") twist=" + this.FormatNumber(twist) + " hidden=" + hiddenOk + " shadePlot=" + shadeOk + " scale=1:2 plotArea=(" + this.FormatNumber(lminx) + "," + this.FormatNumber(lminy) + ")~(" + this.FormatNumber(lmaxx) + "," + this.FormatNumber(lmaxy) + ") source=" + plotSource);
        return true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL viewport 예외: " + ex.GetType().Name + ": " + ex.Message);
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

    private bool TryApplyHiddenVisualStyle(Database db, Transaction tr, Viewport vp)
    {
      try
      {
        DBDictionary vsDict = tr.GetObject(db.VisualStyleDictionaryId, OpenMode.ForRead) as DBDictionary;
        if (vsDict == null)
          return false;

        foreach (DBDictionaryEntry entry in vsDict)
        {
          string name = System.Convert.ToString(entry.Key, System.Globalization.CultureInfo.InvariantCulture);
          if (name.IndexOf("Hidden", System.StringComparison.OrdinalIgnoreCase) < 0)
            continue;

          vp.VisualStyleId = entry.Value;
          PlantOrthoView.FileDiag("PFSNOTABDETAIL Hidden visualStyle=" + name + " id=" + entry.Value);
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
    [CommandMethod("PFSCOUNTVIEW")]
    public void CountViewCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      Editor ed = doc.Editor;
      var baseline = s_viewbaseBaseline ?? new System.Collections.Generic.HashSet<ObjectId>();
      int line = 0;
      int circle = 0;
      int arc = 0;
      int viewrepLine = 0;
      int viewrepCircle = 0;
      int viewrepArc = 0;
      var viewTypes = new System.Collections.Generic.List<string>();
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(doc.Database, tr, "Layout1");
        if (layoutBtrId.IsNull)
        {
          PlantOrthoView.FileDiag("PFSCOUNTVIEW: Layout1 없음");
          ed.WriteMessage("\nPFSCOUNTVIEW: Layout1 없음");
          tr.Commit();
          return;
        }

        BlockTableRecord layoutBtr = tr.GetObject(layoutBtrId, OpenMode.ForRead) as BlockTableRecord;
        if (layoutBtr != null)
        {
          foreach (ObjectId id in layoutBtr)
          {
            if (baseline.Contains(id))
              continue;

            DBObject obj = tr.GetObject(id, OpenMode.ForRead, false);
            if (obj == null)
              continue;

            viewTypes.Add(obj.GetType().Name);
            Entity entity = obj as Entity;
            if (entity == null)
              continue;

            if (entity.GetType().Name == "ViewBorder")
            {
              if (this.TryCountViewRepGeometry(tr, entity, ref viewrepLine, ref viewrepCircle, ref viewrepArc))
                continue;
            }

            this.CountExplodedGeometry(tr, entity, ref line, ref circle, ref arc);
          }
        }
        tr.Commit();
      }

      string types = viewTypes.Count == 0 ? "none" : string.Join(" ", viewTypes.ToArray()).Trim();
      PlantOrthoView.FileDiag("PFSCOUNTVIEW newTypes=[" + types + "] explode Line=" + line + " Circle=" + circle + " Arc=" + arc + " viewrep Line=" + viewrepLine + " Circle=" + viewrepCircle + " Arc=" + viewrepArc);
      ed.WriteMessage("\nPFSCOUNTVIEW types=[" + types + "] Line=" + line + " Circle=" + circle + " Arc=" + arc + " viewrep Line=" + viewrepLine + " Circle=" + viewrepCircle + " Arc=" + viewrepArc);
    }

    private bool TryCountViewRepGeometry(Transaction tr, Entity viewBorder, ref int line, ref int circle, ref int arc)
    {
      if (tr == null || viewBorder == null)
        return false;

      try
      {
        System.Reflection.PropertyInfo property = viewBorder.GetType().GetProperty("BlockId");
        if (property == null)
        {
          PlantOrthoView.FileDiag("PFSCOUNTVIEW viewrep BlockId property 없음 type=" + viewBorder.GetType().FullName);
          return false;
        }

        object value = property.GetValue(viewBorder, null);
        if (!(value is ObjectId))
        {
          PlantOrthoView.FileDiag("PFSCOUNTVIEW viewrep BlockId type mismatch=" + (value == null ? "null" : value.GetType().FullName));
          return false;
        }

        ObjectId blockId = (ObjectId)value;
        if (blockId.IsNull)
        {
          PlantOrthoView.FileDiag("PFSCOUNTVIEW viewrep BlockId null");
          return true;
        }

        DBObject blockObj = tr.GetObject(blockId, OpenMode.ForRead, false);
        PlantOrthoView.FileDiag("PFSCOUNTVIEW viewrep BlockId resolves to " + (blockObj == null ? "null" : blockObj.GetType().FullName));

        BlockReference br = blockObj as BlockReference;
        if (br != null)
        {
          this.CountExplodedGeometry(tr, br, ref line, ref circle, ref arc);
          try
          {
            Extents3d extents = br.GeometricExtents;
            PlantOrthoView.FileDiag("PFSCOUNTVIEW viewrep(BRef) tf=" + br.BlockTransform + " extents=" + extents.MinPoint + "~" + extents.MaxPoint);
          }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSCOUNTVIEW viewrep extents 예외: " + ex.GetType().Name + ": " + ex.Message);
          }
          return true;
        }

        BlockTableRecord btr = blockObj as BlockTableRecord;
        if (btr != null)
        {
          int members = 0;
          foreach (ObjectId gid in btr)
          {
            Entity geometry = tr.GetObject(gid, OpenMode.ForRead, false) as Entity;
            if (geometry == null)
              continue;

            this.CountExplodedGeometry(tr, geometry, ref line, ref circle, ref arc);
            members++;
          }
          PlantOrthoView.FileDiag("PFSCOUNTVIEW viewrep(BTR) name=" + btr.Name + " members=" + members);
          return true;
        }

        PlantOrthoView.FileDiag("PFSCOUNTVIEW viewrep BlockId 미지원 타입");
        return true;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSCOUNTVIEW viewrep 예외: " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
    }

    private ObjectId GetLayoutBlockTableRecordId(Database db, Transaction tr, string layoutName)
    {
      if (db == null || tr == null || string.IsNullOrEmpty(layoutName))
        return ObjectId.Null;

      DBDictionary layouts = tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
      if (layouts == null || !layouts.Contains(layoutName))
        return ObjectId.Null;

      Layout layout = tr.GetObject(layouts.GetAt(layoutName), OpenMode.ForRead) as Layout;
      return layout == null ? ObjectId.Null : layout.BlockTableRecordId;
    }

    private void SnapshotBlockTableRecordIds(Transaction tr, ObjectId btrId, System.Collections.Generic.HashSet<ObjectId> ids)
    {
      if (tr == null || btrId.IsNull || ids == null)
        return;

      BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
      if (btr == null)
        return;

      foreach (ObjectId id in btr)
        ids.Add(id);
    }

    private void CountExplodedGeometry(Transaction tr, Entity entity, ref int line, ref int circle, ref int arc)
    {
      if (tr == null || entity == null)
        return;

      if (entity is Line)
      {
        line++;
        return;
      }
      if (entity is Circle)
      {
        circle++;
        return;
      }
      if (entity is Arc)
      {
        arc++;
        return;
      }

      DBObjectCollection exploded = new DBObjectCollection();
      try
      {
        entity.Explode(exploded);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSCOUNTVIEW explode 예외 type=" + entity.GetType().Name + ": " + ex.GetType().Name + ": " + ex.Message);
      }

      if (exploded.Count > 0)
      {
        foreach (DBObject child in exploded)
        {
          Entity childEntity = child as Entity;
          if (childEntity != null)
            this.CountExplodedGeometry(tr, childEntity, ref line, ref circle, ref arc);
          child.Dispose();
        }
        return;
      }

      BlockReference br = entity as BlockReference;
      if (br == null || br.BlockTableRecord.IsNull)
        return;

      BlockTableRecord def = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
      if (def == null)
        return;

      foreach (ObjectId childId in def)
      {
        Entity childEntity = tr.GetObject(childId, OpenMode.ForRead, false) as Entity;
        if (childEntity != null)
          this.CountExplodedGeometry(tr, childEntity, ref line, ref circle, ref arc);
      }
    }

    [CommandMethod("PFSEXPORTLAYOUT")]
    public void ExportLayoutCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      Editor ed = doc.Editor;
      try
      {
        string directory = System.IO.Path.GetDirectoryName(ExportLayoutPath);
        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
          System.IO.Directory.CreateDirectory(directory);
        if (System.IO.File.Exists(ExportLayoutPath))
          System.IO.File.Delete(ExportLayoutPath);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSEXPORTLAYOUT prepare 예외: " + ex.GetType().Name + ": " + ex.Message);
        ed.WriteMessage("\nPFSEXPORTLAYOUT prepare error: " + ex.GetType().Name + ": " + ex.Message);
        return;
      }

      doc.SendStringToExecute("_.FILEDIA\n0\n_.EXPORTLAYOUT\n" + ExportLayoutPath + "\n_.FILEDIA\n1\n", true, false, false);
      PlantOrthoView.FileDiag("PFSEXPORTLAYOUT queued -> " + ExportLayoutPath);
      ed.WriteMessage("\nPFSEXPORTLAYOUT queued -> " + ExportLayoutPath);
    }

    [CommandMethod("PFSREADEXPORT")]
    public void ReadExportCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      Editor ed = doc == null ? null : doc.Editor;
      int line = 0;
      int circle = 0;
      int arc = 0;
      int members = 0;
      string source = "none";
      Database targetDb = null;

      try
      {
        string want = System.IO.Path.GetFullPath(ExportLayoutPath);
        foreach (Document exportDoc in Application.DocumentManager)
        {
          string docName = exportDoc.Name;
          if (!string.IsNullOrEmpty(docName) && string.Equals(System.IO.Path.GetFullPath(docName), want, System.StringComparison.OrdinalIgnoreCase))
          {
            targetDb = exportDoc.Database;
            source = "open-doc";
            break;
          }
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSREADEXPORT doc-scan 예외: " + ex.GetType().Name + ": " + ex.Message);
      }

      Database sideDb = null;
      try
      {
        if (targetDb == null)
        {
          if (!System.IO.File.Exists(ExportLayoutPath))
          {
            PlantOrthoView.FileDiag("PFSREADEXPORT: export 파일 없음 " + ExportLayoutPath);
            ed?.WriteMessage("\nPFSREADEXPORT: export 파일 없음 " + ExportLayoutPath);
            return;
          }

          string directory = System.IO.Path.GetDirectoryName(ExportLayoutPath);
          string copyPath = System.IO.Path.Combine(directory, "pfs_vb_export_copy.dwg");
          try
          {
            if (System.IO.File.Exists(copyPath))
              System.IO.File.Delete(copyPath);
          }
          catch (System.Exception ex)
          {
            PlantOrthoView.FileDiag("PFSREADEXPORT copy cleanup 예외: " + ex.GetType().Name + ": " + ex.Message);
          }

          System.IO.File.Copy(ExportLayoutPath, copyPath, true);
          sideDb = new Database(false, true);
          sideDb.ReadDwgFile(copyPath, System.IO.FileShare.Read, true, null);
          targetDb = sideDb;
          source = "file-copy";
        }

        using (Transaction tr = targetDb.TransactionManager.StartTransaction())
        {
          BlockTable bt = tr.GetObject(targetDb.BlockTableId, OpenMode.ForRead) as BlockTable;
          if (bt == null || !bt.Has(BlockTableRecord.ModelSpace))
          {
            PlantOrthoView.FileDiag("PFSREADEXPORT: ModelSpace 없음 source=" + source);
            tr.Commit();
            return;
          }

          BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
          if (ms != null)
          {
            foreach (ObjectId id in ms)
            {
              Entity entity = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
              if (entity == null)
                continue;

              members++;
              this.CountExplodedGeometry(tr, entity, ref line, ref circle, ref arc);
            }
          }
          tr.Commit();
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSREADEXPORT 예외: " + ex.GetType().Name + ": " + ex.Message + " source=" + source);
        ed?.WriteMessage("\nPFSREADEXPORT error: " + ex.GetType().Name + ": " + ex.Message);
        return;
      }
      finally
      {
        if (sideDb != null)
          sideDb.Dispose();
      }

      PlantOrthoView.FileDiag("PFSREADEXPORT exp Line=" + line + " Circle=" + circle + " Arc=" + arc + " members=" + members + " source=" + source);
      ed?.WriteMessage("\nPFSREADEXPORT exp Line=" + line + " Circle=" + circle + " Arc=" + arc + " members=" + members + " source=" + source);
    }

    [CommandMethod("PFSPLACEEXPORT")]
    public void PlaceExportCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null)
        return;

      Editor ed = doc.Editor;
      Database targetDb = doc.Database;
      int cloned = 0;
      string source = "none";
      Database sourceDb = null;
      Database sideDb = null;

      try
      {
        if (!System.IO.File.Exists(ExportLayoutPath))
        {
          PlantOrthoView.FileDiag("PFSPLACEEXPORT: export 파일 없음 " + ExportLayoutPath);
          ed.WriteMessage("\nPFSPLACEEXPORT: export 파일 없음 " + ExportLayoutPath);
          return;
        }

        try
        {
          sideDb = new Database(false, true);
          sideDb.ReadDwgFile(ExportLayoutPath, System.IO.FileShare.Read, true, null);
          sourceDb = sideDb;
          source = "side-direct";
        }
        catch (System.Exception ex1)
        {
          PlantOrthoView.FileDiag("PFSPLACEEXPORT side-direct 실패: " + ex1.GetType().Name + ": " + ex1.Message);
          if (sideDb != null)
          {
            sideDb.Dispose();
            sideDb = null;
          }

          try
          {
            string dir = System.IO.Path.GetDirectoryName(ExportLayoutPath);
            string copyPath = System.IO.Path.Combine(dir, "pfs_vb_export_copy.dwg");
            try
            {
              if (System.IO.File.Exists(copyPath))
                System.IO.File.Delete(copyPath);
            }
            catch (System.Exception cex)
            {
              PlantOrthoView.FileDiag("PFSPLACEEXPORT copy cleanup 예외: " + cex.GetType().Name + ": " + cex.Message);
            }

            System.IO.File.Copy(ExportLayoutPath, copyPath, true);
            sideDb = new Database(false, true);
            sideDb.ReadDwgFile(copyPath, System.IO.FileShare.Read, true, null);
            sourceDb = sideDb;
            source = "file-copy";
          }
          catch (System.Exception ex2)
          {
            PlantOrthoView.FileDiag("PFSPLACEEXPORT file-copy 실패: " + ex2.GetType().Name + ": " + ex2.Message);
            if (sideDb != null)
            {
              sideDb.Dispose();
              sideDb = null;
            }

            string want = System.IO.Path.GetFullPath(ExportLayoutPath);
            foreach (Document exportDoc in Application.DocumentManager)
            {
              string dn = exportDoc.Name;
              if (!string.IsNullOrEmpty(dn) && string.Equals(System.IO.Path.GetFullPath(dn), want, System.StringComparison.OrdinalIgnoreCase))
              {
                sourceDb = exportDoc.Database;
                source = "open-doc";
                break;
              }
            }
          }
        }

        if (sourceDb == null)
        {
          PlantOrthoView.FileDiag("PFSPLACEEXPORT: 소스 DB 확보 실패");
          ed.WriteMessage("\nPFSPLACEEXPORT: 소스 DB 확보 실패");
          return;
        }

        ObjectIdCollection ids = new ObjectIdCollection();
        using (Transaction str = sourceDb.TransactionManager.StartTransaction())
        {
          BlockTable sbt = str.GetObject(sourceDb.BlockTableId, OpenMode.ForRead) as BlockTable;
          if (sbt == null || !sbt.Has(BlockTableRecord.ModelSpace))
          {
            PlantOrthoView.FileDiag("PFSPLACEEXPORT: side ModelSpace 없음 source=" + source);
            str.Commit();
            return;
          }

          BlockTableRecord sms = str.GetObject(sbt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
          if (sms != null)
          {
            foreach (ObjectId id in sms)
              ids.Add(id);
          }
          str.Commit();
        }

        PlantOrthoView.FileDiag("PFSPLACEEXPORT side modelspace ids=" + ids.Count + " source=" + source);
        if (ids.Count == 0)
        {
          PlantOrthoView.FileDiag("PFSPLACEEXPORT: side modelspace 비어있음 source=" + source);
          ed.WriteMessage("\nPFSPLACEEXPORT: side modelspace 비어있음");
          return;
        }

        using (DocumentLock dlk = doc.LockDocument())
        using (Transaction ttr = targetDb.TransactionManager.StartTransaction())
        {
          BlockTable tbt = ttr.GetObject(targetDb.BlockTableId, OpenMode.ForRead) as BlockTable;
          if (tbt == null || !tbt.Has(BlockTableRecord.ModelSpace))
          {
            PlantOrthoView.FileDiag("PFSPLACEEXPORT: target ModelSpace 없음");
            ttr.Commit();
            return;
          }

          ObjectId msId = tbt[BlockTableRecord.ModelSpace];
          ObjectId flatLayerId = this.EnsureFlattenLayer(targetDb, ttr);
          Database oldWorking = HostApplicationServices.WorkingDatabase;
          IdMapping idMap = new IdMapping();
          try
          {
            HostApplicationServices.WorkingDatabase = targetDb;
            sourceDb.WblockCloneObjects(ids, msId, idMap, DuplicateRecordCloning.Replace, false);
          }
          finally
          {
            HostApplicationServices.WorkingDatabase = oldWorking;
          }

          foreach (IdPair pair in idMap)
          {
            if (!pair.IsPrimary || !pair.IsCloned)
              continue;

            Entity e = ttr.GetObject(pair.Value, OpenMode.ForWrite, false) as Entity;
            if (e == null)
              continue;

            e.LayerId = flatLayerId;
            cloned++;
          }
          ttr.Commit();
        }
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSPLACEEXPORT 예외: " + ex.GetType().Name + ": " + ex.Message + " source=" + source);
        ed.WriteMessage("\nPFSPLACEEXPORT error: " + ex.GetType().Name + ": " + ex.Message);
        return;
      }
      finally
      {
        if (sideDb != null)
          sideDb.Dispose();
      }

      PlantOrthoView.FileDiag("PFSPLACEEXPORT cloned=" + cloned + " layer=PFS_ORTHO_FLATTEN source=" + source);
      ed.WriteMessage("\nPFSPLACEEXPORT cloned=" + cloned + " source=" + source);
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
          dimH.DimensionText = this.FormatNumber(s_isoRealWidth);
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
            dimLeft.DimensionText = this.FormatNumber(leftReal);
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
            dimRight.DimensionText = this.FormatNumber(rightReal);
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
          dimV.DimensionText = this.FormatNumber(s_isoRealHeight);
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
        dim.Dimexe = h * 0.6;
        dim.Dimexo = h * 0.3;
        dim.Dimgap = h * 0.3;
        dim.Dimtih = false;
        dim.Dimtoh = false;
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
