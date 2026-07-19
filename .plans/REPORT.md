# REPORT - Codex -> Claude

- **cycle**: 78
- **status**: done
- **completed_at**: 2026-07-19
- **title**: 콜아웃 최근접 모서리 대각 배치·비대칭 문자박스·치수 0.#

## 변경 요약
- `NotabCalloutPlacer` 부채꼴 우선순위를 ±45도 대각부터 시도하도록 바꾸고, 두 콜아웃의 기준각을 앵커의 최근접 서포트 모서리 바깥 법선으로 교체했다.
- 문자 landing점과 MText의 실제 near-edge 부착 방향에 맞춰 충돌박스와 커밋 박스를 비대칭으로 통일했다. 문자는 앵커 반대편 X방향으로만 뻗는다.
- 콜아웃 로그에 `textSide`, `anchorX`, `textX`, 실제 배치각을 남기고, 치수 표시를 `0.#`로 바꿨다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- `git diff --check -- PlantFlow_Support/Core/Commands.cs` 통과.
- 두 `Atan2(anchor...)` 기준각 계산이 제거됐고 최근접 모서리 계산 두 곳을 확인했다.
- 빌드는 프로젝트 규칙에 따라 실행하지 않음.

## 라이브 검증 필요
- `dev_test.bat`에서 `PFS_NOTAB_TEST_TAG=GD1-001,GD2-001,GD3-001` 실행.
- `smart=placed diag=`의 `actual=`이 ±30~60도 대각 우선인지, `textSide`와 `anchorX`/`textX`가 반대편 규칙을 지키는지 확인.
- GD2/GD3 문자-부재 겹침과 리더 교차가 없는지, 정수 치수 `274`·`100` 및 소수 치수 `100.1` 표기를 확인.

## 커밋
- 아래 커밋에 `Commands.cs`와 이 보고서만 포함한다.
