---
task: PFS 격리 B4e — 중복 detail 제거 + 뷰큐브 redraw
cycle: 1
status: ready
from: claude
to: codex
plan: C:\Users\HT노승환\.gemini\antigravity\scratch\plan_pfs_iso_b4e_dedup_viewcube_20260713.md
target: PlantFlow_Support/Core/Commands.cs
---

# 지시 (B4e cycle 1)

두 결함 동시 수정. 대상 `PlantFlow_Support/Core/Commands.cs` 만.

## 배경
- B4d(f6c99fa)로 가로 분할 치수(100/200/300)는 값 정확. 그러나:
  1. 클론백이 기존 PFS_ISO_DETAIL을 안 지우고 매번 덧붙여 재실행 시 겹침("100" 이중).
  2. 파이프라인 후 뷰큐브 미표시(설정 OFF 아님 — redraw 누락. 비주얼스타일 변경하면 복귀).
- 사용자 결정: 재실행 시 **기존 detail 삭제 후 최신 1개만**(태그당 최신 도면 1개).

## Fix A — 기존 PFS_ISO_DETAIL/AUTO_DIM 제거
- 위치: `PFSVBISOEXPORTED`(:1497) 클론백 블록. `EnsureIsoAnnotationResources`(:1603) 호출 직후, `sideDb.WblockCloneObjects(...)`(:1609) **직전**.
- 레이어명: detail=`PFS_ISO_DETAIL`(detailLayerId, :1599), 치수=`AUTO_DIM`(annotationLayerId, :1603).
- 구현: `targetMs`(ModelSpace, 이미 ForWrite:1598) 순회.
  - 먼저 삭제 대상 ObjectId를 리스트로 수집(순회 중 Erase 회피): 각 id를 ForRead로 열어 `ent.LayerId == detailLayerId || ent.LayerId == annotationLayerId`면 리스트에 add.
  - 순회 후 리스트의 각 id를 ForWrite로 열어 `ent.Erase()`.
  - per-entity try/catch + FileDiag, null skip. 반드시 WblockClone 전에 수행.
  - 로그: `PlantOrthoView.FileDiag("PFSVBISOEXPORTED priorDetail purge erased=" + n)`.

## Fix B — 뷰큐브 redraw
- 위치: `CloseIsoTempDocument(Document tempDoc, Document originalDoc)`(:1705). originalDoc 재활성(:1711) 후.
- 구현: originalDoc 활성화 성공 후
  ```
  try {
    if (originalDoc != null)
      originalDoc.SendStringToExecute("._REGEN\n", true, false, false);
  } catch (System.Exception ex) {
    PlantOrthoView.FileDiag("PFSVBISOEXPORTED viewcube REGEN 예외: " + ex.GetType().Name + ": " + ex.Message);
  }
  ```
- 로그: `PlantOrthoView.FileDiag("PFSVBISOEXPORTED viewcube REGEN 큐잉 doc=" + originalDoc.Name)`.

## 규율
- PFS_ISO_DETAIL/AUTO_DIM 레이어 한정 삭제만 허용. 다른 레이어 불가침.
- 빈 catch 금지, 예외 FileDiag. B4d 분할/세로 치수 로직 불변.

## 빌드
- 수동 빌드 GREEN 확인. tools\** glob 이슈 있으면 csproj `<Compile Remove="tools\**" />`.

## 완료 보고
- 커밋 후 이 파일 하단에 RESULT 섹션 append + 커밋.
- 라이브 확인: 같은 서포트 2회 실행 → 치수/detail 1세트만 + `priorDetail purge erased=..`, 파이프라인 후 뷰큐브 즉시 표시.

## RESULT (Codex)
- status: done
- commit: cf44403
- backup: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260713_B4e`
- changed: `PlantFlow_Support/Core/Commands.cs`
- summary:
  - clone-back 직전 `PFS_ISO_DETAIL` / `AUTO_DIM` 레이어 엔티티를 수집 후 삭제하는 `PurgePriorIsoDetail` 추가.
  - 기존 detail purge 로그 `PFSVBISOEXPORTED priorDetail purge erased=...` 추가.
  - temp 문서 종료 전 원본 문서 재활성 후 `._REGEN` 큐잉 및 viewcube REGEN 로그 추가.
- verification:
  - `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS
  - braces: `opens=404 closes=404 balance=0`
  - malformed escaped quotes search: PASS
  - build: not run (AGENTS 빌드 수동 원칙)