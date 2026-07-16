# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 42
- **status**: ready
- **issued_at**: 2026-07-16
- **title**: 무탭 추출 후 리본 바인딩發 PERSPECTIVE=1 지연 flip 방어 가드(스코프 제한)
- **target**: `PlantFlow_Support/Core/Commands.cs` (그 외 무수정)

## 착수 전
- cwd `D:\PlantFlow\PlantFlow_Support`. `Commands.cs`의 무탭/PERSPECTIVE 감시 코드만 손댄다.
- 빌드 GREEN까지 Codex 확인(MSBuild). 라이브 테스트는 사용자(`dev_test.bat`).

## 배경 — 근본 원인 계측 완료 (커밋 6ad9e06 등)
`RunNotabDetailPipeline`(무탭 추출) 실행 중엔 활성 모델 `PERSPECTIVE`가 계속 0(계측 `persp enter=0 exit=0`). 그런데 **파이프라인 완료 ~4초 뒤 유휴 시점**에 `PERSPECTIVE`가 0→1로 바뀐다. 실시간 감시기(`PFSPERSPWATCH`) 스택 계측으로 발원 확정:

```
Autodesk.Windows.RibbonListButton.set_Current(RibbonItem)
 → RibbonListButtonBindings.TrySetCurrent
 → WPF BindingExpression.UpdateSource
 → SystemVariable.set_Value → PERSPECTIVE = 1
```

= **AutoCAD 리본 컨트롤의 WPF 데이터 바인딩**이 파이프라인의 그래픽스 갱신에 반응해 유휴 시점에 `PERSPECTIVE=1`을 역기입. **우리 코드/LISP/도면 저장뷰 아님**(QSAVE로 0 저장해도 재발). 리본 바인딩 자체는 AutoCAD 내부라 직접 수정 불가 → **지연 flip을 취소하는 방어 가드**로 증상 제거.

## 요구 사항 (설계)
기존 정적 감시 인프라(`s_perspWatchHandler` / `OnSysVarChangedForPersp` / `PFSPERSPWATCH`)를 재사용해 **스코프 제한 자동 가드**를 추가한다. `IExtensionApplication`은 없으므로 **리액터를 세션당 1회 지연 설치**한다.

1. **가드 상태 필드**(정적): `s_perspGuardUntilUtc`(DateTime), `s_perspGuardValue`(object, 되돌릴 값), 재진입 방지 `s_perspRestoring`(bool), 지연설치 여부 `s_perspGuardInstalled`(bool).

2. **지연 설치 헬퍼** `EnsurePerspGuardInstalled()`: `s_perspGuardInstalled==false`일 때만 `Application.SystemVariableChanged += OnSysVarChangedForPersp` 후 플래그 set. **PFSPERSPWATCH 수동 토글과 중복 구독되지 않도록** 구독 상태를 단일화한다(핸들러는 세션 내 최대 1개만 걸리게; 예: 공용 `s_perspHandlerSubscribed` 플래그 하나로 PFSPERSPWATCH·가드 양쪽이 판단, off 토글은 가드가 필요로 하면 해제 금지). 이중 구독·이중 해제 모두 방어.

3. **가드 무장**: `RunNotabDetailPipeline` 진입부(이미 `savedPerspective` 캡처하는 지점) 직후에
   - `s_perspGuardValue = savedPerspective;`
   - `s_perspGuardUntilUtc = DateTime.UtcNow + TimeSpan.FromSeconds(8);` (창 8초, 상수 또는 env `PFS_PERSP_GUARD_SEC`로 조정 가능)
   - `EnsurePerspGuardInstalled();`
   - 무장 사실 FileDiag 로깅.

4. **가드 발화**: `OnSysVarChangedForPersp` 핸들러에서 기존 로깅 **뒤에**, 아래 조건 모두 참이면 교정:
   - `s_perspRestoring == false`
   - `DateTime.UtcNow <= s_perspGuardUntilUtc` (창 내)
   - `s_perspGuardValue != null` 이고 현재 `PERSPECTIVE` 값이 `s_perspGuardValue`와 **다름**
   교정 절차:
   - `s_perspRestoring = true;` (finally에서 false 복원)
   - `Application.SetSystemVariable("PERSPECTIVE", s_perspGuardValue);`
   - REGEN은 **이 핸들러 컨텍스트(WPF 바인딩/유휴)에서 직접 호출 시 재진입 위험** → `Application.Idle`에 **one-shot** 핸들러를 걸어 다음 유휴에 `doc.Editor.Regen()` 후 자기 해제하는 방식으로 지연 실행(직접 Regen 호출 금지). Regen 실패는 catch+FileDiag.
   - 교정 후 **`s_perspGuardUntilUtc`를 과거로 밀어 즉시 disarm**(리본이 재차 1을 써도 핑퐁 방지, 1회 교정 원칙). FileDiag `가드 교정 X->Y` 기록.
   - `SetSystemVariable`이 다시 `SystemVariableChanged`를 재귀 발화하므로 `s_perspRestoring` 가드로 재진입 무시.

5. **범위 밖 flip 보존**: 창(8초) 밖의 `PERSPECTIVE` 변경(사용자가 의도적으로 3DORBIT/설정)은 **건드리지 않는다**. 가드는 오직 "파이프라인 직후 창 내 리본發 flip"만 취소.

## 방어적 프로그래밍
- 모든 이벤트/SetSystemVariable/Regen은 try/catch + FileDiag(빈 catch 금지).
- 재진입(`s_perspRestoring`)·이중구독·NRE(doc null) 방어.

## 검증
- MSBuild Debug GREEN(에러 0).
- 계측 로그 기대: 다음 `dev_test` 런에서 `PFSPERSPWATCH PERSPECTIVE -> 1` 발화 시 곧바로 `가드 교정 1->0` + 다음 유휴 REGEN, 최종 화면 parallel 유지. 창 밖 수동 flip은 유지되는지 사용자 확인.

## 참고 (현재 파일 상태)
- `RunNotabDetailPipeline`: PERSPECTIVE 저장/finally 복원 + `DescribeActiveViewPerspective` 뷰 로깅 이미 존재(커밋 37f7c72). finally의 기존 복원은 명령 종료 시점이라 **지연 flip을 못 잡음** — 이번 가드가 그 공백(유휴 시점)을 메운다. 기존 finally 로직은 유지.
- `PFSPERSPWATCH`/`OnSysVarChangedForPersp`/스택 로깅: 커밋 6ad9e06.
