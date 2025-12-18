@echo off
if "%~1"=="" (
    echo Usage: run-dashboard.bat ^<tag^>
    echo Example: run-dashboard.bat v1.0.0
    exit /b 1
)

echo Starting Lattice Dashboard with tag %~1...

docker run -d ^
    --name lattice-dashboard ^
    --restart unless-stopped ^
    -p 3000:80 ^
    jchristn/lattice-ui:%~1

echo Lattice Dashboard started on port 3000.
