# REPORT - Codex → Claude

- **cycle**: 91
- **status**: pending_verification
- **completed_at**: 2026-07-20
- **title**: 포트 덤프 + 세로 치수 기준 정합 + U-bolt 태그 조사

## 변경 요약
- 원본 활성 DB에서 선택 서포트의 `Part.GetPorts((PortType)7)` 전량을 인덱스·이름·WCS 좌표로 스냅샷한다. 상세 DB에서는 해당 스냅샷을 `NotabProjectWcsToPaper()`로 투영만 하므로 Plant API가 side DB에서 호출되지 않는다.
- 포트 인덱스 1은 로그에 `role=F2-candidate`로 표시한다. 실제 RC1/RC2/RC3 기둥 위치의 수치 대조는 라이브 추출 로그에서 확정한다. 앵커 작도 로직은 변경하지 않았다.
- RC 가로 치수에서 결정된 `dimHSource`와 `dimReferenceMinX`를 세로 치수선 위치와 두 extension point에 공통 적용했다. params 경로와 legacy 폴백의 기준 혼용을 막는다.
- 자동 포함된 Support 객체만 원본 트랜잭션에서 class/type, `GetSupportDimension()` 전량, 후보 DataLinks 키를 덤프한다. 클래스명 게이트를 통과하지 못한 객체는 기존처럼 `otherPart` 로그로 남는다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 정적 검증
- `git diff --check` 통과.
- 포트 캡처는 원본 DB 구간(`CaptureIsoSelectionMetrics`)에서, 페이퍼 투영은 상세 DB의 viewport 확보 뒤에서 호출되는 것을 확인.
- 세로 치수의 `verticalX`와 두 extension point가 모두 `dimReferenceMinX`를 사용함을 확인.
- 빌드 미실행: 프로젝트 규약에 따라 사용자가 `dev_test.bat`을 수동 실행해야 한다.

## 라이브 검증 필요
- RC1/RC2/RC3: `support-port wcs`, `support-port paper` 전량 및 index=1 `role=F2-candidate`를 실제 기둥 페이퍼 위치와 수치 대조해 기록.
- RC2: `dimH source=params(A+A1)`와 `dim reference source=params(A+A1)`의 minX가 일치하고, 세로 치수 보조선도 같은 x에서 시작하는지 확인.
- U-bolt: `ubolt-probe candidate/support-dims/datalinks` 로그로 class/type 및 태그 후보의 가용성을 판정. `otherPart`만 나오면 클래스명 게이트 미통과로 판정.
- GD1/GD2/GD3 회귀 및 빌드 결과를 사용자 수동 실행 후 갱신.

## 커밋
- `95e9a5a` `notab: dump support ports and ubolt metadata`
