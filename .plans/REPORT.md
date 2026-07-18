# REPORT - Codex -> Claude

- **cycle**: 74
- **status**: done
- **completed_at**: 2026-07-18
- **title**: 부재 콜아웃 소스 config→BOM 전환 (비파괴 augment, config는 폴백 유지)

## 변경 요약
- `HANTEC.BeamProfile`의 기존 BI→BOM 값 맵을 공유 메서드로 추출하고, BOM `col[2]` 값에서 BI를 찾는 `BeamProfileBI` 역조회 메서드를 추가했다.
- `CaptureIsoSupportProfile` 호출 직후 BOM 부재를 앵글→채널→기타 순서로 보강한다. BOM 목록이 기존 콜아웃 집합과 다를 때만 콜아웃 목록을 교체한다.
- BOM 보강은 `s_isoSupportBI`, `s_isoSupportProfile`, `s_isoSupportProfileHeight`를 변경하지 않는다. 기존 config `MemberBIs` 폴백과 `NotabBomSpike` 행 로그는 유지했고, cycle73의 `bom-bridge` 진단 루프는 제거했다.

## 변경 파일
- `PlantFlow_Support/Ortho/HANTEC.cs`
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- `git diff --check` 통과.
- `BeamProfileMap`, `BeamProfileBI`, `AugmentDesignationsFromBom` 및 Capture 직후 호출을 정적 확인.
- `bom-bridge` 진단 코드가 제거된 것을 검색으로 확인.
- 빌드는 프로젝트 규칙에 따라 실행하지 않음.

## 라이브 검증 필요
- `dev_test.bat`에서 `PFS_NOTAB_TEST_TAG=GD1-001,GD2-001,GD3-001`로 실행.
- GD1/GD2 `bom-augment 동일 유지`, GD3 `L-75×75×9 | C-100×50×5×7.5` 교체와 GD3 치수 불변을 확인.

## 커밋
- 대기 중
