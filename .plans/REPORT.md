# REPORT - Codex -> Claude

- cycle: 51
- status: done
- commit: 최종 HEAD 참조
- title: 무탭 치수 개선 - 세로 F값, 부재 콜아웃, 치수 크기, 타입별 가로 위치 스캐폴드

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `PFS_NOTAB_DIM_TXT` 기본값을 10.0, `PFS_NOTAB_DIM_OFFSET` 기본값을 15.0, `PFS_NOTAB_DIM_STACK` 기본값을 15.0으로 조정.
  - `PFS_NOTAB_DIM_ARR` 환경변수(기본 10.0)를 추가해 치수 화살표 크기를 텍스트 크기와 분리.
  - `PSUtil.GetSupportDimension(id)["BI"]`와 `HANTEC.DetailProfile(BI)`로 부재 프로파일을 캐시하고, dimV 텍스트를 프로파일 첫 숫자(F, 예: `75`)로 override.
  - 프로파일 prefix 매핑(1=L, 2=C, 3=H, 4=FB)으로 `L-75×75×9` 형태의 MLeader 콜아웃을 페이퍼공간 `AUTO_DIM` 레이어에 추가.
  - SupportName 첫 `-` 앞 prefix로 support type을 구하고, `GD1`/`RC1`은 가로 치수 하단 강제, 나머지는 기존 배관 근접 auto 로직을 유지.
  - 로그에 `dimV(F)=...`, `callout=...`, `BI=...`, `sideMode=...`, `type=...`, `stack=...`를 추가.

## 보존 확인
- 핸드오프 지정 대상인 `PlantFlow_Support/Core/Commands.cs`만 코드 수정.
- 클립, held-pipe 선택, 스케일, perspective guard, wireframe, 투영 행렬 로직은 변경하지 않음.
- 기존 작업트리의 미추적/삭제 항목은 건드리지 않음.
- BI/DetailProfile 실패 시 예외를 로그로 남기고 기존 realH 세로 치수 텍스트로 폴백.

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `rg -n "dimV\\(F\\)|callout append|profile BI|sideMode|PFS_NOTAB_DIM_STACK|PFS_NOTAB_DIM_ARR" PlantFlow_Support\\Core\\Commands.cs`: 참조 확인.
- `git diff -- PlantFlow_Support\\Core\\Commands.cs`: 지정 변경만 확인.
- 빌드: 프로젝트 규칙상 사용자 명시 요청 없는 빌드 실행 금지라 미실행.

## 라이브 확인 포인트
- `PFSNOTABDETAIL` 또는 `PFSNOTABBATCH` 실행 후 세로 치수 텍스트가 `75` 같은 프로파일 F값으로 표시되는지 확인.
- 부재 옆 MLeader 콜아웃이 `L-75×75×9` 형태로 생성되는지 확인.
- 글자/화살표 10, 오프셋/적층 15 적용 확인.
- `GD1`/`RC1` 가로 치수는 하단, 미등록 타입은 기존 auto 규칙 유지 확인.
