# RC 앵커·세로치수를 포트 기준으로 (cycle 92)

## 1. 확보된 근거 (cycle 91 라이브 로그)

```
RC2  S1 paper=(344.5,377.81)              = 파이프 중심(pipeCenter와 일치)
     S2 paper=(311,  268.9) role=F2-candidate   ← 기둥 밑동 (y = minY)
     S3 paper=(384.5,368.9)                = 우측 끝
     S4 paper=(298,  269.5)
RC3  S2 paper=(342,  258.9) role=F2-candidate
```
- `index=1`(`S2`)은 **기둥 밑동**이다. `y`가 정확히 `minY`.
- `x`가 타입마다 다르다: RC2는 좌측 25% 지점, RC3는 56% 지점.
  **비율 상수로는 맞출 수 없던 값**이며 실제 도면 기둥 위치와 일치한다.
- `HANTEC.RC1()`이 `PPorts[1]`을 F2에 쓰는 것과 인덱스가 일치한다.

## 2. 사용자 확정 목표 (그림 2장 대조)

| 항목 | 현재 | 목표 |
|---|---|---|
| 세로 500 치수 | 베이스 플레이트 좌측 끝 기준, 멀리 떨어짐 | **기둥 바로 옆**, 보조선이 기둥 상·하단에 닿음 |
| L 부재 콜아웃 | 허공(7A 박스 중앙) | **기둥 몸통**에 화살표 접촉 |

세로 치수는 **기둥(F2)을 재는 치수**다. 기준이 부재 좌측이 아니라 **기둥**이어야 한다.

## 3. 설계

### 3-1. 기둥 기하를 포트에서 확정
```
postX      = S2.x                       (포트 index=1, role=F2-candidate)
postBaseY  = S2.y                       (= minY)
postTopY   = postBaseY + F2 * vScale
```
`F2`·`vScale`은 이미 정상 동작 중(`dimV param` 로그로 확인).

### 3-2. 세로 치수를 기둥 기준으로
- 치수선 x = `postX - offset`(기둥 좌측에 근접). 현행 `dimReferenceMinX - offset - dimClear` 대신.
- 보조선 두 점 = `(postX, postBaseY)`, `(postX, postTopY)` → 기둥 상·하단에 접촉.
- **가로 치수 기준(`dimReferenceMinX`)과 분리한다.** 가로는 부재 폭(A+A1), 세로는 기둥 —
  재는 대상이 다르므로 기준이 달라야 한다. cycle 91에서 둘을 같게 만든 것을 **부분 되돌림**.
  단 되돌리는 것이 아니라 **각자 올바른 기준을 갖게** 하는 것이다. 둘 다 로그에 남긴다.

### 3-3. 부재 콜아웃 앵커를 기둥으로
```
anchor = ( postX,  postBaseY + (postTopY - postBaseY) * 0.5 )
RC2 → (311, 318.9)     현행 오류 앵커 (335.5, 318.9) 대비 x 24 어긋남
```
- `MemberAnchorSide`의 `vertical`/`top`/`bottom` **비율 추정식은 폴백 전용으로 강등**.
- 좌우는 cycle 88 R1~R4 그대로(변경 없음).

### 3-4. 폴백
포트 미확보·`F2` 누락·`vScale` 이상 시 **기존 경로 유지** + `source=port | fallback=...` 사유 로그.
GD 계열은 포트 경로를 **소비하지 않는다**(현행 유지).

## 4. 검증
1. RC1/RC2/RC3에서 세로 치수가 기둥 옆에 붙고 보조선이 기둥 상·하단에 접촉.
2. L 콜아웃 화살표가 기둥 몸통에 접촉(허공 0).
3. 가로 치수 450/450/500과 분할 유지(회귀 없음).
4. GD1/GD2/GD3 회귀 없음.

## 3-5. U-bolt 태그 — 읽기 확인 완료, 표기 착수 가능

15:19 추출에서는 `Tag=?` / `SupportName=(empty)`였으나, 사용자가 모델에 태그를 부여한 뒤
15:27 추출에서 정상 취득됐다.
```
ubolt-probe datalinks { SupportName=UB-002 | ShortDescription=UB | Tag=UB-002 | ... }
ubolt-probe datalinks { SupportName=UB-003 | ShortDescription=UB | Tag=UB-003 | ... }
```
- **읽기 경로는 이미 동작한다.** 데이터만 있으면 된다(코드 문제 아니었음).
- 사용할 키 = **`Tag`** (`SupportName`도 동일 값이나 `Tag`가 의미상 맞다).
  `PartNumber`·`TagName`·`Description`은 공백이라 사용 불가.
- **RC1-001에 U-bolt가 2개**(UB-002, UB-003) — 도면의 U볼트 2개와 일치.
  따라서 표기도 **개당 1개씩 복수**여야 한다.
- 앵커: U-bolt 객체의 페이퍼 박스 또는 포트로 산출(기둥 앵커와 동일한 원칙).
- 좌우·상하 배치는 cycle 88 R1~R4를 그대로 따른다.

## 5. 별건 (이번 사이클 범위 밖)
- `DesignStd` 판정 외부화(`PFS STANDARD`+`HANTEC` 동시 인식) 및 `StandardSupport` 개명은
  `TODO.md` 예약 항목으로 유지.
