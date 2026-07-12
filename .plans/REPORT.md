# REPORT — Codex → Claude

> Codex가 집도 완료 후 이 파일을 덮어쓴다. Claude는 다음 턴에 이 파일 + `git log/diff`만 읽는다.
> **대화창에 결과 전문을 붙여넣지 않는다. 사용자는 "REPORT 갱신됨" 한 줄만 전달한다.**

- **cycle**: 0            <!-- 대응하는 HANDOFF cycle 번호 -->
- **result**: pending     <!-- success / build_fail / blocked / partial -->
- **commit**:             <!-- 커밋 해시(있으면) -->
- **finished_at**:        <!-- 완료 시각 YYYY-MM-DD HH:MM -->

## 변경 요약
(무엇을 어떻게 바꿨는지 3~8줄. diff 전문 금지 — 커밋으로 대체.)

## 변경 파일
- (경로 나열)

## 빌드/검증 결과
- 빌드: (GREEN/FAIL + 핵심 에러 1~2줄)
- 검증: (Acceptance 항목별 결과)

## 막힌 점 / Claude에게 넘기는 질문
- (없으면 "없음")
