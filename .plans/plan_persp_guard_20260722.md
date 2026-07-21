# 계획서 — PERSPECTIVE 가드 발원 기반 재설계 (2026-07-22)

- 대상: `PlantFlow_Support/Core/Commands.Persp.cs`
- 성격: 전제-의존·비결정 레이스. 다이얼 높음 이상 권장 트랙.
- 자문: §9 Codex(우선)+Gemini(보조) 1회 완료.

## 1. 확정 실측 (pfs_diag.log, 2026-07-21 19:27)
- 무탭 추출 3건: PERSPECTIVE 안 건드림(enter=0/exit=0).
- t=21.14s: `PERSPECTIVE→1`. 스택 = `RibbonListButton.set_Current → RibbonListButtonBindings.TrySetCurrent → WPF BindingExpression.UpdateSource → SETVAR`. 우리 코드 없음 = **AutoCAD 내장 리본 WPF 바인딩의 stale flush**.
- t=21.16s: 현행 가드가 →0 교정 후 **자폭**.
- t=29.88s: `PERSPECTIVE→1`, CMDNAMES=VIEWCUBEACTION. 가드 꺼져 못 잡음.
- t=31.81s: `PERSPECTIVE→0` (여전히 VIEWCUBEACTION). **자체복귀**.

## 2. 현행 가드 3결함 (코드 판독)
1. **1회 자폭** (`TryRestorePerspGuard` 202행 `s_perspGuardUntilUtc=MinValue`): 첫 교정 후 무장 해제 → 21.16 이후 flip 미포착.
2. **시간창 8초** (191행): 리본 flush가 추출+5초에 오는데 창은 +8초에 닫힘. 마진 2초 = 느린 실행에서 놓침 = **간헐 재현의 정체**.
3. **발원 무차별** (193행): 창 안 모든 변경을 되돌림 → 사용자 의도 토글도 강제 복원 위험. `Environment.StackTrace`는 로그만 하고 복원 판정엔 미사용.

## 3. 자문 종합
### 수렴 (Codex ∧ Gemini)
- 1회 자폭 제거 + 백스톱 60초.
- 스택 판정을 **어셈블리/네임스페이스 기반**으로: `StackFrame.GetMethod().DeclaringType.Assembly.GetName().Name == "AdWindows"` 또는 `Namespace StartsWith "Autodesk.Windows"`. (부분 문자열은 JIT 인라이닝·버전 변경에 취약.)
- Q4 근본치료: 관측 리본은 **내장 리본**(우리 소유 아님) → 바인딩 직접 조작 위험. 사후 가드가 현실적 최선.

### 분기 → Codex 채택 (§9 규칙)
- **VIEWCUBEACTION 중 자동 복원 금지.** 강제 0 덮어쓰기는 뷰큐브 내비게이션과 경쟁 위험 + 해당 이벤트 자체복귀(31.81) + 공개 이벤트로 사용자 의도 확정 불가. Gemini의 "창 안이면 뷰큐브도 롤백"은 정당 조작과 싸울 위험이라 기각.
- 복원은 **strong-ribbon 분류일 때만**. native-command(VIEWCUBEACTION)·unknown은 **로그만**.

## 4. 설계 (3분류 + 발원 기반)
`OnSysVarChangedForPersp`에서 스택을 3분류하고 그 결과를 복원 판정 입력으로 전달:
- **strong-ribbon**: 스택에 `AdWindows` 어셈블리 또는 `Autodesk.Windows` 네임스페이스 프레임 존재 → 리본 아티팩트 → 60초 백스톱 내면 **복원**.
- **native-command**: CMDNAMES에 VIEWCUBEACTION 등 뷰큐브/네이티브 명령 활성 → **로그만**.
- **unknown**: 위 아님 → **로그만**(복원 안 함. 프레임 부재는 "리본 아님"의 증거가 아님).

부수:
- 자폭 제거, 백스톱 60초(기존 env `PFS_PERSP_GUARD_SEC` 재사용, 기본 8→60).
- 가드에 **generation + 대상 문서** 부여, 문서 전환·다음 추출·만료 시 폐기(현 정적 전역은 연속 추출/문서 전환 구분 못 함).

## 5. 계측 강화 (분기 해소용)
- `Document.CommandWillStart/CommandEnded`로 VIEWCUBEACTION 시작·종료 시각+generation 로그.
- sysvar 변경마다 값·CMDNAMES·문서·generation·**분류결과(strong/native/unknown)** 기록.
- 다음 dev_test 1회로 29.88형 이벤트가 사용자發인지 리본 우회인지 실측 확정.

## 6. 선택적 근본치료 스파이크 (Gemini 보조, 후순위)
- 추출 종료 직후 `ComponentManager.Ribbon` 강제 동기화로 stale flush 사전 차단 시도. **계측만**, 가드는 백스톱으로 유지. Codex가 내장 리본 조작에 신중하라 했으므로 이번 사이클 범위 밖(별 사이클).

## 7. 이번 사이클 범위
- Phase 1(핵심): 자폭 제거 + 어셈블리 기반 3분류 + strong-ribbon만 복원 + 60초 + generation/문서 스코프.
- Phase 2(계측): CommandWillStart/Ended + 분류결과 로그.
- Phase 3(스파이크): 보류.

## 8. 성공 기준
- 느린 실행에서도 21.14형 리본 flush를 60초 창 내 확실히 교정(간헐 재현 소멸).
- VIEWCUBEACTION 건은 건드리지 않고 정확히 분류·로깅.
- 다음 라이브 로그로 29.88형의 정체 확정 → Phase 3 착수 여부 결정.
