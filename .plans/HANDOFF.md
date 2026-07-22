# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 104
- **status**: ready
- **issued_at**: 2026-07-22
- **title**: 무탭 추출 UI 통합 + 수동 영역/다중 선택 일괄추가 + 초기 로딩 오버레이
- **작업 경로(백엔드)**: `PlantFlow_Support/Core/Commands.cs`, `PlantFlow_Support/UI/PaletteTab.Drawing.cs`, `PlantFlow_Support/UI/DrawingBridge.cs`
- **작업 경로(프런트)**: `frontend_support/src/app/views/DrawingView.tsx`, `frontend_support/src/app/AppShell.tsx`, `frontend_support/src/app/api/drawingApi.ts`, (신규) `frontend_support/src/app/LoadingOverlay.tsx`
- **계획서**: `.plans/plan_notab_ui_batch_20260722.md`
- **기준 커밋**: `d6f33ac`
- **자문**: §9 Codex(우선)+Gemini(보조) 1회. 분기(DoEvents)는 Codex 채택.

## ⚠ 검증 필수
`dotnet build` 오류 0 + `frontend_support` 빌드(vite) 성공이 완료 조건이다. 미실행 시 커밋하지 말고 `status: blocked` 반려.
Claude가 REPORT 수령 후 **직접 빌드를 재확인**한다.

## 사용자 확정 사항
1. 구 오쏘 export2D → **무탭으로 교체**(구 오쏘는 UI에서 제거, 명령 PFSVBMULTI 등으로만 잔존).
2. 일괄 선택 = **수동 영역/다중 선택**(라인/타입 단위는 범위 밖).
3. 배치 UI 대개편: **뷰 방향 개념 제거**(무탭은 고정 단일 디테일).
4. **Grid 게이트 제거**(무탭은 그리드 미사용). 5. 목록 "View" 컬럼 → **타입/상태**. 6. 진행률 = **완료 요약만**(실시간 후속).

## 재사용 코어 (현 상태 — private)
- `bool RunNotabDetailPipeline(Document doc, ObjectId[] ids)` — 무탭 1건 동기 추출.
- `List<ObjectId> CollectSelectedSupportIds(Database db, ObjectId[] ids)` — 서포트 필터.
- `PFSNOTABBATCH`(Commands.cs:160) 루프 = `CollectSelectedSupportIds`→건별 `RunNotabDetailPipeline(new ObjectId[]{supportId})`→끝에 `GC.Collect`.
- `SupportSelection : Dictionary<string, SupportInfo>` (이름 키). `SupportInfo`는 서포트 `Ids`·`Extents` 보유. `_captureDocName`으로 캡처 문서 가드.

## 집도 지시

### 1) [백엔드] 공개 파사드 신설 (Codex 자문: private라 UI 직접호출 불가)
`Commands.cs`에 UI·명령 공용 공개 메서드 신설. `PFSNOTABBATCH`의 루프 본문을 이 파사드로 추출하고 명령도 이를 호출하도록 리팩터(중복 제거):
```
public bool RunNotabBatch(Document doc, IList<ObjectId> supportIds, out int ok, out int fail, out List<string> failTags)
```
- 건별 `RunNotabDetailPipeline(new ObjectId[]{ supportIds[i] })`, ok/fail/failTags 집계, 끝에 `GC.Collect()+WaitForPendingFinalizers()`(현행 유지), 전 구간 `FileDiag`.
- `PFSNOTABBATCH` 명령은 DRYRUN 분기 유지 후 실추출을 `RunNotabBatch` 호출로 대체.
- **정적 상태 병렬 불가**: 파사드 진입 시 이미 실행 중이면 즉시 실패 반환(재진입 가드). UI `_drawingBusy`와 별개로 백엔드에도 가드.

### 2) [백엔드] `Export2DAsync` → 무탭 교체 (`PaletteTab.Drawing.cs`)
자문 수렴: **fire-and-forget 스케줄링 전면 제거**, 단순 await로 복원.
- 무탭은 활성 문서를 닫지 않으므로(side-DB `ReadDwgFile`/`SaveAs`, 원본 유지) `ExecuteInCommandContextAsync`의 Task를 **정상 await**해도 완료신호 유실 없음(Codex/Gemini 일치). `TaskCompletionSource`·백그라운드 Task 패턴 삭제.
- 흐름:
  1. UI 스레드: 체크된 **태그명 스냅샷**(`string[]`). `item.Selected` 기반 전달 폐기(Codex/Gemini: 스냅샷이 안전).
  2. `_captureDocName` + Database 동일성 가드(현행 유지, Codex 권고로 Database까지).
  3. `ExecuteInCommandContextAsync` 안: 태그→`SupportSelection[tag].Ids`의 **서포트 ObjectId** 해석(캡처 문서 기준) → `RunNotabBatch` 호출.
  4. 복귀 후 ok/fail 요약을 응답으로. **Grid 게이트(`GridSystemExists` 검사) 제거.**
- 메서드/브리지 명칭은 `export2D` 유지 가능(프런트 계약 최소 변경) 또는 `exportNotab` 신설 중 택1 — **택일 시 프런트와 일치**시킬 것.

### 3) [백엔드] 신규 `addBatchFromSelection` (`PaletteTab.Drawing.cs` + `DrawingBridge` 디스패치)
- command context 안에서: **`Application.MainWindow.Focus()` 선행**(Gemini: WebView 포커스가 CAD 선택 입력 가로챔) → `ed.GetSelection()` → `CollectSelectedSupportIds` → 각 서포트 태그/`SupportInfo` 구성.
- **command context는 순수 DTO만 반환**(태그 목록 등). UI 스레드 복귀 후 `SupportSelection`·`lvSupportName` 갱신(중복 제거는 UI 스레드). Codex: `AddSupportCore` 통째 복제 금지(그건 command context 안에서 ListView를 직접 만짐).
- 취소/빈 선택 = 오류 아님 → `USER_CANCELLED` 또는 빈 추가 결과. React busy 해제되게.
- 기존 `addFromSelection`(단건, viewDirs)은 **뷰 인자 제거**: `AddSupportCore`에 뷰 없이 담는 경로로 정리하거나 신규 배치 경로로 통일. 뷰 방향은 무탭에서 완전 제거.

### 4) [프런트] `DrawingView.tsx` 대개편
- **뷰 방향 체크박스 7개 삭제**(`MAIN_VIEW`/`MULTI_VIEWS`/`selectedViews`/`toggleView` 제거).
- 버튼: "Add (도면 선택)" → **"서포트 선택 추가"**(단건), **"일괄 선택 추가"** 신설(→ `addBatchFromSelection`). "Export 2D" → **"무탭 추출"**.
- 목록 **"View" 컬럼 → "타입/상태"**(초기 태그만, 추출 후 ok/fail 상태 반영은 선택).
- **Grid 게이트 제거**: "Grid 미설정 → 추출 불가" 배너, `Grid System - Phase 1b` 자리표시자, `gridReady` 의존 삭제.
- 추출 완료 시 **요약 토스트**(`ok N / fail M`, 실패 태그 목록).
- `drawingApi.ts`: `addFromSelection` 시그니처에서 views 제거, `addBatchFromSelection` 추가, `export2D`(또는 `exportNotab`) 반환에 ok/fail 반영. 타입 안전(§ TS 규칙: any 금지, 명시 타입).

### 5) [프런트] 초기 로딩 오버레이 (`AppShell.tsx` + 신규 `LoadingOverlay.tsx`)
- 전체화면 오버레이: `Loader2 animate-spin`(lucide) + **"PlantFlow_Support 로딩중"**. PFO 관용구 이식(전용 스플래시 없음, `animate-spin`+텍스트).
- AppShell에 `booting` 상태. 최초 `getState` 성공 또는 웹 미리보기 모드 감지 시 오버레이 해제(페이드아웃). 브리지 미준비 구간을 덮는다.
- 배치 추출 중에도 이 오버레이(또는 동형 전역 오버레이)로 **입력 차단**(Gemini: 모드리스 중복클릭 방지). **`DoEvents()` 사용 금지**(Codex: command context 재진입 위험).

## 하지 말 것
- 구 오쏘 `Export2DCore`/`PFSORTHOCONTINUE` 정의 자체는 삭제하지 말 것(명령 경로 PFSVBMULTI 등에서 사용). UI 배선만 무탭으로 교체.
- `RunNotabDetailPipeline` 내부 로직·PERSPECTIVE 가드(cycle103) 변경 금지.
- 무탭 배치/밸룬/치수 로직 변경 금지.
- 라인/타입 단위 일괄, MTO 재구현, 생성 탭 — 범위 밖.

## 제약
- 빈 catch 금지, 실패 경로 `FileDiag`(백엔드)/명시 로깅(프런트, console.log 금지).
- 브리지 응답 계약(`ok/fail/code/error`) 유지.
- `item.Selected`를 CAD 전달 수단으로 쓰지 말 것 → 태그 스냅샷 후 command context서 ObjectId 해석.
- `#nullable disable` 파일(PaletteTab.Drawing.cs 제외 확인) — 명시 null 가드.

## 검증 (Codex, 필수)
1. `dotnet build` 오류 0, 경고 현행(15) 초과 금지.
2. `frontend_support` 빌드(vite) 성공 → `dist/app.html` 갱신. 플러그인이 로드하는 산출물 경로 확인.
3. 커밋 후 `REPORT.md`에 변경 요약·빌드 결과 기록. 라이브(팔레트 조작)는 사용자 수행.

## 성공 기준 (다음 라이브)
- 팔레트: 뷰 체크박스 없음, "일괄 선택 추가"로 영역 선택→서포트만 목록에 담김, "무탭 추출"로 건별 무탭 도면 생성, 완료 시 `ok/fail` 요약.
- Grid 미설정이어도 추출 가능.
- 초기 구동 시 "PlantFlow_Support 로딩중" 오버레이 표시 후 해제.
- 배치 중 UI 입력 차단, 중복 실행 없음.
