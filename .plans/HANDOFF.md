# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 108
- **status**: ready
- **issued_at**: 2026-07-22
- **title**: RC5 F1/F2 포트 재지정(cycle106 오지정 정정) + RC9 P1_1 하향 배치
- **작업 경로**: `Ortho/StandardSupport.cs`(RC5 포트), `Core/Commands.cs`(IsNotabVerticalMemberPort·AppendNotabBalloons)
- **진단 원장**: `.plans/notab_rc5_9_review_20260722.md`
- **기준 커밋**: `0bf945c`
- **자문**: §9 Codex. `RC5.py` 실 포트 순서로 cycle106 오지정 정정.

## ⚠ 검증 필수
`dotnet build` 오류 0(경고 14 초과 금지). 미실행 시 커밋 금지·`status: blocked`. Claude가 REPORT 수령 후 직접 빌드 재확인.
**항목별 독립**: 불확실 건은 해당 항목만 `blocked/partial`.

## 배경 (라이브 4차 + 자문, 재측정 불요)
RC6·RC7·RC8 이상없음(종결). 잔여 = RC5 밸룬, RC9 P1 방향.
- **RC5**: cycle106이 F1=PPorts[0]/F2=PPorts[1]로 지정했으나 **오지정**. `RC5.py` 실제=S2(PPorts1)=가로재 F1끝, S3(PPorts2)=세로재 F2 자유단. x≈354.5(S1/S6)는 파이프중심선(기둥 아님). F2가 S2(좌측)를 가리켜 엉뚱한 곳.
- **RC9**: cycle107 p1-leader-fallback으로 P1_1 작도되나 리더가 F3 관통. 좌측 P1_0처럼 하단·하향이어야.

## 집도 지시

### 1) RC5 F1/F2 포트 재지정 (StandardSupport.cs `RC5()` + Commands.cs)
- **StandardSupport.RC5()**: `F1: PPorts[0]→PPorts[1]`(S2=가로재 끝점), `F2: PPorts[1]→PPorts[2]`(S3=세로재 자유단). cycle106의 "PPorts[0]=가로재" 주석은 `RC5.py`와 불일치 → 정정.
- **Commands.IsNotabVerticalMemberPort()**(≈6252): 현재 `index==1`(S2)만 세로재 판정. **RC5에 한해 F2 앵커(index==2/S3)를 세로재로 인식**하도록 국소 분기 추가(타 타입 불변). 안 하면 F2가 다시 가로재 끝단 규칙으로 빠짐.
- **밸룬 전용**: 세로 치수 앵커(rcMemberGeometry 블록의 S2/index==1)는 **그대로 둘 것**. 치수는 이미 PASS(600). 치수까지 S3 통일 금지(별 검증).
- **라이브 주의(Codex 지적)**: S3 paper=(402.5,401)이 상단 플레이트와 만나는 자유단이라 밸룬이 플레이트에 걸쳐 보일 수 있음. balloon-draw 로그로 F2가 세로재(vertical-port) 판정·기둥 축 배치인지 확인. 어긋나면 `partial` 반려.

### 2) RC9 P1_1 하향 배치 (Commands.cs `AppendNotabBalloons` P1 경로)
- 근인: `p1-leader-fallback`(cycle107)이 콜아웃 상자/리더 교차를 무시 → F3 관통.
- 수정: 일반 8방향 탐색 **이전에**, `standardName=="RC9" && item=="P1_1"`이면 **dir=(0,-1)(하향)만 짧은 거리부터** 탐색. 후보 검사는 **표준 `IsBalloonFree(..., exemptSupportBox:false, allowCalloutLeaderCrossing:false)`** — F3 밸룬/리더·치수·support·기존 상자 전부 계속 차단.
- 첫 자유 후보 채택. 없으면 `balloon-skip reason=rc9-p1-down-no-space`로 종료하고 **일반 탐색·리더교차 폴백으로 되돌아가지 말 것**(하향 규칙 보장).
- P1_0의 실제 dir을 복제하지 말 것(내외 반전 위험). "하향"을 RC9 P1_1에만 명시 적용 → RC1~3·단일 P1 무회귀.
- `p1-leader-fallback`(cycle107)은 RC9 P1_1엔 미적용(위 하향 경로가 선점). 다른 타입 P1엔 유지.

## 하지 말 것
- RC5 세로/가로 **치수** 변경 금지(PASS 상태). RC6/7/8·RC1~3·GD 회귀 금지.
- `IsNotabVerticalMemberPort`의 비-RC5 판정(index==1) 변경 금지.
- 전역 밸룬 배치 규칙·`NotabCalloutPlacer` 시그니처 변경 불필요(호출부 국소).

## 제약
- 빈 catch 금지. `#nullable disable`면 null 가드. 신규 노브는 env.

## 검증 (Codex, 필수)
1. `dotnet build` 오류 0, 경고 14 초과 금지.
2. `REPORT.md`에 항목별 결과·RC5 F2 balloon-draw geom·빌드 결과. 라이브는 사용자.

## 성공 기준 (다음 라이브)
- RC5: F2 밸룬=세로 기둥(S3, vertical-port), F1=가로재. RC9: P1_1이 우측 플레이트 아래 하향(F3 미관통), 좌측 P1과 대칭.
- RC1~3·RC6/7/8·GD 회귀 0.
