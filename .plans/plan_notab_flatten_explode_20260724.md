# 계획서 — 무탭 2D 평면화 explode+투영 (cycle 126, 2026-07-24)

## 방향 확정 (포럼 근거)
- FLATSHOT은 .NET 직접호출 불가(대화상자, 포럼 공식 확인). EXPORTLAYOUT wireframe은 3D 솔리드 통합(실측).
- ★사용자 요구 = **2D Wireframe(전 에지, 은선제거 없음)** → HLR API 불요. 포럼 대안: "재귀 explode로 곡선 획득(은선 판별 안 됨)" = 전 에지 = 요구와 정확히 일치.
- **결론: Solid3d 에지를 뽑아 뷰 평면에 투영 = 전 에지 2D. 순수 API(명령·대화상자 0), 헤드리스 side-DB 인라인 가능.**

## 설계 — RunNotabDetailPipeline 인라인 (헤드리스)
위치: 주석 작도(AppendNotabPaperDimensions 1959) 후 ~ SaveAs(1971) 전. 플래그 `PFS_NOTAB_FLATTEN=1`(재활용).
1. detailDb 모델공간 각 Solid3d:
   - **Brep(Autodesk.AutoCAD.BoundaryRepresentation)로 전 에지 열거** → 각 Edge의 Curve3d → **샘플점 tessellate**(GetSamplePoints류).
   - 각 점을 `NotabProjectWcsToPaper(vp, pt)`(3318)로 **WCS→paper 투영** → 레이아웃(페이퍼)에 **Polyline** 작도(PFS_ISO_DETAIL 레이어).
     (원/호도 tessellate 폴리라인화 = 곡선타입별 투영 수학 회피, 범용.)
2. 모델공간 Solid3d **Erase**(3D 제거, 2D로 대체됨).
3. **뷰포트 Erase**.
4. SaveAs → 결과 = 페이퍼공간에 2D 폴리라인 와이어프레임 + 주석 + 타이틀블록. 3D·뷰포트 없음.

## 핵심 이점
- 헤드리스(editor 명령 0)라 side-DB에서 바로 됨 — cycle122~125의 명령/대화상자/파일잠금 문제 전부 소멸.
- 주석과 동일 변환(NotabProjectWcsToPaper)이라 정렬 자동 보존.
- 별도 PFSNOTABFLATTEN 명령·EXPORTLAYOUT·_flat.dwg 불요 → 그 코드는 폐기(또는 휴면).

## 자문 반영 (Codex 채택)
- **Brep API**: `new Brep(solid)` → `brep.Edges` → `Edge.Curve`(Curve3d) → `Curve3d.GetSamplePoints(lo,hi,chordHeight)` 반환 `PointOnCurve3d[]`. Edge 개별 Dispose 금지(컬렉션 소유), Brep·Curve3d는 Dispose.
- **★참조 추가 필수**: `Brep`는 `AcDbMgd`가 아니라 **`acdbmgdbrep.dll`**. 현 csproj는 `AcDbMgd`만 참조 → **`AcDbMgdBrep` 참조 + `using Autodesk.AutoCAD.BoundaryRepresentation;` 추가**해야 컴파일. (누락 시 type_not_found)
- **chordHeight** = 페이퍼 허용오차 ÷ viewport scale로 환산 + **점 수 상한**(원통 원형 에지 폭증 방지). 직선=2점.
- **삽입 위치(현 파일 기준 정정)**: 치수 작도 **2052 직후 ~ SaveAs 2055 직전**에 `FlattenNotabSolidsToPaper(detailDb, viewportId)` 호출.
  헬퍼 단일 트랜잭션: viewport 읽기 → 모델 Solid3d 수집·Brep 투영 → 레이아웃 BTR에 Polyline 추가 → 전 Solid3d Erase → viewport Erase → Commit.
- **투영**: `NotabProjectWcsToPaper`(3635)는 실패 시 원점 반환 → **`TryNotabProjectWcsToPaper`(3602)로 실패 에지 skip+로그**. viewport Erase 전 투영 완료 필수.
- **작도**: Z=0이라 **2D Polyline**(`AddVertexAt(Point2d)`), PFS_ISO_DETAIL 레이어. 중복점·2점 미만·극소 길이 제거.
- **폴백**: Brep 실패 시에만 explode(Solid3d→Region→Curve). (BRep DLL 회피하나 수명·재귀·곡선처리 취약, 2순위.)

## 스파이크 게이트
- RC1 추출(PFS_NOTAB_FLATTEN=1) → 상세도 열어 **3DORBIT/각도**: 서포트가 2D 폴리라인(3DSOLID·뷰포트 부재)인가. 전 에지 표시. 주석 보존.
- 로그 `PFSNOTABFLATTEN4 solids=… edges=… polylines=… vpErased=… solidErased=…`.

## 폴백
- Brep 에지가 부실하면 explode(Solid3d→Region→Curve) 경로. 그래도 안 되면 FLATTEN 익스프레스 툴.

## 검증
- 상세도 2D 확인·주석 보존·원본 파이프라인 무플래그(0) 회귀 없음. `dotnet build` 오류 0.
