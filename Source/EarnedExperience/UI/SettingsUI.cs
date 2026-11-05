using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using EarnedExperience.Core;

namespace EarnedExperience.UI
{
    /// <summary>
    /// Vanilla-style settings page. Renders global controls + per-activity table.
    /// Uses Widgets.* as per RimWorld conventions (Listing_Standard, sliders, checkboxes, dropdowns).
    /// </summary>
    public static class SettingsUI
    {
        private static Vector2 scrollPos;
        private static readonly float RowHeight = 32f;
        private static readonly float HeaderHeight = 28f;

        public static void Draw(Rect inRect, ExperienceSettings settings)
        {
            if (settings == null) return;

            var activities = System.Enum.GetValues(typeof(ActivityType));
            float contentHeight = 310f + activities.Length * (RowHeight + 2f) + 60f;
            var viewRect = new Rect(0f, 0f, inRect.width - 16f, contentHeight);

            Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            // Global
            Text.Font = GameFont.Medium;
            listing.Label("EarnedExperience_Settings_Global".Translate());
            Text.Font = GameFont.Small;

            listing.CheckboxLabeled("EarnedExperience_MasterEnabled_Label".Translate(), ref settings.masterEnabled,
                "EarnedExperience_MasterEnabled_Desc".Translate());

            listing.Gap(6f);
            listing.Label($"{"EarnedExperience_GlobalMult_Label".Translate()} {settings.globalMultiplier:F2}x");
            settings.globalMultiplier = Widgets.HorizontalSlider(listing.GetRect(22f), settings.globalMultiplier, 0f, 3f, true,
                null, "0x", "3x", 0.05f);
            listing.Gap(2f);
            listing.Label("EarnedExperience_GlobalMult_Desc".Translate());
            listing.Gap(6f);

            listing.CheckboxLabeled("EarnedExperience_Notifications_Label".Translate(), ref settings.enableNotifications,
                "EarnedExperience_Notifications_Desc".Translate());
            listing.CheckboxLabeled("EarnedExperience_DebugLog_Label".Translate(), ref settings.enableDebugLogging,
                "EarnedExperience_DebugLog_Desc".Translate());

            listing.Gap(12f);

            // Restore defaults button
            if (listing.ButtonText("EarnedExperience_RestoreDefaults_Label".Translate()))
            {
                settings.RestoreDefaults();
            }

            listing.Gap(12f);
            listing.GapLine(12f);
            listing.Gap(6f);

            // Per-activity table header
            Text.Font = GameFont.Medium;
            listing.Label("EarnedExperience_Activities_Header".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);

            // Table header row
            var headerRect = listing.GetRect(HeaderHeight);
            DrawHeader(headerRect);
            listing.Gap(2f);

            // Rows
            foreach (ActivityType type in activities)
            {
                var rowRect = listing.GetRect(RowHeight);
                DrawActivityRow(rowRect, type, settings);
                listing.Gap(2f);
            }

            listing.Gap(6f);
            listing.Label("EarnedExperience_Footer_Hint".Translate());
            listing.End();
            Widgets.EndScrollView();
        }

        private static void DrawHeader(Rect r)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(r.LeftPart(0.30f), "EarnedExperience_Col_Activity".Translate());
            Widgets.Label(new Rect(r.x + r.width * 0.30f, r.y, r.width * 0.12f, r.height), "EarnedExperience_Col_Enabled".Translate());
            Widgets.Label(new Rect(r.x + r.width * 0.42f, r.y, r.width * 0.18f, r.height), "EarnedExperience_Col_Multiplier".Translate());
            Widgets.Label(new Rect(r.x + r.width * 0.60f, r.y, r.width * 0.40f, r.height), "EarnedExperience_Col_Skill".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawActivityRow(Rect r, ActivityType type, ExperienceSettings settings)
        {
            // Alternate row bg
            if (Mathf.FloorToInt(((int)type) / 1f) % 2 == 0)
                Widgets.DrawAltRect(r);

            // Activity label
            string labelKey = type.ToLabelKey();
            string label = labelKey.Translate();
            // fallback to def-style name if not translated
            if (label == labelKey) label = type.ToString();

            string descKey = type.ToDescKey();
            string desc = descKey.Translate();

            var labelRect = r.LeftPart(0.30f);
            Widgets.Label(labelRect, label);
            TooltipHandler.TipRegion(labelRect, desc);

            // Enabled toggle
            bool enabled = settings.IsActivityEnabled(type) && settings.masterEnabled;
            bool before = enabled;
            var enabledRect = new Rect(r.x + r.width * 0.30f, r.y + 4f, 24f, 24f);
            Widgets.Checkbox(enabledRect.x, enabledRect.y, ref enabled);
            if (enabled != before)
                settings.SetActivityEnabled(type, enabled);

            // Multiplier slider compact
            var multRect = new Rect(r.x + r.width * 0.42f, r.y + 4f, r.width * 0.18f - 8f, 24f);
            float oldMult = settings.GetMultiplier(type);
            float mult = oldMult;
            // Small +/- buttons + slider
            if (Widgets.ButtonText(new Rect(multRect.x, multRect.y, 18f, 24f), "-"))
                mult = Mathf.Clamp(mult - 0.1f, 0f, 10f);
            if (Widgets.ButtonText(new Rect(multRect.x + multRect.width - 18f, multRect.y, 18f, 24f), "+"))
                mult = Mathf.Clamp(mult + 0.1f, 0f, 10f);
            var sliderRect = new Rect(multRect.x + 20f, multRect.y + 4f, multRect.width - 40f, 16f);
            float newMult = Widgets.HorizontalSlider(sliderRect, Mathf.Clamp(mult, 0f, 3f), 0f, 3f, true, null, null, null, 0.05f);
            if (!Mathf.Approximately(mult, oldMult))
                newMult = mult;
            if (!Mathf.Approximately(newMult, oldMult))
                settings.SetMultiplier(type, newMult);

            // Tooltip for multiplier numeric
            TooltipHandler.TipRegion(multRect, $"{newMult:F2}x");

            // Skill selector dropdown
            var skillRect = new Rect(r.x + r.width * 0.60f, r.y + 3f, r.width * 0.40f - 4f, 26f);
            string curSkillName = settings.GetAssignedSkillDefName(type);
            SkillDef curDef = DefDatabase<SkillDef>.GetNamedSilentFail(curSkillName);
            string btnLabel = curDef != null ? curDef.LabelCap : curSkillName ?? "Crafting";
            if (Widgets.ButtonText(skillRect, btnLabel))
            {
                OpenSkillMenu(type, settings);
            }
        }

        private static void OpenSkillMenu(ActivityType type, ExperienceSettings settings)
        {
            var options = new List<FloatMenuOption>();
            foreach (var sd in DefDatabase<SkillDef>.AllDefsListForReading)
            {
                string label = sd.LabelCap;
                options.Add(new FloatMenuOption(label, () =>
                {
                    settings.SetAssignedSkillDefName(type, sd.defName);
                    settings.Write();
                }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}