# 계획 — RC1 F2/P1 밸룬 미세조정 (cycle129)

## 요구 (사용자, RC1 Main뷰)
- **F2 밸룬**: 밸룬+리더+앵커(화살촉) **통째로 우측 +14 paper-mm**.
- **P1 밸룬**: 앵커점 **고정**, 밸룬+리더만 **좌측 -14 paper-mm**.

## 현 코드 (Commands.cs 밸룬 작도 함수)
- `~7548` `anchor = bestAnchor;` → `~7550~7588` 기존 F2 오프셋(밸룬만, anchor 고정, revert 有, `item=="F2"&&tier=="member-end"`) → `~7592~7605` isVerticalMember leaderExt로 anchor X 추가이동 → `~7609~7611` touch 계산 → `~7649` Leader(anchor→touch)·원/문자(ballCenter).
- 실측: RC1 F2 `tier=member-end`, P1 `tier=normal`. 좌표계 X+ = 우측.

## 자문 채택 (Codex §9 read-only 1회)
1. F2 통째 이동은 leaderExt·touch 후 **작도 직전 최종 평행이동**이 정합. 이동분 bounds/`IsBalloonFree` **재검증하되 자동 revert 금지** → 진단 WARN 로깅(사용자 확정값 보존).
2. P1은 기존 revert 패턴 재사용 금지(−14가 원복될 위험). 앵커 고정+ballCenter/ballBox 이동+touch 재계산 **강제 적용**. 도면 밖 등 치명조건만 WARN(조용한 revert 금지).
3. F2 nudge는 member-end 게이트 자동승계 대신 의미 분리 — RC1 F2 실측이 member-end이므로 **item==F2에 적용**(tier 무관, 로깅으로 확인).
4. 두 노브를 기존 `MemberBalloonDx/Dy`(배치탐색·revert)와 **독립된 공통 "최종 nudge" 단계**로 분리 → RS3/4 회귀 차단. 순서 = 기존 오프셋 → leaderExt → **P1 nudge/touch 재계산** → **F2 통째 nudge**.

## 설계
### 신규 노브 (env→config, RC1 baked, [feedback-manual-calibration-workflow])
- `PFS_NOTAB_F2_SHIFT_<TYPE>` (dx,dy) + `NotabTypeConfig.F2WholeShiftDx/Dy`. **RC1 config = (14, 0)**.
- `PFS_NOTAB_P1_BALLOON_POS_<TYPE>` (dx,dy) + `NotabTypeConfig.P1BalloonDx/Dy`. **RC1 config = (-14, 0)**.
- 우선순위 env > config. 미설정·비RC1 타입 = (0,0) → 무영향(회귀 0).

### 적용 (작도 직전, ~7607 touch 계산 직후 / 원·리더 생성 전)
- **P1 nudge** (`item`이 P1 계열일 때): `ballCenter += p1Shift; ballBox += p1Shift; touch = ballCenter - normalize(ballCenter-anchor)*radius`. anchor 불변.
- **F2 통째 nudge** (`item=="F2"`일 때): `anchor += f2Shift; ballCenter += f2Shift; ballBox += f2Shift; touch += f2Shift`. (전체 평행이동이라 leader 길이·형상 불변.)
- 두 nudge 후: `placer.WithinBounds`·`IsBalloonFree` **재검증 → 실패 시 WARN 로깅만**(revert 금지). `placer.CommitBalloonBox`는 이동된 anchor/touch/ballBox로.
- 로그: `PFSNOTABDETAIL balloon-nudge key=<item> kind=<F2whole|P1balloon> shift=(dx,dy) src=<env|config> anchor→ box→ revalidate=<ok|warn:reason>`.

### P1 item 식별
- P1 밸룬 key는 `P1_0` 등(포트별 접미). `item`은 "P1"(로그 item=P1). config/env 조회는 `item`이 "P1"로 시작 또는 isPlateItem+P1 판별. **집도 시 실제 item 값으로 게이트**(로그 item=P1 근거).

## 검증 레시피
- dev_test `PFS_NOTAB_FLATTEN=1` + `RC1-001` 추출 →
  - **F2**: 밸룬·리더·화살촉이 기존 대비 우측 14 이동(화살촉 부재 접점도 +14). 로그 `balloon-nudge key=F2 kind=F2whole shift=(+14,0)`.
  - **P1**: 화살촉(앵커) 위치 불변, 밸룬·리더만 좌측 14 이동. 로그 `key=P1 kind=P1balloon shift=(-14,0)`, anchor 로그값 불변 확인.
  - 다른 주석(F1·콜아웃·치수) 무변화. bounds WARN 없으면 정상.
- 회귀: RS3/4 등 F2 MemberBalloonDx 사용 타입 무변화(신규 노브 (0,0)).

## 리스크
- P1 좌측 이동이 세로재/치수와 겹치면 IsBalloonFree WARN — 사용자 확정값이라 유지, 겹침 심하면 값 재조정(env로 즉시 튜닝).
- F2 앵커까지 이동 → 화살촉이 실제 부재선에서 14 벗어남(사용자 의도 "통째로"에 부합).
