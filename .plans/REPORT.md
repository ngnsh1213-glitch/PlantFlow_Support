# REPORT - Codex -> Claude

- **cycle**: 86
- **status**: done
- **completed_at**: 2026-07-20
- **title**: cycle85 빌드 오류 및 문자-밑줄 간격 교정

## 변경 요약
- `TryPlace` 내부 후보 정점을 `candP1/candP2`로 분리해 out 파라미터 섀도잉을 제거했다.
- 뷰포트가 있는 외곽 메서드에서 파이프 페이퍼 반경을 계산해 전달하도록 바꿔 `vp` 스코프 오류를 제거했다.
- MText 삽입점을 `baseY + textGap`으로 교정해 문자 박스와 placer 검증 영역을 일치시켰다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.sln --no-restore` 통과: 오류 0, 기존 경고 15개.

## 라이브 검증 필요
- GD1/GD2/GD3 추출 후 GD3 리더 X자 교차, GD2 부재·파이프 관통, 문자-밑줄 3 이격을 확인한다.
- 로그에서 `callout-draw`의 `baseY/nearX/farX/cost/scanned` 및 `pipe-obstacle count=1`을 확인한다.

## 커밋
- 커밋 전
