# AGENTS — PlantFlow_Support (Codex 진입 규칙)

> Codex는 이 파일을 읽는다. 운영수칙은 `CLAUDE.md`와 동일하게 유지한다.
> 둘 중 하나를 고치면 다른 하나도 함께 갱신한다.

## 프로젝트
- PlantFlow_Support (PFS): AutoCAD Plant3D 파이프 서포트 디테일 도면 자동 생성 (C#, .NET). PFO와 형제 제품군, 별도 리포.
- 원격: https://github.com/ngnsh1213-glitch/PlantFlow_Support (기본 브랜치 `master`)
- 이 리포 루트 = 프로젝트 루트(`.sln`·`.csproj`가 루트). Claude·Codex 공용.

## 상태 문서 (반드시 참조·갱신)
- `SESSION.md` — 현재 작업 상태·직전 실측·다음 분기. 세션 시작 시 먼저 읽는다.
- `TODO.md` — 잔여 백로그. 착수/완료 시 갱신.
- `CHANGELOG.md` — 변경 이력 요약. 의미 있는 변경 완료 시 추가.

## 핵심 규칙
1. **집도 잠금**: 명시적 승인 없이 코드 수정 금지. 기본 모드는 분석.
2. 계획 → 승인 → 집도. 수정 후 변경 전후 재검증. **계측 우선, 맹목 apply 금지**.
3. 변경 사항은 리터럴 코드 블록(SEARCH/REPLACE)으로 명시.
4. 방어적 프로그래밍, 빈 catch 금지.
5. 파일 수정 후 커밋. push 전 `git status`로 민감 파일 제외 확인.
6. 모든 산출물 **한글**, 과장/비유 배제.
