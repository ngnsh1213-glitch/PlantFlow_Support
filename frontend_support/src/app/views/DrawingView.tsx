import { useCallback, useEffect, useState } from "react";
import { FileOutput, Loader2, Plus, Trash2 } from "lucide-react";
import { Button } from "../../design-system/ui/Button";
import { CardContent } from "../../design-system/ui/Card";
import { Input } from "../../design-system/ui/Input";
import { SettingsCard } from "../../design-system/ui/SettingsCard";
import { drawingApi, isDrawingBridgeAvailable, type DrawingSettings, type DrawingState } from "../api/drawingApi";

const MAIN_VIEW = "Main";
const MULTI_VIEWS = ["Top", "Bottom", "Front", "Back", "Left", "Right"] as const;

export default function DrawingView() {
  const bridge = isDrawingBridgeAvailable();
  const [selectedViews, setSelectedViews] = useState<string[]>([]);
  const [state, setState] = useState<DrawingState>({ supports: [], gridReady: false, captureDoc: "" });
  const [settings, setSettings] = useState<DrawingSettings>({ template: "", projectNo: "", revision: "" });
  const [checked, setChecked] = useState<Set<string>>(new Set());
  const [busy, setBusy] = useState(false);
  const [toast, setToast] = useState<string>("");

  const flash = (message: string) => {
    setToast(message);
    window.setTimeout(() => setToast(""), 4000);
  };

  const refresh = useCallback(async () => {
    if (!bridge) return;
    try {
      const nextState = await drawingApi.getState();
      setState(nextState);
      setChecked(new Set(nextState.supports.map((s) => s.name)));
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
      const views = [MAIN_VIEW, ...selectedViews];
      const result = await drawingApi.addFromSelection(views);
      setState(result.state);
      setChecked(new Set(result.state.supports.map((s) => s.name)));
      const a = result.added;
      let m = `${a.count}개 추가 (${a.view})`;
      if (a.duplicates || a.unnamed || a.invalid) m += ` [중복 ${a.duplicates}/무명 ${a.unnamed}/무효 ${a.invalid}]`;
      flash(m);
    } catch (e: any) {
      flash(e?.code === "USER_CANCELLED" ? "선택 취소됨" : e?.error ?? "추가 실패");
    }
  });

  const onRemove = (name: string) => guard(async () => {
    try {
      const nextState = await drawingApi.removeSupport(name);
      setState(nextState);
      setChecked((prev) => {
        const next = new Set(prev);
        next.delete(name);
        return next;
      });
    } catch (e: any) {
      flash(e?.error ?? "삭제 실패");
    }
  });

  const onClear = () => guard(async () => {
    try {
      const nextState = await drawingApi.clearSupports();
      setState(nextState);
      setChecked(new Set());
    } catch (e: any) {
      flash(e?.error ?? "초기화 실패");
    }
  });

  const onExport2D = () => guard(async () => {
    try {
      const names = state.supports.filter((s) => checked.has(s.name)).map((s) => s.name);
      if (names.length === 0) {
        flash("추출할 서포트를 선택하세요");
        return;
      }
      await drawingApi.export2D(names);
      flash("Export 2D 완료");
    } catch (e: any) {
      flash(e?.error ?? "Export 실패");
    }
  });

  const onSetting = (key: keyof DrawingSettings, value: string) => {
    setSettings((s) => ({ ...s, [key]: value }));
    drawingApi.setSetting(key, value).catch((e: any) => flash(`설정 저장 실패: ${e?.error ?? ""}`));
  };

  const toggleView = (view: string) => {
    setSelectedViews((prev) =>
      prev.includes(view) ? prev.filter((v) => v !== view) : [...prev, view]
    );
  };

  const allChecked = state.supports.length > 0 && state.supports.every((s) => checked.has(s.name));
  const toggleAllSupports = () => {
    setChecked(allChecked ? new Set() : new Set(state.supports.map((s) => s.name)));
  };

  return (
    <div className="relative flex h-full flex-col gap-3 overflow-auto bg-slate-100 p-4 text-slate-900">
      {toast && <div className="rounded-md bg-slate-800 px-3 py-2 text-sm text-white">{toast}</div>}
      {!bridge && <div className="rounded-md bg-amber-100 px-3 py-2 text-sm text-amber-800">웹 미리보기 모드 - 브리지는 플러그인에서만 동작</div>}

      <SettingsCard title="Support Selection">
        <CardContent className="flex flex-col gap-3 p-4">
          <div className="flex items-center gap-2">
            <div className="flex flex-wrap items-center gap-1">
              <label className="flex items-center gap-1 rounded-md border border-blue-300 bg-blue-50 px-2 py-1.5 text-sm text-blue-700">
                <input type="checkbox" checked readOnly disabled />
                Main
              </label>
              {MULTI_VIEWS.map((v) => (
                <label key={v} className="flex items-center gap-1 rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm">
                  <input
                    type="checkbox"
                    checked={selectedViews.includes(v)}
                    disabled={busy}
                    onChange={() => toggleView(v)}
                  />
                  {v}
                </label>
              ))}
            </div>
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
                  <th className="w-10 px-3 py-2">
                    <input
                      type="checkbox"
                      checked={allChecked}
                      disabled={busy || state.supports.length === 0}
                      onChange={toggleAllSupports}
                      aria-label="전체 서포트 선택"
                    />
                  </th>
                  <th className="px-3 py-2">Support Name</th>
                  <th className="px-3 py-2">View</th>
                  <th className="w-10 px-3 py-2"></th>
                </tr>
              </thead>
              <tbody>
                {state.supports.map((support) => (
                  <tr key={support.name} className="border-t border-slate-100">
                    <td className="px-3 py-1.5">
                      <input
                        type="checkbox"
                        checked={checked.has(support.name)}
                        disabled={busy}
                        onChange={() => {
                          setChecked((prev) => {
                            const next = new Set(prev);
                            if (next.has(support.name)) next.delete(support.name);
                            else next.add(support.name);
                            return next;
                          });
                        }}
                        aria-label={`${support.name} 선택`}
                      />
                    </td>
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
                    <td colSpan={4} className="px-3 py-4 text-center text-slate-400">선택된 서포트 없음</td>
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
        <span className="text-xs text-slate-500">드래그로 여러 서포트를 한 번에 선택할 수 있습니다</span>
        <Button variant="outline" disabled title="MTO는 재구현 예정(보류)">Export MTO (보류)</Button>
        {!state.gridReady && <span className="text-xs text-amber-600">Grid 미설정 - Export 2D 불가(그리드는 Phase 1b)</span>}
      </div>

      <div className="mt-auto rounded-lg border border-dashed border-slate-300 p-4 text-center text-sm text-slate-400">
        Grid System - Phase 1b 예정
      </div>
    </div>
  );
}
