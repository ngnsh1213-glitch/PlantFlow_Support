# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 88
- **status**: ready
- **issued_at**: 2026-07-20
- **title**: 무탭 콜아웃 좌우 규칙 확립 + 이중 TryPlace 단일화
- **작업 경로**: `d:\PlantFlow\PlantFlow_Support\PlantFlow_Support\Core\Commands.cs`
- **계획서**: `d:\PlantFlow\PlantFlow_Support\.plans\plan_notab_callout_side_rule_20260720.md` (**먼저 정독**)
- **핸드오프 위치**: `d:\PlantFlow\PlantFlow_Support\.plans\HANDOFF.md`

## 배경
사용자가 콜아웃 배치 규칙을 확정했다(계획서 §1). 좌우는 결정론, 상하는 탐색, 좌우는 불변.
자문(Codex)에서 현행 구조가 이 규칙을 **구조적으로 보장할 수 없음**이 드러났고 Claude가 실측 확인했다.

## 확정 규칙
- **R1** 라인넘버(B.O.P) 콜아웃의 좌우 = 파이프 중심 X가 **뷰포트 사각형 중앙선**의 어느 쪽인가. 타입 무관.
- **R2** 부재(L-/C-) 콜아웃의 좌우 = 같은 방식, 기준은 **부재 자신의 앵커 X**.
- **R3** 상하는 고정값 없음. 간섭 회피로 탐색.
- **R4** 좌우 절대 불변. 막히면 상하·거리만 조정. 반전 금지.
- 경계(정확히 중앙)는 `referenceX >= centerX` → **우측**으로 결정론 고정.

## 집도 항목

### [선행-치명] A. 이중 TryPlace 단일화
`5198`, `5380`의 `TryPlace` 호출은 이후 `AppendNotabDirectCallout`(5016)이 `5043`에서
다시 호출해 덮어쓴다 → 현재 방향 제어가 **최종 작도에 도달하지 않는다**.
- 5198/5380의 사전 `TryPlace` 및 그에 딸린 방향 계산·env 조회를 **제거**한다.
- 배치는 `AppendNotabDirectCallout` 내 1회로 단일화한다.
- 호출부는 `RequiredSide`(및 필요한 기준 X)를 인자로 전달만 한다.

### [선행-치명] B. 뷰포트 사각형 전달
`NotabCalloutPlacer`의 `_minX/_maxX`는 `supportPaperExt ± 100`(4830)이라 **뷰포트가 아니다**.
- 실제 뷰포트 사각형 extents를 구해 placer에 별도 필드로 전달한다.
- 중앙선 = `(viewportMinX + viewportMaxX) / 2.0`.
- **뷰포트 extents 취득 경로가 불명확하면 임의 추정하지 말고 REPORT에 막힌 지점을 적고 중단할 것.**

### [핵심] C. 좌우를 하드 제약으로 승격
- `TryPlace`에 `RequiredSide { Left, Right }` 도입.
- 후보 좌표 생성을 계획서 §3-3-A 식으로 교체(`sign`/`horizontal`/`textLeft` 고정).
- 좌우 불일치 후보는 `Free()`·비용 계산 **전에** 탈락.
- `PFS_NOTAB_CALLOUT_ANGLE_W` 기본값 0.3 → **0.0**(역할 중복).

### [핵심] D. tier 3단 계단화
`bool checkLeader`를 열거형으로 교체:
```csharp
private enum LeaderCheckScope { All, PlacedCalloutsOnly, None }
```
| tier | 좌우 | 콜아웃끼리 리더 | 외부(치수·서포트·파이프) 리더 |
|---|---|---|---|
| 0 | 고정 | ON | ON |
| 1 | 고정 | **ON** | OFF |
| 2 | 고정 | OFF | OFF |

문자박스 대 문자박스/외부장애물 겹침 검사는 **전 tier 항상 ON**.

### [핵심] E. FAIL 시 무검사 fallback 제거
`5049` 이후 `fallbackLeft` 경로는 장애물 검사 없이 작도해 R4를 깬다.
- tier 2까지 실패하면 **해당 콜아웃 작도를 생략**하고 진단 로그를 남긴다.

### [보조] F. env 노브 정리
`PFS_NOTAB_DIR_<TYPE>_<PIPE|M#>`: 숫자 각도 의미 제거(무시 + deprecated 로그).
값 `L`/`R`만 좌우 강제 오버라이드로 유지.

### [보조] G. 진단 로그
`callout-draw`에 `requiredSide` / `sideSrc(rule|override)` / `referenceX` / `viewportCenterX` /
`tier` / reject 사유별 카운트(`oob`/`box`/`extLeader`/`calloutLeader`)를 추가.

## 완료 기준
1. 빌드 성공(사용자 요청 시 `dev_test.bat`).
2. GD1/GD2/GD3 추출 후 로그 검증:
   - GD2 라인넘버 `requiredSide=right`, GD3 라인넘버 `requiredSide=left`
   - 전 콜아웃 `tier<=1`, 작도 생략 0건
   - `viewportCenterX`가 서포트 중앙이 아닌 실제 뷰포트 중앙값
3. 육안: 콜아웃끼리 간섭 0, 리더의 텍스트 관통 0.

## 회귀 주의
- **GD1은 현재 사용자 OK 판정 상태**다. 좌우 규칙 변경으로 바뀔 수 있으니 반드시 재확인 대상에 포함.
- A(단일화)로 죽어 있던 방향 제어가 처음 살아나므로 세 타입 모두 배치가 크게 달라질 수 있다.

## 자문 출처
전 항목 **Codex MCP**(2026-07-20, read-only). A/B/E 및 계획서 §3-3-A·3-5는 Codex 지적을 Claude가 실측 확인 후 채택.
Gemini 미호출(Codex 단독으로 구조적 결함 3건 확정, 추가 교차검증 불요 판단).
