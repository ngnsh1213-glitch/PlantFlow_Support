# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 126
- **status**: ready
- **issued_at**: 2026-07-24
- **title**: 무탭 2D 평면화 explode+투영 (Brep 에지→NotabProjectWcsToPaper→페이퍼 Polyline, 헤드리스 인라인)
- **작업 경로**: `PlantFlow_Support/Core/Commands.cs` (RunNotabDetailPipeline 치수작도 2052 직후~SaveAs 2055 직전 / NotabProjectWcsToPaper 3635 / Try 3602), `PlantFlow_Support.csproj`(AcDbMgdBrep 참조 추가)
- **계획서**: `.plans/plan_notab_flatten_explode_20260724.md`
- **기준 커밋**: `8630cf7`
- **자문**: Codex(§9). 채택 = ①Brep 직접경로 1순위(new Brep(solid)→Edges→Edge.Curve→GetSamplePoints) ②★AcDbMgdBrep.dll 참조 필수(현 csproj는 AcDbMgd만) ③chordHeight=페이퍼오차÷scale+점수상한 ④삽입 2052~2055, 단일 트랜잭션 투영후 Erase ⑤TryNotabProjectWcsToPaper로 실패 skip ⑥2D Polyline Z=0 ⑦explode 폴백 2순위.

## 배경 (cycle122~125 실측 — 전부 우회로 확정)
FLATSHOT=대화상자(.NET 직접호출 불가, 포럼 공식). EXPORTLAYOUT wireframe=3D 솔리드 유지(3DORBIT 확인). 사용자 요구=2D Wireframe(전 에지=은선제거 불요). → **순수 API explode+투영이 정답**(명령·대화상자·파일잠금 0, 헤드리스 side-DB 인라인). cycle122~125 FLATSHOT/EXPORTLAYOUT 코드(PFSNOTABFLATTEN/FIN 등)는 이번에 **폐기**(플래그 뒤 휴면 아님, 실제 제거).

## ⚠ 검증 필수
`dotnet build` 오류 0(빌드·커밋 분리) 없이 커밋 금지. **push 금지**. 기본(PFS_NOTAB_FLATTEN 미설정) 회귀 0 최우선.

## 집도 항목
1. **csproj**: `AcDbMgdBrep`(acdbmgdbrep.dll) 참조 추가.
2. **FlattenNotabSolidsToPaper(detailDb, viewportId)** 신설, RunNotabDetailPipeline 치수작도(2052) 직후·SaveAs(2055) 직전 호출(플래그 `PFS_NOTAB_FLATTEN=1`).
   단일 트랜잭션: viewport 읽기 → 모델공간 Solid3d 목록 → 각 Solid3d `new Brep`→Edges→각 Edge.Curve GetSamplePoints(interval, chordHeight) → 각 점 `TryNotabProjectWcsToPaper`(실패 skip+로그) → 레이아웃 BTR에 2D Polyline(PFS_ISO_DETAIL, 중복/2점미만/극소 제거) → 전 Solid3d Erase → viewport Erase → Commit.
   chordHeight=페이퍼오차(예 0.5)÷GetNotabViewportScale, 점수 상한(예 곡선당 ≤256). Brep/Curve3d Dispose, Edge 개별 Dispose 금지.
3. **폐기**: cycle122~125의 PFSNOTABFLATTEN/PFSNOTABFLATTENFIN/TryFlattenNotabDetailB/EXPORTLAYOUT 체인/FLATSHOT 헬퍼/NotabFlattenExportState 등 flatten 관련 사문화 코드 제거. (census/뷰포트 카운트가 다른 데서 안 쓰이면 함께 정리.)
4. **로그**: `PFSNOTABFLATTEN4 solids=… edges=… pts=… polylines=… projFail=… solidErased=… vpErased=…`.

## 검증 레시피
- dev_test `PFS_NOTAB_FLATTEN=1` + 태그 RC1-001 → 추출 → 상세도 열어 **3DORBIT/각도**: 서포트가 2D Polyline(3DSOLID·뷰포트 부재), 전 에지 표시, 주석(치수/BOM/밸룬) 보존·정렬.
- `PFS_NOTAB_FLATTEN=0`(미설정): 현행(뷰포트+3D) 완전 동일 = 회귀 0.
- 첫 관문 = Brep 에지 투영이 2D Polyline 서포트 형상을 만들어내는가.
