# 계획서 — 무탭 RS1~5 잔차 수정 (cycle 118, 2026-07-23)

- 선행: cycle 117(`8bf878d`) 라이브 검수 완료. 잔차 3계열. 원장 `.plans/notab_rs_review_20260723.md` cycle117 검수 절.
- 원칙: 기성 인프라 재사용(RC7 스왑·NotabTypeConfig·env→config 캘리브레이션 규약). RC/GD/RS12A 무회귀.

## R1. RS4 좌우 스왑 — Commands.cs ~3571 (RC7 스왑 소비부)
- 증상 3건=1근인: 분할 (600,200) 반대 / 파이프 콜아웃 앵커 385.5(비파이프) / 치수 divider 비중심.
- 원본 RS4.py: 부재 스팬 `-A1 … A`, 원점=파이프 중심 → 투영상 좌=A1(200)/우=A(600).
  공통 규칙 `right=A1`이 반대 → RC7과 동일한 소비부 스왑 분기에 RS4 추가(파라미터 원본 보존, 다른 타입 불변).
- 기대: split=(200,600), pipeCenterX(paper)=파이프 실위치, 파이프 콜아웃 앵커=U볼트 위치.

## R2. 부재(F2) 밸룬 타입별 오프셋 — config 통로 신설 + 출하값 3건
- `NotabTypeConfig`에 `MemberBalloonDx`/`MemberBalloonDy`(기본 0) 추가.
  적용 지점 = F2(세로재/대각재) 밸룬 최종 center 확정 직후 평행이동(밸룬 원+리더 끝점 연동, 앵커·화살표는 불변 —
  화살표는 부재에 닿아야 하므로 rawAnchor/arrow는 이동 금지, center와 리더 문자쪽 끝만 이동).
- env 오버라이드 `PFS_NOTAB_F2_BALLOON_POS_<TYPE>` = "dx[,dy]" (우선순위 env→config→0). ★기존 Pipe 위치 파서는
  정확히 2개 값만 받으므로 **F2 전용 소형 파서 별도**(Codex 자문 채택 — 범용 이동 인프라 신설 금지).
- 출하 config 값(사용자 도면 캘리브레이션 확정): RS2 = (-31.5, 0) / RS3 = (+30, 0) / RS4 = (+36, 0).
- **이동 절차(Codex 자문 명세, member-end 확정 직후 한 묶음)**: 화살표=Leader 첫 정점 `anchor`(불변),
  문자쪽 끝 `touch`는 center에서 재계산되는 구조(NotabCalloutPlacer.cs:180~235) →
  ①`ballCenter`·`ballBox` dx/dy 이동 ②`touch` 재계산 ③`WithinBounds(이동 box)`(IsBalloonFree는 경계 미검사라 필수)
  ④기존과 동일 면제 인자로 `IsBalloonFree(이동 box, anchor, 이동 touch)` ⑤실패 시 전량 원위치.
  **성공한 최종 anchor/touch/ballBox만 `CommitBalloonBox`에 등록** — 아니면 등록 상자·리더가 실작도와 어긋나
  후속 밸룬/RC9 배치가 오통과. 기본 0이면 전 타입 무변화.

## R3. RS5 세로치수선 스팬 = 부재 실장 — Commands.cs param 모드 폴백부(~3686 인근)
- 증상: 값=Ha(500)인데 치수선 스팬=legacy bbox(268.9~389.1=실601) → 라벨·기하 불일치.
- **근인 정밀화(Codex 자문 채택)**: `dimVTopY`는 param으로 갱신되나 **`verticalAnchorTopY`가 maxY 초기화 후
  fallback 경로에서 미갱신**(3686~) → 값 500 / 선은 bbox 스팬. 처방 = param 처리 뒤
  `verticalMode=param && !hasVerticalPortAnchor && dimVBarSpan` 조건에서만
  `verticalAnchorTopY = verticalAnchorBaseY + paramRealH×vScale` 갱신.
  (조건 3중 게이트로 포트 정상 타입(RS3/4·RC1~6·9)은 불변 — 단 S2 누락 등으로 fallback에 떨어진 케이스도
  이 개선의 수혜를 받게 되며 이는 개악이 아님.)
- 대안(port 게이트 키 완화)은 밸룬 결합 부작용(cycle117 자문 F2 철회 사유) 때문에 배제 — 클램프가 국소·안전.
- RS3/4 등 포트 앵커 발동 타입(src=port-S2)은 이 경로를 타지 않으므로 불변. RC 계열도 포트 발동이라 불변.
  fallback param 경로를 실제로 타는 타입 = 현재 RS5뿐(로그 확인). 회귀면 최소.
- .py 대조: RS5.py `setLinearDimension('Ha', psF2…)` 스팬 = 파이프 datum(-Dz/2-P1)에서 아래로 Ha —
  즉 상단 기준이 가로재 상면측. 클램프(하단 기준)와 시각 결과 동일 스팬(길이 500)이며 위치는 다리와 정합.

## 검증 (dev_test.bat 태그 RS2,RS3,RS4,RS5 + 회귀 RC5·RC7·GD1·RS12A)
| 대상 | 기대 |
|---|---|
| RS4 | split=(200,600) / 파이프 콜아웃·치수 divider=U볼트 중심 / F2 밸룬 +36 반영 |
| RS2 | F2 밸룬 -31.5 / 기타 불변 |
| RS3 | F2 밸룬 +30 / 세로 500 불변 |
| RS5 | 세로치수선 스팬=다리 하단~+100paper(실500), 값 500 / 밸룬·BOM 불변 |
| RC7 | 스왑 분기 공유부 무회귀(분할 150/800 유지) |
| RC5·GD1·RS12A | 로그 diff 무변화 |
