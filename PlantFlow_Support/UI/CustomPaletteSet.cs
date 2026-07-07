using Autodesk.AutoCAD.Windows;
using System;
using System.Drawing;
using System.Windows.Forms;

#nullable disable
namespace PlantFlow_Support
{
  public class CustomPaletteSet : PaletteSet
  {
    // P4-3: PaletteTab에서 KeepFocus 토글용 접근. dispose 후 접근은 호출측 try/catch로 흡수.
    public static CustomPaletteSet Instance { get; private set; }

    private readonly PaletteTab _tab;

    public CustomPaletteSet()
      : base("PlantFlow_Support", "PlantFlow_Support", new Guid("ba6361b4-e73c-45e3-a5d6-84039323d0a9"))
    {
      Instance = this;
      this.Style = (PaletteSetStyles) 14;
      this.MinimumSize = new Size(330, 280);
      _tab = new PaletteTab();
      this.Add("Tab", (Control) _tab);
      // P4-4: 도킹↔플로팅 전환/크기 변경 시 WebView 재배치(PFO WebViewPaletteSet 검증 패턴).
      this.StateChanged += (s, e) => _tab.RelayoutCatalogWeb();
      this.SizeChanged += (s, e) => _tab.RelayoutCatalogWeb();
    }
  }
}
