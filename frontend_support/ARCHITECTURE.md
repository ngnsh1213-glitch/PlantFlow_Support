# PFS Preview B Architecture

작성일: 2026-07-06

## 1. 개요

Preview B는 PlantFlow Support의 서포트 파라미터를 WebView2 안의 three.js 라이브 3D 렌더로 보여주는 읽기 전용 프리뷰다.

정식 스택은 **vanilla TypeScript + three.js + Vite**다. React는 Preview B에 도입하지 않는다. 현재 화면은 단일 WebGL 캔버스와 최소 HUD 중심이며, React용 추상계층이나 상태관리 계층을 두지 않는다.

근거:
- `package.json`: `three` 의존성만 존재, React 의존성 없음
- `src/main.ts`: scene/camera/renderer/OrbitControls/WebView2 message handling
- `src/primitiv.ts`: BOX/CYLINDER/TORUS/SPHERE 프리미티브
- `PlantFlow_Support/UI/WebViewControl.cs`: WebView2 호스트

## 2. WebView2 메시지 계약

브리지는 `postMessage` 기반 비동기 메시지만 사용한다. `AddHostObjectToScript`는 Preview B 계약에 포함하지 않는다.

### C# -> frontend

`WebViewControl.LoadShape()`가 JSON 문자열을 `PostWebMessageAsString`으로 전송한다.

```ts
export interface PreviewShapeMessageV1 {
  type: string;
  params: Record<string, string | number>;
  sizeFields?: Record<string, string | number>;
}

export interface PreviewMeshMessageV1 {
  kind: "mesh";
  units?: "mm" | string;
  upAxis?: "Z" | string;
  vertices: [number, number, number][];
  triangles: ([number, number, number] | {
    i: [number, number, number];
    source?: string;
    diagnostic?: boolean;
  })[];
}
```

frontend는 `window.chrome.webview.addEventListener("message", ...)`에서 문자열 메시지를 받고, `JSON.parse` 후 `type`과 `params`를 해석한다. `params` 값은 숫자로 파싱 가능한 값만 렌더 입력으로 사용한다.
mesh 메시지는 C# export JSON의 UTF-8 BOM 가능성을 고려해 선행 `\uFEFF`를 제거한 뒤 파싱한다.
`{ kind: "mesh", vertices, triangles }`는 `THREE.BufferGeometry(position + index)`로 변환하고 `computeVertexNormals()` 후 기존 렌더 경로로 표시한다.
`triangles`는 production 평배열 `[a,b,c]`와 스파이크 export 객체형 `{ i:[a,b,c], source?, diagnostic? }`를 모두 수용한다. `source`와 `diagnostic`은 뷰어가 무시하는 진단 메타다.

### frontend -> C#

frontend는 상태 메시지를 `window.chrome.webview.postMessage()`로 보낸다.

```ts
export type PreviewHostMessageV1 =
  | { kind: "ready"; golden: "pass" | "fail" }
  | { kind: "unsupported"; type: string };
```

버전 규칙:
- 현재 계약 버전은 V1이다.
- 기존 필드의 의미를 바꾸지 않는다.
- 새 필드는 optional로 추가한다.
- breaking change가 필요하면 V2 타입을 새로 정의하고 C# 수신부와 frontend 수신부를 같은 계획에서 함께 갱신한다.

## 3. 좌표계와 단위

좌표계는 AutoCAD/Plant3D와 같은 **Z-up**을 유지한다.

근거:
- `src/main.ts`: `THREE.Object3D.DEFAULT_UP.set(0, 0, 1)`
- `src/main.ts`: `camera.up.set(0, 0, 1)`
- `src/main.ts`: grid helper를 XY 평면에 놓기 위해 `grid.rotation.x = Math.PI / 2`
- `src/primitiv.ts`: 주석과 구현 기준 `1 unit = 1 mm`

단위 계약:
- AutoCAD/Plant3D mm = three.js 1 unit
- 별도 스케일 팩터는 두지 않는다.
- 프리미티브와 변환은 geometry에 `applyMatrix4`로 bake한다.
- Object3D transform을 의미 좌표 변환의 주 수단으로 쓰지 않는다.

## 4. 버전 고정 원칙

현재 고정 기준:
- `three`: `^0.160.0`
- `@types/three`: `^0.160.0`
- `vite`: `^5.2.0`
- `typescript`: `^5.4.0`

업그레이드 원칙:
- three.js minor/major 변경은 geometry extents와 OrbitControls 동작을 다시 확인한 뒤 적용한다.
- Vite/TypeScript 변경은 `npm run build`와 golden 자가검증 로그를 함께 확인한다.
- lockfile 변경은 의존성 변경으로 취급하고, 변경 사유를 계획서나 핸드오프에 남긴다.

## 5. 빌드 산출물과 로딩 경로

frontend 빌드 산출물은 `frontend_support/dist`다.

런타임 로딩 흐름:
1. `WebViewControl.FindDistFolder()`가 실행 어셈블리 위치에서 상위 8단계까지 `frontend_support/dist/index.html`을 탐색한다.
2. dist를 찾으면 `SetVirtualHostNameToFolderMapping("pfsupport.local", dist, Allow)`로 매핑한다.
3. WebView2는 `http://pfsupport.local/index.html`로 탐색한다.
4. dist가 없으면 `InitFailed`를 발생시키고 호출 측은 PNG Preview A로 폴백한다.

개발 서버 포트는 `http://localhost:5174/`로 예약되어 있다. PFO의 5173 포트와 충돌하지 않기 위한 분리다.

## 6. three.js 수명주기와 Dispose

현재 구현 현황:
- `src/main.ts:clearCurrent()`는 현재 오브젝트를 scene에서 제거한다.
- traverse 중 `geometry.dispose()`만 호출한다.
- `MeshStandardMaterial`, `LineBasicMaterial`, `EdgesGeometry`, `WebGLRenderer` dispose는 아직 코드에 반영되어 있지 않다.

정책:
- Preview B가 장시간 AutoCAD 프로세스 안에서 반복 사용될 수 있으므로 geometry/material/edge geometry/renderer/WebGL context 수명주기를 명시적으로 관리해야 한다.
- 본 문서화 작업에서는 동작 코드를 수정하지 않는다.
- dispose 코드 보강은 별도 트랙으로 다루며, 최소 변경 범위는 `clearCurrent()`와 WebViewControl 종료/Dispose 경로다.

## 7. 프리미티브와 미지원 타입

현재 포팅된 기본 프리미티브:
- `BOX`
- `CYLINDER`
- `TORUS`
- `SPHERE`

구현 위치:
- `src/primitiv.ts`: 프리미티브 생성, geometry-level transform bake, golden 자가검증
- `src/main.ts`: `makeBasePrimitive(type, params)` 호출 후 렌더

미포팅 타입 정책:
- 미지원 타입은 `showPlaceholder(type)`로 회색 박스와 안내 문구를 보여준다.
- placeholder는 근사 렌더가 아니다.
- frontend는 C#으로 `{ kind: "unsupported", type }` 메시지를 보낸다.

자가검증:
- `runGoldenTests()`는 BOX/CYLINDER/TORUS/SPHERE 및 변환 extents를 콘솔에 PASS/FAIL로 출력한다.
- 신규 시각 회귀 테스트를 만들지 않고 기존 golden 자가검증을 재활용한다.

## 8. React 이관 시 유지 계약

향후 PFS 본 UI를 React로 이관하더라도 아래 계약은 유지한다.

- WebView2 메시지 계약: `PreviewShapeMessageV1`, `PreviewHostMessageV1`
- 파라미터 스키마: `type`, `params`, optional `sizeFields`
- DOM 진입점: `#app`
- WebView2 handler: `window.chrome.webview.postMessage`, `window.chrome.webview.addEventListener("message", ...)`
- Z-up/mm 좌표계
- golden 자가검증

React를 도입하더라도 React는 이 계약 위에 얹는다. Preview 캔버스와 three.js renderer는 React 밖의 명령형 모듈로 유지할 수 있다.

금지:
- Preview B를 위해 React용 추상계층을 선제 도입하지 않는다.
- 범용 이벤트버스, 상태관리 라이브러리, 렌더러 플러그인 구조를 만들지 않는다.
- Playwright 픽셀비교 등 신규 시각 회귀테스트를 만들지 않는다.
