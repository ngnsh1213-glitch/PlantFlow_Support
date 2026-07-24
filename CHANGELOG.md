# CHANGELOG

이 프로젝트의 사람이 읽는 변경 이력 요약. 상세는 git 커밋 로그 참조.

## [Unreleased]
### Changed
- cycle127: 무탭 평면화 폴리라인을 솔리드 출처별 support/pipe/유볼트(tag별) 블럭으로 묶었다. 전체 bbox 좌하단 공유 기준점을 사용해 삽입 좌표를 보존하며, `PFSNOTABBLOCK` 분류·블럭 카운트 로그를 추가했다. — 2026-07-24
- cycle104: Drawing 팔레트의 구 오쏘 Export 2D 배선을 무탭 배치로 전환했다. 뷰 방향·Grid 게이트를 제거하고,
  수동 다중 선택 추가, 문서·Database 캡처 가드, 완료 ok/fail 요약 및 초기/실행 중 입력 차단 오버레이를 추가했다. — 2026-07-22
- cycle93: HANTEC 클래스·파일·호출을 StandardSupport로 개명하고, PFS STANDARD/기존 HANTEC을 인식하는 단일 DesignStd 판정과 로그를 추가했다. 오쏘 주석 생성 전에 BOM을 초기화해 StandardName 누락을 해소했으며, 무탭 BOM은 실제 모델 DesignStd와 빈 값 폴백을 사용한다. — 2026-07-20

### Fixed
- 무탭 격리 결함+부속 가드 종결(cycle121, `57e0d20`~`f6c3c4a`): ①인접 본체 서포트 혼입 차단 — 수집 시 부속(UB류)만
  자동 포함(코드 목록 단일 원천화, 빈값=포함+WARN) ②부속 단독 추출 거부 가드 ③세로재 리더 관통 타입별 0
  확산(RC1/2/3/6/9 — 20 유지는 RC4·RC5만). RC1 혼입 소멸·UB 단독 거부·무간섭 전건 라이브 통과. — 2026-07-23
- 무탭 RS11~15 타입 검증 종결(cycle120, `6c33cd8`~`b3f269c`) — **RS 패밀리 15종(변형 18종) 완주**:
  ①standardName 태그접두 해상도(RS12B/C/D config 매칭) ②합성 키 "A+A1"·@formulaA 세로 키 ③측면부착 3형제
  HorizontalAnchor=memberRight(파이프 실좌표 보존·분할 생략) ④★V-SPLIT 세로 파이프분할(상/하+총고 스택)
  ⑤스파인 밸룬 vertical-end 옵트인(VerticalBalloonKeys) ⑥투영<실장 시 값=실장 표기 ⑦파이프 하단 타입 세로치수
  상단 기준(RS13 20mm 밀림) ⑧치수 오버라이드 0.1 자리. — 2026-07-23
- 무탭 RS6~10 타입 검증 종결(cycle119, `49cde0f`+`ad3aead`): ①config RS6~10 행(세로 실장·F2 밸룬 포트앵커·VLeaderExt=0)
  ②★양쪽 세로치수 신설 — 다리 2개 타입(Ha/Hb)은 dimVL(좌)+dimVR(우) 표시(`VerticalParamKey2`) ③RS10 세로=F2+단면(575, BOM 규칙 준용)
  ④RS5/6 가로 params(800)+F1 밸룬 앵커 자동정합 ⑤가로 params 스팬 앵커를 부재상자 우측끝→파이프 중심으로 전환 —
  門형 다리 돌출로 주석 전체가 13paper mm 밀리던 결함 해소(RC5/RS4/RC7 수치 동치, RS4 회귀 없음 확인). — 2026-07-23
- 무탭 RS1~5 타입 검증 종결(cycle117~118+후속, `8bf878d`~`465fb74`): ①config RS1~5 행(RS1/2 세로 없음, RS3/4=F2, RS5=Ha)
  ②RS1 BOM 645→500(A+파이프여유 공식이 RS1 기하와 불일치) ③RS5/6 BOM 전멸 해소 — 원인 2중: C#이 존재하지 않는 F2/F3 키 요구
  (Ha/Hb 개명 전 잔재, 신규 분기+원자적실패) + .acat/.pspc ParamDefinition 구명 스냅샷(SQLite 직접 치환, 백업 .bak_20260723)
  ④RS4 좌우 스왑(RC7식) — 분할·파이프콜아웃·치수중심 동시 교정 ⑤F2 밸룬 타입별 오프셋 config+support bbox 면제
  ⑥RS2 F2=대각재 포트앵커(리더 붕괴 해소) ⑦세로재 리더 관통연장 타입별화(RS3/4=0) ⑧RS5 세로치수선 스팬=param 클램프.
  실측 발견: Details DWG는 Fasoo DRM 암호화(외부 계측 불가), 카탈로그 에디터는 ParamDefinition을 재생성하지 않음. — 2026-07-23
- cycle104 UI 라이브 검증 PASS + MDI 가드 결함 수정(`3a0d2ce`): 일괄 선택 추가 후 무탭 추출이 ER_DOCMISMATCH로 즉사 —
  `Document.Database` 관리 래퍼가 재생성되어 `ReferenceEquals`가 같은 도면에서도 false. 캡처 시점 `UnmanagedObject`(IntPtr)
  비교로 교체. 일괄선택 필터(42→17, UB 제외)·추출·오버레이 라이브 정상 확인. — 2026-07-23
- 무탭 RC4~9 타입 회귀 검증 트랙 종결(cycle105~116, `1ee2287`~`7de446d`): 全타입 테스트 모델(TYPE-001) 기준 GD1~3+RC1~9 전 타입 통과(2026-07-23).
  **치수**: ①`GetNotabTypeConfig`에 RC4/5(param/F2/vertical)·RC7/8(`none`=세로 미작도 신설) 행 추가, `rcMemberGeometry`에 RC4/5/7 확장 —
  세로가 부재 단면 F(50/75/100)를 뽑던 폴백 해소(RC4 600·RC5 600), 가로 A+A1 교정(RC5 700→650) ②RC7 A/A1 분할: `paramsHorizontal`이면
  splitGuard 우회 + RC7만 paramLeft/Right 교환(150/800). **밸룬**: ③RC5 F1/F2 포트 재지정 — 디컴파일 포트 의미를 원본 `RC5.py`로 확정
  (S2=가로재 F1끝, S3=세로재 F2 자유단), `IsNotabVerticalMemberPort` RC5만 index2 ④RC9 P1_1=콜아웃 **리더 교차만** 허용 하향 배치
  (box·부재는 차단, cycle107 box허용→F3관통 반려의 정정) ⑤세로재 리더 관통 연장 `PFS_NOTAB_VLEADER_EXT`(기본 20)+화살촉 축소(리더길이×0.45).
  **파이프 콜아웃**: ⑥vertical-member 장애물 box-only(리더의 기둥 가로지름은 사용자 승인) ⑦**높이밴드 규칙** `PFS_NOTAB_PIPE_DY_W`(기본 0.25,
  파이프 전용 |Y-편차| 페널티 — 상향 도주 근인이 Y비용 0) ⑧수동 위치 노브 `PFS_NOTAB_PIPE_POS_<TYPE>` + RC5 출하값 config 승격
  (`PipeCalloutDx/Dy`=100,20, 우선순위 env→config→auto). **교훈**: 심미적 배치는 원격 비용함수 튜닝으로 비수렴(9사이클) —
  수동 캘리브레이션으로 정답 측정 후 규칙 번역이 정도 — 2026-07-23
- PERSPECTIVE 가드 발원 기반 재설계(cycle103, `83d0a3e`): 무탭 추출 직후 원본 뷰가 Parallel→Perspective로 뒤집히던
  간헐 재현을 해소. 원인은 추출 종료 +3~5초에 AutoCAD 내장 리본 WPF 바인딩(`RibbonListButton.set_Current → SETVAR`)이
  `PERSPECTIVE=1`을 역기입하는 것이며, 구 가드의 8초 창 마진 부족(+2초)이 간헐 재현의 정체였다.
  ①**어셈블리 기반 3분류**(`ClassifyPerspOrigin`): 스택 프레임의 `AdWindows` 어셈블리/`Autodesk.Windows` 네임스페이스면
  strong-ribbon → 복원, 그 외 VIEWCUBE 포함 시 native-command, 나머지 unknown → 관망(부분 문자열 매칭은 JIT 인라이닝·버전 취약이라 폐기).
  ②**1회 자폭 제거** — 늦게/반복 오는 flush를 백스톱 60초 내 계속 잡는다. ③**generation+대상 문서 스코프** — 추출마다 재무장,
  명령구독 만료·문서전환 시 해제(누수 0). ④**VIEWCUBEACTION 미개입**(`CommandWillStart/Ended` 계측만) — 강제 복원은
  뷰큐브 내비게이션과 경쟁 위험(§9 자문 Codex 채택). 라이브 PASS: flip→교정 15ms로 체감 flicker 없음. env `PFS_PERSP_GUARD_SEC`(기본 8→60) — 2026-07-22
- 무탭 라인넘버 콜아웃만 꺾임 1회 리더로 복원(`152dc46`): cycle100에서 직접 콜아웃 공통 경로를 1자로 통일하며
  파이프까지 직선이 됐던 것을 되돌렸다. `TryPlace`에 `tailLength`를 추가해 >0이면 문자 접속점에서 앵커 쪽으로
  물러난 점을 꺾임점(`p1`)으로 잡아 마지막 구간이 수평 꼬리가 된다(0이면 `p1==p2` 직선, 유볼트·부재는 0).
  검사 `Free`는 원래부터 두 선분을 보므로 꺾임 구간이 자동으로 검사되며, 작도·등록(`Commit` 3점)도 같은 2선분을 쓴다.
  꼬리 길이 `PFS_NOTAB_PIPE_LEADER_TAIL`(기본 `txt×2`=16, 0이면 직선). 라이브 3회 skip 0건, 파이프 전부 `tier=0` — 2026-07-21
- 무탭 밸룬·콜아웃 배치 트랙 종결(cycle97~102, `8cfd311`~`947663e`): 라이브 3회 skip 0건, 사용자 판정 "이상없음".
  ①**유볼트 화살표**를 실제 유볼트 박스(포함 박스 중 최소 면적, 과대 기각)의 **좌/우 세로 변 중점**에 고정 —
  U자 하변 중점은 다리 사이 빈 공간이라 금지. 리더는 꺾임 없는 직선 1개, 문자 중단 접속, 전용 이격 `PFS_NOTAB_UBOLT_MIN_DX`(10).
  ②**부재 밸룬 전용 배치** — 부재 끝단 바깥 좌/우 2후보를 같은 단계에서 평가해 여유 큰 쪽 채택, 수평 직선 리더.
  거리는 `gap+radius+step×k` 단계 확장이며 자유 후보가 나오는 첫 k에서 멈춘다(F1은 k=0, F2는 k=3~4).
  ③**세로재 좌표를 포트 기반으로 전환** — 기둥은 독립 솔리드가 아니라 `7A`에 병합돼 있어 솔리드 상자를 쓰면
  서포트 전체 외곽을 찍는다. `rawAnchor`(S2 투영)+`verticalBaseY/TopY`(S2+F2×vScale) 사용, `verticalX`(치수선 X) 오용 제거.
  ④부재 밸룬을 콜아웃보다 먼저 배치(표·치수·파이프 장애물 등록은 선행 유지). 실패 시 폴백 없이 생략(R2) — 2026-07-21
- 무탭 RC 트랙 종결(cycle92 후속, `65cc9cb`/`cc0addc`/`647749b`): ①`AppendNotabPaperDimensions`에 `Viewport` 전달(CS0103) ②**기둥이 뻗는 방향이 타입마다 반대** — `S2`는 밑동이 아니라 자유단이라 RC1(상단 기준)에서 "항상 위로" 가정이 범위를 벗어나 폴백했다. 위/아래 중 서포트 범위에 들어오는 쪽을 채택하고 `port-S2(up)|(down)`로 기록 ③콜아웃 간 여백 도입 — 겹침 0인데 간격 3.05라 붙어 보였다. `PFS_NOTAB_CALLOUT_PAD`(기본 8=글자 높이)만큼 부풀려 검사. 사용자 판정 "RC 이상없음 종결, GD 회귀 없음" — 2026-07-20
- 무탭 RC 가로 치수를 SupportParams로(cycle90): 가로재와 베이스 플레이트가 한 `Solid3d`라 bbox는 490이고 450은 기하에서 나오지 않는다. **총 폭=`A+A1`, 우측=`A2`(없으면 `A1`)** 로 전환해 RC1 450(350/100)·RC2 450(250/200)·RC3 500(250/250) 확정 — 2026-07-20

### Added
- 무탭 RC 기둥 포트 앵커 + U-bolt 태그 콜아웃(cycle92): RC1/RC2/RC3 세로 치수·세로 부재 콜아웃을 S2(index=1, 이름 검증)와 F2 높이 기준으로 전환하고, 포트/F2/투영/범위 가드 실패 시 기존 기준으로 폴백. 자동 포함 U-bolt는 Tag+bbox 중심을 별도 스냅샷해 개별 콜아웃으로 배치 — 2026-07-20
- 무탭 N3 치수 핵심(cycle47~49, cfa1fb7~9576258): 페이퍼공간 비연관 치수 직접 제도. ①스케일 표준배율 라운딩+주석여백(`PFS_NOTAB_TARGET_FILL` 0.4, `vp.CustomScale` 명시) ②WCS→paper 투영(`NotabProjectWcsToPaper`, ViewportProjection 이식) ③가로총폭·pipeCenter분할·세로 치수(텍스트=실측mm, 페이퍼 고정크기 `PFS_NOTAB_DIM_TXT` 2.5mm) ④배관 참조: 분할=실배관중심X, 가로치수 상/하단=배관 근접쪽. 라이브 split=(350,100) side=bottom PASS — 2026-07-16
- 무탭 held-pipe 선택 = 서포트 BOP 표고 매칭(cycle44~46, 1cb3192~b0e5c71): 서포트가 실제 잡는 배관만 포함. 같은 라인 평행 배관은 라인/rect/중심거리로 불가 → 서포트 `BOP`(배관 밑면표고)로 `|(pipeCenterZ−외경반경)−BOP|` 최소 선택. env `PFS_NOTAB_BOP_TOL`/`PFS_NOTAB_CONTACT_TOL`. BOP 없으면 기하 폴백+경고 — 2026-07-16
- 무탭 서포트 영역 클립+동적 피팅(cycle43, f5473d8): oriented 클립박스 Solid3d Boolean INTERSECT로 관통 파이프 길이 트림(10m→서포트깊이)·이웃 배제, 고정 1:4→콘텐츠 동적 피팅. env `PFS_NOTAB_CLIP_MARGIN`/`PFS_NOTAB_FIT_PAD` — 2026-07-16
- 무탭 뷰포트 와이어프레임 기본화(999e1f9): 모델영역 Hidden→와이어프레임. Hidden은 `PFS_NOTAB_USE_HIDDEN=1` opt-in — 2026-07-16
- 무탭 perspective 방어 가드(6e01a9b): 추출 후 AutoCAD 리본 WPF 바인딩이 유휴 시점에 `PERSPECTIVE=1`로 역기입하는 현상을, 파이프라인 진입 8초 창 내 1회 복원+Idle REGEN으로 취소. env `PFS_PERSP_GUARD_SEC`. 진단 명령 `PFSPERSPPROBE`/`PFSPERSPWATCH`(스택 계측) — 2026-07-16
- 무탭 자동포함 접촉 판정(73b9974): `AutoIncludeRelatedParts` 마진을 서포트 크기비례(0.5×max) → 고정 tol 150mm(env `PFS_NOTAB_MARGIN`)로 교체. 큰 서포트에서 이웃 서포트가 2D에 묻어나오던 결함 해소, 유볼트/관통파이프만 포착 — 2026-07-16
- 격리 Main 뷰 가로 치수 파이프중심 분할(B4d, f6c99fa): 파이프 원 중심 X 기준 좌/우(예 100/200) + 전체(300). `IsoCircleCandidate.CenterX`·`pipeCenterX` 노출 — 2026-07-13
- 격리 재실행 중복 detail 제거(B4e-A, cf44403): clone-back 직전 기존 `PFS_ISO_DETAIL`/`AUTO_DIM` 삭제(`PurgePriorIsoDetail`) → 태그당 최신 1개만 — 2026-07-13
- 프로젝트 문서 규칙 도입: `CLAUDE.md`, `AGENTS.md`, `SESSION.md`, `TODO.md`, `CHANGELOG.md` (Claude/Codex 공용 상태 관리). PFO에서 PFS를 별도 프로젝트로 분리 — 2026-07-11

### Known Issues
- 격리 파이프라인 후 뷰큐브 미표시(B4e-B REGEN 불충분, 뷰 전환 시 복귀). 후속 보류 — 2026-07-13
- PFS 오쏘 B1c-Main: 서포트 선택→S1 길이방향 Main 뷰 (PFSVBSUPPORT, 46d87da)
- PFS 오쏘 B1b: Main+Top+ISO 다중 뷰 번들 생성 (PFSVBMULTI, e465ecb)

### Notes
- VIEWBASE 뷰생성 커널 완성(PhaseA/B1b/B1c-Main). 다음 단계 = 격리→멀티뷰→B2 치수.
