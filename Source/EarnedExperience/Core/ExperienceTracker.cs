using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace EarnedExperience.Core
{
    /// <summary>
    /// Tracks in-flight work progress per pawn+job to prevent duplicate XP awards and farming via cancel/requeue.
    /// NOT persisted to savefile – progress is flushed to XP incrementally, so reload loses at most one batch.
    /// </summary>
    public sealed class ExperienceTracker
    {
        // Key: pawn thingID + job loadID. We use pawn.GetUniqueLoadID() + job loadID composite.
        private struct TrackerKey : System.IEquatable<TrackerKey>
        {
            public int pawnLoadId;
            public int jobLoadId;
            public bool Equals(TrackerKey other) => pawnLoadId == other.pawnLoadId && jobLoadId == other.jobLoadId;
            public override int GetHashCode() => pawnLoadId * 397 ^ jobLoadId;
            public override bool Equals(object obj) => obj is TrackerKey k && Equals(k);
        }

        private class Progress
        {
            public ActivityType activity;
            public int ticksAccumulated;   // ticks counted toward next batch
            public int ticksTotalAwarded;  // total ticks ever flushed for this job instance (anti-dupe)
            public int lastFlushTick;      // Find.TickManager.TicksGame at last flush
            public bool completed;         // set on job end to avoid double completion reward
        }

        private readonly Dictionary<TrackerKey, Progress> progressByJob = new Dictionary<TrackerKey, Progress>();

        // For hauling / transport we track item load IDs already rewarded in the current job to avoid multi-award.
        private readonly Dictionary<TrackerKey, HashSet<int>> itemsRewardedByJob = new Dictionary<TrackerKey, HashSet<int>>();

        // Notification throttle: pawn loadID -> last notification tick
        private readonly Dictionary<int, int> lastNotificationTickByPawn = new Dictionary<int, int>();

        private TrackerKey MakeKey(Pawn pawn, Job job)
        {
            return new TrackerKey { pawnLoadId = pawn.thingIDNumber, jobLoadId = job?.loadID ?? 0 };
        }

        private TrackerKey MakeKeyByIds(int pawnThingIdNumber, int jobLoadId)
        {
            return new TrackerKey { pawnLoadId = pawnThingIdNumber, jobLoadId = jobLoadId };
        }

        // ---------------------------------------------------------------
        // Tick-based (workbench, cleaning, refuel, firefight, etc.)
        // ---------------------------------------------------------------

        /// <summary>
        /// Record that 'ticks' worth of productive work was done for activity. Buffers and flushes in batches.
        /// Returns true if XP was flushed (caller may want to show notification).
        /// </summary>
        public bool RecordWorkTicks(Pawn pawn, Job job, ActivityType activity, int ticks, out float xpFlushed)
        {
            xpFlushed = 0f;
            if (pawn == null || job == null || ticks <= 0) return false;
            if (pawn.Dead || pawn.skills == null) return false;

            var key = MakeKey(pawn, job);
            if (!progressByJob.TryGetValue(key, out var prog))
            {
                prog = new Progress { activity = activity, lastFlushTick = Find.TickManager.TicksGame };
                progressByJob[key] = prog;
            }

            // If activity changed mid-job (should not happen), reset.
            if (prog.activity != activity)
            {
                // flush leftover under old activity first
                if (prog.ticksAccumulated >= Constants.MinWorkTicksForReward)
                {
                    xpFlushed = ExperienceManager.FlushPendingXp(pawn, prog.activity, prog.ticksAccumulated);
                    prog.ticksTotalAwarded += prog.ticksAccumulated;
                }
                prog.activity = activity;
                prog.ticksAccumulated = 0;
            }

            prog.ticksAccumulated += ticks;

            // Flush in batches to avoid per-tick Learn() overhead and log spam.
            if (prog.ticksAccumulated >= Constants.TickBatchSize)
            {
                var toFlush = prog.ticksAccumulated;
                // Keep remainder (< batch) as carry so we don't lose partial progress.
                var batches = toFlush / Constants.TickBatchSize;
                var flushTicks = batches * Constants.TickBatchSize;
                xpFlushed = ExperienceManager.FlushPendingXp(pawn, activity, flushTicks);
                prog.ticksTotalAwarded += flushTicks;
                prog.ticksAccumulated -= flushTicks;
                prog.lastFlushTick = Find.TickManager.TicksGame;
                return xpFlushed > 0.001f;
            }

            return false;
        }

        /// <summary>
        /// Called when a job ends (completed, interrupted, or failed). Flushes any remaining ticks above threshold.
        /// </summary>
        public float OnJobEnd(Pawn pawn, Job job)
        {
            if (pawn == null || job == null) return 0f;
            var key = MakeKey(pawn, job);
            if (!progressByJob.TryGetValue(key, out var prog)) return 0f;
            if (prog.completed) return 0f;

            prog.completed = true;
            float xp = 0f;
            if (prog.ticksAccumulated >= Constants.MinWorkTicksForReward)
            {
                xp = ExperienceManager.FlushPendingXp(pawn, prog.activity, prog.ticksAccumulated);
                prog.ticksTotalAwarded += prog.ticksAccumulated;
            }

            // Cleanup to avoid leaks – remove entry after job end.
            progressByJob.Remove(key);
            itemsRewardedByJob.Remove(key);
            return xp;
        }

        /// <summary>
        /// Periodic cleanup for pawns that died/desawned or jobs that vanished without callback.
        /// Called from MapComponent or GameComponent tick.
        /// </summary>
        public void CleanupStaleEntries()
        {
            // Remove entries where pawn no longer exists or job is no longer their current job for a long time.
            // We do a conservative scan every ~2000 ticks; stale = lastFlush > 10k ticks ago.
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            var toRemove = new List<TrackerKey>();
            foreach (var kv in progressByJob)
            {
                if (now - kv.Value.lastFlushTick > 10000)
                    toRemove.Add(kv.Key);
            }
            foreach (var k in toRemove)
            {
                progressByJob.Remove(k);
                itemsRewardedByJob.Remove(k);
            }
        }

        // ---------------------------------------------------------------
        // Hauling / transport completion (one-shot per job)
        // ---------------------------------------------------------------

        /// <summary>
        /// Returns true if this haul job has already been rewarded (anti-dupe for same job instance).
        /// If not, marks it rewarded and returns false (caller should proceed to award).
        /// </summary>
        public bool TryMarkHaulRewarded(Pawn pawn, Job job, Thing hauledThing = null)
        {
            if (pawn == null || job == null) return true;
            var key = MakeKey(pawn, job);

            // Simple case: one haul per job – if we already flushed completion XP for this job, skip.
            if (!itemsRewardedByJob.TryGetValue(key, out var set))
            {
                set = new HashSet<int>();
                itemsRewardedByJob[key] = set;
            }

            int itemId = hauledThing != null ? hauledThing.thingIDNumber : job.loadID;
            if (set.Contains(itemId)) return true; // already awarded
            set.Add(itemId);

            // Also handle progress map: create entry so OnJobEnd can clean up.
            if (!progressByJob.ContainsKey(key))
            {
                progressByJob[key] = new Progress { activity = ActivityType.Hauling, lastFlushTick = Find.TickManager.TicksGame };
            }

            return false;
        }

        public bool TryMarkTransportRewarded(Pawn pawn, Job job, Thing loadedThing = null)
        {
            if (pawn == null || job == null) return true;
            var key = MakeKey(pawn, job);
            if (!itemsRewardedByJob.TryGetValue(key, out var set))
            {
                set = new HashSet<int>();
                itemsRewardedByJob[key] = set;
            }
            int itemId = loadedThing != null ? loadedThing.thingIDNumber : job.loadID;
            if (set.Contains(itemId)) return true;
            set.Add(itemId);
            if (!progressByJob.ContainsKey(key))
                progressByJob[key] = new Progress { activity = ActivityType.TransportLoading, lastFlushTick = Find.TickManager.TicksGame };
            return false;
        }

        // ---------------------------------------------------------------
        // Notification throttle
        // ---------------------------------------------------------------

        public bool CanShowNotification(Pawn pawn)
        {
            if (pawn == null) return false;
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int pid = pawn.thingIDNumber;
            if (lastNotificationTickByPawn.TryGetValue(pid, out var last) && now - last < Constants.NotificationMinIntervalTicks)
                return false;
            lastNotificationTickByPawn[pid] = now;
            return true;
        }

        /// <summary>
        /// Called when a pawn is despawned/destroyed – remove all its tracking.
        /// </summary>
        public void Notify_PawnDespawned(Pawn pawn)
        {
            if (pawn == null) return;
            int pid = pawn.thingIDNumber;
            var toRemove = new List<TrackerKey>();
            foreach (var k in progressByJob.Keys)
                if (k.pawnLoadId == pid) toRemove.Add(k);
            foreach (var k in toRemove)
            {
                progressByJob.Remove(k);
                itemsRewardedByJob.Remove(k);
            }
            lastNotificationTickByPawn.Remove(pid);
        }
    }
}
