# REPORT — Codex → Claude

- **cycle**: 117
- **status**: completed
- **completed_at**: 2026-07-23
- **title**: 무탭 RS1~5 결함 일괄 수정

## 결과

- F1: RS1~5 무탭 타입 설정을 추가했다. RS1/2는 세로 치수를 생략하고, RS3/4는 F2+세로 포트 앵커, RS5는 Ha 세로 치수를 사용한다.
- F3: RS4만 A+A1 기반 가로 치수 계산 대상으로 추가했다. 기존 RC 목록과 RS1/2/3의 legacy bbox 동작은 유지했다.
- F4: RS1의 F1 BOM 길이를 A만 사용하도록 수정했고, RS11 및 GD1 경로는 유지했다.
- F5: RS5/RS6은 Ha/Hb 기반 3개 프레임 행을 별도 생성한다. A/A1/Ha/Hb/BI 중 누락·공백이 있으면 전체 행을 만들지 않고 원시값 포함 FileDiag를 남긴다. label_71 및 RS12A 경로는 변경하지 않았다.
- F6: BOM 원천 계측 예외 로그에 표준명, supportId, 예외 타입 및 KeyNotFound 키 정보를 추가했다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`
- `PlantFlow_Support/Models/BOMs.cs`

## 검증

- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.sln --no-restore` 통과: 오류 0, 경고 14.

## 라이브 검증 필요

- dev_test.bat의 RS1~5 태그 설정으로 핸드오프 표의 치수·BOM·밸룬 결과 및 RS4 분할 방향을 확인한다.
- RC1~9, GD1~3, RS12A를 재추출해 회귀가 없는지 확인한다.

## 커밋

- 코드: `8bf878d` (`fix: correct notab RS1-RS5 dimensions and BOM`)
