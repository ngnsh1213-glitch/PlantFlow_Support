# REPORT - Codex -> Claude

- cycle: 49
- status: done
- commit: 최종 HEAD 해시는 Codex 완료 보고 참조
- title: 무탭 가로치수 배관 중심 기준화

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - `s_isoPipeCenterWcs/s_isoPipeCenterValid` 전역을 추가하고 NOTAB 진입 시 초기화.
  - held pipe 확정 직후 원본 DB에서 `TryGetPipeAxisFromId`로 배관 축상 중심 WCS를 캡처.
  - `TryComputeNotabDimPaperGeometry`가 배관 중심 WCS를 paper 좌표로 투영해 `pipeCenterX/Y`를 반환하도록 확장.
  - 가로 분할 기준을 support bbox center가 아니라 실제 배관 중심 X paper 좌표로 교체.
  - 가로 치수는 배관 paper Y가 support 중심보다 아래면 하단, 아니면 상단에 배치.
  - 분할 폭이 `min(txt*2, Dimasz*2)` 미만이면 분할 치수를 생략하고 총폭만 생성.
  - `dim append` 로그에 `side`, `pipeCenterX/Y`, `centerY`, `splitGuard`를 추가.

## 보존 확인
- 대상 파일은 `PlantFlow_Support/Core/Commands.cs`만 수정.
- 세로 치수는 기존 `verticalX=minX-offset` 및 `CreateVerticalDimension` 좌표를 유지.
- 클립, held-pipe 선택, 스케일, perspective guard, wireframe 경로는 수정하지 않음.
- 범위 밖 기존 변경(`dev_test.bat`, 미추적 파일들)은 건드리지 않음.
- 백업: `PlantFlow_Support/Core/Commands.cs.codex_bak_cycle49` 생성(커밋 제외).

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `rg -n "TryComputeNotabDimPaperGeometry|AppendNotabPaperDimensions\\(|s_isoPipeCenter|pipeCenterYPaper|splitGuard" PlantFlow_Support\\Core\\Commands.cs`: 참조 확인.
- `git diff --check -- PlantFlow_Support\\Core\\Commands.cs`: PASS(CRLF 안내만 있음).
- 빌드: 프로젝트 규칙상 사용자 명시 요청이 없어 실행하지 않음.

## 라이브 확인 포인트
- `PFS_NOTAB_TEST_TAG=RC1-001` 라이브 실행.
- 기대 로그: `PFSNOTABDETAIL dim append ... side=bottom ... pipeCenterX(paper)=... pipeCenterY(paper)=... splitGuard=False`.
- 기대 형상: 분할 값이 225/225가 아니라 실제 배관 중심 비율로 나오고, RC1 가로 치수가 하단에 배치되며 세로 치수는 좌측 유지.
