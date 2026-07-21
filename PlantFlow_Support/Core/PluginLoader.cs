using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(PlantFlow_Support.PluginLoader))]

namespace PlantFlow_Support
{
  /// <summary>
  /// AutoCAD 로드/언로드 진입점. PFO PluginLoader와 같은 역할이며, 현재는 리본 등록만 담당한다.
  /// AutoCAD가 IExtensionApplication을 리플렉션으로 찾으므로 타입·멤버 이름을 바꾸지 말 것.
  /// </summary>
  [System.Reflection.Obfuscation(Exclude = true, ApplyToMembers = true)]
  public class PluginLoader : IExtensionApplication
  {
    public void Initialize()
    {
      try
      {
        RibbonService.Schedule();
      }
      catch (System.Exception ex)
      {
        // 리본 예약 실패는 비치명적이다. 명령(PFS)은 직접 입력으로 계속 쓸 수 있다.
        PlantOrthoView.FileDiag("PluginLoader RibbonService.Schedule 실패: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    public void Terminate()
    {
      try
      {
        RibbonService.Teardown();
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PluginLoader RibbonService.Teardown 실패: " + ex.GetType().Name + ": " + ex.Message);
      }
    }
  }
}
