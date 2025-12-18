@echo off
if "%~1"=="" (
    echo Usage: build-server.bat ^<tag^>
    echo Example: build-server.bat v1.0.0
    exit /b 1
)

echo Building jchristn/lattice:%~1 for linux/amd64 and linux/arm64/v8...

docker buildx build ^
    --platform linux/amd64,linux/arm64/v8 ^
    -t jchristn/lattice:%~1 ^
    -f Lattice.Server/Dockerfile ^
    --push ^
    .

echo Build complete.
