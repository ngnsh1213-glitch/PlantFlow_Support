# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 122
- **status**: ready
- **issued_at**: 2026-07-23 (rev2 — 집도자 안전지적 반영)
- **title**: 무탭 2D 평면화 스파이크 (FLATSHOT B변형/임시 전용 문서, 플래그 뒤 격리, 서포트 1개 측정)

## 집도자 질의 응답 (2026-07-23)
1. **임시 전용 문서 방식 승인**: 원본 활성 도면 무선택 FLATSHOT의 주변 형상 오염 위험 인정 → **대상 솔리드만 담은 임시 전용 문서를 활성화해 FLATSHOT** 하는 방식으로 변경 승인(격리 보장). 임시 문서 생성·활성·명령완료·닫기·원본 활성 복귀는 집도자 설계. MDI/명령컨텍스트 유예 주의(과거 모드리스 사고 전례) — 동기 활성 전환이 불안정하면 REPORT에 사유 남기고 대안 제시.
2. **허용 오차 확정**: `alignErr`=두 bbox 최대 코너 오차(페이퍼 mm). **GO if alignErr ≤ `PFS_NOTAB_FLATTEN_TOL`(기본 2.0mm), else NO-GO.**
- **작업 경로**: `PlantFlow_Support/Core/Commands.cs` (RunNotabDetailPipeline 흐름 533~/1955~1962 saveAs 직전), 참조·이식 원본 `PlantFlow_Support/Ortho/PlantOrthoView.cs` (FLATSHOT 스파이크 1379~1671)
- **계획서**: `.plans/plan_notab_flatten_spike_20260723.md`
- **기준 커밋**: `b03642e` (백업 태그 `notab-viewport-v1` 존재)
- **자문**: Codex(§9 단일채널). 채택 = B변형(격리 임시 클론 FLATSHOT→2D를 detailDb 페이퍼로 이식). 정정: FLATSHOT 출력=뷰 DCS 2D(WCS 아님)라 NotabProjectWcsToPaper 직접 투입 금지, 임시 뷰를 상세 vp와 동일 ViewDirection/TwistAngle/ViewTarget 설정 후 동일 아핀식으로 변환. A(활성 오픈)=범위 초과 기각, C(헤드리스 HLR API)=부재 기각.

## ⚠ 검증 필수
`dotnet build` 오류 0 확인(빌드·커밋 분리 실행) 없이 커밋 금지. 미실행 시 `status: blocked`. **push 금지**.
플래그 `PFS_NOTAB_FLATTEN` 기본 0 = 현행 완전 불변(회귀 0)이 최우선 안전조건.

## 집도 항목 (스파이크 — 측정 게이트 우선)
1. **플래그**: `PFS_NOTAB_FLATTEN`(기본 0). 1일 때만 아래 동작. 위치 = saveAs 직전(1958~1962), 뷰포트·주석 다 그린 뒤.
2. **격리 FLATSHOT(임시 전용 문서)**: 대상 솔리드(선택 서포트+자동포함 파이프)만 담은 **임시 전용 문서를 생성·활성화**→임시 뷰 설정→`_.-FLATSHOT`(전용 문서엔 대상만 있으므로 무선택도 안전)→2D 블록 캡처→임시 문서 닫기·원본 활성 복귀. PlantOrthoView.cs:1495~1671 헬퍼(클론/뷰설정/명령/수집/cleanup) 재사용/이식.
3. **임시 뷰 = 상세 vp 동일**: detailDb 뷰포트의 `ViewDirection`·`TwistAngle`·`ViewTarget`을 임시 클론 문서 현재 뷰에 그대로 설정.
4. **1차 GO/NO-GO 측정(핵심 산출물)**: FLATSHOT 블록 2D bbox → 아핀 변환(`paperX=vp.CenterPoint.X+(dcsX-vp.ViewCenter.X)*scale`, scale=GetNotabViewportScale, NotabProjectWcsToPaper 3336~3339 동일 공식) → 같은 support/pipe corners의 `NotabProjectWcsToPaper` bbox와 최대 코너 오차 `alignErr`(페이퍼 mm). **GO if alignErr ≤ `PFS_NOTAB_FLATTEN_TOL`(GetEnvDouble 기본 2.0), else NOGO.** 로그 `PFSNOTABFLATTEN mech=B lines=<n> flatBbox=<..> projBbox=<..> alignErr=<..> tol=<..> gate=GO|NOGO`.
5. **이식(게이트 GO일 때만)**: 2D 엔티티를 아핀 변환해 detailDb 페이퍼 공간(레이아웃)에 배치 → 뷰포트 삭제. NOGO면 이식·삭제 없이 현행 뷰포트 유지 폴백(플래그 무색).
6. 빈 catch 금지, 실패 사유 FileDiag.

## 검증 레시피 (dev_test)
- `set PFS_NOTAB_FLATTEN=1` + 태그 1개(RC1-001). 로그: `PFSNOTABFLATTEN … gate=` + `alignErr` 수치. 눈확인: 2D 그림+주석 정렬, 뷰포트 소멸.
- `set PFS_NOTAB_FLATTEN=0`(또는 미설정): RC1 등 현행과 로그·도면 완전 동일(회귀 0).
- 게이트 NOGO여도 정상 종료(폴백)면 스파이크 성공 — alignErr 수치가 다음 판단 근거.
