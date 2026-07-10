using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;

#nullable disable
namespace PlantFlow_Support
{
  public partial class Commands
  {
    private static CustomPaletteSet palette;
    private static System.Collections.Generic.HashSet<ObjectId> s_viewbaseBaseline;
    private const string ExportLayoutPath = @"C:\TEMP\pfs_vb_export.dwg";
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

    [CommandMethod("PFS")]
    public void CmdPalette()
    {
      if (Commands.palette == null)
        Commands.palette = new CustomPaletteSet();
      ((Window) Commands.palette).Visible = true;
    }
  }
}
