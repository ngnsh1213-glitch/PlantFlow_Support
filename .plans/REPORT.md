# REPORT — Codex → Claude

- cycle: 35
- status: done
- commit: created (current HEAD)
- target: `PlantFlow_Support/Core/Commands.cs`
- build: pass (`dotnet build`, 오류 0 / 기존 경고 15)

## 변경 요약
- A1 plot 스킵 조건을 런타임 파일 marker까지 확장.
  - env `PFS_NOTAB_SKIP_A1PLOT=1` 유지.
  - marker 경로: `%TEMP%\pfs_skip_a1plot.flag`
  - marker 존재 시 `TryConfigureNotabA1Layout(...)` 생략.
  - 로그: `PFSNOTABDETAIL A1plot skip(marker)` 또는 `skip(env)`.
- 플롯 설정 진단 추가.
  - 신규 `LogNotabLayoutPlotConfig(Database db)`
  - 각 Layout에 대해 reflection으로 다음 속성 기록:
    - `PlotConfigurationName`
    - `CanonicalMediaName`
    - `CurrentStyleSheet`
    - `PlotSettingsName`
  - 로그: `PFSNOTABDETAIL plotcfg layout=<name> plotter=<...> media=<...> stylesheet=<...> plotSettings=<...>`
- eInvalidKey 진단용 keyscan 추가.
  - 신규 `ScanNotabInvalidKeys(Database db)`
  - NOD, LayoutDictionary, BlockTable, LayerTable, LinetypeTable, TextStyleTable, DimStyleTable을 스캔.
  - 중첩 DBDictionary는 depth 4까지 재귀 스캔.
  - 빈/공백/control char 키를 비정상 키로 기록.
  - 로그: `PFSNOTABDETAIL keyscan dict=<dictName> count=<n> emptyKeys=<n> keys=[...]`
- 기존 Wblock 저장/PnP purge/recover 계측 유지.
  - `wblock-save`
  - `pnp-purge`
  - `recover-diag nod`
  - `recover-diag reactors`

## 산출 파일
- 수정: `PlantFlow_Support/Core/Commands.cs`
- 수정: `.plans/REPORT.md`
- 백업: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_h3_diag`

## 검증
- `pwd`: `D:\PlantFlow\PlantFlow_Support`
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS (CRLF 경고만 출력)
- `rg pfs_skip_a1plot.flag/plotcfg/keyscan/PFS_NOTAB_SKIP_A1PLOT/A1plot skip`: PASS
- `dotnet build`: PASS
  - 오류 0개.
  - 경고 15개는 기존 경고 범주.
- 주변 코드 확인:
  - `CreateNotabDetailDrawing(...)`의 A1 스킵 판정과 저장 직전 진단만 추가.
  - `CreateIsoDetailDrawing`/`PFSVBISO*` 직접 변경 없음.
  - 빈 catch 추가 없음. 새 catch는 모두 `FileDiag` 기록.

## 라이브 검증
1. 1차 정상 실행:
   - `PFSNOTABDETAIL`
   - `plotcfg ...`, `keyscan ...`, `wblock-save ...` 로그 확인.
2. 2차 H3 확인:
   - `%TEMP%\pfs_skip_a1plot.flag` 빈 파일 생성 후 `PFSNOTABDETAIL`.
   - 기대 로그: `PFSNOTABDETAIL A1plot skip(marker)`.
   - RECOVER 경고 소멸 시 H3(A1 plot 설정 dangling) 확정.
   - 경고 잔존 시 `keyscan`/`plotcfg` 로그로 eInvalidKey 원천 재판정.
