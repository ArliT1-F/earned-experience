using Verse;
using Verse.AI;
using HarmonyLib;
using EarnedExperience.Activities;
using EarnedExperience.Core;

namespace EarnedExperience.Harmony
{
    /// <summary>
    /// Single authoritative hook for job lifecycle. Consolidates all per-job flush and completion rewards
    /// to avoid triple-patching Pawn_JobTracker.EndCurrentJob and to ensure deterministic ordering.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_JobTracker), "EndCurrentJob")]
    public static class Patch_JobTracker_EndCurrentJob
    {
        static void Prefix(Pawn ___pawn, JobCondition condition)
        {
            try
            {
                if (___pawn == null) return;
                var job = ___pawn.CurJob;
                if (job == null) return;

                var type = ActivityUtility.ClassifyJob(job);
                if (type == null) return;

                switch (type.Value)
                {
                    // Tick-work activities: flush remainder on any termination (interrupt, cancel, success)
                    case ActivityType.Stonecutting: StonecuttingHandler.OnJobEnd(___pawn, job); break;
                    case ActivityType.Smelting:     SmeltingHandler.OnJobEnd(___pawn, job); break;
                    case ActivityType.Cremation:    CremationHandler.OnJobEnd(___pawn, job); break;
                    case ActivityType.Cleaning:     CleaningHandler.OnJobEnd(___pawn, job); break;
                    case ActivityType.Refueling:    RefuelingHandler.OnJobEnd(___pawn, job); break;
                    case ActivityType.TrapRearming: TrapRearmingHandler.OnJobEnd(___pawn, job); break;
                    case ActivityType.Firefighting: FirefightingHandler.OnJobEnd(___pawn, job); break;
                    case ActivityType.PatientCare:  PatientCareHandler.OnJobEnd(___pawn, job); break;
                    case ActivityType.TransportLoading:
                        // Transport: award mass-based completion only on success, then cleanup.
                        if (condition == JobCondition.Succeeded)
                            TransportLoadingHandler.OnTransportLoaded(___pawn, job, job.targetA.Thing);
                        TransportLoadingHandler.OnJobEnd(___pawn, job);
                        break;
                    case ActivityType.Hauling:
                        // Hauling: award distance/mass only on success, then cleanup.
                        if (condition == JobCondition.Succeeded)
                        {
                            var start = HaulingState.GetAndClearStart(___pawn, job);
                            if (start != IntVec3.Invalid)
                                HaulingHandler.OnHaulCompleted(___pawn, job, job.targetA.Thing, start);
                        }
                        else
                        {
                            // On interrupt, clear start to avoid stale entry without awarding
                            HaulingState.GetAndClearStart(___pawn, job);
                        }
                        HaulingHandler.OnJobEnd(___pawn, job);
                        break;
                }
            }
            catch (System.Exception ex)
            {
                if (ExperienceManager.Settings != null && ExperienceManager.Settings.enableDebugLogging)
                    Log.Warning($"[EarnedExperience] EndCurrentJob prefix error: {ex.Message}");
            }
        }
    }
}