# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 129
- **status**: ready
- **issued_at**: 2026-07-24
- **title**: RC1 F2/P1 밸룬 미세조정 (F2 통째 +14 우측, P1 밸룬만 -14 좌측)
- **작업 경로**: `PlantFlow_Support/Core/Commands.cs` — 밸룬 작도 함수(~7607 touch 계산 직후) + `NotabTypeConfig`(RC1) + 오프셋 조회 헬퍼 인근
- **계획서**: `.plans/plan_notab_rc1_balloon_nudge_20260724.md`
- **기준 커밋**: `ad890ae`(cycle128)
- **자문**: Codex(§9, read-only 1회). 채택 = ①작도 직전 최종 평행이동 ②재검증하되 자동 revert 금지·WARN 로깅(사용자 확정값 보존) ③P1 강제적용(revert 없음) ④기존 MemberBalloonDx/Dy와 독립된 "최종 nudge" 단계로 분리(RS3/4 회귀 차단) ⑤순서=기존오프셋→leaderExt→P1 nudge→F2 통째 nudge.

## 요구
- **F2 밸룬**: 밸룬+리더+앵커(화살촉) **통째로 우측 +14 paper-mm**.
- **P1 밸룬**: 앵커 **고정**, 밸룬+리더만 **좌측 -14 paper-mm**.
- 실측: RC1 F2 tier=member-end, P1 tier=normal, X+ = 우측.

## ⚠ 검증 필수
`dotnet build` 오류 0(빌드·커밋 분리) 없이 커밋 금지. **push 금지**. 신규 노브 미설정·비RC1 타입 **회귀 0**(RS3/4 F2 MemberBalloonDx 무변화).

## 집도 항목
1. **신규 노브 2종**(기존 balloon-offset과 독립):
   - `PFS_NOTAB_F2_SHIFT_<TYPE>`(dx,dy) + `NotabTypeConfig.F2WholeShiftDx/Dy`. **RC1 = (14,0)**.
   - `PFS_NOTAB_P1_BALLOON_POS_<TYPE>`(dx,dy) + `NotabTypeConfig.P1BalloonDx/Dy`. **RC1 = (-14,0)**.
   - env > config, 기본 (0,0). 파서는 기존 `TryGetNotabF2BalloonOffset` 패턴 준용(1~2 필드).
2. **적용**(작도 직전, touch 계산 후·원/리더 생성 전; 순서 P1 → F2):
   - **P1 nudge**(item이 P1): `ballCenter/ballBox += p1Shift; touch = ballCenter - normalize(ballCenter-anchor)*radius`. anchor 불변. **강제 적용**.
   - **F2 통째 nudge**(item=="F2"): `anchor,ballCenter,ballBox,touch += f2Shift` (전체 평행이동).
   - 두 nudge 후 `WithinBounds`/`IsBalloonFree` **재검증 → 실패 시 WARN 로깅만, revert 금지**. `CommitBalloonBox`는 이동된 anchor/touch/ballBox로.
3. **로그**: `PFSNOTABDETAIL balloon-nudge key=<item> kind=<F2whole|P1balloon> shift=(dx,dy) src=<env|config> anchor=<...> box=<...> revalidate=<ok|warn:reason>`.

## 검증 레시피
- dev_test `PFS_NOTAB_FLATTEN=1` + `RC1-001` →
  - F2: 밸룬·리더·화살촉 통째 우측 14. 로그 `kind=F2whole shift=(+14,0)`.
  - P1: 앵커(화살촉) 불변, 밸룬·리더만 좌측 14. 로그 `kind=P1balloon shift=(-14,0)` + anchor 불변.
  - F1·콜아웃·치수 무변화, bounds WARN 없음.
- 회귀: RS3/4 등 F2 MemberBalloonDx 타입 무변화.

## 비고
- P1 item 게이트는 실제 `item` 값("P1", key=P1_0)으로 판정. 좌측 이동이 세로재/치수와 겹치면 WARN 유지(사용자 확정값, env로 재튜닝).
