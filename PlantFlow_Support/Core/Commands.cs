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
      foreach (Document document in Application.DocumentManager)
      {
        if (string.Compare(document.Name, Commands.DocumentName, true) == 0)
          Application.DocumentManager.MdiActiveDocument = document;
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
      }
      PlantOrthoView.FileDiag("PFSORTHOPHASE2 종료");
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

    [CommandMethod("PFS")]
    public void CmdPalette()
    {
      if (Commands.palette == null)
        Commands.palette = new CustomPaletteSet();
      ((Window) Commands.palette).Visible = true;
    }
  }
}
