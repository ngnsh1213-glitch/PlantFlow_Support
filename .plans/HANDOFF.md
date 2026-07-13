# HANDOFF — Claude → Codex

> Claude가 발행하고 Codex가 읽어 집도한다. **매 사이클 이 파일을 덮어쓴다.**
> Codex는 작업 완료 시 `REPORT.md`에 결과를 기록하고 코드를 커밋한다.

- **cycle**: 8
- **status**: idle
- **issued_at**: 2026-07-13 11:40
- **title**: (취소됨) cycle 8 csproj 제외안은 폐기 — 스파이크를 C:\Lisp\HotReload로 리포 외부 이관(커밋 4447a17)으로 대체

## 착수 전
- cwd가 `...\PlantFlow_Support`인지 확인(AGENTS.md 가드).

## 문제 (라이브 빌드 실측)
메인 `PlantFlow_Support.csproj`가 리포 루트 기준 기본 글롭(`**/*.cs`)을 쓰는데 `tools\**`를 제외하지 않아,
`tools/HotReload/**/*.cs` 및 생성된 `tools/HotReload/**/obj/**/*.AssemblyInfo.cs`가 메인 프로젝트로 컴파일됨
→ 어셈블리 수준 속성 중복 `error CS0579`(AssemblyCompany/Version/TargetFramework 등) 36개 → 제품 빌드 실패.

## 수정 (메인 csproj에서 tools 제외)
`PlantFlow_Support.csproj`의 기존 `<Compile Remove .../>` 블록(28~34행 부근)에 아래 3줄 추가:
```xml
<Compile Remove="tools\**" />
<EmbeddedResource Remove="tools\**" />
<None Remove="tools\**" />
```
- 기존 Remove 항목(TestProject/Microsoft/PlantFlow_Support\obj·bin/Backup) 형식과 동일하게 나란히 추가.
- 그 외 제품 코드 로직 무변경.

## 대상 파일
- `PlantFlow_Support.csproj` (메인, 루트)

## 착수 후 정리 (커밋 불필요, 빌드 위생)
- 메인 프로젝트가 잘못 생성한 잔여 산출물 정리: `tools/HotReload/**/obj`, `tools/HotReload/**/bin`은
  다음 빌드에서 재생성되므로 삭제 가능(선택). git 미추적이라 커밋 영향 없음.

## 제약 (§0/§3/§3-A)
- `PlantFlow_Support.csproj`만 수정(Remove 3줄 추가). 다른 제품 코드/`Commands.cs`/tools/*.cs 무변경.
- `git add PlantFlow_Support.csproj` 만. `git add -A`·`.` 금지. 미커밋 타 파일 건드리지 말 것.
- 빈 결과 금지. 완료 후 커밋(attribution 금지).

## 완료 기준 (Acceptance)
- [ ] 메인 csproj에 tools\** Remove 3줄 추가(Compile/EmbeddedResource/None)
- [ ] 제품 코드 로직 무변경, PlantFlow_Support.csproj만 커밋

## 검증 게이트 (사용자 수동)
1. **메인 제품 빌드**(`CleanAndBuild.bat` 또는 기존 빌드 스크립트) → CS0579 소멸, GREEN 확인.
2. **스파이크는 별도 빌드**: `tools/HotReload/PfHotReload.sln`을 Debug/x64로 빌드
   (메인 빌드 스크립트로 tools를 빌드하지 말 것 — 별도 솔루션).
3. 산출물 `...\PfHotReload.Probe\bin\x64\Debug\PfHotReload.Probe.dll`,
   `...\PfHotReload.Bootstrap\bin\x64\Debug\PfHotReload.Bootstrap.dll`(+옆에 Contract.dll) 확인.
4. 이후 cycle 6 라이브 절차(Bootstrap NETLOAD → PFLOAD → PFRUN Ping → v2 rebuild → PFUNLOAD) 재개.
