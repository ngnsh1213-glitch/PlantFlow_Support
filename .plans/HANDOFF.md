# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 95
- **status**: ready
- **issued_at**: 2026-07-20
- **title**: 무탭 BOM 표 + 밸룬 **신규 설계**(이식 아님)
- **작업 경로**: `d:\PlantFlow\PlantFlow_Support\PlantFlow_Support\Core\Commands.cs`
- **계획서**: `d:\PlantFlow\PlantFlow_Support\.plans\plan_notab_bom_balloon_design_20260720.md` (**먼저 정독**)
- **핸드오프 위치**: `d:\PlantFlow\PlantFlow_Support\.plans\HANDOFF.md`

## 전제 (cycle 94 계측 확정, 재조사 금지)
- **부재 밸룬은 오쏘에도 없다** — `bom-item item='F1' branch=NONE anchorExact=True`.
  생성되는 리더는 `PLN`/`BOP` 둘뿐. **이식이 아니라 신규 설계다.**
- BOM 행은 원본 DB에서 정상 취득(6칸 균일, `HANTEC`·`PFS STANDARD` 모두).
- `TaggingPoints` = `F1`/`F2`/`P1_0` … WCS 확보. **`p1`은 `p0+(50,50,z=0)` 방향 힌트, 실좌표 아님.**
- BOM 표 영역 `(640.5,84.5)~(820.5,108.5)`, 행당 8. **뷰포트(30.5~640.5) 바깥 우측**.
- `BOMs`는 Plant 컨텍스트 필수 → **side DB 호출 불가**.
- **BOM이 부재를 구분한다**: RC3 `F1(500)`/`F2(600)`은 같은 `ANGLE A6`지만 길이가 다른 별개 행.
  현재 무탭은 designation 중복 제거로 하나만 그린다.

## A. BOM/앵커 스냅샷 (원본 → side DB)
- 캡처 지점 = **`Commands.cs:7790`**(cycle 94 계측 코드 자리). `CloneSelectionToSideDatabase()`(1444) 이전.
- 보관 = **명시 DTO 정적 목록**. `List<string[]>`를 그대로 들고 다니지 말 것
  (framework 6칸 / attachment 9칸 불균일).
```csharp
NotabBomRow { Item, Category, Description, Material, QuantityOrLength, Remark }
NotabBalloonAnchor { Item, WcsP0 }
```
- **초기화는 `RunNotabDetailPipeline()` 진입부(1389)에 함께 추가** — 배치 처리 누출 방지.
  기존 `MEASURE state-reset` 로그에 두 목록 개수도 포함시킬 것.

## B. BOM 표 작도 (`Table` 사용)
무탭 `layoutBtr`은 정상 페이퍼 BTR이고 `detailDb`가 `WorkingDatabase`라 `Table` 사용 가능.
직접 선/문자 작도는 병합·정렬·행증가·extents 재구현이라 이득 없음.
- `TableStyle = db.Tablestyle` → **`GenerateLayout()` 먼저** → `AppendEntity`.
- 시작점·열폭·행높이·문자는 오쏘 실측값에서 출발: `(640.5,84.5)`, `15/30/40/40/30/25`, `8`, `3`.
- **`FlowDirection`이 행 증가 방향을 정한다. "위로 자란다"는 라이브 extents로 재확인**할 것.
- 표 `GeometricExtents`를 구해 **placer 장애물로 등록**(안전장치. 표는 뷰포트 밖이라 실질 충돌은 없음).
- 행 수가 많을 때 타이틀블록 충돌 여부를 로그로 남길 것.

## C. 밸룬 작도 (신규, MLeader 금지)
`AppendNotabDirectCallout` **재사용 금지**(MText 폭·밑줄·3정점 리더 전제).
별도 어댑터 경로로 만든다:
```
TryPlace(anchor, side, 2r, 2r, ...)      // 원의 외접 사각형으로 배치
→ 반환 중심 = 원 중심
→ anchor → elbow → 원주 접점 Leader 직접 생성
→ Circle + 중앙정렬 문자(번호)
→ CommitBalloon(외접 사각형, 리더 2선분)
```
- ★기존 `Commit()`은 좌우를 `textCenter.X < anchor.X`로 추론한다(`Commands.cs:124`).
  밸룬은 **`CommitBalloon` 별도 메서드로 명시적 bbox·리더 등록**.
- 반지름·문자 크기는 env(`PFS_NOTAB_BALLOON_R` 등), 기본은 글자 높이 기준.
- 좌우 규칙 R1~R4와 콜아웃 여백(`PFS_NOTAB_CALLOUT_PAD`)은 그대로 적용.

## D. 앵커 가드 (★자동 보장 아님)
좌표 변환은 보장되나 **시각적 유효성은 보장되지 않는다**:
- 무탭은 side DB 솔리드를 **clip-box로 실제 절단**(`Commands.cs:3779`). F1/F2 `p0`가 범위 안이라는
  불변식이 코드에 없다. cycle 92의 S2 검증은 **좌표 변환 검증**이지 F 키 시각 검증이 아니다.
- `NotabProjectWcsToPaper`는 예외 시 `Point3d.Origin`을 반환(`4608`)해 **정상값과 구분 불가**
  → **`TryProject...` 형태로 성공 여부를 분리**할 것.
- 키마다 가드 후 실패 시 **밸룬 생략 + 사유 로그**:
  `balloon-skip reason=projection-failed|outside-viewport|outside-support|no-anchor`

## E. 정책 결정 (구현에 반영)
1. **부재 텍스트 콜아웃 vs 밸룬** — **기본값 = 밸룬로 대체**(부재는 번호만, 규격은 표에서 읽음).
   `PFS_NOTAB_MEMBER_TEXT=1`이면 기존 텍스트 콜아웃도 함께 그림(공존).
   **사용자 라이브 확인 후 확정**하므로 두 경로 모두 동작해야 한다.
2. **한 BOM item ↔ 여러 앵커**(`P1_0`, `P1_1`) — **앵커마다 밸룬 1개**, 번호는 BOM `Item`(`P1`).
   중복 번호가 여러 개 찍히는 것이 제작도 관례에 맞다.
3. **BOM에만 있고 앵커가 없는 항목**(`J1` SET ANCHOR 등) — **밸룬 생략, 표에는 유지**.
4. **작도 순서** = 치수 → BOM 표(장애물 등록) → 파이프 → 부재 → U-bolt → **밸룬**.
   기존 확정 배치의 회귀를 최소화한다.

## 완료 기준
1. 빌드 성공(`dev_test.bat`은 사용자 수동 실행).
2. RC1/RC2/RC3·GD1/GD2/GD3에서 **BOM 표 생성**, 행 수가 BOM 행과 일치.
3. 밸룬이 `F1`/`F2`/`P1` 앵커에 생성되고 서로·기존 콜아웃과 **겹치지 않음**.
4. 표가 타이틀블록·뷰포트와 충돌하지 않음.
5. **기존 배치 회귀 없음**(cycle 88 R1~R4 · cycle 92 포트 앵커 판정 유지).
6. `balloon-skip` 사유가 로그로 구분됨.

## 하지 말 것
- MLeader 사용. 오쏘 PLN/BOP 리더 이식(무탭에 이미 있음).
- side DB에서 `BOMs` 조회(Plant 컨텍스트 없음).
- cycle 88 R1~R4 / cycle 92 포트 앵커 / cycle 93 표준 판정 변경.

## 자문 출처
**Codex MCP**(2026-07-20, read-only) — 캡처 지점 정정(7790), `Table` 사용 가능 판정,
원형 밸룬 어댑터 설계와 `Commit()` 좌우 추론 결함, F1/F2 시각적 보장 부재(clip·은선),
`Point3d.Origin` 폴백 미구분, BOM 행 길이 불균일, item↔앵커 다대일. Gemini 미호출.
