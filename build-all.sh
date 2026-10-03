#!/bin/bash

if [ -z "$1" ]; then
    echo "Usage: ./build-all.sh <tag>"
    echo "Example: ./build-all.sh v1.0.0"
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "Building all Lattice images with tag $1..."
echo

echo "=== Building server ==="
if ! "$SCRIPT_DIR/build-server.sh" "$1"; then
    echo "build-server.sh failed."
    exit 1
fi
echo

echo "=== Building dashboard ==="
if ! "$SCRIPT_DIR/build-dashboard.sh" "$1"; then
    echo "build-dashboard.sh failed."
    exit 1
fi
echo

echo "All builds complete."
