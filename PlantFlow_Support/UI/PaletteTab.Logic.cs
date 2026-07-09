using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    // Logic Fields
    private Dictionary<string, SupportInfo> SupportSelection;
    private Dictionary<string, PlantFlow_Support.Commands.ViewType> ViewTypes;
    private Dictionary<string, Matrix3d> UCSs = new Dictionary<string, Matrix3d>();
    private Dictionary<string, Vector3d> UpVectors = new Dictionary<string, Vector3d>();
    private Dictionary<string, Vector3d> ViewDirectionVectors = new Dictionary<string, Vector3d>();
    private List<Line> XLines = new List<Line>();
    private List<Line> YLines = new List<Line>();
    private System.Data.DataTable SupportData = new System.Data.DataTable();
    
    // Idle Strategy Fields
    private Dictionary<long, string> PendingUpdates;
    private Dictionary<long, string> PendingTags;

    private void InitializeViewDefinitions()
    {
        this.ViewTypes = new Dictionary<string, PlantFlow_Support.Commands.ViewType>();
        this.UCSs = new Dictionary<string, Matrix3d>();
        this.UpVectors = new Dictionary<string, Vector3d>();
        this.ViewDirectionVectors = new Dictionary<string, Vector3d>();

        // Top
        this.ViewTypes["Top"] = PlantFlow_Support.Commands.ViewType.Top;
        this.UCSs["Top"] = Matrix3d.Identity;
        this.UpVectors["Top"] = Vector3d.YAxis.Negate();
        this.ViewDirectionVectors["Top"] = Vector3d.ZAxis;

        // Bottom
        this.ViewTypes["Bottom"] = PlantFlow_Support.Commands.ViewType.Bottom;
        this.UCSs["Bottom"] = Matrix3d.Rotation(Math.PI, Vector3d.XAxis, Point3d.Origin);
        this.UpVectors["Bottom"] = Vector3d.YAxis.Negate();
        this.ViewDirectionVectors["Bottom"] = Vector3d.ZAxis.Negate();

        // Front
        this.ViewTypes["Front"] = PlantFlow_Support.Commands.ViewType.Front;
        this.UCSs["Front"] = Matrix3d.Rotation(Math.PI / 2.0, Vector3d.XAxis, Point3d.Origin);
        this.UpVectors["Front"] = Vector3d.ZAxis;
        this.ViewDirectionVectors["Front"] = Vector3d.YAxis.Negate();

        // Back
        this.ViewTypes["Back"] = PlantFlow_Support.Commands.ViewType.Back;
        this.UCSs["Back"] = Matrix3d.Rotation(Math.PI / 2.0, Vector3d.XAxis, Point3d.Origin) * Matrix3d.Rotation(Math.PI, Vector3d.ZAxis, Point3d.Origin);
        this.UpVectors["Back"] = Vector3d.ZAxis;
        this.ViewDirectionVectors["Back"] = Vector3d.YAxis;

        // Left
        this.ViewTypes["Left"] = PlantFlow_Support.Commands.ViewType.Left;
        this.UCSs["Left"] = Matrix3d.Rotation(Math.PI / 2.0, Vector3d.ZAxis, Point3d.Origin) * Matrix3d.Rotation(Math.PI / 2.0, Vector3d.XAxis, Point3d.Origin);
        this.UpVectors["Left"] = Vector3d.ZAxis;
        this.ViewDirectionVectors["Left"] = Vector3d.XAxis.Negate();

        // Right
        this.ViewTypes["Right"] = PlantFlow_Support.Commands.ViewType.Right;
        this.UCSs["Right"] = Matrix3d.Rotation(-Math.PI / 2.0, Vector3d.ZAxis, Point3d.Origin) * Matrix3d.Rotation(Math.PI / 2.0, Vector3d.XAxis, Point3d.Origin);
        this.UpVectors["Right"] = Vector3d.ZAxis;
        this.ViewDirectionVectors["Right"] = Vector3d.XAxis;
    }

    private List<ViewSpec> CreateViewSpecs(IEnumerable<string> viewNames)
    {
        List<ViewSpec> specs = new List<ViewSpec>();
        if (viewNames == null)
            viewNames = new string[] { "Top" };

        foreach (string rawViewName in viewNames)
        {
            string viewName = string.IsNullOrEmpty(rawViewName) ? "Top" : rawViewName.Trim();
            if (specs.Any(v => string.Equals(v.ViewName, viewName, StringComparison.OrdinalIgnoreCase)))
                continue;
            if (string.Equals(viewName, "Main", StringComparison.OrdinalIgnoreCase))
            {
                ViewSpec mainPlaceholder = new ViewSpec();
                mainPlaceholder.ViewName = "Main";
                mainPlaceholder.ViewType = this.ViewTypes.ContainsKey("Front") ? this.ViewTypes["Front"] : PlantFlow_Support.Commands.ViewType.Front;
                mainPlaceholder.UCS = this.UCSs.ContainsKey("Front") ? this.UCSs["Front"] : Matrix3d.Identity;
                mainPlaceholder.UpVector = this.UpVectors.ContainsKey("Front") ? this.UpVectors["Front"] : Vector3d.ZAxis;
                mainPlaceholder.ViewDirection = this.ViewDirectionVectors.ContainsKey("Front") ? this.ViewDirectionVectors["Front"] : Vector3d.YAxis.Negate();
                mainPlaceholder.PaperCenter = Point3d.Origin;
                specs.Add(mainPlaceholder);
                continue;
            }
            if (!this.ViewTypes.ContainsKey(viewName))
            {
                DiagLog("CreateViewSpecs skip unknown view='" + viewName + "'");
                PlantOrthoView.FileDiag("CreateViewSpecs skip unknown view='" + viewName + "'");
                continue;
            }

            ViewSpec spec = new ViewSpec();
            spec.ViewName = viewName;
            spec.ViewType = this.ViewTypes[viewName];
            spec.UCS = this.UCSs.ContainsKey(viewName) ? this.UCSs[viewName] : Matrix3d.Identity;
            spec.UpVector = this.UpVectors.ContainsKey(viewName) ? this.UpVectors[viewName] : Vector3d.YAxis.Negate();
            spec.ViewDirection = this.ViewDirectionVectors.ContainsKey(viewName) ? this.ViewDirectionVectors[viewName] : Vector3d.ZAxis;
            spec.PaperCenter = Point3d.Origin;
            specs.Add(spec);
        }

        if (specs.Count == 0)
        {
            ViewSpec fallback = new ViewSpec();
            fallback.ViewName = "Top";
            fallback.ViewType = this.ViewTypes.ContainsKey("Top") ? this.ViewTypes["Top"] : PlantFlow_Support.Commands.ViewType.Top;
            fallback.UCS = this.UCSs.ContainsKey("Top") ? this.UCSs["Top"] : Matrix3d.Identity;
            fallback.UpVector = this.UpVectors.ContainsKey("Top") ? this.UpVectors["Top"] : Vector3d.YAxis.Negate();
            fallback.ViewDirection = this.ViewDirectionVectors.ContainsKey("Top") ? this.ViewDirectionVectors["Top"] : Vector3d.ZAxis;
            fallback.PaperCenter = Point3d.Origin;
            specs.Add(fallback);
        }
        return specs;
    }

    private void InitializePSDM()
    {
      this.SupportData.Columns.Add("PnPID", typeof (long));
      this.SupportData.Columns.Add("Tag", typeof (string));
      this.SupportData.Columns.Add("Identifier", typeof (string));
      this.SupportData.Columns.Add("SupportName", typeof (string));
      this.SupportData.Columns.Add("SupportDetail", typeof (string));
      this.SupportData.Columns.Add("Status", typeof (bool)); // Index 5 (Fixed Number)

      this.dgvPSDM.DataSource = (object) this.SupportData;
      
      // 0: PnPID
      this.dgvPSDM.Columns[0].Resizable = DataGridViewTriState.True;
      this.dgvPSDM.Columns[0].SortMode = DataGridViewColumnSortMode.Automatic;
      this.dgvPSDM.Columns[0].Width = 100;
      this.dgvPSDM.Columns[0].ReadOnly = true;

      // 1: Tag
      this.dgvPSDM.Columns[1].Width = 300;
      this.dgvPSDM.Columns[1].Resizable = DataGridViewTriState.True;
      this.dgvPSDM.Columns[1].SortMode = DataGridViewColumnSortMode.Automatic;
      this.dgvPSDM.Columns[1].ReadOnly = true;

      // 2: Identifier
      this.dgvPSDM.Columns[2].Resizable = DataGridViewTriState.True; 
      this.dgvPSDM.Columns[2].SortMode = DataGridViewColumnSortMode.Automatic;
      this.dgvPSDM.Columns[2].Width = 80;

      // 3: SupportName
      this.dgvPSDM.Columns[3].Resizable = DataGridViewTriState.True; 
      this.dgvPSDM.Columns[3].SortMode = DataGridViewColumnSortMode.Automatic;
      this.dgvPSDM.Columns[3].Width = 150;

      // 4: Detail
      this.dgvPSDM.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; 
      this.dgvPSDM.Columns[4].Resizable = DataGridViewTriState.True;
      this.dgvPSDM.Columns[4].SortMode = DataGridViewColumnSortMode.Automatic;

      // 5: Fixed Number (Status)
      this.dgvPSDM.Columns[5].Width = 100; 
      this.dgvPSDM.Columns[5].HeaderText = "Fixed Number";
      this.dgvPSDM.Columns[5].ReadOnly = true; // User cannot manually toggle


      // Selection Logic Added
      this.dgvPSDM.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
      this.dgvPSDM.MultiSelect = true;
      this.dgvPSDM.SelectionChanged += new EventHandler(this.dgvPSDM_SelectionChanged);
      this.dgvPSDM.CellDoubleClick += (sender, e) => { if (e.RowIndex >= 0) this.ZoomToSelectedRows(); };

      // Context Menu
      this.csmPSDM = new ContextMenuStrip();
      
      // 1. Select All
      ToolStripMenuItem itemSelectAll = new ToolStripMenuItem("Select All");
      itemSelectAll.Click += (sender, e) => { this.dgvPSDM.SelectAll(); };
      this.csmPSDM.Items.Add(itemSelectAll);

      // 2. Zoom to Selection
      ToolStripMenuItem itemZoom = new ToolStripMenuItem("Zoom to Selection");
      itemZoom.Click += (sender, e) => { this.ZoomToSelectedRows(); };
      this.csmPSDM.Items.Add(itemZoom);

      // 3. Clear Selection
      ToolStripMenuItem itemClear = new ToolStripMenuItem("Clear Selection");
      itemClear.Click += (sender, e) => { this.dgvPSDM.ClearSelection(); };
      this.csmPSDM.Items.Add(itemClear);

      // 4. Delete Support
      ToolStripMenuItem itemDelete = new ToolStripMenuItem("Delete Support");
      itemDelete.Click += (sender, e) => { this.DeleteSelectedSupports(); };
      this.csmPSDM.Items.Add(itemDelete);

      this.dgvPSDM.ContextMenuStrip = this.csmPSDM;
    }

    private void ZoomToSelectedRows()
    {
        try
        {
            var PSUtil = new PSUtil();
            Document doc = PSUtil.GetDocument();
            if (doc == null) return;
            Editor ed = doc.Editor;

            List<ObjectId> selectedIds = new List<ObjectId>();
            foreach (DataGridViewRow row in this.dgvPSDM.SelectedRows)
            {
                if (row.Cells["PnPID"].Value != null)
                {
                    long pidLong = Convert.ToInt64(row.Cells["PnPID"].Value);
                    try
                    {
                        ObjectId id = PSUtil.ConvertRowIdToObjectId((int)pidLong);
                        if (id != ObjectId.Null && id.IsValid) selectedIds.Add(id);
                    }
                    catch {}
                }
            }

            if (selectedIds.Count > 0)
            {
                // Calculate Extents
                Extents3d ext = this.GeometryLimit(doc.Database, selectedIds, null, Point3d.Origin);
                
                // Zoom
                this.Zoom(doc, ext);
                
                try
                {
                    ed.SetImpliedSelection(selectedIds.ToArray());
                    ed.WriteMessage($"\n[Debug] Zoomed to {selectedIds.Count} items.");
                }
                catch (System.Exception exSel)
                {
                    MessageBox.Show("Zoomed but failed to select: " + exSel.Message);
                }
            }
            else
            {
                 ed.WriteMessage("\n[Debug] No valid ObjectIds found for selection.");
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show("Failed to Zoom: " + ex.Message);
        }
    }

    private void Zoom(Document doc, Extents3d ext)
    {
        using (doc.LockDocument())
        {
            using (ViewTableRecord view = new ViewTableRecord())
            {
                view.CenterPoint = new Point2d((ext.MaxPoint.X + ext.MinPoint.X) / 2.0, (ext.MaxPoint.Y + ext.MinPoint.Y) / 2.0);
                view.Height = (ext.MaxPoint.Y - ext.MinPoint.Y) * 1.2; // 20% margin
                view.Width = (ext.MaxPoint.X - ext.MinPoint.X) * 1.2;
                doc.Editor.SetCurrentView(view);
            }
        }
    }

    private void LoadPreviousSetting()
    {
      this.tbTemplate.Text = (string) Settings.Default["tbTemplate"];
      this.tbSaveMTOAs.Text = (string) Settings.Default["tbSaveMTOAs"];
      this.tbProjectNo.Text = (string) Settings.Default["tbProjectNo"];
      this.tbDwgRevision.Text = (string) Settings.Default["tbDwgRevision"];
      this.tbXlabel.Text = (string) Settings.Default["tbXlabel"];
      this.tbYlabel.Text = (string) Settings.Default["tbYlabel"];

    }

    public void LimitView()
    {
      string code;
      LimitViewCore(out code);
      if (code == "ER1000")
      {
        int num = (int) MessageBox.Show("Code (ER1000): No drawings are open, open project drawing and try again!", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
    }

    // 진단 전용 로거(커맨드라인 + Debug). 로직 무변경.
    private void DiagLog(string m)
    {
        try
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\n[PFS-DIAG] " + m);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[PFS-DIAG] log fail: " + ex.Message); }
    }

    private void LimitViewCore(out string code)
    {
      code = null;
      var PSUtil = new PSUtil();
      Document document = PSUtil.GetDocument();
      if (((DisposableWrapper) document) == ((DisposableWrapper) null))
      {
        code = "ER1000";
        return;
      }
      else
      {
        Database database = document.Database;
        using (document.LockDocument())
        {
          using (Transaction transaction = database.TransactionManager.StartTransaction())
          {
            foreach (KeyValuePair<string, SupportInfo> keyValuePair in this.SupportSelection)
            {
              SupportInfo supportInfo = keyValuePair.Value;
              List<ObjectId> group_ids = new List<ObjectId>();
              if (supportInfo.Ids != null && supportInfo.Ids.Length > 0 && supportInfo.Ids[0] != ObjectId.Null)
              {
                   group_ids.Add(supportInfo.Ids[0]);
              }
              
              List<ObjectId> pipeList = new List<ObjectId>();
              if (supportInfo.PipeList != null)
              {
                  foreach(var pid in supportInfo.PipeList)
                  {
                      if (pid != ObjectId.Null) pipeList.Add(pid);
                  }
              }

              Point3d position = Point3d.Origin;
              if (supportInfo.PPoints != null && supportInfo.PPoints.Count > 0)
              {
                  position = supportInfo.PPoints[0].Position;
              }

              if (supportInfo.AttachmentList != null)
              {
                  foreach (AttachmentInfo attachmentInfo in supportInfo.AttachmentList)
                  {
                      if (attachmentInfo.Id != ObjectId.Null)
                        group_ids.Add(attachmentInfo.Id);
                  }
              }
              
              DiagLog("LimitViewCore support='" + keyValuePair.Key + "' group_ids=" + group_ids.Count + " pipes=" + pipeList.Count);
              if (group_ids.Count > 0)
              {
                  Extents3d extents3d = this.GeometryLimit(database, group_ids, pipeList, position);
                  this.SupportSelection[keyValuePair.Key].Extents = extents3d;
                  DiagLog("LimitViewCore support='" + keyValuePair.Key + "' 최종 Extents min=" + extents3d.MinPoint + " max=" + extents3d.MaxPoint);
              }
              else DiagLog("LimitViewCore support='" + keyValuePair.Key + "' group_ids=0 -> Extents 미설정(퇴화)");
            }
            transaction.Commit();
          }
        }
      }
    }

    private Extents3d GeometryLimit(
      Database database,
      List<ObjectId> group_ids,
      List<ObjectId> pipe_ids,
      Point3d snap_point)
    {
      List<Point3d> source1 = new List<Point3d>();
      List<Point3d> source2 = new List<Point3d>();
      var PSUtil = new PSUtil();
      if (PSUtil.GetDocument() == null) throw new Exception("Failed to get active document in GeometryLimit");

      if (group_ids == null || group_ids.Count == 0) return new Extents3d();

      using (PSUtil.doc.LockDocument())
      {
        using (Transaction transaction = database.TransactionManager.StartTransaction())
        {
          foreach (ObjectId groupId in group_ids)
          {
            if (groupId == ObjectId.Null) continue;
            try {
                DBObject obj = transaction.GetObject(groupId, (OpenMode) 0);
                if (obj is Entity ent)
                {
                    Extents3d geometricExtents = ent.GeometricExtents;
                    source1.Add(geometricExtents.MaxPoint);
                    source2.Add(geometricExtents.MinPoint);
                    DiagLog("GeometryLimit(4) id=" + groupId.ToString() + " type=" + ent.GetType().Name + " min=" + geometricExtents.MinPoint + " max=" + geometricExtents.MaxPoint);
                }
                else DiagLog("GeometryLimit(4) id=" + groupId.ToString() + " NOT Entity(" + (obj?.GetType().Name ?? "null") + ")");
            } catch (Exception ex) { DiagLog("GeometryLimit(4) id=" + groupId.ToString() + " GeometricExtents 예외: " + ex.Message); }
          }
          if (pipe_ids != null)
          {
              foreach (ObjectId pipeId in pipe_ids)
              {
                if (pipeId == ObjectId.Null) continue;
                try {
                    StringCollection stringCollection = new StringCollection()
                    {
                      "Size"
                    };
                    var props = PSUtil.dl_manager.GetProperties(pipeId, stringCollection, true);
                    if (props != null && props.Count > 0)
                    {
                        double num = Convert.ToDouble(props[0]);
                        Point3d point3d1 = new Point3d(snap_point.X + num / 2.0, snap_point.Y + num / 2.0, snap_point.Z + num / 2.0);
                        Point3d point3d2 = new Point3d(snap_point.X - num / 2.0, snap_point.Y - num / 2.0, snap_point.Z - num / 2.0);
                        source1.Add(point3d1);
                        source2.Add(point3d1);
                    }
                } catch (Exception ex) { DiagLog("GeometryLimit(4) pipe 예외: " + ex.Message); }
              }
          }
          transaction.Commit();
        }
      }
      
      if (source1.Count == 0 || source2.Count == 0)
      {
        DiagLog("GeometryLimit(4): 수집점 0 -> 퇴화 Extents 반환. group_ids=" + (group_ids?.Count ?? 0) + " pipe_ids=" + (pipe_ids?.Count ?? 0));
        return new Extents3d();
      }

      double num1 = source1.Max<Point3d>((System.Func<Point3d, double>) (pos => pos.X));
      double num2 = source1.Max<Point3d>((System.Func<Point3d, double>) (pos => pos.Y));
      double num3 = source1.Max<Point3d>((System.Func<Point3d, double>) (pos => pos.Z));
      Point3d point3d3 = new Point3d(num1, num2, num3);
      double num4 = source2.Min<Point3d>((System.Func<Point3d, double>) (pos => pos.X));
      double num5 = source2.Min<Point3d>((System.Func<Point3d, double>) (pos => pos.Y));
      double num6 = source2.Min<Point3d>((System.Func<Point3d, double>) (pos => pos.Z));
      Point3d point3d4 = new Point3d(num4, num5, num6);
      Extents3d extents3d = new Extents3d();
      extents3d.Set(point3d4, point3d3);
      DiagLog("GeometryLimit(4) 반환 Extents min=" + point3d4 + " max=" + point3d3);
      return extents3d;
    }

    private Extents3d GeometryLimit(Database database, List<ObjectId> group_ids)
    {
      List<Point3d> source1 = new List<Point3d>();
      List<Point3d> source2 = new List<Point3d>();
      var PSUtil = new PSUtil();
      if (PSUtil.GetDocument() == null) throw new Exception("Failed to get active document in GeometryLimit");

      if (group_ids == null || group_ids.Count == 0) return new Extents3d();

      using (PSUtil.doc.LockDocument())
      {
        using (Transaction transaction = database.TransactionManager.StartTransaction())
        {
          foreach (ObjectId groupId in group_ids)
          {
            if (groupId == ObjectId.Null) continue;
            try {
                DBObject obj = transaction.GetObject(groupId, (OpenMode) 0);
                if (obj is Entity ent)
                {
                    Extents3d geometricExtents = ent.GeometricExtents;
                    source1.Add(geometricExtents.MaxPoint);
                    source2.Add(geometricExtents.MinPoint);
                }
            } catch {}
          }
          transaction.Commit();
        }
      }
      
      if (source1.Count == 0 || source2.Count == 0) return new Extents3d();

      double num1 = source1.Max<Point3d>((System.Func<Point3d, double>) (pos => pos.X));
      double num2 = source1.Max<Point3d>((System.Func<Point3d, double>) (pos => pos.Y));
      double num3 = source1.Max<Point3d>((System.Func<Point3d, double>) (pos => pos.Z));
      Point3d point3d1 = new Point3d(num1, num2, num3);
      double num4 = source2.Min<Point3d>((System.Func<Point3d, double>) (pos => pos.X));
      double num5 = source2.Min<Point3d>((System.Func<Point3d, double>) (pos => pos.Y));
      double num6 = source2.Min<Point3d>((System.Func<Point3d, double>) (pos => pos.Z));
      Point3d point3d2 = new Point3d(num4, num5, num6);
      Extents3d extents3d = new Extents3d();
      extents3d.Set(point3d2, point3d1);
      return extents3d;
    }

    private bool EntityIsSupport(
      Database database,
      ObjectId id,
      string viewDir,
      out SupportInfo info,
      out string[] new_sp)
    {
      info = new SupportInfo();
      new_sp = new string[2];
      using (Transaction transaction = database.TransactionManager.StartTransaction())
      {
        DBObject dbObject = transaction.GetObject(id, (OpenMode) 0);
        if (dbObject is Support)
        {
          PortCollection ports = ((Part) dbObject).GetPorts((PortType) 7);
          Point3d point3d = Point3d.Origin;
          Vector3d vector3d = Vector3d.ZAxis;
          foreach (Port port in (PnP3dCollection) ports)
          {
            switch (port.Name)
            {
              case "S1":
                vector3d = port.Direction;
                continue;
              case "S2":
                point3d = port.Position;
                continue;
              default:
                continue;
            }
          }
          StringCollection stringCollection = new StringCollection()
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
          // Need PSUtil
      var PSUtil = new PSUtil();
      if(PSUtil.dl_manager == null)
      {
           // Cannot retrieve properties without DataLinksManager
           return false;
      }

      StringCollection properties = PSUtil.dl_manager.GetProperties(id, stringCollection, true);
      new_sp[0] = "";
      if(properties.Count > 0) new_sp[0] = properties[0];
      
      new_sp[1] = viewDir;
      if (string.IsNullOrEmpty(new_sp[0]))
        return false;
      
      info.Name = new_sp[0];
      
      if(this.ViewTypes.ContainsKey(new_sp[1]))
          info.ViewType = this.ViewTypes[new_sp[1]];
          
      info.Ids = new ObjectId[1];
      info.Ids[0] = id;
      
      if(this.UCSs.ContainsKey(new_sp[1]))
          info.UCS = this.UCSs[new_sp[1]];
      
      if(this.UpVectors.ContainsKey(new_sp[1]))
          info.UpVector = this.UpVectors[new_sp[1]];
      
      if(this.ViewDirectionVectors.ContainsKey(new_sp[1]))
          info.ViewDirection = this.ViewDirectionVectors[new_sp[1]];
          
      if(properties.Count > 1) info.CPYDesignStd = properties[1];
      info.DatumPoint = point3d;
      info.SupportDirection = vector3d;
      info.PPoints = ports;
      info.DwgProjectNo = this.tbProjectNo.Text;
      info.DwgRevision = this.tbDwgRevision.Text;
      
      if(properties.Count > 2) info.PipeLineNo = properties[2];
      if(properties.Count > 3) info.SupportCoding = properties[3];
      if(properties.Count > 6) info.Size = properties[6];
      if(properties.Count > 7) info.StdName = properties[7];
      
      if(properties.Count > 4)
      {
           double d;
           if(double.TryParse(properties[4], out d)) info.BOP = Math.Round(d).ToString();
      }
      if(properties.Count > 5)
      {
           double d;
           if(double.TryParse(properties[5], out d)) info.COP = Math.Round(d).ToString();
      }

      return true;
    }
  }
  return false;
}

    private void EntityIsGroup(Database database, ObjectId id)
    {
      List<Group> whichEntityBelongs = this.FindGroupWhichEntityBelongs(database, id);
      if (whichEntityBelongs.Count != 1)
      {
        int num = (int) MessageBox.Show("Code (ER1008): Support are not follow the rules.Please check with admin.", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
      else
      {
        string name = whichEntityBelongs[0].Name;
        this.GetSupportGroup(database, name);
      }
    }

    private ObjectId[] GetSupportGroup(Database database, string sp_name)
    {
      ObjectId[] objectIdArray = new ObjectId[1];
      using (Transaction transaction = database.TransactionManager.StartTransaction())
      {
        DBDictionary dbDictionary = transaction.GetObject(database.GroupDictionaryId, (OpenMode) 0) as DBDictionary;
        return (transaction.GetObject((ObjectId) dbDictionary[sp_name], (OpenMode) 0) as Group).GetAllEntityIds();
      }
    }

    private List<Group> FindGroupWhichEntityBelongs(Database database, ObjectId id)
    {
      List<Group> whichEntityBelongs = new List<Group>();
      using (Transaction transaction = database.TransactionManager.StartTransaction())
      {
        foreach (ObjectId persistentReactorId in ((DBObject) (transaction.GetObject(id, (OpenMode) 0) as Entity)).GetPersistentReactorIds())
        {
          DBObject dbObject = transaction.GetObject(persistentReactorId, (OpenMode) 0);
          if (dbObject is Group)
          {
            Group group = (Group) dbObject;
            whichEntityBelongs.Add(group);
          }
        }
        transaction.Commit();
      }
      return whichEntityBelongs;
    }

    public void LoadGridData()
    {
       var PSUtil = new PSUtil();
       Document document = PSUtil.GetDocument();
       if (((DisposableWrapper) document) == ((DisposableWrapper) null))
       {
         MessageBox.Show("No drawings open!", "PlantFlow_Support");
         return;
       }
       
       this.SupportData.Rows.Clear();
       using (document.LockDocument())
       {
           PnPDatabase pnpDatabase = PSUtil.dl_manager.GetPnPDatabase();
           if (pnpDatabase == null) return;
           
           PnPTable table = pnpDatabase.Tables["Support"];
           if (table == null) return;
           
           PnPRow[] pnpRowArray;
           pnpRowArray = table.Select();
           
           // Gap Detection Logic
            var supportsByIdentifier = new Dictionary<string, List<SupportRowHelper>>();
            var allRows = new List<SupportRowHelper>();
            HashSet<string> uniqueIdentifiers = new HashSet<string>();

            foreach (PnPRow pnpRow in pnpRowArray)
            {
                  string identifier = pnpRow["SupportIdentifier"] != null ? pnpRow["SupportIdentifier"].ToString() : "";
                  if (!string.IsNullOrEmpty(identifier)) uniqueIdentifiers.Add(identifier);
                  string tag = pnpRow["Tag"] != null ? pnpRow["Tag"].ToString() : "";
                  
                  SupportRowHelper rowData = new SupportRowHelper();
                  rowData.PnPID = pnpRow.RowId;
                  rowData.Tag = tag;
                  rowData.Identifier = identifier;
                  rowData.SupportName = (pnpRow["SupportName"] != null) ? pnpRow["SupportName"].ToString() : "";
                  rowData.SupportDetail = (pnpRow["SupportDetail"] != null) ? pnpRow["SupportDetail"].ToString() : "";
                  
                  allRows.Add(rowData);

                  if (!string.IsNullOrEmpty(identifier))
                  {
                      if (!supportsByIdentifier.ContainsKey(identifier))
                          supportsByIdentifier[identifier] = new List<SupportRowHelper>();
                      supportsByIdentifier[identifier].Add(rowData);
                  }
            }

            // Populate ComboBox
            this.cbSupportType.Items.Clear();
            var sortedIds = uniqueIdentifiers.ToList();
            sortedIds.Sort();
            foreach (var id in sortedIds)
            {
                this.cbSupportType.Items.Add(id);
            }

            HashSet<long> processedPnPIDs = new HashSet<long>();
            List<object[]> finalGridRows = new List<object[]>();

            foreach(var kvp in supportsByIdentifier)
            {
                string identifier = kvp.Key;
                List<SupportRowHelper> rows = kvp.Value;
                
                var parsedRows = new List<Tuple<int, SupportRowHelper>>();
                int maxNum = 0;
                
                foreach(var r in rows)
                {
                    processedPnPIDs.Add(r.PnPID);
                    int num = ExtractNumber(r.Tag);
                    if (num > 0) 
                    {
                        if (num > maxNum) maxNum = num;
                        parsedRows.Add(new Tuple<int, SupportRowHelper>(num, r));
                    }
                    else
                    {
                        parsedRows.Add(new Tuple<int, SupportRowHelper>(-1, r));
                    }
                }

                HashSet<int> existingNums = new HashSet<int>();
                foreach(var pr in parsedRows) 
                { 
                     if(pr.Item1 > 0) existingNums.Add(pr.Item1); 
                }
                
                for (int i = 1; i < maxNum; i++)
                {
                    if (!existingNums.Contains(i))
                    {
                        string ghostTag = string.Format("{0}-{1:D4}", identifier, i);
                        // Checked (True) for Missing items (Fixed Number)
                        finalGridRows.Add(new object[] { -1L, ghostTag, identifier, "(Missing)", "N/A", true });
                    }
                }

                foreach(var pr in parsedRows)
                {
                    SupportRowHelper r = pr.Item2;
                    // Existing items are Unchecked (Editable) by default
                    finalGridRows.Add(new object[] { (long)r.PnPID, r.Tag, r.Identifier, r.SupportName, r.SupportDetail, false });
                }
            }

            foreach(var r in allRows)
            {
                if (!processedPnPIDs.Contains(r.PnPID))
                {
                     // Existing items are Unchecked
                     finalGridRows.Add(new object[] { (long)r.PnPID, r.Tag, r.Identifier, r.SupportName, r.SupportDetail, false });
                }
            }

            finalGridRows.Sort((a, b) => {
                 long id1 = (long)a[0];
                 long id2 = (long)b[0];
                 return id1.CompareTo(id2);
            });

            foreach(var rowData in finalGridRows)
            {
                this.SupportData.Rows.Add(rowData);
            }
       }
    }

    private int ExtractNumber(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return 0;
        var parts = tag.Split('-');
        if (parts.Length > 0)
        {
            string lastPart = parts[parts.Length - 1];
            int n;
            if (int.TryParse(lastPart, out n)) return n;
        }
        return 0;
    }

    public void SetIdentifierForDrawing()
    {
        try 
        {
             var PSUtil = new PSUtil();
             PSUtil.Print("Processing entire drawing...");
             Document document = PSUtil.GetDocument();
             if (document == null) return;

             PlantSupportName plantSupportName = new PlantSupportName();
             string drawing_name = document.Name.Split('\\').Last();
             
             List<int> rowIds = plantSupportName.CurrentDrawingData(drawing_name);
             if (rowIds.Count == 0) 
             {
                 MessageBox.Show("No supports found for this drawing.", "PlantFlow_Support");
                 return;
             }

             int successCount = 0;
             using (document.LockDocument())
             {
                 foreach (int row_id in rowIds)
                 {
                    try 
                    {
                        ObjectId objectId = PSUtil.ConvertRowIdToObjectId(row_id);
                        if (objectId == ObjectId.Null) continue;

                        Dictionary<string, string> supportDimension = PSUtil.GetSupportDimension(objectId);
                        StringCollection props = PSUtil.dl_manager.GetProperties(objectId, new StringCollection() { "ShortDescription" }, true);
                        if (props == null || props.Count == 0) continue;
                        
                        string property = props[0];
                        plantSupportName.support_params = supportDimension;
                        plantSupportName.std_name = property;

                        string str1 = "";
                        if (supportDimension.ContainsKey("TY"))
                        {
                           string TY = supportDimension["TY"];
                           str1 = plantSupportName.TurnTypeFromCodeToStr(TY);
                        }
                        string str2 = property + str1;

                        PSUtil.dl_manager.SetProperties(objectId, new StringCollection() { "SupportIdentifier" }, new StringCollection() { str2 });
                        successCount++;
                    }
                    catch {}
                 }
             }

             MessageBox.Show($"Updated {successCount} supports in drawing.", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
             LoadGridData(); 
        }
        catch (Exception ex)
        {
             MessageBox.Show("Error in Pre-Process: " + ex.Message);
        }
    }

    private void SetIdentifierForSelection(List<ObjectId> ids)
    {
        try
        {
            var PSUtil = new PSUtil();
            Document document = PSUtil.GetDocument();
            if (document == null) return;
            Database database = document.Database;

            int successCount = 0;
            using (document.LockDocument())
            {
                using (Transaction transaction = database.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId objectId in ids)
                    {
                        try 
                        {
                             PlantSupportName plantSupportName = new PlantSupportName();
                             Dictionary<string, string> supportDimension = PSUtil.GetSupportDimension(objectId);
                             
                             StringCollection props = PSUtil.dl_manager.GetProperties(objectId, new StringCollection() { "ShortDescription" }, true);
                             if (props == null || props.Count == 0) continue;
                             string property = props[0];

                             plantSupportName.std_name = property;
                             plantSupportName.support_params = supportDimension;

                             string str1 = "";
                             if (supportDimension.ContainsKey("TY"))
                             {
                                 string TY = supportDimension["TY"];
                                 str1 = plantSupportName.TurnTypeFromCodeToStr(TY);
                             }
                             string str2 = property + str1;

                             PSUtil.dl_manager.SetProperties(objectId, new StringCollection() { "SupportIdentifier" }, new StringCollection() { str2 });
                             successCount++;
                        }
                        catch {}
                    }
                    transaction.Commit();
                }
            }
            if (successCount > 0)
                MessageBox.Show($"Successfully set identifier for {successCount} supports.", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error setting identifier: " + ex.Message);
        }
    }

    private void DeleteSelectedSupports()
    {
        if (this.dgvPSDM.SelectedRows.Count == 0) return;
        if (MessageBox.Show($"Are you sure you want to delete {this.dgvPSDM.SelectedRows.Count} items?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try
        {
            var PSUtil = new PSUtil();
            Document doc = PSUtil.GetDocument();
            if (doc == null) return;
            var localPSUtil = new PSUtil();
            PnPDatabase db = localPSUtil.dl_manager.GetPnPDatabase();
            PnPTable table = db.Tables["Support"];

            using (doc.LockDocument())
            {
                db.StartTransaction();
                try
                {
                    List<DataGridViewRow> uiRowsToRemove = new List<DataGridViewRow>();
                    int dbDelCount = 0;
                    bool dbChanged = false;

                    foreach (DataGridViewRow row in this.dgvPSDM.SelectedRows)
                    {
                        if (row.IsNewRow) continue;
                        if (row.Cells["PnPID"].Value == null) continue;
                        long pid = Convert.ToInt64(row.Cells["PnPID"].Value);
                        
                        if (pid == -1) 
                        {
                            // Missing Row: Mark for UI removal only
                            // Note: We intentionally DO NOT delete anything from DB (it's a gap)
                            // And we DO NOT reload grid (which would regenerate the gap)
                            uiRowsToRemove.Add(row);
                        }
                        else
                        {
                            // Real Row: Delete from DB
                            PnPRow[] rows = table.Select(string.Format("PnPID={0}", pid));
                            foreach(var r in rows) 
                            {
                                r.Delete();
                                dbDelCount++;
                                dbChanged = true;
                            }
                        }
                    }

                    if (dbChanged)
                    {
                        db.CommitTransaction();
                    }
                    else
                    {
                         db.CommitTransaction(); 
                    }

                    // Remove Missing Rows from UI
                    foreach(var r in uiRowsToRemove)
                    {
                        if (!r.IsNewRow && this.dgvPSDM.Rows.Contains(r))
                        {
                             this.dgvPSDM.Rows.Remove(r);
                        }
                    }

                    // MessageBox.Show($"Deleted {dbDelCount} DB items and removed {uiRowsToRemove.Count} missing items from list.", "PlantFlow_Support");

                    if (dbChanged)
                    {
                        this.LoadGridData(); // Refresh UI to reflect DB changes
                    }
                }
                catch (Exception ex)
                {
                    db.RollbackTransaction();
                    MessageBox.Show("Error deleting supports: " + ex.Message);
                }
            }
        }
        catch (Exception exInit)
        {
            MessageBox.Show("Error initializing delete: " + exInit.Message);
        }
    }

    private void AutoNameForSelection(HashSet<long> targetPnPIDs)
    {
       string text = this.tbStartNumber.Text;
       int startNum;
       if (!int.TryParse(text, out startNum))
       {
         MessageBox.Show("Please enter a valid Start Number (integer).", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
         return;
       }

       try 
       {
           var PSUtil = new PSUtil();
           Document doc = PSUtil.GetDocument();
           if (doc == null) return;

           // Local PSUtil
           var localPSUtil = new PSUtil();
           PnPDatabase db = localPSUtil.dl_manager.GetPnPDatabase();
           PnPTable table = db.Tables["PipeRunComponent"];
           PnPTable tableSup = db.Tables["Support"];

           using (doc.LockDocument())
           {
               db.StartTransaction();
               try
               {
                // PHASE 1: Clean Tags
                /*
                foreach (DataGridViewRow row in this.dgvPSDM.Rows)
                {
                    if (row.IsNewRow) continue;
                    if (row.Cells["PnPID"].Value == null) continue;
                    long pnpId = Convert.ToInt64(row.Cells["PnPID"].Value);
                    
                    if (targetPnPIDs.Contains(pnpId))
                    {
                         PnPRow[] rowsSup = tableSup.Select(string.Format("PnPID={0}", pnpId));
                         if (rowsSup.Length > 0) rowsSup[0]["SupportName"] = "";

                         PnPRow[] rows = table.Select(string.Format("PnPID={0}", pnpId));
                         if (rows.Length > 0) rows[0]["Tag"] = "";
                    }
                } 
                */
                db.CommitTransaction();
                
                // PHASE 2: Set New Names
                // Get Editor for logging
                Autodesk.AutoCAD.EditorInput.Editor ed = doc.Editor;
                ed.WriteMessage("\n[Debug] AutoName Transaction Started.");

                db.StartTransaction();
                int currentNum = startNum;

                // Use Grid Order for Numbering
                foreach (DataGridViewRow row in this.dgvPSDM.Rows)
                {
                    if (row.IsNewRow) continue;
                    if (row.Cells["PnPID"].Value == null) continue;

                    long pnpId = Convert.ToInt64(row.Cells["PnPID"].Value);

                    // Check Fixed Number (Status)
                    bool isFixed = false;
                    if (row.Cells["Status"].Value != null && row.Cells["Status"].Value is bool)
                    {
                        isFixed = (bool)row.Cells["Status"].Value;
                    }

                    if (targetPnPIDs.Contains(pnpId))
                    {
                        if (isFixed)
                        {
                            // Checked means "Fixed Number" (Missing or Locked).
                            // Skip update but consume sequence number
                            ed.WriteMessage($"\n[Debug] SKIP PnPID {pnpId}: Fixed Number. Consuming sequence.");
                            currentNum++;
                            continue;
                        }

                        string identifier = row.Cells["Identifier"].Value != null ? row.Cells["Identifier"].Value.ToString() : "";
                        string newName = string.Format("{0}-{1}", identifier, currentNum.ToString("D4"));
                        
                        ed.WriteMessage($"\n[Debug] Processing PnPID {pnpId}: NewName={newName}");

                        // Update DB
                        PnPRow[] rowsSup = tableSup.Select(string.Format("PnPID={0}", pnpId));
                        if (rowsSup.Length > 0) rowsSup[0]["SupportName"] = newName;
                        
                        PnPRow[] rows = table.Select(string.Format("PnPID={0}", pnpId));
                        if (rows.Length > 0) rows[0]["Tag"] = newName;

                        // Update UI
                        if (row.Cells["Tag"] != null) row.Cells["Tag"].Value = newName;
                        
                        // Note: Regular items remain Unchecked (Editable).

                        currentNum++;
                    }
                }
                db.CommitTransaction();
                    MessageBox.Show("Auto Name Completed Successfully!", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                catch (Exception ex)
                {
                    // ed.WriteMessage($"\n[Error] AutoName Failure: {ex.Message}");
                    db.RollbackTransaction();
                    MessageBox.Show("Error during Auto Name: " + ex.Message);
                }
           }
       }
       catch (Exception exInit)
       {
           MessageBox.Show("Error initializing Auto Name: " + exInit.Message);
       }
    }


    private void AutoCodingForSelection(IEnumerable<ObjectId> objectIds, Document document)
    {
        Database database = document.Database;
        Dictionary<long, string> updatesToApply = new Dictionary<long, string>();
        Dictionary<long, string> tagsToClean = new Dictionary<long, string>();

        // Phase 1: Calculate Values (AutoCAD Transaction)
        using (document.LockDocument())
        {
          using (Transaction transaction = database.TransactionManager.StartTransaction())
          {
            foreach (ObjectId objectId in objectIds)
            {
              if (!objectId.IsNull && !objectId.IsErased && objectId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof (Support))))
              {
                try
                {
                    // Open for Read
                    DBObject dbObj = transaction.GetObject(objectId, OpenMode.ForRead);
                    Support support = dbObj as Support;
                    if (support == null) continue;

                    PlantAutoCoding plantAutoCoding = new PlantAutoCoding();
                    
                    PortCollection ports = support.GetPorts((PortType) 7);
                    plantAutoCoding.PPorts = ports;
                    
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    // Needs PSUtil instance. Since this method is static-like (passed objects), it assumes PSUtil exists? 
                    // No, PSUtil was a field. We must instantiate one or pass it.
                    var psUtilInstance = new PSUtil(); 
                    
                    foreach (var item in psUtilInstance.dl_manager.GetAllProperties(objectId, true))
                    {
                       dictionary[item.Key] = item.Value;
                    }
                    Dictionary<string, string> supportDimension = psUtilInstance.GetSupportDimension(objectId);
                    plantAutoCoding.SupportParams = supportDimension;

                    // Tag Logic Refactor:
                    // 1. Get Tag
                    string tagVal = dictionary.ContainsKey("Tag") ? dictionary["Tag"] : "";
                    // 2. Clean Tag (Force Update)
                    if (!string.IsNullOrEmpty(tagVal))
                    {
                        // Clean '?' if present, otherwise just re-write to force refresh
                        string cleanTag = tagVal.Replace("?", "").Trim();
                        // Log first one
                        // MessageBox.Show($"Read Tag: '{tagVal}' -> Clean: '{cleanTag}'"); 

                        int rowIdInternal = psUtilInstance.dl_manager.FindAcPpRowId(objectId);
                        
                        // Debug Log
                        // MessageBox.Show($"Tag Check: Val='{tagVal}' RowID={rowIdInternal}");

                        if (rowIdInternal > 0) 
                        {
                            tagsToClean[(long)rowIdInternal] = cleanTag;
                            // Only log if it HAD '?' or just once
                             if (tagVal.Contains("?"))
                                MessageBox.Show($"Phase 1: Found '?' in Tag.\nCleaned: '{cleanTag}'\nPnPID: {rowIdInternal}", "DEBUG - Phase 1");
                        }
                    }
                    
                    plantAutoCoding.Properties = dictionary;
                    
                    string std_type = dictionary.ContainsKey("ShortDescription") ? dictionary["ShortDescription"] : "";
                    string str = plantAutoCoding.SupportCoding(std_type);

                    // Get PnPID
                    int rowId = psUtilInstance.dl_manager.FindAcPpRowId(objectId);
                    if (rowId > 0 && !string.IsNullOrEmpty(str))
                    {
                        updatesToApply[(long)rowId] = str;
                    }
                }
                catch(System.Exception ex)
                {
                    // Log but continue
                    // MessageBox.Show($"Calc Error: {ex.Message}\nStack: {ex.StackTrace}", "Debug - Calc Error");
                }
              }
            }
            transaction.Commit();
          }
        }

                // Phase 2: Update PnP Database Directly (PnP Transaction) - using Idle
                // Wrap in try to match the existing catch block below
                try
                {
                    if (updatesToApply.Count > 0 || tagsToClean.Count > 0)
                {
                    // IDLE STRATEGY FIX
                    // Instead of updating immediately, we queue the update to Application.Idle
                    // This avoids conflict with Plant 3D reactors that cause properties to revert or show "?"
                    
                    if (this.PendingUpdates == null) this.PendingUpdates = new Dictionary<long, string>();
                    if (this.PendingTags == null) this.PendingTags = new Dictionary<long, string>();

                    foreach(var kvp in updatesToApply) this.PendingUpdates[kvp.Key] = kvp.Value;
                    foreach(var kvp in tagsToClean) this.PendingTags[kvp.Key] = kvp.Value;

                    // Ensure handler is attached only once
                    Autodesk.AutoCAD.ApplicationServices.Application.Idle -= new EventHandler(this.Application_Idle_UpdateSupportCoding);
                    Autodesk.AutoCAD.ApplicationServices.Application.Idle += new EventHandler(this.Application_Idle_UpdateSupportCoding);
                    
                    MessageBox.Show("updates queued for Idle event.", "Debug: Set Support Coding");
                }
            }
            catch (System.Exception ex)
            {
               MessageBox.Show("Error initializing Idle Update: " + ex.Message);
            }
    }

    private void Application_Idle_UpdateSupportCoding(object sender, EventArgs e)
    {
        Autodesk.AutoCAD.ApplicationServices.Application.Idle -= new EventHandler(this.Application_Idle_UpdateSupportCoding); // Run Once
        
        if ((this.PendingUpdates == null || this.PendingUpdates.Count == 0) && (this.PendingTags == null || this.PendingTags.Count == 0)) return;

        Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        if (doc == null) return;

        try
        {
            var psUtilInstance = new PSUtil();
            PnPDatabase db = psUtilInstance.pnp_db;
            if (db == null) return;

            db.StartTransaction();
            // MessageBox.Show("Idle Update Started\nPendingTags: " + (this.PendingTags == null ? 0 : this.PendingTags.Count), "DEBUG - Idle");
            try 
            {
                 // 1. Update SupportDetail FIRST (to allow Tag cleaning to overwrite any dirtying)
                 PnPTable tableDetail = db.Tables["Support"]; 
                 if (tableDetail == null) tableDetail = db.Tables["PipeRunComponent"];

                 if (this.PendingUpdates != null && this.PendingUpdates.Count > 0 && tableDetail != null)
                 {
                     foreach(var kvp in this.PendingUpdates)
                     {
                         PnPRow[] rows = tableDetail.Select(string.Format("PnPID={0}", kvp.Key));
                         if (rows.Length > 0)
                         {
                             rows[0]["SupportDetail"] = kvp.Value;
                         }
                     }
                 }

                 // 2. Update Tags (Clean '?') SECOND
                 PnPTable tableTag = db.Tables["PipeRunComponent"];
                 if (tableTag == null) tableTag = db.Tables["EngineeringItems"];
                 // Debug Table
                 // MessageBox.Show($"Using Table for Tags: {(tableTag != null ? "Found" : "NULL")}", "Debug - Idle");

                 if (this.PendingTags != null && this.PendingTags.Count > 0 && tableTag != null)
                 {
                     foreach(var kvp in this.PendingTags)
                     {
                         PnPRow[] rows = tableTag.Select(string.Format("PnPID={0}", kvp.Key));
                         if (rows.Length > 0)
                         {
                             rows[0]["Tag"] = kvp.Value;
                         }
                         else 
                         {
                            // Try EngineeringItems if PipeRun failed
                            PnPTable tableEng = db.Tables["EngineeringItems"];
                            if(tableEng != null) {
                                PnPRow[] rowsEng = tableEng.Select(string.Format("PnPID={0}", kvp.Key));
                                if(rowsEng.Length > 0) rowsEng[0]["Tag"] = kvp.Value;
                            }
                         }
                     }
                 }
                 
                 db.CommitTransaction();
                 // MessageBox.Show("Idle Update Committed", "DEBUG - Idle");
            }
            catch(System.Exception exInner)
            {
                db.RollbackTransaction();
                MessageBox.Show("Idle Update Failed: " + exInner.Message);
            }
            finally
            {
                this.PendingUpdates.Clear();
                this.PendingTags.Clear();
            }
        }
        catch(System.Exception ex)
        {
             MessageBox.Show("Idle Major Error: " + ex.Message);
        }
    }

    private void AutoNameForSelection_Sorted(HashSet<long> targetPnPIDs)
    {
       string text = this.tbStartNumber.Text;
       int startNum;
       if (!int.TryParse(text, out startNum))
       {
         MessageBox.Show("Please enter a valid Start Number (integer).", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
         return;
       }

       // Dynamic Format: "D" + length of input string (e.g. "01" -> "D2", "001" -> "D3")
       string fmt = "D" + text.Trim().Length.ToString();

       try 
       {
           var PSUtil = new PSUtil();
           Document doc = PSUtil.GetDocument();
           if (doc == null) return;

           var localPSUtil = new PSUtil();
           PnPDatabase db = localPSUtil.dl_manager.GetPnPDatabase();
           PnPTable table = db.Tables["PipeRunComponent"];
           PnPTable tableSup = db.Tables["Support"];

           using (doc.LockDocument())
           {
               // Get Editor
               Autodesk.AutoCAD.EditorInput.Editor ed = doc.Editor;
               db.StartTransaction();
               int currentNum = startNum;

               try
               {
                    // 1. Identify Reserved Numbers from ALL Fixed Items
                    HashSet<int> reservedNumbers = new HashSet<int>();
                    foreach (DataGridViewRow row in this.dgvPSDM.Rows)
                    {
                        if (row.Cells["Status"].Value != null && row.Cells["Status"].Value is bool && (bool)row.Cells["Status"].Value)
                        {
                            string tag = row.Cells["Tag"].Value != null ? row.Cells["Tag"].Value.ToString() : "";
                            int rNum = ExtractNumber(tag);
                            if (rNum > 0) reservedNumbers.Add(rNum);
                        }
                    }

                    // 2. Prepare Candidates (Target PnPIDs & Not Fixed)
                    List<DataGridViewRow> candidates = new List<DataGridViewRow>();
                    foreach (DataGridViewRow row in this.dgvPSDM.Rows)
                    {
                        if (row.IsNewRow) continue;
                        if (row.Cells["PnPID"].Value == null) continue;

                        long pnpId = Convert.ToInt64(row.Cells["PnPID"].Value);
                        bool isFixed = (row.Cells["Status"].Value != null && row.Cells["Status"].Value is bool) && (bool)row.Cells["Status"].Value;

                        if (targetPnPIDs.Contains(pnpId) && !isFixed)
                        {
                            candidates.Add(row);
                        }
                    }

                    // 3. Sort Candidates by PnPID (Ascending)
                    candidates.Sort((r1, r2) => {
                        long id1 = Convert.ToInt64(r1.Cells["PnPID"].Value);
                        long id2 = Convert.ToInt64(r2.Cells["PnPID"].Value);
                        return id1.CompareTo(id2);
                    });
                    
                    ed.WriteMessage($"\n[Debug] Found {candidates.Count} candidates to rename (Sorted by PnPID). Reserved Numbers: {reservedNumbers.Count}");

                    foreach (DataGridViewRow row in candidates)
                    {
                        long pnpId = Convert.ToInt64(row.Cells["PnPID"].Value);
                        string identifier = row.Cells["Identifier"].Value != null ? row.Cells["Identifier"].Value.ToString() : "";

                        // Find next available number (Skip Reserved)
                        while (reservedNumbers.Contains(currentNum))
                        {
                            currentNum++;
                        }
                        
                        string newName = string.Format("{0}-{1}", identifier, currentNum.ToString(fmt));

                        ed.WriteMessage($"\n[Debug] Processing PnPID {pnpId}: NewName={newName}");

                        // Update DB
                        PnPRow[] rowsSup = tableSup.Select(string.Format("PnPID={0}", pnpId));
                        if (rowsSup.Length > 0) rowsSup[0]["SupportName"] = newName;

                        PnPRow[] rows = table.Select(string.Format("PnPID={0}", pnpId));
                        if (rows.Length > 0) rows[0]["Tag"] = newName;

                        // Update UI
                        if (row.Cells["Tag"] != null) row.Cells["Tag"].Value = newName;

                        // Mark used
                        reservedNumbers.Add(currentNum);
                        currentNum++;
                    }
                    db.CommitTransaction();
                    MessageBox.Show($"Auto Name Completed! Processed {candidates.Count} items.", "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
               }
               catch (Exception ex)
               {
                    db.RollbackTransaction();
                    MessageBox.Show("Error during Auto Name: " + ex.Message);
               }
           }
       }
       catch (Exception exInit)
       {
           MessageBox.Show("Error initializing Auto Name: " + exInit.Message);
       }
    }

  }
}
