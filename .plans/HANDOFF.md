# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 22
- **status**: ready
- **issued_at**: 2026-07-13
- **title**: 무탭 엔진 Phase N2 — PFSNOTABDETAIL: solid + Hidden 뷰포트 Main (치수 전)
- **plan**: C:\Users\HT노승환\.gemini\antigravity\scratch\plan_pfs_notab_engine_20260713.md
- **target**: PlantFlow_Support/Core/Commands.cs (신규 커맨드 · 기존 파이프라인 무수정)

## 착수 전
- cwd `...\PlantFlow_Support`. ★기존 PFSVBISO* 체인 무수정. N1 헬퍼(`CloneSelectionToSideDatabase`/`CollectSolidsRecursive`) 재사용.

## 배경 (N1 PASS)
- 문서 없이 side-DB 재귀 explode로 파이프+서포트 Solid3d 추출 확인(solids=2, depth3 서포트 solid).
- 스파이크 C: side-DB에 solid + Hidden 뷰포트 → 은선제거 Main 렌더 확인.
- 목표: 두 결과를 합쳐 **문서 0으로 별도도면(Main, 타이틀블록)** 생성. 치수는 N3.

## 지시 (cycle 22) — 신규 `PFSNOTABDETAIL`
`[CommandMethod("PFSNOTABDETAIL", CommandFlags.Session)]`. 흐름:

1. `ed.GetSelection()`(서포트+파이프). `CaptureIsoSelectionMetrics`(:3249) 재사용해 s_isoRealWidth/Height·s_isoSupportTag·s_isoPipeLineNo·s_isoBOP·s_isoPipeAxis 확보.
   - ★**up 벡터 저장 추가**: CaptureIsoSelectionMetrics 내 계산된 up(~:3264)을 신규 static `s_isoPipeUp`에 저장(현재 버려짐). (선언 :26 인근, 초기화 :896 인근.)
2. **solid 추출(N1 재사용)**: `CloneSelectionToSideDatabase` → `CollectSolidsRecursive`로 Solid3d ObjectId 수집(sourceDb 내).
3. **tagDb 생성**: `ResolveIsoTemplatePath()` → `new Database(false,true).ReadDwgFile`. `HostApplicationServices.WorkingDatabase=tagDb` 스왑(finally 복원).
4. **solid 이송**: sourceDb의 Solid3d들을 tagDb ModelSpace로 `WblockCloneObjects`(Ignore). 이송된 solid extents(중심 dcx/dcy/dcz) 계산.
5. **FACETRES=10**: tagDb에 `db.Setapdbmod`? 아니면 `Application.SetSystemVariable`은 활성DB용 → tagDb는 side-DB라 FACETRES는 뷰포트 저장에 영향. 우선 `tagDb.Facetres = 10.0;`(Database.Facetres 있으면) 시도, 없으면 skip+로그.
6. **Hidden 뷰포트**(FitIsoDetailViewport의 drawing-area/plotArea 로직 재사용 + 3D뷰 세팅 추가):
   - "Title Block" Layout에 신규 Viewport(drawing-area box: plotArea Limits 기반, 기존 계수).
   - `vp.ViewDirection = s_isoPipeAxis;` `vp.ViewTarget = solids중심(dcx,dcy,dcz);`
   - up 정렬: `vp.TwistAngle` = viewDir 기준 s_isoPipeUp가 화면 위를 향하도록 계산(안 되면 0 + 로그).
   - `vp.VisualStyleId = Hidden`(VisualStyleDictionary서 "Hidden" 검색, 스파이크 C 방식).
   - `vp.ShadePlot = Viewport.ShadePlotType.Hidden;` (열거자명 실제 확인).
   - `vp.CustomScale = 0.5;`(1:2) `vp.On = true;`
   - 로그: `PFSNOTABDETAIL viewport viewDir=.. target=.. twist=.. hidden=.. shadePlot=.. scale=1:2`.
7. **타이틀블록**: `UpdateIsoTitleBlockAttributes(tr, tagDb, tag)` 재사용.
8. **저장**: `<ProjectDwgDirectory>\Details\<safeTag>_notab.dwg` (★기존 <tag>.dwg 안 건드리게 `_notab` 접미사). WorkingDatabase 복원, tagDb.Dispose.
9. 치수는 이번 단계 생략(N3).

## 규율
- 기존 파이프라인/커맨드 무수정. cross-DB=WblockClone. WorkingDatabase finally 복원. 빈 catch 금지. 원본 비파괴.

## 빌드/완료
- 수동 빌드 GREEN. `.plans/REPORT.md` 결과. 커밋(거부 시 Claude 대리).
- 사용자: `PFSNOTABDETAIL`(서포트+파이프 선택) → `Details\<tag>_notab.dwg` 열어 **Title Block 레이아웃에 은선제거 Main(파이프원+서포트사각형) + 타이틀블록** 확인. 로그 `viewport ... hidden=..`.
