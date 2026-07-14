# REPORT — Codex → Claude

- cycle: 38
- status: done
- commit: HEAD (최종 해시는 Codex 완료 보고 참조)
- title: 크래시 회귀 격리 — cycle37 D(TileMode/활성레이아웃) 원복

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `CreateNotabDetailDrawing` 저장 직전의 `SetNotabInitialPaperLayout(detailDb)` 호출 제거.
  - `SetNotabInitialPaperLayout(Database db)` 헬퍼 전체 제거.
  - 제거 범위: `detailDb.TileMode = false`, `Title Block` `TabSelected` 활성화, `layout active=... tilemode=... zoomExtents=...` 로그.

## 보존 확인
- A 유지: `CreateNotabDetailViewport` fixedRect viewport 생성 로그 유지.
- B 유지: `ConfigureNotabDetailViewport` supportExt target 중심 설정 유지.
- C 유지: viewport `CustomScale=0.25`, `scale=1:4` 로그 유지.
- E 유지: `sourceDb.Dispose(); sourceDb = null;` 및 `PFSNOTABDETAIL sourceDb disposed(before-save) ok` 로그 유지.
- 저장 경로는 기존 `SaveNotabDetailWithWblockFallback(detailDb, savedPath)` 그대로 유지.

## 검증
- 백업 생성: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_cycle38`
- `dotnet build`: PASS, 오류 0 / 기존 경고 15
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS, CRLF 경고만 있음
- `rg "SetNotabInitialPaperLayout|layout active|layout tilemode|zoomExtents|TileMode = false"`: D 제거 확인
- `CreateIsoDetailDrawing` / `PFSVBISO*` 집도 없음

## 라이브 확인 포인트
- `PFSNOTABDETAIL layout active=... tilemode=... zoomExtents=...` 로그가 더 이상 나오지 않아야 함.
- `PFSNOTABDETAIL` 실행 시 크래시 소멸 여부 확인.
- 크래시 없이 저장되면 RECOVER 경고 소멸 여부와 A/B/C 레이아웃 정합을 확인.
