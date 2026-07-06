# PlantFlow Support 통합 아키텍처 맵 (Integration Map)

본 문서는 파이썬 기반의 구조물 모델링(CustomScripts)과 C# 기반의 후처리 시스템(PlantFlow_Support)이 어떻게 상호작용하는지를 규정하며, 로컬 지식 베이스(옵시디언)의 단절된 데이터 노드를 융합시키는 메인 브릿지(Bridge) 문서입니다.

## 🔗 핵심 컴포넌트 맵핑 (Node Links)

### 1. 직교 도면 (Ortho) 태그 연결
파이썬 스크립트에서 정의된 포트(`setPoint`)는 2D 도면 추출 시 HANTEC 쪽의 `TaggingPoints`로 직결됩니다.
- [[scan_FS_py_20260426_144416|FS]] ↔ [[scan_HANTEC_cs_20260426_144159|HANTEC]] : `FS()` 로직에서 배관 포인트를 1:1 매칭
- [[scan_GD1_py_20260426_144443|GD1]] ↔ [[scan_HANTEC_cs_20260426_144159|HANTEC]] : `GD1()` 로직에서 `PPorts[1]`을 "F1" 위치로 치환 
- [[scan_GD2_py_20260426_144504|GD2]] ↔ [[scan_HANTEC_cs_20260426_144159|HANTEC]] : `GD2()` 단일 파트 로직
- [[scan_GD3_py_20260426_144526|GD3]] ↔ [[scan_HANTEC_cs_20260426_144159|HANTEC]] : `GD3()` 파트 로직 연동

### 2. 물량 산출 (BOM) 파라미터 연결
사용자가 AutoCAD에서 기입한 파라미터(`BI`, `Dn`)는 C# 백엔드로 넘어가 부속품 물량 산출로 환산됩니다.
- [[scan_FS_py_20260426_144416|FS]] ↔ [[scan_BOMs_cs_20260426_144100|BOMs]] : `this.SupportParams["BI"]` 속성 참조 
- [[scan_GD1_py_20260426_144443|GD1]] ↔ [[scan_BOMs_cs_20260426_144100|BOMs]] : 앵커 볼트 및 Baseplate 등 프로파일 기반 물량 계산
- [[scan_GD2_py_20260426_144504|GD2]] ↔ [[scan_BOMs_cs_20260426_144100|BOMs]]
- [[scan_GD3_py_20260426_144526|GD3]] ↔ [[scan_BOMs_cs_20260426_144100|BOMs]]

### 3. ERP 자재 코드 (AutoCoding) 연결
모든 파이썬 지원물 규격은 최종적으로 회사 ERP에 넘길 자동 생성 넘버링 규칙 파일의 통제를 받습니다.
- [[scan_FS_py_20260426_144416|FS]] ↔ [[scan_PlantAutoCoding_cs_20260426_144237|PlantAutoCoding]]
- [[scan_GD1_py_20260426_144443|GD1]] ↔ [[scan_PlantAutoCoding_cs_20260426_144237|PlantAutoCoding]]
- [[scan_GD2_py_20260426_144504|GD2]] ↔ [[scan_PlantAutoCoding_cs_20260426_144237|PlantAutoCoding]]
- [[scan_GD3_py_20260426_144526|GD3]] ↔ [[scan_PlantAutoCoding_cs_20260426_144237|PlantAutoCoding]]

### 4. 핵심 형상 라이브러리
모든 서포트 파이썬 모델은 공통 기하학 수학과 템플릿을 공유합니다.
- [[scan_SHAPE_py_20260426_144539|SHAPE]] : `C_SHAPE`, `L_SHAPE` 등 3D 솔리드 모델링 함수군 전역 제공

## 🚨 개발자 주의사항 (Design Principles)
> [!CAUTION]
> **포트 선언 순서 불변의 법칙**: 파이썬 스크립트(`GD1.py`, `GD2.py` 등) 최하단에 위치한 `s.setPoint()` 선언 순서를 한 줄이라도 바꾸면, [[scan_HANTEC_cs_20260426_144159|HANTEC]]가 참조하는 내부 배열 `PPorts[n]` 인덱스가 통째로 꼬여 도면 상의 치수 기입 태깅(Tagging)이 완전히 어긋납니다. 치수 체계 개편(JIS 도입 등)은 적극 장려하되, **setPoint 배열 순서와 개수는 어떠한 경우에도 임의 수정/삭제하지 마십시오.**
