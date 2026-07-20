# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 94
- **status**: ready
- **issued_at**: 2026-07-20
- **title**: 오쏘 BOM·밸룬 자산의 무탭 이식 — **계측 전용**(작도 변경 금지)
- **작업 경로**: `d:\PlantFlow\PlantFlow_Support\PlantFlow_Support\`
- **계획서**: `d:\PlantFlow\PlantFlow_Support\.plans\plan_notab_bom_balloon_measure_20260720.md` (**먼저 정독**)
- **핸드오프 위치**: `d:\PlantFlow\PlantFlow_Support\.plans\HANDOFF.md`

## 이번 사이클의 성격
**계측만 한다. 표·밸룬 작도에 착수하지 않는다.**
RC 트랙에서 추정으로 3연속 빗나간 전례가 있어, 이식 전 의존성을 실측으로 굳힌다.
산출물은 로그와 REPORT의 **이식 가능/불가 판정**이다.

## 재조사 금지 (확정 사실)
- **무탭은 MLeader를 폐기했다**(cycle 79~82 실측 → 83). `content.Location` 무시 /
  `SetTextAttachmentType` 양방향 호출해도 렌더 바이트 동일 / `TextLocation` 이동해도 끝단 불변.
  → 오쏘 밸룬(`CIRCLE_ST` + `PSUtil.CreateMLeader`)은 **그대로 이식 불가**. 데이터 계층만 재사용.
- 오쏘 PLN/BOP 콜아웃은 **이식 불필요** — 무탭에 직접 작도로 이미 있다(가져오면 퇴행).
- `BOMs`는 **원본 Plant 컨텍스트 필수**(`SupportHelper.GetSupportParameters`, `dl_manager.GetProperties`,
  `PSUtil`의 프로젝트/DataLinksManager 의존) → **side DB에서 호출 불가.**

## ★M0. 전제 검증 (최우선)
`CreateAnnotations()`의 밸룬 루프는 **`P*`/`R*` ITEM만 처리하고 `F*` 분기가 없다**
(`OrthoViewportManager.cs:855`).
→ **부재(F1~F4) 밸룬이 실제로 생성되는지부터 판정**한다.
- 오쏘 실행 시 **ITEM별 처리 결과와 실제 생성 엔티티를 분리 기록**.
- "이식하면 된다"가 아니라 **"애초에 없을 수도 있다"**를 먼저 확인한다.
- 없다면 다음 사이클은 이식이 아니라 **신규 설계**가 된다.

## M1. BOM 데이터 캡처 지점 실측
- 캡처 지점 = `AutoIncludeRelatedParts()` **직후**, `CloneSelectionToSideDatabase()` **직전**
  (`Commands.cs:1417` 부근). 그 시점에 BOM 행을 얻을 수 있는지 확인하고 **전량 로그**.
- 보관 형태는 `string[]` 대신 **명시 DTO** 권장(framework 6칸 / attachment 9칸 혼합이라 위험):
  `Item·Ancillary·Description·Material·QuantityOrLength·Remark` + attachment `BOP·COP·LineNumber`.
- `s_isoDesignStd`는 cycle 93에서 실제 값 사용 확인됨 — 그대로 활용.

## M2. `AttachmentList` 대응물 — 무탭엔 없다
팔레트 선택 처리에서만 구성된다(`PaletteTab.Events.cs:171~300`):
`SupportType=ATTACHMENT` + DataLinks 속성 + `Part.GetPorts()` → `AttachmentInfo`,
`FindNearestSupport()`로 소유 framework 배정 → `R1`/`R2` 명명.
무탭 `AutoIncludeRelatedParts()`는 **ObjectId만 추가**한다.
→ **원본 DB 단계에서 캡처 가능한지 실측**하고 로그:
attachment ObjectId·`Item`/`Name`(R1…)·`Description`·`Size`·`BOP`·`COP`·`LineNumber`·
포트 전량 WCS·`PortZero` 후보·소유자 배정 결과.
**배정 실패 / 포트 없음 / 동일 거리 경쟁도 반드시 로그.**
- `R<n>` 순번이 **선택·열거 순서 의존**이다. 재실행/다중 attachment에서 **순서 안정성 측정**
  (흔들리면 BOM 번호와 밸룬 번호가 어긋난다).

## M3. 밸룬 앵커 원천 실측
- **`S<n> → F<n>` 일반 규칙은 코드에 없다.** 관계는
  `ITEM F<n> → 타입별 TaggingPoints 규칙 → PPorts[index]`
  (예: GD2 `F1←PPorts[2]`, `F2←PPorts[1]`, `F3←PPorts[3]`, `F4←PPorts[4]`).
  **포트 이름 매핑을 추측하지 말 것.**
- 원본 단계에서 `StandardSupport.StandardInformation(ShortDescription)`을 실행해
  **`TaggingPoints` 자체를 WCS로 스냅샷**하고, `NotabProjectWcsToPaper` 투영 결과와 함께 로그.
- `TaggingPoints` 키 집합 ↔ BOM `ITEM` 집합의 **차집합**을 타입별로 기록:
  `BOM-only` / `anchor-only` / 수량 확장키(`P1_0`).
- `TaggingPoints`는 **두 점**이다. 첫 점=지시점, 둘째 점=리더 방향 힌트.
  무탭 직접 `Leader`에서 둘째 점을 그대로 쓸지 방향 힌트로만 쓸지 판단 근거를 남길 것.
- `NotabProjectWcsToPaper()`는 투영 실패 시 **원점 폴백**(`Commands.cs:4578`).
  **원점 폴백 건수를 기록** — 밸룬 작도 금지 조건이 된다.

## M4. 표 배치 공간 실측
- `CreateBOMTable`은 좌표가 **절대 하드코딩**: 위치 `(640.5, 84.5, 0)`,
  열 폭 `15/30/40/40/30/25`, 행 높이 `8`, 문자 `3` (`OrthoViewportManager.cs:729`).
- 무탭 뷰포트 실측이 `(30.5,84.5)~(640.5,573.5)`라 **표 시작 x가 뷰포트 우측 끝과 정확히 일치**한다.
  숫자 재사용 가능성은 있으나 **같은 템플릿·가용영역일 때만**이다.
- **행 수별 최종 extents**와 타이틀블록·뷰포트·기존 치수·콜아웃과의 **겹침을 계측**.
- 표는 `NotabCalloutPlacer` **장애물로 등록해야 함**을 전제로 필요한 좌표를 남길 것.

## M5. 상태 수명 검증
BOM/attachment 캡처는 `s_iso*` 정적 상태와 같은 수명이라 **배치 처리(`PFSNOTABBATCH`)에서 누출 위험**.
서포트별 불변 스냅샷을 만들고 **다음 대상 시작 시 초기화되었는지 로그로 검증**.

## 완료 기준
1. 빌드 성공(`dev_test.bat`은 사용자 수동 실행).
2. `bom-measure` / `attachment-measure` / `balloon-measure` / `bomtable-space` 로그 생성.
3. REPORT에 **M0 판정**(F* 밸룬 실재 여부)과 항목별 **이식 가능/불가 + 근거 수치**.
4. **무탭·오쏘 회귀 없음** — 계측 로그 외 동작 변경이 없어야 한다.

## 하지 말 것
- 표·밸룬 **작도 착수**. MLeader 기반 코드 이식. 오쏘 PLN/BOP 콜아웃 이식.
- 무탭 배치 규칙(cycle 88 R1~R4)·포트 앵커(cycle 92)·표준 판정(cycle 93) 변경.

## 자문 출처
**Codex MCP**(2026-07-20, read-only) — M0 전제 붕괴 발견(`F*` 분기 부재), `S<n>→F<n>` 매핑 부재 정정,
`AttachmentList` 구성 경로와 무탭 대응물 부재, `BOMs`의 Plant 컨텍스트 의존과 캡처 지점,
표 좌표 하드코딩·장애물 등록 필요, `R<n>` 순서 의존·원점 폴백·정적 상태 누출 위험. Gemini 미호출.
