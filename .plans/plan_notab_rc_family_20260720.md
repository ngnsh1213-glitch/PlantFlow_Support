# 무탭 RC 패밀리 대응 (cycle 89)

## 1. 확정 사실 (사용자 + 규격집 RC1 시트)

- **RC1은 용접 구조**다. 세로 `MEMBER "M"`과 가로 부재가 용접된 별개 다리다(절곡 아님).
- **세로와 가로는 같은 프로파일**이다. BOM 행이 하나(`BI=16 → L-65×65×6`)인 것이 정상이며,
  세로 부재 콜아웃은 **같은 designation을 재사용**하고 앵커만 세로 부재 위에 추가한다.
- **세로 치수는 3D의 `F2` 값을 도면의 `H` 자리**(베이스 플레이트 아래면 ~ 가로 부재)에 넣는다.
- BOM의 `180x180x6mm`은 **베이스 플레이트**, `(M12)1/2"x125`는 **앵커 볼트**다.
  둘 다 형강 매핑표에 없는 것이 정상 → `bom-augment 미매핑` 로그는 **결함이 아니다**.
  (초기 진단에서 각형강관 SHS로 오판했음. `BeamProfileMap` 확장은 **불필요**.)

## 2. 현행 코드 실측

`GetNotabTypeConfig`(5666~5679)
```
GD1 {fheight, top,    bottom}
RC1 {fheight, top,    bottom}          ← 세로 치수가 F2가 아님
GD2 {pipecenter, top, auto, MemberBIs["16","215"]}
GD3 {full,    bottom, auto}
default {fheight, top, auto}           ← RC2/RC3가 여기로 떨어짐
```
- 라이브 로그가 `std=RC2`/`RC3`인데 `sideMode=auto`로 찍힘 → **RC2/RC3 행 부재 확정**.
- `fheight` 모드(4809~4830)는 `s_isoSupportProfileHeight`(=앵글 단면 높이 65)를 세로 치수로 쓴다.
  RC 계열에서는 값(65 vs F2)도 위치(`H` 구간 아님)도 어긋난다.

## 3. 설계

### 3-1. 세로 치수 모드 `param` 신설
`NotabTypeConfig`에 `VerticalParamKey`(예: `"F2"`)를 추가하고 `VerticalMode = "param"`을 신설한다.
- 값 = SupportParams의 해당 키(3D 실측). 표기 텍스트도 이 값.
- 구간 = 베이스 플레이트 아래면(paper minY) ~ 가로 부재 하단. `H` 자리에 대응.
- 키가 없거나 파싱 실패하면 **기존 `full`로 폴백**하고 진단 로그를 남긴다(무음 실패 금지).

### 3-2. RC 행 등록
```
RC1 {param(F2), ...}
RC2 {param(F2), ...}
RC3 {param(F2), ...}
```
`PipeCalloutSide` / `HorizontalSide`는 라이브 관측으로 확정한다(현 RC1=bottom, RC2/RC3=auto 상태).

### 3-3. 세로 부재 앵커 추가
- 부재 콜아웃 앵커를 **가로 1개 + 세로 1개**로 늘린다. designation은 동일(`L-65×65×6`).
- 좌우는 cycle 88의 R2(부재 자신의 앵커 X 기준)를 그대로 따른다 — 규칙 변경 없음.
- 세로 앵커는 세로 부재 span의 중앙 부근. 정확한 비율은 `member-spike` 실측 박스로 산출.

### 3-4. 가로 치수 규격집 대조
규격집 RC1은 `A`(MAX 550) / `100` / `30` 체계다. 현재 라이브는 `split=(350,100)` 등으로 나온다.
**대조만 하고 이번 사이클에서 변경은 보류**한다(세로 치수·콜아웃과 뒤섞지 않는다).

## 3-7. 자문(Codex) 반영 — 선행 게이트 2건

**G1. F2는 치수 작성 시점에 읽을 수 없다.**
`dims` 딕셔너리는 `CaptureIsoSupportProfile()`(7559 부근)의 **지역 변수**이고,
치수를 만드는 `AppendNotabPaperDimensions()`(4721)는 그 이후다.
→ `CaptureIsoSupportProfile()`에서 **정적 필드로 캡처·보관**해야 한다(`s_isoSupportParams` 사전 복사 권장).

**G2. `member-spike`는 세로 MEMBER를 식별하지 못한다.**
현재는 모델 엔티티를 단순 나열만 한다(투영 자체는 `NotabProjectWcsToPaper`로 이미 가능).
종횡비·높이·파이프 거리만으로 고르면 **파이프나 볼트를 오인**할 수 있다.
→ 앵커 구현 **전에**, clone 엔티티의 타입/레이어/색상/원본 대응 중
무엇으로 세로 MEMBER를 식별할 수 있는지 **실측 스파이크 선행**.

### 추가 반영 (Codex 지적)
- **치수 형상도 함께 바꿔야 한다.** 텍스트만 F2로 바꾸면 치수선은 계속 전체 높이를 가리킨다.
  상단 = `minY + F2 * vScale`, `AppendNotabPaperDimensionEntity`의 `realValue`도 F2로 전달.
  `0 < F2 <= realH` 검사 후 실패 시 `full` 폴백 + 로그.
- **designation 리스트에 문자열을 중복 삽입하지 말 것.** `multiDesignation=true`가 되어
  일반 다부재 배치 경로로 잘못 진입한다. `List<NotabProfileCallout>{Designation, AnchorKind}`로
  분리하고 루프를 designation 루프 → callout 루프로 바꾼다.
  env 방향 키도 `M0/M1` 대신 `HORIZONTAL`/`VERTICAL` 의미 기반 권장.
- `double.TryParse` **문화권 의존** — F2가 `.` 소수점이면 한국 문화권에서 파싱 실패 가능.
  `InvariantCulture` 우선 파싱 검토(기존 동일 패턴 포함).
- **F2 기준점 검증 필요** — support extents의 `minY`가 실제 base plate 아래면인지.
  앵커 볼트·베이스 플레이트가 extents에 포함되면 어긋난다.
- `member-spike`는 예외를 삼키고 진행한다. 앵커 추출로 승격 시
  **후보 없음/복수 후보를 명시 로그** 후 가로 앵커만 생성하거나 생략(임의 선택 금지).
- `s_isoShortDesc`가 `RC2`/`RC3`로 정확히 파싱되는지 우선 확인(타입 판정 원천).

## 4. 검증
1. `dev_test.bat`로 RC1/RC2/RC3 추출.
2. 세로 치수 값이 `F2`와 일치하고 `H` 구간에 놓이는지.
3. 부재 콜아웃이 가로·세로 **2개** 나오고 각각 실제 부재를 가리키는지(허공 금지).
4. `sideMode`가 default가 아닌 RC 행 값으로 찍히는지.
5. GD1/GD2/GD3 회귀 없음(cycle 88 판정 유지).

## 5. 하지 말 것
- `BeamProfileMap`에 각형강관/플레이트/볼트 추가 — 오진단이었다.
- cycle 88의 좌우 규칙 R1~R4 변경 — 이번 사이클 범위 밖이다.
