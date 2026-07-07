import { useCallback, useEffect, useState } from "react";
import { FileOutput, Loader2, Plus, Trash2 } from "lucide-react";
import { Button } from "../../design-system/ui/Button";
import { CardContent } from "../../design-system/ui/Card";
import { Input } from "../../design-system/ui/Input";
import { SettingsCard } from "../../design-system/ui/SettingsCard";
import { drawingApi, isDrawingBridgeAvailable, type DrawingSettings, type DrawingState } from "../api/drawingApi";

const VIEWS = ["Top", "Bottom", "Front", "Back", "Left", "Right"] as const;

export default function DrawingView() {
  const bridge = isDrawingBridgeAvailable();
  const [viewDir, setViewDir] = useState<string>("Top");
  const [state, setState] = useState<DrawingState>({ supports: [], gridReady: false, captureDoc: "" });
  const [settings, setSettings] = useState<DrawingSettings>({ template: "", projectNo: "", revision: "" });
  const [busy, setBusy] = useState(false);
  const [toast, setToast] = useState<string>("");

  const flash = (message: string) => {
    setToast(message);
    window.setTimeout(() => setToast(""), 4000);
  };

  const refresh = useCallback(async () => {
    if (!bridge) return;
    try {
      setState(await drawingApi.getState());
      setSettings(await drawingApi.getSettings());
    } catch (e: any) {
      flash(e?.error ?? "상태 로드 실패");
    }
  }, [bridge]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const guard = async (fn: () => Promise<void>) => {
    if (busy) return;
    setBusy(true);
    try {
      await fn();
    } finally {
      setBusy(false);
    }
  };

  const onAdd = () => guard(async () => {
    try {
      const result = await drawingApi.addFromSelection(viewDir);
      setState(result.state);
      flash(`${result.added.name} (${result.added.view}) 추가`);
    } catch (e: any) {
      flash(e?.code === "USER_CANCELLED" ? "선택 취소됨" : e?.error ?? "추가 실패");
    }
  });

  const onRemove = (name: string) => guard(async () => {
    try {
      setState(await drawingApi.removeSupport(name));
    } catch (e: any) {
      flash(e?.error ?? "삭제 실패");
    }
  });

  const onClear = () => guard(async () => {
    try {
      setState(await drawingApi.clearSupports());
    } catch (e: any) {
      flash(e?.error ?? "초기화 실패");
    }
  });

  const onExport2D = () => guard(async () => {
    try {
      await drawingApi.export2D();
      flash("Export 2D 완료");
    } catch (e: any) {
      flash(e?.error ?? "Export 실패");
    }
  });

  const onSetting = (key: keyof DrawingSettings, value: string) => {
    setSettings((s) => ({ ...s, [key]: value }));
    drawingApi.setSetting(key, value).catch((e: any) => flash(`설정 저장 실패: ${e?.error ?? ""}`));
  };

  return (
    <div className="relative flex h-full flex-col gap-3 overflow-auto bg-slate-100 p-4 text-slate-900">
      {toast && <div className="rounded-md bg-slate-800 px-3 py-2 text-sm text-white">{toast}</div>}
      {!bridge && <div className="rounded-md bg-amber-100 px-3 py-2 text-sm text-amber-800">웹 미리보기 모드 - 브리지는 플러그인에서만 동작</div>}

      <SettingsCard title="Support Selection">
        <CardContent className="flex flex-col gap-3 p-4">
          <div className="flex items-center gap-2">
            <select
              value={viewDir}
              onChange={(e) => setViewDir(e.target.value)}
              disabled={busy}
              className="rounded-md border border-slate-300 px-2 py-1.5 text-sm"
            >
              {VIEWS.map((v) => <option key={v} value={v}>{v}</option>)}
            </select>
            <Button disabled={busy} onClick={onAdd}>
              {busy ? <Loader2 size={15} className="mr-2 animate-spin" /> : <Plus size={15} className="mr-2" />}
              Add (도면 선택)
            </Button>
            <Button variant="outline" disabled={busy || state.supports.length === 0} onClick={onClear}>
              <Trash2 size={15} className="mr-2" />Clear
            </Button>
          </div>

          <div className="max-h-[200px] overflow-auto rounded-lg border border-slate-200 bg-white">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-left text-[12px] uppercase text-slate-500">
                <tr>
                  <th className="px-3 py-2">Support Name</th>
                  <th className="px-3 py-2">View</th>
                  <th className="w-10 px-3 py-2"></th>
                </tr>
              </thead>
              <tbody>
                {state.supports.map((support) => (
                  <tr key={support.name} className="border-t border-slate-100">
                    <td className="px-3 py-1.5">{support.name}</td>
                    <td className="px-3 py-1.5">{support.view}</td>
                    <td className="px-3 py-1.5">
                      <button disabled={busy} onClick={() => onRemove(support.name)} className="text-slate-400 hover:text-red-500">
                        <Trash2 size={14} />
                      </button>
                    </td>
                  </tr>
                ))}
                {state.supports.length === 0 && (
                  <tr>
                    <td colSpan={3} className="px-3 py-4 text-center text-slate-400">선택된 서포트 없음</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </CardContent>
      </SettingsCard>

      <SettingsCard title="Settings">
        <CardContent className="grid grid-cols-3 gap-3 p-4">
          <label className="text-sm">Template<Input value={settings.template} disabled={busy} onChange={(e) => onSetting("template", e.target.value)} /></label>
          <label className="text-sm">Project No<Input value={settings.projectNo} disabled={busy} onChange={(e) => onSetting("projectNo", e.target.value)} /></label>
          <label className="text-sm">Revision<Input value={settings.revision} disabled={busy} onChange={(e) => onSetting("revision", e.target.value)} /></label>
        </CardContent>
      </SettingsCard>

      <div className="flex items-center gap-2">
        <Button disabled={busy || state.supports.length === 0} onClick={onExport2D}>
          <FileOutput size={15} className="mr-2" />Export 2D
        </Button>
        <Button variant="outline" disabled title="MTO는 재구현 예정(보류)">Export MTO (보류)</Button>
        {!state.gridReady && <span className="text-xs text-amber-600">Grid 미설정 - Export 2D 불가(그리드는 Phase 1b)</span>}
      </div>

      <div className="mt-auto rounded-lg border border-dashed border-slate-300 p-4 text-center text-sm text-slate-400">
        Grid System - Phase 1b 예정
      </div>
    </div>
  );
}
