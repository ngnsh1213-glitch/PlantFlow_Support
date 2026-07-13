@echo off
setlocal EnableExtensions
rem PFS dev loop Tier1: build - deploy - restart 2026 - launch via PowerShell (unicode-safe)

set "PROJ=%~dp0PlantFlow_Support.csproj"
set "SRC=%~dp0bin\Release\PlantFlow_Support.dll"
set "DEPLOY=C:\Lisp\PlantFlow_Support"
set "LAUNCH=C:\Lisp\pfs_launch.ps1"

echo ============================================================
echo  [1/4] Building (Release, incremental)...
echo ============================================================
dotnet build "%PROJ%" -c Release --nologo
if errorlevel 1 goto :buildfail

echo.
echo  [2/4] Deploying to %DEPLOY% ...
if not exist "%SRC%" echo   [ERROR] build output missing: %SRC% & goto :end
if not exist "%DEPLOY%" mkdir "%DEPLOY%"
xcopy "%~dp0bin\Release\*" "%DEPLOY%\" /Y /E >nul
if errorlevel 1 echo   [WARN] deploy copy failed (target in use?) & goto :end
echo   [OK] deployed.

echo.
echo  [3/4] Closing AutoCAD 2026 (only 2026) ...
powershell -NoProfile -Command "Get-Process acad -ErrorAction SilentlyContinue | Where-Object { $_.Path -like '*Autodesk\AutoCAD 2026*' } | Stop-Process -Force"
timeout /t 2 /nobreak >nul

echo.
echo  [4/4] Launching Plant 3D 2026 (PowerShell, unicode-safe) ...
powershell -NoProfile -ExecutionPolicy Bypass -File "%LAUNCH%"
goto :end

:buildfail
echo.
echo  BUILD FAILED. Fix errors above. AutoCAD not restarted.

:end
echo.
pause
endlocal
