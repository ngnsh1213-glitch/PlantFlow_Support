import type { AddVariantResult, CatalogApi, CatalogType, ParameterDef, PreviewRequest, VariantInput, VariantRow } from "./catalogApi";

type CatalogMethod = keyof Omit<CatalogApi, "dtoVersion">;

interface CatalogRequest {
  ch: "catalog";
  id: string;
  method: CatalogMethod;
  args: unknown[];
}

interface CatalogResponse<T = unknown> {
  ch: "catalog";
  id: string;
  ok: boolean;
  result?: T;
  error?: string;
}

interface WebViewBridge {
  postMessage(message: string): void;
  addEventListener(type: "message", listener: (event: MessageEvent) => void): void;
}

declare global {
  interface Window {
    chrome?: {
      webview?: WebViewBridge;
    };
  }
}

const REQUEST_TIMEOUT_MS = 15000;
let nextId = 1;

const pending = new Map<string, {
  resolve(value: unknown): void;
  reject(reason?: unknown): void;
  timeout: number;
}>();

let listenerAttached = false;

export function isWebViewCatalogAvailable(): boolean {
  return Boolean(window.chrome?.webview?.postMessage && window.chrome?.webview?.addEventListener);
}

function attachListener() {
  if (listenerAttached || !window.chrome?.webview) return;
  window.chrome.webview.addEventListener("message", (event: MessageEvent) => {
    const data = parseResponse(event.data);
    if (!data || data.ch !== "catalog" || !data.id) return;

    const slot = pending.get(data.id);
    if (!slot) return;

    window.clearTimeout(slot.timeout);
    pending.delete(data.id);
    if (data.ok) slot.resolve(data.result);
    else slot.reject(new Error(data.error || "Catalog bridge request failed."));
  });
  listenerAttached = true;
}

function parseResponse(data: unknown): CatalogResponse | null {
  if (typeof data === "string") {
    try {
      return JSON.parse(data) as CatalogResponse;
    } catch (err) {
      console.error("catalog bridge response parse failed", err);
      return null;
    }
  }
  if (data && typeof data === "object") return data as CatalogResponse;
  return null;
}

function request<T>(method: CatalogMethod, args: unknown[] = []): Promise<T> {
  if (!isWebViewCatalogAvailable()) {
    return Promise.reject(new Error("WebView2 catalog bridge is unavailable."));
  }

  attachListener();
  const id = `catalog-${Date.now()}-${nextId++}`;
  const envelope: CatalogRequest = { ch: "catalog", id, method, args };

  return new Promise<T>((resolve, reject) => {
    const timeout = window.setTimeout(() => {
      pending.delete(id);
      reject(new Error(`Catalog bridge request timed out: ${method}`));
    }, REQUEST_TIMEOUT_MS);

    pending.set(id, {
      resolve: (value) => resolve(value as T),
      reject,
      timeout
    });

    try {
      window.chrome?.webview?.postMessage(JSON.stringify(envelope));
    } catch (err) {
      window.clearTimeout(timeout);
      pending.delete(id);
      reject(err);
    }
  });
}

export const realCatalogApi: CatalogApi = {
  dtoVersion: "pfs-catalog-v1",
  listTypes: () => request<CatalogType[]>("listTypes"),
  listVariants: (type: string) => request<VariantRow[]>("listVariants", [type]),
  getParamKeys: (type: string) => request<ParameterDef[]>("getParamKeys", [type]),
  addVariant: (type: string, input: VariantInput) => request<AddVariantResult>("addVariant", [type, input]),
  updateVariant: (pnpId: number, type: string, input: VariantInput) => request<AddVariantResult>("updateVariant", [pnpId, type, input]),
  deleteVariant: (pnpId: number) => request<AddVariantResult>("deleteVariant", [pnpId]),
  loadAcat: () => request<AddVariantResult>("loadAcat"),
  preview: (preview: PreviewRequest) => request<AddVariantResult>("preview", [preview])
};
