# SESSION — 현재 작업 상태

_최종 갱신: 2026-07-11_

## 진행 중 트랙
- **PFS 오쏘 뷰 생성 파이프라인 (VIEWBASE 커널)**.
  - 목표 출력: 4뷰(DOWN/WEST=Main/SOUTH/ISO) + 치수는 Main에만 + BOM표 + 밸룬 + 위치콜아웃 + 타이틀블록 (caetech.vn 사양).
  - 설계철학: 뷰 = Pipe Axis 기준. Main = 서포트 길이방향(탑 제외), viewDir∥파이프 → 서포트 직사각형 + 파이프 원. 치수는 Main에만.

## 직전 실측 사실 (2026-07-10~11)
- VIEWBASE 뷰생성 커널 완성: PhaseA(EXPORTLAYOUT→실2D 이송)·B1b(Main+Top+ISO 번들)·B1c-Main(서포트 선택→S1→Main) ✅.
- 직렬화 관용구 = `[모델탭 전환 → SetImpliedSelection → VIEWBASE]` (단 `_.MODEL`은 pickfirst 소거하므로 사용 금지).
- 최근 커밋: 46d87da(B1c-Main), e465ecb(B1b 다중뷰 번들), 64164cc(직렬화 프로브).

## 대기 중 액션
- 없음 (다음 단계 착수 대기).

## 다음 결정 분기
- **격리**(서포트 + 관통 파이프 구간만 추출) → **멀티뷰** → **B2 치수**(평면화 후처리, Main만).
- 미해결: test1 뒤집힘(content-rotation), Main 탑뷰 배제(B3), 경사 후속.

## 관련 파일·경로
- 잔여 백로그: `TODO.md`
- 변경 이력: `CHANGELOG.md`
- 상세 설계철학·목표사양은 프로젝트 메모리(`project_pfs_design_philosophy_core`, `project_pfs_target_output_spec_caetech`) 참조.
