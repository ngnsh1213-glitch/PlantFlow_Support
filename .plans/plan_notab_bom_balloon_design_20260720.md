# 무탭 BOM 표 + 밸룬 신규 설계 (cycle 95)

## 1. 전제 — cycle 94 계측으로 확정된 사실 (재조사 금지)

| 항목 | 실측 결과 |
|---|---|
| **부재 밸룬** | **오쏘에도 존재하지 않는다.** `bom-item item='F1' branch=NONE anchorExact=True` — 앵커는 있는데 `P*`/`R*` 어느 분기에도 안 걸려 미생성. 생성되는 리더는 `PLN`/`BOP` 둘뿐. → **이식이 아니라 신규 설계** |
| **BOM 행** | 원본 DB에서 정상 취득. 전부 **6칸 균일**. `HANTEC`·`PFS STANDARD` 양쪽 모두 동작 |
| **밸룬 앵커** | `TaggingPoints` = `F1`/`F2`/`P1_0` … WCS 확보. **`p1`은 `p0+(50,50,z=0)` 방향 힌트일 뿐 실좌표 아님** |
| **표 공간** | `(640.5,84.5)~(820.5,108.5)`, 행당 8. **뷰포트(30.5~640.5) 바깥 우측**이라 내부 겹침 없음 |
| **상태 수명** | 배치 처리에서 누출 없음(`state-reset` 전부 0) |
| **`BOMs` 의존** | Plant 프로젝트·DataLinksManager 필수 → **side DB 호출 불가.** 원본 단계 캡처 필수 |

### ★핵심 발견 — BOM이 부재를 구분한다
```
RC3  F1 | FRAMEWORK | ANGLE A6 | ASTM A36 | 500
     F2 | FRAMEWORK | ANGLE A6 | ASTM A36 | 600
     P1 | BASE PLATE| 180x180x6mm | ASTM A36 | 1
     J1 | SET ANCHOR| (M10)3/8"   | ASTM A36 | 4ea
```
같은 프로파일(`ANGLE A6`)이라도 **길이가 다른 별개 행**이다.
현재 무탭은 designation 중복 제거로 부재 콜아웃을 하나만 그린다(`L-65×65×6`).
BOM 행을 쓰면 **세로재·가로재를 각각 표기**할 수 있고, 밸룬 번호(`F1`/`F2`)와
앵커(`TaggingPoints`)까지 일관되게 이어진다.

## 2. ★설계 분기 (사용자 결정 필요)

밸룬을 도입하면 **부재 텍스트 콜아웃(`L-65×65×6`)과 역할이 겹친다.**

- **(가) 밸룬으로 대체** — 부재는 밸룬 번호(`F1`)만 찍고 규격은 BOM 표에서 읽는다.
  제작도 관례에 가깝고 도면이 깨끗해진다. 콜아웃 혼잡(cycle 88~92에서 계속 싸운 문제)이 크게 준다.
- **(나) 공존** — 밸룬 + 텍스트 콜아웃 둘 다. 정보는 많지만 좁은 도면에서 배치 실패가 늘어난다.
- **(다) 선택형** — env 노브로 전환.

**권고 = (가)**. 다만 규격집(`PIPE SUPPORT STANDARD`)은 `MEMBER "M"` 형태의 지시선 라벨도 쓰므로,
**사용자 확정 필요**. 확정 전에는 (다)로 구현해 두 형태를 라이브에서 비교하는 것도 가능하다.

## 3. 설계

### 3-1. BOM 스냅샷 (원본 DB → side DB 이송)
- 캡처 지점 = `AutoIncludeRelatedParts()` 직후, `CloneSelectionToSideDatabase()` 직전.
  (cycle 94 계측 코드가 이미 그 위치에서 BOM을 읽는 데 성공했다.)
- 보관 = **명시 DTO 리스트**. `string[]` 원본 배열을 그대로 들고 다니지 않는다.
  ```
  NotabBomRow { Item, Category, Description, Material, QuantityOrLength, Remark }
  ```
- `TaggingPoints`도 같은 시점에 **WCS로 스냅샷**(`NotabBalloonAnchor { Item, Wcs }`).
- 정적 상태 수명은 기존 `s_iso*`와 동일하게 진입부에서 초기화(M5 로그 유지).

### 3-2. BOM 표 작도 (무탭 페이퍼)
- 위치·열폭은 오쏘 실측값을 시작점으로: `(640.5, 84.5)`, 열폭 `15/30/40/40/30/25`, 행높이 `8`, 문자 `3`.
- **행 수에 따라 위로 자란다**(오쏘 `FlowDirection` 확인 필요). RC3 4행이면 h=48.
- 표 `GeometricExtents`를 구해 **`NotabCalloutPlacer` 장애물로 등록**.
  뷰포트 바깥이라 현 허용영역과는 겹치지 않지만, 등록해 두는 것이 안전하다.
- 타이틀블록과의 충돌은 라이브로 확인.

### 3-3. 밸룬 작도 (신규, 직접 작도)
**MLeader 금지**(cycle 79~83 폐기). 무탭 방식 = 원(Circle) + 구형 `Leader` + 문자.
- 앵커 = `TaggingPoints[Item].p0`를 `NotabProjectWcsToPaper`로 투영한 페이퍼 좌표.
  **`p1`은 방향 힌트로만** 쓰거나 무시하고 배치기가 방향을 정한다.
- 원 반지름·문자 크기는 env 노브(`PFS_NOTAB_BALLOON_R`, 기본은 글자 높이 기준).
- **배치는 `NotabCalloutPlacer`를 통과시킨다** — 기존 콜아웃·치수·서포트·표와 간섭 회피,
  cycle 88의 좌우 규칙 R1~R4와 콜아웃 간 여백(`PFS_NOTAB_CALLOUT_PAD`) 적용.
- 투영 실패 시 **원점 폴백이 발생하면 밸룬을 그리지 않는다**(cycle 94 지적).
- BOM에만 있고 앵커가 없는 항목(`J1` SET ANCHOR 등)은 **밸룬 생략**(정상 동작).

### 3-4. 표 ↔ 밸룬 정합
- 밸룬 번호 = BOM `Item`. 표에 없는 번호를 그리지 않는다.
- 표에 있는데 앵커가 없으면 밸룬 없이 표에만 남는다(`J1`).
- 불일치 건수를 로그로 남긴다(`balloon-skip reason=no-anchor|projection-origin`).

## 3-5. 자문(Codex) 반영

**캡처 지점 정정**: 실제 호출 위치는 `AutoIncludeRelatedParts()` 직후가 아니라
축·파이프중심·선택계측 뒤인 `Commands.cs:7790`(cycle 94 계측 코드 자리)이다.
`CloneSelectionToSideDatabase()`(1444) 이전이므로 Plant API 접근은 유효하다.
초기화는 `RunNotabDetailPipeline()` 진입부(1389)에 이미 집중돼 있으므로 두 DTO 목록도 거기서 `Clear()`.

**`Table` 사용 가능**: 무탭 `layoutBtr`도 정상 페이퍼 BTR이고 `detailDb`가 `WorkingDatabase`다.
`TableStyle=db.Tablestyle` → `GenerateLayout()` → `AppendEntity` 순서면 구조적 문제 없다.
직접 선/문자 작도는 병합·정렬·행증가·extents를 재구현해야 해 이득이 없다.
- `GenerateLayout()`은 반드시 `AppendEntity` **전에**.
- `FlowDirection=(FlowDirection)1`이 행 증가 방향을 결정 — **"위로 자란다"는 라이브 extents로 재확인**.

**원형 밸룬은 어댑터 경로**: `AppendNotabDirectCallout` 재사용 금지(MText 폭·밑줄·3정점 리더 전제).
```
TryPlace(anchor, side, 2r, 2r, ...)  → 외접 사각형으로 배치
→ 반환 중심을 원 중심으로 → anchor→elbow→원주 접점 Leader 직접 생성
→ Circle + 중앙정렬 문자 → CommitBalloon(외접 사각형 + 리더 2선분)
```
★`Commit()`이 좌우를 `textCenter.X < anchor.X`로 추론한다(`Commands.cs:124`).
밸룬은 **`CommitBalloon` 별도 메서드로 명시적 bbox·리더를 등록**해야 안전하다.

**F1/F2 앵커는 자동 보장이 아니다**: 좌표 변환(WCS→DCS→페이퍼)은 보장되지만,
- 무탭은 side DB 솔리드를 **clip-box로 실제 절단**한다(`Commands.cs:3779`). F1/F2 `p0`가
  그 범위 안이라는 불변식은 코드에 없다.
- cycle 92의 S2 검증은 **좌표 변환 검증**이지 모든 F 키의 시각적 유효성 검증이 아니다.
- 은선/겹침으로 투영점은 맞아도 대상이 가려질 수 있다.
→ 키마다 가드 필요: **원점 폴백 여부 / 뷰포트 안 / `supportPaperExt`·clip 범위 안 / 라이브 육안**.

**추가 위험**
- `NotabProjectWcsToPaper`는 예외 시 `Point3d.Origin` 반환(`4608`)이라 **정상값과 구분 불가**.
  → `TryProject...` 형태로 **성공 여부를 분리**해야 한다.
- BOM 행 길이 불균일(framework 6 / attachment 9). **DTO 정규화 필수**.
- **한 BOM item ↔ 여러 앵커**(`P1_0`, `P1_1`). "행당 밸룬 1개" vs "앵커마다 밸룬" **정책 명시 필요**.
- 표 extents를 placer 장애물로 등록해야 하나, 현재 placer 허용영역은 **뷰포트 내부**이고
  표는 뷰포트 밖이라 실질 충돌은 없다. 등록은 안전장치.

## 4. 검증
1. 빌드 성공.
2. RC1/RC2/RC3·GD1/GD2/GD3에서 **BOM 표 생성**, 행 수가 BOM 행과 일치.
3. 밸룬이 `F1`/`F2`/`P1` 앵커 위치에 생성되고 **서로·기존 콜아웃과 겹치지 않음**.
4. 표가 타이틀블록·뷰포트와 충돌하지 않음.
5. **기존 배치 회귀 없음** — 치수·라인넘버/BOP·부재·U-bolt 콜아웃(cycle 88·92 판정 유지).

## 5. 하지 말 것
- MLeader 사용. 오쏘 PLN/BOP 리더 이식(무탭에 이미 있음).
- cycle 88 좌우 규칙 R1~R4 / cycle 92 포트 앵커 / cycle 93 표준 판정 변경.
- BOM을 side DB에서 조회하려는 시도(Plant 컨텍스트 없음).
