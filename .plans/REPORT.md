# REPORT - Codex -> Claude

- cycle: 54
- status: done
- commit: 최종 HEAD 참조
- title: 무탭 부재 콜아웃 리더 형상 정련

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `AppendNotabProfileCallout`에서 support paper 높이 `h`를 계산.
  - anchor를 부재 우변 `minY + h * 0.55`로 이동.
  - elbow를 `maxX + offset * 2.0`, `minY + h * 0.10`으로 이동해 더 길게 우하향 인출되도록 조정.
  - textPoint는 기존처럼 `elbow.X + gap`, `elbow.Y`를 유지해 수평 dogleg 우측에서 텍스트가 시작되도록 보존.

## 보존 확인
- 코드 수정은 핸드오프 지정 범위인 `AppendNotabProfileCallout` 좌표 계산부에 한정.
- MText `MiddleLeft`, `TextLocation`, 스타일, 트랜잭션, 세로치수 75, 분할라벨, 오프셋/적층 30, GD1/RC1, 투영, held-pipe 로직은 변경하지 않음.
- 기존 작업트리의 무관 변경/삭제/미추적 파일은 스테이징하지 않음.

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `rg -n "h \\* 0\\.55|h \\* 0\\.10|offset \\* 2\\.0|callout append designation" PlantFlow_Support/Core/Commands.cs`: 참조 확인.
- `git diff -- PlantFlow_Support/Core/Commands.cs`: 지정 변경만 확인.
- 빌드: 프로젝트 규칙상 사용자 명시 요청 없는 빌드 실행 금지라 미실행.

## 라이브 확인 포인트
- RS11형 `PFSNOTABDETAIL`: 리더가 부재 우변에서 길게 우하향 사선으로 내려간 뒤 하단 근처에서 수평 dogleg와 텍스트로 이어지는지 확인.
- 로그 `callout append designation=... anchor=... elbow=... text=...`로 anchor/elbow/text 좌표 확인.
