# REPORT — Codex → Claude

- cycle: 30
- status: done
- commit: created (current HEAD)
- target: `PlantFlow_Support/Core/Commands.cs`
- build: not_run (빌드 수동 원칙)

## 변경 요약
- `PFSNOTABDETAIL` 저장 직전 audit 호출 추가.
  - 신규 `TryAuditNotabDetailDatabase(...)`
  - `AuditInfo`/`Database.Audit`는 AutoCAD 버전 차이를 피하려고 reflection으로 호출.
  - 로그: `PFSNOTABDETAIL audit errors=... fixes=...`
- A1 레이아웃 설정 보강.
  - `TryConfigureNotabA1Layout(...)`에서 `PlotSettingsValidator`로 A1 canonical media 탐색 후 적용 시도.
  - 실패 시 `FileDiag` 후 계속.
  - 로그: `PFSNOTABDETAIL A1 media=... plotArea=(0,0)~(841,594)`
- viewport drawing-area를 Layout limits 의존에서 A1 hard 좌표로 전환.
  - `CreateNotabDetailViewport(...)`의 plotArea 기준을 `(0,0)~(841,594)`로 고정.
  - 이전 평범 DB 기본 limits `12x9` 영향 제거.
  - 로그 source는 `A1-hard`.
- 타이틀블록 2D clone 후 기본 리소스 보정 추가.
  - 신규 `NormalizeNotabClonedTitleBlockEntities(...)`
  - cloned entity의 `LayerId`/`LinetypeId`가 무효면 layer `0`, linetype `Continuous`로 보정.
  - 로그: `PFSNOTABDETAIL titleblock clone normalized=N`

## 산출 파일
- 수정: `PlantFlow_Support/Core/Commands.cs`
- 백업: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_notab_a1_audit`

## 검증
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS (CRLF 경고만 출력)
- `rg A1 media/A1-hard/TryAuditNotabDetailDatabase/audit errors/NormalizeNotabClonedTitleBlockEntities/titleblock clone normalized`: PASS
- 주변 코드 확인:
  - `PFSNOTABDETAIL` 저장 경로 유지.
  - 기존 `PFSVBISO*`, `CreateIsoDetailDrawing` 직접 수정 없음.
  - viewport plotArea가 Layout limits를 읽지 않도록 변경 확인.
- 빈 catch 없음.
- 빌드/Plant3D 실행 검증은 지시대로 수행하지 않음.

## 커밋 상태
- 커밋 완료: `Align notab A1 layout and audit save`
- stage 대상은 `PlantFlow_Support/Core/Commands.cs`, `.plans/REPORT.md`만 포함.

## 라이브 검증
1. 수동 빌드 후 `PFSNOTABDETAIL` 실행.
2. 기대 로그:
   - `PFSNOTABDETAIL A1 media=... plotArea=(0,0)~(841,594)`
   - `PFSNOTABDETAIL viewport 1차 생성 완료 ... source=A1-hard`
   - `PFSNOTABDETAIL titleblock clone normalized=N`
   - `PFSNOTABDETAIL audit errors=... fixes=...`
   - `PFSNOTABDETAIL saved path=..._notab.dwg`
3. 저장 RECOVER 경고가 사라지는지 확인.
4. 레이아웃 A1 프레임/은선 Main 배치가 맞는지 확인.
