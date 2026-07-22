import { useCallback, useEffect, useState } from "react";
import { FileOutput, Loader2, Plus, Trash2 } from "lucide-react";
import { Button } from "../../design-system/ui/Button";
import { CardContent } from "../../design-system/ui/Card";
import { Input } from "../../design-system/ui/Input";
import { SettingsCard } from "../../design-system/ui/SettingsCard";
import { drawingApi, isDrawingBridgeAvailable, type DrawingSettings, type DrawingState } from "../api/drawingApi";

export default function DrawingView() {
  const bridge = isDrawingBridgeAvailable();
  const [state, setState] = useState<DrawingState>({ supports: [], captureDoc: "" });
  const [settings, setSettings] = useState<DrawingSettings>({ template: "", projectNo: "", revision: "" });
  const [checked, setChecked] = useState<Set<string>>(new Set());
  const [busy, setBusy] = useState(false);
  const [toast, setToast] = useState("");
  const flash = (message: string) => { setToast(message); window.setTimeout(() => setToast(""), 4000); };
  const refresh = useCallback(async () => {
    if (!bridge) return;
    try { const next = await drawingApi.getState(); setState(next); setChecked(new Set(next.supports.map((s) => s.name))); setSettings(await drawingApi.getSettings()); }
    catch (e: any) { flash(e?.error ?? "상태 로드 실패"); }
  }, [bridge]);
  useEffect(() => { refresh(); }, [refresh]);
  useEffect(() => { window.dispatchEvent(new CustomEvent("pfs:drawing-busy", { detail: busy })); return () => { window.dispatchEvent(new CustomEvent("pfs:drawing-busy", { detail: false })); }; }, [busy]);
  const guard = async (action: () => Promise<void>) => { if (busy) return; setBusy(true); try { await action(); } finally { setBusy(false); } };
  const add = (batch: boolean) => guard(async () => {
    try { const result = batch ? await drawingApi.addBatchFromSelection() : await drawingApi.addFromSelection(); setState(result.state); setChecked(new Set(result.state.supports.map((s) => s.name))); flash(`${result.added.count}개 추가${result.added.duplicates ? ` (중복 ${result.added.duplicates})` : ""}`); }
    catch (e: any) { flash(e?.code === "USER_CANCELLED" ? "선택 취소됨" : e?.error ?? "추가 실패"); }
  });
  const remove = (name: string) => guard(async () => { try { const next = await drawingApi.removeSupport(name); setState(next); setChecked((old) => { const nextChecked = new Set(old); nextChecked.delete(name); return nextChecked; }); } catch (e: any) { flash(e?.error ?? "삭제 실패"); } });
  const clear = () => guard(async () => { try { setState(await drawingApi.clearSupports()); setChecked(new Set()); } catch (e: any) { flash(e?.error ?? "초기화 실패"); } });
  const exportNotab = () => guard(async () => { const names = state.supports.filter((s) => checked.has(s.name)).map((s) => s.name); if (!names.length) { flash("추출할 서포트를 선택하세요"); return; } try { const result = await drawingApi.export2D(names); flash(`무탭 추출 완료: ok ${result.ok} / fail ${result.fail}${result.failTags.length ? ` (${result.failTags.join(", ")})` : ""}`); } catch (e: any) { flash(e?.error ?? "무탭 추출 실패"); } });
  const setSetting = (key: keyof DrawingSettings, value: string) => { setSettings((current) => ({ ...current, [key]: value })); drawingApi.setSetting(key, value).catch((e: any) => flash(`설정 저장 실패: ${e?.error ?? ""}`)); };
  const allChecked = state.supports.length > 0 && state.supports.every((s) => checked.has(s.name));
  return <div className="relative flex h-full flex-col gap-3 overflow-auto bg-slate-100 p-4 text-slate-900">
    {toast && <div className="rounded-md bg-slate-800 px-3 py-2 text-sm text-white">{toast}</div>}
    {!bridge && <div className="rounded-md bg-amber-100 px-3 py-2 text-sm text-amber-800">웹 미리보기 모드 - 브리지는 플러그인에서만 동작</div>}
    <SettingsCard title="Support Selection"><CardContent className="flex flex-col gap-3 p-4"><div className="flex flex-wrap items-center gap-2"><Button disabled={busy} onClick={() => add(false)}>{busy ? <Loader2 size={15} className="mr-2 animate-spin" /> : <Plus size={15} className="mr-2" />}서포트 선택 추가</Button><Button variant="outline" disabled={busy} onClick={() => add(true)}><Plus size={15} className="mr-2" />일괄 선택 추가</Button><Button variant="outline" disabled={busy || !state.supports.length} onClick={clear}><Trash2 size={15} className="mr-2" />Clear</Button></div><div className="max-h-[200px] overflow-auto rounded-lg border border-slate-200 bg-white"><table className="w-full text-sm"><thead className="bg-slate-50 text-left text-[12px] uppercase text-slate-500"><tr><th className="w-10 px-3 py-2"><input type="checkbox" checked={allChecked} disabled={busy || !state.supports.length} onChange={() => setChecked(allChecked ? new Set() : new Set(state.supports.map((s) => s.name)))} /></th><th className="px-3 py-2">Support Name</th><th className="px-3 py-2">타입/상태</th><th className="w-10 px-3 py-2"></th></tr></thead><tbody>{state.supports.map((support) => <tr key={support.name} className="border-t border-slate-100"><td className="px-3 py-1.5"><input type="checkbox" checked={checked.has(support.name)} disabled={busy} onChange={() => setChecked((old) => { const next = new Set(old); if (next.has(support.name)) next.delete(support.name); else next.add(support.name); return next; })} /></td><td className="px-3 py-1.5">{support.name}</td><td className="px-3 py-1.5">{support.status}</td><td className="px-3 py-1.5"><button disabled={busy} onClick={() => remove(support.name)} className="text-slate-400 hover:text-red-500"><Trash2 size={14} /></button></td></tr>)}{!state.supports.length && <tr><td colSpan={4} className="px-3 py-4 text-center text-slate-400">선택된 서포트 없음</td></tr>}</tbody></table></div></CardContent></SettingsCard>
    <SettingsCard title="Settings"><CardContent className="grid grid-cols-3 gap-3 p-4"><label className="text-sm">Template<Input value={settings.template} disabled={busy} onChange={(e) => setSetting("template", e.target.value)} /></label><label className="text-sm">Project No<Input value={settings.projectNo} disabled={busy} onChange={(e) => setSetting("projectNo", e.target.value)} /></label><label className="text-sm">Revision<Input value={settings.revision} disabled={busy} onChange={(e) => setSetting("revision", e.target.value)} /></label></CardContent></SettingsCard>
    <div className="flex items-center gap-2"><Button disabled={busy || !state.supports.length} onClick={exportNotab}><FileOutput size={15} className="mr-2" />무탭 추출</Button><span className="text-xs text-slate-500">영역 또는 다중 선택으로 서포트를 목록에 추가할 수 있습니다</span><Button variant="outline" disabled title="MTO는 재구현 예정(보류)">Export MTO (보류)</Button></div>
  </div>;
}
