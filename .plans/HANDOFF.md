# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 115
- **status**: ready
- **issued_at**: 2026-07-22
- **title**: 파이프 콜아웃 높이밴드 규칙(dyW) + RC5 출하 기본위치 config 승격
- **작업 경로**: `Core/NotabCalloutPlacer.cs`(cost), `Core/Commands.cs`(NotabTypeConfig·env 우선순위)
- **진단 원장**: `.plans/notab_rc5_9_review_20260722.md`
- **기준 커밋**: `ed71948`
- **자문**: §9 Codex 1회(비용식·기본값 0.25·후보격자 통과·회귀·승격 위치 확정).

## ⚠ 검증 필수
`dotnet build` 오류 0(경고 14 초과 금지). 미실행 시 커밋 금지·`status: blocked`. Claude가 REPORT 수령 후 직접 빌드 재확인.

## 배경 (측정 완료, 재측정 불요)
- 수동 노브 `PFS_NOTAB_PIPE_POS_RC5=100,20`으로 RC5 콜아웃 목표 위치 확보(사용자 확정). 이 값 = 측정된 정답.
- 자동배치가 위로 도주한 근인 = **cost에 Y-편차 항 부재**(외곽초과+r×0.01뿐, angleW=0).
- 자문 확인: 후보 격자(fan 0/±15/±30, r 2.5스텝)가 r≈102·fan+15 = anchor+(98.6,26.4) 후보를 **이미 생성**(정답 근접). `PipeCalloutSide`는 #if false 죽은 경로라 충돌 없음.

## 집도 지시 (2가지)

### 1) 파이프 콜아웃 Y-편차 페널티 `PFS_NOTAB_PIPE_DY_W`
- `NotabCalloutPlacer.TryPlace`의 **Free() 통과 후 cost 계산 지점**에, **파이프 콜아웃에 한해**(호출부 `isPipeCallout`/ownerTag=="pipe" 스코프) 다음 항 추가:
  `cost += Math.Abs(candY - leaderFrom.Y) * pipeDyW`  (candY=텍스트 접속점 Y, leaderFrom=파이프 앵커)
- `pipeDyW = GetEnvDouble("PFS_NOTAB_PIPE_DY_W", 0.25, 0.0, 10.0)`. **기본 0.25**(자문: gap 이점 28.6). 0이면 현행 동작.
- 진단 로그에 `dy=`, `dyW=`, base cost를 함께 남길 것(다음 값 조정용).
- **부재/유볼트 콜아웃·밸룬에는 미적용**(파이프만).
- fan 보강(±10°)은 **이번엔 하지 말 것** — dyW만으로 재현 안 될 때 후속.

### 2) RC5 출하 기본위치를 `NotabTypeConfig`로 승격
- `NotabTypeConfig`에 `PipeCalloutDx`, `PipeCalloutDy`, `HasPipeCalloutPosition`(bool, 0,0과 미설정 구분) 추가.
- RC5 행에 `PipeCalloutDx=100, PipeCalloutDy=20, HasPipeCalloutPosition=true`. 타 타입 미설정.
- 파이프 콜아웃 배치 우선순위: **①env `PFS_NOTAB_PIPE_POS_<TYPE>`(현장 오버라이드) → ②config 출하 기본위치 → ③자동 TryPlace(dyW 포함)**. ①②는 기존 cycle114 수동 경로(탐색 우회·oob 폴백) 재사용, 소스만 다름. 로그 `sideSrc=env|config|auto` 구분.

## 하지 말 것
- cycle113(vertical-member 리더검사 제외)·cycle114(env 노브) 되돌리기 금지.
- F2·밸룬·치수·타 타입 변경 금지. fan 격자 변경 금지(후속).
- `PFS_NOTAB_CALLOUT_ANGLE_W` 재활용/변경 금지(±각 무차별이라 부적합, 자문 기각).

## 제약
- 빈 catch 금지. env 파싱 InvariantCulture. `#nullable disable`면 null 가드.

## 검증 (Codex, 필수)
1. `dotnet build` 오류 0, 경고 14 초과 금지.
2. `REPORT.md`에 dyW 적용 지점·config 필드·우선순위 로그 형식 기록. 라이브는 사용자.

## 성공 기준 (다음 라이브)
- RC5: env 미설정 시 config(100,20)로 동일 위치(`sideSrc=config`).
- 실험: config·env 다 끄면(임시) dyW=0.25 자동탐색이 같은 틈(anchor+(≈99,≈26))을 고르는지 — 로그 `dy=`로 확인. 안 되면 후속(fan ±10°).
- GD1~3·RC1~4/6~9 파이프 콜아웃 위치 불변(이미 낮은 dy라 무영향 예상). 회귀 0.
