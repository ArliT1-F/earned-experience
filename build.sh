#!/usr/bin/env bash
set -euo pipefail

# Build Earned Experience for RimWorld
# Works with no env setup if RimWorld is in a default Steam location.
# Override with: RIMWORLD_DIR=/path/to/RimWorld*_Data/Managed ./build.sh

# ---- auto-detect RimWorld Managed dir if RIMWORLD_DIR not set ----
if [[ -z "${RIMWORLD_DIR:-}" ]]; then
  candidates=(
    # Linux
    "$HOME/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed"
    "$HOME/.local/share/Steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed"
    "$HOME/snap/steam/common/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed"
    # Windows (when running under Git Bash / WSL / MSYS)
    "C:/Program Files (x86)/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed"
    "C:/Program Files/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed"
    "C:/Program Files (x86)/Steam/steamapps/common/RimWorld/RimWorldWin_Data/Managed"
    "C:/Program Files/Steam/steamapps/common/RimWorld/RimWorldWin_Data/Managed"
    "D:/SteamLibrary/steamapps/common/RimWorld/RimWorldWin64_Data/Managed"
    "D:/SteamLibrary/steamapps/common/RimWorld/RimWorldWin_Data/Managed"
    # Alternate drive letter sometimes used
    "E:/SteamLibrary/steamapps/common/RimWorld/RimWorldWin64_Data/Managed"
  )
  for c in "${candidates[@]}"; do
    if [[ -f "$c/Assembly-CSharp.dll" ]]; then
      RIMWORLD_DIR="$c"
      echo "Auto-detected RimWorld at: $RIMWORLD_DIR"
      break
    fi
  done
fi

if [[ -z "${RIMWORLD_DIR:-}" ]]; then
  echo "RimWorld not found in default locations and RIMWORLD_DIR not set."
  echo "Set it manually, e.g.:"
  echo "  Linux:   export RIMWORLD_DIR=~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed"
  echo "  Windows: export RIMWORLD_DIR=\"C:/Program Files (x86)/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed\""
  echo ""
  echo "Then re-run: ./build.sh"
  exit 1
fi

if [[ ! -f "$RIMWORLD_DIR/Assembly-CSharp.dll" ]]; then
  echo "Assembly-CSharp.dll not found in RIMWORLD_DIR=$RIMWORLD_DIR"
  echo "Check the path points to the Managed folder (must contain Assembly-CSharp.dll, UnityEngine.*.dll, 0Harmony.dll)"
  exit 1
fi

export RIMWORLD_DIR
echo "Building EarnedExperience with RIMWORLD_DIR=$RIMWORLD_DIR"
dotnet build Source/EarnedExperience/EarnedExperience.csproj -c Release

echo "Done. Output: Assemblies/EarnedExperience.dll"
ls -lh Assemblies/EarnedExperience.dll
