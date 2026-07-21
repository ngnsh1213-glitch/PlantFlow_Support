# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 97
- **status**: ready
- **issued_at**: 2026-07-21
- **title**: 밸룬 화살표를 실측 부재 변 중점에 붙이고 리더 길이를 절반으로 줄인다
- **작업 경로**: `d:\PlantFlow\PlantFlow_Support\PlantFlow_Support\Core\Commands.cs`
- **핸드오프 위치**: `d:\PlantFlow\PlantFlow_Support\.plans\HANDOFF.md`
- **선행 커밋**: `8cfd311` (유볼트 화살표 변 중점 + 최소 면적 박스 채택 — 라이브 PASS)

## 배경 (재조사 금지 — 실측 확정)

직전 커밋 `8cfd311`로 **유볼트** 화살표는 해결됐다. 로그(09:05 라이브) 실측:

```
ubolt-box 채택 tag=UB-002 W=17.82 H=17.82 oversizeRejected=1
ubolt-box 채택 tag=UB-003 W=33.6  H=29.6  oversizeRejected=1
ubolt-box 채택 tag=UB-004 W=17.82 H=17.82 oversizeRejected=0
ubolt-box 채택 tag=UB-006 W=17.82 H=17.82 oversizeRejected=0
```
`ubolt-box 없음` 0건. 사용자 육안 확인 "UB-003 위치 양호". **이 경로는 건드리지 말 것.**

남은 결함은 **밸룬(F1/F2/P1) 경로** 2건이다.

### 결함 A — 화살표가 실제 부재 구석에 붙는다
`8cfd311`에서 `BoxEdgeMidpointToward`(변 중점)를 밸룬에도 적용했는데도 F2 화살표가
세로기둥 좌상단 구석에 보인다. 원인은 화살표 함수가 아니라 **기준 사각형**이다.

`Commands.cs` 약 8298~8311행:
```csharp
double memberThick = System.Math.Max(txt, radius);   // = 9.6, 실측 아님 추정값
if (isVerticalMember)
  memberRect = new Extents3d(
    new Point3d(verticalX - memberThick / 2.0, verticalBaseY, 0.0),
    new Point3d(verticalX + memberThick / 2.0, verticalTopY, 0.0));
```
기둥 폭을 **9.6으로 가정한 가상 사각형**이다. 실제 기둥 폭·중심과 어긋나므로,
그 가상 사각형의 변 중점을 정확히 찍어도 눈에는 실제 기둥의 구석으로 보인다.

### 결함 B — 리더가 항상 최대 길이로 뻗는다
라이브 로그 `balloon-draw` 9건 중 **6건이 `dist=96.4`(후보 상한)**, 나머지 58 / 38.8.
최솟값 19.6을 고른 건은 0건이다.
```
key=F2 center=(292.5,478.65) r=9.6 dir=2 dist=96.4 clear=78.3 score=20.46
```
점수식이 `score = clear - dist * leaderW` (leaderW 기본 0.6)인데, 멀어질수록 `clear`가
페널티보다 빠르게 커져 **"가장 멀고 한적한 곳"이 항상 이긴다.** F2는 주변에 간섭이
전혀 없는데도 86.8(=96.4-9.6) 길이의 리더가 그려진다. 사용자 요구 = **절반 수준으로**.

## 집도 지시

### 1) `memberRect`를 실측 박스 우선으로 (결함 A)
- `AppendNotabBalloons`에 `List<NotabMemberBox> memberBoxes` 파라미터를 추가한다.
  **호출부는 `Commands.cs:5291`** 한 곳이며, 그 스코프에 `memberBoxes`가 이미 존재한다
  (`AppendNotabUboltCallouts`에 같은 것을 넘기고 있다).
- `memberRect` 결정 순서를 바꾼다:
  1. `anchor`를 포함하는 `memberBoxes` 중 **최소 면적** 박스를 찾는다.
     (유볼트에서 쓴 것과 동일한 판정. 단 **과대 박스 기각은 적용하지 말 것** —
     밸룬은 기둥·가로재 같은 큰 부재를 정당하게 가리킨다.)
  2. 찾으면 그것을 `memberRect`로 쓴다.
  3. 못 찾으면 **현행 추정 사각형 로직을 그대로 폴백**으로 남긴다(제거 금지).
- 어느 경로를 탔는지 로그에 남긴다:
  `balloon member-rect key=... source=measured|estimated W=.. H=..`
- `isVerticalMember` / `horizontal-mid` 분기는 폴백 안에서 현행 그대로 유지한다.

### 2) 리더 길이 축소 (결함 B)
- `PFS_NOTAB_BALLOON_LEADER_W` **기본값 0.6 → 2.0** (범위는 현행 0.0~10.0 유지).
- 후보 상한 하드코딩 `step * 8.0`을 env로 뺀다:
  `PFS_NOTAB_BALLOON_MAX_STEPS` **기본 4**, 범위 1~16.
  → 상한이 `gap + radius + step * 4` = 약 48.4가 된다.
- 기존 `balloon-draw` 로그의 `dist=` 출력은 유지한다(검증 지표).

## 제약
- 유볼트 경로(`AppendNotabUboltCallouts`, `BoxEdgeMidpointToward`)는 **수정 금지**.
- `BoxEdgeToward`는 삭제하지 말 것(밸룬 후보 방향 산출 8340행에서 계속 쓴다).
- 빈 catch 금지. 실패 경로는 반드시 `FileDiag`로 남긴다.
- 상수 추가는 전부 `GetEnvDouble` 경유(하드코딩 금지).

## 검증 (Codex가 직접 수행)
1. `dotnet build` — 오류 0. (현재 경고 15건은 기존, 늘리지 말 것)
2. 커밋 후 `REPORT.md`에 기록.
3. 라이브 판정은 사용자가 `dev_test.bat`로 수행한다. Codex는 빌드까지만.

## 성공 기준 (다음 라이브에서 판정)
- `balloon member-rect ... source=measured` 가 최소 1건 이상 출력된다.
- `balloon-draw` 의 `dist=` 가 96.4 상한 고정에서 벗어나고, **F2가 50 미만**으로 내려온다.
- 육안: F2 화살표가 세로기둥 **상변 정중앙**에 닿는다.
