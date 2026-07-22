# REPORT — Codex → Claude

- **cycle**: 106
- **status**: completed
- **completed_at**: 2026-07-22
- **title**: RC7 A/A1 분할 + RC5 F1·F2 앵커 교정 + RC9 P1 확장 배치

## 변경 요약

- RC7의 `params(A+A1)` 분할은 파이프 중심 전용 최소 간격 가드를 우회한다. A=800/A1=150처럼 짧은 A1도 분할 치수로 작도한다.
- RC5의 F1 앵커를 `PPorts[0]`(하단 가로재 위 기준점), F2 앵커를 `PPorts[1]`(S2 세로 기둥 자유단)으로 교정했다. `Support/RC5/RC5.py`에서 포트 생성 순서가 datum(PPorts[0]) → F1 끝점(PPorts[1]) → F2 상단(PPorts[2])임을 확인했고, PPorts[0]은 하단 가로재 내부 점이므로 F1에 선택했다.
- RC9을 포함한 다중 플레이트의 P1 밸룬만 `PFS_NOTAB_P1_EXT_STEPS`(기본 24, 범위 1~48)까지 확장 탐색한다. 기존 F 계열·기타 밸룬 탐색 범위는 유지한다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`
- `PlantFlow_Support/Ortho/StandardSupport.cs`

## 검증

- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.sln --no-restore` 통과: 오류 0, 경고 14.

## 라이브 검증 필요

- RC7: `PFSNOTABDETAIL dim append` 로그의 `split=(800,150)` 및 치수 표시 확인.
- RC5: `balloon-anchor`/`balloon-draw` 로그에서 F2 `p0`이 S2와 일치하고, F1 `p0`이 하단 가로재 위인지 확인.
- RC9: `P1_0`·`P1_1` 두 항목 모두 `balloon-draw`, `balloon-skip key=P1_* reason=no-space` 0건 확인.
- RC1~RC3·GD 등 기존 밸룬 회귀 없음 확인.

## 커밋

- 코드: `f9b14f5` (`fix: correct RC5 RC7 RC9 notab annotations`)
