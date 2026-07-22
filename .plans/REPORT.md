# REPORT — Codex → Claude

- **cycle**: 110
- **status**: completed
- **completed_at**: 2026-07-22
- **title**: RC5 F2 리더 가시화 + 파이프 콜아웃 기둥회피 + RC9 P1_1 리더교차만 허용

## 항목별 결과

1. **RC5 F2 리더 가시화**
   - 세로재만 밸룬 중심 거리를 `반경 + Dimasz + 가시 여유`로 확장했다. 화살표 앵커와 `Dimasz` 값은 변경하지 않았다.
2. **RC5 파이프 콜아웃 기둥회피**
   - `verticalAnchorX/baseY/topY` 기반 세로 기둥 상자를 `vertical-member` 장애물로 등록했다. 세로재 없는 타입은 등록하지 않는다.
   - 파이프 콜아웃만 모든 탐색 tier에서 해당 장애물의 리더 교차를 계속 차단한다. 문자 상자 충돌 및 기존 좌우·fan·반경 탐색은 유지했다.
3. **RC9 P1_1 하향 배치**
   - `rc9-p1-down` 검사에서 `allowCalloutLeaderCrossing=true`을 적용했다.
   - 콜아웃 상자·support·치수·부재 상자 충돌은 계속 차단된다.
   - 허용된 교차 대상은 기존 콜아웃 리더(라이브 계측상 파이프 콜아웃 리더로 추정)이며, F3 리더인지 여부는 라이브에서 확인이 필요하다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`
- `PlantFlow_Support/Core/NotabCalloutPlacer.cs`

## 검증

- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.sln --no-restore` 통과: 오류 0, 경고 14.

## 라이브 검증 필요

- RC5 F2 화살촉 뒤 리더 선분 노출과 파이프 콜아웃의 기둥 미관통을 확인한다.
- RC9 P1_1이 우측 플레이트 아래에 작도되고, 허용 교차 대상이 파이프 콜아웃 리더인지 확인한다. F3 리더 교차면 재검토한다.
- RC1~3, RC4~8 및 GD 회귀를 확인한다.

## 커밋

- 코드: `6778daf` (`fix: improve RC5 and RC9 notab callouts`)
