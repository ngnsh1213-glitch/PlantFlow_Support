# REPORT - Codex -> Claude

- **cycle**: 77
- **status**: done
- **completed_at**: 2026-07-19
- **title**: 콜아웃 스마트배치 엔진 이식 및 치수 속성 정렬

## 변경 요약
- `NotabCalloutPlacer`를 신설해 파이프 우선, 이후 부재 콜아웃이 동일한 문자 박스·리더 상태를 회피하도록 연결했다.
- strict 배치에서 문자/장애물 관통, 기존 리더의 새 문자 관통, 리더 교차를 거부하고 실패 시 기존 고정 배치를 유지한다.
- 두 치수 헬퍼에 Above, 소수 1자리, Ext line ext=5를 적용하고 override 텍스트도 `0.0` 형식으로 통일했다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- 변경 주변 호출부와 diff를 확인했다.
- `git diff --check -- PlantFlow_Support/Core/Commands.cs` 통과.
- 빌드는 프로젝트 규칙에 따라 실행하지 않음.

## 라이브 검증 필요
- `dev_test.bat`에서 `PFS_NOTAB_TEST_TAG=GD1-001,GD2-001,GD3-001`로 실행한다.
- GD3의 두 부재 리더가 교차하지 않고 타 문자 박스를 관통하지 않는지, 로그에 `smart=placed diag=tier=0`이 남는지 확인한다.
- 치수 속성창의 Text pos vert=Above, Precision=0.0, Ext line ext=5.0000 및 콜아웃 텍스트 비변경을 확인한다.

## 커밋
- 아래 커밋에 `Commands.cs`와 이 보고서만 포함한다.
