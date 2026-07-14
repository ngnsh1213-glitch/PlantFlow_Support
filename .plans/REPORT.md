# REPORT — Codex → Claude

- cycle: 39
- status: done
- commit: HEAD (최종 해시는 Codex 완료 보고 참조)
- title: 진단 cleanup + #1 페이퍼 초기화면 안전 재구현

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - RECOVER 해결 후 불필요해진 진단/우회 호출과 함수 정의 제거.
  - 최종 저장을 `detailDb.SaveAs(savedPath, DwgVersion.Current)` 직접 호출로 원복.
  - A1 플롯 스킵 토글 제거 후 `TryConfigureNotabA1Layout` 항상 적용.
  - `%TEMP%\pfs_notab_paper.flag` 존재 시에만 `detailDb.TileMode = false`를 적용하는 `TryApplyNotabPaperInit` 추가.

## 삭제 목록
- `TryRemoveNotabPnpDictionary`
- `NormalizeNotabLayoutPlotters`
- `ScanNotabInvalidKeys`
- `LogNotabLayoutPlotConfig`
- `LogNotabNamedObjectsDictionary`
- `SaveNotabDetailWithWblockFallback`
- `ShouldSkipNotabA1Plot`
- 관련 보조 헬퍼: keyscan/plotcfg/wblock/pnp 카운트/포맷 헬퍼
- RECOVER reactor 진단 로그와 `CountPersistentReactors`

## 보존 확인
- E 유지: `sourceDb.Dispose(); sourceDb = null;` 및 `PFSNOTABDETAIL sourceDb disposed(before-save) ok`.
- A 유지: viewport fixedRect 생성 및 `viewport rect` 로그.
- B 유지: supportExt target 중심 설정 및 `viewport target` 로그.
- C 유지: `CustomScale=0.25`, `scale=1:4` 로그.
- `TryStripCleanSolidMetadata`의 확장딕셔너리/XData strip 본체 유지.
- `CreateIsoDetailDrawing` / `PFSVBISO*` / `PFSNOTABBOX*` 집도 없음.

## #1 토글
- 파일 마커: `%TEMP%\pfs_notab_paper.flag`
- 마커 있음: 저장 직전 `detailDb.TileMode = false` 적용, 로그 `PFSNOTABDETAIL paper-init tilemode=0(marker) ok`.
- 마커 없음: paper-init 미적용, 로그 없음.

## 검증
- 백업 생성: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_cycle39`
- `dotnet build`: PASS, 오류 0 / 기존 경고 15
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS, CRLF 경고만 있음
- cleanup 대상 문자열 `pnp-purge|plotter-normalize|plotcfg|keyscan|recover-diag|wblock-save|pfs_skip_a1plot|PFS_NOTAB_SKIP_A1PLOT`: 잔존 없음

## 라이브 확인 포인트
- 1차: 마커 없이 `PFSNOTABDETAIL` 실행, 크래시/RECOVER 없음과 저장 성공 확인.
- 2차: `%TEMP%\pfs_notab_paper.flag` 생성 후 `paper-init tilemode=0(marker) ok` 로그와 페이퍼 공간 초기 표시 여부 확인.
