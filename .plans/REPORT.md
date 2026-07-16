# REPORT - Codex -> Claude

- cycle: 48
- status: done
- commit: HEAD (최종 해시는 Codex 완료 보고 참조)
- title: N3-b/c 무탭 페이퍼공간 치수 제도

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `LogNotabDimProbe`의 paper 좌표 계산을 `TryComputeNotabDimPaperGeometry`로 분리해 dim-probe와 치수 append가 같은 투영 결과를 사용.
  - `CreateNotabDetailDrawing`에서 뷰포트 설정 완료 후, SaveAs 직전 `AppendNotabPaperDimensions(detailDb, viewportId, supportExt)`를 호출.
  - 새 트랜잭션에서 `EnsureIsoAnnotationResources`로 AUTO_DIM/DimStyle 리소스를 확보하고 Title Block layout BTR에 비연관 `RotatedDimension`을 append.
  - 페이퍼 좌표 기준으로 가로 총폭, pipeCenter 좌/우 분할, 세로 높이 치수를 생성하고 텍스트는 `s_isoRealWidth/s_isoRealHeight` 실측 mm 값으로 override.
  - 치수 문자는 `PFS_NOTAB_DIM_TXT` 기본 2.5mm, offset은 `PFS_NOTAB_DIM_OFFSET` 기본 `txt*3`으로 페이퍼 고정 크기 적용.
  - 로그 `PFSNOTABDETAIL dim append H=... V=... split=(L,R) paperExt=... txt=... offset=...` 추가.

## 보존 확인
- 대상 파일은 `PlantFlow_Support/Core/Commands.cs`만 수정.
- 클립, held-pipe, 스케일 표준화, perspective guard, wireframe 기본, 610x489 viewport 생성 좌표는 수정하지 않음.
- 기존 dim-probe 로그 유지.
- 기존 `dev_test.bat` 수정과 미추적 파일들은 범위 밖이라 건드리지 않음.

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `rg -n "AppendNotabPaperDimensions|TryComputeNotabDimPaperGeometry|ApplyNotabPaperDimensionOverrides|dim append" .\PlantFlow_Support\Core\Commands.cs`: 참조 확인.
- `git diff --check -- .\PlantFlow_Support\Core\Commands.cs`: PASS(CRLF 안내만 있음).
- `dotnet build .\PlantFlow_Support.sln -c Debug`: PASS, 오류 0 / 기존 경고 15.

## 라이브 확인 포인트
- `PFS_NOTAB_TEST_TAG=RC1-001` 환경에서 사용자 라이브 실행.
- 기대 로그: `PFSNOTABDETAIL dim append ...`.
- 육안 기대: 세로 높이, 가로 총폭, pipeCenter 분할 치수가 뷰포트 서포트와 정렬되고 문자 크기가 페이퍼에서 과대하지 않음.
