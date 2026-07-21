# REPORT — Codex → Claude

- **cycle**: 100
- **status**: pending_verification
- **completed_at**: 2026-07-21
- **title**: 밸룬 2단계 탐색 + P1 앵커 원복 + 유볼트 리더 문자 중단 접속

## 변경 요약

- 밸룬은 기존 `PFS_NOTAB_BALLOON_MAX_STEPS`(기본 4)만 먼저 탐색하고, 자유 후보가 없을 때만 `PFS_NOTAB_BALLOON_EXT_STEPS`(신설, 기본 12, 1~32)까지 확장한다. 확장 단계는 최초 자유 거리 링만 점수화해 불필요하게 먼 후보 선택을 막았다.
- `P1`은 로컬 상자 외곽 보정을 제외하고 `rawAnchor`를 유지한다. F 계열은 기존 포트+로컬 상자 보정을 유지한다.
- 직접 콜아웃의 검사·MText·리더 종점·등록을 문자 좌우 변의 세로 중단점으로 통일했다. 유볼트도 이 경로를 사용하므로 리더가 문자 하단 대신 중단에 접속한다.
- 밸룬 로그에 `tier=normal|extended`, `extended=true|false`, 거리와 확장 실패 시 거절 사유를 기록한다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`

## 검증

- `git diff --check -- PlantFlow_Support/Core/Commands.cs` 통과.
- `dotnet build --no-restore` 통과: 오류 0, 경고 15 (허용 상한 15).

## 라이브 검증 필요

- `dev_test.bat` 후 F1/F2의 `balloon-draw`가 `tier=extended`로 기록되고 밸룬이 그려지는지 확인.
- P1은 `adjust=port`, `anchor=rawAnchor`로 기록되고 실제 부재를 가리키는지 확인.
- 유볼트 리더가 문자 세로 중앙에 직선 1개로 접속하는지 확인.

## 커밋

- `1f3a276` `fix: restore notab balloon placement`
