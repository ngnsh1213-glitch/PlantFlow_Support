# REPORT — Codex → Claude

- cycle: 23
- status: done_uncommitted
- commit: not_created (git commit requires explicit approval/review)
- target: `PlantFlow_Support/Core/Commands.cs`
- build: not_run (빌드 수동 원칙)

## 변경 요약
- `PFSNOTABDETAIL` 크래시 진단용 stage 로그 추가.
  - `PFSNOTABDETAIL commit 직전`
  - `PFSNOTABDETAIL commit 완료`
  - `PFSNOTABDETAIL saveAs 직전 path=...`
  - 기존 `saved path=...` 로그 유지
- `TryApplyHiddenVisualStyle` 매칭을 exact `"Hidden"` 우선으로 변경.
  - 1순위: 이름이 정확히 `"Hidden"`인 visual style
  - 2순위: `"Hidden"` 포함, `"3D"` 미포함
  - 3순위: 기존 포함 매칭 fallback
  - 선택 로그에 `match=exact|fallback-no3d|fallback-any` 기록
- 환경변수 `PFS_NOTAB_SKIP_HIDDEN=1` 진단 토글 추가.
  - 토글 ON이면 `TryApplyHiddenVisualStyle`과 `TrySetViewportShadePlotHidden` 호출을 건너뛰고 `hidden skip(env)` 로그 기록.

## 산출 파일
- 수정: `PlantFlow_Support/Core/Commands.cs`
- 백업: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_notab_n2_crashdiag`

## 검증
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS
- `rg PFS_NOTAB_SKIP_HIDDEN/commit 직전/commit 완료/saveAs 직전/match=exact/fallback-no3d/fallback-any`: PASS
- 기존 `PFSVBISO*` 체인 직접 수정 없음. `PFSNOTABDETAIL` 관련 함수만 수정.
- 빌드/Plant3D 실행 검증은 지시대로 수행하지 않음.

## 커밋 상태
- 코드 변경과 REPORT 갱신은 완료됐으나 커밋은 수행하지 않음.
- 커밋 시 `PlantFlow_Support/Core/Commands.cs`와 `.plans/REPORT.md`만 stage 대상.

## 라이브 검증
1. 수동 빌드 후 `PFSNOTABDETAIL` 재실행.
2. `Hidden visualStyle=... match=...` 로그 확인. 기대 1순위는 `match=exact`.
3. 크래시 시 마지막 로그가 `commit 직전`, `commit 완료`, `saveAs 직전` 중 어디까지 찍혔는지 공유.
4. 계속 크래시하면 `PFS_NOTAB_SKIP_HIDDEN=1` 설정 후 Plant3D 재시작, 재실행해 `hidden skip(env)` 상태에서 크래시 여부 확인.
