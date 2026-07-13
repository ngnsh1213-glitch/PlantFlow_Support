# REPORT — Codex → Claude

- cycle: 24
- status: done_uncommitted
- commit: not_created (git commit requires explicit approval/review)
- target: `PlantFlow_Support/Core/Commands.cs`
- build: not_run (빌드 수동 원칙)

## 변경 요약
- 환경변수 `PFS_NOTAB_SKIP_VIEWPORT=1` 진단 토글 추가.
  - 토글 ON이면 `CreateNotabDetailViewport` 호출을 건너뛰고 `PFSNOTABDETAIL viewport skip(env)` 로그 기록.
  - solids clone + titleblock + SaveAs만 수행하므로, commit 크래시가 viewport 원인인지 solid-into-template 원인인지 분리 가능.
- `PFSNOTABDETAIL` viewport 생성을 2단계로 분리.
  - 1차 트랜잭션: viewport append 후 `CenterPoint/Width/Height/On`만 설정하고 commit.
  - 2차 트랜잭션: 생성된 viewport를 다시 열어 `ViewTarget/ViewDirection/ViewCenter/TwistAngle/CustomScale`과 Hidden/ShowPlot 설정.
- 단계 로그 추가.
  - `PFSNOTABDETAIL viewport 1차 생성 완료 ...`
  - `PFSNOTABDETAIL viewport 2차 view설정 직전`
  - `PFSNOTABDETAIL viewport 2차 view설정 완료`

## 산출 파일
- 수정: `PlantFlow_Support/Core/Commands.cs`
- 백업: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_notab_n2_viewport2phase`

## 검증
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS
- `rg PFS_NOTAB_SKIP_VIEWPORT/viewport 1차/viewport 2차/CreateNotabDetailViewport/ConfigureNotabDetailViewport`: PASS
- 빈 catch 없음.
- 기존 `PFSVBISO*` 체인 직접 수정 없음. `PFSNOTABDETAIL` 관련 함수만 수정.
- 빌드/Plant3D 실행 검증은 지시대로 수행하지 않음.

## 커밋 상태
- 코드 변경과 REPORT 갱신은 완료됐으나 커밋은 수행하지 않음.
- 커밋 시 `PlantFlow_Support/Core/Commands.cs`와 `.plans/REPORT.md`만 stage 대상.

## 라이브 검증
1. 수동 빌드 후 `PFSNOTABDETAIL` 재실행.
2. 기대 로그:
   - `viewport 1차 생성 완료`
   - `commit 직전`
   - `commit 완료`
   - `viewport 2차 view설정 완료`
   - `saveAs 직전`
   - `saved path=...`
3. 여전히 크래시하면 `PFS_NOTAB_SKIP_VIEWPORT=1` 설정 후 Plant3D 재시작, 재실행.
4. skip viewport에서도 `commit 완료` 전 크래시이면 solid-into-template commit 원인으로 분기.
