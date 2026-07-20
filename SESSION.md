# SESSION — 현재 작업 상태

## ★★★이관 스레드 상태 (2026-07-21, 다음 세션 시작점)

### ① 진행 중 트랙 — 무탭 BOM 표 + 밸룬 (cycle 94~96)
오쏘의 BOM/밸룬 자산을 무탭에 합치는 트랙. **밸룬은 이식이 아니라 신규 설계**다.
- **cycle 94(계측) 종결**: 오쏘 `CreateAnnotations()`의 밸룬 루프는 `P*`/`R*` ITEM만 처리하고
  **`F*` 분기가 없다** → `bom-item item='F1' branch=NONE anchorExact=True`.
  **부재 밸룬은 오쏘에도 존재한 적이 없다.** 생성되는 리더는 `PLN`/`BOP` 둘뿐.
- **cycle 95(BOM 표+밸룬) 집도 완료**: 표·밸룬 모두 도면에 나온다(사용자 확인).
- **cycle 96(밸룬 배치)** 진행 중. 핸드오프 발행(`f8dc2c7`)했으나 **사용자 지시로 Claude가 직접 집도**.

### ② 직전 실측 (2026-07-21 08:03~08:04 로그)
```
RC2 F1 anchor=(339.5,368.9) center=(369.1,239.6) r=129.3 cost=48.79 scanned=181
       reject(oob/box/extLeader/calloutLeader)=1852/659/630/974  extLeaderBy{unnamed=630}
RC3 F1 r=149.3 cost=43.99   RC3 F2 r=134.3 cost=95.34 scanned=37
```
**★밀집 원인 확정(자문 검증)**: `costRefExt`는 밸룬별 앵커가 아니라
`AppendNotabPaperDimensions()`에서 **한 번** 설정된다(서포트+파이프 bbox).
`preferDown=false`·`angleW=0`이라 실질 비용은
`cost = costRefExt 밖으로 나간 양 + radius × 0.01` →
**거리가 사실상 공짜**라 "멀지만 기준상자 안쪽" 후보가 항상 이긴다.
`extLeaderBy{unnamed}` = 차단자는 서포트가 아니라 **치수**.

### ③ 대기 중 액션 — `28237e7` 라이브 미검증
마지막 집도(`28237e7`)를 **아직 추출해보지 않았다.** `dev_test.bat` 후 로그 확인이 다음 단계.
확인 키:
- `balloon-draw`의 `dir=/dist=/clear=/score=` — **`dist`가 리더 길이**(기존 129~149 → 20~40 기대)
- 밸룬이 각 부재 옆 빈 공간에 흩어졌는지(F1 우측 / F2 기둥 옆 / P1 하단)
- 유볼트 화살표가 원 바깥 quadrant에 닿는지, `ubolt-box 없음` 로그 유무
- `balloon-skip ... reason=` / 기존 콜아웃(치수·라인넘버·부재) 회귀

### ④ 사용자 확정 배치 규칙 (수작업 그림 기준, 재논의 금지)
1. **화살표는 부재 중심이 아니라 외곽 접점**. 유볼트는 원형이므로 **quadrant 포인트**.
2. **리더는 꺾임 없이 직선 1개**.
3. **밸룬은 부재 옆 "한적한" 자리** — 서포트 bbox 밖이 아니라 **도면 내부 빈 공간도 사용**.
4. 밸룬 옆 부재 코드는 **가로 방향**(원 바깥쪽). `ANGLE A6` → `A6`.

### ⑤ 구현 상태 (cycle 96, 세 번 갈아엎음)
| 시도 | 커밋 | 결과 |
|---|---|---|
| 전역 비용 탐색(`TryPlace` 재사용) | `6e423f8` | **밀집**(r=129~149) |
| 둘레 배치(서포트 bbox 밖 4변) | `a45d9b0` | 방향은 맞으나 **내부 빈 공간 미사용** |
| **부재 외곽 접점 + 한적도 점수** | **`28237e7`** | **미검증** |

현행 로직:
```
부재 사각형 = 세로재(기둥구간 cycle92) / 가로재(A+A1 cycle90) / 그 외(포트 주변 소형)
후보 = 부재 사각형에서 8방향 × 거리 단계
score = placer.ClearanceTo(box) − 리더길이 × PFS_NOTAB_BALLOON_LEADER_W(0.6)
화살표 = NotabCalloutPlacer.BoxEdgeToward(부재 사각형, 밸룬 중심)
판정 = placer.IsBalloonFree — 상자 겹침은 전 장애물 금지,
       리더 교차는 기존 콜아웃만(서포트·치수 위 통과는 정상)
```

### ⑥ 미결 정책 (사용자 결정 대기)
- **(가) 밸룬 대체 vs (나) 텍스트 콜아웃 공존** — 현재 기본 **(가)**.
  `PFS_NOTAB_MEMBER_TEXT=1`이면 (나). 단 (나)는 designation 중복 제거로
  **밸룬 3개 + 텍스트 1개**가 되어 짝이 안 맞는다(RC는 가로·세로가 같은 `ANGLE A6`).
  제3안 = 텍스트를 BOM 행에서 생성(`F1 → L-65×65×6 L=450`).
- 밸룬 반지름·코드 위치 기본값 확정(`PFS_NOTAB_BALLOON_R`, `PFS_NOTAB_BALLOON_SUB=below|side`).

### ⑦ 이번 세션에서 잡은 자체 결함 (재발 주의)
- **`PFS_NOTAB_CALLOUT_PAD` 이름 결합**(`ef09036`): `AddObstacle` 패딩이 같은 노브를 읽어,
  cycle92에서 콜아웃 여백용으로 기본 8을 도입하자 **모든 장애물이 2→8로 부풀었다**.
  → `PFS_NOTAB_OBSTACLE_PAD`(기본 2)로 분리. **밸룬과 무관하게 기존 배치에도 영향을 주던 결함.**
- **BOM 표 `FlowDirection` 누락**(`01bcbb4`): 기본값이면 표가 아래로 자라 타이틀블록에 묻힌다.
- **`PSUtil.Log`는 파일 로그가 아니다**(`6d71127`): `Editor.WriteMessage`라 사후 검증 불가.
  진단은 반드시 `PlantOrthoView.FileDiag`.
- **계측 코드 자체의 결함**: 무탭 BOM 행에는 헤더가 없는데 행 0을 헤더로 건너뛰어
  `balloon-diff`에서 F1이 누락됐다(`9a6abff`).

### ⑧ 재조사 금지 (확정 실측)
- `BOMs`는 Plant 프로젝트·DataLinksManager 의존 → **side DB 호출 불가**. 원본 단계 캡처 필수.
- `TaggingPoints`의 `p1`은 `p0+(50,50,z=0)` **방향 힌트**, 실좌표 아님.
- `S<n> → F<n>` 일반 규칙 없음. 타입별 메서드가 포트 인덱스를 개별 지정
  (RC1: `F1←PPorts[2]`, `F2←PPorts[1]`, `P1_0←PPorts[3]`).
- 세로 기둥은 독립 솔리드가 아니다(`7A`가 기둥+가로재+플레이트 병합). 유볼트는 독립.
- 상세도에 **2D 선분이 없다**(복제 `Solid3d` + 뷰포트 와이어프레임).
- `NotabProjectWcsToPaper`는 실패 시 `Point3d.Origin` 반환 → 밸룬은 `TryNotabProjectWcsToPaper` 사용.

### ⑨ 관련 파일·경로
- 계획서: `.plans/plan_notab_bom_balloon_measure_20260720.md`(cycle94 계측)
  · `plan_notab_bom_balloon_design_20260720.md`(cycle95 설계)
  · `plan_notab_balloon_placement_20260721.md`(cycle96 밀집)
- 핸드오프: `.plans/HANDOFF.md`(cycle 96, **Claude 직접 집도로 대체됨**)
- 코드: `PlantFlow_Support/Core/Commands.cs`
  - `NotabCalloutPlacer`(상단 ~200): `TryPlace`/`Free`/`IsBalloonFree`/`ClearanceTo`/`CommitBalloonBox`/`BoxEdgeToward`
  - `AppendNotabBomTable` · `AppendNotabBalloons` · `AppendNotabUboltCallouts` · `AppendNotabDirectCallout`(anchorBox 오버로드)
  - `MeasureNotabBomBalloonSources`(원본 DB BOM·TaggingPoints 스냅샷)
- 로그 `C:\Temp\pfs_diag.log`. 키: `balloon-draw`/`balloon-skip`/`bom-table`/`ubolt-box`/`MEASURE *`
- 최근 커밋: `6e423f8`(표+밸룬) `01bcbb4`(FlowDirection) `717cb98`(직선리더) `9c1e4c3`(코드표기)
  `253c967`(코드 옆배치) `7a66482`(앵커 중앙) `0ee7d75`(서포트 리더면제) `bca5efa`(차단자 계측)
  `ef09036`(패딩 분리) `a45d9b0`(둘레배치) `28237e7`(**외곽접점+한적도, 미검증**)

### ⑩ 프로세스 교훈
- **같은 문제 3회 실패 시 상수 튜닝 중단** — cycle96에서 앵커 이동·리더 면제·패딩 분리가
  연속 실패한 뒤 계획→자문→핸드오프로 전환해 원인(비용식)을 확정했다.
- **자문이 "거리 가중치만 올리면 네 번째 튜닝 실패"라고 사전 경고**했고 그대로였다.
- **로그 수치가 한 자리도 안 바뀌면 그 가설은 틀린 것** — 서포트 리더 면제가 무효였던 판정 근거.
- **배포 DLL 시각과 로그 시각을 먼저 대조**할 것. 한 번은 수정 전 바이너리 결과를 보고 있었다.

---


_최종 갱신: 2026-07-20 (cycle93 StandardSupport 개명·DesignStd 외부화 집도 완료, 라이브 검증 대기)_

## ★★★ cycle 93 종결 — StandardSupport 개명 + DesignStd 외부화 (2026-07-20, 전 항목 검증)

### 결과
- `HANTEC` 클래스·파일·정적호출 89곳 → **`StandardSupport`**. 빌드 1회 통과.
- 리터럴 `"HANTEC"` 4곳은 **개별 처리**(일괄 치환 금지 지시 준수). 남은 것은
  지원 목록·빈값 폴백·주석뿐. `span_table_JIS.json`의 `WELCRON HANTEC`(출처 데이터) 보존.
- **표준 판정 단일 지점** = `StandardSupport.IsSupportedStandard()`.
  목록 `{ "PFS STANDARD", "HANTEC" }`, 대소문자·공백 무시. **고객사 표준은 이 목록에만 추가.**
- 무탭은 `GetNotabBomDesignStd()`로 **실제 `s_isoDesignStd`** 전달, 빈 값이면 legacy 폴백+로그.

### 라이브 검증 (전부 통과)
```
value='HANTEC'       supported=True   (RC1-001 등)
value='PFS STANDARD' supported=True   (GD1-003)
```
- **같은 도면에 구/신 값이 섞여도 둘 다 통과** → 출하 카탈로그 전환 시 기존 프로젝트 안 깨짐.
- `DesignStd empty; legacy fallback` 로그 없음 = 폴백 미발동, 실제 값 사용 중.
- 무탭 RC1~3·GD1~3 도면 회귀 없음(사용자 판정).
- **오쏘 BOM 표 정상 생성 확인** — `F1 FRAMEWORK / ANGLE A7 / ASTM A36 / 445`,
  타이틀 `PIPE SUPPORT DETAIL FOR GD1-003`.

### ★자문(Codex)이 찾은 독립 결함 — 개명과 무관하게 오쏘 주석이 죽어 있었다
`CreateAnnotations()`가 `ContentsByDesignStd()`를 호출하지 않은 채 `boMs.StandardName`을
`StandardInformation()`에 넘겨 **그 값이 `null`**이었다 → 타입별 `TaggingPoints` 미생성.
`HANTEC` 객체에서도 마찬가지였다. **헬퍼만 교체했으면 "고쳤는데 여전히 주석 없음"으로 끝났을 건**.
→ `OrthoViewportManager.cs:786`에 BOM 초기화 추가로 해소.

### 후속 교정 (Claude)
- `PSUtil.Log`는 `Editor.WriteMessage`(명령행)이라 `pfs_diag.log`에 안 남아 사후 검증 불가였다.
  → **`PlantOrthoView.FileDiag`로 교체**(`6d71127`). 진단은 파일 로그가 이 프로젝트 표준.

### 커밋
`d5b237f`(Codex 개명·외부화) · `6d71127`(Claude 진단 경로)
계획서 `.plans/plan_standard_support_rename_20260720.md`

### 제품 맥락 (확정)
Python 서포트 스크립트 + PFS **한 패키지 판매**. 기본 스탠다드 = HANTEC 규격집을 개명한
`PIPE SUPPORT STANDARD`, **출하 `DesignStd` = `PFS STANDARD`**. 고객사 표준은 회사명으로 추가.
타입 메서드(`RC1()`/`GD1()`) **구조 분리는 두 번째 표준이 실제로 올 때**(현재 보류).

### 다음 — ★오쏘 주석 자산을 무탭 엔진으로 통합 (사용자 방향 제시)
오쏘에서 BOM 표가 나오는 것을 확인했으므로, 이 자산을 무탭 상세도에 합친다.
무탭은 이미 치수·라인넘버/BOP 콜아웃·부재 콜아웃·U-bolt 콜아웃을 갖췄고,
**남은 것은 BOM 표 + 밸룬(F1~F4 마크 ↔ TaggingPoints)**이다. `TODO.md`의 N4 항목.

---

## ★★★ cycle 92 종결 — RC 패밀리 완료 (2026-07-20, 사용자 "RC 타입 이상없음 종결" + "GD 회귀 없음")

### 결론: 추정 기하 → 실측 포트로 전환하며 RC 트랙이 끝났다.
- RC 세로 치수·세로 부재 콜아웃 = **`S2` 포트(index=1 + 이름 검증) + F2 높이**의 기둥 기준.
- U-bolt = `Tag` + 원본 bbox 중심을 별도 스냅샷해 **개별 콜아웃**. 빈/중복 태그·`ShortDescription != UB` 제외.

### ★핵심 교훈 — 기하로 못 찾던 것이 포트에 있었다
RC1→RC2→RC3에서 **비율 상수 추정이 3연속 실패**했다. 원인은 하나였다:
- **세로 기둥은 독립 솔리드가 아니다.** `7A`가 기둥+가로재+베이스 플레이트를 통째로 병합.
  `7C`(페이퍼 17.82×17.82 정사각형)는 볼트류. **기하 분류로는 기둥을 찾을 수 없다.**
- 상세도에는 **2D 선분이 없다**(복제 `Solid3d`+뷰포트 와이어프레임) → 선분 계측 불가.
- `7A` bbox ↔ `A/A1/A2` 역산은 **순환 논증**(같은 union solid 외곽의 재표현).
- **답은 `HANTEC.RC1()`에 이미 있었다** — `PPorts[1]`(F2)로 앵커를 뽑고 있었다.
  포트는 솔리드 병합과 무관하게 **부재별 부착점을 보유**한다.

### 가로 치수도 기하가 아니라 파라미터에서
`support params dump` 실측으로 규칙 확정:
```
RC1 A=350 A1=100        → A+A1=450 (350/100)
RC2 A=400 A1=50 A2=200  → A+A1=450 (250/200)
RC3 A=250 A1=250 A2=250 → A+A1=500 (250/250)
```
**총 폭=`A+A1`, 우측=`A2`(없으면 `A1`), 좌측=나머지.** 490은 파라미터에 없는 값(=플레이트 bbox).

### 라이브에서 잡은 후속 3건 (Claude 직접 집도)
1. **`vp` 미전달** CS0103 → `AppendNotabPaperDimensions`에 `Viewport` 파라미터 추가 (`65cc9cb`)
2. **★기둥 방향이 타입마다 반대** (`cc0addc`) — `S2`는 밑동이 아니라 **자유단**이다.
   RC1은 `S2`가 상단(382.25, ext max 388.75)이라 "항상 위로" 가정이 482.25로 범위를 벗어나
   `post-outside-support-extents` 폴백 → 세로 치수·부재 앵커가 함께 옛 추정식으로 회귀했다.
   → **위/아래 중 범위에 들어오는 쪽 채택**, 로그에 `port-S2(up)|(down)` 표기.
   **가드가 잘못된 작도를 막고 사유를 남긴 덕에 한 번에 원인이 잡혔다**(cycle 88 원칙의 실효).
3. **콜아웃 간 여백 없음** (`647749b`) — 겹침 0인데 간격 3.05(글자 높이 8)라 붙어 보였다.
   `_placedBoxes` 비교 시 `PFS_NOTAB_CALLOUT_PAD`(기본 8)만큼 부풀림. 리더 교차는 원본 상자 기준 유지.

### U-bolt 태그 = 데이터 문제였음 (코드 정상)
`Tag=?`였다가 사용자가 모델에 태그 부여 후 `Tag=UB-002`/`UB-003` 정상 취득.
사용 키는 **`Tag`**(`SupportName`도 동일 값). `PartNumber`·`TagName`·`Description`은 공백.

### 커밋
`8caaa82`(Codex 집도) · `65cc9cb` · `cc0addc` · `647749b` (Claude 후속)
계획서 `.plans/plan_notab_rc_port_anchor_20260720.md` · 그 전 단계 `plan_notab_member_identify_20260720.md`
· `plan_notab_rc_post_and_ubolt_20260720.md`

### 다음
- **cycle 93 = `HANTEC` → `StandardSupport` 개명 + `DesignStd` 외부화**(`TODO.md` 예약분).
- 이후: 나머지 타입(SHOE/RS/TR/FS…) 확장.

---

## ★★★ cycle 88 종결 — 콜아웃 배치 규칙 확립 (2026-07-20, 사용자 "이상 없음" 판정)

### 확정 규칙 (사용자 결정, 재논의 금지)
- **R1** 라인넘버(B.O.P) 콜아웃 좌우 = 파이프 중심 X가 **뷰포트 사각형 중앙선**의 어느 쪽인가. **서포트 타입 무관**.
- **R2** 부재(L-/C-) 콜아웃 좌우 = 같은 방식, 기준은 **부재 자신의 앵커 X**.
- **R3** 상하는 고정값 없음 — 간섭 회피로 탐색. 단 **부재는 하단 선호 편향**(치수가 항상 상단·좌측).
- **R4** 좌우 **절대 불변**. 막히면 상하·거리만 조정, 반전 금지. 끝까지 실패하면 작도 생략(`callout-skip`).
- 경계(정확히 중앙)는 `referenceX >= centerX` → **우측** 결정론 고정.

### 자문(Codex)이 잡아낸 구조적 결함 3건 — 전부 실측 확인 후 해소
1. **이중 `TryPlace`** — 호출부(구 5198/5380)에서 부른 결과를 `AppendNotabDirectCallout`이 다시 호출해 덮어씀.
   → `PFS_NOTAB_DIR_*` 방향 노브가 **죽은 코드**였다(cycle87이 넣은 제어가 애초에 작동한 적 없음). 배치 1회로 단일화.
2. **중앙선 기준 오류** — placer의 `_minX/_maxX`는 뷰포트가 아니라 `supportPaperExt ± 100`. 실제 `Viewport.CenterPoint/Width/Height`로 교체.
3. **무검사 fallback 작도** — `TryPlace` 실패 후에도 `fallbackLeft`로 장애물 검사 없이 그림. 제거하고 작도 생략으로 변경.

### 추가 교정 (라이브 로그로 발견)
- **허용영역이 좁아 라인넘버 3건 전량 skip**: `reject(oob)=3096/3672/3432`, **장애물 거절 0건**. 좌우 고정 후 폭 119.49 텍스트가 마진 100을 초과.
  → 허용영역을 **실제 뷰포트 사각형**으로 교체(`callout-bounds` 로그로 출처 기록, 취득 실패 시 구 계산 폴백).
- 부재 하단 선호 = `PFS_NOTAB_CALLOUT_DOWN_W`(기본 1.0), 앵커보다 **위로 간 거리만 가산**하는 편향.

### 커밋
- `2ee0392` 좌우 하드 제약 + 단일 배치 경로(Codex) · `3d2f993` CS0177 수정(Claude) · `d563f96` 허용영역 뷰포트화 + 하단 선호(Claude)
- 계획서 `.plans/plan_notab_callout_side_rule_20260720.md` · 핸드오프 `.plans/HANDOFF.md`(cycle 88)

### env 노브 현황
- `PFS_NOTAB_DIR_<TYPE>_<PIPE|M#>` = **`L`/`R`만 유효**(좌우 강제 오버라이드). 숫자 각도는 deprecated 로그 후 무시.
- `PFS_NOTAB_CALLOUT_ANGLE_W` 기본 **0.0**(좌우가 제약이 되어 역할 중복) · `PFS_NOTAB_CALLOUT_DOWN_W` 기본 1.0

### 다음
- 무탭 全타입(TYPE-001) 확대 검증. GD1/GD2/GD3 외 타입에서 R1~R4가 그대로 성립하는지.
- 실패 시 진단은 `reject(oob/box/extLeader/calloutLeader)` 카운트로 **범위 부족 vs 장애물 과밀** 즉시 구분 가능.

---

## ★★★이관 스레드 상태 (2026-07-20, 다음 세션 시작점)

### ① 진행 중 트랙 + 직전 실측
- **트랙 = 무탭 콜아웃 배치 로직 확립**(cycle72~87). 타입별 좌표 하드코딩을 버리고 **단일 배치 엔진**으로 통일하는 작업. 앵커(어디를 가리키나)만 타입별, **방향·문자·간섭회피는 전역 로직**.
- **배치 엔진 = `NotabCalloutPlacer`**(PFO `SmartPlacementService` 경량 이식). 부채꼴(±각도) 후보 × 반경 스캔 → 충돌검사 → **최소 sprawl 비용** 채택. 2-tier(strict/relaxed).
- **★MLeader 폐기(cycle83)**: 6사이클 동안 "밑줄이 문자 어느 끝에 붙는가"를 제어 못 함. 2채널 자문(**Codex 우선**+Gemini) **만장일치**로 → **MText 독립 + 구형 `Leader` 직접 작도**로 전환. 좌표를 전부 우리가 계산하므로 로그=렌더 일치.
- **작도 형태**: 화살표(앵커) → 대각선 → **밑줄**(문자 폭만큼 수평), 문자는 밑줄 위 `textGap`(3) 띄워 얹음. `Leader` 3정점 = `[anchor, (nearX,baseY), (farX,baseY)]`.
- **직전 실측(로그 09:58, cycle86 빌드통과 후)**:
  - `dim-obstacle count=4/2/4` 등록됨 · `pipe-obstacle skip: paper radius invalid`(미등록)
  - 전 콜아웃 `smart=placed`, `sep=40.5~52.7`(이격 규칙 OK)
  - **GD1 `L-75×75×9` 문자박스가 서포트 내부**(`[314.9,369.7]×[310.7,318.7]` vs 서포트 `[260.5,410.5]×[285,373]`), `cost=0.42`(=radius×0.01, 초과량 0) → 내부 확증
  - GD2 파이프만 `tier=1`(relaxed)

### ② 대기 중 액션
- **cycle87 발행됨(HANDOFF ready), 미집도**. 사용자가 Codex에 `1` → 집도 → `dev_test.bat` → `2`.
- cycle87 집도 5건:
  1. **[치명] 소유자 예외 역작동** — `Free()`에서 `string.Equals(ownerTag ?? "", obstacle.Owner)`가 `""==""`로 **참** → 서포트·치수 장애물이 **부재 콜아웃에서 전량 skip**. cycle85에서 유입. `!string.IsNullOrEmpty(obstacle.Owner) &&` 조건 추가로 교정. **← 그동안 간섭이 안 잡힌 진짜 이유**
  2. 파이프 반경 획득 — `s_isoPipeId`는 원본 DB ObjectId라 디테일 트랜잭션서 조회 실패 → 원본 식별 시점에 **모델 반경 static 저장** 후 배율만 곱함
  3. 바깥 방향 우선 비용 — `cost += |fanOffset| × angleW`(env `PFS_NOTAB_CALLOUT_ANGLE_W`, 기본 0.3)
  4. **방향 지정 env 노브** — `PFS_NOTAB_DIR_<TYPE>_<PIPE|M0|M1>`=각도. 자동(최근접 모서리) 오버라이드. 타입 분기 대신 설정으로 처리
  5. GD3 앵커 기본값 `PFS_NOTAB_GD3_ANCHOR_FX0` `0.25→0.305`(L 앵커 10.7 좌측 이탈 보정, 미완료였던 건)

### ③ 다음 결정 분기
- cycle87 집도 → `dev_test` → **소유자 버그 해소 후 장애물이 처음 실효**되므로 배치가 크게 바뀜. 후보 감소로 `tier=1`/`FAIL` 증가 가능 → REPORT에 `tier/scanned/FAIL` 기록 요청함.
- **사용자 확정 배치 요청**(권장 env 세트로 검증):
  ```
  PFS_NOTAB_DIR_GD1_M0=0        # GD1 L → 우측
  PFS_NOTAB_DIR_GD2_PIPE=0      # GD2 라인넘버 → 우측
  PFS_NOTAB_DIR_GD2_M1=-135     # GD2 C → 좌하단
  PFS_NOTAB_DIR_GD3_PIPE=-135   # GD3 라인넘버 → 좌하단
  PFS_NOTAB_DIR_GD3_M0=-45      # GD3 L → 우하단
  ```
  GD3는 자동 sprawl로는 **둘 다 우하단**이 되어 충돌하므로 env 지정 필수(좌하단은 minX 밖 125 초과, 우하단은 초과 0).
- 어긋나면 **좌표 하드코딩 금지** — `PFS_NOTAB_DIR_*`·`ANGLE_W` 조정. 그래도 안 되면 `out/fan/cost/scanned/dirSrc` 실측 첨부해 재설계.
- 이후 잔여: 나머지 타입(SHOE/RS/TR/FS…) 확장, config `MemberBIs` 완전 제거(BOM 검증 후).

### ④ 사용자 확정 배치 규칙 (전 타입 공통, 불변)
1. **방향** = 최근접 서포트 모서리 밖으로 **대각**(빈 공간 우선) / 필요 시 env 지정
2. **문자-화살표** = 문자는 **항상 화살표 반대쪽으로만** 뻗음. 4사분면 전수:
   - 문자가 앵커 **우측** → **시작점**(좌측 변) ≥ `anchorX + 40`
   - 문자가 앵커 **좌측** → **끝점**(우측 변) ≤ `anchorX − 40`
   - **상/하는 무관**, 좌우로만 결정
3. **최소 수평이격 40**(`PFS_NOTAB_CALLOUT_MIN_DX`)
4. **문자-밑줄 간격 3**(`PFS_NOTAB_CALLOUT_TEXT_GAP`)
5. **간섭 금지**: 리더 X자 교차, 리더의 타 문자 관통, 부재·치수문자 겹침
6. **장애물** = 활성 서포트 부재 + **치수(Dimension extents)** + **파이프**. 소유자 태그로 자기 파이프만 예외
7. **앵커** = 타입별(부재 개별 solid 추출 불가 → 비율 + env 노브)
8. 치수: `Dimtad=1`(Above), `Dimdec=1`, `Dimexe=5`, 숫자 `0.#`(정수는 소수점 숨김)

### ⑤ 이번 세션 확정된 실측 사실 (재조사 금지)
- **서포트는 통짜 Solid3d 1개로 병합**되어 있고 **파이프는 별도 solid**(cycle79 member-spike). → **부재 바 개별 장애물/앵커 자동산출 불가**. 이 때문에 "부재별 모델링" 대형 재작성을 **스파이크로 사전 폐기**함.
- **MLeader 제어 불가**(cycle79~82 실측): `content.Location` 무시 / `SetTextAttachmentType` 양방향 호출해도 **렌더 바이트 동일** / `TextLocation` 이동해도 **연결 끝단 불변**(생성 시점 고정). → 직접 작도가 유일 해법.
- BOM 정상: `BeamProfile` 딕셔너리 역인덱스로 BOM 부재코드↔BI 브리지(cycle73~74). GD3 앵글 A7 자동 복원됨.

### ⑥ 규칙 변경 (글로벌 반영 완료)
- **자문 우선순위 = Codex 우선, Gemini 보조**. `C:\Users\HT노승환\.claude\CLAUDE.md` §1·§5·§9 반영(2026-07-20). Gemini 단독 자문 금지, 결론 상충 시 Codex 채택+근거 기록, Codex 자문은 `sandbox: read-only`/`approval-policy: never`로 텍스트만.

### ⑦ 관련 파일·경로 (2026-07-20)
- 핸드오프 `.plans/HANDOFF.md`(**cycle87 ready**) / 결과 `.plans/REPORT.md`(cycle86 done)
- **진단 원장 `.plans/plan_notab_callout_placement_diag_20260719.md`** — 배치 근본원인·아키텍처 결정(Q1~Q3)·실현성 판정 전문
- 진행표 `.plans/notab_alltype_progress.md`(GD1~GD3 종결 표기)
- 코드 `PlantFlow_Support/Core/Commands.cs`:
  - `NotabCalloutPlacer`(파일 상단 ~14-135): `TryPlace`(부채꼴+비용) / `Free`(**소유자 버그 위치**) / `Commit`(2세그먼트 등록) / `AddObstacle(box, owner)` / `SetCostReference`
  - 콜아웃 작도: `AppendNotabProfileCallout`(부재) · `AppendNotabPipeCallout`(파이프) — 둘 다 MText+`Leader` 직접 작도
  - 치수: `AppendNotabPaperDimensions`(치수→장애물 등록→콜아웃 순서) · `AddNotabDimensionObstacles` · `AddNotabPipeObstacle`
  - 앵커 분기: `gd2Two`/`gd3Two`(≈5030~5060, `PFS_NOTAB_GD*_ANCHOR_FX*`)
  - BOM: `AugmentDesignationsFromBom` · `HANTEC.BeamProfileBI`(역인덱스)
- **최근 커밋**: cycle83 `c87c907`(직접작도) · cycle84 `6418e79`(치수장애물) · cycle85 `e27d866`(2세그먼트, 빌드실패) · cycle86 `a30dc34`(빌드교정)
- 로그 `C:\Temp\pfs_diag.log`. 핵심 키: `callout-draw`(anchor/nearX/farX/baseY/W/H/side/sep/cost/scanned) · `dim-obstacle` · `pipe-obstacle` · `member-spike` · `render-check`
- **프로세스 교훈**: ①라이브 핑퐁 3회 초과 시 즉시 측정 스파이크로 전환(대형 오설계 방지 실증) ②로그 숫자만으로 "정상" 판정 금지 — **렌더와 대조** 필수(판정기준 오류로 GD2 L-65 이격 0.03을 통과로 오판) ③집도 중 HANDOFF 덮어쓰기=요구 누락

---
## (이전) 이관 스레드 상태 (2026-07-18)

## ★★이관 스레드 상태 (2026-07-18, 다음 세션 시작점)
### ① 진행 중 트랙 + 직전 실측
- **트랙 = 무탭 서포트 detail 타입별 매핑**(N3 치수/콜아웃 확장). 규격집([[pfs-hantec-support-standard-catalog]]) 타입을 각 `TYPE-001`로 3D 배치→추출→4관찰로 패밀리별 로직 확정([[pfs-notab-alltype-test-model]]).
- **아키텍처(cycle63)**: 타입판정=`GetNotabStandardName()`(**ShortDescription**=`s_isoShortDesc`, 사용자태그 아님) → `NotabTypeConfig`(VerticalMode/PipeCalloutSide/HorizontalSide/**MemberBIs**) 단일표 `GetNotabTypeConfig(std)`. 타입 추가=표 한 줄.
- **현 config 행**: GD1{fheight,top,bottom}·RC1{fheight,top,bottom}·GD2{**pipecenter**,top,auto,MemberBIs["16","215"]}·GD3{full,bottom,auto}·default{fheight,top,auto}.
- **직전 실측(로그 16:2x)**: GD1 rowCount 정상·std=GD1. GD2 std=GD2 세로 pipecenter(바닥~배관중심=300, vScale=0.2). GD3 std=GD3 full=274. **BOM 스파이크 성공**: GD1[ANGLE A7], GD2[CHANNEL C15×2+ANGLE A6×2] (BI 없어도 정상!), GD3[C10×2+**A7×2**]. 6컬럼=`마크|카테고리|사이즈|재질|길이|여유`, 마크 F1~F4=HANTEC TaggingPoints 키.

### ② 대기 중 액션
- **cycle71 발행됨(HANDOFF ready), 미집도**. = PFSNOTABTEST 콤마 split(GD1→GD2→GD3 순차 자동추출). cycle70에 C로 넣었으나 집도 타이밍상 누락→분리 재발행. 사용자 Codex `1`→집도→dev_test 실행→`2`.
- **cycle70(945403b) 집도됨-라이브 미검증**: A(GD2 세로=배관중심300 좌측)✅ B(멀티부재 텍스트 좌우발산)✅ 정적PASS. 라이브서 GD2 세로300·L좌C우 발산 확인 필요.

### ③ 다음 결정 분기
- cycle71 집도+dev_test 3타입 연속추출 → cycle70 A/B 라이브 검증 → **GD2 미결**: 앵커 화살표 정밀조준(현 fx=0.25/0.75는 근사, env/config 노브화 제안)·가로 A/A2 치수.
- **★큰 방향 전환 후보**: 부재 콜아웃 소스를 config MemberBIs(하드코딩 우회책) → **BOMs 행**으로. 근거=BOM 스파이크가 全타입 부재를 정확히 앎(GD3 A7 누락 등 config 불완전 노출). BOMs.HANTECContents→FrameBOM/AttaBOM 재사용=[[pfs-hantec-annotation-engine]].
- **BOM 트랙 착수 시점**: GD2 완결(부재+세로) 직후. 스파이크 통과=데이터파이프 무탭서 정상. 다음=BOM 테이블 렌더 설계→밸룬(BOM행↔F1~4 TaggingPoints).

### ④ 이번 세션 해결한 오염/소실 트랙 (완료)
- cycle64~66: 추출 지오메트리 오염. **원인=선택영역 과대**(사용자 통찰). ①이웃서포트=`AutoIncludeRelatedParts` isSup를 probe(±150)→**supContactBox(anchor±50)** 접촉박스(cycle65, RC1 배제) ②GD1 파이프소실=축투영이 9.6m 먼 세그먼트 오선택→**pipeReachBox(anchor±300) 세그먼트 근접게이트**(cycle66). 태그필터는 폐기하고 영역박스 철학으로 통일.

### 관련 파일·경로 (2026-07-18)
- 핸드오프: `.plans/HANDOFF.md`(cycle71 ready), `.plans/REPORT.md`(cycle70 done). 진행표: `.plans/notab_alltype_progress.md`(타입별 4관찰 기입표, GD1✅·GD2진행·GD3진행).
- 코드 `PlantFlow_Support/Core/Commands.cs`: 타입판정=`GetNotabStandardName`/`GetNotabTypeConfig`(~4936). 치수=`AppendNotabPaperDimensions`(dimV 모드분기 fheight/full/pipecenter). 부재콜아웃=`AppendNotabProfileCallout`(멀티=fx분산+좌우발산)/`ApplyNotabCalloutNearEdgeAttachment`(방향 파라미터). PLN콜아웃=`AppendNotabPipeCallout`. 선택=`AutoIncludeRelatedParts`(supContactBox/pipeReachBox). BOM스파이크=`NotabBomSpike`. 프로파일=`CaptureIsoSupportProfile`+`CaptureIsoSupportProfileFromConfig`+`TryBuildNotabSupportDesignation`.
- BOMs=`Models/BOMs.cs`(생성자 id+attachments, `ContentsByDesignStd("HANTEC")`→FrameBOM/AttaBOM). HANTEC=`Ortho/HANTEC.cs`(DetailProfile·타입별 TaggingPoints 엔진).
- dev_test.bat env: `PFS_NOTAB_TEST_TAG=GD1-001,GD2-001,GD3-001`(cycle71 콤마순회 필요), `PFS_NOTAB_BOM_SPIKE=1`, `PFS_NOTAB_DIM_TXT=8`, `PFS_NOTAB_PIPE_CALLOUT_DX=180`, `PFS_NOTAB_MEMBER_CALLOUT_DX=5`. 위치노브=`PFS_NOTAB_*_CALLOUT_DX/DY`, 영역=`PFS_NOTAB_SUPPORT_TOL`(50)/`PFS_NOTAB_PIPE_REACH`(300)/`MARGIN`(150). 로그=`C:\Temp\pfs_diag.log`.
- **프로세스 교훈**: 집도 중 HANDOFF.md 덮어쓰기=요구 누락 유발(cycle70 C). 추가요구는 다음 사이클로 분리.

---
## (이전) 이관 스레드 상태 (2026-07-16)

## ★이관 스레드 상태 (2026-07-16, 다음 세션 시작점)
### ① 진행 중 트랙 + 직전 실측
- **트랙 = N3 치수 정련**(핵심 제도는 완성 cycle47~49, 지금은 표기 다듬기). 무탭 엔진 = 서포트 detail 자동생성(와이어프레임 뷰포트+클립+held-pipe BOP선택+페이퍼 비연관 치수). **다중선택 배치 = `PFSNOTABBATCH`(cycle50) 완성**.
- **직전 실측(cycle49 라이브)**: 가로 치수 `split=(350,100) side=bottom`, 세로 176, 배관중심 분할·상/하단 배치 PASS. 설정=글자2.5/화살표4/오프셋7.5/적층6.25.
- **BI 부재규격 규명(이번 세션 핵심)**: 서포트 속성 `BI="BPn+BF"`(BPn 1L/2C/3H/4FB + BF). `BI=17`→BPn1(앵글)+BF7→`SHAPE.py profile[7]={F:75,T:9}`=`HANTEC.DetailProfile("17")="75x75x9"` 일치. 즉 앵글 75×75×9(사용자말 "70"은 근사). 읽기=`PSUtil.GetSupportDimension(id)→SupportParams["BI"]`. 멤버 정의=`PlantFlow_Support\Support\member\MEMBER.py`+`library\SHAPE.py`.

### ② 대기 중 액션 (무엇의 결과를 기다리나)
- **cycle51 발행됨(HANDOFF `f1abe34`), 미집도**. 사용자가 Codex에 `1` 입력 → 집도 → `2`로 나에게 통보 → 내가 리뷰·빌드검증.
- 사용자가 **全 서포트 타입 `PFSNOTABBATCH` 테스트 후, 가로 치수 상/하단을 타입별로 어떻게 원하는지 데이터 제공 예정** → (1) per-type 테이블 채움.

### ③ cycle51 요구 (미집도 내용)
- **(2a) 세로 치수 텍스트=앵글 F높이 `75`**(DetailProfile 첫숫자 파싱, 176=앵글+유볼트+배관 합산이라 무의미→대체).
- **(2b) 별도 지시선(MLeader) 콜아웃 `L-75×75×9`** 부재 옆(=`PSUtil.CreateMLeader`+`EnsureMLeaderStyles` 재사용). 높이(치수)와 규격(콜아웃) 분리.
- **설정**: 글자10/화살표10(Dimasz txt×1.6 분리)/오프셋15/적층15.
- **(1) 가로 위치 타입별 스캐폴드**: `GetNotabHorizontalDimSide(type)` 테이블(GD1·RC1=하단, 미등록=현 배관근접). type=SupportName의 `-`앞 prefix.

### ④ 다음 결정 분기
- cycle51 집도 PASS(세로75+콜아웃+설정+가로스캐폴드) → 사용자 全타입 테스트 → 타입별 가로위치 데이터 수령 → (1) 테이블 완성 → **N3 종결**.
- cycle51 FAIL(콜아웃 위치/MLeader 이슈) → 로그로 앵커/스타일 교정.
- N3 종결 후 → **N4(밸룬/라인번호·BOP 콜아웃/BOM = AnnotateViewport·SPInfo.AttachmentList 재사용)** → N5(3부채 코드 소멸).

### 관련 파일·경로
- 계획: `<appDataDir>\scratch\plan_pfs_notab_n3_20260714.md`(★갱신 섹션), `plan_pfs_notab_viewport_review_20260716.md`.
- 핸드오프: `.plans/HANDOFF.md`(cycle51 ready), `.plans/REPORT.md`(cycle50 done).
- 코드: `PlantFlow_Support/Core/Commands.cs` — 치수=`AppendNotabPaperDimensions`(~4419)/`AppendNotabPaperDimensionEntity`(~4507)/`ApplyNotabPaperDimensionOverrides`(~4519), 투영=`NotabProjectWcsToPaper`, 배치=`PFSNOTABBATCH`. BI=`PSUtil.GetSupportDimension`, `HANTEC.DetailProfile`(Ortho/HANTEC.cs:1894), MLeader=`PSUtil.CreateMLeader`/`OrthoViewportManager.EnsureMLeaderStyles`(662).
- 로그: `C:\Temp\pfs_diag.log`(런마커 `RUN START`). dev: `dev_test.bat`(태그 env `PFS_NOTAB_TEST_TAG`, 없으면 GD1-001; 또는 ACAD서 `PFSNOTABDETAIL`/`PFSNOTABBATCH` 수동선택). env 조정: `PFS_NOTAB_TARGET_FILL`(0.4)/`DIM_TXT`/`DIM_OFFSET`/`CLIP_MARGIN`/`BOP_TOL`/`CONTACT_TOL`/`USE_HIDDEN`.
- ⚠️ 미푸시 커밋 2개(cycle51 handoff). 작업트리에 무관 삭제(D PDF)·untracked 노이즈 있음(스테이징 안 함).

---

_이전 갱신: 2026-07-16 (★N3 치수 핵심 완성: 스케일표준화·투영·치수제도·배관참조. 사용자 全타입 테스트 중. 다음=N4)_

## ★★ N3 무탭 치수 핵심 완성 (2026-07-16, 커밋 cfa1fb7~9576258 = cycle 47~49 + 9893b5b) — 라이브 PASS
페이퍼공간에 서포트 치수를 비연관으로 직접 제도. 사용자가 이제 **全 서포트 타입 테스트 예정**. 다음 트랙 = **N4(밸룬/라인번호·BOP 콜아웃/BOM)**.
- **N3-0 스케일 표준화(cfa1fb7, 9893b5b)**: 동적 피팅→표준배율 라운딩(1:1/2/5/10…) + 주석여백. `PFS_NOTAB_TARGET_FILL`(기본0.4=프레임~여백충분). `vp.CustomScale` 명시(투영 정합). RC1=1:5, GD1=1:2. 사용자 "스케일 과대" 해소.
- **N3-a 투영(cfa1fb7)**: `ViewportProjection`(Ortho, private sealed) 로직 이식 `NotabProjectWcsToPaper`=WCS→paper. 검증: support-paper 중심=vpCenter, 크기=실측×배율. 유효스케일=`vp.Height/vp.ViewHeight`.
- **N3-b/c 치수 제도(dbb87bf)**: `AppendIsoBoundingDimensions` 구조 재사용, 좌표원천=투영 페이퍼. 가로 총폭·pipeCenter 분할·세로 높이, 텍스트=실측mm override. **★크기=페이퍼 고정(Dimtxt 2.5mm, `PFS_NOTAB_DIM_TXT`)** — 기존 ComputeIsoDimensionSize(real/12≈50mm)는 페이퍼 과대라 회피. Title Block 레이아웃 append, `EnsureIsoAnnotationResources`.
- **배관 참조 규칙(9576258)**: (1) 가로 분할=**실제 배관중심 X**(서포트중심 아님. `s_isoPipeCenterWcs`=held-pipe p0 전역 캡처→투영). (2) 가로 치수를 **배관 근접 쪽 상/하단** 배치(pipeCenterY<centerY→하단). 세로는 좌측 유지. 라이브 `split=(350,100) side=bottom` 정답. 분할 경계 가드(한쪽<최소→총폭만). 타입 하드코딩 아닌 기하 규칙.
- **핵심 교훈**: 치수 배치는 타입별 하드코딩 대신 **배관 위치 기하 규칙**(중심 분할·근접 쪽 배치)이 보편. 페이퍼 치수는 뷰포트 스케일 무관 **고정 mm 크기**(Dimscale=1). 비연관=정의점 스냅샷(detailDb frozen). [[pfs-support-held-pipe-bop]]
- **잔여(N4/후속)**: 밸룬·라인번호/BOP 콜아웃·BOM(기존 AnnotateViewport·SPInfo.AttachmentList 재사용). 全타입 테스트서 나오는 엣지(배관 없는 서포트·특이 형상) 대응.

---

_이전 갱신: 2026-07-16 (무탭 뷰포트 품질 트랙 완결: 와이어프레임·클립·held-pipe 선택)_

## ★ 무탭 뷰포트 품질 트랙 완결 (2026-07-16, 커밋 999e1f9~b0e5c71 = cycle 43~46) — 라이브 PASS
추출물 품질 리뷰 3건(비주얼스타일·영역클립·초점)에서 출발해 아래 순차 완결. 다음 트랙 = **N3(치수)**.
- **와이어프레임 기본화(Phase1, 999e1f9)**: 모델영역 Hidden→와이어프레임 기본. Hidden은 opt-in `PFS_NOTAB_USE_HIDDEN=1`. 육안 채택.
- **서포트 영역 클립(cycle43, f5473d8)**: 참조 OrthoGen 3중클립(쿼리박스+앞뒤클립+클립솔리드) 등가 → **oriented 클립박스 Solid3d + Boolean INTERSECT**로 관통 파이프 **길이 트림(10m→서포트깊이)** + 뷰포트 **동적 피팅**(고정1:4 폐기, `ComputeNotabViewportFit`). §9 반영(clone-per-op·empty drop·접촉 선필터).
- **held-pipe 선택(cycle44~46)**: 서포트가 실제 잡는 배관만 포함. cycle44(중심거리)→cycle45(서포트 bbox 수직평면 투영 rect 포함)→**cycle46 최종=BOP 표고 매칭**. 같은 라인 평행 배관(수직200mm)은 rect·중심거리로 못 갈려 오선택 → **서포트 `BOP`(배관 밑면표고)로 판정**: `|(pipeCenterZ−외경반경)−BOP|` 최소. 라이브 `reason=bop bopErr=0.25 pipeCenterZ=467.7 bop=423` 정답 선택. BOP 없으면 기하 최근접 폴백+경고. env `PFS_NOTAB_BOP_TOL`(10mm)/`PFS_NOTAB_CONTACT_TOL`.
- **핵심 교훈**: "서포트가 어느 배관을 잡나"는 기하 중심추측으로 불가(부착위치=새들/윗면이 중심 아님) → **Plant BOP 데이터가 결정적**. [[pfs-notab-engine-go]]
- **⚠️ 미결(N3 입력)**: 동적 피팅 스케일이 현재 **~1:1.4(비표준·과대)** → 서포트가 프레임 87% 차지. 치수선·라인번호/BOP 콜아웃 공간 부족. **N3에서 표준스케일 라운딩+주석 여백 함께 설계**(주석 footprint가 필요여백을 결정).

---

_이전 갱신: 2026-07-16 (무탭 접촉판정 마진 + perspective 리본 flip 가드 완결)_

## ★ 무탭 결함 2건 해결 (2026-07-16, 커밋 73b9974·6e01a9b) — 라이브 PASS
N3 착수 전 라이브 테스트 중 발견된 2건 종결. 다음 트랙 = **N3(치수)** 유지.
- **결함A — 자동포함 이웃 묻어남**: `AutoIncludeRelatedParts` 마진이 `0.5×서포트 max치수` 비례라 큰 서포트에서 과대(1977mm)→파이프 축 따라 이웃 서포트 쓸어담음. **해결=접촉 판정 고정 tol 150mm**(env `PFS_NOTAB_MARGIN`). 라이브 `margin=150 addedPipe=1 addedSupport=1` 확인. 커밋 73b9974.
- **결함B — 추출 후 원본 뷰 perspective flip**: 계측(`PFSPERSPWATCH` 스택)으로 **근본원인=AutoCAD 리본 컨트롤 WPF 바인딩**(`RibbonListButton.set_Current→BindingExpression.UpdateSource→SystemVariable.set_Value`)이 파이프라인 그래픽스 갱신 후 유휴 시점에 `PERSPECTIVE=1` 역기입. **우리 코드/LISP/도면 저장뷰 무관**(계측 `persp enter=0 exit=0`). 리본 내부는 수정 불가 → **스코프 제한 방어 가드**(cycle 42, Codex 커밋 6e01a9b): 파이프라인 진입 시 8초 창 무장→창 내 flip 감지 시 1회 복원+Idle one-shot REGEN+즉시 disarm. 재진입/이중구독/핑퐁 방어. 라이브 PASS(`guard 교정 1→0`+Regen 완료, 육안 parallel 유지).
  - 진단 자산(잔존, 무해): `PFSPERSPPROBE`(즉석 프로브), `PFSPERSPWATCH`(실시간 감시+스택). `pfs_dev_start.scr`에 WATCH/PROBE 연결됨. env `PFS_PERSP_GUARD_SEC`로 창 조정. 커밋 37f7c72·ca80c0f·6ad9e06.
  - ⚠️ 잠재 영향: perspective 잔존 시 VIEWBASE/-VPOINT 오쏘 경로가 어긋날 여지(무탭 추출 자체는 뷰 무관·무영향). 가드로 증상 제거됨.

---

_이전 갱신: 2026-07-14 (무탭 레이아웃·자동포함·자동화 완결, 다음=N3 치수)_

## ★ 무탭 레이아웃/워크플로/자동화 완결 (2026-07-14, 커밋 ~4fcf1b1)
이 세션에서 RECOVER 해결(H5) 이후 아래 전부 라이브 PASS. 다음 트랙 = **N3(치수)**.
- **RECOVER 해결(H5)**: `sourceDb`(side clone DB) 미Dispose가 detailDb.SaveAs 오염 → `CopyCleanNotabSolids` 직후·SaveAs 전 Dispose(ref 시그니처). 포럼 근거. cycle30~36 detailDb 내부 가설(H0~H4) 전부 기각.
- **#1 페이퍼 초기화면**: 헤드리스 TileMode=크래시 → **저장 후 reopen** 방식(`new Database(false,true)`+`TileMode=false`+`SaveAs(...SecurityParameters)`, WorkingDatabase 미변경). 포럼 3종 근거(생성자/SecurityParameters/TileMode). ※빌드DB TileMode·활성레이아웃 전환은 네이티브 크래시.
- **뷰포트 도면영역 정합**: ID 실측 LL(30.5,84.5)~UR(640.5,573.5)=610×489, center(335.5,329). scale 1:4. target=서포트중심(supportExt).
- **서포트만 선택→파이프+유볼트 자동포함**: `AutoIncludeRelatedParts`(서포트 bbox+margin150 교차 Pipe/Support 추가). 유볼트=AcPpDb3dSupport.
- **초기화면 zoom extents**: reopen에서 오버올 뷰포트(활성화前 Number=-1이라 dims로 판별) 뷰를 페이퍼 extents로.
- **★dev 완전 자동화**: dev_test.bat이 로그 초기화 후 런치 → `pfs_dev_start.scr`=SECURELOAD0(보안다이얼로그 제거)+NETLOAD+`PFSNOTABTEST`(SupportName 태그로 서포트 자동선택→추출). Claude가 `C:\Temp\pfs_diag.log` 직접 Read(복붙 불요). [[pfs-dev-loop-tier1]]

### 다음 세션 시작점 (N3 = 치수, 최대 리스크)
- **진행 트랙**: 무탭 엔진. 레이아웃/워크플로/자동화 전부 완결(위). 남은 것 = N3(치수)→N4(밸룬/BOP/BOM)→N5(3부채 코드 소멸).
- **직전 실측**: `PFSNOTABTEST`(GD1-001) 원클릭 라이브 = auto-include(파이프+유볼트)·뷰포트 610×489·zoom·페이퍼열림·RECOVER0·크래시0 전부 PASS. 최신 커밋 613ae6f.
- **대기 액션**: N3 착수 미개시. 계획 초안 `<appDataDir>\scratch\plan_pfs_notab_n3_20260714.md` 존재. 최대 리스크(§9 지목=DCS→PSDCS 매핑 정밀도)라 계획→§9 자문→핸드오프 권장.
- **N3 핵심**: 평면화 2D블록 없이 **뷰포트 투영(DCS→PSDCS 행렬)으로 치수 배치좌표·pipeCenter 계산** → Main에 비연관(non-associative) 치수 직접 제도(가로 폭 분할+세로 높이, 텍스트=실측 mm). 재사용=`s_isoRealWidth/Height`, basis right/up, `PSUtil.CreateHorizontal/VerticalDimension`, B4c/d/e 분할 로직.
- **N3 검증 이점**: PFSNOTABTEST 원클릭 + Claude가 pfs_diag.log 직접 read로 매핑 정밀도 빠른 반복 가능.
- **관련 경로**: 코드=PlantFlow_Support/Core/Commands.cs(RunNotabDetailPipeline/CreateNotabDetailViewport/ConfigureNotabDetailViewport/TryReopenSetNotabPaperSpace/AutoIncludeRelatedParts/PFSNOTABTEST). 계획=plan_pfs_notab_n3_20260714.md·plan_pfs_notab_layout_fit_20260714.md·plan_pfs_notab_recover_20260714.md. 로그=C:\Temp\pfs_diag.log(런마커 `===== RUN START =====`).

---

_이전 갱신: 2026-07-14 (★무탭 엔진 Main 라이브 PASS)_

## ★★★ 무탭 엔진 Main — 라이브 PASS (2026-07-14, cycle 30 커밋 5efbf40)
- **판정**: `PFSNOTABDETAIL` 라이브 실행 → `Details\GD1-001_notab.dwg` 열람 확인.
  - Title Block 레이아웃: **A1 프레임 + 타이틀블록 정상**, 뷰포트 안 **파이프 원 + 서포트 직사각형이 2D 은선제거 투영**으로 배치. = 설계 북극성 Main 실물 실증.
  - 모델 공간: 원본 3D 솔리드(소스, 정상). 열기 시 손상/누락 없음.
- **의미**: 문서 0(무탭) 파이프라인으로 VIEWBASE/EXPORTLAYOUT/평면화 **3부채 없이 Main 정투영 생성 라이브 증명**. side-DB(평범 DB) Solid3d + Hidden 뷰포트 방식 확정.
- **RECOVER = ★해결 완료(2026-07-14, cycle 37-H5, 커밋 d7897e5)**: 근인=누수 side Database. `NotabDetailCommand`의 `sourceDb`(side clone)가 `detailDb.SaveAs` 시점까지 미Dispose(finally라 저장 후)→다른 DB 저장 오염=RECOVER. 해결=`CopyCleanNotabSolids` 직후·SaveAs 전 `sourceDb.Dispose()`(ref 시그니처). 라이브 PASS=경고+크래시 없음. 근거=Autodesk 포럼 accepted solution(사용자 발견). cycle 30~36 detailDb 내부 가설(H0~H4) 전부 기각됐던 이유=근인이 외부 누수DB라 keyscan 미검출.
  - ★크래시 주의: 헤드리스 side-DB `TileMode=0`+활성레이아웃(cycle37 D)=네이티브 크래시→원복(cycle 38). 페이퍼 초기화면(#1)은 안전방법 후속.
- **레이아웃 정합 완료(cycle 37 A/B/C, 커밋 c432ae6)**: 뷰포트 fixedRect LL(30.5,84.5)·640.5×573.5·center(350.75,371.25), target=서포트중심(supportExt), scale=1:4.
- **cycle 30 집도 내역**(커밋 5efbf40, `Core/Commands.cs`): A1 hard 좌표 `(0,0)~(841,594)`(`source=A1-hard`), `PlotSettingsValidator` A1 media 적용, 타이틀블록 clone 정규화(normalized=0), audit reflection(사장).
- **다음**: N3(치수 3D→PSDCS 비연관, 최대리스크) → N4(밸룬/BOP/BOM=기존 AnnotateViewport 재사용) → N5(3부채 코드 소멸) + RECOVER polish.

---

_이전 갱신: 2026-07-14 (무탭 엔진 크래시 해결·세션 이관)_

## ★세션 요약 (2026-07-13~14) — 격리 치수 완성 → 별도 도면 → 무탭 엔진
### 1. 격리 치수/중복 (완료, 라이브 PASS)
- **B4d**(f6c99fa): 가로 치수 파이프중심 분할(100/200)+전체(300). **B4c**(f02ee1b): 세로 치수 서포트만. **B4e**(cf44403): 재실행 중복 제거(`PurgePriorIsoDetail`).
### 2. 별도 도면(태그별 .dwg+타이틀블록) — ✅ 라이브 완성
- `CreateIsoDetailDrawing`: side-DB tagDb(템플릿 .dwt)→2D+치수 클론→플로팅 뷰포트(1:2 고정, Layout.Limits 기반)→DRAWING_TITLE 속성(SUPPORT_CODE=GD1-001 등)→`Details\<tag>.dwg`. 원본 무변경. 커밋 6ded95e~c841ad0.
- 태그 소스=서포트 `SupportName`/`Tag`(=GD1-001), PIPE_NO=LineNumberTag, STD=DesignStd. (PFSISOTBLPROBE로 실측 확정)
### 3. ★무탭 엔진 — GO 확정 + 크래시 해결 (이번 세션 핵심)
- **동기**: 기존 파이프라인 3부채=뷰큐브 미표시·EXPORTLAYOUT 다이얼로그·temp dwg 누적. 근본=temp 문서 MDI 활성화 + VIEWBASE/EXPORTLAYOUT 명령.
- **스파이크(C:\Lisp\NoTabSpike)**: ①접근C=side-DB Solid3d+Hidden 뷰포트로 은선제거 정투영 실증(VIEWBASE/EXPORTLAYOUT 불요) ②스파이크D=side-DB에서 Plant `entity.Explode()` 작동(문서없이 Solid3d 추출) → **완전 무탭 GO**. 메모리 [[pfs-notab-engine-go]].
- **엔진 구현**: N1(PFSNOTABN1, 커밋 73861e6)=side-DB 재귀 explode→Solid3d. N2(PFSNOTABDETAIL)=solid+Hidden 뷰포트+타이틀블록→`Details\<tag>_notab.dwg`.
- **★N2 commit 크래시 진단·해결(cycle 22~29)**: 근본원인=**".dwt 템플릿 DB에 은선/3D 뷰포트(ViewDirection/Hidden/ShadePlot) commit"** = 네이티브 크래시. (진단 격리: 템플릿+박스=OK, 템플릿+평면뷰포트=OK, 평범DB+Hidden뷰포트=OK[스파이크C], 오직 템플릿+3D뷰포트만 크래시. Plant solid/비주얼스타일명 무죄.)
  - **해결=§9 #3(커밋 703f1ee)**: 뷰포트를 템플릿이 아닌 **평범 DB(`new Database(true,true)`)** 에 생성 + 템플릿 타이틀블록 2D만 WblockClone 병합 → **크래시 소멸**(commit 완료+saved).
### 대기 중 액션 (다음 세션 시작점)
- **cycle 30(HANDOFF.md 발행됨) 라이브 대기**: A1 레이아웃/플롯 정합(현재 평범DB 기본 12×9라 타이틀블록 A1좌표와 미스매치) + 저장 무결성("run RECOVER" 경고 제거, Audit+dangling 의존 교정). Codex 집도(`1`)→빌드→`PFSNOTABDETAIL` 라이브.
### 다음 결정 분기
- cycle 30 PASS(A1 정합+경고소멸) → **무탭 Main 완성**(3부채 소멸) → **N3(치수 재투영: 3D→PSDCS 비연관 치수, 최대리스크)** → N4(밸룬/BOP/PLN 콜아웃/BOM=기존 AnnotateViewport·SPInfo.AttachmentList 재사용) → N5(정리·전환: VIEWBASE/EXPORTLAYOUT/temp 제거).
- cycle 30 FAIL → 로그로 A1/저장 원인 재판정.
### 관련 경로
- 계획: `<appDataDir>\scratch\plan_pfs_notab_engine_20260713.md`·`plan_pfs_notab_spike_20260713.md`·`plan_pfs_iso_separate_drawing_20260713.md`·`plan_pfs_iso_annotation_integration_20260713.md`.
- 핸드오프: `.plans/HANDOFF.md`(cycle 30 ready). 코드: PlantFlow_Support/Core/Commands.cs (PFSNOTABDETAIL/N1/CreateNotabDetailDrawing/CopyCleanNotabSolids/CloneTemplateTitleBlock2D).
- 스파이크: C:\Lisp\NoTabSpike (PFSNOTABSPIKE/PFSNOTABEXPLODE). 진단 커맨드(제품): PFSNOTABBOXTEST/BOXVPTEST/BOXVPWIRE(격리 완료, 잔존 무해).
### ⚠️ 이관 주의
- 커밋 규율: Codex가 git commit escalation 거부 다발 → **Claude가 대리 커밋** 중(코드 검증 후). 현 HEAD=703f1ee(+cycle30 대기).
- 밸룬 멤버 소스=`SPInfo.AttachmentList`(SupportType"ATTACHMENT" 부착서포트, PaletteTab.Events.cs:171-319). 격리 선택에 부착서포트 포함+SPInfo 채우기 필요(N4).
- 밸룬/BOP콜아웃/BOM은 기존 `OrthoViewportManager.AnnotateViewport` 엔진 재사용(새로 안 만듦).
- (이전) **격리 방향 제어 — ✅ B3a PASS (2026-07-12)**.
  - 파이프라인: 선택셋 → 1st temp(Plant clone) → explode Solid3d → 2nd temp(순수 solid) → **-VPOINT(파이프축)** → SendStringToExecute VIEWBASE `_O _Current` → EXPORTLAYOUT 평면화 → 원본 clone-back(PFS_ISO_DETAIL 레이어).
  - **방향 제어(B3a)**: -VPOINT(파이프축)+VIEWBASE `_O _Current`로 Main뷰(파이프=원+서포트=직사각형) 생성. X/Y/수직Z 전 축 + 다중 타입 육안 확정. "축방향 응시=Main" 보편 적용.
  - 커밋: e7ffa14(B1g), 7a7653d(B2a), cc6602e(B2b), cbe738c(B3a).
  - 후속(별도 트랙): 스케일 보정(viewport 1:100→실크기), 치수(평면화 2D 후처리 Main만), 배치 오프셋, -0 표기 정규화(경미), EXPORTLAYOUT open-prompt 억제(세션당 1회, 낮음).
- (이전) **격리 VIEWBASE 커널 — ✅ B1g PASS**, **B2b clone-back PASS**.
  - 목표 출력: 4뷰(DOWN/WEST=Main/SOUTH/ISO) + 치수는 Main에만 + BOM표 + 밸룬 + 위치콜아웃 + 타이틀블록 (caetech.vn 사양).
  - 설계철학: 뷰 = Pipe Axis 기준. Main = 서포트 길이방향(탑 제외), viewDir∥파이프 → 서포트 직사각형 + 파이프 원. 치수는 Main에만.

## 직전 실측 사실 (2026-07-12) — 격리 VIEWBASE 커널 완성
- **B1g ✅ PASS**: 격리 파이프라인 3회 결정적 재현. `PFSVBISODONE newEntities=4`(base+projected), eInvalidInput 소멸.
- 전 파이프라인: 선택셋 → 1st temp WblockClone(Plant 지오 보존, proxy=0) → explode Solid3d id수집 → 2nd temp WblockClone(순수 solid만) → Idle 게이트(active/CMDNAMES) → **SendStringToExecute VIEWBASE _E** → sentinel(PFSVBISODONE) 완료판정 → Idle close.
- ★핵심 교훈: VIEWBASE(무거운 model-doc UI 명령)는 갓 Open한 문서서 `ed.Command`(직접호출) 불가 → **`SendStringToExecute`(명령 큐=타이핑 등가) + sentinel 명령 체이닝**으로 구동/완료판정. B1b~B1f 실패가 이 하나로 수렴.
- Plant 객체는 caller가 Erase 불가(eCannotBeErasedByCaller) → 순수 solid만 2nd temp에 clone(M3). transient solid 직접 cross-DB append 금지 → WblockCloneObjects만.
- 상세: `plan_pfs_isolation_20260711.md`(§4-A~P), 메모리 `pfs-wblockclone-preserves-plant-geometry`.

## 진행 중 (2026-07-13 최신) — 무탭 엔진
- **별도 도면(태그별 .dwg+타이틀블록) ✅ 라이브 완성**: side-DB tagDb(템플릿 .dwt)→2D+치수 클론→플로팅 뷰포트(1:2 고정)→DRAWING_TITLE 속성→Details\<tag>.dwg. 커밋 6ded95e~c841ad0. 원본 무변경.
- **★무탭 엔진 GO (스파이크 실증)**: side-DB Solid3d+Hidden 뷰포트로 VIEWBASE/EXPORTLAYOUT 없이 은선제거 정투영 성공. 상세=메모리 [[pfs-notab-engine-go]]. 스파이크=C:\Lisp\NoTabSpike.
- **대기 중 액션**: 스파이크 D(side-DB Plant explode) 라이브 실행 → 완전vs부분 무탭 확정.
- **다음 결정 분기**: D 성공(Solid3d 산출)→완전 무탭 엔진 계획 / D 실패→부분 무탭(1차 explode 문서 1회만). 이후 밸룬+BOP/PLN 콜아웃+BOM(기존 AnnotateViewport 재사용) / 치수 3D→PSDCS 재설계.
- **백로그(TODO)**: 주석엔진 통합(밸룬/BOP/BOM), 뷰포트-타이틀블록 정렬(미세조정), 무탭 3부채(뷰큐브/다이얼로그/temp누적—무탭이면 소멸).

## 관련 파일·경로
- 잔여 백로그: `TODO.md`
- 변경 이력: `CHANGELOG.md`
- 상세 설계철학·목표사양은 프로젝트 메모리(`project_pfs_design_philosophy_core`, `project_pfs_target_output_spec_caetech`) 참조.
