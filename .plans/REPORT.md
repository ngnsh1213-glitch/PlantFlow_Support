# REPORT — Codex → Claude

- **cycle**: 109
- **status**: completed
- **completed_at**: 2026-07-22
- **title**: RC5 F2 리더 기둥 축 연장 + RC9 P1_1 down-out 배치 및 거부 계측

## 항목별 결과

1. **RC5 F2 리더**
   - 세로 부재의 `endMinX/endMaxX`를 `rawAnchor.X`로 통일했다. 따라서 F2 화살표 끝점은 기둥 중심축에 닿는다.
   - `halfThick`은 진단 및 기존 밸룬/문자 여백 계산에 유지했으며, 끝점 산출에서만 제외했다.
2. **RC9 P1_1 down-out**
   - 순수 하향 `(0,-1)`을 첫 후보로 유지하고, 실패 시 우측으로 `step × 0.25/0.5/0.75`만큼 이동한 하향 후보를 순서대로 검사한다.
   - 모든 후보에서 `IsBalloonFree(..., false, false)`를 유지하여 support·기존 상자·콜아웃 리더 교차를 면제하지 않는다.
   - 자유 후보가 없으면 `rc9-p1-down-no-space` 로그에 `rejBy{...}` 거부 원인 집계를 기록한다.
3. **F3 계측**
   - 기존 `balloon-draw` 로그가 모든 키에 `rawAnchor`, `anchor`, `center`, `box`를 이미 기록하므로 F3 좌표 계측을 추가하지 않았다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`

## 검증

- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.sln --no-restore` 통과: 오류 0, 경고 14.

## 라이브 검증 필요

- RC5 F2 `balloon-draw`의 `anchor.X`가 기둥 축(예: 약 402.5)에 일치하는지 확인한다.
- RC9 P1_1이 우측 플레이트 아래로 하향 작도되고 F3 리더를 관통하지 않는지 확인한다. 실패 시 `rejBy{...}`로 차단 원인을 확인한다.
- RC1~3, RC4~8 및 GD 회귀가 없는지 확인한다.

## 커밋

- 코드: `3c054be` (`fix: extend RC5 leader and place RC9 P1 down-out`)
