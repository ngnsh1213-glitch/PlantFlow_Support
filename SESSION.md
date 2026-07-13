# SESSION — 현재 작업 상태

_최종 갱신: 2026-07-13 (B4d+B4e 라이브 PASS)_

## 진행 중 트랙
- **PFS 오쏘 격리(1서포트=1도면) — B4d(가로 분할 치수)·B4e(중복제거) 라이브 PASS (2026-07-13)**.
  - ✅ 완료: B1g(VIEWBASE 커널)·B2(EXPORTLAYOUT+clone-back)·B3a(방향 Main)·B4a(치수 폭300/높이75)·B4c(세로 치수 서포트만)·**B4d·B4e**.
  - **B4d(커밋 f6c99fa, 라이브 PASS)**: 가로 치수를 파이프 원 중심 X 기준 좌/우 분할(예 100/200) + 전체(300) 유지. `IsoCircleCandidate.CenterX` 추가, `TryGetIsoSupportPaperExtents`가 `pipeCenterX` 노출. 라벨=`s_isoRealWidth` 비례 안분.
  - **B4e(커밋 cf44403, 라이브 PASS)**: (A) 재실행 시 기존 `PFS_ISO_DETAIL`/`AUTO_DIM` 레이어 객체 삭제(`PurgePriorIsoDetail`) → 최신 1개만. 라이브 `priorDetail purge erased=N` 확인, 중복 100 해소. (B) 원본 재활성 후 `REGEN` 큐잉.
  - **★미해결(나중): 뷰큐브 미표시** — B4e의 REGEN으론 부족(뷰 전환해야 표시). 다른 방식 필요(뷰/시점 리셋 등). 사용자 지시로 후순위 보류.
  - 잔여 노이즈(무해): `LogIsoDimensionExtents` `eInvalidExtents h=25` 로깅만 실패(치수 생성엔 영향 없음).
  - 데이터: PLN 미부여(빈값), BOP=423, size=80, realW=300/realH=75.
  - 상세: `plan_pfs_iso_b4d_dimsplit_20260713.md`, `plan_pfs_iso_b4e_dedup_viewcube_20260713.md`. 핸드오프 `.plans/HANDOFF.md`(cycle 9)·`HANDOFF_B4d.md`·`HANDOFF_B4e.md`(RESULT done).
- (종료) **핫 리로드 스파이크** — 리포 밖 `C:\Lisp\HotReload`로 이관 완료(커밋 4447a17). HANDOFF.md는 B4e(cycle 9)로 회수됨. 더 이상 사용 안 함.
- (이전) **격리 방향 제어 — ✅ B3a PASS (2026-07-12)**.
  - 파이프라인: 선택셋 → 1st temp(Plant clone) → explode Solid3d → 2nd temp(순수 solid) → **-VPOINT(파이프축)** → SendStringToExecute VIEWBASE `_O _Current` → EXPORTLAYOUT 평면화 → 원본 clone-back(PFS_ISO_DETAIL 레이어).
  - **방향 제어(B3a)**: -VPOINT(파이프축)+VIEWBASE `_O _Current`로 Main뷰(파이프=원+서포트=직사각형) 생성. X/Y/수직Z 전 축 + 다중 타입 육안 확정. "축방향 응시=Main" 보편 적용.
  - 커밋: e7ffa14(B1g), 7a7653d(B2a), cc6602e(B2b), cbe738c(B3a).
  - 후속(별도 트랙): 스케일 보정(viewport 1:100→실크기), 치수(평면화 2D 후처리 Main만), 배치 오프셋, -0 표기 정규화(경미), EXPORTLAYOUT open-prompt 억제(세션당 1회, 낮음).
- (이전) **격리 VIEWBASE 커널 — ✅ B1g PASS**, **B2b clone-back PASS**.
  - 목표 출력: 4뷰(DOWN/WEST=Main/SOUTH/ISO) + 치수는 Main에만 + BOM표 + 밸룬 + 위치콜아웃 + 타이틀블록 (caetech.vn 사양).
  - 설계철학: 뷰 = Pipe Axis 기준. Main = 서포트 길이방향(탑 제외), viewDir∥파이프 → 서포트 직사각형 + 파이프 원. 치수는 Main에만.

## 직전 실측 사실 (2026-07-12) — 격리 VIEWBASE 커널 완성
- **B1g ✅ PASS**: 격리 파이프라인 3회 결정적 재현. `PFSVBISODONE newEntities=4`(base+projected), eInvalidInput 소멸.
- 전 파이프라인: 선택셋 → 1st temp WblockClone(Plant 지오 보존, proxy=0) → explode Solid3d id수집 → 2nd temp WblockClone(순수 solid만) → Idle 게이트(active/CMDNAMES) → **SendStringToExecute VIEWBASE _E** → sentinel(PFSVBISODONE) 완료판정 → Idle close.
- ★핵심 교훈: VIEWBASE(무거운 model-doc UI 명령)는 갓 Open한 문서서 `ed.Command`(직접호출) 불가 → **`SendStringToExecute`(명령 큐=타이핑 등가) + sentinel 명령 체이닝**으로 구동/완료판정. B1b~B1f 실패가 이 하나로 수렴.
- Plant 객체는 caller가 Erase 불가(eCannotBeErasedByCaller) → 순수 solid만 2nd temp에 clone(M3). transient solid 직접 cross-DB append 금지 → WblockCloneObjects만.
- 상세: `plan_pfs_isolation_20260711.md`(§4-A~P), 메모리 `pfs-wblockclone-preserves-plant-geometry`.

## 대기 중 액션
- 없음. B4d+B4e 라이브 PASS로 종결.

## 다음 결정 분기
- **뷰큐브 미표시(보류)**: REGEN 불충분 확인. 후속 소분기에서 뷰/시점 리셋 등 다른 방식 검토.
- **후보 트랙**: (a) 별도 도면 분리(태그당 최신 1개 — B4e 정합) (b) BOP MLeader(B4b) (c) 스케일/배치 (d) eInvalidExtents 로깅 노이즈 정리.

## 관련 파일·경로
- 잔여 백로그: `TODO.md`
- 변경 이력: `CHANGELOG.md`
- 상세 설계철학·목표사양은 프로젝트 메모리(`project_pfs_design_philosophy_core`, `project_pfs_target_output_spec_caetech`) 참조.
