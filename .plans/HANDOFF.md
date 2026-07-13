# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 23
- **status**: ready
- **issued_at**: 2026-07-14
- **title**: 무탭 N2 크래시 진단+수정 — 비주얼스타일 exact "Hidden" + Commit/SaveAs 스테이지 로그
- **target**: PlantFlow_Support/Core/Commands.cs

## 착수 전
- cwd `...\PlantFlow_Support`. 기존 PFSVBISO* 무수정. PFSNOTABDETAIL/N1 관련만.

## 배경 (라이브 크래시 2회 재현)
- `PFSNOTABDETAIL`이 `template cloned=2 viewport=ok titleblock=ok`(:2217)까지 로그 후 **AutoCAD 크래시**. `saved path=`(:2230) 미출력.
  → 크래시 구간 = **tr.Commit()(:2218) 또는 tagDb.SaveAs(:2229)**.
- ★단서: 로그 `Hidden visualStyle=3D Hidden`. 성공한 스파이크 C는 `visualStyle=Hidden`(다름). `TryApplyHiddenVisualStyle`이 `name.IndexOf("Hidden")>=0`로 **"3D Hidden"**을 먼저 매칭 → side-DB SaveAs/GS 크래시 의심.

## 지시 (cycle 23)

### Fix A — 비주얼스타일 정확히 "Hidden" 우선 (스파이크 C와 일치)
- `TryApplyHiddenVisualStyle`(약 :2430) 매칭 로직 변경:
  1. VS 딕셔너리에서 **이름이 정확히 "Hidden"(대소문자 무시)** 인 항목 우선 선택.
  2. 없으면 "Hidden" 포함하되 "3D"를 포함하지 않는 항목.
  3. 그래도 없으면 기존 폴백(포함 매칭). 선택된 이름 로그.
- 목적: 크래시 유발 의심 "3D Hidden" 회피, 스파이크 C의 "Hidden"과 동작 일치.

### Fix B — Commit/SaveAs 스테이지 로그(크래시 지점 확정)
- `CreateNotabDetailDrawing`(:2189~2231)에 로그 추가:
  - tr.Commit() **직전** `PFSNOTABDETAIL commit 직전`
  - tr.Commit() **직후** `PFSNOTABDETAIL commit 완료`
  - SaveAs **직전** `PFSNOTABDETAIL saveAs 직전 path=..`
  - SaveAs **직후**(기존 saved 로그 유지)
- 목적: 다음 실행 시 크래시가 Commit인지 SaveAs인지 로그로 확정.

### Fix C — 진단 토글(선택, env var로 재컴파일 없이 bisection)
- 환경변수 `PFS_NOTAB_SKIP_HIDDEN`=1이면 `TryApplyHiddenVisualStyle`/`TrySetViewportShadePlotHidden` 호출 skip(뷰포트는 2D wireframe로 저장). 로그 `hidden skip(env)`.
- 목적: 사용자가 env 세팅 후 재실행 → Hidden 없이도 크래시나는지로 "Hidden GS vs solid/SaveAs" 분리.

## 규율
- 기존 무수정. 빈 catch 금지, 예외 FileDiag. WorkingDatabase finally 복원 유지.

## 빌드/완료
- 수동 빌드 GREEN. 커밋(거부 시 Claude 대리). `.plans/REPORT.md` 결과.
- 사용자 라이브 순서:
  1. 그냥 `PFSNOTABDETAIL` → Fix A(exact Hidden)로 크래시 사라지는지 + commit/saveAs 로그 확인.
  2. 여전히 크래시면: `set PFS_NOTAB_SKIP_HIDDEN=1`(또는 시스템 env) 후 Plant3D 재시작→재실행 → Hidden 없이 크래시 여부.
  3. 로그(commit/saveAs 스테이지 + hidden 스타일명) 공유 → 다음 판정.
