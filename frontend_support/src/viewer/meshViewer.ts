import * as THREE from "three";
import { OrbitControls } from "three/examples/jsm/controls/OrbitControls.js";
import { makeBasePrimitive, Prim } from "../primitiv";

THREE.Object3D.DEFAULT_UP.set(0, 0, 1);

export type MeshViewerHandle = {
  handleMessage(raw: string): void;
  resize(): void;
  dispose(): void;
};

type MeshViewerOptions = {
  hud?: HTMLElement | ((text: string) => void);
  postUnsupported?: (type: string) => void;
};

type MeshPayload = {
  kind?: string;
  units?: string;
  upAxis?: string;
  vertices?: unknown;
  triangles?: unknown;
};

function parseJsonText(raw: string): unknown {
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

function disposeMaterial(material: THREE.Material | THREE.Material[]) {
  if (Array.isArray(material)) {
    material.forEach((m) => m.dispose());
    return;
  }
  material.dispose();
}

export function createMeshViewer(container: HTMLElement, opts: MeshViewerOptions = {}): MeshViewerHandle {
  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0xf0f0f0);

  const camera = new THREE.PerspectiveCamera(50, 1, 1, 100000);
  camera.up.set(0, 0, 1);
  camera.position.set(600, -800, 500);

  const renderer = new THREE.WebGLRenderer({ antialias: true });
  renderer.setPixelRatio(window.devicePixelRatio);
  container.appendChild(renderer.domElement);

  const controls = new OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true;

  scene.add(new THREE.AmbientLight(0xffffff, 0.7));
  const dir = new THREE.DirectionalLight(0xffffff, 0.8);
  dir.position.set(1, -1, 1);
  scene.add(dir);

  const grid = new THREE.GridHelper(2000, 20, 0xcccccc, 0xe5e5e5);
  grid.rotation.x = Math.PI / 2;
  scene.add(grid);
  scene.add(new THREE.AxesHelper(300));

  let current: THREE.Object3D | null = null;
  let animationFrameId = 0;
  let disposed = false;

  const setHud = (text: string) => {
    if (typeof opts.hud === "function") opts.hud(text);
    else if (opts.hud) opts.hud.textContent = text;
  };

  const clearCurrent = () => {
    if (!current) return;
    scene.remove(current);
    current.traverse((o) => {
      const mesh = o as THREE.Mesh;
      if (mesh.geometry) mesh.geometry.dispose();
      if (mesh.material) disposeMaterial(mesh.material as THREE.Material | THREE.Material[]);
    });
    current = null;
  };

  const frame = (obj: THREE.Object3D) => {
    const box = new THREE.Box3().setFromObject(obj);
    const c = box.getCenter(new THREE.Vector3());
    const size = box.getSize(new THREE.Vector3()).length() || 500;
    controls.target.copy(c);
    const d = size * 1.4;
    camera.position.set(c.x + d, c.y - d, c.z + d * 0.7);
    camera.near = Math.max(1, size / 100);
    camera.far = size * 100;
    camera.updateProjectionMatrix();
  };

  const showPrim = (prim: Prim, label: string) => {
    if (disposed) return;
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
    setHud(label);
  };

  const showPlaceholder = (type: string) => {
    if (disposed) return;
    clearCurrent();
    setHud("서버 형상 로딩 중...");
    opts.postUnsupported?.(type);
  };

  const showMeshError = (message: string, err?: unknown) => {
    console.error("[mesh] " + message, err);
    clearCurrent();
    setHud(`mesh 로드 실패: ${message}`);
  };

  const renderMesh = (data: MeshPayload, source: string) => {
    if (disposed) return;
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
  };

  const handleMessage = (raw: string) => {
    if (disposed) return;
    let data: any;
    try {
      data = parseJsonText(raw);
    } catch (err) {
      console.debug("[host] ignored non-json message", err);
      return;
    }
    if (data?.ch === "catalog") return;
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
  };

  const resize = () => {
    if (disposed) return;
    const w = Math.max(1, container.clientWidth);
    const h = Math.max(1, container.clientHeight);
    renderer.setSize(w, h, false);
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
  };

  const animate = () => {
    if (disposed) return;
    animationFrameId = window.requestAnimationFrame(animate);
    controls.update();
    renderer.render(scene, camera);
  };

  resize();
  animate();

  return {
    handleMessage,
    resize,
    dispose() {
      if (disposed) return;
      disposed = true;
      if (animationFrameId) window.cancelAnimationFrame(animationFrameId);
      controls.dispose();
      clearCurrent();
      scene.traverse((obj) => {
        const mesh = obj as THREE.Mesh;
        if (mesh.geometry) mesh.geometry.dispose();
        if (mesh.material) disposeMaterial(mesh.material as THREE.Material | THREE.Material[]);
      });
      renderer.dispose();
      renderer.forceContextLoss();
      renderer.domElement.remove();
    }
  };
}
