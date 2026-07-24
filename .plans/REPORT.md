# REPORT — Codex → Claude

- **cycle**: 129
- **status**: completed
- **completed_at**: 2026-07-24
- **title**: RC1 F2/P1 밸룬 미세조정

## 적용 결과

- RC1에 F2 전체 이동 `(14,0)`과 P1 밸룬 전용 이동 `(-14,0)`을 독립 노브로 등록했다.
- 환경변수 `PFS_NOTAB_F2_SHIFT_<TYPE>`, `PFS_NOTAB_P1_BALLOON_POS_<TYPE>`가 config보다 우선하며, `dx` 또는 `dx,dy` 형식을 허용한다.
- 작도 직전 P1은 anchor를 유지한 채 밸룬/상자/touch만 이동하고, F2는 anchor·밸룬·상자·touch 전체를 평행이동한다.
- 이동 후 bounds 및 충돌을 재검증한다. 실패하면 `PFSNOTABDETAIL balloon-nudge ... revalidate=warn:<reason>`만 남기며 이동값을 되돌리지 않는다.
- 신규 노브가 0인 비RC1 타입은 기존 `MemberBalloonDx/Dy` 경로와 독립되어 동작이 변하지 않는다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증

- `git diff --check` 통과.
- `dotnet build .\\PlantFlow_Support.csproj --no-restore` — 오류 0, 기존 경고 14개.
- 라이브 검증 대기: `PFS_NOTAB_FLATTEN=1` + `RC1-001`에서 F2/P1의 `balloon-nudge` 로그 및 형상을 확인한다.

## 커밋

- `240b52e feat: nudge RC1 F2 and P1 balloons`
- push하지 않았다.
