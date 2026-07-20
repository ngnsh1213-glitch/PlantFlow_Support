# 오쏘 BOM·밸룬 자산의 무탭 이식 — 계측 선행 (cycle 94)

## 1. 목표
오쏘에서 정상 동작이 확인된 **BOM 표 + 밸룬**을 무탭 상세도에 합친다(`TODO.md` N4).
합쳐지면 오쏘 파이프라인을 유지할 이유가 없어져 **N5(3부채 코드 소멸)**로 이어진다.

RC 트랙에서 추정으로 3연속 빗나간 전례가 있으므로 **이식 전 의존성을 실측**한다.

## 2. 현재 격차

| 요소 | 무탭 | 오쏘 |
|---|---|---|
| 치수 | ✅ 파라미터 기반(RC/GD 확립) | ✅ |
| 라인넘버·BOP 콜아웃 | ✅ 직접 작도 | ✅ MLeader |
| 부재 콜아웃 | ✅ 포트 앵커 | ✅ |
| U-bolt 태그 콜아웃 | ✅ | — |
| **BOM 표** | ❌ | ✅ (`F1 FRAMEWORK ANGLE A7 ASTM A36 445` 확인) |
| **밸룬** | ❌ | ✅ |

## 3. 예비 계측 결과 (Claude, 코드 리딩)

### 3-1. BOM 표 = 이식성 높음
`CreateBOMTable` **52줄**, 외부 의존 **3개뿐**:
```
PlantOrthoView.SPInfo.Ids · .CPYDesignStd · .AttachmentList
```
- MLeader 미사용. 표는 선·문자 작도라 무탭의 페이퍼공간 직접 작도와 충돌하지 않는다.
- 세 값 모두 무탭에도 대응물이 있다(`s_isoDesignStd`, 자동포함 결과 등).

### 3-2. 밸룬 = **그대로 이식 불가**
`CreateAnnotations` **150줄**, `PSUtil.CreateMLeader` **6회** 사용.
밸룬은 `CIRCLE_ST` 블록 + MLeader 조합이다.
```
MLeader mleader = PSUtil.CreateMLeader(trans, bt["CIRCLE_ST"], ml_style_id1, projectedLeaderPoints, str13, ...);
```
**★무탭은 MLeader를 폐기했다**(cycle 79~82 실측 → cycle 83). 재조사 금지 사실:
- `content.Location` 무시 / `SetTextAttachmentType` 양방향 호출해도 렌더 바이트 동일
- `TextLocation` 이동해도 연결 끝단 불변(생성 시점 고정)
→ 무탭은 **MText + 구형 `Leader` 직접 작도**로 전환해 좌표를 전부 자체 계산한다.

따라서 밸룬은 **작도부를 다시 써야 하고, 재사용 가능한 것은 데이터 계층**이다:
- `AttachmentList`(부착 서포트 = 밸룬 대상)
- `StandardSupport.TaggingPoints`(`F1`/`F2`/`P1_0` … 마크 키 ↔ 지시점)
- BOM 행의 `ITEM` 열(= 밸룬 번호). 표와 밸룬은 **짝**이다.

### 3-3. PLN/BOP 콜아웃 = 이식 불필요
무탭에 이미 직접 작도로 존재한다. 오쏘의 MLeader 버전을 가져오면 **퇴행**이다.

## 4. cycle 94 = 계측만. 작도 변경 없음.

### M1. BOM 데이터 원천 대응 실측
무탭 컨텍스트에서 아래가 확보되는지 로그로 확인한다.
- `SPInfo.Ids[0]` ↔ 무탭의 대상 서포트 ObjectId
- `SPInfo.CPYDesignStd` ↔ `s_isoDesignStd`(cycle 93에서 실제 값 사용 확인됨)
- `SPInfo.AttachmentList` ↔ **무탭에 대응물이 있는가가 최대 미지수**.
  자동포함(`AutoIncludeRelatedParts`)이 U-bolt를 잡는 것은 확인됐으나,
  `AttachmentInfo`(부착 서포트 + `PPoints`)와 동일한 정보인지는 미확인.
- **`BOMs`가 원본 DB 컨텍스트를 요구하는지** 확인. `PSUtil`은 Plant 프로젝트·DataLinksManager
  의존이라 **side DB에서 호출 불가**(cycle 91 자문). → 원본 단계에서 **BOM 행을 캡처해 보관**해야 할 수 있다.

### M2. 밸룬 앵커 원천 실측
- `StandardSupport.TaggingPoints`를 무탭 대상 타입(GD1/RC1 등)에서 **실제로 생성해 덤프**.
  키·좌표(WCS)와 `NotabProjectWcsToPaper` 투영 결과를 남긴다.
- BOM 행의 `ITEM`(F1/F2…)과 `TaggingPoints` 키가 **1:1로 맞는지** 대조.
  cycle 91 포트 덤프에서 이미 `S1~S4`를 봤고, 규격집 마크는 `F1~F4`다. **둘의 관계 확인 필요.**

### M3. 표 배치 공간 실측
- 무탭 페이퍼(타이틀블록 포함)에서 BOM 표가 들어갈 **빈 영역 좌표**를 확인.
- 기존 콜아웃 배치기(`NotabCalloutPlacer`)의 허용영역(뷰포트 사각형)과 **겹치는지**.
  겹치면 표를 장애물로 등록해야 한다(안 그러면 콜아웃이 표 위에 놓인다).

## 4-A. 자문(Codex) 반영 — 전제 붕괴 1건 + 정정

### ★전제 붕괴 — "오쏘에 밸룬이 이미 있다"가 F* 기준으로는 미확인
`CreateAnnotations()`의 밸룬 루프는 **`P*`와 `R*` ITEM만 처리하고 `F*` 분기가 없다**
(`OrthoViewportManager.cs:855`). 즉 **부재(F1~F4) 밸룬이 실제로 생성되는지 확인되지 않았다.**
→ 계측에서 **ITEM별 처리 결과와 실제 생성 엔티티를 분리 기록**해야 한다.
"이식하면 된다"가 아니라 "애초에 없을 수도 있다"를 먼저 판정한다.

### 정정 — `S<n> → F<n>` 일반 규칙은 없다
- BOM의 `F1~`은 `FrameBOM()`이 규격/파라미터에서 부여하는 **부재 식별자**.
- `TaggingPoints`의 `F1~`은 타입별 메서드가 **특정 포트 인덱스**로 계산한 밸룬 앵커.
  예) GD2: `F1←PPorts[2]`, `F2←PPorts[1]`, `F3←PPorts[3]`, `F4←PPorts[4]`.
- 무탭이 관측한 `S1~S4`는 Plant 포트의 **Name**일 뿐이다.
→ 관계는 `ITEM F<n> → 타입별 TaggingPoints 규칙 → PPorts[index]`.
  **원본 단계에서 `StandardInformation(ShortDescription)`을 실행해 `TaggingPoints` 자체를
  WCS로 스냅샷**하는 것이 안전하다(포트 이름 매핑을 추측하지 말 것).

### `AttachmentList`는 무탭에 대응물이 없다
팔레트 선택 처리에서만 구성된다(`PaletteTab.Events.cs:171~300`):
`SupportType=ATTACHMENT` 객체의 DataLinks 속성 + `Part.GetPorts()` → `AttachmentInfo`,
`FindNearestSupport()`로 소유 framework 배정 → `R1`/`R2` 명명 + Plant `SupportName` 갱신.
무탭 `AutoIncludeRelatedParts()`는 **ObjectId만 선택 집합에 추가**할 뿐 위 작업을 하지 않는다.
→ 원본 DB 단계에서 캡처할 것: attachment ObjectId·`Item`/`Name`(R1…)·`Description`·`Size`·
`BOP`·`COP`·`LineNumber`·포트 전량 WCS·`PortZero` 후보·소유자 배정 결과,
그리고 **배정 실패/포트 없음/동일 거리 경쟁도 로그**.

### `BOMs`는 원본 Plant 컨텍스트 필수
생성자가 `SupportHelper.GetSupportParameters(id)`를 호출하고,
`StandardSupportContents()`가 `dl_manager.GetProperties()`를 쓴다. `PSUtil` 자체가 Plant 프로젝트·
`DataLinksManager` 의존. → **side DB에서 호출 불가.**
캡처 지점 = `AutoIncludeRelatedParts()` **직후**, `CloneSelectionToSideDatabase()` **직전**.
보관은 `string[]` 대신 **명시 DTO**(framework 6칸 / attachment 9칸 혼합 배열이라 위험):
`Item·Ancillary·Description·Material·QuantityOrLength·Remark` + attachment용 `BOP·COP·LineNumber`.

### BOM 표 좌표는 절대 하드코딩
위치 `(640.5, 84.5, 0)`, 열 폭 `15/30/40/40/30/25`, 행 높이 `8`, 문자 `3`
(`OrthoViewportManager.cs:729`). 전달받은 BTR 공간에 `Table`을 추가하며 BOM은 **첫 뷰에서만** 그린다.
- 무탭 뷰포트 실측이 `(30.5,84.5)~(640.5,573.5)`이므로 **표 시작 x가 뷰포트 우측 끝과 정확히 일치**한다.
  숫자 재사용 가능성은 있으나 **같은 템플릿·가용영역일 때만**이다.
- 표의 실제 `GeometricExtents`를 구해 **`NotabCalloutPlacer` 장애물로 등록**해야 한다.
  안 그러면 콜아웃이 표 위에 놓인다.

### 추가 계측 항목 (자문 권고)
- `TaggingPoints` 키 집합 ↔ BOM `ITEM` 집합의 **차집합**: `BOM-only` / `anchor-only` /
  수량 확장키(`P1_0`)를 타입별로 로그.
- `TaggingPoints`의 **두 점 중 첫 점=지시점, 둘째 점=리더 방향 힌트**.
  무탭 직접 `Leader` 배치기에서 둘째 점을 그대로 쓸지 방향 힌트로만 쓸지 실측.
- attachment `R<n>` 순번이 **선택/열거 순서 의존**이다. 재실행·다중 attachment에서
  순서가 안정적인지 측정해야 BOM 번호와 밸룬 번호가 흔들리지 않는다.
- `NotabProjectWcsToPaper()`는 투영 실패 시 **원점 폴백**이다(`Commands.cs:4578`).
  원점 폴백 건수를 **밸룬 작도 금지 조건**으로 기록.
- BOM 캡처는 `s_iso*` 정적 상태와 같은 수명이라 **배치 처리에서 누출 위험**.
  서포트별 불변 스냅샷을 만들고 다음 대상 시작 시 초기화되었는지 로그로 검증.

## 5. 산출물
- `bom-measure` / `balloon-measure` / `bomtable-space` 로그.
- REPORT에 **이식 가능/불가 판정**과 근거 수치.
- 다음 사이클 설계 입력: 무엇을 재사용하고 무엇을 다시 쓰는지 확정.

## 6. 하지 말 것
- **MLeader 기반 밸룬 코드를 그대로 가져오기** — cycle 79~83에서 폐기한 경로다.
- 오쏘 PLN/BOP 콜아웃 이식 — 무탭에 이미 있다(퇴행).
- 이번 사이클에서 표·밸룬 **작도 착수**. 계측 결과로 설계를 확정한 뒤에 한다.
- 무탭 배치 규칙(cycle 88 R1~R4)·포트 앵커(cycle 92)·표준 판정(cycle 93) 변경.
