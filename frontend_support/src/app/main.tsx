import React from "react";
import { createRoot } from "react-dom/client";
import AppShell from "./AppShell";
import "./app.css";

// P4-1b: PaletteSet KeepFocus 토글 주채널(셸 전역).
const hostWebview = (window as unknown as { chrome?: { webview?: { postMessage(msg: string): void } } }).chrome?.webview;
if (hostWebview) {
  const postUi = (event: "focus" | "blur") => {
    try { hostWebview.postMessage(JSON.stringify({ ch: "ui", event })); }
    catch (err) { console.debug("[ui] postMessage 실패", err); }
  };
  window.addEventListener("focusin", () => postUi("focus"));
  window.addEventListener("focusout", (e) => { if (!(e as FocusEvent).relatedTarget) postUi("blur"); });
  window.addEventListener("blur", () => postUi("blur"));
}

const root = document.getElementById("app-root");
if (!root) throw new Error("app-root element not found");
createRoot(root).render(
  <React.StrictMode>
    <AppShell />
  </React.StrictMode>
);
