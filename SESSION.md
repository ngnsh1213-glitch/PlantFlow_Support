# SESSION — 현재 작업 상태

_최종 갱신: 2026-07-12_

## 진행 중 트랙
- **PFS 오쏘 격리(1서포트=1도면) — ✅ B2b PASS = 전 파이프라인 완성 (2026-07-12)**.
  - 선택셋 → 1st temp(Plant clone) → explode Solid3d → 2nd temp(순수 solid) → SendStringToExecute VIEWBASE → EXPORTLAYOUT 평면화 → 원본 clone-back(PFS_ISO_DETAIL 레이어). "1서포트 → 격리 2D 디테일 → 원본 도면" 결정적 동작.
  - 커밋: e7ffa14(B1g), 7a7653d(B2a), cc6602e(B2b).
  - 후속(별도 트랙): 스케일 보정(viewport 1:100→실크기), 배치 오프셋/멀티서포트, EXPORTLAYOUT open-prompt 억제(세션당 1회, 낮음), 방향제어(-VPOINT, Main=파이프축), 치수(평면화 2D 후처리 Main만).
- (이전) **격리 VIEWBASE 커널 — ✅ B1g PASS (2026-07-12)**.
  - 목표 출력: 4뷰(DOWN/WEST=Main/SOUTH/ISO) + 치수는 Main에만 + BOM표 + 밸룬 + 위치콜아웃 + 타이틀블록 (caetech.vn 사양).
  - 설계철학: 뷰 = Pipe Axis 기준. Main = 서포트 길이방향(탑 제외), viewDir∥파이프 → 서포트 직사각형 + 파이프 원. 치수는 Main에만.

## 직전 실측 사실 (2026-07-12) — 격리 VIEWBASE 커널 완성
- **B1g ✅ PASS**: 격리 파이프라인 3회 결정적 재현. `PFSVBISODONE newEntities=4`(base+projected), eInvalidInput 소멸.
- 전 파이프라인: 선택셋 → 1st temp WblockClone(Plant 지오 보존, proxy=0) → explode Solid3d id수집 → 2nd temp WblockClone(순수 solid만) → Idle 게이트(active/CMDNAMES) → **SendStringToExecute VIEWBASE _E** → sentinel(PFSVBISODONE) 완료판정 → Idle close.
- ★핵심 교훈: VIEWBASE(무거운 model-doc UI 명령)는 갓 Open한 문서서 `ed.Command`(직접호출) 불가 → **`SendStringToExecute`(명령 큐=타이핑 등가) + sentinel 명령 체이닝**으로 구동/완료판정. B1b~B1f 실패가 이 하나로 수렴.
- Plant 객체는 caller가 Erase 불가(eCannotBeErasedByCaller) → 순수 solid만 2nd temp에 clone(M3). transient solid 직접 cross-DB append 금지 → WblockCloneObjects만.
- 상세: `plan_pfs_isolation_20260711.md`(§4-A~P), 메모리 `pfs-wblockclone-preserves-plant-geometry`.

## 대기 중 액션
- 없음 (B2 착수 대기).

## 다음 결정 분기
- **B2**: 2nd temp VIEWBASE Layout → EXPORTLAYOUT → 2D를 원본 target으로 clone-back(PFSPLACEEXPORT 재활용).
- **방향 제어**: Main뷰=파이프축 → VIEWBASE 전 `-VPOINT` 세팅(설계 [MODEL→-VPOINT→VIEWBASE]). 현재는 기본 방향.
- **base/projected 정제**: newEntities=4(base+projected 자동) → 사양 뷰 구성 조정.

## 관련 파일·경로
- 잔여 백로그: `TODO.md`
- 변경 이력: `CHANGELOG.md`
- 상세 설계철학·목표사양은 프로젝트 메모리(`project_pfs_design_philosophy_core`, `project_pfs_target_output_spec_caetech`) 참조.
