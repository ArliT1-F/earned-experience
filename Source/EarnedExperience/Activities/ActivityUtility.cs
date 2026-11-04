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
        private static JobDef cleanFilthDef;
        private static JobDef haulToCellDef;
        private static JobDef haulToContainerDef;
        private static JobDef haulToTransporterDef;
        private static JobDef refuelDef;
        private static JobDef refuelAtomicDef;
        private static JobDef rearmTrapDef;
        private static JobDef doBurnDef;
        private static JobDef extinguishFireDef;
        private static JobDef feedPatientDef;
        private static JobDef takeToBedToFeedDef;
        private static JobDef loadTransporterDef;

        public static void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            // All lookups are SilentFail – missing defs simply mean that activity is not present (e.g., modded version).
            doBillDef            = DefDatabase<JobDef>.GetNamedSilentFail("Do_Bill");
            cleanFilthDef        = DefDatabase<JobDef>.GetNamedSilentFail("CleanFilth");
            haulToCellDef        = DefDatabase<JobDef>.GetNamedSilentFail("HaulToCell");
            haulToContainerDef   = DefDatabase<JobDef>.GetNamedSilentFail("HaulToContainer");
            haulToTransporterDef = DefDatabase<JobDef>.GetNamedSilentFail("HaulToTransporter");
            refuelDef            = DefDatabase<JobDef>.GetNamedSilentFail("Refuel");
            refuelAtomicDef      = DefDatabase<JobDef>.GetNamedSilentFail("RefuelAtomic"); // added in 1.5 for grav generators etc.
            rearmTrapDef         = DefDatabase<JobDef>.GetNamedSilentFail("RearmTrap");
            doBurnDef            = DefDatabase<JobDef>.GetNamedSilentFail("DoBurn"); // unused; cremation uses Do_Bill
            extinguishFireDef    = DefDatabase<JobDef>.GetNamedSilentFail("ExtinguishSelf");
            // Firefighting uses multiple defs:
            // - BeatFire (1.4+)
            // - ExtinguishFire (older)
            if (extinguishFireDef == null)
                extinguishFireDef = DefDatabase<JobDef>.GetNamedSilentFail("BeatFire");
            if (extinguishFireDef == null)
                extinguishFireDef = DefDatabase<JobDef>.GetNamedSilentFail("ExtinguishFire");

            feedPatientDef       = DefDatabase<JobDef>.GetNamedSilentFail("FeedPatient");
            takeToBedToFeedDef   = DefDatabase<JobDef>.GetNamedSilentFail("TakeWoundedPrisonerToBed"); // proxy
            loadTransporterDef   = DefDatabase<JobDef>.GetNamedSilentFail("LoadTransporter");
        }

        public static bool IsDoBill(Job job) => job != null && job.def == doBillDef;
        public static bool IsCleanFilth(Job job) => job != null && job.def == cleanFilthDef;
        public static bool IsHaul(Job job)
        {
            if (job == null) return false;
            return job.def == haulToCellDef || job.def == haulToContainerDef || job.def == haulToTransporterDef;
        }
        public static bool IsRefuel(Job job) => job != null && (job.def == refuelDef || job.def == refuelAtomicDef);
        public static bool IsRearmTrap(Job job) => job != null && job.def == rearmTrapDef;
        public static bool IsFirefight(Job job) => job != null && job.def == extinguishFireDef;
        public static bool IsFeedPatient(Job job) => job != null && job.def == feedPatientDef;
        public static bool IsLoadTransporter(Job job) => job != null && job.def == loadTransporterDef;

        /// <summary>
        /// For Do_Bill jobs we need to distinguish stonecutting / smelting / cremation by workbench/recipe.
        /// This inspects the bill's recipe and the target building.
        /// </summary>
        public static Core.ActivityType? ClassifyDoBill(Job job)
        {
            if (job == null || job.targetA.Thing == null) return null;

            // Bill is stored on the job via job.bill (Verse.AI.Job.bill) in 1.4+. Check.
            var bill = job.bill;
            if (bill == null) return null;

            var recipe = bill.recipe;
            if (recipe == null) return null;

            var bench = job.targetA.Thing;
            var benchDefName = bench.def.defName;

            // Stonecutting: recipe on Stonecutter's Table variant.
            // defNames: TableStonecutter, TableStonecutterElectric (mods)
            if (benchDefName.Contains("Stonecutter"))
            {
                return Core.ActivityType.Stonecutting;
            }

            // Smelting: FueledSmelter / ElectricSmelter with recipes like SmeltWeapon, SmeltApparel, etc.
            if (benchDefName.Contains("Smelter"))
            {
                // Further filter: only smelt recipes, not cremation (which uses Crematorium def)
                if (recipe.defName.Contains("Smelt") || recipe.defName.StartsWith("Smelt"))
                    return Core.ActivityType.Smelting;
            }

            // Cremation: Crematorium building. Recipes: BurnCorpse, BurnApparel, etc.
            if (benchDefName.Contains("Crematorium") || benchDefName.Contains("Cremator"))
            {
                return Core.ActivityType.Cremation;
            }

            // Generic: if recipe is Make_StoneBlocks (fallback) classify as stonecutting anyway.
            if (recipe.defName == "Make_StoneBlocks" || recipe.defName == "MakeStoneBlocks")
                return Core.ActivityType.Stonecutting;

            return null;
        }

        public static Core.ActivityType? ClassifyJob(Job job)
        {
            EnsureInitialized();
            if (job == null) return null;

            if (IsDoBill(job))
            {
                return ClassifyDoBill(job);
            }
            if (IsCleanFilth(job)) return Core.ActivityType.Cleaning;
            if (IsHaul(job)) return Core.ActivityType.Hauling;
            if (IsRefuel(job)) return Core.ActivityType.Refueling;
            if (IsRearmTrap(job)) return Core.ActivityType.TrapRearming;
            if (IsFirefight(job)) return Core.ActivityType.Firefighting;
            if (IsFeedPatient(job)) return Core.ActivityType.PatientCare;
            if (IsLoadTransporter(job)) return Core.ActivityType.TransportLoading;

            return null;
        }
    }
}
