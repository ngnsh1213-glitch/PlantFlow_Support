# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 0            <!-- 사이클 번호. Claude가 발행할 때마다 +1 -->
- **status**: idle        <!-- ready = Codex 착수 대기 / done = Codex 완료(REPORT 참조) / idle = 대기 없음 -->
- **issued_at**:          <!-- 발행 시각 YYYY-MM-DD HH:MM -->

## 작업 지시
(무엇을 할지 1~5줄. 배경은 계획서로 링크.)

## 대상 파일
- (경로 나열)

## 계획서 / 근거
- (plan_*.md 경로 또는 SESSION.md 해당 섹션)

## 완료 기준 (Acceptance)
- [ ] 빌드 GREEN (`CleanAndBuild.bat` 또는 build_project.bat)
- [ ] (기능 검증 항목)

## 제약
- 집도 잠금 준수: 이 지시 범위 밖 수정 금지. 계측 우선, 빈 catch 금지.
- 변경은 SEARCH/REPLACE 리터럴로. 완료 후 커밋(attribution 금지).
