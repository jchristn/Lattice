@echo off
if "%~1"=="" (
    echo Usage: build-dashboard.bat ^<tag^>
    echo Example: build-dashboard.bat v1.0.0
    exit /b 1
)

echo Building jchristn/lattice-ui:%~1 for linux/amd64 and linux/arm64/v8...

docker buildx build ^
    --platform linux/amd64,linux/arm64/v8 ^
    -t jchristn/lattice-ui:%~1 ^
    -f Dockerfile ^
    --push ^
    .

echo Build complete.
