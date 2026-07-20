# TODO — 잔여 작업 백로그

_완료 항목은 여기서 지우고 `CHANGELOG.md`로 옮긴다._

## ★무탭 엔진 (현재 트랙, 2026-07-14)
- [x] 무탭 Main 라이브 PASS (은선 정투영, cycle 30)
- [x] RECOVER 해결 (H5=sourceDb 조기 Dispose, cycle 37, 커밋 d7897e5)
- [x] 진단 코드 cleanup (cycle 39): pnp-purge/plotter-normalize/keyscan/wblock-fallback 제거, 직접 SaveAs 원복
- [x] #1 페이퍼 초기화면: reopen(new Database(false,true)+TileMode=false+SaveAs(...SecurityParameters)). Title Block 페이퍼로 열림. 포럼 3종 근거
- [x] 뷰포트 도면영역 정합: ID 실측 LL(30.5,84.5)~UR(640.5,573.5)=610x489, center(335.5,329), scale 1:4, target 서포트중심. 육안 PASS
- [x] 서포트만 선택시 파이프+유볼트 자동포함: AutoIncludeRelatedParts(서포트 bbox+margin150 교차 Pipe/Support). 유볼트=AcPpDb3dSupport. PASS
- [x] 초기화면 zoom extents: reopen 오버올 뷰포트(활성화前 Number=-1이라 dims 판별) 뷰를 페이퍼 extents로. PASS
- [x] dev 완전 자동화: dev_test.bat=SECURELOAD0+NETLOAD+PFSNOTABTEST(태그 자동선택), Claude가 C:\Temp\pfs_diag.log 직접 read
- [x] 무탭 자동포함 접촉판정(마진 크기비례→고정150, 73b9974). 이웃 서포트 배제
- [x] 무탭 perspective flip 방어 가드(리본 바인딩發, cycle42 6e01a9b). 계측=PFSPERSPWATCH+스택
- [x] 무탭 뷰포트 와이어프레임 기본화(Phase1, 999e1f9). Hidden=PFS_NOTAB_USE_HIDDEN opt-in
- [x] 무탭 서포트 영역 클립+동적피팅(cycle43, f5473d8): Solid3d Boolean로 관통 파이프 길이 트림+콘텐츠 피팅
- [x] 무탭 held-pipe 선택=서포트 BOP 표고 매칭(cycle44~46, b0e5c71): 같은 라인 평행 배관 중 잡는 배관만. 라이브 PASS
- [x] 무탭 스케일 표준화(cycle47, cfa1fb7): 동적→표준배율 라운딩+주석여백. PFS_NOTAB_TARGET_FILL 0.4
- [x] **N3 치수 핵심(cycle47~49)**: 투영(WCS→paper)+가로총폭/pipeCenter분할/세로 제도+배관참조(분할=실배관중심X, 상/하단=배관근접). 라이브 PASS. 계획=plan_pfs_notab_n3_20260714.md
- [ ] **N3 全타입 테스트(사용자 진행 중)**: 모든 서포트 타입서 치수/스케일/배관참조 검증. 엣지(배관없는 서포트·특이형상) 대응
- [ ] **[별도 트랙] 무탭 2D 평면화 결정**: 뷰포트 유지 vs 진짜 2D 선요소(클립된 solid에 FLATSHOT). 재검토
- [ ] **N4(밸룬/라인번호·BOP 콜아웃/BOM)**: 기존 AnnotateViewport·SPInfo.AttachmentList 재사용 -> N5(3부채 코드 소멸)

## 서포트 생성 — "세트(SET)" 개념 도입 (신규, 2026-07-20)
- [ ] **GD1 생성 시 UBOLT 동반 자동배치**: 사용자가 GD1만 눌러도 UBOLT가 같이 생성되도록 PFS 생성 명령에 세트 개념 도입. 두 객체는 각각 **독립 Plant 파트로 유지**(BOM/태그 정상)하되 부모 태그(`GD1-001`)로 그룹핑 → 무탭 추출 시 한 세트로 취급. 위치·사이즈 산출은 기존 held-pipe BOP 매칭 로직 재사용.
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

## 미해결 / 후속
- [ ] test1 뷰 뒤집힘 (content-rotation) 해소
- [ ] Main 탑뷰 배제 (B3)
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
