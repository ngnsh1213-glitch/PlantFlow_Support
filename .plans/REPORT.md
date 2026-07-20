# REPORT - Codex → Claude

- **cycle**: 90
- **status**: done
- **completed_at**: 2026-07-20
- **title**: RC 가로 치수 SupportParams + 세로 MEMBER 앵커 실측화

## 변경 요약
- `CollectNotabMemberBoxes()`가 단일 트랜잭션에서 모델 솔리드를 수집·투영하고, 기존 `member-spike` 로그를 유지한다. DTO에는 값과 `ObjectId`만 보관한다.
- RC1/RC2/RC3의 가로 치수는 InvariantCulture로 `A + A1`을 읽고, `A2`(없으면 `A1`)로 좌·우 분할을 정한다. 보조선도 가장 넓은 부재 박스의 우측 끝을 기준으로 같은 폭에 맞춘다.
- 파라미터·투영·부재 박스가 유효하지 않으면 기존 `s_isoRealWidth` 경로로 폴백하며 원인을 로그에 기록한다. GD 계열은 새 결과를 소비하지 않는다.
- RC 세로 부재 앵커는 파이프 최단거리 후보를 제외한 뒤 페이퍼 박스 세로 종횡비 상대 최댓값을 사용한다. 동률·비세로 후보면 기존 추정식으로 폴백한다.
- 기본 가로 치수 오프셋을 `txt * 1.5`에서 `txt * 2.0`으로 상향했다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- `git diff --check` 통과.
- 정적 확인: 새 메서드 호출·시그니처 일치, RC 전용 소비 조건, GD 기존 분기 보존을 확인.
- 빌드 미실행: 프로젝트 규칙에 따라 사용자가 수동으로 `dev_test.bat`을 실행해야 한다.

## 라이브 검증 필요
- RC1/RC2/RC3에서 `dimH source=params(A+A1)` 및 450/450/500, 분할 350/100·250/200·250/250을 확인.
- `member-anchor source=member-geometry` 로그와 화살표의 세로 MEMBER 몸통 접촉을 확인.
- GD1/GD2/GD3에서 기존 치수·다중 designation 앵커가 유지되는지 확인.

## 커밋
- 이 REPORT와 코드 변경을 동일 커밋으로 기록.
