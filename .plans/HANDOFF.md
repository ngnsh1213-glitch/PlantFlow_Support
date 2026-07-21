# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 102
- **status**: ready
- **issued_at**: 2026-07-21
- **title**: 부재 밸룬(F1/F2) 전용 배치 — 부재 끝단 바깥 2후보로 한정
- **작업 경로**: `d:\PlantFlow\PlantFlow_Support\PlantFlow_Support\Core\Commands.cs`
- **핸드오프 위치**: `d:\PlantFlow\PlantFlow_Support\.plans\HANDOFF.md`
- **기준 커밋**: `4d24627` (cycle 101)
- **자문**: Codex(§9) 1회. **`verticalX` 오용이라는 구조적 결함이 새로 발견되어 지시에 포함.**

## ⚠ 검증 필수
`dotnet build` **오류 0**이 완료 조건이다. 미실행 시 커밋하지 말고 `status: blocked`로 반려하라.

## 사용자 확정 요구 (수작업 도면 3장 대조로 확정. 논쟁 종료)

> **부재 밸룬은 그 부재의 "여유 있는 쪽 끝단" 바로 바깥에 놓고,
> 화살표는 그 끝단에 닿는다. 리더는 짧은 수평 직선 1개.**

- RC2 수작업: F1이 가로재 **오른쪽** 끝단 바깥
- RC3 수작업: F1이 가로재 **왼쪽** 끝단 바깥 → **끝단 선택만 다르고 원리는 동일**
- 두 경우 모두 F2는 세로재 **오른쪽 측면**에 짧은 가로 리더
- F1 = 가로재, F2 = 세로재 (매핑은 현행이 맞다)

## 현재 동작과 결함

```
RC2 F1 adjust=horizontal-mid rawAnchor=(384.5,368.9) anchor=(339.5,368.9) center=(252.7,368.9) dist=86.8 tier=extended free=3/64
RC3 F1 adjust=horizontal-mid rawAnchor=(385.5,378.9) anchor=(335.5,378.9) center=(267.9,378.9) dist=67.6 tier=extended free=1/48
RC3 F2 adjust=vertical-mid   anchor=(311,318.9)      center=(253,318.9)   dist=58            free=1/40
```

1. **mid 보정이 화살표를 부재 중앙으로 끌고 간다.** 끝단이어야 한다.
2. **8방향 × 거리 링 전역 탐색**이라 "끝단 바깥"이라는 개념 자체가 없다. 반대편으로 58~87 돌아간다.
3. **밸룬이 마지막 배치**라 앞선 콜아웃이 좋은 자리를 선점(`free=1~3`).
4. **🔴 `verticalX`는 부재 X가 아니라 치수선 X다**(자문 발견).
   `verticalX = verticalAnchorX - offset - dimClear`. 이 값을 F2 앵커에 쓰면
   **세로재 측면이 아니라 치수선 쪽**을 가리킨다. F2가 계속 어긋난 구조적 원인일 수 있다.

## 집도 지시

### 1) F1/F2 전용 배치 경로 신설 (약 8272~8380 구간)
- `vertical-mid` / `horizontal-mid` 앵커 보정을 **제거**한다.
- F1/F2 판정 직후 **전용 후보 2개**만 만든다.
  - **F1(가로재)**: `anchor = (가로재MinX 또는 MaxX, 부재 중심 Y)`.
    후보 중심은 그 끝단의 **좌/우 바깥에만**.
  - **F2(세로재)**: `anchor = (세로재박스.MinX 또는 MaxX, 세로재 중심 Y)`.
    후보 중심은 그 측면의 **좌/우 바깥에만**.
- `touch`를 **수평으로** 계산한다 → 리더가 반드시 **짧은 수평 선분 1개**가 된다.
- 두 후보 중 `IsBalloonFree` 통과분만 대상으로 `ClearanceTo`가 **큰 쪽 채택**.
  동점이면 **리더가 짧은 쪽**, 그래도 동점이면 타입별 기본 방향을 두고 **주석에 명시**.
- 전용 경로가 성공하면 기존 8방향×거리 링 코드는 **실행하지 않는다.**

### 2) 🔴 `verticalX` 오용 제거 (자문 발견, 필수)
- **`verticalX`(치수선 X)를 밸룬 형상 좌표로 재사용하지 말 것.**
- F2의 좌/우 측면과 중심 Y는 **`memberBoxes` / `TryGetNotabVerticalMemberBox`의 `PaperBox`**에서 얻는다.
  - 좌측면 `PaperBox.MinPoint.X`, 우측면 `PaperBox.MaxPoint.X`, 리더 Y `(MinY+MaxY)/2`.
- 포트 기반 세로 길이(`verticalBaseY/TopY`)는 치수용으로 유용하나 **밸룬 측면 작도의 근거로는 부족**하다
  (길이만 있고 폭이 없다). 솔리드 투영 상자를 우선한다.
- F1 끝단은 현재 전달값(`dimReferenceMinX`, `maxX`)으로 대체로 충분하나,
  **이름을 `horizontalMemberMinX/MaxX`류로 바꿔 치수용 값과 혼동되지 않게 할 것**(자문 권고).

### 3) 배치 순서 조정 (제한적)
- **F1/F2 밸룬만** 파이프/부재/U-bolt 콜아웃보다 **먼저** 배치한다.
- 단 아래는 **먼저 유지**한다 — 밸룬이 이들을 침범하면 안 된다:
  BOM 표, 치수, 파이프 장애물 등록.
- 순서: BOM 표·치수·파이프 장애물 → **F1/F2 밸룬 배치 + `CommitBalloonBox`** →
  U-bolt·일반 콜아웃(밸룬 상자·리더를 피해 배치) → 나머지 밸룬(P1 등).
- **부작용 인지**: UB-xxx·라인넘버가 외곽 자리를 잃어 더 멀어지거나 skip될 수 있다.
  사용자 요구가 부재 밸룬 끝단 고정이므로 이 우선순위 변경은 요구에 부합한다.
  완화책으로 F1/F2 후보를 **끝단 바깥 짧은 거리로 제한**하고, 다른 콜아웃은 기존 탐색을 유지한다.

### 4) 실패 시 작도 생략 (폴백 금지)
확정 규칙 R2대로 처리한다. **8방향 폴백을 두지 말 것** — 끝단 바깥 규칙을 깨는 도면이 다시 나온다.
- 전용 후보 성공 → 작도
- 전용 후보 2개 모두 실패 → `balloon-skip reason=member-end-no-space`
- 부재 형상 판별 실패 → `balloon-skip reason=member-geometry-unavailable`
- **F1/F2가 아닌 품목(P1 등)은 기존 perimeter 탐색 유지**

### 5) 계측
`balloon-draw`에 선택한 **끝단(left/right)**, 사용한 **`PaperBox` 좌표**, 두 후보의 `ClearanceTo`를 남긴다.
수작업 도면 대조 시 어긋남의 원인이 형상인지 배치인지 즉시 갈린다.

## 회귀 위험 (자문 지적. 라이브에서 반드시 확인)
- **최대 위험 = "어떤 `NotabMemberBox`가 F1/F2인지"의 식별.**
  `TryGetNotabVerticalMemberBox`는 종횡비·파이프 거리 휴리스틱이고, 가로재는 "가장 넓은 상자"다.
  RC1~RC3에서는 맞아도 보강재·클램프·베이스 솔리드가 추가된 형상에서 다른 솔리드를 고를 수 있다.
- `horizontalMinX/MaxX`가 실제 가로재가 아니라 치수 산출용 파라미터 범위일 경우 끝단이 미세하게 어긋난다.
  → 5)의 로그로 확인한다.
- 끝단 바로 바깥 후보가 support 장애물 패딩과 닿는지, 원주·서브라벨을 포함한 `ballBox`가
  뷰포트 안인지 RC2/RC3에서 검증할 것.

## 이번 사이클에서 하지 말 것
- **파이프(라인넘버) 꺾임 리더 복원 금지.** 다음 사이클로 유지.
- 유볼트 경로(1자 리더, `UBOLT_MIN_DX`, 중단 접속, `BoxVerticalEdgeMidpointToward`) 변경 금지.
- P1 앵커 처리(`adjust=port`, `rawAnchor` 유지) 변경 금지.
- `PFS_NOTAB_OBSTACLE_PAD`, `BALLOON_LEADER_W`, `MAX_STEPS`, `EXT_STEPS` 기본값 변경 금지.

## 제약
- 빈 catch 금지. 실패 경로는 `FileDiag`.
- 신규 상수는 `GetEnvDouble` 경유.
- 검사·등록·작도가 쓰는 점은 항상 동일해야 한다.

## 검증 (Codex, 필수)
1. `dotnet build` — 오류 0, 경고 15 초과 금지. 미실행 시 커밋 금지.
2. 커밋 후 `REPORT.md` 기록. 라이브는 사용자가 `dev_test.bat`로 수행.

## 성공 기준 (다음 라이브)
- F1 밸룬이 **가로재 끝단 바깥**, F2가 **세로재 측면 바깥**에 놓이고 화살표가 그 지점에 닿는다.
- 리더가 **짧은 수평 직선 1개**다. `dist`가 대폭 감소한다(현재 58~87 → 20~30대 기대).
- RC2는 오른쪽, RC3는 왼쪽 끝단이 선택된다(여유 큰 쪽 규칙이 수작업본을 재현).
- F2가 **치수선이 아니라 세로재 측면**을 가리킨다(`verticalX` 오용 해소 확인).
