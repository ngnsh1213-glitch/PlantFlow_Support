# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 51
- **status**: ready
- **issued_at**: 2026-07-16
- **title**: 무탭 치수 개선 — 세로=부재 designation(BI), 치수 크기 설정, 가로 위치 타입별 스캐폴드
- **target**: `PlantFlow_Support/Core/Commands.cs` (그 외 무수정)

## 착수 전
- cwd `D:\PlantFlow\PlantFlow_Support`. 무탭 치수부(AppendNotabPaperDimensions/Entity/Overrides) + BI/타입 헬퍼만. 그 외 무수정.
- 빌드 GREEN(MSBuild Debug, 에러0). 라이브는 사용자.

## 배경 (사용자 확정)
cycle49 치수 라이브 후 3건 요구:
- **(2) 세로 치수 = 부재 designation**: 현 세로 `176`은 앵글+유볼트+배관 높이 **합산이라 무의미**. 사용자는 **서포트 빔(앵글/채널) 부재 규격**만 원함. → 세로 치수 텍스트를 **`L-75×75×9`** 형식으로 override(측정 176 대체). ※§9(Gemini)는 "치수 텍스트 override보다 리더 콜아웃" 권장했으나 **사용자가 명시적으로 치수 자리 표기(A) 선택 — 기각 확정**.
- **설정**: 글자 10 / **화살표 10** / 오프셋 15 / 적층 15.
- **(1) 가로 치수 위치 = 타입별**: 사용자가 全타입 테스트 후 타입별 상/하단 데이터를 줄 예정 → **이번엔 per-type 테이블 스캐폴드만**(GD1·RC1=하단 기지값 + 기본=현 로직).

## §9 확정 데이터 경로
- **BI**: `PSUtil.GetSupportDimension(id)` → `SupportParams["BI"]`(SupportHelper.GetSupportParameters, `_` 제거). 확실 경로. (CaptureIsoSupportProperties엔 BI 없음.)
- **BI 디코드**: `BI="BPn+BF"`(문자열). BPn 첫자리 1→L, 2→C, 3→H, 4→FB(플랫). 나머지=BF.
- **치수 문자열**: `HANTEC.DetailProfile(BI)`(Ortho/HANTEC.cs:1894, `public static`, 같은 어셈블리 호출 가능) → `"17"→"75x75x9"`. prefix 없음.
- **designation 조립**: `<prefix>-<DetailProfile>` = `"L-75×75×9"`(두께 포함, §9 d). BeamProfile("ANGLE A7")은 부적합 — 미사용.
- **서포트 타입**: `s_isoSupportTag`(=SupportName, 예 "GD1-001"). 타입 = **첫 `-` 앞 prefix**("GD1"). 전용 함수 없음 → 파싱 추가.
- **세로 override 지점**: `AppendNotabPaperDimensionEntity`의 `dim.DimensionText=FormatNumber(realValue)`(~4507). `dimV`(라벨 "dimV")일 때만 designation 주입 or dimV 전용 경로 분리.
- **설정 기본값**: `txt=GetEnvDouble("PFS_NOTAB_DIM_TXT",2.5..)`, `offset=..txt*3`, `stack=txt*2.5`(~4442). `ApplyNotabPaperDimensionOverrides`(~4519) Dimasz=txt*1.6.

## 요구
### A. 치수 크기 설정 변경
- `PFS_NOTAB_DIM_TXT` 기본 **10.0**, `PFS_NOTAB_DIM_OFFSET` 기본 **15.0**, stack 기본 **15.0**(txt*2.5 폐기, 고정 15 or env `PFS_NOTAB_DIM_STACK`).
- **화살표 분리**: `ApplyNotabPaperDimensionOverrides`의 `Dimasz=txt*1.6` → **`Dimasz=PFS_NOTAB_DIM_ARR`(기본 10.0)**. Dimgap 등은 txt 기반 유지 or 적정.

### B. 세로 치수 = 부재 designation
- `AppendNotabPaperDimensions`에서 세로(dimV) 텍스트를 realH 대신 **부재 designation**으로:
  - support의 BI 획득(`PSUtil.GetSupportDimension` / SupportParams["BI"]). 문자열.
  - `prefix = BI[0]` 매핑(1 L,2 C,3 H,4 FB), `dims = HANTEC.DetailProfile(BI)`. `designation = prefix + "-" + dims`(예 "L-75x75x9"). "x"→"×" 표기 선택(FormatNumber 불필요, 문자열).
  - dimV `DimensionText = designation`. 방어: BI 없음/DetailProfile 키 없음 → **폴백 = 기존 realH 텍스트**(+로그).
  - dimV 위치/스팬은 현행 유지(좌측). 텍스트만 교체.
- 로그 `dim append ... dimV=designation BI=<..> profile=<..>`.

### C. 가로 위치 타입별 스캐폴드 (미완, 채울 준비)
- 헬퍼 `GetNotabHorizontalDimSide(string supportType) → "top"|"bottom"|"auto"`: 내부 테이블 `{ "GD1":"bottom", "RC1":"bottom" }`, 미등록=`"auto"`.
- supportType = SupportName 첫 `-` 앞 prefix.
- `AppendNotabPaperDimensions`의 상/하단 결정에서: side="top"/"bottom"이면 강제, "auto"면 **기존 배관근접 로직**(cycle49) 유지.
- 로그 `dim horizontal side=<top|bottom|auto> type=<GD1..>`. **테이블은 사용자 추후 데이터로 확장 예정(주석 명시).**

## 방어/보존
- BI/GetSupportDimension/HANTEC try/catch+FileDiag(빈 catch 금지). designation 실패→realH 폴백.
- 클립·held-pipe·스케일·persp가드·wireframe·투영 무수정. 가로 분할(배관중심)·세로 위치 유지.

## 검증
- MSBuild Debug GREEN. 변경 주변 20줄 수동 확인.
- 라이브(여러 타입, PFSNOTABBATCH 가능): 세로=`L-75×75×9`류, 글자/화살표 10·오프셋/적층 15, GD1/RC1 가로=하단. 로그 `dimV=.. side=..`.

## 참고
- SHAPE.py profile[7]={F:75,T:9}=DetailProfile("17")="75x75x9" 일치. cycle49 커밋 9576258.
