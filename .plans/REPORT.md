# REPORT - Codex -> Claude

- cycle: 44
- status: done
- commit: HEAD (최종 해시는 Codex 완료 보고 참조)
- title: 무탭 배관 포함을 서포트가 실제 잡는 배관으로 한정

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `AutoIncludeRelatedParts`의 Pipe 자동 포함 조건을 bbox 교차에서 line tag + 축거리 접촉 판정으로 교체.
  - 선택 서포트의 `LineNumberTag` / `LineNumber` / `Line Number`를 읽어 기준 라인으로 사용.
  - 기준 라인이 없으면 Pipe auto-include를 하지 않음.
  - 후보 Pipe는 같은 라인만 통과하고, 포트 2개로 얻은 중심축과 서포트 bbox 중심 사이 수직거리로 접촉 판정.
  - 허용값은 `pipeRadius + PFS_NOTAB_CONTACT_TOL`이며 기본 접촉 여유는 20mm.
  - 축/반경 판정 불가 시 bbox 최근접 거리 폴백을 로그와 함께 사용.
  - co-located Support 후보 포함은 기존 bbox probe 방식 유지.
  - `TryGetPlantLineNumber`, `TryGetPipeAxisFromId`, `TryGetPipeRadiusFromProperties`, 거리 계산 헬퍼 추가.

## 보존 확인
- 대상 외 파일 수정 없음.
- `CopyCleanNotabSolids` 클립, `ConfigureNotabDetailViewport` 동적 피팅, PERSPECTIVE guard, wireframe 기본, 610x489 viewport 로직 미수정.
- `dev_test.bat`의 기존 작업트리 변경은 범위 밖이라 건드리지 않음.

## 검증
- `dotnet build .\PlantFlow_Support.sln -c Debug`: PASS, 오류 0 / 기존 경고 15
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS, CRLF 안내만 있음
- 변경 주변 20줄 이상 수동 확인 완료.

## 라이브 확인 포인트
- `PFS_NOTAB_TEST_TAG=RC1-001` 환경에서 `dev_test.bat` 실행.
- `C:\Temp\pfs_diag.log`에서 `PFSNOTABDETAIL auto-include ... pipeCand=... incl=... dropLine=... dropDist=...` 확인.
- 육안: 200mm 평행 이웃 배관 원이 사라지고, 서포트가 실제 잡는 배관 1개만 클립/트림되어 표시되는지 확인.
