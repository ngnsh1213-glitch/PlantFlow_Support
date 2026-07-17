# REPORT - Codex -> Claude

- **cycle**: 64
- **status**: done
- **completed_at**: 2026-07-17
- **title**: 추출 오염 차단 - 이웃 서포트 자동포함 게이트

## 변경 요약
- `AutoIncludeRelatedParts`의 서포트 자동포함 분기를 강화했다.
- 기본값에서는 후보 서포트의 `SupportName`이 선택 서포트 `SupportName`과 같은 경우만 포함한다.
- `PFS_NOTAB_INCLUDE_NEIGHBOR_SUPPORT >= 0.5`일 때만 기존처럼 근접 이웃 서포트 포함을 허용한다.
- `TryGetSupportName` 헬퍼를 추가해 `SupportName` 조회 실패 시 로그를 남기고 안전하게 제외되도록 했다.
- 파이프 포함, 앵커 계산, 클립, held-pipe/BOP 선택 로직은 수정하지 않았다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 백업
- `PlantFlow_Support/Core/Commands.cs.codex_bak_20260717_cycle64`

## 검증
- 변경 주변 20줄 수동 확인 완료.
- `git diff --check -- PlantFlow_Support/Core/Commands.cs` 실행: 오류 없음. 단, Git의 CRLF 변환 안내 경고만 출력됨.
- `git diff -- PlantFlow_Support/Core/Commands.cs` 확인: 지정 분기와 `SupportName` 조회 헬퍼만 변경.
- 빌드는 프로젝트 규칙에 따라 사용자 명시 요청이 없어 실행하지 않음.

## 커밋
- 코드 반영 커밋: `e37761e` (`Gate notab support auto include`)
