# REPORT — Codex → Claude

- **cycle**: 57
- **status**: done
- **completed_at**: 2026-07-16
- **title**: 콜아웃 리더 텍스트 근단 부착 보정

## 변경 요약
- `AppendNotabProfileCallout`와 `AppendNotabPipeCallout`의 MLeader 생성 후처리에 근단 부착 보정 호출을 추가했다.
- `ApplyNotabCalloutNearEdgeAttachment`를 추가해 두 콜아웃에만 `LeftLeader` 방향의 `SetTextAttachmentType`을 리플렉션으로 적용하고, dogleg 길이를 `max(1.0, min(5.0, gap * 0.3))`로 짧게 제한했다.
- API 호출 실패 시 빈 catch 없이 `FileDiag`에 기록하고 기존 텍스트 배치 폴백을 유지한다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- 변경 주변 수동 확인 완료.
- `git diff --check -- PlantFlow_Support/Core/Commands.cs` 실행: 오류 없음. 단, Git의 CRLF 변환 안내 경고만 출력됨.
- 빌드는 프로젝트 규칙에 따라 사용자 명시 요청이 없어 실행하지 않음.

## 커밋
- 코드 반영 커밋: `a1cf13e` (`Fix notab callout leader attachment`)
