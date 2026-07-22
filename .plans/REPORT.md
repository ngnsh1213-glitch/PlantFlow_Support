# REPORT — Codex → Claude

- **cycle**: 111
- **status**: completed
- **completed_at**: 2026-07-22
- **title**: RC5 파이프콜아웃 기둥회피(장애물 X 교정) + F2 리더 가시화(밸룬 유지)

## 결과

1. **RC5 기둥 장애물**
   - `vertical-member` 상자를 치수 S2 축 대신 RC5 F2의 실제 S3(PPorts[2]) 포트 투영값으로 만들었다.
   - F2 포트 또는 지원 범위 내 세로 span이 없으면 해당 장애물을 등록하지 않는다.
   - 라이브 로그에 `vertical-member source=RC5-F2-port=... x=... spanY=...`를 남긴다. 콜아웃 실제 elbow/endX/fan은 라이브 추출 후 확인한다.

2. **RC5 F2 리더**
   - cycle110의 밸룬 중심 거리 확장을 되돌려 기존 `radius + gap + step*k` 배치를 유지했다.
   - 세로재 리더에만 화살표 크기를 실제 노출 리더 길이의 45% 이하로 축소했다. 전역 `Dimasz`와 치수는 변경하지 않았다.
   - 라이브 로그에 `leaderArrow=`를 남긴다.

## 변경 파일

- `PlantFlow_Support/Core/Commands.cs`

## 검증

- `git diff --check` 통과.
- `dotnet build PlantFlow_Support.csproj --no-restore` 통과: 오류 0, 경고 14.

## 라이브 검증 필요

- RC5 파이프 콜아웃의 `elbow/endX/fan`과 기둥·플레이트 미관통 여부.
- RC5 F2 밸룬이 기둥 근처에 유지되며 리더 선분이 보이는지.
- RC1~3, RC4·6~9, GD 회귀 여부.

## 커밋

- 코드: 기록 예정
