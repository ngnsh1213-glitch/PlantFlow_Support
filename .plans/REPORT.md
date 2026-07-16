# REPORT - Codex -> Claude

- cycle: 47
- status: done
- commit: HEAD (최종 해시는 Codex 완료 보고 참조)
- title: N3-0/N3-a 무탭 뷰포트 스케일 표준화 + WCS→paper 투영 계측

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `ConfigureNotabDetailViewport`에 `supportExt` 전달을 추가해 뷰포트 설정 확정 후 dim-probe 계측에 사용.
  - `ComputeNotabViewportFit`에서 `PFS_NOTAB_TARGET_FILL` 기본 0.6 기준으로 필요 viewHeight를 계산하고, `{1, 1/2, 1/5, 1/10, 1/20, 1/25, 1/50}` 표준 배율 중 가장 큰 수용 가능 scale을 선택.
  - 선택된 표준 배율로 `vp.ViewHeight = vp.Height / stdScale`, `vp.CustomScale = stdScale`를 명시 설정.
  - 로그 `PFSNOTABDETAIL viewport scale std=1:N ... fill=... targetFill=...` 추가.
  - `NotabProjectWcsToPaper`/`GetNotabViewportScale`/`LogNotabDimProbe` 추가.
  - `supportExt` 8코너와 support 중심을 paper 좌표로 투영해 `PFSNOTABDETAIL dim-probe support-paper=... pipeCenterX(paper)=...` 로그를 남김.

## 보존 확인
- 대상 파일은 `PlantFlow_Support/Core/Commands.cs`만 수정.
- 치수 객체는 생성하지 않음.
- 클립, held-pipe, perspective guard, wireframe 기본, 610x489 viewport 생성 좌표는 수정하지 않음.
- 기존 미추적 파일들은 범위 밖이라 건드리지 않음.

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `rg -n "ConfigureNotabDetailViewport\\(|ComputeNotabViewportFit\\(|SelectNotabStandardScale|LogNotabDimProbe|NotabProjectWcsToPaper|GetNotabViewportScale|viewport scale std|dim-probe" .\PlantFlow_Support\Core\Commands.cs`: 참조 확인.
- `git diff --check -- .\PlantFlow_Support\Core\Commands.cs`: PASS(CRLF 안내만 있음).
- `dotnet build .\PlantFlow_Support.sln -c Debug`: PASS, 오류 0 / 기존 경고 15.

## 라이브 확인 포인트
- `PFS_NOTAB_TEST_TAG=RC1-001` 환경에서 사용자 라이브 실행.
- 기대 로그: `viewport scale std=1:N`, `dim-probe support-paper=... pipeCenterX(paper)=...`.
- 육안 기대: 표준 배율 적용으로 도형이 축소되어 치수/콜아웃 여백이 확보되고, dim-probe paper 좌표가 뷰포트 내 원+사각형 위치와 정렬.
