# 계획서 — 무탭 RS6~10 + RS5 재개방 수정 (cycle 119, 2026-07-23)

- 원장: `.plans/notab_rs_review_20260723.md` "RS6~15 검수" 절. 사용자 코멘트 RS6(4건)·RS7/8/9(각 2건)·RS10(2건).
- param 실측: RS6={A,A1,Ha,Hb,B1} / RS7·8·9·10={A,A1,A2,F2(,B1)} (.acat ParamDefinition + 12:36 로그 dump).
- 원칙: RS1~5에서 확정한 기성 통로(config 행·params 가로·세로재 포트 앵커·VLeaderExt) 최대 재사용. 유일 신규 = P5(양쪽 세로치수).

## P1. config 행 추가 — RS6~10 (Commands.cs GetNotabTypeConfig)
```
RS6  = param/Ha  + (P5의 우측 키 Hb)                       ← RS5와 동족(門형+B1)
RS7  = param/F2 + MemberAnchorSide="vertical" + VLeaderExt=0
RS8  = 동일
RS9  = 동일
RS10 = 동일 + (P2 단면 가산)
```
- 효과: 세로치수 65/75→실장, F2 밸룬 허공→세로재 포트 기반(RS3/4 검증 완료 경로).

## P2. RS10 세로치수 값 = F2+단면폭
- 사용자 확정 575 = F2(500)+단면(75). BOM 선례 = BOMs.cs label_67 RS10 분기(`BI!="210"`이면 `+DetailProfile 폭`).
- config 플래그 `VerticalAddProfileWidth=true`(RS10만) + BOM과 동일 조건(BI!="210", `DetailProfile(BI)` 첫 토큰) — BOMs.cs:1539~1545 준용.
- **가산 지점(자문 검토 후 결정)**: 사용자 의도 = "세로 부재 575"(부재 실장)이므로 **표기+스팬 모두 575** —
  `paramRealH` 파싱 직후(3734~3743) 가산을 채택. (자문이 경고한 "S2 포트 기하 확장"이 곧 의도된 효과 —
  십자형 세로재는 상단 관통부까지가 부재. 눈검수에서 스팬이 과하면 표기만 가산(3824 직전)으로 후퇴.)

## P3. RS5/RS6 가로치수 = params(A+A1)
- RS6 실측 930=A+A1(800)+다리 단면×2 → bbox 오염. RS5는 우연히 bbox=800이나 규칙상 params가 정도(사용자 "RS5도 동일").
- `rcMemberGeometry` 목록에 RS5·RS6 추가. 분할 400/400(A/A1 대칭이라 스왑 무관).

## P4. 門형 F1(가로재) 밸룬 앵커 = 가로재 실구간 끝 — **P3로 자동 성립 (Codex 자문 확인)**
- 밸룬 horizontal-span = `dimReferenceMinX/MaxX` 전달(3913~) = 치수 extX와 **동일 원천**(3626).
  P3(RS5/6 목록 추가)가 되면 minX=maxX−paramTotal×scale(3595~)로 좁혀져 F1 화살표가 가로재 끝에 안착.
- 별도 집도 불요. 검증 = RS5/6의 `dimH source=params` extX와 밸룬 `horizontal-span minX/maxX` 로그 일치 대조.

## P5. ★신규 — 다리 2개 타입 세로치수 좌·우 양쪽 표시 (Codex 자문 명세 반영)
- 사용자 규칙: Ha/Hb(또는 F계열 다리쌍)가 있으면 좌측 치수=좌측 다리(Ha), 우측 치수=우측 다리(Hb).
- config `VerticalParamKey2`(RS5/6="Hb") 신설. 우측 = 좌측의 param 파싱·`minY+value×vScale` 클램프 흐름 재사용,
  `verticalAnchorX=maxX`, `lineX=maxX+offset+dimClear`.
- **함정 회피(자문)**: ①우측에 S2 포트 게이트(3758~) 재사용 금지 — S2 하나를 양쪽에 오공유 위험.
  RS5/6 양쪽 모두 **ext 기반 fallback 앵커**(좌=minX/우=maxX)로 제한. ②우측 작도는 기존 `if(!none)` 블록 내
  좌측 직후·**3903 이전** — `AddNotabDimensionObstacles`(4000~)가 레이아웃 전 Dimension을 재등록하므로
  별도 장애물 통로 불요. ③로그 라벨 `dimVL`/`dimVR` 분리.
- F계열 다리쌍(RS12A F2/F3 등) 일반화는 키 이름만 config로 받으므로 자동 확보 — 값 지정은 차기 검수 때.

## 검증 (dev_test.bat 태그 RS5~RS10 + 회귀 RS3,RS4,RC5)
| 대상 | 기대 |
|---|---|
| RS6 | 세로 좌 500(Ha)+우 500(Hb) 양쪽 / 가로 800(400/400) / F1 화살표=가로재 끝 / F2·F3 밸룬 기존 유지 |
| RS5 | 동일(가로 params 800, 양쪽 세로, F1 앵커) |
| RS7/8/9 | 세로 500(F2) / F2 밸룬 세로재 포트+화살촉 접촉 |
| RS10 | 세로 **575**(F2+단면) / F2 밸룬 세로재 포트 |
| 회귀 | RS3/RS4(포트 앵커·오프셋·VLeaderExt=0 유지), RC5(가로 650·관통 20) 로그 diff 무변화 |
