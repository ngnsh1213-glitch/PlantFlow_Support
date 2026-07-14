# REPORT — Codex → Claude

- cycle: 37
- status: done
- commit: HEAD (최종 해시는 Codex 완료 보고 참조)
- title: 무탭 레이아웃 정합 — 페이퍼 초기화면 + 뷰포트 사각형 + 스케일1:4 + target 서포트중심

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `CreateNotabDetailViewport`의 paperspace viewport를 고정 사각형으로 변경.
    - LL=(30.5,84.5), size=(640.5,573.5), center=(350.75,371.25)
    - 로그: `PFSNOTABDETAIL viewport rect ...`
  - `CopyCleanNotabSolids`가 전체 solid extents와 별도로 support extents/source를 반환하도록 확장.
    - side DB ModelSpace의 cloned Support 엔티티를 class/type/dxf 이름으로 식별.
    - Support 엔티티를 재귀 `Explode`하여 나온 `Solid3d.GeometricExtents`를 결합.
    - 실패 시 전체 solid extents로 fallback하고 `src=fallback` 로그.
  - `ConfigureNotabDetailViewport` target을 support extents 중심으로 변경.
    - `ViewCenter=(0,0)` 유지.
    - `CustomScale=0.25`로 변경, 로그 `scale=1:4`.
  - 저장 직전 `SetNotabInitialPaperLayout(detailDb)` 추가.
    - `detailDb.TileMode=false` 설정.
    - Layout dictionary에서 `Title Block`을 `TabSelected=true`로 지정, 나머지는 false.
    - headless side DB에서 zoom extents 직접 API는 적용하지 않고 `zoomExtents=skip` 로그.

## 검증
- 백업 생성: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_cycle37`
- `dotnet build`: PASS, 오류 0 / 기존 경고 15
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS, CRLF 경고만 있음
- 빈 catch 없음
- `CreateIsoDetailDrawing` / `PFSVBISO*` 집도 없음

## 라이브 확인 포인트
- `PFSNOTABDETAIL cleanSolid ... supportExt=... src=support|fallback`
- `PFSNOTABDETAIL viewport rect LL=(30.5,84.5) size=(640.5,573.5) center=(350.75,371.25)`
- `PFSNOTABDETAIL viewport target=(...) src=support` 기대
- `PFSNOTABDETAIL viewport ... scale=1:4`
- `PFSNOTABDETAIL layout active=Title Block tilemode=0 zoomExtents=skip|ok`
- 생성 DWG가 Title Block 페이퍼 공간으로 열리는지, 서포트가 중앙/1:4로 보이는지 확인.
