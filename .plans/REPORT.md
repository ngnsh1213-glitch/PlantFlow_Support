# REPORT - Codex → Claude

- **cycle**: 93
- **status**: pending_verification
- **completed_at**: 2026-07-20
- **title**: HANTEC → StandardSupport 개명 + DesignStd 외부화

## 변경 요약
- `HANTEC` 클래스·파일·정적 호출을 `StandardSupport`로 개명했다.
- `PFS STANDARD`와 기존 `HANTEC`을 공백·대소문자 무시로 판정하는 단일 헬퍼와 로그를 추가했다.
- BOM 및 오쏘 주석이 실제 `DesignStd`를 사용하도록 변경했고, 주석 생성 전에 BOM을 초기화해 `StandardName` 누락을 해소했다.
- 무탭 BOM은 `s_isoDesignStd`를 전달하며, 값이 비어 있으면 기존 표준으로 폴백하고 로그를 남긴다.

## 변경 파일
- `PlantFlow_Support/Ortho/StandardSupport.cs` (기존 `HANTEC.cs` 개명)
- `PlantFlow_Support/Models/BOMs.cs`
- `PlantFlow_Support/Ortho/PlantAutoCoding.cs`
- `PlantFlow_Support/Ortho/OrthoViewportManager.cs`
- `PlantFlow_Support/Core/Commands.cs`

## 정적 검증
- 대상 C# 파일에서 이전 `HANTEC` 식별자 참조 제거 확인.
- 호환 리터럴 `HANTEC`은 지원 목록 및 빈 DesignStd 폴백에만 유지 확인.
- `git diff --check` 통과.
- 빌드 미실행: 프로젝트 규약에 따라 사용자가 `dev_test.bat`을 수동 실행해야 한다.

## 라이브 검증 필요
- `DesignStd = PFS STANDARD` 및 `HANTEC` 각각에서 오쏘 BOM·주석 생성 확인.
- 무탭 RC1/RC2/RC3 및 GD1/GD2/GD3 회귀 확인.
- 로그 `StandardSupport design standard value=... supported=True` 확인.

## 커밋
- 완료 후 기록