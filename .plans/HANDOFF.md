# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 21
- **status**: ready
- **issued_at**: 2026-07-13
- **title**: 무탭 엔진 Phase N1 — side-DB 재귀 explode → Solid3d (검증 커맨드)
- **plan**: C:\Users\HT노승환\.gemini\antigravity\scratch\plan_pfs_notab_engine_20260713.md
- **target**: PlantFlow_Support/Core/Commands.cs (신규 커맨드/헬퍼 추가 · 기존 파이프라인 무수정)

## 착수 전
- cwd `...\PlantFlow_Support` 확인.
- ★기존 `PFSVBISOCLONE/OPEN/DONE/EXPORTED` 체인 **절대 수정 금지**(병렬 신규만 추가).

## 배경
- 스파이크 D 실증: side-DB(문서 없음)에서 Plant `entity.Explode()` 작동. Pipe→Solid3d, Support→중첩 BlockReference(재귀 필요). cross-DB append=eWrongDatabase→WblockClone 이송.
- 목표: 문서 열지 않고 선택 Plant 객체→순수 Solid3d 추출을 제품 코드에서 실증(무탭 엔진 1단).

## 지시 (cycle 21) — 신규 검증 커맨드 `PFSNOTABN1`
`[CommandMethod("PFSNOTABN1", CommandFlags.Session)]` 추가.

1. `ed.GetSelection()`으로 서포트+파이프 선택(기존 CLONE과 동일 UX).
2. 선택셋을 **side-DB로 clone**: `Database srcDb = new Database(true,true);` + `originalDb.WblockCloneObjects(ids, srcMsId, map, Ignore, false)` (기존 CLONE 방식 참고, 단 문서 열지 않음).
   - 또는 기존 1차 clone 산출 방식 재사용하되 **문서 오픈 없이** srcDb 확보.
3. **재귀 explode 헬퍼** `CollectSolidsRecursive(Transaction tr, Entity ent, List<ObjectId> solidIds, int depth)`:
   - `ent.Explode(coll)` 시도(try/catch, eNotApplicable 등 로깅).
   - coll 각 DBObject: Solid3d면 solidIds에 추가(또는 임시 owner에 append 후 id수집); BlockReference/기타 복합이면 재귀(depth+1, 상한 예: 6); Point 등은 skip.
   - Plant 객체(AcPpDb3d*)는 srcDb 트랜잭션 내에서 explode(스파이크 D처럼).
4. 수집된 Solid3d들을 **결과 side-DB로 WblockClone**(eWrongDatabase 회피) → `C:\Temp\notab_n1_solids_*.dwg` 저장.
5. 로그(`PFSVBISOEXPORTED`류 FileDiag 또는 신규 prefix `PFSNOTABN1`): 입력 엔티티 타입, explode 성공/예외, depth별 산출, 최종 Solid3d 수/extents, 저장 경로.

## 검증 기준
- 기존 `pfs_iso_solids_*.dwg`(문서-open explode 산출)와 **Solid3d 수/extents 일치**하면 N1 성공(문서 없이 동일 결과).
- 특히 Support의 중첩 BlockRef를 재귀로 파고들어 **서포트 solid까지** 추출되는지(스파이크 D는 1단만 했음).

## 규율
- 기존 파이프라인 무수정. cross-DB=WblockClone. 빈 catch 금지, 예외 FileDiag. 비파괴(원본 읽기만).

## 빌드/완료
- 수동 빌드 GREEN. `.plans/REPORT.md`에 결과. 커밋.
- 사용자: `PFSNOTABN1` 실행(서포트+파이프 선택) → 로그로 Solid3d 추출 수/extents 확인 → N2(Hidden 뷰포트) 진행.
