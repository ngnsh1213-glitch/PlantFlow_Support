# GD2.py 통합 최적화 및 논리 복구 계획
> 작성일: 2026-04-23

## 개요
이 계획은 `GD2.py` 스크립트의 상반된 논리 구조(TYPE A 구현 후 검증에서 차단)를 바로잡고, 프로젝트 표준인 `JIS/KS` 제원을 이식하여 `FS/GD1` 시리즈와 통일된 품질을 확보하는 것을 목표로 합니다.

## User Review Required

> [!CAUTION]
> **🟥 1. TYPE A(-1) 로직 개방**
> - 현재 코드 내부에 TYPE A 전용 로직이 존재함에도 상단 검증 단계에서 오직 TYPE B(-2)만 허용하도록 잠겨 있습니다. 이를 수정하여 두 타입 모두 선택 가능하도록 복구합니다.

> [!IMPORTANT]
> **🟡 2. 헤더 정보 및 관경 표준화**
> - 작성자(`Noh Seunghwan`) 및 표준 레퍼런스 경로를 삽입합니다.
> - `pipe_size`를 JIS/KS 규격으로 교정하고 누락된 **DN32(42.7)** 데이터를 추가합니다.

## Proposed Changes

### 1. 파일 헤더 표준화
- 적용 코드 블록:
```python
## GD2
## Developed by Noh Seunghwan
## Last Modified: Apr 23, 2026
## V:\4_부서관리\9_배관팀\03 설계자료\03_02 Support\PIPE SUPPORT STANDARD_R0_260311
##--------------------------------
```

### 2. JIS/KS 표준 관경 개편 (pipe_size)
- **교정 항목**: DN15(21.7), DN20(27.2), DN25(34.0), DN125(139.8) 등.
- **신규 항목**: **DN32(42.7)** 삽입.

### 3. 논리 결함 수정 및 가독성 개선
- **검증 로직 수정**: `if (TY not in [-1, -2]):`로 변경하여 모든 타입 대응.
- **노이즈 제거**: 하단의 5줄 중복 구분선 정리.

## Verification Plan

### Manual Verification
- AutoCAD Plant 3D 환경에서 TYPE A와 TYPE B 모델링이 정상적으로 분기되는지 테스트.
- DN32(32A) 관경 선택 시 형상 생성 모델링 검증.
- 객체 결합 로직의 안정성 최종 확인.
