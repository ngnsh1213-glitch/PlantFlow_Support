using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Ribbon;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Autodesk.ProcessPower.Common;
using Autodesk.ProcessPower.DataLinks;
using Autodesk.ProcessPower.Drawings2d;
using Autodesk.ProcessPower.P3dProjectParts;
using Autodesk.ProcessPower.P3dUI;
using Autodesk.ProcessPower.PlantInstance;
using Autodesk.ProcessPower.PnIDGUIUtil;
using Autodesk.ProcessPower.PnP3dOrthoDrawingsUI;
using Autodesk.ProcessPower.PnPCommonUiUtils;
using Autodesk.ProcessPower.ProjectManager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

#nullable disable
namespace PlantFlow_Support
{
  public class UIUtils
  {
    public static string s_DefaultScaleName = "1:4";
    public const double VerySmallDouble = 1E-07;
    private static bool s_isRefModelsSaving = false;
    private static bool bAMESysVarChanged = false;
    public static string s_TopView = "Top";
    public static string s_BottomView = "Bottom";
    public static string s_FrontView = "Front";
    public static string s_BackView = "Back";
    public static string s_LeftView = "Left";
    public static string s_RightView = "Right";
    public static string s_NWView = "NW";
    public static string s_NEView = "NE";
    public static string s_SWView = "SW";
    public static string s_SEView = "SE";
    public static string s_CurrentView = "Current";
    public const string OrthoPartName = "Ortho";

    public static bool IsDocActiveDuringRefModelsSave() => UIUtils.s_isRefModelsSaving;

    public static ModelFile[] GetResolvedRefModels(IDwgViewDef viewDef)
    {
      ArrayList arrayList = new ArrayList();
      if (!UIUtils.DownloadReferenceDwgsToLocalWorkspace(viewDef.DwgQuery.ModelFiles))
        return (ModelFile[]) arrayList.ToArray(typeof (ModelFile));
      foreach (ModelFile modelFile in viewDef.DwgQuery.ModelFiles)
      {
        if (!modelFile.IsReferencedDwg)
        {
          string newGuid;
          string referenceModelPath = UIUtils.GetResolvedReferenceModelPath(modelFile.ModelFileName, modelFile.ModelFileGuid, out newGuid);
          if (!string.IsNullOrEmpty(referenceModelPath))
          {
            if (UIUtils.DownloadReferenceDwgToLocalWorkspace(referenceModelPath))
            {
              modelFile.ModelFileName = referenceModelPath;
              if (!string.IsNullOrEmpty(newGuid))
                modelFile.ModelFileGuid = newGuid;
              arrayList.Add((object) modelFile);
            }
            else
              break;
          }
        }
      }
      return (ModelFile[]) arrayList.ToArray(typeof (ModelFile));
    }

    private static string GetResolvedReferenceModelPath(string dwgPath, string dwgGuid)
    {
      return UIUtils.GetResolvedReferenceModelPath(dwgPath, dwgGuid, out string _);
    }

    private static string GetResolvedReferenceModelPath(
      string dwgPath,
      string dwgGuid,
      out string newGuid)
    {
      PnPProjectDrawing referenceModel = UIUtils.GetReferenceModel(dwgPath, dwgGuid);
      newGuid = (string) null;
      if (referenceModel != null)
      {
        newGuid = ((PnPProjectItem) referenceModel).Guid;
        Project projectPart = PlantApplication.CurrentProject.ProjectParts["Piping"];
        if (projectPart != null)
          return projectPart.GetLocalResolvedFilePath((PnPProjectFile) referenceModel);
      }
      return string.Empty;
    }

    private static PnPProjectDrawing GetReferenceModel(string dwgPath, string dwgGuid)
    {
      Project projectPart = PlantApplication.CurrentProject.ProjectParts["Piping"];
      PnPProjectDrawing referenceModel = (PnPProjectDrawing) null;
      if (!string.IsNullOrEmpty(dwgGuid))
        referenceModel = projectPart.GetDwgFile(dwgGuid);
      if (referenceModel != null)
        return referenceModel;
      foreach (PnPProjectDrawing pnPdrawingFile in projectPart.GetPnPDrawingFiles())
      {
        string absoluteFileName = ((PnPProjectFile) pnPdrawingFile).AbsoluteFileName;
        if (string.Compare(dwgPath, absoluteFileName, true) == 0 || string.Compare(dwgGuid, ((PnPProjectItem) pnPdrawingFile).Guid, true) == 0)
        {
          referenceModel = pnPdrawingFile;
          break;
        }
      }
      if (referenceModel != null)
        return referenceModel;
      if (referenceModel == null)
      {
        try
        {
          string projectDwgDirectory = projectPart.ProjectDwgDirectory;
          string projectDirectory = projectPart.ProjectDirectory;
          string str1 = projectDwgDirectory.Substring(projectDirectory.Length);
          if (dwgPath.Trim().ToLower().Contains(str1.Trim().ToLower()))
          {
            string[] separator = new string[1]
            {
              str1.Trim().ToLower()
            };
            string[] strArray = dwgPath.Trim().ToLower().Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length > 1)
            {
              if (!string.IsNullOrEmpty(strArray[1]))
              {
                string str2 = strArray[1].TrimStart();
                string path = projectDwgDirectory + str2.TrimEnd();
                if (File.Exists(path))
                  referenceModel = projectPart.GetDwgFileByName(path);
              }
            }
          }
        }
        catch
        {
        }
      }
      return referenceModel;
    }

    public static bool DoesViewNeedUpdate(
      IDwgSheet dwgSheet,
      IDwgViewDef viewDef,
      out bool modelExist,
      out bool bModelChanged,
      out bool bViewChanged)
    {
      modelExist = true;
      bModelChanged = false;
      bViewChanged = false;
      try
      {
        DateTime dateTime = Convert.ToDateTime(viewDef.ViewCreationTime);
        foreach (ModelFile modelFile in viewDef.DwgQuery.ModelFiles)
        {
          if (!modelFile.IsReferencedDwg)
          {
            string referenceModelPath = UIUtils.GetResolvedReferenceModelPath(modelFile.ModelFileName, modelFile.ModelFileGuid);
            if (!File.Exists(referenceModelPath))
            {
              modelExist = false;
              return true;
            }
            if (DateTime.Compare(File.GetLastWriteTimeUtc(referenceModelPath), dateTime) > 0)
            {
              bModelChanged = true;
              return true;
            }
          }
        }
        if (dwgSheet != null)
        {
          IDwgView view = dwgSheet.GetView(viewDef.ViewName);
          if (view != null)
          {
            if (view.ViewNeedsUpdate())
            {
              bViewChanged = true;
              return true;
            }
          }
        }
      }
      catch
      {
      }
      return false;
    }

    public static string GetMissingModels(IDwgViewDef viewDef)
    {
      string missingModels = string.Empty;
      try
      {
        foreach (ModelFile modelFile in viewDef.DwgQuery.ModelFiles)
        {
          if (!modelFile.IsReferencedDwg)
          {
            string referenceModelPath = UIUtils.GetResolvedReferenceModelPath(modelFile.ModelFileName, modelFile.ModelFileGuid);
            if (string.IsNullOrEmpty(referenceModelPath))
              missingModels = missingModels + modelFile.ModelFileName + "\n";
            else if (!File.Exists(referenceModelPath))
              missingModels = missingModels + referenceModelPath + "\n";
          }
        }
      }
      catch
      {
      }
      return missingModels;
    }

    public static string GetMissingRefModels(IDwgViewDef viewDef, ref int numMissingModels)
    {
      string missingRefModels = string.Empty;
      numMissingModels = 0;
      try
      {
        foreach (ModelFile modelFile in viewDef.DwgQuery.ModelFiles)
        {
          if (!modelFile.IsReferencedDwg)
          {
            string referenceModelPath = UIUtils.GetResolvedReferenceModelPath(modelFile.ModelFileName, modelFile.ModelFileGuid);
            if (!File.Exists(referenceModelPath))
            {
              missingRefModels = missingRefModels + referenceModelPath + "\n";
              ++numMissingModels;
            }
          }
        }
      }
      catch
      {
      }
      return missingRefModels;
    }

    public static void SetRibbonPaletteSet(bool status)
    {
      RibbonPaletteSet ribbonPaletteSet = RibbonServices.RibbonPaletteSet;
      if (ribbonPaletteSet == null)
      {
        if (!status)
          return;
        try
        {
          RibbonServices.CreateRibbonPaletteSet();
          if (RibbonServices.RibbonPaletteSet == null)
            return;
          ((Window) RibbonServices.RibbonPaletteSet).Visible = true;
        }
        catch
        {
        }
      }
      else
      {
        if (((Window) ribbonPaletteSet).Visible == status)
          return;
        ((Window) ribbonPaletteSet).Visible = status;
      }
    }

    public static void SetOrthoEditing(short state)
    {
      try
      {
        SystemObjects.Variables["ORTHOEDITOR"].Value = (object) state;
        UIUtils.SetRibbonPaletteSet(true);
      }
      catch
      {
      }
    }

    public static bool CreateDwgSheet(Database dwgDatabase, out IDwgSheet dwgSheet)
    {
      dwgSheet = (IDwgSheet) null;
      bool dwgSheet1 = false;
      if (((DisposableWrapper) dwgDatabase) == ((DisposableWrapper) null))
        return dwgSheet1;
      dwgSheet = Dwg2dDbx.NewSheet(dwgDatabase);
      return true;
    }

    public static bool CreateDwgViewDef(
      IDwgSheetDef dwgSheetDef,
      string strViewName,
      IDwgSheetDefs sheetDefs,
      double boxSize,
      Vector3d viewDir,
      Vector3d upVector,
      Point3d ptIns,
      double viewportWidth,
      double viewportHeight,
      out IDwgViewDef dwgViewDef)
    {
      dwgViewDef = (IDwgViewDef) null;
      bool dwgViewDef1 = false;
      if (dwgSheetDef == null)
        return dwgViewDef1;
      IDwgViewDef.ViewParams viewParams;
      upVector = UIUtils.NormalizeMainUpVector(strViewName, viewDir, upVector);
      UIUtils.CreateViewParams(viewDir, upVector, boxSize, ptIns, viewportWidth, viewportHeight, out viewParams);
      dwgViewDef = Dwg2dDef.NewViewDef(sheetDefs, strViewName, ref viewParams);
      return true;
    }

    private static Vector3d NormalizeMainUpVector(string strViewName, Vector3d viewDir, Vector3d upVector)
    {
      if (!string.Equals(strViewName, "Main", StringComparison.OrdinalIgnoreCase))
        return upVector;

      PlantOrthoView.FileDiag("MAIN_UP_PASSTHROUGH viewDir=" + viewDir + " up=" + upVector);
      return upVector;
    }

    public static void CreateViewParams(
      Vector3d viewDir,
      Vector3d upVector,
      double boxSize,
      Point3d ptIns,
      double viewportWidth,
      double viewportHeight,
      out IDwgViewDef.ViewParams viewParams)
    {
      viewParams = new IDwgViewDef.ViewParams();
      viewParams.m_eyePoint = (Point3d.Origin) - ((boxSize) * (viewDir));
      viewParams.m_viewDirection = (boxSize) * (viewDir);
      viewParams.m_upVector = upVector;
      viewParams.m_frontClip = boxSize / 2.0;
      viewParams.m_backClip = viewParams.m_frontClip + boxSize;
      viewParams.m_width = viewportWidth;
      viewParams.m_height = viewportHeight;
      viewParams.m_centerPoint = ptIns;
    }

    public static bool CreateDwgView(
      IDwgSheet dwgSheet,
      string strViewName,
      ObjectId viewportId,
      out IDwgView dwgView)
    {
      dwgView = (IDwgView) null;
      bool dwgView1 = false;
      if (string.IsNullOrEmpty(strViewName) || dwgSheet == null)
        return dwgView1;
      dwgView = dwgSheet.NewView(strViewName, viewportId);
      return dwgView != null;
    }

    public static bool CreateDwgQueryByBox(
      IDwgViewDef dwgViewDef,
      IDwgSheetDefs sheetDefs,
      Point3d lowerLeftPt,
      Point3d upperRightPt,
      out IDwgQueryByBox dwgQueryByBox)
    {
      dwgQueryByBox = (IDwgQueryByBox) null;
      bool dwgQueryByBox1 = false;
      if (dwgViewDef == null || sheetDefs == null)
        return dwgQueryByBox1;
      double num1 = Math.Abs(lowerLeftPt.X - upperRightPt.X);
      double num2 = Math.Abs(lowerLeftPt.Y - upperRightPt.Y);
      double num3 = Math.Abs(lowerLeftPt.Z - upperRightPt.Z);
      if (num3 <= 0.0)
        num3 = 2000.0;
      Point3d point3d = new Point3d(lowerLeftPt.X, lowerLeftPt.Y, lowerLeftPt.Z);
      Vector3d vector3d1 = new Vector3d(num1, 0.0, 0.0);
      Vector3d vector3d2 = new Vector3d(0.0, num2, 0.0);
      uint num4 = 1;
      dwgViewDef.DwgQuery = (IDwgQuery) Dwg2dDef.NewQueryByBox(sheetDefs, ref point3d, ref vector3d1, ref vector3d2, num3, num4);
      dwgQueryByBox = (IDwgQueryByBox) dwgViewDef.DwgQuery;
      return true;
    }

    public static double GetClipInlateBy()
    {
      using (UISettings uiSettings = new UISettings())
        return (int)uiSettings.GetProjectUnit() == 1 ? 0.001 : 4E-05;
    }

    public static int ProxyShow()
    {
      try
      {
        return (int) (short) Application.GetSystemVariable("PROXYSHOW");
      }
      catch
      {
        return 1;
      }
    }

    public static void CreateOrUpdateOrthoView(
      IDwgSheetDef dwgSheetDef,
      string viewName,
      Project projectPart,
      bool skipClipping,
      bool bOrthoUpdateCmd,
      IDwgSetClipper clipper,
      double viewportRotation,
      StringCollection frozenLayers,
      StringCollection offLayers,
      Vector3d offset,
      double rotation)
    {
      if (dwgSheetDef == null || projectPart == null)
        return;
      
      PlantFlow_Support.PSUtil.Log("[UIUtils] Setting AMESysVariable...");
      UIUtils.SetAMESysVariable();
      
      PlantFlow_Support.PSUtil.Log("[UIUtils] Calling Dwg2dGen.NewDwgGenerator...");
      IDwgGenerator idwgGenerator = Dwg2dGen.NewDwgGenerator(projectPart, dwgSheetDef, viewName, skipClipping, bOrthoUpdateCmd, clipper, viewportRotation, frozenLayers, offLayers, offset, rotation);
      IDwgGenerator.IsViewPortHeightUpdated = false;
      IDwgGenerator.IsViewPortWidthUpdated = false;
      
      PlantFlow_Support.PSUtil.Log("[UIUtils] Calling idwgGenerator.UpdateDrawingSheet (CRITICAL)...");
      System.Windows.Forms.Application.DoEvents(); 
      
      idwgGenerator.UpdateDrawingSheet(null);
      
      PlantFlow_Support.PSUtil.Log("[UIUtils] UpdateDrawingSheet Returned.");
      UIUtils.ResetAMESysVariable();
    }

    public static PlantFlow_Support.Commands.ViewType GetViewTypeFromName(string viewName)
    {
      PlantFlow_Support.Commands.ViewType viewTypeFromName = (PlantFlow_Support.Commands.ViewType) 0;
      if (string.IsNullOrEmpty(viewName))
        return viewTypeFromName;
      try
      {
        if (string.Compare(viewName, UIUtils.s_TopView, true) == 0 || string.Compare(LocalResources.IDS_PlantOrthoView_EnterViewTypeName_Top, viewName, true) == 0)
          viewTypeFromName = (PlantFlow_Support.Commands.ViewType) 1;
        else if (string.Compare(viewName, UIUtils.s_BottomView, true) == 0 || string.Compare(LocalResources.IDS_PlantOrthoView_EnterViewTypeName_Bottom, viewName, true) == 0)
          viewTypeFromName = (PlantFlow_Support.Commands.ViewType) 2;
        else if (string.Compare(viewName, UIUtils.s_FrontView, true) == 0 || string.Compare(LocalResources.IDS_PlantOrthoView_EnterViewTypeName_Front, viewName, true) == 0)
          viewTypeFromName = (PlantFlow_Support.Commands.ViewType) 3;
        else if (string.Compare(viewName, UIUtils.s_BackView, true) == 0 || string.Compare(LocalResources.IDS_PlantOrthoView_EnterViewTypeName_Back, viewName, true) == 0)
          viewTypeFromName = (PlantFlow_Support.Commands.ViewType) 4;
        else if (string.Compare(viewName, UIUtils.s_LeftView, true) == 0 || string.Compare(LocalResources.IDS_PlantOrthoView_EnterViewTypeName_Left, viewName, true) == 0)
          viewTypeFromName = (PlantFlow_Support.Commands.ViewType) 5;
        else if (string.Compare(viewName, UIUtils.s_RightView, true) == 0 || string.Compare(LocalResources.IDS_PlantOrthoView_EnterViewTypeName_Right, viewName, true) == 0)
          viewTypeFromName = (PlantFlow_Support.Commands.ViewType) 6;
        else if (string.Compare(viewName, UIUtils.s_NWView, true) == 0 || string.Compare(LocalResources.IDS_PlantOrthoView_EnterViewTypeName_NW, viewName, true) == 0)
          viewTypeFromName = (PlantFlow_Support.Commands.ViewType) 7;
        else if (string.Compare(viewName, UIUtils.s_NEView, true) == 0 || string.Compare(LocalResources.IDS_PlantOrthoView_EnterViewTypeName_NE, viewName, true) == 0)
          viewTypeFromName = (PlantFlow_Support.Commands.ViewType) 8;
        else if (string.Compare(viewName, UIUtils.s_SEView, true) == 0 || string.Compare(LocalResources.IDS_PlantOrthoView_EnterViewTypeName_SE, viewName, true) == 0)
          viewTypeFromName = (PlantFlow_Support.Commands.ViewType) 10;
        else if (string.Compare(viewName, UIUtils.s_SWView, true) == 0 || string.Compare(LocalResources.IDS_PlantOrthoView_EnterViewTypeName_SW, viewName, true) == 0)
        {
          viewTypeFromName = (PlantFlow_Support.Commands.ViewType) 9;
        }
        else
        {
          if (string.Compare(viewName, UIUtils.s_CurrentView, true) != 0)
          {
            if (string.Compare(LocalResources.IDS_PlantOrthoView_EnterViewTypeName_Current, viewName, true) != 0)
              goto label_26;
          }
          viewTypeFromName = (PlantFlow_Support.Commands.ViewType) 0;
        }
      }
      catch
      {
      }
label_26:
      return viewTypeFromName;
    }

    public static string GetDisplayViewName(PlantFlow_Support.Commands.ViewType viewType)
    {
      string empty = string.Empty;
      string displayViewName;
      switch ((int)viewType - 1)
      {
        case 0:
          displayViewName = LocalResources.IDS_PlantOrthoView_EnterViewTypeName_Top;
          break;
        case 1:
          displayViewName = LocalResources.IDS_PlantOrthoView_EnterViewTypeName_Bottom;
          break;
        case 2:
          displayViewName = LocalResources.IDS_PlantOrthoView_EnterViewTypeName_Front;
          break;
        case 3:
          displayViewName = LocalResources.IDS_PlantOrthoView_EnterViewTypeName_Back;
          break;
        case 4:
          displayViewName = LocalResources.IDS_PlantOrthoView_EnterViewTypeName_Left;
          break;
        case 5:
          displayViewName = LocalResources.IDS_PlantOrthoView_EnterViewTypeName_Right;
          break;
        case 6:
          displayViewName = LocalResources.IDS_PlantOrthoView_EnterViewTypeName_NW;
          break;
        case 7:
          displayViewName = LocalResources.IDS_PlantOrthoView_EnterViewTypeName_NE;
          break;
        case 8:
          displayViewName = LocalResources.IDS_PlantOrthoView_EnterViewTypeName_SW;
          break;
        case 9:
          displayViewName = LocalResources.IDS_PlantOrthoView_EnterViewTypeName_SE;
          break;
        default:
          displayViewName = LocalResources.IDS_PlantOrthoView_EnterViewTypeName_Current;
          break;
      }
      return displayViewName;
    }

    public static string GetDefaultViewName(PlantFlow_Support.Commands.ViewType viewType)
    {
      string empty = string.Empty;
      string defaultViewName;
      switch ((int)viewType - 1)
      {
        case 0:
          defaultViewName = LocalResources.IDS_PlantOrthoView_DefaultViewName_Top;
          break;
        case 1:
          defaultViewName = LocalResources.IDS_PlantOrthoView_DefaultViewName_Bottom;
          break;
        case 2:
          defaultViewName = LocalResources.IDS_PlantOrthoView_DefaultViewName_Front;
          break;
        case 3:
          defaultViewName = LocalResources.IDS_PlantOrthoView_DefaultViewName_Back;
          break;
        case 4:
          defaultViewName = LocalResources.IDS_PlantOrthoView_DefaultViewName_Left;
          break;
        case 5:
          defaultViewName = LocalResources.IDS_PlantOrthoView_DefaultViewName_Right;
          break;
        case 6:
          defaultViewName = LocalResources.IDS_PlantOrthoView_DefaultViewName_NW;
          break;
        case 7:
          defaultViewName = LocalResources.IDS_PlantOrthoView_DefaultViewName_NE;
          break;
        case 8:
          defaultViewName = LocalResources.IDS_PlantOrthoView_DefaultViewName_SW;
          break;
        case 9:
          defaultViewName = LocalResources.IDS_PlantOrthoView_DefaultViewName_SE;
          break;
        default:
          defaultViewName = LocalResources.IDS_PlantOrthoView_DefaultViewName_Current;
          break;
      }
      return defaultViewName;
    }

    public static string GetGlobalViewName(PlantFlow_Support.Commands.ViewType viewType)
    {
      string empty = string.Empty;
      string globalViewName;
      switch ((int)viewType - 1)
      {
        case 0:
          globalViewName = UIUtils.s_TopView;
          break;
        case 1:
          globalViewName = UIUtils.s_BottomView;
          break;
        case 2:
          globalViewName = UIUtils.s_FrontView;
          break;
        case 3:
          globalViewName = UIUtils.s_BackView;
          break;
        case 4:
          globalViewName = UIUtils.s_LeftView;
          break;
        case 5:
          globalViewName = UIUtils.s_RightView;
          break;
        case 6:
          globalViewName = UIUtils.s_NWView;
          break;
        case 7:
          globalViewName = UIUtils.s_NEView;
          break;
        case 8:
          globalViewName = UIUtils.s_SWView;
          break;
        case 9:
          globalViewName = UIUtils.s_SEView;
          break;
        default:
          globalViewName = UIUtils.s_CurrentView;
          break;
      }
      return globalViewName;
    }

    public static string GetDisplayViewName(string viewName)
    {
      return UIUtils.GetDisplayViewName(UIUtils.GetViewTypeFromName(viewName));
    }

    public static string GetGlobalViewName(string viewName)
    {
      return UIUtils.GetGlobalViewName(UIUtils.GetViewTypeFromName(viewName));
    }

    public static bool GetViewVectors(
      PlantFlow_Support.Commands.ViewType viewType,
      Matrix3d trans,
      out Vector3d viewDirection,
      out Vector3d upVector)
    {
      viewDirection = Vector3d.ZAxis;
      upVector = -(Vector3d.YAxis);
      switch ((int)viewType - 1)
      {
        case 0:
          viewDirection = Vector3d.ZAxis;
          upVector = -(Vector3d.YAxis);
          goto label_15;
        case 1:
          viewDirection = -(Vector3d.ZAxis);
          upVector = Vector3d.YAxis;
          goto label_15;
        case 2:
          viewDirection = -(Vector3d.YAxis);
          upVector = -(Vector3d.ZAxis);
          goto label_15;
        case 3:
          viewDirection = Vector3d.YAxis;
          upVector = -(Vector3d.ZAxis);
          goto label_15;
        case 4:
          viewDirection = -(Vector3d.XAxis);
          upVector = -(Vector3d.ZAxis);
          goto label_15;
        case 5:
          viewDirection = Vector3d.XAxis;
          upVector = -(Vector3d.ZAxis);
          goto label_15;
        case 6:
          viewDirection = new Vector3d(-1.0, 1.0, 1.0);
          upVector = -(Vector3d.ZAxis);
          break;
        case 7:
          viewDirection = new Vector3d(1.0, 1.0, 1.0);
          upVector = -(Vector3d.ZAxis);
          break;
        case 8:
          viewDirection = new Vector3d(-1.0, -1.0, 1.0);
          upVector = -(Vector3d.ZAxis);
          break;
        case 9:
          viewDirection = new Vector3d(1.0, -1.0, 1.0);
          upVector = -(Vector3d.ZAxis);
          break;
        default:
          if (!CommonUIUtils.accppGetViewDir(out viewDirection, out upVector))
            return false;
          upVector = -(upVector);
          break;
      }
      Vector3d vector3d1 = viewDirection.CrossProduct(upVector);
      upVector = vector3d1.CrossProduct(viewDirection);
      viewDirection = viewDirection.GetNormal();
      upVector = upVector.GetNormal();
label_15:
      if (viewType != PlantFlow_Support.Commands.ViewType.Unknown)
      {
        Vector3d vector3d2 = viewDirection.TransformBy(trans);
        viewDirection = vector3d2.GetNormal();
        Vector3d vector3d3 = upVector.TransformBy(trans);
        upVector = vector3d3.GetNormal();
      }
      return true;
    }

    public static string GetDwgGuidfromDwgName(string dwgName)
    {
      string dwgGuidfromDwgName = string.Empty;
      foreach (PnPProjectDrawing pnPdrawingFile in PlantApplication.CurrentProject.ProjectParts["Piping"].GetPnPDrawingFiles())
      {
        string absoluteFileName = ((PnPProjectFile) pnPdrawingFile).AbsoluteFileName;
        if (string.Compare(dwgName, absoluteFileName, true) == 0 || string.Compare(dwgName, ((PnPProjectFile) pnPdrawingFile).ResolvedFilePath, true) == 0)
        {
          dwgGuidfromDwgName = ((PnPProjectItem) pnPdrawingFile).Guid;
          break;
        }
      }
      return dwgGuidfromDwgName;
    }

    public static StringCollection GetUnLoadedDwgFiles(Database modelDb)
    {
      StringCollection unLoadedDwgFiles = new StringCollection();
      XrefGraph hostDwgXrefGraph = modelDb.GetHostDwgXrefGraph(false);
      for (int index = 0; index < ((Graph) hostDwgXrefGraph).NumNodes; ++index)
      {
        ObjectId blockTableRecordId = hostDwgXrefGraph.GetXrefNode(index).BlockTableRecordId;
        if (!blockTableRecordId.IsNull)
        {
          using (OpenCloseTransaction closeTransaction = new OpenCloseTransaction())
          {
            try
            {
              BlockTableRecord blockTableRecord = (BlockTableRecord) ((Transaction) closeTransaction).GetObject(blockTableRecordId, (OpenMode) 0, false);
              if (((DisposableWrapper) blockTableRecord) != ((DisposableWrapper) null))
              {
                if (blockTableRecord.PathName != null)
                {
                  if (blockTableRecord.PathName.Length > 0)
                  {
                    string lower = blockTableRecord.PathName.Trim().ToLower();
                    XrefStatus xrefStatus = blockTableRecord.XrefStatus;
                    if ((int)xrefStatus != 2)
                    {
                      if ((int)xrefStatus != 4)
                        continue;
                    }
                    unLoadedDwgFiles.Add(lower);
                  }
                }
              }
            }
            catch (Exception _ex)
            {
              System.Diagnostics.Debug.WriteLine("[PFS-DIAG] GetUnLoadedDwgFiles xref 검사 예외(삼킴 유지): " + _ex.Message);
            }
            finally
            {
              ((Transaction) closeTransaction).Commit();
            }
          }
        }
      }
      try
      {
          Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?
              .Editor.WriteMessage("\n[PFS-DIAG] UnLoadedDwgFiles count=" + unLoadedDwgFiles.Count
              + " : " + string.Join(", ", unLoadedDwgFiles.Cast<string>()));
      }
      catch (Exception _ex) { System.Diagnostics.Debug.WriteLine("[PFS-DIAG] " + _ex.Message); }
      return unLoadedDwgFiles;
    }

    public static bool DownloadReferenceDwgsToLocalWorkspace(StringCollection dwg3dFiles)
    {
      ArrayList modelFiles = new ArrayList();
      return UIUtils.UpdateFileCollectionWithXrefs(dwg3dFiles, ref modelFiles, out Extents3d _);
    }

    public static bool DownloadReferenceDwgsToLocalWorkspace(ModelFile[] modelFiles)
    {
      Project projectPart = PlantApplication.CurrentProject.ProjectParts["Piping"];
      List<PnPProjectFile> pnPprojectFileList = new List<PnPProjectFile>();
      foreach (ModelFile modelFile in modelFiles)
      {
        if (!modelFile.IsReferencedDwg)
        {
          PnPProjectDrawing referenceModel = UIUtils.GetReferenceModel(modelFile.ModelFileName, modelFile.ModelFileGuid);
          if (referenceModel != null)
            pnPprojectFileList.Add((PnPProjectFile) referenceModel);
        }
      }
      return pnPprojectFileList.Count == 0 || projectPart.ResolvedWorkspaceFiles(pnPprojectFileList.ToArray());
    }

    public static bool DownloadReferenceDwgToLocalWorkspace(string dwgFile)
    {
      Project projectPart = PlantApplication.CurrentProject.ProjectParts["Piping"];
      if (!FileExistsChecker.FileExists(dwgFile, 3000))
      {
        PnPProjectDrawing pnPprojectDrawing = projectPart.GetDwgFileByName(dwgFile) ?? projectPart.GetDwgByLocalResolvedPath(dwgFile) as PnPProjectDrawing;
        if (pnPprojectDrawing != null)
          return !string.IsNullOrEmpty(projectPart.GetResolvedWorkspaceFilePath((PnPProjectFile) pnPprojectDrawing, true));
      }
      return true;
    }

    public static bool UpdateFileCollectionWithXrefs(
      ModelFile[] refModels,
      ref ArrayList modelFiles,
      out Extents3d exts)
    {
      exts = new Extents3d();
      foreach (ModelFile refModel in refModels)
      {
        if (!refModel.IsReferencedDwg)
          UIUtils.UpdateFileCollectionWithXrefs(refModel.ModelFileName, refModel.ModelFileGuid, Matrix3d.Identity, false, ref modelFiles, (StringCollection) null, ref exts);
      }
      Point3d minPoint = exts.MinPoint;
      double x1 = minPoint.X;
      Point3d maxPoint = exts.MaxPoint;
      double x2 = maxPoint.X;
      if (x1 > x2)
        exts = new Extents3d(new Point3d(-0.5, -0.5, -0.5), new Point3d(0.5, 0.5, 0.5));
      return true;
    }

    public static bool UpdateFileCollectionWithXrefs(
      StringCollection dwg3dFiles,
      ref ArrayList modelFiles,
      out Extents3d exts)
    {
      exts = new Extents3d();
      Project projectPart = PlantApplication.CurrentProject.ProjectParts["Piping"];
      List<PnPProjectFile> pnPprojectFileList = new List<PnPProjectFile>();
      foreach (string dwg3dFile in dwg3dFiles)
      {
        PnPProjectDrawing pnPprojectDrawing = projectPart.GetDwgFileByName(dwg3dFile) ?? projectPart.GetDwgByLocalResolvedPath(dwg3dFile) as PnPProjectDrawing;
        if (pnPprojectDrawing != null)
          pnPprojectFileList.Add((PnPProjectFile) pnPprojectDrawing);
      }
      if (pnPprojectFileList.Count > 0 && !projectPart.ResolvedWorkspaceFiles(pnPprojectFileList.ToArray()))
        return false;
      foreach (string dwg3dFile in dwg3dFiles)
        UIUtils.UpdateFileCollectionWithXrefs(dwg3dFile, UIUtils.GetDwgGuidfromDwgName(dwg3dFile), Matrix3d.Identity, false, ref modelFiles, (StringCollection) null, ref exts);
      Point3d point3d = exts.MinPoint;
      double x1 = point3d.X;
      point3d = exts.MaxPoint;
      double x2 = point3d.X;
      if (x1 > x2)
        exts = new Extents3d(new Point3d(-0.5, -0.5, -0.5), new Point3d(0.5, 0.5, 0.5));
      return true;
    }

    public static void UpdateFileCollectionWithXrefs(
      string strFileName,
      string strDwgGuid,
      Matrix3d rootMatrix,
      bool bIsRefencedDwg,
      ref ArrayList modelFiles,
      StringCollection unLoadedFiles,
      ref Extents3d exts)
    {
      Project projectPart = PlantApplication.CurrentProject.ProjectParts["Piping"];
      ModelFile modelFile = new ModelFile(strFileName, rootMatrix, bIsRefencedDwg, strDwgGuid);
      if (modelFiles.Contains((object) modelFile) || !UIUtils.DownloadReferenceDwgToLocalWorkspace(strFileName))
        return;
      bool flag1 = false;
      Database dwgDatabase = PnPDwg2dUtil.GetDwgDatabase(strFileName, false, ref flag1);
      if (((DisposableWrapper) dwgDatabase) == ((DisposableWrapper) null))
        return;
      modelFiles.Add((object) modelFile);
      try
      {
          Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?
              .Editor.WriteMessage("\n[PFS-DIAG] UFC enter file='" + strFileName + "' isRef=" + bIsRefencedDwg
              + " modelFiles.Count(after root add)=" + modelFiles.Count);
      }
      catch (Exception _px) { System.Diagnostics.Debug.WriteLine("[PFS-DIAG] UFC enter 로그 예외: " + _px.Message); }
      if (unLoadedFiles == null)
        unLoadedFiles = UIUtils.GetUnLoadedDwgFiles(dwgDatabase);
      try
      {
        Extents3d extents3d = new Extents3d(dwgDatabase.Extmin, dwgDatabase.Extmax);
        extents3d.TransformBy(rootMatrix);
        exts.AddExtents(extents3d);
      }
      catch
      {
      }
      using (Transaction transaction = dwgDatabase.TransactionManager.StartTransaction())
      {
        BlockTable blockTable = (BlockTable) transaction.GetObject(dwgDatabase.BlockTableId, (OpenMode) 0, false);
        if (((DisposableWrapper) blockTable) != ((DisposableWrapper) null))
        {
          bool flag2 = false;
          foreach (ObjectId objectId in (SymbolTable) blockTable)
          {
            if (!flag2)
            {
              using (BlockTableRecord blockTableRecord = (BlockTableRecord) transaction.GetObject(objectId, (OpenMode) 0))
              {
                if (blockTableRecord.IsFromExternalReference)
                {
                  try
                  {
                      Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?
                          .Editor.WriteMessage("\n[PFS-DIAG] flag2-scan xrefBTR path='" + blockTableRecord.PathName
                          + "' overlay=" + blockTableRecord.IsFromOverlayReference
                          + " unloaded=" + blockTableRecord.IsUnloaded);
                  }
                  catch (Exception _px) { System.Diagnostics.Debug.WriteLine("[PFS-DIAG] flag2-scan 로그 예외: " + _px.Message); }
                  if (!blockTableRecord.IsFromOverlayReference)
                  {
                    if (!blockTableRecord.IsUnloaded)
                      flag2 = true;
                  }
                }
              }
            }
            else
              break;
          }
          try
          {
              Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?
                  .Editor.WriteMessage("\n[PFS-DIAG] UFC file='" + strFileName + "' flag2(ModelSpace 순회 진입)=" + flag2);
          }
          catch (Exception _px) { System.Diagnostics.Debug.WriteLine("[PFS-DIAG] flag2 결과 로그 예외: " + _px.Message); }
          if (flag2)
          {
            BlockTableRecord blockTableRecord1 = (BlockTableRecord) transaction.GetObject(((SymbolTable) blockTable)[BlockTableRecord.ModelSpace], (OpenMode) 0);
            foreach (ObjectId objectId in blockTableRecord1)
            {
              Entity entity = (Entity) transaction.GetObject(objectId, (OpenMode) 0);
              if (entity != null && entity is BlockReference && entity.Visible)
              {
                BlockReference blockReference = entity as BlockReference;
                BlockTableRecord blockTableRecord2;
                try
                {
                  blockTableRecord2 = (BlockTableRecord) transaction.GetObject(blockReference.BlockTableRecord, (OpenMode) 0);
                }
                catch
                {
                  blockTableRecord2 = (BlockTableRecord) null;
                }
                if (blockTableRecord2 != null)
                {
                  try
                  {
                      Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?
                          .Editor.WriteMessage("\n[PFS-DIAG] msref BTR path='" + blockTableRecord2.PathName
                          + "' isExtRef=" + blockTableRecord2.IsFromExternalReference
                          + " overlay=" + blockTableRecord2.IsFromOverlayReference
                          + " unloaded=" + blockTableRecord2.IsUnloaded
                          + " visible=" + (entity != null && entity.Visible));
                  }
                  catch (Exception _px) { System.Diagnostics.Debug.WriteLine("[PFS-DIAG] msref 로그 예외: " + _px.Message); }
                  if (blockTableRecord2.IsFromExternalReference && !blockTableRecord2.IsFromOverlayReference && !blockTableRecord2.IsUnloaded)
                  {
                    string pathName = blockTableRecord2.PathName;
                    if (!string.IsNullOrEmpty(pathName))
                    {
                      if (unLoadedFiles != null && unLoadedFiles.Count > 0)
                      {
                        string lower = pathName.Trim().ToLower();
                        if (unLoadedFiles.Contains(lower))
                        {
                          try
                          {
                              Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?
                                  .Editor.WriteMessage("\n[PFS-DIAG] SKIP(unLoadedFiles) path='" + lower + "'");
                          }
                          catch (Exception _px) { System.Diagnostics.Debug.WriteLine("[PFS-DIAG] SKIP 로그 예외: " + _px.Message); }
                          continue;
                        }
                      }
                      string empty = string.Empty;
                      string fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(strFileName), pathName));
                      string strFileName1 = UIUtils.GetResolvedReferenceModelPath(fullPath, (string) null);
                      if (string.IsNullOrEmpty(strFileName1))
                        strFileName1 = fullPath;
                      Matrix3d blockTransform = blockReference.BlockTransform;
                      Matrix3d rootMatrix1 = blockTransform.PreMultiplyBy(rootMatrix);
                      UIUtils.UpdateFileCollectionWithXrefs(strFileName1, string.Empty, rootMatrix1, true, ref modelFiles, unLoadedFiles, ref exts);
                    }
                  }
                  ((DisposableWrapper) blockTableRecord2).Dispose();
                }
              }
            }

            ((DisposableWrapper) blockTableRecord1).Dispose();
          }
        }
      }
      if (flag1)
        return;
      ((DisposableWrapper) dwgDatabase).Dispose();
    }

    public static bool GetExtents(string strModelName, out Extents3d exts)
    {
      exts = new Extents3d();
      bool flag1 = false;
      bool extents = false;
      if (string.IsNullOrEmpty(strModelName))
        return extents;
      try
      {
        bool flag2 = false;
        Database dwgDatabase = PnPDwg2dUtil.GetDwgDatabase(strModelName, false, ref flag2);
        if (((DisposableWrapper) null) != ((DisposableWrapper) dwgDatabase))
        {
          using (OpenCloseTransaction closeTransaction = new OpenCloseTransaction())
          {
            BlockTable blockTable = (BlockTable) ((Transaction) closeTransaction).GetObject(dwgDatabase.BlockTableId, (OpenMode) 0, false);
            foreach (ObjectId objectId in (BlockTableRecord) ((Transaction) closeTransaction).GetObject(((SymbolTable) blockTable)[BlockTableRecord.ModelSpace], (OpenMode) 0, false))
            {
              Entity entity = (Entity) ((Transaction) closeTransaction).GetObject(objectId, (OpenMode) 0);
              if (((DisposableWrapper) entity) != ((DisposableWrapper) null))
              {
                try
                {
                  exts.AddExtents(entity.GeometricExtents);
                  flag1 = true;
                }
                catch
                {
                }
              }
            }
            ((Transaction) closeTransaction).Commit();
          }
          if (!flag2)
            ((DisposableWrapper) dwgDatabase).Dispose();
          if (!flag1)
          {
            ArrayList modelFiles = new ArrayList();
            UIUtils.UpdateFileCollectionWithXrefs(strModelName, string.Empty, Matrix3d.Identity, false, ref modelFiles, (StringCollection) null, ref exts);
            Point3d minPoint = exts.MinPoint;
            double x1 = minPoint.X;
            Point3d maxPoint = exts.MaxPoint;
            double x2 = maxPoint.X;
            if (x1 > x2)
              exts = new Extents3d(new Point3d(-0.5, -0.5, -0.5), new Point3d(0.5, 0.5, 0.5));
          }
          return true;
        }
      }
      catch
      {
        if (!flag1)
          exts = new Extents3d(new Point3d(-0.5, -0.5, -0.5), new Point3d(0.5, 0.5, 0.5));
      }
      return extents;
    }

    public static bool DoesViewNameExistInDwgSheet(IDwgSheetDef dwgSheet, string strViewName)
    {
      bool flag = false;
      StringCollection colViewName;
      if (string.IsNullOrEmpty(strViewName) || !UIUtils.GetAllViewNamesInDwgSheet(dwgSheet, out colViewName))
        return flag;
      foreach (string strA in colViewName)
      {
        if (string.Compare(strA, strViewName, true) == 0)
        {
          flag = true;
          break;
        }
      }
      return flag;
    }

    public static bool DoesViewNameExist(string strViewName, string currentOrthoDwgName)
    {
      bool flag = false;
      if (string.IsNullOrEmpty(strViewName) || string.IsNullOrEmpty(currentOrthoDwgName))
        return true;
      IDwgSheetDef dwgSheetDef = (IDwgSheetDef) null;
      StringCollection colViewName;
      if (UIUtils.GetDwgSheetFromOrthoDrawingName(currentOrthoDwgName, out dwgSheetDef) && UIUtils.GetAllViewNamesInDwgSheet(dwgSheetDef, out colViewName))
      {
        foreach (string strA in colViewName)
        {
          if (string.Compare(strA, strViewName, true) == 0)
          {
            flag = true;
            break;
          }
        }
      }
      return flag;
    }

    public static bool DoesViewNameExist(string strViewName, PlantOrthoView orthoView)
    {
      bool flag = false;
      IDwgSheetDef dwgSheetDef = (IDwgSheetDef) null;
      StringCollection colViewName;
      if (UIUtils.GetDwgSheetFromOrthoView(orthoView, out dwgSheetDef) && UIUtils.GetAllViewNamesInDwgSheet(dwgSheetDef, out colViewName))
      {
        foreach (string strA in colViewName)
        {
          if (string.Compare(strA, strViewName, true) == 0)
          {
            flag = true;
            break;
          }
        }
      }
      return flag;
    }

    public static bool GetAllViewNamesInDwgSheet(
      IDwgSheetDef dwgSheetDef,
      out StringCollection colViewName)
    {
      colViewName = new StringCollection();
      if (dwgSheetDef != null)
      {
        foreach (IDwgViewDef dwgViewDef in (IEnumerable<IDwgViewDef>) dwgSheetDef.DwgViewDefs)
        {
          if (dwgViewDef != null)
            colViewName.Add(dwgViewDef.ViewName);
        }
      }
      return colViewName.Count > 0;
    }

    public static bool GetSheetDefByViewName(string strViewName, out IDwgSheetDef outDwgSheetDef)
    {
      bool sheetDefByViewName = false;
      outDwgSheetDef = (IDwgSheetDef) null;
      IDwgSheetDefs orthoDwgSheetDefs = UIUtils.OrthoDwgSheetDefs;
      if (orthoDwgSheetDefs != null)
      {
        foreach (IDwgSheetDef idwgSheetDef in (IEnumerable<IDwgSheetDef>) orthoDwgSheetDefs)
        {
          if (idwgSheetDef != null)
          {
            foreach (IDwgViewDef dwgViewDef in (IEnumerable<IDwgViewDef>) idwgSheetDef.DwgViewDefs)
            {
              if (dwgViewDef != null && string.Compare(dwgViewDef.ViewName, strViewName, true) == 0)
              {
                sheetDefByViewName = true;
                outDwgSheetDef = idwgSheetDef;
                break;
              }
            }
          }
        }
      }
      return sheetDefByViewName;
    }

    public static bool GetViewDefByViewName(
      string strViewName,
      IDwgSheetDef dwgSheetDef,
      out IDwgViewDef outDwgViewDef)
    {
      bool viewDefByViewName = false;
      outDwgViewDef = (IDwgViewDef) null;
      if (dwgSheetDef != null)
      {
        foreach (IDwgViewDef dwgViewDef in (IEnumerable<IDwgViewDef>) dwgSheetDef.DwgViewDefs)
        {
          if (dwgViewDef != null && string.Compare(dwgViewDef.ViewName, strViewName, true) == 0)
          {
            viewDefByViewName = true;
            outDwgViewDef = dwgViewDef;
            return viewDefByViewName;
          }
        }
      }
      return viewDefByViewName;
    }

    public static bool DoesContainIllegalChars(string name)
    {
      return name.Contains("|") || name.Contains("*") || name.Contains("\\") || name.Contains("/") || name.Contains(":") || name.Contains(";") || name.Contains(">") || name.Contains("<") || name.Contains("?") || name.Contains("\"") || name.Contains(",") || name.Contains("=") || name.Contains("`");
    }

    public static bool IsActiveDoc(string strFileName)
    {
      try
      {
        DocumentCollection documentManager = Application.DocumentManager;
        foreach (Document document in documentManager)
        {
          if (string.Compare(document.Name, strFileName, true) == 0 && documentManager.DocumentActivationEnabled && document.IsActive)
            return true;
        }
      }
      catch
      {
      }
      return false;
    }

    public static bool IsDocOpen(string strFileName, out Document docReturned)
    {
      docReturned = (Document) null;
      if (string.IsNullOrEmpty(strFileName))
        return false;
      try
      {
        foreach (Document document in Application.DocumentManager)
        {
          if (string.Compare(document.Name, strFileName, true) == 0)
          {
            docReturned = document;
            return true;
          }
        }
      }
      catch
      {
      }
      return false;
    }

    public static void OpenDocument(string strFileName, out Document oDoc, bool isReadOnlyDwg)
    {
      oDoc = (Document) null;
      try
      {
        bool flag = false;
        DocumentCollection documentManager = Application.DocumentManager;
        if (documentManager == null)
          return;
        if (documentManager.Count > 0)
        {
          foreach (Document document in documentManager)
          {
            if (string.Compare(document.Name, strFileName, true) == 0)
            {
              flag = true;
              if (documentManager.DocumentActivationEnabled)
              {
                if (!document.IsActive)
                {
                  Application.DocumentManager.MdiActiveDocument = document;
                  if (document.Window.WindowState == (Window.State)1)
                    document.Window.WindowState = (Window.State) 0;
                  oDoc = document;
                  break;
                }
                break;
              }
              break;
            }
          }
        }
        if (flag)
          return;
        if (!File.Exists(strFileName))
          oDoc = DocumentCollectionExtension.Add(documentManager, "");
        oDoc = DocumentCollectionExtension.Open(documentManager, strFileName, isReadOnlyDwg);
      }
      catch
      {
      }
    }

    public static Bitmap ExtractDwgThumbnail(string pwszFileName)
    {
      byte[] numArray1 = new byte[16]
      {
        (byte) 31,
        (byte) 37,
        (byte) 109,
        (byte) 7,
        (byte) 212,
        (byte) 54,
        (byte) 40,
        (byte) 40,
        (byte) 157,
        (byte) 87,
        (byte) 202,
        (byte) 63,
        (byte) 157,
        (byte) 68,
        (byte) 16,
        (byte) 43
      };
      using (FileStream fileStream = new FileStream(pwszFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
      {
        int count1 = 1024;
        byte[] numArray2 = new byte[count1];
        fileStream.Read(numArray2, 0, count1);
        string str = Encoding.ASCII.GetString(numArray2);
        if (!str.StartsWith("AC402") && !str.StartsWith("AC401") && !str.StartsWith("AC1800") && !str.StartsWith("AC1018") && !str.StartsWith("AC701a") && !str.StartsWith("AC1021") && (!str.StartsWith("AC1015") || numArray2[11] < (byte) 1))
          return (Bitmap) null;
        int num1 = UIUtils.pickUInt32(new byte[4]
        {
          numArray2[13],
          numArray2[14],
          numArray2[15],
          numArray2[16]
        });
        fileStream.Position = (long) num1;
        byte[] buffer1 = new byte[16];
        if (fileStream.Read(buffer1, 0, 16) == 0)
          return (Bitmap) null;
        for (int index = 0; index < buffer1.Length; ++index)
        {
          if ((int) buffer1[index] != (int) numArray1[index])
            return (Bitmap) null;
        }
        byte[] numArray3 = new byte[4];
        if (fileStream.Read(numArray3, 0, 4) == 0)
          return (Bitmap) null;
        UIUtils.pickUInt32(numArray3);
        byte[] buffer2 = new byte[1];
        if (fileStream.Read(buffer2, 0, 1) == 0)
          return (Bitmap) null;
        Bitmap dwgThumbnail = (Bitmap) null;
        for (int index = (int) buffer2[0]; index > 0; --index)
        {
          byte[] buffer3 = new byte[1];
          if (fileStream.Read(buffer3, 0, 1) == 0)
            return (Bitmap) null;
          byte[] numArray4 = new byte[4];
          if (fileStream.Read(numArray4, 0, 4) == 0)
            return (Bitmap) null;
          int num2 = UIUtils.pickUInt32(numArray4);
          byte[] numArray5 = new byte[4];
          if (fileStream.Read(numArray5, 0, 4) == 0)
            return (Bitmap) null;
          int count2 = UIUtils.pickUInt32(numArray5);
          if ((byte) 2 == buffer3[0] && count2 <= 100000000)
          {
            fileStream.Position = (long) num2;
            byte[] buffer4 = new byte[count2];
            if (fileStream.Read(buffer4, 0, count2) == 0)
              return (Bitmap) null;
            GCHandle gcHandle = GCHandle.Alloc((object) buffer4, GCHandleType.Pinned);
            dwgThumbnail = Autodesk.AutoCAD.Runtime.Marshaler.BitmapInfoToBitmap(gcHandle.AddrOfPinnedObject());
            if (gcHandle.IsAllocated)
            {
              gcHandle.Free();
              break;
            }
            break;
          }
        }
        fileStream.Close();
        return dwgThumbnail;
      }
    }

    private static int pickUInt32(byte[] pBuffer)
    {
      return (int) pBuffer[0] | (int) pBuffer[1] << 8 | (int) pBuffer[2] << 16 | (int) pBuffer[3] << 24;
    }

    public static void ShowCancelWarningMsg()
    {
      Application.DocumentManager.MdiActiveDocument.Editor?.WriteMessage(LocalResources.IDS_SelectRefModels_Warning);
    }

    public static OrthoProject OrthoProjectPart
    {
      get
      {
        try
        {
          return PlantApplication.CurrentProject.ProjectParts["Ortho"] as OrthoProject;
        }
        catch
        {
          return (OrthoProject) null;
        }
      }
    }

    public static IDwgSheetDefs OrthoDwgSheetDefs
    {
      get
      {
        return UIUtils.OrthoProjectPart != null ? UIUtils.OrthoProjectPart.DwgSheetDefinitions : (IDwgSheetDefs) null;
      }
    }

    public static void GetScaleList(Database db, out List<AnnotationScale> scaleList)
    {
      scaleList = new List<AnnotationScale>();
      if (db == null)
        return;
      try
      {
        ObjectContextManager objectContextManager = db.ObjectContextManager;
        if (objectContextManager == null)
          return;
        ObjectContextCollection contextCollection = objectContextManager.GetContextCollection("ACDB_ANNOTATIONSCALES");
        if (contextCollection == null)
          return;
        ObjectIdCollection objectIdCollection = new ObjectIdCollection();
        foreach (ObjectContext objectContext in contextCollection)
        {
          if (objectContext is AnnotationScale)
          {
            AnnotationScale annotationScale = (AnnotationScale) objectContext;
            if (annotationScale != null)
              scaleList.Add(annotationScale);
          }
        }
      }
      catch
      {
      }
    }

    private static int CompareScaleByValue(AnnotationScale x, AnnotationScale y)
    {
      return x != null ? (y != null ? x.Scale.CompareTo(y.Scale) : 1) : (y != null ? -1 : 0);
    }

    public static void GetSortedStyleAnnotationScaleList(
      out List<AnnotationScale> list,
      bool bImperial)
    {
      list = new List<AnnotationScale>();
      PlantOrthoView plantOrthoView = new PlantOrthoView((SupportInfo) null);
      if (plantOrthoView == null)
        return;
      List<AnnotationScale> scaleList = (List<AnnotationScale>) null;
      UIUtils.GetScaleList(plantOrthoView.OrthoDwgDatabase, out scaleList);
      if (scaleList != null && scaleList.Count > 0)
      {
        foreach (AnnotationScale annotationScale in scaleList)
        {
          if (((ObjectContext) annotationScale).Name.Contains("'") || ((ObjectContext) annotationScale).Name.Contains("\""))
          {
            if (bImperial)
              list.Add(annotationScale);
          }
          else if (!bImperial)
            list.Add(annotationScale);
        }
      }
      list.Sort(new Comparison<AnnotationScale>(UIUtils.CompareScaleByValue));
      list.Reverse();
    }

    public static void GetAllScaleNamesFromDrawing(out StringCollection scaleNames)
    {
      scaleNames = new StringCollection();
      PlantOrthoView orthoView = new PlantOrthoView((SupportInfo) null);
      if (orthoView == null)
        return;
      List<AnnotationScale> scaleList = (List<AnnotationScale>) null;
      UIUtils.GetScaleList(orthoView.OrthoDwgDatabase, out scaleList);
      if (scaleList != null && scaleList.Count > 0)
      {
        foreach (AnnotationScale annotationScale in scaleList)
          scaleNames.Add(((ObjectContext) annotationScale).Name);
      }
      IDwgSheetDef dwgSheetDef = (IDwgSheetDef) null;
      if (!UIUtils.GetDwgSheetFromOrthoView(orthoView, out dwgSheetDef))
        return;
      foreach (IDwgViewDef dwgViewDef in (IEnumerable<IDwgViewDef>) dwgSheetDef.DwgViewDefs)
      {
        AnnotationScale annoScale = (AnnotationScale) null;
        if (UIUtils.OrthoProjectPart == null)
          break;
        try
        {
          double viewScale = dwgViewDef.ViewScale;
        }
        catch
        {
          continue;
        }
        UIUtils.GetAnnotationScaleFromScale(dwgViewDef.ViewScale, (Project) UIUtils.OrthoProjectPart, out annoScale);
        if (((DisposableWrapper) annoScale) == ((DisposableWrapper) null))
          scaleNames.Insert(0, dwgViewDef.ViewScale.ToString());
      }
    }

    public static void GetAnnotationScaleFromName(
      string name,
      Project orthoProjectPart,
      out AnnotationScale scale)
    {
      scale = (AnnotationScale) null;
      if (string.IsNullOrEmpty(name))
        return;
      try
      {
        foreach (Document document in Application.DocumentManager)
        {
          Database database = document.Database;
          if (string.Compare(PnPProjectUtils.GetProjectPartTypeFromDrawingFile(database), "Ortho", true) == 0)
          {
            List<AnnotationScale> scaleList;
            UIUtils.GetScaleList(database, out scaleList);
            foreach (AnnotationScale annotationScale in scaleList)
            {
              if (string.Compare(((ObjectContext) annotationScale).Name, name, true) == 0)
              {
                scale = annotationScale;
                return;
              }
            }
          }
        }
      }
      catch
      {
      }
    }

    public static void GetAnnotationScaleFromScale(
      double scale,
      Project orthoProjectPart,
      out AnnotationScale annoScale)
    {
      annoScale = (AnnotationScale) null;
      try
      {
        foreach (Document document in Application.DocumentManager)
        {
          Database database = document.Database;
          if (string.Compare(PnPProjectUtils.GetProjectPartTypeFromDrawingFile(database), "Ortho", true) == 0)
          {
            List<AnnotationScale> scaleList;
            UIUtils.GetScaleList(database, out scaleList);
            foreach (AnnotationScale annotationScale in scaleList)
            {
              if (UIUtils.IsDoubleEqual(scale, annotationScale.Scale))
              {
                annoScale = annotationScale;
                return;
              }
            }
          }
        }
      }
      catch
      {
      }
    }

    public static bool IsDoubleEqual(double dValue1, double dValue2)
    {
      return Math.Abs(dValue1 - dValue2) < 1E-07;
    }

    public static bool IsPaperSpace(Database db)
    {
      return db != null && !db.TileMode;
    }

    public static bool IsMSpaceActive()
    {
      bool locked = false;
      int cvport;
      return UIUtils.GetVportProps(out locked, out cvport) && 1 != cvport;
    }

    public static void GetCurrentLayoutSize(out double width, out double height)
    {
      width = 0.0;
      height = 0.0;
      Autodesk.AutoCAD.DatabaseServices.TransactionManager transactionManager = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager;
      using (Transaction transaction = transactionManager.StartTransaction())
      {
        try
        {
          using (Application.DocumentManager.MdiActiveDocument.LockDocument())
          {
            LayoutManager current = LayoutManager.Current;
            ObjectId layoutId = current.GetLayoutId(current.CurrentLayout);
            Layout layout = (Layout) transactionManager.GetObject(layoutId, (OpenMode) 0);
            Extents2d limits = layout.Limits;
            Point2d maxPoint = limits.MaxPoint;
            limits = layout.Limits;
            Point2d minPoint = limits.MinPoint;
            width = Math.Abs(maxPoint.X - minPoint.X);
            height = Math.Abs(maxPoint.Y - minPoint.Y);
          }
          transaction.Commit();
        }
        catch
        {
          transaction.Abort();
        }
      }
    }

    public static bool UnlockViewportScale(bool bToUnlock, out bool bLocked)
    {
      bLocked = false;
      Transaction transaction = (Transaction) null;
      try
      {
        Document _doc = Application.DocumentManager.MdiActiveDocument;
        Editor _diagEditor = _doc != null ? _doc.Editor : null;
        _diagEditor?.WriteMessage("\n[PFS-DIAG] UnlockViewportScale 진입 bToUnlock=" + bToUnlock + " doc='" + (_doc == null ? "<null>" : _doc.Name) + "'");
        transaction = ((Autodesk.AutoCAD.DatabaseServices.TransactionManager) _doc.TransactionManager).StartTransaction();
        Editor editor = _doc.Editor;
        ObjectId _activeVpId = editor.ActiveViewportId;
        _diagEditor?.WriteMessage("\n[PFS-DIAG] UnlockViewportScale activeViewportId=" + _activeVpId + " isNull=" + _activeVpId.IsNull);
        Viewport viewport = (Viewport) transaction.GetObject(_activeVpId, (OpenMode) 1);
        if (((DisposableWrapper) null) != ((DisposableWrapper) viewport))
        {
          bLocked = viewport.Locked;
          _diagEditor?.WriteMessage("\n[PFS-DIAG] UnlockViewportScale viewport number=" + viewport.Number + " locked=" + bLocked + " on=" + viewport.On + " center=" + viewport.CenterPoint + " size=(" + viewport.Width + "," + viewport.Height + ")");
          if (bToUnlock & bLocked)
            viewport.Locked = false;
          else if (!bLocked && !bToUnlock)
            viewport.Locked = true;
        }
        else
        {
          _diagEditor?.WriteMessage("\n[PFS-DIAG] UnlockViewportScale viewport=null");
        }
        transaction.Commit();
      }
      catch (Exception _ux)
      {
        try { transaction?.Abort(); } catch (Exception _abortEx) { System.Diagnostics.Debug.WriteLine("[PFS-DIAG] UnlockViewportScale Abort 예외: " + _abortEx.Message); }
        try { Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\n[PFS-DIAG] UnlockViewportScale 예외: " + _ux.GetType().Name + ": " + _ux.Message); }
        catch (Exception _logEx) { System.Diagnostics.Debug.WriteLine("[PFS-DIAG] UnlockViewportScale 로그 예외: " + _logEx.Message); }
        return false;
      }
      return true;
    }

    private static bool GetVportProps(out bool locked, out int cvport)
    {
      locked = false;
      cvport = 1;
      using (Transaction transaction = ((Autodesk.AutoCAD.DatabaseServices.TransactionManager) Application.DocumentManager.MdiActiveDocument.TransactionManager).StartTransaction())
      {
        try
        {
          Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;
          Viewport viewport = (Viewport) transaction.GetObject(editor.ActiveViewportId, (OpenMode) 0);
          locked = viewport.Locked;
          cvport = viewport.Number;
          transaction.Commit();
        }
        catch
        {
          transaction.Abort();
          return false;
        }
      }
      return true;
    }

    public static bool ValidateRefModelSaveState(string refModel)
    {
      bool flag1 = true;
      DocumentCollection documentManager = Application.DocumentManager;
      foreach (Document document in documentManager)
      {
        if (string.Compare(document.Name, refModel, true) == 0)
        {
          bool flag2 = false;
          bool flag3 = false;
          if (DocumentExtension.GetAcadDocument(document) is AcadDocument acadDocument)
          {
            flag2 = ((IAcadDocument) acadDocument).Saved;
            flag3 = ((IAcadDocument) acadDocument).ReadOnly;
          }
          if (flag2)
            return true;
          try
          {
            documentManager.MdiActiveDocument = document;
            int result = 0;
            if (int.TryParse(Application.GetSystemVariable("dbmod").ToString(), out result))
            {
              if ((result & 1) == 0)
                return true;
            }
          }
          catch
          {
          }
          try
          {
            if (flag3)
              return true;
            UIUtils.s_isRefModelsSaving = true;
            UIUtils.s_isRefModelsSaving = false;
            return true;
          }
          catch
          {
            return false;
          }
        }
      }
      return flag1;
    }

    public static bool HasSpecialCharacters(string name)
    {
      if (name.Length >= 250)
        return true;
      string str1 = "\\/:*?\"<>|\t";
      string empty = string.Empty;
      try
      {
        for (int startIndex = 0; startIndex < name.Length; ++startIndex)
        {
          string str2 = name.Substring(startIndex, 1);
          if (str1.Contains(str2))
            return true;
        }
      }
      catch
      {
      }
      return false;
    }

    public static void CreateViewConfig(
      IDwgSheetDefs sheetDefs,
      string configName,
      Point3d lowerPt,
      double l,
      double w,
      double h,
      string scaleName,
      string viewName,
      out IDwgViewConfiguration vconf)
    {
      vconf = (IDwgViewConfiguration) null;
      try
      {
        if (sheetDefs == null || string.IsNullOrEmpty(configName) || sheetDefs.ViewConfigurations.Contains(configName))
          return;
        vconf = Dwg2dDef.NewViewConfiguration(sheetDefs, configName);
        if (vconf == null)
          return;
        vconf.Height = h;
        vconf.Length = l;
        vconf.Width = w;
        vconf.LowerLeft = lowerPt;
        vconf.Scale = scaleName;
        vconf.OrthoView = viewName;
        sheetDefs.AcceptChanges();
      }
      catch
      {
      }
    }

    public static bool ItContains(List<string> list, string name)
    {
      bool flag = false;
      if (string.IsNullOrEmpty(name) || list.Count == 0)
        return flag;
      for (int index = 0; index < list.Count; ++index)
      {
        string strB = list[index];
        if (!string.IsNullOrEmpty(strB) && string.Compare(name, strB, true) == 0)
          return true;
      }
      return flag;
    }

    public static void GetTempOrthoDWG(out string tempOrthoDWGName)
    {
      tempOrthoDWGName = string.Empty;
      try
      {
        string tempFileName = Commands.OrthoView.TempFileName;
        if (string.IsNullOrEmpty(tempFileName) || !File.Exists(tempFileName))
          return;
        tempOrthoDWGName = tempFileName;
      }
      catch
      {
      }
    }

    public static bool GetDwgSheetFromOrthoView(
      PlantOrthoView orthoView,
      out IDwgSheetDef dwgSheetDef)
    {
      dwgSheetDef = (IDwgSheetDef) null;
      Database orthoDwgDatabase = orthoView.OrthoDwgDatabase;
      if (((DisposableWrapper) orthoDwgDatabase) != ((DisposableWrapper) null))
      {
        Project orthoProjectPart = orthoView.OrthoProjectPart;
        if (orthoProjectPart != null)
        {
          DataLinksManager dataLinksManager = orthoProjectPart.DataLinksManager;
          if (dataLinksManager != null)
          {
            try
            {
              int drawingId = dataLinksManager.GetDrawingId(orthoDwgDatabase);
              if (drawingId > 0)
                return UIUtils.GetDwgSheetFromOrthoDrawingGuid(dataLinksManager.GetDrawingGuid(drawingId), out dwgSheetDef);
            }
            catch
            {
              return false;
            }
          }
        }
      }
      return UIUtils.GetDwgSheetFromOrthoDrawingName(orthoView.CurrentOrthoDwgName, out dwgSheetDef);
    }

    public static bool GetDwgSheetFromOrthoDwg(Database db, out IDwgSheetDef dwgSheetDef)
    {
      dwgSheetDef = (IDwgSheetDef) null;
      if (((DisposableWrapper) db) != ((DisposableWrapper) null))
      {
        Project orthoProjectPart = (Project) UIUtils.OrthoProjectPart;
        if (orthoProjectPart != null)
        {
          DataLinksManager dataLinksManager = orthoProjectPart.DataLinksManager;
          if (dataLinksManager != null)
          {
            try
            {
              int drawingId = dataLinksManager.GetDrawingId(db);
              if (drawingId > 0)
                return UIUtils.GetDwgSheetFromOrthoDrawingGuid(dataLinksManager.GetDrawingGuid(drawingId), out dwgSheetDef);
            }
            catch
            {
            }
          }
        }
      }
      return false;
    }

    public static bool GetDwgSheetFromOrthoDrawingName(string dwgName, out IDwgSheetDef dwgSheetDef)
    {
      dwgSheetDef = (IDwgSheetDef) null;
      foreach (IDwgSheetDef orthoDwgSheetDef in (IEnumerable<IDwgSheetDef>) UIUtils.OrthoDwgSheetDefs)
      {
        if (string.Compare(UIUtils.GetDwgSheetDefResolvedPath(orthoDwgSheetDef), dwgName, true) == 0)
        {
          dwgSheetDef = orthoDwgSheetDef;
          break;
        }
      }
      return dwgSheetDef != null;
    }

    public static bool GetDwgSheetFromOrthoDrawingGuid(string guid, out IDwgSheetDef dwgSheetDef)
    {
      dwgSheetDef = (IDwgSheetDef) null;
      foreach (IDwgSheetDef orthoDwgSheetDef in (IEnumerable<IDwgSheetDef>) UIUtils.OrthoDwgSheetDefs)
      {
        if (string.Compare(guid, orthoDwgSheetDef.DwgGuid, true) == 0)
        {
          dwgSheetDef = orthoDwgSheetDef;
          break;
        }
      }
      return dwgSheetDef != null;
    }

    public static bool GetViewportData(
      PlantFlow_Support.Commands.ViewType viewType,
      Matrix3d trans,
      Vector3d viewDir,
      Vector3d upVector,
      double l,
      double w,
      double h,
      double scale,
      out double viewportWidth,
      out double viewportHeight,
      out double viewportDepth)
    {
      viewportWidth = viewportHeight = 0.0;
      viewportDepth = 0.0;
      using (Polyline polyline = new Polyline(4))
      {
        polyline.Normal = new Vector3d(0.0, 0.0, 1.0);
        polyline.AddVertexAt(0, new Point2d(0.0, 0.0), 0.0, 0.0, 0.0);
        polyline.AddVertexAt(1, new Point2d(l, 0.0), 0.0, 0.0, 0.0);
        polyline.AddVertexAt(2, new Point2d(l, w), 0.0, 0.0, 0.0);
        polyline.AddVertexAt(3, new Point2d(0.0, w), 0.0, 0.0, 0.0);
        polyline.Closed = true;
        polyline.Thickness = h;
        if ((int)viewType != 0)
          polyline.TransformBy(trans);
        Point3d origin1 = Point3d.Origin;
        Vector3d vector3d1 = -(upVector);
        Vector3d vector3d2 = vector3d1.CrossProduct(viewDir);
        Vector3d vector3d3 = -(upVector);
        Vector3d vector3d4 = viewDir;
        Point3d origin2 = Point3d.Origin;
        Vector3d xaxis = Vector3d.XAxis;
        Vector3d yaxis = Vector3d.YAxis;
        Vector3d zaxis = Vector3d.ZAxis;
        Matrix3d matrix3d = Matrix3d.AlignCoordinateSystem(origin1, vector3d2, vector3d3, vector3d4, origin2, xaxis, yaxis, zaxis);
        try
        {
          polyline.TransformBy(matrix3d);
          Extents3d geometricExtents = polyline.GeometricExtents;
          viewportWidth = scale * (geometricExtents.MaxPoint.X - geometricExtents.MinPoint.X);
          viewportHeight = scale * (geometricExtents.MaxPoint.Y - geometricExtents.MinPoint.Y);
          viewportDepth = geometricExtents.MaxPoint.Z - geometricExtents.MinPoint.Z;
        }
        catch
        {
          return false;
        }
        double num = PnPDwg2dUtil.ViewportPadding();
        viewportWidth *= 1.0 + num;
        viewportHeight *= 1.0 + num;
      }
      return true;
    }

    public static void HighlightViewport(ObjectId viewportId)
    {
      if (viewportId.IsNull)
        return;
      Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
      if (((DisposableWrapper) mdiActiveDocument) == ((DisposableWrapper) null))
        return;
      using (Transaction transaction = ((Autodesk.AutoCAD.DatabaseServices.TransactionManager) mdiActiveDocument.TransactionManager).StartTransaction())
      {
        try
        {
          ((Entity) transaction.GetObject(viewportId, (OpenMode) 0)).Highlight();
          transaction.Commit();
        }
        catch
        {
          transaction.Abort();
        }
      }
    }

    public static bool RenameExistingView(string dwgName, string oldViewName, string newViewName)
    {
      if (string.IsNullOrEmpty(dwgName) || string.IsNullOrEmpty(oldViewName) || string.IsNullOrEmpty(newViewName))
        return false;
      Database database = Application.DocumentManager.MdiActiveDocument.Database;
      using (Application.DocumentManager.MdiActiveDocument.LockDocument())
        Dwg2dDbx.GetSheet(database)?.RenameView(oldViewName, newViewName);
      IDwgSheetDef dwgSheetDef = (IDwgSheetDef) null;
      if (!UIUtils.GetDwgSheetFromOrthoDrawingName(dwgName, out dwgSheetDef))
        return false;
      IDwgViewDef idwgViewDef = (IDwgViewDef) null;
      foreach (IDwgViewDef dwgViewDef in (IEnumerable<IDwgViewDef>) dwgSheetDef.DwgViewDefs)
      {
        if (string.Compare(dwgViewDef.ViewName, oldViewName, true) == 0)
        {
          idwgViewDef = dwgViewDef;
          break;
        }
      }
      if (idwgViewDef == null)
        return false;
      idwgViewDef.ViewName = newViewName;
      return true;
    }

    public static ObjectId FindVPortInSelSet(
      SelectionSet selset,
      Database dwg,
      out double vpWidth,
      out double vpHeight,
      out double scale,
      out Point3d vpCenter)
    {
      vpWidth = 0.0;
      vpHeight = 0.0;
      scale = 1.0;
      vpCenter = Point3d.Origin;
      foreach (ObjectId objectId in selset.GetObjectIds())
      {
        if (UIUtils.IsValidViewpport(objectId, out vpWidth, out vpHeight, out scale, out vpCenter))
          return objectId;
      }
      IDwgSheet sheet = Dwg2dDbx.GetSheet(dwg);
      if (sheet != null)
      {
        uint numViews = sheet.NumViews;
        for (uint index = 0; index < numViews; ++index)
        {
          IDwgView view = sheet.GetView(index);
          if (view != null)
          {
            foreach (ObjectId objectId in selset.GetObjectIds())
            {
              if (view.IsMatchLineBorder(objectId) && UIUtils.IsValidViewpport(view.ViewportId, out vpWidth, out vpHeight, out scale, out vpCenter))
                return view.ViewportId;
            }
          }
        }
      }
      return ObjectId.Null;
    }

    public static bool GetExistingViewport(
      out ObjectId viewportId,
      out double vpWidth,
      out double vpHeight,
      out double scale,
      out Point3d vpCenter,
      UIUtils.EditType type)
    {
      vpWidth = 0.0;
      vpHeight = 0.0;
      scale = 1.0;
      viewportId = ObjectId.Null;
      vpCenter = Point3d.Origin;
      PromptSelectionOptions selectionOptions = new PromptSelectionOptions();
      selectionOptions.SingleOnly = true;
      bool adjacentView = false;
      Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;
      switch (type)
      {
        case UIUtils.EditType.Update:
          selectionOptions.MessageForAdding = "\n" + LocalResources.IDS_UpdateView_SelectOrthoViewPrompt;
          adjacentView = true;
          break;
        case UIUtils.EditType.Delete:
          selectionOptions.MessageForAdding = "\n" + LocalResources.IDS_DeleteView_SelectOrthoViewPrompt;
          adjacentView = true;
          break;
        case UIUtils.EditType.Adjacent:
          selectionOptions.MessageForAdding = "\n" + LocalResources.IDS_AdjacenView_SelectOrthoViewPrompt;
          adjacentView = true;
          break;
        case UIUtils.EditType.SingleLine:
          selectionOptions.MessageForAdding = "\n" + LocalResources.IDS_SINGLE_LINE_SELECT_VIEW_PROMPT;
          break;
        default:
          selectionOptions.MessageForAdding = "\n" + LocalResources.IDS_PlantOrthoView_SelectExistingVPPrompt;
          break;
      }
      try
      {
        while (true)
        {
          PromptSelectionResult selection = editor.GetSelection(selectionOptions);
          if (selection.Status == PromptStatus.OK)
          {
            SelectionSet selset = selection.Value;
            if (selset != null)
            {
              if (selset.Count < 3)
              {
                viewportId = UIUtils.FindVPortInSelSet(selset, Application.DocumentManager.MdiActiveDocument.Database, out vpWidth, out vpHeight, out scale, out vpCenter);
                if (!viewportId.IsNull)
                  goto label_14;
              }
              else
              {
                editor.WriteMessage("\n" + LocalResources.IDS_Wanring_MultipleViewPort_Selection);
                continue;
              }
            }
            UIUtils.NotValidViewportMsg(adjacentView);
          }
          else
            break;
        }
        return false;
label_14:
        return true;
      }
      catch
      {
      }
      return false;
    }

    public static bool IsValidViewpport(
      ObjectId id,
      out double vpWidth,
      out double vpHeight,
      out double scale,
      out Point3d vpCenter)
    {
      vpWidth = 0.0;
      vpHeight = 0.0;
      vpCenter = Point3d.Origin;
      scale = 1.0;
      if ((ObjectId.Null) == (id))
        return false;
      try
      {
        Autodesk.AutoCAD.DatabaseServices.TransactionManager transactionManager = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager;
        using (transactionManager.StartTransaction())
        {
          Viewport viewport = transactionManager.GetObject(id, (OpenMode) 0) as Viewport;
          if (((DisposableWrapper) null) != ((DisposableWrapper) viewport))
          {
            vpWidth = viewport.Width;
            vpHeight = viewport.Height;
            vpCenter = viewport.CenterPoint;
            scale = viewport.CustomScale;
            return true;
          }
        }
      }
      catch
      {
      }
      return false;
    }

    private static void NotValidViewportMsg(bool adjacentView)
    {
      Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;
      if (adjacentView)
        editor.WriteMessage("\n" + LocalResources.IDS_Warning_Invalid_Orthoview);
      else
        editor.WriteMessage("\n" + LocalResources.IDS_PlantOrthoView_SelectExistingVPWarning);
    }

    public static bool GetOrthoEntityListInView(
      string dwgName,
      string viewName,
      out List<ObjectId> orthoIds)
    {
      IDwgSheetDef dwgSheetDef = (IDwgSheetDef) null;
      orthoIds = new List<ObjectId>();
      if (!UIUtils.GetDwgSheetFromOrthoDrawingName(dwgName, out dwgSheetDef) || !UIUtils.DoesViewNameExistInDwgSheet(dwgSheetDef, viewName))
        return false;
      bool flag = false;
      Database dwgDatabase = PnPDwg2dUtil.GetDwgDatabase(dwgName, false, ref flag);
      if (((DisposableWrapper) dwgDatabase) == ((DisposableWrapper) null))
        return false;
      IDwgSheet sheet = Dwg2dDbx.GetSheet(dwgDatabase);
      if (sheet == null)
        return false;
      sheet.GetdwgRepIdsInView(viewName, orthoIds);
      if (!flag)
        ((DisposableWrapper) dwgDatabase).Dispose();
      return true;
    }

    public static bool ValidateOrthoDwgSheetVersion(Project prj, IDwgSheetDef dwgSheetDef)
    {
      if (!(prj is OrthoProject orthoProject))
        return false;
      if (orthoProject.IsLatestDwgSheetVersion(dwgSheetDef))
        return true;
      Application.DocumentManager.MdiActiveDocument.Editor?.WriteMessage("\n" + LocalResources.IDS_Warning_OutdatedDwg);
      return false;
    }

    public static void RefreshAnnosIntheDwg(string dwgName)
    {
      if (string.IsNullOrEmpty(dwgName))
        return;
      Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
      if (((DisposableWrapper) mdiActiveDocument) == ((DisposableWrapper) null))
        return;
      using (mdiActiveDocument.LockDocument())
      {
        using (PlantOrthoSyncStyles plantOrthoSyncStyles = new PlantOrthoSyncStyles(mdiActiveDocument.Database))
        {
          if (((SyncStyles) plantOrthoSyncStyles).IsDatabaseOutOfDate())
            ((SyncStyles) plantOrthoSyncStyles).ExecuteSyncStyles();
          else
            OrthoAnnotationUtils.RefreshAnnos(mdiActiveDocument.Database);
        }
      }
    }

    public static string GetDwgSheetDefResolvedPath(IDwgSheetDef dwgSheetDef)
    {
      string sheetDefResolvedPath = string.Empty;
      if (UIUtils.OrthoProjectPart != null && dwgSheetDef != null)
        sheetDefResolvedPath = UIUtils.OrthoProjectPart.GetDwgSheetDefResolvedPath(dwgSheetDef.DwgPath, dwgSheetDef.DwgGuid);
      return sheetDefResolvedPath;
    }

    public static Solid3d GetClipSolid(Database dwg, string viewName)
    {
      Solid3d clipSolid = (Solid3d) null;
      try
      {
        IDwgSheet sheet = Dwg2dDbx.GetSheet(dwg);
        if (sheet != null)
        {
          IDwgView view = sheet.GetView(viewName);
          if (view != null)
          {
            ObjectId clipSolidId = view.ClipSolidId;
            if (!clipSolidId.IsNull)
            {
              using (OpenCloseTransaction closeTransaction = clipSolidId.Database.TransactionManager.StartOpenCloseTransaction())
              {
                clipSolid = PnPDwg2dSolidUtil.MakeSolidCopy((Solid3d) ((Transaction) closeTransaction).GetObject(clipSolidId, (OpenMode) 0), true);
                ((Transaction) closeTransaction).Commit();
              }
            }
          }
        }
      }
      catch
      {
      }
      return clipSolid;
    }

    public static void SetVportSysVars(Database dwg, string viewName)
    {
      try
      {
        short num = -1;
        IDwgSheet sheet = Dwg2dDbx.GetSheet(dwg);
        if (sheet != null)
        {
          IDwgView view = sheet.GetView(viewName);
          if (view != null)
            num = (short) view.HiddenLineMode;
        }
        if (num < (short) 0)
          num = (short) 0;
        Application.SetSystemVariable("PLANTORTHOHIDDENLINEMODE", (object) num);
      }
      catch
      {
      }
    }

    public static void SetAMESysVariable()
    {
      try
      {
        if ((short) Application.GetSystemVariable("AECFORCEEXPLODETOSOLID") == (short) 0)
        {
          Application.SetSystemVariable("AECFORCEEXPLODETOSOLID", (object) 1);
          UIUtils.bAMESysVarChanged = true;
        }
        if ((short) Application.GetSystemVariable("AECFORCEDEFAULTMODELVIEW") != (short) 0)
          return;
        Application.SetSystemVariable("AECFORCEDEFAULTMODELVIEW", (object) 1);
      }
      catch
      {
      }
    }

    public static void ResetAMESysVariable()
    {
      try
      {
        if (!UIUtils.bAMESysVarChanged)
          return;
        Application.SetSystemVariable("AECFORCEEXPLODETOSOLID", (object) 0);
        Application.SetSystemVariable("AECFORCEDEFAULTMODELVIEW", (object) 0);
        UIUtils.bAMESysVarChanged = false;
      }
      catch
      {
      }
    }

    public static void GetViewportDataForRibbon(
      double scale,
      out double VPWidth,
      out double VPHeight,
      out double VPDepth,
      out PlantFlow_Support.Commands.ViewType viewType,
      out string strViewType,
      out Vector3d viewDir,
      out Vector3d upVector)
    {
      VPWidth = 0.0;
      VPHeight = 0.0;
      VPDepth = 0.0;
      viewType = (PlantFlow_Support.Commands.ViewType)0;
      strViewType = string.Empty;
      viewDir = Vector3d.ZAxis;
      upVector = Vector3d.ZAxis;
      PlantOrthoView plantOrthoView = new PlantOrthoView((SupportInfo) null);
      if (plantOrthoView == null)
        return;
      PlantOrthoCube orthoCube = plantOrthoView.OrthoCube;
      if (orthoCube == null)
        return;
      Point3d origin = Point3d.Origin;
      double w = 0.0;
      double h = 0.0;
      double l = 0.0;
      Matrix3d identity = Matrix3d.Identity;
      orthoCube.GetCubeProperties(out origin, out l, out w, out h, out identity);
      UISettings uiSettings = new UISettings();
      viewType = UIUtils.GetViewTypeFromName(uiSettings.OrthoView);
      strViewType = UIUtils.GetGlobalViewName(viewType);
      UIUtils.GetViewVectors(viewType, identity, out viewDir, out upVector);
      UIUtils.GetViewportData(viewType, identity, viewDir, upVector, l, w, h, scale, out VPWidth, out VPHeight, out VPDepth);
    }

    private static List<int> GetSpecifiedViewNumbers(
      StringCollection viewNames,
      string viewTypeName)
    {
      List<int> specifiedViewNumbers = new List<int>();
      foreach (string viewName in viewNames)
      {
        if (viewName.StartsWith(viewTypeName))
        {
          int numberFromName = UIUtils.GetNumberFromName(viewName, viewTypeName);
          if (numberFromName > 0)
            specifiedViewNumbers.Add(numberFromName);
        }
      }
      return specifiedViewNumbers;
    }

    private static int GetNumberFromName(string viewName, string viewTypeName)
    {
      if (string.Compare(viewName.Trim(), viewTypeName.Trim()) == 0)
        return 1;
      int result;
      return !int.TryParse(viewName.Remove(0, viewTypeName.Length).Trim(), out result) ? -1 : result;
    }

    public static string GenerateViewName()
    {
      string defaultViewName = UIUtils.GetDefaultViewName(UIUtils.GetViewTypeFromName(new UISettings().OrthoView));
      StringCollection colViewName = new StringCollection();
      IDwgSheetDef dwgSheetDef = (IDwgSheetDef) null;
      if (UIUtils.GetDwgSheetFromOrthoView(new PlantOrthoView((SupportInfo) null), out dwgSheetDef))
        UIUtils.GetAllViewNamesInDwgSheet(dwgSheetDef, out colViewName);
      if (colViewName == null || colViewName.Count <= 0)
        return defaultViewName;
      List<int> specifiedViewNumbers = UIUtils.GetSpecifiedViewNumbers(colViewName, defaultViewName);
      if (specifiedViewNumbers == null || specifiedViewNumbers.Count <= 0)
        return defaultViewName;
      for (int index = 1; index < int.MaxValue; ++index)
      {
        if (!specifiedViewNumbers.Contains(index))
          return defaultViewName + index.ToString();
      }
      return defaultViewName;
    }

    public static double GetScaleFromName(string name)
    {
      AnnotationScale scale = (AnnotationScale) null;
      PlantOrthoView plantOrthoView = new PlantOrthoView((SupportInfo) null);
      if (plantOrthoView == null)
        return double.NaN;
      UIUtils.GetAnnotationScaleFromName(name, plantOrthoView.OrthoProjectPart, out scale);
      if (((DisposableWrapper) null) != ((DisposableWrapper) scale))
        return scale.Scale;
      try
      {
        return double.Parse(name);
      }
      catch
      {
        return double.NaN;
      }
    }

    public static bool TryGetProjectAndProjectFile(
      TreeNode node,
      out Project prj,
      out PnPProjectFile pitem)
    {
      prj = (Project) null;
      pitem = (PnPProjectFile) null;
      string str = "Piping";
      if ((prj = PlantApplication.CurrentProject.ProjectParts[str]) == null)
        return false;
      int extendedPropertyAsInt32 = UIUtils.GetTreeNodeExtendedPropertyAsInt32(node, "rid", -1);
      return (pitem = prj.GetProjectItem(extendedPropertyAsInt32) as PnPProjectFile) != null;
    }

    public static string GetTreeNodeExtendedProperty(TreeNode node, string propname)
    {
      if (node == null || string.IsNullOrEmpty(propname))
        return (string) null;
      Dictionary<string, string> tag = (Dictionary<string, string>) node.Tag;
      if (tag != null)
      {
        string extendedProperty = "";
        if (tag.TryGetValue(propname, out extendedProperty))
          return extendedProperty;
      }
      return (string) null;
    }

    public static int GetTreeNodeExtendedPropertyAsInt32(
      TreeNode node,
      string propname,
      int defval)
    {
      if (node == null || string.IsNullOrEmpty(propname))
        return defval;
      Dictionary<string, string> tag = (Dictionary<string, string>) node.Tag;
      int result = defval;
      string s;
      if (tag != null && tag.TryGetValue(propname, out s))
        int.TryParse(s, out result);
      return result;
    }

    public static void SetTreeNodeExtendedProperty(TreeNode node, string propname, string propval)
    {
      if (node == null || string.IsNullOrEmpty(propname))
        return;
      Dictionary<string, string> dictionary = (Dictionary<string, string>) node.Tag;
      if (dictionary == null)
      {
        dictionary = new Dictionary<string, string>((IEqualityComparer<string>) StringComparer.OrdinalIgnoreCase);
        node.Tag = (object) dictionary;
      }
      if (dictionary.ContainsKey(propname))
        dictionary[propname] = propval;
      else
        dictionary.Add(propname, propval);
    }

    public static void AddAecXdata(BlockReference bref, Transaction t)
    {
      if (!((DBObject) bref).IsWriteEnabled)
        return;
      DynamicLinker dynamicLinker = SystemObjects.DynamicLinker;
      if (((DisposableWrapper) dynamicLinker) == ((DisposableWrapper) null))
        return;
      if (!dynamicLinker.IsModuleLoaded("aeccore.crx"))
        return;
      try
      {
        if (t.GetObject(((DBObject) bref).Database.RegAppTableId, (OpenMode) 0) is RegAppTable regAppTable && !((SymbolTable) regAppTable).Has("AECGUIBASE"))
        {
          using (RegAppTableRecord regAppTableRecord = new RegAppTableRecord())
          {
            ((SymbolTableRecord) regAppTableRecord).Name = "AECGUIBASE";
            ((DBObject) regAppTable).UpgradeOpen();
            ((SymbolTable) regAppTable).Add((SymbolTableRecord) regAppTableRecord);
            ((DBObject) regAppTable).DowngradeOpen();
            t.AddNewlyCreatedDBObject((DBObject) regAppTableRecord, true);
          }
        }
        ((DBObject) bref).XData = new ResultBuffer(new TypedValue[3]
        {
          new TypedValue(1001, (object) "AECGUIBASE"),
          new TypedValue(1070, (object) 100),
          new TypedValue(1000, (object) "Standard")
        });
      }
      catch
      {
      }
    }

    public enum EditType
    {
      Unknown,
      Create,
      Update,
      Delete,
      Adjacent,
      Edit,
      Recreate,
      Zoom,
      Validate,
      SingleLine,
    }
  }
}

