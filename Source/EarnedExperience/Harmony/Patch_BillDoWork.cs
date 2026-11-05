using System;
using Verse;
using Verse.AI;
using HarmonyLib;
using EarnedExperience.Activities;
using EarnedExperience.Core;

namespace EarnedExperience.Harmony
{
    /// <summary>
    /// Intercepts bill work ticks (DoBill) for stonecutting, smelting, cremation and similar.
    /// Vanilla JobDriver_DoBill performs recipe work inside Toils_Recipe.DoRecipeWork().
    /// Attaching a pre-tick action to the returned Toil runs once per real work tick only while
    /// the pawn is actively performing the recipe (never while walking to gather ingredients).
    /// </summary>
    [HarmonyPatch(typeof(Toils_Recipe), nameof(Toils_Recipe.DoRecipeWork))]
    public static class Patch_DoBill_NotifyWork
    {
        static void Postfix(Toil __result)
        {
            if (__result == null) return;

            __result.AddPreTickAction(() =>
            {
                try
                {
                    var pawn = __result.actor;
                    var job = pawn?.CurJob;
                    if (pawn == null || job == null) return;

                    var type = ActivityUtility.ClassifyDoBill(job);
                    if (type == null) return;

                    switch (type.Value)
                    {
                        case ActivityType.Stonecutting: StonecuttingHandler.OnWorkTick(pawn, job, 1); break;
                        case ActivityType.Smelting:     SmeltingHandler.OnWorkTick(pawn, job, 1); break;
                        case ActivityType.Cremation:    CremationHandler.OnWorkTick(pawn, job, 1); break;
                        default: break;
                    }
                }
                catch (Exception ex)
                {
                    if (ExperienceManager.Settings != null && ExperienceManager.Settings.enableDebugLogging)
                        Log.Warning($"[EarnedExperience] DoBill tick error: {ex.Message}");
                }
            });
        }
    }
}
