# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 24
- **status**: ready
- **issued_at**: 2026-07-14
- **title**: 무탭 N2 크래시 분리 — 뷰포트 생략 토글 + 2단계 뷰포트 폴백
- **target**: PlantFlow_Support/Core/Commands.cs

## 착수 전
- cwd `...\PlantFlow_Support`. PFSNOTABDETAIL 관련만. 기존 PFSVBISO* 무수정.

## 배경 (크래시 좁힘)
- `PFSNOTABDETAIL`이 `commit 직전`까지 로그 후 **tr.Commit()에서 크래시**(commit 완료 미출력).
- `PFS_NOTAB_SKIP_HIDDEN=1`(Hidden/ShadePlot skip)로도 **여전히 crash** → Hidden 무관.
- N1(solid→plain DB 저장) 무탈, 스파이크 C(side-DB 뷰포트+SaveAs) 무탈. N2(템플릿DB+뷰포트+side-DB explode solid) commit만 crash.
- → 남은 용의자: (a) 뷰포트(3D solid 참조, ViewDirection/On) commit, (b) solid-into-템플릿 commit.

## 지시 (cycle 24)

### Fix A — 뷰포트 생략 진단 토글
- 환경변수 `PFS_NOTAB_SKIP_VIEWPORT=1`이면 `CreateNotabDetailViewport` 호출 자체를 **skip**(로그 `viewport skip(env)`), solids만 클론+타이틀블록+저장.
- 목적: skip-viewport로 commit이 완주하면 → 뷰포트가 crash 원인. 여전히 crash면 → solid-into-template가 원인.

### Fix B — 2단계 뷰포트(폴백 설계, 항상 적용)
뷰포트를 **같은 트랜잭션에서 view 속성까지 세팅→commit**하는 현재 방식이 크래시 의심.
아래로 재구성:
1. **1차 트랜잭션**: 뷰포트 생성 = AppendEntity + `vp.Width/Height/CenterPoint`만 설정 + `vp.On=true`. **ViewDirection/ViewTarget/TwistAngle/CustomScale/VisualStyle/ShadePlot은 이때 설정하지 않음.** tr.Commit.
   - solids WblockClone/레이어/타이틀블록도 이 트랜잭션 or 그 전에.
2. **2차 트랜잭션**(신규): 방금 생성한 뷰포트 ObjectId를 ForWrite로 다시 열어 **ViewDirection/ViewTarget/TwistAngle/CustomScale + (env 미skip 시)VisualStyle/ShadePlot** 설정. tr.Commit.
   - 근거: 새로 append된 뷰포트에 view 속성을 같은 트랜잭션서 설정 시 GS view 미초기화로 commit 크래시 가능. 별도 트랜잭션서 이미 DB에 존재하는 뷰포트에 설정하면 안전(관용구).
- 각 단계 로그: `viewport 1차 생성 완료`, `viewport 2차 view설정 완료`.

## 규율
- 기존 무수정. WorkingDatabase finally 복원. 빈 catch 금지, 예외 FileDiag.

## 빌드/완료
- 수동 빌드 GREEN. 커밋(거부 시 Claude 대리). `.plans/REPORT.md`.
- 사용자 라이브 순서:
  1. 그냥 `PFSNOTABDETAIL` → Fix B(2단계 뷰포트)로 crash 사라지는지 + `viewport 1차/2차` 로그.
  2. 여전히 crash면: `PFS_NOTAB_SKIP_VIEWPORT=1` env 설정→Plant3D 재시작→재실행 → 뷰포트 없이 commit 완주하는지(= solid-into-template 원인 여부).
  3. 로그 공유.
