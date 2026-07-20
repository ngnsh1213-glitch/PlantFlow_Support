# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 91
- **status**: ready
- **issued_at**: 2026-07-20
- **title**: 포트 덤프(기둥 앵커 원천) + 세로 치수 기준 정합 + U-bolt 태그 조사
- **작업 경로**: `d:\PlantFlow\PlantFlow_Support\PlantFlow_Support\Core\Commands.cs`
- **계획서**: `d:\PlantFlow\PlantFlow_Support\.plans\plan_notab_rc_post_and_ubolt_20260720.md` (**먼저 정독**)
- **핸드오프 위치**: `d:\PlantFlow\PlantFlow_Support\.plans\HANDOFF.md`

## 배경 (재조사 금지)
- 세로 기둥은 **독립 솔리드가 아니다**. `7A`가 기둥+가로재+베이스 플레이트를 통째로 덮고,
  `7C`(페이퍼 17.82×17.82 정사각형)는 볼트류다. **기하 분류로는 기둥을 찾을 수 없다.**
- 상세도에는 **2D 선분이 없다**(복제 `Solid3d` + 뷰포트 와이어프레임). 선분 기반 계측 불가.
- `7A` bbox ↔ `A/A1/A2` 역산은 **순환 논증**이라 독립 관측값이 아니다.

## ★A. 포트 덤프 — 기둥 앵커의 진짜 원천 (이번 사이클 핵심, **로그만**)

`HANTEC.RC1()`이 앵커를 bbox가 아니라 **포트**에서 뽑고 있다:
```csharp
// HANTEC.cs:868~888
ConvertPortToPoint(this.PPorts[2]);   // F1
ConvertPortToPoint(this.PPorts[1]);   // F2 ← 세로 부재
ConvertPortToPoint(this.PPorts[3]);   // P1
```
포트는 병합 솔리드와 무관하게 **부재별 부착점을 보유**한다.

요구:
1. **원본 DB**에서 대상 서포트의 **포트 전량 덤프** — 인덱스·이름·WCS 좌표.
   `Part.GetPorts` 경로 존재(메모리 `p5-topology-identification`). `HANTEC.ConvertPortToPoint`도 참고.
2. 각 포트를 `NotabProjectWcsToPaper()`로 투영해 **페이퍼 좌표를 함께** 남긴다.
3. RC1/RC2/RC3에서 `PPorts[1]`(F2)이 실제 기둥 위에 오는지 **REPORT에 수치로 대조**.
4. **앵커 작도는 변경하지 않는다.** 이번엔 덤프·대조까지.
   포트가 기둥을 정확히 지시함이 확인되면 다음 사이클에서 앵커를 포트로 전환한다.

주의: `PSUtil`은 Plant 프로젝트/DataLinksManager 의존이라 **원본 활성 문서에서만** 호출하고
side DB·복제 이후에는 호출하지 말 것(자문 지적).

## B. 세로 치수 기준 정합
`verticalX = minX - offset - dimClear`인데 `minX`가 플레이트 기준(490)이라
부재 기준(450)으로 옮긴 가로 치수와 어긋난다(RC2에서 500 치수가 안 움직인 원인).

- 수정 지점은 `AppendNotabPaperDimensions(...)` **한 곳**이다.
- `dimHSource`에서 파생한 **`dimReferenceMinX` / `dimReferenceSource`를 명시 변수로** 만들고
  `verticalX`, **세로 치수의 두 extension point**, 로그가 모두 같은 값을 쓰게 한다.
- `verticalX`만 바꾸면 보조선이 여전히 다른 기준을 가리킨다(자문 지적) — **불완전 수정 금지**.
- params 경로면 params 기준, legacy 폴백이면 legacy 기준. **기준 혼용 금지.**

## C. U-bolt 태그 조사 (조사·덤프까지)
- 상세도 솔리드는 `TryStripCleanSolidMetadata()`로 XData·확장사전이 제거되므로 **읽을 수 없다.**
  반드시 **원본 DB**에서 캡처한다.
- 덤프 위치 = `AutoIncludeRelatedParts()`의 `isSup` 분기, `result.Add(eid)` 전후.
  원본 트랜잭션 안이고 실제 자동포함된 객체만 대상이다.
- 방법: `PSUtil.GetSupportDimension(eid)` **전량 덤프** + DataLinks는 이름 열거 API가 없으므로
  후보 키 명시 조회(`SupportName`, `ShortDescription`, `PartNumber`, `Tag`, `TagName`,
  `SupportDetail`, `Description`).
- **선행 확인**: auto-include는 클래스명에 `Support`가 있어야 포함한다.
  U-bolt가 그 조건을 못 맞추면 `otherPart`로 빠진다 → **실제 class/type명을 먼저 로그로 확인.**

## 완료 기준
1. 빌드 성공(`dev_test.bat`은 사용자가 수동 실행).
2. RC1/RC2/RC3에서 포트 덤프(WCS+페이퍼)가 남고, `PPorts[1]`과 기둥 위치 대조가 REPORT에 수치로 정리.
3. RC2 세로 치수가 가로와 **같은 기준**으로 이동(보조선 포함).
4. U-bolt의 class/type명과 속성 덤프로 태그 가용성 판정.
5. **GD1/GD2/GD3 회귀 없음.**

## 하지 말 것
- 기둥 앵커에 새 비율 상수 도입(이번 사이클의 존재 이유가 계측이다).
- `Solid3d` 면 순회로 기둥 기하 분리(union 후 이력 없음).
- cycle 88 좌우 규칙 R1~R4, 세로 치수 `param(F2)` 변경.

## 자문 출처
**Codex MCP**(2026-07-20, read-only) — (b) 불가 확인, (a) 순환 논증 위험, B의 최소 변경 지점과
`verticalX`만 고치면 불완전하다는 지적, C의 덤프 위치·API 경로·auto-include 클래스명 게이트,
`PSUtil` 호출 범위 제한. 포트 원천 발견은 Claude(HANTEC.cs 리딩). Gemini 미호출.
