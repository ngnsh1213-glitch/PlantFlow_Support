# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 30
- **status**: ready
- **issued_at**: 2026-07-14
- **title**: 무탭 N2 정리 — A1 레이아웃/플롯 정합 + 저장 무결성(RECOVER 경고)
- **target**: PlantFlow_Support/Core/Commands.cs

## 착수 전
- cwd `...\PlantFlow_Support`. PFSNOTABDETAIL/CreateNotabDetailDrawing만. 기존 무수정.

## 배경 (cycle 29 = 크래시 해결 ✅)
- 평범 DB(new Database(true,true))+solid+Hidden뷰포트+템플릿 타이틀블록 2D 클론 → **크래시 없이 commit 완료+saved**. §9 #3 적중.
- 잔여 2건:
  1. 저장 시 "An error occurred during save. run RECOVER" 경고(무결성).
  2. `layout A1 limits skip: Limits setter 없음` → `plotArea=(0,0)~(12,9)` = 평범 DB 기본 레이아웃 12×9. 타이틀블록은 A1(~841×594) 좌표 클론 → 레이아웃/뷰포트와 미스매치.

## 지시 (cycle 30)

### Fix A — A1 레이아웃/플롯 정합
- 평범 DB 레이아웃("Title Block")을 A1로 설정:
  - `PlotSettingsValidator psv = PlotSettingsValidator.Current;`
  - 레이아웃의 PlotSettings에 `psv.SetPlotConfigurationName(ps, "None", null)`(또는 유효 device) 후 `psv.SetCanonicalMediaName(ps, "ISO_full_bleed_A1_(841.00_x_594.00_MM)")` 등 **A1 미디어** 설정. 미디어명은 `psv.GetCanonicalMediaNameList(ps)`에서 "A1" 포함 항목 탐색해 선택(로그로 남김).
  - `layout.CopyFrom(ps)` 반영.
  - 실패 시 FileDiag 후 계속.
- **뷰포트 drawing-area는 Limits(12×9) 대신 A1 하드좌표 사용**: plotArea를 (0,0)~(841,594)로 고정(또는 A1 미디어 크기 조회). drawing-area box 계수(RightInset 등) 그대로 A1 기준 적용. → 뷰포트가 A1 프레임 안에 정합.
- 로그: `PFSNOTABDETAIL A1 media=.. plotArea=(0,0)~(841,594)`.

### Fix B — 저장 무결성(RECOVER 경고)
- 저장 직전 `detailDb.Audit(AuditPassType.Fix, false)` 또는 가능한 진단 호출로 무결성 정리 시도(예외 방어). 로그 `PFSNOTABDETAIL audit errors=..`.
- 타이틀블록 2D 클론 시 dangling 의존(플롯스타일 GEC_PIPING.ctb, 레이어) 여부 점검:
  - WblockClone 후 클론 엔티티의 LayerId/LinetypeId 유효성 확인, 무효면 기본값(레이어 0)로 교정.
  - 플롯스타일 테이블 참조가 dangling이면 detailDb 기본 CTB로.
- 목적: "run RECOVER" 경고 제거(또는 원인 로깅).

## 규율
- 기존 무수정. 빈 catch 금지, 예외 FileDiag. WorkingDatabase finally 복원, Dispose.

## 빌드/완료
- 수동 빌드 GREEN. 커밋(거부 시 Claude 대리). `.plans/REPORT.md`.
- 사용자: `PFSNOTABDETAIL`(서포트+파이프) → 저장 경고 없이 `_notab.dwg` 생성, **레이아웃(A1)에 은선 Main + 타이틀블록 프레임이 정합 배치**. 로그 `A1 media`, `audit`.
