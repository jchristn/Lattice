@echo off
setlocal

echo ============================================
echo   Lattice Docker Factory Reset
echo ============================================
echo.
echo WARNING: This will stop the Docker containers and restore all
echo configuration and data files to their factory defaults.
echo.
echo The following will be reset:
echo   - Server configuration (lattice.json)
echo   - Database (lattice.db) - all data will be lost
echo   - Log files will be deleted
echo.

set /p CONFIRM="Type RESET to confirm: "
if /i not "%CONFIRM%"=="RESET" (
    echo Reset cancelled.
    exit /b 1
)

echo.
echo Stopping Docker containers...
docker compose -f "%~dp0..\compose.yaml" down 2>nul
docker compose -f "%~dp0..\..\docker-compose.yml" down 2>nul

echo.
echo Restoring factory configuration...
copy /y "%~dp0lattice.json" "%~dp0..\server\lattice.json" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy lattice.json
) else (
    echo   [OK] lattice.json restored
)

echo.
echo Restoring factory database...
del /q "%~dp0..\server\data\lattice.db" 2>nul
del /q "%~dp0..\server\data\lattice.db-shm" 2>nul
del /q "%~dp0..\server\data\lattice.db-wal" 2>nul
copy /y "%~dp0lattice.db" "%~dp0..\server\data\lattice.db" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy lattice.db
) else (
    echo   [OK] lattice.db restored
)

echo.
echo Deleting log files...
del /q "%~dp0..\server\logs\*.log" 2>nul
for /d %%d in ("%~dp0..\server\logs\*") do (
    del /q "%%d\*.log" 2>nul
)
echo   [OK] Log files deleted

echo.
echo ============================================
echo   Factory reset complete.
echo   Run 'run.bat <tag>' to start Lattice.
echo ============================================

endlocal
