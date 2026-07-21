# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 99
- **status**: ready
- **issued_at**: 2026-07-21
- **title**: 밸룬 F1/F2 소멸 복구(앵커를 부재 외곽 변 중점으로) + 유볼트 1자 리더·짧은 이격
- **작업 경로**: `d:\PlantFlow\PlantFlow_Support\PlantFlow_Support\Core\Commands.cs`
- **핸드오프 위치**: `d:\PlantFlow\PlantFlow_Support\.plans\HANDOFF.md`
- **기준 커밋**: `005d730` (cycle 98 + 빌드 수리)
- **자문**: Codex(§9) 1회. **Claude의 원인 가설이 기각되어 지시 내용이 바뀌었다.**

## ⚠ 검증 필수 (cycle 98 사고 재발 방지)
cycle 98에서 `dotnet build`를 건너뛴 채 커밋해 **CS0103 8건으로 컴파일 불가 상태가 리포에 들어갔다.**
이번 사이클은 **빌드 통과가 완료 조건**이다. 빌드를 돌리지 않았다면 커밋하지 말고
`REPORT.md`에 `status: blocked`로 기록하라. "수동 빌드 원칙"은 이 항목의 예외 사유가 아니다.

## 확정된 원인 (자문 검증. 재조사 금지)

### 밸룬 F1·F2 소멸
라이브 3회 전부:
```
balloon-skip key=F1 reason=no-space perimeter cand=32 free=0 maxClear=0
balloon-skip key=F2 reason=no-space perimeter cand=32 free=0 maxClear=0
balloon-draw key=P1_0 adjust=port ... free=3~14 maxClear=12.3~36.8   ← P1만 생존
```
**틀린 가설(폐기)**: "리더가 서포트를 관통해 치수선과 교차하여 기각된다."
→ `IsBalloonFree`(약 162행)는 **리더와 장애물의 교차를 검사하지 않는다.**
  `SetLeaderExemptOwner("support")`도 밸룬 경로에서는 쓰이지 않고 일반 콜아웃 `Free`에서만 쓰인다.
  **리더 면제 관련 작업은 이번 사이클에서 하지 말 것.**

**확정 원인**: `free=0`은 32개 후보의 **밸룬 원 상자 각각이** 장애물(서포트 패딩·치수·BOM·파이프)
또는 **이미 확정된 콜아웃 상자와 겹쳤다**는 뜻이다. `maxClear=0`은 자유 후보가 0이라 초기값이 남은 것이다.
F1/F2는 cycle 98에서 앵커가 `vertical-mid`/`horizontal-mid`로 **부재 한가운데**로 보정되면서,
탐색 반경(기본 4 step = 48.4) 안이 전부 서포트 내부가 됐다. P1은 `adjust=port`로 앵커가
**끝단**이라 바깥으로 향하는 후보가 살아남았다.

## 집도 지시

### 1) 밸룬 skip 진단 닫기 (먼저 할 것)
- `IsBalloonFree`의 `out string rej`를 현재 **버리고 있다.** 사유별로 집계해 skip 로그에 남긴다:
  `rejBy{box=N, calloutBox=N, calloutLeader=N, ...}` 형태. 실제 사유 문자열에 맞춰 집계할 것.
- 이 로그가 있어야 2)의 효과를 다음 라이브에서 판정할 수 있다. 2)보다 먼저 넣는다.

### 2) F1/F2 앵커를 "해당 부재의 실제 외곽 변 중점"으로
- `vertical-mid` / `horizontal-mid` 보정을 **부재 중앙**이 아니라
  **그 ITEM의 실제 형상 bbox의 변 중점**으로 바꾼다. 화살표는 실제 형상 위에 남고,
  후보는 서포트 바깥 방향으로 확보된다.
- **반드시 해당 ITEM의 로컬 형상 bbox를 쓸 것**(자문 지적). `supportPaperExt` 같은 전체 bbox의
  외곽을 쓰면 **다른 부재를 가리키게 된다.**
- 어느 변을 고를지는 밸룬 후보 방향(또는 포트 방향) 기준으로 정하고, **동률 규칙을 명시**할 것.
- 앵커가 부재 내부에 박히는 것 자체는 문제가 아니다(자문 확인). 문제는 **후보 공간이 없어지는 것**이다.
  따라서 목표는 "앵커를 밖으로 빼서 바깥 후보를 살리는 것"이다.
- `rawAnchor` 로그는 유지한다.

### 3) 유볼트 리더를 1자로 + 이격 10 이하 (2·3을 하나로 처리)
사용자 확정 요구: **"꺾임 없이 1자로, 부재 콜아웃과 동일하게. 리더 길이는 10 이하."**

**주의(자문 지적)**: 작도만 바꾸면 충돌 모델이 어긋난다. 현재도 `TryPlace`/`Commit`은
**중심 앵커**를 모델로 쓰는데 실제 작도는 `arrowFrom`(박스 변 중점)을 쓰는 **불일치가 이미 있다.**
1자 전환은 아래를 **한 묶음으로** 처리한다:
- `TryPlace`가 실제 시작점 `arrowFrom`을 입력으로 받아 **단일 선분 `arrowFrom → p1`** 을 검사.
- `Commit`도 **같은 단일 선분**만 등록.
- `p2`(수평 꼬리) 생성·검사·등록을 제거하거나 단일 선분 모델로 API를 정리.
- `Leader.AppendVertex`도 정확히 **같은 두 점**만 사용.
- 로그의 `nearX`/`farX`와 충돌 진단도 단일 종점 기준으로 변경.

**이격**:
- 유볼트 전용 `PFS_NOTAB_UBOLT_MIN_DX` 신설(기본 10, 범위 0~500). 전역 `PFS_NOTAB_CALLOUT_MIN_DX`(40)는
  **변경 금지** — 다른 콜아웃이 쓴다.
- 단순히 10으로 낮추면 텍스트가 유볼트에 겹친다. 자문 권고대로 **외곽 기준**으로 계산할 것:
  `effectiveMinDx = max(전용설정값, 중심→선택된 유볼트 변 거리 + 리더여유)`
  → 짧은 리더라도 시작점이 유볼트 외곽이고 텍스트가 유볼트 내부로 들어가지 않는다.
- 텍스트 상자와 유볼트 bbox의 겹침 금지는 **유지**한다.
  `PFS_NOTAB_CALLOUT_TEXT_GAP`은 수직 간격만 보장하므로 수평 겹침의 해결책이 아니다(자문).
- `BoxVerticalEdgeMidpointToward`(좌우 변 중점)는 **유지**한다. 사용자 승인 항목이다.

## 이번 사이클에서 하지 말 것
- **밸룬 리더 교차 검사 추가 금지.** 원인이 아니다(자문 확정).
- **최소 여유 게이트(`MIN_CLEAR`) 추가 금지.** free=0인 현 상태에서 넣으면 밸룬이 더 사라진다.
- `PFS_NOTAB_BALLOON_LEADER_W`(2.0), `PFS_NOTAB_BALLOON_MAX_STEPS`(4) 변경 금지.
- 유볼트 박스 채택 로직(`ubolt-box 채택`) 수정 금지 — 라이브 PASS 상태.
- P1이 플레이트가 아닌 부재를 가리키는 문제는 범위 밖(앵커 출처 문제, 별도 사이클).

## 제약
- 빈 catch 금지. 실패 경로는 `FileDiag`.
- 신규 상수는 전부 `GetEnvDouble` 경유.
- **검사·등록·작도가 쓰는 점은 항상 동일해야 한다.** 이번 사이클의 핵심 원칙이다.

## 검증 (Codex, 필수)
1. `dotnet build` — **오류 0**, 경고 15 초과 금지. **미실행 시 커밋 금지.**
2. 커밋 후 `REPORT.md` 기록. 라이브는 사용자가 `dev_test.bat`로 수행.

## 성공 기준 (다음 라이브)
- **F1·F2 밸룬이 다시 그려진다**(`balloon-skip free=0`이 사라지고 `free>0`).
- F1·F2 화살표가 허공이 아니라 부재 선 위에 닿는다.
- 유볼트 리더가 **꺾임 없는 직선 1개**이고, 길이가 **10 이하**이며 텍스트가 유볼트와 겹치지 않는다.
- skip 발생 시 `rejBy{...}`로 사유가 특정된다.
