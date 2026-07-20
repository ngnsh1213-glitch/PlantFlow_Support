# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 90
- **status**: ready
- **issued_at**: 2026-07-20
- **title**: RC 가로 치수를 SupportParams로 + 세로 MEMBER 앵커 실측화
- **작업 경로**: `d:\PlantFlow\PlantFlow_Support\PlantFlow_Support\Core\Commands.cs`
- **계획서**: `d:\PlantFlow\PlantFlow_Support\.plans\plan_notab_member_identify_20260720.md` (**먼저 정독**)
- **핸드오프 위치**: `d:\PlantFlow\PlantFlow_Support\.plans\HANDOFF.md`

## 왜 이 사이클인가
비율 상수 추정으로 RC1→RC2→RC3에서 **3연속 빗나갔다**. 추정 튜닝을 중단하고
**실측 파라미터 + 실측 부재 박스**로 전환한다. 상수 추가 튜닝은 이번 사이클의 금지 사항이다.

## A. 가로 치수 = SupportParams (기하 아님)

**중요**: "가로 MEMBER 박스 폭 = 450"은 **틀린 전제**다. 가로재와 베이스 플레이트가
한 `Solid3d`라 bbox는 490이다. 450은 기하에서 나오지 않는다.

라이브 `support params dump` 실측:
```
RC1  A=350 A1=100          → A+A1 = 450   (좌 350 / 우 100)
RC2  A=400 A1=50  A2=200   → A+A1 = 450   (좌 250 / 우 200)
RC3  A=250 A1=250 A2=250   → A+A1 = 500   (좌 250 / 우 250)
```
- **총 폭 = `A + A1`**
- **우측 분할 = `A2`**(없으면 `A1`), **좌측 = 총 폭 − 우측**
- 세 타입 모두 사용자 기대치와 일치.

구현 요구:
- `s_isoSupportParams`에서 읽는다(cycle 89에 캡처됨). **InvariantCulture 파싱**.
- **치수 값·분할**은 파라미터, **보조선 위치**는 부재 우측 끝 기준 총 폭만큼 좌측.
  bbox를 그대로 쓰면 숫자만 450이고 보조선은 490 폭에 남는 불일치가 생긴다.
- 키 누락/비유한/0 이하 → **기존 `s_isoRealWidth` 경로 폴백** + 사유 로그.
- 로그에 `dimH source=params(A+A1) | fallback=legacy` 및 사용된 값 전량 기록.

## B. 세로 MEMBER 앵커 실측화

현행 `MemberAnchorSide="vertical"` 추정식은 `minX + barPaperH*0.5 ≈ 293`인데
실제 세로 MEMBER는 `x = 335.6~353.4`다. **40 이상 빗나가며 재추출해도 허공.**

- `NotabMemberGeometrySpike()`를 반환 헬퍼로 승격:
```csharp
private struct NotabMemberBox
{ public ObjectId Id; public string Handle; public Extents3d PaperBox;
  public Vector3d WcsDims; public double Aspect; public double PipeDist; }

private List<NotabMemberBox> CollectNotabMemberBoxes(Transaction tr, Database db, Viewport vp)
```
- **열린 `Entity`/`Solid3d` 참조를 DTO에 보관하지 말 것**(값 타입·ObjectId만).
- **한 트랜잭션 안에서 1회 수집** → `AppendNotabPaperDimensions` → `AppendNotabProfileCallout`로
  전달. 하위 메서드가 ModelSpace를 재순회하면 분류 불일치가 생긴다.
- 현행 `supportExt` 인자는 미사용이므로 제거 가능. **로그(`member-spike`)는 유지**.
- 세로 MEMBER 앵커 = 해당 `PaperBox` 몸통 중앙. **비율 상수식은 폴백 전용으로 강등**.

## C. 분류 규칙 — 임계값 하드코딩 금지
절대 임계값(“종횡비 5 이상이면 세로재”) 대신 **같은 도면 안 후보 간 상대 순위**로 판정한다.
1. `pipeDist` **상대 최솟값** 후보 = 파이프 클램프/부속 → 제외
2. 남은 후보 중 **페이퍼 박스가 가장 세로인** 후보 = 세로 MEMBER
3. 세로 우위가 명확하지 않거나 유일하지 않으면 **식별 실패로 판정**(억지 선택 금지)
- 판정 결과와 근거를 로그에 남긴다.
- **WCS `dx/dy/dz`가 아니라 `PaperBox` 기준**으로 가로/세로를 판정한다(뷰 방향 의존).

## D. GD 회귀 방지 (필수)
- **새 `MemberGeometry` 소비는 RC1/RC2/RC3만 허용**한다.
- GD1/GD2/GD3은 기존 `supportPaperExt` 분기를 **그대로 유지**. 수집 헬퍼는 GD에서 로그만.
- GD2/GD3의 다중 designation·인덱스별 앵커 특수 분기에 **일반 분류 결과를 섞지 말 것**.
- RC에서 아래 중 하나라도 실패하면 **기존 동작으로 완전 폴백**:
  솔리드 수/투영 실패 · 세로 후보 유일성 없음 · 파라미터 누락/불일치 · 비유한·0 이하
- 모든 경로에 `source=member-geometry | fallback=legacy`와 **사유**를 로그.

## E. 치수 오프셋
현행 `offset = txt*1.5`(=12)는 RC3에서 클램프 위를 지난다.
- **상수 상향까지만** 한다(적정값은 라이브로 확정). 실제 렌더 내용 기준 산출은 이번 범위 밖.

## 완료 기준
1. 빌드 성공(`dev_test.bat`은 사용자가 수동 실행).
2. RC1 450(350/100) · RC2 450(250/200) · RC3 500(250/250) — 490 소멸.
3. 치수 보조선이 숫자와 같은 구간에 놓임(450 숫자 + 490 보조선 불일치 없음).
4. L 부재 콜아웃 화살표가 **세로 MEMBER 몸통**에 닿음(허공 0).
5. **GD1/GD2/GD3 회귀 없음** — cycle 88 판정 유지.

## 하지 말 것
- 비율 상수 추가 튜닝(이번 사이클의 존재 이유가 그 폐기다).
- `Solid3d` 면/서브엔티티 순회로 450을 기하에서 뽑아내려는 시도.
  Boolean union 이후 생성 이력이 없어 RC2 한 사례용 휴리스틱이 된다(자문 지적).
- cycle 88 좌우 규칙 R1~R4 변경.
- 세로 치수 `param(F2)` 로직 변경(RC2 raw=500, RC3 raw=600 정상 동작 중).

## 자문 출처
**Codex MCP**(2026-07-20, read-only). A의 전제 정정(bbox≠450), 헬퍼 시그니처·트랜잭션 수명,
상대 순위 분류, GD 폴백 설계, `Solid3d` 순회 비권장 전부 Codex 지적. Gemini 미호출.
