import { useEffect, useMemo, useState } from "react";
import { Box, Database, Eye, FileUp, Plus, RefreshCcw, Save, Trash2 } from "lucide-react";
import { motion } from "framer-motion";
import { Button } from "../../design-system/ui/Button";
import { Card, CardContent } from "../../design-system/ui/Card";
import { Input } from "../../design-system/ui/Input";
import { SettingsCard } from "../../design-system/ui/SettingsCard";
import { SettingsField, SettingsSelect } from "../../design-system/ui/SettingsControls";
import { mockCatalogApi } from "../api/mockCatalogApi";
import type { ParameterDef, VariantInput, VariantRow } from "../api/catalogApi";

type LoadState = "idle" | "loading" | "error";

function inputFromRow(row: VariantRow | null, params: ParameterDef[]): VariantInput {
  const source = row?.paramDefinition ?? {};
  return {
    dn: row?.dn ?? "",
    shortDescription: row?.shortDescription ?? "",
    params: Object.fromEntries(params.map((param) => [param.key, String(source[param.key] ?? "")]))
  };
}

export default function SupportCatalogView() {
  const [types, setTypes] = useState([] as Awaited<ReturnType<typeof mockCatalogApi.listTypes>>);
  const [activeType, setActiveType] = useState("");
  const [variants, setVariants] = useState<VariantRow[]>([]);
  const [params, setParams] = useState<ParameterDef[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [form, setForm] = useState<VariantInput>({ dn: "", shortDescription: "", params: {} });
  const [status, setStatus] = useState("Mock catalog API ready.");
  const [loadState, setLoadState] = useState<LoadState>("idle");
  const [sortAsc, setSortAsc] = useState(true);

  const selectedRow = variants.find((row) => row.pnpId === selectedId) ?? null;
  const activeMeta = types.find((type) => type.id === activeType);
  const sortedVariants = useMemo(() => {
    const copy = [...variants];
    copy.sort((a, b) => (sortAsc ? Number(a.dn) - Number(b.dn) : Number(b.dn) - Number(a.dn)));
    return copy;
  }, [sortAsc, variants]);

  useEffect(() => {
    let cancelled = false;
    setLoadState("loading");
    mockCatalogApi.listTypes()
      .then((next) => {
        if (cancelled) return;
        setTypes(next);
        setActiveType(next[0]?.id ?? "");
        setLoadState("idle");
      })
      .catch((err: unknown) => {
        console.error("catalog types load failed", err);
        if (!cancelled) {
          setLoadState("error");
          setStatus("Catalog type load failed. See console.");
        }
      });
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    if (!activeType) return;
    let cancelled = false;
    setLoadState("loading");
    Promise.all([mockCatalogApi.listVariants(activeType), mockCatalogApi.getParamKeys(activeType)])
      .then(([nextRows, nextParams]) => {
        if (cancelled) return;
        setVariants(nextRows);
        setParams(nextParams);
        const first = nextRows[0] ?? null;
        setSelectedId(first?.pnpId ?? null);
        setForm(inputFromRow(first, nextParams));
        setStatus(`${activeType} mock data loaded.`);
        setLoadState("idle");
      })
      .catch((err: unknown) => {
        console.error("catalog data load failed", err);
        if (!cancelled) {
          setLoadState("error");
          setStatus("Catalog data load failed. See console.");
        }
      });
    return () => { cancelled = true; };
  }, [activeType]);

  useEffect(() => {
    setForm(inputFromRow(selectedRow, params));
  }, [params, selectedRow]);

  const updateParam = (key: string, value: string) => {
    setForm((current) => ({ ...current, params: { ...current.params, [key]: value } }));
  };

  const refresh = async () => {
    if (!activeType) return;
    const [nextRows, nextParams] = await Promise.all([mockCatalogApi.listVariants(activeType), mockCatalogApi.getParamKeys(activeType)]);
    setVariants(nextRows);
    setParams(nextParams);
    setStatus(`${activeType} refreshed from mock adapter.`);
  };

  const runAction = async (action: "add" | "update" | "delete" | "load" | "preview") => {
    try {
      setLoadState("loading");
      let result;
      if (action === "add") result = await mockCatalogApi.addVariant(activeType, form);
      if (action === "update" && selectedId != null) result = await mockCatalogApi.updateVariant(selectedId, activeType, form);
      if (action === "delete" && selectedId != null) result = await mockCatalogApi.deleteVariant(selectedId);
      if (action === "load") result = await mockCatalogApi.loadAcat();
      if (action === "preview") result = await mockCatalogApi.preview({ type: activeType, params: form.params });
      setStatus(result?.message ?? "Action skipped.");
      await refresh();
    } catch (err) {
      console.error(`catalog action ${action} failed`, err);
      setStatus(`Action ${action} failed. See console.`);
      setLoadState("error");
      return;
    }
    setLoadState("idle");
  };

  return (
    <main className="h-full overflow-hidden bg-slate-100 text-slate-900">
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,_rgba(59,130,246,0.16),_transparent_32%),linear-gradient(135deg,_#f8fafc_0%,_#e2e8f0_100%)]" />
      <div className="relative flex h-full flex-col gap-4 p-5">
        <header className="flex min-h-[54px] items-center justify-between gap-4 rounded-[14px] border border-white/60 bg-white/65 px-5 py-3 shadow-sm backdrop-blur-xl">
          <div>
            <div className="text-[12px] font-semibold uppercase tracking-[0.18em] text-blue-600">PlantFlow Support</div>
            <h1 className="text-[22px] font-bold leading-tight">Support Catalog</h1>
          </div>
          <div className="flex flex-wrap items-center justify-end gap-2">
            <Button variant="outline" onClick={() => runAction("load")}><FileUp size={15} className="mr-2" />Load .acat</Button>
            <Button variant="outline" onClick={() => refresh()}><RefreshCcw size={15} className="mr-2" />Refresh</Button>
            <Button onClick={() => runAction("preview")}><Eye size={15} className="mr-2" />Preview</Button>
          </div>
        </header>
        <section className="grid min-h-0 flex-1 grid-cols-[250px_minmax(360px,1fr)_360px] gap-4">
          <SettingsCard title="Support Types" className="min-h-0">
            <CardContent className="flex h-full min-h-0 flex-col gap-3 p-4">
              <SettingsSelect value={activeType} options={types.map((type) => type.id)} onChange={setActiveType} />
              <div className="min-h-0 flex-1 space-y-2 overflow-auto pr-1">
                {types.map((type) => (
                  <button key={type.id} type="button" onClick={() => setActiveType(type.id)} className={`w-full rounded-xl border px-3 py-3 text-left transition ${activeType === type.id ? "border-blue-300 bg-blue-50/90 text-blue-900" : "border-slate-200 bg-white/55 hover:bg-white"}`}>
                    <div className="font-semibold">{type.label}</div>
                    <div className="mt-1 text-[12px] leading-snug text-slate-500">{type.description}</div>
                  </button>
                ))}
              </div>
            </CardContent>
          </SettingsCard>
          <SettingsCard title="Variants" className="min-h-0">
            <CardContent className="flex h-full min-h-0 flex-col gap-3 p-4">
              <div className="flex items-center justify-between gap-3 text-sm text-slate-600">
                <span>{activeMeta?.description ?? "Select a support type."}</span>
                <Button variant="ghost" onClick={() => setSortAsc((value) => !value)}>DN {sortAsc ? "up" : "down"}</Button>
              </div>
              <div className="min-h-0 flex-1 overflow-auto rounded-xl border border-slate-200 bg-white/60">
                <table className="w-full border-collapse text-sm">
                  <thead className="sticky top-0 bg-slate-50 text-left text-[12px] uppercase tracking-wide text-slate-500"><tr><th className="px-3 py-2">PnP ID</th><th className="px-3 py-2">DN</th><th className="px-3 py-2">Description</th><th className="px-3 py-2">Params</th></tr></thead>
                  <tbody>
                    {sortedVariants.map((row) => (
                      <tr key={row.pnpId} onClick={() => setSelectedId(row.pnpId)} className={`cursor-pointer border-t border-slate-100 ${selectedId === row.pnpId ? "bg-blue-50/90" : "hover:bg-slate-50"}`}>
                        <td className="px-3 py-2 font-mono text-[12px]">{row.pnpId}</td><td className="px-3 py-2 font-semibold">{row.dn}</td><td className="px-3 py-2">{row.shortDescription}</td><td className="px-3 py-2 text-[12px] text-slate-500">{Object.keys(row.paramDefinition).join(", ")}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              {loadState === "error" && <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{status}</div>}
            </CardContent>
          </SettingsCard>
          <div className="flex min-h-0 flex-col gap-4">
            <SettingsCard title="Variant Editor" className="min-h-0 flex-[1.1]">
              <CardContent className="flex h-full min-h-0 flex-col gap-3 p-4">
                <SettingsField label="DN"><Input value={form.dn} onChange={(event) => setForm((current) => ({ ...current, dn: event.target.value }))} /></SettingsField>
                <SettingsField label="Override Desc"><Input value={form.shortDescription} onChange={(event) => setForm((current) => ({ ...current, shortDescription: event.target.value }))} /></SettingsField>
                <div className="min-h-0 flex-1 space-y-2 overflow-auto border-t border-slate-200 pt-3">
                  {params.map((param) => (
                    <SettingsField key={param.key} label={param.label} helpText={param.unit ? `${param.unit} / ${param.kind}` : param.kind}>
                      <Input type={param.kind === "number" ? "number" : "text"} value={form.params[param.key] ?? ""} onChange={(event) => updateParam(param.key, event.target.value)} />
                    </SettingsField>
                  ))}
                </div>
                <div className="grid grid-cols-3 gap-2 border-t border-slate-200 pt-3">
                  <Button onClick={() => runAction("add")}><Plus size={14} className="mr-1" />Add</Button>
                  <Button variant="outline" disabled={selectedId == null} onClick={() => runAction("update")}><Save size={14} className="mr-1" />Update</Button>
                  <Button variant="destructive" disabled={selectedId == null} onClick={() => runAction("delete")}><Trash2 size={14} className="mr-1" />Delete</Button>
                </div>
              </CardContent>
            </SettingsCard>
            <Card className="min-h-[178px] flex-[0.9] overflow-hidden !rounded-[14px] !border-slate-200/70 !bg-white/70">
              <CardContent className="flex h-full flex-col p-4">
                <div className="mb-3 flex items-center justify-between"><div className="flex items-center gap-2 font-bold text-slate-800"><Box size={17} className="text-blue-600" />3D Preview Panel</div><span className="rounded-full bg-slate-100 px-2 py-1 text-[11px] font-semibold text-slate-500">P3 stub</span></div>
                <motion.div initial={{ opacity: 0.72 }} animate={{ opacity: 1 }} className="flex flex-1 items-center justify-center rounded-xl border border-dashed border-slate-300 bg-slate-50/70 text-center text-sm text-slate-500"><div><Database className="mx-auto mb-2 text-slate-400" size={24} /><div>Mock DTO v1: {mockCatalogApi.dtoVersion}</div><div className="mt-1 text-[12px]">Canvas integration is deferred to P3.</div></div></motion.div>
              </CardContent>
            </Card>
          </div>
        </section>
        <footer className="flex min-h-[34px] items-center justify-between rounded-[10px] border border-white/60 bg-white/65 px-4 text-sm text-slate-600 shadow-sm backdrop-blur-xl"><span>{loadState === "loading" ? "Loading..." : status}</span><span>C# untouched / mock adapter / catalog.html entry</span></footer>
      </div>
    </main>
  );
}
