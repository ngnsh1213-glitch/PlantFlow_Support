# REPORT - Codex → Claude

- **cycle**: 88
- **status**: done
- **completed_at**: 2026-07-20
- **title**: 무탭 콜아웃 좌우 하드 제약 및 단일 배치 경로

## 변경 요약
- `NotabCalloutPlacer.TryPlace`를 `RequiredSide` 하드 제약으로 전환했다. 후보의 좌우는 고정되고 상하·반경만 탐색한다.
- 실제 `Viewport.CenterPoint/Width/Height`로 페이퍼 사각형과 중앙선을 산출했다. 파이프는 파이프 중심 X, 부재는 각 앵커 X를 기준으로 좌우를 결정한다. 중앙선은 우측으로 결정한다.
- 호출부의 사전 `TryPlace`를 제거하고 직접 작도 직전의 한 곳에서만 배치한다. tier는 `All → PlacedCalloutsOnly → None`으로 완화한다.
- 실패하면 MText를 삭제하고 작도를 생략한다. `callout-draw`/`callout-skip`에 좌우 출처, 기준 X, 뷰포트 중앙선, tier 및 거절 사유 카운트를 기록한다.
- `PFS_NOTAB_DIR_*`는 `L`/`R`만 강제 오버라이드로 허용하며 다른 값은 deprecated 로그 후 무시한다. angle 비용 기본값은 `0.0`이다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- `git diff --check` 통과.
- 정적 검색: 활성 `TryPlace` 호출은 직접 작도 경로 1곳뿐이며, 구 각도 계산 호출은 제거됨을 확인.
- 빌드 미실행: 사용자 요청이 없어 프로젝트 규칙에 따라 실행하지 않음.

## 라이브 검증 필요
- `dev_test.bat`로 GD1/GD2/GD3를 추출하고 `callout-draw`에서 GD2 PIPE=`requiredSide=right`, GD3 PIPE=`requiredSide=left`, `tier<=1`, `callout-skip` 0건을 확인한다.
- 육안으로 문자/리더 간섭과 리더의 문자 관통이 없는지 확인한다.

## 커밋
- 이 REPORT와 코드 변경을 동일 커밋으로 기록.
