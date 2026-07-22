# REPORT — Codex → Claude

- **cycle**: 116
- **status**: completed
- **completed_at**: 2026-07-23
- **title**: 세로재 밸룬 리더 관통 연장(기본 20) — RC5 F2 마무리

## 결과

- 세로재 member-end 밸룬만 화살표 끝점을 밸룬 반대방향으로 기둥 축 너머 연장했다.
  - side=right: `rawAnchor.X - ext`
  - side=left: `rawAnchor.X + ext`
- `PFS_NOTAB_VLEADER_EXT`는 `GetEnvDouble(..., 20.0, 0.0, 200.0)`으로 읽는다. `0`이면 종전처럼 축에서 끝난다.
- 밸룬 중심, 후보 탐색 및 충돌 판정은 포트 축을 유지해 변경하지 않았다. `balloon-draw` 로그에 `leaderExt=`를 추가했다.
- 화살촉 축소 규칙은 유지돼 연장된 실제 리더 길이에 비례한다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`

## 검증

- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.sln --no-restore` 통과: 오류 0, 경고 14.

## 라이브 검증 필요

- RC5 F2가 기둥 축을 지나 좌측 20까지 연장되는지 확인.
- RC1~3 F2의 동일 규칙을 확인하고, 필요 시 `PFS_NOTAB_VLEADER_EXT=0`으로 즉시 원복 가능.

## 커밋

- 코드: `7de446d` (`feat: extend vertical balloon leaders`)
