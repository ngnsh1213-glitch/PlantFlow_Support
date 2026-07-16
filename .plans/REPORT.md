# REPORT - Codex -> Claude

- cycle: 52
- status: done
- commit: 최종 HEAD 참조
- title: 무탭 치수 라이브 정련 - dimV 바 스팬, 우측 콜아웃, 분할라벨 중앙, 오프셋 30

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `AppendNotabPaperDimensions`의 `PFS_NOTAB_DIM_OFFSET`/`PFS_NOTAB_DIM_STACK` 기본값을 30.0으로 변경.
  - split 치수 `dimSplitL`/`dimSplitR`의 텍스트 위치를 각 세그먼트 중앙으로 명시 지정.
  - dimV 정의점을 support 전체 높이에서 프로파일 F 높이 기반 바 높이(`s_isoSupportProfileHeight * paperH / realH`)로 축소하고, 실패 시 기존 전체 스팬으로 폴백.
  - dimV 로그에 `dimVBarSpan`, `barRealH`, `barPaperH`, `paperH`, `vScale`을 추가.
  - `AppendNotabProfileCallout`의 콜아웃 anchor/elbow/textPoint를 support 우측 방향으로 반전하고 offset 기본값을 30.0으로 맞춤.

## 보존 확인
- 코드 수정은 핸드오프 지정 대상인 `PlantFlow_Support/Core/Commands.cs`에 한정.
- 클립, held-pipe, 스케일, perspective guard, 투영, 텍스트값 캐시, GD1/RC1 하단 로직은 변경하지 않음.
- 기존 작업트리의 무관 변경/삭제/미추적 파일은 스테이징하지 않음.
- 파싱 또는 좌표 지정 실패는 `PlantOrthoView.FileDiag`에 기록하고 기존 동작으로 폴백.

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `git diff -- PlantFlow_Support/Core/Commands.cs`: 지정 변경만 확인.
- 빌드: 프로젝트 규칙상 사용자 명시 요청 없는 빌드 실행 금지라 미실행.

## 라이브 확인 포인트
- RS11형 `PFSNOTABDETAIL`: 세로 치수선이 하단 부재 바 75mm만 스팬하는지 확인.
- `L-75×75×9` 콜아웃이 우측으로 인출되는지 확인.
- `100`/`200` 분할 라벨이 각 세그먼트 중앙에 표시되는지 확인.
- 오프셋/적층 30과 로그 `barPaperH=... vScale=...` 확인.
