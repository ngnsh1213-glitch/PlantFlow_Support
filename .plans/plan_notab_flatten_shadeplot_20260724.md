# 계획서 — 무탭 평면화 ShadePlot=2D Wireframe 시도 (cycle 125, 2026-07-24)

## 선행 실측 (cycle124)
- EXPORTLAYOUT 체인 실증: 뷰포트 제거·주석 보존·`_flat.dwg` 생성·gate=GO. FLATSHOT 벽 우회 성공.
- ★단, 산출물 서포트가 **3D 솔리드 그대로**(사용자 육안: 각도 틀면 3D). EXPORTLAYOUT은 뷰포트 ShadePlot 설정을 따르는데
  현재 뷰포트=기본(As Displayed/wireframe)이라 3D 통합. **은선/2D 플롯 설정이 없어 평면화 안 됨.**

## 가설 (사용자 지정)
뷰포트 **`ShadePlot = 2D Wireframe`**로 설정 후 EXPORTLAYOUT하면 3D가 **2D 와이어프레임으로 투영**(전 에지 표시, 은선제거 없이 평면화)될 것.
- 사용자 선택: Hidden(은선제거) 아닌 **2D Wireframe**(전 에지). 서포트 디테일 관례상 모든 에지 표시.

## 설계 — PFSNOTABFLATTEN에 ShadePlot 설정 추가
- 위치: `PFSNOTABFLATTEN`에서 EXPORTLAYOUT 큐 **직전**, 활성 상세도 뷰포트(TryFindNotabFlattenViewport로 이미 획득)에 적용.
- 활성 doc 트랜잭션으로 뷰포트 ForWrite → `ShadePlot`을 2D Wireframe로 → commit → 그 상태로 EXPORTLAYOUT.
- 기존 `TrySetViewportShadePlotHidden`(5766, 리플렉션으로 ShadePlot enum "Hidden" 매칭) 패턴 재사용/일반화.
  ★단 "2D Wireframe"은 `ShadePlotType` enum 직접값이 아닐 수 있음(VisualStyle+2dWireframe 비주얼스타일 필요) — 자문으로 정확 API 확정.

## 자문 확인 사항
1. AutoCAD API에서 뷰포트 "ShadePlot = 2D Wireframe" 정확 설정법: `ShadePlotType.Wireframe`인가, 아니면 `ShadePlotType.VisualStyle`+2dWireframe 비주얼스타일 id인가.
2. EXPORTLAYOUT이 ShadePlot=2D Wireframe 뷰포트를 실제로 2D로 평면화하는가(Hidden만 되는지, Wireframe도 되는지 불확실).
3. 적용 지점(활성 detail 뷰포트, EXPORTLAYOUT 큐 직전)이 EXPORTLAYOUT에 반영되는가 — 저장 없이 메모리 상태로 충분한지.

## 스파이크 게이트
- RC1 상세도 오픈 → PFSNOTABFLATTEN → `_flat.dwg` 열어 **각도 틀어 확인**: 서포트가 2D 선요소(3D 솔리드 아님)인가.
- 선택 시 Properties가 Line/Polyline 등(3D Solid 아님). 주석 보존 유지.
- 로그 `PFSNOTABFLATTEN3 shadeplot=set-2dwf` + 기존 FIN 카운트(3D solid members 대신 line/poly 증가 확인).

## ★자문 경고 (Codex, 반영)
- **저확률**: cycle124 기본 뷰포트가 이미 "2D 와이어프레임"이었는데 3D 솔리드로 나옴 → Wireframe 명시가 결과를 바꿀 확률 낮음.
  가장 가능성 높은 결과 = 또 3D 솔리드. **사용자 지정이라 1회 시도하되 저확률임을 인지.**
- API: `ShadePlot = ShadePlotType.Wireframe`(정확 enum 매칭). 5766의 부분문자열 "Hidden" 매칭 방식 → "Wireframe" 정확 매칭 + **enum 목록·기존 VisualStyleId 로그**.
- 성공 판정: 선 개수 부족 → `_flat.dwg`에 **3DSOLID 부재** 확인이 필수(사용자 3DORBIT/속성).

## 폴백 (Wireframe 실패 시 — 자문상 유력)
- **(B) FLATTEN 익스프레스 툴**(명령행 3D→2D 투영, 대화상자 없음)로 전환. Hidden으로 요구 변경은 안 함(전 에지 요구 우선).
  FLATTEN 스크립트 가능성·전 에지 보존 여부가 다음 자문 대상.

## 검증
- `_flat.dwg` 서포트 2D 확인 + 주석 보존. 원본 비파괴. `dotnet build` 오류 0.
