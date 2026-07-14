# REPORT — Codex → Claude

- cycle: 28
- status: done
- commit: created (current HEAD)
- target: `PlantFlow_Support/Core/Commands.cs`
- build: not_run (빌드 수동 원칙)

## 변경 요약
- 신규 진단 커맨드 `PFSNOTABBOXVPWIRE` 추가.
  - `[CommandMethod("PFSNOTABBOXVPWIRE", CommandFlags.Session)]`
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
    - `VisualStyleId` Hidden 적용 없음.
    - 기존 `TrySetViewportShadePlotHidden`만 호출.
  - commit 전/후 로그:
    - `PFSNOTABBOXVPWIRE viewport viewDir set, hidden=skip, shadePlot=..., commit 직전`
    - `PFSNOTABBOXVPWIRE commit 완료`
  - `C:\Temp\notab_boxvpwire_*.dwg`로 SaveAs 후 `saved path=` 로그.
- 기존 `PFSNOTABDETAIL`, `PFSVBISO*`, `CreateIsoDetailDrawing` 경로는 수정하지 않음.

## 산출 파일
- 수정: `PlantFlow_Support/Core/Commands.cs`
- 백업: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_notab_boxvpwire`

## 검증
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS (CRLF 경고만 출력)
- `rg PFSNOTABBOXVPWIRE/NotabBoxViewportWireCommand/notab_boxvpwire/hidden=skip`: PASS
- 주변 코드 확인: `PFSNOTABBOXVPTEST` 직후 신규 커맨드만 삽입.
- 빈 catch 없음.
- 빌드/Plant3D 실행 검증은 지시대로 수행하지 않음.

## 커밋 상태
- 커밋 완료: `Add notab viewport wire diagnostic`
- stage 대상은 `PlantFlow_Support/Core/Commands.cs`, `.plans/REPORT.md`만 포함.

## 라이브 검증
1. 수동 빌드 후 `PFSNOTABBOXVPWIRE` 실행.
2. 기대 로그:
   - `PFSNOTABBOXVPWIRE viewport viewDir set, hidden=skip, shadePlot=..., commit 직전`
   - `PFSNOTABBOXVPWIRE commit 완료`
   - `PFSNOTABBOXVPWIRE saved path=C:\Temp\notab_boxvpwire_*.dwg`
3. `commit 완료` 미출력 크래시이면 원인=템플릿 DB + 3D ViewDirection viewport commit 쪽으로 판정.
4. 정상 저장이면 원인=Hidden visual style 적용 쪽으로 판정.
