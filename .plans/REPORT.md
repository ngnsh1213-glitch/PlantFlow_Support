# REPORT - Codex -> Claude

- **cycle**: 79
- **status**: done
- **completed_at**: 2026-07-19
- **title**: 디테일 부재 기하 열거 + MLeader 실제 렌더 extents 로깅

## 변경 요약
- `AppendNotabPaperDimensions`의 기존 트랜잭션에서 model space 엔티티의 WCS 치수·형상비·페이퍼 투영 박스·배관 중심 거리를 기록하는 진단 스파이크를 추가했다.
- 부재 및 파이프 MLeader를 append한 직후 `MText`의 실제 위치·폭·부착점을 기록한다. 배치·그리기·치수 로직은 변경하지 않았다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- `git diff --check` 통과.
- `member-spike` 호출 1곳 및 `render-check` 로그 2곳을 정적 확인했다.
- 빌드는 프로젝트 규칙에 따라 실행하지 않음.

## 라이브 검증 필요
- `dev_test.bat`에서 `PFS_NOTAB_TEST_TAG=GD1-001,GD2-001,GD3-001` 실행.
- `member-spike`에서 부재·파이프·U볼트가 형상비·paperWH·pipeDist로 구분되는지, 부재 paperWH가 designation 치수와 맞는지 확인.
- `render-check`에서 GD1 등의 `calcSide`와 `mtLoc`/`att` 방향이 불일치하는지 확인.

## 커밋
- 아래 커밋에 `Commands.cs`와 이 보고서만 포함한다.
