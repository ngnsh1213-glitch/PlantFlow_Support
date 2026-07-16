# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 46
- **status**: ready
- **issued_at**: 2026-07-16
- **title**: 무탭 배관 선택 tiebreak를 "rect중심 최근접"→"서포트 BOP 표고 매칭"으로 교체
- **target**: `PlantFlow_Support/Core/Commands.cs` (그 외 무수정)

## 착수 전
- cwd `D:\PlantFlow\PlantFlow_Support`. `AutoIncludeRelatedParts` 선택부 + BOP 조회 헬퍼만. 그 외 무수정.
- 빌드 GREEN. 라이브는 사용자(`PFS_NOTAB_TEST_TAG=RC1-001`).

## 배경 — cycle45 라이브: 잡는 배관 아닌 다른 배관 선택 (계측)
같은 라인 평행 배관이 **수직 200mm 차이**로 2개 rect 통과(`passCount=2`). cycle45 tiebreak=rect중심 최근접이 **오선택**.
- 실측: 선택됨(오답) 배관 중심Z=667.7 / 드롭됨(정답) 배관 중심Z=467.7.
- **결정 단서 = BOP**: 서포트 속성 `BOP=423`(배관 밑면 표고), 외경반경=44.45 → 잡는 배관 중심Z = BOP+반경 = 467.45. **드롭된 467.7이 정답**(BOP 일치), 선택된 667.7은 불일치.
- BOP는 "서포트가 어느 표고 배관을 잡는지" Plant가 기록한 데이터 → 기하 중심추측보다 확실.

## 요구 — tiebreak 교체 (rect 필터는 유지)
1. **서포트 BOP 직접 조회 헬퍼** `TryGetSupportBop(PSUtil ps, ObjectId supId, out double bop)`: `ps.dl_manager.GetProperties(supId, ["BOP"], true)` → 파싱(기존 `CaptureIsoSupportProperties`(~5744)가 props[4]="BOP"를 `RoundPropertyValue`로 읽는 패턴 재사용). **auto-include 시점엔 `s_isoBOP` 미설정이므로 직접 조회 필수.** 실패 시 hasBop=false.
2. **rect 통과 필터**(`DoesPipeAxisProjectThroughSupport`, cycle45)는 **그대로 유지**(수평/수직 실루엣 포함 판정=1차 후보).
3. **선택(tiebreak) 교체** (`pipeIncludes.Count>0` 선택부, ~6548):
   - hasBop=true: 각 후보의 **BOP오차** `bopErr = |(p0.Z - pipeRadius) - bop|` 계산(외경반경, `TryGetPipeRadiusFromProperties`). **bopErr 최소** 후보 선택.
     - 동일 BOP(±tol 내 복수, 드묾): 2차 tiebreak=rect중심 2D거리(기존 score) 최소.
     - tol `PFS_NOTAB_BOP_TOL` 기본 10mm(±5~10 권장, ±반경 금지).
   - hasBop=false: **폴백=기존 rect중심 최근접**(현 score) + **경고 로그**("BOP 부재 기하폴백"). ※배관 미포함(단독추출) 아님 — 도면/BOM 누락 방지(자문 정정).
4. **경사 배관 주의**: p0.Z는 축 기준점 표고. 직관이면 축중심 표고와 동일. 경사면 서포트 X/Y 위치에서 축점 Z 투영이 더 엄밀하나, 현 케이스(직관)는 p0.Z로 충분 — 경사는 후속.
5. 로그: 후보별 `bopErr=.. pipeCenterZ=.. radius=.. bop=..`, 선택 `include ... reason=bop|fallback`.

## 좌표계
- BOP·배관기하 모두 WCS Z 가정(자문 e). UCS 변환은 현 스코프 밖(직관·WCS 전제), 필요 시 후속.

## 방어/보존
- GetProperties/파싱 try/catch+FileDiag, ps null, 파싱 실패 시 hasBop=false 폴백. 빈 catch 금지.
- 클립·동적피팅·persp가드·wireframe·610×489·라인전처리(C)·rect 필터 **무수정 유지**. 이 변경은 **tiebreak 선택 로직만**.

## 검증
- MSBuild Debug GREEN. 변경 주변 20줄 수동 확인.
- 라이브(RC1-001): 기대 `include ... reason=bop`, **Z≈467 배관(BOP일치) 선택**, 667 배관 dropAmbiguous. 잡는 배관 정확·메인방향·스케일 유지. env `PFS_NOTAB_BOP_TOL`.

## 참고
- cycle45 커밋 27f9214(rect 투영). BOP 읽기=CaptureIsoSupportProperties(~5744), 반경=TryGetPipeRadiusFromProperties(~6704), 축=TryGetPipeAxisFromId(~6624), 선택부(~6548).
- 별도 백로그: 동적피팅 스케일 표준화(현 ~1:1.4 비표준 → 표준스케일 라운딩).
