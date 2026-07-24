# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 123
- **status**: ready
- **issued_at**: 2026-07-24
- **title**: 무탭 2D 평면화 지연 후처리 — PFSNOTABFLATTEN 신규 명령(현재 활성 상세도 in-place)
- **작업 경로**: `PlantFlow_Support/Core/Commands.cs` (신규 명령 + TryFlattenNotabDetailB 1985~ 대체, 재사용 헬퍼 2133~2297)
- **계획서**: `.plans/plan_notab_flatten_deferred_20260724.md`
- **진단 원장**: 없음(계획서에 cycle122 실측 요약)
- **기준 커밋**: `b4cc9d7`
- **자문**: Codex(§9). 채택 = ①일반 CommandMethod(Session 플래그 금지) ②동일 DB 이식=**DeepCloneObjects**(WblockCloneObjects 금지)+원본 모델 FLATSHOT 블록 Erase ③모델공간 사전검사(Solid3d 외/기존 FLATSHOT 있으면 NOGO) ④검증 카운트 로그(dims/vp/flatObj 전후) ⑤신규 BlockReference 정확히 1개 가드.

## 배경 (cycle122 실측)
인라인 B변형 `stage=flatshot eInvalidInput` — PFSNOTABDETAIL 명령 진행 중 교차문서 Editor.Command 불가. 인라인 스파이크 코드는 플래그(PFS_NOTAB_FLATTEN) 뒤라 존치·무영향. 지연 후처리로 재설계.

## ⚠ 검증 필수
`dotnet build` 오류 0(빌드·커밋 분리) 없이 커밋 금지. 미실행 시 `status: blocked`. **push 금지**.

## 집도 항목 (스파이크 — 신규 명령)
1. **`[CommandMethod("PFSNOTABFLATTEN")]`** — `CommandFlags.Session` 붙이지 말 것. 현재 활성 doc에서만 동작(문서 Open/전환 없음).
2. **사전검사**: 활성 doc 모델공간이 Solid3d만 갖는가. 그 외 엔티티 or 기존 FLATSHOT BlockReference 있으면 `PFSNOTABFLATTEN2 NOGO reason=modelspace-dirty` 로그 후 중단.
3. **뷰포트 탐색+스냅샷**: 페이퍼(레이아웃)에서 뷰포트 1개 → `TryGetNotabFlattenViewportSnapshot` 재사용. 0개면 skip.
4. **FLATSHOT**: TILEMODE=1 + `SetNotabFlattenTempView`(스냅샷) → `Editor.Command("_.-FLATSHOT","_Insert","_ByBlock","_ByBlock","_No","_No","0,0,0",1.0,1.0,0.0)`. baseline 대비 신규 BlockReference **정확히 1개** 확인(2개 이상이면 NOGO reason=flat-block-ambiguous).
5. **측정**: `NotabFlattenDcsToPaperExtents` → `GetNotabFlattenAlignError` vs `PFS_NOTAB_FLATTEN_TOL`(2.0). 로그 `PFSNOTABFLATTEN2 lines=… flatBbox=… projBbox=… alignErr=… tol=… gate=GO|NOGO`.
6. **GO 이식**: `DeepCloneObjects`로 모델공간 FLATSHOT BlockReference→레이아웃 BTR 복제 + 아핀 변환(paperX=vp.Center.X+(dcsX-vp.ViewCenter.X)*scale) 적용 → 원본 모델 FLATSHOT BlockReference **Erase** → 뷰포트 삭제 → **QSAVE**. TryCloneNotabFlattenBlockToPaper를 DeepClone 방식으로 재작성.
7. **NOGO/사후정리**: 모델공간 FLATSHOT 출력 Erase해 원상복구(변경 0). 
8. **검증 카운트 로그**: 실행 전후 레이아웃 Dimension/MLeader/Table 수·뷰포트 수(1→0)·모델 FLATSHOT 출력(1→0). `… dims=<b>/<a> vp=<b>/<a> flatObj=<b>/<a>`.
9. `TryFlattenNotabDetailB`(1985~) 및 임시파일/Open/MDI 전환 로직 제거. 재사용 헬퍼는 현재 DB로 호출.

## 검증 레시피
- RC1-001_notab.dwg를 ACAD에서 **수동으로 연다**(활성). → `PFSNOTABFLATTEN` 입력.
- 로그: `PFSNOTABFLATTEN2 … alignErr=… gate=` + dims/vp/flatObj 전후 수치. 눈확인: 뷰포트 소멸·2D 그림·주석 정렬 유지.
- 첫 관문 = 이 신규 명령에서 Editor.Command FLATSHOT이 eInvalidInput 없이 실행되는가(같은-문서 합법성 실측).
