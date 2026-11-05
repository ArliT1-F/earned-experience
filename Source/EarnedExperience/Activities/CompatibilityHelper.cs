using Verse;

namespace EarnedExperience.Compatibility
{
    /// <summary>
    /// Centralises soft compatibility checks.
    /// </summary>
    public static class CompatibilityHelper
    {
        public static bool IsModActive(string packageId)
        {
            if (string.IsNullOrEmpty(packageId)) return false;
            return ModsConfig.IsActive(packageId) || ModLister.GetActiveModWithIdentifier(packageId) != null;
        }

        public static void LogActiveMods()
        {
            if (Core.ExperienceManager.Settings != null && !Core.ExperienceManager.Settings.enableDebugLogging) return;
            foreach (var mod in LoadedModManager.RunningModsListForReading)
            {
                Log.Message($"[EarnedExperience] Active mod: {mod.PackageId} v{mod.ModMetaData.ModVersion}");
            }
        }
    }
}