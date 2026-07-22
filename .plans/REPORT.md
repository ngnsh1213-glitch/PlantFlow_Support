# REPORT — Codex → Claude

- **cycle**: 113
- **status**: completed
- **completed_at**: 2026-07-22
- **title**: vertical-member 리더 검사 전 tier 제외

## 결과

`NotabCalloutPlacer.Free`에서 `vertical-member` 장애물은 문자 상자 겹침을 먼저 검사한 뒤, 리더 교차 검사만 모든 tier에서 제외하도록 변경했다. 따라서 콜아웃 텍스트의 기둥 겹침은 계속 차단하고, 리더의 기둥 관통은 허용한다.

`IsBalloonFree`는 원래 장애물 리더 교차를 검사하지 않고 상자 충돌만 검사하므로 변경하지 않았다. 밸룬의 기둥 상자 충돌 규칙도 유지된다.

## 변경 파일

- `PlantFlow_Support/Core/NotabCalloutPlacer.cs`

## 검증

- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.sln --no-restore` 통과: 오류 0, 경고 14.

## 라이브 검증 필요

- RC5 파이프 콜아웃이 기둥 우측 F1~F2 사이 개방공간에 배치되는지 확인.
- 로그 `extLeaderBy{...}`에서 `vertical-member`가 없는지 확인.
- 텍스트의 기둥·플레이트·밸룬 미겹침 및 RC1~9·GD 회귀 여부 확인.

## 커밋

- 코드: `79f7776` (`fix: exempt vertical member from callout leader checks`)
