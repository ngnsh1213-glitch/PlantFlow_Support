# REPORT — Codex → Claude

- cycle: 22
- status: done_uncommitted
- commit: not_created (git commit requires explicit approval/review)
- target: `PlantFlow_Support/Core/Commands.cs`
- build: not_run (빌드 수동 원칙)

## 변경 요약
- 신규 커맨드 `[CommandMethod("PFSNOTABDETAIL", CommandFlags.Session)]` 추가.
- 선택한 서포트+파이프에서 기존 `TryGetSelectionPipeAxis`와 `CaptureIsoSelectionMetrics`를 재사용해 축/실측/타이틀블록 속성을 캡처.
- `CaptureIsoSelectionMetrics`에서 계산하던 up 벡터를 신규 static `s_isoPipeUp`에 저장하도록 추가.
- N1 헬퍼(`CloneSelectionToSideDatabase`, `CollectNotabN1SolidIds`)를 재사용해 문서 오픈 없이 side-DB에서 Solid3d를 수집.
- 템플릿 DWG를 side-DB로 열고, 수집 Solid3d를 `WblockCloneObjects`로 ModelSpace에 이송.
- `Title Block` 레이아웃에 신규 viewport를 생성하고 pipe axis 기반 `ViewDirection`, solid extents 중심 `ViewTarget`, 계산 twist, Hidden visual style, shade plot reflection 설정, 1:2 custom scale을 적용.
- 타이틀블록은 기존 `UpdateIsoTitleBlockAttributes`를 재사용.
- 저장 경로는 기존 상세도와 충돌하지 않도록 `<ProjectDwgDirectory>\Details\<safeTag>_notab.dwg`.

## 산출 파일
- 수정: `PlantFlow_Support/Core/Commands.cs`
- 백업: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260713_notab_n2`
- 실행 커맨드: `PFSNOTABDETAIL`
- 출력 DWG: `<ProjectDwgDirectory>\Details\<safeTag>_notab.dwg`

## 검증
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS
- `rg catch\s*\{\s*\}`: 빈 catch 없음
- `rg PFSNOTABDETAIL/s_isoPipeUp/CreateNotabDetailDrawing/CreateNotabDetailViewport/TryApplyHiddenVisualStyle`: PASS
- 기존 `PFSVBISOCLONE/OPEN/DONE/EXPORTED` 체인은 직접 수정 없음. 신규 커맨드/헬퍼 병렬 추가와 `s_isoPipeUp` 저장만 수행.
- 빌드/Plant3D 실행 검증은 지시대로 수행하지 않음.

## 커밋 상태
- 코드 변경과 REPORT 갱신은 완료됐으나 커밋은 수행하지 않음.
- 커밋 시 `PlantFlow_Support/Core/Commands.cs`와 `.plans/REPORT.md`만 stage 대상.

## 라이브 실행
- 수동 빌드 후 Plant3D에서 `PFSNOTABDETAIL` 실행.
- 서포트+파이프 선택 후 `Details\<safeTag>_notab.dwg`를 열어 `Title Block` 레이아웃의 은선제거 Main viewport와 타이틀블록 값을 확인.
- FileDiag에서 `PFSNOTABDETAIL viewport ... hidden=True/False shadePlot=True/False scale=1:2` 로그 확인.
