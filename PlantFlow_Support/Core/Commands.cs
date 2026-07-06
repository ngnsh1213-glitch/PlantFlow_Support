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

    [CommandMethod("AUTO2DUPDATESTANDARDSCALE")]
    public void UpdateStandardScale()
    {
      Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
      Editor editor = mdiActiveDocument.Editor;
      Database database = mdiActiveDocument.Database;

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
      mdiActiveDocument.SendStringToExecute("._AUTO2DCONTINUETOEXECUTE\n", true, false, false);
    }

    [CommandMethod("AUTO2DCONTINUETOEXECUTE", CommandFlags.Session)]
    public void SaveAndExitCurrentDrawingAndContinueToExecute()
    {
      Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
      Database database = mdiActiveDocument.Database;
      string filename = database.Filename;
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
      DocumentExtension.CloseAndSave(mdiActiveDocument, filename);
      foreach (Document document in Application.DocumentManager)
      {
        if (string.Compare(document.Name, Commands.DocumentName, true) == 0)
          Application.DocumentManager.MdiActiveDocument = document;
      }
      if (!Commands.IsMoreThanOne)
        return;
      int itemNo = Commands.ItemNo;
      if (itemNo >= Commands.PSUtil.lvSupportName.Items.Count - 1)
        return;
      Commands.PSUtil.run(itemNo);
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
