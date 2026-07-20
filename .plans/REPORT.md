# REPORT - Codex -> Claude

- **cycle**: 85
- **status**: done
- **completed_at**: 2026-07-20
- **title**: 리더 2세그먼트 충돌 정합 + 파이프 장애물 + 문자-밑줄 간격

## 변경 요약
- placer가 실제 리더 `anchor→p1→p2` 두 세그먼트를 장애물·기존 문자·기존 리더에 검사하고, 채택 후 모두 등록한다.
- `PFS_NOTAB_CALLOUT_TEXT_GAP`(기본 3)과 `PFS_NOTAB_CALLOUT_PAD`(기본 2)를 적용하고, placer가 확정한 `p1/p2`를 작도부가 그대로 사용한다.
- 파이프 중심과 반경×뷰포트 스케일로 파이프 장애물을 추가하며, 파이프 콜아웃에만 owner=`pipe` 예외를 적용한다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- `git diff --check` 통과.
- 2세그먼트 등록, pad·textGap·pipe-obstacle 정적 검색 확인.
- 프로젝트 규칙에 따라 빌드 및 라이브 도면 추출은 실행하지 않음.

## 라이브 검증 필요
- GD1/GD2/GD3 추출 후 GD3 리더 X자 교차, GD2 부재·파이프 관통, 문자-밑줄 3 이격을 확인한다.
- 로그에서 `callout-draw`의 `baseY/nearX/farX/cost/scanned` 및 `pipe-obstacle count=1`을 확인한다.

## 커밋
- 최신 커밋 `fix: align notab callout leader collisions` (해시는 `git log -1 --oneline`으로 확인)
