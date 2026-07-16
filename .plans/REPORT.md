# REPORT - Codex -> Claude

- cycle: 55
- status: done
- commit: 최종 HEAD 참조
- title: 부재 콜아웃 바 지향 보정 및 PLN/BOP 콜아웃 추가

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `AppendNotabProfileCallout`에서 프로파일 높이 기반 `barPaperH`를 계산해 anchor를 부재 바 우변 중앙으로 이동.
  - 부재 콜아웃 elbow를 `maxX + offset * 3.0`, `minY + barPaperH * 0.15`로 조정해 더 우측/하단으로 인출.
  - 부재 콜아웃 로그에 `barPaperH`를 추가.
  - `AppendNotabPipeCallout`을 추가해 `s_isoPipeLineNo`/`s_isoBOP` 기반 2줄 MLeader를 배관 위쪽에 생성.
  - `AppendNotabPaperDimensions`에서 부재 콜아웃 직후 pipe 콜아웃을 호출.

## 보존 확인
- 대상은 핸드오프 지정 파일 `PlantFlow_Support/Core/Commands.cs`와 보고서뿐.
- 세로치수 75, 분할라벨, 오프셋/적층 30, GD1/RC1, 투영, held-pipe 로직은 변경하지 않음.
- 기존 작업트리의 무관 변경/삭제/미추적 파일은 스테이징하지 않음.

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `rg -n "AppendNotabPipeCallout|pipe callout|barPaperH=|offset \\* 3\\.0|barPaperH \\* 0\\.15" PlantFlow_Support/Core/Commands.cs`: 참조 확인.
- `git diff -- PlantFlow_Support/Core/Commands.cs`: 지정 변경만 확인.
- 빌드: 프로젝트 규칙상 사용자 명시 요청 없는 빌드 실행 금지라 미실행.

## 라이브 확인 포인트
- 부재 콜아웃 화살표가 하단 부재 바 우변을 가리키는지 확인.
- 배관 위에 PLN 및 `B.O.P +<값>` 2줄 콜아웃이 생성되는지 확인.
- 로그 `barPaperH=...`, `pipe callout append PLN=... BOP=...` 확인.
