namespace EarnedExperience.Core
{
    /// <summary>
    /// Tunable balance constants. Values chosen to feel like vanilla progression:
    /// a pawn stonecutting full-time should gain roughly 1 Crafting level every 1.5-2 quadrums
    /// at level 5-8 with no passion. Higher levels remain slow.
    /// </summary>

    public static class Constants
    {
        public const float BaseXpPerWorkTick_Stonecutting = 0.025f;
        public const float BaseXpPerWorkTick_Smelting     = 0.020f;
        public const float BaseXpPerWorkTick_Cleaning     = 0.008f;
        public const float BaseXpPerWorkTick_Cremation    = 0.012f;
        public const float BaseXpPerWorkTick_Refueling    = 0.010f;
        public const float BaseXpPerWorkTick_TrapRearm    = 0.015f;
        public const float BaseXpPerWorkTick_Firefighting = 0.018f;
        public const float BaseXpPerWorkTick_PatientCare  = 0.015f;


        public const float Hauling_BaseXpPerJob           = 1.8f;
        public const float Hauling_XpPerCellDistance      = 0.05f;
        public const float Hauling_XpPerKgMass            = 0.04f;
        public const float Hauling_MaxXpPerJob            = 8f;
        public const float Hauling_MinDistanceForBonus    = 5f;

        public const float TransportLoading_BaseXp        = 1.5f;
        public const float TransportLoading_XpPerKg       = 0.03f;
        public const float TransportLoading_MaxXpPerJob   = 6f;


        public const int TickBatchSize = 250;

        public const int NotificationMinIntervalTicks = 600;

        public const int MinWorkTicksForReward = 15;
    }

    /// <summary>
    /// Identifies each supported activity for settings, logging and routing.
    /// </summary>

    public enum ActivityType
    {
        Stonecutting = 0,
        Smelting = 1,
        Cleaning = 2,
        Hauling = 3,
        TransportLoading = 4,
        Refueling = 5,
        TrapRearming = 6,
        Cremation = 7,
        Firefighting = 8,
        PatientCare = 9
    }

    public static class ActivityTypeExtensions
    {
        public static string ToSaveKey(this ActivityType type) => type.ToString();
        public static string ToLabelKey(this ActivityType type) => $"EarnedExperience_{type}_Label";
        public static string ToDescKey(this ActivityType type) => $"EarnedExperience_{type}_Desc";
    }
}