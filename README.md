# Earned Experience

> If a pawn spends time doing useful work, that work should contribute to their development.

**Earned Experience** is a RimWorld mod that fills the gaps in vanilla skill progression. Several activities that consume serious pawn labour — stonecutting, smelting, cleaning, hauling, refueling, trap rearming, cremation, firefighting and patient feeding — award **no XP at all** in vanilla RimWorld. This mod awards fair, configurable XP for those activities while staying perfectly in tune with vanilla balance.

- **Target versions:** RimWorld 1.6 and 1.7 (Windows + Linux)
- **Dependencies:** None required. [Harmony](https://github.com/pardeike/Harmony) is *recommended* for full functionality but the mod degrades gracefully without it.
- **DLC:** None required.
- **Compatibility:** Designed for Steam Workshop. No Def replacements, no save-breaking changes.

---

## Supported Activities

| Activity | Workbench / Job | XP Skill (default) | XP Source | Vanilla XP? |
|---|---|---|---|---|
| **Stonecutting** | Stonecutter's Table (`Do_Bill` `Make_StoneBlocks`) | Crafting | Work ticks (1.0x) | **None** |
| **Smelting** | Fueled / Electric Smelter (`Smelt*`) | Crafting | Work ticks (1.0x) | **None** |
| **Cleaning** | `CleanFilth` | Crafting (0.5x) | Work ticks | **None** |
| **Hauling** | `HaulToCell` / `HaulToContainer` / `HaulToTransporter` | Crafting (0.5x) | Distance + mass on completion | **None** |
| **Transport loading** | `LoadTransporter` | Crafting (0.5x) | Mass on completion | **None** |
| **Refueling** | `Refuel` / `RefuelAtomic` | Construction (0.5x) | Work ticks | **None** |
| **Trap rearming** | `RearmTrap` | Crafting (0.75x) | Work ticks | **None** |
| **Cremation** | Crematorium (`BurnCorpse` etc.) | Crafting (0.5x) | Work ticks | **None** |
| **Firefighting** | `BeatFire` | Construction (1.0x) | Work ticks | **None** |
| **Patient care** | `FeedPatient` | Medicine (0.75x) | Work ticks | **None** (tending already gives Medicine — not duplicated) |

All multipliers are defaults; change them in **Options → Mod Settings → Earned Experience**.

### Activities deliberately excluded (vanilla already awards XP)

- **Repairing** → Construction
- **Tending / surgery / doctoring** → Medicine
- **Construction / building** → Construction
- **Growing / harvesting** → Plants
- **Mining** → Mining

See [Technical Report](docs/TECHNICAL_REPORT.md) for the full verification.

---

## How XP Works

- **Proportional to work:** a pawn who stonecuts for 10 s earns less than one who cuts for 60 s. Hauling XP scales with distance and mass.
- **Respects vanilla rules:** passion multipliers, learning rate, daily learning saturation, and the great-memory / fast-learner genes all apply — XP is routed through `SkillRecord.Learn()` exactly as vanilla does.
- **Anti-exploit:** canceling/resuming a job, interrupting, or fiddling with bills cannot farm XP. Duplicate awards for the same work interval are impossible. See Technical Report §5.

Tuning aim: a pawn stonecutting full-time at level 5-8 with no passion gains roughly one Crafting level every 1.5–2 quadrums. Higher levels remain slow, as in vanilla.

---

## Installation

### Steam Workshop (recommended)
1. Subscribe to **Earned Experience** on the Workshop.
2. Enable it in RimWorld → Mods.
3. Restart and set preferences in **Options → Mod Settings**.

### Manual Install
1. Download the latest release `.zip`.
2. Extract to your RimWorld `Mods/` folder so the path is `Mods/EarnedExperience/About/About.xml`.
3. Enable in **Mods**, restart.

### Building from Source
```bash
# Set path to your RimWorld Managed folder
export RIMWORLD_DIR="/path/to/RimWorld/RimWorld*_Data/Managed"  # e.g. ~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed

# Build (net472, outputs to Assemblies/)
dotnet build Source/EarnedExperience/EarnedExperience.csproj -c Release

# Or use your IDE (Rider / Visual Studio)
```

The `.csproj` expects `Assembly-CSharp.dll`, `UnityEngine.*.dll`, and `0Harmony.dll` in `$(RIMWORLD_DIR)`. They are not bundled. A build without RimWorld installed is useful for review but does not produce a runnable DLL.

---

## Configuration

Open **Options → Mod Settings → Earned Experience**:

- **Enable/disable the entire system** — master toggle.
- **Global XP multiplier** (0x–3x, default 1x) — affects every activity.
- **Per-activity toggles** — disable any activity individually.
- **Per-activity multiplier** (0x–10x) — fine-tune balance.
- **Skill assignment** — choose which vanilla skill receives the XP (e.g., remap Cleaning to Intellectual). Add your own custom skills via other mods and they appear in the dropdown.
- **Floating XP text** — subtle notification near the pawn, throttled to one per 10 seconds.
- **Debug logging** — dev-mode log spam for diagnosis.
- **Restore defaults** — one-click reset.

Settings are stored in `ModsConfig` and survive save/load.

---

## Skill Assignment

Where no vanilla skill fits perfectly (cleaning, hauling), the default is **Crafting** at a conservative 0.5x so general labour still contributes without eclipsing dedicated crafters. Every activity can be reassigned:

- Leave it on Crafting for a simple setup.
- Reassign Cleaning to Intellectual, Hauling to Social, Firefighting to Construction — whatever fits your playstyle.
- Set multiplier to 0 or disable the activity to turn off XP entirely.

Custom skills added by other mods (e.g., *Combat Extended*, *Vanilla Skills Expanded*) appear automatically because the dropdown enumerates `DefDatabase<SkillDef>`.

---

## Notifications

When a pawn earns XP, a small floating text appears near them (e.g., `+2.1 Crafting`). It is throttled so a room full of cleaners doesn't spam the screen. Disable it in settings if you prefer a clean UI.

No notification is shown for every individual work tick — XP is aggregated into batches (~250 ticks) and at job completion.

---

## Known Limitations

- Requires **Harmony** for automatic patching. Without Harmony the mod loads but awards no XP and logs a friendly warning. Most players already have Harmony for other mods.
- Hauling XP is awarded **on completion** (distance-based), not per-tick, because hauling has no work-tick phase. This means a pawn who is drafted mid-haul earns nothing for that haul.
- Notification text uses vanilla `MoteMaker.ThrowText`; it does not appear while the game is paused.
- Save/reload during a work job may lose at most one XP batch (~5 XP) — an intentional trade-off to avoid persisting per-job state.
- Load order: place **Earned Experience** after Harmony if you use a manual load order.

---

## Compatibility

- **RimWorld 1.6 and 1.7**, Windows and Linux. No DLC required.
- **Other mods:** Does not replace any `JobDef`, `ThingDef`, or `WorkGiver`. Patches are narrowly scoped driver ticks. Compatible with most content mods; tested conceptually against *Pick Up And Haul*, *Allow Tool*, *Vanilla Expanded*. If another mod replaces `JobDriver_DoBill` entirely, the `Do_Bill` activities fall back gracefully (no crash, just no bonus XP for those jobs).
- **Save compatible:** Add or remove mid-save safely. Removing does not corrupt saves because no Def or World state is injected.

---

## Project Structure

```
EarnedExperience/
├── About/About.xml
├── Assemblies/EarnedExperience.dll   # built output
├── Defs/GameComponents.xml
├── Languages/English/Keyed/...
├── Source/EarnedExperience/
│   ├── Core/         # Manager, Tracker, Settings, Constants
│   ├── Activities/   # One handler per activity + router
│   ├── Harmony/      # Narrowly scoped patches + bootstrap
│   ├── UI/           # Settings page + notifications
│   └── Compatibility/
├── Textures/
└── docs/TECHNICAL_REPORT.md
```

See `docs/TECHNICAL_REPORT.md` for the full research phase, anti-exploit design, and interception rationale.

---

## Development Workflow

1. **Research** — inspect assemblies, verify which activities truly lack XP (Technical Report §3).
2. **Core** — XP manager + batched tracker + anti-duplication.
3. **Activities** — implement each verified activity.
4. **UI** — vanilla-style settings, skill dropdown, notifications.
5. **Testing** — per-activity playtests, interrupt/resume, passion, save/load, multi-pawn, performance.
6. **Release** — build for 1.6 + 1.7, publish to Workshop.

---

## License & Credits

Created for the *Earned Experience* specification by ArliT1-F. Feel free to use, fork, and include in modpacks with credit.

RimWorld is owned by Ludeon Studios. This mod is not affiliated with Ludeon.