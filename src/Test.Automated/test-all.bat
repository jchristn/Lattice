@echo off
setlocal enabledelayedexpansion

echo ========================================
echo   Lattice Automated Tests - All Databases
echo ========================================
echo.

set FAILED=0
set PASSED=0

REM Test SQLite
echo [1/4] Testing SQLite...
echo ----------------------------------------
dotnet run -- sqlite test.db
if %ERRORLEVEL% EQU 0 (
    set /a PASSED+=1
    echo SQLite: PASSED
) else (
    set /a FAILED+=1
    echo SQLite: FAILED
)
echo.

REM Test PostgreSQL
echo [2/4] Testing PostgreSQL...
echo ----------------------------------------
dotnet run -- postgresql localhost 5432 postgres password lattice
if %ERRORLEVEL% EQU 0 (
    set /a PASSED+=1
    echo PostgreSQL: PASSED
) else (
    set /a FAILED+=1
    echo PostgreSQL: FAILED
)
echo.

REM Test MySQL
echo [3/4] Testing MySQL...
echo ----------------------------------------
dotnet run -- mysql localhost 3306 root password lattice
if %ERRORLEVEL% EQU 0 (
    set /a PASSED+=1
    echo MySQL: PASSED
) else (
    set /a FAILED+=1
    echo MySQL: FAILED
)
echo.

REM Test SQL Server
echo [4/4] Testing SQL Server...
echo ----------------------------------------
dotnet run -- sqlserver localhost 1433 sa password lattice
if %ERRORLEVEL% EQU 0 (
    set /a PASSED+=1
    echo SQL Server: PASSED
) else (
    set /a FAILED+=1
    echo SQL Server: FAILED
)
echo.

REM Summary
echo ========================================
echo   Summary
echo ========================================
echo Passed: %PASSED%
echo Failed: %FAILED%
echo.

if %FAILED% GTR 0 (
    echo Some tests failed!
    exit /b 1
) else (
    echo All tests passed!
    exit /b 0
)
