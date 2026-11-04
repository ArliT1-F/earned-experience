using Verse;
using Verse.AI;
using EarnedExperience.Core;

namespace EarnedExperience.Activities
{
    public static class HaulingHandler
    {
        // Hauling jobs are movement-heavy rather than work-tick heavy, so XP is awarded on completion.
        // We capture start position at job start in the patch and compute XP on job end.

        public static void OnHaulCompleted(Pawn pawn, Job job, Thing hauledThing, IntVec3 startCell)
        {
            if (pawn == null || job == null) return;
            if (ExperienceManager.Tracker.TryMarkHaulRewarded(pawn, job, hauledThing))
                return; // already awarded for this job/item – anti-dupe

            IntVec3 endCell = job.targetB.IsValid ? job.targetB.Cell : pawn.Position;
            float baseXp = ExperienceManager.ComputeHaulingXp(startCell, endCell, hauledThing);
            ExperienceManager.AwardCompletionXp(pawn, ActivityType.Hauling, baseXp);

            // Cleanup is handled by OnJobEnd; keep item tracking until job fully ends.
        }

        public static void OnJobEnd(Pawn pawn, Job job)
        {
            ExperienceManager.Tracker.OnJobEnd(pawn, job);
        }
    }
}
