#!/bin/bash
# MedBridge Codespace setup — runs once after container creation
set -e

echo "=== Setting up MedBridge database ==="
sudo service postgresql start 2>/dev/null || true
sleep 2

# Create DB user and database
sudo -u postgres psql -c "CREATE USER medbridge WITH PASSWORD 'medbridge123';" 2>/dev/null || echo "User already exists"
sudo -u postgres psql -c "CREATE DATABASE \"MedBridgeEMR\" OWNER medbridge;" 2>/dev/null || echo "Database already exists"
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE \"MedBridgeEMR\" TO medbridge;" 2>/dev/null || true

echo "=== Database ready ==="
echo ""
echo "╔══════════════════════════════════════════╗"
echo "║  MedBridge is set up!                    ║"
echo "║  Run:  bash run.sh                       ║"
echo "║  Login: Mamishi.Madire@admin             ║"
echo "║  Pass : Admin123@                        ║"
echo "╚══════════════════════════════════════════╝"
