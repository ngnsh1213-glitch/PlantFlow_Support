# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 107
- **status**: ready
- **issued_at**: 2026-07-22
- **title**: RC5 F2 밸룬 기둥 배치 + RC7 분할 좌우반전·F2 대각재 + RC9 P1 리더교차 폴백
- **작업 경로**: `Core/Commands.cs`, `Core/NotabCalloutPlacer.cs`, `Ortho/StandardSupport.cs`
- **진단 원장**: `.plans/notab_rc5_9_review_20260722.md`
- **기준 커밋**: `e72b019`
- **자문**: §9 Codex(3-스트라이크 전환). 근인·수정지점 코드로 확정.

## ⚠ 검증 필수
`dotnet build` 오류 0(경고 14 초과 금지). 미실행 시 커밋 금지·`status: blocked`. Claude가 REPORT 수령 후 직접 빌드 재확인.
**항목별 독립**: 3건 중 불확실한 건 해당 항목만 `blocked/partial`로 남기고 나머지 커밋.

## 배경 (라이브 3차 측정 + Codex 자문, 재측정 불요)
cycle 106으로 RC5 F2 앵커=S2(기둥)·RC7 분할 렌더까지 됐으나 배치/방향이 미흡. 3건 잔여.

## 집도 지시

### 1) RC5 F2 밸룬을 기둥에 붙이기 (Commands.cs `AppendNotabBalloons` member-end 세로 분기)
- 근인: F2 앵커=S2 기둥(axisX=274.5, 좌측 근처). 우측=기둥 내부인데 `supportPaperExt`가 "support" 장애물로 등록돼 **밸룬 상자 겹침 금지**로 우측 탈락 → 좌측 허공(dist48.4·clear2.8)에 배치.
- 수정: **세로재(isVerticalMember) 밸룬에 한해** 배치 후보의 **"support" 상자 충돌만 면제**하는 옵션을 `IsBalloonFree`(NotabCalloutPlacer.cs)에 추가하고 그 경로에서만 사용. **치수·BOM·기존 콜아웃 상자/리더 충돌은 계속 금지.** 이러면 기둥 옆(내부측)에 바짝 배치 가능.
- side 선택: 세로재는 support 내부(기둥 반대편 여백)를 향하도록. 단순 우측 강제 금지(support 벗어날 때까지 멀어짐). "가장 가까운 자유 위치"가 되게.
- **F1 포트는 건드리지 말 것**(PPorts[0]=하단 가로재 정상). PPorts[2] 금지.
- 검증: F2 balloon-draw가 기둥 axisX 근처(dist 축소)로, clear 개선.

### 2) RC7 가로 분할 좌우 반전 + F2 대각재 밸룬
- **2a 분할 반전**: `TryGetNotabRcHorizontalParams`(≈3471, 공통 left=A/right=A1)는 **건드리지 말 것**. `standardName=="RC7"`일 때만 소비부(≈3623 직전/직후)에서 `paramLeft`↔`paramRight` 교환. → split tick가 배관측(좌 150)으로, 좌/우=150/800. **RC5(400/250) 불변 확인.**
- **2b F2 대각재**: RC7 F2 밸룬이 대각 브레이스를 가리키게. RC7은 `RS2()` 재사용(StandardSupport:175), F2=PPorts[2](:365). 포트는 유지하되, Commands가 `F*`·비세로를 가로 span 끝단으로 강제하는 규칙을 **RC7 F2엔 적용하지 말고** PPorts[2] raw anchor를 그대로 앵커로 쓰는 분기 추가.
- **불확실 시**: 2a만 커밋하고 2b는 `partial`. 2b는 대각재 형상 앵커라 라이브 확인 필요.

### 3) RC9 P1 리더교차 2차 폴백 (Commands.cs + NotabCalloutPlacer.cs)
- 근인: P1_1 정상탐색 free=0(포화). 차단 다수가 상자 무겹침·**리더만 교차**(calloutLeader78/calloutBox31). `SetLeaderExemptOwner`는 `IsBalloonFree`가 미참조라 무효.
- 수정: **P1(isPlateItem) 정상탐색이 free=0일 때만** 2차 폴백 — `IsBalloonFree`에 "기존 콜아웃 상자/리더와의 **리더 교차만** 허용" 옵션 추가. **밸룬 상자 겹침·support·치수 충돌은 계속 금지.**
- 한정: P1 + normal free=0에만. RC1~3 P1은 정상 후보 있으면 기존 경로 유지 → 무회귀.
- P1 선배치 순서 변경은 하지 말 것(파이프·유볼트 밀어냄).

## 하지 말 것
- 치수 총폭·세로(cycle105)·RC7 분할값 산출(cycle106) 로직 변경 금지.
- `GetNotabTypeConfig`·`rcMemberGeometry` 변경 금지.
- RC1~3·GD·RC4/6/8 밸룬·치수 회귀 금지. 전역 배치 규칙(부재 끝단·유볼트 변중점) 훼손 금지.
- `IsBalloonFree` 면제 옵션은 **호출부에서 명시적으로 켤 때만** 동작(기본 동작 불변).

## 제약
- 빈 catch 금지. 신규 노브는 env 경유. `#nullable disable`면 null 가드.

## 검증 (Codex, 필수)
1. `dotnet build` 오류 0, 경고 14 초과 금지.
2. `REPORT.md`에 항목별(1/2a/2b/3) 결과·면제 옵션 적용 범위·빌드 결과. 라이브는 사용자.

## 성공 기준 (다음 라이브)
- RC5 F2 밸룬이 기둥에 인접(dist 대폭↓), F1=하단 가로재. RC7 분할 150(좌·배관)/800(우), F2=대각재. RC9 P1 2개 모두 작도.
- RC1~3·GD·RC4/6/8 회귀 0.
