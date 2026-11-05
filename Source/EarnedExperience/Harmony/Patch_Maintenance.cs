using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;
using EarnedExperience.Activities;

namespace EarnedExperience.Harmony
{
    internal static class DelayToilPatchHelper
    {
        public static IEnumerable<Toil> AttachRefuelOrRearmTicks(IEnumerable<Toil> toils, JobDriver driver)
        {
            if (toils == null) yield break;
            foreach (var toil in toils)
            {
                if (toil != null && toil.defaultCompleteMode == ToilCompleteMode.Delay)
                {
                    toil.AddPreTickAction(() =>
                    {
                        try
                        {
                            var pawn = driver?.pawn;
                            var job = driver?.job;
                            if (pawn == null || job == null) return;
                            if (ActivityUtility.IsRearmTrap(job))
                                TrapRearmingHandler.OnWorkTick(pawn, job, 1);
                            else if (ActivityUtility.IsRefuel(job))
                                RefuelingHandler.OnWorkTick(pawn, job, 1);
                        }
                        catch { }
                    });
                }
                yield return toil;
            }
        }
    }

    // Refueling & turret rearming: JobDriver_Refuel.MakeNewToils
    [HarmonyPatch(typeof(JobDriver_Refuel), "MakeNewToils")]
    public static class Patch_Refueling
    {
        static IEnumerable<Toil> Postfix(IEnumerable<Toil> values, JobDriver __instance)
        {
            return DelayToilPatchHelper.AttachRefuelOrRearmTicks(values, __instance);
        }
    }

    // Atomic refueling & turret rearming: JobDriver_RefuelAtomic.MakeNewToils
    [HarmonyPatch(typeof(JobDriver_RefuelAtomic), "MakeNewToils")]
    public static class Patch_RefuelingAtomic
    {
        static IEnumerable<Toil> Postfix(IEnumerable<Toil> values, JobDriver __instance)
        {
            return DelayToilPatchHelper.AttachRefuelOrRearmTicks(values, __instance);
        }
    }

    // Modded trap rearming: JobDriver_RearmTrap (optional – skipped cleanly when absent)
    [HarmonyPatch]
    public static class Patch_TrapRearm
    {
        static bool Prepare() => TargetMethod() != null;

        static MethodBase TargetMethod()
        {
            var t = GenTypes.GetTypeInAnyAssembly("RimWorld.JobDriver_RearmTrap")
                 ?? GenTypes.GetTypeInAnyAssembly("JobDriver_RearmTrap");
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
                            TrapRearmingHandler.OnWorkTick(__instance.pawn, __instance.job, 1);
                        }
                        catch { }
                    });
                }
                yield return toil;
            }
        }
    }

    // Firefighting: Pawn_NativeVerbs.TryBeatFire initiates a ~30-tick fire-beating stance on success
    [HarmonyPatch(typeof(Pawn_NativeVerbs), nameof(Pawn_NativeVerbs.TryBeatFire))]
    public static class Patch_Firefighting
    {
        static void Postfix(Pawn ___pawn, bool __result)
        {
            try
            {
                if (!__result || ___pawn == null) return;
                var job = ___pawn.CurJob;
                if (job == null) return;
                FirefightingHandler.OnWorkTick(___pawn, job, 30);
            }
            catch { }
        }
    }

    // Patient care: Toils_Ingest.ChewIngestible when feeder != patient (JobDriver_FoodFeedPatient)
    [HarmonyPatch(typeof(Toils_Ingest), nameof(Toils_Ingest.ChewIngestible))]
    public static class Patch_FeedPatient
    {
        static void Postfix(Pawn chewer, Toil __result)
        {
            if (__result == null) return;
            __result.AddPreTickAction(() =>
            {
                try
                {
                    var actor = __result.actor;
                    if (actor == null || actor == chewer) return;
                    var job = actor.CurJob;
                    if (job == null || !ActivityUtility.IsFeedPatient(job)) return;
                    PatientCareHandler.OnWorkTick(actor, job, 1);
                }
                catch { }
            });
        }
    }
}
