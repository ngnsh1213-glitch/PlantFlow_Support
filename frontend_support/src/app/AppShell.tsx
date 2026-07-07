import { useState } from "react";
import type { ReactNode } from "react";
import { FileOutput, Tags, Rows3, Package } from "lucide-react";
import SupportCatalogView from "../catalog/views/SupportCatalogView";
import DrawingView from "./views/DrawingView";

type TabId = "Drawing" | "Tagging" | "Span" | "Catalog";
const TABS: { id: TabId; label: string; icon: ReactNode }[] = [
  { id: "Drawing", label: "Drawing & MTO",    icon: <FileOutput size={18} /> },
  { id: "Tagging", label: "Tagging & Coding", icon: <Tags size={18} /> },
  { id: "Span",    label: "Span",             icon: <Rows3 size={18} /> },
  { id: "Catalog", label: "Catalog",          icon: <Package size={18} /> },
];

function Placeholder({ label }: { label: string }) {
  return (
    <div className="flex h-full items-center justify-center text-slate-400">
      <div className="text-center">
        <div className="text-lg font-medium">{label}</div>
        <div className="mt-1 text-sm">준비 중 — 다음 단계에서 이관됩니다.</div>
      </div>
    </div>
  );
}

export default function AppShell() {
  const [active, setActive] = useState<TabId>("Catalog");

  return (
    <div className="flex h-screen w-screen flex-col bg-white">
      <nav className="flex shrink-0 gap-1 border-b border-slate-200 bg-slate-50 px-2 py-1">
        {TABS.map((t) => (
          <button
            key={t.id}
            onClick={() => setActive(t.id)}
            className={
              "flex items-center gap-2 rounded-md px-3 py-1.5 text-sm font-medium transition " +
              (active === t.id ? "bg-white text-slate-900 shadow-sm" : "text-slate-500 hover:text-slate-700")
            }
          >
            {t.icon}
            {t.label}
          </button>
        ))}
      </nav>
      <main className="relative min-h-0 flex-1 overflow-hidden">
        {active === "Catalog" ? <SupportCatalogView />
          : active === "Drawing" ? <DrawingView />
          : <Placeholder label={TABS.find((t) => t.id === active)!.label} />}
      </main>
    </div>
  );
}
