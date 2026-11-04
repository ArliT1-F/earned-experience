using Verse;
using Verse.AI;
using EarnedExperience.Core;

namespace EarnedExperience.Activities
{
    public static class SmeltingHandler
    {
        public static void OnWorkTick(Pawn pawn, Job job, int ticks)
        {
            if (pawn == null || job == null) return;
            float xp;
            ExperienceManager.Tracker.RecordWorkTicks(pawn, job, ActivityType.Smelting, ticks, out xp);
        }

        public static void OnJobEnd(Pawn pawn, Job job)
        {
            if (pawn == null || job == null) return;
            ExperienceManager.Tracker.OnJobEnd(pawn, job);
        }
    }
}
