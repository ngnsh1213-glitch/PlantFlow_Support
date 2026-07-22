# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 112
- **status**: ready
- **issued_at**: 2026-07-22
- **title**: RC5 파이프콜아웃 기둥 box-only 회피(리더 교차 허용) + F2 자가간섭 해소
- **작업 경로**: `Core/Commands.cs`, `Core/NotabCalloutPlacer.cs`
- **진단 원장**: `.plans/notab_rc5_9_review_20260722.md`
- **기준 커밋**: `c8a0f1f`
- **자문**: 라이브 7차 + 사용자 결정. 근본 지오메트리 충돌.

## ⚠ 검증 필수
`dotnet build` 오류 0(경고 14 초과 금지). 미실행 시 커밋 금지·`status: blocked`. Claude가 REPORT 수령 후 직접 빌드 재확인.

## 배경 (라이브 7차 + 사용자 결정)
RC9·RC4·RC6·RC7·RC8 종결. **잔여=RC5 콜아웃+F2.**
- cycle111: 기둥 장애물이 hard-leader(`preserveVerticalMemberLeaderClearance`)라 파이프→우측 리더의 기둥 가로지름 금지 → 콜아웃이 **플레이트 위(Y424)로 상승**(악화). 게다가 그 장애물이 **F2 자가배치 방해**(clear 40.8→3).
- **근본**: 파이프(기둥 좌측 355.5)→개방공간(기둥 우측 x>407)은 리더가 기둥(402.5) 가로지름 불가피.
- **★사용자 결정(그린 이미지)**: 리더는 기둥 가로지르게 **허용**, 텍스트만 개방 우측에.

## 집도 지시

### 1) vertical-member 장애물을 box-only로 (hard-leader 되돌림)
- cycle110/111의 `preserveVerticalMemberLeaderClearance`(전 tier 리더교차 유지)를 **제거/off**. vertical-member 장애물은 **텍스트 상자 겹침만 차단**하고 **리더 교차는 허용**한다(일반 장애물처럼 tier에 따름).
- 효과: 파이프 콜아웃 텍스트는 기둥 상자를 피해 **개방 우측**에 놓이고, 리더는 파이프→텍스트로 기둥을 얇게 가로지른다(사용자 승인).
- `NotabCalloutPlacer`의 hard 옵션이 다른 데 안 쓰이면 관련 파라미터도 정리(호출부 기본 동작 불변 유지).

### 2) 기둥 장애물이 F2 자신의 밸룬 배치를 방해하지 않게
- 현재 vertical-member 장애물(S3=402.5)이 **F2 밸룬 자신의** free 검사도 막아 clear 40.8→3로 악화.
- 수정: F2(세로재 자기 자신) 밸룬 배치 시 **자기 기둥 장애물("vertical-member")은 제외**. 방법: (a)장애물을 파이프 콜아웃 placer에만 등록하고 밸룬 placer엔 미등록, 또는 (b)밸룬 free 검사에서 owner=="vertical-member" 제외. 밸룬-치수-콜아웃 다른 충돌은 유지.
- F2 밸룬은 기둥 근처(cycle109 위치)로 복귀해야 한다.

### 3) F2 리더 화살촉 축소 유지 (cycle111)
- `leaderArrow = Min(default, 리더길이×0.45)` 유지(밸룬 안 밀어냄, 화살촉만 축소). 변경 불필요, 회귀만 없게.

## 하지 말 것
- RC9·RC1~3·RC4~8·GD 회귀 금지. 치수·`Dimasz` 전역 변경 금지. R1 좌우규칙 변경 금지.
- 리더 교차를 **다른 장애물(치수·기존 콜아웃 상자)** 까지 무차별 허용하지 말 것 — vertical-member의 **리더** 교차만 허용(텍스트 상자 겹침은 계속 차단).
- 플레이트 장애물 신규 등록 금지(이번엔 기둥 box-only 효과 먼저 확인).

## 제약
- 빈 catch 금지. `#nullable disable`면 null 가드.

## 검증 (Codex, 필수)
1. `dotnet build` 오류 0, 경고 14 초과 금지.
2. `REPORT.md`에 콜아웃 새 elbow/endX/fan·F2 clear 복귀·빌드 결과. 라이브는 사용자.

## 성공 기준 (다음 라이브)
- 파이프 콜아웃 텍스트가 우측 개방공간(기둥/플레이트 미겹침), 리더는 파이프→텍스트로 기둥을 가로질러도 됨.
- F2 밸룬 기둥 근처 복귀(clear 회복), 리더 선분 보임.
- RC9·RC1~8·GD 회귀 0.
