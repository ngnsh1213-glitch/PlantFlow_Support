# REPORT - Codex -> Claude

- **cycle**: 65
- **status**: done
- **completed_at**: 2026-07-17
- **title**: 서포트 자동포함을 태그필터에서 접촉박스로 대체

## 변경 요약
- `AutoIncludeRelatedParts`의 Support 후보 포함 판정을 `probe(anchor +/- PFS_NOTAB_MARGIN)`에서 별도 접촉박스 `anchor +/- PFS_NOTAB_SUPPORT_TOL`로 변경했다.
- `PFS_NOTAB_SUPPORT_TOL` 기본값은 50mm이며, 허용 범위는 0~10000으로 두었다.
- cycle64의 `SupportName`/`sameSupport` 태그 기반 게이트와 `PFS_NOTAB_INCLUDE_NEIGHBOR_SUPPORT` 분기를 제거했다.
- 미사용 `TryGetSupportName` 헬퍼를 제거했다.
- 파이프 포함, 앵커 계산, 클립, held-pipe/BOP 선택 로직은 수정하지 않았다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 백업
- 별도 백업 파일 생성 없음. 변경 범위가 단일 함수 내부이며 Git diff로 롤백 가능.

## 검증
- 변경 주변 20줄 수동 확인 완료.
- `rg`로 `PFS_NOTAB_INCLUDE_NEIGHBOR_SUPPORT`, `TryGetSupportName`, `selectedSupTag`, `sameSupport`, `candTag`, `includeNeighborSup` 잔존 참조 없음 확인.
- `git diff -- PlantFlow_Support/Core/Commands.cs` 확인: 지정 Support 분기와 미사용 헬퍼 제거만 포함.
- 빌드는 프로젝트 규칙에 따라 사용자 명시 요청이 없어 실행하지 않음.

## 커밋
- 코드 반영 커밋: `e825df0` (`Use contact box for notab support include`)
