# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 118
- **status**: ready
- **issued_at**: 2026-07-23
- **title**: 무탭 RS1~5 잔차 3계열 (RS4 좌우 스왑·F2 밸룬 타입별 오프셋·RS5 세로치수선 스팬)
- **작업 경로**: `PlantFlow_Support/Core/Commands.cs` (RC7 스왑 소비부 ~3571 / dimV param 폴백 ~3686·3742 / AppendNotabBalloons member-end 확정부 6610~6883 / NotabTypeConfig·GetNotabTypeConfig ~4910)
- **계획서**: `.plans/plan_notab_rs_residual_20260723.md` (R1·R2·R3 — 자문 반영 최종본)
- **진단 원장**: `.plans/notab_rs_review_20260723.md` (cycle117 검수 절)
- **기준 커밋**: `8bf878d`
- **자문**: Codex(§9 단일채널). 채택 = ①R1 `RC7||RS4` 소비부 확장 ②R2 이동 묶음(ballCenter/ballBox 이동→touch 재계산→WithinBounds→IsBalloonFree(동일 면제 인자)→실패 시 원위치, 최종값만 CommitBalloonBox 등록, F2 전용 소형 파서) ③R3 근인=`verticalAnchorTopY` fallback 미갱신, 조건 `param && !hasVerticalPortAnchor && dimVBarSpan` 3중 게이트.

## ⚠ 검증 필수
`dotnet build` 오류 0 확인 없이 커밋 금지. 미실행 시 `status: blocked` 반려. **push 금지**(사용자 수행).

## 집도 항목
1. **R1**: RC7 스왑 분기(~3571)를 `RC7 || RS4`로 확장. 파라미터 원본 보존(소비부만), 다른 타입 불변.
   → RS4 split=(200,600)·pipeCenterX·파이프 콜아웃 앵커 동시 교정.
2. **R2**: `NotabTypeConfig`에 `MemberBalloonDx/Dy`(기본 0) + RS2=(-31.5,0)·RS3=(+30,0)·RS4=(+36,0) 출하값.
   env `PFS_NOTAB_F2_BALLOON_POS_<TYPE>`="dx[,dy]" 전용 파서(env→config→0). F2(member-end) 배치 확정 직후
   계획서 R2의 5단계 이동 묶음 그대로. 실패 시 원위치+로그(`balloon-offset skip reason=`). 로그에 `offset=(dx,dy)` 표기.
3. **R3**: param 처리 뒤 `verticalMode=param && !hasVerticalPortAnchor && dimVBarSpan`일 때
   `verticalAnchorTopY = verticalAnchorBaseY + paramRealH*vScale`. 다른 소비자(dimVBarSpan/barRealH) 불변.

## 검증 레시피 (dev_test.bat 태그 RS2,RS3,RS4,RS5 + RC5,RC7,GD1,RS12A)
| 대상 | 기대 |
|---|---|
| RS4 | split=(200,600) / 콜아웃·divider=U볼트 중심 / F2 밸룬 offset=(36,0) 로그 |
| RS2 | F2 offset=(-31.5,0) / 세로치수 없음 유지 |
| RS3 | F2 offset=(30,0) / 세로 500 불변 |
| RS5 | 세로치수선 스팬=baseY~baseY+100paper(실500), 값 500 / BOM·밸룬 불변 |
| RC7 | 분할 150/800 유지(스왑 공유부 회귀 확인) |
| RC5·GD1·RS12A | 로그 diff 무변화 |
