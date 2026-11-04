using Verse;
using UnityEngine;

namespace EarnedExperience.Core
{
    /// <summary>
    /// Mod entry point. Initialises Harmony patches if available and exposes settings.
    /// </summary>
    public class EarnedExperienceMod : Mod
    {
        public static ExperienceSettings SettingsInstance { get; private set; }
        public static EarnedExperienceMod Instance { get; private set; }

        public EarnedExperienceMod(ModContentPack content) : base(content)
        {
            Instance = this;
            SettingsInstance = GetSettings<ExperienceSettings>();

            LongEventHandler.ExecuteWhenFinished(() =>
            {
                try
                {
                    Harmony.HarmonyPatches.Apply();
                }
                catch (System.Exception ex)
                {
                    Log.Warning($"[EarnedExperience] Harmony patches not applied (Harmony missing or incompatible): {ex.Message}. Mod will be inactive until Harmony is installed.");
                }
            });
        }

        public override string SettingsCategory() => "Earned Experience";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            UI.SettingsUI.Draw(inRect, SettingsInstance);
            base.DoSettingsWindowContents(inRect);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }
    }
}
