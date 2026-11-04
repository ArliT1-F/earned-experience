#!/usr/bin/env bash
set -euo pipefail

# Build Earned Experience for RimWorld
# Usage: RIMWORLD_DIR=/path/to/RimWorld/Managed ./build.sh
# Example (Linux): RIMWORLD_DIR=~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed ./build.sh
# Example (Windows): RIMWORLD_DIR="/c/Program Files (x86)/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed" ./build.sh

if [[ -z "${RIMWORLD_DIR:-}" ]]; then
  echo "RIMWORLD_DIR not set. Define it as the RimWorld Managed folder containing Assembly-CSharp.dll"
  echo "Example: export RIMWORLD_DIR=~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed"
  exit 1
fi

if [[ ! -f "$RIMWORLD_DIR/Assembly-CSharp.dll" ]]; then
  echo "Assembly-CSharp.dll not found in RIMWORLD_DIR=$RIMWORLD_DIR"
  exit 1
fi

echo "Building EarnedExperience with RIMWORLD_DIR=$RIMWORLD_DIR"
dotnet build Source/EarnedExperience/EarnedExperience.csproj -c Release

echo "Done. Output: Assemblies/EarnedExperience.dll"
ls -lh Assemblies/EarnedExperience.dll
