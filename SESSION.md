# SESSION — 현재 작업 상태

_최종 갱신: 2026-07-16 (세션 이관: N3 치수 정련 중. cycle51 발행됨-미집도. 사용자 全타입 테스트 진행)_

## ★이관 스레드 상태 (2026-07-16, 다음 세션 시작점)
### ① 진행 중 트랙 + 직전 실측
- **트랙 = N3 치수 정련**(핵심 제도는 완성 cycle47~49, 지금은 표기 다듬기). 무탭 엔진 = 서포트 detail 자동생성(와이어프레임 뷰포트+클립+held-pipe BOP선택+페이퍼 비연관 치수). **다중선택 배치 = `PFSNOTABBATCH`(cycle50) 완성**.
- **직전 실측(cycle49 라이브)**: 가로 치수 `split=(350,100) side=bottom`, 세로 176, 배관중심 분할·상/하단 배치 PASS. 설정=글자2.5/화살표4/오프셋7.5/적층6.25.
- **BI 부재규격 규명(이번 세션 핵심)**: 서포트 속성 `BI="BPn+BF"`(BPn 1L/2C/3H/4FB + BF). `BI=17`→BPn1(앵글)+BF7→`SHAPE.py profile[7]={F:75,T:9}`=`HANTEC.DetailProfile("17")="75x75x9"` 일치. 즉 앵글 75×75×9(사용자말 "70"은 근사). 읽기=`PSUtil.GetSupportDimension(id)→SupportParams["BI"]`. 멤버 정의=`PlantFlow_Support\Support\member\MEMBER.py`+`library\SHAPE.py`.

### ② 대기 중 액션 (무엇의 결과를 기다리나)
- **cycle51 발행됨(HANDOFF `f1abe34`), 미집도**. 사용자가 Codex에 `1` 입력 → 집도 → `2`로 나에게 통보 → 내가 리뷰·빌드검증.
- 사용자가 **全 서포트 타입 `PFSNOTABBATCH` 테스트 후, 가로 치수 상/하단을 타입별로 어떻게 원하는지 데이터 제공 예정** → (1) per-type 테이블 채움.

### ③ cycle51 요구 (미집도 내용)
- **(2a) 세로 치수 텍스트=앵글 F높이 `75`**(DetailProfile 첫숫자 파싱, 176=앵글+유볼트+배관 합산이라 무의미→대체).
- **(2b) 별도 지시선(MLeader) 콜아웃 `L-75×75×9`** 부재 옆(=`PSUtil.CreateMLeader`+`EnsureMLeaderStyles` 재사용). 높이(치수)와 규격(콜아웃) 분리.
- **설정**: 글자10/화살표10(Dimasz txt×1.6 분리)/오프셋15/적층15.
- **(1) 가로 위치 타입별 스캐폴드**: `GetNotabHorizontalDimSide(type)` 테이블(GD1·RC1=하단, 미등록=현 배관근접). type=SupportName의 `-`앞 prefix.

### ④ 다음 결정 분기
- cycle51 집도 PASS(세로75+콜아웃+설정+가로스캐폴드) → 사용자 全타입 테스트 → 타입별 가로위치 데이터 수령 → (1) 테이블 완성 → **N3 종결**.
- cycle51 FAIL(콜아웃 위치/MLeader 이슈) → 로그로 앵커/스타일 교정.
- N3 종결 후 → **N4(밸룬/라인번호·BOP 콜아웃/BOM = AnnotateViewport·SPInfo.AttachmentList 재사용)** → N5(3부채 코드 소멸).

### 관련 파일·경로
- 계획: `<appDataDir>\scratch\plan_pfs_notab_n3_20260714.md`(★갱신 섹션), `plan_pfs_notab_viewport_review_20260716.md`.
- 핸드오프: `.plans/HANDOFF.md`(cycle51 ready), `.plans/REPORT.md`(cycle50 done).
- 코드: `PlantFlow_Support/Core/Commands.cs` — 치수=`AppendNotabPaperDimensions`(~4419)/`AppendNotabPaperDimensionEntity`(~4507)/`ApplyNotabPaperDimensionOverrides`(~4519), 투영=`NotabProjectWcsToPaper`, 배치=`PFSNOTABBATCH`. BI=`PSUtil.GetSupportDimension`, `HANTEC.DetailProfile`(Ortho/HANTEC.cs:1894), MLeader=`PSUtil.CreateMLeader`/`OrthoViewportManager.EnsureMLeaderStyles`(662).
- 로그: `C:\Temp\pfs_diag.log`(런마커 `RUN START`). dev: `dev_test.bat`(태그 env `PFS_NOTAB_TEST_TAG`, 없으면 GD1-001; 또는 ACAD서 `PFSNOTABDETAIL`/`PFSNOTABBATCH` 수동선택). env 조정: `PFS_NOTAB_TARGET_FILL`(0.4)/`DIM_TXT`/`DIM_OFFSET`/`CLIP_MARGIN`/`BOP_TOL`/`CONTACT_TOL`/`USE_HIDDEN`.
- ⚠️ 미푸시 커밋 2개(cycle51 handoff). 작업트리에 무관 삭제(D PDF)·untracked 노이즈 있음(스테이징 안 함).

---

_이전 갱신: 2026-07-16 (★N3 치수 핵심 완성: 스케일표준화·투영·치수제도·배관참조. 사용자 全타입 테스트 중. 다음=N4)_

## ★★ N3 무탭 치수 핵심 완성 (2026-07-16, 커밋 cfa1fb7~9576258 = cycle 47~49 + 9893b5b) — 라이브 PASS
페이퍼공간에 서포트 치수를 비연관으로 직접 제도. 사용자가 이제 **全 서포트 타입 테스트 예정**. 다음 트랙 = **N4(밸룬/라인번호·BOP 콜아웃/BOM)**.
- **N3-0 스케일 표준화(cfa1fb7, 9893b5b)**: 동적 피팅→표준배율 라운딩(1:1/2/5/10…) + 주석여백. `PFS_NOTAB_TARGET_FILL`(기본0.4=프레임~여백충분). `vp.CustomScale` 명시(투영 정합). RC1=1:5, GD1=1:2. 사용자 "스케일 과대" 해소.
- **N3-a 투영(cfa1fb7)**: `ViewportProjection`(Ortho, private sealed) 로직 이식 `NotabProjectWcsToPaper`=WCS→paper. 검증: support-paper 중심=vpCenter, 크기=실측×배율. 유효스케일=`vp.Height/vp.ViewHeight`.
- **N3-b/c 치수 제도(dbb87bf)**: `AppendIsoBoundingDimensions` 구조 재사용, 좌표원천=투영 페이퍼. 가로 총폭·pipeCenter 분할·세로 높이, 텍스트=실측mm override. **★크기=페이퍼 고정(Dimtxt 2.5mm, `PFS_NOTAB_DIM_TXT`)** — 기존 ComputeIsoDimensionSize(real/12≈50mm)는 페이퍼 과대라 회피. Title Block 레이아웃 append, `EnsureIsoAnnotationResources`.
- **배관 참조 규칙(9576258)**: (1) 가로 분할=**실제 배관중심 X**(서포트중심 아님. `s_isoPipeCenterWcs`=held-pipe p0 전역 캡처→투영). (2) 가로 치수를 **배관 근접 쪽 상/하단** 배치(pipeCenterY<centerY→하단). 세로는 좌측 유지. 라이브 `split=(350,100) side=bottom` 정답. 분할 경계 가드(한쪽<최소→총폭만). 타입 하드코딩 아닌 기하 규칙.
- **핵심 교훈**: 치수 배치는 타입별 하드코딩 대신 **배관 위치 기하 규칙**(중심 분할·근접 쪽 배치)이 보편. 페이퍼 치수는 뷰포트 스케일 무관 **고정 mm 크기**(Dimscale=1). 비연관=정의점 스냅샷(detailDb frozen). [[pfs-support-held-pipe-bop]]
- **잔여(N4/후속)**: 밸룬·라인번호/BOP 콜아웃·BOM(기존 AnnotateViewport·SPInfo.AttachmentList 재사용). 全타입 테스트서 나오는 엣지(배관 없는 서포트·특이 형상) 대응.

---

_이전 갱신: 2026-07-16 (무탭 뷰포트 품질 트랙 완결: 와이어프레임·클립·held-pipe 선택)_

## ★ 무탭 뷰포트 품질 트랙 완결 (2026-07-16, 커밋 999e1f9~b0e5c71 = cycle 43~46) — 라이브 PASS
추출물 품질 리뷰 3건(비주얼스타일·영역클립·초점)에서 출발해 아래 순차 완결. 다음 트랙 = **N3(치수)**.
- **와이어프레임 기본화(Phase1, 999e1f9)**: 모델영역 Hidden→와이어프레임 기본. Hidden은 opt-in `PFS_NOTAB_USE_HIDDEN=1`. 육안 채택.
- **서포트 영역 클립(cycle43, f5473d8)**: 참조 OrthoGen 3중클립(쿼리박스+앞뒤클립+클립솔리드) 등가 → **oriented 클립박스 Solid3d + Boolean INTERSECT**로 관통 파이프 **길이 트림(10m→서포트깊이)** + 뷰포트 **동적 피팅**(고정1:4 폐기, `ComputeNotabViewportFit`). §9 반영(clone-per-op·empty drop·접촉 선필터).
- **held-pipe 선택(cycle44~46)**: 서포트가 실제 잡는 배관만 포함. cycle44(중심거리)→cycle45(서포트 bbox 수직평면 투영 rect 포함)→**cycle46 최종=BOP 표고 매칭**. 같은 라인 평행 배관(수직200mm)은 rect·중심거리로 못 갈려 오선택 → **서포트 `BOP`(배관 밑면표고)로 판정**: `|(pipeCenterZ−외경반경)−BOP|` 최소. 라이브 `reason=bop bopErr=0.25 pipeCenterZ=467.7 bop=423` 정답 선택. BOP 없으면 기하 최근접 폴백+경고. env `PFS_NOTAB_BOP_TOL`(10mm)/`PFS_NOTAB_CONTACT_TOL`.
- **핵심 교훈**: "서포트가 어느 배관을 잡나"는 기하 중심추측으로 불가(부착위치=새들/윗면이 중심 아님) → **Plant BOP 데이터가 결정적**. [[pfs-notab-engine-go]]
- **⚠️ 미결(N3 입력)**: 동적 피팅 스케일이 현재 **~1:1.4(비표준·과대)** → 서포트가 프레임 87% 차지. 치수선·라인번호/BOP 콜아웃 공간 부족. **N3에서 표준스케일 라운딩+주석 여백 함께 설계**(주석 footprint가 필요여백을 결정).

---

_이전 갱신: 2026-07-16 (무탭 접촉판정 마진 + perspective 리본 flip 가드 완결)_

## ★ 무탭 결함 2건 해결 (2026-07-16, 커밋 73b9974·6e01a9b) — 라이브 PASS
N3 착수 전 라이브 테스트 중 발견된 2건 종결. 다음 트랙 = **N3(치수)** 유지.
- **결함A — 자동포함 이웃 묻어남**: `AutoIncludeRelatedParts` 마진이 `0.5×서포트 max치수` 비례라 큰 서포트에서 과대(1977mm)→파이프 축 따라 이웃 서포트 쓸어담음. **해결=접촉 판정 고정 tol 150mm**(env `PFS_NOTAB_MARGIN`). 라이브 `margin=150 addedPipe=1 addedSupport=1` 확인. 커밋 73b9974.
- **결함B — 추출 후 원본 뷰 perspective flip**: 계측(`PFSPERSPWATCH` 스택)으로 **근본원인=AutoCAD 리본 컨트롤 WPF 바인딩**(`RibbonListButton.set_Current→BindingExpression.UpdateSource→SystemVariable.set_Value`)이 파이프라인 그래픽스 갱신 후 유휴 시점에 `PERSPECTIVE=1` 역기입. **우리 코드/LISP/도면 저장뷰 무관**(계측 `persp enter=0 exit=0`). 리본 내부는 수정 불가 → **스코프 제한 방어 가드**(cycle 42, Codex 커밋 6e01a9b): 파이프라인 진입 시 8초 창 무장→창 내 flip 감지 시 1회 복원+Idle one-shot REGEN+즉시 disarm. 재진입/이중구독/핑퐁 방어. 라이브 PASS(`guard 교정 1→0`+Regen 완료, 육안 parallel 유지).
  - 진단 자산(잔존, 무해): `PFSPERSPPROBE`(즉석 프로브), `PFSPERSPWATCH`(실시간 감시+스택). `pfs_dev_start.scr`에 WATCH/PROBE 연결됨. env `PFS_PERSP_GUARD_SEC`로 창 조정. 커밋 37f7c72·ca80c0f·6ad9e06.
  - ⚠️ 잠재 영향: perspective 잔존 시 VIEWBASE/-VPOINT 오쏘 경로가 어긋날 여지(무탭 추출 자체는 뷰 무관·무영향). 가드로 증상 제거됨.

---

_이전 갱신: 2026-07-14 (무탭 레이아웃·자동포함·자동화 완결, 다음=N3 치수)_

## ★ 무탭 레이아웃/워크플로/자동화 완결 (2026-07-14, 커밋 ~4fcf1b1)
이 세션에서 RECOVER 해결(H5) 이후 아래 전부 라이브 PASS. 다음 트랙 = **N3(치수)**.
- **RECOVER 해결(H5)**: `sourceDb`(side clone DB) 미Dispose가 detailDb.SaveAs 오염 → `CopyCleanNotabSolids` 직후·SaveAs 전 Dispose(ref 시그니처). 포럼 근거. cycle30~36 detailDb 내부 가설(H0~H4) 전부 기각.
- **#1 페이퍼 초기화면**: 헤드리스 TileMode=크래시 → **저장 후 reopen** 방식(`new Database(false,true)`+`TileMode=false`+`SaveAs(...SecurityParameters)`, WorkingDatabase 미변경). 포럼 3종 근거(생성자/SecurityParameters/TileMode). ※빌드DB TileMode·활성레이아웃 전환은 네이티브 크래시.
- **뷰포트 도면영역 정합**: ID 실측 LL(30.5,84.5)~UR(640.5,573.5)=610×489, center(335.5,329). scale 1:4. target=서포트중심(supportExt).
- **서포트만 선택→파이프+유볼트 자동포함**: `AutoIncludeRelatedParts`(서포트 bbox+margin150 교차 Pipe/Support 추가). 유볼트=AcPpDb3dSupport.
- **초기화면 zoom extents**: reopen에서 오버올 뷰포트(활성화前 Number=-1이라 dims로 판별) 뷰를 페이퍼 extents로.
- **★dev 완전 자동화**: dev_test.bat이 로그 초기화 후 런치 → `pfs_dev_start.scr`=SECURELOAD0(보안다이얼로그 제거)+NETLOAD+`PFSNOTABTEST`(SupportName 태그로 서포트 자동선택→추출). Claude가 `C:\Temp\pfs_diag.log` 직접 Read(복붙 불요). [[pfs-dev-loop-tier1]]

### 다음 세션 시작점 (N3 = 치수, 최대 리스크)
- **진행 트랙**: 무탭 엔진. 레이아웃/워크플로/자동화 전부 완결(위). 남은 것 = N3(치수)→N4(밸룬/BOP/BOM)→N5(3부채 코드 소멸).
- **직전 실측**: `PFSNOTABTEST`(GD1-001) 원클릭 라이브 = auto-include(파이프+유볼트)·뷰포트 610×489·zoom·페이퍼열림·RECOVER0·크래시0 전부 PASS. 최신 커밋 613ae6f.
- **대기 액션**: N3 착수 미개시. 계획 초안 `<appDataDir>\scratch\plan_pfs_notab_n3_20260714.md` 존재. 최대 리스크(§9 지목=DCS→PSDCS 매핑 정밀도)라 계획→§9 자문→핸드오프 권장.
- **N3 핵심**: 평면화 2D블록 없이 **뷰포트 투영(DCS→PSDCS 행렬)으로 치수 배치좌표·pipeCenter 계산** → Main에 비연관(non-associative) 치수 직접 제도(가로 폭 분할+세로 높이, 텍스트=실측 mm). 재사용=`s_isoRealWidth/Height`, basis right/up, `PSUtil.CreateHorizontal/VerticalDimension`, B4c/d/e 분할 로직.
- **N3 검증 이점**: PFSNOTABTEST 원클릭 + Claude가 pfs_diag.log 직접 read로 매핑 정밀도 빠른 반복 가능.
- **관련 경로**: 코드=PlantFlow_Support/Core/Commands.cs(RunNotabDetailPipeline/CreateNotabDetailViewport/ConfigureNotabDetailViewport/TryReopenSetNotabPaperSpace/AutoIncludeRelatedParts/PFSNOTABTEST). 계획=plan_pfs_notab_n3_20260714.md·plan_pfs_notab_layout_fit_20260714.md·plan_pfs_notab_recover_20260714.md. 로그=C:\Temp\pfs_diag.log(런마커 `===== RUN START =====`).

---

_이전 갱신: 2026-07-14 (★무탭 엔진 Main 라이브 PASS)_

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
