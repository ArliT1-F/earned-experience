using Verse;
using Verse.AI;
using HarmonyLib;
using EarnedExperience.Activities;

namespace EarnedExperience.Harmony
{
    /// <summary>
    /// Hauling: capture start position at job start and award on completion via lifecycle hook.
    /// Additional placement hook on Toils_Haul.PlaceHauledThingInCell handles multi-item hauls cleanly.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class Patch_Hauling_Start
    {
        static void Postfix(Pawn ___pawn, Job newJob)
        {
            try
            {
                if (___pawn == null || newJob == null) return;
                if (!ActivityUtility.IsHaul(newJob)) return;
                HaulingState.RecordStart(___pawn, newJob, ___pawn.Position);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(Toils_Haul), nameof(Toils_Haul.PlaceHauledThingInCell))]
    public static class Patch_Hauling_PlaceInCell
    {
        static void Postfix(Toil __result)
        {
            if (__result == null) return;
            __result.AddFinishAction(() =>
            {
                try
                {
                    var pawn = __result.actor;
                    var job = pawn?.CurJob;
                    if (pawn == null || job == null || !ActivityUtility.IsHaul(job)) return;
                    var start = HaulingState.PeekStart(pawn, job);
                    if (start == IntVec3.Invalid) return;
                    // Don't clear here – let EndCurrentJob clear to avoid race; duplicate guard in tracker prevents double.
                    HaulingHandler.OnHaulCompleted(pawn, job, job.targetA.Thing, start);
                }
                catch { }
            });
        }
    }

    /// <summary>
    /// In-memory map for hauling start positions. Not persisted.
    /// </summary>
    internal static class HaulingState
    {
        private struct Key : System.IEquatable<Key>
        {
            public int pawnId;
            public int jobId;
            public bool Equals(Key other) => pawnId == other.pawnId && jobId == other.jobId;
            public override int GetHashCode() => pawnId * 397 ^ jobId;
            public override bool Equals(object obj) => obj is Key k && Equals(k);
        }

        private static readonly System.Collections.Generic.Dictionary<Key, IntVec3> map =
            new System.Collections.Generic.Dictionary<Key, IntVec3>();

        public static void RecordStart(Pawn pawn, Job job, IntVec3 pos)
        {
            var k = new Key { pawnId = pawn.thingIDNumber, jobId = job.loadID };
            map[k] = pos;
        }

        public static IntVec3 PeekStart(Pawn pawn, Job job)
        {
            var k = new Key { pawnId = pawn.thingIDNumber, jobId = job.loadID };
            return map.TryGetValue(k, out var pos) ? pos : IntVec3.Invalid;
        }

        public static IntVec3 GetAndClearStart(Pawn pawn, Job job)
        {
            var k = new Key { pawnId = pawn.thingIDNumber, jobId = job.loadID };
            if (map.TryGetValue(k, out var pos))
            {
                map.Remove(k);
                return pos;
            }
            return IntVec3.Invalid;
        }
    }
}
