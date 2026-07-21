@echo off
title ISO ReadOnly Watcher

set "TARGET=%LOCALAPPDATA%\Autodesk\AutoCAD Plant 3D\CollaborationCache\SEP(SAMSUNG)\Isometric\Check_A2\ProdIsos\Drawings"

echo ============================================
echo   ISO DWG Read-Only Auto Remover
echo   Watching every 3 seconds...
echo   Close this window to stop.
echo ============================================
echo.
echo Target: %TARGET%
echo.

if not exist "%TARGET%\" (
    echo [ERROR] Folder not found.
    pause
    exit /b 1
)

echo [OK] Folder found. Watching...
echo.

:LOOP
pushd "%TARGET%"
for %%f in (*.dwg) do (
    attrib -r "%%f"
)
popd
timeout /t 3 /nobreak >nul
goto LOOP
