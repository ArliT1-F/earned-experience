using Verse;
using RimWorld;
using HarmonyLib;
using EarnedExperience.Activities;
using EarnedExperience.Core;

namespace EarnedExperience.Harmony
{
    /// <summary>
    /// Cleaning: JobDriver_CleanFilth calls Filth.ThinFilth() each time a layer of filth is cleaned
    /// (~35 work ticks at 1.0x cleaning speed). Hooking ThinFilth() avoids per-tick overhead and
    /// guarantees XP is only awarded for actual cleaning work rather than walking between filth spots.
    /// </summary>
    [HarmonyPatch(typeof(Filth), nameof(Filth.ThinFilth))]
    public static class Patch_Filth_ThinFilth
    {
        static void Postfix(Filth __instance)
        {
            try
            {
                if (__instance == null) return;
                if (ExperienceManager.Settings != null && !ExperienceManager.Settings.IsActivityEnabled(ActivityType.Cleaning)) return;

                var map = __instance.MapHeld;
                var pawns = map?.mapPawns?.AllPawnsSpawned;
                if (pawns == null) return;

                for (int i = 0; i < pawns.Count; i++)
                {
                    var pawn = pawns[i];
                    var job = pawn?.CurJob;
                    if (job != null && ActivityUtility.IsCleanFilth(job) && job.targetA.Thing == __instance)
                    {
                        CleaningHandler.OnWorkTick(pawn, job, 35);
                        break;
                    }
                }
            }
            catch { }
        }
    }
}
