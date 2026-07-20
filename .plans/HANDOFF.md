# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 93
- **status**: ready
- **issued_at**: 2026-07-20
- **title**: HANTEC → StandardSupport 개명 + DesignStd 외부화
- **작업 경로**: `d:\PlantFlow\PlantFlow_Support\PlantFlow_Support\`
- **계획서**: `d:\PlantFlow\PlantFlow_Support\.plans\plan_standard_support_rename_20260720.md` (**먼저 정독**)
- **핸드오프 위치**: `d:\PlantFlow\PlantFlow_Support\.plans\HANDOFF.md`

## 배경
제품 출하 시 벤더명 노출을 없애고 다중 표준(고객사별)을 받을 수 있게 한다.
출하 카탈로그 `DesignStd = PFS STANDARD`(사용자가 이미 카탈로그·스펙 수정 완료).
사용자 모델에는 `PFS STANDARD`와 `HANTEC`이 **둘 다 존재**한다(테스트 편의).

## A. 식별자 개명 (기계적)
- 클래스 `HANTEC` → `StandardSupport`
- 파일 `Ortho/HANTEC.cs` → `Ortho/StandardSupport.cs` (**csproj 수정 불필요** — SDK 기본 포함)
- `HANTECContents()` → `StandardSupportContents()`
- `OrthoViewportManager.cs`의 **지역변수 `hantec`과 주석**도 함께 개명
- 규모: 식별자 89곳(대부분 `HANTEC.` 정적 호출), 파일별 BOMs 53 · PlantAutoCoding 30 · 나머지 소수

## B. 리터럴 `"HANTEC"` 4곳 — **일괄 치환 금지, 개별 처리**

| 위치 | 현재 | 조치 |
|---|---|---|
| `BOMs.cs:45` | `design_std == "HANTEC"` | **`StandardSupport.IsSupportedStandard(design_std)`로 교체** |
| `OrthoViewportManager.cs:787` | `CPYDesignStd == "HANTEC"` | 동일 교체 (**모델 실제 값과 비교하는 곳**) |
| `Commands.cs:8002` | `ContentsByDesignStd("HANTEC")` | **`s_isoDesignStd`를 넘기도록 변경** |
| `Commands.cs:8025` | `ContentsByDesignStd("HANTEC")` | 동일 |

- ★`ContentsByDesignStd`는 **문자열**을 받고 헬퍼는 **bool**이다. 인자를 헬퍼로 바꾸지 말 것.
  메서드 **내부 비교**를 헬퍼로 교체한다.
- ★`s_isoDesignStd`가 **비어 있으면 기존 동작(지원으로 간주) + 경고 로그**로 폴백.
  무탭(RC/GD) 회귀 방지용 가드다.

## C. 표준 판정 헬퍼
`Ortho/StandardSupport.cs`의 `StandardSupport` 클래스에:
```csharp
internal static bool IsSupportedStandard(string designStd)
```
- 인식 목록 = `PFS STANDARD`, `HANTEC`. **대소문자·앞뒤 공백 무시.**
- 고객사 표준은 **이 목록에 추가만** 하면 되도록 한 곳에 모은다.
- **인식된 값과 판정 결과를 로그로 남긴다**(무음 실패 방지).

## D. ★독립 결함 동시 수정 — 주석 엔진 `StandardName=null`
자문에서 발견: `CreateAnnotations()`는 새 `BOMs` 인스턴스를 만들지만
**`ContentsByDesignStd()`를 호출하지 않은 채** `boMs.StandardName`을 `StandardInformation()`에 넘긴다.
→ 그 값은 **현재 `null`**이고, 객체는 진입하지만 **타입별 `TaggingPoints`가 생성되지 않는다.**
→ **헬퍼만 교체해서는 주석이 복구되지 않는다.**
이미 만든 `bomData` 또는 BOM 생성 결과의 `StandardName`을 **재사용/재조회**하도록 정리할 것.

## E. 건드리지 말 것
- `BOMs.StandardName`의 **값 자체** — 이건 `DesignStd`가 아니라 타입/ShortDescription
  (`RC1`/`GD2`/`RS12`…)이고 `FrameBOM()` 분기·`StandardInformation()` 타입 선택에 쓰인다.
  지원 표준으로 판정된 뒤에만 `StandardSupportContents()`가 실행돼 채워지게 한다.
- `span_table_JIS.json`의 `WELCRON HANTEC` — **출처 데이터**.
- 문서(`SESSION.md`/`TODO.md`/`PROJECT_STATUS.md`/`REFACTORING_GUIDE.md`/`Support/**/*.py`)의
  `HANTEC` 참조 — `[[scan_HANTEC_cs_...]]` 등은 **문서 링크**라 무차별 변경 시 깨진다.
- `PlantFlow_Support_Backup_Stable/` — 백업 폴더.
- 타입 메서드(`RC1()`/`GD1()`…) **구조 분리(인터페이스+구현체)는 하지 않는다** —
  두 번째 표준이 실제로 올 때. 요구를 모르는 상태로 인터페이스를 먼저 깎지 않는다.
- 무탭 배치 규칙(cycle 88 R1~R4)·포트 앵커(cycle 92) 로직.

## 완료 기준
1. 빌드 성공(`dev_test.bat`은 사용자 수동 실행).
2. 무탭 RC1/RC2/RC3 + GD1/GD2/GD3 **회귀 없음**(cycle 92 종결 상태 유지).
3. 오쏘에서 **`PFS STANDARD` 서포트의 BOM·주석이 정상 생성**(현재 깨져 있음).
4. `DesignStd = HANTEC` 기존 객체도 동일 동작.
5. 로그에 인식된 표준 값이 남는다.

## 자문 출처
**Codex MCP**(2026-07-20, read-only) — 리터럴 4곳 데이터 흐름 검증 및 줄번호 정정(8002/8025),
`ContentsByDesignStd` 타입 불일치 지적, `StandardName`≠`DesignStd` 확인, 헬퍼 위치 권고,
**D의 독립 결함 발견**, 문서 링크·출처 데이터 치환 위험. Gemini 미호출.
