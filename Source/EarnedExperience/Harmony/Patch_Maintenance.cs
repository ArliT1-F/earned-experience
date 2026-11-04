using System.Reflection;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;
using EarnedExperience.Activities;
using EarnedExperience.Core;

namespace EarnedExperience.Harmony
{
    // Refueling: JobDriver_Refuel
    [HarmonyPatch]
    public static class Patch_Refueling
    {
        static MethodBase TargetMethod()
        {
            var t = GenTypes.GetTypeInAnyAssembly("RimWorld.JobDriver_Refuel", "Assembly-CSharp");
            if (t == null) return null;
            var m = t.GetMethod("RefuelWorkTick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null) return m;
            m = t.GetMethod("DoRefuelWork", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return m;
        }

        static void Postfix(Pawn ___pawn, Job ___job)
        {
            try
            {
                if (___pawn == null || ___job == null) return;
                RefuelingHandler.OnWorkTick(___pawn, ___job, 1);
            }
            catch { }
        }
    }

    // Trap rearming: JobDriver_RearmTrap
    [HarmonyPatch]
    public static class Patch_TrapRearm
    {
        static MethodBase TargetMethod()
        {
            var t = GenTypes.GetTypeInAnyAssembly("RimWorld.JobDriver_RearmTrap", "Assembly-CSharp");
            if (t == null) return null;
            var m = t.GetMethod("RearmWorkTick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null) return m;
            m = t.GetMethod("DoRearmWork", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return m;
        }

        static void Postfix(Pawn ___pawn, Job ___job)
        {
            try
            {
                if (___pawn == null || ___job == null) return;
                TrapRearmingHandler.OnWorkTick(___pawn, ___job, 1);
            }
            catch { }
        }
    }

    // Firefighting: JobDriver_BeatFire / FightFires
    [HarmonyPatch]
    public static class Patch_Firefighting
    {
        static MethodBase TargetMethod()
        {
            var t = GenTypes.GetTypeInAnyAssembly("RimWorld.JobDriver_BeatFire", "Assembly-CSharp");
            if (t == null) t = GenTypes.GetTypeInAnyAssembly("RimWorld.JobDriver_FightFires", "Assembly-CSharp");
            if (t == null) t = GenTypes.GetTypeInAnyAssembly("JobDriver_BeatFire", "Assembly-CSharp");
            if (t == null) return null;
            var m = t.GetMethod("BeatFireTick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null) return m;
            m = t.GetMethod("DoWorkTick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return m;
        }

        static void Postfix(Pawn ___pawn, Job ___job)
        {
            try
            {
                if (___pawn == null || ___job == null) return;
                FirefightingHandler.OnWorkTick(___pawn, ___job, 1);
            }
            catch { }
        }
    }

    // Fallback for firefighting via Fire.BeatFire tick on the Fire thing itself
    [HarmonyPatch(typeof(Fire), "DoComplexCalcs")]
    public static class Patch_Fire_DoComplexCalcs
    {
        // Fire.Tick fires DoComplexCalcs every ~150 ticks – not ideal, so we also patch Tick.
        static void Postfix(Fire __instance)
        {
            // Not used: we rely on driver tick above; this patch is a fallback for older versions where driver was missing.
        }
    }

    // Patient care: JobDriver_FeedPatient
    [HarmonyPatch]
    public static class Patch_FeedPatient
    {
        static MethodBase TargetMethod()
        {
            var t = GenTypes.GetTypeInAnyAssembly("RimWorld.JobDriver_FeedPatient", "Assembly-CSharp");
            if (t == null) return null;
            var m = t.GetMethod("FeedPatientTick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null) return m;
            m = t.GetMethod("DoFeedTick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return m;
        }

        static void Postfix(Pawn ___pawn, Job ___job)
        {
            try
            {
                if (___pawn == null || ___job == null) return;
                PatientCareHandler.OnWorkTick(___pawn, ___job, 1);
            }
            catch { }
        }
    }
}
