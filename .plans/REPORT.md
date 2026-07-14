# REPORT — Codex → Claude

- cycle: 32
- status: done
- commit: created (current HEAD)
- target: `PlantFlow_Support/Core/Commands.cs`
- build: pass (`dotnet build`, 오류 0 / 기존 경고 15)

## 변경 요약
- `TryStripCleanSolidMetadata(...)`의 미존재 API 호출 제거.
  - 삭제: `solid.RemovePersistentReactor(...)` 기반 reactor 제거 루프.
  - 유지: `solid.GetPersistentReactorIds()` 카운트 계측.
  - 신규 로그: `PFSNOTABDETAIL cleanSolid reactors(strip-skipped API-absent)=<count> id=<solid.ObjectId>`
- cycle 31 계측은 보존.
  - `CopyCleanNotabSolids(...)`의 `recover-diag reactors src=... clean=...` 유지.
  - NOD 덤프 `recover-diag nod=[...]` 유지.
  - `preTitleSave=...` 바이섹션 유지.

## 산출 파일
- 수정: `PlantFlow_Support/Core/Commands.cs`
- 수정: `.plans/REPORT.md`
- 백업: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_reactor_api_fix`

## 검증
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS (CRLF 경고만 출력)
- `rg RemovePersistentReactor`: no matches
- `rg reactors(strip-skipped API-absent)/recover-diag reactors/preTitleSave/recover-diag nod`: PASS
- 주변 코드 확인:
  - `TryStripCleanSolidMetadata(...)`만 실질 수정.
  - `CreateNotabDetailDrawing`/`CopyCleanNotabSolids`의 계측·바이섹션 무손상.
- 빈 catch 추가 없음. 새/기존 catch는 `FileDiag` 기록.
- `dotnet build`: PASS
  - 오류 0개.
  - 경고 15개는 HANDOFF 배경의 기존 경고 범주.

## 라이브 검증
1. `PFSNOTABDETAIL` 실행.
2. 핵심 로그 확인:
   - `PFSNOTABDETAIL recover-diag reactors src=Ns clean=Nc ...`
   - `PFSNOTABDETAIL cleanSolid reactors(strip-skipped API-absent)=...`
   - `PFSNOTABDETAIL recover-diag nod=[...]`
   - `PFSNOTABDETAIL recover-diag preTitleSave=...`
3. 분기:
   - `src Ns > 0`: H0 사실 확정 → 다음 cycle F-A(`detailDb.Wblock()` 계열) 검토.
   - `src Ns = 0`: H0 기각 → NOD/preTitleSave 로그로 H2 판정.
