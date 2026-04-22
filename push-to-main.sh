#!/usr/bin/env bash
# MedBridge EMR — merge feature branch into main and push to GitHub
# Usage: ./push-to-main.sh
#   Or:  GITHUB_TOKEN=ghp_xxx ./push-to-main.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FEATURE="claude/medication-management-interface-nuclU"
MAIN="main"
REPO="mamishimadire/Hopital-system"

cd "$SCRIPT_DIR"

echo "┌──────────────────────────────────────┐"
echo "│  MedBridge EMR — Push to main        │"
echo "└──────────────────────────────────────┘"
echo ""

# ── Require GitHub token
if [[ -z "${GITHUB_TOKEN:-}" ]]; then
  echo "Enter your GitHub Personal Access Token (classic with 'repo' scope):"
  read -rs GITHUB_TOKEN
  echo ""
fi

REMOTE_URL="https://${GITHUB_TOKEN}@github.com/${REPO}.git"
CURRENT_BRANCH="$(git branch --show-current)"

echo "[1/5] Saving current branch: $CURRENT_BRANCH"

echo "[2/5] Fetching latest main from GitHub..."
git fetch "$REMOTE_URL" "$MAIN":refs/remotes/origin/"$MAIN" --no-tags 2>/dev/null

echo "[3/5] Switching to $MAIN and pulling latest..."
git checkout "$MAIN"
git merge --ff-only origin/"$MAIN" 2>/dev/null || git reset --hard origin/"$MAIN"

echo "[4/5] Merging $FEATURE into $MAIN..."
git merge --no-ff "$FEATURE" -m "Merge MedBridge EMR feature branch into main

Includes:
- Full ASP.NET Core 8 MVC hospital management system
- Role-based access: Admin, Doctor, Nurse, Pharmacist, Procurement, Finance, Auditor, Supplier
- Medication management, prescriptions, procurement, blockchain audit trail
- Help Desk ticketing system with comments and priority management
- Password expiry, account lockout, email notifications
- PostgreSQL database with EF Core migrations
- Fully responsive UI (phones, tablets, laptops, TV/4K screens)"

echo "[5/5] Pushing main to GitHub..."
git push "$REMOTE_URL" "$MAIN"

echo ""
echo "[+] Returning to $CURRENT_BRANCH..."
git checkout "$CURRENT_BRANCH"

echo ""
echo "╔══════════════════════════════════════════╗"
echo "║  All changes pushed to main!             ║"
echo "║  https://github.com/$REPO  ║"
echo "╚══════════════════════════════════════════╝"
