import { Loader2 } from "lucide-react";

export default function LoadingOverlay({ visible }: { visible: boolean }) {
  if (!visible) return null;
  return <div className="absolute inset-0 z-50 flex flex-col items-center justify-center gap-3 bg-white/90 text-slate-700 transition-opacity"><Loader2 className="animate-spin" size={30} /><span className="text-sm font-medium">PlantFlow_Support 로딩중</span></div>;
}
