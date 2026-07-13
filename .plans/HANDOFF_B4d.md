---
task: PFS 격리 B4d — 가로 치수 파이프중심 분할
cycle: 1
status: ready
from: claude
to: codex
plan: C:\Users\HT노승환\.gemini\antigravity\scratch\plan_pfs_iso_b4d_dimsplit_20260713.md
target: PlantFlow_Support/Core/Commands.cs
---

# 지시 (B4d cycle 1)

격리 Main 뷰의 가로 치수를 **파이프 원 중심 X 기준 좌/우 분할 + 전체** 3개로 확장한다.
현재는 전체 폭 1개만 생성된다. 목표는 사용자 참조 이미지: 상단에 좌(예 100)·우(예 200), 그 아래 전체(예 300).

## 대상
`PlantFlow_Support/Core/Commands.cs` 만 수정.

## 구현 (상세는 plan 파일 참조)

### 1) IsoCircleCandidate에 CenterX 추가 (약 line 2754)
- 생성자 시그니처: `IsoCircleCandidate(ObjectId id, double radius, double centerX, double centerY)`, 필드 `public double CenterX;` 추가.
- 인스턴스화(약 line 2619): `circles.Add(new IsoCircleCandidate(id, radius, center.X, center.Y));`

### 2) 파이프중심 X 노출: TryGetIsoSupportPaperExtents (line 2582)
- 시그니처에 `out double pipeCenterX` 추가. 진입 즉시 `pipeCenterX = double.NaN;`.
- excludedCircleId 확정 후, 해당 후보의 CenterX를 pipeCenterX에 대입(루프에서 excluded 후보를 찾을 때 CenterX 저장). fallback=maxCircle 경로도 그 원의 CenterX 사용.
- 모든 return 경로에서 pipeCenterX 세팅 보장.
- 호출부(line 2539) 갱신: `this.TryGetIsoSupportPaperExtents(tr, source as BlockReference, out supportExt, out pipeCenterX)`.

### 3) AppendIsoBoundingDimensions 가로 분할 (line 2519~)
- 가로 base extents = verticalExt(서포트) 우선, 없으면 paperExt.
- baseMinX/baseMaxX/baseMaxY, w=baseMaxX-baseMinX 산출.
- 분할 조건: `!double.IsNaN(pipeCenterX) && pipeCenterX > baseMinX+1e-6 && pipeCenterX < baseMaxX-1e-6 && w>1e-6`.
  - leftReal = s_isoRealWidth * (pipeCenterX-baseMinX)/w; rightReal = s_isoRealWidth-leftReal.
  - dimLineY = baseMaxY + 50 + dimSize*2 (전체선 위 단).
  - 좌 dim: CreateHorizontalDimension(p1=(baseMinX,baseMaxY,0), p2=(pipeCenterX,baseMaxY,0), p3=(baseMinX,dimLineY,0), Matrix3d.Identity, dimStyleId), DimensionText=FormatNumber(leftReal).
  - 우 dim: p1=(pipeCenterX,baseMaxY,0), p2=(baseMaxX,baseMaxY,0), p3=(pipeCenterX,dimLineY,0), DimensionText=FormatNumber(rightReal).
  - 전체 dim: 기존 extent 기반 dimH(하단) 유지, 라벨 s_isoRealWidth.
  - 좌/우/전체 각각: DimensionStyle 세팅, ApplyIsoDimensionOverrides(_, dimSize, label), annotationLayerId 있으면 LayerId, targetMs.AppendEntity + tr.AddNewlyCreatedDBObject, LogIsoDimensionExtents.
  - 로그: `PlantOrthoView.FileDiag("PFSVBISOEXPORTED dimSplit centerX=..+ baseMinX/baseMaxX/left/right/total=..")`.
- 분할 불만족: 기존 단일 전체 가로 치수만(현행 유지). 로그 `dimSplit skip: <이유>`.
- 세로 치수(dimV) 로직은 변경 금지.

## 규율
- 방어적: 빈 catch 금지, 예외 시 FileDiag 후 폴백.
- 비파괴, 계측우선. SendStringToExecute 패턴/ed.Command 금지 규율 무관(이 집도는 순수 후처리).

## 빌드 유의
- 병렬 핫리로드가 `tools/HotReload/` 를 리포에 추가했으면 CS0579 발생 가능 →
  `PlantFlow_Support.csproj`에 `<Compile Remove="tools\**" />` (ItemGroup 내) 필요 시 추가.
- 빌드 GREEN 확인.

## 완료 보고
- 커밋 후 `.plans/REPORT.md` 대신 이 파일 하단 또는 별도 표기 대신, `.plans/HANDOFF_B4d.md`에 RESULT 섹션 append + 커밋.
- 라이브 확인 항목: `PFSVBISOEXPORTED dimSplit` 로그, Main 뷰 100/200/300 3개 치수, 좌+우=전체.

## RESULT (Codex)
- status: done
- commit: f6c99fa
- backup: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260713_B4d`
- changed: `PlantFlow_Support/Core/Commands.cs`
- summary:
  - `IsoCircleCandidate`에 `CenterX`를 추가하고, support-only extents 계산에서 제외된 파이프 원 중심 X를 `pipeCenterX`로 노출.
  - 격리 Main 가로 치수에 `dimSplitL`/`dimSplitR` 좌우 분할 치수를 추가하고, 기존 전체 가로 치수와 세로 치수는 유지.
  - `PFSVBISOEXPORTED dimSplit ...` / `dimSplit skip ...` 로그 추가.
- verification:
  - `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS
  - braces: `opens=393 closes=393 balance=0`
  - `rg TryGetIsoSupportPaperExtents/new IsoCircleCandidate/dimSplit`: PASS
  - malformed escaped quotes search: PASS
  - build: not run (AGENTS 빌드 수동 원칙)