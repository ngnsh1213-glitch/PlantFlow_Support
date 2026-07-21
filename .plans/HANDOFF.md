# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 103
- **status**: ready
- **issued_at**: 2026-07-22
- **title**: PERSPECTIVE 가드 발원(스택 어셈블리) 기반 재설계 — 자폭 제거·strong-ribbon만 복원·VIEWCUBEACTION 존중
- **작업 경로**: `d:\PlantFlow\PlantFlow_Support\PlantFlow_Support\Core\Commands.Persp.cs`
- **계획서**: `d:\PlantFlow\PlantFlow_Support\.plans\plan_persp_guard_20260722.md`
- **핸드오프 위치**: `d:\PlantFlow\PlantFlow_Support\.plans\HANDOFF.md`
- **기준 커밋**: `a0acba6`
- **자문**: §9 Codex(우선)+Gemini(보조) 1회. 분기(VIEWCUBEACTION 처리)는 Codex 채택.

## ⚠ 검증 필수
`dotnet build` **오류 0**이 완료 조건이다. 미실행 시 커밋하지 말고 `status: blocked`로 반려하라.
Claude가 REPORT 수령 후 **직접 빌드를 재확인**한다(cycle98 사고 근거).

## 확정 실측 (pfs_diag.log 2026-07-21 19:27, 재측정 불요)
- 추출 자체는 PERSPECTIVE 안 건드림.
- t=21.14s `PERSPECTIVE→1`: 스택 = `RibbonListButton.set_Current → RibbonListButtonBindings.TrySetCurrent → BindingExpression.UpdateSource → SETVAR`. **AutoCAD 내장 리본 WPF 바인딩 stale flush.**
- t=21.16s 현행 가드 →0 교정 후 **자폭**.
- t=29.88s `→1` CMDNAMES=VIEWCUBEACTION, t=31.81s `→0` **자체복귀**. (가드는 이미 꺼져 미개입)

## 현행 결함 (Commands.Persp.cs)
1. `TryRestorePerspGuard` 202행 `s_perspGuardUntilUtc=MinValue` → **1회 교정 후 자폭**.
2. 191행 시간창 8초 → 리본 flush(+5초) 마진 2초 = **간헐 재현 원인**.
3. 193행 **발원 무차별 복원**. 캡처한 스택(169행)은 로그만 하고 복원 판정에 미전달.

## 집도 지시

### 1) 스택 3분류 도입 (핵심)
`OnSysVarChangedForPersp`에서 값 변경 시 스택을 분류하고, 그 결과를 `TryRestorePerspGuard`에 **인자로 전달**한다. 분류는 `Environment.StackTrace` 부분 문자열이 아니라 **`new System.Diagnostics.StackTrace()`의 프레임을 순회**해 판정:
- **strong-ribbon**: 어떤 프레임의 `GetMethod()?.DeclaringType?.Assembly.GetName().Name == "AdWindows"` **또는** `DeclaringType?.Namespace`가 `"Autodesk.Windows"`로 시작.
- **native-command**: strong-ribbon 아님 + `CMDNAMES`에 `VIEWCUBE`(대소문자 무시 포함) 존재.
- **unknown**: 그 외.
- 분류 판정은 예외 안전(프레임/메서드 null 가드). 실패 시 `unknown`으로 처리하고 `FileDiag`.

### 2) 복원 정책 교체
`TryRestorePerspGuard(object currentValue, PerspOrigin origin)`:
- **strong-ribbon일 때만** 되돌린다(백스톱 창 내·currentValue!=saved 조건은 유지).
- **native-command·unknown은 되돌리지 않는다.** 로그만 남긴다: `persp guard 관망 origin=... cmd=...`.
- **1회 자폭 제거**: 복원 후 `s_perspGuardUntilUtc`를 MinValue로 만들지 말 것. 백스톱 만료까지 무장 유지(리본 flush는 늦게/반복 올 수 있음).

### 3) 백스톱 60초 + generation/문서 스코프
- `ArmPerspGuard` 기본 `seconds` 8→**60**. env `PFS_PERSP_GUARD_SEC` 재사용(있으면 우선).
- 가드에 **generation(long 증가)** 와 **대상 문서(Document 참조 또는 이름)** 를 부여한다.
  - 새 추출에서 `ArmPerspGuard` 재호출 시 generation +1, saved/until 갱신.
  - `TryRestorePerspGuard`는 **현재 활성 문서가 가드 대상 문서와 같을 때만** 복원. 다르면 로그 후 무개입.
  - 백스톱 만료 시 자연 무효(현행 `UtcNow>until` 조건 유지).

### 4) 계측 강화 (분기 해소용, 필수)
- `OnSysVarChangedForPersp` 로그에 **분류결과·generation·활성문서**를 추가.
- **`Document.CommandWillStart`/`CommandEnded`** 구독을 추출 가드 무장 동안 걸어, `VIEWCUBEACTION` **시작·종료 시각 + generation**을 `FileDiag`로 남긴다(구독은 무장 시 1회, 만료/문서전환 시 해제 — 누수 금지).
- 목적: 다음 dev_test 1회로 29.88형 이벤트가 사용자發 뷰큐브인지 리본 우회 flush인지 실측 확정.

## 하지 말 것
- **VIEWCUBEACTION 중 PERSPECTIVE 강제 복원 금지**(자문 채택 결론). 계측만.
- 내장 리본 바인딩 직접 조작/억제 금지(별 사이클 스파이크로 보류).
- `PFSNOTABDETAIL` 추출 로직·`DescribeActiveViewPerspective`·`SchedulePerspGuardRegen`(Idle Regen)의 동작 변경 금지. Regen 예약은 strong-ribbon 복원 시에만 기존대로 호출.
- 무탭 배치/밸룬/치수 코드 무관 — 건드리지 말 것.

## 제약
- 빈 catch 금지. 모든 실패 경로 `FileDiag`.
- 신규 상수는 기존 env 관용구(`PFS_PERSP_GUARD_SEC`) 재사용. 새 매직넘버 지양.
- `#nullable disable` 파일이므로 명시적 null 가드로 방어.
- 스택 순회는 sysvar 변경 콜백마다 도는 핫패스 — take 상한(예: 앞 40프레임)으로 제한.

## 검증 (Codex, 필수)
1. `dotnet build` — 오류 0, 경고 현행 대비 증가 금지. 미실행 시 커밋 금지.
2. 커밋 후 `REPORT.md`에 변경 요약·빌드 결과 기록. 라이브는 사용자가 `dev_test.bat`로 수행.

## 성공 기준 (다음 라이브)
- 느린 실행에서도 21.14형 리본 flush를 60초 창 내 확실히 교정 → **간헐 재현 소멸**.
- VIEWCUBEACTION 건은 미개입·정확 분류·로깅.
- 로그에 `origin=strong-ribbon/native-command/unknown` + generation + 문서 + VIEWCUBE 명령 구간이 남아, 29.88형 정체를 다음 세션이 코드 재측정 없이 판정.
