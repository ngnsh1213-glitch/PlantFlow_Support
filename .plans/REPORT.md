# REPORT - Codex -> Claude

- **cycle**: 69
- **status**: done
- **completed_at**: 2026-07-17
- **title**: BOM 스파이크(계측) - BOMs.ContentsByDesignStd 무탭 컨텍스트 실측 준비

## 변경 요약
- `CaptureIsoSupportProfile(supportId)` 직후 `PFS_NOTAB_BOM_SPIKE` env 게이트를 추가했다.
- 게이트가 0.5 이상일 때만 `NotabBomSpike(supportId)`를 호출한다. 기본값은 off다.
- `NotabBomSpike`는 `BOMs(supportId, new List<AttachmentInfo>())`로 프레임 BOM만 계측하고, `std`, `rowCount`, 최대 40개 행의 컬럼 수/내용을 `PFSNOTABDETAIL bom ...` 로그로 남긴다.
- 전체 BOM 스파이크를 try/catch로 감싸 예외를 로그로만 남기며 무탭 디테일 흐름을 방해하지 않게 했다.
- 호출 위치를 프로파일 캡처 바깥으로 두어 GD2처럼 `BI`가 없고 config fallback으로 빠지는 경우도 BOM 파이프를 실측할 수 있게 했다.

## 변경 파일
- `PlantFlow_Support/Core/Commands.cs`
- `.plans/REPORT.md`

## 백업
- 별도 백업 파일 생성 없음. 변경 범위가 단일 코드 파일과 보고서이며 Git diff로 롤백 가능.

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `rg`로 `PFS_NOTAB_BOM_SPIKE`, `NotabBomSpike`, `bom spike` 반영 위치 확인.
- `git diff -- PlantFlow_Support/Core/Commands.cs` 확인: env 게이트와 로그 전용 헬퍼 추가만 포함.
- 빌드는 프로젝트 규칙에 따라 사용자 명시 요청이 없어 실행하지 않음.

## 커밋
- 코드 반영 커밋: `baa34a9` (`Add notab BOM spike`)
