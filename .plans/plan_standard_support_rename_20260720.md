# HANTEC → StandardSupport 개명 + DesignStd 외부화 (cycle 93)

## 1. 제품 맥락 (판단 근거, 사용자 확정 2026-07-20)
- Python 서포트 스크립트 + PFS를 **한 패키지로 판매**한다.
- 기본 스탠다드 = HANTEC 규격집을 **이름만 바꿔 `PIPE SUPPORT STANDARD`로 제공**.
- **출하 카탈로그 `DesignStd` = `PFS STANDARD`** (사용자가 이미 카탈로그·스펙 수정 완료, 라이브 확인됨).
- 향후 기업/그룹 요청 시 **회사명으로 표준 추가** → **다중 표준은 확정된 요구**다.

## 2. 현황 실측
```
식별자 HANTEC : 89곳   (거의 전부 `HANTEC.` 정적 호출, +HANTECContents 2)
리터럴 "HANTEC":  4곳
파일: BOMs.cs 53 · PlantAutoCoding.cs 30 · Commands.cs 4 · OrthoViewportManager.cs 4 · HANTEC.cs 2
(PlantFlow_Support_Backup_Stable/ 은 백업이므로 대상 제외)
```

## 3. 두 갈래를 반드시 구분한다

### 3-1. 식별자 = 개명 대상
클래스명 `HANTEC` → `StandardSupport`, 파일명 `HANTEC.cs` → `StandardSupport.cs`,
`HANTECContents()` → `StandardSupportContents()`.
대부분 `HANTEC.DetailProfile(...)` 형태의 정적 호출이라 기계적이다.

### 3-2. 리터럴 `"HANTEC"` = **일괄 치환 금지**
Plant3D 카탈로그의 `DesignStd` **데이터 값**이다. 성격이 둘로 갈린다.

| 위치 | 현재 | 성격 | 조치 |
|---|---|---|---|
| `BOMs.cs:45` | `design_std == "HANTEC"` | 인자와 비교 | **표준 판정 헬퍼로 교체** |
| `OrthoViewportManager.cs:787` | `CPYDesignStd == "HANTEC"` | **모델에서 읽은 실제 값**과 비교 | **표준 판정 헬퍼로 교체 (★가장 중요)** |
| `Commands.cs:7902/7925` | `ContentsByDesignStd("HANTEC")` | 리터럴을 **인자로 전달** | 헬퍼가 참을 반환하는 값으로 통일 |

**★핵심 위험**: `OrthoViewportManager.cs:787`은 모델의 실제 `DesignStd`와 비교한다.
카탈로그가 `PFS STANDARD`로 바뀐 지금 **이미 이 경로는 깨져 있다** —
BOM이 빈 채로 반환되고 주석 엔진 분기도 건너뛴다.
빌드는 통과하고 **런타임에 조용히 비는 형태**라 발견이 늦다.

무탭(현 RC/GD 트랙)은 `ContentsByDesignStd("HANTEC")`처럼 리터럴을 인자로 넘기고
타입 판정도 `ShortDescription`을 쓰므로 **영향 없음**(실측 확인).

## 4. 설계

### 4-1. 표준 판정 단일 지점
```csharp
// 출하 표준명은 PFS STANDARD. 기존 데이터 호환을 위해 HANTEC도 인식한다.
public static bool IsSupportedStandard(string designStd)
```
- 인식 목록에 `PFS STANDARD`, `HANTEC` 둘 다. 대소문자·앞뒤 공백 무시.
- **고객사 표준은 이 목록에 추가만** 하면 되도록 한 곳에 모은다.
- **인식된 값과 판정 결과를 로그로 남긴다**(무음 실패 방지).

### 4-2. 하위 호환 필수
사용자 모델에 **두 값이 모두 존재**한다(사용자 확인: "둘다 있어, 테스트하기 편하게").
`PFS STANDARD`만 인식하면 기존 데이터가 깨진다.

### 4-3. 범위 밖 — 구조 분리는 하지 않는다
`RC1()`/`GD1()` 등 타입 메서드의 표준별 구현 분리(인터페이스+구현체)는
**두 번째 표준이 실제로 올 때**. 요구를 모르는 상태로 인터페이스를 먼저 깎지 않는다.

## 4-4. 자문(Codex) 반영 — 정정 및 추가 발견

**정정 1 — 줄 번호**: `Commands.cs`의 두 지점은 `7902/7925`가 아니라 **`8002`(BOM 스파이크 진단,
`PFS_NOTAB_BOM_SPIKE`)/`8025`(무탭 콜아웃 BOM 보강)** 이다.

**정정 2 — "헬퍼가 참을 반환하는 값으로 통일"은 타입상 성립하지 않는다.**
`ContentsByDesignStd`는 문자열을 받고 헬퍼는 `bool`이다.
→ **메서드 내부 비교를 `IsSupportedStandard(design_std)`로 교체**하는 구조가 맞다.
→ `Commands.cs` 두 곳은 리터럴 대신 **실제 모델 값 `s_isoDesignStd`를 넘기도록** 바꾼다.
  그래야 지원하지 않는 표준이 기본 BOM 엔진을 강제로 타지 않는다.
  **단 `s_isoDesignStd`가 비어 있으면 기존 동작(지원으로 간주) + 경고 로그**로 폴백한다
  (무탭 회귀 방지).

**정정 3 — `BOMs.StandardName`은 `DesignStd`가 아니다.**
서포트의 **타입/ShortDescription**(`RC1`/`GD2`/`RS12` …)이며 `FrameBOM()` 분기와
`StandardInformation()` 타입 선택에 쓰인다. **값 자체를 바꾸면 안 된다.**
지원 표준으로 판정된 뒤에만 `HANTECContents()`(개명 후)가 실행돼 `StandardName`을 채우게 한다.

**헬퍼 위치**: 개명 후 `Ortho/StandardSupport.cs`의 `StandardSupport` 클래스에
`internal static bool IsSupportedStandard(string designStd)`. 이 클래스가 프로파일·BOM 규칙·
주석 앵커 엔진을 소유하므로 지원 `DesignStd` 목록도 함께 두는 것이 응집도가 좋다.
(`BOMs`에 두면 모델 클래스가 표준 정책을 소유하게 되고, `Commands`에 두면 재사용이 어렵다.)

**★추가 발견 — `PFS STANDARD`와 무관한 독립 결함**
`CreateAnnotations()`는 새 `BOMs` 인스턴스를 만들지만 **`ContentsByDesignStd()`를 호출하지 않은 채**
`boMs.StandardName`을 `StandardInformation()`에 넘긴다. 따라서 그 값은 **현재 `null`**이다.
`HANTEC` 객체는 진입하지만 **타입별 `TaggingPoints`가 생성되지 않는다.**
→ 헬퍼만 교체해서는 주석 엔진이 복구되지 않는다. `bomData` 또는 BOM 생성 결과의
`StandardName`을 재사용/재조회하도록 함께 정리해야 한다.

**개명 시 주의(자문)**
- 파일명 변경에 `csproj` 수정은 불필요(SDK 기본 포함 방식).
- `OrthoViewportManager.cs`의 **주석과 지역변수 `hantec`** 도 함께 개명.
- `SESSION.md`/`TODO.md`/`PROJECT_STATUS.md`/`REFACTORING_GUIDE.md`/`Support/**/*.py`의
  문서 링크에 `HANTEC` 참조가 많다. `[[scan_HANTEC_cs_...]]`는 **문서 링크**라 무차별 변경 시 깨진다.
- `span_table_JIS.json`의 `WELCRON HANTEC`은 **출처 데이터**라 변경 대상이 아니다.

## 5. 검증
1. 빌드 성공.
2. 무탭 RC1/RC2/RC3 + GD1/GD2/GD3 **회귀 없음**(cycle 92 종결 상태 유지).
3. **오쏘 경로에서 `PFS STANDARD` 서포트의 BOM·주석이 정상 생성**(현재 깨져 있는 것이 고쳐짐).
4. `DesignStd = HANTEC`인 기존 객체도 동일하게 동작.
5. 로그에 인식된 표준 값이 남는다.

## 6. 하지 말 것
- 리터럴 `"HANTEC"` **일괄 치환**(sed 전량 치환 금지). 4곳을 개별 판단할 것.
- `PlantFlow_Support_Backup_Stable/` 수정.
- 무탭 배치 규칙(cycle 88 R1~R4)·포트 앵커(cycle 92) 로직 변경.
