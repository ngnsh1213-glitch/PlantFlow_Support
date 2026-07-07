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
      Editor editor = mdiActiveDocument.Editor;
      Database database = mdiActiveDocument.Database;
      PlantOrthoView.FileDiag("PFSORTHOUPDATESCALE 진입 doc='" + mdiActiveDocument.Name + "'");

      if (Commands.ViewportId == ObjectId.Null)
      {
          editor.WriteMessage("\nError: ViewportId is null. Skipping UpdateStandardScale.\n");
          return;
      }

      using (mdiActiveDocument.LockDocument())
      {
        using (Transaction transaction = database.TransactionManager.StartTransaction())
        {
          try
          {
              BlockTable blockTable = transaction.GetObject(database.BlockTableId, (OpenMode) 0) as BlockTable;
              BlockTableRecord blockTableRecord = transaction.GetObject(((SymbolTable) blockTable)[BlockTableRecord.ModelSpace], (OpenMode) 1) as BlockTableRecord;
              string currentLayout = LayoutManager.Current.CurrentLayout;
              DBDictionary dbDictionary = transaction.GetObject(database.LayoutDictionaryId, (OpenMode) 0) as DBDictionary;
              
              if (!dbDictionary.Contains(currentLayout)) {
                  editor.WriteMessage("\nError: Current layout '" + currentLayout + "' not found in dictionary.\n");
                  return;
              }

              Layout layout = transaction.GetObject((ObjectId) dbDictionary[currentLayout], (OpenMode) 0) as Layout;
              
              // Removed suspicious/useless line: transaction.GetObject(layout.BlockTableRecordId, (OpenMode) 1);

              double customScale = 1.0;
              try {
                  if (Commands.ViewportId == ObjectId.Null) {
                      editor.WriteMessage("\nWarning: ViewportId is Null in inner check.\n");
                  } else {
                      customScale = (transaction.GetObject(Commands.ViewportId, (OpenMode) 1) as Viewport).CustomScale;
                  }
              } catch (System.Exception ex) {
                  editor.WriteMessage("\nWarning: Could not get CustomScale from Viewport (" + ex.Message + "). Using default 1.0.\n");
              }

              foreach (ObjectId objectId in blockTableRecord)
              {
                try 
                {
                    DBObject dbObject = transaction.GetObject(objectId, (OpenMode) 1);
                    switch (dbObject)
                    {
                      case RotatedDimension _:
                        ((Dimension) dbObject).Dimscale = 1.0 / customScale;
                        continue;
                      case MLeader _:
                        ((MLeader) dbObject).Scale = 1.0 / customScale;
                        continue;
                      case BlockReference _:
                        BlockReference blockReference = (BlockReference) dbObject;
                        if (blockReference.Name == "DATUM_SYMBOL")
                        {
                          try 
                          {
                              Point3d position = blockReference.Position;
                              Matrix3d matrix3d = Matrix3d.Scaling(1.0 / customScale, position);
                              ((Entity) blockReference).TransformBy(matrix3d);
                          }
                          catch (Autodesk.AutoCAD.Runtime.Exception ex)
                          {
                              if (ex.ErrorStatus == ErrorStatus.CannotScaleNonUniformly)
                              {
                                  editor.WriteMessage("\nWarning: Cannot scale DATUM_SYMBOL non-uniformly. Skipped.\n");
                              }
                              else
                              {
                                  throw; 
                              }
                          }
                          continue;
                        }
                        continue;
                      default:
                        continue;
                    }
                }
                catch (System.Exception ex)
                {
                    editor.WriteMessage("\nError processing entity " + objectId + ": " + ex.Message + "\n");
                }
              }
              transaction.Commit();
          }
          catch (System.Exception ex)
          {
              editor.WriteMessage("\nCRITICAL EXCEPTION in UpdateStandardScale: " + ex.ToString() + "\n");
          }
        }
      }
      mdiActiveDocument.SendStringToExecute("._MSPACE\n", true, false, false);
      // Removed recursive call that causes infinite loop/freezing
      // mdiActiveDocument.SendStringToExecute("._AUTO2DPROCESSZOOM\n", true, false, false);
      mdiActiveDocument.SendStringToExecute("._PSPACE\n", true, false, false);
      // Removed unknown command
      // mdiActiveDocument.SendStringToExecute("._AUTO2DEXPORTLAYOUTTOACAD2D\n", true, false, false);
      PlantOrthoView.FileDiag("PFSORTHOUPDATESCALE 종료: PFSORTHOCONTINUE 큐잉");
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
             try {
                (transaction.GetObject(Commands.ViewportId, (OpenMode) 1) as Viewport).Locked = true;
             } catch {}
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
