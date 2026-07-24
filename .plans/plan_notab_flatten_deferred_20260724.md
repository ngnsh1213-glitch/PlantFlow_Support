# 계획서 — 무탭 2D 평면화 지연 후처리 재설계 (cycle 123, 2026-07-24)

- 선행 실측(cycle122): 인라인 B변형은 `stage=flatshot eInvalidInput` — **바깥 명령(PFSNOTABDETAIL) 진행 중 교차-문서 Editor.Command 불가**. Open/활성/tilemode/set-view는 성공. 즉 명령형 평면화는 파이프라인 명령 밖에서만 가능(3-스트라이크로 인라인 중단).
- 방향(사용자 확정): 인라인 스파이크 코드는 플래그 뒤 존치(무영향), 지연 후처리로 재설계.

## ★핵심 단순화
저장된 상세도(`*_notab.dwg`)는 **모델공간=클립 솔리드, 페이퍼공간=뷰포트+주석**을 이미 보유.
→ 임시 DWG·클론(cycle122 eInvalidialInput 2회의 원흉) **전부 불필요**. 상세도를 **활성 문서로 연 상태**에서 그 자리에서 평면화.
→ 같은-문서 `Editor.Command("-FLATSHOT")`는 합법(오쏘 스파이크 PlantOrthoView.cs:1656 실증). 교차-문서만 막혔던 것.

## 명령 컨텍스트 규약 (실측 기반)
- **PFSNOTABFLATTEN = 현재 활성 문서에서만 동작**(문서 Open/전환 안 함). 활성 문서가 이미 열린 `*_notab.dwg`여야 함.
- 스파이크 검증: 사용자가 RC1-001_notab.dwg를 **수동으로 열고**(활성화) → `PFSNOTABFLATTEN` 실행. Open/전환 없음 = 명령중첩 없음.
- 자동화(스파이크 PASS 후): 추출 종료 시 `SendStringToExecute("_.OPEN <path> PFSNOTABFLATTEN ...")` 큐 — 각 명령이 top-level로 실행돼 활성 문서=상세도, FLATSHOT 합법.

## 스파이크 범위 (PFSNOTABFLATTEN 신규 명령) — 자문 반영
- **명령 등록**: 일반 `[CommandMethod("PFSNOTABFLATTEN")]`(★`CommandFlags.Session` 붙이지 말 것 — 자문). 현재 활성 doc에서만.
  ★같은-문서 Editor.Command 합법성은 코드경로 증거일 뿐 — **이 신규 명령 단독 실행이 첫 실측 관문.**
1. **사전검사(자문 신설)**: 활성 doc 모델공간이 **Solid3d만** 갖는가. 그 외 엔티티 or 기존 FLATSHOT BlockReference 있으면 **NOGO**(사용자 편집본·재실행 오염 방지). 신규 생성본은 클립 솔리드만이라 안전(Commands.cs:1901~1906 확인).
2. 페이퍼공간 **뷰포트 1개** 탐색. 없으면 skip.
3. 뷰포트 스냅샷 — `TryGetNotabFlattenViewportSnapshot` 재사용.
4. TILEMODE=1 + 뷰=스냅샷 방향(`SetNotabFlattenTempView`) → `Editor.Command("_.-FLATSHOT" …)`. baseline 대비 **신규 BlockReference 정확히 1개** 확인(`TryFindNotabFlattenBlock` + 개수 가드, 오인 방지).
5. DCS→paper 아핀 → `alignErr` vs `PFS_NOTAB_FLATTEN_TOL`(2.0) GO/NOGO.
6. **GO 이식(★자문 정정)**: 동일 DB이므로 `WblockCloneObjects` 금지 → **`DeepCloneObjects`로 모델공간 FLATSHOT BlockReference를 레이아웃 BTR로 복제** + 아핀 변환 적용. 그 뒤 **원본 모델공간 FLATSHOT BlockReference를 Erase**(재실행 시 2D가 FLATSHOT 입력에 섞임 방지). 뷰포트 삭제. **QSAVE**(SaveAs 불요).
7. NOGO: 변경 없이 로그만(모델공간 FLATSHOT 출력도 Erase해 원상복구).
- **재작성**: `TryFlattenNotabDetailB`(1985~) 통째 제거/대체(임시파일·Open·MDI전환·cleanup 전부). 재사용=snapshot/set-view/snapshot-model-ids/find-block/DCS→paper/align. `TryCloneNotabFlattenBlockToPaper`는 DeepClone 방식으로 재작성.
- **검증 카운트 로그(자문 신설)**: 실행 전후 레이아웃 Dimension/MLeader/Table 수·뷰포트 수(1→0)·모델공간 FLATSHOT 출력(생성 후 1→이식 후 0).
  `PFSNOTABFLATTEN2 lines=… flatBbox=… projBbox=… alignErr=… tol=… gate=GO|NOGO dims=<b>/<a> vp=<b>/<a> flatObj=<b>/<a>`.

## 비범위 (스파이크 후)
- 자동 큐(SendStringToExecute), 배치 다건, 멀티뷰(Top/ISO), 레이아웃 캘리브레이션.

## 검증
- RC1-001_notab.dwg 수동 오픈 → PFSNOTABFLATTEN → 로그 alignErr/gate + 눈확인(뷰포트 소멸·2D·주석 정렬).
- 인라인 경로(PFS_NOTAB_FLATTEN) 무영향 유지. `dotnet build` 오류 0.
