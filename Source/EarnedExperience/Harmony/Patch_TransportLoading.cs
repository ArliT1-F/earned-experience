using System.Reflection;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;
using EarnedExperience.Activities;
using EarnedExperience.Core;

namespace EarnedExperience.Harmony
{
    [HarmonyPatch]
    public static class Patch_LoadTransporter
    {
        static MethodBase TargetMethod()
        {
            var t = GenTypes.GetTypeInAnyAssembly("RimWorld.JobDriver_LoadTransporter", "Assembly-CSharp");
            if (t == null) t = GenTypes.GetTypeInAnyAssembly("JobDriver_LoadTransporter", "Assembly-CSharp");
            if (t == null) return null;
            var m = t.GetMethod("LoadWorkTick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null) return m;
            m = t.GetMethod("DoLoadTick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return m;
        }

        static void Postfix(Pawn ___pawn, Job ___job)
        {
            try
            {
                if (___pawn == null || ___job == null) return;
                float xp;
                ExperienceManager.Tracker.RecordWorkTicks(___pawn, ___job, ActivityType.TransportLoading, 1, out xp);
            }
            catch { }
        }
    }
}