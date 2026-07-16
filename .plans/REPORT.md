# REPORT - Codex -> Claude

- cycle: 56
- status: done
- commit: 최종 HEAD 참조
- title: 콜아웃 텍스트 위치 env 노브 추가

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - 부재 콜아웃에 `PFS_NOTAB_MEMBER_CALLOUT_DX/DY` 환경변수를 추가.
  - PLN/BOP 콜아웃에 `PFS_NOTAB_PIPE_CALLOUT_DX/DY` 환경변수를 추가.
  - 두 콜아웃 모두 anchor는 고정하고, elbow와 textPoint에만 DX/DY를 가산.
  - 로그에 `mdx/mdy`, `pdx/pdy`를 추가.

## 보존 확인
- 코드 수정은 `AppendNotabProfileCallout` / `AppendNotabPipeCallout` 좌표부에 한정.
- 기본값 0.0이라 env 미설정 시 cycle55 출력과 동일.
- 세로치수 75, 분할라벨, 오프셋/적층 30, GD1/RC1, 투영, held-pipe, 콜아웃 텍스트값, anchor 지향점은 변경하지 않음.
- 기존 작업트리의 무관 변경/삭제/미추적 파일은 스테이징하지 않음.

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `rg -n "MEMBER_CALLOUT_D[XY]|PIPE_CALLOUT_D[XY]|mdx=|pdx=" PlantFlow_Support/Core/Commands.cs`: 참조 확인.
- `git diff -- PlantFlow_Support/Core/Commands.cs`: 지정 변경만 확인.
- 빌드: 프로젝트 규칙상 사용자 명시 요청 없는 빌드 실행 금지라 미실행.

## 라이브 확인 포인트
- env 미설정 시 cycle55와 동일 출력 확인.
- `PFS_NOTAB_PIPE_CALLOUT_DX=200` 등 설정 후 재실행하면 재빌드 없이 해당 콜아웃 elbow/text가 이동하는지 확인.
- 로그 `mdx/mdy`, `pdx/pdy` 반영 확인.
