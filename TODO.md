# TODO — 잔여 작업 백로그

_완료 항목은 여기서 지우고 `CHANGELOG.md`로 옮긴다._

## 진행 중 / 다음 (VIEWBASE 오쏘 파이프라인)
- [ ] **격리**: 서포트 + 관통 파이프 구간만 추출 (현재 `_E` 상태)
- [ ] **멀티뷰**: Main + Top + ISO 격리된 대상으로 재생성
- [ ] **B2 치수**: 평면화 후처리로 실mm 치수, **Main 뷰에만**

## 미해결 / 후속
- [ ] test1 뷰 뒤집힘 (content-rotation) 해소
- [ ] Main 탑뷰 배제 (B3)
- [ ] 경사(slope) 후속 처리

## 완료 (요약, 상세는 CHANGELOG)
- [x] VIEWBASE 뷰생성 커널: PhaseA(EXPORTLAYOUT→실2D), B1b(Main+Top+ISO 번들), B1c-Main (2026-07-10)
- [x] 오쏘 다중뷰 엔진 + 신형 UI (3각법 동시추출 실증)
- [x] Preview B 서버사이드 메시 추출 (Support→Explode→Solid3d→SubDMesh)
- [x] UI 셸 마이그레이션 Phase 0, Catalog P4 (WebView 실탭 교체)
