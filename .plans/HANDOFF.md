# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 44
- **status**: ready
- **issued_at**: 2026-07-16
- **title**: 무탭 배관 포함을 "서포트가 실제 잡는 배관"으로 한정 (A 기하 접촉 + C 라인 전처리)
- **target**: `PlantFlow_Support/Core/Commands.cs` (그 외 무수정)
- **plan**: `<appDataDir>\scratch\plan_pfs_notab_viewport_review_20260716.md`

## 착수 전
- cwd `D:\PlantFlow\PlantFlow_Support`. `AutoIncludeRelatedParts` 및 신규 헬퍼만. 그 외 무수정.
- 빌드 GREEN(MSBuild Debug, 에러0)까지 Codex 확인. 라이브는 사용자(`dev_test.bat`, `PFS_NOTAB_TEST_TAG`).

## 배경 (라이브 실측 + 결정)
- cycle43 클립으로 관통 파이프 **길이 트림**·동적 피팅은 성공(라이브 PASS). 그러나 사용자가 서포트 옆 **200mm 평행 배관(같은 라인)**을 두니, 그 이웃 배관이 detail에 원(단면)으로 노출됨.
- 사용자 확인: 이웃이 **같은 LineNumberTag(같은 라인)** → 라인태그 필터만으론 구분 불가. **기하 접촉 필요.** 서포트에 LineNumberTag 있음.
- 결정: **A(기하 접촉) + C(라인 전처리)** 를 **AutoIncludeRelatedParts(Plant 객체 단계)**에 적용. 이웃 파이프를 애초에 선택셋에서 배제 → 하류(`TryGetSelectionPipeAxis`=최장 파이프 선택, `CaptureIsoSelectionMetrics`, 복사/클립) 오염 차단. 클립(cycle43)은 그대로 유지(역할 분리: auto-include=선택필터, 클립=가시범위/길이트림).

## 요구 — AutoIncludeRelatedParts 파이프 포함 조건 교체
현재는 anchor(서포트 bbox)+margin 교차로 Pipe를 추가. 이를 **"서포트가 잡는 배관만"** 으로 교체한다.

### 신규 반환형 헬퍼(전역대입 헬퍼에서 분리)
- `TryGetPlantLineNumber(PSUtil ps, ObjectId id, out string lineNo)`: `ps.dl_manager.GetProperties(id, ["LineNumberTag","LineNumber","Line Number"], true)` 후보 순회로 값 반환(기존 `CaptureIsoPipeLineNo`(~5806)/`CaptureIsoSupportProperties`(~5744) 로직 재사용, 전역 `s_isoPipeLineNo` 대입은 하지 않음).
- `TryGetPipeAxisFromId(Transaction tr, ObjectId pipeId, out Point3d p0, out Vector3d dir, out double radius)`: `Part.GetPorts((PortType)7)` 첫 두 포트로 p0/dir. radius=Size 속성(지름/2) 또는 포트/‏bbox 근사. 포트<2면 false(축 판정 불가).

### 판정 로직 (§9 확정)
1. 대상 서포트: bbox 중심 `supCenter`, `TryGetPlantLineNumber` → `supTag`.
2. **supTag가 없으면(부착정보 없음)**: 파이프 auto-include를 **하지 않는다**(서포트 단독 추출 — 사용자 명시). 서포트 후보(유볼트) 포함 로직만 유지.
3. supTag가 있으면, 각 candidate Pipe에 대해:
   - **C 라인 전처리**: `TryGetPlantLineNumber(pipe)` != supTag → skip.
   - **A 기하 접촉**: 파이프 **중심축 선분**(p0,dir)에서 `supCenter`까지 **수직거리** `d = |(supCenter-p0) - ((supCenter-p0)·dir)dir|`. **축직교 판정**(WCS AABB 사용 금지 — 사선 파이프 부풀림 회피).
   - **tol(동적)** = `pipeRadius + 접촉여유`(여유 기본 20mm, env `PFS_NOTAB_CONTACT_TOL`). `d <= tol` 이면 포함, 아니면 drop(200mm 평행 이웃은 tol 밖 → drop).
   - 축 판정 불가(포트<2/dir≈0): 폴백으로 supCenter↔pipe bbox 최근접 거리 tol 판정 or skip(로그).
4. **Support 후보(유볼트/ATTACHMENT)**: 동일 원리 — 유볼트는 잡는 파이프를 감싸 중심이 축과 거의 일치하므로, 대상 서포트 bbox에 **근접(소 tol)** 한 서포트만 포함(현행 유지하되 margin 축소 검토). 이웃 프레임워크 서포트는 이미 배제됨(addedSupport=0).
5. 로그: `PFSNOTABDETAIL auto-include line=<supTag> pipeCand=N incl=K dropLine=.. dropDist=.. tol=..`.

## 방어적 프로그래밍
- 모든 GetProperties/GetPorts/Transaction try/catch + FileDiag(빈 catch 금지). ps/dl_manager null 방어.
- 축 degenerate·radius 미상 시 폴백 경로 + 로그. supTag 공백 처리.

## 보존/충돌
- 클립(`CopyCleanNotabSolids`)·동적피팅(`ConfigureNotabDetailViewport`)·persp가드·wireframe·610×489 **무수정 유지**. 이 변경은 **선택셋 축소만**.
- 재사용: `GetExtentsCorners`(~5841), dot 투영 패턴, `TryGetSelectionPipeAxis`(~6479, 참고).

## 검증
- MSBuild Debug GREEN. 변경 주변 20줄 수동 확인.
- 라이브(RC1-001, `PFS_NOTAB_TEST_TAG=RC1-001`): 로그 `pipeCand=.. incl=.. dropDist=..`, **평행 이웃 배관 원 소멸**, 잡는 파이프 1개만 트림되어 표시. env `PFS_NOTAB_CONTACT_TOL` 조정 가능.

## 참고
- cycle43 클립/피팅 커밋 f5473d8. Phase1 와이어프레임 999e1f9.
