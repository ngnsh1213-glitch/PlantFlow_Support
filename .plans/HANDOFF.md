# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 101
- **status**: ready
- **issued_at**: 2026-07-21
- **title**: 밸룬 F 계열 local-box 보정 제거 (화살표를 부재 중심선에 고정)
- **작업 경로**: `d:\PlantFlow\PlantFlow_Support\PlantFlow_Support\Core\Commands.cs`
- **핸드오프 위치**: `d:\PlantFlow\PlantFlow_Support\.plans\HANDOFF.md`
- **기준 커밋**: `bcb108e` (cycle 100)
- **자문**: Codex(§9) 1회. **자문 권고로 범위를 1건으로 축소**(파이프 꺾임 리더는 cycle 102로 분리).

## ⚠ 검증 필수
`dotnet build` **오류 0**이 완료 조건이다. 미실행 시 커밋하지 말고 `status: blocked`로 반려하라.

## 확정된 원인 (라이브 3회 + 자문 코드리딩. 재조사 금지)

cycle 100의 2단계 탐색으로 **F1·F2 밸룬은 다시 그려진다**(`tier=extended`, `free=1~3`). 성과는 유지한다.
남은 결함은 화살표가 부재가 아닌 **빈 공간**을 찍는 것이다.

```
F1 adjust=horizontal-mid+local-box rawAnchor=(385.5,378.9) anchor=(285.5,318.9)   ← X100 Y60 이동
F2 adjust=vertical-mid+local-box   rawAnchor=(342,258.9)   anchor=(385.5,318.9)   ← X43  Y60 이동
P1 adjust=port                     rawAnchor=(291.3,364.25) anchor=(291.3,364.25) ← cycle100에서 제외, 정상
```

**원인(자문 확정)**:
1. 최종 `anchor`는 중앙 보정 결과가 아니다. 후보 밸룬 중심 `c`마다
   `BoxEdgeMidpointToward(itemBox, c)`로 **화살표 시작점을 다시 계산**한다
   (약 8330행, 8409행). 밸룬이 어느 방향에 놓이느냐에 따라 **상자 폭/높이만큼 점프**한다.
2. 그 `itemBox` 선택기가 **F ITEM의 실제 형상을 식별하지 못한다.**
   - F1: `rawAnchor` 최근접 → 동률 시 최소 면적 → Handle 사전순 (약 5010행).
   - F2: 파이프 최근접 상자를 제외한 뒤 **종횡비 `h/w` 최대** (약 4968행).
     병합 Solid3d·볼트·기타 형상이 모두 후보다.
   → BOM ITEM ↔ 형상의 안정적 식별자가 아니다.

**판단**: 고쳐 쓸 가치가 낮다(자문). 안정적 식별자를 만들려면 별도 트랙이 필요하고,
근접/종횡비 추정은 다른 타입에서 같은 결함을 재발시킨다.
`vertical-mid`/`horizontal-mid`는 **부재의 긴 방향 중앙**으로 옮기는 보정이라
결과가 항상 부재 중심선 위에 있다. 이것만 남기는 것이 최소·안전한 조치다.

또한 `local-box`를 넣은 cycle 99의 명분("중앙 앵커라 후보 공간이 없다")은
cycle 100의 확장 탐색으로 이미 해소됐다. 존재 이유가 사라진 보정이다.

## 집도 지시 (1건)

### F 계열 `local-box` 보정 제거
- F1(`horizontal-mid`)·F2(`vertical-mid`) 경로에서 `local-box` 보정을 **제거**한다.
  후보마다 `BoxEdgeMidpointToward(itemBox, c)`로 화살표 시작점을 재계산하는 부분이 대상이다.
- 화살표 시작점 = **중앙 보정을 마친 앵커** 하나로 고정한다(후보마다 바뀌지 않는다).
- `vertical-mid` / `horizontal-mid` 보정 자체는 **유지**한다. 제거 대상이 아니다.
- P1은 cycle 100에서 이미 제외돼 있다. 그 동작을 바꾸지 말 것.
- 검사(`IsBalloonFree`)·작도·`Commit`이 **같은 점**을 쓰도록 유지한다.
- 이 제거로 `itemBox` 선택기(약 4968·5010행)가 **완전히 미사용이 되면 함께 제거**하고,
  다른 곳에서 여전히 쓰이면 유지한 뒤 어디서 쓰이는지 `REPORT.md`에 적을 것.
- 로그의 `adjust=`는 이제 `vertical-mid` / `horizontal-mid` / `port`만 나와야 한다.

### 회귀 감시 (자문 지적)
탐색 중심이 바뀌므로 **기존 자유 후보가 보존된다는 보장은 없다.** `free=0` 재발 가능성이 있다.
- 확장 단계는 `stepIndex=5..12`를 탐색하며 거리식은 `gap + radius + max(radius,txt) × stepIndex`.
  기본값(`txt=8`)이면 최외곽 중심거리 약 **115.6**으로, F1의 X100·F2의 Y60 이동을 흡수할 규모다.
- 다만 확장 단계는 **최초로 자유 후보가 나온 링만** 평가하므로, 그 링의 8방향이 모두 막히면
  더 먼 링으로 넘어가지 않는다. **이 동작을 이번 사이클에서 바꾸지 말 것** —
  라이브에서 `free=0`이 재발하면 그때 별도로 다룬다.
- `tier` / `free` / `cand` / `balloon-skip` 로그는 그대로 유지한다(판정 지표).

## 이번 사이클에서 하지 말 것
- **파이프(라인넘버) 콜아웃 꺾임 리더 복원 금지.** cycle 102로 분리했다.
  이유(자문): `TryPlace`의 리더 모델 확장이 필요한 별개 변경이라, 함께 하면 라이브 실패 시
  원인이 분리되지 않는다.
- 확장 탐색 로직(`PFS_NOTAB_BALLOON_EXT_STEPS`, 최초 자유 링만 평가) 변경 금지.
- `PFS_NOTAB_BALLOON_LEADER_W`(2.0), `MAX_STEPS`(4), `OBSTACLE_PAD` 변경 금지.
- 유볼트 경로(1자 리더, `UBOLT_MIN_DX`, `BoxVerticalEdgeMidpointToward`, 중단 접속) 변경 금지.
- P1 앵커 처리 변경 금지.

## 제약
- 빈 catch 금지. 실패 경로는 `FileDiag`.
- 신규 상수는 `GetEnvDouble` 경유.
- 검사·등록·작도가 쓰는 점은 항상 동일해야 한다.

## 검증 (Codex, 필수)
1. `dotnet build` — 오류 0, 경고 15 초과 금지. 미실행 시 커밋 금지.
2. 커밋 후 `REPORT.md` 기록. 라이브는 사용자가 `dev_test.bat`로 수행.

## 성공 기준 (다음 라이브)
- `balloon-draw`의 `anchor`가 `rawAnchor`에서 **부재 중심선 방향으로만** 이동한다
  (X로 100씩 튀는 현상 소멸). `adjust=`에 `+local-box`가 사라진다.
- F1 화살표가 **가로재** 위에, F2 화살표가 **세로재** 위에 닿는다.
- F1·F2 밸룬이 계속 그려진다(`free>0`). skip이면 `rejBy{...}`·`tier`로 사유가 특정된다.

## 다음 사이클 예고 (cycle 102, 이번엔 손대지 말 것)
라인넘버(파이프) 콜아웃만 **꺾임 1회**(경사 + 수평 꼬리)로 되돌린다. 유볼트는 1자 유지.
자문이 제시한 불변식:
| 단계 | 유볼트 | 파이프 |
|---|---|---|
| 배치 검사 | `leaderFrom→textEdge` 1선분 | `leaderFrom→elbow→textEdge` **2선분 모두** |
| 작도 | 같은 1선분 | 같은 2선분 |
| 등록 | 같은 1선분 | 같은 2선분 |
`Commit`의 3점 overload가 이미 두 선분 등록을 지원한다(약 149행).
cycle 100의 중단 접속과는 충돌하지 않는다 — 파이프의 `textEdge`를 그대로 두고 앞에 `elbow`만 넣는다.
