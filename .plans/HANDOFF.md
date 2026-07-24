# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 130
- **status**: ready
- **issued_at**: 2026-07-24
- **title**: 유볼트 Phase 2 — 분류 뒤집기 + 실 실루엣 렌더(에지+실루엣) + 블럭 재정합
- **작업 경로**: `PlantFlow_Support/Core/Commands.cs` — `FlattenNotabSolidsToPaper`(1980)·`ClassifyNotabFlattenSolid`(2116) 개조 + 실루엣 헬퍼 신설
- **계획서**: `.plans/plan_notab_ubolt_silhouette_20260724.md`
- **기준 커밋**: `240b52e`(cycle129)
- **자문**: Codex(§9, read-only 1회). 채택 = ①에지+실루엣 보강(UB 캡 에지 억제) ②Cylinder.Axis+정점투영 tMin/tMax ③Torus face-on `|V̂·N̂|≥1-ε`·경계정점 atan2 trim호(전원 금지) ④실루엣 3D WCS→TryNotabProjectWcsToPaper ⑤분류=최근접+UB 시그니처(Cyl R6/Torus R52·r6), 애매하면 support+WARN.

## 확정 사실 (cycle128 스파이크)
Cylinder{Origin,Radius,Axis}/Torus{Center,MajorRadius,MinorRadius,축}/Plane{Normal} 반사 취득 가능. 유볼트=토러스아치(R52·r6)+다리원기둥(R6)+육각너트+워셔(R12). Main 시선 V=s_isoPipeAxis=(0,1,0), 유볼트평면 X-Z(face-on). 배관 솔리드=Cylinder R44.55(cycle127이 UB 오분류). 상세DB 솔리드 WCS 보존.

## ⚠ 검증 필수
`dotnet build` 오류 0(빌드·커밋 분리) 없이 커밋 금지. **push 금지**. `PFS_NOTAB_FLATTEN=0` 회귀 0. **집도 초반 Cylinder.Axis/Torus 회전축 반사값을 로그로 검증**(스파이크 미확인분) 후 실루엣 진행.

## 집도 항목
1. **분류 뒤집기**(ClassifyNotabFlattenSolid 재설계):
   - 면 시그니처: hasTorus(MinorR≈6), rodCyl(R≈6) 개수, 대형Cyl(R≈s_isoPipeRadiusModel).
   - pipe = 대형Cyl && 축≈s_isoPipeAxis → "PIPE". ubolt = (hasTorus||rodCyl≥2) && s_isoUbolts 최근접 tag 근접 → "UB:<tag>"(tag별 최근접+시그니처 만족 추가 솔리드 포함). 나머지 support. 애매 → support+`unclassified WARN`.
2. **유볼트 렌더 = 에지+실루엣**:
   - 기존 Brep.Edges 투영 유지하되 **원기둥 캡(원형 끝단) 에지 skip**(반경≈Cyl R인 circular edge).
   - Cylinder 실루엣: `off=R·normalize(Axis×V)`(|Axis×V|<ε면 skip). 두 선분 = Origin±off, 축범위 tMin/tMax(면 정점 축투영, 곡선에지 표본추가). 3D WCS → TryNotabProjectWcsToPaper.
   - Torus 실루엣: face-on(`abs(dot(V̂,N̂))≥1-ε`, N=회전축)이면 반경 Major±Minor 두 호, 각도구간=면 경계 정점 atan2(0° 래핑·다중loop 주의, 중간점 샘플 검증), 다분할 WCS 폴리라인 → 투영. 아니면 skip+WARN.
   - Plane: 실루엣 없음.
   - 실루엣 폴리라인은 해당 UB tag 그룹 누적(PFS_ISO_DETAIL ByLayer).
3. **블럭 재정합**: 분류 수정으로 `PFS_FLAT_SUPPORT`/`PFS_FLAT_PIPE`/`PFS_FLAT_UB_<tag>` 올바른 분리. UB 블럭=에지(캡억제)+실루엣.
4. **로그**: `PFSNOTABSIL solid= tag= cyl=(sil=,skip=) torus=(faceon=,warn=) capEdgeSkip=` + 기존 `PFSNOTABBLOCK support= pipe= ubolt= unclassified= blocks=`.

## 검증 레시피
- dev_test `PFS_NOTAB_FLATTEN=1` + `RC1-001` →
  - 분류 `support≥1 pipe=1 ubolt=UB-002,UB-003`(베이스플레이트=support, R44.55=pipe).
  - 유볼트 = 아치 동심호+다리 평행선+너트로 **깨끗**(조각남 해소), 3DORBIT 2D 확인.
  - 각재·배관 원 무변화, 블럭 올바른 분리(선택 확인).
- `PFS_NOTAB_FLATTEN=0`: 회귀 0.
- 첫 관문 = Cylinder.Axis/Torus 회전축 반사 취득 로그 + 유볼트 실루엣이 참조 이미지처럼 나오는가.

## 범위 밖
멀티뷰(Top/ISO) 토러스 일반 실루엣(quartic)은 후속 사이클. 이번은 Main뷰 face-on 한정. 밸룬 미세조정(cycle129) 손대지 말 것.
