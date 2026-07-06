import * as THREE from "three";

// varmain.primitiv 프리미티브의 three.js 포팅 (primitiv_spec.md §4 기준).
// 핵심: Plant3D 변환은 월드원점 기준이고 geometry에 baked → THREE.Object3D transform이 아니라
// BufferGeometry.applyMatrix4로 정점에 직접 누적 bake한다.
// 좌표: Z-up, 1 unit = 1 mm.

const RAD = Math.PI / 180;

// 한 프리미티브(이미 placed local 좌표로 bake된 geometry 보유). 변환은 월드원점 좌측곱.
export class Prim {
  geometry: THREE.BufferGeometry;
  constructor(geometry: THREE.BufferGeometry) {
    this.geometry = geometry;
  }
  translate(v: [number, number, number]): this {
    this.geometry.applyMatrix4(new THREE.Matrix4().makeTranslation(v[0], v[1], v[2]));
    return this;
  }
  rotateX(deg: number): this {
    this.geometry.applyMatrix4(new THREE.Matrix4().makeRotationX(deg * RAD));
    return this;
  }
  rotateY(deg: number): this {
    this.geometry.applyMatrix4(new THREE.Matrix4().makeRotationY(deg * RAD));
    return this;
  }
  rotateZ(deg: number): this {
    this.geometry.applyMatrix4(new THREE.Matrix4().makeRotationZ(deg * RAD));
    return this;
  }
  extents(): THREE.Box3 {
    this.geometry.computeBoundingBox();
    return this.geometry.boundingBox!.clone();
  }
}

// BOX(L,W,H): 원점중심, 축매핑 X<-H, Y<-L, Z<-W.
export function BOX(L: number, W: number, H: number): Prim {
  return new Prim(new THREE.BoxGeometry(H, L, W));
}

// CYLINDER(R,H,O): 축 Z, 밑면 z=O, 높이 H(+Z), 반지름 R.
export function CYLINDER(R: number, H: number, O = 0, segments = 48): Prim {
  const g = new THREE.CylinderGeometry(R, R, H, segments); // 축 Y, y∈[-H/2,H/2]
  g.applyMatrix4(new THREE.Matrix4().makeRotationX(90 * RAD)); // 축 Y->Z
  g.applyMatrix4(new THREE.Matrix4().makeTranslation(0, 0, O + H / 2)); // 밑면 z=O
  return new Prim(g);
}

// CYLINDER 타원기둥(R1,R2,H,O): X반경 R1, Y반경 R2, 축 Z. (Z정렬 회전 전에 로컬 스케일)
export function CYLINDER_ELLIPSE(R1: number, R2: number, H: number, O = 0, segments = 48): Prim {
  const g = new THREE.CylinderGeometry(1, 1, H, segments); // 단위원통, 축 Y
  g.scale(R1, 1, R2); // 로컬 단면: X=R1, Z=R2 (Y=높이 유지)
  g.applyMatrix4(new THREE.Matrix4().makeRotationX(90 * RAD)); // 축 Z, 단면 X=R1·Y=R2
  g.applyMatrix4(new THREE.Matrix4().makeTranslation(0, 0, O + H / 2));
  return new Prim(g);
}

// TORUS(R1,R2): XY평면 링, 링반경 R1, 튜브반경 R2, 원점중심.
export function TORUS(R1: number, R2: number, seg = 48): Prim {
  return new Prim(new THREE.TorusGeometry(R1, R2, 20, seg));
}

// SPHERE(R): 원점중심.
export function SPHERE(R: number, seg = 32): Prim {
  return new Prim(new THREE.SphereGeometry(R, seg, Math.max(12, seg / 2)));
}

// 베이스 프리미티브 라우팅(파라미터 객체 기반). Phase 2 shapeRegistry의 토대.
export function makeBasePrimitive(type: string, p: Record<string, number>): Prim | null {
  switch (type.toUpperCase()) {
    case "BOX":
      return BOX(p.L ?? 100, p.W ?? 100, p.H ?? 100);
    case "CYLINDER":
      if (p.R1 !== undefined && p.R2 !== undefined)
        return CYLINDER_ELLIPSE(p.R1, p.R2, p.H ?? 100, p.O ?? 0);
      return CYLINDER(p.R ?? 50, p.H ?? 100, p.O ?? 0);
    case "TORUS":
      return TORUS(p.R1 ?? 100, p.R2 ?? 20);
    case "SPHERE":
      return SPHERE(p.R ?? 50);
    default:
      return null;
  }
}

// 골든 자가검증(primitiv_spec.md §4 관측값과 대조). 콘솔에 PASS/FAIL 출력.
export function runGoldenTests(): string[] {
  const out: string[] = [];
  const approx = (a: number, b: number) => Math.abs(a - b) < 1e-6;
  const chk = (name: string, box: THREE.Box3, mn: number[], mx: number[]) => {
    const ok =
      approx(box.min.x, mn[0]) && approx(box.min.y, mn[1]) && approx(box.min.z, mn[2]) &&
      approx(box.max.x, mx[0]) && approx(box.max.y, mx[1]) && approx(box.max.z, mx[2]);
    const line = `${ok ? "PASS" : "FAIL"} ${name} min(${box.min.x.toFixed(1)},${box.min.y.toFixed(1)},${box.min.z.toFixed(1)}) max(${box.max.x.toFixed(1)},${box.max.y.toFixed(1)},${box.max.z.toFixed(1)})`;
    out.push(line);
  };

  chk("BOX(100,200,50)", BOX(100, 200, 50).extents(), [-25, -50, -100], [25, 50, 100]);
  chk("CYLINDER(R50,H300,O0)", CYLINDER(50, 300, 0).extents(), [-50, -50, 0], [50, 50, 300]);
  chk("CYLINDER(R1=60,R2=30,H300)", CYLINDER_ELLIPSE(60, 30, 300, 0).extents(), [-60, -30, 0], [60, 30, 300]);
  chk("TORUS(100,20)", TORUS(100, 20).extents(), [-120, -120, -20], [120, 120, 20]);
  chk("SPHERE(80)", SPHERE(80).extents(), [-80, -80, -80], [80, 80, 80]);

  // 변환 검증: BOX(100,100,100) translate((10,20,30)) -> rotateZ(90)
  const t = BOX(100, 100, 100).translate([10, 20, 30]).rotateZ(90);
  chk("BOX translate->rotateZ90", t.extents(), [-70, -40, -20], [30, 60, 80]);
  const t2 = BOX(100, 100, 100).translate([10, 20, 30]).rotateZ(90).rotateX(90);
  chk("...->rotateX90", t2.extents(), [-70, -80, -40], [30, 20, 60]);

  return out;
}
