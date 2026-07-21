using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Autodesk.ProcessPower.PnP3dObjects;

#nullable disable
namespace PlantFlow_Support
{
  // Commands 부분 클래스: perspective 방어 가드와 계측(cycle42, 6e01a9b).
  // 추출 직후 AutoCAD 리본 WPF 바인딩이 PERSPECTIVE=1로 역기입하는 현상을 취소한다.
  public partial class Commands
  {
    // 활성 뷰포트의 '실제' Gs-뷰 상태를 문자열로 기술한다. PERSPECTIVE sysvar와 화면 표시가
    // 어긋나는 케이스(수동 3DORBIT 잔존 등) 계측용. sysvar만으로는 실제 투영모드를 못 잡는다.
    private string DescribeActiveViewPerspective(Document doc)
    {
      if (doc == null) return "doc=null";
      try
      {
        using (ViewTableRecord vtr = doc.Editor.GetCurrentView())
        {
          if (vtr == null) return "vtr=null";
          // ViewTableRecord은 perspective bool을 직접 노출 안 함 → 렌즈/방향/타깃으로 간접 계측.
          // perspective 뷰는 통상 lens<=finite + target 거동이 parallel과 달라 값 차이로 식별.
          return "lens=" + vtr.LensLength.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + " dir=" + this.FormatVectorForCommand(vtr.ViewDirection) + " target=" + vtr.Target.ToString();
        }
      }
      catch (System.Exception ex)
      {
        return "예외:" + ex.GetType().Name;
      }
    }

    // 독립 프로브: 아무 때나 호출해 PERSPECTIVE sysvar + 실제 뷰 perspective를 로그+화면에 찍는다.
    // 3DORBIT/NETLOAD 전후로 실행해 어느 액션이 뷰를 뒤집는지 before/after 비교하는 데 쓴다.
    [CommandMethod("PFSPERSPPROBE", CommandFlags.Session)]
    public void PerspectiveProbeCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null) return;
      object sv = null;
      try { sv = Application.GetSystemVariable("PERSPECTIVE"); }
      catch (System.Exception px) { PlantOrthoView.FileDiag("PFSPERSPPROBE sysvar 예외: " + px.GetType().Name + ": " + px.Message); }
      string svStr = sv == null ? "(null)" : sv.ToString();
      string viewStr = this.DescribeActiveViewPerspective(doc);
      short tileMode = (short)Application.GetSystemVariable("TILEMODE");
      PlantOrthoView.FileDiag("PFSPERSPPROBE PERSPECTIVE=" + svStr + " TILEMODE=" + tileMode + " view={" + viewStr + "}");
      doc.Editor.WriteMessage("\nPFSPERSPPROBE PERSPECTIVE=" + svStr + " TILEMODE=" + tileMode + " view={" + viewStr + "}");
    }

    // 실시간 감시기: PERSPECTIVE sysvar가 '바뀌는 순간'을 잡아 그때 실행 중인 명령(CMDNAMES)을
    // 로그로 남긴다. "아무것도 안 했는데 켜진다"의 진짜 트리거(리액터/줌/뷰큐브/오픈 등)를 특정하기 위함.
    // 토글: 첫 호출=on, 다시 호출=off.
    private static bool s_perspWatchOn = false;
    private static Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventHandler s_perspWatchHandler = null;
    private static bool s_perspHandlerSubscribed = false;
    private static bool s_perspGuardInstalled = false;
    private static System.DateTime s_perspGuardUntilUtc = System.DateTime.MinValue;
    private static object s_perspGuardValue = null;
    private static bool s_perspRestoring = false;

    [CommandMethod("PFSPERSPWATCH", CommandFlags.Session)]
    public void PerspectiveWatchCommand()
    {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      if (doc == null) return;
      if (s_perspWatchOn)
      {
        try
        {
          if (s_perspHandlerSubscribed && s_perspWatchHandler != null && !s_perspGuardInstalled)
          {
            Autodesk.AutoCAD.ApplicationServices.Application.SystemVariableChanged -= s_perspWatchHandler;
            s_perspHandlerSubscribed = false;
          }
          else if (s_perspGuardInstalled)
          {
            PlantOrthoView.FileDiag("PFSPERSPWATCH off: 가드 구독 유지");
          }
        }
        catch (System.Exception ux) { PlantOrthoView.FileDiag("PFSPERSPWATCH off 예외: " + ux.GetType().Name + ": " + ux.Message); }
        s_perspWatchOn = false;
        PlantOrthoView.FileDiag("PFSPERSPWATCH off");
        doc.Editor.WriteMessage("\nPFSPERSPWATCH off (감시 중지)");
        return;
      }
      Commands.EnsurePerspHandlerSubscribed("PFSPERSPWATCH");
      s_perspWatchOn = true;
      PlantOrthoView.FileDiag("PFSPERSPWATCH on");
      doc.Editor.WriteMessage("\nPFSPERSPWATCH on (PERSPECTIVE 변경 감시 시작)");
    }

    private static void EnsurePerspGuardInstalled()
    {
      if (s_perspGuardInstalled)
        return;

      s_perspGuardInstalled = true;
      Commands.EnsurePerspHandlerSubscribed("guard");
      PlantOrthoView.FileDiag("PFSNOTABDETAIL persp guard installed");
    }

    private static void EnsurePerspHandlerSubscribed(string reason)
    {
      try
      {
        if (s_perspWatchHandler == null)
          s_perspWatchHandler = new Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventHandler(Commands.OnSysVarChangedForPersp);

        if (s_perspHandlerSubscribed)
        {
          PlantOrthoView.FileDiag("PFSPERSPWATCH subscribe skip reason=" + reason);
          return;
        }

        Autodesk.AutoCAD.ApplicationServices.Application.SystemVariableChanged += s_perspWatchHandler;
        s_perspHandlerSubscribed = true;
        PlantOrthoView.FileDiag("PFSPERSPWATCH subscribed reason=" + reason);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSPERSPWATCH subscribe 예외 reason=" + reason + ": " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private static void ArmPerspGuard(object savedPerspective)
    {
      try
      {
        s_perspGuardValue = savedPerspective;
        double seconds = 8.0;
        string env = System.Environment.GetEnvironmentVariable("PFS_PERSP_GUARD_SEC");
        if (!string.IsNullOrWhiteSpace(env))
        {
          double parsed;
          if (double.TryParse(env, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed) && parsed > 0.0)
            seconds = parsed;
          else
            PlantOrthoView.FileDiag("PFSNOTABDETAIL persp guard env 무시 PFS_PERSP_GUARD_SEC=" + env);
        }
        s_perspGuardUntilUtc = System.DateTime.UtcNow.AddSeconds(seconds);
        Commands.EnsurePerspGuardInstalled();
        PlantOrthoView.FileDiag("PFSNOTABDETAIL persp guard armed seconds=" + seconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " value=" + (savedPerspective == null ? "(null)" : savedPerspective.ToString()));
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL persp guard arm 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }

    private static void OnSysVarChangedForPersp(object sender, Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs e)
    {
      try
      {
        if (e == null || string.IsNullOrEmpty(e.Name)) return;
        if (!string.Equals(e.Name, "PERSPECTIVE", System.StringComparison.OrdinalIgnoreCase)) return;
        object val = Application.GetSystemVariable("PERSPECTIVE");
        object cmd = Application.GetSystemVariable("CMDNAMES");
        PlantOrthoView.FileDiag("PFSPERSPWATCH PERSPECTIVE -> " + (val == null ? "(null)" : val.ToString()) + " CMDNAMES='" + (cmd == null ? "" : cmd.ToString()) + "'");
        // 스택 계측: SETVAR가 managed 리액터(우리/서드파티 .NET)에서 오는지 vs 네이티브 UI(뷰큐브 등)
        // 에서 오는지 판별. managed 발원이면 프레임에 해당 타입/메서드가 찍히고, 네이티브면 이벤트
        // 디스패치만 얕게 찍힌다. 앞 ~20프레임만 기록.
        try
        {
          string st = System.Environment.StackTrace;
          if (st != null)
          {
            string[] lines = st.Split('\n');
            int take = System.Math.Min(20, lines.Length);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < take; i++) sb.Append(lines[i].Trim()).Append(" | ");
            PlantOrthoView.FileDiag("PFSPERSPWATCH stack: " + sb.ToString());
          }
        }
        catch (System.Exception sx) { PlantOrthoView.FileDiag("PFSPERSPWATCH stack 예외: " + sx.GetType().Name); }
        Commands.TryRestorePerspGuard(val);
      }
      catch (System.Exception ex) { PlantOrthoView.FileDiag("PFSPERSPWATCH 핸들러 예외: " + ex.GetType().Name + ": " + ex.Message); }
    }

    private static void TryRestorePerspGuard(object currentValue)
    {
      if (s_perspRestoring)
        return;
      if (s_perspGuardValue == null)
        return;
      if (System.DateTime.UtcNow > s_perspGuardUntilUtc)
        return;
      if (object.Equals(currentValue, s_perspGuardValue))
        return;

      object fromValue = currentValue;
      object toValue = s_perspGuardValue;
      try
      {
        s_perspRestoring = true;
        Application.SetSystemVariable("PERSPECTIVE", toValue);
        s_perspGuardUntilUtc = System.DateTime.MinValue;
        PlantOrthoView.FileDiag("PFSNOTABDETAIL persp guard 교정 " + (fromValue == null ? "(null)" : fromValue.ToString()) + " -> " + toValue);
        Commands.SchedulePerspGuardRegen();
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL persp guard 교정 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
      finally
      {
        s_perspRestoring = false;
      }
    }

    private static void SchedulePerspGuardRegen()
    {
      System.EventHandler idleHandler = null;
      idleHandler = delegate(object idleSender, System.EventArgs idleArgs)
      {
        try
        {
          Autodesk.AutoCAD.ApplicationServices.Application.Idle -= idleHandler;
        }
        catch (System.Exception ux)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL persp guard Idle 해제 예외: " + ux.GetType().Name + ": " + ux.Message);
        }

        try
        {
          Document doc = Application.DocumentManager == null ? null : Application.DocumentManager.MdiActiveDocument;
          if (doc == null || doc.Editor == null)
          {
            PlantOrthoView.FileDiag("PFSNOTABDETAIL persp guard Regen skip: doc/editor null");
            return;
          }

          doc.Editor.Regen();
          PlantOrthoView.FileDiag("PFSNOTABDETAIL persp guard Regen 완료");
        }
        catch (System.Exception ex)
        {
          PlantOrthoView.FileDiag("PFSNOTABDETAIL persp guard Regen 예외: " + ex.GetType().Name + ": " + ex.Message);
        }
      };

      try
      {
        Autodesk.AutoCAD.ApplicationServices.Application.Idle += idleHandler;
        PlantOrthoView.FileDiag("PFSNOTABDETAIL persp guard Regen Idle 예약");
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("PFSNOTABDETAIL persp guard Idle 예약 예외: " + ex.GetType().Name + ": " + ex.Message);
      }
    }
  }
}
