#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DOCKER_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

echo "============================================"
echo "  Lattice Docker Factory Reset"
echo "============================================"
echo ""
echo "WARNING: This will stop the Docker containers and restore all"
echo "configuration and data files to their factory defaults."
echo ""
echo "The following will be reset:"
echo "  - Server configuration (lattice.json)"
echo "  - Database (lattice.db) - all data will be lost"
echo "  - Log files will be deleted"
echo ""

read -rp "Type RESET to confirm: " CONFIRM
if [ "$CONFIRM" != "RESET" ]; then
    echo "Reset cancelled."
    exit 1
fi

echo ""
echo "Stopping Docker containers..."
docker compose -f "$DOCKER_DIR/compose.yaml" down 2>/dev/null
docker compose -f "$ROOT_DIR/docker-compose.yml" down 2>/dev/null

echo ""
echo "Restoring factory configuration..."
if cp "$SCRIPT_DIR/lattice.json" "$DOCKER_DIR/server/lattice.json"; then
    echo "  [OK] lattice.json restored"
else
    echo "  ERROR: Failed to copy lattice.json"
fi

echo ""
echo "Restoring factory database..."
rm -f "$DOCKER_DIR/server/data/lattice.db" \
      "$DOCKER_DIR/server/data/lattice.db-shm" \
      "$DOCKER_DIR/server/data/lattice.db-wal"
if cp "$SCRIPT_DIR/lattice.db" "$DOCKER_DIR/server/data/lattice.db"; then
    echo "  [OK] lattice.db restored"
else
    echo "  ERROR: Failed to copy lattice.db"
fi

echo ""
echo "Deleting log files..."
find "$DOCKER_DIR/server/logs" -name "*.log" -type f -delete 2>/dev/null
echo "  [OK] Log files deleted"

echo ""
echo "============================================"
echo "  Factory reset complete."
echo "  Run './run.bat <tag>' to start Lattice."
echo "============================================"
