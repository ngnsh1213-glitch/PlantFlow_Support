# REPORT — Codex → Claude

- **cycle**: 115
- **status**: completed
- **completed_at**: 2026-07-23
- **title**: 파이프 콜아웃 높이밴드 규칙(dyW) + RC5 출하 기본위치 config 승격

## 결과

- 파이프 콜아웃 자동배치에만 `PFS_NOTAB_PIPE_DY_W`를 추가했다. `GetEnvDouble(..., 0.25, 0.0, 10.0)`으로 읽으며, Free 통과 후보 비용에 `abs(candY-anchorY) * dyW`를 더한다. 유볼트·부재 콜아웃·밸룬은 0 가중치로 기존 비용식을 유지한다.
- 자동배치 진단은 `dy=`, `dyW=`, `baseCost=`, `cost=`를 기록한다.
- `NotabTypeConfig`에 `PipeCalloutDx`, `PipeCalloutDy`, `HasPipeCalloutPosition`을 추가하고 RC5에 `(100,20)` 출하 기본값을 등록했다.
- 파이프 위치 우선순위는 `env PFS_NOTAB_PIPE_POS_<TYPE>` → config 기본값 → 자동탐색이다. 수동 위치 경로는 cycle114의 탐색 우회·뷰포트 밖 폴백을 재사용하고 `sideSrc=env|config|auto`로 구분한다.

## 변경 파일

- `PlantFlow_Support/Core/NotabCalloutPlacer.cs`
- `PlantFlow_Support/Core/Commands.cs`

## 검증

- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.sln --no-restore` 통과: 오류 0, 경고 14.

## 라이브 검증 필요

- RC5 env 미설정에서 `sideSrc=config`와 앵커+(100,20) 위치를 확인.
- config/env을 임시로 비활성화한 실험에서 자동배치 로그의 `dy=`와 선택 위치를 확인.

## 커밋

- 대기 중
