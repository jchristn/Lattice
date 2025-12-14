@echo off
REM Lattice Server Cleanup Script for Windows
REM Removes logs, databases, settings, and data directories

echo Cleaning Lattice Server data...

REM Delete logs directory
if exist "logs" (
    echo Removing logs directory...
    rmdir /s /q "logs"
)

REM Delete lattice.json settings file
if exist "lattice.json" (
    echo Removing lattice.json...
    del /f /q "lattice.json"
)

REM Delete database files
if exist "lattice.db" (
    echo Removing lattice.db...
    del /f /q "lattice.db"
)

REM Delete any other .db files
for %%f in (*.db) do (
    echo Removing %%f...
    del /f /q "%%f"
)

REM Delete documents directory
if exist "documents" (
    echo Removing documents directory...
    rmdir /s /q "documents"
)

REM Delete data directory
if exist "data" (
    echo Removing data directory...
    rmdir /s /q "data"
)

echo Cleanup complete.
