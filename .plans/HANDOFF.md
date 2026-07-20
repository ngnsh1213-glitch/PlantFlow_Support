# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 92
- **status**: ready
- **issued_at**: 2026-07-20
- **title**: RC 세로치수·부재앵커를 포트 S2 기준으로 + U-bolt 태그 콜아웃
- **작업 경로**: `d:\PlantFlow\PlantFlow_Support\PlantFlow_Support\Core\Commands.cs`
- **계획서**: `d:\PlantFlow\PlantFlow_Support\.plans\plan_notab_rc_port_anchor_20260720.md` (**먼저 정독**)
- **핸드오프 위치**: `d:\PlantFlow\PlantFlow_Support\.plans\HANDOFF.md`

## 확보된 실측 (재조사 금지)
```
RC2  S2 paper=(311,268.9) role=F2-candidate   RC3  S2 paper=(342,258.9)
     S1=파이프중심  S3=우측끝                  (S2.y = minY = 기둥 밑동)
```
`x`가 타입마다 다르다(RC2 좌측 25%, RC3 56%) — **비율 상수로는 맞출 수 없던 값**.
`HANTEC.RC1()`의 `PPorts[1]`(F2)과 인덱스 일치.

U-bolt 태그도 취득 확인:
```
ubolt-probe datalinks { SupportName=UB-002 | ShortDescription=UB | Tag=UB-002 }
                      { SupportName=UB-003 | ShortDescription=UB | Tag=UB-003 }
```
RC1-001에 U-bolt **2개**. 읽기 경로는 이미 동작한다(데이터만 없었던 것).

## 사용자 확정 목표 (그림 2장 대조)
| 항목 | 현재 | 목표 |
|---|---|---|
| 세로 500 치수 | 플레이트 좌측 기준, 멀리 떨어짐 | **기둥 바로 옆**, 보조선이 기둥 상·하단에 접촉 |
| L 부재 콜아웃 | 허공(7A 박스 중앙) | **기둥 몸통**에 화살표 접촉 |
| U-bolt | 표기 없음 | **개당 1개씩** 태그 콜아웃 |

## A. 기둥 기하를 포트에서 확정
전달 경로는 **이미 존재한다**(자문 확인). 새 인자 불필요:
- 원본 캡처 = `s_isoSupportPorts`(`NotabSupportPortSnapshot{Index,Name,Wcs}`), `CaptureIsoSupportPorts()`
- 상세 투영 = `LogNotabSupportPortProjection(vp)`가 `AppendNotabPaperDimensions` 진입 전 호출됨
```
postX     = S2.x        postBaseY = S2.y (= minY)
postTopY  = postBaseY + F2 * vScale
```
- `Index==1`뿐 아니라 **`Name=="S2"`도 함께 로그 검증**. 불일치 시 기존 앵커 폴백.
- `AppendNotabPaperDimensions`에서 산출해 `AppendNotabProfileCallout`에 **인자로 전달**.

## B. 세로 치수를 기둥 기준으로 (가로와 **의도적 분리**)
cycle 91에서 가로·세로 기준을 통일했으나, **재는 대상이 다르므로 분리가 맞다**.
되돌림이 아니라 각자 올바른 기준을 갖게 하는 것이다.

변수 의미를 고정할 것(자문 권고):
- `dimReferenceMinX` / `dimReferenceSource` — **가로 전용**(현행 `dimHSource`에서 유도)
- `verticalAnchorX` / `verticalAnchorBaseY` / `verticalAnchorTopY` / `verticalAnchorSource` — **세로 전용**
- `verticalX` = `verticalAnchorX - offset - dimClear` **오직 이것만**
- 현재 세로 extension point가 `dimReferenceMinX`를 쓴다 → **`verticalAnchorX`로 교체**
- 로그는 한 줄에 두 기준을 함께:
  `dimH src=... x=(...) | dimV src=port-S2 x=... baseY=... topY=... lineX=... | fallback=...`

## C. 부재 콜아웃 앵커를 기둥으로
```
anchor = ( postX, postBaseY + (postTopY - postBaseY) * 0.5 )
RC2 → (311, 318.9)    현행 오류 앵커 (335.5,318.9) 대비 x 24 어긋남
```
- `MemberAnchorSide`의 `vertical`/`top`/`bottom` **비율 추정식은 폴백 전용으로 강등**.
- 좌우는 cycle 88 R1~R4 그대로. **변경 금지.**

## D. U-bolt 태그 콜아웃
- **`designations` 리스트에 섞지 말 것.** 그 리스트는 BOM 기반 구조 부재이고
  GD2/GD3 다중 배치 규칙과 결합돼 있다(자문 지적).
- 별도 `NotabUboltSnapshot` 목록을 만든다:
  - 원본 자동포함 분기에서 **`Tag` + 원본 `GeometricExtents` 중심(WCS)** 캡처
    (현 `DumpNotabAutoIncludedSupportMetadata`는 덤프만 하고 저장하지 않음)
  - 상세 단계에서 WCS 중심을 `NotabProjectWcsToPaper`로 투영 → 페이퍼 앵커
  - **포트보다 bbox 중심 우선**(U-bolt 포트의 의미가 실측되지 않았다)
- `AppendNotabUboltCallouts(...)`를 별도로 만들고 스냅샷마다
  `AppendNotabDirectCallout(..., label:"ubolt callout")` **1회씩** 호출.
  이 경로는 성공 시 장애물로 `Commit`하므로 UB-002/003이 서로 회피한다.
- **중복·잡객체 배제**: 빈 `Tag` 제외, `HashSet<Tag>` 중복 제거, `ShortDescription=="UB"` 보조 확인.
- **배치 순서 = 치수 → 파이프 → 부재 → U-bolt**(기존 확정 배치 회귀 최소화).
  U-bolt가 자리를 못 찾으면 `callout-skip`으로 남기고 넘어간다.

## E. 폴백·가드 (자문 지적)
- 포트 미확보·`F2` 누락·`vScale` 이상 → **기존 경로 유지** + `source=port | fallback=사유` 로그.
- **`postBaseY`가 support paper extents를 벗어나거나 `postTopY < postBaseY`면 사용 금지·폴백.**
  (S2의 `y=minY`는 현재 뷰 방향에서만 확인된 사실이다. 회전/반전 뷰 대비.)
- 정적 상태(`s_isoSupportPorts`, U-bolt 목록)는 **캡처 실패 시에도 빈 상태와 사유를 명시 로그**.
- 자동포함 클래스 게이트를 못 통과한 U-bolt는 `skip=class-gate`로 **구분해 로그**
  (다른 카탈로그에서 `otherPart`로 빠질 수 있다).
- GD 계열은 포트 경로를 **소비하지 않는다**(현행 유지).

## 완료 기준
1. 빌드 성공(`dev_test.bat`은 사용자 수동 실행).
2. RC1/RC2/RC3 세로 치수가 기둥 옆에 붙고 보조선이 기둥 상·하단 접촉.
3. L 콜아웃 화살표가 기둥 몸통 접촉(허공 0).
4. RC1-001에 U-bolt 콜아웃 **2개**(UB-002, UB-003), 서로 겹치지 않음.
5. 가로 치수 450/450/500·분할 유지, **GD1/GD2/GD3 회귀 없음**.

## 하지 말 것
- 비율 상수 추가 튜닝. cycle 88 좌우 규칙 R1~R4 변경. `param(F2)` 세로 치수 로직 변경.
- `DesignStd` 외부화·`StandardSupport` 개명 — `TODO.md` 예약 항목이며 이번 범위 밖.

## 자문 출처
**Codex MCP**(2026-07-20, read-only) — 포트 전달 경로 기존 존재 확인, 변수 분리 설계,
U-bolt를 designations에서 분리할 것, bbox 중심 우선, 배치 순서 위험, 정적 상태·뷰 반전 가드.
