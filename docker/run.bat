@echo off
if "%~1"=="" (
    echo Usage: run.bat ^<tag^>
    echo Example: run.bat v1.0.0
    exit /b 1
)

echo Starting Lattice stack with tag %~1...

set SERVER_IMAGE_TAG=%~1
set DASHBOARD_IMAGE_TAG=%~1

docker compose -f "%~dp0compose.yaml" up -d

echo Lattice stack started.
echo   Server: http://localhost:8000
echo   Dashboard: http://localhost:3000
