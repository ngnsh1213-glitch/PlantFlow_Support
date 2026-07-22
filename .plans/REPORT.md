# REPORT — Codex → Claude

- **cycle**: 105
- **status**: completed
- **completed_at**: 2026-07-22
- **title**: 무탭 RC4·5·7·8 치수 교정 (가로 A+A1 / 세로 F2 / 세로 삭제 none)

## 변경 요약

- `rcMemberGeometry`에 RC4·RC5·RC7을 추가했다. RC4·RC5는 가로 A+A1과 세로 S2 앵커 경로를 사용하고, RC7은 가로 A/A1 분할 경로만 사용한다.
- `GetNotabTypeConfig`에 RC7·RC8의 `VerticalMode = "none"` 설정을 추가했다. 기존 RC4·RC5의 F2 설정은 이미 존재하여 중복 수정하지 않았다.
- `VerticalMode = "none"`이면 세로 값 계산·S2 앵커·세로 치수 엔티티 등록을 건너뛰고 `PFSNOTABDETAIL dimV skip: mode=none`을 남긴다. 가로 치수와 이후 주석 흐름은 유지한다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`

## 검증

- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.sln --no-restore` 통과: 오류 0, 경고 14.

## 라이브 검증 필요

- RC4: 가로 500 유지, 세로 F2=600.
- RC5: 가로 A+A1=650, 세로 F2=600.
- RC7: 가로 800/150 분할, `dimV skip: mode=none`, 세로 치수 없음.
- RC8: 가로 기존 유지, `dimV skip: mode=none`, 세로 치수 없음.
- RC6·RC9·RC1~RC3·GD1~GD3 및 밸룬 회귀 없음.

## 커밋

- 코드: 아래 Git 커밋 결과로 갱신.
