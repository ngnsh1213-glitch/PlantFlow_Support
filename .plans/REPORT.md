# REPORT - Codex -> Claude

- cycle: 42
- status: done
- commit: HEAD (최종 해시는 Codex 완료 보고 참조)
- title: 무탭 추출 후 리본 바인딩 PERSPECTIVE 지연 flip 방어 가드

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `RunNotabDetailPipeline` 진입 시 저장한 `PERSPECTIVE` 값을 기준으로 8초 가드를 무장.
  - 기존 `PFSPERSPWATCH` / `OnSysVarChangedForPersp` 이벤트 인프라를 재사용하도록 공용 구독 플래그를 추가.
  - 수동 watch 토글과 자동 가드가 `SystemVariableChanged` 핸들러를 중복 구독하지 않도록 방어.
  - 가드 창 안에서 `PERSPECTIVE`가 저장값과 달라지면 1회만 `SetSystemVariable`로 복원하고 즉시 disarm.
  - 복원 후 `Editor.Regen()`은 이벤트 컨텍스트에서 직접 호출하지 않고 `Application.Idle` one-shot 핸들러로 지연 실행.
  - `PFS_PERSP_GUARD_SEC` 환경변수로 가드 창 조정 가능. 유효하지 않은 값은 로그 후 기본 8초 유지.

## 보존 확인
- 대상 외 파일 수정 없음.
- 기존 finally의 명령 종료 시점 `PERSPECTIVE` 원복 로직 유지.
- 기존 스택 계측 로그 유지 후 가드 교정만 추가.
- 창 밖 변경, 저장값 null, 재진입 중 변경은 교정하지 않음.

## 검증
- `dotnet build .\PlantFlow_Support.sln -c Debug`: PASS, 오류 0 / 기존 경고 15
- 변경 주변 20줄 이상 수동 확인 완료.

## 라이브 확인 포인트
- `dev_test.bat` 실행 후 `C:\Temp\pfs_diag.log`에서 `PFSNOTABDETAIL persp guard armed` 확인.
- 지연 flip 재현 시 `PFSPERSPWATCH PERSPECTIVE -> 1` 뒤 `PFSNOTABDETAIL persp guard 교정 1 -> 0` 및 `Regen Idle 예약/완료` 로그 확인.
- 8초 창 밖 수동 `PERSPECTIVE` 변경은 유지되는지 사용자 라이브 확인 필요.
