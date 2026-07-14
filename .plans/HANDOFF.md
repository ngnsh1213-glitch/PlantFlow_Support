# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 38
- **status**: ready
- **issued_at**: 2026-07-14
- **title**: 크래시 회귀 격리 — cycle37 D(TileMode/활성레이아웃) 원복, A/B/C/E 유지
- **target**: PlantFlow_Support/Core/Commands.cs
- **plan**: `<appDataDir>\scratch\plan_pfs_notab_layout_fit_20260714.md`

## 착수 전
- cwd `D:\PlantFlow\PlantFlow_Support`. `CreateNotabDetailDrawing` 및 저장 전 레이아웃 활성화 코드만. 그 외 무수정. 진단 로그 유지.
- 빌드 GREEN까지 Codex 확인. 라이브는 사용자.

## 배경 (cycle 37 라이브 = 크래시 회귀)
- cycle 37(커밋 c432ae6 레이아웃 + d7897e5 H5) 라이브: **AutoCAD Plant 3D 네이티브 크래시**(소프트 경고→하드 크래시 악화).
- 로그: H5(`sourceDb disposed(before-save) ok`)·A/B/C(뷰포트 fixedRect·target support·scale 1:4) 정상 적용. **`layout active=Title Block tilemode=0 zoomExtents=skip`(D) 직후 `wblock-save fallback SaveAs: eInvalidInput` → 크래시.**
- 진단: 크래시 유력 원인 = **D(헤드리스 side-DB에 `TileMode=0` + 활성 레이아웃 Title Block 전환)**. SESSION 이력 원조 N2 크래시(헤드리스 DB에 페이퍼공간 상태 강제→Plant 세션 네이티브 크래시)와 동일 계열. A/B/C/E는 속성 세팅이라 크래시 개연성 낮음.

## 집도 지시 (격리 — 단일)
- cycle 37의 **D 작업(초기화면=Title Block 페이퍼 공간)만 원복**:
  - `detailDb.TileMode = false` 설정 제거(기본 TileMode=1 유지).
  - 활성 레이아웃 `Title Block` 전환 코드 제거.
  - zoomExtents 관련 코드 제거.
  - `layout active=... tilemode=... zoomExtents=...` 로그 제거.
- **유지(원복 금지)**: A(뷰포트 fixedRect), B(target 서포트중심/supportExt), C(scale 1:4), E(H5 sourceDb 조기 Dispose). 이들은 그대로 둔다.
- 저장 경로는 cycle 34의 `SaveNotabDetailWithWblockFallback` 그대로.

## 검증 (Codex)
- `dotnet build` GREEN(오류 0). D 관련 코드만 제거됐고 A/B/C/E 무손상 확인.
- `git diff --check` PASS. 빈 catch 없음. `CreateIsoDetailDrawing`/`PFSVBISO*` 무수정.
- REPORT에 제거한 D 코드 범위 명시.

## 라이브 검증 (사용자, 빌드 후)
1. `PFSNOTABDETAIL` 실행.
2. 판정:
   - **크래시 소멸 여부**(D 원복으로) — 소멸 시 D가 크래시 원인 확정.
   - 크래시 없이 저장되면: 레이아웃 A/B/C 육안(뷰포트 프레임 정합·서포트 중앙·1:4) + **RECOVER 경고 소멸 여부(H5 판정)**.
3. 분기:
   - 크래시 소멸 + RECOVER 소멸 → **H5 확정(트랙 ① 종결)** + 레이아웃 정합. #1(페이퍼 초기화면)은 안전한 방법으로 후속 검토.
   - 크래시 소멸 + RECOVER 잔존 → H5 기각, RECOVER 재defer. 레이아웃만 확정 후 N3.
   - 크래시 잔존 → D 외 원인(A/B/C/E) → 추가 격리.
