# TODO — 잔여 작업 백로그

_완료 항목은 여기서 지우고 `CHANGELOG.md`로 옮긴다._

## 예약 — cycle 91 집도 직후 착수 (2026-07-20 사용자 확정)

### 제품 맥락 (판단 근거, 2026-07-20)
- Python 스크립트(`D:\AutoCAD Plant 3D 2026 Content\CPak Common\CustomScripts\Support`) + PFS를
  **한 패키지로 판매**한다.
- 기본 카탈로그·스탠다드 = HANTEC 규격집을 **이름만 바꿔 `PIPE SUPPORT STANDARD`로 제공**.
- 향후 기업/그룹 요청 시 **회사명으로 표준을 추가**한다 → **다중 표준이 확정된 요구**다.
- **★출하 카탈로그 `DesignStd` 값 = `PFS STANDARD`** (사용자 확정).

- [ ] **`HANTEC` → `StandardSupport` 이름 변경 + DesignStd 외부화**
  - 목적: 벤더(회사)명 노출 제거 + 다중 표준 대비. 늦을수록 분기점이 늘어 작업량 증가.
  - **범위 1 — 이름**: 클래스명 `HANTEC`, 파일명 `HANTEC.cs`, 내부 변수/메서드/주석.
  - **범위 2 — DesignStd 하드코딩 제거 (★핵심)**: 현재 한 값과 직접 비교한다.
    `BOMs.cs:45`(`design_std == "HANTEC"`), `Commands.cs:7902/7925`(`ContentsByDesignStd("HANTEC")`),
    `OrthoViewportManager.cs:787`(`CPYDesignStd == "HANTEC"`).
    → **알려진 표준 목록/설정 조회**로 바꾼다. 고객사 표준은 목록에 추가만 하면 되게.
  - **★하위 호환 필수**: 현재 테스트 중인 GS칼텍스 프로젝트 모델이 `DesignStd = HANTEC`이다
    (라이브 로그에서 `ContentsByDesignStd("HANTEC")`가 실제 BOM 행 반환 확인).
    `PFS STANDARD`만 인식하게 바꾸면 **현 테스트 모델이 즉시 깨진다. 둘 다 인식할 것.**
  - **실패 양상 주의**: DesignStd 불일치는 빌드 통과 + 런타임에 BOM이 조용히 비는 형태라
    발견이 늦다. 인식된 DesignStd 값을 **로그로 남길 것**.
  - **범위 밖 — 타입 메서드 구조 분리는 하지 않는다**: `RC1()`/`GD1()` 등은 그대로.
    표준별 구현 분리(인터페이스+구현체)는 **두 번째 표준이 실제로 올 때**.
    요구를 모르는 상태로 인터페이스를 먼저 깎지 않는다.
  - `PlantFlow_Support_Backup_Stable/`은 백업 폴더이므로 대상 제외.
  - **착수 시점**: cycle 91은 계측(로그 추가) 중심이라 `HANTEC.cs` 본문을 거의 안 건드린다.
    **cycle 91 집도 종료 즉시** 착수하면 충돌 없고 지연도 최소.

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
