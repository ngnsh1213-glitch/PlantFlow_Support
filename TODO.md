# TODO — 잔여 작업 백로그

_완료 항목은 여기서 지우고 `CHANGELOG.md`로 옮긴다._

## ★무탭 엔진 (현재 트랙, 2026-07-14)
- [x] 밸룬·콜아웃 배치 종결(cycle97~102, `947663e`). 라이브 3회 skip 0건, 사용자 판정 이상없음 → CHANGELOG
- [x] 라인넘버 콜아웃만 꺾임 1회 리더로 복원(`152dc46`). 유볼트 직선 유지, skip 0건 → CHANGELOG
- [x] **무탭 全타입 회귀 확인 완료**(cycle105~121, 2026-07-23): **GD1~3+RC1~9+RS1~15(변형 18종) 전 타입 통과** → CHANGELOG.
      결과=①F1/F2 매핑은 타입별 상이(원본 .py 대조 필수 — RS12A/B/D 가로/세로 역배정 2회 근인) ②타입별 config 행이 설계 관용구
      ③RC4 눈확인 완료 ④신규 통로=양쪽 세로치수(dimVL/VR)·V-SPLIT(세로 파이프분할)·합성키/@formulaA·memberRight 앵커·스파인 밸룬 옵트인.
- [x] **격리 결함 + 부속 가드**(cycle121, `57e0d20`+`f6c3c4a`): 인접 본체 서포트 혼입 차단(부속 코드 단일원천), 부속(UB) 단독추출 거부 → CHANGELOG.
- [x] **무탭 UI 통합·일괄선택**(cycle104, `bb020e5`+MDI가드 `3a0d2ce`): Drawing 팔레트 구오쏘→무탭, 일괄 선택 추가, 로딩 오버레이. 라이브 PASS → CHANGELOG.
- [ ] **미검증 잔여 타입**: TR/TRS(트런니언)·FS(필드)·SHOE. 같은 루프(타입별 4관찰+캘리브레이션)로.
- [x] 무탭 Main 라이브 PASS (은선 정투영, cycle 30)
- [x] RECOVER 해결 (H5=sourceDb 조기 Dispose, cycle 37, 커밋 d7897e5)
- [x] 진단 코드 cleanup (cycle 39): pnp-purge/plotter-normalize/keyscan/wblock-fallback 제거, 직접 SaveAs 원복
- [x] #1 페이퍼 초기화면: reopen(new Database(false,true)+TileMode=false+SaveAs(...SecurityParameters)). Title Block 페이퍼로 열림. 포럼 3종 근거
- [x] 뷰포트 도면영역 정합: ID 실측 LL(30.5,84.5)~UR(640.5,573.5)=610x489, center(335.5,329), scale 1:4, target 서포트중심. 육안 PASS
- [x] 서포트만 선택시 파이프+유볼트 자동포함: AutoIncludeRelatedParts(서포트 bbox+margin150 교차 Pipe/Support). 유볼트=AcPpDb3dSupport. PASS
- [x] 초기화면 zoom extents: reopen 오버올 뷰포트(활성화前 Number=-1이라 dims 판별) 뷰를 페이퍼 extents로. PASS
- [x] dev 완전 자동화: dev_test.bat=SECURELOAD0+NETLOAD+PFSNOTABTEST(태그 자동선택), Claude가 C:\Temp\pfs_diag.log 직접 read
- [x] 무탭 자동포함 접촉판정(마진 크기비례→고정150, 73b9974). 이웃 서포트 배제
- [x] 무탭 perspective flip 방어 가드(리본 바인딩發). cycle42 스코프가드(6e01a9b) → **cycle103 발원 기반 재설계(`83d0a3e`)로 종결**:
      어셈블리(AdWindows/Autodesk.Windows) 3분류로 strong-ribbon만 복원, 자폭 제거, 백스톱 60초+generation/문서 스코프,
      VIEWCUBEACTION 미개입. 라이브 PASS(2026-07-22, flip→교정 15ms). 계측=PFSPERSPWATCH+스택 → CHANGELOG
- [x] 무탭 뷰포트 와이어프레임 기본화(Phase1, 999e1f9). Hidden=PFS_NOTAB_USE_HIDDEN opt-in
- [x] 무탭 서포트 영역 클립+동적피팅(cycle43, f5473d8): Solid3d Boolean로 관통 파이프 길이 트림+콘텐츠 피팅
- [x] 무탭 held-pipe 선택=서포트 BOP 표고 매칭(cycle44~46, b0e5c71): 같은 라인 평행 배관 중 잡는 배관만. 라이브 PASS
- [x] 무탭 스케일 표준화(cycle47, cfa1fb7): 동적→표준배율 라운딩+주석여백. PFS_NOTAB_TARGET_FILL 0.4
- [x] **N3 치수 핵심(cycle47~49)**: 투영(WCS→paper)+가로총폭/pipeCenter분할/세로 제도+배관참조(분할=실배관중심X, 상/하단=배관근접). 라이브 PASS. 계획=plan_pfs_notab_n3_20260714.md
- [x] **무탭 2D 평면화 결정**(2026-07-23 사용자 확정): 뷰포트 유지 아님 → **EXPORTLAYOUT 왕복으로 2D 굽기**(뷰포트 삭제, 그림+페이퍼 주석만 잔존). **Main·멀티 두 경로 모두 평면화(통일)**. → 아래 멀티뷰 트랙으로 흡수.
- [x] **N4(밸룬/라인번호·BOP 콜아웃/BOM)** 완료(cycle95~102). 단 "기존 AnnotateViewport 재사용"은 **기각**됐다 —
      오쏘에는 부재 밸룬(`F*`) 분기가 애초에 없어 이식이 불가했고 신규 설계로 진행했다. → CHANGELOG
      _(N3 全타입 테스트 항목은 위 8행 "타 타입 회귀 확인"과 동일 작업이라 통합)_

## 💡 아이디어 풀 (미확정, 착수 전 취사선택 — 2026-07-20 수집)
_확정 트랙이 아니다. 사양이 아니라 후보다. 구현 착수 시점에 걸러내고, 채택된 것만 위 트랙으로 승격한다. 기각 시 지우지 말고 기각 사유를 한 줄 남긴다._

- [ ] **서포트 생성 "세트(SET)" — GD1 생성 시 UBOLT 동반 자동배치**: 사용자가 GD1만 눌러도 UBOLT가 같이 생성되도록 PFS 생성 명령에 세트 개념 도입. 두 객체는 각각 **독립 Plant 파트로 유지**(BOM/태그 정상)하되 부모 태그(`GD1-001`)로 그룹핑 → 무탭 추출 시 한 세트로 취급. 위치·사이즈 산출은 기존 held-pipe BOP 매칭 로직 재사용.
  - 선결 규칙 4건: ①타입별 UBOLT 동반 여부(GD1=예, RS 계열 등은?) ②파이프 사이즈→UBOLT 규격 매핑표 ③개수(1/2개) ④UBOLT 제외 옵션(해제 수단).
  - 대안 검토됨: Python 콘텐츠 스크립트 내 동반작도(BOM 개별행 불가로 부적합), .acat Assembly 등록(정식이나 .acat+.pspc 이중관리 비용 큼).
  - 착수 시점: 별도 트랙 아님. 관련 생성/주석 트랙 집도 시 **함께 구현**.

- [ ] **서포트 다중선택 → 개별 도면 일괄 추출**: 사용자가 영역/다중 선택으로 여러 객체를 잡아도 **서포트 본체만 필터링**(유볼트·슈·TR 등 부속 제외)하여, 각 서포트마다 **개별 도면**으로 추출. 부속은 세트 규칙에 따라 해당 서포트 도면에 자동 포함(기존 AutoIncludeRelatedParts 재사용).
  - 쟁점: 서포트 본체 vs 부속 판별 기준(SupportType/RowClassName), 추출 순서·태그 정렬, 도면 파일 분리 vs 다중 레이아웃, 실패 1건이 전체를 중단시키지 않도록 건별 예외 처리.

- [ ] **[신규 탭] 서포트 생성 탭 (그림 클릭 → 서포트 생성)**: PFS UI에 "서포트 생성" 탭을 추가하고, 타입별 서포트 **그림(썸네일)을 클릭하면 해당 서포트가 모델에 생성**되도록 한다. 목표=사용자가 Plant3D **SPEC 뷰어를 거치지 않고 PFS 안에서** 서포트를 생성.
  - 구성: 카탈로그(.acat) 기반 타입 목록 → 썸네일 그리드 → 클릭 시 배치 명령 호출(파이프 선택→BOP/사이즈 자동 산출). 세트 규칙(UBOLT 동반) 연동.
  - 쟁점: 썸네일 이미지 원천(규격집 PDF 발췌 vs 렌더 생성), 카탈로그 타입 목록 동적 로딩, WebView(app.html) 탭 라우팅 추가, 배치 시 명령 컨텍스트(모드리스→ExecuteInCommandContextAsync) 유예 이슈 재발 방지.

- [ ] **PFO "대기 중" 화면 PFS 이식 + UI 전반 개선**: PFO의 대기(로딩/진행) 오버레이 화면을 PFS에 그대로 이식해 장시간 작업(추출·생성·BOM) 중 무반응처럼 보이는 구간을 없앤다. 원천=PFO 프런트(`D:\PlantFlow\PlantFlow_Ortho\frontend`).
  - 함께: PFS UI 전반 개선(탭 일관성, 버튼/폰트/여백 정리, 진행률·에러 표시 표준화). 기존 app.html 셸 4탭 구조 위에서 진행.

- [ ] **생성 시 태그 선부여 + 자동 채번**: 생성 탭에 태그 시작번호 입력란을 두고, 사용자가 `0001` 입력 후 GD1을 생성하면 `GD1-0001`, 다음 클릭은 `GD1-0002`로 자동 증가. 생성 후 수동 부여가 아니라 **배치 트랜잭션 안에서 선부여**. 기반=기존 `PlantAutoCoding.cs` / Tagging&Coding 탭 로직 재사용.
  - 규칙: ①카운터는 **타입별 독립**(GD1과 RS 각각) ②자릿수=입력 문자열 길이 유지(`0001`→4자리 zero-pad) ③이미 사용된 번호는 건너뛰고 다음 빈 번호 ④시작값 미입력 시 기존 최대값+1 이어받기.
  - 쟁점: 채번 조회는 **프로젝트 DB 전역**이어야 함(현재 도면만 보면 도면 간 중복). 동시 작업자 경합 시 부여 직후 재조회·충돌 재시도. 부속(UBOLT) 태그는 부모 상속(`GD1-0001-UB`) vs 독립 채번 — 세트 항목과 함께 결정.

- [ ] **라인 단위 일괄 추출 (Connected Line Number 선택)**: Plant3D 우클릭 메뉴의 `Add to Selection > Connected Line Number`를 서포트 선택 기능에 도입. 사용자가 이 버튼을 누르고 **배관 하나만 클릭**하면 해당 라인에 연결된 모든 아이템이 선택되고, 그중 **연결된 서포트만 각각 개별 도면으로 추출**한다. 유볼트·슈 등 지정 부속은 추출 대상에서 제외(해당 서포트 도면에는 포함).
  - 위 "다중선택 → 개별 도면 일괄 추출"과 **동일한 필터·추출 파이프라인**을 공유. 차이는 선택 수집 방식(수동 다중선택 vs 라인 그래프 순회)뿐 → 필터/추출을 먼저 만들고 선택 수집기를 갈아끼우는 구조로.
  - 쟁점: Connected Line Number를 API로 재현(포트 그래프 순회 = P5 토폴로지 식별 자산 재사용) vs 기존 Plant 명령 호출. 제외 대상 아이템 목록의 **설정화**(하드코딩 금지). 대형 라인에서 서포트 수십 개 → 진행률 표시·중단 수단 필요.

- [ ] **서포트 타입 단위 일괄 추출**: 타입(GD1/GD2/RS/FS…)을 지정하면 프로젝트/도면 범위 내 **해당 타입 서포트 전량을 개별 도면으로 일괄 추출**. 위 두 항목(다중선택·라인 단위)과 같은 필터·추출 파이프라인을 공유하며, **선택 수집기만 "타입 질의"로 교체**하는 세 번째 수집 방식.
  - 쟁점: 범위 지정(현재 도면 vs 프로젝트 전역), 동일 타입 내 사이즈·형상 변형이 다르면 도면도 각각 나와야 하는지 vs 대표 1장, 태그 순 정렬, 수량이 크므로 진행률·중단·부분 실패 허용 필수.

- [ ] **서포트 생성 후 "Viewer Open" → 나비스웍스 관측점 이동**: PFS에서 서포트 생성 후 사용자가 "Viewer Open"을 누르면 나비스웍스가 열리며 **클릭한 서포트 위치로 뷰포인트가 자동 이동**한다.
  - 전제 조건: `<프로젝트 폴더>\Plant 3D Models` 폴더에 각 모델(.dwg)과 **동일 이름의 `.nwc`**가 존재해야 하고, 그 nwc들을 합친 **`.nwf`**로 이동한다. nwf를 열고 클릭한 서포트로 뷰포인트를 이동시킨다.
  - 기반=[[navisworks-plantflow-integration-idea]] NavisHidePick 플러그인 파이프라인 검증 완료(C:\Lisp\NavisHidePick). 후보 ②(도면↔모델 관측점 왕복)의 구체화.
  - 쟁점: ①nwc/nwf 존재·최신 여부 확인(없으면 재변환 안내) ②PFS↔나비스웍스 프로세스 간 통신(파일 기반 관측점 vs Automation API vs COM) ③서포트 좌표→나비스웍스 카메라 관측점 변환 ④객체 선택·하이라이트까지 할지 카메라 이동만 할지 ⑤나비스웍스 미실행 시 자동 기동.

## ★ 멀티뷰 + 평면화 트랙 (신규, 2026-07-23 방향 확정)
_사용자 확정 4건: ①Main·멀티 모두 뷰포트 삭제·평면화(통일) ②멀티=메인+탑+등각 ③치수·주석은 메인에만(설계철학 유지) ④레이아웃은 나중에 캘리브레이션._
- [ ] **현재 무탭(뷰포트 잔존 버전) 백업**: 메인 경로도 평면화로 바뀌므로 현행을 git tag로 고정(A/B 비교·회귀 기준). 테스트 후 메인뷰 확정.
- [x] **평면화 코어**: BRep 에지 투영으로 2D Polyline화·뷰포트 제거(cycle126), 카테고리 블럭화 Phase 1 구현(cycle127 `5c9cb25`, 라이브 검증 대기).
- [ ] **멀티뷰 생성**: 메인+탑+등각 3뷰포트(뷰 방향 3종, GetNotabViewBasis 확장). 메인에만 주석. Top/ISO=그림만.
- [ ] **UI 토글**: Drawing 팔레트에 Main/멀티뷰 선택. 멀티 선택 시에만 멀티뷰 생성 동작.
- [ ] **레이아웃 캘리브레이션**: 3뷰 사각형 배치(메인 크게+탑/등각)를 env 노브로 심어 눈으로 확정 후 규칙 번역.

## 미해결 / 후속
- [ ] test1 뷰 뒤집힘 (content-rotation) 해소
- [ ] ~~Main 탑뷰 배제 (B3)~~ → 위 멀티뷰 트랙으로 흡수
- [ ] 경사(slope) 후속 처리

## 격리 별도 도면 — 다음 트랙(N4로 흡수)
- [ ] **주석 엔진 통합(밸룬+BOP/PLN 콜아웃+BOM)**: 새로 만들지 말고 기존 `OrthoViewportManager.AnnotateViewport` 재사용. 밸룬 소스=`SPInfo.AttachmentList`(멤버=SupportType"ATTACHMENT" 부착 서포트, PaletteTab.Events.cs:171-319에서 분석·R1/R2 명명). 콜아웃=`CreateMLeader(MText=PipeLineNo/"B.O.P+"+BOP)`. BOM=`BOMs(SPInfo.Ids[0], AttachmentList)`. 3축: 격리 선택/분석 확장 / SPInfo 채우기 / AnnotateViewport를 side-DB에 대응.

## (구) VIEWBASE 오쏘 파이프라인 — 무탭으로 대체 진행 중
- [ ] 뷰큐브 미표시 / EXPORTLAYOUT 다이얼로그: 무탭 엔진이 3부채 자체를 소멸시키므로 N5에서 정리

## 완료 (요약, 상세는 CHANGELOG)
- [x] VIEWBASE 뷰생성 커널: PhaseA(EXPORTLAYOUT→실2D), B1b(Main+Top+ISO 번들), B1c-Main (2026-07-10)
- [x] 오쏘 다중뷰 엔진 + 신형 UI (3각법 동시추출 실증)
- [x] Preview B 서버사이드 메시 추출 (Support→Explode→Solid3d→SubDMesh)
- [x] UI 셸 마이그레이션 Phase 0, Catalog P4 (WebView 실탭 교체)
