import * as THREE from "three";
import { OrbitControls } from "three/examples/jsm/controls/OrbitControls.js";
import { makeBasePrimitive, runGoldenTests, Prim } from "./primitiv";

// Z-up (primitiv_spec.md §4). geometry보다 camera/controls 방향에 영향.
THREE.Object3D.DEFAULT_UP.set(0, 0, 1);

const hud = document.getElementById("hud") as HTMLDivElement;
const app = document.getElementById("app") as HTMLDivElement;

const scene = new THREE.Scene();
scene.background = new THREE.Color(0xf0f0f0);

const camera = new THREE.PerspectiveCamera(50, 1, 1, 100000);
camera.up.set(0, 0, 1);
camera.position.set(600, -800, 500);

const renderer = new THREE.WebGLRenderer({ antialias: true });
renderer.setPixelRatio(window.devicePixelRatio);
app.appendChild(renderer.domElement);

const controls = new OrbitControls(camera, renderer.domElement);
controls.enableDamping = true;

scene.add(new THREE.AmbientLight(0xffffff, 0.7));
const dir = new THREE.DirectionalLight(0xffffff, 0.8);
dir.position.set(1, -1, 1);
scene.add(dir);

const grid = new THREE.GridHelper(2000, 20, 0xcccccc, 0xe5e5e5);
grid.rotation.x = Math.PI / 2; // XY 평면(Z-up)
scene.add(grid);
scene.add(new THREE.AxesHelper(300));

let current: THREE.Object3D | null = null;

function clearCurrent() {
  if (current) {
    scene.remove(current);
    current.traverse((o) => {
      const m = o as THREE.Mesh;
      if (m.geometry) m.geometry.dispose();
    });
    current = null;
  }
}

function frame(obj: THREE.Object3D) {
  const box = new THREE.Box3().setFromObject(obj);
  const c = box.getCenter(new THREE.Vector3());
  const size = box.getSize(new THREE.Vector3()).length() || 500;
  controls.target.copy(c);
  const d = size * 1.4;
  camera.position.set(c.x + d, c.y - d, c.z + d * 0.7);
  camera.near = Math.max(1, size / 100);
  camera.far = size * 100;
  camera.updateProjectionMatrix();
}

function showPrim(prim: Prim, label: string) {
  clearCurrent();
  const mesh = new THREE.Mesh(
    prim.geometry,
    new THREE.MeshStandardMaterial({ color: 0x4a90d9, metalness: 0.1, roughness: 0.6 })
  );
  const edges = new THREE.LineSegments(
    new THREE.EdgesGeometry(prim.geometry, 20),
    new THREE.LineBasicMaterial({ color: 0x1b3a5c })
  );
  const group = new THREE.Group();
  group.add(mesh);
  group.add(edges);
  scene.add(group);
  current = group;
  frame(group);
  hud.textContent = label;
}

function showPlaceholder(type: string) {
  // Phase 2 미포팅 타입: 회색 박스 + 안내(근사 아님, 명시적 미지원).
  clearCurrent();
  const mesh = new THREE.Mesh(
    new THREE.BoxGeometry(300, 300, 300),
    new THREE.MeshStandardMaterial({ color: 0x999999, transparent: true, opacity: 0.5 })
  );
  scene.add(mesh);
  current = mesh;
  frame(mesh);
  hud.textContent = `'${type}' 형상은 아직 미포팅(Phase 2)\n기본 프리미티브(BOX/CYLINDER/TORUS/SPHERE)만 렌더`;
  post(JSON.stringify({ kind: "unsupported", type }));
}

// C# 호스트로 메시지(진단/상태).
function post(s: string) {
  try {
    (window as any).chrome?.webview?.postMessage(s);
  } catch {
    /* 호스트 외부(브라우저 dev) — 무시 */
  }
}

// C#->JS: {type, params, sizeFields}
function handlePayload(raw: string) {
  let data: any;
  try {
    data = JSON.parse(raw);
  } catch {
    return; // 비-JSON 메시지(예: 에코) 무시
  }
  if (!data || typeof data.type !== "string") return;
  const type: string = data.type;
  const params: Record<string, number> = {};
  const src = data.params || {};
  for (const k of Object.keys(src)) {
    const n = parseFloat(src[k]);
    if (!Number.isNaN(n)) params[k] = n;
  }
  const prim = makeBasePrimitive(type, params);
  if (prim) showPrim(prim, `${type}  ${JSON.stringify(params)}`);
  else showPlaceholder(type);
}

(window as any).chrome?.webview?.addEventListener?.("message", (e: any) => {
  const d = e?.data;
  if (typeof d === "string") handlePayload(d);
});

// 리사이즈
function resize() {
  const w = app.clientWidth || window.innerWidth;
  const h = app.clientHeight || window.innerHeight;
  renderer.setSize(w, h, false);
  camera.aspect = w / h;
  camera.updateProjectionMatrix();
}
window.addEventListener("resize", resize);
resize();

// 렌더 루프
function animate() {
  requestAnimationFrame(animate);
  controls.update();
  renderer.render(scene, camera);
}
animate();

// 골든 자가검증 — 콘솔 출력(개발 검증). FAIL 있으면 HUD 경고.
const golden = runGoldenTests();
const fails = golden.filter((l) => l.startsWith("FAIL"));
console.log("[primitiv golden]\n" + golden.join("\n"));
if (fails.length) console.error("golden FAIL:", fails);

// 초기 데모(메시지 오기 전): 단위 박스.
showPrim(makeBasePrimitive("BOX", { L: 300, W: 300, H: 300 })!, "demo BOX(300,300,300) — 호스트 메시지 대기");
hud.textContent += fails.length ? `\n⚠ golden ${fails.length} FAIL(콘솔)` : "";
post(JSON.stringify({ kind: "ready", golden: fails.length ? "fail" : "pass" }));
