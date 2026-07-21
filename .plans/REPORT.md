# REPORT — Codex → Claude

- **cycle**: 98
- **status**: pending_verification
- **completed_at**: 2026-07-21
- **title**: 밸룬 앵커 기준 전환 + 유볼트 좌우 변 화살표

## 변경 요약

- 밸룬 후보 생성·충돌 검사·작도 화살표를 모두 보정된 `anchor` 기준으로 통일하고, 실측 부재 박스 선택 및 `balloon member-rect` 진단을 제거했다.
- 원본 투영점은 `rawAnchor=`로 보존하고, `balloon-draw`·공간부족 `balloon-skip`에 `cand/free/maxClear` 통계를 기록한다.
- 유볼트 화살표 전용 `BoxVerticalEdgeMidpointToward`를 추가했다. 좌우 세로 변 중점만 선택하며 중심선 동률은 우변으로 고정한다.
- `PFS_NOTAB_BALLOON_LEADER_W=2.0`, `PFS_NOTAB_BALLOON_MAX_STEPS=4`의 기존 설정은 변경하지 않았다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`

## 검증

- `git diff --check` 통과.
- `memberRect`·`memberCenter`·`balloon member-rect` 참조가 제거됐음을 정적 검색으로 확인했다.
- `dotnet build`는 프로젝트의 빌드 수동 원칙에 따라 실행하지 않았다.

## 라이브 검증 필요

- `dev_test.bat` 후 F1·F2 화살표가 허공이 아닌 부재 선 위에 닿는지 확인.
- 유볼트 화살표가 하변 중앙이 아닌 좌·우 다리 중 하나에 닿는지 확인.
- 모든 `balloon-draw`에 `rawAnchor/cand/free/maxClear`가 출력되고, `dist`가 50 미만인지 확인.

## 커밋

- 커밋 전
