# 무탭(NoTab) 서포트 디테일 — 全타입 테스트 진행표

> 규격집: `[WELCRON HANTEC] PIPE SUPPORT STANDARD`. 목표=각 타입 3D `TYPE-001` 배치 → `PFSNOTABBATCH` 전량추출 / `PFSNOTABTEST`(env `PFS_NOTAB_TEST_TAG`) 단건 → 4관찰로 패밀리별 로직 확정.
> 4관찰: ①세로치수 스팬(부재 정확?) ②가로 상/하단(→`GetNotabHorizontalDimSide`) ③콜아웃(부재/PLN/BOP) 위치·지향 ④뷰 충분성(단일 vs 멀티뷰).

## 확정 env (GD1 기준 정착값)
```
PFS_NOTAB_DIM_TXT=8          # 치수·콜아웃 글자 (화살표 ARR=10 별도)
PFS_NOTAB_DIM_OFFSET=30      # 기본
PFS_NOTAB_DIM_STACK=30       # 기본
PFS_NOTAB_PIPE_CALLOUT_DX=180
PFS_NOTAB_MEMBER_CALLOUT_DX=5
```
※ 타입마다 DX/DY는 재조정 가능(재빌드 불요). 위 값은 GD1에서 확정.

## 진행표
| 패밀리 | 타입 | 상태 | 세로스팬 | 가로side | 콜아웃 | 뷰 | 비고 |
|---|---|---|---|---|---|---|---|
| A/E 가이드 | **GD1** | ⚠ **재검증 필요** | 바 F=75 OK | 하단(기지값) | 3종 근단착지 OK | 단일뷰 OK | cycle47~59 종결이나 **밸룬 도입 이전**. 랜딩=수평부착모드(cycle59) |
| A/E 가이드 | **GD2** | ⚠ **재검증 필요** | 세로=B/D 파이프중심(300) | 하단 | 부재 2개 L=중앙수직재·C=하단수평재우측(cycle72) | 단일뷰 OK | config MemberBIs `["16","215"]`. 부재앵커 재배치 cycle72 |
| A/E 가이드 | **GD3** | ⚠ **재검증 필요** | | 하단 | 단일 콜아웃 근단착지 OK | 단일뷰 OK | ★BOM엔 C10+A7 2부재이나 채널만 주석(A7 누락) |
| A 수평바 | SHOE(S) | ⬜ 대기 | | | | | 3/4~8" vs 10~24"(H빔) |
| A 수평바 | RS11 | ⬜ 대기 | | | | | 수직앵글+U볼트, ELEV F/E |
| A 수평바 | RS15 | ⬜ 대기 | | | | | 수직스탠션+브레이스 |
| B 캔틸레버 | RS1·2·3·4·7·8·12·13 | ⬜ 대기 | | | | | 세로 스팬 전제 깨질 수 있음 |
| B 캔틸레버 | **RC1·RC2·RC3** | ✅ 종결 | S2 포트+`F2`×vScale | 하단 | 밸룬(F1 가로재 끝단/F2 세로재 측면/P1 포트)+유볼트 직선+라인넘버 꺾임 | 단일뷰 OK | cycle92~102. 라이브 3회 skip 0건 |
| B 캔틸레버 | RC4~9 | ⬜ 대기 | | | | | 콘크리트 기초형 |
| C 門형 | RS5·6 | ⬜ 대기 | | | | | 양다리 프레임 |
| D 걸침보 | RS9·10·14 | ⬜ 대기 | | | | | 양단 지지 |
| F 트런니언 | TR·TRS | ⬜ 대기 | | | | | 배관축≠트런니언축 |
| G 필드 | FS | ⬜ 대기 | | | | | 방향 현장결정 |

부속(BP·SET ANCHOR BOLT·REINFORCED PAD)=서포트 디테일 대상 아님, 제외.

## 다음 — GD 재검증이 최우선 (2026-07-21)
GD1~GD3의 "종결"은 **cycle 47~72 시점**이고, 그 뒤 cycle 95~102에서 **밸룬이 부재 텍스트 콜아웃을
대체**하도록 바뀌었다(`PFS_NOTAB_MEMBER_TEXT=1`이어야 옛 텍스트가 나온다). 즉 GD는 검증 당시와
**다른 코드 경로**를 탄다. 새 타입보다 GD 재검증이 먼저다.

**RC에서 세운 전제 3건이 GD에서 성립하는지가 핵심 질문이다.**
1. F1=가로재 / F2=세로재 매핑 — GD는 BOM ITEM 구성이 다를 수 있다(GD3는 C10+A7).
2. 세로재 좌표의 **S2 포트 전제** — `IsNotabVerticalMemberPort`가 GD에서도 참인가.
3. 기둥 상하단 = **`F2` 파라미터 × vScale** — `GetNotabTypeConfig`의 `VerticalParamKey`가
   GD에서도 `F2`인지(RC1/2/3은 모두 `F2`로 확인됨).

판정 키(로그): `member-geometry-unavailable`(전제 붕괴) / `member-end-no-space`(공간 부족) /
`callout-skip` / `balloon-skip reason=no-bom-row`(ITEM 매핑 불일치) /
`member-spike`의 솔리드 구성(RC는 `7A` 병합이었다. GD는 부재가 분리돼 있을 수 있다).

절차: `dev_test.bat`의 `PFS_NOTAB_TEST_TAG`를 GD로 바꿔 실행 → 로그 판정 → 이 표에 4관찰 기입 →
어긋나는 지점만 per-family 분기/env로 채운다.
