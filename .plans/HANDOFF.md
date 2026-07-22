# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 116
- **status**: ready
- **issued_at**: 2026-07-23
- **title**: 세로재 밸룬 리더 관통 연장(기본 20) — RC5 F2 마무리
- **작업 경로**: `Core/Commands.cs` (세로재 member-end 리더 끝점, cycle109에서 endMinX/endMaxX=rawAnchor.X로 만든 지점)
- **진단 원장**: `.plans/notab_rc5_9_review_20260722.md`
- **기준 커밋**: `3f77d95`
- **자문**: 불요(사용자 지정값 20, 단순 연장).

## ⚠ 검증 필수
`dotnet build` 오류 0(경고 14 초과 금지). 미실행 시 커밋 금지·`status: blocked`. Claude가 REPORT 수령 후 직접 빌드 재확인.

## 배경
- RC5 파이프 콜아웃 config(100,20) 자리 확정. F2 리더가 기둥 축(402.5)에서 멈춰 살짝 짧아 보임.
- 사용자: **리더선이 좌측으로 20만 더 가면 좋겠다** = 화살표 끝이 축을 지나 반대편으로 20 관통.

## 집도 지시 (한 가지)

### 세로재 밸룬 리더 화살표 끝점을 축 너머로 연장
- 세로재(isVerticalMember) member-end 리더의 화살표 끝점 X를 `rawAnchor.X`에서 **밸룬 반대 방향으로 `ext`만큼 연장**:
  - 밸룬 side=right → 끝점 X = `rawAnchor.X - ext`
  - 밸룬 side=left → 끝점 X = `rawAnchor.X + ext`
- `ext = GetEnvDouble("PFS_NOTAB_VLEADER_EXT", 20.0, 0.0, 200.0)`. **기본 20**(사용자 지정). 0이면 현행(축까지).
- 화살촉 축소(cycle111 `leaderArrow=min(default, 리더길이×0.45)`)는 유지 — 리더가 길어지므로 화살촉도 비례해 자연스러워짐.
- 밸룬 위치·anchor 로그의 다른 값 불변. 로그에 `leaderExt=` 추가.
- **모든 세로재 밸룬에 균일 적용**(RC1/2/3 F2 포함) — 얇은 기둥 관통 표현은 일반 개선. 라이브에서 RC1~3 확인, 어색하면 env 0으로 원복 가능.

## 하지 말 것
- 콜아웃(cycle115)·F2 밸룬 위치·치수·타 경로 변경 금지. 가로재/유볼트/P1 리더 변경 금지.

## 제약
- 빈 catch 금지. env 파싱 InvariantCulture.

## 검증 (Codex, 필수)
1. `dotnet build` 오류 0, 경고 14 초과 금지.
2. `REPORT.md`에 변경 지점·로그 형식. 라이브는 사용자.

## 성공 기준 (다음 라이브)
- RC5 F2 리더가 기둥을 지나 좌측 20까지 연장(연결감 확보).
- RC1~3 F2 리더도 동일 규칙으로 자연스러움(어색 시 `PFS_NOTAB_VLEADER_EXT=0` 원복). 타 회귀 0.
