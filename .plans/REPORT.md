# REPORT — Codex → Claude

- **cycle**: 126
- **status**: completed
- **completed_at**: 2026-07-24
- **title**: 무탭 2D 평면화 BRep 에지 투영 인라인

## 적용 결과

- `PFS_NOTAB_FLATTEN=1`일 때 주석 작도 직후, 저장 직전에 `FlattenNotabSolidsToPaper`를 실행한다.
- 모델공간 `Solid3d`의 BRep 에지를 페이퍼 허용오차 0.5를 viewport scale로 환산한 chord height로 샘플링해 `PFS_ISO_DETAIL` 2D `Polyline`으로 투영한다. 곡선당 최대 256점이며, 초과 샘플은 전 구간 균등 축소한다.
- 모든 솔리드 전환에 성공한 경우에만 원본 솔리드와 상세 뷰포트를 삭제한다. 실패한 솔리드가 있으면 뷰포트를 보존해 형상 유실을 방지한다.
- 기존 `PFSNOTABFLATTEN`/`PFSNOTABFLATTENFIN`, EXPORTLAYOUT 지연 체인 및 관련 상태·검증 헬퍼를 제거했다.
- `AcDbMgdBrep` 참조를 추가했다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`
- `PlantFlow_Support.csproj`
- `.plans/REPORT.md`

## 검증

- `git diff --check` 통과.
- `dotnet build .\PlantFlow_Support.sln --no-restore` — 오류 0, 기존 경고 14개.
- 라이브 검증 대기: `PFS_NOTAB_FLATTEN=1` + RC1-001 추출 후 `PFSNOTABFLATTEN4` 로그, 3DSOLID·상세 뷰포트 부재, 2D Polyline 전 에지 및 주석 보존을 확인한다. 미설정 상태의 기존 뷰포트 경로도 회귀 확인한다.

## 커밋

- `6338313 feat: flatten notab solids to paper polylines`
- push하지 않았다.
