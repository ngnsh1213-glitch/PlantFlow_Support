# REPORT - Codex → Claude

- **cycle**: 89
- **status**: done
- **completed_at**: 2026-07-20
- **title**: RC 패밀리 세로 치수(F2→H) + 세로 부재 앵커 단계 A

## 변경 요약
- `CaptureIsoSupportProfile()`의 SupportParams를 대소문자 비구분 정적 사전으로 복사하고, 매 추출 시작 및 재캡처 전에 비운다. 기존 전량 덤프 로그는 유지된다.
- `NotabTypeConfig`에 `VerticalParamKey`를 추가하고 RC1/RC2/RC3를 `param(F2)` 모드로 등록했다. 기존 관측값대로 RC1은 `top/bottom`, RC2/RC3은 `top/auto`를 유지했다.
- `param` 모드는 F2를 InvariantCulture 우선으로 파싱한다. `0 < F2 <= realH`일 때 치수 상단을 `minY + F2 × vScale`로 설정하고, 실제 치수값도 F2로 전달한다. 실패하면 전체 높이 치수로 폴백하며 진단 로그를 남긴다.
- `member-spike`에 ObjectId, Handle, 레이어, 색상 인덱스를 추가 기록했다. 세로 MEMBER 앵커 작도는 단계 B로 미구현이다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- `git diff --check` 통과.
- 정적 확인: F2 저장 사전의 초기화·캡처·읽기 경로, RC1/RC2/RC3 설정 행, `param`의 치수 형상과 F2 `realValue` 전달을 확인.
- 빌드 미실행: 사용자 요청이 없어 프로젝트 규칙에 따라 실행하지 않음.

## 라이브 검증 필요
- RC1/RC2/RC3 추출 후 `support params dump`의 F2, `dimV param`의 raw/real/topY, `dim append`의 `vMode=param`과 `sideMode`를 확인한다.
- `minY`가 베이스 플레이트 아래면인지 `dimV param` 로그의 minY/maxY 및 렌더로 검증한다. 어긋나면 보정 없이 실측값을 다음 REPORT에 기록한다.
- `member-spike`의 id/handle/layer/colorIndex로 세로 MEMBER를 원본 대응 가능한지 판정한다.

## 커밋
- 이 REPORT와 코드 변경을 동일 커밋으로 기록.
