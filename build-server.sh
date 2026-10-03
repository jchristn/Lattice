#!/bin/bash
set -e

if [ -z "$1" ]; then
    echo "Usage: ./build-server.sh <tag>"
    echo "Example: ./build-server.sh v1.0.0"
    exit 1
fi

cd "$(dirname "$0")"

echo "Building jchristn77/lattice:$1 for linux/amd64 and linux/arm64/v8..."

docker buildx build \
    --builder cloud-jchristn77-jchristn77 \
    --platform linux/amd64,linux/arm64/v8 \
    -t jchristn77/lattice:$1 \
    -t jchristn77/lattice:latest \
    -f src/Lattice.Server/Dockerfile \
    --push \
    src

echo "Pulling pushed images into the local registry..."
docker pull jchristn77/lattice:$1
docker pull jchristn77/lattice:latest

echo "Build complete."
