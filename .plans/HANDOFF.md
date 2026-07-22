# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 110
- **status**: ready
- **issued_at**: 2026-07-22
- **title**: RC5 F2 리더 가시화 + 파이프콜아웃 기둥회피 + RC9 P1_1 리더교차만 허용
- **작업 경로**: `Core/Commands.cs`, `Core/NotabCalloutPlacer.cs`
- **진단 원장**: `.plans/notab_rc5_9_review_20260722.md`
- **기준 커밋**: `7715755`
- **자문**: §9 Codex. F2 리더=gap 없음(화살촉 과대), 기둥=장애물 미등록, RC9=leader:calloutLeader=20 확정.

## ⚠ 검증 필수
`dotnet build` 오류 0(경고 14 초과 금지). 미실행 시 커밋 금지·`status: blocked`. Claude가 REPORT 수령 후 직접 빌드 재확인. 항목별 독립(불확실 건만 partial).

## 배경 (라이브 6차 + 자문)
RC4·RC6·RC7·RC8 종결. RC5 F1/F2 위치·치수 정상. 잔여 = RC5 F2 리더 가시성·파이프콜아웃 기둥간섭, RC9 P1_1.

## 집도 지시

### 1) RC5-A: F2 리더 가시화 (Commands.cs ≈6453, 세로재 outDist)
- 근인(자문): 리더 끝점은 이미 기둥축(402.5)에 정확히 닿음(gap 없음). 노출 리더 길이 10인데 화살표 크기 `Dimasz`≈10이라 **화살촉이 선분을 다 덮어 "끊긴 듯"** 보임.
- 수정: **세로재(isVerticalMember)에 한해** `outDist`를 `원 반경 + 화살표 크기 + 짧은 가시 여유`로 늘린다. anchor(화살표 끝점)는 그대로. `Dimasz` 자체 변경 금지(치수 영향).
- 검증: F2 리더에 화살촉 뒤 선분이 보이는지.

### 2) RC5-B: 파이프 콜아웃이 세로 기둥 회피 (Commands.cs ≈3830 + NotabCalloutPlacer.cs)
- 근인(자문): 세로 기둥이 **장애물 미등록**(obst=7에 supportPaperExt·치수·pipe·BOM뿐). 콜아웃 텍스트 좌단(407.77)·tail이 기둥(axisX402.5, 우변~407.5) 관통.
- 수정:
  - Commands.cs:3830 직후(파이프 콜아웃 호출 전) **포트 기반 세로 상자**를 장애물로 등록. 형상 원천=이미 계산된 `verticalAnchorX/baseY/topY`(3799). **`memberBoxes` 사용 금지**(병합 솔리드).
  - 상자만 등록하면 tier1/2에서 리더가 외부장애물 완화로 재관통 → `NotabCalloutPlacer.TryPlace/Free`에 **"vertical-member" hard 장애물 옵션**(모든 tier에서 리더 교차 유지) 국소 추가하고 이 기둥 상자에만 적용.
- **R1 좌우 규칙 불변**(TryPlace 우측 X부호 고정, fan/반경 탐색). 상하·거리로 회피.
- 회귀 주의: 세로 기둥 장애물은 **세로재 있는 타입(rcMemberGeometry vertical)에서만** 등록. 없는 타입 불변.

### 3) RC9 P1_1 리더교차만 허용 (Commands.cs rc9-p1-down 경로)
- 근인(계측 확정): P1_1 하향 20후보 전부 `rejBy{leader:calloutLeader=20}` — **콜아웃 리더 교차로만** 탈락(box·support·member 무겹침, maxClear=38.8). 즉 하향 위치는 F3 box를 안 건드리고 얇은 콜아웃 리더(파이프 B.O.P 유력)만 가로지름.
- 수정: rc9-p1-down 후보 검사를 **`IsBalloonFree(..., exemptSupportBox:false, allowCalloutLeaderCrossing:TRUE)`** 로. **calloutLeader 교차만 허용**, calloutBox·support·치수·부재 겹침은 계속 차단. → P1_1이 플레이트 아래 하향 작도(F3 box 미관통).
- cycle107 p1-leader-fallback(calloutBox까지 허용)과 다름 — **box는 계속 금지**. 순수 하향 우선 유지.
- 라이브 확인점: 교차되는 리더가 파이프 콜아웃이면 OK. 만약 F3 리더면 사용자 재검토 → 그때 F3 리더만 hard로 승격. REPORT에 P1_1 draw 시 교차 대상 명시.

## 하지 말 것
- RC5/7/8·RC1~3·GD 회귀·치수 변경 금지. `Dimasz` 변경 금지.
- 기둥 장애물을 세로재 없는 타입에 등록 금지. `IsBalloonFree` 기존 기본동작(옵션 off) 불변.
- P1_1 선배치(순서변경) 금지.

## 제약
- 빈 catch 금지. 신규 노브 env. `#nullable disable`면 null 가드.

## 검증 (Codex, 필수)
1. `dotnet build` 오류 0, 경고 14 초과 금지.
2. `REPORT.md`에 항목별 결과·RC9 P1_1 교차대상·빌드 결과. 라이브는 사용자.

## 성공 기준 (다음 라이브)
- RC5 F2 리더 선분 보임(끊김 해소), 파이프 콜아웃이 기둥 미관통.
- RC9 P1_1 우측 플레이트 아래 하향 작도(F3 미관통).
- RC1~3·RC4~8·GD 회귀 0.
