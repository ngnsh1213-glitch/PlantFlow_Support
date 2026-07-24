# REPORT — Codex → Claude

- **cycle**: 127
- **status**: completed
- **completed_at**: 2026-07-24
- **title**: 무탭 평면화 솔리드 분류 + 카테고리 블럭화 (Phase 1)

## 적용 결과

- 투영 폴리라인을 솔리드 단위로 support/pipe/유볼트(tag별) 분류해 누적한다.
- 유볼트는 확장 extents 포함을 우선하고, 복수 후보는 중심 최소거리로 태그를 결정한다. 배관은 축 수직거리와 축 방향 extents 겹침을 함께 확인한다.
- 전체 투영 bbox 좌하단을 공유 기준점으로 사용해 `PFS_FLAT_SUPPORT`/`PFS_FLAT_PIPE`/`PFS_FLAT_UB_<tag>` 블럭 정의와 `BlockReference`를 생성한다. 엔티티와 참조는 모두 `PFS_ISO_DETAIL` ByLayer다.
- 분류 예외는 support로 보존하고 `PFSNOTABBLOCK unclassified` 로그를 남긴다. 기존 솔리드·뷰포트 삭제 안전장치는 유지한다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증

- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.sln --no-restore` — 오류 0, 기존 경고 14개.
- 라이브 검증 대기: `PFS_NOTAB_FLATTEN=1` + RC1-001에서 `PFSNOTABBLOCK support=… pipe=0 ubolt=UB-002,UB-003` 로그와 3개 BlockReference, 좌표 무이동, EXPLODE 무손실을 확인한다. 미설정 경로도 회귀 확인한다.

## 커밋

- `5c9cb25 feat: group flattened solids into category blocks`
- push하지 않았다.
