# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 100
- **status**: ready
- **issued_at**: 2026-07-21
- **title**: 밸룬 2단계 탐색으로 F1/F2 복구 + P1 앵커 원복 + 유볼트 리더 문자 중단 접속
- **작업 경로**: `d:\PlantFlow\PlantFlow_Support\PlantFlow_Support\Core\Commands.cs`
- **핸드오프 위치**: `d:\PlantFlow\PlantFlow_Support\.plans\HANDOFF.md`
- **기준 커밋**: `a4bc217` (cycle 99)
- **자문**: Codex(§9) 1회. 지적 반영(support 장애물 실측, P1 한정 제외, 중단 접속 동반 수정).

## ⚠ 검증 필수
`dotnet build` **오류 0**이 완료 조건이다. 미실행 시 커밋하지 말고 `status: blocked`로 반려하라.
(cycle 98에서 빌드를 건너뛴 채 커밋해 컴파일 불가 상태가 리포에 들어간 전례가 있다.)

## 확정된 원인 (라이브 3회 + 자문. 재조사 금지)

### 결함 A — F1·F2 밸룬 실종 (2사이클 연속)
```
balloon-skip key=F1 free=0 cand=32 maxClear=0 rejBy{box:support=25,box:unnamed=7}
balloon-skip key=F2 free=0 cand=32 maxClear=0 rejBy{box:support=20,box:unnamed=12}
```
32후보 전부 **밸룬 원 상자가 `support` 장애물과 겹쳐** 탈락. 리더·치수 교차가 아니다.

**산수로 확정(자문 실측)**:
- `support` 장애물 = `supportPaperExt` 전체 + `PFS_NOTAB_OBSTACLE_PAD`(기본 2.0) 사방 확장.
  최근 라이브 RC2 기준 실제 차단 범위 `(283.5,256.9)~(387.5,401.1)` = **폭 약 104 × 높이 약 144**.
- 현재 최대 중심거리 = `gap + radius + step × 4` ≈ **58**.
- 중앙 부근 앵커에서 원 상자까지 완전히 탈출하려면 **약 81.6 이상** 필요.
→ **4 step으로는 구조적으로 불가능.** cycle 97에서 `MAX_STEPS` 8→4로 줄인 것이 직접 원인.
  P1만 살아남은 건 앵커가 끝단이라 58로 탈출 가능해서다.

### 결함 B — P1 밸룬이 허공 (cycle 99 신규 회귀)
```
balloon-draw key=P1_0 adjust=port+local-box rawAnchor=(291.3,364.25) anchor=(335.5,388.75)
```
cycle 99에서 넣은 local-box 보정이 후보마다
`candidateAnchor = BoxEdgeMidpointToward(itemBox, c)`로 리더 시작점을 옮기는데,
P1에서 원본 대비 **X+44, Y+24** 이동해 실제 부재를 벗어났다.

### 결함 C — 유볼트 리더가 문자 하단에 접속
사용자 요구: **문자의 중단(세로 중앙)에 접속**. 현재 `AttachmentPoint.BottomLeft` +
`baseY + textGap`이라 리더 끝이 문자 밑변에 닿는다. cycle 99의 1자 전환에 딸린 잔여 항목.

## 집도 지시 (3건)

### 1) 밸룬 2단계 탐색 (결함 A)
- 1단계: 현행 `PFS_NOTAB_BALLOON_MAX_STEPS`(4) 범위로 탐색. **자유 후보가 1개 이상이면 여기서 확정**하고
  확장하지 않는다(짧은 리더 선호 유지).
- 2단계: 1단계 자유 후보가 **0일 때만** `PFS_NOTAB_BALLOON_EXT_STEPS`(신설, 기본 12, 범위 1~32)로 재탐색.
- **확장 단계의 거리 폭주 방지**(자문 지적): 점수식 `clearance - distance × leaderW`는 여유를 크게
  선호해 확장 시 불필요하게 먼 후보를 고를 수 있다. 확장 단계에서는 **자유 후보가 처음 나타나는
  거리 구간만 평가**하거나 거리 가중치를 높인다. 둘 중 택일하고 근거를 `REPORT.md`에 적을 것.
- 로그에 `tier=normal|extended`와 선택 거리·확장 여부를 남긴다.
- **`support` 장애물을 밸룬용으로 축소·면제하지 말 것**(자문 권고). 원 상자가 서포트 실루엣과
  겹치면 읽기성이 크게 나빠진다. "리더는 서포트 위를 지나도 되고, 원 상자는 안 된다"는 현 정책이 옳다.

### 2) P1 한정 local-box 보정 제외 (결함 B)
- local-box 외곽 접점 보정을 **P1에만** 적용하지 않는다. 앵커는 `rawAnchor` 그대로 쓴다.
- **`port` 앵커 전체를 제외하지 말 것**(자문 지적). F 계열도 `port`를 거치며 외곽 접점 혜택이
  필요하다. `P1` ITEM(또는 그 부재 분류)에 한정할 것.
- 이 제한 덕에 F1/F2는 영향을 받지 않으므로, 1)의 효과 판정이 흐려지지 않는다.
- 로그의 `adjust=`에 실제 적용 여부가 드러나게 한다(`port` vs `port+local-box`).

### 3) 유볼트 리더를 문자 **중단**에 접속 (결함 C)
**주의(자문 지적)**: `Attachment`만 바꾸면 충돌 모델과 실제 위치가 어긋난다. 아래를 한 묶음으로:
- `TryPlace`의 `candP1`을 하단 기준 `baseY`가 아니라 **텍스트 상자의 좌·우 변 중점 `(edgeX, y)`** 로 생성.
- `MText.Attachment`를 배치 방향별로 `left ? MiddleRight : MiddleLeft`.
- `MText.Location`도 같은 변 중점으로: 왼쪽 배치면 `(rightX, y)`, 오른쪽 배치면 `(leftX, y)`.
- `p1`의 그 좌표를 **검사·작도·`Commit`에 동일하게** 사용.
- `baseY + textGap` 기반 작도 좌표와 `TryPlace`가 가정하는 `height` 상자 관계를 **중단 기준으로 재정의**.
→ `TryPlace` 상자 / 실제 MText 범위 / 리더 종점 / 등록 선분이 모두 같은 기하를 쓰게 할 것.

## 이번 사이클에서 하지 말 것
- `PFS_NOTAB_BALLOON_LEADER_W`(2.0), `PFS_NOTAB_BALLOON_MAX_STEPS`(4) 기본값 변경 금지.
  1)은 **상한 추가**이지 기존 값 변경이 아니다.
- `support` 장애물 패드(`PFS_NOTAB_OBSTACLE_PAD`) 변경 금지.
- 밸룬 리더-장애물 교차 검사 추가 금지(cycle 99에서 원인 아님으로 확정).
- 유볼트 박스 채택 로직, `BoxVerticalEdgeMidpointToward`, `PFS_NOTAB_UBOLT_MIN_DX`(10) 변경 금지.
- 전역 `PFS_NOTAB_CALLOUT_MIN_DX`(40) 변경 금지.

## 제약
- 빈 catch 금지. 실패 경로는 `FileDiag`.
- 신규 상수는 전부 `GetEnvDouble` 경유.
- **검사·등록·작도가 쓰는 점은 항상 동일해야 한다.**

## 검증 (Codex, 필수)
1. `dotnet build` — 오류 0, 경고 15 초과 금지. 미실행 시 커밋 금지.
2. 커밋 후 `REPORT.md` 기록. 라이브는 사용자가 `dev_test.bat`로 수행.

## 성공 기준 (다음 라이브)
- **F1·F2 밸룬이 그려진다.** skip이면 `rejBy{...}`와 `tier=extended`로 사유가 특정된다.
- P1 밸룬 화살표가 `rawAnchor` 위치(실제 부재)에 닿는다. `adjust=port`로 찍힌다.
- 유볼트 리더가 문자 **세로 중앙**에 접속하고, 여전히 꺾임 없는 직선 1개다.
- 밸룬 `dist`가 1단계로 해결된 건은 여전히 50 미만이다(짧은 리더 회귀 없음).
