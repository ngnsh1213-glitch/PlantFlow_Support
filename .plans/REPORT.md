# REPORT - Codex -> Claude

- **cycle**: 84
- **status**: done
- **completed_at**: 2026-07-20
- **title**: 콜아웃 간섭 해소 — 세로 정합, 치수 장애물, 색/레이어, 앵커 노브

## 변경 요약
- 치수 작도 완료 후 `RecomputeDimensionBlock(true)`로 실제 extents를 수집해 `NotabCalloutPlacer` 장애물로 등록한다. 실패 시 문자 위치 기반 보수 bbox로 폴백하며 등록 수와 영역을 로그에 남긴다.
- 직접 `MText(BottomLeft)` 작도에서 placer 후보 Y(문자 중심)를 `baseY = y - actualHeight / 2`로 변환했다. 리더는 콜아웃 레이어·ACI 1, 문자는 같은 레이어·ACI 3으로 명시하고 `Leader.DimensionStyle` 공유를 제거했다.
- GD2/GD3의 2부재 앵커 비율을 `PFS_NOTAB_GD2_ANCHOR_FX0/FX1`, `PFS_NOTAB_GD3_ANCHOR_FX0/FX1` 환경변수로 노출했다. 기본값은 각각 `0.5/0.8`, `0.25/0.8`이고 `anchorFx`를 진단 로그에 남긴다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- `git diff --check` 통과.
- `git diff --check` 통과.
- 치수 장애물 등록, 실제 높이 기반 `baseY`, 색/레이어 명시, `DimensionStyle` 공유 제거, 4개 앵커 환경변수를 정적 검색으로 확인.
- 프로젝트 규칙에 따라 빌드와 라이브 도면 추출은 실행하지 않음.

## 라이브 검증 필요
- `dev_test.bat`에서 GD1-001, GD2-001, GD3-001을 추출한다.
- `dim-obstacle count`가 3 이상인지, GD3 파이프 콜아웃이 치수문자와 겹치지 않는지 확인한다.
- 모든 `callout-draw` 로그에서 `sep >= 40` 및 문자 Y범위가 서포트/치수 장애물과 분리되는지 확인한다.
- GD3 `PFS_NOTAB_GD3_ANCHOR_FX0=0.305` 적용 시 L 앵커 중심 정합과 `anchorFx=0.305` 로그를 확인한다.

## 커밋
- 집도 완료 후 기록.
