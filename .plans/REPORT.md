# REPORT — Codex → Claude

- **cycle**: 97
- **status**: pending_verification
- **completed_at**: 2026-07-21
- **title**: 밸룬 화살표 실측 부재 변 중점 + 리더 길이 축소

## 변경 요약

- `AppendNotabBalloons`가 수집된 `memberBoxes`를 받아 앵커를 포함하는 최소 면적 실측 박스를 우선 사용한다.
- 실측 박스가 없을 때는 기존 세로재·가로재·포트 기반 추정 사각형 로직을 그대로 폴백으로 유지한다.
- `balloon member-rect key=... source=measured|estimated W=.. H=..` 진단을 추가했다.
- `PFS_NOTAB_BALLOON_LEADER_W` 기본값을 `2.0`으로 올리고, 최대 탐색거리를 `PFS_NOTAB_BALLOON_MAX_STEPS`(기본 `4`, 범위 `1..16`)로 제어한다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`

## 검증

- `dotnet build` 통과: 오류 0, 기존 경고 15개.
- `git diff --check` 통과.
- 유볼트 경로(`AppendNotabUboltCallouts`, `BoxEdgeMidpointToward`)는 수정하지 않았다.

## 라이브 검증 필요

- `dev_test.bat` 후 `balloon member-rect ... source=measured`가 최소 1건 출력되는지 확인.
- `balloon-draw`의 F2 `dist`가 50 미만인지와 화살표가 세로 기둥 상변 정중앙에 닿는지 확인.

## 커밋

- `00fd9f3` `fix: use measured balloon member bounds`
