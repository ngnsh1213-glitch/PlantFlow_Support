# TODO — 잔여 작업 백로그

_완료 항목은 여기서 지우고 `CHANGELOG.md`로 옮긴다._

## ★무탭 엔진 (현재 트랙, 2026-07-14)
- [x] 무탭 Main 라이브 PASS (은선 정투영, cycle 30)
- [x] RECOVER 해결 (H5=sourceDb 조기 Dispose, cycle 37, 커밋 d7897e5)
- [x] 레이아웃 정합: 뷰포트 fixedRect LL(30.5,84.5)·640.5×573.5·1:4·target 서포트중심 (cycle 37 A/B/C)
- [ ] **#1 페이퍼 초기화면**: 열 때 Title Block 페이퍼공간 zoom-extents. ★cycle37 D(헤드리스 TileMode=0+활성레이아웃)=네이티브 크래시→원복(cycle38). **안전 방법 재구현 필요**(cycle 39).
- [ ] **진단 코드 cleanup (cycle 39)**: RECOVER 해결로 불필요해진 pnp-purge·plotter-normalize·keyscan·마커토글·wblock-fallback(항상 eInvalidInput) 제거, 직접 SaveAs 원복.
- [ ] **N3 치수**: 3D 실측→PSDCS 비연관, Main에만 (최대 리스크). 계획=plan_pfs_notab_n3_20260714.md
- [ ] N4(밸룬/BOP/BOM=AnnotateViewport 재사용) → N5(3부채 코드 소멸)

## 진행 중 / 다음 (VIEWBASE 오쏘 파이프라인)
- [ ] **격리**: 서포트 + 관통 파이프 구간만 추출 (현재 `_E` 상태)
- [ ] **멀티뷰**: Main + Top + ISO 격리된 대상으로 재생성
- [ ] **B2 치수**: 평면화 후처리로 실mm 치수, **Main 뷰에만**

## 미해결 / 후속
- [ ] test1 뷰 뒤집힘 (content-rotation) 해소
- [ ] Main 탑뷰 배제 (B3)
- [ ] 경사(slope) 후속 처리

## 격리 별도 도면 — 다음 트랙
- [ ] **주석 엔진 통합(밸룬+BOP/PLN 콜아웃+BOM)**: 새로 만들지 말고 기존 `OrthoViewportManager.AnnotateViewport` 재사용. 밸룬 소스=`SPInfo.AttachmentList`(멤버=SupportType"ATTACHMENT" 부착 서포트, PaletteTab.Events.cs:171-319에서 분석·R1/R2 명명). 콜아웃=`CreateMLeader(MText=PipeLineNo/"B.O.P+"+BOP)`. BOM=`BOMs(SPInfo.Ids[0], AttachmentList)`. **3축**: ①격리 선택/분석 확장(부착 서포트 포함→플래튼) ②SPInfo 채우기 ③AnnotateViewport를 tagDb(side-DB)에 대응. 라인넘버/BOP 콜아웃은 파이프 중심 leader(사용자 이미지1).
- [ ] **(미세조정, 후순위) 뷰포트 사각형-타이틀블록 정렬**: drawing-area 인셋 계수(0.14/0.10/0.03) 튜닝으로 뷰포트를 타이틀 프레임에 맞춤. 스케일(1:2)은 OK. 구현 우선, 정렬 나중.

## 무탭/엔진 재설계 트랙 (근본 원인=temp 문서 MDI 활성화 + 명령 기반 VIEWBASE/EXPORTLAYOUT)
> 아래 두 항목은 개별 패치 실패/불가로 엔진 재설계 시 함께 해결. 지금은 화면·프롬프트 잡음(기능 결함 아님).
- [ ] **뷰큐브 미표시**: temp 문서 open/close로 MDI 활성 전환 후 원본 뷰큐브 GS 위젯 재-paint 안 됨. `navvcube=3`(설정 ON) 확인 → redraw 문제 확정. REGENALL·NAVVCUBEDISPLAY 토글 2회 실패(커밋 0b817e5 외). VIEWBASE가 활성 문서 요구 → 완전 무탭 불가가 근본. 남은 후보: `Editor.UpdateTiledViewportsFromDatabase`/`Application.UpdateScreen` 1회 조사 여지.
- [ ] **EXPORTLAYOUT "open now?" 다이얼로그 억제**: EXPORTLAYOUT 명령이 띄우는 태스크 다이얼로그. FILEDIA/CMDDIA=0로 안 막힘. 결과 dwg는 이미 side-DB로 읽으므로 에디터 open 불필요 → API 평면화로 EXPORTLAYOUT 명령 대체 시 소멸. 단 과거 `.Explode(viewrep)` 사망으로 EXPORTLAYOUT 채택한 이력.

## 완료 (요약, 상세는 CHANGELOG)
- [x] VIEWBASE 뷰생성 커널: PhaseA(EXPORTLAYOUT→실2D), B1b(Main+Top+ISO 번들), B1c-Main (2026-07-10)
- [x] 오쏘 다중뷰 엔진 + 신형 UI (3각법 동시추출 실증)
- [x] Preview B 서버사이드 메시 추출 (Support→Explode→Solid3d→SubDMesh)
- [x] UI 셸 마이그레이션 Phase 0, Catalog P4 (WebView 실탭 교체)
