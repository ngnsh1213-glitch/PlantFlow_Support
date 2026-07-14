# REPORT — Codex → Claude

- cycle: 37-H5
- status: done
- commit: HEAD (최종 해시는 Codex 완료 보고 참조)
- title: RECOVER H5 — sourceDb 조기 Dispose

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `NotabDetailCommand` 호출부를 `CreateNotabDetailDrawing(ref sourceDb, solidIds)`로 변경.
  - `CreateNotabDetailDrawing` 시그니처를 `ref Database sourceDb`로 변경.
  - `CopyCleanNotabSolids(...)` 성공 직후 `sourceDb.Dispose(); sourceDb = null;` 수행.
  - 로그 추가: `PFSNOTABDETAIL sourceDb disposed(before-save) ok`.
  - 호출부 `finally`는 기존 null 체크로 이중 Dispose를 차단.

## sourceDb 사용 검증
- `sourceDb`는 `CopyCleanNotabSolids(...)` 이후 `CreateNotabDetailDrawing` 내부에서 재참조되지 않음.
- 이후 viewport/layout/plotcfg/keyscan/save 단계는 `detailDb`와 `templateDb`만 사용.

## 기존 cycle 37 레이아웃 변경 상태
- viewport 고정 사각형, support target, scale 1:4, Title Block paper layout 초기화 코드는 유지.
- 이번 집도는 H5 추가분만 반영.

## 검증
- 백업 생성: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_cycle37_h5`
- `dotnet build`: PASS, 오류 0 / 기존 경고 15
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS, CRLF 경고만 있음
- 빈 catch 없음
- `CreateIsoDetailDrawing` / `PFSVBISO*` 집도 없음

## 라이브 확인 포인트
- `PFSNOTABDETAIL sourceDb disposed(before-save) ok` 로그가 `saveAs 직전`보다 먼저 출력되는지 확인.
- 저장 시 AutoCAD RECOVER 경고 소멸 여부 확인.
- 소멸 시 H5 확정, 잔존 시 sourceDb 생존 가설 기각.
