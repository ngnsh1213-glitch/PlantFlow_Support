# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 124
- **status**: ready
- **issued_at**: 2026-07-24
- **title**: 무탭 2D 평면화 EXPORTLAYOUT 재설계 (FLATSHOT 폐기 → 지연 명령 체인)
- **작업 경로**: `PlantFlow_Support/Core/Commands.cs` (cycle123 PFSNOTABFLATTEN 1985~ FLATSHOT 본체 대체 + PFSNOTABFLATTENFIN 신설), 참조 `Commands.Export.cs`(237 PFSEXPORTLAYOUT/265 READEXPORT), `Commands.cs`(1536 PFSVBISODONE→1625 PFSVBISOEXPORTED 체인 미러)
- **계획서**: `.plans/plan_notab_flatten_exportlayout_20260724.md`
- **기준 커밋**: `19fb7b9`
- **자문**: Codex(§9). 채택 = ①FLATSHOT은 설정 대화상자라 자동화 불가 확정(폐기) ②EXPORTLAYOUT은 SendStringToExecute+FILEDIA0(실증 경로) ③FIN=CommandFlags.Session+static 작업상태 경로(MdiActiveDocument 금지) ④완료판정=File.Exists+ReadDwgFile+모델공간검증 ⑤GUID 고유 temp ⑥FILEDIA 원값 저장·복구 ⑦_flat.dwg 별도(원본 비파괴) ⑧DRM은 ReadDwgFile+재오픈 게이트.

## 배경 (cycle122/123 실측)
`editor.Command("-FLATSHOT")` → eInvalidInput. FLATSHOT은 억제 불가 설정 대화상자(사용자 육안 확인). FLATSHOT 트랙 종결. cycle123 사전검사·뷰포트 카운트는 재사용, FLATSHOT 본체(SetNotabFlattenTempView/FLATSHOT command/DCS 아핀/DeepClone)는 폐기.

## ⚠ 검증 필수
`dotnet build` 오류 0(빌드·커밋 분리) 없이 커밋 금지. 미실행 시 `status: blocked`. **push 금지**.
스파이크는 `<원본>_flat.dwg` 별도 산출 = 원본 상세도 비파괴가 최우선 안전조건.

## 집도 항목
1. **PFSNOTABFLATTEN 재작성**: cycle123 사전검사(TryValidateNotabFlattenInput census)+뷰포트 존재 확인 유지. FLATSHOT 본체 제거.
   → GUID 고유 temp 경로 산출, FILEDIA 원값 저장 → static 작업상태(sourcePath=활성 doc 경로, outputPath=`<원본>_flat.dwg`, tempPath, origFiledia) 보관 →
   `doc.SendStringToExecute("_.FILEDIA\n0\n_.EXPORTLAYOUT\n" + tempPath + "\nPFSNOTABFLATTENFIN\n", true, false, false)`.
2. **PFSNOTABFLATTENFIN 신설** `[CommandMethod("PFSNOTABFLATTENFIN", CommandFlags.Session)]`: static 작업상태 참조(활성문서 금지). PFSVBISOEXPORTED(1625~) 미러 —
   `File.Exists(tempPath)` → side `Database.ReadDwgFile`(FileShare.Read) → 모델공간 Line/Circle/Arc>0·뷰포트 0 검증 → GO면 tempPath를 outputPath(`_flat.dwg`)로 저장/복사 → FILEDIA 원값 복구 → temp cleanup.
3. **로그**: `PFSNOTABFLATTEN3 stage=queue temp=… ` / `PFSNOTABFLATTEN3 FIN exported=<T/F> lines=… circles=… arcs=… vp=… members=… gate=GO|NOGO output=<path>`.
4. FLATSHOT 헬퍼(SetNotabFlattenTempView/TryFindNotabFlattenBlock/NotabFlattenDcsToPaperExtents/GetNotabFlattenAlignError/TryCloneNotabFlattenBlockToPaper) 제거. NotabFlattenCounts/census/뷰포트 카운트는 유지.

## 검증 레시피
- RC1-001_notab.dwg를 ACAD에서 **수동으로 연다**(활성) → `PFSNOTABFLATTEN` 입력 → 체인 자동 실행 → `RC1-001_notab_flat.dwg` 생성 확인.
- 로그: `PFSNOTABFLATTEN3 …` stage/게이트 + lines/circles/arcs/vp 수치. `_flat.dwg`를 **열어서** 눈확인(뷰포트 소멸·2D 그림·주석[dims/BOM/balloon] 보존·형상 원본과 일치).
- 첫 관문 = EXPORTLAYOUT이 대화상자 없이 실행되고 `_flat.dwg`가 생성되는가(FLATSHOT 벽 우회 실증).
- 원본 RC1-001_notab.dwg는 **불변**(비파괴 확인).
