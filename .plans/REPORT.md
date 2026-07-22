# REPORT — Codex → Claude

- **cycle**: 107
- **status**: completed
- **completed_at**: 2026-07-22
- **title**: RC5 F2 기둥 배치 + RC7 분할 반전·F2 대각재 + RC9 P1 리더교차 폴백

## 항목별 결과

1. **RC5 F2**: 세로재 `member-end` 후보에만 `support` 상자 겹침 면제를 명시 적용했다. 치수·BOM·기존 콜아웃 상자 및 리더 검사는 유지한다. support는 여유 점수에서도 제외해 기둥 인접 후보가 실제 여유로 비교된다.
2. **RC7**:
   - **2a**: `TryGetNotabRcHorizontalParams`는 변경하지 않고 소비부에서만 `paramLeft`/`paramRight`를 교환했다. RC7 분할은 좌 150 / 우 800으로 작도된다.
   - **2b**: RC7 `F2`만 가로재 끝단 배치를 우회하고 `RS2()`의 `PPorts[2]` 원본 앵커를 기준으로 일반 후보 탐색한다. 단, 다른 부재보다 먼저 배치되는 순서는 유지한다.
3. **RC9 P1**: P1 정상 탐색의 자유 후보가 0일 때, 확장 탐색에서만 기존 콜아웃 상자/리더와의 리더 교차를 허용한다. 밸룬 상자 겹침·support·치수 충돌은 계속 금지한다. 로그 tier는 `p1-leader-fallback`이다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`
- `PlantFlow_Support/Core/NotabCalloutPlacer.cs`

## 검증

- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.sln --no-restore` 통과: 오류 0, 경고 14.

## 라이브 검증 필요

- RC5 F2 `balloon-draw`가 기둥 축 근처로 배치되고 F1은 하단 가로재인지 확인.
- RC7 `split=(150,800)` 및 F2가 대각 브레이스를 가리키는지 확인.
- RC9 P1 두 항목이 모두 작도되고, 폴백 사용 시 `tier=p1-leader-fallback`인지 확인.
- RC1~3·GD·RC4/6/8 회귀 없음 확인.

## 커밋

- 코드: `c5fa9c0` (`fix: refine RC notab balloon placement`)
