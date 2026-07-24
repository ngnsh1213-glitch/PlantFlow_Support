# 계획 — 무탭 유볼트/배관 히든처리 판정 계측 스파이크 (cycle128)

## 목적
유볼트·배관을 "히든처리"(실루엣 기반 깨끗한 투영)로 그릴 수 있는지 **재빌드 1회로 판정**한다. 작도 없음, **순수 로그 계측**.

## 배경
- 카테고리별 렌더링 확정(사용자): 서포트=`Brep.Edges` 위상에지(현행 깨끗) / 배관·유볼트=히든처리.
- 유볼트 조각남 근인 = `Brep.Edges`는 위상에지만 → 둥근 봉의 실루엣 누락([SESSION 평면화 트랙]).
- Main뷰는 파이프축 정면 → 유볼트 실루엣이 해석적(다리 평행선2+아치 동심호2 = 사용자 참조 이미지). **단 관리형 Brep API가 곡면(원기둥/토러스)의 축·반경을 노출하는지 미검증** → 이 스파이크가 그 게이트.

## 계측 항목 (작도·Erase 없음, env `PFS_NOTAB_FACE_PROBE=1`일 때만)
`FlattenNotabSolidsToPaper` 진입 직후(솔리드 Erase 전) 또는 별도 헬퍼에서, 모델공간 각 `Solid3d`에 대해:

1. **면 표면 덤프**: `new Brep(solid)`의 `Brep.Faces` 순회 — 각 Face의 실제 표면 지오메트리 타입과 파라미터를 최대한 로그:
   - `Face.GetSurfaceAsTrimmedSurface()` 결과 타입명, 그 BaseSurface/외부표면 캐스팅 시도(`ExternalBoundedSurface`→내부 AcGe 표면).
   - 표면이 원기둥/원뿔/토러스/구/평면 중 무엇인지 판별 가능한지, 가능하면 축·반경·중심 등 파라미터 값.
   - 로그: `PFSNOTABFACE solid=<id> class=<solid분류> faceN=<i> surf=<typeName> params={...} 판별=<가능/불가>`
2. **분류 대조**: 각 솔리드에 cycle127 `ClassifyNotabFlattenSolid`(또는 스파이크용 재분류)를 적용해 support/pipe/ubolt 중 무엇인지 함께 로그 → 유볼트 솔리드의 면 구성(원기둥N+토러스1+평면N 예상) 확인.
3. **배관 렌더 출처 계측**:
   - `s_isoPipeId`, `s_isoPipeCenterValid`, `s_isoPipeCenterWcs`, `s_isoPipeRadiusModel`, `s_isoPipeAxis(Valid)` 값 로그.
   - 모델공간 솔리드 중 배관으로 분류되는 것이 **있는지**(cycle127 로그 solids=4엔 없음) — 있으면 그 bbox, 없으면 "배관 솔리드 부재".
   - 도면의 배관 원이 어디서 나오는지 단서: 배관 원을 그리는 기존 코드 경로가 있으면 그 위치를 REPORT에 명시(grep: 배관 circle 작도/pipe-obstacle 작도 여부).

## 판정 게이트
- **PASS**(면 표면 타입·파라미터가 코드에서 읽힘): 유볼트 히든처리 = **②실 실루엣**(해석적 계산) 방향으로 Phase 2 본집도. 파라메트릭 심볼 불요.
- **FAIL**(관리형 API가 곡면 파라미터를 안 열어줌/캐스팅 실패): **①파라메트릭 심볼**(참조 이미지 기준)으로 확정.
- 배관: 원 렌더 출처가 판명되면 배관 히든처리(원 직접작도 or 기존 대체) 설계 확정.

## 방어·회귀
- `PFS_NOTAB_FACE_PROBE` 미설정 시 진입 0 = 완전 무영향. 설정 시에도 **작도·Erase·저장 로직 불변**(로그만 추가).
- 각 면 계측 예외는 개별 skip+로그, 전체 중단 금지.

## 검증 레시피
- dev_test `PFS_NOTAB_FLATTEN=1 PFS_NOTAB_FACE_PROBE=1` + `RC1-001` 추출 → `PFSNOTABFACE …` 로그 수집.
- Claude가 로그를 읽어 PASS/FAIL 판정 → Phase 2 방식(②/①) 확정 후 본집도 핸드오프.

## 비고
- 이 스파이크는 **측정 전용**이라 §9 자문 생략(저위험, 사용자 "짧은 스파이크" 지시). Phase 2 본집도 핸드오프 시 자문 수행.
