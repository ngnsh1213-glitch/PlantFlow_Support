# 계획서 — 무탭 2D 평면화 스파이크 (cycle 122, 2026-07-23)

- 목표: 무탭 메인뷰를 뷰포트 삭제·2D 그림으로 굽되 **페이퍼 주석 정렬 보존**. 서포트 1개 측정 스파이크(플래그 뒤 격리, 현행 무영향).
- 사용자 방향: Main·멀티 모두 평면화 / 치수는 메인만 / 레이아웃 후속. 백업 태그 `notab-viewport-v1` 존재.
- 성격: **미검증 메커니즘 측정**. 성공 시 메인뷰 확정→멀티뷰 확장. 실패 시 라이브 뷰포트 유지 대안.

## ★핵심 아키텍처 제약 (실측 확정)
- 무탭 상세도는 **side Database(`detailDb`)에서 헤드리스로 조립** 후 `detailDb.SaveAs`(Commands.cs:1962). **활성 문서가 아님.**
- 뷰포트는 detailDb 레이아웃, 솔리드는 detailDb 모델공간(side sourceDb에서 클론).
- 주석 배치 = `NotabProjectWcsToPaper(vp, wcs)`(3318) — 뷰포트 DCS 변환+scale+center로 WCS→paper. **이 변환이 정렬의 단일 원천.**
- **문제**: 기존 FLATSHOT 스파이크(PlantOrthoView.cs:1656)는 `doc.Editor.Command("_.-FLATSHOT"...)` — **활성 문서 editor 명령**. detailDb는 활성이 아니라 **헤드리스에서 FLATSHOT 명령 불가.**

## 메커니즘 — **B 변형 채택** (Codex 자문)
- 활성 문서(현행 원본, 명령 실행 가능)에서 **대상 솔리드만 격리 클론**(기존 스파이크 패턴 재사용: 클론→FLATSHOT→cleanup) →
  임시 뷰를 **상세 뷰포트와 동일 `ViewDirection`·`TwistAngle`·`ViewTarget`**으로 설정 → FLATSHOT으로 2D 블록 생성 →
  2D 점을 **동일 아핀식**(`paperX = vpCenter.X + (dcsX - viewCenter.X)×scale`, NotabProjectWcsToPaper 3336~3339과 동일 공식)으로
  detailDb **페이퍼 공간**에 이식 → 뷰포트 삭제.
- ★정정(자문): FLATSHOT 출력은 **WCS 아님 = 뷰 DCS 평면 2D**. `NotabProjectWcsToPaper`에 직접 투입 금지.
  DCS 축(원점·방향·twist)이 상세 뷰포트와 동일하게 재현되는지가 유일 핵심 리스크.
- 기각: A(SaveAs 후 활성 오픈 — MDI/명령컨텍스트 재설계, 스파이크 범위 초과, 2차 대안) / C(헤드리스 HLR 공개 API 부재, 사실상 렌더러 신작).

## 스파이크 범위 (B 변형)
- 플래그 `PFS_NOTAB_FLATTEN=1`. 기본 0이면 현행(뷰포트 유지) 완전 불변.
- 서포트 1개(RC1 등)만. 기존 스파이크 헬퍼(CreateFlatshotSpikeSolids/SetFlatshotSpikeView/RunFlatshotSpikeCommand/
  FindFlatshotSpikeCreatedIds/Count... + CleanupFlatshotSpike, PlantOrthoView.cs:1495~) 로직 재사용/이식.
- **1차 GO/NO-GO 게이트(자문 지정, 최저비용)**: 임시 클론 뷰=상세 vp의 ViewDirection/TwistAngle/ViewTarget →
  FLATSHOT 블록 2D bbox를 아핀 변환한 값 vs 같은 support/pipe corners의 `NotabProjectWcsToPaper` bbox → **오차 수치 로그**.
  DCS 원점·방향·twist 정합 증명이 관문.
- 게이트 PASS면: 2D를 detailDb 페이퍼로 이식 + 뷰포트 삭제(주석 잔존 확인). FAIL이면 이식 없이 현행 폴백.
- 로그 `PFSNOTABFLATTEN mech=B lines=… flatBbox=… projBbox=… alignErr=… gate=GO|NOGO`.

## 비범위 (스파이크 후)
- 멀티뷰(Top/ISO) 생성, UI 토글, 레이아웃 캘리브레이션. 스파이크 PASS 후 별도 사이클.

## 검증
- dev_test `PFS_NOTAB_FLATTEN=1` + 태그 1개. 로그 3게이트 + 눈확인. 기본 0 회귀 무.
- `dotnet build` 오류 0.
