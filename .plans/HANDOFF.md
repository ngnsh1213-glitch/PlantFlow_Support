# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 45
- **status**: ready
- **issued_at**: 2026-07-16
- **title**: 무탭 배관 접촉 판정을 "서포트 중심거리"→"서포트 bbox 투영 포함"으로 교체 (cycle44 회귀 수정)
- **target**: `PlantFlow_Support/Core/Commands.cs` (그 외 무수정)

## 착수 전
- cwd `D:\PlantFlow\PlantFlow_Support`. `AutoIncludeRelatedParts`의 파이프 접촉 판정부 + 관련 헬퍼만. 그 외 무수정.
- 빌드 GREEN(MSBuild Debug, 에러0). 라이브는 사용자(`PFS_NOTAB_TEST_TAG=RC1-001`).

## 배경 — cycle44 라이브 회귀 (계측)
cycle44 접촉 필터가 **잡는 배관까지 전부 드롭**(`pipeCand=6 incl=0 dropDist=6`). 로그: 최근접 배관 `d=125.47 tol=64.45`(radius44.45+여유20). 나머지 226/376/408/583/793.
- **근인**: 거리를 **서포트 bbox '중심'** 기준으로 쟀는데, 서포트는 파이프를 **중심이 아니라 윗면/새들에서** 잡음 → 잡는 배관도 중심에서 125mm라 tol(64) 초과 드롭.
- **파급 3증상(모두 이 하나에서)**: ①배관 안 나옴(incl=0) ②메인 방향 회귀(배관 없어 `TryGetSelectionPipeAxis` 실패→viewDir 기본 1,0,0) ③스케일 리셋(metrics가 서포트만). → **잡는 배관 하나 포함되면 셋 다 복구**.

## 요구 — 접촉 판정 교체 (중심거리 → 서포트 bbox 투영 포함)
사용자 확정 방향: **"서포트 크기(bbox)에 맞춰, 그 라인의 배관 중 서포트 bbox를 지나는(컨택) 배관만 포함"**. §9(Gemini d)=파이프축 수직평면 투영.

`AutoIncludeRelatedParts`의 파이프 판정에서 `DistancePointToPipeAxis(supCenter, ...)` 기반 tol 비교를 **아래로 교체**:

1. 파이프 축(p0, dir) 획득(`TryGetPipeAxisFromId`, 기존). dir 정규화. 유효 실패 시 기존 bbox 폴백 유지.
2. **파이프축에 수직인 평면 basis** 구성: `right`,`up` = dir에 직교하는 2축(예: dir와 월드Z로 `right=dir×Z`(정규화, 퇴화 시 dir×Y), `up=dir×right`). 
3. **서포트 bbox 8코너**(`GetExtentsCorners(supExt)`)를 right/up에 투영 → `[minR,maxR]×[minU,maxU]` 사각형(= 서포트 실루엣, "서포트 크기").
4. **파이프축 점 p0**를 같은 평면 원점 정합 위해: 서포트 코너와 p0 모두 **동일 기준(월드 원점)** 으로 right/up 투영해 비교(축 방향 성분은 무시=수직평면). `pr=p0·right, pu=p0·up`.
5. **접촉 판정**: `(minR - tol) <= pr <= (maxR + tol)` AND `(minU - tol) <= pu <= (maxU + tol)`. `tol = pipeRadius + PFS_NOTAB_CONTACT_TOL`(여유 기본값 소폭, 예 10~20mm). 통과=포함, 아니면 dropDist.
   - 이유: 잡는 배관은 축이 서포트 실루엣 안(또는 윗면±tol)을 지남→포함. 200mm 평행 이웃은 실루엣 밖→drop. **중심거리 문제(윗면 잡힘) 해소.**
6. **라인 전처리(C)**는 그대로 유지(같은 LineNumberTag만 후보). supTag 없으면 파이프 미포함(단독 추출) 유지.
7. **안전장치(다중 통과 대비)**: 만약 통과 배관이 2개 이상이면(서포트 폭 > 배관 간격 등 모호), **수직평면상 서포트 사각형 중심에 가장 가까운 1개** 우선 포함(나머지 로그 후 제외). 서포트는 통상 1개 배관을 잡음.
8. 로그: `auto-include line=.. pipeCand=N incl=K dropLine=.. dropDist=.. rect=(minR,maxR,minU,maxU) p0proj=(pr,pu) tol=..` (판정 근거 가시화).

## 방어/보존
- 기존 try/catch+FileDiag, ps/null, 축 degenerate 폴백 유지. 빈 catch 금지.
- 클립(`CopyCleanNotabSolids`)·동적피팅·persp가드·wireframe·610×489 **무수정**. 이 변경은 auto-include 판정부만.
- `DistancePointToPipeAxis`는 폴백/타 용도로 남겨도 무방(미사용 시 제거 판단은 Codex).

## 검증
- MSBuild Debug GREEN. 변경 주변 20줄 수동 확인.
- 라이브(RC1-001): 기대 `pipeCand=6 incl=1 dropDist=5`, 잡는 배관 1개만 포함 → **메인 방향/스케일 복구 + 평행 이웃 소멸**. env `PFS_NOTAB_CONTACT_TOL`로 실루엣 여유 미세조정.

## 참고
- cycle44 커밋 1cb3192(중심거리 판정, 이번에 교체). 헬퍼 `TryGetPipeAxisFromId`/`TryGetPlantLineNumber`/`GetExtentsCorners` 재사용.
