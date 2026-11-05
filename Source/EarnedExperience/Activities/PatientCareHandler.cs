using Verse;
using Verse.AI;
using EarnedExperience.Core;

namespace EarnedExperience.Activities
{
    public static class PatientCareHandler
    {
        public static void OnWorkTick(Pawn pawn, Job job, int ticks)
        {
            if (pawn == null || job == null) return;
            // Feeding does not already award Medical XP in vanilla (only tending/surgery do).
            float xp;
            ExperienceManager.Tracker.RecordWorkTicks(pawn, job, ActivityType.PatientCare, ticks, out xp);
        }

        public static void OnJobEnd(Pawn pawn, Job job)
        {
            ExperienceManager.Tracker.OnJobEnd(pawn, job);
        }
    }
}