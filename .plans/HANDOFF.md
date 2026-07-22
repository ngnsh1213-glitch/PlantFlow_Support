# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 105
- **status**: ready
- **issued_at**: 2026-07-22
- **title**: 무탭 RC4·5·7·8 치수 교정 (가로 A+A1 / 세로 F2 / 세로 삭제 none). RC6·RC9 제외.
- **작업 경로**: `PlantFlow_Support/Core/Commands.cs` (`rcMemberGeometry` 3559행, 세로 dim 블록 ≈3659~3792, `GetNotabTypeConfig` ≈4766행)
- **진단 원장**: `.plans/notab_rc5_9_review_20260722.md`
- **기준 커밋**: `aa70b41`
- **자문**: §9 Codex+Gemini 1회(게이트 얽힘·config 파생 확인). ★단 가로 전제는 사용자 실측으로 정정됨(700→650이 정답).

## ⚠ 검증 필수
`dotnet build` 오류 0(경고 14 초과 금지). 미실행 시 커밋 금지·`status: blocked`. Claude가 REPORT 수령 후 직접 빌드 재확인.

## 배경 (사용자 타입별 verdict + 로그, 재측정 불요)
- 무탭 세로 치수가 부재 단면 F(50/75/100)를 뽑는 결함. RC1~3은 `rcMemberGeometry`+config(param/F2)라 정상.
- **가로도 결함**: RC5 가로 700은 버그, 정답은 `A+A1`=650(사용자 확정). RC1~3과 동일 원인(가로재+플레이트 병합 solid의 bbox 오염). `rcMemberGeometry`가 A+A1 경로(3564) + 세로 S2 앵커(3739)를 함께 켠다.
- 사용자 최종 verdict: **RC4·RC5=세로 수정 / RC7=가로 A/A1 분할+세로 삭제 / RC8=세로 삭제만 / RC6·RC9=제외(무변경).**
- params: RC4(A250 A1250 F2600), RC5(A400 A1250 F2600), RC7(A800 A1150 **F2없음**), RC8(A300 A1150 **F2없음**). RC4~9 전부 S2 포트 보유.

## 집도 지시 (치수 전용 3요소. 밸룬 일절 손대지 말 것)

### 1) `rcMemberGeometry`(3559행)에 RC4·RC5·RC7 추가
현재 `RC1||RC2||RC3` → `RC1||RC2||RC3||RC4||RC5||RC7`. **RC6·RC8·RC9는 넣지 말 것.**
- 효과: RC4/5/7 가로 A+A1 경로(3564) + 세로 S2 앵커(3739) 활성.
- RC4 가로=A+A1=500(현재와 동일, 무변). RC5=650(700에서 교정). RC7=A+A1 분할(현재 splitGuard로 950 단일 → 800/150).

### 2) `GetNotabTypeConfig`(≈4766행)에 행 4개 추가
```csharp
if (RC4) return new NotabTypeConfig { VerticalMode = "param", VerticalParamKey = "F2", PipeCalloutSide = "top", HorizontalSide = "auto", MemberAnchorSide = "vertical" };
if (RC5) return new NotabTypeConfig { VerticalMode = "param", VerticalParamKey = "F2", PipeCalloutSide = "top", HorizontalSide = "auto", MemberAnchorSide = "vertical" };
if (RC7) return new NotabTypeConfig { VerticalMode = "none", PipeCalloutSide = "top", HorizontalSide = "auto" };
if (RC8) return new NotabTypeConfig { VerticalMode = "none", PipeCalloutSide = "top", HorizontalSide = "auto" };
```
(문법은 기존 행과 동일하게 `string.Equals(standardName, "RC4", OrdinalIgnoreCase)` 형태로.)

### 3) 세로 dim 코드에 `VerticalMode="none"` 분기 신규 (세로 미작도)
- 세로 치수 블록(≈3659~3792, 값 계산·S2 앵커·`CreateVerticalDimension`·`AppendNotabPaperDimensionEntity(...,"dimV",...)`)을 **`verticalMode=="none"`이면 통째로 skip**한다.
- 구현: 세로 블록 진입 직전에 `if (string.Equals(verticalMode,"none",OrdinalIgnoreCase)) { FileDiag("PFSNOTABDETAIL dimV skip: mode=none"); }` 후 세로 dim 생성·등록을 건너뛴다(가로 dim·나머지 로직은 정상 진행).
- **RC7은 rcMemberGeometry=true이자 none**이다 — 가로 A/A1은 그리고 세로는 안 그린다. none skip이 S2 앵커 블록보다 우선하도록 배치.

## 하지 말 것
- **밸룬(F1/F2/P1) 일절 변경 금지** — F1·F2 오배치는 TaggingPoint 문제로 별도 후속.
- **RC6·RC9·기존 RC1/2/3·GD1~3 변경 금지.** RC8은 rcMemberGeometry에 넣지 말 것(가로 정상).
- 가로 split 로직·파이프 콜아웃·`TryGetNotabRcHorizontalParams` 변경 금지(config/게이트만).
- 기존 `fheight`/`param`/`pipecenter`/`full` 분기 동작 변경 금지(“none”만 신설).

## 제약
- 빈 catch 금지. 신규 매직넘버 금지. `#nullable disable`면 null 가드.

## 검증 (Codex, 필수)
1. `dotnet build` 오류 0, 경고 14 초과 금지.
2. 커밋 후 `REPORT.md`에 변경 요약·빌드 결과. 라이브는 사용자.

## 성공 기준 (다음 라이브)
- RC4 세로 100→600, 가로 500 유지. RC5 가로 700→650·세로 50→600(`dimV param key=F2`+`dimH source=params(A+A1)`).
- RC7 가로 800/150 분할 표시(`split≠skip`), 세로 치수 없음(`dimV skip: mode=none`). RC8 세로 없음·가로 150/300/450 유지.
- **RC6·RC9·RC1~3·GD1~3 회귀 없음.** 밸룬 위치는 이번엔 불변(후속).
