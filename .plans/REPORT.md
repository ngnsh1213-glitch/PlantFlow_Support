# REPORT — Codex → Claude

- **cycle**: 101
- **status**: completed
- **completed_at**: 2026-07-21
- **title**: 밸룬 F 계열 local-box 보정 제거 (화살표를 부재 중심선에 고정)

## 변경 요약

- F1/F2 밸룬의 `local-box` 앵커 재계산을 제거했다. `vertical-mid`/`horizontal-mid` 보정 직후의 앵커를 후보 검사, 리더 길이 점수, 작도, 등록에 공통 사용한다.
- P1은 기존대로 `rawAnchor`와 `adjust=port`를 유지한다.
- 더 이상 사용되지 않는 `TryGetNotabBalloonItemBox`와 `BoxEdgeMidpointToward`를 제거했다. `TryGetNotabVerticalMemberBox`는 프로필 콜아웃에서 계속 사용하므로 유지했다.
- 확장 탐색, 유볼트, 파이프 콜아웃은 변경하지 않았다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`

## 검증

- `rg` 확인: `local-box`, `BoxEdgeMidpointToward`, `TryGetNotabBalloonItemBox` 참조 없음.
- `git diff --check -- PlantFlow_Support/Core/Commands.cs` 통과.
- `dotnet build --no-restore` 통과: 오류 0, 경고 15 (허용 상한 15).

## 라이브 검증 필요

- `dev_test.bat` 후 F1/F2 `balloon-draw` 로그의 `adjust=horizontal-mid`/`vertical-mid`, `free>0`를 확인한다.
- F1 화살표가 가로재, F2 화살표가 세로재 중심선에 닿는지 확인한다.

## 커밋

- 코드: `54d84fa` `fix: anchor F balloons to member centerline`
