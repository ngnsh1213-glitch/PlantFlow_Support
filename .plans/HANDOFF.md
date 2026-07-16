# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 51
- **status**: ready
- **issued_at**: 2026-07-16
- **title**: 무탭 치수 개선 — 세로=앵글높이(75)+지시선 콜아웃(L-75×75×9), 치수 크기 설정, 가로 위치 타입별 스캐폴드
- **target**: `PlantFlow_Support/Core/Commands.cs` (그 외 무수정)

## 착수 전
- cwd `D:\PlantFlow\PlantFlow_Support`. 무탭 치수부(AppendNotabPaperDimensions/Entity/Overrides) + BI/타입 헬퍼만. 그 외 무수정.
- 빌드 GREEN(MSBuild Debug, 에러0). 라이브는 사용자.

## 배경 (사용자 확정)
cycle49 치수 라이브 후 3건 요구:
- **(2) 세로 치수=앵글 높이 숫자 + 별도 지시선 콜아웃**: 현 세로 `176`은 앵글+유볼트+배관 **합산이라 무의미**. 사용자 요구 **둘 다**:
  - **(2a) 세로 치수 텍스트 = 앵글 높이 숫자 `75`**(BI→DetailProfile "75x75x9"의 **F=첫 숫자** 파싱, 176 대체).
  - **(2b) 별도 지시선(MLeader) 콜아웃 `L-75×75×9`** 부재 옆 표기(§9 표준). 즉 높이 숫자(치수)와 부재 규격(콜아웃)을 **분리**.
- **설정**: 글자 10 / **화살표 10** / 오프셋 15 / 적층 15.
- **(1) 가로 치수 위치 = 타입별**: 사용자가 全타입 테스트 후 타입별 상/하단 데이터를 줄 예정 → **이번엔 per-type 테이블 스캐폴드만**(GD1·RC1=하단 기지값 + 기본=현 로직).

## §9 확정 데이터 경로
- **BI**: `PSUtil.GetSupportDimension(id)` → `SupportParams["BI"]`(SupportHelper.GetSupportParameters, `_` 제거). 확실 경로. (CaptureIsoSupportProperties엔 BI 없음.)
- **BI 디코드**: `BI="BPn+BF"`(문자열). BPn 첫자리 1→L, 2→C, 3→H, 4→FB(플랫). 나머지=BF.
- **치수 문자열**: `HANTEC.DetailProfile(BI)`(Ortho/HANTEC.cs:1894, `public static`, 같은 어셈블리 호출 가능) → `"17"→"75x75x9"`. prefix 없음.
- **designation 조립**: `<prefix>-<DetailProfile>` = `"L-75×75×9"`(두께 포함, §9 d). BeamProfile("ANGLE A7")은 부적합 — 미사용.
- **서포트 타입**: `s_isoSupportTag`(=SupportName, 예 "GD1-001"). 타입 = **첫 `-` 앞 prefix**("GD1"). 전용 함수 없음 → 파싱 추가.
- **세로 override 지점**: `AppendNotabPaperDimensionEntity`의 `dim.DimensionText=FormatNumber(realValue)`(~4507). `dimV`(라벨 "dimV")일 때 F숫자 주입 or dimV 전용 경로 분리.
- **MLeader 콜아웃**: `PSUtil.CreateMLeader(...)`(2 오버로드)+`EnsureMLeaderStyles`(OrthoViewportManager:662, MLeaderStyle Dict). 참조 사용례=projectedPoints+MText 991/892행대. Commands.cs에서 호출 가능(같은 어셈블리).
- **설정 기본값**: `txt=GetEnvDouble("PFS_NOTAB_DIM_TXT",2.5..)`, `offset=..txt*3`, `stack=txt*2.5`(~4442). `ApplyNotabPaperDimensionOverrides`(~4519) Dimasz=txt*1.6.

## 요구
### A. 치수 크기 설정 변경
- `PFS_NOTAB_DIM_TXT` 기본 **10.0**, `PFS_NOTAB_DIM_OFFSET` 기본 **15.0**, stack 기본 **15.0**(txt*2.5 폐기, 고정 15 or env `PFS_NOTAB_DIM_STACK`).
- **화살표 분리**: `ApplyNotabPaperDimensionOverrides`의 `Dimasz=txt*1.6` → **`Dimasz=PFS_NOTAB_DIM_ARR`(기본 10.0)**. Dimgap 등은 txt 기반 유지 or 적정.

### B. 세로 치수 = 앵글 높이 숫자(2a) + 지시선 콜아웃(2b)
공통: support의 BI 획득(`PSUtil.GetSupportDimension` / SupportParams["BI"]) → `dims = HANTEC.DetailProfile(BI)`(예 "75x75x9"), `prefix = BI[0]` 매핑(1 L,2 C,3 H,4 FB). 실패 시 폴백+로그.
- **(2a) 세로 치수 텍스트 = F 높이 숫자**: `dims`를 "x"/"×"로 split → **첫 요소=F(예 "75")**. `AppendNotabPaperDimensionEntity`에서 dimV(라벨 "dimV")일 때 `DimensionText = F`(realH 대체). dimV 위치/스팬 현행 유지(좌측). BI 없으면 폴백=realH.
- **(2b) 지시선 콜아웃 `<prefix>-<dims>`**(예 "L-75×75×9"): **MLeader**로 부재(빔/앵글) 옆에 표기. `EnsureMLeaderStyles`(OrthoViewportManager:662 패턴)로 스타일 확보, `PSUtil.CreateMLeader(projectedPoints, new MText{Contents=designation, TextHeight=txt(10), TextStyleId=..}, ml_style_id, Matrix3d.Identity)` 재사용.
  - 앵커=부재 위치 페이퍼점(빔=서포트 rect 하단부 or 좌하단, `NotabProjectWcsToPaper`로 산출). 텍스트는 여백 쪽(겹침 회피)으로 인출.
  - Title Block 레이아웃 페이퍼공간에 append(치수와 동일 트랜잭션). 레이어=AUTO_DIM or 신규.
- 로그 `dim append ... dimV(F)=<75> callout=<L-75×75×9> BI=<..>`.

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
- 라이브(여러 타입, PFSNOTABBATCH 가능): **세로 치수=`75`(F숫자)** + **지시선 콜아웃=`L-75×75×9`(부재 옆)**, 글자/화살표 10·오프셋/적층 15, GD1/RC1 가로=하단. 로그 `dimV(F)=.. callout=.. side=..`. 콜아웃이 부재 옆에 인출·겹침 없는지 육안.

## 참고
- SHAPE.py profile[7]={F:75,T:9}=DetailProfile("17")="75x75x9" 일치. cycle49 커밋 9576258.
