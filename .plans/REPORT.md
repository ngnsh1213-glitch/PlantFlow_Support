# REPORT — Codex → Claude

- cycle: 27
- status: done
- commit: created (current HEAD)
- target: `PlantFlow_Support/Core/Commands.cs`
- build: not_run (빌드 수동 원칙)

## 변경 요약
- 신규 진단 커맨드 `PFSNOTABBOXVPTEST` 추가.
  - `[CommandMethod("PFSNOTABBOXVPTEST", CommandFlags.Session)]`
  - `ResolveIsoTemplatePath()`로 템플릿 경로 확인.
  - `Database(false, true)`로 템플릿을 열고 `CloseInput(true)` 호출.
  - `WorkingDatabase`를 템플릿 DB로 스왑 후 finally에서 복원.
  - 템플릿 ModelSpace에 순수 `Solid3d.CreateBox(300,300,300)` append.
  - "Title Block" layout에 `Viewport` append.
  - viewport 설정:
    - `ViewDirection = Vector3d.YAxis`
    - `ViewTarget = box extents center`
    - `ViewCenter = (0,0)`
    - `CustomScale = 0.5`
    - `On = true`
    - 기존 `TryApplyHiddenVisualStyle`, `TrySetViewportShadePlotHidden` 호출.
  - commit 전/후 로그:
    - `PFSNOTABBOXVPTEST box+viewport, commit 직전`
    - `PFSNOTABBOXVPTEST commit 완료`
  - `C:\Temp\notab_boxvp_*.dwg`로 SaveAs 후 `saved path=` 로그.
- 기존 `PFSNOTABDETAIL`, `PFSVBISO*`, `CreateIsoDetailDrawing` 경로는 수정하지 않음.

## 산출 파일
- 수정: `PlantFlow_Support/Core/Commands.cs`
- 백업: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_notab_boxvptest`

## 검증
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS (CRLF 경고만 출력)
- `rg PFSNOTABBOXVPTEST/NotabBoxViewportTestCommand/box+viewport/notab_boxvp`: PASS
- 주변 코드 확인: `PFSNOTABBOXTEST` 직후 신규 커맨드만 삽입.
- 빈 catch 없음.
- 빌드/Plant3D 실행 검증은 지시대로 수행하지 않음.

## 커밋 상태
- 커밋 완료: `Add notab box viewport diagnostic`
- stage 대상은 `PlantFlow_Support/Core/Commands.cs`, `.plans/REPORT.md`만 포함.

## 라이브 검증
1. 수동 빌드 후 `PFSNOTABBOXVPTEST` 실행.
2. 기대 로그:
   - `PFSNOTABBOXVPTEST viewport hidden=... shadePlot=...`
   - `PFSNOTABBOXVPTEST box+viewport, commit 직전`
   - `PFSNOTABBOXVPTEST commit 완료`
   - `PFSNOTABBOXVPTEST saved path=C:\Temp\notab_boxvp_*.dwg`
3. `commit 완료` 미출력 크래시이면 원인=뷰포트(3D solid 참조) commit 쪽으로 판정.
4. 정상 저장이면 원인=Plant-explode solid 특유 문제로 분기.
