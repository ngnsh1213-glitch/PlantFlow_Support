# 무탭 RS 계열 검증 원장 (2026-07-23~)

트랙: RS1~15 타입별 검증. 루프 = 사용자 눈검수 코멘트 수집 → 일괄 진단 → 수정 사이클.
선행 종결: GD1~3+RC1~9 전 타입(cycle105~116), cycle104 UI 검증 PASS(`3a0d2ce`).
추출 경로: 팔레트 일괄 선택 추가 → 무탭 추출(검증 완료 경로).

## 사용자 코멘트 (눈검수)

### RS1 (2026-07-23)
1. **BOM QUAN/LEN=645 ≠ 치수 500** — 3D 데이터상 500이 정답. 가로치수는 100+400=500으로 정상.
   - 유사 전례: RC 트랙에서 가로재+플레이트 병합 solid bbox 오염(RC5 700→650, param이 정답).
   - 조사 대상: BOM 행 길이 산출부(BOMs → StandardSupport.RS1?)가 param이 아닌 bbox/카탈로그 값을 쓰는지.
2. **세로치수 75 삭제** — RS1은 세로치수 불요.
   - 처방 후보: `GetNotabTypeConfig`에 RS1 행 추가, vertical=`none`(RC7/8과 동일 모드).

### RS2 (2026-07-23)
1. **세로치수 75 삭제** — RS1과 동일 처방(config vertical=`none`).
2. **F2 밸룬 좌측 31mm 이동** — 대각재(F2) 옆으로 근접. 스크린샷상 F2가 대각재 우측 허공에 떠 있고
   리더가 부재에 닿지 않아 보임. RC7 전례(대각재 앵커 `isRc7DiagonalMember` 분기) 참조.
   - 수동 캘리브레이션 워크플로우 후보: 노브로 확정 후 config 승격.
- 가로 944.6/144.6 소수점 값 = **실제 데이터 값, 정상 인정**(사용자 확정 2026-07-23). 교정 대상 아님.
### RS3 (2026-07-23)
1. **세로치수 50→500** — 부재 단면 F(50) 폴백의 재림(RC4/5 근인과 동일: `GetNotabTypeConfig`에 RS3 행 부재).
   사용자 표기 위치 = 세로재 전장 스팬(상단 가로재~하단). 처방: config 행 추가(param/F2류/vertical), 파라미터 키는 덤프로 확인.
2. **F2 밸룬 이동 X -118, Y +48.5** — 세로재 옆 중단부로. 사용자가 F2를 세로재 중앙에 손표기.
   RS2 F2(-31)와 함께 세로재/대각재 밸룬 앵커 계열 결함. 세로재 밸룬 = 포트 기반 배치 확인 필요.
### RS4 (2026-07-23) — 부재코드 C10(찬넬)
1. **가로치수 900→800** — 사용자 표기 위치의 800이 정답. RC5 700→650 전례(병합 solid bbox 오염 vs param) 의심.
   분할 300/600은 표시 중 — 합 900 ≠ 정답 800: 분할·전장 산출원 불일치 확인 필요.
2. **세로치수 100→500** — config 폴백 계열(RS3 50→500과 동일 패턴, 100은 단면/부분값).
3. **F2 밸룬 이동 X -104, Y -30** — 세로재 옆으로. RS2(-31)·RS3(-118,+48.5)와 동일 계열.
4. **파이프 콜아웃 좌/우 반대** — 사용자: "좌측으로 나와야 하는데 우측으로 나왔음".
   ※스크린샷상 텍스트는 좌상단으로 보임 — 진단 시 `callout-draw` 로그의 `requiredSide/side/viewportCenterX`로
   실측 대조해 어느 쪽 규칙(R1 중앙선)이 틀어졌는지 확정할 것.
### RS5 (2026-07-23) — 門형(패밀리 C), RESTING STRUCTURE NO 5
1. **BOM 테이블 누락** — 표 자체가 없음.
2. **부재 밸룬 누락** — F1/F2 밸룬 없음. 대신 구형 텍스트 콜아웃(L-65×65×6)만 표시
   (= BOM 행 없음 → 밸룬 대체 비활성 → member-text 폴백 경로로 추정, `member-text skip` 조건 역).
3. **세로치수 65→500** — 단면 폴백 계열(65=L-65 단면).
- **★사용자 진단: 파이썬 스크립트/데이터 문제 가능성** — Part Geometry 실측: Dn=80, Bl=16, A=400, A1=400,
  **Ha/Hb 공백**, P1=0, TY=-1. **SupportParams에 F2/F3 없음** → StandardSupport.RS5()가 BOM 행을 못 만들어
  표·밸룬이 연쇄 누락됐을 개연성. C# 결함이 아니라 카탈로그 Python(@activate)/모델 데이터 소관일 수 있음.
  - 분기: (a) Ha/Hb를 모델에서 채우면 해소되는지 먼저 확인 (b) 스크립트가 F2/F3을 아예 안 쓰면 RS5() 매핑 재설계.
  - 참고: 빈 BOM 무음 실패 전례(DesignStd 하드코딩 사고) — 무음 누락에 진단 로그 필요.

## 관찰 메모
- RS1 밸룬 F1=A7 정상 표시, U볼트 UB-012 콜아웃·라인넘버/BOP 콜아웃 정상으로 보임(스크린샷 기준).

---

## 진단 결과 (2026-07-23, 로그 09:08 일괄추출 + 코드 리딩으로 근인 확정)

### 실측 param (support params dump)
| 타입 | params | BOM rows |
|---|---|---|
| RS1 | A=500 (F2 없음) | 1 |
| RS2 | A=800, F2=900 | ? |
| RS3 | A=600, A1=200, F2=500 | 2 |
| RS4 | A=600, A1=200, F2=500, BI=210 | 2 |
| RS5 | A=400, A1=400, **Ha=∅, Hb=∅** | **0** |
| RS6 | A=400, A1=400, **Ha=∅, Hb=∅**, B1=0 | **0** |

### 계열 A — 치수: RS는 param 경로 진입 자체가 안 됨 (근인 확정)
- 전 RS 로그 `dimH src=fallback=legacy`, `dimV fallback=not-rc`, `vMode=fheight`.
- `GetNotabTypeConfig`(Commands.cs:4910)에 **RS 행이 하나도 없음** → 기본 `fheight`(단면 F) 폴백 = RS1 75/RS2 75/RS3 50/RS4 100/RS5 65.
- 가로 게이트 `rcMemberGeometry`(3559~)도 RC 하드코딩 → RS4는 병합 solid bbox 900(wcsDims=900 실측)이 그대로 치수화. param A+A1=800이 정답.
- **단, RS2는 bbox 944.55가 정답**(사용자 확정) — RS2 기하 자체가 A+pipe/2+100=944.55로 지어짐. → 가로 소스는 타입별 지정 필요(RS4=params, RS1/2/3=legacy 유지).

### 계열 B — F2 밸룬: horizontal-end 규칙에 강제 스냅 (근인 확정)
- RS3: `balloon-draw key=F2 rawAnchor=(255.5,268.9)[=S2 포트, 정답 위치] → anchor=(415.5,268.9)` — `geom=horizontal-span`이
  가로 스팬 우측 끝단으로 이동시킴. RS4 동일(rawAnchor 255.5,379 → 425.5,379).
- 세로재 분기(`verticalPortGate`, 3758)가 config `MemberAnchorSide="vertical"`+`VerticalParamKey="F2"`를 요구 → RS 행 부재로 미발동.
- rawAnchor는 이미 정답(S2 포트) — config 행만 넣으면 RC와 같은 세로재 배치로 전환될 구조.
- 사용자 이동값: RS2 X-31 / RS3 X-118,Y+48.5 / RS4 X-104,Y-30 → 포트 기반 전환 후 잔차만 캘리브레이션.

### 계열 C-1 — RS1 BOM 645 (근인 확정)
- BOMs.cs `label_56`(1462): `F1 = ceil(A + PipeSize(Dn)/2 + 100)` = 500+44.55+100 → **645**. 공식이 낳은 값.
- RS1 기하 실측: 부재 solid=500(wcsDims 500×75×75) — python은 A만 사용. 공식·기하 불일치.
- 대조: RS2는 기하도 944.55(=A+pipe/2+100)라 같은 공식이 **맞음**. → RS1만 F1=A로 교정(GD1·RS11 공유 분기라 국소 조건 필수).

### 계열 D — RS5/RS6 BOM 전멸 (근인 확정, 데이터 아님)
- 10:01 재추출: 사용자가 **Ha=500/Hb=500 채웠는데도 rows 0** → 데이터 문제 기각.
- BOMs.cs `label_71`(1563): `SupportParams["F2"]`·`["F3"]` 요구 — RS5/6 param에는 **F2/F3 키가 없음**(Ha/Hb뿐)
  → KeyNotFound 예외 → 상위 무음 삼킴 → rows 0 (빈 catch성 무음 실패, 진단로그도 없음).
- 처방: RS5/6 분기 = F1=A+A1, F2=Ha, F3=Hb + FrameBOM 예외 시 FileDiag 로그 의무화.
- RS5 세로치수 500 = Ha. 단 `verticalPortGate`가 `VerticalParamKey=="F2"` 하드코딩(3759) → 키 일반화 필요.

### 부수 관찰 (RS6~15 미코멘트분, 사용자 검수 대기)
- RS12A~D·RS12 계열 BOM rows 0 (BOMs.cs에 RS12 case 부재 추정) — RS6~15 코멘트 수집 시 함께.
- RS4 파이프 콜아웃: 로그 `requiredSide=left side=left` — R1 규칙대로 좌측 배치. **사용자 확정(2026-07-23): 현 로직 유지, 교정 대상 아님.** RS4 항목 4 종결.
