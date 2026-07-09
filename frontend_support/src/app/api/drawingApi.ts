interface DrawingResponse<T = unknown> {
  ch: "drawing";
  id: string;
  ok: boolean;
  result?: T;
  code?: string;
  error?: string;
}

interface WebViewBridge {
  postMessage(message: string): void;
  addEventListener(type: "message", listener: (event: MessageEvent) => void): void;
}

function getWebview(): WebViewBridge | undefined {
  return (window as unknown as { chrome?: { webview?: WebViewBridge } }).chrome?.webview;
}

const DEFAULT_TIMEOUT = 15000;
const PICK_TIMEOUT = 180000;
let nextId = 1;
const pending = new Map<string, { resolve(v: unknown): void; reject(e: unknown): void; timeout: number }>();
let listenerAttached = false;

export function isDrawingBridgeAvailable(): boolean {
  const webview = getWebview();
  return Boolean(webview?.postMessage && webview?.addEventListener);
}

function attachListener() {
  const webview = getWebview();
  if (listenerAttached || !webview) return;
  webview.addEventListener("message", (event: MessageEvent) => {
    let data: DrawingResponse | null = null;
    try {
      data = typeof event.data === "string" ? JSON.parse(event.data) : (event.data as DrawingResponse);
    } catch (err) {
      console.error("drawing bridge response parse failed", err);
      return;
    }
    if (!data || data.ch !== "drawing" || !data.id) return;
    const slot = pending.get(data.id);
    if (!slot) return;
    window.clearTimeout(slot.timeout);
    pending.delete(data.id);
    if (data.ok) slot.resolve(data.result);
    else slot.reject({ code: data.code ?? "ERX", error: data.error ?? "drawing bridge failed" });
  });
  listenerAttached = true;
}

function request<T>(method: string, args: unknown[] = [], timeoutMs = DEFAULT_TIMEOUT): Promise<T> {
  const webview = getWebview();
  if (!webview) return Promise.reject({ code: "ER_NOBRIDGE", error: "WebView2 bridge unavailable" });
  attachListener();
  const id = `drawing-${Date.now()}-${nextId++}`;
  return new Promise<T>((resolve, reject) => {
    const timeout = window.setTimeout(() => {
      pending.delete(id);
      reject({ code: "ER_TIMEOUT", error: `timed out: ${method}` });
    }, timeoutMs);
    pending.set(id, { resolve: (v) => resolve(v as T), reject, timeout });
    try {
      webview.postMessage(JSON.stringify({ ch: "drawing", id, method, args }));
    } catch (err) {
      window.clearTimeout(timeout);
      pending.delete(id);
      reject({ code: "ERX", error: String(err) });
    }
  });
}

export interface DrawingSupport { name: string; view: string; }
export interface DrawingState { supports: DrawingSupport[]; gridReady: boolean; captureDoc: string; }
export interface DrawingSettings { template: string; projectNo: string; revision: string; }
export interface AddSummary { count: number; duplicates: number; unnamed: number; invalid: number; view: string; }

export const drawingApi = {
  getState: () => request<DrawingState>("getState"),
  getSettings: () => request<DrawingSettings>("getSettings"),
  setSetting: (key: string, val: string) => request<{ ok: boolean }>("setSetting", [key, val]),
  addFromSelection: (views: string[]) => request<{ added: AddSummary; state: DrawingState }>("addFromSelection", [views], PICK_TIMEOUT),
  removeSupport: (name: string) => request<DrawingState>("removeSupport", [name]),
  clearSupports: () => request<DrawingState>("clearSupports"),
  export2D: (names: string[]) => request<{ ok: boolean }>("export2D", [names], PICK_TIMEOUT),
};
