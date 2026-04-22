#!/usr/bin/env bash
# MedBridge EMR — startup script
# Usage: ./run.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP_DIR="$SCRIPT_DIR/MedBridge"
PORT=5000
URL="http://localhost:$PORT"
LOG="/tmp/medbridge.log"

echo "┌──────────────────────────────────────┐"
echo "│  MedBridge EMR — Starting up         │"
echo "└──────────────────────────────────────┘"
echo ""

# ── 1. Ensure PostgreSQL is running
echo "[1/4] Checking PostgreSQL..."
if ! pg_isready -q 2>/dev/null; then
  echo "      PostgreSQL is not running — starting it..."
  pg_ctlcluster 16 main start 2>/dev/null \
    || sudo service postgresql start 2>/dev/null \
    || sudo systemctl start postgresql 2>/dev/null \
    || { echo "      ERROR: Could not start PostgreSQL. Start it manually then re-run."; exit 1; }
  sleep 3
fi
echo "      PostgreSQL is ready ✓"

# ── 2. Kill any previous instance of the app
pkill -f "dotnet.*MedBridge" 2>/dev/null || true
sleep 1

# ── 3. Start the app
export PATH="$PATH:$HOME/.dotnet/tools"
echo "[2/4] Building and launching MedBridge..."
echo "      Logs → $LOG"
nohup dotnet run --project "$APP_DIR" --urls "$URL" > "$LOG" 2>&1 &
APP_PID=$!
echo "      App PID: $APP_PID"

# ── 4. Wait for app to respond (up to 60 seconds)
echo "[3/4] Waiting for app to be ready..."
READY=0
for i in $(seq 1 30); do
  HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$URL/" 2>/dev/null || true)
  if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "302" ]]; then
    READY=1
    break
  fi
  printf "      (%ds)...\r" "$((i * 2))"
  sleep 2
done

if [[ $READY -eq 0 ]]; then
  echo ""
  echo "      WARNING: App may still be starting. Check log: $LOG"
fi
echo "      App is ready ✓"

# ── 5. Open browser
echo "[4/4] Opening browser at $URL ..."
xdg-open "$URL" 2>/dev/null \
  || sensible-browser "$URL" 2>/dev/null \
  || google-chrome "$URL" 2>/dev/null \
  || chromium-browser "$URL" 2>/dev/null \
  || firefox "$URL" 2>/dev/null \
  || echo "      No browser detected — open manually: $URL"

echo ""
echo "╔══════════════════════════════════════════╗"
echo "║  MedBridge EMR is running!               ║"
echo "║                                          ║"
echo "║  URL  :  http://localhost:5000           ║"
echo "║  Login:  Mamishi.Madire@admin            ║"
echo "║  Pass :  Admin123@                       ║"
echo "║                                          ║"
echo "║  Logs :  tail -f /tmp/medbridge.log      ║"
echo "║  Stop :  pkill -f 'dotnet.*MedBridge'    ║"
echo "╚══════════════════════════════════════════╝"
