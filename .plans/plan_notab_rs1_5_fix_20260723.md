# 계획서 — 무탭 RS1~5 결함 일괄 수정 (2026-07-23)

- 진단 원장: `.plans/notab_rs_review_20260723.md` (근인 전부 로그+코드로 확정, 재조사 불요)
- 기준: 사용자 눈검수 RS1~5 코멘트 + 09:08 일괄추출 로그 + BOMs.cs/Commands.cs 코드 리딩.
- 원칙: RC 트랙 기성 인프라(config·포트 앵커) 재사용. GD/RC 전 타입 무회귀.

## 수정 항목

### F1. `GetNotabTypeConfig`에 RS1~5 행 추가 — Commands.cs:4910
```csharp
if (RS1) return { VerticalMode="none", PipeCalloutSide="top", HorizontalSide="auto" };
if (RS2) return { VerticalMode="none", PipeCalloutSide="top", HorizontalSide="auto" };
if (RS3) return { VerticalMode="param", VerticalParamKey="F2", PipeCalloutSide="top", HorizontalSide="auto", MemberAnchorSide="vertical" };
if (RS4) return { VerticalMode="param", VerticalParamKey="F2", PipeCalloutSide="top", HorizontalSide="auto", MemberAnchorSide="vertical" };
if (RS5) return { VerticalMode="param", VerticalParamKey="Ha", PipeCalloutSide="top", HorizontalSide="auto" };
```
- 효과: RS1/2 세로치수 삭제, RS3/4 세로 500(F2), RS5 세로 500(Ha).
- RS3/4 `MemberAnchorSide="vertical"` → F2 밸룬이 세로재 포트 기반 배치로 전환(계열 B).

### F2. ~~verticalPortGate 키 완화~~ — **철회 (Codex 자문 채택)**
- Codex 검증: `VerticalMode="param"` 처리(3730~3748)가 `VerticalParamKey`를 직접 읽어 **Ha도 이미 동작**.
  `verticalPortGate`(3756~)는 S2 포트 앵커 결합 전용 — RS5는 `MemberAnchorSide` 비움 → gate false → 완화 무효과.
  회귀 이득 없이 동작 범위만 넓혀 위험 증가 → **무집도**.
- 전제 확인(Codex 지적): param 세로는 `paramRealH <= realH` 불변조건(3742~3752)에서 폴백.
  RS5 실측 realH=601 ≥ Ha=500 → 성립 ✓(로그 dim append V=601).

### F3. RS4 가로치수 = params(A+A1) — Commands.cs:3559 인근
- `rcMemberGeometry` 하드코딩 목록(RC1/2/3/5/7…)에 RS4 추가(또는 config 플래그화 — 집도자 판단, 단 기존 RC 목록 변경 금지).
- 효과: RS4 가로 900(bbox 오염) → 800(A=600+A1=200). 분할 좌우는 `TryGetNotabRcHorizontalParams` 규칙 그대로 두고 라이브에서 확인.
- **RS1/2/3은 legacy(bbox) 유지** — RS2는 bbox 944.55가 정답(사용자 확정), RS1(500)/RS3(800)은 bbox=정답과 일치.

### F4. RS1 BOM 길이 = A — BOMs.cs label_56(1462~)
- 현행 `F1 = ceil(A + PipeSize(Dn)/2 + 100)` = 645. RS1 기하는 A만 사용(실측 500).
- `if (StandardName == "RS1") num87 = 0.0;` 국소 조건. **GD1·RS11 경로 불변**(RS11은 이미 A1 대체, GD1은 기존 공식 유지 — GD 트랙 통과분 무회귀).
- RS2(label_66)는 공식==기하(944.55)라 손대지 않는다.

### F5. RS5/RS6 BOM 키 교정 — **별도 분기 신설** (Codex 자문 채택)
- ★`label_71`은 RS5/RS6 외 **RS12A도 공유**(BOMs.cs:1398, 교차검증 확인) — label_71 직접 수정 금지.
- RS5/RS6만 **새 분기로 복제**: F1=ceil(A+A1), F2=ceil(Ha), F3=ceil(Hb). 기존 label_71·RS12A goto 불변.
- 행별 생략(TryGetValue+skip) **비권고 채택**: F1만 남으면 `s_isoBomRows.Count>0`이 member-text 폴백을 끄고
  F2/F3 밸룬만 조용히 사라지는 불투명 부분성공(3911~3920). → **원자적 실패**: A/A1/Ha/Hb/BI 중
  하나라도 없거나 공백이면 프레임 행 전체 미생성 + 타입·키·원시값 FileDiag 로그.

### F6. 무음 예외 로그 보강 — 삼킴 지점 특정 완료 (Codex 자문 채택)
- `FrameBOM` 자체엔 catch 없음. 삼킴 = `MeasureNotabBomBalloonSources()`의 catch(Commands.cs:6906~6938,
  기존 "MEASURE bom-source 예외" 로그). → 이 로그에 `std`(StandardName)·예외 타입(KeyNotFound 키)·supportId 보강.
- 신규 catch 추가 아님. 동작 변경 없음, 진단성만.

## 라이브 검증 (dev_test.bat, 태그 RS1~5 설정 완료)
| 타입 | 기대 |
|---|---|
| RS1 | 세로치수 없음 / BOM F1=500 / 가로 500(100/400) 유지 |
| RS2 | 세로치수 없음 / 가로 944.6 유지 / F2 밸룬 위치는 이번 범위 밖(아래 잔여) |
| RS3 | 세로 500 / F2 밸룬 세로재(S2 포트) 옆 / BOM 2행 유지 |
| RS4 | 가로 800 / 세로 500 / F2 밸룬 세로재 옆 / 파이프 콜아웃 좌측 유지(현 로직 확정) |
| RS5 | BOM 3행(A+A1=800, Ha=500, Hb=500) / F1~F3 밸룬 생성 / 세로 500 / member-text 폴백 소멸 |
| RS4 분할 | params 전환 후 좌우 분할(left=600/right=200)이 도면 투영과 맞는지 눈확인 — 반대면 RC7식 소비부 스왑 후보 |
| 회귀 | RC1~9·GD1~3·**RS12A** 재추출 1회 — 치수/밸룬/콜아웃 로그 diff 무변화 (RS12A는 label_71 공유 확인용) |

## 범위 밖(후속 사이클)
- RS2 F2 밸룬 -31mm(대각재 인접) — RC7 대각재 분기 일반화 vs 노브 캘리브레이션, F1 결과 보고 결정.
- RS3/4 F2 밸룬 잔차(사용자 이동값 대비) — 포트 전환 후 재검수로 잔차 확정 → 캘리브레이션.
- RS6~15 검수 코멘트 수집(RS12 계열 BOM case 부재 등 이미 관찰됨).
- RS5 카탈로그 Python에 Ha/Hb 기본값 출하(사용자 소관).
