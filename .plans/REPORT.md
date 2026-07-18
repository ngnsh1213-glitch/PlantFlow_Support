# REPORT - Codex -> Claude

- **cycle**: 72
- **status**: done
- **completed_at**: 2026-07-18
- **title**: GD2 부재 콜아웃 앵커/텍스트 재배치 — L=중앙수직재, C만 우측하단

## 변경 요약
- `AppendNotabProfileCallout`에서 GD2·2부재 조합만 별도 배치했다.
- idx0(L)는 중앙 수직재 몸통을 앵커로 하고 좌측 하단 텍스트를 유지한다.
- idx1(C)는 하단 수평재 우측을 앵커로 하고 우측 하단 텍스트를 배치한다.
- 단일 designation 및 GD2 외 다중 designation은 기존 균등 분할 로직을 유지한다.
- `PFS_NOTAB_MEMBER_CALLOUT_DX0/DY0/DX1/DY1`를 추가해 엘보·텍스트만 부재별 미세 조정할 수 있다. 앵커는 고정한다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `git diff --check -- PlantFlow_Support/Core/Commands.cs` 통과.
- 지정한 `AppendNotabProfileCallout` 루프 앵커·엘보·텍스트 산출부 외 제품 코드 변경 없음.
- 빌드는 프로젝트 규칙과 핸드오프 지시에 따라 실행하지 않았다.

## 라이브 확인 필요
- `dev_test.bat`을 `PFS_NOTAB_TEST_TAG=GD2-001`로 실행한다.
- 로그에서 `callout append(multi)`의 idx0 앵커가 `(centerX, minY+h*0.5)`, idx1 앵커가 `(minX+width*0.8, minY)`인지 확인한다.
- 결과 DWG에서 L 화살표/좌측 하단 텍스트와 C 화살표/우측 하단 텍스트를 확인한다.
- GD1·GD3은 기존 단일 designation 호출이 동일한지 회귀 확인한다.

## 커밋
- `e69b957` (`Fix GD2 member callout anchors`)
