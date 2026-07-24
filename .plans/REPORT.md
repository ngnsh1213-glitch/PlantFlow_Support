# REPORT — Codex → Claude

- **cycle**: 128
- **status**: completed
- **completed_at**: 2026-07-24
- **title**: 유볼트/배관 히든처리 판정 계측 스파이크 (Brep.Faces 표면 덤프, 작도 없음)

## 적용 결과

- `PFS_NOTAB_FACE_PROBE=1`일 때에만 평면화 전 각 `Solid3d`의 `Brep.Faces` 표면을 `PFSNOTABFACE`로 계측한다. 미설정 경로는 호출·로그 모두 없다.
- 표면은 실제 런타임 공개 메서드/속성(`GetSurfaceAsTrimmedSurface` → `Surface`, `BaseSurface`/외부 `Surface`)을 반사로 추적하고, 축·중심·반경 등 읽을 수 있는 파라미터만 기록한다. 개별 면 예외는 skip 로그 후 계속한다.
- 솔리드 분류, 배관 상태(id·중심·반경·축), 각 솔리드 bbox와 배관 분류 솔리드 수를 함께 기록한다. 배관이 없으면 `status=배관 솔리드 부재`를 남긴다.
- 작도·Erase·저장·기존 flatten/블럭 로직은 변경하지 않았다.
- 배관 원 작도는 현행 무탭 코드에 없다. `AddNotabPipeObstacle`은 주석 충돌 회피용 bbox 등록만 수행하며, 반경은 `s_isoPipeRadiusModel` 또는 `TryGetNotabPipePaperRadiusFromDetailSolids`에서 취득한다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증

- `git diff --check` 통과.
- `dotnet build .\\PlantFlow_Support.csproj --no-restore` — 오류 0, 기존 경고 14개.
- 라이브 검증 대기: `PFS_NOTAB_FLATTEN=1 PFS_NOTAB_FACE_PROBE=1` + RC1-001에서 `PFSNOTABFACE` 면 타입·파라미터·분류·배관 상태 로그를 수집한다. 이어서 `PFS_NOTAB_FACE_PROBE` 미설정으로 로그/작도 회귀가 없는지 확인한다.

## 커밋

- `ad890ae feat: add notab face probe spike`
- push하지 않았다.
