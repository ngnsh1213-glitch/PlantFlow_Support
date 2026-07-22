# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 109
- **status**: ready
- **issued_at**: 2026-07-22
- **title**: RC5 F2 리더 기둥까지 연장 + RC9 P1_1 down-out 배치(+rejBy 계측)
- **작업 경로**: `Core/Commands.cs`(AppendNotabBalloons member-end·P1 down 경로)
- **진단 원장**: `.plans/notab_rc5_9_review_20260722.md`
- **기준 커밋**: `60ca015`
- **자문**: §9 Codex. RC5=화살표 끝점 축으로, RC9=down-out+계측.

## ⚠ 검증 필수
`dotnet build` 오류 0(경고 14 초과 금지). 미실행 시 커밋 금지·`status: blocked`. Claude가 REPORT 수령 후 직접 빌드 재확인.

## 배경 (라이브 5차 + 자문)
RC4·RC6·RC7·RC8 종결. RC5 F1/F2 **위치 정상**(cycle108) — 잔여는 F2 리더가 기둥에 안 닿음. RC9 P1_1은 cycle107(F3관통)↔108(누락) 진동.

## 집도 지시

### 1) RC5 F2 리더를 기둥까지 연장 (Commands.cs ≈6420, member-end 세로 분기)
- 근인: 세로재 화살표 끝점 `endMinX/endMaxX = rawAnchor.X ± halfThick`. `halfThick`(가독성 하한 4.8)이 실제 기둥 반폭보다 커 화살표가 기둥 우측 허공에 뜸.
- 수정: **세로재(isVerticalMember) 화살표 끝점을 `rawAnchor.X`(기둥 축)로** 둔다(endMinX=endMaxX=rawAnchor.X). rawAnchor.X가 기둥 중심축이라 화살표가 실제 부재를 관통해 닿는다.
- `halfThick`은 밸룬 원·문자 간격 등 **다른 용도엔 그대로 유지**(제거하면 원·문자 규칙 영향). 화살표 끝점 산출에서만 빼는 국소 변경.
- `memberBoxes`로 실제 폭 산출은 **금지**(기둥·가로재·플레이트 병합 솔리드라 폭 특정 불가). F2=600은 세로 길이지 폭 아님.
- 검증: F2 balloon-draw 화살표 X가 기둥 축(≈402.5)에 닿는지.

### 2) RC9 P1_1 down-out 배치 + rejBy 계측 (Commands.cs ≈6521, rc9-p1-down 경로)
- 근인: cycle108 순수 하향(dir=(0,-1)) 5후보 전부 차단. `maxClear=38.8·free=0` → **리더 교차** 탈락 유력(F3가 부재 pass서 먼저 배치·등록). 거리 확대만으론 같은 교차 반복 가능.
- 수정: rc9-p1-down 경로에서 **순수 하향을 첫 후보로 유지**, 실패 시 **소량 외측 하향 `(+ε, -1)` 후보 추가**(우측 플레이트 외측=+X 방향, ε는 작게 단계 확대). 검사는 **표준 `IsBalloonFree(..., exemptSupportBox:false, allowCalloutLeaderCrossing:false)`** 유지(F3/치수/support/기존상자 계속 차단).
- 첫 자유 후보 채택. 없으면 `balloon-skip reason=rc9-p1-down-no-space`로 종료(일반 탐색·p1-leader-fallback 미복귀 유지).
- **계측 추가(필수)**: rc9-p1-down skip 로그에 **`rejBy{...}` 집계**(box:support/box:callout/leader:calloutLeader/leader:calloutBox 등)를 포함. 다음 라이브에서 box 충돌인지 리더 교차인지 확정 위함.
- **`balloon-draw key=F3`의 anchor/center/box도 로그에 남는지 확인**(F3 좌표 확보). 이미 남으면 추가 불필요.
- RC9/P1_1 조건 국소 → P1_0·F3·RC1~3 무회귀.

## 하지 말 것
- RC5/RC7/RC8·RC1~3·GD 회귀 금지. 치수 변경 금지.
- P1_1 선배치(순서 변경) 금지 — F3 회귀 위험(자문 비권고).
- `IsBalloonFree` 면제 옵션을 RC9 하향에 켜지 말 것(F3 관통 재발).
- `halfThick`의 밸룬 원·문자 간격 용도 변경 금지.

## 제약
- 빈 catch 금지. 신규 노브는 env. `#nullable disable`면 null 가드.

## 검증 (Codex, 필수)
1. `dotnet build` 오류 0, 경고 14 초과 금지.
2. `REPORT.md`에 항목별 결과·RC5 화살표 X·RC9 rejBy 계측 추가 여부·빌드 결과. 라이브는 사용자.

## 성공 기준 (다음 라이브)
- RC5 F2 리더가 기둥에 닿음(끊김 없음).
- RC9 P1_1이 우측 플레이트 아래(하향, F3 미관통) 작도. 실패 시 skip 로그에 rejBy로 원인 노출.
- RC1~3·RC4~8·GD 회귀 0.
