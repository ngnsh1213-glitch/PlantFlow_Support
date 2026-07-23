# PlantFlow_Support — 프로젝트 규칙 (Claude / Codex 공용)

> 이 파일은 **이 리포 전용** 운영 규칙입니다. 개인 글로벌 규칙(`~/.claude/CLAUDE.md`)을 상속하되, 충돌 시 이 파일이 우선합니다.
> Codex는 `AGENTS.md`를 읽습니다. 두 파일의 운영수칙은 항상 동일하게 유지합니다.

## 1. 프로젝트 개요
- **PlantFlow_Support (PFS)**: AutoCAD Plant3D 기반 파이프 서포트 디테일 도면 자동 생성 도구 (C#, .NET). PFO(오쏘)와 형제 제품군, **별도 리포로 분리**.
- 원격: https://github.com/ngnsh1213-glitch/PlantFlow_Support  (기본 브랜치 `master`)
- **이 리포 루트가 곧 프로젝트 루트**(`.sln`·`.csproj`가 루트에 있음). Claude Code·Codex 모두 이 폴더를 작업 폴더로 연다.

## 2. 상태 문서 (Single Source of Truth)
작업 흐름은 아래 3개 문서로 관리한다. **매 작업 종료 시 갱신 후 커밋**한다.

| 파일 | 용도 | 갱신 시점 |
|---|---|---|
| `SESSION.md` | 현재 진행 중인 작업 상태·직전 실측 사실·다음 결정 분기 | 세션 시작/종료, 컨텍스트 전환 시 |
| `TODO.md` | 잔여 작업 백로그 (완료 항목은 CHANGELOG로 이동) | 작업 착수/완료 시 |
| `CHANGELOG.md` | 사람이 읽는 변경 이력 요약 (git 커밋과 별개) | 의미 있는 변경 완료 시 |

## 3. 집도(코드 수정) 규칙
1. **집도 잠금**: 명시적 승인 없이 코드 수정 금지. 기본 모드는 분석(수정 금지).
2. 계획 제안 후 "집도할까요?" 승인 → 수정. "계속/진행해" 등 문맥 동의도 승인으로 처리.
3. 수정 직후 변경 전후를 다시 읽어 검증한다.
4. **계측 우선**: 맹목 apply 금지. 로그·실측으로 확인 후 수정. 방어적 프로그래밍, 빈 catch 금지.

## 3-A. 핸드오프 왕복 규약 (Claude ↔ Codex, 복사 왕복 제거)
복사-붙여넣기 왕복을 없애기 위해 `.plans/` 두 파일로 주고받는다. **대화창에 지시/결과 전문을 붙여넣지 않는다.**

| 파일 | 방향 | 규칙 |
|---|---|---|
| `.plans/HANDOFF.md` | Claude → Codex | Claude가 지시를 여기에 **덮어쓴다**. `status: ready`, `cycle` +1. 대화창엔 "HANDOFF cycle N 발행" 한 줄만. |
| `.plans/REPORT.md` | Codex → Claude | Codex가 완료 후 결과를 여기에 **덮어쓰고** 코드를 커밋. 대화창엔 "REPORT cycle N 갱신" 한 줄만. |

- 흐름: ①Claude가 `HANDOFF.md` 발행 → ②사용자가 Codex에 "`.plans/HANDOFF.md` 읽고 집도" 지시(§5 수동 위임 준수) → ③Codex가 집도·커밋 후 `REPORT.md` 기록 → ④사용자가 "REPORT 갱신됨" 통보 → ⑤Claude가 `REPORT.md` + `git log/diff`만 Read.
- Claude·Codex 모두 결과 diff 전문을 본문에 싣지 않는다(커밋으로 대체) → 토큰 절약.
- `HANDOFF.md`/`REPORT.md`는 git 추적(왕복 이력 보존). 계획서 상세는 `.plans/plan_*.md`.

### 트리거 예약어 (사용자 입력 간소화)
| 입력 | 대상 | 의미 |
|---|---|---|
| `1` | Codex | ".plans/HANDOFF.md 읽고 집도" |
| `2` | Claude | ".plans/REPORT.md 읽고 이어가기" |
| `3` | Claude | "현재 과업을 계획→자문(§9)→핸드오프 발행까지 순차 실행" |

- **엄격 일치**: 메시지를 trim했을 때 **정확히 `1`/`2`/`3` 단독**일 때만 트리거로 해석한다.
- 다른 텍스트가 섞이면(`1번 먼저`, `2. 그리고`, `step 1`) **일반 문장**으로 처리(트리거 아님).
- Claude는 `2` 단독 입력을 받으면 `REPORT.md` + `git log/diff`를 읽고 작업을 이어간다.

### `3` = 계획→자문→핸드오프 순차 실행 (Claude 전용)
현재 대화에서 논의 중인 과업에 대해 아래를 **무정지로 순차 수행**한다:
1. **계획**: 요구 분해 → 위험 평가 → 필요 시 `.plans/plan_*.md` 초안.
2. **자문(§9)**: Codex MCP 단독 1회 종합(Gemini는 사용자가 명시 요청 시에만 추가). 한도 `review_round ≤ 3`, 누적 응답 ≤ 50K 토큰. 사용자가 "중단/그만"을 입력하면 즉시 정지.
3. **핸드오프 발행**: `.plans/HANDOFF.md`에 `cycle +1`, `status: ready` 기록 후 "HANDOFF cycle N 발행" 한 줄 출력.
- **정지점**: `3`은 **발행까지만**이다. 집도 착수는 글로벌 §5(수동 위임)에 따라 사용자가 `1`을 Codex에 입력해야 시작된다(`3`이 `1`을 자동 호출하지 않는다).
- 논의 중인 과업이 불명확하면 자문·발행 전에 대상을 먼저 확인한다.

## 4. 커밋·동기화 규칙
- 파일 수정 후 커밋한다(attribution 금지, push는 요청 시 또는 세션 종료 시).
- 커밋 전 `git status`로 민감 파일(비밀키·백업·대용량 DLL) 제외 확인.
- **빌드는 완료 조건**이다. `dotnet build` 오류 0을 확인하지 않은 코드는 커밋하지 않는다.
  Codex의 REPORT를 수령하면 Claude가 **직접 빌드를 재확인**한다(2026-07-21 cycle98 사고 근거).
- **push된 커밋은 amend·rebase 금지.** 수정이 필요하면 새 커밋을 쌓는다.
  집도자(Codex 등)는 **push하지 않는다** — push는 사용자가 수행한다.
  (2026-07-21: push 후 amend로 원격과 이력이 갈라져 merge 충돌 발생.)
- 이력이 갈라진 경우: 로컬이 검증된 최신이면 `git merge -s ours origin/master`로
  원격 이력만 수용한 뒤 push한다. force push는 쓰지 않는다.
- 모든 대화·산출물은 **한글**. 과장/비유 배제.
