using Verse;
using Verse.AI;
using EarnedExperience.Core;

namespace EarnedExperience.Activities
{
    /// <summary>
    /// Handler for stonecutting work. Actual per-tick interception happens in HarmonyPatches
    /// via JobDriver_DoBill notification. This class contains the business logic so it is testable
    /// and keeps the patch thin.
    /// </summary>
    public static class StonecuttingHandler
    {
        public static void OnWorkTick(Pawn pawn, Job job, int ticks)
        {
            if (pawn == null || job == null) return;
            // Use tracker to batch and flush.
            float xp;
            ExperienceManager.Tracker.RecordWorkTicks(pawn, job, ActivityType.Stonecutting, ticks, out xp);
        }

        public static void OnJobEnd(Pawn pawn, Job job)
        {
            if (pawn == null || job == null) return;
            var xp = ExperienceManager.Tracker.OnJobEnd(pawn, job);
            // Notification already handled in FlushPendingXp.
        }
    }
}
