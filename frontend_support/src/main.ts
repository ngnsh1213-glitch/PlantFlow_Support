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

type MeshPayload = {
  kind?: string;
  units?: string;
  upAxis?: string;
  vertices?: unknown;
  triangles?: unknown;
};

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
  clearCurrent();
  hud.textContent = "서버 형상 로딩 중...";
  post(JSON.stringify({ kind: "unsupported", type }));
}

function showMeshError(message: string, err?: unknown) {
  console.error("[mesh] " + message, err);
  clearCurrent();
  hud.textContent = `mesh 로드 실패: ${message}`;
}

// C# 호스트로 메시지(진단/상태).
function post(s: string) {
  try {
    (window as any).chrome?.webview?.postMessage(s);
  } catch (err) {
    console.debug("[host] postMessage unavailable", err);
  }
}

function parseJsonText(raw: string): any {
  return JSON.parse(raw.replace(/^\uFEFF/, ""));
}

function asNumberTriple(value: unknown, label: string, index: number): [number, number, number] {
  if (!Array.isArray(value) || value.length !== 3) {
    throw new Error(`${label}[${index}] must be [x,y,z]`);
  }
  const out = value.map((v) => Number(v));
  if (out.some((v) => !Number.isFinite(v))) {
    throw new Error(`${label}[${index}] contains non-finite number`);
  }
  return [out[0], out[1], out[2]];
}

function extractTriIndices(tri: unknown, index: number): unknown {
  if (Array.isArray(tri)) return tri;
  if (tri && typeof tri === "object" && Array.isArray((tri as { i?: unknown }).i)) {
    return (tri as { i: unknown[] }).i;
  }
  throw new Error(`triangles[${index}] unsupported shape`);
}

function buildMeshGeometry(data: MeshPayload): THREE.BufferGeometry {
  if (!Array.isArray(data.vertices) || !Array.isArray(data.triangles)) {
    throw new Error("vertices/triangles arrays are required");
  }
  const vertices = data.vertices;
  const triangles = data.triangles;
  if (vertices.length === 0 || triangles.length === 0) {
    throw new Error("vertices/triangles arrays must not be empty");
  }

  const flatVerts: number[] = [];
  vertices.forEach((v, i) => flatVerts.push(...asNumberTriple(v, "vertices", i)));

  const flatTriangles: number[] = [];
  triangles.forEach((tri, i) => {
    const t = asNumberTriple(extractTriIndices(tri, i), "triangles", i);
    for (const rawIndex of t) {
      if (!Number.isInteger(rawIndex)) {
        throw new Error(`triangles[${i}] contains non-integer index`);
      }
      if (rawIndex < 0 || rawIndex >= vertices.length) {
        throw new Error(`triangles[${i}] index ${rawIndex} out of range`);
      }
      flatTriangles.push(rawIndex);
    }
  });

  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute("position", new THREE.Float32BufferAttribute(flatVerts, 3));
  geometry.setIndex(flatTriangles);
  geometry.computeVertexNormals();
  return geometry;
}

function renderMesh(data: MeshPayload, source: string) {
  try {
    const geometry = buildMeshGeometry(data);
    const vertexCount = Array.isArray(data.vertices) ? data.vertices.length : 0;
    const triangleCount = Array.isArray(data.triangles) ? data.triangles.length : 0;
    showPrim(
      new Prim(geometry),
      `mesh ${source}  vertices=${vertexCount} triangles=${triangleCount} units=${data.units ?? "?"} upAxis=${data.upAxis ?? "?"}`
    );
  } catch (err) {
    showMeshError(source, err);
  }
}

// C#->JS: {type, params, sizeFields} or {kind:"mesh", vertices, triangles}
function handlePayload(raw: string) {
  let data: any;
  try {
    data = parseJsonText(raw);
  } catch (err) {
    console.debug("[host] ignored non-json message", err);
    return; // 비-JSON 메시지(예: 에코) 무시
  }
  if (data?.kind === "mesh") {
    renderMesh(data, "host");
    return;
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

async function loadMeshFromQuery() {
  const meshUrl = new URLSearchParams(window.location.search).get("mesh");
  if (!meshUrl) return false;

  try {
    const response = await fetch(meshUrl);
    if (!response.ok) throw new Error(`HTTP ${response.status} ${response.statusText}`);
    const data = parseJsonText(await response.text());
    renderMesh({ ...data, kind: "mesh" }, `dev ${meshUrl}`);
    return true;
  } catch (err) {
    showMeshError(`dev ${meshUrl}`, err);
    return true;
  }
}

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

// 초기 데모(메시지 오기 전): 단위 박스. ?mesh=<url>이면 export JSON을 직접 로드한다.
loadMeshFromQuery().then((loaded) => {
  if (!loaded) {
    showPrim(makeBasePrimitive("BOX", { L: 300, W: 300, H: 300 })!, "demo BOX(300,300,300) — 호스트 메시지 대기");
    hud.textContent += fails.length ? `\n⚠ golden ${fails.length} FAIL(콘솔)` : "";
  }
  post(JSON.stringify({ kind: "ready", golden: fails.length ? "fail" : "pass" }));
});
