# 계획서 — 무탭 RS11~15 수정 (cycle 120, 2026-07-23) — Claude 직접 집도

## 2차 추가분 (재검수 반영, 2026-07-23 오후)
- **V-SPLIT(신규)**: 측면부착 3형제(RS12A/B/D) 세로치수에 **파이프 중심 분할** 표시 — 가로 legacy 분할의 세로 미러.
  값은 투영 기하 결정론(pipeCenterYPaper 기준 상/하 실값): RS12A 450/150, RS12B 250/250, RS12D 300/200 자연 도출.
  config 플래그 `VerticalSplitAtPipe`(3형제만 true). 배치=세로 total 치수 좌측에 스택(가로 splitY/totalY 미러).
- **H-F2**: RS12A 가로 param 소스 = **F2(500, 가로재 실장)** — A+A1(600)은 세로 스파인 값이었음(재해석 확정).
  래퍼에 RS12A 분기: total=F2, 분할 없음, source="params(F2)".
- RS12A F3 밸룬 허공은 가로 스팬 500 정합 후 자동 해소 기대(끝단 스냅이 부재 좌단으로 이동) — 재검수로 판정.

## 3차 추가분 (.py 전문 판독, 부재 의미 확정)
- **RS12A**: 스파인(세로)=**F1=A+A1(600)**, 파이프 분할 A1(상150)/A(하450). 가로재=F2(상)·F3(하) 각 500.
  → 세로 키를 F2+단면(우연 600)에서 **"A+A1" 합성**으로 교체(의미 정합). 가로=F2.
- **RS12B**: 스파인=**F1=A+A1(500)**, 가로재=F2(500). → 세로 키 "A+A1"로 교체(값 동일, 의미 정합).
  **F1 밸룬 허공 근인 = 세로재(스파인)에 horizontal-end 스냅 적용** — X가 가로재 끝으로 끌려가 허공 코너.
- **처방(신규)**: config `VerticalBalloonKeys`(쉼표 목록) — 해당 키 밸룬은 vertical-end 배치(axisX=rawAnchor.X,
  spanY=세로치수 스팬). RS12A="F1", RS12B="F1". 기존 포트 판정(IsNotabVerticalMemberPort) 불변, 옵트인 추가만.

- 원장: `.plans/notab_rs_review_20260723.md` RS11~15 절. 격리 결함은 별도 사이클(분리, 사용자 확정).
- param 실측(.acat): RS11={A,A1} / RS12A={F2,F3,A,A1} / RS12B={A,A1,A2,F2} / RS12C={A} / RS12D={F2,A} / RS13={A,A1,F2,F3} / RS14={A1,A2} / RS15={A1,A2,L1,F2}.
- 모델 실측값: RS11 세로=550(=A+A1) / RS12A 세로 600(=F2+단면100)·가로 500(=A+A1) / RS12B 세로 500(F2)·가로 500(A+A1) / RS12D 세로 600(=F2+단면)·가로 445(=A+파이프/2+100, label_66 공식) / RS13 좌우 800(F2/F3) / RS15 600(F2).

## Q1. 세로치수 — config 행 8종
| 타입 | VerticalMode | 키 | 비고 |
|---|---|---|---|
| RS11 | param | **"A+A1"(합성)** | 파서 확장: '+' 분리 후 합산(신규, 범용) |
| RS12A | param | F2 + `VerticalAddProfileWidth` | 500+100=600 (RS10 기성 가산) |
| RS12B | param | F2 | 500 |
| RS12C | none | — | 삭제 |
| RS12D | param | F2 + `VerticalAddProfileWidth` | 500+100=600 |
| RS13 | param | F2 + `VerticalParamKey2`=F3 | 좌 800+우 800 (cycle119 기성) |
| RS14 | none | — | 삭제 |
| RS15 | param | F2 | 600 |
- 밸룬 포트 앵커(MemberAnchorSide)는 이번에 지정하지 않음(밸룬 지적은 "허공"뿐 — 가로 스팬 정합으로 자동 해소 기대, P4 선례).

## Q2. 가로치수 — 앵커 전략 타입별화
- 현행: `rcMemberGeometry` 목록 + 파이프중심 앵커(ad3aead). RS12A/B/D는 **파이프가 부재 끝/측면 부착**이라
  파이프중심 ± left/right가 성립하지 않음 → 앵커 전략 config 신설:
  - `HorizontalAnchor`: `"pipe"`(기본, 기존 타입 전부) | `"memberRight"`(부재상자 우측끝 — ad3aead 이전 방식 재사용)
  - RS12A·RS12B = params(A+A1) + memberRight / RS12D = **공식형**(A+PipeSize(Dn)/2+100, label_66과 동일) + memberRight
- `TryGetNotabRcHorizontalParams`는 A1 필수라 RS12D(A만)는 못 탐 → RS12D는 공식형 산출 별도(총폭만, 분할은 left=A/right=파이프/2+100).
- 기존 타입(파이프중심) 무변경 — RC5/RS4/RC7/RS5/RS6 로그 diff 0이 회귀 기준.

## Q3. 후속 관찰(이번 집도 후 재평가)
- RS12B 파이프·U볼트 콜아웃 과신장 — 스팬 정합 후 재추출로 재평가(그래도 길면 위치 노브).
- RS12B/C/D BOM rows 0(아침 관찰, 예외로그 보강 전) — 최신 로그로 재확인. RS12C는 param이 {A}뿐이라 분기 자체 부재 가능성.

## 검증 (이격 배치 상태에서 dev_test 태그 RS11~15 + 회귀 RC5,RS4,RS6)
| 대상 | 기대 |
|---|---|
| RS11 | 세로 550(A+A1) |
| RS12A | 세로 600 / 가로 500 스팬=부재 / F1·F2 밸룬 부재 접속 |
| RS12B | 세로 500 / 가로 500 / F1 접속 / (콜아웃 길이 관찰) |
| RS12C·RS14 | 세로치수 없음 |
| RS12D | 세로 600 / 가로 445 스팬=부재 / F1·F2 접속 |
| RS13 | 좌 800 + 우 800 |
| RS15 | 세로 600 |
| 회귀 | RC5·RS4·RS6 extX/dimV 로그 무변화 |
