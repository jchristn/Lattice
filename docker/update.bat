@echo off
REM Pull the latest images, recreate the stack, and show container status.
REM Optional argument: image tag (defaults to the tag pinned in compose.yaml).

if not "%~1"=="" (
    set SERVER_IMAGE_TAG=%~1
    set DASHBOARD_IMAGE_TAG=%~1
    echo Updating Lattice stack with tag %~1...
) else (
    echo Updating Lattice stack...
)

docker compose -f "%~dp0compose.yaml" pull
if errorlevel 1 (
    echo docker compose pull failed.
    exit /b 1
)

docker compose -f "%~dp0compose.yaml" down
if errorlevel 1 (
    echo docker compose down failed.
    exit /b 1
)

docker compose -f "%~dp0compose.yaml" up -d
if errorlevel 1 (
    echo docker compose up failed.
    exit /b 1
)

docker ps -a

echo Update complete.
