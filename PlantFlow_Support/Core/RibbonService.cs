using System;
using System.IO;
using System.Windows.Media.Imaging;
using Autodesk.Windows;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AcAppFull = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PlantFlow_Support
{
  /// <summary>
  /// PFS 리본 패널을 코드로 등록한다(CUIX 불요). PFO RibbonService에서 이식했다.
  /// Initialize 시점엔 ComponentManager.Ribbon이 null일 수 있어 Application.Idle 1회로 지연 생성한다.
  /// </summary>
  public static class RibbonService
  {
    // [제품군 공유] 탭 Id는 PlantFlow 제품 공용이다(PFO와 동일 문자열).
    // 각 제품은 "탭을 찾거나 생성"하고 "자기 패널만" 제거·추가한다.
    // 탭을 통째로 제거하면 다른 제품이 이미 붙인 패널까지 날아간다.
    private const string TabId = "PLANTFLOW_TAB";
    private const string PanelSourceId = "PLANTFLOW_SUPPORT_PANEL";

    private static bool _hooked;

    /// <summary>Initialize에서 호출. 리본 준비 시점까지 Application.Idle로 지연 후 1회 생성.</summary>
    public static void Schedule()
    {
      if (_hooked) return;
      _hooked = true;
      AcAppFull.Idle += OnIdle;
    }

    /// <summary>Terminate에서 호출. 미해제 핸들러 정리(세션 종료 위생).</summary>
    public static void Teardown()
    {
      if (!_hooked) return;
      AcAppFull.Idle -= OnIdle;
      _hooked = false;
    }

    private static void OnIdle(object sender, EventArgs e)
    {
      // 첫 진입 즉시 해제 — 성공·예외 무관 1회만. 미해제 시 Idle마다 재생성되어 프리징·누수가 난다.
      AcAppFull.Idle -= OnIdle;
      _hooked = false;
      try
      {
        BuildRibbon();
      }
      catch (System.Exception ex)
      {
        // 리본 생성 실패는 세션을 중단시키지 않는다. 명령은 직접 입력으로 계속 쓸 수 있다.
        WarnFallback(ex);
      }
    }

    private static void BuildRibbon()
    {
      RibbonControl ribbon = ComponentManager.Ribbon;
      if (ribbon == null)
      {
        // 아직 리본 미준비 — 다음 Idle로 재예약.
        Schedule();
        return;
      }

      // 탭 = 제품군(PlantFlow), 패널 = 개별 제품(PlantFlow Support).
      RibbonTab tab = ribbon.FindTab(TabId);
      if (tab == null)
      {
        tab = new RibbonTab
        {
          Title = "PlantFlow",
          Id = TabId
        };
        ribbon.Tabs.Add(tab);
      }

      // 중복 가드는 자기 패널에만 적용한다(idempotent). PFO 패널은 건드리지 않는다.
      for (int i = tab.Panels.Count - 1; i >= 0; i--)
      {
        RibbonPanelSource existingSource = tab.Panels[i].Source;
        if (existingSource != null && existingSource.Id == PanelSourceId)
          tab.Panels.RemoveAt(i);
      }

      RibbonPanelSource src = new RibbonPanelSource { Title = "PlantFlow Support", Id = PanelSourceId };
      RibbonPanel panel = new RibbonPanel { Source = src };
      tab.Panels.Add(panel);

      // 버튼 구성은 기능 구현 완료 후 확정한다(사용자 보류). 현재는 패널 열기만 노출한다.
      src.Items.Add(MakeButton("Open Panel", "PFS", "PlantFlow Support 패널 열기", "open.png"));

      PlantOrthoView.FileDiag("RibbonService 패널 등록 완료 tab=" + TabId + " panel=" + PanelSourceId);
    }

    private static RibbonButton MakeButton(string text, string command, string tooltip, string iconFile)
    {
      RibbonButton btn = new RibbonButton
      {
        Text = text,
        ShowText = true,
        ShowImage = true,
        Size = RibbonItemSize.Large,
        Orientation = System.Windows.Controls.Orientation.Vertical,
        ToolTip = tooltip,
        CommandHandler = new PfsRibbonCommandHandler(command)
      };

      BitmapImage img = LoadIcon(iconFile);
      if (img != null)
      {
        btn.LargeImage = img;
        btn.Image = img;
      }
      else
      {
        // 아이콘 미제작 시 텍스트 전용 — 빌드·동작에 영향 없다.
        btn.ShowImage = false;
      }
      return btn;
    }

    private static BitmapImage LoadIcon(string fileName)
    {
      try
      {
        string dllDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        if (dllDir == null) return null;
        string path = Path.Combine(dllDir, "icons", fileName);
        if (!File.Exists(path)) return null;

        BitmapImage img = new BitmapImage();
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.UriSource = new Uri(path, UriKind.Absolute);
        img.EndInit();
        img.Freeze();
        return img;
      }
      catch (System.Exception ex)
      {
        // 아이콘 로드 실패는 비치명적 → 진단 후 텍스트 폴백.
        PlantOrthoView.FileDiag("RibbonService LoadIcon 실패 file=" + fileName + ": " + ex.GetType().Name + ": " + ex.Message);
        return null;
      }
    }

    private static void WarnFallback(System.Exception ex)
    {
      PlantOrthoView.FileDiag("RibbonService 리본 등록 실패: " + ex.GetType().Name + ": " + ex.Message);
      try
      {
        Autodesk.AutoCAD.ApplicationServices.Document doc = AcApp.DocumentManager?.MdiActiveDocument;
        if (doc != null)
          doc.Editor.WriteMessage("\n[PlantFlow Support] 리본 등록 실패: " + ex.Message
            + "\n명령(PFS)은 직접 입력으로 사용 가능합니다.");
      }
      catch (System.Exception inner)
      {
        PlantOrthoView.FileDiag("RibbonService WarnFallback 예외: " + inner.GetType().Name + ": " + inner.Message);
      }
    }
  }

  /// <summary>
  /// 리본 버튼 → SendStringToExecute. 활성 문서가 없으면(시작 페이지) 비활성화해 NRE를 막는다.
  /// CanExecuteChanged는 WPF CommandManager.RequerySuggested에 위임해 문서 전환 시 자동 재질의된다.
  /// </summary>
  internal sealed class PfsRibbonCommandHandler : System.Windows.Input.ICommand
  {
    private readonly string _command;

    public PfsRibbonCommandHandler(string command)
    {
      _command = command;
    }

    public event EventHandler CanExecuteChanged
    {
      add { System.Windows.Input.CommandManager.RequerySuggested += value; }
      remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object parameter)
    {
      try
      {
        return AcApp.DocumentManager?.MdiActiveDocument != null;
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("RibbonService CanExecute 예외: " + ex.GetType().Name + ": " + ex.Message);
        return false;
      }
    }

    public void Execute(object parameter)
    {
      try
      {
        Autodesk.AutoCAD.ApplicationServices.Document doc = AcApp.DocumentManager?.MdiActiveDocument;
        if (doc == null) return;
        // 종료문자를 포함해야 명령이 실제로 실행된다.
        doc.SendStringToExecute(_command + "\n", true, false, true);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("RibbonService Execute 실패 command=" + _command + ": " + ex.GetType().Name + ": " + ex.Message);
      }
    }
  }
}
