using System.Reflection;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;
using EarnedExperience.Activities;
using EarnedExperience.Core;

namespace EarnedExperience.Harmony
{
    /// <summary>
    /// Cleaning: JobDriver_CleanFilth performs ticks of cleaning via a toil that ticks Filth.ThinFilth/clean progress.
    /// We patch the driver tick.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_Cleaning
    {
        static MethodBase TargetMethod()
        {
            var t = GenTypes.GetTypeInAnyAssembly("RimWorld.JobDriver_CleanFilth", "Assembly-CSharp");
            if (t == null) t = GenTypes.GetTypeInAnyAssembly("JobDriver_CleanFilth", "Assembly-CSharp");
            if (t == null) return null;
            // Look for the method that does work each tick
            var m = t.GetMethod("CleanFilthTick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null) return m;
            m = t.GetMethod("DoWorkTick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null) return m;
            // Fallback: general tick
            m = t.GetMethod("DriverTick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return m;
        }

        static void Postfix(Pawn ___pawn, Job ___job)
        {
            try
            {
                if (___pawn == null || ___job == null) return;
                if (!ActivityUtility.IsCleanFilth(___job)) return;
                if (ExperienceManager.Settings != null && !ExperienceManager.Settings.IsActivityEnabled(ActivityType.Cleaning)) return;
                CleaningHandler.OnWorkTick(___pawn, ___job, 1);
            }
            catch { }
        }
    }

    /// <summary>
    /// Alternative direct hook for 1.6 where cleaning tick is exposed as Filth.CleaningWorkDone or Toils.
    /// We also patch Pawn's filth cleaning action if available as a safety net.
    /// </summary>
    [HarmonyPatch(typeof(Filth), "ThinFilth")]
    public static class Patch_Filth_ThinFilth
    {
        static void Postfix(Filth __instance, Pawn cleaner)
        {
            try
            {
                if (cleaner == null || __instance == null) return;
                var job = cleaner.CurJob;
                if (job == null || !ActivityUtility.IsCleanFilth(job)) return;
                // ThinFilth is called per cleaning completion chunk, not per tick.
                // Award proportionally: 1 ThinFilth call corresponds to ~15 ticks of cleaning work.
                CleaningHandler.OnWorkTick(cleaner, job, 15);
            }
            catch { }
        }
    }
}
