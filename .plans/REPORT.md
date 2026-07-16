# REPORT - Codex -> Claude

- cycle: 53
- status: done
- commit: 최종 HEAD 참조
- title: 무탭 부재 콜아웃 텍스트 우측 배치

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `AppendNotabProfileCallout`에서 `gap = PFS_NOTAB_DIM_ARR + txt * 0.6`로 텍스트 시작 여백을 계산.
  - `textPoint`를 dogleg landing(`elbow`) 기준 우측 `gap` 위치로 변경.
  - MText를 `AttachmentPoint.MiddleLeft`와 `Location=textPoint`로 생성해 텍스트가 landing 우측으로 자라도록 지정.
  - 생성된 `MLeader`에도 `TextLocation=textPoint`와 MText `MiddleLeft`/`Location`을 재적용하고, 실패 시 로그 후 기존 리더 생성 흐름을 유지.
  - callout 로그에 `elbow`, `gap`, `arr`를 추가.

## 보존 확인
- 코드 수정은 핸드오프 지정 범위인 `AppendNotabProfileCallout` 내부에 한정.
- 세로치수 75 스팬, 분할라벨, 오프셋/적층 30, GD1/RC1, 투영, held-pipe 로직은 변경하지 않음.
- 기존 작업트리의 무관 변경/삭제/미추적 파일은 스테이징하지 않음.

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `rg -n "callout text placement|TextLocation|MiddleLeft|gap=|PFS_NOTAB_DIM_ARR" PlantFlow_Support/Core/Commands.cs`: 참조 확인.
- `git diff -- PlantFlow_Support/Core/Commands.cs`: 지정 변경만 확인.
- 빌드: 프로젝트 규칙상 사용자 명시 요청 없는 빌드 실행 금지라 미실행.

## 라이브 확인 포인트
- RS11형 `PFSNOTABDETAIL`: `L-75×75×9` 텍스트가 화살표/dogleg 끝보다 우측에 놓이고 리더선과 겹치지 않는지 확인.
- 로그 `callout append designation=... elbow=... text=... gap=... arr=...`에서 `text.X > elbow.X` 확인.
