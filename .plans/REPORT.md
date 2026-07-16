# REPORT — Codex → Claude

- **cycle**: 59
- **status**: done
- **completed_at**: 2026-07-16
- **title**: 콜아웃 수평 부착 모드 설정

## 변경 요약
- `ApplyNotabCalloutNearEdgeAttachment`에서 `SetTextAttachmentType(..., LeftLeader)` 호출 전에 `MLeader.TextAttachmentDirection = AttachmentHorizontal`을 리플렉션으로 설정하도록 추가했다.
- 기존/신규 `ML_PIPETAG_85` `MLeaderStyle`에도 가능하면 `TextAttachmentDirection = AttachmentHorizontal`을 반영하도록 보강했다.
- 수평 부착 설정 성공/스킵과 `attachType=6`, `dir=LeftLeader`가 로그에 남도록 `FileDiag`를 보강했다.
- 좌표 노브, anchor 지향, 텍스트값, dogleg 계산, 세로75, 분할, 투영, held 관련 로직은 수정하지 않았다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- 변경 주변 수동 확인 완료.
- `git diff --check -- PlantFlow_Support/Core/Commands.cs` 실행: 오류 없음. 단, Git의 CRLF 변환 안내 경고만 출력됨.
- `rg -n "attachDir=|TrySetNotabTextAttachmentDirection|mleader style existing attachDir" PlantFlow_Support/Core/Commands.cs` 실행: 신규 설정/로그 위치 확인.
- 빌드는 프로젝트 규칙에 따라 사용자 명시 요청이 없어 실행하지 않음.

## 커밋
- 코드 반영 커밋: `4ecb830` (`Set notab callout horizontal attachment`)
