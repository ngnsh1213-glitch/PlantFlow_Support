# REPORT - Codex -> Claude

- cycle: 43
- status: done
- commit: HEAD (최종 해시는 Codex 완료 보고 참조)
- title: 무탭 추출물 서포트 영역 클립 + 뷰포트 동적 피팅

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `CopyCleanNotabSolids`에서 support solid extents 산출 후 oriented clip box를 생성.
  - `s_isoPipeAxis` / `s_isoPipeUp` basis 기준 right/up/viewDir 투영 범위에 `PFS_NOTAB_CLIP_MARGIN` 기본 150mm를 적용.
  - 각 clean `Solid3d` append 및 metadata strip 직후 `BooleanOperationType.BoolIntersect`로 클립.
  - clip box clone을 solid마다 사용하고, 비교차/empty solid는 erase 후 제외.
  - Boolean 실패는 로그 후 원본 유지 폴백.
  - 클립 후 잔존 solid extents만 `solidExt`에 누적하고 `copied` 카운트에 반영.
  - `ConfigureNotabDetailViewport`의 고정 `CustomScale=0.25` / `ViewCenter=(0,0)`를 제거.
  - 클립 후 `solidExt` 기준으로 right/up 투영 폭/높이를 계산해 `ViewCenter`와 `ViewHeight`를 동적 설정.
  - `PFS_NOTAB_FIT_PAD` 기본 0.15로 뷰포트 여백 조정 가능.

## 보존 확인
- 대상 외 파일 수정 없음.
- 610x489 뷰포트 rect 생성 로직 유지.
- wireframe 기본 및 `PFS_NOTAB_USE_HIDDEN=1` opt-in 유지.
- cycle 42 PERSPECTIVE guard, paper reopen/zoom 로직 미수정.
- `supportExt` fallback은 기존처럼 유지하되, 뷰포트 피팅은 클립 후 `solidExt`를 사용.

## 검증
- `dotnet build .\PlantFlow_Support.sln -c Debug`: PASS, 오류 0 / 기존 경고 15
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS, CRLF 안내만 있음
- 변경 주변 20줄 이상 수동 확인 완료.

## 라이브 확인 포인트
- `PFS_NOTAB_TEST_TAG=RC1-001` 환경에서 `dev_test.bat` 실행.
- `C:\Temp\pfs_diag.log`에서 `PFSNOTABDETAIL clip solids=... kept=... trimmed=... dropped=...` 확인.
- `PFSNOTABDETAIL viewport fit center=... ViewHeight=... content=...` 확인.
- 육안: 이웃 배관/관통 파이프 장거리 구간이 사라지고 서포트 영역만 610x489 프레임에 맞게 표시되는지 확인.
