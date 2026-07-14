# REPORT — Codex → Claude

- cycle: 31
- status: done
- commit: created (current HEAD)
- target: `PlantFlow_Support/Core/Commands.cs`
- build: not_run (빌드 수동 원칙)

## 변경 요약
- `PFSNOTABDETAIL` 저장 RECOVER 근인 판별용 계측 추가.
  - `CopyCleanNotabSolids(...)`에서 `src`/`clean` persistent reactor 개수를 복제 직후 기록.
  - 로그: `PFSNOTABDETAIL recover-diag reactors src=Ns clean=Nc id=<id>`
  - `detailDb.NamedObjectsDictionary` 최상위 키를 pre-title 저장 전/최종 SaveAs 직전에 기록.
  - 로그: `PFSNOTABDETAIL recover-diag nod=[...]`
- 타이틀블록 clone 전 바이섹션 저장 추가.
  - 솔리드+레이아웃 커밋 후 `<savedPath>.pretitle.dwg`로 임시 SaveAs를 시도하고 삭제.
  - 로그: `PFSNOTABDETAIL recover-diag preTitleSave=ok|warn:<msg>`
  - 원 산출물 경로는 유지.
- H0 대비 저비용 F0 반영.
  - `TryStripCleanSolidMetadata(...)`에서 persistent reactor를 순회 제거.
  - 개별 제거 실패도 `FileDiag`로 남김.
  - 로그: `PFSNOTABDETAIL cleanSolid reactors removed=N id=<id>`
- cycle 30의 audit reflection 제거.
  - `TryAuditNotabDetailDatabase(...)` 호출 삭제.
  - `TryAuditNotabDetailDatabase(...)`, `GetReflectedPropertyValue(...)` 함수 삭제.

## 산출 파일
- 수정: `PlantFlow_Support/Core/Commands.cs`
- 수정: `.plans/REPORT.md`
- 백업: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_recover_diag`

## 검증
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS (CRLF 경고만 출력)
- `rg recover-diag/cleanSolid reactors removed`: PASS
- `rg TryAuditNotabDetailDatabase/GetReflectedPropertyValue`: no matches
- 주변 코드 확인:
  - `CreateNotabDetailDrawing` 흐름은 솔리드+레이아웃 커밋 → pre-title 저장 계측 → 타이틀블록/뷰포트 커밋 → 최종 SaveAs.
  - 기존 `PFSVBISO*`, `CreateIsoDetailDrawing` 직접 변경 없음(diff 범위 밖).
- 빈 catch 추가 없음. 새 catch는 모두 `FileDiag` 기록.
- 빌드/Plant3D 실행 검증은 지시대로 수행하지 않음.

## 라이브 검증
1. 수동 빌드 후 `PFSNOTABDETAIL` 실행.
2. 기대 로그:
   - `PFSNOTABDETAIL recover-diag reactors src=... clean=...`
   - `PFSNOTABDETAIL cleanSolid reactors removed=...`
   - `PFSNOTABDETAIL recover-diag nod=[...]`
   - `PFSNOTABDETAIL recover-diag preTitleSave=...`
   - `PFSNOTABDETAIL saved path=..._notab.dwg`
3. RECOVER 경고 소멸 여부 확인.
   - 소멸: H0(persistent reactor) 확정 가능성 높음.
   - 잔존: `preTitleSave`/`nod` 로그로 H2(타이틀블록 clone/딕셔너리 반입) 재판정.
