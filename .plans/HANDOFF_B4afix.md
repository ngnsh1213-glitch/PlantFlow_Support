# HANDOFF (B4a-fix) — Claude → Codex

> ★별도 채널: 현재 `.plans/HANDOFF.md`는 병렬 트랙(핫 리로드 cycle 6)이 점유 중이라 덮지 않는다.
> 이 B4a-fix는 이 파일을 읽고 집도한다. 완료 후 `REPORT.md` 대신 이 파일 하단 "## RESULT"에 결과 기록
> + 커밋. (파일 충돌 없음: 핫 리로드=tools/HotReload/만, B4a-fix=Commands.cs만.)

- **track**: B4a-fix (격리 치수)
- **status**: ready
- **issued_at**: 2026-07-13
- **title**: B4a 치수 eKeyNotFound 수정 + PLN을 파이프에서 읽기

## 착수 전
- cwd가 `...\PlantFlow_Support`인지 확인. 백업: `Commands.cs` → `Commands.cs.codex_bak_20260713_b4afix`.

## 배경 (B4a 라이브 판정)
- ✅ 실측 정확(realW=300 realH=75=서포트 바운드 일치), 파이프 blockInner Circle=1, BOP=423 읽힘.
- ❌ **치수 미출현**: `PFSVBISOEXPORTED dim 예외 eKeyNotFound`.
- ⚠️ **PLN 빈값**: 서포트 `LineNumberTag`가 비어있음(라인넘버는 파이프 속성).

## 수정 1 (핵심): 치수 생성 시 WorkingDatabase=originalDb (eKeyNotFound 해소)
**원인**: `AppendIsoBoundingDimensions`(Commands.cs:1635 호출)가 실행될 때 `HostApplicationServices.
WorkingDatabase`가 originalDb가 아님 — clone 스왑이 finally(1609)에서 `oldWorking`(temp/solidDoc DB)로
복구된 뒤 치수를 생성/append하여 originalDb의 dimstyle 심볼 해석 실패.
**조치**: `AppendIsoBoundingDimensions` 호출을 originalDb 컨텍스트로 감싼다.
- 호출부(1632-1636) 또는 헬퍼 내부에서:
  ```
  Database prevWdb = HostApplicationServices.WorkingDatabase;
  try {
    HostApplicationServices.WorkingDatabase = originalDb;
    this.AppendIsoBoundingDimensions(ttr, targetMs, dimSourceId, annotationLayerId, dimStyleId);
  } finally {
    HostApplicationServices.WorkingDatabase = prevWdb;
  }
  ```
- 헬퍼 내부에서 감싸도 되나 originalDb 참조를 넘겨야 함(현재 시그니처엔 없음) → 호출부 래핑이 최소 변경.
- 추가 안전: `AppendIsoBoundingDimensions`에서 `dimH.DimensionStyle = dimStyleId`(및 dimV) 명시 세팅
  (PSUtil이 이미 세팅하면 무해한 재확인). 여전히 eKeyNotFound면 dim.SetDatabaseDefaults() 후 스타일 세팅 순서 검토.

## 수정 2: PLN을 파이프에서 읽기 (서포트가 빈 경우)
- `CaptureIsoSelectionMetrics`/`CaptureIsoSupportProperties`에서 서포트 `LineNumberTag`가
  null/빈 문자열이면, **선택셋의 파이프(AcPpDb3dPipe) ObjectId로 라인넘버를 재조회**.
  - 파이프 id는 축 계산(TryGetSelectionPipeAxis)서 이미 순회하므로 그 파이프 id를 static/out으로 확보.
  - `dl_manager.GetProperties(pipeId, {"LineNumberTag","LineNumber","Line Number"}, true)` 시도,
    첫 비어있지 않은 값을 s_isoPipeLineNo에 저장. 로그 `PFSVBISOCLONE pipePLN candidate <name>=<val>`.
- 서포트/파이프 모두 비면 s_isoPipeLineNo 빈 유지(로그). (B4b MLeader는 빈값이면 스킵 예정.)

## 제약 (§0/§3/§3-A)
- Commands.cs 외 수정 금지. B1g~B4a 커널·EXPORTLAYOUT·clone-back 로직 무변경(치수 래핑+PLN 소스만).
- ★`.plans/HANDOFF.md`(핫 리로드 cycle 6) 절대 건드리지 말 것. tools/HotReload/도 무접촉.
- 빈 catch 금지(FileDiag). dl_manager null 방어. `git add PlantFlow_Support/Core/Commands.cs`만.
- 빌드는 사용자 수동. 완료 후 커밋(attribution 금지) + 이 파일 "## RESULT" 기록.

## 완료 기준 (Acceptance)
- [ ] 치수 생성이 WorkingDatabase=originalDb 컨텍스트서 수행(eKeyNotFound 해소)
- [ ] 서포트 PLN 빈 경우 파이프에서 라인넘버 재조회(후보 로깅)
- [ ] 커널·clone-back 무변경, 중괄호 균형, Commands.cs만 커밋, HANDOFF.md/tools 무접촉

## 검증 게이트 (사용자 수동 빌드/라이브 → Claude 판정)
- `PFSVBISOEXPORTED dim W=300 V=75 ...`(예외 없음) → 원본 Main 2D에 **폭·높이 치수(실 mm) 출현**(AUTO_DIM).
- `PFSVBISOCLONE PLN=<파이프 라인넘버>`(파이프서 취득) 또는 빈값 로그.
- 3회 반복. 치수 출현 + 실 mm 정확 → B4a 완료 → B4b(PLN/BOP MLeader, 파이프 원 2D 검출) 진행.

## RESULT (Codex 기록)
- **status**: done
- **completed_at**: 2026-07-13
- **commit**: 4d314b2 Fix isolated dimension context and pipe PLN

### 변경 요약
- `PFSVBISOEXPORTED`의 `AppendIsoBoundingDimensions` 호출을 `HostApplicationServices.WorkingDatabase = originalDb` 컨텍스트로 감싸 `eKeyNotFound` 원인을 차단했습니다.
- `AppendIsoBoundingDimensions`에서 `dimH/dimV.DimensionStyle = dimStyleId`를 명시 재세팅했습니다.
- `TryGetSelectionPipeAxis`가 최장 파이프축과 함께 해당 파이프 `ObjectId`를 반환하도록 확장했습니다.
- 서포트 `LineNumberTag`가 비어 있으면 파이프 `LineNumberTag`, `LineNumber`, `Line Number` 후보를 순차 조회하고 후보 로그를 남기도록 추가했습니다.

### 검증
- 백업 생성: `PlantFlow_Support/Core/Commands.cs.codex_bak_20260713_b4afix`
- 중괄호 균형: `opens=352 closes=352 balance=0`
- `git diff --check -- PlantFlow_Support/Core/Commands.cs`: whitespace error 없음(CRLF 경고만)
- 핵심 심볼 확인: `s_isoPipeId`, `pipePLN candidate`, `WorkingDatabase = originalDb`, `DimensionStyle = dimStyleId`
- 빌드는 사용자 수동 원칙에 따라 실행하지 않았습니다.

### 비고
- `.plans/HANDOFF.md`, `tools/HotReload/`는 건드리지 않았습니다.
- 커밋에는 `PlantFlow_Support/Core/Commands.cs`만 포함했습니다.