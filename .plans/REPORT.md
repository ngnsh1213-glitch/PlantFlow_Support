# REPORT — Codex → Claude

- cycle: 40
- status: done
- commit: HEAD (최종 해시는 Codex 완료 보고 참조)
- title: #1 페이퍼 초기화면 — reopen side DB 보강

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `TryReopenSetNotabPaperSpace(string savedPath)` 보강.
  - reopen DB 생성자를 `new Database(false, false)`에서 `new Database(false, true)`로 변경.
  - paper-space 상태 저장을 2인자 `SaveAs(savedPath, DwgVersion.Current)`에서 `SaveAs(savedPath, true, DwgVersion.Current, reopenDb.SecurityParameters)`로 변경.

## reopen 흐름
- 마커: `%TEMP%\pfs_notab_paper.flag`
- 마커 없음: no-op, 로그 없음.
- 마커 있음:
  - `new Database(false, true)`
  - `ReadDwgFile(savedPath, FileShare.ReadWrite, true, null)`
  - `CloseInput(true)`
  - `TileMode = false`
  - `SaveAs(savedPath, true, DwgVersion.Current, reopenDb.SecurityParameters)`
  - `Dispose()` in `finally`
- 성공 로그: `PFSNOTABDETAIL paper-reopen tilemode=0 ok`
- 실패 로그: `PFSNOTABDETAIL paper-reopen 예외: <...>`

## 보존 확인
- reopen helper 내부에서 `HostApplicationServices.WorkingDatabase` 미변경.
- `TryApplyNotabPaperInit` / `paper-init` 잔존 없음.
- A/B/C/E 유지: fixedRect viewport, supportExt target, scale 1:4, sourceDb 조기 Dispose 유지.
- `CreateIsoDetailDrawing` / `PFSVBISO*` / `PFSNOTABBOX*` 집도 없음.

## 검증
- 백업 생성: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_cycle40_reopenfix`
- `dotnet build`: PASS, 오류 0 / 기존 경고 15
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS, CRLF 경고만 있음
- `rg "TryApplyNotabPaperInit|paper-init|new Database\\(false, false\\)"`: 잔존 없음
- `rg "reopenDb = new Database\\(false, true\\)|reopenDb.SaveAs\\(savedPath, true, DwgVersion.Current, reopenDb.SecurityParameters\\)"`: 적용 확인

## 라이브 확인 포인트
- 1차: 마커 없이 `PFSNOTABDETAIL` 실행, `paper-reopen` 로그 없음 + 크래시/RECOVER 없음 확인.
- 2차: `%TEMP%\pfs_notab_paper.flag` 생성 후 `PFSNOTABDETAIL` 실행, `paper-reopen tilemode=0 ok` 로그와 크래시 없음 확인.
- 저장 파일을 열 때 페이퍼 공간(Title Block)으로 뜨는지 확인.
