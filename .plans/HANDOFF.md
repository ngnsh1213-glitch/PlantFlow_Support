# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 49
- **status**: ready
- **issued_at**: 2026-07-16
- **title**: 무탭 가로치수 배관 중심 기준화 — (1) 배관중심X 분할 + (2) 배관 근접 쪽(상/하단) 배치
- **target**: `PlantFlow_Support/Core/Commands.cs` (그 외 무수정)

## 착수 전
- cwd `D:\PlantFlow\PlantFlow_Support`. 배관중심 전역 + `TryComputeNotabDimPaperGeometry` + `AppendNotabPaperDimensions` 가로 배치만. **세로 치수는 무수정(그대로).**
- 빌드 GREEN(MSBuild Debug, 에러0). 라이브는 사용자(`PFS_NOTAB_TEST_TAG=RC1-001`).

## 배경 (라이브 피드백)
cycle48 치수 라이브 성공(세로597.5·가로총폭450·분할). 그러나 **분할이 225/225=서포트 중심**(실제 배관 아님)이고, **가로 치수가 상단**에 있어 배관(하단 암 위)과 멀어 인식 어려움. 사용자 요구:
- (1) 분할 기준 = **실제 배관 중심 X**.
- (2) 가로 치수를 **배관에 가까운 쪽(상/하단)**에 배치(RC1=배관 하단→치수 하단). **세로는 좌측 유지.**
- 원칙: 타입 하드코딩 아닌 **배관 위치 기하 규칙**.

## 근인 (계측/코드)
- 현 `TryComputeNotabDimPaperGeometry`(~4035)가 `pipeCenterXPaper`에 **support bbox center 투영**(4060-4065)을 씀 → 항상 절반 분할.
- 배관 중심 WCS를 저장한 전역 없음(`s_isoPipeAxis/up/id/valid`만). held-pipe=`s_isoPipeId`(TryGetSelectionPipeAxis 1030, 필터 후 실제 잡는 배관).

## 요구
### A. 배관 중심 WCS 전역 확보
- 신규 전역 `s_isoPipeCenterWcs`(Point3d) + `s_isoPipeCenterValid`(bool). 파이프라인 진입부(다른 s_iso 초기화 지점 ~1012-1024)에서 리셋.
- `RunNotabDetailPipeline`에서 `s_isoPipeId` 확정(1030) **직후**, source db에서 `TryGetPipeAxisFromId(sourceTr/db, s_isoPipeId, out p0, out dir, out radius)`로 p0 획득 → `s_isoPipeCenterWcs=p0; s_isoPipeCenterValid=true`. 실패 시 valid=false. (p0=배관축상 점=단면중심, 투영 시 배관 단면중심 페이퍼좌표, §9 e.)

### B. pipeCenter를 배관 투영으로 교체 + Y 산출
- `TryComputeNotabDimPaperGeometry`: `s_isoPipeCenterValid`면 `pipePaper=NotabProjectWcsToPaper(vp, s_isoPipeCenterWcs)` → `pipeCenterXPaper=pipePaper.X`, **신규 out `pipeCenterYPaper=pipePaper.Y`**. invalid면 기존 support center 폴백(pipeCenterYPaper=support center Y).

### C. 가로 치수 상/하단 배치 (AppendNotabPaperDimensions)
- `centerY=(minY+maxY)/2`. **`pipeCenterYPaper < centerY - eps` → 하단, 아니면(수평관통 tiebreak 포함) 상단**(§9 c 기본 상단).
- 상단(기존): split 기준Y=maxY, `splitY=maxY+offset`, `totalY=splitY+stack`. 화살표/방향 기존.
- **하단(신규)**: split 기준Y=minY, `splitY=minY-offset`, `totalY=splitY-stack`. **Y 부호만 반전, 화살표/point 방향 반전 불필요**(§9 a). CreateHorizontalDimension의 두 점 Y=minY, dim line point Y=splitY/totalY.
- **분할 경계 가드(§9 d)**: `leftReal` 또는 `rightReal`에 해당하는 페이퍼폭(pipeCenterXPaper-minX, maxX-pipeCenterXPaper)이 `min(txt*2, Dimasz*2)` 미만이면 **분할 생략, 총폭만**.
- 세로 치수(verticalX=minX-offset) **무수정**.
- 로그 `dim append ... side=top|bottom pipeCenterX(paper)=.. pipeCenterY(paper)=.. centerY=.. splitGuard=..`.

## 방어/보존
- TryGetPipeAxisFromId/투영 try/catch+FileDiag(빈 catch 금지). pipe center invalid→support center 폴백. p0 degenerate 방어.
- 클립·held-pipe·스케일·persp가드·wireframe·**세로 치수** 무수정.

## 검증
- MSBuild Debug GREEN. 변경 주변 20줄 수동 확인.
- 라이브(RC1-001): 로그 `side=bottom`(배관 하단), 분할이 **실제 배관중심 비율**(예 350/100류, 225/225 아님), 가로 치수가 **하단**에 정렬. 세로 유지. env 조정 불요(기하 자동).

## 참고
- cycle48 커밋 dbb87bf. 투영=NotabProjectWcsToPaper(4075). TryGetPipeAxisFromId(~6992/7042). 배치 좌표=4193-4217.
