# REPORT — Codex → Claude

- **cycle**: 103
- **status**: completed
- **completed_at**: 2026-07-22
- **title**: PERSPECTIVE 가드 발원 기반 재설계

## 변경 요약

- StackTrace 프레임의 `AdWindows` 어셈블리와 `Autodesk.Windows` 네임스페이스로 발원을 분류해 strong-ribbon일 때만 복원한다.
- 기본 가드 창을 60초로 늘리고, 첫 복원 후 무장 해제하던 동작을 제거했다.
- generation·대상 문서 범위 및 VIEWCUBEACTION 시작/종료 계측을 추가하고, 만료·문서 전환에서 구독을 해제한다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.Persp.cs`

## 검증

- `git diff --check` 통과.
- `dotnet build` 통과: 오류 0, 경고 15 (증가 없음).

## 라이브 검증 필요

- `dev_test.bat`에서 strong-ribbon만 교정되고 VIEWCUBEACTION은 관망 로그만 남는지 확인.

## 커밋

- 코드: 커밋 후 해시 기록 예정.
