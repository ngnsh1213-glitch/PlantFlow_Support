# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 128
- **status**: ready
- **issued_at**: 2026-07-24
- **title**: 유볼트/배관 히든처리 판정 계측 스파이크 (Brep.Faces 표면 덤프, 작도 없음)
- **작업 경로**: `PlantFlow_Support/Core/Commands.cs` — `FlattenNotabSolidsToPaper`(1980) 진입부 또는 전용 헬퍼(계측만)
- **계획서**: `.plans/plan_notab_faceprobe_spike_20260724.md`
- **기준 커밋**: `5c9cb25`(cycle127)
- **자문**: 생략(측정 전용 저위험, 사용자 "짧은 스파이크" 지시). Phase 2 본집도 때 §9 수행.

## 목적
유볼트·배관을 실루엣 기반 "히든처리"로 깨끗하게 그릴 수 있는지 **재빌드 1회로 판정**. 순수 로그 계측 — **작도·Erase·저장 로직 일절 변경 금지**.

## ⚠ 제약
- `PFS_NOTAB_FACE_PROBE=1`일 때만 계측 진입. 미설정 시 완전 무영향(회귀 0).
- 기존 flatten/블럭 로직 불변(로그만 추가). 빌드 오류 0 없이 커밋 금지. **push 금지**.

## 집도 항목 (env `PFS_NOTAB_FACE_PROBE=1` 게이트)
`FlattenNotabSolidsToPaper` 진입 직후(솔리드 Erase 전), 모델공간 각 `Solid3d`에 대해:
1. **면 표면 덤프**: `new Brep(solid)`의 `Brep.Faces` 순회. 각 Face의 실제 표면 지오메트리 타입/파라미터를 최대한 추출·로그:
   - `Face.GetSurfaceAsTrimmedSurface()` 반환 타입명 + BaseSurface/외부표면 캐스팅 시도 → 원기둥/원뿔/토러스/구/평면 판별 가능 여부, 가능하면 축·반경·중심 값.
   - 로그: `PFSNOTABFACE solid=<id> class=<분류> face=<i>/<n> surf=<typeName> params={...} 판별=<가능|불가>`.
2. **분류 대조**: 각 솔리드에 `ClassifyNotabFlattenSolid` 적용해 support/pipe/ubolt 로그(유볼트 면구성=원기둥N+토러스1+평면N 예상 확인).
3. **배관 렌더 출처 계측**:
   - `s_isoPipeId / s_isoPipeCenterValid / s_isoPipeCenterWcs / s_isoPipeRadiusModel / s_isoPipeAxisValid` 값 로그.
   - 모델공간에 배관 분류 솔리드가 **있는지**(없으면 "배관 솔리드 부재") + bbox.
   - 도면 배관 원을 그리는 기존 코드 경로가 있으면 위치를 REPORT에 명시(pipe circle/pipe-obstacle 작도 grep).
4. 각 면 계측 예외는 개별 skip+로그, 전체 중단 금지.

## 검증 레시피
- dev_test `PFS_NOTAB_FLATTEN=1 PFS_NOTAB_FACE_PROBE=1` + `RC1-001` 추출 → `PFSNOTABFACE …` 로그 생성 확인.
- `PFS_NOTAB_FACE_PROBE` 미설정: 로그·동작 완전 동일(회귀 0).

## 판정(Claude가 로그로)
- 면 표면 타입·파라미터 읽힘 → 유볼트 **②실 실루엣**. 안 열림 → **①파라메트릭 심볼**(참조 이미지).
- 배관 원 출처 판명 → 배관 히든처리 설계 확정.

## 범위 밖 (다음 = Phase 2 본집도)
분류 뒤집기(유볼트 tag별 최근접 1솔리드) + 배관 원 + 유볼트 심볼/실루엣 + 블럭 정합. 이번 스파이크에선 손대지 말 것.
