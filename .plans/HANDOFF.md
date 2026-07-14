# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 41
- **status**: ready
- **issued_at**: 2026-07-14
- **title**: 뷰포트 크기·위치 교정(도면영역 정합) + #1 페이퍼 초기화면 기본 승격
- **target**: PlantFlow_Support/Core/Commands.cs
- **plan**: `<appDataDir>\scratch\plan_pfs_notab_layout_fit_20260714.md`

## 착수 전
- cwd `D:\PlantFlow\PlantFlow_Support`. `CreateNotabDetailViewport`/`TryReopenSetNotabPaperSpace` 및 관련만. 그 외 무수정.
- 빌드 GREEN까지 Codex 확인. 라이브는 사용자.

## 배경 (cycle 40 라이브 = #1 성공)
- #1 reopen 방식 라이브 PASS: 크래시/RECOVER 없음, Title Block 페이퍼 공간으로 열림.
- 잔여: 뷰포트 크기가 타이틀블록 도면영역과 불일치(현재 640.5×573.5, 도면영역 610×489). LL 오프셋 (30.5,84.5)만큼 큼(640.5−30.5=610, 573.5−84.5=489 정확 일치).

## 집도 지시 (2개)

### A. 뷰포트 사각형 교정 (`CreateNotabDetailViewport`)
- 현재 `center=(350.75,371.25) width=640.5 height=573.5`(LL 30.5,84.5)를 아래로 교체:
  - **CenterPoint=(366, 413.5)**, **Width=610**, **Height=489** (LL=(61,169), UR=(671,658) 유지).
- 로그: `PFSNOTABDETAIL viewport rect LL=(61,169) size=(610,489) center=(366,413.5)`.
- ★ B(supportExt target)·C(scale 1:4)는 그대로. 뷰포트 크기만 변경.

### B. #1 마커 기본 승격 (`TryReopenSetNotabPaperSpace`)
- 파일 마커 `%TEMP%\pfs_notab_paper.flag` 게이트 **제거** → reopen paper-space를 **항상 적용**(기본 ON).
  - (cycle 40에서 크래시/RECOVER 없이 검증 완료 = 상시 적용 안전.)
  - 로그: `PFSNOTABDETAIL paper-reopen tilemode=0 ok`(마커 무관 항상).
- reopen 흐름(생성자 `new Database(false,true)`, `SaveAs(...SecurityParameters)`, WorkingDatabase 미변경)은 그대로 유지.

## 검증 (Codex)
- `dotnet build` GREEN(오류 0). Viewport center/width/height 값·마커 게이트 제거 확인.
- `git diff --check` PASS. 빈 catch 없음. B/C/E·핵심 흐름 무손상. `pfs_notab_paper.flag` 참조 제거 확인(`rg`).
- REPORT에 교정 좌표·마커 제거 명시.

## 라이브 검증 (사용자, 빌드 후)
1. `PFSNOTABDETAIL` 실행(마커 불필요) → `Details\GD1-001_notab.dwg` 열기.
2. 판정:
   - 열자마자 **Title Block 페이퍼 공간**(크래시/RECOVER 없음).
   - **뷰포트가 도면영역(초록 프레임 안쪽)에 정합**(610×489, LL 61,169).
   - 서포트 뷰포트 중앙, 1:4.
3. PASS → 레이아웃 정합 완결 → **N3(치수) 착수**. 미세 오차 시 좌표 재조정.
