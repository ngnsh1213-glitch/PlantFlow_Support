# 계획 — 핫 리로드 제품 통합 (A안: 명령 로직만)

작성 2026-07-13. 스파이크 PASS([[pfs-hot-reload-spike-pass]]) 후속. §9(Gemini+Codex) A안 수렴.

## 결정
- **A안 채택**: UI(PaletteSet/WebView2/WinForms)는 기존 NETLOAD로 상주. **명령/기하 로직만** collectible ALC로 핫리로드.
- B안(전체) 폐기 — 네이티브 핸들로 언로드 불가에 가까움. 장기 연구 트랙으로만 보류.

## 핵심 아키텍처
```
[상주 NETLOAD: 제품 UI 셸 + 부트스트랩]
  ├ CustomPaletteSet / WebView2 / 폼  (그대로, 세션 상주)
  ├ 고정 [CommandMethod] stub (SOURCE GENERATOR 생성)  → Dispatcher.Invoke("PFS...", args)
  └ Dispatcher: 단일 락 + Idle-only swap + run-id
[Hot ALC(byte[] 로드): 명령/기하 로직]
  └ IPluginSession.Execute(cmd, ctx) — Document/Editor/ObjectId는 호출 중에만, 끝나면 버림
[Contract(기본 컨텍스트 공유): IPluginSession, ICommandContext, IUiBridge(DTO)]
```

## 근본 난관 2 + 대책
1. **명명 명령/sentinel**: `SendStringToExecute`가 raw 명령명을 큐잉 → 그 이름이 등록돼야 함.
   → 부트스트랩에 **고정 stub**(각 명령명+sentinel)이 상주 등록, 실행만 ALC로 포워딩.
   → 36개 수동 금지 → **C# Source Generator**: 단일 레지스트리(`[HotReloadCommand("PFS..")]` 또는 commands.json)에서 `CommandStubs.g.cs` 생성.
2. **언로드 차단(참조 잔존)**: 로직 ALC가 UI/장수 객체·정적 이벤트·타이머·Task를 붙들면 언로드 실패.
   → 로직은 **stateless 규율**: Document/Editor/ObjectId/Transaction을 필드·정적으로 보관 금지, 호출 스코프 내에서만.
   → 이벤트 구독 시 반드시 `IPluginSession.Dispose`에서 `-=` 전부 해제.
   → sentinel 간 전달 상태(현 static s_iso*)는 **부트스트랩(상주) 측 state bag**에 두거나 run-id로 관리(ALC 교체와 무관하게 생존).

## 위험 (§9 명시)
- `SendStringToExecute` 큐 대기 중 unload/reload → 크래시. → **디스패처 락 + 진행중/대기중 명령 0일 때만 swap(Idle 게이트)**.
- sentinel 체이닝 재진입/중복 → run-id 부여.

## 단계 (blast radius 최소 우선)
### Phase 0 — 좁은 수직 슬라이스 (go/no-go 판정용)
- 사용자가 실제 이터레이션하는 **격리 파이프라인 명령군**(PFSVBISOCLONE/…DONE/…EXPORTED 등 소수)만 대상.
- 이 명령군 로직을 `PfHotReload` 계열 **로직 모듈 프로젝트**로 분리(제품에서 호출부만 남기거나 병행).
- 부트스트랩 stub(이 소수 세트는 **수동**으로 충분) → ALC 포워딩.
- s_iso* 상태를 상주 state bag으로 이전.
- **판정**: 격리 로직 수정 → 재빌드 → PFUNLOAD/PFLOAD → 재시작 없이 반영 + collected=True(실 AutoCAD API 사용하 언로드 성공). PASS 시 Phase 1.
- ★결정 필요: **제품 코드 직접 이관** vs **스크래치 스테이징**(아래 전략 분기).

### Phase 1 — Source Generator + 명령군 확장
- stub 자동 생성 도입, 명령군 점진 확대.

### Phase 2 — UI 경계(IUiBridge/DTO)
- UI 접점 명령을 DTO 이벤트 경유로 전환(로직이 UI 객체 직접 접근 제거).

## 전략 분기 (Phase 0 착수 전 사용자 결정)
- **(가) 제품 코드 직접 이관**: 실 Commands.cs 로직을 로직 모듈로 이동 → 핫리로드가 곧 실개발 루프. 이득 크나 **작동 중 제품 구조 변경(영구 복잡도↑)**.
- **(나) 스크래치 스테이징**: 제품은 그대로, 새 로직만 hot 모듈서 개발·검증 후 Commands.cs로 병합. 제품 무변경(저위험)이나 코드 드리프트/병합 수고.

## 비고
- 이 통합은 **개발 편의(dev-loop)** 투자다. 제품 아키텍처를 핫리로드용으로 영구 재구성하는 비용 대비 이득을 (가)/(나)로 저울질할 것.
- 상세 스파이크 자산: `C:\Lisp\HotReload\`(리포 밖). Contract/Bootstrap/디스패처는 여기서 확장.
