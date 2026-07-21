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
  // Commands 부분 클래스: VIEWBASE 기반 오쏘/뷰 생성 명령군(레거시).
  // 무탭 엔진이 이 경로를 대체해 가는 중이므로 신규 작업은 Commands.Notab.* 쪽에 둔다.
  public partial class Commands
  {
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
  }
}
