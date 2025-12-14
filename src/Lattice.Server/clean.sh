#!/bin/bash
# Lattice Server Cleanup Script for Linux/Mac
# Removes logs, databases, settings, and data directories

echo "Cleaning Lattice Server data..."

# Delete logs directory
if [ -d "logs" ]; then
    echo "Removing logs directory..."
    rm -rf "logs"
fi

# Delete lattice.json settings file
if [ -f "lattice.json" ]; then
    echo "Removing lattice.json..."
    rm -f "lattice.json"
fi

# Delete database files
if [ -f "lattice.db" ]; then
    echo "Removing lattice.db..."
    rm -f "lattice.db"
fi

# Delete any other .db files
for f in *.db; do
    if [ -f "$f" ]; then
        echo "Removing $f..."
        rm -f "$f"
    fi
done

# Delete documents directory
if [ -d "documents" ]; then
    echo "Removing documents directory..."
    rm -rf "documents"
fi

# Delete data directory
if [ -d "data" ]; then
    echo "Removing data directory..."
    rm -rf "data"
fi

echo "Cleanup complete."
