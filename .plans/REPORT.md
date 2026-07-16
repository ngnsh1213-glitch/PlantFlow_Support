# REPORT - Codex -> Claude

- cycle: 45
- status: done
- commit: HEAD (최종 해시는 Codex 완료 보고 참조)
- title: 무탭 배관 접촉 판정 bbox 투영 방식 교체

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `AutoIncludeRelatedParts`의 Pipe 접촉 판정을 서포트 중심거리 방식에서 서포트 bbox 투영 포함 방식으로 교체.
  - Pipe 축 방향에 수직인 `right/up` 평면 basis를 만들고, 선택 서포트 bbox 8코너를 투영해 실루엣 rect를 계산.
  - Pipe 축점 `p0`도 같은 basis로 투영해 `rect ± (pipeRadius + PFS_NOTAB_CONTACT_TOL)` 안에 들어오는지 판정.
  - 통과 Pipe가 2개 이상이면 투영 rect 중심에 가장 가까운 1개만 포함하고 나머지는 `dropAmbiguous` 로그 후 제외.
  - line tag 전처리, supTag 없을 때 pipe 미포함, 축 실패 시 bbox 거리 폴백은 유지.
  - 로그에 `rect=(minR,maxR,minU,maxU)`, `p0proj=(pr,pu)`, `tol`, `radius`, `passCount`를 남기도록 보강.

## 보존 확인
- 대상 외 파일 수정 없음.
- `CopyCleanNotabSolids` 클립, `ConfigureNotabDetailViewport` 동적 피팅, PERSPECTIVE guard, wireframe 기본, 610x489 viewport 로직 미수정.
- 기존 `dev_test.bat` 작업트리 변경은 범위 밖이라 건드리지 않음.

## 검증
- `dotnet build .\PlantFlow_Support.sln -c Debug`: PASS, 오류 0 / 기존 경고 15
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS, CRLF 안내만 있음
- 변경 주변 20줄 이상 수동 확인 완료.

## 라이브 확인 포인트
- `PFS_NOTAB_TEST_TAG=RC1-001` 환경에서 `dev_test.bat` 실행.
- 기대 로그: `pipeCand=6 incl=1 dropDist=5` 및 include 후보의 `rect=... p0proj=... tol=... passCount=...`.
- 육안: 잡는 배관 1개만 포함되어 메인 방향/스케일이 복구되고, 200mm 평행 이웃 배관 원은 사라지는지 확인.
