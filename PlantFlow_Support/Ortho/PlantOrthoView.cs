using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Interop.Common;
using Autodesk.AutoCAD.Runtime;
using Autodesk.ProcessPower.DataLinks;
using Autodesk.ProcessPower.DataObjects;
using Autodesk.ProcessPower.Drawings2d;
using Autodesk.ProcessPower.P3dProjectParts;
using Autodesk.ProcessPower.P3dUI;
using Autodesk.ProcessPower.PlantInstance;
using Autodesk.ProcessPower.PnP3dOrthoDrawingsUI;
using Autodesk.ProcessPower.PnPCommonUiUtils;
using Autodesk.ProcessPower.ProjectManager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;

#nullable disable
namespace PlantFlow_Support
{
  public class PlantOrthoView
  {
    private IDwgSheetDefs dwgSheetDefs;
    private PnPDatabase projectDatabase;
    private PlantProject plantProject = PlantApplication.CurrentProject;
    private Project pipingProjectPart;
    private Project orthoProjectpart;
    private Editor m_orthoDocEditor;
    private bool m_2DWorkflow = true;
    private bool m_IsEditingView;
    private string strCurrentDwgName = string.Empty;
    private string strCurrent3DDwgName = string.Empty;
    private Database m_dwgDatabase;
    private PlantOrthoCube m_plantOrthoCube;
    private string m_tempFileName = string.Empty;
    private StringCollection m_sysVars;
    private bool m_insulationDisplay;
    private bool m_markerDisplay;
    private bool m_placeHolderDisplay;
    private Point3d m_lowerLeftPt = Point3d.Origin;
    private Point3d m_upperRightPt = Point3d.Origin;
    private string m_strViewName;
    private double m_dScale;
    private double m_dViewportWidth;
    private double m_dViewportHeight;
    private double m_dViewportDepth;
    private Commands.ViewType m_viewType;
    private string m_strViewType;
    private Vector3d m_viewDir;
    private Vector3d m_upVector;
    private Point3d m_lowerLeftCubePt = Point3d.Origin;
    private Point3d m_upperRightCubePt = Point3d.Origin;
    private StringCollection m_FrozenDwgLayers;
    private StringCollection m_OffDwgLayers;
    private bool m_bSuccess = true;
    private bool m_bClosing;
    public UIUtils.EditType commandType = UIUtils.EditType.Create;
    private static UISettings settings = new UISettings();
    private StringCollection m_selectedModels;
    private double m_paperWidth;
    private double m_paperHeight;
    public static SupportInfo SPInfo; // Made public for refactoring
    private DataLinksManager dl_manager;

    public DataLinksManager DlManager => this.dl_manager; // Expose for OrthoViewportManager
    public Commands.ViewType CurrentViewType => this.m_viewType; // Expose for OrthoViewportManager
    public string CurrentViewTypeString => this.m_strViewType; // Expose for OrthoViewportManager
    public Database DwgDatabase => this.m_dwgDatabase; // Expose for OrthoViewportManager

    public Database OrthoDwgDatabase => this.m_dwgDatabase;

    public Project OrthoProjectPart => this.orthoProjectpart;

    public Project PipingProjectPart => this.pipingProjectPart;

    public PlantOrthoCube OrthoCube => this.m_plantOrthoCube;

    public string Model3DName => this.strCurrent3DDwgName;

    public string TempFileName => this.m_tempFileName;

    public string CurrentOrthoDwgName => this.strCurrentDwgName;

    public StringCollection SelectedModels => this.m_selectedModels;

    public double PaperWidth => this.m_paperWidth;

    public double PaperHeight => this.m_paperHeight;

    public bool Is2DWorkflow
    {
      get => this.m_2DWorkflow;
      set => this.m_2DWorkflow = value;
    }

    public bool IsEditingView
    {
      get => this.m_IsEditingView;
      set => this.m_IsEditingView = value;
    }

    public Vector3d Offset { get; set; }

    public double Rotation { get; set; }

    private OrthoViewportManager m_viewportManager;

    public PlantOrthoView(SupportInfo info)
    {
        this.m_viewportManager = new OrthoViewportManager(this);
        this.InitializeProjectData(info);
    }

    public void Print(string mess)
    {
      Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n" + mess + "\n");
    }

    private void InitializeProjectData(SupportInfo info)
    {
      try
      {
        PlantOrthoView.SPInfo = info;
        this.dl_manager = this.plantProject.ProjectParts["Piping"].DataLinksManager;
        Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
        this.pipingProjectPart = this.plantProject.ProjectParts["Piping"];
        this.orthoProjectpart = this.plantProject.ProjectParts["Ortho"];
        this.strCurrent3DDwgName = mdiActiveDocument.Name;
        this.SelectOrthoDwg(ref mdiActiveDocument);
        this.m_orthoDocEditor = Application.DocumentManager.MdiActiveDocument.Editor;
        this.dwgSheetDefs = UIUtils.OrthoDwgSheetDefs;
        if (this.dwgSheetDefs != null)
          this.projectDatabase = this.dwgSheetDefs.ProjectDatabase;
        this.m_dwgDatabase = mdiActiveDocument.Database;
      }
      catch (Exception ex)
      {
        this.m_bSuccess = false;
      }
    }

    public void run()
    {
      if (!this.m_bSuccess)
        return;
      bool flag = UIUtils.IsMSpaceActive();
      try
      {
        CommonUIUtils.acedPspace();
        UIUtils.GetCurrentLayoutSize(out this.m_paperWidth, out this.m_paperHeight);
        this.CreateView(PlantOrthoView.SPInfo.Extents);
      }
      catch
      {
      }
      finally
      {
        if (flag)
          CommonUIUtils.acedMspace();
      }
      this.m_strViewName = PlantOrthoView.SPInfo.ViewType.ToString();
      this.m_dScale = 0.5;
      this.m_dViewportWidth = 300.0;
      this.m_dViewportHeight = 300.0;
      this.m_dViewportDepth = 0.0;
      this.m_viewType = PlantOrthoView.SPInfo.ViewType;
      this.m_strViewType = this.m_strViewName;
      this.m_viewDir = PlantOrthoView.SPInfo.ViewDirection;
      this.m_upVector = PlantOrthoView.SPInfo.UpVector;
      this.CreateOrthoFromCube();
    }

    private bool SelectOrthoDwg(ref Document currentDoc)
    {
      if (PlantOrthoView.cchNewProjectDwgRun("Ortho", false) && this.orthoProjectpart is OrthoProject orthoProjectpart)
      {
        IDwgSheetDef dwgSheetDef = (IDwgSheetDef) null;
        if (UIUtils.GetDwgSheetFromOrthoDwg(Application.DocumentManager.MdiActiveDocument.Database, out dwgSheetDef))
          orthoProjectpart.SetOrthoDwgVersion(dwgSheetDef, 1);
      }
      currentDoc = Application.DocumentManager.MdiActiveDocument;
      this.strCurrentDwgName = Application.DocumentManager.MdiActiveDocument.Name;
      this.m_2DWorkflow = false;
      return true;
    }

    public static bool cchNewProjectDwgRun(string prjType, bool mustClose = true)
    {
      Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;
      Project project = PlantApplication.ProjectPart(prjType);
      if (project == null)
        return false;
      AcadApplication acadApplication = (AcadApplication) Application.AcadApplication;
      string str = PlantOrthoView.SPInfo.Name;
      if (str.EndsWith(".dwg", true, (CultureInfo) null))
        str = str.Substring(0, str.Length - 4);
      string path = Path.Combine(project.ProjectDwgDirectory, str + ".dwg");
      bool flag1 = true;
      int num = 1;
      while (flag1)
      {
        if (File.Exists(path))
        {
          path = Path.Combine(project.ProjectDwgDirectory, str + "_" + num.ToString() + ".dwg");
          ++num;
        }
        else
          flag1 = false;
      }
      string orthoTemplate = PSUtil.OrthoTemplate;
      string empty = string.Empty;
      AcadDocument acadDocument = ((IAcadDocuments) ((IAcadApplication) acadApplication).Documents).Add((object) orthoTemplate);
      ((IAcadDocument) acadDocument).SaveAs(path, Type.Missing, Type.Missing);
      bool flag2 = false;
      bool flag3 = false;
      project.AddPnPDrawingFile(path, empty, str, ref flag2, ref flag3);
      if (mustClose)
        ((IAcadDocument) acadDocument).Close(Type.Missing, Type.Missing);
      project.Save();
      Application.DocumentManager.MdiActiveDocument.SendStringToExecute("_.REFRESHPMESW\n", true, false, false);
      Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n" + str + "가 출력되었습니다. 저장 위치: " + path + "\n");
      return true;
    }

    public void CreateView(Extents3d Exts)
    {
      Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
      IDwgSheetDef dwgSheetDef = (IDwgSheetDef) null;
      if (!UIUtils.GetDwgSheetFromOrthoView(this, out dwgSheetDef) || !UIUtils.ValidateOrthoDwgSheetVersion(this.orthoProjectpart, dwgSheetDef))
        return;
      mdiActiveDocument.Editor.SetImpliedSelection((ObjectId[]) null);
      if (this.m_selectedModels == null)
      {
        StringCollection stringCollection = new StringCollection();
      }
      else
      {
        StringCollection selectedModels = this.m_selectedModels;
      }
      this.m_selectedModels = new StringCollection();
      this.m_selectedModels.Add(this.strCurrent3DDwgName);
      this.Offset = new Vector3d(0.0, 0.0, 0.0);
      this.Rotation = 0.0;
      this.orthoProjectpart.SetProjectVariable("SHOWHIDDENLINE", "TRUE");
      int num = 2;
      if ((int)PlantOrthoView.SPInfo.ViewType == 8)
        num = 0;
      Application.SetSystemVariable("PLANTORTHOHIDDENLINEMODE", (object) num);
      try
      {
        Extents3d ext = Exts;
        this.m_plantOrthoCube = new PlantOrthoCube();
        this.m_plantOrthoCube.InitializeCube(ext);
        this.m_lowerLeftPt = ext.MinPoint;
        this.m_upperRightPt = ext.MaxPoint;
        this.SetView(ext, new Vector3d(-1.0, -1.0, 1.0), new Vector3d(0.0, 0.0, 1.0));
        if (string.IsNullOrEmpty(PlantOrthoView.settings.OrthoView))
          PlantOrthoView.settings.OrthoView = UIUtils.s_TopView;
        this.m_plantOrthoCube.SetOrthoViewFaceColor((Autodesk.ProcessPower.PnP3dOrthoDrawingsUI.Commands.ViewType)(int)UIUtils.GetViewTypeFromName(PlantOrthoView.settings.OrthoView));
      }
      catch
      {
      }
    }

    public void GenerateOrthoTemplateFile()
    {
      string projectDwgDirectory = this.orthoProjectpart.ProjectDwgDirectory;
      string empty = string.Empty;
      string str = Path.Combine(projectDwgDirectory, "Orthographic View Selection");
      for (uint index = 1; index < uint.MaxValue; ++index)
      {
        bool flag = false;
        try
        {
          this.m_tempFileName = string.Format("{0} {1}", (object) str, (object) index) + ".dwg";
          if (File.Exists(this.m_tempFileName))
          {
            Document docReturned;
            if (UIUtils.IsDocOpen(this.m_tempFileName, out docReturned) && docReturned != null)
            {
              Application.DocumentManager.MdiActiveDocument = docReturned;
              this.CloseOrthoViewDoc();
            }
            if ((File.GetAttributes(this.m_tempFileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
              File.SetAttributes(this.m_tempFileName, File.GetAttributes(this.m_tempFileName) & ~FileAttributes.ReadOnly);
            File.Delete(this.m_tempFileName);
            flag = false;
          }
        }
        catch
        {
          flag = true;
        }
        if (!flag)
          break;
      }
    }

    public void CloseOrthoViewDoc()
    {
      if (this.m_bClosing)
        return;
      if (string.IsNullOrEmpty(this.TempFileName))
        return;
      try
      {
        this.m_bClosing = true;
        Document document1 = Application.DocumentManager.MdiActiveDocument;
        if (document1 != null && string.Compare(document1.Name, this.TempFileName, true) == 0)
        {
          document1.Editor.SetImpliedSelection((ObjectId[]) null);
        }
        else
        {
          document1 = (Document) null;
          foreach (Document document2 in Application.DocumentManager)
          {
            if (string.Compare(document2.Name, this.TempFileName, true) == 0)
            {
              document1 = document2;
              break;
            }
          }
        }
        if (document1 == null)
          return;
        try
        {
          if ((int)document1.LockMode() == 4)
            CommonUIUtils.acedPostCommand("CANCELCMD");
          DocumentExtension.CloseAndDiscard(document1);
        }
        catch
        {
        }
        this.ResetSysVars();
      }
      finally
      {
        this.m_bClosing = false;
      }
    }

    public void CreateOrthoFromCube()
    {
      this.OrthoCube.OnCloseDoc();
      Solid3d cubeSolid = (Solid3d) null;
      using (Application.DocumentManager.MdiActiveDocument.LockDocument())
        cubeSolid = this.OrthoCube.MakeCubeCopy();
      foreach (Document document in Application.DocumentManager)
      {
        if (string.Compare(document.Name, this.CurrentOrthoDwgName, true) == 0)
        {
          Application.DocumentManager.MdiActiveDocument = document;
          break;
        }
      }
      this.m_orthoDocEditor = Application.DocumentManager.MdiActiveDocument.Editor;
      Extents3d extents = this.OrthoCube.Extents;
      this.m_lowerLeftCubePt = extents.MinPoint;
      this.m_upperRightCubePt = extents.MaxPoint;
      this.m_dViewportDepth = this.m_upperRightCubePt.Z - this.m_lowerLeftCubePt.Z;
      this.CloseOrthoViewDoc();
      this.CreateOrthos(this.SelectedModels, this.m_strViewName, this.m_dScale, this.m_dViewportWidth, this.m_dViewportHeight, this.m_dViewportDepth, this.m_viewType, this.m_strViewType, this.m_viewDir, this.m_upVector, this.m_lowerLeftCubePt, this.m_upperRightCubePt, cubeSolid);
      if (cubeSolid == null)
        return;
      cubeSolid.Dispose();
    }

    public void CreateOrthos(
      StringCollection selectedModels,
      string strViewName,
      double scale,
      double viewportWidth,
      double viewportHeight,
      double viewportDepth,
      Commands.ViewType viewType,
      string strViewType,
      Vector3d viewDir,
      Vector3d upVector,
      Point3d lowerLeftPt,
      Point3d upperRightPt,
      Solid3d cubeSolid)
    {
      using (Application.DocumentManager.MdiActiveDocument.LockDocument())
      {
        try
        {
          Point3d ptIns = Point3d.Origin;
          ObjectId viewportId = ObjectId.Null;
          double newScale = 1.0;
          double newVPWidth = 0.0;
          double newVPHeight = 0.0;
          bool bLocked = false;
          if (!UIUtils.UnlockViewportScale(true, out bLocked))
            return;
          double rotationAngle = 0.0;
          if (this.IsEditingView)
          {
            ptIns = Point3d.Origin;
          }
          else
          {
            double newVPScale = 0.0;
            if (!this.ProcessViewportPlacement(viewportWidth, viewportHeight, scale, out newScale, out viewportId, out ptIns, out newVPWidth, out newVPHeight, out newVPScale, out rotationAngle))
              return;
            double num = PnPDwg2dUtil.ViewportPadding();
            if (!viewportId.IsNull && newVPWidth > 0.0 && newVPHeight > 0.0)
            {
              viewportWidth = newVPWidth;
              viewportHeight = newVPHeight;
              if (scale != newVPScale)
              {
                scale = newVPScale;
                AnnotationScale annoScale = (AnnotationScale) null;
                UIUtils.GetAnnotationScaleFromScale(scale, this.OrthoProjectPart, out annoScale);
                PlantOrthoView.settings.OrthoScale = annoScale != null ? ((ObjectContext) annoScale).Name : scale.ToString();
              }
            }
            if (scale != newScale)
            {
              viewportWidth /= 1.0 + num;
              viewportHeight /= 1.0 + num;
              viewportWidth = viewportWidth / scale * newScale;
              viewportHeight = viewportHeight / scale * newScale;
              viewportWidth *= 1.0 + num;
              viewportHeight *= 1.0 + num;
              scale = newScale;
              AnnotationScale annoScale = (AnnotationScale) null;
              UIUtils.GetAnnotationScaleFromScale(scale, this.OrthoProjectPart, out annoScale);
              PlantOrthoView.settings.OrthoScale = annoScale != null ? ((ObjectContext) annoScale).Name : scale.ToString();
            }
          }
          this.GenerateOrthos(selectedModels, strViewName, strViewType, lowerLeftPt, upperRightPt, viewType, ptIns, viewDir, upVector, viewportId, viewportWidth, viewportHeight, viewportDepth, scale, rotationAngle, cubeSolid);
          CommonUIUtils.acedPostCommandPrompt();
        }
        catch
        {
        }
      }
    }

    private ObjectId CreateTempLockedLayer(Document modelDoc)
    {
      ObjectId objectId = ObjectId.Null;
      try
      {
        using (Database database = modelDoc.Database)
        {
          using (OpenCloseTransaction closeTransaction = new OpenCloseTransaction())
          {
            LayerTable layerTable = (LayerTable) ((Transaction) closeTransaction).GetObject(database.LayerTableId, (OpenMode) 1);
            LayerTableRecord layerTableRecord = new LayerTableRecord();
            layerTableRecord.IsLocked = true;
            ((SymbolTableRecord) layerTableRecord).Name = "TempOrthoLayer";
            ((SymbolTable) layerTable).Add((SymbolTableRecord) layerTableRecord);
            objectId = ((DBObject) layerTableRecord).ObjectId;
            layerTableRecord.Dispose();
            layerTable.Dispose();
            ((Transaction) closeTransaction).Commit();
          }
        }
      }
      catch
      {
      }
      return objectId;
    }

    private bool CreateTempDrawing(
      StringCollection dwg3dFiles,
      string strFileName,
      Vector3d offset,
      double rotation,
      out bool retry)
    {
      bool tempDrawing = true;
      retry = false;
      AcadDocument acadDocument = ((IAcadDocuments) ((IAcadApplication) Application.AcadApplication).Documents).Add((object) "orthoTemp.dwt");
      try
      {
        ((IAcadDocument) acadDocument).SaveAs(strFileName, (object) (AcSaveAsType) 64, Type.Missing);
      }
      catch
      {
        tempDrawing = false;
        retry = true;
      }
      if (!retry)
      {
        this.TurnOffSysVars();
        try
        {
          Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
          using (mdiActiveDocument.LockDocument())
          {
            Database database = mdiActiveDocument.Database;
            Application.SetSystemVariable("PERSPECTIVE", (object) 0);
            if (this.OrthoDwgDatabase != null)
            {
              database.Lunits = this.OrthoDwgDatabase.Lunits;
              database.Luprec = this.OrthoDwgDatabase.Luprec;
              database.Aunits = this.OrthoDwgDatabase.Aunits;
              database.Auprec = this.OrthoDwgDatabase.Auprec;
              if (database.Dimzin == 0)
                database.Dimzin = 3;
            }
            ObjectId tempLockedLayer = this.CreateTempLockedLayer(mdiActiveDocument);
            mdiActiveDocument.Database.DisableUndoRecording(true);
            using (Transaction t = database.TransactionManager.StartTransaction())
            {
              // Suppress logs
              object oldCmdEcho = Application.GetSystemVariable("CMDECHO");
              Application.SetSystemVariable("CMDECHO", 0);
              
              foreach (string dwg3dFile in dwg3dFiles)
              {
                if (File.Exists(dwg3dFile))
                {
                  string fileName = Path.GetFileName(dwg3dFile);
                  ObjectId objectId = database.AttachXref(dwg3dFile, fileName);
                  try
                  {
                    BlockTableRecord blockTableRecord = (BlockTableRecord) t.GetObject(objectId, (OpenMode) 1);
                    blockTableRecord.Origin = Point3d.Origin;
                    blockTableRecord.Dispose();
                  }
                  catch
                  {
                  }
                  BlockTable blockTable = (BlockTable) t.GetObject(database.BlockTableId, (OpenMode) 0);
                  BlockTableRecord blockTableRecord1 = (BlockTableRecord) t.GetObject(((SymbolTable) blockTable)[BlockTableRecord.ModelSpace], (OpenMode) 1);
                  using (BlockReference bref = new BlockReference(Point3d.Origin, objectId))
                  {
                    bref.Rotation = rotation;
                    blockTableRecord1.AppendEntity((Entity) bref);
                    t.AddNewlyCreatedDBObject((DBObject) bref, true);
                    if (!tempLockedLayer.IsNull)
                      ((Entity) bref).LayerId = tempLockedLayer;
                    UIUtils.AddAecXdata(bref, t);
                  }
                }
              }
              // Restore logs
              Application.SetSystemVariable("CMDECHO", oldCmdEcho);
              LayerTable layerTable = (LayerTable) t.GetObject(database.LayerTableId, (OpenMode) 0);
              if (this.m_FrozenDwgLayers != null)
              {
                foreach (string frozenDwgLayer in this.m_FrozenDwgLayers)
                {
                  if (((SymbolTable) layerTable).Has(frozenDwgLayer))
                  {
                    try
                    {
                      ((LayerTableRecord) t.GetObject(((SymbolTable) layerTable)[frozenDwgLayer], (OpenMode) 1)).IsFrozen = true;
                    }
                    catch
                    {
                    }
                  }
                }
              }
              if (this.m_OffDwgLayers != null)
              {
                foreach (string offDwgLayer in this.m_OffDwgLayers)
                {
                  if (((SymbolTable) layerTable).Has(offDwgLayer))
                  {
                    try
                    {
                      ((LayerTableRecord) t.GetObject(((SymbolTable) layerTable)[offDwgLayer], (OpenMode) 1)).IsOff = true;
                    }
                    catch
                    {
                    }
                  }
                }
              }
              t.Commit();
            }
          }
        }
        catch
        {
          tempDrawing = false;
        }
      }
      return tempDrawing;
    }

    private void SetView(Extents3d ext, Vector3d viewDir, Vector3d upVector)
    {
      Point3d point3d1 = ext.MaxPoint;
      double x1 = point3d1.X;
      point3d1 = ext.MinPoint;
      double x2 = point3d1.X;
      double val1 = Math.Abs(x1 - x2);
      Point3d point3d2 = ext.MaxPoint;
      double y1 = point3d2.Y;
      point3d2 = ext.MinPoint;
      double y2 = point3d2.Y;
      double num = Math.Abs(y1 - y2);
      Point3d point3d3 = ext.MaxPoint;
      double z1 = point3d3.Z;
      point3d3 = ext.MinPoint;
      double z2 = point3d3.Z;
      double val2_1 = Math.Abs(z1 - z2);
      double val2_2 = num;
      Vector3d vector3d1 = val2_1 * viewDir.GetNormal();
      Vector3d vector3d2 = viewDir.CrossProduct(upVector);
      vector3d2 = vector3d2.CrossProduct(viewDir);
      Vector3d normal = vector3d2.GetNormal();
      CommonUIUtils.acppSetViewDir((ValueType) (object) vector3d1, (ValueType) (object) normal);
      CommonUIUtils.acppZoomExtents((ValueType) (object) ext);
    }

    protected void ReadLayersInCubeDwg()
    {
      try
      {
        if ((short) Application.GetSystemVariable("PlantOrthoUseModelLayerStates") != (short) 0)
        {
          this.m_FrozenDwgLayers = (StringCollection) null;
          this.m_OffDwgLayers = (StringCollection) null;
          return;
        }
      }
      catch
      {
      }
      this.m_FrozenDwgLayers = new StringCollection();
      this.m_OffDwgLayers = new StringCollection();
      try
      {
        Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
        if (mdiActiveDocument == null)
          return;
        using (mdiActiveDocument.LockDocument())
        {
          Database database = mdiActiveDocument.Database;
          using (Transaction transaction = (Transaction) database.TransactionManager.StartOpenCloseTransaction())
          {
            foreach (ObjectId objectId in (SymbolTable) transaction.GetObject(database.LayerTableId, (OpenMode) 0))
            {
              LayerTableRecord layerTableRecord = (LayerTableRecord) transaction.GetObject(objectId, (OpenMode) 0);
              if (((SymbolTableRecord) layerTableRecord).IsDependent && ((SymbolTableRecord) layerTableRecord).IsResolved || string.Compare(((SymbolTableRecord) layerTableRecord).Name, "0") == 0)
              {
                if (layerTableRecord.IsFrozen)
                  this.m_FrozenDwgLayers.Add(((SymbolTableRecord) layerTableRecord).Name);
                if (layerTableRecord.IsOff)
                  this.m_OffDwgLayers.Add(((SymbolTableRecord) layerTableRecord).Name);
              }
            }
            transaction.Commit();
          }
        }
      }
      catch
      {
      }
    }

    private void GenerateOrthos(
      StringCollection dwg3dFiles,
      string strViewName,
      string strViewType,
      Point3d lowerLeftPt,
      Point3d upperRightPt,
      Commands.ViewType viewType,
      Point3d ptIns,
      Vector3d viewDir,
      Vector3d upVector,
      ObjectId viewportId,
      double viewportWidth,
      double viewportHeight,
      double viewportDepth,
      double scale,
      double viewportRotation,
      Solid3d cubeSolid)
    {
      try
      {
        if (this.dwgSheetDefs != null)
          this.dwgSheetDefs.ClearSaveSet();
        this.m_dwgDatabase = Application.DocumentManager.MdiActiveDocument.Database;
        IDwgSheet dwgSheet = Dwg2dDbx.GetSheet(this.m_dwgDatabase);
        if (dwgSheet == null && !UIUtils.CreateDwgSheet(this.m_dwgDatabase, out dwgSheet))
        {
          this.m_orthoDocEditor.WriteMessage("Error on creating dwg Sheet");
          throw new Exception();
        }
        IDwgSheetDef dwgSheetDef = (IDwgSheetDef) null;
        UIUtils.GetDwgSheetFromOrthoView(this, out dwgSheetDef);
        if (!this.IsEditingView && !UIUtils.CreateDwgView(dwgSheet, strViewName, viewportId, out IDwgView _))
        {
          this.m_orthoDocEditor.WriteMessage("Error on creating dwg view");
          throw new Exception();
        }
        string str = (string) this.orthoProjectpart.DwtTemplateFile.Clone();
        if (string.IsNullOrEmpty(str))
          File.Copy(this.strCurrentDwgName, str);
        string directoryName = Path.GetDirectoryName(str);
        if (string.IsNullOrEmpty(directoryName) || !Directory.Exists(directoryName))
          str = string.Format("{0}\\{1}", (object) this.orthoProjectpart.ProjectDwgDirectory, (object) str);
        if (dwgSheetDef == null)
        {
          this.m_orthoDocEditor.WriteMessage("Error on getting sheet def");
          throw new Exception();
        }
        dwgSheetDef.DwgTemplateName = str;
        IDwgViewDef dwgViewDef = (IDwgViewDef) null;
        if (dwgViewDef == null && !UIUtils.CreateDwgViewDef(dwgSheetDef, strViewName, this.dwgSheetDefs, viewportDepth, viewDir, upVector, ptIns, viewportWidth, viewportHeight, out dwgViewDef))
        {
          this.m_orthoDocEditor.WriteMessage("Error on creating View Def");
          throw new Exception();
        }
        if (dwgViewDef != null)
        {
          dwgViewDef.OCCenterPoint = (lowerLeftPt + upperRightPt.GetAsVector()) / 2.0;
          dwgViewDef.OCHeight = Math.Abs(upperRightPt.Z - lowerLeftPt.Z);
          dwgViewDef.OCLength = Math.Abs(upperRightPt.X - lowerLeftPt.X);
          dwgViewDef.OCWidth = Math.Abs(upperRightPt.Y - lowerLeftPt.Y);
          dwgViewDef.ViewScale = scale;
          dwgViewDef.ViewType = strViewType;
          dwgViewDef.ViewCreationTime = DateTime.Now.ToUniversalTime().ToString();
        }
        IDwgQueryByBox dwgQueryByBox;
        if (!UIUtils.CreateDwgQueryByBox(dwgViewDef, this.dwgSheetDefs, lowerLeftPt, upperRightPt, out dwgQueryByBox))
        {
          this.m_orthoDocEditor.WriteMessage("Error on creating query box");
          throw new Exception();
        }
        ArrayList modelFiles = new ArrayList();
        Extents3d exts;
        UIUtils.UpdateFileCollectionWithXrefs(dwg3dFiles, ref modelFiles, out exts);
        ((IDwgQuery) dwgQueryByBox).ModelFiles = (ModelFile[]) modelFiles.ToArray(typeof (ModelFile));
        IDwgSetClipper clipper = cubeSolid == null ? Dwg2dGen.NewDwgSetClipper(dwgViewDef, UIUtils.GetClipInlateBy()) : Dwg2dGen.NewDwgSetClipper(cubeSolid, UIUtils.GetClipInlateBy());
        bool skipClipping = false;
        if (this.commandType == UIUtils.EditType.Create)
        {
          // ISSUE: explicit constructor call
          exts = new Extents3d();
          exts.AddPoint(this.m_lowerLeftPt);
          exts.AddPoint(this.m_upperRightPt);
        }
        this.dwgSheetDefs.Add(dwgSheetDef);
        dwgSheetDef.DwgViewDefs.Add(dwgViewDef);
        UIUtils.CreateOrUpdateOrthoView(dwgSheetDef, strViewName, this.orthoProjectpart, skipClipping, false, clipper, viewportRotation, this.m_FrozenDwgLayers, this.m_OffDwgLayers, this.Offset, this.Rotation);
        this.m_viewportManager.AnnotateViewport(viewportId);
      }
      catch (OutOfMemoryException ex)
      {
        throw new OutOfMemoryException(ex.Message);
      }
      catch (Exception ex)
      {
        this.Print(ex.Message);
        if (this.dwgSheetDefs == null)
          return;
        this.dwgSheetDefs.RejectChanges();
      }
    }



    private void BlockRequired(Document doc)
    {
    }

    private bool ProcessViewportPlacement(
      double viewportWidth,
      double viewportHeight,
      double scale,
      out double newScale,
      out ObjectId viewportId,
      out Point3d ptIns,
      out double newVPWidth,
      out double newVPHeight,
      out double newVPScale,
      out double rotationAngle)
    {
        return m_viewportManager.ProcessViewportPlacement(viewportWidth, viewportHeight, scale, out newScale, out viewportId, out ptIns, out newVPWidth, out newVPHeight, out newVPScale, out rotationAngle);
    }

    public void TurnOffSysVars()
    {
      bool regenDwgOnVarChange = PlantOrthoView.settings.RegenDwgOnVarChange;
      try
      {
        this.m_sysVars = new StringCollection();
        if ((short) Application.GetSystemVariable("PLANTPIPESILHDISPLAY") != (short) 0)
        {
          this.m_sysVars.Add("PLANTPIPESILHDISPLAY");
          Application.SetSystemVariable("PLANTPIPESILHDISPLAY", (object) 0);
        }
        if ((short) Application.GetSystemVariable("PLANTPROPMISMATCHDISPLAY") != (short) 0)
        {
          this.m_sysVars.Add("PLANTPROPMISMATCHDISPLAY");
          Application.SetSystemVariable("PLANTPROPMISMATCHDISPLAY", (object) 0);
        }
        if ((short) Application.GetSystemVariable("PLANTPORTDISPLAY") != (short) 0)
        {
          this.m_sysVars.Add("PLANTPORTDISPLAY");
          Application.SetSystemVariable("PLANTPORTDISPLAY", (object) 0);
        }
        if ((short) Application.GetSystemVariable("PLANTWELDDISPLAY") != (short) 0)
        {
          this.m_sysVars.Add("PLANTWELDDISPLAY");
          Application.SetSystemVariable("PLANTWELDDISPLAY", (object) 0);
        }
        this.m_insulationDisplay = PlantOrthoView.settings.DisplayInsulation;
        this.m_markerDisplay = PlantOrthoView.settings.DisplayMarkers;
        this.m_placeHolderDisplay = PlantOrthoView.settings.DisplayPlaceholder;
        PlantOrthoView.settings.RegenDwgOnVarChange = false;
        if (this.m_markerDisplay)
          PlantOrthoView.settings.DisplayMarkers = false;
        if (this.m_placeHolderDisplay)
          PlantOrthoView.settings.DisplayPlaceholder = false;
        if (this.m_insulationDisplay)
          return;
        try
        {
          if ((short) Application.GetSystemVariable("PLANTORTHOINSULATIONMODE") <= (short) 0)
            return;
          PlantOrthoView.settings.DisplayInsulation = true;
        }
        catch
        {
        }
      }
      catch (Exception ex)
      {
      }
      finally
      {
        PlantOrthoView.settings.RegenDwgOnVarChange = regenDwgOnVarChange;
      }
    }

    public void ResetSysVars()
    {
      bool regenDwgOnVarChange = PlantOrthoView.settings.RegenDwgOnVarChange;
      try
      {
        if (this.m_sysVars != null)
        {
          for (int index = 0; index < this.m_sysVars.Count; ++index)
            Application.SetSystemVariable(this.m_sysVars[index], (object) 1);
          this.m_sysVars.Clear();
        }
        PlantOrthoView.settings.RegenDwgOnVarChange = false;
        if (this.m_markerDisplay)
        {
          this.m_markerDisplay = false;
          PlantOrthoView.settings.DisplayMarkers = true;
        }
        if (this.m_placeHolderDisplay)
        {
          this.m_placeHolderDisplay = false;
          PlantOrthoView.settings.DisplayPlaceholder = true;
        }
        if (this.m_insulationDisplay == PlantOrthoView.settings.DisplayInsulation)
          return;
        PlantOrthoView.settings.DisplayInsulation = this.m_insulationDisplay;
      }
      catch (Exception ex)
      {
      }
      finally
      {
        PlantOrthoView.settings.RegenDwgOnVarChange = regenDwgOnVarChange;
      }
    }
  }
}
