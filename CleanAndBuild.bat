@echo off
setlocal EnableExtensions
rem [2026-06-18] PFS 빌드 스크립트. PFO CleanAndBuild.bat 구조 차용(평면 call/goto, 실패 경로 -> :end pause).
rem PFS = 단일 프로젝트(net8.0-windows, AutoCAD 2026). frontend/멀티버전/DeployBundle 없음.
rem 모든 실패 경로가 :end(pause)로 수렴 -> 창이 즉시 닫히지 않음.

cd /d "%~dp0"

echo ==========================================
echo Force Cleaning Project (bin/obj)...
echo ==========================================
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

echo.
echo ==========================================
echo Building frontend_support (three.js preview, Phase B)...
echo ==========================================
rem [프리뷰 B] frontend_support/dist 생성. WebViewControl.FindDistFolder가 bin 상위에서 탐색.
rem npm 미설치/빌드 실패는 경고 후 계속(기존 dist 폴백). 빌드오프라인!=런타임오프라인(§9).
if not exist "frontend_support\package.json" (
  echo   [SKIP] frontend_support 없음 - 프리뷰 B 비활성.
  goto :afterfront
)
where npm >nul 2>&1
if errorlevel 1 (
  echo   [WARN] npm 미설치 - frontend 빌드 건너뜀. 기존 dist 사용^(있으면^).
  goto :afterfront
)
pushd frontend_support
if not exist "node_modules" (
  echo   Installing npm dependencies ^(최초 1회, 네트워크 필요^)...
  call npm install
  if errorlevel 1 echo   [WARN] npm install 실패 - 기존 dist 폴백.
)
call npm run build
if errorlevel 1 (
  echo   [WARN] frontend 빌드 실패 - 기존 dist 폴백^(있으면^).
) else (
  echo   [OK] frontend_support\dist 생성.
)
popd
:afterfront

echo.
echo ==========================================
echo Restoring NuGet Packages...
echo ==========================================
dotnet restore PlantFlow_Support.csproj --force

echo.
echo ==========================================
echo Building for AutoCAD 2026 (net8.0-windows, Release)...
echo ==========================================
echo Shutting down stray .NET build servers (obj DLL lock prevention)...
dotnet build-server shutdown >nul 2>&1
del build_error.log >nul 2>&1

rem -nodeReuse:false releases obj DLL handles (prevents CS2012 lock errors).
dotnet build PlantFlow_Support.csproj -c Release -nodeReuse:false >> build_error.log 2>&1
if errorlevel 1 goto :buildfail

echo   [OK] AutoCAD 2026 (net8.0-windows) build succeeded.

echo.
echo ==========================================
echo Build artifact:
if exist "bin\Release\PlantFlow_Support.dll" echo   [OK]    bin\Release\PlantFlow_Support.dll present.
if not exist "bin\Release\PlantFlow_Support.dll" echo   [STALE] bin\Release\PlantFlow_Support.dll NOT FOUND - check log.
echo ==========================================

echo.
echo ==========================================
echo Deploying to C:\Lisp\PlantFlow_Support ...
echo ==========================================
rem [2026-07-06] 빌드 산출물을 기존 로드 경로(C:\Lisp\PlantFlow_Support)에도 복사.
rem DLL 미갱신 방지 위해 빌드 성공 시에만 수행. xcopy /Y /E로 runtimes 하위폴더 포함.
if not exist "bin\Release\PlantFlow_Support.dll" (
  echo   [SKIP] 산출물 없음 - 배포 건너뜀.
  goto :end
)
set "DEPLOY_DIR=C:\Lisp\PlantFlow_Support"
if not exist "%DEPLOY_DIR%" mkdir "%DEPLOY_DIR%"
xcopy "bin\Release\*" "%DEPLOY_DIR%\" /Y /E >nul
if errorlevel 1 (
  echo   [WARN] C:\Lisp\PlantFlow_Support 복사 실패 - 대상 사용 중/권한 확인.
) else (
  echo   [OK] C:\Lisp\PlantFlow_Support 배포 완료.
)

rem [프리뷰 B] frontend_support\dist도 배포. FindDistFolder가 어셈블리 상위에서
rem frontend_support\dist\index.html 탐색 -> 반드시 동일 서브폴더 구조로 복사.
if exist "frontend_support\dist\index.html" (
  xcopy "frontend_support\dist\*" "%DEPLOY_DIR%\frontend_support\dist\" /Y /E >nul
  if errorlevel 1 (
    echo   [WARN] frontend_support\dist 복사 실패 - 대상 사용 중/권한 확인.
  ) else (
    echo   [OK] frontend_support\dist 배포 완료.
  )
) else (
  echo   [SKIP] frontend_support\dist 없음 - 프리뷰 B dist 미배포^(기존 폴백^).
)
echo ==========================================
goto :end

rem ---------------- failure paths ----------------

:buildfail
echo.
echo ==========================================
echo ERROR: dotnet build failed. Full output below (also in build_error.log):
echo ==========================================
type build_error.log
goto :end

:end
echo.
echo ==========================================
echo Done. Press any key to close.
echo ==========================================
pause
endlocal
exit /b
