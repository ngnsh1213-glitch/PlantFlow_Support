# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 127
- **status**: ready
- **issued_at**: 2026-07-24
- **title**: 무탭 평면화 솔리드 분류(support/pipe/ubolt) + 카테고리 블럭화 (Phase 1)
- **작업 경로**: `PlantFlow_Support/Core/Commands.cs` — `FlattenNotabSolidsToPaper`(1980) 내부 개조
- **계획서**: `.plans/plan_notab_classify_block_20260724.md`
- **기준 커밋**: `6338313`(cycle126 flatten PASS)
- **자문**: Codex(§9, read-only 1회). 채택 = ①분류는 "유볼트 중심이 솔리드 확장extents 내부" 우선·복수는 최소거리·애매하면 미분류 WARN(support 강등) ②배관=축수직거리+축방향범위+반경tol 3조건 ③블럭은 공유 기준점(전체 bbox 좌하단) 상대좌표화 후 그 점 삽입(이중이동 0) ④엔티티 ByLayer 고정·BlockReference 명시 레이어 ⑤Phase 1/2 분리·유볼트 블럭은 그릇으로 존치.

## 배경
cycle126에서 각 Solid3d의 Brep 에지를 페이퍼 2D Polyline로 투영 성공(라이브 PASS). 이번엔 그 폴리라인을 **출처 솔리드별로 분류**해 **카테고리 블럭**으로 묶는다(사용자 요구: 서포트/배관/유볼트 개별 블럭). 유볼트 조각남의 근본 해결(표준 심볼)은 **Phase 2**(별도 사이클, 캘리브레이션).

## ⚠ 검증 필수
`dotnet build` 오류 0(빌드·커밋 분리) 없이 커밋 금지. **push 금지**. 기본(PFS_NOTAB_FLATTEN 미설정) 회귀 0 최우선. 블럭화가 형상·주석 좌표를 이동시키면 안 됨(이중이동 0 = cycle126과 위치 동일).

## 집도 항목 (FlattenNotabSolidsToPaper 개조)
1. **분류**: 폴리라인을 즉시 layout.AppendEntity 하지 말고, 각 Solid3d를 `GeometricExtents` 중심으로 분류.
   - ubolt: `s_isoUbolts[].WcsCenter`가 솔리드 확장 extents 내부(또는 중심이 유볼트 외경/폭 tol 내) → 복수 매칭은 최소거리 tag.
   - pipe: `s_isoPipeCenterValid` && 축수직거리 ≤ 반경+tol && 축방향 범위 겹침 → "PIPE".
   - 나머지 → "SUPPORT". 확신 불가 시 `PFSNOTABBLOCK unclassified` WARN + support 처리(형상 유실 금지).
2. **카테고리 누적**: support 1그룹 / pipe 1그룹(비면 스킵) / ubolt **tag별 분리**. 폴리라인 레이어 PFS_ISO_DETAIL, 속성 ByLayer.
3. **블럭 정의·삽입**: 전체 투영 폴리라인 bbox 좌하단 = 공유 base. 카테고리마다 BlockTableRecord 신설(`PFS_FLAT_SUPPORT`/`PFS_FLAT_PIPE`/`PFS_FLAT_UB_<tag>`, 충돌 시 `_n`) → 그룹 폴리라인을 `-base` 이동 후 BTR append → 레이아웃에 `BlockReference(base, btrId)` 삽입(레이어 PFS_ISO_DETAIL, 회전0/스케일1). 빈 카테고리 미생성.
4. **로그**: `PFSNOTABBLOCK support=<n> pipe=<n> ubolt=<tags> unclassified=<n> blocks=<count> base=(x,y)`.
5. **방어**: 분류/블럭화 예외는 solid 단위 skip+로그, 전체 중단 금지. 기존 Erase 안전장치(성공 솔리드만/전 솔리드 성공 시 viewport) 유지.

## 검증 레시피
- dev_test `PFS_NOTAB_FLATTEN=1` + 태그 `RC1-001` → 추출 → 상세도:
  - 로그 `PFSNOTABBLOCK …` 카운트(support≥1, ubolt=UB-002,UB-003, pipe=0 예상) 확인.
  - 선택 시 **블럭 단위**(support 한 번, ubolt 각각)로 잡히는지, LIST에서 BlockReference 확인.
  - 형상·주석 위치가 cycle126과 **동일**(이중이동 0). EXPLODE 시 원 폴리라인 무손실 복원 1회.
- `PFS_NOTAB_FLATTEN=0`: 회귀 0.
- 첫 관문 = 분류 카운트가 실제 구성(각재=support, UB=ubolt)과 일치하는가.

## 범위 밖 (Phase 2 = 차기 `3`)
유볼트 표준 2D 심볼 대체(조각남 근본 해결). 심볼 형상은 열린 질문(HANTEC 규격집 재판독+사용자 확정). 이번 cycle127에서 손대지 말 것.
