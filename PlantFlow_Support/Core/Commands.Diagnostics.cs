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
  // Commands 부분 클래스: 무탭 계측/실험 명령군(클립박스·뷰포트·와이어프레임 테스트).
  // 제품 경로가 아니라 진단용이다. 출력 도면에 영향을 주지 않는다.
  public partial class Commands
  {
    [CommandMethod("PFSNOTABBOXTEST", CommandFlags.Session)]
    public void NotabBoxTestCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      Editor ed = doc != null ? doc.Editor : null;
      Database tdb = null;
      Database oldWorking = HostApplicationServices.WorkingDatabase;
      try
      {
        string templatePath = this.ResolveIsoTemplatePath();
        if (string.IsNullOrWhiteSpace(templatePath) || !System.IO.File.Exists(templatePath))
        {
          PlantOrthoView.FileDiag("PFSNOTABBOXTEST template 없음 path=" + (templatePath ?? string.Empty));
          if (ed != null)
            ed.WriteMessage("\nPFSNOTABBOXTEST: template 없음");
          return;
        }

        tdb = new Database(false, true);
        tdb.ReadDwgFile(templatePath, System.IO.FileShare.Read, true, null);
        tdb.CloseInput(true);
        HostApplicationServices.WorkingDatabase = tdb;

        using (Transaction tr = tdb.TransactionManager.StartTransaction())
        {
          ObjectId msId = this.GetModelSpaceId(tdb, tr);
          if (msId == ObjectId.Null)
            throw new System.InvalidOperationException("template ModelSpace 없음");

          BlockTableRecord ms = tr.GetObject(msId, OpenMode.ForWrite, false) as BlockTableRecord;
          if (ms == null)
            throw new System.InvalidOperationException("template ModelSpace open 실패");

          Solid3d box = new Solid3d();
          box.SetDatabaseDefaults(tdb);
          box.CreateBox(300.0, 300.0, 300.0);
          ms.AppendEntity(box);
          tr.AddNewlyCreatedDBObject(box, true);
          PlantOrthoView.FileDiag("PFSNOTABBOXTEST box appended, commit 직전");
          tr.Commit();
          PlantOrthoView.FileDiag("PFSNOTABBOXTEST commit 완료");
        }

        string tempDir = @"C:\Temp";
        System.IO.Directory.CreateDirectory(tempDir);
        string savedPath = System.IO.Path.Combine(tempDir, "notab_boxtest_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture) + ".dwg");
        tdb.SaveAs(savedPath, DwgVersion.Current);
        PlantOrthoView.FileDiag("PFSNOTABBOXTEST saved path=" + savedPath);
        if (ed != null)
          ed.WriteMessage("\nPFSNOTABBOXTEST saved=" + savedPath);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABBOXTEST 예외: " + ex.GetType().Name + ": " + ex.Message);
        if (ed != null)
          ed.WriteMessage("\nPFSNOTABBOXTEST error: " + ex.GetType().Name + ": " + ex.Message);
      }
      finally
      {
        HostApplicationServices.WorkingDatabase = oldWorking;
        if (tdb != null)
          tdb.Dispose();
      }
    }

    [CommandMethod("PFSNOTABBOXVPTEST", CommandFlags.Session)]
    public void NotabBoxViewportTestCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      Editor ed = doc != null ? doc.Editor : null;
      Database tdb = null;
      Database oldWorking = HostApplicationServices.WorkingDatabase;
      try
      {
        string templatePath = this.ResolveIsoTemplatePath();
        if (string.IsNullOrWhiteSpace(templatePath) || !System.IO.File.Exists(templatePath))
        {
          PlantOrthoView.FileDiag("PFSNOTABBOXVPTEST template 없음 path=" + (templatePath ?? string.Empty));
          if (ed != null)
            ed.WriteMessage("\nPFSNOTABBOXVPTEST: template 없음");
          return;
        }

        tdb = new Database(false, true);
        tdb.ReadDwgFile(templatePath, System.IO.FileShare.Read, true, null);
        tdb.CloseInput(true);
        HostApplicationServices.WorkingDatabase = tdb;

        using (Transaction tr = tdb.TransactionManager.StartTransaction())
        {
          ObjectId msId = this.GetModelSpaceId(tdb, tr);
          if (msId == ObjectId.Null)
            throw new System.InvalidOperationException("template ModelSpace 없음");

          BlockTableRecord ms = tr.GetObject(msId, OpenMode.ForWrite, false) as BlockTableRecord;
          if (ms == null)
            throw new System.InvalidOperationException("template ModelSpace open 실패");

          Solid3d box = new Solid3d();
          box.SetDatabaseDefaults(tdb);
          box.CreateBox(300.0, 300.0, 300.0);
          ms.AppendEntity(box);
          tr.AddNewlyCreatedDBObject(box, true);
          Extents3d boxExt = box.GeometricExtents;
          Point3d target = new Point3d(
            (boxExt.MinPoint.X + boxExt.MaxPoint.X) / 2.0,
            (boxExt.MinPoint.Y + boxExt.MaxPoint.Y) / 2.0,
            (boxExt.MinPoint.Z + boxExt.MaxPoint.Z) / 2.0);

          ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(tdb, tr, "Title Block");
          if (layoutBtrId == ObjectId.Null)
            throw new System.InvalidOperationException("Title Block layout 없음");

          BlockTableRecord layoutBtr = tr.GetObject(layoutBtrId, OpenMode.ForWrite, false) as BlockTableRecord;
          if (layoutBtr == null)
            throw new System.InvalidOperationException("Title Block layout open 실패");

          Viewport vp = new Viewport();
          layoutBtr.AppendEntity(vp);
          tr.AddNewlyCreatedDBObject(vp, true);
          vp.CenterPoint = new Point3d(420.0, 297.0, 0.0);
          vp.Width = 600.0;
          vp.Height = 380.0;
          vp.ViewDirection = Vector3d.YAxis;
          vp.ViewTarget = target;
          vp.ViewCenter = new Point2d(0.0, 0.0);
          vp.CustomScale = 0.5;
          vp.On = true;
          bool hiddenOk = this.TryApplyHiddenVisualStyle(tdb, tr, vp);
          bool shadeOk = this.TrySetViewportShadePlotHidden(vp);
          PlantOrthoView.FileDiag("PFSNOTABBOXVPTEST viewport hidden=" + hiddenOk + " shadePlot=" + shadeOk + " target=(" + this.FormatNumber(target.X) + "," + this.FormatNumber(target.Y) + "," + this.FormatNumber(target.Z) + ")");
          PlantOrthoView.FileDiag("PFSNOTABBOXVPTEST box+viewport, commit 직전");
          tr.Commit();
          PlantOrthoView.FileDiag("PFSNOTABBOXVPTEST commit 완료");
        }

        string tempDir = @"C:\Temp";
        System.IO.Directory.CreateDirectory(tempDir);
        string savedPath = System.IO.Path.Combine(tempDir, "notab_boxvp_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture) + ".dwg");
        tdb.SaveAs(savedPath, DwgVersion.Current);
        PlantOrthoView.FileDiag("PFSNOTABBOXVPTEST saved path=" + savedPath);
        if (ed != null)
          ed.WriteMessage("\nPFSNOTABBOXVPTEST saved=" + savedPath);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABBOXVPTEST 예외: " + ex.GetType().Name + ": " + ex.Message);
        if (ed != null)
          ed.WriteMessage("\nPFSNOTABBOXVPTEST error: " + ex.GetType().Name + ": " + ex.Message);
      }
      finally
      {
        HostApplicationServices.WorkingDatabase = oldWorking;
        if (tdb != null)
          tdb.Dispose();
      }
    }

    [CommandMethod("PFSNOTABBOXVPWIRE", CommandFlags.Session)]
    public void NotabBoxViewportWireCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      Editor ed = doc != null ? doc.Editor : null;
      Database tdb = null;
      Database oldWorking = HostApplicationServices.WorkingDatabase;
      try
      {
        string templatePath = this.ResolveIsoTemplatePath();
        if (string.IsNullOrWhiteSpace(templatePath) || !System.IO.File.Exists(templatePath))
        {
          PlantOrthoView.FileDiag("PFSNOTABBOXVPWIRE template 없음 path=" + (templatePath ?? string.Empty));
          if (ed != null)
            ed.WriteMessage("\nPFSNOTABBOXVPWIRE: template 없음");
          return;
        }

        tdb = new Database(false, true);
        tdb.ReadDwgFile(templatePath, System.IO.FileShare.Read, true, null);
        tdb.CloseInput(true);
        HostApplicationServices.WorkingDatabase = tdb;

        using (Transaction tr = tdb.TransactionManager.StartTransaction())
        {
          ObjectId msId = this.GetModelSpaceId(tdb, tr);
          if (msId == ObjectId.Null)
            throw new System.InvalidOperationException("template ModelSpace 없음");

          BlockTableRecord ms = tr.GetObject(msId, OpenMode.ForWrite, false) as BlockTableRecord;
          if (ms == null)
            throw new System.InvalidOperationException("template ModelSpace open 실패");

          Solid3d box = new Solid3d();
          box.SetDatabaseDefaults(tdb);
          box.CreateBox(300.0, 300.0, 300.0);
          ms.AppendEntity(box);
          tr.AddNewlyCreatedDBObject(box, true);
          Extents3d boxExt = box.GeometricExtents;
          Point3d target = new Point3d(
            (boxExt.MinPoint.X + boxExt.MaxPoint.X) / 2.0,
            (boxExt.MinPoint.Y + boxExt.MaxPoint.Y) / 2.0,
            (boxExt.MinPoint.Z + boxExt.MaxPoint.Z) / 2.0);

          ObjectId layoutBtrId = this.GetLayoutBlockTableRecordId(tdb, tr, "Title Block");
          if (layoutBtrId == ObjectId.Null)
            throw new System.InvalidOperationException("Title Block layout 없음");

          BlockTableRecord layoutBtr = tr.GetObject(layoutBtrId, OpenMode.ForWrite, false) as BlockTableRecord;
          if (layoutBtr == null)
            throw new System.InvalidOperationException("Title Block layout open 실패");

          Viewport vp = new Viewport();
          layoutBtr.AppendEntity(vp);
          tr.AddNewlyCreatedDBObject(vp, true);
          vp.CenterPoint = new Point3d(420.0, 297.0, 0.0);
          vp.Width = 600.0;
          vp.Height = 380.0;
          vp.ViewDirection = Vector3d.YAxis;
          vp.ViewTarget = target;
          vp.ViewCenter = new Point2d(0.0, 0.0);
          vp.CustomScale = 0.5;
          vp.On = true;
          bool shadeOk = this.TrySetViewportShadePlotHidden(vp);
          PlantOrthoView.FileDiag("PFSNOTABBOXVPWIRE viewport viewDir set, hidden=skip, shadePlot=" + shadeOk + ", commit 직전");
          tr.Commit();
          PlantOrthoView.FileDiag("PFSNOTABBOXVPWIRE commit 완료");
        }

        string tempDir = @"C:\Temp";
        System.IO.Directory.CreateDirectory(tempDir);
        string savedPath = System.IO.Path.Combine(tempDir, "notab_boxvpwire_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture) + ".dwg");
        tdb.SaveAs(savedPath, DwgVersion.Current);
        PlantOrthoView.FileDiag("PFSNOTABBOXVPWIRE saved path=" + savedPath);
        if (ed != null)
          ed.WriteMessage("\nPFSNOTABBOXVPWIRE saved=" + savedPath);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABBOXVPWIRE 예외: " + ex.GetType().Name + ": " + ex.Message);
        if (ed != null)
          ed.WriteMessage("\nPFSNOTABBOXVPWIRE error: " + ex.GetType().Name + ": " + ex.Message);
      }
      finally
      {
        HostApplicationServices.WorkingDatabase = oldWorking;
        if (tdb != null)
          tdb.Dispose();
      }
    }

  }
}
