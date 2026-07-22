# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 114
- **status**: ready
- **issued_at**: 2026-07-22
- **title**: 파이프 콜아웃 타입별 수동 위치 노브(자동탐색 우회) — RC5 종결용
- **작업 경로**: `Core/Commands.cs` (파이프 콜아웃 배치 호출부)
- **진단 원장**: `.plans/notab_rc5_9_review_20260722.md`
- **기준 커밋**: `03f30c2` 이후 최신
- **자문**: 불요 — 자동배치 튜닝 9사이클 비수렴으로 결정론적 노브 전환(사용자 합의).

## ⚠ 검증 필수
`dotnet build` 오류 0(경고 14 초과 금지). 미실행 시 커밋 금지·`status: blocked`. Claude가 REPORT 수령 후 직접 빌드 재확인.

## 배경
- RC5 파이프 콜아웃 자동배치가 cycle 106~113 동안 비수렴(위쪽/플레이트 옆을 전전). 심미적 목표 위치는 비용함수 튜닝으로 예측 불가 판정.
- 결정: **타입별 env 노브로 위치를 직접 지정**하고 자동탐색을 우회한다. 기존 관용구(`PFS_NOTAB_DIR_<TYPE>_*` 타입별 env, `GetEnvDouble`) 재사용.
- cycle113의 vertical-member 리더검사 제외는 **유지**(노브 미설정 타입의 자동배치 개선분).

## 집도 지시 (한 가지)

### 파이프 콜아웃 수동 위치 노브 `PFS_NOTAB_PIPE_POS_<TYPE>`
- 형식: `PFS_NOTAB_PIPE_POS_RC5=dx,dy` — **파이프 앵커 기준 상대 오프셋**(paper 단위, +X=우, +Y=상, 범위 ±2000). 파싱 실패/미설정이면 현행 자동배치.
- 동작: 현재 `standardName`에 해당 노브가 있으면 **TryPlace 탐색을 건너뛰고** 텍스트 접속점(endX/middleY)을 `anchor+(dx,dy)`로 직접 배치. 리더/elbow/tail 작도는 기존 관용구 그대로(접속점만 지정값). 좌우(side)는 dx 부호로 결정, R1 로그는 `sideSrc=env`로 남김.
- **충돌검사 없이 그린다**(수동 지정 = 사용자 책임). 단 뷰포트 밖(oob)이면 `callout-skip reason=env-pos-oob` 로그 후 자동배치로 폴백.
- 장애물 등록(`CommitBox` 등)은 기존과 동일하게 수행(후속 밸룬이 피하도록).
- 다른 타입·노브 미설정 시 동작 불변.

### dev_test.bat 예시 주석(선택)
- `rem set "PFS_NOTAB_PIPE_POS_RC5=100,20"` 형태 주석 1줄 추가(참고용). 실값 설정은 사용자.

## 하지 말 것
- 자동배치 로직 추가 튜닝 금지(이번 사이클은 노브만). cycle113 리더검사 제외 되돌리기 금지.
- F2(해결됨)·타 타입·치수 변경 금지.

## 제약
- 빈 catch 금지. env 파싱은 InvariantCulture, 실패 시 FileDiag 후 자동배치.

## 검증 (Codex, 필수)
1. `dotnet build` 오류 0, 경고 14 초과 금지.
2. `REPORT.md`에 노브 이름·의미(기준점·부호)·폴백 조건 명기. 라이브는 사용자.

## 성공 기준 (다음 라이브)
- `PFS_NOTAB_PIPE_POS_RC5` 설정 시 콜아웃이 지정 위치에 정확히 작도(`sideSrc=env` 로그).
- 사용자가 dev_test.bat 숫자 조정만으로(재빌드 없이) 원하는 자리에 수렴.
- 노브 미설정 타입 회귀 0.
