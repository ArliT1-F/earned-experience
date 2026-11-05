using System;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace EarnedExperience.Core
{
    /// <summary>
    /// Central XP routing. All activity handlers call through here.
    /// Owns the singleton tracker and applies multipliers, skill resolution, and vanilla Learn() semantics.
    /// </summary>
    public static class ExperienceManager
    {
        private static ExperienceTracker _tracker;
        public static ExperienceTracker Tracker
        {
            get
            {
                if (_tracker == null) _tracker = new ExperienceTracker();
                return _tracker;
            }
        }

        public static ExperienceSettings Settings => EarnedExperienceMod.SettingsInstance;

        /// <summary>
        /// Main entry for tick-work activities: convert workTicks -> XP and delegate to SkillRecord.Learn().
        /// Respects: masterEnabled, activity enabled, global/activity multipliers, passion, learning rate,
        /// and daily learning saturation via vanilla Learn() pipeline.
        /// </summary>
        public static float FlushPendingXp(Pawn pawn, ActivityType activity, int workTicks)
        {
            if (pawn == null || workTicks <= 0) return 0f;
            if (Settings != null && !Settings.masterEnabled) return 0f;
            if (Settings != null && !Settings.IsActivityEnabled(activity)) return 0f;
            if (pawn.skills == null || pawn.Dead) return 0f;

            var skillDef = SkillAssignment.ResolveSkill(activity, Settings);
            if (skillDef == null) return 0f;

            var skillRecord = pawn.skills.GetSkill(skillDef);
            if (skillRecord == null) return 0f;

            // Already maxed? Still call Learn() – vanilla handles xpSinceMidnight etc., but we can early-out for perf.
            if (skillRecord.Level >= 20 && skillRecord.xpSinceMidnight >= SkillRecord.XpRequiredToLevelUpFrom(20))
            {
                // Still allow XP to affect xpSinceMidnight tracking? Vanilla caps; we just skip.
                // Let vanilla handle it; but to avoid spam we still skip.
            }

            float baseXpPerTick = GetBaseXpPerTick(activity);
            float rawXp = workTicks * baseXpPerTick;

            float globalMult = Settings != null ? Settings.globalMultiplier : 1f;
            float activityMult = Settings != null ? Settings.GetMultiplier(activity) : 1f;
            float finalXp = rawXp * globalMult * activityMult;

            if (finalXp <= 0.001f) return 0f;

            // Use vanilla Learn() – it applies passion multipliers, learning rate, and daily limit internally.
            // direct = false so that implants/genes/etc. apply. We also ensure xp > 0.
            try
            {
                float xpBefore = skillRecord.xpSinceLastLevel;
                // SkillRecord.Learn signature in 1.4/1.5: Learn(float xp, bool direct = false)
                // In 1.6 it remains the same. We pass direct:false to respect vanilla modifiers.
                skillRecord.Learn(finalXp, false);

                if (Settings != null && Settings.enableDebugLogging)
                    Log.Message($"[EarnedExperience] {pawn.LabelShort} +{finalXp:F2} {skillDef.defName} ({activity}, {workTicks} ticks, g:{globalMult:F2} a:{activityMult:F2})");

                // Check if notification should be shown – throttle is in tracker, but we also respect global toggle.
                if (Settings != null && Settings.enableNotifications && Tracker.CanShowNotification(pawn))
                {
                    UI.ExperienceNotifications.NotifyXpGain(pawn, skillDef, finalXp);
                }

                return finalXp;
            }
            catch (Exception ex)
            {
                Log.Error($"[EarnedExperience] Failed to award XP ({activity}) to {pawn?.LabelShort}: {ex}");
                return 0f;
            }
        }

        /// <summary>
        /// One-shot completion XP (hauling, transport). Derived from distance/mass elsewhere; here we apply multipliers and Learn().
        /// </summary>
        public static float AwardCompletionXp(Pawn pawn, ActivityType activity, float baseAmount)
        {
            if (pawn == null || baseAmount <= 0f) return 0f;
            if (Settings != null && !Settings.masterEnabled) return 0f;
            if (Settings != null && !Settings.IsActivityEnabled(activity)) return 0f;
            if (pawn.skills == null || pawn.Dead) return 0f;

            var skillDef = SkillAssignment.ResolveSkill(activity, Settings);
            if (skillDef == null) return 0f;

            var skillRecord = pawn.skills.GetSkill(skillDef);
            if (skillRecord == null) return 0f;

            float globalMult = Settings != null ? Settings.globalMultiplier : 1f;
            float activityMult = Settings != null ? Settings.GetMultiplier(activity) : 1f;
            float finalXp = baseAmount * globalMult * activityMult;

            if (finalXp <= 0.001f) return 0f;

            try
            {
                skillRecord.Learn(finalXp, false);
                if (Settings != null && Settings.enableDebugLogging)
                    Log.Message($"[EarnedExperience] {pawn.LabelShort} +{finalXp:F2} {skillDef.defName} ({activity} completion, base:{baseAmount:F2})");

                if (Settings != null && Settings.enableNotifications && Tracker.CanShowNotification(pawn))
                    UI.ExperienceNotifications.NotifyXpGain(pawn, skillDef, finalXp);

                return finalXp;
            }
            catch (Exception ex)
            {
                Log.Error($"[EarnedExperience] Failed to award completion XP ({activity}) to {pawn?.LabelShort}: {ex}");
                return 0f;
            }
        }

        private static float GetBaseXpPerTick(ActivityType activity)
        {
            switch (activity)
            {
                case ActivityType.Stonecutting:     return Constants.BaseXpPerWorkTick_Stonecutting;
                case ActivityType.Smelting:         return Constants.BaseXpPerWorkTick_Smelting;
                case ActivityType.Cleaning:         return Constants.BaseXpPerWorkTick_Cleaning;
                case ActivityType.Cremation:        return Constants.BaseXpPerWorkTick_Cremation;
                case ActivityType.Refueling:        return Constants.BaseXpPerWorkTick_Refueling;
                case ActivityType.TrapRearming:     return Constants.BaseXpPerWorkTick_TrapRearm;
                case ActivityType.Firefighting:     return Constants.BaseXpPerWorkTick_Firefighting;
                case ActivityType.PatientCare:      return Constants.BaseXpPerWorkTick_PatientCare;
                default:                            return 0.015f;
            }
        }

        /// <summary>
        /// Helper for handlers to compute hauling XP from distance/mass.
        /// </summary>
        public static float ComputeHaulingXp(IntVec3 start, IntVec3 dest, Thing hauledThing)
        {
            float distance = (float)System.Math.Sqrt((dest.x - start.x) * (dest.x - start.x) + (dest.z - start.z) * (dest.z - start.z));
            float mass = 0f;
            try
            {
                if (hauledThing != null)
                {
                    // Thing mass: def.BaseMass * stackCount (for stackables) – approximate via GetStatValue.
                    mass = hauledThing.stackCount * (hauledThing.def.BaseMass);
                    // Non-stackables: single item mass
                    if (hauledThing.def.stackLimit == 1) mass = hauledThing.def.BaseMass;
                }
            }
            catch { }

            float xp = Constants.Hauling_BaseXpPerJob;
            if (distance >= Constants.Hauling_MinDistanceForBonus)
                xp += distance * Constants.Hauling_XpPerCellDistance;
            xp += mass * Constants.Hauling_XpPerKgMass;

            return UnityEngine.Mathf.Clamp(xp, 0f, Constants.Hauling_MaxXpPerJob);
        }

        public static float ComputeTransportXp(Thing loadedThing)
        {
            float mass = 0f;
            try
            {
                if (loadedThing != null)
                    mass = loadedThing.stackCount * loadedThing.def.BaseMass;
            }
            catch { }

            float xp = Constants.TransportLoading_BaseXp + mass * Constants.TransportLoading_XpPerKg;
            return UnityEngine.Mathf.Clamp(xp, 0f, Constants.TransportLoading_MaxXpPerJob);
        }
    }
}
