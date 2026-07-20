# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 89
- **status**: ready
- **issued_at**: 2026-07-20
- **title**: RC 패밀리 세로 치수(F2→H) + 세로 부재 앵커 (단계 A: 계측 선행)
- **작업 경로**: `d:\PlantFlow\PlantFlow_Support\PlantFlow_Support\Core\Commands.cs`
- **계획서**: `d:\PlantFlow\PlantFlow_Support\.plans\plan_notab_rc_family_20260720.md` (**먼저 정독**)
- **핸드오프 위치**: `d:\PlantFlow\PlantFlow_Support\.plans\HANDOFF.md`

## 확정 사실 (사용자 + 규격집 RC1 시트, 재조사 금지)
- **RC1은 용접 구조** — 세로 `MEMBER "M"`과 가로가 별개 다리(절곡 아님).
- **세로·가로는 같은 프로파일** — BOM 행 1개(`BI=16 → L-65×65×6`)가 **정상**.
  세로 콜아웃은 **같은 designation 재사용**, 앵커만 추가.
- **세로 치수 = 3D `F2` 값을 도면 `H` 자리**(베이스 플레이트 아래면 ~ 가로 부재).
- BOM의 `180x180x6mm`=베이스 플레이트, `(M12)1/2"x125`=앵커 볼트.
  `bom-augment 미매핑` 로그는 **결함 아님**. → **`BeamProfileMap` 확장 금지**(초기 오진단).

## 이번 사이클 = 단계 A (계측·저수준 정비까지). 앵커 추가는 단계 B로 분리.

### A-1. F2 캡처 (선행 게이트 G1)
`dims`는 `CaptureIsoSupportProfile()`(7559 부근) **지역 변수**라 치수 작성 시점
(`AppendNotabPaperDimensions()` 4721)에는 접근 불가.
- `CaptureIsoSupportProfile()`에서 **정적 필드로 보관**(`s_isoSupportParams` 사전 복사 권장).
- 캡처 직후 **키·값 전량을 진단 로그로 덤프**한다(F2 존재·표기·소수점 형식 확인 목적).
- `double.TryParse`는 **`InvariantCulture` 우선**으로 파싱한다(문화권 의존 위험).

### A-2. 세로 치수 모드 `param` 신설
- `NotabTypeConfig`에 `VerticalParamKey` 추가, `VerticalMode = "param"` 분기 신설.
- **텍스트만 바꾸면 안 된다** — 치수선 형상도 함께: 상단 = `minY + F2 * vScale`,
  `AppendNotabPaperDimensionEntity`의 `realValue`도 F2로 전달.
- `0 < F2 <= realH` 검사. 실패 시 **`full` 폴백 + 진단 로그**(무음 실패 금지).
- **F2 기준점 검증**: support extents의 `minY`가 실제 base plate 아래면인지 로그로 확인.
  어긋나면 임의 보정하지 말고 REPORT에 실측값을 적을 것.

### A-3. RC 행 등록
`GetNotabTypeConfig`에 `RC1`/`RC2`/`RC3` 행 추가, `VerticalMode="param"`, `VerticalParamKey="F2"`.
- `PipeCalloutSide`/`HorizontalSide`는 **현 관측값 유지**(RC1=top/bottom, RC2·RC3=top/auto).
  추정으로 바꾸지 말 것 — 라이브 실측 후 다음 사이클에서 확정한다.
- `s_isoShortDesc`가 `RC2`/`RC3`로 정확히 파싱되는지 로그로 확인(타입 판정 원천).

### A-4. 세로 MEMBER 식별 계측 (선행 게이트 G2) — **집도 아님, 로그만**
현행 `member-spike`는 엔티티를 나열만 하고 세로 MEMBER를 식별하지 못한다.
종횡비·높이·파이프 거리만으로 고르면 **파이프·볼트 오인** 위험.
- `NotabMemberGeometrySpike()`에 각 엔티티의 **타입/레이어/색상/원본 대응 등
  식별 가능한 속성을 추가 로깅**한다(paper box는 이미 있음).
- **이번 사이클에서 앵커를 만들지 않는다.** 무엇으로 식별 가능한지 REPORT에 정리만 한다.

## 다음 사이클(B) 예고 — 이번엔 하지 말 것
- 세로 앵커 추가. 구현 시 `designations` 리스트에 **문자열 중복 삽입 금지**
  (`multiDesignation=true`가 되어 다부재 배치 경로로 오진입).
  `List<NotabProfileCallout>{Designation, AnchorKind}`로 분리하고 루프를 callout 루프로 전환.
  env 방향 키는 `M0/M1` 대신 `HORIZONTAL`/`VERTICAL` 의미 기반.
- 가로 치수 `A`/`100`/`30` 규격집 대조.
- cycle 88의 좌우 규칙 R1~R4 변경 — 범위 밖.

## 완료 기준
1. 빌드 성공(빌드까지만. `dev_test.bat`은 사용자가 수동 실행).
2. RC1/RC2/RC3 추출 시 세로 치수 값이 `F2`와 일치하고 `H` 구간에 놓임.
3. `sideMode`가 default가 아닌 RC 행 값으로 찍힘.
4. SupportParams 덤프 로그에 F2가 보이고, member-spike에 식별 속성이 추가됨.
5. GD1/GD2/GD3 회귀 없음(cycle 88 판정 유지).

## 자문 출처
**Codex MCP**(2026-07-20, read-only). G1/G2 및 치수 형상·자료구조·문화권 파싱 지적 전부 Codex.
Gemini 미호출.
