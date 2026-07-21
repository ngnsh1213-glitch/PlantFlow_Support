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
      AddSupportCore(this.GetSelectedViewNames(), out code, out msg, out added, out cancelled);
      if (cancelled)
        return;
      if (code != null)
      {
        int num = (int)MessageBox.Show("Code (" + code + "): " + msg, "PlantFlow_Support", MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
    }

    private void AddSupportCore(string viewDir, out string code, out string msg, out object added, out bool cancelled)
    {
      AddSupportCore(new List<string>() { viewDir }, out code, out msg, out added, out cancelled);
    }

    private List<string> GetSelectedViewNames()
    {
      List<string> views = new List<string>();
      if (this.clbViewDirections != null)
      {
        foreach (object item in this.clbViewDirections.CheckedItems)
        {
          string view = item == null ? string.Empty : item.ToString();
          if (!string.IsNullOrEmpty(view) && !views.Contains(view))
            views.Add(view);
        }
      }
      if (views.Count == 0 && this.cbbViewDirection != null && !string.IsNullOrEmpty(this.cbbViewDirection.Text))
        views.Add(this.cbbViewDirection.Text);
      if (views.Count == 0)
        views.Add("Top");
      return views;
    }

    private void AddSupportCore(List<string> viewDirs, out string code, out string msg, out object added, out bool cancelled)
    {
      code = null;
      msg = null;
      added = null;
      cancelled = false;
      if (viewDirs == null || viewDirs.Count == 0)
        viewDirs = new List<string>() { "Top" };
      string representativeView = viewDirs[0];
      string viewSummary = string.Join(",", viewDirs.ToArray());
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
            List<ObjectId> frameworkIds = new List<ObjectId>();
            List<AttachmentInfo> attachmentInfoList = new List<AttachmentInfo>();
            List<ObjectId> objectIdList = new List<ObjectId>();
            Dictionary<ObjectId, Point3d> pipeSnapPoints = new Dictionary<ObjectId, Point3d>();
            Dictionary<ObjectId, PortCollection> pipePorts = new Dictionary<ObjectId, PortCollection>();
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
                      try
                      {
                        pipePorts[objectId] = (dbObject as Part).GetPorts((PortType) 7);
                      }
                      catch (Exception ex)
                      {
                        string pipePortError = "AddSupportCore pipe port 조회 실패 id=" + objectId + " error=" + ex.Message;
                        DiagLog(pipePortError);
                        PlantOrthoView.FileDiag(pipePortError);
                        pipeSnapPoints[objectId] = this.GetPartNearestPoint(dbObject as Part, Point3d.Origin);
                      }
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
                          frameworkIds.Add(objectId);
                          continue;
                        default:
                          continue;
                      }
                  }
                }
                transaction.Commit();
              }
            }
            if (frameworkIds.Count == 0)
            {
              code = "ER1002";
              msg = "Selected object are not Plant Support type. Please reselect!";
              return;
            }

            List<SupportInfo> supportInfos = new List<SupportInfo>();
            Dictionary<string, string[]> supportRows = new Dictionary<string, string[]>();
            int unnamedCount = 0;
            int invalidCount = 0;
            int duplicateCount = 0;
            foreach (ObjectId frameworkId in frameworkIds)
            {
              SupportInfo info;
              string[] new_sp;
              if (!this.EntityIsSupport(database, frameworkId, representativeView, out info, out new_sp))
              {
                if (new_sp != null && new_sp.Length > 0 && string.IsNullOrEmpty(new_sp[0]))
                  unnamedCount++;
                else
                  invalidCount++;
                continue;
              }
              info.ViewSpecs = this.CreateViewSpecs(viewDirs);
              if (info.ViewSpecs.Count > 0)
              {
                ViewSpec firstSpec = info.ViewSpecs[0];
                info.ViewType = firstSpec.ViewType;
                info.UCS = firstSpec.UCS;
                info.UpVector = firstSpec.UpVector;
                info.ViewDirection = firstSpec.ViewDirection;
                new_sp[1] = viewSummary;
              }

              if (this.SupportSelection.ContainsKey(info.Name) || supportRows.ContainsKey(info.Name) || this.ListViewContainsSupport(info.Name))
              {
                duplicateCount++;
                continue;
              }

              supportInfos.Add(info);
              supportRows[info.Name] = new_sp;
            }

            if (supportInfos.Count == 0)
            {
              if (duplicateCount > 0)
              {
                code = "ER1003";
                msg = "Already existed in current selection list. Please reselect!";
                return;
              }

              code = unnamedCount > 0 ? "ER1007" : "ER1002";
              msg = unnamedCount > 0
                ? "This support is unnamed. Please check the SupportName property."
                : "Selected object are not Plant Support type. Please reselect!";
              return;
            }

            Dictionary<string, List<AttachmentInfo>> attachmentsBySupport = new Dictionary<string, List<AttachmentInfo>>();
            Dictionary<string, List<ObjectId>> pipesBySupport = new Dictionary<string, List<ObjectId>>();
            foreach (SupportInfo info in supportInfos)
            {
              attachmentsBySupport[info.Name] = new List<AttachmentInfo>();
              pipesBySupport[info.Name] = new List<ObjectId>();
            }

            foreach (AttachmentInfo attachmentInfo in attachmentInfoList)
            {
              SupportInfo owner = this.FindNearestSupport(supportInfos, attachmentInfo == null ? null : attachmentInfo.PPoints);
              if (owner != null)
                attachmentsBySupport[owner.Name].Add(attachmentInfo);
            }

            foreach (ObjectId pipeId in objectIdList)
            {
              SupportInfo owner = pipePorts.ContainsKey(pipeId) ? this.FindNearestSupport(supportInfos, pipePorts[pipeId]) : null;
              if (owner == null)
              {
                Point3d pipePoint = pipeSnapPoints.ContainsKey(pipeId) ? pipeSnapPoints[pipeId] : Point3d.Origin;
                owner = this.FindNearestSupport(supportInfos, pipePoint);
              }
              if (owner != null)
                pipesBySupport[owner.Name].Add(pipeId);
            }

            int addedCount = 0;
            foreach (SupportInfo info in supportInfos)
            {
              int num8 = 1;
              foreach (AttachmentInfo attachmentInfo in attachmentsBySupport[info.Name])
              {
                string str = info.Name + ":R" + num8.ToString();
                attachmentInfo.Name = "R" + num8.ToString();
                try
                {
                  PSUtil.dl_manager.SetProperties(attachmentInfo.Id, new StringCollection()
                  {
                    "SupportName"
                  }, new StringCollection() { str });
                }
                catch (Exception ex)
                {
                  string attachmentNameError = "AddSupportCore attachment SupportName set 실패 support='" + info.Name + "' id=" + attachmentInfo.Id + " error=" + ex.Message;
                  DiagLog(attachmentNameError);
                  PlantOrthoView.FileDiag(attachmentNameError);
                }
                ++num8;
              }
              info.AttachmentList = attachmentsBySupport[info.Name];
              info.PipeList = pipesBySupport[info.Name];
              this.RefreshViewSpecsAfterPipeResolution(info, viewDirs, database);
              this.ProbePipeAxis(info, database);
              this.SupportSelection.Add(info.Name, info);
              ListViewItem listViewItem1 = new ListViewItem(supportRows[info.Name]);
              this.lvSupportName.Items.Add(listViewItem1);
              addedCount++;
            }
            added = new { count = addedCount, duplicates = duplicateCount, unnamed = unnamedCount, invalid = invalidCount, view = viewSummary };
            if (addedCount > 1 || duplicateCount > 0 || unnamedCount > 0 || invalidCount > 0)
            {
              string addSummary = "AddSupportCore summary added=" + addedCount + " duplicate=" + duplicateCount + " unnamed=" + unnamedCount + " invalid=" + invalidCount;
              DiagLog(addSummary);
              PlantOrthoView.FileDiag(addSummary);
            }
            PSUtil.ed.Regen();
          }
        }
      }
    }

    private bool ListViewContainsSupport(string supportName)
    {
      foreach (ListViewItem item in this.lvSupportName.Items)
      {
        if (item.SubItems.Count > 0 && item.SubItems[0].Text == supportName)
          return true;
      }
      return false;
    }

    private SupportInfo FindNearestSupport(List<SupportInfo> supports, Point3d point)
    {
      SupportInfo nearest = null;
      double nearestDistance = double.MaxValue;
      foreach (SupportInfo support in supports)
      {
        double distance = support.DatumPoint.DistanceTo(point);
        if (distance < nearestDistance)
        {
          nearestDistance = distance;
          nearest = support;
        }
      }
      return nearest;
    }

    private SupportInfo FindNearestSupport(List<SupportInfo> supports, PortCollection ports)
    {
      if (ports == null || ports.Count == 0)
        return null;

      SupportInfo nearest = null;
      double nearestDistance = double.MaxValue;
      foreach (SupportInfo support in supports)
      {
        foreach (Port port in (PnP3dCollection) ports)
        {
          double distance = support.DatumPoint.DistanceTo(port.Position);
          if (distance < nearestDistance)
          {
            nearestDistance = distance;
            nearest = support;
          }
        }
      }
      return nearest;
    }

    // Spike-only diagnostics. Read-only pipe axis measurement; no support logic changes.
    private void ProbePipeAxis(SupportInfo info, Database database)
    {
      try
      {
        if (info == null)
          return;

        int pipeCount = info.PipeList == null ? 0 : info.PipeList.Count;
        string type = info.StdName ?? "";
        if (pipeCount == 0)
        {
          PlantOrthoView.FileDiag("PIPEAXIS support='" + info.Name + "' type='" + type + "' pipeCount=0 FALLBACK_NEEDED");
          return;
        }

        Vector3d s1 = info.SupportDirection;
        Vector3d representativeAxis = Vector3d.ZAxis;
        double representativeLength = -1.0;
        bool connMatched = false;

        using (Transaction transaction = database.TransactionManager.StartTransaction())
        {
          int index = 0;
          foreach (ObjectId pipeId in info.PipeList)
          {
            try
            {
              Part part = transaction.GetObject(pipeId, OpenMode.ForRead) as Part;
              if (part == null)
              {
                PlantOrthoView.FileDiag("PIPEAXIS pipe null support='" + info.Name + "' id=" + pipeId);
                index++;
                continue;
              }

              Vector3d axis;
              double length;
              string method;
              int portCount;
              string portDump;
              if (!this.TryGetPipeAxis(part, out axis, out length, out method, out portCount, out portDump))
              {
                PlantOrthoView.FileDiag("PIPEAXIS axis 실패 support='" + info.Name + "' id=" + pipeId);
                index++;
                continue;
              }

              double dotZ = Math.Abs(axis.DotProduct(Vector3d.ZAxis));
              double dotX = Math.Abs(axis.DotProduct(Vector3d.XAxis));
              double dotY = Math.Abs(axis.DotProduct(Vector3d.YAxis));
              string classification = dotZ > 0.9 ? "vertical" : dotX > 0.9 ? "horizontalX" : dotY > 0.9 ? "horizontalY" : "inclined";
              double horizontalDot = Math.Sqrt(dotX * dotX + dotY * dotY);
              double slopeDeg = Math.Acos(Math.Min(1.0, horizontalDot)) * 180.0 / Math.PI;
              double s1dot = s1.Length > 1e-9 ? Math.Abs(s1.GetNormal().DotProduct(axis)) : 0.0;
              double s1cross = s1.Length > 1e-9 ? s1.GetNormal().CrossProduct(axis).Length : 0.0;

              PlantOrthoView.FileDiag("PIPEAXIS support='" + info.Name + "' type='" + type + "' pipe[" + index + "] id=" + pipeId
                + " portCount=" + portCount + " ports=[" + portDump + "]"
                + " axis=" + axis + " len=" + length + " method=" + method
                + " class=" + classification + " slopeDeg=" + slopeDeg.ToString("0.##")
                + " s1dot=" + s1dot.ToString("0.###") + " s1cross=" + s1cross.ToString("0.###"));

              if (length > representativeLength)
              {
                representativeLength = length;
                representativeAxis = axis;
              }
            }
            catch (Exception ex)
            {
              PlantOrthoView.FileDiag("PIPEAXIS pipe 예외 support='" + info.Name + "' id=" + pipeId + ": " + ex.GetType().Name + ": " + ex.Message);
            }
            index++;
          }
          transaction.Commit();
        }

        PlantOrthoView.FileDiag("PIPEAXIS support='" + info.Name + "' pipeCount=" + pipeCount
          + " representativeAxis(longest)=" + representativeAxis + " repLen=" + representativeLength
          + " supportS1=" + s1 + " datum=" + info.DatumPoint + " connMatched=" + connMatched);
      }
      catch (Exception ex)
      {
        PlantOrthoView.FileDiag("ProbePipeAxis 예외 support='" + (info == null ? "?" : info.Name) + "': " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private void RefreshViewSpecsAfterPipeResolution(SupportInfo info, IEnumerable<string> viewNames, Database database)
    {
      if (info == null)
        return;

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
          ViewSpec mainSpec;
          if (this.TryCreateMainViewSpec(info, database, out mainSpec))
            specs.Add(mainSpec);
          continue;
        }

        if (!this.ViewTypes.ContainsKey(viewName))
        {
          DiagLog("RefreshViewSpecsAfterPipeResolution skip unknown view='" + viewName + "'");
          PlantOrthoView.FileDiag("RefreshViewSpecsAfterPipeResolution skip unknown view='" + viewName + "'");
          continue;
        }

        specs.Add(this.CreateFixedViewSpec(viewName));
      }

      if (specs.Count == 0)
        specs.Add(this.CreateFixedViewSpec("Top"));

      info.ViewSpecs = specs;
      ViewSpec firstSpec = specs[0];
      info.ViewType = firstSpec.ViewType;
      info.UCS = firstSpec.UCS;
      info.UpVector = firstSpec.UpVector;
      info.ViewDirection = firstSpec.ViewDirection;
    }

    private ViewSpec CreateFixedViewSpec(string viewName)
    {
      ViewSpec spec = new ViewSpec();
      spec.ViewName = viewName;
      spec.ViewType = this.ViewTypes.ContainsKey(viewName) ? this.ViewTypes[viewName] : PlantFlow_Support.Commands.ViewType.Top;
      spec.UCS = this.UCSs.ContainsKey(viewName) ? this.UCSs[viewName] : Matrix3d.Identity;
      spec.UpVector = this.UpVectors.ContainsKey(viewName) ? this.UpVectors[viewName] : Vector3d.YAxis.Negate();
      spec.ViewDirection = this.ViewDirectionVectors.ContainsKey(viewName) ? this.ViewDirectionVectors[viewName] : Vector3d.ZAxis;
      spec.PaperCenter = Point3d.Origin;
      return spec;
    }

    private bool TryCreateMainViewSpec(SupportInfo info, Database database, out ViewSpec spec)
    {
      spec = null;
      Vector3d axis;
      Vector3d up;
      Vector3d mainViewDir;
      if (!this.ComputePipeAxis(info, database, out axis, out up, out mainViewDir))
      {
        PlantOrthoView.FileDiag("MAIN_VIEW_SPEC skip support='" + (info == null ? "?" : info.Name) + "' reason=axis_compute_failed");
        return false;
      }

      spec = new ViewSpec();
      spec.ViewName = "Main";
      spec.ViewType = this.ViewTypes.ContainsKey("Front") ? this.ViewTypes["Front"] : PlantFlow_Support.Commands.ViewType.Front;
      spec.UCS = Matrix3d.AlignCoordinateSystem(
        Point3d.Origin,
        Vector3d.XAxis,
        Vector3d.YAxis,
        Vector3d.ZAxis,
        Point3d.Origin,
        axis,
        up,
        mainViewDir);
      spec.UpVector = up;
      spec.ViewDirection = mainViewDir;
      spec.PaperCenter = Point3d.Origin;
      PlantOrthoView.FileDiag("MAIN_VIEW_SPEC support='" + info.Name + "' viewName=Main viewType=" + spec.ViewType
        + " axis=" + axis + " up=" + up + " mainViewDir=" + mainViewDir
        + " isStandardView=" + IsStandardViewDirection(mainViewDir));
      return true;
    }

    private bool ComputePipeAxis(SupportInfo info, Database database, out Vector3d axis, out Vector3d up, out Vector3d mainViewDir)
    {
      axis = Vector3d.ZAxis;
      up = Vector3d.ZAxis;
      mainViewDir = Vector3d.XAxis;
      try
      {
        if (info == null || database == null)
          return false;

        Vector3d s1 = info.SupportDirection;
        Vector3d s1Normal = s1.Length > 1e-9 ? s1.GetNormal() : Vector3d.ZAxis;
        bool foundPipe = false;
        double bestDot = -1.0;
        double bestLength = -1.0;
        ObjectId bestPipeId = ObjectId.Null;
        string bestMethod = "none";

        if (info.PipeList != null && info.PipeList.Count > 0)
        {
          using (Transaction transaction = database.TransactionManager.StartTransaction())
          {
            foreach (ObjectId pipeId in info.PipeList)
            {
              try
              {
                Part part = transaction.GetObject(pipeId, OpenMode.ForRead) as Part;
                if (part == null)
                  continue;

                Vector3d candidateAxis;
                double candidateLength;
                string method;
                int portCount;
                string portDump;
                if (!this.TryGetPipeAxis(part, out candidateAxis, out candidateLength, out method, out portCount, out portDump))
                  continue;

                double s1dot = Math.Abs(s1Normal.DotProduct(candidateAxis));
                if (s1dot > bestDot)
                {
                  foundPipe = true;
                  bestDot = s1dot;
                  bestLength = candidateLength;
                  bestPipeId = pipeId;
                  bestMethod = method;
                  axis = candidateAxis;
                }
              }
              catch (Exception exPipe)
              {
                PlantOrthoView.FileDiag("PIPEAXIS_COMPUTE pipe 예외 support='" + info.Name + "' id=" + pipeId + ": " + exPipe.GetType().Name + ": " + exPipe.Message);
              }
            }
            transaction.Commit();
          }
        }

        string source = "pipe";
        if (!foundPipe)
        {
          if (s1.Length <= 1e-9)
          {
            PlantOrthoView.FileDiag("PIPEAXIS_COMPUTE 실패 support='" + info.Name + "' reason=no_pipe_and_s1_zero");
            return false;
          }
          axis = s1.GetNormal();
          source = "s1fallback";
          bestDot = 1.0;
          bestLength = 0.0;
          bestMethod = "supportDirection";
        }

        if (!this.ComputeMainViewBasis(axis, out up, out mainViewDir))
        {
          PlantOrthoView.FileDiag("PIPEAXIS_COMPUTE 실패 support='" + info.Name + "' reason=basis_failed axis=" + axis);
          return false;
        }

        Vector3d safeViewDir;
        Vector3d safeUp;
        double chosenExpose;
        double currentExpose;
        bool pipeSetNonEmpty;
        if (this.TrySafeMainViewBasis(info, axis, database, out safeViewDir, out safeUp, out chosenExpose, out currentExpose, out pipeSetNonEmpty)
          && pipeSetNonEmpty
          && chosenExpose > currentExpose + 0.01)
        {
          up = safeUp;
          mainViewDir = safeViewDir;
          PlantOrthoView.FileDiag("VIEWAXIS_APPLY support='" + info.Name + "' newViewDir=" + mainViewDir + " newUp=" + up
            + " expose " + currentExpose.ToString("0.###") + "->" + chosenExpose.ToString("0.###"));
        }
        else
        {
          PlantOrthoView.FileDiag("VIEWAXIS_KEEP support='" + info.Name + "' viewDir=" + mainViewDir + " up=" + up
            + " (pipeNonEmpty=" + pipeSetNonEmpty + ")");
        }

        PlantOrthoView.FileDiag("PIPEAXIS_COMPUTE support='" + info.Name + "' axis=" + axis
          + " up=" + up + " mainViewDir=" + mainViewDir + " src=" + source
          + " bestPipeId=" + bestPipeId + " bestS1Dot=" + bestDot.ToString("0.###")
          + " bestLen=" + bestLength.ToString("0.###") + " method=" + bestMethod);
        return true;
      }
      catch (Exception ex)
      {
        PlantOrthoView.FileDiag("ComputePipeAxis 예외 support='" + (info == null ? "?" : info.Name) + "': " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
    }

    private sealed class SafeViewPipeInfo
    {
      public Vector3d Axis;
      public string AxisLabel;
      public bool Spurious;
      public bool ConnMatched;
      public double MinZ;
      public double MaxZ;
      public ObjectId PipeId;
    }

    private bool TrySafeMainViewBasis(SupportInfo info, Vector3d pipeAxis, Database database, out Vector3d safeViewDir, out Vector3d safeUp, out double chosenExpose, out double currentExpose, out bool pipeSetNonEmpty)
    {
      safeViewDir = Vector3d.XAxis;
      safeUp = Vector3d.ZAxis;
      chosenExpose = 0.0;
      currentExpose = 0.0;
      pipeSetNonEmpty = false;
      try
      {
        if (info == null || database == null)
          return false;

        Vector3d currentMainViewDir;
        Vector3d currentUp;
        if (!this.ComputeMainViewBasis(pipeAxis, out currentUp, out currentMainViewDir))
          return false;
        Extents3d supportExtents;
        bool hasSupportExtents = this.TryGetSupportExtents(info, database, out supportExtents);
        const double SupportPipeZMargin = 150.0; // Pipe can sit above support steel by radius/gap; tune after VIEWAXIS_SAFE logs.
        double supportMinZ = hasSupportExtents ? supportExtents.MinPoint.Z : info.DatumPoint.Z - 1.0;
        double supportMaxZ = hasSupportExtents ? supportExtents.MaxPoint.Z : info.DatumPoint.Z + 1.0;
        double supportMinZWithMargin = supportMinZ - SupportPipeZMargin;
        double supportMaxZWithMargin = supportMaxZ + SupportPipeZMargin;

        List<SafeViewPipeInfo> pipes = new List<SafeViewPipeInfo>();
        if (info.PipeList != null && info.PipeList.Count > 0)
        {
          using (Transaction transaction = database.TransactionManager.StartTransaction())
          {
            int index = 0;
            foreach (ObjectId pipeId in info.PipeList)
            {
              try
              {
                Part part = transaction.GetObject(pipeId, OpenMode.ForRead) as Part;
                if (part == null)
                {
                  index++;
                  continue;
                }

                Vector3d axis;
                double length;
                string method;
                int portCount;
                string portDump;
                if (!this.TryGetPipeAxis(part, out axis, out length, out method, out portCount, out portDump))
                {
                  index++;
                  continue;
                }

                double minZ;
                double maxZ;
                bool hasZRange = this.TryGetPortZRange(part, out minZ, out maxZ);
                double zGap = hasZRange ? this.ComputeZGap(minZ, maxZ, supportMinZ, supportMaxZ) : double.NaN;
                bool connMatched = hasZRange && maxZ >= supportMinZWithMargin && minZ <= supportMaxZWithMargin;
                bool spurious = hasZRange && !connMatched;
                pipes.Add(new SafeViewPipeInfo()
                {
                  Axis = axis,
                  AxisLabel = "p" + index + ":" + this.FormatAxisForLog(axis) + ":zgap=" + zGap.ToString("0.###"),
                  Spurious = spurious,
                  ConnMatched = connMatched,
                  MinZ = hasZRange ? minZ : double.NaN,
                  MaxZ = hasZRange ? maxZ : double.NaN,
                  PipeId = pipeId
                });
              }
              catch (Exception exPipe)
              {
                PlantOrthoView.FileDiag("VIEWAXIS_SAFE pipe 예외 support='" + info.Name + "' id=" + pipeId + ": " + exPipe.GetType().Name + ": " + exPipe.Message);
              }
              index++;
            }
            transaction.Commit();
          }
        }

        List<Vector3d> supportPipeAxes = pipes.Where(p => !p.Spurious).Select(p => p.Axis).ToList();
        Vector3d longestAxis;
        string longestAxisName;
        double dx;
        double dy;
        double dz;
        this.GetLongestExtentAxis(hasSupportExtents ? supportExtents : info.Extents, out longestAxis, out longestAxisName, out dx, out dy, out dz);

        Vector3d[] candidates = new Vector3d[]
        {
          Vector3d.XAxis, Vector3d.XAxis.Negate(),
          Vector3d.YAxis, Vector3d.YAxis.Negate(),
          Vector3d.ZAxis, Vector3d.ZAxis.Negate(),
          currentMainViewDir
        };

        string chosen = this.FormatAxisForLog(currentMainViewDir);
        Vector3d chosenViewDir = currentMainViewDir.Length > 1e-9 ? currentMainViewDir.GetNormal() : Vector3d.XAxis;
        Vector3d chosenUp = Vector3d.ZAxis;
        bool chosenSafe = false;
        bool fallbackUsed = true;
        currentExpose = 1.0 - Math.Abs(longestAxis.DotProduct(currentMainViewDir.Length > 1e-9 ? currentMainViewDir.GetNormal() : Vector3d.XAxis));
        chosenExpose = currentExpose;
        double bestExpose = double.MinValue;
        var candidateLog = new System.Text.StringBuilder();

        for (int i = 0; i < candidates.Length; i++)
        {
          Vector3d candidate = candidates[i];
          if (candidate.Length <= 1e-9)
            continue;

          Vector3d viewDir = candidate.GetNormal();
          double maxDotP = this.MaxDotAgainstP(viewDir, supportPipeAxes);
          Vector3d candidateUp;
          double maxUpDotP;
          bool hasSafeUp = this.TryFindSafeUpCandidate(viewDir, supportPipeAxes, out candidateUp, out maxUpDotP);
          bool isVertical = Math.Abs(viewDir.DotProduct(Vector3d.ZAxis)) > 0.9; // B3: Main=입면 강제, 탑/바텀 배제.
          bool safe = supportPipeAxes.Count > 0 && maxDotP < 0.1 && hasSafeUp && !isVertical;
          double exposeScore = 1.0 - Math.Abs(longestAxis.DotProduct(viewDir));
          string label = i == candidates.Length - 1 ? "current" + this.FormatAxisForLog(viewDir) : this.FormatAxisForLog(viewDir);
          candidateLog.Append(label + ":" + maxDotP.ToString("0.###") + ":" + (safe ? "T" : "F") + ":" + exposeScore.ToString("0.###") + ":up=" + (hasSafeUp ? this.FormatAxisForLog(candidateUp) : "none") + ":vert=" + (isVertical ? "T" : "F") + " ");
          if (safe && exposeScore > bestExpose)
          {
            bestExpose = exposeScore;
            chosen = label;
            chosenViewDir = viewDir;
            chosenUp = candidateUp;
            chosenSafe = true;
            chosenExpose = exposeScore;
            fallbackUsed = false;
          }
        }

        PlantOrthoView.FileDiag("VIEWAXIS_SAFE support='" + info.Name + "' P=[" + this.FormatPipeSet(pipes, false) + "] spurious=[" + this.FormatPipeSet(pipes, true) + "]"
          + " bbox=(" + dx.ToString("0.###") + "," + dy.ToString("0.###") + "," + dz.ToString("0.###") + ") bboxZ=(" + supportMinZ.ToString("0.###") + ".." + supportMaxZ.ToString("0.###") + ") zMargin=" + SupportPipeZMargin.ToString("0.###") + " bboxSource=" + (hasSupportExtents ? "geometric" : "datumFallback") + " longestAxis=" + longestAxisName
          + " candidates=[" + candidateLog.ToString().Trim() + "] chosen=" + chosen
          + " chosenSafe=" + (chosenSafe ? "T" : "F") + " chosenUp=" + this.FormatAxisForLog(chosenUp) + " chosenExpose=" + chosenExpose.ToString("0.###")
          + " fallbackUsed=" + (fallbackUsed ? "T" : "F") + " currentMainViewDir=" + this.FormatAxisForLog(currentMainViewDir)
          + " pipeAxis=" + this.FormatAxisForLog(pipeAxis));

        safeViewDir = chosenViewDir;
        safeUp = chosenUp;
        pipeSetNonEmpty = supportPipeAxes.Count > 0;
        return chosenSafe && !fallbackUsed;
      }
      catch (Exception ex)
      {
        PlantOrthoView.FileDiag("VIEWAXIS_SAFE 예외 support='" + (info == null ? "?" : info.Name) + "': " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
    }
    private bool TryGetSupportExtents(SupportInfo info, Database database, out Extents3d supportExtents)
    {
      supportExtents = new Extents3d();
      bool hasExtents = false;
      if (info == null || database == null || info.Ids == null || info.Ids.Length == 0)
        return false;

      using (Transaction transaction = database.TransactionManager.StartTransaction())
      {
        foreach (ObjectId id in info.Ids)
        {
          if (id.IsNull || id.IsErased)
            continue;

          try
          {
            Entity entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
            if (entity == null)
              continue;

            Extents3d extents = entity.GeometricExtents;
            if (!hasExtents)
            {
              supportExtents = extents;
              hasExtents = true;
            }
            else
            {
              supportExtents.AddExtents(extents);
            }
          }
          catch (Exception ex)
          {
            PlantOrthoView.FileDiag("VIEWAXIS_SAFE bbox 예외 support='" + info.Name + "' id=" + id + ": " + ex.GetType().Name + ": " + ex.Message);
          }
        }
        transaction.Commit();
      }
      return hasExtents;
    }
    private double ComputeZGap(double pipeMinZ, double pipeMaxZ, double supportMinZ, double supportMaxZ)
    {
      if (pipeMaxZ < supportMinZ)
        return supportMinZ - pipeMaxZ;
      if (pipeMinZ > supportMaxZ)
        return pipeMinZ - supportMaxZ;
      return 0.0;
    }
    private bool TryGetPortZRange(Part part, out double minZ, out double maxZ)
    {
      minZ = double.MaxValue;
      maxZ = double.MinValue;
      if (part == null)
        return false;

      PortCollection ports = part.GetPorts((PortType) 7);
      if (ports == null || ports.Count == 0)
        return false;

      foreach (Port port in (PnP3dCollection) ports)
      {
        double z = port.Position.Z;
        if (z < minZ)
          minZ = z;
        if (z > maxZ)
          maxZ = z;
      }
      return minZ != double.MaxValue && maxZ != double.MinValue;
    }

    private void GetLongestExtentAxis(Extents3d extents, out Vector3d longestAxis, out string longestAxisName, out double dx, out double dy, out double dz)
    {
      dx = Math.Abs(extents.MaxPoint.X - extents.MinPoint.X);
      dy = Math.Abs(extents.MaxPoint.Y - extents.MinPoint.Y);
      dz = Math.Abs(extents.MaxPoint.Z - extents.MinPoint.Z);
      longestAxis = Vector3d.XAxis;
      longestAxisName = "X";
      if (dy > dx && dy >= dz)
      {
        longestAxis = Vector3d.YAxis;
        longestAxisName = "Y";
      }
      else if (dz > dx && dz > dy)
      {
        longestAxis = Vector3d.ZAxis;
        longestAxisName = "Z";
      }
    }

    private bool TryFindSafeUpCandidate(Vector3d viewDir, List<Vector3d> pipeAxes, out Vector3d safeUp, out double maxUpDotP)
    {
      safeUp = Vector3d.ZAxis;
      maxUpDotP = 0.0;
      if (viewDir.Length <= 1e-9)
        return false;

      Vector3d v = viewDir.GetNormal();
      List<Vector3d> upCandidates = new List<Vector3d>();
      Vector3d gravityUp = this.RemoveAxisComponent(Vector3d.ZAxis, v);
      if (gravityUp.Length > 1e-9)
        upCandidates.Add(gravityUp.GetNormal());

      Vector3d[] axes = new Vector3d[]
      {
        Vector3d.XAxis, Vector3d.XAxis.Negate(),
        Vector3d.YAxis, Vector3d.YAxis.Negate(),
        Vector3d.ZAxis, Vector3d.ZAxis.Negate()
      };
      foreach (Vector3d axis in axes)
      {
        if (axis.Length <= 1e-9)
          continue;

        Vector3d normal = axis.GetNormal();
        if (Math.Abs(normal.DotProduct(v)) < 0.1)
          upCandidates.Add(normal);
      }

      double bestDot = double.MaxValue;
      bool found = false;
      foreach (Vector3d candidate in upCandidates)
      {
        double dot = this.MaxDotAgainstP(candidate, pipeAxes);
        if (dot < bestDot)
        {
          bestDot = dot;
          safeUp = candidate;
        }
        if (dot < 0.1)
        {
          maxUpDotP = dot;
          return true;
        }
        found = true;
      }

      maxUpDotP = found ? bestDot : 0.0;
      return false;
    }

    private Vector3d RemoveAxisComponent(Vector3d source, Vector3d axis)
    {
      Vector3d normal = axis.Length > 1e-9 ? axis.GetNormal() : Vector3d.XAxis;
      double dot = source.DotProduct(normal);
      return new Vector3d(source.X - normal.X * dot, source.Y - normal.Y * dot, source.Z - normal.Z * dot);
    }

    private double MaxDotAgainstP(Vector3d candidate, List<Vector3d> pipeAxes)
    {
      if (candidate.Length <= 1e-9 || pipeAxes == null || pipeAxes.Count == 0)
        return 0.0;

      Vector3d normal = candidate.GetNormal();
      double maxDot = 0.0;
      foreach (Vector3d pipeAxis in pipeAxes)
      {
        if (pipeAxis.Length <= 1e-9)
          continue;
        double dot = Math.Abs(normal.DotProduct(pipeAxis.GetNormal()));
        if (dot > maxDot)
          maxDot = dot;
      }
      return maxDot;
    }

    private string FormatPipeSet(List<SafeViewPipeInfo> pipes, bool spurious)
    {
      if (pipes == null || pipes.Count == 0)
        return "";

      var builder = new System.Text.StringBuilder();
      foreach (SafeViewPipeInfo pipe in pipes)
      {
        if (pipe.Spurious != spurious)
          continue;
        builder.Append(pipe.AxisLabel + ":z=" + pipe.MinZ.ToString("0.###") + ".." + pipe.MaxZ.ToString("0.###")
          + ":conn=" + (pipe.ConnMatched ? "T" : "F") + " ");
      }
      return builder.ToString().Trim();
    }

    private string FormatAxisForLog(Vector3d axis)
    {
      if (axis.Length <= 1e-9)
        return "(0,0,0)";

      Vector3d n = axis.GetNormal();
      return "(" + n.X.ToString("0.###") + "," + n.Y.ToString("0.###") + "," + n.Z.ToString("0.###") + ")";
    }


    private bool ComputeMainViewBasis(Vector3d rawAxis, out Vector3d axisUp, out Vector3d mainViewDir)
    {
      axisUp = Vector3d.ZAxis;
      mainViewDir = Vector3d.XAxis;
      if (rawAxis.Length <= 1e-9)
        return false;

      Vector3d axis = rawAxis.GetNormal();
      Vector3d preferredUp = Math.Abs(axis.DotProduct(Vector3d.ZAxis)) > 0.9 ? Vector3d.YAxis : Vector3d.ZAxis;
      Vector3d rawViewDir = axis.CrossProduct(preferredUp);
      if (rawViewDir.Length < 1e-6)
      {
        preferredUp = Math.Abs(axis.DotProduct(Vector3d.XAxis)) < 0.9 ? Vector3d.XAxis : Vector3d.YAxis;
        rawViewDir = axis.CrossProduct(preferredUp);
      }
      if (rawViewDir.Length < 1e-6)
        return false;

      mainViewDir = rawViewDir.GetNormal();
      axisUp = GetTwistNeutralUp(mainViewDir, mainViewDir.CrossProduct(axis).GetNormal());
      return true;
    }

    private static bool IsStandardViewDirection(Vector3d viewDir)
    {
      if (viewDir.Length <= 1e-9)
        return false;

      Vector3d normal = viewDir.GetNormal();
      return Math.Abs(Math.Abs(normal.DotProduct(Vector3d.XAxis)) - 1.0) < 1e-6
        || Math.Abs(Math.Abs(normal.DotProduct(Vector3d.YAxis)) - 1.0) < 1e-6
        || Math.Abs(Math.Abs(normal.DotProduct(Vector3d.ZAxis)) - 1.0) < 1e-6;
    }

    private static Vector3d GetTwistNeutralUp(Vector3d viewDir, Vector3d fallbackUp)
    {
      Vector3d up = fallbackUp.Length > 1e-9 ? fallbackUp.GetNormal() : Vector3d.ZAxis;
      PlantOrthoView.FileDiag("MAIN_VIEW_UP_PHASE1 viewDir=" + viewDir + " up=" + up);
      return up;
    }

    private bool TryGetPipeAxis(Part part, out Vector3d axis, out double length, out string method, out int portCount, out string portDump)
    {
      axis = Vector3d.ZAxis;
      length = 0.0;
      method = "direction";
      portCount = 0;
      portDump = "";
      if (part == null)
        return false;

      PortCollection ports = part.GetPorts((PortType) 7);
      portCount = ports == null ? 0 : ports.Count;
      List<Point3d> points = new List<Point3d>();
      Vector3d firstDirection = Vector3d.ZAxis;
      var dump = new System.Text.StringBuilder();
      int index = 0;

      if (ports != null)
      {
        foreach (Port port in (PnP3dCollection) ports)
        {
          dump.Append(port.Name + ":" + port.Position + ":" + port.Direction + " ");
          points.Add(port.Position);
          if (index == 0)
            firstDirection = port.Direction;
          index++;
        }
      }
      portDump = dump.ToString().Trim();

      Point3d firstPoint = Point3d.Origin;
      Point3d secondPoint = Point3d.Origin;
      double maxDistance = -1.0;
      for (int i = 0; i < points.Count; i++)
      {
        for (int j = i + 1; j < points.Count; j++)
        {
          double distance = points[i].DistanceTo(points[j]);
          if (distance > maxDistance)
          {
            maxDistance = distance;
            firstPoint = points[i];
            secondPoint = points[j];
          }
        }
      }

      if (maxDistance > 1e-6)
      {
        axis = (secondPoint - firstPoint).GetNormal();
        length = maxDistance;
        method = "ports";
        return true;
      }

      if (firstDirection.Length > 1e-9)
      {
        axis = firstDirection.GetNormal();
        length = 0.0;
        method = "direction";
        return true;
      }

      return false;
    }

    private Point3d GetPartNearestPoint(Part part, Point3d fallback)
    {
      if (part == null)
        return fallback;
      try
      {
        return this.GetNearestPortPoint(part.GetPorts((PortType) 7), fallback);
      }
      catch (Exception ex)
      {
        string partPointError = "GetPartNearestPoint 실패: " + ex.Message;
        DiagLog(partPointError);
        PlantOrthoView.FileDiag(partPointError);
        return fallback;
      }
    }

    private Point3d GetNearestPortPoint(PortCollection ports, Point3d fallback)
    {
      if (ports == null || ports.Count == 0)
        return fallback;

      Point3d nearest = fallback;
      double nearestDistance = double.MaxValue;
      foreach (Port port in (PnP3dCollection) ports)
      {
        double distance = port.Position.DistanceTo(fallback);
        if (distance < nearestDistance)
        {
          nearestDistance = distance;
          nearest = port.Position;
        }
      }
      return nearest;
    }

    private void Export2DCore(out string code)
    {
      code = null;
      if (this.lvSupportName.Items.Count == 0)
      {
        code = "ER1004";
        return;
      }
      if (!ProjectDataUtils.GridSystemExists())
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

