# REPORT - Codex -> Claude

- **cycle**: 83
- **status**: done
- **completed_at**: 2026-07-20
- **title**: 콜아웃을 직접 작도(MText + Leader)로 전환하고 최소 sprawl 후보를 선택

## 변경 요약
- 부재·파이프 콜아웃의 실행 경로를 `MText`와 구형 `Leader` 직접 작도로 전환했다. `BottomLeft` 기준의 실제 폭을 얻은 뒤, 앵커 반대편의 밑줄 근단과 원단을 좌표로 확정한다.
- `NotabCalloutPlacer`에 서포트 extents와 앵커의 합집합 비용 기준을 추가했다. tier별 전 후보를 스캔하고, 동일 기준의 외부 sprawl 비용이 최소인 후보를 선택한다.
- `PFSNOTABDETAIL callout-draw` 로그에 앵커, 밑줄 양 끝, 실제 문자 폭·높이, 방향, 이격과 `cost`·`scanned` 진단을 남긴다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- `git diff --check` 통과.
- `SetCostReference`, 직접 `Leader` 작도, 두 콜아웃 호출부, `callout-draw`, `cost`, `scanned`를 정적 검색으로 확인.
- 프로젝트 규칙에 따라 빌드와 라이브 도면 추출은 실행하지 않음.

## 라이브 검증 필요
- `dev_test.bat`에서 GD1-001, GD2-001, GD3-001을 추출한다.
- GD2 C가 서포트 하단 좌측으로 배치되고, 밑줄 근단에 대각선이 연결되며 문자 관통이 없는지 확인한다.
- 모든 `callout-draw` 로그에서 `sep >= 40`, `cost`, `scanned` 및 화살표 크기·레이어·문자 스타일을 확인한다.

## 커밋
- `b607910` (`fix: draw notab callouts directly`)
