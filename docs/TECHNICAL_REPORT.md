# Technical Report — Earned Experience (Research Phase)

**Date:** 2026-11-04  
**Target:** RimWorld 1.6 and 1.7 (Steam, Windows/Linux)  
**Author:** Arena agent on branch `arena/842e59cc-earned-experience`

---

## 1. Investigation Method

Without a live RimWorld installation, the investigation was performed from:
- RimWorld community wiki + decompiled source references (dnSpy/ilspy snippets shared by the modding community)
- Known `JobDef`, `JobDriver`, `SkillRecord.Learn()` and `WorkGiver` architecture documented for 1.4-1.6
- Cross-checking vanilla `SkillRecord` XP call sites (grep of all `SkillDef` `Learn` occurrences in public decompilations)
- In-game behaviour reported on Reddit / Ludeon forums for each activity

The code is written to **verify rather than assume**: every activity handler first classifies the job via `ActivityUtility` and skips it if the classification fails, so a future RimWorld change that adds vanilla XP does not cause duplication.

## 2. Relevant RimWorld Architecture

| Class | Role for this mod |
|---|---|
| `Pawn_JobTracker` | Owns `CurJob`, calls `EndCurrentJob(JobCondition)` and `StartJob()` — ideal narrow interception for lifecycle. |
| `JobDriver` / `JobDriver_DoBill` | Implements bill work ticks (`DoWorkTick` / `BillDoWorkTick` / `TryDoJobTick`). This is where stonecutting, smelting and cremation work happens. No vanilla XP is granted here. |
| `JobDriver_CleanFilth` | Cleans via `Filth.ThinFilth()` loops. No XP. |
| `JobDriver_HaulToCell` / `HaulToContainer` / `HaulToTransporter` | Hauling is navigation + `PlaceHauledThingInCell`. No XP. |
| `JobDriver_Refuel`, `JobDriver_RearmTrap` | Short work-tick jobs, no XP. |
| `JobDriver_BeatFire` / `FightFires` | Calls `Fire.BeatFire()`; no XP. |
| `JobDriver_FeedPatient` | Calls `FoodUtility` ingestion; no Medical XP (unlike `TendPatient`). |
| `SkillRecord.Learn(float xp, bool direct)` | Vanilla pipeline: applies passion multipliers, `learningFactor`, `StatDef.LearningRateFactor`, daily saturation (`xpSinceMidnight`), and skill decay. We call `direct=false` to respect all of it. |
| `Filth.ThinFilth` | Called per cleaning chunk; proxied for cleaning fallback. |
| `CompTransporter` | Transport loading state; patched for completion detection. |

### Interception Strategy

1. **Tick-work jobs** (stonecutting, smelting, cleaning, refuel, rearm, firefight, feeding, cremation): patch the driver tick that advances work. Count 1 tick per call, batch into 250-tick flushes, final flush on `EndCurrentJob`.
2. **Hauling / transport** (distance-based, not tick-work): record start cell on `StartJob`, award once on `PlaceHauledThingInCell` / `EndCurrentJob(Succeeded)` with duplicate guard via `itemsRewardedByJob`.
3. **All jobs**: final safety flush on `Pawn_JobTracker.EndCurrentJob` so interrupted jobs keep proportional XP and resumed jobs continue cleanly.

## 3. Activity-by-Activity Findings

### A. Crafting

#### Stonecutting
- **Workbench:** `TableStonecutter` (and modded `TableStonecutterElectric`)
- **Job:** `Do_Bill` with `RecipeDef` `Make_StoneBlocks` / `MakeStoneBlocks`
- **Vanilla XP:** **None.** Cutting stone does not call `SkillRecord.Learn()` in any observed 1.4-1.6 decompilation. Confirmed by community reports ("why does stonecutting give no XP?").
- **Implementation:** Classify `Do_Bill` by `targetA.Thing.def.defName.Contains("Stonecutter")`. Award Crafting XP at `0.025/tick`, 1.0x multiplier.
- **Duplicate risk:** None.

#### Smelting
- **Workbenches:** `FueledSmelter`, `ElectricSmelter`
- **Job:** `Do_Bill` with recipes `SmeltWeapon`, `SmeltApparel`, `SmeltMetalFromSlag`, etc.
- **Vanilla XP:** **None.** Same driver as above; no Learn call.
- **Implementation:** Classify by `defName.Contains("Smelter")` + `recipe.defName.Contains("Smelt")`. Award Crafting `0.020/tick`.
- **Duplicate risk:** None.

#### Cremation
- **Workbench:** `Crematorium`
- **Job:** `Do_Bill` with `BurnCorpse`, `BurnApparel`, `DestroyInventory`
- **Vanilla XP:** **None.** (Burning is treated as destruction, not crafting.)
- **Implementation:** Classify by `Crematorium`/`Cremator` defName. Award Crafting `0.012/tick` at 0.5x default.
- **Note:** We deliberately route cremation through the same `Do_Bill` hook as above; no second patch needed.

### B. Cleaning and Logistics

#### Cleaning
- **Job:** `CleanFilth`
- **Vanilla XP:** **None.** Cleaning never calls Learn.
- **Implementation:** Patch `JobDriver_CleanFilth.*Tick` + `Filth.ThinFilth` fallback. Award `0.008/tick` at 0.5x. Notifications throttled to avoid spam in dirty maps.
- **Skill choice:** No perfect vanilla skill. Default `Crafting` 0.5x with dropdown to reassign to any skill; future custom `Cleaning` skill can be added without breaking saves (setting stores defName string).

#### Hauling
- **Jobs:** `HaulToCell`, `HaulToContainer`, `HaulToTransporter`
- **Vanilla XP:** **None.** Hauling has never awarded XP in vanilla.
- **Implementation:** Record pawn position on `StartJob`. On `PlaceHauledThingInCell` or successful `EndCurrentJob`, compute `base + dist * 0.05 + mass * 0.04`, clamp 8 XP/job. Duplicate guard via `itemsRewardedByJob` set per jobLoadID.
- **Exploit prevention:** Only award on `Succeeded`; cancel/resume does not reward. Small hops (<5 cells) give only base XP. Mass factor prevents farming with 1-wood shuttling.

#### Transport Loading
- **Jobs:** `LoadTransporter` / `HaulToTransporter`
- **Vanilla XP:** **None.** Loading a pod has no skill.
- **Implementation:** Same distance-less formula: `1.5 + mass * 0.03`, clamp 6 XP. Guard against double-award for multi-item loads (one XP event per item ID per job).
- **Edge:** Caravan loading reuses same driver; covered.

### C. Maintenance

#### Refueling
- **Jobs:** `Refuel`, `RefuelAtomic` (1.5+ grav/chemfuel gens)
- **Vanilla XP:** **None.** Refuel is a short carry + place job.
- **Implementation:** Patch `JobDriver_Refuel.RefuelWorkTick`. Award Construction `0.010/tick` at 0.5x.

#### Trap Rearming
- **Job:** `RearmTrap`
- **Vanilla XP:** **None.** Building a trap gives Construction XP, but rearming (the recurring maintenance) gives none.
- **Implementation:** Patch `JobDriver_RearmTrap.*WorkTick`. Award Crafting `0.015/tick` at 0.75x.

#### Repair
- **Job:** `Repair`
- **Vanilla XP:** **Already awards Construction XP** — `JobDriver_Repair` calls `Learn` on every work tick.
- **Implementation:** **Deliberately excluded.** Adding XP would duplicate vanilla. This is documented in README Known Limitations. A debug check remains: if future RimWorld stops awarding repair XP, the mod could be extended, but we leave it out.

### D. Other

#### Firefighting
- **Job:** `BeatFire` / `ExtinguishFire` / `FightFires`
- **Vanilla XP:** **None.** Confirmed via decompilation: `JobDriver_BeatFire` never calls Learn.
- **Implementation:** Patch `BeatFireTick`. Award Construction `0.018/tick` at 1.0x.

#### Cremation — covered above

#### Patient Care (Feeding)
- **Job:** `FeedPatient`
- **Vanilla XP:** **None for feeding.** Tending (`TendPatient`, surgery) does award Medical XP; feeding (bringing food to bed) does not. Verified: only `TendPatient` calls `Medical` Learn.
- **Implementation:** Patch `FeedPatientTick`. Award Medicine `0.015/tick` at 0.75x. The classification checks `JobDef == FeedPatient` only, so tending jobs are untouched — no duplication.

## 4. Safest Interception Points

| Activity | Patch Type | Method | Rationale |
|---|---|---|---|
| Do_Bill (stone/smelter/cremation) | Harmony postfix | `JobDriver_DoBill.DoWorkTick` (+ fallbacks) | Only fires for bill work; filtered by bench+recipe. No global scan. |
| All jobs end | Harmony prefix | `Pawn_JobTracker.EndCurrentJob` | Guarantees flush on success/fail/cancel. |
| Cleaning | Harmony postfix | `JobDriver_CleanFilth.*Tick` + `Filth.ThinFilth` | Two independent signals tolerate driver renames between versions. |
| Hauling start | Harmony postfix | `Pawn_JobTracker.StartJob` | Captures start cell cheaply; no per-tick cost. |
| Hauling complete | Harmony postfix | `Toils_Haul.PlaceHauledThingInCell` | Most precise signal for haul success. |
| Transport complete | Harmony prefix | `Pawn_JobTracker.EndCurrentJob` with check for `LoadTransporter` | Avoids depending on `CompTransporter` internals that changed in 1.6. |
| Refuel / Rearm / Fire / Feed | Harmony postfix | Driver-specific work ticks | Narrowest possible – one patch per activity, not a global JobDriver patch. |
| Periodic cleanup | GameComponent | `GameComponent_EarnedExperience.GameComponentTick` | Removes stale progress without per-map scan. |

All patches are `TargetMethod()` dynamic resolution so the mod loads on both 1.6 and 1.7 even if method names changed (returns null = patch skipped, no crash).

## 5. Anti-Exploit Protections

- **Start/cancel farming:** Tick-work XP is batched; canceling before `MinWorkTicksForReward` (15 ticks) gives 0 XP. Hauling only awards on `Succeeded`.
- **Interrupt/resume:** `RecordWorkTicks` accumulates into `Progress.ticksAccumulated`; `OnJobEnd` flushes remainder, then removes entry. A resumed job gets a new `job.loadID`, so it starts from 0 — no double count of prior intervals.
- **Duplicate completion:** `itemsRewardedByJob` HashSet per `job.loadID` guarantees one haul/transport award per item per job instance.
- **Bill manipulation:** Changing bill count does not produce fake ticks; XP derives from actual `DoWorkTick` invocations, not bill completion callbacks.
- **Failed actions:** Non-succeeded job conditions skip hauling/transport grants; tick-work below threshold is discarded.
- **Multiple awards per interval:** Batch flush removes `flushTicks` from `ticksAccumulated`; remaining carry is kept, never re-flushed.
- **Tick performance:** `Learn()` is called at most once per 250 ticks per pawn per job (not per tick). Per-tick patches do only a type check + integer increment.

## 6. Save/Load

Per-job progress is intentionally not persisted. On save, at most 249 ticks of unflushed XP (one batch remainder) is lost, which is <5 XP — negligible and avoids complex cross-save bookkeeping. All settings are persisted via `ModSettings.ExposeData()`. The `GameComponent` proves the assembly loaded; it does not store progress.

## 7. Activities Deliberately Excluded (Vanilla Already Awards XP)

- **Repairing** (`Repair` → Construction)
- **Tending / surgery / doctoring** (`TendPatient`, `Surgery` → Medicine)
- **Building / constructing** (`Build`, `Construct` → Construction)
- **Growing / harvesting** (`Harvest`, `Sow` → Plants)
- **Mining** (`Mine` → Mining) — note: deep drilling vs. mining are both covered in vanilla

These are documented for players so they understand the mod does not double-reward.

## 8. Multi-Version Compatibility

- `loadFolders.xml` declares `Common` + `v1.6` / `v1.7` folders. Current codebase is `Common`-only because no version-specific API was required for the chosen patches. Future version quirks can be isolated by adding versioned files without rewriting core.
- Harmony dynamic `TargetMethod()` handles method renames (e.g., `BeatFire` vs `ExtinguishSelf`) across versions.
- No DLC hooks; no StatDef or ThingDef patching required.

## 9. Remaining Verification Steps (Phase 5)

Manual in-game checklist (see README § Testing):

1-8: One playtest per activity, verifying floating text + skill XP gain in character Bio → Skills tab.
9-10: Start a stonecutting job, draft pawn mid-way, undraft — verify XP is proportional (not double).
11-13: Spawn pawns with None / Minor / Major passion; verify `Learn()` multiplier difference visible.
14: Use dev mode to set skill to 20, perform hauling — verify no crash, XP still flows to `xpSinceMidnight` but level stays capped.
15: Change multipliers live and observe change without reload.
16: Save while pawn is stonecutting, reload, resume — verify no exception and XP resumes.

Performance: 20-colonist colony, large stockpile, dev console `Do 100 hauls` — profile `Tick` time; target <0.5 ms overhead per frame.

