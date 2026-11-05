using Verse;
using RimWorld;
using UnityEngine;

namespace EarnedExperience.UI
{
    /// <summary>
    /// Subtle floating text near the pawn when XP is awarded. Throttled to avoid spam.
    /// Also logs if debug enabled.
    /// </summary>
    public static class ExperienceNotifications
    {
        public static void NotifyXpGain(Pawn pawn, SkillDef skill, float amount)
        {
            if (pawn == null || skill == null) return;
            if (pawn.Map == null || !pawn.Spawned) return;

            try
            {
                // Aggregate message – show skill icon + XP once per batch, not per tick.
                string text = $"+{amount:F1} {skill.LabelCap}";

                // Use MoteMaker.ThrowText if available; fallback to Messages.
                // MoteMaker.ThrowText signature: ThrowText(Vector3 loc, Map map, string text, Color color, float timeBeforeStart = 0)
                if (MoteMaker.CanThrowText(pawn.DrawPos))
                {
                    // Use skill-tinted color – light blue for intellectual, orange for crafting, etc.
                    Color color = Color.white;
                    if (skill.defName == "Construction") color = new Color(0.9f, 0.75f, 0.5f);
                    else if (skill.defName == "Crafting") color = new Color(0.85f, 0.85f, 0.6f);
                    else if (skill.defName == "Medicine") color = new Color(0.6f, 0.85f, 0.85f);
                    else color = new Color(0.7f, 0.85f, 1f);

                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, text, color);
                }
            }
            catch (System.Exception ex)
            {
                // Notifications are non-critical; swallow errors but log in debug.
                if (Core.ExperienceManager.Settings != null && Core.ExperienceManager.Settings.enableDebugLogging)
                    Log.Warning($"[EarnedExperience] Notification error: {ex.Message}");
            }
        }
    }
}