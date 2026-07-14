# REPORT — Codex → Claude

- cycle: 36
- status: done
- commit: HEAD (최종 해시는 Codex 완료 보고 참조)
- title: RECOVER 마지막 타깃 시도 — 전 레이아웃 플로터 None 정규화

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `CreateNotabDetailDrawing` 최종 저장 직전 흐름에 `NormalizeNotabLayoutPlotters(detailDb)` 호출 추가.
  - 신규 헬퍼 `NormalizeNotabLayoutPlotters(Database db)` 추가.
  - `db.LayoutDictionaryId`의 모든 `Layout`을 ForWrite로 열고 `PlotSettingsValidator.Current.SetPlotConfigurationName(layout, "None", null)`로 플로터만 `None` 정규화.
  - media/stylesheet은 변경하지 않음.
  - 레이아웃별 로그: `PFSNOTABDETAIL plotter-normalize layout=<name> -> None ok|fail:<msg>`.

## 호출 위치
- `ConfigureNotabDetailViewport(...)` 이후
- `TryRemoveNotabPnpDictionary(detailDb)` 이후
- 최종 `LogNotabLayoutPlotConfig`, `ScanNotabInvalidKeys`, `SaveNotabDetailWithWblockFallback` 이전

## 검증
- 백업 생성: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_cycle36`
- `dotnet build`: PASS, 오류 0 / 기존 경고 15
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS, CRLF 경고만 있음
- 빈 catch 없음
- `CreateIsoDetailDrawing` / `PFSVBISO*` 집도 없음

## 라이브 확인 포인트
- `PFSNOTABDETAIL` 실행 후 `plotter-normalize layout=... -> None ok`가 전체 Layout에 출력되는지 확인.
- 정규화 후 `plotcfg`에서 Model/Layout1/Layout2 모두 `plotter=None`으로 바뀌는지 확인.
- `wblock-save eInvalidKey`와 RECOVER 경고 소멸 여부로 H4 최종 판정.



