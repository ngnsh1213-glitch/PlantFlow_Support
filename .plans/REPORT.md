# REPORT - Codex -> Claude

- cycle: 46
- status: done
- commit: HEAD (최종 해시는 Codex 완료 보고 참조)
- title: 무탭 배관 선택 tiebreak를 서포트 BOP 표고 매칭으로 교체

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `AutoIncludeRelatedParts`에서 서포트 `BOP`를 `ps.dl_manager.GetProperties(..., ["BOP"], true)`로 직접 조회하는 `TryGetSupportBop` 헬퍼 추가.
  - rect 통과 필터(`DoesPipeAxisProjectThroughSupport`)는 유지하고, 통과 후보 선택만 `|(pipeCenterZ - radius) - supportBop|` 최소 기준으로 교체.
  - `PFS_NOTAB_BOP_TOL` 기본값 10mm를 추가하고, BOP 오차가 tol 내 동률이면 기존 rect 중심 score로 2차 선택.
  - BOP가 없거나 후보 BOP 계산이 불가하면 기존 rect 중심 score 폴백을 유지해 배관 누락을 방지.
  - include/dropAmbiguous 로그에 `reason`, `bopErr`, `pipeCenterZ`, `radius`, `bop`, `bopTol`, `passCount`를 기록.

## 보존 확인
- 대상 파일은 `PlantFlow_Support/Core/Commands.cs`만 수정.
- 클립, 동적 피팅, PERSPECTIVE guard, wireframe, 610x489 viewport, line tag 전처리, rect 통과 필터는 수정하지 않음.
- 기존 작업트리의 `dev_test.bat` 수정 및 미추적 파일들은 범위 밖이라 건드리지 않음.

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `rg -n "new NotabPipeIncludeCandidate|TryGetSupportBop|reason=|BOP 부재" .\PlantFlow_Support\Core\Commands.cs`: 호출/로그 참조 확인.
- `git diff -- .\PlantFlow_Support\Core\Commands.cs`: 대상 diff 확인.
- 빌드는 프로젝트 규칙상 사용자의 명시 요청이 없어 실행하지 않음.

## 라이브 확인 포인트
- `PFS_NOTAB_TEST_TAG=RC1-001` 환경에서 사용자 빌드 후 `dev_test.bat` 실행.
- 기대 로그: `include ... reason=bop`, BOP 423 기준 Z 약 467 배관의 `bopErr` 최소, Z 약 667 배관은 `dropAmbiguous`.
