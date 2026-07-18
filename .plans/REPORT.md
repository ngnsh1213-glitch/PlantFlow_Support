# REPORT - Codex -> Claude

- **cycle**: 70
- **status**: done
- **completed_at**: 2026-07-18
- **title**: GD2 — 세로치수=배관중심까지(좌) + 멀티부재 콜아웃 텍스트 바깥 발산

## 변경 요약
- GD2의 `VerticalMode`를 `pipecenter`로 변경했다.
- 배관 중심 Y가 유효하면 좌측 세로치수를 바닥부터 배관 중심까지로 생성하고, 치수값은 페이퍼-실물 스케일로 환산한다. 중심 좌표 또는 스케일이 유효하지 않으면 기존 전체 높이 치수로 폴백하고 진단 로그를 남긴다.
- 다중 부재 콜아웃은 중앙 기준 좌측 부재가 왼쪽, 우측 부재가 오른쪽으로 발산하도록 텍스트 위치, MText attachment, MLeader 방향을 적용했다.
- 단건 부재와 배관 콜아웃은 기존 `LeftLeader` 방향을 유지했다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `rg`로 `pipecenter`, `leftHalf`, `ApplyNotabCalloutNearEdgeAttachment`의 정의·모든 호출부를 확인했다.
- `git diff --check` 통과.
- 빌드는 프로젝트 규칙과 핸드오프 지시에 따라 실행하지 않았다.

## 라이브 확인 필요
- GD2-001: 좌측 세로치수 300, 다중 콜아웃의 `leftHalf=True/False` 로그 및 좌우 발산을 확인한다.
- GD1/GD3: 세로치수 및 단건 콜아웃이 기존과 동일한지 확인한다.

## 커밋
- 코드 반영 커밋: `945403b` (`Fix GD2 pipe center dimension callouts`)
