using Verse;
using Verse.AI;
using RimWorld;

namespace EarnedExperience.Activities
{
    /// <summary>
    /// Shared helpers to identify job types without fragile string matching on every tick.
    /// We resolve JobDefs once and cache them.
    /// </summary>
    public static class ActivityUtility
    {
        private static bool initialized;
        private static JobDef doBillDef;
        private static JobDef doBillLegacyDef;
        private static JobDef cleanDef;
        private static JobDef cleanFilthLegacyDef;
        private static JobDef haulToCellDef;
        private static JobDef haulToContainerDef;
        private static JobDef haulToTransporterDef;
        private static JobDef loadTransporterDef;
        private static JobDef refuelDef;
        private static JobDef refuelAtomicDef;
        private static JobDef rearmTrapDef;
        private static JobDef rearmTurretDef;
        private static JobDef rearmTurretAtomicDef;
        private static JobDef beatFireDef;
        private static JobDef extinguishFiresNearbyDef;
        private static JobDef extinguishSelfDef;
        private static JobDef extinguishFireLegacyDef;
        private static JobDef feedPatientDef;

        public static void EnsureInitialized()
        {
            if (initialized) return;
            if (DefDatabase<JobDef>.DefCount == 0) return;
            initialized = true;

            // All lookups are SilentFail – missing defs simply mean that activity is not present.
            doBillDef                = DefDatabase<JobDef>.GetNamedSilentFail("DoBill");
            doBillLegacyDef          = DefDatabase<JobDef>.GetNamedSilentFail("Do_Bill");
            cleanDef                 = DefDatabase<JobDef>.GetNamedSilentFail("Clean");
            cleanFilthLegacyDef      = DefDatabase<JobDef>.GetNamedSilentFail("CleanFilth");
            haulToCellDef            = DefDatabase<JobDef>.GetNamedSilentFail("HaulToCell");
            haulToContainerDef       = DefDatabase<JobDef>.GetNamedSilentFail("HaulToContainer");
            haulToTransporterDef     = DefDatabase<JobDef>.GetNamedSilentFail("HaulToTransporter");
            loadTransporterDef       = DefDatabase<JobDef>.GetNamedSilentFail("LoadTransporter");
            refuelDef                = DefDatabase<JobDef>.GetNamedSilentFail("Refuel");
            refuelAtomicDef          = DefDatabase<JobDef>.GetNamedSilentFail("RefuelAtomic");
            rearmTrapDef             = DefDatabase<JobDef>.GetNamedSilentFail("RearmTrap");
            rearmTurretDef           = DefDatabase<JobDef>.GetNamedSilentFail("RearmTurret");
            rearmTurretAtomicDef     = DefDatabase<JobDef>.GetNamedSilentFail("RearmTurretAtomic");
            beatFireDef              = DefDatabase<JobDef>.GetNamedSilentFail("BeatFire");
            extinguishFiresNearbyDef = DefDatabase<JobDef>.GetNamedSilentFail("ExtinguishFiresNearby");
            extinguishSelfDef        = DefDatabase<JobDef>.GetNamedSilentFail("ExtinguishSelf");
            extinguishFireLegacyDef  = DefDatabase<JobDef>.GetNamedSilentFail("ExtinguishFire");
            feedPatientDef           = DefDatabase<JobDef>.GetNamedSilentFail("FeedPatient");
        }

        public static bool IsDoBill(Job job)
        {
            EnsureInitialized();
            return job != null && job.def != null && (job.def == doBillDef || job.def == doBillLegacyDef);
        }

        public static bool IsCleanFilth(Job job)
        {
            EnsureInitialized();
            return job != null && job.def != null && (job.def == cleanDef || job.def == cleanFilthLegacyDef);
        }

        public static bool IsHaul(Job job)
        {
            EnsureInitialized();
            if (job == null || job.def == null) return false;
            return job.def == haulToCellDef || job.def == haulToContainerDef;
        }

        public static bool IsRefuel(Job job)
        {
            EnsureInitialized();
            if (job == null || job.def == null) return false;
            return job.def == refuelDef || job.def == refuelAtomicDef;
        }

        public static bool IsRearmTrap(Job job)
        {
            EnsureInitialized();
            if (job == null || job.def == null) return false;
            return job.def == rearmTrapDef || job.def == rearmTurretDef || job.def == rearmTurretAtomicDef;
        }

        public static bool IsFirefight(Job job)
        {
            EnsureInitialized();
            if (job == null || job.def == null) return false;
            return job.def == beatFireDef
                || job.def == extinguishFiresNearbyDef
                || job.def == extinguishSelfDef
                || job.def == extinguishFireLegacyDef;
        }

        public static bool IsFeedPatient(Job job)
        {
            EnsureInitialized();
            return job != null && job.def != null && job.def == feedPatientDef;
        }

        public static bool IsLoadTransporter(Job job)
        {
            EnsureInitialized();
            if (job == null || job.def == null) return false;
            return job.def == haulToTransporterDef || job.def == loadTransporterDef;
        }

        /// <summary>
        /// For DoBill jobs we need to distinguish stonecutting / smelting / cremation by workbench/recipe.
        /// This inspects the bill's recipe and the target building.
        /// </summary>
        public static Core.ActivityType? ClassifyDoBill(Job job)
        {
            if (job == null || job.targetA.Thing == null) return null;

            var bill = job.bill;
            if (bill == null) return null;

            var recipe = bill.recipe;
            if (recipe == null) return null;

            // Never duplicate XP if a recipe (vanilla or modded) already awards skill XP natively.
            if (recipe.workSkill != null && recipe.workSkillLearnFactor > 0f)
                return null;

            var bench = job.targetA.Thing;
            var benchDefName = bench.def.defName;

            // Stonecutting: recipe on Stonecutter's Table variant.
            if (benchDefName.Contains("Stonecutter"))
            {
                return Core.ActivityType.Stonecutting;
            }

            // Smelting: FueledSmelter / ElectricSmelter with recipes like SmeltWeapon, SmeltApparel, etc.
            if (benchDefName.Contains("Smelter"))
            {
                if (recipe.defName.Contains("Smelt"))
                    return Core.ActivityType.Smelting;
            }

            // Cremation: Crematorium building. Recipes: BurnCorpse, BurnApparel, etc.
            if (benchDefName.Contains("Crematorium") || benchDefName.Contains("Cremator"))
            {
                return Core.ActivityType.Cremation;
            }

            // Generic fallback if recipe is Make_StoneBlocks on a modded bench.
            if (recipe.defName.StartsWith("Make_StoneBlocks") || recipe.defName == "MakeStoneBlocks")
                return Core.ActivityType.Stonecutting;

            return null;
        }

        public static Core.ActivityType? ClassifyJob(Job job)
        {
            EnsureInitialized();
            if (job == null) return null;

            if (IsDoBill(job)) return ClassifyDoBill(job);
            if (IsCleanFilth(job)) return Core.ActivityType.Cleaning;
            if (IsLoadTransporter(job)) return Core.ActivityType.TransportLoading;
            if (IsHaul(job)) return Core.ActivityType.Hauling;
            if (IsRefuel(job)) return Core.ActivityType.Refueling;
            if (IsRearmTrap(job)) return Core.ActivityType.TrapRearming;
            if (IsFirefight(job)) return Core.ActivityType.Firefighting;
            if (IsFeedPatient(job)) return Core.ActivityType.PatientCare;

            return null;
        }
    }
}
