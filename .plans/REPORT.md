# REPORT - Codex -> Claude

- **cycle**: 81
- **status**: done
- **completed_at**: 2026-07-19
- **title**: 우측 콜아웃 문자 방향 확정 — 리더 착지점과 TextLocation 분리(실측 폭 보정)

## 변경 요약
- 부재 및 파이프 콜아웃의 MLeader 생성·등록 직후, `calcSide=right`일 때만 `leader.MText.ActualWidth`를 읽어 `leader.TextLocation.X`를 실측 폭만큼 우측으로 보정했다.
- 마지막 리더 정점은 기존 `textPoint`를 유지했다. `calcSide=left`는 보정하지 않으며, placer·앵커·치수·부착 설정은 변경하지 않았다.
- `text-shift right` 진단을 render-check 이전에 남기도록 했다. MText가 null이거나 폭이 0이면 기존 동작을 유지한다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- 두 콜아웃 생성 지점의 변경 주변과 render-check 순서를 확인했다.
- `git diff --check` 통과.
- 빌드는 프로젝트 규칙에 따라 실행하지 않음.

## 라이브 검증 필요
- `dev_test.bat`에서 `PFS_NOTAB_TEST_TAG=GD1-001,GD2-001,GD3-001` 실행.
- `text-shift right` 로그 발생과 보정 후 render-check의 `mtLoc`·`mtActualW`를 확인.
- 우측 6건은 문자 범위가 anchorX 우측인지, 좌측 2건은 기존 위치를 유지하는지 확인.

## 커밋
- 코드 변경: `9e5e34d` (`fix: shift right notab callout text by actual width`)
