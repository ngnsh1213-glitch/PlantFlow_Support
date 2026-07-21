# HANDOFF (B4a-fix2) — Claude → Codex

> ★별도 채널: `.plans/HANDOFF.md`(핫 리로드)는 건드리지 않는다. 이 파일 읽고 집도, 하단 "## RESULT" 기록+커밋.
> 파일 충돌 없음(B4a-fix2=Commands.cs만).

- **track**: B4a-fix2 (격리 치수 가시성)
- **status**: ready
- **issued_at**: 2026-07-13
- **title**: 치수 텍스트/화살표 크기를 기하에 비례(가시성 확보) + 치수 extents 로그

## 착수 전
- cwd 확인. 백업: `Commands.cs` → `Commands.cs.codex_bak_20260713_b4afix2`.

## 배경 (B4a-fix 라이브 판정)
- ✅ eKeyNotFound 해소(WorkingDatabase). 치수 생성·커밋 완벽(QSELECT `Rotated Dimension (3)`, AUTO_DIM 레이어).
- ❌ **화면 미표시 원인 확정**: STANDARD 치수스타일 인치 기본값이라 **Text height=0.18, Arrow=0.18** —
  300mm 객체에 극소 크기(0.06%)라 안 보임. 순수 스케일 문제.
- 데이터: PLN은 서포트·파이프 모두 빈값(라인넘버 미부여). BOP=423 정상. → B4b MLeader는 빈 PLN 스킵.

## 대상 파일
- `PlantFlow_Support/Core/Commands.cs` (단일 writer). AnnotationUtils/PSUtil 무변경(참조만).

## 설계 (AppendIsoBoundingDimensions에 크기 오버라이드 + 로그)
`AppendIsoBoundingDimensions`(치수 생성부)에서 각 RotatedDimension에 **기하 비례 크기 오버라이드**를 세팅.

### 변경 1: 텍스트/화살표 크기를 기하에 비례
- 기준 크기 `h` 계산: `double baseSize = System.Math.Max(s_isoRealWidth, s_isoRealHeight);`
  `double h = baseSize / 12.0;` → **clamp [10, 500]**(baseSize 0/비정상 방어, 최소 10).
  (예: 300mm → h=25mm. 큰/작은 서포트에도 비례.)
- 각 dim(dimH, dimV)에 엔티티 오버라이드 세팅(RotatedDimension의 dimvar 오버라이드):
  ```
  dim.Dimtxt = h;        // 텍스트 높이
  dim.Dimasz = h;        // 화살표 크기
  dim.Dimexe = h * 0.6;  // 치수보조선 연장
  dim.Dimexo = h * 0.3;  // 치수보조선 오프셋
  dim.Dimgap = h * 0.3;  // 텍스트-치수선 간격
  dim.Dimtih = false;    // 내부 텍스트 수평화 해제(정렬)
  dim.Dimtoh = false;    // 외부 텍스트 수평화 해제
  ```
  (프로퍼티명이 API에서 다르면 동등 dimvar 세터 사용. 세팅 실패는 FileDiag 후 계속.)
- DimensionText(실 mm)는 기존대로 유지.

### 변경 2: 치수 위치·크기 진단 로그
- 각 dim append 후 `dim.GeometricExtents` 로그:
  `PFSVBISOEXPORTED dimH ext=<min>~<max> h=<h>` / `dimV ext=...`.
  (향후 배치·오프셋 판단 근거.)

## 제약 (§0/§3/§3-A)
- Commands.cs 외 수정 금지. 커널·clone-back·EXPORTLAYOUT·B4a 실측/리소스 로직 무변경(크기 오버라이드+로그만).
- ★`.plans/HANDOFF.md`·`tools/HotReload/` 무접촉. 빈 catch 금지. `git add PlantFlow_Support/Core/Commands.cs`만.
- 빌드 사용자 수동. 완료 후 커밋 + 이 파일 "## RESULT" 기록.

## 완료 기준 (Acceptance)
- [ ] AppendIsoBoundingDimensions가 h(기하 비례, clamp) 기반 Dimtxt/Dimasz/Dimexe/Dimexo/Dimgap 오버라이드
- [ ] dim GeometricExtents 로그 추가
- [ ] 커널·clone-back 무변경, 중괄호 균형, Commands.cs만 커밋, HANDOFF.md/tools 무접촉

## 검증 게이트 (사용자 수동 빌드/라이브 → Claude 판정)
- `PFSVBISOEXPORTED dimH ext=.. h=25 ..` → 원본 Main 2D에 **폭·높이 치수가 육안으로 보이는 크기**로 출현
  (텍스트 ~25mm, 화살표 보임). 실 mm 텍스트(300/75) 정확.
- 3회 반복. 치수 가시 + 실 mm 정확 → **B4a 완료** → 다음(B4b PLN/BOP MLeader 또는 치수 의미 정련 B4c).

## 알려진 후속(별개, 이번 범위 아님)
- 세로 치수 기하가 블록 전체(서포트+파이프원 ≈164)를 재는데 라벨은 서포트만(75) → 의미 정련은 B4c
  (서포트만 치수 또는 파이프경 별도). 이번엔 가시성만.

## RESULT (Codex 기록)
- **status**: done
- **completed_at**: 2026-07-13
- **commit**: b636f83 Scale isolated bounding dimensions

### 변경 요약
- `AppendIsoBoundingDimensions`에서 `s_isoRealWidth/s_isoRealHeight` 기반 `h = max(W,H)/12`를 계산하고 `[10,500]`으로 clamp했습니다.
- `RotatedDimension`별로 `Dimtxt`, `Dimasz`, `Dimexe`, `Dimexo`, `Dimgap`, `Dimtih`, `Dimtoh` 엔티티 오버라이드를 적용했습니다.
- 각 치수 append 후 `dimH/dimV`의 `GeometricExtents`와 `h`를 `PFSVBISOEXPORTED ... ext=... h=...` 형식으로 로깅합니다.
- 오버라이드/extent 조회 실패는 `FileDiag` 후 계속 진행하도록 방어했습니다.

### 검증
- 백업 생성: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260713_b4afix2`
- 중괄호 균형: `opens=359 closes=359 balance=0`
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: whitespace error 없음(CRLF 경고만)
- 핵심 심볼 확인: `ComputeIsoDimensionSize`, `ApplyIsoDimensionOverrides`, `LogIsoDimensionExtents`, `Dimtxt/Dimasz/Dimexe/Dimexo/Dimgap/Dimtih/Dimtoh`
- 빌드는 사용자 수동 원칙에 따라 실행하지 않았습니다.

### 비고
- `.plans/HANDOFF.md`, `tools/HotReload/`는 건드리지 않았습니다.
- 커밋에는 `PlantFlow_Support/Core/Commands.cs`만 포함했습니다.