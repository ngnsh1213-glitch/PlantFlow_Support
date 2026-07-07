import { useEffect, useRef } from "react";
import { createMeshViewer, type MeshViewerHandle } from "../../viewer/meshViewer";

type WebViewMessageEvent = MessageEvent<string>;
type WebViewMessageTarget = {
  addEventListener(type: "message", listener: (event: WebViewMessageEvent) => void): void;
  removeEventListener(type: "message", listener: (event: WebViewMessageEvent) => void): void;
};

export default function MeshViewer() {
  const containerRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const viewer: MeshViewerHandle = createMeshViewer(container);
    const onMessage = (event: WebViewMessageEvent) => {
      if (typeof event.data === "string") viewer.handleMessage(event.data);
    };
    const webview = window.chrome?.webview as WebViewMessageTarget | undefined;
    webview?.addEventListener("message", onMessage);

    const resizeObserver = new ResizeObserver(() => viewer.resize());
    resizeObserver.observe(container);
    viewer.resize();

    return () => {
      webview?.removeEventListener("message", onMessage);
      resizeObserver.disconnect();
      viewer.dispose();
    };
  }, []);

  return <div ref={containerRef} className="h-full min-h-[150px] w-full overflow-hidden rounded-xl bg-slate-100" />;
}
