# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 106
- **status**: ready
- **issued_at**: 2026-07-22
- **title**: RC7 가로 A/A1 분할 + RC5 F1·F2 밸룬 포트 교정 + RC9 P1 배치 완화
- **작업 경로**: `PlantFlow_Support/Core/Commands.cs`(분할·배치), `PlantFlow_Support/Ortho/StandardSupport.cs`(RC5 포트)
- **진단 원장**: `.plans/notab_rc5_9_review_20260722.md`
- **기준 커밋**: `1ee2287`
- **자문**: §9 Codex 1회(수정 지점·포트 역전·배치 실패 원인 확정).

## ⚠ 검증 필수
`dotnet build` 오류 0(경고 14 초과 금지). 미실행 시 커밋 금지·`status: blocked`. Claude가 REPORT 수령 후 직접 빌드 재확인.

## 배경 (라이브 2차 + 자문, 재측정 불요)
cycle 105로 RC4/5/7/8 치수 PASS. 남은 3건:
- RC7: 가로 A/A1 분할이 splitGuard에 막혀 950 단일만 표시.
- RC5: F1·F2 밸룬 위치 오류(F2=상단플레이트, F1=좌측). 앵커 포트 매핑 역전.
- RC9: P1 밸룬 2개 중 1개 배치 실패로 누락.

## 집도 지시

### 1) RC7 가로 A/A1 분할 (Commands.cs ≈3633)
- `paramsHorizontal`(dimHSource=="params(A+A1)")일 때 **splitGuard 우회**. 3633행을:
  ```csharp
  splitGuard = !paramsHorizontal && (leftPaper < splitGuardLimit || rightPaper < splitGuardLimit);
  ```
- 분할점(3625)·값(3629-30)은 이미 정확(A/A1). 전역 limit 축소 금지(파이프센터 분할 회귀 위험).
- 성공: RC7 `split=(800,150)` 표시.

### 2) RC5 F1·F2 밸룬 포트 교정 (StandardSupport.cs, RC5 분기만)
- 근인: RC5의 F1/F2 TaggingPoint 포트가 RC1/2/3과 역전. RC1/2/3=F1:PPorts[2]/F2:PPorts[1](S2). RC5=F1:PPorts[1]/F2:PPorts[2].
- **F2 → PPorts[1]**(S2=기둥 자유단)로 교정 → `IsNotabVerticalMemberPort`(Commands.cs:6242) S2 일치 통과 → 세로 기둥 배치.
- **F1 → 하단 가로재를 가리키는 포트**로 교정. **PPorts[2]는 상단 플레이트라 쓰면 안 됨**(단순 스왑 금지). RC5 포트 구성을 조사해(미사용 PPorts[0]/[3] 등) 가로재 위 점을 특정하라. 특정 근거를 REPORT에 남길 것.
- **검증(필수)**: 수정 후 `MEASURE balloon-anchor key=F1/F2 p0=` 로그로 F2 p0가 S2와 일치, F1 p0가 가로재(수평 바) 위인지 확인. 불명확하면 F2만 고치고 F1은 `status: blocked`로 반려(둘 다 S2로 겹치게 두지 말 것).
- **StandardSupport.cs는 오쏘 경로 공용** → RC5(해당 std) 분기만 국소 수정. 타 타입·오쏘 회귀 금지.

### 3) RC9 P1 배치 완화 (Commands.cs AppendNotabBalloons)
- P1_0/P1_1 앵커 둘 다 존재. P1_1이 `IsBalloonFree` 충돌(support/box/leader)로 자유 후보 0 → skip.
- 완화 방향(택1, 저침습): ①P1 밸룬을 부재 밸룬처럼 콜아웃보다 먼저 배치해 자리 선점 ②P1 후보 탐색 반경/스텝 확대(P1 전용 노브) ③리더 교차 면제 확대.
- **회귀 주의**: 기존 정상 타입(RC1~3 등) P1 배치가 바뀌지 않는 범위에서. 바꾸면 광범위 회귀 위험 → 최소 변경.
- 불확실하면 이 항목만 `status: partial`로 남기고 1·2 먼저 커밋.

## 하지 말 것
- 치수 총폭·세로(cycle105 결과) 변경 금지. RC6/RC9 치수 제외 유지.
- `GetNotabTypeConfig`·`rcMemberGeometry` 변경 금지(cycle105 확정).
- 밸룬 배치 전역 로직(부재 밸룬 끝단 규칙 등) 훼손 금지.

## 제약
- 빈 catch 금지. 신규 매직넘버는 env 경유. `#nullable disable`면 null 가드.

## 검증 (Codex, 필수)
1. `dotnet build` 오류 0, 경고 14 초과 금지.
2. 커밋 후 `REPORT.md`에 항목별(1/2/3) 결과·F1 포트 특정 근거·빌드 결과. 라이브는 사용자.

## 성공 기준 (다음 라이브)
- RC7 가로 800/150 분할 표시.
- RC5 F2 밸룬=세로 기둥, F1 밸룬=하단 가로재(`vertical-port`/`horizontal-span` 로그 정합).
- RC9 P1 2개 모두 작도. RC1~3·GD·기타 회귀 없음.
