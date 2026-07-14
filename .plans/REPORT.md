# REPORT — Codex → Claude

- cycle: 33
- status: done
- commit: created (current HEAD)
- target: `PlantFlow_Support/Core/Commands.cs`
- build: pass (`dotnet build`, 오류 0 / 기존 경고 15)

## 변경 요약
- `CreateNotabDetailDrawing(...)` 최종 `SaveAs` 직전에 `Autodesk_PNP` NOD 엔트리 제거 호출 추가.
  - 호출 위치: `ConfigureNotabDetailViewport(...)` 이후, `detailDb.SaveAs(...)` 직전.
  - 호출: `TryRemoveNotabPnpDictionary(detailDb)`
- 신규 `TryRemoveNotabPnpDictionary(Database db)` 추가.
  - `NamedObjectsDictionary`를 `OpenMode.ForWrite`로 열고 `nod.Contains("Autodesk_PNP")` 확인.
  - 존재 시 `nod.GetAt("Autodesk_PNP")` 후 `nod.Remove("Autodesk_PNP")`.
  - 제거된 객체는 가능한 경우 `Erase(true)` 시도. 실패해도 `FileDiag` 기록 후 SaveAs 진행.
  - 로그: `PFSNOTABDETAIL pnp-purge before=<n> removed=<0|1> after=<n>`
- 신규 `CountDictionaryEntries(DBDictionary dictionary)` 추가.
  - pnp purge 전/후 NOD entry 수 계산용.
- 기존 계측 유지.
  - `recover-diag nod=[...]`
  - `recover-diag preTitleSave=...`
  - `recover-diag reactors src=... clean=...`

## 산출 파일
- 수정: `PlantFlow_Support/Core/Commands.cs`
- 수정: `.plans/REPORT.md`
- 백업: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_pnp_purge`

## 검증
- `pwd`: `D:\PlantFlow\PlantFlow_Support`
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS (CRLF 경고만 출력)
- `rg pnp-purge`: PASS
- `rg recover-diag`: PASS
- `dotnet build`: PASS
  - 오류 0개.
  - 경고 15개는 기존 경고 범주.
- 주변 코드 확인:
  - `CreateNotabDetailDrawing(...)`의 최종 저장 직전만 호출 추가.
  - `CreateIsoDetailDrawing`/`PFSVBISO*` 직접 변경 없음.
  - 기존 recover 계측·preTitleSave 바이섹션 유지.
- 빈 catch 추가 없음. 새 catch는 모두 `FileDiag` 기록.

## 라이브 검증
1. 수동 로드 후 `PFSNOTABDETAIL` 실행.
2. 기대 로그:
   - `PFSNOTABDETAIL pnp-purge before=... removed=1 after=...`
   - `PFSNOTABDETAIL saved path=..._notab.dwg`
3. 판정:
   - RECOVER 경고 소멸 → H1(`Autodesk_PNP` side-DB 주입) 근인 제거 완료.
   - RECOVER 경고 잔존 → F-A(`detailDb.Wblock()` fresh DB 저장) 백스톱 검토.
