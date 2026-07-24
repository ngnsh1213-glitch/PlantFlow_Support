# 계획 — 무탭 평면화 솔리드 분류 + 카테고리 블럭화 (cycle127, Phase 1)

## 목표
평면화된 2D 폴리라인을 **출처 솔리드별로 분류(support/pipe/ubolt)**하고, 카테고리별 **BlockReference로 묶어** 선택·관리 편의를 준다. 유볼트 조각남의 근본 해결(표준 심볼)은 **Phase 2로 분리**(캘리브레이션 필요).

## 배경 (실측 확정)
- cycle126 `FlattenNotabSolidsToPaper`([Commands.cs:1980](../PlantFlow_Support/Core/Commands.cs#L1980)) 라이브 PASS: `solids=4 edges=194 polylines=168 projFail=0 solidErased=4 vpErased=True`.
- 각재(프리즘)는 위상에지=외곽선이라 깨끗. **유볼트(원통봉)는 실루엣이 위상에지가 아니라 조각남** — Phase 2에서 심볼로 대체.
- 분류 근거(이미 존재하는 정적 상태):
  - `s_isoUbolts: List<NotabUboltSnapshot>{Tag, WcsCenter}` — 유볼트별 WCS중심(원본 파트 extents 중심).
  - `s_isoPipeCenterWcs` / `s_isoPipeAxis` / `s_isoPipeRadiusModel` / `s_isoPipeCenterValid`.
  - 상세DB 솔리드는 WblockClone로 **WCS 좌표 보존** → 공간 매칭 성립([pfs-wblockclone-preserves-plant-geometry]).
- ★로그상 투영 솔리드=각재2+유볼트2, **배관 솔리드 미포함 정황** → pipe 블럭은 비어있을 수 있음(방어 필수).

## 자문 채택 (Codex §9, read-only 1회)
1. 중심거리만으론 곡선 유볼트 조각 중심이 원 중심에서 벗어나 오분류 위험 → **"유볼트 중심이 솔리드 (확장)extents 내부"** 조건 우선, 복수 후보는 **최소거리**로 결정. tol은 모델단위·유볼트 외경/폭 연동. 애매하면 support 강등 대신 **미분류 WARN 로깅**.
2. 배관은 **축까지 수직거리 + 축방향 투영범위 + 반경tol** 3조건 동시.
3. 블럭 정의 좌표는 블록좌표계(MCS). 절대 페이퍼좌표+원점삽입도 시각 동일하나 재사용/편집/선택 불편 → **공유 기준점(전체 투영 bbox 좌하단)으로 상대좌표화** 후 그 점에 삽입(3블럭 공동 정합).
4. 엔티티는 **ByLayer(PFS_ISO_DETAIL) 고정**, BlockReference도 명시 레이어. ByLayer/ByBlock 혼용 금지.
5. Phase 1/2 분리 타당. Phase 1 유볼트 블럭은 "분류·제외 검증 + Phase 2 심볼의 그릇"으로 존치. 조각난 유볼트 선을 최종물로 확정하지 말 것.

## 집도 설계 (Phase 1)

### A. 솔리드 분류 (FlattenNotabSolidsToPaper 내, 폴리라인 append 전)
각 Solid3d의 `GeometricExtents`에서 WCS 중심·bbox 산출 후 분류:
- **ubolt**: `s_isoUbolts` 순회 — 유볼트 WcsCenter가 이 솔리드 확장 extents 내부이거나, 솔리드 중심이 유볼트 반경(외경/폭 기반 tol) 내. 복수 매칭 시 최소거리 tag 채택. → `classifiedTag = 유볼트 Tag`.
- **pipe**: `s_isoPipeCenterValid` && (솔리드 중심의 축수직거리 ≤ 반경+tol) && (축방향 투영이 파이프 구간과 겹침). → `classifiedTag = "PIPE"`.
- **support**: 위 미해당. → `classifiedTag = "SUPPORT"`.
- 어느 것도 확신 못 하면(예: pipe 후보인데 반경 크게 벗어남) `PFSNOTABBLOCK unclassified` WARN 로깅 후 support로 처리(형상 유실 금지).

### B. 폴리라인 카테고리 누적
- 기존처럼 즉시 `layout.AppendEntity(polyline)` 하지 말고, **카테고리 키별 리스트에 누적**:
  - support → 1개 그룹
  - pipe → 1개 그룹(비면 스킵)
  - ubolt → **tag별 분리**(UB-002, UB-003 각각) — 개별 선택 편의.
- 각 폴리라인 레이어 = PFS_ISO_DETAIL(ByLayer), 색/선종 ByLayer.

### C. 블럭 정의·삽입
- 전체 투영 폴리라인 bbox의 **좌하단 = 공유 기준점 base**.
- 카테고리(=블럭)마다:
  1. `BlockTableRecord` 신설. 이름 규약: `PFS_FLAT_SUPPORT` / `PFS_FLAT_PIPE` / `PFS_FLAT_UB_<tag>`(예 `PFS_FLAT_UB_UB-002`). 이름 충돌 시 접미 `_n`.
  2. 원점(0,0)을 base로 두기 위해 그룹 폴리라인을 **`-base`만큼 이동**(Matrix3d.Displacement) 후 BTR에 append.
  3. 레이아웃 BTR에 `BlockReference(base, btrId)` 삽입, 레이어=PFS_ISO_DETAIL, 회전 0, 스케일 1.
- 빈 카테고리(pipe 등)는 블럭 미생성.

### D. 로그
`PFSNOTABBLOCK support=<n> pipe=<n> ubolt=<tags> unclassified=<n> blocks=<count> base=(x,y)`.

### E. 방어·회귀
- `PFS_NOTAB_FLATTEN` 미설정 시 이 경로 진입 안 함(기존과 동일). 블럭화는 flatten 성공 시에만.
- 분류/블럭화 예외는 개별 solid skip + 로그, 전체 중단 금지. 폴리라인이 하나도 블럭에 안 담기면 블럭 미생성.
- 기존 안전장치 유지: 폴리라인 생성 성공 솔리드만 Erase, 전 솔리드 성공 시에만 viewport Erase.

## 검증 레시피
- dev_test `PFS_NOTAB_FLATTEN=1` + 태그 `RC1-001` → 추출 → 상세도:
  - 로그 `PFSNOTABBLOCK support= pipe= ubolt=UB-002,UB-003 …` 카운트 확인.
  - 폴리라인 선택 시 **블럭 단위로 선택**(support 한 번, ubolt 각각) — LIST/속성에서 BlockReference 확인.
  - 형상·주석 위치 cycle126과 **동일**(블럭화가 좌표 이동시키지 않음 — 이중이동 0).
  - EXPLODE 시 원래 폴리라인 복원되는지 1회 확인(무손실).
- `PFS_NOTAB_FLATTEN=0`: 회귀 0.

## Phase 2 (차기 `3`, 이 계획서엔 미포함)
유볼트 조각남 근본 해결 = **표준 2D 유볼트 심볼**. 유볼트 솔리드 투영 제외 + `s_isoUbolts` 위치·(카탈로그)크기로 심볼 작도 → `PFS_FLAT_UB_<tag>` 블럭에 주입. **심미 캘리브레이션**이라 [feedback-manual-calibration-workflow]에 따라 env 노브 우선. **열린 질문 = 심볼 형상**(Main뷰 유볼트 표준 도형: 아치+다리+너트? [pfs-hantec-support-standard-catalog] 재판독 + 사용자 확정 필요).

## 리스크
- 배관 솔리드 부재 시 pipe 블럭 없음(정상, 방어됨). 사용자가 배관 블럭을 기대하면 held-pipe 복제 포함 여부 별도 검토.
- 유볼트 explode 분할로 1 tag가 복수 솔리드 → 모두 같은 tag 블럭에 누적(설계 반영).
- 분류 tol 오설정 시 오분류 → 로그 카운트로 즉시 검출.
