import { runGoldenTests } from "./primitiv";
import { createMeshViewer } from "./viewer/meshViewer";

const hud = document.getElementById("hud") as HTMLDivElement;
const app = document.getElementById("app") as HTMLDivElement;

function post(s: string) {
  try {
    (window as any).chrome?.webview?.postMessage(s);
  } catch (err) {
    console.debug("[host] postMessage unavailable", err);
  }
}

function parseJsonText(raw: string): unknown {
  return JSON.parse(raw.replace(/^\uFEFF/, ""));
}

const viewer = createMeshViewer(app, {
  hud,
  postUnsupported: (type) => post(JSON.stringify({ kind: "unsupported", type }))
});

(window as any).chrome?.webview?.addEventListener?.("message", (e: any) => {
  const d = e?.data;
  if (typeof d === "string") viewer.handleMessage(d);
});

window.addEventListener("resize", viewer.resize);
viewer.resize();

async function loadMeshFromQuery() {
  const meshUrl = new URLSearchParams(window.location.search).get("mesh");
  if (!meshUrl) return false;

  try {
    const response = await fetch(meshUrl);
    if (!response.ok) throw new Error(`HTTP ${response.status} ${response.statusText}`);
    const data = parseJsonText(await response.text());
    viewer.handleMessage(JSON.stringify({ ...(data as object), kind: "mesh" }));
    return true;
  } catch (err) {
    console.error("[mesh] dev " + meshUrl, err);
    hud.textContent = `mesh 로드 실패: dev ${meshUrl}`;
    return true;
  }
}

const golden = runGoldenTests();
const fails = golden.filter((l) => l.startsWith("FAIL"));
console.log("[primitiv golden]\n" + golden.join("\n"));
if (fails.length) console.error("golden FAIL:", fails);

loadMeshFromQuery().then((loaded) => {
  if (!loaded) {
    viewer.handleMessage(JSON.stringify({ type: "BOX", params: { L: 300, W: 300, H: 300 } }));
    hud.textContent = "demo BOX(300,300,300) - 호스트 메시지 대기";
    hud.textContent += fails.length ? `\n⚠ golden ${fails.length} FAIL(콘솔)` : "";
  }
  post(JSON.stringify({ kind: "ready", golden: fails.length ? "fail" : "pass" }));
});
