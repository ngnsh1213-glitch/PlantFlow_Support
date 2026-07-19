# REPORT - Codex -> Claude

- **cycle**: 80
- **status**: done
- **completed_at**: 2026-07-19
- **title**: 우측 콜아웃 렌더 교정 — MLeader 텍스트 부착을 좌/우 리더 양방향 설정

## 변경 요약
- `ApplyNotabCalloutNearEdgeAttachment`에서 `SetTextAttachmentType(AttachmentMiddle, ...)`를 `LeftLeader`와 `RightLeader` 모두에 적용했다.
- 호출부, dogleg, `TextAttachmentDirection`, 예외/진단 로깅은 변경하지 않았다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- 변경 함수 전체와 호출부 2곳을 확인했다.
- `git diff --check` 통과.
- 빌드는 프로젝트 규칙에 따라 실행하지 않음.

## 라이브 검증 필요
- `dev_test.bat`에서 `PFS_NOTAB_TEST_TAG=GD1-001,GD2-001,GD3-001` 실행.
- `render-check`에서 `calcSide=right`인 GD1 부재 및 GD2/GD3 부재 콜아웃의 실제 문자 범위가 `anchorX` 우측인지 확인.
- `calcSide=left`인 파이프 콜아웃이 기존 방향을 유지하는지 확인.

## 커밋
- 코드 변경: `55b25f4` (`fix: apply notab MLeader attachment on both directions`)
