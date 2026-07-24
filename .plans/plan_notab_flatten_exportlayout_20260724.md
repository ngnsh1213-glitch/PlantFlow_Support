# 계획서 — 무탭 2D 평면화 EXPORTLAYOUT 재설계 (cycle 124, 2026-07-24)

## 선행 실측 (FLATSHOT 사망)
- cycle122/123: `editor.Command("-FLATSHOT")` → `eInvalidInput`. 원인 확정(사용자 육안): **FLATSHOT은 설정 대화상자 명령**, `-FLATSHOT`도 대화상자 뜸(FILEDIA로 억제 불가). `editor.Command`가 모달 대화상자 구동 불가 → 자동화 원천 불가. **FLATSHOT 트랙 종결.**
- FLATSHOT은 Model space 전용도 확인("Command only valid in Model space").

## 메커니즘 전환 — EXPORTLAYOUT (스크립트 가능)
- **EXPORTLAYOUT은 파일 대화상자**라 `FILEDIA=0`으로 억제 가능. 기존 자산 `PFSEXPORTLAYOUT`(Commands.Export.cs:237)이
  `doc.SendStringToExecute("_.FILEDIA\n0\n_.EXPORTLAYOUT\n<path>\n_.FILEDIA\n1\n")`로 실증. `PFSREADEXPORT`(265)가 결과 2D 판독.
- EXPORTLAYOUT = **현재 레이아웃(뷰포트+주석+타이틀블록)을 새 DWG 모델공간에 2D로 통째 평면화.** 뷰포트→2D 블록, 주석·좌표 보존.
  → ★DCS→paper 아핀 변환 불요(cycle122/123 정렬 리스크 소멸). 멀티뷰 시 레이아웃의 3뷰포트가 한 번에 평면화되는 이점.

## 설계 — 지연 명령 체인 (SendStringToExecute) — ★기존 iso 체인 미러 (Commands.cs:1536 PFSVBISODONE→1625 PFSVBISOEXPORTED)
EXPORTLAYOUT은 지연 실행이라 단일 명령 내 동기 캡처 불가 → 2단 체인:
1. **PFSNOTABFLATTEN**(현재 활성 상세도): 사전검사(cycle123 재사용) → **GUID 고유 temp 경로**(고정 ExportLayoutPath 금지 — 충돌) →
   **FILEDIA 원값 저장**(static) → `SendStringToExecute("_.FILEDIA\n0\n_.EXPORTLAYOUT\n<temp>\n PFSNOTABFLATTENFIN\n")` 큐.
   ★source Document·sourcePath·outputPath·원 FILEDIA를 **static 작업상태**에 보관(FIN이 참조).
2. **PFSNOTABFLATTENFIN**(`[CommandMethod(CommandFlags.Session)]`, EXPORTLAYOUT 완료 후 실행):
   ★MdiActiveDocument 참조 금지 — static 작업상태의 경로 사용. 완료판정 = `File.Exists(temp)` + `Database.ReadDwgFile` 성공 +
   모델공간 검증(Line/Circle/Arc>0, 뷰포트 0). GO면 temp를 **`<원본>_flat.dwg`로 저장**(원본 비파괴). **FILEDIA 원값 복구**. cleanup·카운트 로그.

## 확정 (자문 반영)
- **출력 위치**: 1차 스파이크 = **`<원본>_flat.dwg` 별도 저장**(원본 비파괴). 원본 교체·문서전환·clone-back은 스파이크 PASS 후.
- **주석 보존**: EXPORTLAYOUT 판독 코드(PFSREADEXPORT)는 Line/Circle/Arc만 셈 — dims/MLeader/Table "보존"은 **RC1 눈확인 게이트**로 별도 확인.
- **DRM**: Fasoo는 코드로 판정 불가. `File.Exists`만으로 통과 금지 → **`ReadDwgFile` 성공 + `_flat.dwg` 재오픈 육안 확인**을 게이트로.
- **FILEDIA**: 무조건 1 복원 금지(사용자 원값이 0이면 회귀) — 원값 저장·복구.

## 스파이크 범위 (측정)
- 서포트 1개(RC1) 상세도를 활성 오픈 → PFSNOTABFLATTEN → 체인 완료 후 `_flat.dwg` 생성 확인.
- 게이트: ①EXPORTLAYOUT이 eInvalidInput 없이 실행(대화상자 억제 성공) ②결과 모델공간 2D 엔티티>0·뷰포트 0 ③주석(dims/table/balloon) 보존 ④2D 그림이 원본 뷰포트 표시와 형상 일치(눈확인).
- 로그 `PFSNOTABFLATTEN3 stage=… exported=… lines=… circles=… arcs=… vp=… dims=…`.

## 재사용 / 폐기
- 재사용: cycle123 사전검사(TryValidateNotabFlattenInput census)·뷰포트 카운트·NotabFlattenCounts. PFSEXPORTLAYOUT/READEXPORT 패턴.
- 폐기: cycle123 FLATSHOT 본체(SetNotabFlattenTempView/FLATSHOT command/DCS 아핀/DeepClone). FLATSHOT 헬퍼 제거.

## 검증
- RC1 상세도 오픈 → PFSNOTABFLATTEN → 로그 stage/게이트 + `_flat.dwg` 눈확인. `dotnet build` 오류 0.
