# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 119
- **status**: ready
- **issued_at**: 2026-07-23
- **title**: 무탭 RS6~10 + RS5 재개방 (config 행·가로 params·RS10 단면 가산·★양쪽 세로치수 신설)
- **작업 경로**: `PlantFlow_Support/Core/Commands.cs` (GetNotabTypeConfig ~4944 / rcMemberGeometry ~3559 / dimV 파싱·클램프 3734~3825 / 세로치수 작도 `if(!none)` 블록 ~3814+, 3903 이전)
- **계획서**: `.plans/plan_notab_rs6_10_fix_20260723.md` (P1~P5, 자문 반영 최종본)
- **진단 원장**: `.plans/notab_rs_review_20260723.md` "RS6~15 검수" 절
- **기준 커밋**: `4198cf0`
- **자문**: Codex(§9 단일채널). 채택 = ①P4는 P3로 자동 성립(밸룬 span=dimReference와 동일 원천 3913/3626) ②P5 함정 3건(우측에 S2 포트 게이트 재사용 금지→ext fallback 한정 / 우측 작도는 좌측 직후·3903 이전이면 장애물 등록 자동(4000~) / dimVL·dimVR 로그 분리) ③P2 가산 지점 양택 중 스팬 포함(3734~ 직후) 채택 — 사용자 의도="부재 실장 575", 과하면 표기만 가산으로 후퇴.

## ⚠ 검증 필수
`dotnet build` 오류 0 확인 없이 커밋 금지(빌드와 커밋 명령 분리 실행). 미실행 시 `status: blocked`. **push 금지**.

## 집도 항목
1. **P1**: config 행 — RS6=`param/Ha`+`VerticalParamKey2:"Hb"` / RS7·8·9=`param/F2`+`MemberAnchorSide:"vertical"`+`HasVLeaderExt=true,VLeaderExt=0` / RS10=동일+`VerticalAddProfileWidth=true`. RS5에 `VerticalParamKey2:"Hb"` 추가.
2. **P2**: `VerticalAddProfileWidth` — `paramRealH` 파싱 직후(3734~3743) `BI!="210"`이면 `StandardSupport.DetailProfile(BI)` 첫 토큰(x split) 가산. BOMs.cs:1539~1545와 동일 규칙.
3. **P3**: `rcMemberGeometry` 목록에 RS5·RS6 추가(스왑 분기에는 추가 금지 — A/A1 대칭). RS1/2/3·캔틸레버 불변.
4. **P4**: 집도 없음 — P3로 자동. 검증만(로그 대조).
5. **P5**: `VerticalParamKey2` 신설 — 우측 세로치수 미러 작도. 좌측 param 파싱·클램프 흐름 재사용, `verticalAnchorX=maxX`·`lineX=maxX+offset+dimClear`, **S2 포트 게이트 미사용(양쪽 ext fallback)**, 기존 `if(!none)` 블록 내 좌측 직후(3903 이전) 작도. 로그 `dimVL=`/`dimVR=` 분리. key2 있는 타입은 좌측도 포트 게이트 비활성(RS5/6 현행이 이미 fallback이라 실변화 없음).

## 검증 레시피 (dev_test.bat 태그 RS5~RS10 + 회귀 RS3,RS4,RC5)
| 대상 | 기대 |
|---|---|
| RS6 | dimVL=Ha 500(좌)+dimVR=Hb 500(우) / dimH params 800(400/400) / F1 화살표=가로재 끝(span=extX 일치) |
| RS5 | 동일 (가로 800·양쪽 세로·F1 앵커) |
| RS7/8/9 | dimV param F2=500 / F2 밸룬 vertical-port + leaderExt=0 |
| RS10 | 세로 575(F2 500+단면 75) span 포함 / F2 밸룬 vertical-port |
| 회귀 | RS3/RS4(오프셋·VLeaderExt=0)·RC5(650·관통 20) 로그 diff 무변화 |
