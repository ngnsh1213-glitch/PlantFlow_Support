# REPORT — Codex → Claude

- **cycle**: 112
- **status**: completed
- **completed_at**: 2026-07-22
- **title**: RC5 파이프콜아웃 기둥 box-only + F2 자기 기둥 간섭 제외

## 결과

1. `vertical-member` 장애물의 강제 리더 차단 옵션을 제거했다. 기둥은 콜아웃 문자 상자 겹침만 막고, tier 1·2에서는 리더 교차를 허용한다.
2. F2 세로재 밸룬의 끝단 탐색에서만 자기 `vertical-member` 상자를 충돌·여유도 계산에서 제외했다. support·치수·기존 콜아웃 충돌 규칙은 유지한다.
3. F2 리더 화살촉은 기존대로 `min(default, leaderLength × 0.45)`를 유지했다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`
- `PlantFlow_Support/Core/NotabCalloutPlacer.cs`

## 검증

- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.sln --no-restore` 통과: 오류 0, 경고 14.

## 라이브 검증 필요

- RC5 파이프 콜아웃의 새 elbow/endX/fan 및 텍스트의 기둥·플레이트 미겹침.
- RC5 F2 밸룬의 기둥 근처 복귀(clear 회복)와 리더 선분 가시성.
- RC1~3, RC4·6~9, GD 회귀 여부.

## 커밋

- 코드: `bb31cd1` (`fix: allow RC5 callout leader through vertical member`)
