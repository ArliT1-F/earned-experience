using Verse;

namespace EarnedExperience.Core
{
    /// <summary>
    /// Runs periodic cleanup and could persist cross-save state if needed.
    /// Currently only handles tracker cleanup; does not expose save data because per-job
    /// progress is flushed incrementally (no need to persist). Existence of this component
    /// also proves the assembly loaded correctly in savegames.
    /// </summary>
    public class GameComponent_EarnedExperience : GameComponent
    {
        private int lastCleanupTick;

        public GameComponent_EarnedExperience(Game game) { }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (now - lastCleanupTick > 2000)
            {
                lastCleanupTick = now;
                ExperienceManager.Tracker.CleanupStaleEntries();
            }
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            lastCleanupTick = Find.TickManager.TicksGame;
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            lastCleanupTick = Find.TickManager.TicksGame;
            if (ExperienceManager.Settings != null && ExperienceManager.Settings.enableDebugLogging)
                Log.Message("[EarnedExperience] Game loaded – tracker ready.");
        }
    }
}