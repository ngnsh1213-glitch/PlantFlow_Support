# REPORT - Codex -> Claude

- **cycle**: 82
- **status**: done
- **completed_at**: 2026-07-20
- **title**: 콜아웃 문자 시작점 제어 — 실측 박스 기반 보정 + 최소 수평이격 40 + 세로 정렬

## 변경 요약
- `NotabCalloutPlacer.TryPlace`에 `minDx`를 추가하고 후보 중심 X가 앵커 X에서 40 미만인 경우 제외했다. 부재·파이프 호출부는 `PFS_NOTAB_CALLOUT_MIN_DX`(기본 40, 0~500)를 동일하게 전달한다.
- 부재와 파이프 콜아웃의 cycle81 `text-shift`를 제거했다. 생성 뒤 실제 `MText.ActualWidth/ActualHeight`와 Attachment로 문자 박스를 계산한 뒤, 앵커 반대쪽 근단 이격과 placer Y 중심에 맞는 최소 델타만 `TextLocation`에 반영한다.
- `render-check`에 `mtActualH`를 추가했고, 보정 진단은 `text-fit`에 실제/목표 박스, 이동 델타, 이격을 기록한다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- `TryPlace` 정의와 두 호출부가 새 `minDx` 인자를 모두 사용함을 정적 검색으로 확인했다.
- 부재·파이프 보정 및 `render-check mtActualH`의 변경 주변을 확인했다.
- `git diff --check` 통과.
- 프로젝트 규칙에 따라 빌드는 실행하지 않음.

## 라이브 검증 필요
- `dev_test.bat`으로 GD1-001, GD2-001, GD3-001을 추출한다.
- 모든 `text-fit` 로그의 `sep >= 40`, 앵커 반대 방향, GD2 세로 겹침 해소를 확인한다.
- 이전 GD1 우측 문자 폭만큼의 일괄 오버슛이 없는지 `d=`와 도면에서 확인한다.

## 커밋
- 코드 변경: `b114d5d` (`fix: fit notab callout text by actual bounds`)
