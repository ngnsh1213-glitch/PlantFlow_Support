# REPORT — Codex → Claude

- cycle: 40
- status: done
- commit: HEAD (최종 해시는 Codex 완료 보고 참조)
- title: #1 페이퍼 초기화면 — 저장 후 재열기 방식

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - cycle39의 빌드 DB `TryApplyNotabPaperInit(detailDb)` 호출 제거.
  - `TryApplyNotabPaperInit(Database db)`를 `TryReopenSetNotabPaperSpace(string savedPath)`로 교체.
  - `NotabDetailCommand`에서 `CreateNotabDetailDrawing(ref sourceDb, solidIds)` 반환 직후 `TryReopenSetNotabPaperSpace(savedPath)` 호출.

## reopen 흐름
- 마커: `%TEMP%\pfs_notab_paper.flag`
- 마커 없음: no-op, 로그 없음.
- 마커 있음:
  - `new Database(false, false)`
  - `ReadDwgFile(savedPath, FileShare.ReadWrite, true, null)`
  - `CloseInput(true)`
  - `TileMode = false`
  - `SaveAs(savedPath, DwgVersion.Current)`
  - `Dispose()` in `finally`
- 성공 로그: `PFSNOTABDETAIL paper-reopen tilemode=0 ok`
- 실패 로그: `PFSNOTABDETAIL paper-reopen 예외: <...>`

## 보존 확인
- `CreateNotabDetailDrawing` 내부 저장 전에는 더 이상 `TileMode=false`를 적용하지 않음.
- reopen helper 내부에서 `HostApplicationServices.WorkingDatabase` 미변경.
- A/B/C/E 유지: fixedRect viewport, supportExt target, scale 1:4, sourceDb 조기 Dispose 유지.
- `CreateIsoDetailDrawing` / `PFSVBISO*` / `PFSNOTABBOX*` 집도 없음.

## 검증
- 백업 생성: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_cycle40`
- `dotnet build`: PASS, 오류 0 / 기존 경고 15
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS, CRLF 경고만 있음
- `rg "TryApplyNotabPaperInit|paper-init"`: 잔존 없음

## 라이브 확인 포인트
- 1차: 마커 없이 `PFSNOTABDETAIL` 실행, `paper-reopen` 로그 없음 + 크래시/RECOVER 없음 확인.
- 2차: `%TEMP%\pfs_notab_paper.flag` 생성 후 `PFSNOTABDETAIL` 실행, `paper-reopen tilemode=0 ok` 로그와 크래시 없음 확인.
- 저장 파일을 열 때 페이퍼 공간(Title Block)으로 뜨는지 확인.
