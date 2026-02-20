@echo off
if "%~1"=="" (
    echo Usage: build-dashboard.bat ^<tag^>
    echo Example: build-dashboard.bat v1.0.0
    exit /b 1
)

echo Building jchristn77/lattice-ui:%~1 for linux/amd64 and linux/arm64/v8...

docker buildx build ^
    --platform linux/amd64,linux/arm64/v8 ^
    -t jchristn77/lattice-ui:%~1 ^
    -t jchristn77/lattice-ui:latest ^
    -f Dockerfile ^
    --push ^
    .

echo Build complete.
