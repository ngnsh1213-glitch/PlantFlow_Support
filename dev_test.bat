@echo off
setlocal EnableExtensions
rem PFS dev loop Tier1: build - deploy - restart 2026 - launch via PowerShell (unicode-safe)
rem NOTE: PFSNOTABTEST reads env PFS_NOTAB_TEST_TAG (default GD1-001). Set it here to auto-test a specific tag,
rem       or run PFSNOTABDETAIL in ACAD and pick the support manually to test any type.

set "PROJ=%~dp0PlantFlow_Support.csproj"
set "SRC=%~dp0bin\Release\PlantFlow_Support.dll"
set "DEPLOY=C:\Lisp\PlantFlow_Support"
set "LAUNCH=C:\Lisp\pfs_launch.ps1"

rem --- notab dimension/callout text height (arrow kept separate via PFS_NOTAB_DIM_ARR) ---
set "PFS_NOTAB_DIM_TXT=8"

rem --- BOM spike (measurement only; logs BOMs rows, no output change). Set 0 to disable. ---
set "PFS_NOTAB_BOM_SPIKE=1"

rem --- auto-extract tags (comma-separated). PFSNOTABTEST loops each. Edit to change types. ---
set "PFS_NOTAB_TEST_TAG=RC5-001"

rem --- PFSNOTABBATCH dry-run (1 = classify + log only, no drawings) ---
rem     PFSNOTABBATCH needs an interactive selection, so it is NOT auto-run by this script.
rem     Usage: run this script, then in ACAD select everything (window/crossing) and type PFSNOTABBATCH.
rem     Logs: "PFSNOTABBATCH probe" per item (ShortDescription etc.) and "probe-summary" totals.
rem     Set back to 0 to actually extract drawings.
set "PFS_NOTAB_BATCH_DRYRUN=1"

rem --- PFSNOTABBATCH attachment exclusion (Plant ShortDescription, comma-separated) ---
rem     Default in code = UB,UB1,UB2,UBD (all ubolt variants). Excluded parts get no drawing of their own;
rem     they are still pulled into the parent support drawing by AutoIncludeRelatedParts.
rem     Uncomment and edit only if the catalog adds a new attachment code.
rem set "PFS_NOTAB_BATCH_EXCLUDE=UB,UB1,UB2,UBD"

rem --- notab callout text position offsets (paper mm, +X=right +Y=up, range +-2000) ---
rem     Edit these numbers and re-run (no code change). Inherited by AutoCAD via child process env.
set "PFS_NOTAB_PIPE_CALLOUT_DX=180"
set "PFS_NOTAB_MEMBER_CALLOUT_DX=5"

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
echo  [log] Backing up and clearing C:\Temp\pfs_diag.log ...
if not exist "C:\Temp" mkdir "C:\Temp"
if exist "C:\Temp\pfs_diag.log" copy /Y "C:\Temp\pfs_diag.log" "C:\Temp\pfs_diag.prev.log" >nul
if exist "C:\Temp\pfs_diag.log" del /Q "C:\Temp\pfs_diag.log"

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
