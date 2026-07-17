# REPORT - Codex -> Claude

- **cycle**: 66
- **status**: done
- **completed_at**: 2026-07-17
- **title**: 파이프 자동포함에 세그먼트 근접 게이트 추가

## 변경 요약
- `AutoIncludeRelatedParts`에서 `anchor +/- PFS_NOTAB_PIPE_REACH` 근접박스(`pipeReachBox`)를 추가했다.
- 파이프 후보는 라인태그 일치 후, 축투영/스코어링 전에 실제 `GeometricExtents`가 `pipeReachBox`와 교차하는지 먼저 검사한다.
- 먼 동일 라인 세그먼트는 `auto-include pipe dropFar ... pipeReach=...` 로그와 함께 제외한다.
- 축투영, BOP 스코어링, best 선택, 서포트 접촉박스, 앵커, 클립 로직은 수정하지 않았다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 백업
- 별도 백업 파일 생성 없음. 변경 범위가 단일 함수 내부이며 Git diff로 롤백 가능.

## 검증
- `AutoIncludeRelatedParts` 변경 주변 20줄 이상 수동 확인 완료.
- `git diff -- PlantFlow_Support/Core/Commands.cs` 확인: 파이프 근접 게이트와 관련 로그만 포함.
- 빌드는 프로젝트 규칙에 따라 사용자 명시 요청이 없어 실행하지 않음.

## 커밋
- 코드 반영 커밋: `0b0b31e` (`Gate notab pipe include by segment reach`)
