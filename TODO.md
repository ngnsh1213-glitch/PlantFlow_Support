# TODO — 잔여 작업 백로그

_완료 항목은 여기서 지우고 `CHANGELOG.md`로 옮긴다._

## 예약 — RC 트랙 종료 후 착수 (2026-07-20 사용자 확정)
- [ ] **`HANTEC` → `StandardSupport` 이름 변경 (방식 (가): 이름만, 내용·구조 불변)**
  - 목적: 벤더(회사)명이 코드 전면에 드러나는 것 제거. 대외 공개·제품화 대비.
  - 범위: 클래스명 `HANTEC`, 파일명 `HANTEC.cs`, 내부 변수/메서드/주석. 총 181곳 중 리터럴 제외분.
  - **★절대 바꾸지 말 것 — 문자열 리터럴 `"HANTEC"`**: Plant3D 카탈로그에 실제 저장된
    DesignStd 값이라 외부 데이터다. 바꾸면 조회가 전부 깨진다.
    `BOMs.cs:45`(`design_std == "HANTEC"`), `Commands.cs:7902/7925`(`ContentsByDesignStd("HANTEC")`),
    `OrthoViewportManager.cs:787`(`CPYDesignStd == "HANTEC"`).
  - 결과 의미: "표준 서포트 주석 엔진"이 "DesignStd=HANTEC인 데이터"를 다룬다 — 관계가 이름에 드러남.
  - `PlantFlow_Support_Backup_Stable/`은 백업 폴더이므로 대상 제외.
  - **추상화(인터페이스+구현체 분리)는 하지 않는다** — 현재 HANTEC 외 표준 요구가 없다.
    다른 벤더 표준이 실제로 생기면 그때 (나)안으로 재검토.
  - **착수 시점**: RC 트랙(cycle 91~) 한 단락 후. 지금은 `HANTEC.cs`/`Commands.cs`를
    계속 수정 중이라 대규모 rename이 충돌한다.

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
