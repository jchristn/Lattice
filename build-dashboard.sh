#!/bin/bash
set -e

if [ -z "$1" ]; then
    echo "Usage: ./build-dashboard.sh <tag>"
    echo "Example: ./build-dashboard.sh v1.0.0"
    exit 1
fi

cd "$(dirname "$0")"

echo "Building jchristn77/lattice-ui:$1 for linux/amd64 and linux/arm64/v8..."

docker buildx build \
    --builder cloud-jchristn77-jchristn77 \
    --platform linux/amd64,linux/arm64/v8 \
    -t jchristn77/lattice-ui:$1 \
    -t jchristn77/lattice-ui:latest \
    -f dashboard/Dockerfile \
    --push \
    dashboard

echo "Pulling pushed images into the local registry..."
docker pull jchristn77/lattice-ui:$1
docker pull jchristn77/lattice-ui:latest

echo "Build complete."
