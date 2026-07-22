# 계획서 — 무탭 추출 UI 통합 + 일괄 선택 + 로딩 오버레이 (2026-07-22)

- 성격: 다단계 프런트+백엔드 UI 설계. 다이얼 엑스트라~최대 권장.
- 사용자 확정: ①구 오쏘 export2D → **무탭으로 교체** ②일괄 선택 = **수동 영역/다중 선택** ③배치 UI 대개편(뷰방향 제거) ④초기 구동 로딩 오버레이.

## 재사용 코어 (이미 존재)
- `RunNotabDetailPipeline(doc, ObjectId[]) : bool` — 무탭 1건 추출.
- `CollectSelectedSupportIds(db, ObjectId[]) : List<ObjectId>` — 원시 선택에서 서포트만 필터.
- `PFSNOTABBATCH`가 영역선택→필터→건별 루프를 이미 구현(명령행).

## A. 배치 UI 대개편 — `frontend_support/src/app/views/DrawingView.tsx`
무탭은 **뷰 방향 개념이 없다**(고정 단일 디테일). 따라서:
- **뷰 방향 체크박스 7개(Main/Top/…/Right) 제거.** `selectedViews`·`toggleView`·`MAIN_VIEW`·`MULTI_VIEWS` 삭제.
- 목록 테이블의 **"View" 컬럼 제거** → 자리에 서포트 **타입/상태**(추출 결과 표시용) 컬럼. 초기값은 태그만, 진행 시 상태 갱신.
- "Add (도면 선택)" → **"서포트 선택 추가"**(뷰 인자 없이 단일/영역 선택으로 목록 추가).
- **"일괄 선택 추가" 버튼 신설** — 영역/다중 선택 → 서포트만 필터 → 목록 일괄 추가.
- "Export 2D" → **"무탭 추출"**. 체크된 서포트를 건별 무탭 추출.
- **Grid 게이트 제거**(권고): 구 `Export2DCore`는 `GridSystemExists()`를 요구했으나 무탭(`RunNotabDetailPipeline`)은 그리드를 쓰지 않는다. "Grid 미설정 → 추출 불가" 배너와 `Grid System - Phase 1b` 자리표시자 제거. **← 확인 필요**
- 진행률: 일괄 추출 시 `N/M` 토스트/상태. (백엔드가 건별 진행 콜백을 못 주면 완료 후 요약만 — 1차는 요약으로.)

## B. 브리지/백엔드 배선
- **`DrawingBridge.cs`**: 계약 유지(ok/fail). 신규 메서드명만 추가.
- **`PaletteTab.Drawing.cs`**:
  - `AddFromSelectionAsync`에서 **viewDirs 인자 제거/무시**(무탭은 뷰 불요). 기존 `AddSupportCore`가 뷰 목록을 받으므로, 무탭용은 뷰 없이 서포트 태그만 목록화하는 경로로 정리.
  - `Export2DAsync` → **무탭 호출로 교체**: 체크된 태그의 ObjectId를 모아 `ExecuteInCommandContextAsync` 안에서 `RunNotabDetailPipeline` 건별 호출(성공/실패 카운트 반환). 구 `Export2DCore`(PFSORTHOCONTINUE 체인) 미사용.
  - **신규 `addBatchFromSelection`**: `ed.GetSelection()`→`CollectSelectedSupportIds`→태그 조회→목록 일괄 추가. (PFSNOTABBATCH의 수집부 재사용, 추출은 하지 않고 목록에만 담아 사용자가 확인 후 "무탭 추출"로 실행.)
  - `exportMTO`는 보류 유지.
- **MDI 가드 유지**: `_captureDocName` 일치 검사 그대로.

## C. 로딩 오버레이 — `AppShell.tsx` (+ 신규 `LoadingOverlay.tsx`)
- 초기 구동 시 전체화면 오버레이: `Loader2 animate-spin` + **"PlantFlow_Support 로딩중"** (PFO 관용구 이식).
- 표시 종료 조건: 브리지 준비 + 최초 상태 로드 완료(또는 웹 미리보기 모드 감지) 시 페이드아웃.
- AppShell에 `booting` 상태 추가, 최초 `getState`/타이머 완료까지 오버레이.

## D. 검증
- `frontend_support` 빌드(vite) → `dist/app.html` 갱신 → 플러그인이 로드하는 산출물 반영 확인.
- C# `dotnet build` 오류 0.
- 라이브: `dev_test.bat` 아님(자동추출임). 팔레트에서 서포트 선택→일괄 추가→무탭 추출 수동 확인.

## 미결/확인 필요
1. **Grid 게이트 제거**가 맞는가(무탭은 그리드 불요 확정?). — 코드상 무탭은 그리드 미사용이나 사용자 확인.
2. 목록 "View" 컬럼을 타입/상태로 바꾸는 것 vs 단순 제거.
3. 일괄 추출 **진행률 실시간 표시**를 1차에 넣을지, 완료 요약만으로 시작할지.

## 범위 밖(후속)
- 라인 단위/타입 단위 일괄 선택(TODO 별 항목).
- 생성 탭·자동 채번·세트(UBOLT 동반).
- MTO 재구현.
