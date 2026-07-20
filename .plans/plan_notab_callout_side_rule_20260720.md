# 무탭 콜아웃 좌우 규칙 확립 (cycle 88)

## 1. 확정된 사용자 규칙 (2026-07-20)

| # | 규칙 |
|---|---|
| R1 | **라인넘버(B.O.P) 콜아웃의 좌우** = 파이프(원) 중심이 **뷰포트 사각형 중앙선**의 어느 쪽에 있는지로 결정. 우측 절반→우측, 좌측 절반→좌측. 서포트 타입 무관. |
| R2 | **부재(L-/C-) 콜아웃의 좌우** = R1과 **같은 방식**이되 기준 객체가 **부재 자신의 위치**. |
| R3 | **상하**는 고정값 없음. 간섭 회피 조건으로 로직이 탐색해 선택. |
| R4 | **좌우는 절대 불변**. 막히면 상하·거리만 조정하고 좌우 반전은 하지 않는다. |

### 검증 케이스 (실측 그림 2장)
- GD2: 파이프가 뷰포트 중앙선 **우측** → 라인넘버 **우측**.
- GD3: 파이프가 뷰포트 중앙선 **좌측** → 라인넘버 **좌측**.

## 2. 현행 코드와의 괴리

`Commands.cs:5042 / 5198 / 5380`
```
double outwardAngle = (m == dB) ? -90.0 : (m == dT) ? 90.0 : (m == dL) ? 180.0 : 0.0;
```
- 좌우가 **앵커가 서포트의 어느 면에 가까운가**로 정해진다. 뷰포트 중앙선과 무관 → R1/R2 위반.
- `outwardAngle` 하나가 좌우와 상하를 동시에 결정한다. R3(상하만 탐색)와 구조가 맞지 않는다.
- `TryPlace`는 좌우를 **비용 항**으로만 다룬다(`PFS_NOTAB_CALLOUT_ANGLE_W`). 비용이면 언제든 뒤집힐 수 있어 R4(불변) 위반.

`Commands.cs:50`
```
bool checkLeader = tier == 0;
```
- tier 1 강등 시 리더 충돌 검사가 **전부** 해제된다. GD2에서 두 콜아웃 모두 tier=1로 떨어져 리더가 텍스트를 관통한 근인.

## 3. 설계

### 3-1. 좌우를 하드 제약으로 승격
`NotabCalloutPlacer`에 `RequiredSide { Left, Right }`를 도입한다.
- `TryPlace` 후보 루프에서 `textLeft != (side == Left)` 인 후보는 **비용 계산 전에 탈락**시킨다.
- 좌우가 제약이 되었으므로 `outwardAngleDeg`는 **상하 탐색의 시드**로 의미가 축소된다. 시드는 0°(우) / 180°(좌)로 두고 Fan이 상하를 훑는다.
- `PFS_NOTAB_CALLOUT_ANGLE_W`(바깥 방향 이탈 비용)는 좌우가 제약으로 바뀌면 역할이 중복된다. 상하 선호가 없으므로 **기본값 0.0으로 낮춘다**(env로 부활 가능).

### 3-0. 선행 교정 (Codex 자문 지적, 실측 확인 완료)

**P1. 중앙선 기준 오류** — `placer`의 `_minX/_maxX`는 뷰포트 사각형이 아니다.
`Commands.cs:4830`에서 `supportPaperExt ± 100`(콜아웃 허용영역)으로 생성된다.
→ **뷰포트 사각형 extents를 별도로 구해 placer에 전달**하고, 중앙선은 그것으로 계산한다.
placer 경계값을 중앙선 산출에 쓰면 안 된다.

**P2. 이중 TryPlace — env 방향 노브가 죽은 코드** — `5198`/`5380`의 `TryPlace` 결과는
이후 `AppendNotabDirectCallout`(5016)이 `5043`에서 `TryPlace`를 **다시** 호출해 덮어쓴다.
`5042`가 방향을 자체 재계산하므로 `PFS_NOTAB_DIR_*`는 최종 작도에 도달하지 않는다.
→ 배치는 `AppendNotabDirectCallout` **한 곳에서 1회만** 수행하도록 단일화한다.
5198/5380의 사전 `TryPlace`는 제거한다.

**P3. FAIL 후 무검사 fallback 작도** — `5049` 이후 `fallbackLeft` 기반으로
장애물 검사 없이 콜아웃을 그린다. R4를 위반할 수 있다.
→ 고정 좌우 배치가 tier 2까지 실패하면 **작도를 생략하고 진단 로그를 남긴다**.

### 3-2. 좌우 판정
중앙선 = (뷰포트 사각형 MinX + MaxX) / 2.0. **P1에 따라 뷰포트 extents를 별도 전달받아 사용**.
- 라인넘버: 기준 X = **파이프 중심 X(페이퍼)** = `pipeCenterXPaper`.
- 부재: 기준 X = **해당 부재 앵커의 X**.
- 경계(정확히 중앙) 시 우측으로 본다(결정론 보장).

### 3-3. 완화 순서 계단화 (R4 준수)
`checkLeader` 단일 플래그를 2축으로 분리한다.

| tier | 좌우 | 콜아웃끼리 리더 검사 | 치수·서포트·파이프 리더 검사 |
|---|---|---|---|
| 0 | 고정 | ON | ON |
| 1 | 고정 | **ON (유지)** | OFF |
| 2 | 고정 | OFF | OFF |

- 콜아웃 간 간섭이 도면 판독성을 가장 크게 해치므로 **마지막까지 지킨다**.
- tier 2에서도 실패하면 **좌우를 어기지 않고 FAIL**로 종료하고 로그에 남긴다(R4가 배치 성공보다 우선).

### 3-3-A. 후보 생성 개선 (Codex 권고 채택)
극좌표 후보를 좌우 고정에 맞게 재구성한다.
```
sign = (side == Left) ? -1 : +1
horizontal = max(minDx, radius * abs(cos(angle)))
x = anchor.X + sign * horizontal
y = anchor.Y + radius * sin(angle)
textLeft = (side == Left)   // 좌표 비교로 재판정하지 않는다
```
±90° 부근에서 부동소수점 부호로 좌우가 갈리는 문제와 `boxLeft` 클램프로 인한
중복 후보를 함께 제거한다.

탐색 실패율 대책: 반경 상한을 상수 60이 아니라 **실제 허용영역에서 산출**하고,
reject 사유(`out-of-bounds` / `box obstacle` / `external leader` / `placed-callout leader`)
별 카운트를 로그에 남겨 범위 부족과 장애물 과밀을 구분한다.

### 3-4. env 노브 정리
- `PFS_NOTAB_DIR_<TYPE>_<PIPE|M#>`의 **각도 오버라이드 의미는 제거**한다(R1/R2/R4와 충돌, 게다가 P2로 죽은 코드).
- 호환을 위해 값 `L`/`R`만 **좌우 강제 오버라이드**로 남긴다. 숫자 각도는 무시하고 deprecated 로그.
- 상하는 env로 고정하지 않는다(R3).

### 3-5. 부재 콜아웃 폭 산출 통일 (Codex 지적)
부재 콜아웃은 `designation.Length * txt * 0.7`로 폭을 추정하는데 실작도는 `MText.ActualWidth`를 쓴다.
배치 상자와 실제 문자가 어긋난다. P2의 단일화로 `MText` 실측 폭·높이 1회 배치로 통일된다.

### 3-6. 진단 로그 (R4 검증용)
`callout-draw`에 `requiredSide` / `sideSrc(rule|override)` / `referenceX` / `viewportCenterX` / `tier` / 실패 사유를 남긴다.

## 4. 검증
1. `dev_test.bat` 빌드·배포 후 GD1/GD2/GD3 추출.
2. 로그 `C:\Temp\pfs_diag.log`에서 `callout-draw`의 `side` / `sideSrc` / `tier` / `scanned` / `obst` 확인.
3. 기대: GD2 라인넘버 `side=right`, GD3 라인넘버 `side=left`, 전 콜아웃 `tier<=1`, `FAIL` 0건.
4. 육안: 콜아웃끼리 간섭 0, 리더의 텍스트 관통 0.

## 5. 회귀 주의
- GD1은 현재 사용자 OK 판정 상태다. 좌우 규칙 변경으로 GD1이 바뀔 수 있으므로 **추출 후 재확인 필수**.
