#!/usr/bin/env bash
# Starts the .NET 9 API on http://localhost:5099
set -euo pipefail

# Make the Homebrew .NET 9 SDK discoverable without touching global PATH.
export PATH="/opt/homebrew/opt/dotnet@9/bin:$PATH:$HOME/.dotnet/tools"
export DOTNET_ROOT="/opt/homebrew/opt/dotnet@9/libexec"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

cd "$(dirname "$0")/backend/PlanReview.Api"
echo "Starting API on http://localhost:5099 (Swagger at /swagger) ..."
dotnet run
