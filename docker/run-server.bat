@echo off
if "%~1"=="" (
    echo Usage: run-server.bat ^<tag^>
    echo Example: run-server.bat v1.0.0
    exit /b 1
)

echo Starting Lattice Server with tag %~1...

docker run -d ^
    --name lattice-server ^
    --restart unless-stopped ^
    -p 8000:8000 ^
    -v "%~dp0server\lattice.json:/app/lattice.json:ro" ^
    -v "%~dp0server\logs:/app/logs" ^
    -v "%~dp0server\data:/app/data" ^
    jchristn/lattice:%~1 ^
    lattice.json

echo Lattice Server started on port 8000.
