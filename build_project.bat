@echo off
echo Starting PlantFlow_Support Build...
dotnet build "C:\Users\HT노승환\Documents\PlantFlow_Support\PlantFlow_Support.sln"
if %ERRORLEVEL% EQU 0 (
    echo.
    echo =======================
    echo    Build SUCCESSFUL
    echo =======================
) else (
    echo.
    echo =======================
    echo    Build FAILED
    echo =======================
)
pause
