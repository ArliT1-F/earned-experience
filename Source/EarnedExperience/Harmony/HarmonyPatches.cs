using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;
using HarmonyLib;

namespace EarnedExperience.Harmony
{
    /// <summary>
    /// Patch bootstrap. All patches are narrowly scoped – we do NOT patch every JobDriver indiscriminately.
    /// We patch specific JobDrivers and Toil builders that were verified to award zero XP in vanilla.
    ///
    /// WHY HARMONY IS REQUIRED:
    /// RimWorld does not expose a mod-friendly event for "pawn performed N ticks of work on job X".
    /// Bills, cleaning, hauling etc. implement work inside JobDriver toils with no virtual hook.
    /// XML Def patching can replace JobDrivers but would break compatibility with other mods overriding the same
    /// JobDefs. Harmony prefix/postfix on well-known driver/toil methods is the least-invasive, most compatible approach.
    /// The mod gracefully degrades if Harmony is absent (no XP, but no crash).
    /// </summary>
    public static class HarmonyPatches
    {
        private static bool applied;
        private static object harmonyInstanceBoxed;

        public static void Apply()
        {
            if (applied) return;

            if (GenTypes.GetTypeInAnyAssembly("HarmonyLib.Harmony") == null)
            {
                Log.Message("[EarnedExperience] Harmony not found – Earned Experience will remain inactive. Install Harmony to enable XP.");
                return;
            }

            try
            {
                ApplyInternal();
            }
            catch (Exception ex)
            {
                Log.Error($"[EarnedExperience] Failed to apply Harmony patches: {ex}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ApplyInternal()
        {
            var harmony = new HarmonyLib.Harmony("arlit.earnedexperience");
            harmonyInstanceBoxed = harmony;

            int succeeded = 0;
            int failed = 0;
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0)
                    continue;

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                    succeeded++;
                }
                catch (Exception ex)
                {
                    failed++;
                    Log.Warning($"[EarnedExperience] Skipping patch {type.Name}: {ex.Message}");
                }
            }

            applied = true;
            Log.Message($"[EarnedExperience] Applied {succeeded} Harmony patch class(es)" + (failed > 0 ? $" ({failed} skipped)." : "."));
        }

        public static void Unapply()
        {
            if (!applied || harmonyInstanceBoxed == null) return;
            try
            {
                UnapplyInternal();
            }
            catch { }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void UnapplyInternal()
        {
            if (harmonyInstanceBoxed is HarmonyLib.Harmony harmony)
            {
                harmony.UnpatchAll("arlit.earnedexperience");
            }
            harmonyInstanceBoxed = null;
            applied = false;
        }
    }
}
