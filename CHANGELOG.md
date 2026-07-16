# CHANGELOG

이 프로젝트의 사람이 읽는 변경 이력 요약. 상세는 git 커밋 로그 참조.

## [Unreleased]
### Added
- 무탭 held-pipe 선택 = 서포트 BOP 표고 매칭(cycle44~46, 1cb3192~b0e5c71): 서포트가 실제 잡는 배관만 포함. 같은 라인 평행 배관은 라인/rect/중심거리로 불가 → 서포트 `BOP`(배관 밑면표고)로 `|(pipeCenterZ−외경반경)−BOP|` 최소 선택. env `PFS_NOTAB_BOP_TOL`/`PFS_NOTAB_CONTACT_TOL`. BOP 없으면 기하 폴백+경고 — 2026-07-16
- 무탭 서포트 영역 클립+동적 피팅(cycle43, f5473d8): oriented 클립박스 Solid3d Boolean INTERSECT로 관통 파이프 길이 트림(10m→서포트깊이)·이웃 배제, 고정 1:4→콘텐츠 동적 피팅. env `PFS_NOTAB_CLIP_MARGIN`/`PFS_NOTAB_FIT_PAD` — 2026-07-16
- 무탭 뷰포트 와이어프레임 기본화(999e1f9): 모델영역 Hidden→와이어프레임. Hidden은 `PFS_NOTAB_USE_HIDDEN=1` opt-in — 2026-07-16
- 무탭 perspective 방어 가드(6e01a9b): 추출 후 AutoCAD 리본 WPF 바인딩이 유휴 시점에 `PERSPECTIVE=1`로 역기입하는 현상을, 파이프라인 진입 8초 창 내 1회 복원+Idle REGEN으로 취소. env `PFS_PERSP_GUARD_SEC`. 진단 명령 `PFSPERSPPROBE`/`PFSPERSPWATCH`(스택 계측) — 2026-07-16
- 무탭 자동포함 접촉 판정(73b9974): `AutoIncludeRelatedParts` 마진을 서포트 크기비례(0.5×max) → 고정 tol 150mm(env `PFS_NOTAB_MARGIN`)로 교체. 큰 서포트에서 이웃 서포트가 2D에 묻어나오던 결함 해소, 유볼트/관통파이프만 포착 — 2026-07-16
- 격리 Main 뷰 가로 치수 파이프중심 분할(B4d, f6c99fa): 파이프 원 중심 X 기준 좌/우(예 100/200) + 전체(300). `IsoCircleCandidate.CenterX`·`pipeCenterX` 노출 — 2026-07-13
- 격리 재실행 중복 detail 제거(B4e-A, cf44403): clone-back 직전 기존 `PFS_ISO_DETAIL`/`AUTO_DIM` 삭제(`PurgePriorIsoDetail`) → 태그당 최신 1개만 — 2026-07-13
- 프로젝트 문서 규칙 도입: `CLAUDE.md`, `AGENTS.md`, `SESSION.md`, `TODO.md`, `CHANGELOG.md` (Claude/Codex 공용 상태 관리). PFO에서 PFS를 별도 프로젝트로 분리 — 2026-07-11

### Known Issues
- 격리 파이프라인 후 뷰큐브 미표시(B4e-B REGEN 불충분, 뷰 전환 시 복귀). 후속 보류 — 2026-07-13
- PFS 오쏘 B1c-Main: 서포트 선택→S1 길이방향 Main 뷰 (PFSVBSUPPORT, 46d87da)
- PFS 오쏘 B1b: Main+Top+ISO 다중 뷰 번들 생성 (PFSVBMULTI, e465ecb)

### Notes
- VIEWBASE 뷰생성 커널 완성(PhaseA/B1b/B1c-Main). 다음 단계 = 격리→멀티뷰→B2 치수.
