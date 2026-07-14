# REPORT — Codex → Claude

- cycle: 34
- status: done
- commit: created (current HEAD)
- target: `PlantFlow_Support/Core/Commands.cs`
- build: pass (`dotnet build`, 오류 0 / 기존 경고 15)

## 변경 요약
- 최종 저장을 `detailDb.SaveAs(...)` 직접 호출에서 Wblock 백스톱 저장으로 전환.
  - 신규 `SaveNotabDetailWithWblockFallback(Database detailDb, string savedPath)`
  - 주 경로: `Database cleanDb = detailDb.Wblock();` 후 `cleanDb.SaveAs(savedPath, DwgVersion.Current)`
  - 실패 시: 로그 후 기존 `detailDb.SaveAs(...)`로 폴백.
  - `cleanDb`는 `finally`에서 Dispose.
  - 로그: `PFSNOTABDETAIL wblock-save cleanDb=ok layouts=<n>`
- A1 plot 설정 스킵 토글 추가.
  - env `PFS_NOTAB_SKIP_A1PLOT=1`이면 `TryConfigureNotabA1Layout(...)` 호출 생략.
  - 로그: `PFSNOTABDETAIL A1plot skip(env)`
- preTitle 바이섹션 저장 제거.
  - `TrySaveNotabPreTitleProbe(...)` 호출 삭제.
  - `TrySaveNotabPreTitleProbe(...)` 함수 삭제.
  - `preTitleSave` 로그 제거.
- 기존 계측과 PNP purge 유지.
  - `recover-diag nod=[...]`
  - `recover-diag reactors src=... clean=...`
  - `TryRemoveNotabPnpDictionary(detailDb)` 및 `pnp-purge` 로그 유지.

## 산출 파일
- 수정: `PlantFlow_Support/Core/Commands.cs`
- 수정: `.plans/REPORT.md`
- 백업: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_wblock_save`

## 검증
- `pwd`: `D:\PlantFlow\PlantFlow_Support`
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS (CRLF 경고만 출력)
- `rg preTitleSave/TrySaveNotabPreTitleProbe`: no matches
- `rg PFS_NOTAB_SKIP_A1PLOT/A1plot skip/wblock-save/SaveNotabDetailWithWblockFallback`: PASS
- `dotnet build`: PASS
  - 오류 0개.
  - 경고 15개는 기존 경고 범주.
- 주변 코드 확인:
  - `CreateNotabDetailDrawing(...)` 저장 경로만 Wblock 백스톱으로 변경.
  - `CreateIsoDetailDrawing`/`PFSVBISO*` 직접 변경 없음.
  - 빈 catch 추가 없음. 새 catch는 모두 `FileDiag` 기록.

## 라이브 검증
1. 1차 정상 실행:
   - `PFSNOTABDETAIL`
   - 기대 로그: `PFSNOTABDETAIL wblock-save cleanDb=ok layouts=<n>`
   - RECOVER 경고가 1회만 뜨는지 또는 소멸하는지 확인.
2. 2차 H3 확인:
   - `PFS_NOTAB_SKIP_A1PLOT=1` 설정 후 `PFSNOTABDETAIL`
   - 경고 소멸 시 H3(A1 plot 설정 dangling) 확정.
   - 경고 잔존 시 솔리드/base DB modeler 계열로 별도 진단.
