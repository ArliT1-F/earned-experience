using System.Reflection;
using Verse;
using Verse.AI;
using HarmonyLib;
using EarnedExperience.Activities;
using EarnedExperience.Core;

namespace EarnedExperience.Harmony
{
    /// <summary>
    /// Intercepts bill work ticks (DoBill) for stonecutting, smelting, cremation and similar.
    /// Vanilla BillStack / JobDriver_DoBill performs work via Toils_General.DoAtomicWork or DoWork.
    /// We patch the driver that owns the work-tick method.
    ///
    /// Implementation note: In 1.6 the driver class is RimWorld.JobDriver_DoBill. We patch its
    /// TickAction/DoWork helper via the method that advances bill work.
    /// To remain robust across 1.6/1.7 we use reflection with fallback name matching.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_DoBill_NotifyWork
    {
        static MethodBase TargetMethod()
        {
            var driverType = GenTypes.GetTypeInAnyAssembly("RimWorld.JobDriver_DoBill", "Assembly-CSharp");
            if (driverType == null) return null;

            string[] candidates = { "DoWorkTick", "BillDoWorkTick", "DoBillWorkTick", "TryDoJobTick", "DoWork" };
            foreach (var name in candidates)
            {
                var m = driverType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null) return m;
            }
            return null;
        }

        static void Postfix(Pawn ___pawn, Job ___job, float workDone)
        {
            try
            {
                if (___pawn == null || ___job == null) return;
                var type = ActivityUtility.ClassifyDoBill(___job);
                if (type == null) return;

                int ticks = 1;
                // workDone is passed as work units; we count 1 real tick regardless of pawn speed
                // (spec: XP proportional to actual work ticks, not stat-speed-inflated workDone)
                switch (type.Value)
                {
                    case ActivityType.Stonecutting: StonecuttingHandler.OnWorkTick(___pawn, ___job, ticks); break;
                    case ActivityType.Smelting:     SmeltingHandler.OnWorkTick(___pawn, ___job, ticks); break;
                    case ActivityType.Cremation:    CremationHandler.OnWorkTick(___pawn, ___job, ticks); break;
                    default: break;
                }
            }
            catch (System.Exception ex)
            {
                if (ExperienceManager.Settings != null && ExperienceManager.Settings.enableDebugLogging)
                    Log.Warning($"[EarnedExperience] DoBill postfix error: {ex.Message}");
            }
        }
    }
}