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
| A/E 가이드 | **GD1** | ✅ 재검증 완료(2026-07-21, 밸룬 도입 후) | 바 F=75 OK | 하단(기지값) | 3종 근단착지 OK | 단일뷰 OK | cycle47~59 종결이나 **밸룬 도입 이전**. 랜딩=수평부착모드(cycle59) |
| A/E 가이드 | **GD2** | ✅ 재검증 완료(2026-07-21) | 세로=B/D 파이프중심(300) | 하단 | 부재 2개 L=중앙수직재·C=하단수평재우측(cycle72) | 단일뷰 OK | config MemberBIs `["16","215"]`. 부재앵커 재배치 cycle72 |
| A/E 가이드 | **GD3** | ✅ 재검증 완료(2026-07-21) | | 하단 | 단일 콜아웃 근단착지 OK | 단일뷰 OK | ★BOM엔 C10+A7 2부재이나 채널만 주석(A7 누락) |
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

## GD 재검증 결과 (2026-07-21) — 이상없음
GD1~GD3의 기존 "종결"은 cycle 47~72 시점이라 **밸룬 도입(cycle 95~102) 이전**이었다.
밸룬이 부재 텍스트 콜아웃을 대체하도록 바뀌어 코드 경로가 달라졌으므로 재검증했고,
**RC에서 세운 전제 3건이 GD에서도 깨지지 않았다**(사용자 판정 "이상없음").
1. F1=가로재 / F2=세로재 매핑
2. 세로재 좌표의 S2 포트 전제(`IsNotabVerticalMemberPort`)
3. 기둥 상하단 = `F2` 파라미터 × vScale

## 다음 — 일괄추출 부속 제외 (cycle103)
`PFSNOTABBATCH`는 UI 아이콘으로 노출할 예정이며, **전체 드래그 선택으로 실행해도
(pipe, ubolt, ubolt for guide)를 제외한 서포트 본체만** 개별 도면으로 뽑아야 한다.

현재 결함: `CollectSelectedSupportIds`가 클래스명에 "Support"만 있으면 통과시킨다.
파이프는 자동 제외되지만 **유볼트도 `AcPpDb3dSupport`라 통과**해, 전체 선택 시 유볼트마다
개별 도면이 나온다.

- **1단계(집도 완료)**: 계측. 선택 항목별 `SupportName/ShortDescription/Tag/TagName/PartNumber/
  SupportDetail/Description` + 클래스·Handle을 이름 기반으로 덤프(`PFSNOTABBATCH probe`),
  요약(`probe-summary`)에 선택수·Support수·후보수·조회실패수·중복 SupportName 기록.
  `PFS_NOTAB_BATCH_DRYRUN=1`이면 도면을 뽑지 않고 목록만 보고한다.
- **2단계(대기)**: 1단계 실측으로 "ubolt for guide"의 ShortDescription을 확정한 뒤
  `PFS_NOTAB_BATCH_EXCLUDE`(콤마 구분, 하드코딩 금지)로 제외. 제외된 부속은 종전대로
  `AutoIncludeRelatedParts`가 해당 서포트 도면에 자동 포함한다.
  ⚠ 조회 실패 시 기본 포함하면 현 결함이 재발하고, 기본 제외하면 정상 본체를 누락한다 —
  1단계에서는 미분류를 경고로만 기록한다(자문).

절차: 전체 선택 → `PFSNOTABBATCH`(DRYRUN=1) → 로그의 `probe` 줄에서 유볼트류 ShortDescription 확정.

