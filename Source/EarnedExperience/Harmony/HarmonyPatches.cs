using System;
using System.Reflection;
using Verse;
using HarmonyLib;

namespace EarnedExperience.Harmony
{
    /// <summary>
    /// Patch bootstrap. All patches are narrowly scoped – we do NOT patch every JobDriver indiscriminately.
    /// We patch specific JobDrivers and WorkGivers that were verified to award zero XP in vanilla.
    ///
    /// WHY HARMONY IS REQUIRED:
    /// RimWorld does not expose a mod-friendly event for "pawn performed N ticks of work on job X".
    /// Bills, cleaning, hauling etc. implement work inside private JobDriver toils with no virtual hook.
    /// XML Def patching can replace JobDrivers but would break compatibility with other mods overriding the same
    /// JobDefs. Harmony prefix/postfix on well-known driver methods is the least-invasive, most compatible approach.
    /// The mod gracefully degrades if Harmony is absent (no XP, but no crash).
    /// </summary>
    public static class HarmonyPatches
    {
        private static bool applied;
        private static HarmonyLib.Harmony harmonyInstance;

        public static void Apply()
        {
            if (applied) return;

            // Check Harmony is actually present to allow dependency-free loading.
            bool harmonyAvailable = false;
            try
            {
                var _ = typeof(HarmonyLib.Harmony);
                harmonyAvailable = true;
            }
            catch { }

            if (!harmonyAvailable)
            {
                Log.Message("[EarnedExperience] Harmony not found – Earned Experience will remain inactive. Install Harmony to enable XP.");
                return;
            }

            harmonyInstance = new HarmonyLib.Harmony("arlit.earnedexperience");
            try
            {
                harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                applied = true;
                Log.Message("[EarnedExperience] Patches applied successfully.");
            }
            catch (Exception ex)
            {
                Log.Error($"[EarnedExperience] Failed to apply Harmony patches: {ex}");
            }
        }

        public static void Unapply()
        {
            if (!applied || harmonyInstance == null) return;
            try
            {
                harmonyInstance.UnpatchAll("arlit.earnedexperience");
                applied = false;
            }
            catch { }
        }
    }
}
