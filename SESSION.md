# SESSION — 현재 작업 상태

_최종 갱신: 2026-07-14 (★무탭 엔진 Main 라이브 PASS)_

## ★★★ 무탭 엔진 Main — 라이브 PASS (2026-07-14, cycle 30 커밋 5efbf40)
- **판정**: `PFSNOTABDETAIL` 라이브 실행 → `Details\GD1-001_notab.dwg` 열람 확인.
  - Title Block 레이아웃: **A1 프레임 + 타이틀블록 정상**, 뷰포트 안 **파이프 원 + 서포트 직사각형이 2D 은선제거 투영**으로 배치. = 설계 북극성 Main 실물 실증.
  - 모델 공간: 원본 3D 솔리드(소스, 정상). 열기 시 손상/누락 없음.
- **의미**: 문서 0(무탭) 파이프라인으로 VIEWBASE/EXPORTLAYOUT/평면화 **3부채 없이 Main 정투영 생성 라이브 증명**. side-DB(평범 DB) Solid3d + Hidden 뷰포트 방식 확정.
- **RECOVER = ★해결 완료(2026-07-14, cycle 37-H5, 커밋 d7897e5)**: 근인=누수 side Database. `NotabDetailCommand`의 `sourceDb`(side clone)가 `detailDb.SaveAs` 시점까지 미Dispose(finally라 저장 후)→다른 DB 저장 오염=RECOVER. 해결=`CopyCleanNotabSolids` 직후·SaveAs 전 `sourceDb.Dispose()`(ref 시그니처). 라이브 PASS=경고+크래시 없음. 근거=Autodesk 포럼 accepted solution(사용자 발견). cycle 30~36 detailDb 내부 가설(H0~H4) 전부 기각됐던 이유=근인이 외부 누수DB라 keyscan 미검출.
  - ★크래시 주의: 헤드리스 side-DB `TileMode=0`+활성레이아웃(cycle37 D)=네이티브 크래시→원복(cycle 38). 페이퍼 초기화면(#1)은 안전방법 후속.
- **레이아웃 정합 완료(cycle 37 A/B/C, 커밋 c432ae6)**: 뷰포트 fixedRect LL(30.5,84.5)·640.5×573.5·center(350.75,371.25), target=서포트중심(supportExt), scale=1:4.
- **cycle 30 집도 내역**(커밋 5efbf40, `Core/Commands.cs`): A1 hard 좌표 `(0,0)~(841,594)`(`source=A1-hard`), `PlotSettingsValidator` A1 media 적용, 타이틀블록 clone 정규화(normalized=0), audit reflection(사장).
- **다음**: N3(치수 3D→PSDCS 비연관, 최대리스크) → N4(밸룬/BOP/BOM=기존 AnnotateViewport 재사용) → N5(3부채 코드 소멸) + RECOVER polish.

---

_이전 갱신: 2026-07-14 (무탭 엔진 크래시 해결·세션 이관)_

## ★세션 요약 (2026-07-13~14) — 격리 치수 완성 → 별도 도면 → 무탭 엔진
### 1. 격리 치수/중복 (완료, 라이브 PASS)
- **B4d**(f6c99fa): 가로 치수 파이프중심 분할(100/200)+전체(300). **B4c**(f02ee1b): 세로 치수 서포트만. **B4e**(cf44403): 재실행 중복 제거(`PurgePriorIsoDetail`).
### 2. 별도 도면(태그별 .dwg+타이틀블록) — ✅ 라이브 완성
- `CreateIsoDetailDrawing`: side-DB tagDb(템플릿 .dwt)→2D+치수 클론→플로팅 뷰포트(1:2 고정, Layout.Limits 기반)→DRAWING_TITLE 속성(SUPPORT_CODE=GD1-001 등)→`Details\<tag>.dwg`. 원본 무변경. 커밋 6ded95e~c841ad0.
- 태그 소스=서포트 `SupportName`/`Tag`(=GD1-001), PIPE_NO=LineNumberTag, STD=DesignStd. (PFSISOTBLPROBE로 실측 확정)
### 3. ★무탭 엔진 — GO 확정 + 크래시 해결 (이번 세션 핵심)
- **동기**: 기존 파이프라인 3부채=뷰큐브 미표시·EXPORTLAYOUT 다이얼로그·temp dwg 누적. 근본=temp 문서 MDI 활성화 + VIEWBASE/EXPORTLAYOUT 명령.
- **스파이크(C:\Lisp\NoTabSpike)**: ①접근C=side-DB Solid3d+Hidden 뷰포트로 은선제거 정투영 실증(VIEWBASE/EXPORTLAYOUT 불요) ②스파이크D=side-DB에서 Plant `entity.Explode()` 작동(문서없이 Solid3d 추출) → **완전 무탭 GO**. 메모리 [[pfs-notab-engine-go]].
- **엔진 구현**: N1(PFSNOTABN1, 커밋 73861e6)=side-DB 재귀 explode→Solid3d. N2(PFSNOTABDETAIL)=solid+Hidden 뷰포트+타이틀블록→`Details\<tag>_notab.dwg`.
- **★N2 commit 크래시 진단·해결(cycle 22~29)**: 근본원인=**".dwt 템플릿 DB에 은선/3D 뷰포트(ViewDirection/Hidden/ShadePlot) commit"** = 네이티브 크래시. (진단 격리: 템플릿+박스=OK, 템플릿+평면뷰포트=OK, 평범DB+Hidden뷰포트=OK[스파이크C], 오직 템플릿+3D뷰포트만 크래시. Plant solid/비주얼스타일명 무죄.)
  - **해결=§9 #3(커밋 703f1ee)**: 뷰포트를 템플릿이 아닌 **평범 DB(`new Database(true,true)`)** 에 생성 + 템플릿 타이틀블록 2D만 WblockClone 병합 → **크래시 소멸**(commit 완료+saved).
### 대기 중 액션 (다음 세션 시작점)
- **cycle 30(HANDOFF.md 발행됨) 라이브 대기**: A1 레이아웃/플롯 정합(현재 평범DB 기본 12×9라 타이틀블록 A1좌표와 미스매치) + 저장 무결성("run RECOVER" 경고 제거, Audit+dangling 의존 교정). Codex 집도(`1`)→빌드→`PFSNOTABDETAIL` 라이브.
### 다음 결정 분기
- cycle 30 PASS(A1 정합+경고소멸) → **무탭 Main 완성**(3부채 소멸) → **N3(치수 재투영: 3D→PSDCS 비연관 치수, 최대리스크)** → N4(밸룬/BOP/PLN 콜아웃/BOM=기존 AnnotateViewport·SPInfo.AttachmentList 재사용) → N5(정리·전환: VIEWBASE/EXPORTLAYOUT/temp 제거).
- cycle 30 FAIL → 로그로 A1/저장 원인 재판정.
### 관련 경로
- 계획: `<appDataDir>\scratch\plan_pfs_notab_engine_20260713.md`·`plan_pfs_notab_spike_20260713.md`·`plan_pfs_iso_separate_drawing_20260713.md`·`plan_pfs_iso_annotation_integration_20260713.md`.
- 핸드오프: `.plans/HANDOFF.md`(cycle 30 ready). 코드: PlantFlow_Support/Core/Commands.cs (PFSNOTABDETAIL/N1/CreateNotabDetailDrawing/CopyCleanNotabSolids/CloneTemplateTitleBlock2D).
- 스파이크: C:\Lisp\NoTabSpike (PFSNOTABSPIKE/PFSNOTABEXPLODE). 진단 커맨드(제품): PFSNOTABBOXTEST/BOXVPTEST/BOXVPWIRE(격리 완료, 잔존 무해).
### ⚠️ 이관 주의
- 커밋 규율: Codex가 git commit escalation 거부 다발 → **Claude가 대리 커밋** 중(코드 검증 후). 현 HEAD=703f1ee(+cycle30 대기).
- 밸룬 멤버 소스=`SPInfo.AttachmentList`(SupportType"ATTACHMENT" 부착서포트, PaletteTab.Events.cs:171-319). 격리 선택에 부착서포트 포함+SPInfo 채우기 필요(N4).
- 밸룬/BOP콜아웃/BOM은 기존 `OrthoViewportManager.AnnotateViewport` 엔진 재사용(새로 안 만듦).
- (이전) **격리 방향 제어 — ✅ B3a PASS (2026-07-12)**.
  - 파이프라인: 선택셋 → 1st temp(Plant clone) → explode Solid3d → 2nd temp(순수 solid) → **-VPOINT(파이프축)** → SendStringToExecute VIEWBASE `_O _Current` → EXPORTLAYOUT 평면화 → 원본 clone-back(PFS_ISO_DETAIL 레이어).
  - **방향 제어(B3a)**: -VPOINT(파이프축)+VIEWBASE `_O _Current`로 Main뷰(파이프=원+서포트=직사각형) 생성. X/Y/수직Z 전 축 + 다중 타입 육안 확정. "축방향 응시=Main" 보편 적용.
  - 커밋: e7ffa14(B1g), 7a7653d(B2a), cc6602e(B2b), cbe738c(B3a).
  - 후속(별도 트랙): 스케일 보정(viewport 1:100→실크기), 치수(평면화 2D 후처리 Main만), 배치 오프셋, -0 표기 정규화(경미), EXPORTLAYOUT open-prompt 억제(세션당 1회, 낮음).
- (이전) **격리 VIEWBASE 커널 — ✅ B1g PASS**, **B2b clone-back PASS**.
  - 목표 출력: 4뷰(DOWN/WEST=Main/SOUTH/ISO) + 치수는 Main에만 + BOM표 + 밸룬 + 위치콜아웃 + 타이틀블록 (caetech.vn 사양).
  - 설계철학: 뷰 = Pipe Axis 기준. Main = 서포트 길이방향(탑 제외), viewDir∥파이프 → 서포트 직사각형 + 파이프 원. 치수는 Main에만.

## 직전 실측 사실 (2026-07-12) — 격리 VIEWBASE 커널 완성
- **B1g ✅ PASS**: 격리 파이프라인 3회 결정적 재현. `PFSVBISODONE newEntities=4`(base+projected), eInvalidInput 소멸.
- 전 파이프라인: 선택셋 → 1st temp WblockClone(Plant 지오 보존, proxy=0) → explode Solid3d id수집 → 2nd temp WblockClone(순수 solid만) → Idle 게이트(active/CMDNAMES) → **SendStringToExecute VIEWBASE _E** → sentinel(PFSVBISODONE) 완료판정 → Idle close.
- ★핵심 교훈: VIEWBASE(무거운 model-doc UI 명령)는 갓 Open한 문서서 `ed.Command`(직접호출) 불가 → **`SendStringToExecute`(명령 큐=타이핑 등가) + sentinel 명령 체이닝**으로 구동/완료판정. B1b~B1f 실패가 이 하나로 수렴.
- Plant 객체는 caller가 Erase 불가(eCannotBeErasedByCaller) → 순수 solid만 2nd temp에 clone(M3). transient solid 직접 cross-DB append 금지 → WblockCloneObjects만.
- 상세: `plan_pfs_isolation_20260711.md`(§4-A~P), 메모리 `pfs-wblockclone-preserves-plant-geometry`.

## 진행 중 (2026-07-13 최신) — 무탭 엔진
- **별도 도면(태그별 .dwg+타이틀블록) ✅ 라이브 완성**: side-DB tagDb(템플릿 .dwt)→2D+치수 클론→플로팅 뷰포트(1:2 고정)→DRAWING_TITLE 속성→Details\<tag>.dwg. 커밋 6ded95e~c841ad0. 원본 무변경.
- **★무탭 엔진 GO (스파이크 실증)**: side-DB Solid3d+Hidden 뷰포트로 VIEWBASE/EXPORTLAYOUT 없이 은선제거 정투영 성공. 상세=메모리 [[pfs-notab-engine-go]]. 스파이크=C:\Lisp\NoTabSpike.
- **대기 중 액션**: 스파이크 D(side-DB Plant explode) 라이브 실행 → 완전vs부분 무탭 확정.
- **다음 결정 분기**: D 성공(Solid3d 산출)→완전 무탭 엔진 계획 / D 실패→부분 무탭(1차 explode 문서 1회만). 이후 밸룬+BOP/PLN 콜아웃+BOM(기존 AnnotateViewport 재사용) / 치수 3D→PSDCS 재설계.
- **백로그(TODO)**: 주석엔진 통합(밸룬/BOP/BOM), 뷰포트-타이틀블록 정렬(미세조정), 무탭 3부채(뷰큐브/다이얼로그/temp누적—무탭이면 소멸).

## 관련 파일·경로
- 잔여 백로그: `TODO.md`
- 변경 이력: `CHANGELOG.md`
- 상세 설계철학·목표사양은 프로젝트 메모리(`project_pfs_design_philosophy_core`, `project_pfs_target_output_spec_caetech`) 참조.
