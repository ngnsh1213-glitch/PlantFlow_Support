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
    private System.Collections.Generic.List<ObjectId> m_cubeSolidIds;
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
    private Point3d m_currentPaperCenter = new Point3d(380.0, 330.0, 0.0);
    private bool m_orthoContinueQueued = false;
    private bool m_anySuccessView = false;
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
        if (!this.SelectOrthoDwg(ref mdiActiveDocument))
        {
          this.m_bSuccess = false;
          this.PoDiag("InitializeProjectData: Ortho DWG 생성/선택 실패 model3d='" + this.strCurrent3DDwgName + "'");
          return;
        }
        this.m_orthoDocEditor = mdiActiveDocument.Editor;
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

    // 진단 전용 로거(로그만, 로직 무변경). 파일로도 tee.
    private void PoDiag(string m)
    {
        FileDiag(m);
        try { Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\n[PFS-DIAG] " + m); }
        catch (Exception _dx) { System.Diagnostics.Debug.WriteLine("[PFS-DIAG] " + _dx.Message); }
    }

    // 파일 진단 sink(문서 닫힘/컨텍스트 전환 무관 캡처). 실패는 Debug로만.
    internal static void FileDiag(string m)
    {
        try { System.IO.File.AppendAllText(@"C:\Temp\pfs_diag.log", System.DateTime.Now.ToString("HH:mm:ss.fff") + " [PFS-DIAG] " + m + System.Environment.NewLine); }
        catch (Exception _fx) { System.Diagnostics.Debug.WriteLine("[PFS-DIAG] FileDiag 실패: " + _fx.Message); }
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
        try
        {
            var _e = PlantOrthoView.SPInfo.Extents;
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?
                .Editor.WriteMessage("\n[PFS-DIAG] engine SPInfo.Extents min=" + _e.MinPoint + " max=" + _e.MaxPoint
                + " viewType=" + PlantOrthoView.SPInfo.ViewType + " viewDir=" + PlantOrthoView.SPInfo.ViewDirection
                + " name=" + PlantOrthoView.SPInfo.Name);
        }
        catch (Exception _ex)
        {
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?
                .Editor.WriteMessage("\n[PFS-DIAG] engine SPInfo.Extents 접근 예외(미설정?): " + _ex.Message);
        }
        this.CreateView(PlantOrthoView.SPInfo.Extents);
      }
      catch (Exception _ex)
      {
        Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?
            .Editor.WriteMessage("\n[PFS-DIAG] engine run/CreateView 예외(원래 전파 보존): " + _ex.Message);
        throw; // 원본은 try/finally(전파). 진단 로그만 남기고 전파 유지 — 로직 무변경.
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
      this.EnsureViewSpecs();
      this.ScheduleCreateOrthoFromCubePhase2();
    }

    private void EnsureViewSpecs()
    {
      if (PlantOrthoView.SPInfo == null)
        return;
      if (PlantOrthoView.SPInfo.ViewSpecs != null && PlantOrthoView.SPInfo.ViewSpecs.Count > 0)
        return;

      PlantOrthoView.SPInfo.ViewSpecs = new List<ViewSpec>();
      PlantOrthoView.SPInfo.ViewSpecs.Add(new ViewSpec()
      {
        ViewName = PlantOrthoView.SPInfo.ViewType.ToString(),
        ViewType = PlantOrthoView.SPInfo.ViewType,
        UCS = PlantOrthoView.SPInfo.UCS,
        ViewDirection = PlantOrthoView.SPInfo.ViewDirection,
        UpVector = PlantOrthoView.SPInfo.UpVector,
        PaperCenter = new Point3d(380.0, 330.0, 0.0)
      });
    }

    private Point3d GetPaperCenterForViewSpec(ViewSpec spec, double viewportWidth, double viewportHeight)
    {
      Point3d frontCenter = new Point3d(380.0, 330.0, 0.0);
      double gap = 40.0;
      double xStep = Math.Max(viewportWidth * 0.5, 180.0) + gap;
      double yStep = Math.Max(viewportHeight * 0.5, 180.0) + gap;
      string viewName = spec == null || string.IsNullOrEmpty(spec.ViewName) ? "Top" : spec.ViewName;

      if (string.Equals(viewName, "Front", StringComparison.OrdinalIgnoreCase))
        return frontCenter;
      if (string.Equals(viewName, "Top", StringComparison.OrdinalIgnoreCase))
        return new Point3d(frontCenter.X, frontCenter.Y + yStep, 0.0);
      if (string.Equals(viewName, "Bottom", StringComparison.OrdinalIgnoreCase))
        return new Point3d(frontCenter.X, frontCenter.Y - yStep, 0.0);
      if (string.Equals(viewName, "Right", StringComparison.OrdinalIgnoreCase))
        return new Point3d(frontCenter.X + xStep, frontCenter.Y, 0.0);
      if (string.Equals(viewName, "Left", StringComparison.OrdinalIgnoreCase))
        return new Point3d(frontCenter.X - xStep, frontCenter.Y, 0.0);
      if (string.Equals(viewName, "Back", StringComparison.OrdinalIgnoreCase))
        return new Point3d(frontCenter.X + (xStep * 2.0), frontCenter.Y, 0.0);
      return frontCenter;
    }

    private void ApplyViewSpecToCurrentState(ViewSpec spec)
    {
      if (spec == null)
        throw new ArgumentNullException("spec");

      this.m_strViewName = string.IsNullOrEmpty(spec.ViewName) ? spec.ViewType.ToString() : spec.ViewName;
      this.m_viewType = spec.ViewType;
      this.m_strViewType = this.m_strViewName;
      this.m_viewDir = spec.ViewDirection;
      this.m_upVector = spec.UpVector;
      this.m_currentPaperCenter = spec.PaperCenter;
      settings.OrthoView = this.m_strViewName;

      if (PlantOrthoView.SPInfo != null)
      {
        PlantOrthoView.SPInfo.ViewType = spec.ViewType;
        PlantOrthoView.SPInfo.ViewDirection = spec.ViewDirection;
        PlantOrthoView.SPInfo.UpVector = spec.UpVector;
        PlantOrthoView.SPInfo.UCS = spec.UCS;
      }
      this.PoDiag("ApplyViewSpec view='" + this.m_strViewName + "' viewType=" + this.m_viewType + " paperCenter=" + this.m_currentPaperCenter);
    }

    private bool SelectOrthoDwg(ref Document currentDoc)
    {
      string createdOrthoPath;
      if (!PlantOrthoView.cchNewProjectDwgRun("Ortho", false, out createdOrthoPath))
      {
        this.PoDiag("SelectOrthoDwg: cchNewProjectDwgRun 실패");
        return false;
      }
      Document orthoDoc = this.FindOpenDocument(createdOrthoPath);
      if (orthoDoc == null)
      {
        this.PoDiag("SelectOrthoDwg: 생성된 Ortho 문서를 열림 목록에서 찾지 못함 path='" + createdOrthoPath + "'");
        return false;
      }
      Application.DocumentManager.MdiActiveDocument = orthoDoc;
      currentDoc = orthoDoc;
      this.strCurrentDwgName = createdOrthoPath;
      this.m_2DWorkflow = false;
      if (this.orthoProjectpart is OrthoProject orthoProjectpart)
      {
        IDwgSheetDef dwgSheetDef = (IDwgSheetDef) null;
        if (UIUtils.GetDwgSheetFromOrthoDwg(orthoDoc.Database, out dwgSheetDef))
          orthoProjectpart.SetOrthoDwgVersion(dwgSheetDef, 1);
      }
      this.PoDiag("SelectOrthoDwg: PIPE 시작 공식 경로 Ortho 활성화 model3d='" + this.strCurrent3DDwgName + "' ortho='" + this.strCurrentDwgName + "'");
      return true;
    }

    private Document FindOpenDocument(string path)
    {
      string targetFullPath = Path.GetFullPath(path);
      string targetFileName = Path.GetFileName(path);
      foreach (Document document in Application.DocumentManager)
      {
        try
        {
          string docName = document.Name;
          if (string.Compare(docName, path, true) == 0)
            return document;
          if (string.Compare(Path.GetFullPath(docName), targetFullPath, true) == 0)
            return document;
          if (string.Compare(Path.GetFileName(docName), targetFileName, true) == 0)
            return document;
        }
        catch (Exception _dx)
        {
          System.Diagnostics.Debug.WriteLine("[PFS-DIAG] FindOpenDocument 비교 예외: " + _dx.Message);
        }
      }
      return null;
    }

    private bool EnsureCurrentOrthoDocument(string reason)
    {
      Document activeDoc = Application.DocumentManager.MdiActiveDocument;
      if (activeDoc != null && string.Compare(Path.GetFullPath(activeDoc.Name), Path.GetFullPath(this.CurrentOrthoDwgName), true) == 0)
      {
        this.m_orthoDocEditor = activeDoc.Editor;
        this.m_dwgDatabase = activeDoc.Database;
        return true;
      }
      this.PoDiag("EnsureCurrentOrthoDocument 검증 실패 reason='" + reason + "' active='" + (activeDoc == null ? "<null>" : activeDoc.Name) + "' ortho='" + this.CurrentOrthoDwgName + "'");
      return false;
    }

    private System.Collections.Generic.HashSet<ObjectId> SnapshotSourceModelSpaceIds(string reason)
    {
      var ids = new System.Collections.Generic.HashSet<ObjectId>();
      try
      {
        Document srcDoc = this.FindOpenDocument(this.strCurrent3DDwgName);
        if (srcDoc == null)
        {
          FileDiag("source cube snapshot skip reason='" + reason + "' source doc not found model3d='" + this.strCurrent3DDwgName + "'");
          return null;
        }
        using (srcDoc.LockDocument())
        using (Transaction tr = srcDoc.Database.TransactionManager.StartTransaction())
        {
          BlockTable bt = (BlockTable)tr.GetObject(srcDoc.Database.BlockTableId, OpenMode.ForRead);
          BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
          foreach (ObjectId id in ms)
            ids.Add(id);
          tr.Commit();
        }
        FileDiag("source cube snapshot reason='" + reason + "' count=" + ids.Count + " doc='" + srcDoc.Name + "'");
      }
      catch (Exception ex)
      {
        FileDiag("source cube snapshot 예외 reason='" + reason + "': " + ex.GetType().Name + ": " + ex.Message);
        return null;
      }
      return ids;
    }

    private void CaptureSourceCubeSolids(System.Collections.Generic.HashSet<ObjectId> beforeIds)
    {
      this.m_cubeSolidIds = new System.Collections.Generic.List<ObjectId>();
      try
      {
        if (beforeIds == null)
        {
          FileDiag("source cube capture skip: baseline snapshot unavailable");
          return;
        }
        Document srcDoc = this.FindOpenDocument(this.strCurrent3DDwgName);
        if (srcDoc == null)
        {
          FileDiag("source cube capture skip: source doc not found model3d='" + this.strCurrent3DDwgName + "'");
          return;
        }
        int newCount = 0;
        using (srcDoc.LockDocument())
        using (Transaction tr = srcDoc.Database.TransactionManager.StartTransaction())
        {
          BlockTable bt = (BlockTable)tr.GetObject(srcDoc.Database.BlockTableId, OpenMode.ForRead);
          BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
          foreach (ObjectId id in ms)
          {
            if (beforeIds.Contains(id))
              continue;
            newCount++;
            try
            {
              Solid3d solid = tr.GetObject(id, OpenMode.ForRead, false) as Solid3d;
              if (solid != null)
                this.m_cubeSolidIds.Add(id);
            }
            catch (Exception ix)
            {
              FileDiag("source cube capture 신규 id 조회 예외 id='" + id.ToString() + "': " + ix.GetType().Name + ": " + ix.Message);
            }
          }
          tr.Commit();
        }
        FileDiag("source cube capture newIds=" + newCount + " solidIds=" + this.m_cubeSolidIds.Count + " doc='" + srcDoc.Name + "'");
        if (this.m_cubeSolidIds.Count == 0)
          FileDiag("source cube capture solidIds=0: cube가 source model DB 외부에 생성됐을 가능성 있음");
      }
      catch (Exception ex)
      {
        FileDiag("source cube capture 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private void EraseSourceCubeSolids()
    {
      try
      {
        if (this.m_cubeSolidIds == null || this.m_cubeSolidIds.Count == 0)
        {
          FileDiag("source cube 정리 skip: tracked solidIds empty model3d='" + this.strCurrent3DDwgName + "'");
          return;
        }
        Document srcDoc = this.FindOpenDocument(this.strCurrent3DDwgName);
        if (srcDoc == null)
        {
          FileDiag("source cube 정리 skip: source doc not found model3d='" + this.strCurrent3DDwgName + "'");
          return;
        }
        int erased = 0;
        int failed = 0;
        using (srcDoc.LockDocument())
        using (Transaction tr = srcDoc.Database.TransactionManager.StartTransaction())
        {
          foreach (ObjectId id in this.m_cubeSolidIds)
          {
            try
            {
              Entity ent = tr.GetObject(id, OpenMode.ForWrite, false) as Entity;
              if (ent != null && !ent.IsErased)
              {
                ent.Erase();
                erased++;
              }
            }
            catch (Exception ix)
            {
              failed++;
              FileDiag("source cube 정리 개별 예외 id='" + id.ToString() + "': " + ix.GetType().Name + ": " + ix.Message);
            }
          }
          tr.Commit();
        }
        FileDiag("source cube 정리 erased=" + erased + " failed=" + failed + " tracked=" + this.m_cubeSolidIds.Count + " doc='" + srcDoc.Name + "'");
      }
      catch (Exception ex)
      {
        FileDiag("source cube 정리 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    public static bool cchNewProjectDwgRun(string prjType, bool mustClose = true)
    {
      string createdPath;
      return PlantOrthoView.cchNewProjectDwgRun(prjType, mustClose, out createdPath);
    }

    public static bool cchNewProjectDwgRun(string prjType, bool mustClose, out string createdPath)
    {
      createdPath = string.Empty;
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
      createdPath = path;
      Application.DocumentManager.MdiActiveDocument.SendStringToExecute("_.REFRESHPMESW\n", true, false, false);
      Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n" + str + "가 출력되었습니다. 저장 위치: " + path + "\n");
      return true;
    }

    public void CreateView(Extents3d Exts)
    {
      try { PoDiag("CreateView 진입 Exts min=" + Exts.MinPoint + " max=" + Exts.MaxPoint + " currentOrtho='" + this.CurrentOrthoDwgName + "'"); }
      catch (Exception _cvEntry) { PoDiag("CreateView Exts 접근 예외: " + _cvEntry.Message); }
      Document mdiActiveDocument = Application.DocumentManager.MdiActiveDocument;
      IDwgSheetDef dwgSheetDef = (IDwgSheetDef) null;
      bool _sheetOk = UIUtils.GetDwgSheetFromOrthoView(this, out dwgSheetDef);
      bool _sheetVersionOk = false;
      if (_sheetOk)
        _sheetVersionOk = UIUtils.ValidateOrthoDwgSheetVersion(this.orthoProjectpart, dwgSheetDef);
      PoDiag("CreateView sheet gate sheetOk=" + _sheetOk + " versionOk=" + _sheetVersionOk + " sheetDefNull=" + (dwgSheetDef == null));
      if (!_sheetOk || !_sheetVersionOk)
      {
        PoDiag("CreateView early return: sheet gate failed");
        return;
      }
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
        System.Collections.Generic.HashSet<ObjectId> _sourceCubeBefore = this.SnapshotSourceModelSpaceIds("before InitializeCube");
        this.m_plantOrthoCube = new PlantOrthoCube();
        this.m_plantOrthoCube.InitializeCube(ext);
        this.m_lowerLeftPt = ext.MinPoint;
        this.m_upperRightPt = ext.MaxPoint;
        this.SetView(ext, new Vector3d(-1.0, -1.0, 1.0), new Vector3d(0.0, 0.0, 1.0));
        if (string.IsNullOrEmpty(PlantOrthoView.settings.OrthoView))
          PlantOrthoView.settings.OrthoView = UIUtils.s_TopView;
        this.m_plantOrthoCube.SetOrthoViewFaceColor((Autodesk.ProcessPower.PnP3dOrthoDrawingsUI.Commands.ViewType)(int)UIUtils.GetViewTypeFromName(PlantOrthoView.settings.OrthoView));
        this.CaptureSourceCubeSolids(_sourceCubeBefore);
      }
      catch (Exception _cvx)
      {
        PoDiag("CreateView 예외(삼킴 유지): " + _cvx.Message);
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

    private void ScheduleCreateOrthoFromCubePhase2()
    {
      Commands.OrthoView = this;
      Document orthoDoc = this.FindOpenDocument(this.CurrentOrthoDwgName);
      if (orthoDoc == null)
      {
        this.PoDiag("Schedule phase2 실패: Ortho 문서를 찾지 못함 ortho='" + this.CurrentOrthoDwgName + "'");
        this.m_bSuccess = false;
        return;
      }
      this.PoDiag("Schedule phase2: orthoDoc.SendStringToExecute target='" + orthoDoc.Name + "'");
      orthoDoc.SendStringToExecute("._PFSORTHOPHASE2\n", true, false, false);
    }

    // continuation(PFSORTHOCONTINUE=CloseAndSave+PIPE재활성+배치진행)을 정확히 1회만 큐잉. 뷰 루프 종료 시 호출.
    private void QueueOrthoContinue(string reason)
    {
      if (this.m_orthoContinueQueued)
      {
        FileDiag("QueueOrthoContinue skip(중복) reason='" + reason + "'");
        return;
      }
      Document orthoDoc = this.FindOpenDocument(this.CurrentOrthoDwgName);
      if (orthoDoc == null)
      {
        FileDiag("QueueOrthoContinue 실패: ortho 문서 없음 ortho='" + this.CurrentOrthoDwgName + "' reason='" + reason + "'");
        return;
      }
      this.m_orthoContinueQueued = true;
      FileDiag("QueueOrthoContinue: PFSORTHOCONTINUE 큐잉 reason='" + reason + "' target='" + orthoDoc.Name + "' 성공뷰수기록됨");
      orthoDoc.SendStringToExecute("._PFSORTHOCONTINUE\n", true, false, false);
    }

    public void CreateOrthoFromCube()
    {
      this.OrthoCube.OnCloseDoc();
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
      // 큐브 Extents 캡처 완료 후 원본 큐브 정리(투영 전). 조기 삭제 시 OrthoCube.Extents 퇴화(1E+20)→투영 실패 회귀 방지.
      this.EraseSourceCubeSolids();
      this.CloseOrthoViewDoc();
      if (!this.EnsureCurrentOrthoDocument("CreateOrthoFromCube before CreateOrthos"))
        return;

      this.EnsureViewSpecs();
      List<ViewSpec> specs = PlantOrthoView.SPInfo == null ? null : PlantOrthoView.SPInfo.ViewSpecs;
      if (specs == null || specs.Count == 0)
      {
        FileDiag("CreateOrthoFromCube: specs 없음 — continuation만 큐잉 후 종료");
        this.QueueOrthoContinue("specs 없음");
        return;
      }

      try
      {
        for (int i = 0; i < specs.Count; i++)
        {
          ViewSpec spec = specs[i];
          // 단일뷰는 기존 중앙(380,330) 유지(회귀 방지). 3각법 오프셋은 다중뷰에서만 적용.
          spec.PaperCenter = specs.Count <= 1
            ? new Point3d(380.0, 330.0, 0.0)
            : this.GetPaperCenterForViewSpec(spec, this.m_dViewportWidth, this.m_dViewportHeight);
          this.ApplyViewSpecToCurrentState(spec);
          bool isLastView = i == specs.Count - 1;
          Solid3d cubeSolid = (Solid3d)null;
          try
          {
            using (Application.DocumentManager.MdiActiveDocument.LockDocument())
              cubeSolid = this.OrthoCube.MakeCubeCopy();
            if (!this.EnsureCurrentOrthoDocument("CreateOrthoFromCube loop view='" + this.m_strViewName + "'"))
            {
              this.PoDiag("CreateOrthoFromCube view skip(문서검증 실패) view='" + this.m_strViewName + "' — 다음 뷰 진행");
              continue;
            }
            this.PoDiag("CreateOrthoFromCube view loop " + (i + 1) + "/" + specs.Count + " view='" + this.m_strViewName + "' isLast=" + isLastView);
            this.CreateOrthos(this.SelectedModels, this.m_strViewName, this.m_dScale, this.m_dViewportWidth, this.m_dViewportHeight, this.m_dViewportDepth, this.m_viewType, this.m_strViewType, this.m_viewDir, this.m_upVector, this.m_lowerLeftCubePt, this.m_upperRightCubePt, cubeSolid, this.m_currentPaperCenter, isLastView);
            this.m_anySuccessView = true;
          }
          catch (Exception ex)
          {
            // 개별 뷰 예외는 기록하고 계속 진행해 continuation을 보장한다.
            this.PoDiag("CreateOrthoFromCube view loop 예외 view='" + this.m_strViewName + "': " + ex.Message);
          }
          finally
          {
            if (cubeSolid != null)
              cubeSolid.Dispose();
          }
        }
      }
      finally
      {
        FileDiag("CreateOrthoFromCube 뷰 루프 종료 anySuccessView=" + this.m_anySuccessView + " specCount=" + specs.Count);
        this.QueueOrthoContinue("뷰 루프 종료");
      }
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
      Solid3d cubeSolid,
      Point3d paperCenter,
      bool isLastView)
    {
      PoDiag("CreateOrthos 진입 selectedModels.Count=" + (selectedModels == null ? 0 : selectedModels.Count)
        + " viewName='" + strViewName + "' viewType='" + strViewType + "' lowerLeft=" + lowerLeftPt + " upperRight=" + upperRightPt
        + " cubeSolidNull=" + (cubeSolid == null));
      if (!this.EnsureCurrentOrthoDocument("CreateOrthos entry"))
        return;
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
          bool _unlockOk = UIUtils.UnlockViewportScale(true, out bLocked);
          PoDiag("CreateOrthos UnlockViewportScale result=" + _unlockOk + " bLocked=" + bLocked);
          if (!_unlockOk)
          {
            PoDiag("CreateOrthos early return: UnlockViewportScale failed");
            return;
          }
          double rotationAngle = 0.0;
          if (this.IsEditingView)
          {
            ptIns = Point3d.Origin;
          }
          else
          {
            double newVPScale = 0.0;
            bool _placementOk = this.ProcessViewportPlacement(viewportWidth, viewportHeight, scale, paperCenter, out newScale, out viewportId, out ptIns, out newVPWidth, out newVPHeight, out newVPScale, out rotationAngle);
            PoDiag("CreateOrthos ProcessViewportPlacement result=" + _placementOk + " viewportId=" + viewportId
              + " ptIns=" + ptIns + " newVPWidth=" + newVPWidth + " newVPHeight=" + newVPHeight
              + " newVPScale=" + newVPScale + " rotationAngle=" + rotationAngle);
            if (!_placementOk)
            {
              PoDiag("CreateOrthos early return: ProcessViewportPlacement failed");
              return;
            }
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
          PoDiag("CreateOrthos GenerateOrthos 호출 직전 viewportId=" + viewportId + " viewportWidth=" + viewportWidth + " viewportHeight=" + viewportHeight + " scale=" + scale);
          this.GenerateOrthos(selectedModels, strViewName, strViewType, lowerLeftPt, upperRightPt, viewType, ptIns, viewDir, upVector, viewportId, viewportWidth, viewportHeight, viewportDepth, scale, rotationAngle, cubeSolid, isLastView);
          PoDiag("CreateOrthos GenerateOrthos 호출 완료");
          CommonUIUtils.acedPostCommandPrompt();
        }
        catch (Exception _cox)
        {
          PoDiag("CreateOrthos 예외(삼킴 유지): " + _cox.Message);
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
              
              PoDiag("AttachLoop dwg3dFiles count=" + (dwg3dFiles == null ? 0 : dwg3dFiles.Count));
              foreach (string dwg3dFile in dwg3dFiles)
              {
                bool _exists = File.Exists(dwg3dFile);
                PoDiag("attach file='" + dwg3dFile + "' exists=" + _exists);
                if (_exists)
                {
                  string fileName = Path.GetFileName(dwg3dFile);
                  ObjectId objectId = database.AttachXref(dwg3dFile, fileName);
                  PoDiag("attached '" + fileName + "' xrefId=" + objectId.ToString());
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
                  PoDiag("FrozenLayer=" + frozenDwgLayer);
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
                  PoDiag("OffLayer=" + offDwgLayer);
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
              // [PFS-DIAG 2-3] attach 후 오쏘 DB xref 상태 열람(읽기 전용, t 아직 활성).
              try
              {
                Autodesk.AutoCAD.DatabaseServices.XrefGraph _g = database.GetHostDwgXrefGraph(true);
                int _n = ((Autodesk.AutoCAD.DatabaseServices.Graph) _g).NumNodes;
                PoDiag("orthoXref nodes=" + _n);
                for (int _i = 0; _i < _n; _i++)
                {
                  Autodesk.AutoCAD.DatabaseServices.XrefGraphNode _node = _g.GetXrefNode(_i);
                  if (_node == null || _node.BlockTableRecordId.IsNull) continue;
                  BlockTableRecord _btr = t.GetObject(_node.BlockTableRecordId, (OpenMode) 0) as BlockTableRecord;
                  if (_btr != null)
                    PoDiag("orthoXref path='" + _btr.PathName + "' status=" + _btr.XrefStatus + " unloaded=" + _btr.IsUnloaded);
                }
              }
              catch (Exception _gx) { PoDiag("orthoXref 열람 예외: " + _gx.Message); }
              t.Commit();
            }
          }
        }
        catch (Exception _ex)
        {
          PoDiag("attach 블록 예외(삼킴 유지): " + _ex.Message);
          tempDrawing = false;
        }
      }
      return tempDrawing;
    }

    private void SetView(Extents3d ext, Vector3d viewDir, Vector3d upVector)
    {
      try { PoDiag("SetView 진입 ext min=" + ext.MinPoint + " max=" + ext.MaxPoint + " viewDir=" + viewDir + " up=" + upVector); }
      catch (Exception _sx) { PoDiag("SetView ext 접근 예외: " + _sx.Message); }
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
      Solid3d cubeSolid,
      bool isLastView)
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
        try
        {
            var _in = new System.Collections.Generic.List<string>();
            if (dwg3dFiles != null) foreach (string _f in dwg3dFiles) _in.Add(_f);
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?
                .Editor.WriteMessage("\n[PFS-DIAG] Stage-P input dwg3dFiles.Count=" + (dwg3dFiles == null ? 0 : dwg3dFiles.Count)
                + " qbox lowerLeft=" + lowerLeftPt + " upperRight=" + upperRightPt
                + " files=[" + string.Join(" | ", _in) + "]");
        }
        catch (Exception _px) { System.Diagnostics.Debug.WriteLine("[PFS-DIAG] Stage-P input 로그 예외: " + _px.Message); }
        ArrayList modelFiles = new ArrayList();
        Extents3d exts;
        UIUtils.UpdateFileCollectionWithXrefs(dwg3dFiles, ref modelFiles, out exts);
        try
        {
            var _names = new System.Collections.Generic.List<string>();
            foreach (var _mf in modelFiles)
            {
                var _pn = _mf as ModelFile;
                _names.Add(_pn != null ? _pn.ModelFileName : _mf.ToString());
            }
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?
                .Editor.WriteMessage("\n[PFS-DIAG] Stage-P modelFiles.Count=" + modelFiles.Count
                + " exts min=" + exts.MinPoint + " max=" + exts.MaxPoint
                + " files=[" + string.Join(" | ", _names) + "]");
        }
        catch (Exception _px) { System.Diagnostics.Debug.WriteLine("[PFS-DIAG] Stage-P modelFiles 로그 예외: " + _px.Message); }
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
        FileDiag("GenerateOrthos: FlattenDiagSnapshot 호출 직전");
        var _flattenBaseline = this.FlattenDiagSnapshot();
        FileDiag("GenerateOrthos: CreateOrUpdateOrthoView 호출 직전 (baseline=" + _flattenBaseline.Count + ")");
        UIUtils.CreateOrUpdateOrthoView(dwgSheetDef, strViewName, this.orthoProjectpart, skipClipping, false, clipper, viewportRotation, this.m_FrozenDwgLayers, this.m_OffDwgLayers, this.Offset, this.Rotation);
        FileDiag("GenerateOrthos: CreateOrUpdateOrthoView 반환, FlattenDiagStage1 호출 직전");
        this.FlattenDiagStage1(viewportId, _flattenBaseline);
        FileDiag("GenerateOrthos: FlattenDiagStage1 반환, FlattenDiagStage2 호출 직전");
        this.FlattenDiagStage2(_flattenBaseline);
        FileDiag("GenerateOrthos: FlattenDiagStage2 반환, AnnotateViewport 호출 직전");
        this.m_viewportManager.AnnotateViewport(viewportId, isLastView);
        FileDiag("GenerateOrthos: AnnotateViewport 반환");
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



    // Stage1: generator 전 baseline 스냅샷(paper+model space ObjectId 집합).
    private System.Collections.Generic.HashSet<ObjectId> FlattenDiagSnapshot()
    {
      var set = new System.Collections.Generic.HashSet<ObjectId>();
      try
      {
        Document doc = Application.DocumentManager.MdiActiveDocument;
        if (doc == null)
        {
          System.Diagnostics.Debug.WriteLine("[PFS-DIAG] FlattenDiagSnapshot: doc null");
          return set;
        }
        Database db = doc.Database;
        FileDiag("FlattenDiagSnapshot enter (상위 락 사용, 자체 LockDocument 없음)");
        // 상위 CreateOrthos가 이미 문서 잠금 보유 → 자체 LockDocument 제거(Session 컨텍스트 중첩 데드락 방지).
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          BlockTable bt = (BlockTable) tr.GetObject(db.BlockTableId, OpenMode.ForRead);
          foreach (string sp in new string[] { BlockTableRecord.PaperSpace, BlockTableRecord.ModelSpace })
          {
            BlockTableRecord btr = (BlockTableRecord) tr.GetObject(bt[sp], OpenMode.ForRead);
            foreach (ObjectId id in btr)
              set.Add(id);
          }
          tr.Commit();
        }
        FileDiag("FlattenDiagSnapshot exit count=" + set.Count);
      }
      catch (Exception ex)
      {
        FileDiag("FlattenDiagSnapshot 예외: " + ex.Message);
        System.Diagnostics.Debug.WriteLine("[PFS-DIAG] FlattenDiagSnapshot 예외: " + ex.Message);
      }
      return set;
    }

    // Stage1: generator 후 재열거 -> baseline에 없는 신규 엔티티(=엔진 산출물)만 보고(로그 전용).
    private void FlattenDiagStage1(ObjectId viewportId, System.Collections.Generic.HashSet<ObjectId> baseline)
    {
      try
      {
        Document doc = Application.DocumentManager.MdiActiveDocument;
        if (doc == null)
        {
          System.Diagnostics.Debug.WriteLine("[PFS-DIAG] FlattenDiagStage1: doc null");
          return;
        }
        Editor ed = doc.Editor;
        Database db = doc.Database;
        if (baseline == null)
          baseline = new System.Collections.Generic.HashSet<ObjectId>();
        FileDiag("FlattenDiagStage1 enter (상위 락 사용, 자체 LockDocument 없음)");
        // 상위 CreateOrthos가 이미 문서 잠금 보유 → 자체 LockDocument 제거(데드락 방지).
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          BlockTable bt = (BlockTable) tr.GetObject(db.BlockTableId, OpenMode.ForRead);
          foreach (string sp in new string[] { BlockTableRecord.PaperSpace, BlockTableRecord.ModelSpace })
          {
            var typeCount = new System.Collections.Generic.Dictionary<string, int>();
            var layerCount = new System.Collections.Generic.Dictionary<string, int>();
            var blockNames = new System.Collections.Generic.List<string>();
            var samples = new System.Collections.Generic.List<string>();
            int newCount = 0;
            BlockTableRecord btr = (BlockTableRecord) tr.GetObject(bt[sp], OpenMode.ForRead);
            foreach (ObjectId id in btr)
            {
              if (baseline.Contains(id))
                continue;
              Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
              if (ent == null)
                continue;
              newCount++;
              string tn = ent.GetType().Name;
              typeCount[tn] = (typeCount.ContainsKey(tn) ? typeCount[tn] : 0) + 1;
              string ln = ent.Layer;
              layerCount[ln] = (layerCount.ContainsKey(ln) ? layerCount[ln] : 0) + 1;
              BlockReference br = ent as BlockReference;
              if (br != null)
              {
                try
                {
                  BlockTableRecord def = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                  blockNames.Add((def != null ? def.Name : "?") + "[" + br.GetType().FullName + "]");
                }
                catch (Exception _bx)
                {
                  blockNames.Add("?(blockNameEx=" + _bx.Message + ")[" + br.GetType().FullName + "]");
                }
              }
              if (samples.Count < 8)
                samples.Add(tn + ":" + ent.Handle.ToString() + ":" + ln);
            }
            var tp = new System.Collections.Generic.List<string>();
            foreach (var kv in typeCount)
              tp.Add(kv.Key + "=" + kv.Value);
            var lp = new System.Collections.Generic.List<string>();
            foreach (var kv in layerCount)
              lp.Add(kv.Key + "=" + kv.Value);
            string _line = "FlattenDiag NEW " + sp + " count=" + newCount
              + " types={" + string.Join(", ", tp) + "}"
              + " layers={" + string.Join(", ", lp) + "}"
              + " blockRefs=[" + string.Join(" | ", blockNames) + "]"
              + " samples=[" + string.Join(" | ", samples) + "]";
            FileDiag(_line);
            ed?.WriteMessage("\n[PFS-DIAG] " + _line);
          }
          FileDiag("FlattenDiagStage1 space 열거 완료, viewport 조회 직전");
          try
          {
            if (!viewportId.IsNull)
            {
              Viewport vp = tr.GetObject(viewportId, OpenMode.ForRead) as Viewport;
              if (vp != null)
              {
                FileDiag("FlattenDiag viewport On=" + vp.On + " CustomScale=" + vp.CustomScale);
                ed?.WriteMessage("\n[PFS-DIAG] FlattenDiag viewport On=" + vp.On
                  + " CustomScale=" + vp.CustomScale);
              }
            }
          }
          catch (Exception _vx)
          {
            FileDiag("FlattenDiag viewport 접근 예외: " + _vx.Message);
            ed?.WriteMessage("\n[PFS-DIAG] FlattenDiag viewport 접근 예외: " + _vx.Message);
          }
          tr.Commit();
        }
        FileDiag("FlattenDiagStage1 exit(정상)");
      }
      catch (Exception ex)
      {
        FileDiag("FlattenDiag 예외: " + ex.GetType().Name + ": " + ex.Message);
        try { Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\n[PFS-DIAG] FlattenDiag 예외: " + ex.GetType().Name + ": " + ex.Message); }
        catch (Exception _dx) { System.Diagnostics.Debug.WriteLine("[PFS-DIAG] " + _dx.Message); }
      }
    }

    // Stage2: 오쏘 엔진 산출 익명 블록(*U##)을 비파괴 explode -> 2D 엔티티를 PFS_ORTHO_FLATTEN 레이어에 복사(실증).
    private void FlattenDiagStage2(System.Collections.Generic.HashSet<ObjectId> baseline)
    {
      try
      {
        Document doc = Application.DocumentManager.MdiActiveDocument;
        if (doc == null)
        {
          FileDiag("FlattenDiagStage2: doc null");
          return;
        }
        Database db = doc.Database;
        if (baseline == null)
          baseline = new System.Collections.Generic.HashSet<ObjectId>();
        FileDiag("FlattenDiagStage2 enter (상위 락 사용, 자체 LockDocument 없음)");
        // 상위 CreateOrthos가 문서 잠금 보유 -> 자체 LockDocument 금지(hang 방지).
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          ObjectId flatLayerId = ObjectId.Null;
          LayerTable lt = (LayerTable) tr.GetObject(db.LayerTableId, OpenMode.ForRead);
          const string flatLayer = "PFS_ORTHO_FLATTEN";
          if (lt.Has(flatLayer))
          {
            flatLayerId = lt[flatLayer];
          }
          else
          {
            lt.UpgradeOpen();
            LayerTableRecord ltr = new LayerTableRecord();
            ltr.Name = flatLayer;
            ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
            flatLayerId = lt.Add(ltr);
            tr.AddNewlyCreatedDBObject(ltr, true);
          }

          BlockTable bt = (BlockTable) tr.GetObject(db.BlockTableId, OpenMode.ForRead);
          BlockTableRecord ms = (BlockTableRecord) tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

          var targets = new System.Collections.Generic.List<ObjectId>();
          foreach (ObjectId id in ms)
          {
            if (baseline.Contains(id))
              continue;
            BlockReference br = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
            if (br == null)
              continue;
            BlockTableRecord def = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
            string nm = def != null ? def.Name : "";
            if (nm.StartsWith("*"))
              targets.Add(id);
          }
          FileDiag("FlattenDiagStage2 대상 익명블록 수=" + targets.Count);

          var typeCount = new System.Collections.Generic.Dictionary<string, int>();
          int appended = 0;
          int nested = 0;
          int failed = 0;
          foreach (ObjectId id in targets)
          {
            BlockReference br = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
            if (br == null)
              continue;
            ExplodeRecursive(br, ms, tr, flatLayerId, 0, typeCount, ref appended, ref nested, ref failed);
          }

          var tp = new System.Collections.Generic.List<string>();
          foreach (var kv in typeCount)
            tp.Add(kv.Key + "=" + kv.Value);
          FileDiag("FlattenDiagStage2 결과 append(leaf 2D)=" + appended + " nested블록=" + nested
            + " explode실패=" + failed + " leaf타입={" + string.Join(", ", tp) + "}");
          tr.Commit();
        }
        FileDiag("FlattenDiagStage2 exit(정상)");
      }
      catch (Exception ex)
      {
        FileDiag("FlattenDiagStage2 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    // 재귀 explode: leaf 2D 엔티티는 PFS_ORTHO_FLATTEN에 append, 중첩 블록은 재귀. 비-append 객체는 Dispose.
    private void ExplodeRecursive(Entity ent, BlockTableRecord ms, Transaction tr, ObjectId layerId,
      int depth, System.Collections.Generic.Dictionary<string, int> typeCount,
      ref int appended, ref int nested, ref int failed)
    {
      if (depth > 12)
      {
        FileDiag("ExplodeRecursive 깊이 초과(12)");
        return;
      }
      DBObjectCollection col = new DBObjectCollection();
      try
      {
        ent.Explode(col);
      }
      catch (Exception ex)
      {
        failed++;
        FileDiag("Explode 실패(depth=" + depth + "): " + ex.Message);
        return;
      }
      foreach (DBObject o in col)
      {
        Entity e = o as Entity;
        if (e == null)
        {
          o.Dispose();
          continue;
        }
        if (e is BlockReference nb)
        {
          nested++;
          ExplodeRecursive(nb, ms, tr, layerId, depth + 1, typeCount, ref appended, ref nested, ref failed);
          nb.Dispose();
        }
        else if (e is AttributeDefinition)
        {
          e.Dispose();
        }
        else
        {
          string tn = e.GetType().Name;
          typeCount[tn] = typeCount.ContainsKey(tn) ? typeCount[tn] + 1 : 1;
          try
          {
            e.LayerId = layerId;
            ms.AppendEntity(e);
            tr.AddNewlyCreatedDBObject(e, true);
            appended++;
          }
          catch (Exception ax)
          {
            failed++;
            FileDiag("append 실패 " + tn + ": " + ax.Message);
            e.Dispose();
          }
        }
      }
    }
    private void BlockRequired(Document doc)
    {
    }

    private bool ProcessViewportPlacement(
      double viewportWidth,
      double viewportHeight,
      double scale,
      Point3d paperCenter,
      out double newScale,
      out ObjectId viewportId,
      out Point3d ptIns,
      out double newVPWidth,
      out double newVPHeight,
      out double newVPScale,
      out double rotationAngle)
    {
        return m_viewportManager.ProcessViewportPlacement(viewportWidth, viewportHeight, scale, paperCenter, out newScale, out viewportId, out ptIns, out newVPWidth, out newVPHeight, out newVPScale, out rotationAngle);
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
