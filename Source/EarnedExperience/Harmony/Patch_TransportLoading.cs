using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;
using HarmonyLib;
using EarnedExperience.Core;

namespace EarnedExperience.Harmony
{
    /// <summary>
    /// Optional hook for modded JobDriver_LoadTransporter delay toils.
    /// Vanilla HaulToTransporter jobs award mass-based completion XP via Patch_JobTracker_EndCurrentJob.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_LoadTransporter
    {
        static bool Prepare() => TargetMethod() != null;

        static MethodBase TargetMethod()
        {
            var t = GenTypes.GetTypeInAnyAssembly("RimWorld.JobDriver_LoadTransporter")
                 ?? GenTypes.GetTypeInAnyAssembly("JobDriver_LoadTransporter");
            return t?.GetMethod("MakeNewToils", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        static IEnumerable<Toil> Postfix(IEnumerable<Toil> values, JobDriver __instance)
        {
            if (values == null) yield break;
            foreach (var toil in values)
            {
                if (toil != null && toil.defaultCompleteMode == ToilCompleteMode.Delay)
                {
                    toil.AddPreTickAction(() =>
                    {
                        try
                        {
                            if (__instance?.pawn == null || __instance.job == null) return;
                            float xp;
                            ExperienceManager.Tracker.RecordWorkTicks(__instance.pawn, __instance.job, ActivityType.TransportLoading, 1, out xp);
                        }
                        catch { }
                    });
                }
                yield return toil;
            }
        }
    }
}
