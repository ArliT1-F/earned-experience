using Verse;
using Verse.AI;
using EarnedExperience.Core;

namespace EarnedExperience.Activities
{
    /// <summary>
    /// Central router for maintenance-type activities. Kept as a single file to document the mapping
    /// and to make future additions trivial. Actual per-activity handlers above are thin wrappers.
    /// </summary>
    public static class MaintenanceHandler
    {
        public static void RouteWorkTick(Pawn pawn, Job job, ActivityType type, int ticks)
        {
            switch (type)
            {
                case ActivityType.Refueling:    RefuelingHandler.OnWorkTick(pawn, job, ticks); break;
                case ActivityType.TrapRearming: TrapRearmingHandler.OnWorkTick(pawn, job, ticks); break;
                default: break;
            }
        }
    }
}