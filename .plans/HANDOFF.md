# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 50
- **status**: ready
- **issued_at**: 2026-07-16
- **title**: 무탭 다중선택 배치 추출 명령 PFSNOTABBATCH (서포트 N개 → 각각 개별 detail)
- **target**: `PlantFlow_Support/Core/Commands.cs` (그 외 무수정)

## 착수 전
- cwd `D:\PlantFlow\PlantFlow_Support`. 신규 배치 명령 + `RunNotabDetailPipeline` 반환형 변경만. 그 외 무수정.
- 빌드 GREEN(MSBuild Debug, 에러0). 라이브는 사용자.

## 배경 — 요구
사용자가 全 서포트 타입 테스트 위해 **서포트 다중선택→각 서포트마다 개별 detail 도면 추출**(Details\<tag>_notab.dwg) 기능 요청. 현 `PFSNOTABDETAIL`(928)은 선택셋 전체를 union anchor 기반 **한 장**으로 처리 → 배치 불가.

## §9 확정 사실
- `RunNotabDetailPipeline`(~1010): 진입부 `s_iso*` 전역 리셋(1012-1028), `sourceDb` Dispose(2810/finally 1067), persp가드 매 호출 재무장 → **직렬 반복 안전**. 단 내부 try/catch가 예외를 삼켜(1049-1066) **성공/실패를 반환 안 함** → 배치 집계 위해 반환형 필요.
- 서포트 판정=`ObjectId.ObjectClass.Name`에 "Support" 포함(986/6790/6061). 저장태그=`s_isoSupportTag`(SupportName)→ShortDesc+ts→SUPPORT_+ts(2774), Details\<safeTag>_notab.dwg 덮어쓰기(2823). 무명 다수 같은 초=충돌 위험.

## 요구
### A. RunNotabDetailPipeline 반환형 (void → bool)
- 성공(saved) 시 `true`, 내부 catch 진입 시 `false` 반환. 기존 동작·로그·finally 유지, 시그니처만 `bool` + 성공 플래그 반환. 기존 호출부(PFSNOTABDETAIL 946, PFSNOTABTEST 968)는 반환값 무시 or 로그.

### B. 신규 명령 PFSNOTABBATCH
`[CommandMethod("PFSNOTABBATCH", CommandFlags.Session)]`, `PFSNOTABDETAIL`(928) 근처.
1. `ed.GetSelection()`(또는 서포트 SelectionFilter). 취소/빈 선택 방어.
2. 선택셋을 트랜잭션으로 열어 **`ObjectClass.Name`에 "Support" 포함 ObjectId만 수집**(파이프 등 무시). 0개면 메시지+종료.
3. **직렬 루프**: 각 supportId에 대해
   - 진행 로그 `PFSNOTABBATCH [i/N] tag=<SupportName> 처리` (SupportName 조회, FindSupportByTag의 property 읽기 패턴 재사용 or 간이).
   - `bool ok = false; try { ok = this.RunNotabDetailPipeline(doc, new[]{ supportId }); } catch(Exception ex){ ok=false; FileDiag(예외) }` — **한 서포트 실패가 배치 중단 금지**.
   - 성공/실패 카운트, 실패 시 태그 목록 수집.
   - (선택) 반복 후 `GC.Collect()` 1회로 side-DB 언매니지드 누적 완화(대량 배치 대비, 과하면 생략).
4. **요약**: `PFSNOTABBATCH done total=N ok=K fail=M fails=[tags]` FileDiag + `ed.WriteMessage`. (선택) `ProgressMeter`로 진행바.

### C. 무명/중복 태그 충돌 완화 (경미, 선택)
- 저장태그 fallback(2774)에서 무명 서포트는 **Handle 기반**(예 `SUPPORT_<Handle>`)로 하면 같은 초 충돌 회피. SupportName 있으면 기존대로. (배치 안전성 향상, 범위 크면 생략 가능하나 무명 다중 시 덮어쓰기 방지 권장.)

## 방어/보존
- 선택 필터·SupportName 조회·루프 각 반복 try/catch+FileDiag(빈 catch 금지). doc/ps null 방어.
- `RunNotabDetailPipeline` 내부 로직(클립·held-pipe·치수·스케일·persp가드)·단일 명령 **무수정**(반환형만). 배치는 얇은 루프 래퍼.

## 검증
- MSBuild Debug GREEN. 변경 주변 20줄 수동 확인.
- 라이브: 서포트 여러 개(여러 타입) 선택→`PFSNOTABBATCH`. 로그 `[i/N]`·요약 `ok=K fail=M`. Details 폴더에 태그별 <tag>_notab.dwg 생성 확인. 각 도면 치수/스케일/배관참조 정상. 실패 서포트는 스킵·목록 보고.

## 참고
- 단일=PFSNOTABDETAIL(928)/RunNotabDetailPipeline(1010). 저장태그=2774, 서포트판정=986. SupportName 읽기=FindSupportByTag(971 property 패턴).
