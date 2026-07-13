# REPORT — Codex → Claude

- cycle: 25
- status: done
- commit: created (current HEAD)
- target: `PlantFlow_Support/Core/Commands.cs`
- build: not_run (빌드 수동 원칙)

## 변경 요약
- `CreateNotabDetailDrawing`의 템플릿 로드 직후 `tagDb.CloseInput(true)`를 추가해 DWG 입력 스트림을 해제.
- N2 템플릿 DB 이송 경로에서 `sourceDb.WblockCloneObjects(...)`를 제거.
- 신규 `CopyCleanNotabSolids(...)`를 추가해 source DB의 `Solid3d`를 읽고 target DB 모델공간에 새 `Solid3d`로 `CopyFrom` 생성.
  - target DB 트랜잭션과 source DB 트랜잭션을 분리.
  - 각 solid 실패는 `FileDiag` 후 skip, 전체 중단 없음.
  - 성공 로그: `PFSNOTABDETAIL cleanSolid copied=N ext=...`
- 신규 `TryStripCleanSolidMetadata(...)`로 clean solid의 ExtensionDictionary/xdata 제거를 시도.
  - `ReleaseExtensionDictionary`는 AutoCAD API 버전 차이를 피하려고 reflection으로만 호출.
  - 실패/미지원은 `FileDiag`로 남김.
- 기존 `PFS_NOTAB_SKIP_HIDDEN`, `PFS_NOTAB_SKIP_VIEWPORT`, 2단계 viewport 진단 경로는 유지.

## 산출 파일
- 수정: `PlantFlow_Support/Core/Commands.cs`
- 백업: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260714_notab_clean_copyfrom`

## 검증
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: PASS (CRLF 경고만 출력)
- `rg CopyCleanNotabSolids/cleanSolid copied/tagDb.CloseInput`: PASS
- `rg sourceDb.WblockCloneObjects(ids, msId...)`: N2 대상 경로 제거 확인. 다른 기존 경로는 보존.
- 빈 catch 없음.
- 빌드/Plant3D 실행 검증은 지시대로 수행하지 않음.

## 커밋 상태
- 커밋 완료: `Fix notab clean solid transfer`
- stage 대상은 `PlantFlow_Support/Core/Commands.cs`, `.plans/REPORT.md`만 포함.

## 라이브 검증
1. 수동 빌드 후 `PFSNOTABDETAIL` 재실행.
2. 기대 로그:
   - `PFSNOTABDETAIL cleanSolid copied=N ext=...`
   - `PFSNOTABDETAIL commit 직전`
   - `PFSNOTABDETAIL commit 완료`
   - `PFSNOTABDETAIL saved path=...`
3. 크래시가 사라지면 Plant 잔재 solid 정화 이송 원인 확정.
4. 계속 크래시하면 `cleanSolid copied` 이후 어느 단계 로그에서 멈추는지로 다음 분기.
