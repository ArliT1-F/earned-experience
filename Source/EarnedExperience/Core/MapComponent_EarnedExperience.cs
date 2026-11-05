using RimWorld;
using Verse;

namespace EarnedExperience.Core
{
    /// <summary>
    /// Per-map housekeeping (cleanup of dead pawns' tracker entries).
    /// Lightweight – only runs every 5 seconds game time.
    /// </summary>
    public class MapComponent_EarnedExperience : MapComponent
    {
        private int lastTick;

        public MapComponent_EarnedExperience(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            int now = Find.TickManager.TicksGame;
            if (now - lastTick < 300) return; // every ~5 ticks would be too frequent; we throttle to 300 ticks (~5s)
            lastTick = now;

            // No per-tick work – this is just a hook for future per-map features.
            // Tracker cleanup is in GameComponent; this component is reserved for expansion.
        }
    }
}
