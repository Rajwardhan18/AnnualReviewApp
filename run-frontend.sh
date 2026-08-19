#!/usr/bin/env bash
# Starts the React (Vite) dev server on http://localhost:5173
set -euo pipefail

export PATH="/opt/homebrew/bin:$PATH"

cd "$(dirname "$0")/frontend"
if [ ! -d node_modules ]; then
  echo "Installing frontend dependencies..."
  npm install
fi
echo "Starting frontend on http://localhost:5173 ..."
npm run dev
