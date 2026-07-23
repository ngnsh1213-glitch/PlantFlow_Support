# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 117
- **status**: ready
- **issued_at**: 2026-07-23
- **title**: 무탭 RS1~5 결함 일괄 수정 (config 행·RS4 가로 params·RS1 BOM·RS5/6 BOM 분기 신설·진단로그)
- **작업 경로**: `PlantFlow_Support/Core/Commands.cs` (GetNotabTypeConfig ~4910 / rcMemberGeometry ~3559 / MeasureNotabBomBalloonSources catch 6906~6938), `PlantFlow_Support/Models/BOMs.cs` (label_56 1462~ / RS5·RS6 case 832·843 → 신규 분기)
- **계획서**: `.plans/plan_notab_rs1_5_fix_20260723.md` (F1·F3·F4·F5·F6. **F2는 철회됨 — 집도 금지**)
- **진단 원장**: `.plans/notab_rs_review_20260723.md`
- **기준 커밋**: `01fa545`
- **자문**: Codex 단독(§9 단일채널=Codex 선택). 채택 3건 = ①F2 철회(param 세로치수는 키를 일반적으로 읽어 Ha 동작, 게이트는 포트앵커 전용) ②label_71은 **RS12A도 공유**(1398행)라 RS5/6 별도 분기 복제 + 원자적 실패 ③삼킴 지점=MeasureNotabBomBalloonSources catch(6906~), 신규 catch 아닌 기존 로그 보강. 전건 Claude가 코드로 교차검증 후 채택.

## ⚠ 검증 필수
`dotnet build` 오류 0 확인 없이 커밋 금지. 미실행 시 `status: blocked`로 반려. Claude가 REPORT 수령 후 직접 빌드 재확인. **push 금지**(사용자 수행).

## 집도 항목 (계획서 F1·F3·F4·F5·F6 — F2 없음)
1. **F1**: `GetNotabTypeConfig`에 RS1~5 행 추가 — RS1/2=`VerticalMode:"none"`, RS3/4=`VerticalMode:"param"`+`VerticalParamKey:"F2"`+`MemberAnchorSide:"vertical"`, RS5=`VerticalMode:"param"`+`VerticalParamKey:"Ha"`(MemberAnchorSide 없음). 전 행 `PipeCalloutSide:"top"`, `HorizontalSide:"auto"`.
2. **F3**: `rcMemberGeometry` 판정(~3559)에 RS4 추가(기존 RC 목록 불변). RS1/2/3은 추가 금지 — legacy bbox가 정답(RS2 944.6 사용자 확정).
3. **F4**: BOMs.cs label_56 — `StandardName=="RS1"`일 때만 `num87=0.0`(→F1=A=500). GD1(기존 공식)·RS11(A1 대체) 경로 불변.
4. **F5**: RS5/RS6 case(832·843)를 label_71에서 분리해 **신규 분기 복제** — F1=ceil(A+A1), F2=ceil(Ha), F3=ceil(Hb), BeamProfile(BI) 동일. A/A1/Ha/Hb/BI 중 하나라도 없거나 공백이면 **프레임 행 전체 미생성 + FileDiag(타입·누락 키·원시값)** (부분성공 금지 — F1만 남으면 member-text 폴백이 꺼져 더 불투명해짐). label_71 본문·RS12A goto(1398) 불변.
5. **F6**: 6936~6938 catch의 "MEASURE bom-source 예외" 로그에 std(StandardName)·예외 타입(KeyNotFound면 키)·supportId 보강. 동작 변경 금지.

## 검증 레시피 (dev_test.bat 태그 RS1~5 설정 완료)
| 대상 | 기대 로그/도면 |
|---|---|
| RS1 | 세로치수 없음(vMode=none) / bom-table rows=1, F1=500 / 가로 500(100/400) 유지 |
| RS2 | 세로치수 없음 / 가로 944.55 유지(dimH legacy) |
| RS3 | dimV param key=F2 500 / balloon F2가 세로재 포트 기반(geom=vertical-port) |
| RS4 | dimH src=params 800 / 세로 500 / F2 밸룬 세로재 / 분할(left=600,right=200) 투영 방향 눈확인 — 반대면 REPORT에 기록만(스왑은 후속) |
| RS5 | bom-table rows=3(800/500/500) / balloon F1~F3 / dimV param key=Ha 500 / member-text 폴백 소멸 |
| 회귀 | RC1~9·GD1~3·**RS12A**(label_71 공유 확인) 로그 diff 무변화 |
