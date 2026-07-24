# 계획 — 유볼트 Phase 2: 분류 뒤집기 + 실 실루엣 렌더 + 블럭 재정합 (cycle130)

## 목표
유볼트 조각남을 근본 해결한다. `Brep.Edges`가 놓치는 곡면 실루엣을 **해석적으로 계산·보강**해 참조 이미지(아치+다리+너트)를 실형상에서 재현한다. 전제로 cycle127 분류결함을 고친다. **Main뷰(face-on) 우선**, 멀티뷰 토러스 실루엣은 후속.

## 확정 사실 (cycle128 스파이크 실측)
- 관리형 Brep API 곡면 파라미터 노출: Cylinder{Origin,Radius,Axis}, Torus{Center,MajorRadius,MinorRadius,축}, Plane{Normal}.
- 유볼트 솔리드 = 토러스 아치(MajorR52·MinorR6) + 다리 원기둥(R6) + 육각너트(30° 평면) + 워셔(R12). Main 시선 V=s_isoPipeAxis=(0,1,0), 유볼트 평면=X-Z(face-on).
- 배관 솔리드 존재(Cylinder R44.55, axis(0,1,0)). cycle127이 UB로 오분류(support=0).
- 투영 = `TryNotabProjectWcsToPaper`(WCS→페이퍼). 상세DB 솔리드는 WCS 보존.

## 자문 채택 (Codex §9 read-only 1회)
1. **에지+실루엣 보강**(저리스크). UB 원기둥 **캡(끝단 원) 에지 억제**(선분/타원 클러터 방지), 평면/너트/트림 에지는 유지.
2. 원기둥 축=`Cylinder.Axis`, 축범위 tMin/tMax = 면 loop/edge 정점을 축에 내적투영(`t=dot(P-Origin, Â)`), 곡선에지는 표본점 추가. 정점 부족 시 WARN.
3. 토러스 face-on = `abs(dot(V̂,N̂)) ≥ 1-ε`(ε≈1e-3, N=토러스 **회전축**). 반원 아치는 Major±Minor 전원 금지 → 면 경계 정점을 토러스 평면에 투영해 atan2 각도범위 산출(0° 래핑·다중 trim loop 주의, 중간점 샘플로 실 구간 판정).
4. 실루엣은 **3D WCS 생성 → TryNotabProjectWcsToPaper 투영**(뷰변환·축척·회전 동일경로라 에지와 정합). 호는 충분 분할점.
5. 분류=tag별 최근접 솔리드 + **UB 양성 시그니처**(Cylinder R≈6 / Torus R52·r6 / 육각평면 조합) 동반. 단순 거리tol만은 베이스플레이트 재흡수 → 시그니처 필수. 애매하면 support 보수분류+WARN.

## 설계 (FlattenNotabSolidsToPaper 개조)

### A. 분류 뒤집기 (ClassifyNotabFlattenSolid 재설계)
- 각 Solid3d의 면 시그니처 사전계산: hasTorus(MinorR≈6), rodCylCount(R≈6), 대형Cyl(R≈파이프반경).
- **pipe**: 대형 Cylinder(R≈s_isoPipeRadiusModel, tol) && 축≈s_isoPipeAxis → "PIPE".
- **ubolt**: UB 시그니처(hasTorus || rodCyl≥2) && s_isoUbolts 중 최근접 tag의 중심과 근접 → "UB:<tag>". (tag별 최근접 1 + 시그니처 만족 추가 솔리드 포함.)
- **support**: 나머지. 애매(시그니처 불명확)하면 support + `PFSNOTABBLOCK unclassified WARN`.
- 베이스플레이트=평면 박스(torus/rodCyl 없음)→support 안정.

### B. 유볼트 렌더 (에지 + 실루엣)
- UB 솔리드도 기존 `Brep.Edges` 폴리라인 투영 **유지**하되 **캡 에지 억제**: 원기둥 면의 원형 끝단 에지(circular, 반경≈해당 Cyl R)는 skip.
- 각 UB 솔리드 면 순회, 곡면 실루엣 3D WCS 생성:
  - **Cylinder**(축 A, Origin, R): `off = R·normalize(A×V)` (|A×V|<ε면 end-on → 실루엣 없음 skip). 두 선분 = Origin±off 기준으로 축범위 tMin..tMax(정점 투영). WCS 2선분 → 투영.
  - **Torus**(중심 C, MajorR, MinorR, 회전축 N): face-on이면 반경 Major±Minor 두 호, 각도구간=면 경계 정점 atan2. WCS 다분할 폴리라인 → 투영. face-on 아니면 skip+WARN(멀티뷰 후속).
  - **Plane**: 실루엣 없음(에지가 처리).
- 실루엣 폴리라인은 해당 UB tag 그룹에 누적(PFS_ISO_DETAIL, ByLayer).

### C. 블럭 재정합
- 분류 수정으로 support/pipe/UB-tag 그룹이 올바르게 분리 → `PFS_FLAT_SUPPORT`/`PFS_FLAT_PIPE`/`PFS_FLAT_UB_<tag>` 블럭. UB 블럭 = 에지(캡억제)+실루엣.

### D. 로그
- `PFSNOTABSIL solid=<id> tag=<UB> cyl=<n>(sil=<n>,skip=<n>) torus=<n>(faceon=<n>,warn=<n>) capEdgeSkip=<n>`.
- 분류: `PFSNOTABBLOCK support=<n> pipe=<n> ubolt=<tags> unclassified=<n> blocks=<n>` (수정 후 support≥1, pipe=1 기대).

## 검증 레시피
- dev_test `PFS_NOTAB_FLATTEN=1` + `RC1-001` →
  - 분류 로그 `support≥1 pipe=1 ubolt=UB-002,UB-003`(베이스플레이트=support, R44.55=pipe).
  - 유볼트가 참조 이미지처럼 **아치 동심호+다리 평행선+너트**로 깨끗(조각남 해소). 3DORBIT로 2D 확인.
  - support(각재)·배관 원 무변화(회귀 0). 블럭 3+2종 올바른 분리(선택 확인).
- `PFS_NOTAB_FLATTEN=0`: 회귀 0.

## 스코프·리스크
- **Main뷰 face-on 한정**. 멀티뷰(Top/ISO) 토러스 일반 실루엣은 후속 사이클(quartic, 난이도↑).
- 캡 에지 억제 판정이 어려우면 1차는 전 에지 유지(다소 지저분) 후 refine — 단 로그로 캡 개수 계측.
- Cylinder.Axis/Torus 회전축 반사 취득이 스파이크 미확인분 → 집도 시 로그로 값 검증 먼저.
- 분류 시그니처 tol 오설정 시 오분류 → 로그 카운트로 즉시 검출.
