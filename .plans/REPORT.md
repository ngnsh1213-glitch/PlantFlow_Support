# REPORT — Codex → Claude

- **cycle**: 114
- **status**: completed
- **completed_at**: 2026-07-22
- **title**: 파이프 콜아웃 타입별 수동 위치 노브

## 결과

`PFS_NOTAB_PIPE_POS_<TYPE>=dx,dy`를 추가했다. 값은 파이프 앵커 기준 paper 단위이며, `+X=우`, `+Y=상`, 허용 범위는 각 ±2000이다.

유효한 값이 있으면 파이프 콜아웃의 텍스트 접속점(`endX`, `middleY`)을 앵커+오프셋으로 직접 지정하고 `TryPlace` 탐색을 건너뛴다. 좌우는 `dx` 부호로 결정하며 로그에는 `sideSrc=env`가 남는다. 리더 elbow/tail 작도와 장애물 등록은 기존 직접 작도 경로를 그대로 사용한다.

파싱 실패·미설정은 자동배치를 유지한다. 수동 위치의 실제 문자 상자가 뷰포트 밖이면 `callout-skip reason=env-pos-oob`를 기록한 뒤 자동배치로 폴백한다. 수동 위치는 충돌 검사를 하지 않는다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`
- `dev_test.bat` — `rem set "PFS_NOTAB_PIPE_POS_RC5=100,20"` 참고 주석 추가(기존 사용자 변경 파일이므로 커밋 제외)

## 검증

- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.sln --no-restore` 통과: 오류 0, 경고 14.

## 라이브 검증 필요

- `PFS_NOTAB_PIPE_POS_RC5=100,20` 등의 값을 설정해 RC5 콜아웃 접속점과 `sideSrc=env` 로그를 확인.
- 뷰포트 밖 값으로 `env-pos-oob` 로그와 자동배치 폴백을 확인.

## 커밋

- 코드: 아래 커밋 해시 참조.
