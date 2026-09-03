@echo off
if "%~1"=="" (
    echo Usage: build-all.bat ^<tag^>
    echo Example: build-all.bat v1.0.0
    exit /b 1
)

echo Building all Lattice images with tag %~1...
echo.

echo === Building server ===
call "%~dp0build-server.bat" %~1
if errorlevel 1 (
    echo build-server.bat failed.
    exit /b 1
)
echo.

echo === Building dashboard ===
call "%~dp0build-dashboard.bat" %~1
if errorlevel 1 (
    echo build-dashboard.bat failed.
    exit /b 1
)
echo.

echo All builds complete.
