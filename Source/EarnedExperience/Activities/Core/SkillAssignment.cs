using System.Collections.Generic;
using Verse;

namespace EarnedExperience.Core
{
    /// <summary>
    /// Resolves which SkillDef should receive XP for a given activity.
    /// Defaults are intentionally conservative and use only vanilla skills.
    /// Players can remap any activity to any SkillDef via settings (or disable it).
    /// </summary>
    public static class SkillAssignment
    {
        // Default skill defNames – all vanilla 1.6 skills.
        // These were chosen because no vanilla skill perfectly matches cleaning/hauling etc.,
        // so we pick the closest thematic fit and let the player override.
        private static readonly Dictionary<ActivityType, string> DefaultSkillDefNames =
            new Dictionary<ActivityType, string>
            {
                { ActivityType.Stonecutting,     "Crafting" },
                { ActivityType.Smelting,         "Crafting" },
                { ActivityType.Cleaning,         "Social" },      // configurable; Intellectual also sensible
                { ActivityType.Hauling,          "Crafting" },      // "Logistics" would be custom; fallback to Crafting
                { ActivityType.TransportLoading, "Crafting" },
                { ActivityType.Refueling,        "Construction" },
                { ActivityType.TrapRearming,     "Crafting" },
                { ActivityType.Cremation,        "Crafting" },
                { ActivityType.Firefighting,     "Construction" },
                { ActivityType.PatientCare,      "Medicine"  }
            };

        public static string GetDefaultSkillDefName(ActivityType type)
        {
            return DefaultSkillDefNames.TryGetValue(type, out var name) ? name : "Crafting";
        }

        /// <summary>
        /// Resolve the configured SkillDef for an activity, falling back to the default if unconfigured or def not found.
        /// Returns null if the activity is disabled (caller should skip XP).
        /// </summary>
        public static SkillDef ResolveSkill(ActivityType type, ExperienceSettings settings)
        {
            if (settings == null) return DefDatabase<SkillDef>.GetNamedSilentFail(GetDefaultSkillDefName(type));
            if (!settings.IsActivityEnabled(type)) return null;

            var configuredName = settings.GetAssignedSkillDefName(type);
            SkillDef def = null;
            if (!string.IsNullOrEmpty(configuredName))
            {
                def = DefDatabase<SkillDef>.GetNamedSilentFail(configuredName);
            }
            if (def == null)
            {
                // Fallback to default; also handles first load before settings have entries.
                var fallback = GetDefaultSkillDefName(type);
                def = DefDatabase<SkillDef>.GetNamedSilentFail(fallback);
            }
            return def;
        }

        /// <summary>
        /// All available SkillDefs for the settings dropdown.
        /// </summary>
        public static List<SkillDef> AllSkills
        {
            get
            {
                var list = new List<SkillDef>();
                foreach (var sd in DefDatabase<SkillDef>.AllDefsListForReading)
                    list.Add(sd);
                return list;
            }
        }
    }
}