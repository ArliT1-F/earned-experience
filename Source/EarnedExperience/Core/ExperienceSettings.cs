using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EarnedExperience.Core
{
    /// <summary>
    /// Persistent mod settings. Uses ModSettings Scribe system (savefile: ModsConfig).
    /// Per-activity toggles + multipliers + skill assignment.
    /// </summary>
    public class ExperienceSettings : ModSettings
    {
        // Global
        public bool masterEnabled = true;
        public float globalMultiplier = 1.0f;
        public bool enableNotifications = true;
        public bool enableDebugLogging = false;

        // Per-activity. Stored as dictionaries keyed by ActivityType name for forward compatibility.
        private Dictionary<string, bool> activityEnabled = new Dictionary<string, bool>();
        private Dictionary<string, float> activityMultipliers = new Dictionary<string, float>();
        private Dictionary<string, string> activitySkillAssignments = new Dictionary<string, string>();

        // Defaults for multipliers – tuned so hauling/cleaning don't outpace dedicated production jobs.
        private static readonly Dictionary<ActivityType, float> DefaultMultipliers = new Dictionary<ActivityType, float>
        {
            { ActivityType.Stonecutting,     1.00f },
            { ActivityType.Smelting,         1.00f },
            { ActivityType.Cleaning,         0.50f },
            { ActivityType.Hauling,          0.50f },
            { ActivityType.TransportLoading, 0.50f },
            { ActivityType.Refueling,        0.50f },
            { ActivityType.TrapRearming,     0.75f },
            { ActivityType.Cremation,        0.50f },
            { ActivityType.Firefighting,     1.00f },
            { ActivityType.PatientCare,      0.75f }
        };

        public ExperienceSettings()
        {
            EnsureDefaults();
        }

        private void EnsureDefaults()
        {
            foreach (ActivityType type in System.Enum.GetValues(typeof(ActivityType)))
            {
                var key = type.ToSaveKey();
                if (!activityEnabled.ContainsKey(key))
                    activityEnabled[key] = true;
                if (!activityMultipliers.ContainsKey(key))
                    activityMultipliers[key] = DefaultMultipliers[type];
                if (!activitySkillAssignments.ContainsKey(key))
                    activitySkillAssignments[key] = SkillAssignment.GetDefaultSkillDefName(type);
            }
        }

        public bool IsActivityEnabled(ActivityType type)
        {
            if (!masterEnabled) return false;
            var key = type.ToSaveKey();
            return activityEnabled.TryGetValue(key, out var v) ? v : true;
        }

        public void SetActivityEnabled(ActivityType type, bool enabled)
        {
            activityEnabled[type.ToSaveKey()] = enabled;
        }

        public float GetMultiplier(ActivityType type)
        {
            var key = type.ToSaveKey();
            if (activityMultipliers.TryGetValue(key, out var m)) return Mathf.Clamp(m, 0f, 10f);
            return 1f;
        }

        public void SetMultiplier(ActivityType type, float mult)
        {
            activityMultipliers[type.ToSaveKey()] = Mathf.Clamp(mult, 0f, 10f);
        }

        public string GetAssignedSkillDefName(ActivityType type)
        {
            var key = type.ToSaveKey();
            if (activitySkillAssignments.TryGetValue(key, out var name)) return name;
            return SkillAssignment.GetDefaultSkillDefName(type);
        }

        public void SetAssignedSkillDefName(ActivityType type, string defName)
        {
            activitySkillAssignments[type.ToSaveKey()] = defName;
        }

        public static float DefaultMultiplier(ActivityType type) => DefaultMultipliers[type];

        public void RestoreDefaults()
        {
            masterEnabled = true;
            globalMultiplier = 1.0f;
            enableNotifications = true;
            enableDebugLogging = false;
            activityEnabled.Clear();
            activityMultipliers.Clear();
            activitySkillAssignments.Clear();
            EnsureDefaults();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            EnsureDefaults();

            Scribe_Values.Look(ref masterEnabled, "masterEnabled", true);
            Scribe_Values.Look(ref globalMultiplier, "globalMultiplier", 1f);
            Scribe_Values.Look(ref enableNotifications, "enableNotifications", true);
            Scribe_Values.Look(ref enableDebugLogging, "enableDebugLogging", false);

            // Per-activity collections are scribes as parallel lookups.
            // We scribe dictionaries manually via custom handling for compatibility.
            Scribe_Collections.Look(ref activityEnabled, "activityEnabled", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref activityMultipliers, "activityMultipliers", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref activitySkillAssignments, "activitySkillAssignments", LookMode.Value, LookMode.Value);

            // Re-ensure after load for newly added activities in future versions.
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (activityEnabled == null) activityEnabled = new Dictionary<string, bool>();
                if (activityMultipliers == null) activityMultipliers = new Dictionary<string, float>();
                if (activitySkillAssignments == null) activitySkillAssignments = new Dictionary<string, string>();
                EnsureDefaults();
            }
        }
    }
}
