# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 113
- **status**: ready
- **issued_at**: 2026-07-22
- **title**: vertical-member 리더 검사 전 tier 제외 (cycle112 문구 정정)
- **작업 경로**: `Core/NotabCalloutPlacer.cs` (Free/extLeader 검사부)
- **진단 원장**: `.plans/notab_rc5_9_review_20260722.md`
- **기준 커밋**: `03f30c2`
- **자문**: 불요 — cycle112 로그로 원인 확정(발행자 문구 오류 정정).

## ⚠ 검증 필수
`dotnet build` 오류 0(경고 14 초과 금지). 미실행 시 커밋 금지·`status: blocked`. Claude가 REPORT 수령 후 직접 빌드 재확인.

## 배경 (cycle112 라이브 로그, 재측정 불요)
- cycle112 후에도 콜아웃 위치 **cycle111과 동일**: `elbow=(379.5,424.45) fan=90 cost=135.95`, **`extLeaderBy{vertical-member=538}` 그대로**.
- 원인: cycle112는 vertical-member 리더 교차를 "tier 1·2에서 허용"(일반 장애물化)으로 구현. 그러나 **tier 0(전체 검사)이 위쪽 자리(cost 135.95)를 먼저 성공**시키므로 tier 1/2는 실행되지 않음. → 사용자 승인("리더는 기둥 가로지르게 허용")이 반영 안 됨.
- F2는 해결됨(clear 40.8 복귀·leaderArrow 4.5) — **건드리지 말 것**.

## 집도 지시 (한 가지)

### vertical-member 장애물을 리더 교차 검사에서 **모든 tier에서 제외**
- `NotabCalloutPlacer`의 리더 교차 검사(`Free`의 extLeader/calloutLeader 경로)에서 **owner=="vertical-member"인 장애물은 tier와 무관하게 검사하지 않는다**(리더가 기둥을 가로질러도 항상 허용).
- **box(문자 상자) 겹침 검사는 유지**(콜아웃 텍스트가 기둥과 안 겹치게).
- 콜아웃뿐 아니라 밸룬 리더 검사(IsBalloonFree)도 동일 원칙(이미 F2 자기 제외 있음 — 회귀만 없게).
- 기대: `extLeaderBy{vertical-member}`=0이 되고, tier 0에서 우측 자리(F1 box Y290.6과 F2 box Y331.4 사이 틈, r≈60)가 cost로 위쪽(135.95)을 이겨 **콜아웃이 우측 개방공간으로 이동**(사용자 이미지).

## 하지 말 것
- F2 밸룬 경로(cycle112 해결) 변경 금지. R1·치수·타 타입 회귀 금지.
- vertical-member의 box 검사 제거 금지(텍스트 겹침 방지 유지).
- 다른 owner 장애물의 리더 검사 변경 금지.

## 제약
- 빈 catch 금지. `#nullable disable`면 null 가드.

## 검증 (Codex, 필수)
1. `dotnet build` 오류 0, 경고 14 초과 금지.
2. `REPORT.md`에 변경 요약·빌드 결과. 라이브는 사용자.

## 성공 기준 (다음 라이브)
- RC5 파이프 콜아웃이 우측 개방공간(기둥 우측, F1~F2 사이 높이대)에 배치, 로그 `extLeaderBy`에 vertical-member 없음.
- 텍스트는 기둥/플레이트/밸룬과 미겹침, 리더는 파이프→텍스트로 기둥 가로지름.
- F2·RC9·RC1~8·GD 회귀 0.
