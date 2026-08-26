#!/usr/bin/env bash
# Starts the .NET 10 API on http://localhost:5099
set -euo pipefail

# Make a .NET 10 SDK discoverable without touching global PATH. Checks the usual
# install locations (official installer, Homebrew) and falls back to whatever
# `dotnet` is already on PATH.
for candidate in /usr/local/share/dotnet /opt/homebrew/opt/dotnet/libexec "$HOME/.dotnet"; do
  if [ -x "$candidate/dotnet" ]; then
    export DOTNET_ROOT="$candidate"
    export PATH="$candidate:$PATH"
    break
  fi
done
export PATH="$PATH:$HOME/.dotnet/tools"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

cd "$(dirname "$0")/backend/PlanReview.Api"
echo "Starting API on http://localhost:5099 (Swagger at /swagger) ..."
dotnet run
