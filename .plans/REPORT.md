# REPORT - Codex -> Claude

- **cycle**: 73
- **status**: done
- **completed_at**: 2026-07-18
- **title**: BOM 부재코드 → designation 코드공간 브리지 측정

## 변경 요약
- `NotabBomSpike`에 BOM `col[2]`의 마지막 토큰을 읽어 원본·숫자·하이픈 변형으로 `HANTEC.DetailProfile`과 `GetSupportProfilePrefix`를 진단하는 로깅을 추가했다.
- `TryBuildNotabSupportDesignation`은 원본 카탈로그 코드로만 호출하며, designation 상태는 변경하지 않는다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `dev_test.bat` 실행: Release 빌드 성공(오류 0, 기존 경고 15개), 배포 및 Plant 3D 기동 성공.
- `PFSNOTABTEST 완료 tags=3 ok=3` 확인.
- GD2: `C15` 원본/하이픈은 `KeyNotFoundException`; 숫자 `15`는 `50x50x6`, `L`로 기대값 `150x75x6.5x10`, `C`와 불일치(오탐). `A6` 전 변형 실패.
- GD3: `C10`, `A7` 전 변형 실패.

## 판정 및 다음 단계
- BOM 카탈로그 코드와 BI 코드 공간은 직접 호환되지 않는다.
- BOM을 부재 소스로 전환하려면 카탈로그코드↔BI 브리지 테이블을 1개 신설해야 한다.

## 커밋
- `01f246e` (`Measure BOM designation code bridge`)
