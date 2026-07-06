# PlantFlow Support 프로젝트 현황 및 로드맵

**최종 업데이트**: 2026-02-04
**작성자**: Antigravity AI

이 문서는 PlantFlow Support 프로젝트의 현재 진행 상황, 완료된 작업, 그리고 향후 계획을 요약합니다.

## 1. 완료된 작업 (Accomplishments)

### 1.1 빌드 및 컴파일 안정화
- **빌드 오류 해결**:
    - `OrthoViewportManager.cs`의 `CS1524` (catch 블록 누락) 수정.
    - `.csproj`의 `MSB3577` (중복 리소스) 수정.
    - `PSUtil.cs`, `Commands.cs`의 디컴파일 아티팩트(연산자, ref 키워드 등) 정리.
- **경고 정리**: 주요 빌드 경로의 크리티컬한 경고들을 검토 및 확인.

### 1.2 런타임 안정성 강화 (Bug Fixes)
- **크래시(Crash) 수정**:
    - **`eNullObjectId`**: `UpdateTitleBlockAttributes` 메서드에 견고한 예외 처리 및 안전한 객체 접근 로직 추가.
    - **`eCannotScaleNonUniformly`**: `UpdateStandardScale` 내 예외 처리 추가.
    - **무한 루프**: `_AUTO2DPROCESSZOOM` 재귀 호출 문제 해결.
    - **"No drawings open" (ER1000)**: `PSUtil.GetDocument`의 반전된 `null` 체크 로직 수정.
- **기능 오동작 수정**:
    - Top View에서 텍스트/형상이 뒤집히는 문제 해결 (`UpVector` 수정).
    - Xref 메시지 억제 및 저장 위치 출력 기능 추가.

### 1.3 아키텍처 리팩토링 (Refactoring)
- **폴더 구조 재정비**: 프로젝트 파일을 역할별로 분류하여 가독성 및 유지보수성 향상.
    - `UI`: 폼 및 팔레트 탭 (`PaletteTab`, `Form...`)
    - `Core`: 핵심 명령 로직 (`Commands.cs`)
    - `Ortho`: 평면/정면도 생성 로직 (`PlantOrthoView`, `OrthoViewportManager`, `HANTEC`)
    - `Models`: 데이터 모델 (`SupportInfo`, `BOMs`)
    - `Utils`: 공용 유틸리티
- **PSUtil 분해 (Decomposition)**:
    - 1500줄 이상의 거대 클래스 `PSUtil`을 기능별로 분리하고, 기존 코드는 `Utils` 폴더로 이동하여 파사드(Facade) 패턴 적용.
    - 분리된 모듈: `GeomUtils` (기하학), `AnnotationUtils` (주석), `DatabaseUtils` (DB), `CadUtils` (CAD 제어), `ProjectDataUtils` (데이터).
- **OrthoViewportManager 도입**:
    - `PlantOrthoView`의 복잡한 뷰포트 처리 로직을 전담 매니저 클래스로 추출.

---

## 2. 현재 상태 (Current Status)

- **테스트 단계**: 사용자가 직접 AutoCAD Plant 3D 환경에서 수정 사항을 검증하는 단계입니다.
- **검증 항목**:
    1. `PFS` 명령 실행 및 팔레트 로드 정상 여부.
    2. "Export 2D" 기능 실행 시 도면 생성 및 속성 업데이트 정상 여부.
    3. `eNullObjectId` 등 기존 런타임 오류 재발 방지 확인.

---

## 3. 알려진 문제 (Known Issues)

- **"Export MTO" 기능 비활성화**: 해당 버튼이 호출하는 `PSUtil.GenerateSupportData` 메서드가 현재 비어 있습니다(Placeholder). 이는 리팩토링 과정에서 원본 로직의 재구현이 필요하여 일시적으로 보류된 상태입니다.
- **빌드 경고**: `BOMs.IsBaseplate` 필드가 할당되지 않았다는 경고(CS0649)가 존재합니다. BOM 생성 로직에서 Baseplate 여부가 항상 `false`로 처리될 수 있는 잠재적 이슈입니다.

---

## 4. 향후 계획 (Future Roadmap)

### 4.1 "Export MTO" 복구
- 손실되거나 분리된 `GenerateSupportData` 로직을 찾아 `ProjectDataUtils` 또는 `BOMs` 클래스 내에 재구현.
- MTO 내보내기 기능 정상화 및 검증.

### 4.2 PSUtil 파사드 제거 (Cleanup)
- 현재 `PSUtil`은 구형 코드와의 호환성을 위해 유지되고 있음.
- `PlantOrthoView`, `Commands` 등에서 `PSUtil`을 거치지 않고 `GeomUtils`, `CadUtils` 등을 직접 호출하도록 코드 업데이트.
- 최종적으로 `PSUtil.cs` 파일 삭제.

### 4.3 코드 품질 개선
- `BOMs` 클래스의 미사용 필드 로직 검토 및 수정.
- 하드코딩된 문자열을 `LocalResources` 사용으로 전환 확대.
- 예외 처리 로깅 시스템 개선 (사용자에게 더 친절한 에러 메시지 제공).

---

위 내용은 프로젝트의 진행 상황에 따라 수시로 업데이트될 예정입니다.
