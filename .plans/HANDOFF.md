# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 43
- **status**: ready
- **issued_at**: 2026-07-16
- **title**: 무탭 추출물 서포트 영역 클립(Solid3d Boolean) + 뷰포트 동적 피팅 (Phase 2)
- **target**: `PlantFlow_Support/Core/Commands.cs` (그 외 무수정)
- **plan**: `<appDataDir>\scratch\plan_pfs_notab_viewport_review_20260716.md`

## 착수 전
- cwd `D:\PlantFlow\PlantFlow_Support`. `CopyCleanNotabSolids` / `ConfigureNotabDetailViewport` 및 신규 헬퍼만. 그 외 무수정.
- 빌드 GREEN(MSBuild Debug, 에러0)까지 Codex 확인. 라이브는 사용자(`dev_test.bat`, 태그=`PFS_NOTAB_TEST_TAG`).

## 배경 — 0-B 실측(RC1-001) + 참조 근거
- 마진 고정(150)으로 이웃 **서포트**는 배제(addedSupport=0) 성공. 그러나 **이웃 배관(200mm 간격) + 관통 파이프 전체 길이(Y 10m)** 가 뷰에 그대로 노출. 사용자 요구=**서포트 영역(bbox)만 잘라내기**.
- 참조 프로젝트(레거시 OrthoGen, `C:\Users\HT노승환\Documents\PlantFlow_Support`)는 서포트 영역 박스로 3중 클립: ①쿼리박스(`NewQueryByBox`) ②앞뒤 클립(`m_frontClip=boxSize/2,m_backClip=+boxSize`) ③클립솔리드(`NewDwgSetClipper(cubeSolid, GetClipInlateBy())`). 무탭은 OrthoGen 없음 → **Solid3d Boolean으로 등가 구현**.

## 요구 1 — 서포트 영역 클립 (Solid3d Boolean Intersection)
`CopyCleanNotabSolids`(라인 ~3272) **내부**에서, `supportExt` 산출 후 각 `clean` solid를 append·metadata strip한 **직후** 클립한다. **클립 후 ext만 `solidExt`에 누적**(기존 out 파라미터 정합 유지).

1. **oriented 클립 박스**(WCS AABB 아님 — 축 정합 필수): basis `viewDir=s_isoPipeAxis`(정규화, invalid 시 XAxis), `up=s_isoPipeUp`(정규화), `right=up×viewDir`. `supportExt` 8코너(`GetExtentsCorners` 재사용)를 이 basis에 dot 투영해 right/up/viewDir 각 축의 min/max 산출 → 인플레이트 마진(기본 co-loc 마진급, env `PFS_NOTAB_CLIP_MARGIN`, 기본 예: 100~150mm) 적용. **viewDir(깊이) 방향은 서포트 깊이+소폭 마진으로 제한**(관통 파이프 길이 트림 핵심). 이 basis로 `Solid3d.CreateBox` 후 basis 회전+중심 이동 Matrix3d로 배치(oriented box).
   - 접촉 선필터(Gemini): 클립 대상은 **서포트 bbox(인플레이트)와 실제 교차하는 solid만**. 비교차 solid는 Boolean 없이 제외(파편·부하 방지).
2. **Boolean INTERSECT**: solid마다 클립박스 **복제본**(`clipBox.Clone()`)을 인자로 `solid.BooleanOperation(BooleanOperationType.Intersection, clipCopy)`. 원본 clipBox 재사용 금지(소비됨→eInvalidInput).
   - **empty 결과 처리**: `eNoIntersection` 등 예외 try/catch, 연산 후 `Volume<=1e-9` 또는 `IsNull` → 해당 solid는 detailDb에서 **Erase/제외**(빈 solid 도면 유입 금지)+로그. `copied` 카운트는 유효 잔존만.
3. 클립박스 자체·복제본·중간 solid는 DB에 append하지 않는다(최종 유효 solid만 유지). 클립박스는 계산용 transient → 사용 후 Dispose.
4. 방어: 각 Boolean try/catch+FileDiag(빈 catch 금지), 실패 시 해당 solid는 **클립 없이 원본 유지**(안전 폴백)+로그. 로그 예: `PFSNOTABDETAIL clip solids=N kept=K trimmed=T dropped=D marginDepth=..`.

## 요구 2 — 뷰포트 동적 피팅
`ConfigureNotabDetailViewport`(라인 ~3584)의 고정 `CustomScale=0.25` + `ViewCenter=(0,0)`를 **클립 후 콘텐츠에 맞춘 동적 피팅**으로 교체.
- basis(`dir/up/right`, twist는 `ComputeNotabViewportTwist` 동일)로, **클립 후 solidExt(또는 supportExt) 8코너를 `(corner - ViewTarget)` 기준 right/up에 투영**해 min/max→ 폭 w, 높이 h 산출.
- `ViewCenter=(cx,cy)`(투영 중심), `ViewHeight = max(h*(1+pad), w*(1+pad)/aspect)`. aspect=뷰포트 Width/Height(현 610×489 유지). pad=여백(기본 예: 0.15, env `PFS_NOTAB_FIT_PAD`).
- `CustomScale`은 제거하거나 ViewHeight와 정합되게(표준스케일 라운딩은 선택). ViewCenter(0,0) 제거.
- 재사용 후보: `GetExtentsCorners`, `CaptureIsoSelectionMetrics`의 dot 투영 패턴.

## 충돌/보존 (Codex 확인)
- persp 가드(cycle 42)·`TryReopenSetNotabPaperSpace`·paper-zoom과 무충돌. reopen paper-zoom은 페이퍼 초기화면용이라 디테일 viewport의 ViewTarget/Center/Height 미변경. **610×489 viewport 크기 판별 로직 유지**.
- 기존 wireframe 기본(Phase1, `PFS_NOTAB_USE_HIDDEN` opt-in) 유지.

## 검증
- MSBuild Debug GREEN(에러0). 변경 주변 20줄 수동 확인.
- 라이브 기대(RC1-001, `PFS_NOTAB_TEST_TAG=RC1-001`): 로그 `clip solids/kept/trimmed/dropped`, 이웃 배관 소멸·관통 파이프 길이 트림, `viewport fit ViewHeight/center=..`. 육안: 서포트 영역만 프레임에 꽉 참.
- env: `PFS_NOTAB_CLIP_MARGIN`, `PFS_NOTAB_FIT_PAD` 조정 가능.

## 참고 (현재 상태)
- Phase1 커밋 999e1f9(와이어프레임 기본). 마진 접촉판정 73b9974. persp 가드 6e01a9b.
- `s_isoPipeAxis`/`s_isoPipeUp`/`s_isoRealWidth/Height`는 `RunNotabDetailPipeline` 진입부에서 세팅됨(클립/피팅에서 참조 가능).
