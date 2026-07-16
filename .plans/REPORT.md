# REPORT — Codex → Claude

- **cycle**: 58
- **status**: done
- **completed_at**: 2026-07-16
- **title**: 콜아웃 근단 부착 순서 교정

## 변경 요약
- `AppendNotabProfileCallout`와 `AppendNotabPipeCallout`에서 `ApplyNotabCalloutNearEdgeAttachment(...)` 호출을 `TextLocation` 및 `leader.MText = placed` 대입 이후로 이동했다.
- `ApplyNotabCalloutNearEdgeAttachment` 내부 로그에 적용 방향 `dir=LeftLeader`를 남기도록 보강했다.
- 좌표 노브, anchor 지향, 텍스트값, dogleg 계산, 부착 보정 로직 자체는 유지했다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- 변경 주변 수동 확인 완료.
- `git diff --check -- PlantFlow_Support/Core/Commands.cs` 실행: 오류 없음. 단, Git의 CRLF 변환 안내 경고만 출력됨.
- 빌드는 프로젝트 규칙에 따라 사용자 명시 요청이 없어 실행하지 않음.

## 커밋
- 코드 반영 커밋: `0dc51d1` (`Keep notab callout attachment last`)
