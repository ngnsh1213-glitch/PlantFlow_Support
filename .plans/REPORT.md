# REPORT - Codex → Claude

- **cycle**: 87
- **status**: done
- **completed_at**: 2026-07-20
- **title**: 콜아웃 장애물 소유자 예외 및 파이프 반경·방향 제어 교정

## 변경 요약
- 빈 소유자 장애물(서포트·치수)은 더 이상 부재 콜아웃에서 제외되지 않는다. 성공 진단에 `scanned`와 `obst`를 남긴다.
- 원본 도면에서 얻은 파이프 모델 반경을 정적 상태로 보존해 페이퍼 반경으로 변환한다. 해당 값이 없으면 상세 DB의 중심 인접 정사각 `Solid3d` 투영 폭을 폴백으로 사용한다.
- `PFS_NOTAB_CALLOUT_ANGLE_W`(기본 0.3)로 바깥 방향 이탈 비용을 추가했고, `PFS_NOTAB_DIR_<TYPE>_<PIPE|M#>` 환경 변수로 바깥 방향을 오버라이드할 수 있다.
- GD3 M0 앵커 기본값을 `0.305`로 조정했다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- `git diff --check` 통과.
- 변경 주변 정적 확인: 잘못된 `System.Min` 호출 없음, 방향 노브·진단 필드·파이프 장애물 로그 연결 확인.
- 빌드 미실행: 프로젝트 규칙에 따라 사용자의 명시 요청 없이 빌드를 실행하지 않음.

## 라이브 검증 필요
- 권장 env: `PFS_NOTAB_DIR_GD1_M0=0`, `PFS_NOTAB_DIR_GD2_PIPE=0`, `PFS_NOTAB_DIR_GD2_M1=-135`, `PFS_NOTAB_DIR_GD3_PIPE=-135`, `PFS_NOTAB_DIR_GD3_M0=-45`.
- GD1/GD2/GD3 추출 후 `pipe-obstacle box=... r=...`, `callout-draw`의 `out/fan/angleW/scanned/obst/dirSrc`, `tier=1` 또는 `FAIL` 여부를 확인한다.
- 서포트·치수·파이프 관통 없음, GD1 M0 우측, GD2 PIPE 우측·M1 좌하, GD3 PIPE 좌하·M0 우하를 확인한다.

## 커밋
- 코드: `9273143` (`fix: correct notab callout obstacle placement`)
