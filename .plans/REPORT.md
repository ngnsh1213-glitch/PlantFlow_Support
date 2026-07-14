# REPORT — Codex → Claude

- cycle: 29
- status: done
- commit: created (current HEAD)
- target: `PlantFlow_Support/Core/Commands.cs`
- build: not_run (빌드 수동 원칙)

## 변경 요약
- `CreateNotabDetailDrawing`을 템플릿 DB 기반에서 평범 DB 기반으로 재구성.
  - `Database(true, true)`로 `detailDb` 생성.
  - `CopyCleanNotabSolids(...)`로 N1 source DB의 `Solid3d`를 `detailDb` ModelSpace에 CopyFrom 이송.
  - 기존 템플릿 DB에는 3D solid/3D viewport를 넣지 않음.
- `detailDb` layout 준비 helper 추가.
  - `EnsureNotabDetailLayout(...)`: `"Title Block"` layout 우선, 없으면 `"Layout1"`을 `"Title Block"`으로 rename 시도.
  - `TryConfigureNotabA1Layout(...)`: Layout limits를 841x594로 설정 시도(reflection, 실패 시 로그 후 계속).
- 템플릿 타이틀블록 2D 병합 helper 추가.
  - `CloneTemplateTitleBlock2D(...)`: 템플릿 `"Title Block"` paperspace 엔티티 중 `Viewport` 제외 후 `detailDb` layout paperspace로 `WblockCloneObjects`.
  - 로그: `PFSNOTABDETAIL titleblock clone cloned=N source=M`.
- 타이틀블록 속성 갱신 전용 helper 추가.
  - `UpdateIsoTitleBlockAttributesInLayout(...)`: 이번 detail layout의 `DRAWING_TITLE`만 업데이트.
  - 기존 공유 `UpdateIsoTitleBlockAttributes(...)`는 `"Title Block"` 전용 동작 유지.
- Hidden/3D viewport는 평범 DB인 `detailDb`에서만 생성/설정.
  - 기존 `CreateNotabDetailViewport` + `ConfigureNotabDetailViewport` 흐름 유지.
  - `PFS_NOTAB_SKIP_VIEWPORT` 진단 토글도 유지.

## 산출 파일
- 수정: `PlantFlow_Support/Core/Commands.cs`
- 백업: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_notab_plain_db_titleclone`

## 검증
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS (CRLF 경고만 출력)
- `rg plainDb/CloneTemplateTitleBlock2D/EnsureNotabDetailLayout/UpdateIsoTitleBlockAttributesInLayout/titleblock clone`: PASS
- 주변 코드 확인:
  - `PFSNOTABDETAIL` 저장 경로는 `<ProjectDwgDirectory>\Details\<safeTag>_notab.dwg` 유지.
  - 기존 `PFSVBISO*`, `CreateIsoDetailDrawing` 직접 수정 없음.
  - 공유 titleblock helper는 원래 `"Title Block"` lookup 유지.
- 빈 catch 없음.
- 빌드/Plant3D 실행 검증은 지시대로 수행하지 않음.

## 커밋 상태
- 커밋 완료: `Rebuild notab detail on plain database`
- stage 대상은 `PlantFlow_Support/Core/Commands.cs`, `.plans/REPORT.md`만 포함.

## 라이브 검증
1. 수동 빌드 후 `PFSNOTABDETAIL` 실행.
2. 기대 로그:
   - `PFSNOTABDETAIL cleanSolid copied=N ext=...`
   - `PFSNOTABDETAIL titleblock clone cloned=N source=M`
   - `PFSNOTABDETAIL plainDb cloned=N viewport=ok titleblockClone=N titleblockUpdate=...`
   - `PFSNOTABDETAIL commit 완료`
   - `PFSNOTABDETAIL saved path=..._notab.dwg`
3. 크래시가 사라지면 템플릿 DB + 3D viewport commit 회피 성공.
4. A1 프레임/타이틀블록 좌표가 어긋나면 다음 cycle에서 배치 미세조정.
