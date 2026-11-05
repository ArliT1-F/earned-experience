using Verse;

namespace EarnedExperience.Core
{
    public class MapComponent_EarnedExperience : MapComponent
    {
        private int lastTick;

        public MapComponent_EarnedExperience(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            int now = Find.TickManager.TicksGame;
            if (now - lastTick < 300) return;
            lastTick = now;
        }
    }
}