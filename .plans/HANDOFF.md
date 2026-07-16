# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 47
- **status**: ready
- **issued_at**: 2026-07-16
- **title**: N3-0/N3-a — 무탭 뷰포트 스케일 표준화 + WCS→paper 투영 좌표 계측 (치수 미제도)
- **target**: `PlantFlow_Support/Core/Commands.cs` (그 외 무수정)
- **plan**: `<appDataDir>\scratch\plan_pfs_notab_n3_20260714.md` (★갱신 섹션)

## 착수 전
- cwd `D:\PlantFlow\PlantFlow_Support`. `ConfigureNotabDetailViewport`/`CreateNotabDetailDrawing` 및 신규 투영 헬퍼만. 그 외 무수정.
- 빌드 GREEN(MSBuild Debug, 에러0)까지 Codex 확인. 라이브는 사용자.
- **이번 사이클은 계측 우선 — 치수는 그리지 않는다.** 스케일 표준화 + 페이퍼 좌표 로그까지만.

## 배경
N3(치수)는 최대 리스크라 계측 선행. 뷰포트는 cycle43 이후 동적 피팅(ViewCenter=(0,0), ViewHeight 계산, **CustomScale 미설정**). 사용자가 **스케일 과대(~1:1.4, 프레임 87%)로 치수·콜아웃 공간 부족** 지적. §9 확정: 유효스케일=`vp.Height/vp.ViewHeight`, `CustomScale` 명시 세팅해야 투영 정합.

## 요구 A — N3-0: 스케일 표준화 + CustomScale 명시
`ComputeNotabViewportFit`(~3924) 또는 `ConfigureNotabDetailViewport`(~3828)에서:
1. 동적으로 산출한 최소 `viewHeight`(content*(1+pad) 기반)를 **표준배율로 라운딩**: 후보 배율 목록 `{1, 1/2, 1/5, 1/10, 1/20, 1/25, 1/50}`(scale=paper/model) 중, **content가 주석 여백 포함해 뷰포트에 들어가는 가장 큰(=도형이 너무 작지 않은) 표준배율** 선택.
   - 주석 여백: content가 뷰포트의 **약 55~65%**만 차지하도록(치수선·콜아웃 공간). 즉 `요구 model 크기 = content / 0.6` 정도를 담는 표준배율.
   - 환경변수 `PFS_NOTAB_TARGET_FILL`(기본 0.6)로 조정.
2. 선택 표준배율 `stdScale`로 `viewHeight = vpPaperHeight / stdScale` 재설정, `vp.CustomScale = stdScale` **명시 세팅**. (ViewHeight/CustomScale 이중 세팅 정합 주의 — 최종적으로 `vp.CustomScale=stdScale`, `vp.ViewHeight=vpHeight/stdScale` 일관.)
3. 로그 `PFSNOTABDETAIL viewport scale std=1:<1/stdScale> viewHeight=.. fill=.. content=(W,H) vp=(w,h)`.

## 요구 B — N3-a: WCS→paper 투영 헬퍼 이식 + 좌표 계측 (치수 미제도)
`ViewportProjection`(Ortho/OrthoViewportManager.cs:171-218)은 `private sealed`라 재사용 불가 → **동일 로직을 Commands.cs에 이식**:
1. 헬퍼 `NotabProjectWcsToPaper(Viewport vp, Point3d wcs) → Point3d paper`:
   - `dcsToWcs = Matrix3d.Rotation(-vp.TwistAngle, viewDir, vp.ViewTarget) * Matrix3d.Displacement(vp.ViewTarget - Point3d.Origin) * Matrix3d.PlaneToWorld(viewDir)` (viewDir=vp.ViewDirection, 퇴화 시 ZAxis). `wcsToDcs = dcsToWcs.Inverse()`.
   - `dcs = wcs.TransformBy(wcsToDcs)`; `scale = vp.CustomScale`(N3-0서 세팅) 또는 `vp.Height/vp.ViewHeight`; `paperX = vp.CenterPoint.X + (dcs.X - vp.ViewCenter.X)*scale`; `paperY = 동일 Y`. return (paperX,paperY,0).
2. `ConfigureNotabDetailViewport` 커밋 후(뷰 설정 확정된 vp로), **서포트 extents(supportExt) 8코너 + pipeCenter(=supportExt 중심 또는 파이프 solid 중심)를 페이퍼 투영** → paper rect(minX/maxX/minY/maxY)·pipeCenterX(paper) 산출.
3. 로그 `PFSNOTABDETAIL dim-probe support-paper=(minX,minY)~(maxX,maxY) pipeCenterX(paper)=.. realW=.. realH=.. scale=.. vpCenter=.. vpTarget=..`.
   - **치수는 그리지 않는다.** 이 좌표가 라이브에서 뷰포트 도형(원+사각형)과 정렬되는지 육안+로그로 검증하는 게 목적.

## 방어/보존
- 투영/행렬 try/catch+FileDiag(빈 catch 금지). viewDir 퇴화·CustomScale 0 방어(0이면 Height/ViewHeight 폴백).
- 클립·held-pipe·persp가드·wireframe·610×489 **무수정**. 이번 변경은 스케일 표준화 + 투영 계측만(치수 없음).
- supportExt는 `CopyCleanNotabSolids` out으로 이미 있음 → `CreateNotabDetailDrawing`서 투영 단계로 전달.

## 검증
- MSBuild Debug GREEN. 변경 주변 20줄 수동 확인.
- 라이브(RC1-001): 로그 `viewport scale std=1:N`(표준배율), `dim-probe support-paper=.. pipeCenterX=..`. 육안: 스케일 낮아져 도형 작아지고 **주석 여백 확보**, 투영 좌표가 도형과 정렬(다음 N3-b 치수 제도 전제). 어긋나면 좌표 로그로 매핑 교정.

## 참고
- 다음 단계(별도 사이클): N3-b(세로 치수 제도)→N3-c(가로 분할). 치수 헬퍼=AnnotationUtils.cs:29-39/58-68, 텍스트 override 패턴=Commands.cs:5957/5980/6009, DimStyle=참조 OrthoViewportManager:180-192(Dimscale=1).
