# REPORT - Codex -> Claude

- **cycle**: 67
- **status**: done
- **completed_at**: 2026-07-17
- **title**: GD2 고정 부재 BI 기반 이중 콜아웃

## 변경 요약
- `NotabTypeConfig`에 `MemberBIs`를 추가하고 GD2에 `215`, `16` 고정 BI를 설정했다.
- 부재 designation을 리스트(`s_isoSupportDesignations`)로 보존하도록 확장했다.
- SupportParams `BI`가 있는 타입은 기존처럼 단건 designation을 리스트에 1개만 넣어 하위호환을 유지했다.
- SupportParams `BI`가 비어 있는 타입은 `GetNotabStandardName()`/`GetNotabTypeConfig()`의 `MemberBIs`로 `HANTEC.DetailProfile` designation을 구성한다.
- `AppendNotabProfileCallout`은 designation 리스트를 순회해 `idx=0`은 기존 위치, 이후 항목은 `txt * 1.8` 간격으로 아래에 적층 렌더한다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 백업
- 별도 백업 파일 생성 없음. 변경 범위가 단일 파일 내부이며 Git diff로 롤백 가능.

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `rg`로 `s_isoSupportDesignations`, `MemberBIs`, `CaptureIsoSupportProfileFromConfig`, `TryBuildNotabSupportDesignation`, `callout append idx`, `profile designations count` 참조 확인.
- `git diff -- PlantFlow_Support/Core/Commands.cs` 확인: 지정된 부재 콜아웃/config/profile 경로만 포함.
- 빌드는 프로젝트 규칙에 따라 사용자 명시 요청이 없어 실행하지 않음.

## 커밋
- 코드 반영 커밋: `9a570cd` (`Add GD2 notab member callouts`)
