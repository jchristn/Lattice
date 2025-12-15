#!/bin/bash

echo "========================================"
echo "  Lattice Automated Tests - All Databases"
echo "========================================"
echo

FAILED=0
PASSED=0

# Test SQLite
echo "[1/4] Testing SQLite..."
echo "----------------------------------------"
if dotnet run -- sqlite test.db; then
    ((PASSED++))
    echo "SQLite: PASSED"
else
    ((FAILED++))
    echo "SQLite: FAILED"
fi
echo

# Test PostgreSQL
echo "[2/4] Testing PostgreSQL..."
echo "----------------------------------------"
if dotnet run -- postgresql localhost 5432 postgres password lattice; then
    ((PASSED++))
    echo "PostgreSQL: PASSED"
else
    ((FAILED++))
    echo "PostgreSQL: FAILED"
fi
echo

# Test MySQL
echo "[3/4] Testing MySQL..."
echo "----------------------------------------"
if dotnet run -- mysql localhost 3306 root password lattice; then
    ((PASSED++))
    echo "MySQL: PASSED"
else
    ((FAILED++))
    echo "MySQL: FAILED"
fi
echo

# Test SQL Server
echo "[4/4] Testing SQL Server..."
echo "----------------------------------------"
if dotnet run -- sqlserver localhost 1433 sa password lattice; then
    ((PASSED++))
    echo "SQL Server: PASSED"
else
    ((FAILED++))
    echo "SQL Server: FAILED"
fi
echo

# Summary
echo "========================================"
echo "  Summary"
echo "========================================"
echo "Passed: $PASSED"
echo "Failed: $FAILED"
echo

if [ $FAILED -gt 0 ]; then
    echo "Some tests failed!"
    exit 1
else
    echo "All tests passed!"
    exit 0
fi
