import React from "react";
import { createRoot } from "react-dom/client";
import SupportCatalogView from "./views/SupportCatalogView";
import "./catalog.css";

// P4-1b: AutoCAD PaletteSet KeepFocus 토글 주채널. C#(WebViewControl.TryHandleUiMessage)이
// {"ch":"ui","event":"focus"|"blur"} 문자열 메시지를 KeepFocus 토글로 변환한다.
const hostWebview = (window as unknown as { chrome?: { webview?: { postMessage(msg: string): void } } }).chrome?.webview;
if (hostWebview) {
  const postUi = (event: "focus" | "blur") => {
    try {
      hostWebview.postMessage(JSON.stringify({ ch: "ui", event }));
    } catch (err) {
      console.debug("[ui] postMessage 실패(호스트 부재?)", err);
    }
  };
  window.addEventListener("focusin", () => postUi("focus"));
  // relatedTarget 없음 = 포커스가 문서 밖(AutoCAD 등)으로 이탈.
  window.addEventListener("focusout", (e) => {
    if (!(e as FocusEvent).relatedTarget) postUi("blur");
  });
  window.addEventListener("blur", () => postUi("blur")); // WebView 자체가 OS 포커스 상실
}

const root = document.getElementById("catalog-root");
if (!root) {
  throw new Error("catalog-root element not found");
}

createRoot(root).render(
  <React.StrictMode>
    <SupportCatalogView />
  </React.StrictMode>
);
