# REPORT — Codex → Claude

- **cycle**: 99
- **status**: pending_verification
- **completed_at**: 2026-07-21
- **title**: 밸룬 F1/F2 외곽 변 앵커 + 유볼트 단일 리더

## 변경 요약

- 밸룬은 포트에 가장 가까운 ITEM 로컬 형상 상자를 선택한다. 동률은 작은 면적, Handle 사전순으로 고정하며 후보 방향의 외곽 변 중점을 화살표 앵커로 쓴다.
- `IsBalloonFree`의 거절 사유를 `balloon-skip`의 `rejBy{...}`로 집계한다.
- 유볼트는 `PFS_NOTAB_UBOLT_MIN_DX`(기본 10)를 사용한다. 외곽 세로 변 중점을 실제 시작점으로 확정해 배치 검사·등록·작도를 모두 단일 선분으로 통일했다.
- `PFS_NOTAB_BALLOON_LEADER_W=2.0`, `PFS_NOTAB_BALLOON_MAX_STEPS=4`, 전역 `PFS_NOTAB_CALLOUT_MIN_DX`는 변경하지 않았다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`

## 검증

- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.sln --no-restore` 통과: 오류 0, 경고 14.

## 라이브 검증 필요

- `dev_test.bat` 후 F1·F2 화살표가 허공이 아닌 부재 선 위에 닿는지 확인.
- 유볼트 화살표가 하변 중앙이 아닌 좌·우 다리 중 하나에 닿는지 확인.
- 공간 부족 시 `balloon-skip`의 `rejBy{...}`가 실제 차단 사유를 집계하는지 확인.
- 유볼트 `callout-draw`의 `leaderFrom/endX/effectiveMinDx`로 외곽 시작·짧은 이격을 확인.

## 커밋

- (커밋 전)
