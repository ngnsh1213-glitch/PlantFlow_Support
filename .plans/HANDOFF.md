# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 125
- **status**: ready
- **issued_at**: 2026-07-24
- **title**: 무탭 평면화 뷰포트 ShadePlot=2D Wireframe 시도 (EXPORTLAYOUT 2D 유도, 저확률 스파이크)
- **작업 경로**: `PlantFlow_Support/Core/Commands.cs` (RunNotabFlatten 2084~ 큐잉 직전 / TrySetViewportShadePlotHidden 5766 패턴 일반화)
- **계획서**: `.plans/plan_notab_flatten_shadeplot_20260724.md`
- **기준 커밋**: `f53e9c0`
- **자문**: Codex(§9). 채택 = ①2D Wireframe=`ShadePlotType.Wireframe`(정확 매칭, VisualStyle 아님) ②큐잉 직전 활성 뷰포트 ForWrite+Commit로 충분(EXPORTLAYOUT이 메모리 DB 읽음) ③★저확률 경고(cycle124 기본이 이미 2D 와이어프레임인데 3D 나옴) ④성공판정=3DSOLID 부재 ⑤enum목록·기존 VisualStyleId 로그.

## ⚠ 검증 필수 + 기대치
`dotnet build` 오류 0(빌드·커밋 분리) 없이 커밋 금지. **push 금지**.
★이 시도는 **자문상 저확률**(cycle124 기본 뷰포트가 이미 2D 와이어프레임인데 3D 솔리드로 나옴). 사용자 지정이라 1회 시도. 실패 시 요구를 Hidden으로 바꾸지 말고 FLATTEN 폴백(별도 사이클).

## 집도 항목
1. **RunNotabFlatten**: 뷰포트 탐색(TryFindNotabFlattenViewport) 직후 ~ EXPORTLAYOUT SendStringToExecute 큐잉 **직전**에,
   활성 doc 트랜잭션으로 상세 뷰포트 ForWrite → `ShadePlot = ShadePlotType.Wireframe` 설정 → Commit.
2. **설정 헬퍼**: 기존 `TrySetViewportShadePlotHidden`(5766) 리플렉션 패턴 일반화 or 신규 — enum 이름 **"Wireframe" 정확 매칭**(부분문자열 금지).
   로그: 설정 성공/실패 + ShadePlot enum 전체 목록 + 뷰포트 현재 VisualStyleId(과거 PFS_NOTAB_USE_HIDDEN 잔재 판별).
   `PFSNOTABFLATTEN3 shadeplot set=<T/F> value=Wireframe enums=[…] vsId=…`.
3. 나머지 EXPORTLAYOUT 체인(cycle124)은 불변. FIN 카운트 로그 그대로.

## 검증 레시피
- RC1-001_notab.dwg 수동 오픈 → `PFSNOTABFLATTEN` → 체인 → `RC1-001_notab_flat.dwg`.
- ★`_flat.dwg`를 열어 **3DORBIT/각도 틀어** 확인: 서포트가 2D 선요소(3DSOLID 부재)인가. 선택 시 Properties가 Line/Polyline 등.
- 로그 `shadeplot set=T value=Wireframe` + FIN gate=GO.
- **판정**: 3DSOLID 없으면 성공(트랙 진전) / 여전히 3D면 실패 → REPORT에 명시, FLATTEN 폴백 사이클로.
