# HANDOFF (B4c) — Claude → Codex

> ★별도 채널: `.plans/HANDOFF.md`(핫 리로드)는 건드리지 않는다. 이 파일 읽고 집도, 하단 "## RESULT" 기록+커밋.
> 파일 충돌 없음(B4c=Commands.cs만).

- **track**: B4c (격리 치수 의미 정련)
- **status**: ready
- **issued_at**: 2026-07-13
- **title**: 세로 치수를 서포트만 재도록(파이프 원 제외 서브영역)

## 착수 전
- cwd 확인. 백업: `Commands.cs` → `Commands.cs.codex_bak_20260713_b4c`.

## 배경 (B4a 완료 후)
- 바운딩 치수 가시화 완료(폭 300/높이 75). ❌ **세로 치수 기하가 블록 전체(≈164=서포트75+파이프원)를
  걸치는데 라벨은 75** → 불일치(육안). 가로(300)는 정합.
- blockInner: Line=5(서포트), Circle=1(파이프). s_isoSize=파이프 nominal.

## 목표
세로 치수를 **서포트 사각형만** 재도록(기하 75 ↔ 라벨 75 일치). 치수 기준을 블록 전체 extents →
**서포트 서브영역(파이프 Circle 제외) extents**로 교체.

## 확정 사실 (§9 Gemini+Codex 수렴)
- **Q1**: 블록 정의(BlockTableRecord) 내부 엔티티 GeometricExtents는 **로컬 좌표** →
  반드시 **`BlockReference.BlockTransform` 적용**(8 corner 변환 후 WCS 합산). Identity 가정 금지.
- **Q2**: 파이프 원 식별 = **반경 매칭 우선**(`PSUtil.PipeSize(size)/2`, 우리 케이스 축척≈1로 추정되나
  **실측 반경을 로그**로 남겨 확인) + **위치 보조(원 중심이 서브영역 상단)**, **실패 시 최대원 fallback+로그**.
  s_isoSize 포맷이 PipeSize() 기대와 다를 수 있음 → fallback 필수, 조용한 실패 금지.
- **Q3**: 3D projection 역추적은 리스크 과대 → 채택 안 함. 2D 파싱+BlockTransform이 정답.

## 대상 파일
- `PlantFlow_Support/Core/Commands.cs` (단일 writer). PSUtil.PipeSize 등 참조만.

## 설계
### 변경 1: 신규 `TryGetIsoSupportPaperExtents(BlockReference br, out Extents3d supportExt)`
1. `br`의 BlockTableRecord 순회. 각 엔티티:
   - **8 corner를 `br.BlockTransform`으로 변환** → WCS 좌표. (엔티티 로컬 extents corner 변환.)
   - **파이프 Circle 판정**(제외 대상):
     - 기대 반경 `expR = PSUtil.PipeSize(int.Parse(s_isoSize)) / 2.0`(파싱 실패 방어). 
     - Circle이고 `|circle.Radius - expR| / expR < 0.15`(±15%)면 파이프 후보. 실측 로그
       `PFSVBISOEXPORTED pipeCircle R=<r> expR=<expR> match=<bool>`.
     - 후보 여럿이면 중심 Y 최상단(서포트 위) 원 채택.
     - 반경 매칭 원 0개면 **fallback=최대 반경 Circle 제외**, 로그 `fallback=maxCircle R=<r>`.
   - 파이프 Circle **제외**한 나머지 엔티티(Line 등) corner를 supportExt에 누적.
2. 누적 엔티티 0개/degenerate면 false 반환(호출부는 전체 블록 extents로 fallback).
3. 로그 `PFSVBISOEXPORTED supportExt=<min>~<max> excludedCircles=<n>`.

### 변경 2: AppendIsoBoundingDimensions에서 세로 치수만 supportExt 사용
- `TryGetIsoSupportPaperExtents(source as BlockReference, out supportExt)` 성공 시:
  - **세로 치수**: `CreateVerticalDimension(supportExt, ..., dimStyleId, supportExt.MaxPoint.X, out dimV)`
    + `dimV.DimensionText = realHeight`. (기하=서포트 높이, 라벨=realHeight 일치.)
  - **가로 치수**: 현행 유지(전체 paperExt 또는 supportExt.Width도 동일값 300 — supportExt로 통일 가능).
- 실패(또는 source가 BlockReference 아님) 시 현행 전체 paperExt 유지(회귀 안전).
- 크기 오버라이드(h)·레이어·WorkingDatabase 래핑 등 기존 로직 유지.

## 제약 (§0/§3/§3-A)
- Commands.cs 외 수정 금지. 커널·clone-back·EXPORTLAYOUT·B4a 로직 무변경(세로치수 기준 extents+헬퍼만).
- ★`.plans/HANDOFF.md`·`tools/HotReload/` 무접촉. 빈 catch 금지(FileDiag). PipeSize 파싱/BlockTransform 방어.
- `git add PlantFlow_Support/Core/Commands.cs`만. 빌드 사용자 수동. 완료 후 커밋 + "## RESULT" 기록.

## 완료 기준 (Acceptance)
- [ ] TryGetIsoSupportPaperExtents: BlockTransform 적용 + 파이프 Circle(반경매칭 우선/최대원 fallback+로그) 제외
- [ ] 세로 치수가 supportExt 기준(실패시 전체 fallback), 텍스트=realHeight
- [ ] 커널·B4a 무변경, 중괄호 균형, Commands.cs만 커밋, HANDOFF.md/tools 무접촉

## 검증 게이트 (사용자 수동 빌드/라이브 → Claude 판정)
- `PFSVBISOEXPORTED pipeCircle R=.. expR=.. match=..` + `supportExt=.. excludedCircles=1` →
  원본 Main 2D에서 **세로 치수가 서포트 사각형 높이에만 걸치고 라벨 75 일치**(파이프 원 미포함). 가로 정합 유지.
- 3회 반복. 판정: 세로 치수 기하-라벨 일치 → B4c 완료.
- fallback=maxCircle 로그가 뜨면 반경 매칭 실패(축척/포맷) → 실측 R/expR로 재조정.

## RESULT (Codex 기록)
- **status**: done
- **completed_at**: 2026-07-13
- **commit**: f02ee1b Refine isolated vertical dimension extents

### 변경 요약
- `AppendIsoBoundingDimensions`에서 세로 치수 기준 extents를 `paperExt`에서 support-only `verticalExt`로 분리했습니다. support extents 산출 실패 시 기존 `paperExt`로 fallback합니다.
- `TryGetIsoSupportPaperExtents(Transaction, BlockReference, out Extents3d)`를 추가해 블록 정의 내부 엔티티 extents의 8 corner를 `BlockReference.BlockTransform`으로 WCS 변환해 누적합니다.
- 파이프 원 제외는 `s_isoSize` → `PSUtil.PipeSize(nominal)/2` 기대 반경 ±15% 매칭을 우선하고, 실패 시 최대 반경 Circle을 제외하도록 fallback 로그를 남깁니다.
- `pipeCircle R/expR/match/centerY`, `supportExt`, `supportExt fallback`, `degenerate` 로그를 추가했습니다.

### 검증
- 백업 생성: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260713_b4c`
- 중괄호 균형: `opens=389 closes=389 balance=0`
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: whitespace error 없음(CRLF 경고만)
- 핵심 심볼 확인: `TryGetIsoSupportPaperExtents`, `CreateVerticalDimension(verticalExt...)`, `pipeCircle R=`, `fallback=maxCircle`, `supportExt=`
- 빌드는 사용자 수동 원칙에 따라 실행하지 않았습니다.

### 비고
- `.plans/HANDOFF.md`, `tools/HotReload/`는 건드리지 않았습니다.
- 커밋에는 `PlantFlow_Support/Core/Commands.cs`만 포함했습니다.