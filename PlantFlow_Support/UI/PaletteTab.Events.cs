using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.ProcessPower.DataLinks;
using Autodesk.ProcessPower.ProjectManager;
using Autodesk.ProcessPower.PnP3dObjects;
using Autodesk.ProcessPower.DataObjects;
using Autodesk.ProcessPower.PnP3dPipeSupport;
using PlantFlow_Support.Properties;

namespace PlantFlow_Support
{
  public partial class PaletteTab
  {
    private void dgvPSDM_SelectionChanged(object sender, EventArgs e)
    {
        try 
        {
            var PSUtil = new PSUtil();
            if (PSUtil.ed == null) return;

            List<ObjectId> selectedIds = new List<ObjectId>();
            foreach (DataGridViewRow row in this.dgvPSDM.SelectedRows)
            {
                if (row.Cells["PnPID"].Value != null)
                {
                    // Handle potential type mismatch (long vs int)
                    long pidLong = Convert.ToInt64(row.Cells["PnPID"].Value);
                    selectedIds.Add(PSUtil.ConvertRowIdToObjectId((int)pidLong));
                }
            }

            if (selectedIds.Count > 0)
            {
                PSUtil.ed.SetImpliedSelection(selectedIds.ToArray());
            }
            else
            {
                PSUtil.ed.SetImpliedSelection(new ObjectId[0]);
            }
        }
        catch (System.Exception) 
        {
            // Suppress errors during selection sync to avoid popping up messages on rapid clicking
        }
    }

    private void btAdd_Click(object sender, EventArgs e)
    {
      string code;
      string msg;
      object added;
      bool cancelled;
      AddSupportCore(this.cbbViewDirection.Text, out code, out msg, out added, out cancelled);
      if (cancelled)
        return;
      if (code != null)
      {
        int num = (int)MessageBox.Show("Code (" + code + "): " + msg, "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
    }

    private void AddSupportCore(string viewDir, out string code, out string msg, out object added, out bool cancelled)
    {
      code = null;
      msg = null;
      added = null;
      cancelled = false;
      var PSUtil = new PSUtil();
      if (string.IsNullOrEmpty(this.tbTemplate.Text))
      {
        code = "ER1010";
        msg = "Invalid input. Please check your template directory!";
        return;
      }
      else if (!File.Exists(this.tbTemplate.Text))
      {
        code = "ER1016";
        msg = "No template found. Please check your template directory!";
        return;
      }
      else
      {
        PSUtil.OrthoTemplate = this.tbTemplate.Text;
        Document document = PSUtil.GetDocument();
        if (((DisposableWrapper) document) == ((DisposableWrapper) null))
        {
          code = "ER1000";
          msg = "No drawings are open, open project drawing and try again!";
          return;
        }
        else
        {
          Database database = document.Database;
          PromptSelectionResult selection = PSUtil.ed.GetSelection();
          if (selection.Status != PromptStatus.OK)
          {
            cancelled = true;
            return;
          }
          else
          {
            SelectionSet selectionSet = selection.Value;
            ObjectId id = ObjectId.Null;
            List<AttachmentInfo> attachmentInfoList = new List<AttachmentInfo>();
            List<ObjectId> objectIdList = new List<ObjectId>();
            using (document.LockDocument())
            {
              using (Transaction transaction = database.TransactionManager.StartTransaction())
              {
                foreach (ObjectId objectId in selectionSet.GetObjectIds())
                {
                  DBObject dbObject = transaction.GetObject(objectId, (OpenMode) 1);
                  switch (dbObject)
                  {
                    case Pipe _:
                      objectIdList.Add(objectId);
                      break;
                    case Support _:
                      StringCollection stringCollection = new StringCollection()
                      {
                        "SupportType",
                        "ShortDescription",
                        "Size",
                        "BOP",
                        "Position Z",
                        "LineNumberTag"
                      };
                      StringCollection properties = PSUtil.dl_manager.GetProperties(objectId, stringCollection, true);
                      switch (properties[0])
                      {
                        case "ATTACHMENT":
                          PortCollection ports = ((Part) dbObject).GetPorts((PortType) 7);
                          attachmentInfoList.Add(new AttachmentInfo()
                          {
                            Id = objectId,
                            Description = "TYPE " + properties[1],
                            Size = properties[2] + "A",
                            BOP = Math.Round(Convert.ToDouble(properties[3])).ToString(),
                            COP = Math.Round(Convert.ToDouble(properties[4])).ToString(),
                            LineNumber = properties[5],
                            PPoints = ports
                          });
                          continue;
                        case "FRAMEWORK":
                          id = objectId;
                          continue;
                        default:
                          continue;
                      }
                  }
                }
                transaction.Commit();
              }
            }
            SupportInfo info;
            string[] new_sp;
            if (!this.EntityIsSupport(database, id, viewDir, out info, out new_sp))
            {
              if (string.IsNullOrEmpty(new_sp[0]))
              {
                code = "ER1007";
                msg = "This support is unnamed. Please check the SupportName property.";
                return;
              }
              else
              {
                code = "ER1002";
                msg = "Selected object are not Plant Support type. Please reselect!";
                return;
              }
            }
            else if (this.SupportSelection.ContainsKey(info.Name))
            {
              code = "ER1003";
              msg = "Already existed in current selection list. Please reselect!";
              return;
            }
            else
            {
              int num8 = 1;
              foreach (AttachmentInfo attachmentInfo in attachmentInfoList)
              {
                string str = info.Name + ":R" + num8.ToString();
                attachmentInfo.Name = "R" + num8.ToString();
                PSUtil.dl_manager.SetProperties(attachmentInfo.Id, new StringCollection()
                {
                  "SupportName"
                }, new StringCollection() { str });
                ++num8;
              }
              info.AttachmentList = attachmentInfoList;
              info.PipeList = objectIdList;
              this.SupportSelection.Add(info.Name, info);
              ListViewItem listViewItem1 = new ListViewItem(new_sp);
              foreach (ListViewItem listViewItem2 in this.lvSupportName.Items)
              {
                if (listViewItem2.SubItems[0].Text == new_sp[0])
                {
                  code = "ER1003";
                  msg = "This support already add to the current selection list.";
                  return;
                }
              }
              this.lvSupportName.Items.Add(listViewItem1);
              added = new { name = info.Name, view = viewDir };
              PSUtil.ed.Regen();
            }
          }
        }
      }
    }

    private void Export2DCore(out string code)
    {
      code = null;
      if (this.lvSupportName.Items.Count == 0)
      {
        code = "ER1004";
        return;
      }
      if (!File.Exists("C:\\TEMP\\CADLIB\\GridSystem.json"))
      {
        code = "ER1005";
        return;
      }

      Dictionary<string, SupportInfo> finalSelection = this.SupportSelection;
      if (this.lvSupportName.SelectedItems.Count > 0)
      {
        finalSelection = new Dictionary<string, SupportInfo>();
        foreach (ListViewItem item in this.lvSupportName.SelectedItems)
        {
          string name = item.SubItems[0].Text;
          if (this.SupportSelection.ContainsKey(name))
          {
            finalSelection[name] = this.SupportSelection[name];
          }
        }

        if (finalSelection.Count == 0)
        {
          code = "ER1006";
          return;
        }
      }

      this.LimitViewCore(out code);
      if (code != null) return;

      var PSUtil = new PSUtil();
      Document document = PSUtil.GetDocument();
      if (document == null)
      {
        code = "ER1000";
        return;
      }

      Database database = document.Database;
      using (document.LockDocument())
      {
        using (database.TransactionManager.StartTransaction())
        {
          database.ResolveXrefs(true, false);
        }
      }

      if (finalSelection == null)
      {
        code = "ER1004";
        return;
      }

      Commands.PSUtil = PSUtil;
      PSUtil.lvSupportName = this.lvSupportName;
      PSUtil.SupportSelection = finalSelection;
      Commands.DocumentName = document.Name;
      Commands.Export2DSession();
    }

    private void btExport_Click(object sender, EventArgs e)
    {
      try
      {
          string code;
          Export2DCore(out code);
          if (code == "ER1004")
            MessageBox.Show("Code (ER1004): No supports are selected. Please select your support to export", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
          else if (code == "ER1005")
            MessageBox.Show("Code (ER1005): Please set Grid System before export to 2D!", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
          else if (code == "ER1006")
            MessageBox.Show("Selected items not found in cache. Please clear and re-add.", "PlantFlow_Support");
          else if (code == "ER1000")
            MessageBox.Show("Code (ER1000): No drawings are open, open project drawing and try again!", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
      catch (Exception ex)
      {
           MessageBox.Show($"Export 2D Fatal Error: {ex.Message}\nStack: {ex.StackTrace}", "Debug - Export 2D");
      }
    }

    private void tmsRemove_Click(object sender, EventArgs e)
    {
      if (this.lvSupportName.SelectedItems.Count <= 0)
        return;
      this.SupportSelection.Remove(this.lvSupportName.SelectedItems[0].Text);
      this.lvSupportName.Items.Remove(this.lvSupportName.SelectedItems[0]);
    }

    private void tmsClearItem_Click(object sender, EventArgs e)
    {
      if (this.lvSupportName.Items.Count <= 0)
        return;
      this.SupportSelection.Clear();
      this.lvSupportName.Items.Clear();
    }

    private void btExportMTO_Click(object sender, EventArgs e)
    {
      try
      {
        if (this.SupportSelection.Count == 0 || this.lvSupportName.Items.Count == 0)
        {
          int num1 = (int) MessageBox.Show("Code (ER1004): No supports are selected. Please select your support to export", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }
        else if (string.IsNullOrEmpty(this.tbSaveMTOAs.Text))
        {
          int num2 = (int) MessageBox.Show("Code (ER1010): Invalid input. Please check your template directory!", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }
        else
        {
          var PSUtil = new PSUtil();
          PSUtil.OrthoTemplate = this.tbTemplate.Text;
          PSUtil.GenerateSupportData(this.SupportSelection, this.tbSaveMTOAs.Text);
          int num3 = (int) MessageBox.Show("Export successfull!", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }
      }
      catch
      {
      }
    }



    private class SupportRowHelper
    {
        public long PnPID;
        public string Tag;
        public string Identifier;
        public string SupportName;
        public string SupportDetail;
    }

    private void btPreProcess_Click(object sender, EventArgs e)
    {
        // Restore: Current Dwg Process (Set Identifier for entire drawing)
        this.SetIdentifierForDrawing();
    }



    private void btGetXGrid_Click(object sender, EventArgs e)
    {
      Document mdiActiveDocument = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
      Editor editor = mdiActiveDocument.Editor;
      Database database = mdiActiveDocument.Database;
      PromptSelectionResult selection = editor.GetSelection();
      if (selection.Status != PromptStatus.OK)
      {
        int num1 = (int) MessageBox.Show("Code (ER1001): Nothing was selected! Please try again.", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
      else
      {
        SelectionSet selectionSet = selection.Value;
        var PSUtil = new PSUtil();
        List<Line> list1 = new List<Line>();
        using (mdiActiveDocument.LockDocument())
        {
          using (Transaction transaction = database.TransactionManager.StartTransaction())
          {
            foreach (ObjectId objectId in selectionSet.GetObjectIds())
            {
              DBObject dbObject = transaction.GetObject(objectId, (OpenMode) 1);
              if (dbObject is Line)
              {
                Line line = dbObject as Line;
                Point3d point3d = ((Curve) line).StartPoint;
                double x1 = point3d.X;
                point3d = ((Curve) line).EndPoint;
                double x2 = point3d.X;
                if (x1 != x2)
                {
                  int num2 = (int) MessageBox.Show("Code (ER1014): Some grid lines are not have the same Y coordinate. Please check!", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                  ((Entity) line).Highlight();
                  editor.UpdateScreen();
                  return;
                }
                list1.Add(line);
              }
            }
            transaction.Commit();
          }
        }
        this.XLines = PSUtil.GridLineSorting(list1, true);
        PSUtil.Print("Successfull!");
      }
    }

    private void btGetYGrid_Click(object sender, EventArgs e)
    {
      Document mdiActiveDocument = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
      Editor editor = mdiActiveDocument.Editor;
      Database database = mdiActiveDocument.Database;
      PromptSelectionResult selection = editor.GetSelection();
      if (selection.Status != PromptStatus.OK)
      {
        int num1 = (int) MessageBox.Show("Code (ER1001): Nothing was selected! Please try again.", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
      else
      {
        SelectionSet selectionSet = selection.Value;
        var PSUtil = new PSUtil();
        List<Line> list1 = new List<Line>();
        using (mdiActiveDocument.LockDocument())
        {
          using (Transaction transaction = database.TransactionManager.StartTransaction())
          {
            foreach (ObjectId objectId in selectionSet.GetObjectIds())
            {
              DBObject dbObject = transaction.GetObject(objectId, (OpenMode) 1);
              if (dbObject is Line)
              {
                Line line = dbObject as Line;
                Point3d point3d = ((Curve) line).StartPoint;
                double y1 = point3d.Y;
                point3d = ((Curve) line).EndPoint;
                double y2 = point3d.Y;
                if (y1 != y2)
                {
                  int num2 = (int) MessageBox.Show("Code (ER1014): Some grid lines are not have the same Y coordinate. Please check.", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                  ((Entity) line).Highlight();
                  editor.UpdateScreen();
                  return;
                }
                list1.Add(line);
              }
            }
            transaction.Commit();
          }
        }
        this.YLines = PSUtil.GridLineSorting(list1, false);
        PSUtil.Print("Successfull!");
      }
    }

    private void btSetGrid_Click(object sender, EventArgs e)
    {
      string[] strArray1 = this.tbXlabel.Text.Split(',');
      string[] strArray2 = this.tbYlabel.Text.Split(',');
      if (strArray1.Length != this.XLines.Count || strArray2.Length != this.YLines.Count)
      {
        int num1 = (int) MessageBox.Show("Code (ER1015): Number of label is not equal with number of of grid line. Please check your input label.", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
      else
      {
        var PSUtil = new PSUtil();
        Dictionary<string, Point2d> Grid = new Dictionary<string, Point2d>();
        for (int index1 = 0; index1 < this.XLines.Count; ++index1)
        {
          Line xline = this.XLines[index1];
          for (int index2 = 0; index2 < this.YLines.Count; ++index2)
          {
            Line yline = this.YLines[index2];
            Point3dCollection point3dCollection = new Point3dCollection();
            ((Entity) xline).IntersectWith((Entity) yline, (Intersect) 1, point3dCollection, IntPtr.Zero, IntPtr.Zero);
            if (point3dCollection.Count > 0)
            {
              string key = strArray1[index1] + "-" + strArray2[index2];
              Point3d point3d = point3dCollection[0];
              Point2d point2d = new Point2d(point3d.X, point3d.Y);
              Grid.Add(key, point2d);
            }
          }
        }
        PSUtil.WriteJsonFile(Grid);
        int num2 = (int) MessageBox.Show("Successfull set grid system for this project.", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
      }
    }

    private void btSetIdentifierCurrentSelection_Click(object sender, EventArgs e)
    {
        // Restore: Use AutoCAD Selection
        var PSUtil = new PSUtil();
        Document document = PSUtil.GetDocument();
        if (document == null) return;
        
        PromptSelectionResult selection = PSUtil.ed.GetSelection();
        if (selection.Status != PromptStatus.OK)
        {
            MessageBox.Show("Please select supports in the drawing first.", "PlantFlow_Support");
            return;
        }

        List<ObjectId> selectedIds = new List<ObjectId>(selection.Value.GetObjectIds());
        this.SetIdentifierForSelection(selectedIds);
        
        // Refresh Grid to show changes
        this.LoadGridData();
    }

    private void btAutoCodingForSelectedOnly_Click(object sender, EventArgs e)
    {
      var PSUtil = new PSUtil();
      Document document = PSUtil.GetDocument();
      if (((DisposableWrapper) document) == ((DisposableWrapper) null))
      {
        int num = (int) MessageBox.Show("Code (ER1000): No drawings are open, open project drawing and try again!", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
      else
      {
         List<ObjectId> targetIds = new List<ObjectId>();
         System.Collections.IEnumerable rowsToProcess;
         if (this.dgvPSDM.SelectedRows.Count > 0)
             rowsToProcess = this.dgvPSDM.SelectedRows;
         else
             rowsToProcess = (System.Collections.IEnumerable)this.dgvPSDM.Rows;

         foreach (DataGridViewRow row in rowsToProcess)
         {
             if (row.Cells["PnPID"].Value != null)
             {
                  long pidLong = Convert.ToInt64(row.Cells["PnPID"].Value);
                  targetIds.Add(PSUtil.ConvertRowIdToObjectId((int)pidLong));
             }
         }

         if (targetIds.Count == 0) return;
         AutoCodingForSelection(targetIds, document);
      }
    }

    private void btSetIdentifierCurrentList_Click(object sender, EventArgs e)
    {
        // Gather IDs from Current List and call SetIdentifierForSelection
        var PSUtil = new PSUtil();
        List<ObjectId> allIds = new List<ObjectId>();
        
        foreach (DataGridViewRow row in this.dgvPSDM.Rows)
        {
            if (row.IsNewRow) continue;
            if (row.Cells["PnPID"].Value != null)
            {
               long pid = Convert.ToInt64(row.Cells["PnPID"].Value);
               try 
               {
                   ObjectId id = PSUtil.ConvertRowIdToObjectId((int)pid);
                   if (id != ObjectId.Null) allIds.Add(id);
               }
               catch {}
            }
        }
        
        if (allIds.Count > 0)
        {
            this.SetIdentifierForSelection(allIds);
            this.LoadGridData(); // Refresh UI
        }
        else
        {
            MessageBox.Show("No supports found in the list.", "PlantFlow_Support");
        }
    }

    private void btAutoCodingCurrentList_Click(object sender, EventArgs e)
    {
        Document mdiActiveDocument = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        if (mdiActiveDocument == null) return;
        Editor ed = mdiActiveDocument.Editor;

        // Init PSUtil safely
        PSUtil localPSUtil = new PSUtil();

        using (mdiActiveDocument.LockDocument())
        {
             // Execute Support Coding for ALL items in Current List
            List<ObjectId> allIds = new List<ObjectId>();
            
            try 
            {
                ed.WriteMessage("\n[Debug] Starting AutoCoding Loop...");
                foreach (DataGridViewRow row in this.dgvPSDM.Rows)
                {
                    if (row.IsNewRow) continue;
                    if (row.Cells["PnPID"].Value != null)
                    {
                       long pid = Convert.ToInt64(row.Cells["PnPID"].Value);
                       try 
                       {
                           ObjectId id = localPSUtil.ConvertRowIdToObjectId((int)pid);
                           if (id != ObjectId.Null) allIds.Add(id);
                       }
                       catch {}
                    }
                }
                ed.WriteMessage($"\n[Debug] Found {allIds.Count} IDs.");
            }
            catch (System.Exception exLoop)
            {
                ed.WriteMessage($"\n[Error] AutoCoding Loop Failed: {exLoop.Message}");
                MessageBox.Show("Error converting IDs: " + exLoop.Message);
                return;
            }
            
            if (allIds.Count > 0)
            {
                ed.WriteMessage("\n[Debug] Calling AutoCodingForSelection...");
                this.AutoCodingForSelection(allIds, mdiActiveDocument);
            }
            else
            {
                 MessageBox.Show("No supports in list found in current drawing.", "PlantFlow_Support");
            }
        }
    }
    
    // Modified: AutoName button now processes Current List instead of Selection
    private void btProcessing_Click_New(object sender, EventArgs e) 
    {
         Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
         if (mdiActiveDocument == null) return;
         Editor ed = mdiActiveDocument.Editor;

         ed.WriteMessage("\n[Debug] btProcessing_Click_New (AutoName) Started.");

         using (mdiActiveDocument.LockDocument())
         {
             HashSet<long> targetPnPIDs = new HashSet<long>();
             
             try
             {
                 foreach (DataGridViewRow row in this.dgvPSDM.Rows)
                 {
                     if (row.IsNewRow) continue;
                     if (row.Cells["PnPID"].Value != null)
                     {
                        try 
                        {
                            long pid = Convert.ToInt64(row.Cells["PnPID"].Value);
                            targetPnPIDs.Add(pid);
                        }
                        catch {}
                     }
                 }
                 ed.WriteMessage($"\n[Debug] Found {targetPnPIDs.Count} PnPIDs to process.");
             }
             catch(Exception exLoop)
             {
                 ed.WriteMessage($"\n[Error] AutoName Loop Failed: {exLoop.Message}");
                 MessageBox.Show("Error gathering IDs: " + exLoop.Message);
                 return;
             }
             
             if (targetPnPIDs.Count > 0)
             {
                 ed.WriteMessage("\n[Debug] Calling AutoNameForSelection logic...");
                 this.AutoNameForSelection_Sorted(targetPnPIDs);
                 
                 // UI Refresh is handled inside AutoNameForSelection (updating Grid Cells)
             }
             else
             {
                  MessageBox.Show("No items in list to process.", "PlantFlow_Support");
             }
         }
    }


    private void btAutoCodingForDwgOnly_Click(object sender, EventArgs e)
    {
         Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
         if (mdiActiveDocument == null) return;
         Editor ed = mdiActiveDocument.Editor;

         // Init PSUtil safely
         PSUtil localPSUtil = new PSUtil();

         using (mdiActiveDocument.LockDocument())
         {
              // Execute Support Coding for ALL items in Current List (Logic is same as Current List based on UI request)
              
             List<ObjectId> allIds = new List<ObjectId>();
             
             try 
             {
                 ed.WriteMessage("\n[Debug] Starting AutoCoding (Dwg) Loop...");
                 foreach (DataGridViewRow row in this.dgvPSDM.Rows)
                 {
                     if (row.IsNewRow) continue;
                     if (row.Cells["PnPID"].Value != null)
                     {
                        long pid = Convert.ToInt64(row.Cells["PnPID"].Value);
                        try 
                        {
                            ObjectId id = localPSUtil.ConvertRowIdToObjectId((int)pid);
                            if (id != ObjectId.Null) allIds.Add(id);
                        }
                        catch {}
                     }
                 }
                 ed.WriteMessage($"\n[Debug] Found {allIds.Count} IDs.");
             }
             catch (Exception exLoop)
             {
                 ed.WriteMessage($"\n[Error] AutoCoding Loop Failed: {exLoop.Message}");
                 return;
             }
             
             if (allIds.Count > 0)
             {
                 ed.WriteMessage("\n[Debug] Calling AutoCodingForSelection...");
                 this.AutoCodingForSelection(allIds, mdiActiveDocument);
             }
             else
             {
                  MessageBox.Show("No supports in list found in current drawing.", "PlantFlow_Support");
             }
         }
    }

    private void tbTemplate_TextChanged(object sender, EventArgs e)
    {
      Settings.Default["tbTemplate"] = (object) this.tbTemplate.Text;
      Settings.Default.Save();
    }

    private void tbSaveMTOAs_TextChanged(object sender, EventArgs e)
    {
      Settings.Default["tbSaveMTOAs"] = (object) this.tbSaveMTOAs.Text;
      Settings.Default.Save();
    }

    private void tbXlabel_TextChanged(object sender, EventArgs e)
    {
      Settings.Default["tbXlabel"] = (object) this.tbXlabel.Text;
      Settings.Default.Save();
    }

    private void tbYlabel_TextChanged(object sender, EventArgs e)
    {
      Settings.Default["tbYlabel"] = (object) this.tbYlabel.Text;
      Settings.Default.Save();
    }

    private void tbProjectNo_TextChanged(object sender, EventArgs e)
    {
      Settings.Default["tbProjectNo"] = (object) this.tbProjectNo.Text;
      Settings.Default.Save();
    }

    private void tbDwgRevision_TextChanged(object sender, EventArgs e)
    {
      Settings.Default["tbDwgRevision"] = (object) this.tbDwgRevision.Text;
      Settings.Default.Save();
    }






    
    // Add DropDown and SelectedIndexChanged for completeness as they were in original
    private void cbSupportType_DropDown(object sender, EventArgs e) 
    {
        // No specific implementation found in snippet, assuming handled by Designer or empty
    }

    private void cbSupportType_SelectedIndexChanged(object sender, EventArgs e)
    {
         string filter = this.cbSupportType.Text;
         if (string.IsNullOrEmpty(filter))
         {
             this.SupportData.DefaultView.RowFilter = "";
         }
         else
         {
             this.SupportData.DefaultView.RowFilter = string.Format("Identifier = '{0}'", filter);
         }
    }

    private void btRefresh_Click(object sender, EventArgs e)
    {
         this.LoadGridData();
    }
    private void btProjectScan_Click(object sender, EventArgs e)
    {
        try
        {
            var PSUtil = new PSUtil();
            PnPDatabase pnpDatabase = PSUtil.dl_manager.GetPnPDatabase();
            if (pnpDatabase == null) 
            {
                 MessageBox.Show("Cannot access Project Database.");
                 return;
            }

            PnPTable table = null;
            try { table = pnpDatabase.Tables["Support"]; } catch { }
            
            if (table == null)
            {
                MessageBox.Show("Support table not found.");
                return;
            }
            
            PnPTable engItemsTable = null;
            try { engItemsTable = pnpDatabase.Tables["EngineeringItems"]; } catch { }

            PnPTable dwgTable = null;
            // Try singular and plural table names
            try { dwgTable = pnpDatabase.Tables["PnPDrawing"]; } catch { }
            if (dwgTable == null) try { dwgTable = pnpDatabase.Tables["PnPDrawings"]; } catch { }

            PnPTable linksTable = null;
            try { linksTable = pnpDatabase.Tables["PnPDataLinks"]; } catch { }
            
            Dictionary<int, string> dwgCache = new Dictionary<int, string>();
            
            if(dwgTable != null)
            {
                bool hasDwgNameSpaced = dwgTable.Columns.Contains("Dwg Name");
                bool hasDwgName = dwgTable.Columns.Contains("DwgName");
                bool hasPnPDwgFile = dwgTable.Columns.Contains("PnPDwgFileName");

                foreach(var dRow in dwgTable.Select())
                {
                    string dName = "";
                    if(hasDwgNameSpaced) dName = dRow["Dwg Name"]?.ToString();
                    else if(hasDwgName) dName = dRow["DwgName"]?.ToString();
                    else if(hasPnPDwgFile) dName = dRow["PnPDwgFileName"]?.ToString();
                    
                    if(!string.IsNullOrEmpty(dName))
                    {
                        if(dName.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase))
                           dName = Path.GetFileNameWithoutExtension(dName);
                        
                        dwgCache[dRow.RowId] = dName;
                    }
                }
            }

            PnPRow[] rows = table.Select();
            
            var results = new Dictionary<string, DrawingScanResult>();
            int maxUB = 0;
            int maxUBC = 0;
            
            var regex = new System.Text.RegularExpressions.Regex(@"\d+");
            
            bool supportHasDrawingId = table.Columns.Contains("DrawingId");
            bool engHasDrawingId = (engItemsTable != null && engItemsTable.Columns.Contains("DrawingId"));
            // Re-adding the missing declaration
            bool linksHasDwgId = (linksTable != null && linksTable.Columns.Contains("DwgId"));

            foreach(var row in rows)
            {
                 string type = "";
                 try { type = row["SupportIdentifier"]?.ToString(); } catch {}
                 
                 if(type != "UB" && type != "UBC") continue;

                 string dwgName = "Unknown"; 
                 int dwgId = -1;
                 
                 // 1. Try direct DrawingId column
                 if(supportHasDrawingId && row["DrawingId"] != DBNull.Value)
                 {
                     dwgId = Convert.ToInt32(row["DrawingId"]);
                 }
                 // 2. Try EngineeringItems (Parent Table)
                 else if (engItemsTable != null && engHasDrawingId)
                 {
                     var engRow = engItemsTable.Select($"PnPID={row.RowId}").FirstOrDefault();
                     if(engRow != null && engRow["DrawingId"] != DBNull.Value)
                     {
                         dwgId = Convert.ToInt32(engRow["DrawingId"]);
                     }
                 }
                 
                 // 3. Try PnPDataLinks mapping (PnPID -> DwgId)
                 if (dwgId == -1 && linksTable != null && linksHasDwgId)
                 {
                     // Select where PnPID matches
                     var lRows = linksTable.Select($"PnPID={row.RowId}");
                     if(lRows.Length > 0 && lRows[0]["DwgId"] != DBNull.Value)
                     {
                         dwgId = Convert.ToInt32(lRows[0]["DwgId"]);
                     }
                 }

                 if (dwgId > 0)
                 {
                     if(dwgCache.ContainsKey(dwgId)) 
                         dwgName = dwgCache[dwgId];
                     else
                         dwgName = $"ID-{dwgId}"; 
                 }

                 if(!results.ContainsKey(dwgName))
                 {
                     results[dwgName] = new DrawingScanResult() { DrawingName = dwgName, DwgId = dwgId };
                 }

                 if(type == "UB") results[dwgName].CountUB++;
                 if(type == "UBC") results[dwgName].CountUBC++;

                 string tag = "";
                 try { tag = row["Tag"]?.ToString(); } catch {}
                 
                 if(!string.IsNullOrEmpty(tag))
                 {
                      var match = regex.Match(tag);
                      if(match.Success)
                      {
                           int num = int.Parse(match.Value);
                           if(type == "UB" && num > maxUB) maxUB = num;
                           if(type == "UBC" && num > maxUBC) maxUBC = num;
                      }
                 }
            }
            
            var sortedResults = results.Values.OrderBy(r => r.DrawingName).ToList();

            using(var form = new FormProjectScanResult(sortedResults, maxUB, maxUBC))
            {
                // Subscribe to Export Event
                form.OnExport2DRequested += (s, drawings) => 
                {
                     // Close the form to proceed? Or keep open?
                     // Usually better to close or minimize.
                     // Logic to process export:
                     // We need to pass the list of Drawings to a processor.
                     // But we are inside a static/instance method.
                     // We can define a local function or method.
                     ProcessDirectExport(drawings, pnpDatabase, ref PSUtil);
                };

                form.ShowDialog();
                if(form.Saved)
                {
                    try 
                    {
                        string projectPath = Path.GetDirectoryName(PSUtil.GetDocument()?.Name);
                        string configPath = Path.Combine(projectPath, "SupportTagConfig.txt");
                        string content = $"UB={form.MaxUB};UBC={form.MaxUBC}";
                        File.WriteAllText(configPath, content);
                        MessageBox.Show($"Configuration saved to {configPath}");
                    }
                    catch(Exception exSave)
                    {
                        MessageBox.Show($"Failed to save config: {exSave.Message}");
                    }
                }
            }
        }
        catch(Exception ex)
        {
            MessageBox.Show($"Scan Error: {ex.Message}");
        }
    }

    private void ProcessDirectExport(List<string> drawings, PnPDatabase db, ref PSUtil psUtil)
    {
        // Placeholder for Logic
        // 1. Iterate drawings
        // 2. Resolve Path
        // 3. Open Side Database (or active Document)
        // 4. Find UB/UBC Items
        // 5. Generate 2D
        MessageBox.Show($"Requested Export for {drawings.Count} drawings.\nFeature under construction: \n" + string.Join("\n", drawings.Take(5)) + (drawings.Count>5?"...":""));
        
        // Detailed Logic Implementation Needed Here.
        // Since user asked "Is it possible?", demonstrating the hook is the first step.
        // Full implementation involves significant code (opening docs, transaction, etc).
        // I will implement the loop structure next.
    }
  }
}
