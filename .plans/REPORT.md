# REPORT - Codex → Claude

- **cycle**: 92
- **status**: pending_verification
- **completed_at**: 2026-07-20
- **title**: RC 기둥 포트 앵커 + U-bolt 태그 콜아웃

## 변경 요약
- RC1/RC2/RC3의 세로 치수 extension point와 치수선 기준을 `S2`(index=1, 이름 검증) 포트 및 F2 높이로 분리했다. 포트·F2·투영·support extents 가드 실패 시 기존 앵커로 폴백한다.
- 세로 부재 콜아웃은 유효한 같은 S2/F2 앵커의 기둥 중점에 접속한다. GD 계열 및 기존 좌우 배치 규칙은 변경하지 않았다.
- 자동 포함된 U-bolt의 `Tag`와 원본 bbox 중심을 별도 스냅샷하고, 상세 단계에서 각 U-bolt마다 독립 콜아웃을 배치한다. 빈 태그·중복 태그·`ShortDescription != UB`는 제외한다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`
- `SESSION.md`
- `CHANGELOG.md`

## 정적 검증
- `git diff --check` 통과.
- 호출부/시그니처 단일 일치 확인.
- 변경 주변 코드와 U-bolt 캡처·투영·배치 순서(치수 → 파이프 → 부재 → U-bolt) 확인.
- 빌드 미실행: 프로젝트 규약에 따라 사용자가 `dev_test.bat`을 수동 실행해야 한다.

## 라이브 검증 필요
- RC1/RC2/RC3: `dimH ... | dimV src=port-S2` 로그와 실제 기둥 상·하단 접촉을 대조.
- RC1-001: `ubolt capture tag=UB-002/UB-003` 및 U-bolt 콜아웃 2개와 상호 비겹침 확인.
- GD1/GD2/GD3 회귀 확인.

## 커밋
- 커밋 후 갱신 예정.
