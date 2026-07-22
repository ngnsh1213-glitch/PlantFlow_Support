# REPORT — Codex → Claude

- **cycle**: 108
- **status**: completed
- **completed_at**: 2026-07-22
- **title**: RC5 F1/F2 포트 재지정 + RC9 P1_1 하향 배치

## 항목별 결과

1. **RC5 F1/F2**
   - `StandardSupport.RC5()`의 F1 앵커를 `PPorts[1]`(S2), F2 앵커를 `PPorts[2]`(S3)로 정정했다.
   - `IsNotabVerticalMemberPort()`는 RC5에서만 index 2를 세로재로 판정하도록 분기했다. 다른 타입은 기존 index 1 판정을 유지한다.
   - 세로 치수의 S2 앵커 경로는 변경하지 않았다.
2. **RC9 P1_1**
   - 일반 8방향 탐색 전에 `P1_1`만 `(0,-1)` 방향을 짧은 거리부터 탐색한다.
   - 표준 충돌 검사(`exemptSupportBox=false`, `allowCalloutLeaderCrossing=false`)를 유지했다.
   - 자유 후보가 없으면 `reason=rc9-p1-down-no-space`를 기록하고 일반 탐색 및 `p1-leader-fallback`으로 진행하지 않는다.

## 변경 파일

- `PlantFlow_Support/Ortho/StandardSupport.cs`
- `PlantFlow_Support/Core/Commands.cs`

## 검증

- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.sln --no-restore` 통과: 오류 0, 경고 14.

## 라이브 검증 필요

- RC5 F1이 가로재, F2가 S3 세로재로 배치되는지와 F2 `balloon-draw`의 `adjust=vertical-end`, `geom=vertical-port`를 확인한다.
- RC9 P1_1이 하향으로 작도되어 F3를 관통하지 않는지 확인한다. 공간이 없으면 `rc9-p1-down-no-space`만 허용된다.
- RC1~3·RC6~8·GD 회귀가 없는지 확인한다.

## 커밋

- 코드: `a27e24e` (`fix: correct RC5 ports and RC9 P1 placement`)
