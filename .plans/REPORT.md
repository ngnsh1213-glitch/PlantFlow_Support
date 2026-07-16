# REPORT - Codex -> Claude

- cycle: 50
- status: done
- commit: 최종 HEAD 해시는 Codex 완료 보고 참조
- title: 무탭 다중선택 배치 추출 명령 PFSNOTABBATCH

## 변경 요약
- `PlantFlow_Support/Core/Commands.cs`
  - 신규 명령 `PFSNOTABBATCH`를 추가.
  - 선택셋에서 `ObjectClass.Name`에 `Support`가 포함된 ObjectId만 수집.
  - 수집된 서포트를 직렬 반복하며 각 서포트마다 `RunNotabDetailPipeline(doc, new[] { supportId })`를 호출.
  - 반복별 `[i/N] tag=<SupportName>` 로그와 최종 `total/ok/fail/fails` 요약 로그를 추가.
  - 개별 서포트 실패가 배치 전체를 중단하지 않도록 반복 단위 try/catch를 추가.
  - `RunNotabDetailPipeline` 반환형을 `void`에서 `bool`로 변경하고, 저장 성공 시 `true`, 내부 catch 진입 시 `false`를 반환.
  - SupportName이 비어 있는 저장 태그 fallback을 초 단위 timestamp에서 support handle 기반으로 변경해 무명 다중 배치의 파일명 충돌을 완화.

## 보존 확인
- 대상 파일은 `PlantFlow_Support/Core/Commands.cs`만 수정.
- 단일 `PFSNOTABDETAIL`/`PFSNOTABTEST` 동작은 반환값 무시 형태로 유지.
- `RunNotabDetailPipeline` 내부 클립, held-pipe, 치수, 스케일, perspective guard, wireframe 흐름은 반환형 외에는 변경하지 않음.
- 범위 밖 기존 미추적 파일들은 건드리지 않음.
- 백업: `PlantFlow_Support/Core/Commands.cs.codex_bak_cycle50` 생성(커밋 제외).

## 검증
- 변경 주변 20줄 이상 수동 확인 완료.
- `rg -n "PFSNOTABBATCH|RunNotabDetailPipeline|CollectSelectedSupportIds|GetSupportNameForLog|GetNotabPrimarySupportHandle" PlantFlow_Support\\Core\\Commands.cs`: 참조 확인.
- `git diff --check -- PlantFlow_Support\\Core\\Commands.cs`: PASS(CRLF 안내만 있음).
- 빌드: 프로젝트 규칙상 사용자 명시 요청이 없어 실행하지 않음.

## 라이브 확인 포인트
- 여러 서포트를 선택한 뒤 `PFSNOTABBATCH` 실행.
- 기대 로그: `PFSNOTABBATCH [i/N] tag=... 처리`, 최종 `done total=N ok=K fail=M fails=[...]`.
- 기대 파일: `Details\\<tag>_notab.dwg`가 서포트별로 생성되고, 실패 항목은 목록으로만 보고되며 다음 항목 처리가 계속됨.
