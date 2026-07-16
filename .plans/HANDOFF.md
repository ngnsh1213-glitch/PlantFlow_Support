# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 48
- **status**: ready
- **issued_at**: 2026-07-16
- **title**: N3-b/c — 무탭 페이퍼공간 치수 제도(세로 높이 + 가로 총폭/pipeCenter 분할, 실측 mm override)
- **target**: `PlantFlow_Support/Core/Commands.cs` (그 외 무수정)
- **plan**: `<appDataDir>\scratch\plan_pfs_notab_n3_20260714.md` (★갱신 섹션)

## 착수 전
- cwd `D:\PlantFlow\PlantFlow_Support`. 무탭 치수 append 신규 메서드 + 진입점만. 그 외 무수정.
- 빌드 GREEN(MSBuild Debug, 에러0)까지 Codex 확인. 라이브는 사용자.

## 배경 — 투영 검증 완료(cycle47)
cycle47 dim-probe로 서포트의 **페이퍼 좌표**(support-paper rect, pipeCenterX) 정확 산출 검증됨(중심=vpCenter, 크기=실측×배율). 스케일 표준화(1:5 등)+여백도 확정(9893b5b). 이제 그 좌표로 **페이퍼공간에 비연관 치수 제도**.

## 재사용 자산 (신규 최소화)
- 드로잉 구조: `AppendIsoBoundingDimensions`(6039, 특히 6068-6135) = 가로총폭(text=realW)+pipeCenter 분할 left/right+세로(text=realH), 각 `RotatedDimension` + `DimensionText` override + `AppendEntity`. **이 구조를 페이퍼 좌표 인자 버전으로 이식**.
- 투영: `NotabProjectWcsToPaper`(4051), `LogNotabDimProbe`(4007)의 좌표 계산(4017 supportExt 8코너 투영→paperExt, 4034 support center→pipeCenterX). → **`TryComputeNotabDimPaperGeometry(vp, supportExt, out Extents3d paperExt, out double pipeCenterXPaper)`로 분리**(LogNotabDimProbe는 이 헬퍼 호출로 로그 유지).
- 리소스: `EnsureIsoAnnotationResources(detailDb, tr, out layerId, out textStyleId, out dimStyleId)`(5991) = AUTO_DIM 레이어+RMS_85 텍스트+STANDARD dimstyle.
- 레이아웃 BTR: `GetNotabDetailLayoutBlockTableRecordId`(2899) → ForWrite.
- 실측 텍스트값: 전역 `s_isoRealWidth/Height`(CaptureIsoSelectionMetrics 5851 세팅). **fitWidth/fitHeight(fit content)와 혼동 금지 — 텍스트값은 실측 realW/realH.**

## 요구 — AppendNotabPaperDimensions
1. **신규 메서드** `AppendNotabPaperDimensions(Transaction tr, BlockTableRecord layoutBtr, Extents3d supportPaperExt, double pipeCenterXPaper, double realW, double realH, ObjectId dimStyleId, ObjectId layerId)`:
   - `AppendIsoBoundingDimensions` 6068-6135 드로잉 구조 복제하되 **좌표=supportPaperExt(페이퍼)**, **분할=pipeCenterXPaper**, **텍스트 override=realW/realH**(가로 분할은 pipeCenter 비율로 leftReal/rightReal).
   - 가로 총폭(supportPaperExt 상/하 밖), pipeCenter 분할 left/right, 세로 높이(supportPaperExt 좌/우 밖). 각 `DimensionStyle=dimStyleId`, `LayerId=layerId`, `layoutBtr.AppendEntity`+`AddNewlyCreatedDBObject`.
2. **★치수 크기 = 페이퍼 고정(핵심 보정)**: 기존 `ComputeIsoDimensionSize`(=max(realW,realH)/12≈50mm)는 평면화 1:1용 → **페이퍼(1:5) 도형엔 과대**. 대신 **페이퍼 표준 고정 크기**: 문자높이 `PFS_NOTAB_DIM_TXT`(기본 2.5mm), Dimasz≈문자×1.6, Dimexe/Dimexo≈1.5, Dimgap≈0.6×문자. `ApplyIsoDimensionOverrides` 유사 override를 **고정 mm**로. Dimscale=1(페이퍼 1:1).
3. **오프셋 배치(겹침 방지, §9 e)**: 치수선을 supportPaperExt **밖**에 페이퍼 mm 적층. 안쪽 치수=도형에서 `3×문자`, 바깥 적층 간격=`2.5×문자`. env `PFS_NOTAB_DIM_OFFSET`(기본=문자×3). 작은치수 안쪽/전체 바깥, 교차 금지. 뷰포트(도형) 위로 겹치지 않게.
4. **진입점**: `ConfigureNotabDetailViewport(...)` 반환 후(뷰 확정), `SaveAs`(2851-2852) **직전**에 새 트랜잭션:
   - `EnsureIsoAnnotationResources(detailDb,...)` → dimStyleId/layerId.
   - `TryComputeNotabDimPaperGeometry(vp, supportExt, out paperExt, out pipeCenterXPaper)`.
   - `layoutBtr=GetNotabDetailLayoutBlockTableRecordId(detailDb)` ForWrite.
   - `AppendNotabPaperDimensions(tr, layoutBtr, paperExt, pipeCenterXPaper, s_isoRealWidth, s_isoRealHeight, dimStyleId, layerId)`. commit.
5. 로그 `PFSNOTABDETAIL dim append H=realW V=realH split(L,R) paperExt=.. txt=.. offset=..`.

## 방어/보존
- 모든 append/투영 try/catch+FileDiag(빈 catch 금지). paperExt 퇴화·realW/H≤0·pipeCenter 범위밖(분할 skip, §기존 6120 폴백 유지) 방어.
- 클립·held-pipe·스케일·persp가드·wireframe·610×489 **무수정**. dim-probe 로그도 유지(TryCompute로 분리).
- 비연관(정의점 스냅샷)이라 detailDb는 저장 산출물=frozen → 문제없음(§9 c).

## 검증
- MSBuild Debug GREEN. 변경 주변 20줄 수동 확인.
- 라이브(RC1-001): 로그 `dim append ..`. 육안: **세로 높이(=realH)·가로 총폭(=realW)·pipeCenter 분할 치수가 뷰포트 서포트에 정렬**되고 문자 크기가 페이퍼 적정(과대X)·겹침 없음. env `PFS_NOTAB_DIM_TXT`/`PFS_NOTAB_DIM_OFFSET` 조정.

## 참고
- cycle47 커밋 cfa1fb7(투영 계측). 텍스트 override 패턴 6077/6100/6111/6129. DimStyle=STANDARD(EnsureIsoAnnotationResources).
