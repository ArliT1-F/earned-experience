using Verse;
using Verse.AI;
using EarnedExperience.Core;

namespace EarnedExperience.Activities
{
    public static class TransportLoadingHandler
    {
        public static void OnTransportLoaded(Pawn pawn, Job job, Thing loadedThing)
        {
            if (pawn == null || job == null) return;
            if (ExperienceManager.Tracker.TryMarkTransportRewarded(pawn, job, loadedThing))
                return;

            float baseXp = ExperienceManager.ComputeTransportXp(loadedThing);
            ExperienceManager.AwardCompletionXp(pawn, ActivityType.TransportLoading, baseXp);
        }

        public static void OnJobEnd(Pawn pawn, Job job)
        {
            ExperienceManager.Tracker.OnJobEnd(pawn, job);
        }
    }
}