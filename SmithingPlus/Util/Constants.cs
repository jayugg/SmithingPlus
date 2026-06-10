namespace SmithingPlus.Util;

public static class Constants
{
    internal const string AnvilWorkableColor = "#00A36C";
    internal const string Sp = "sp";
}

public static class ModTempAttributes
{
    internal const string FlipItemToolMode = $"{Constants.Sp}:flipItemToolMode";
}

public static class ModStackAttributes
{
    // Tool and tool head attributes
    internal const string RepairSmith = "repairSmith";
    internal const string BrokenCount = "brokenCount";
    internal const string SmithingQuality = $"{Constants.Sp}:smithingQuality";
    internal const string ToolRepairPenaltyModifier = $"{Constants.Sp}:toolRepairPenaltyModifier";
    internal const string CastTool = $"{Constants.Sp}:castTool";

    // Tool head attributes
    internal const string RepairedToolStack = "repairedToolStack";

    // Work item attributes
    internal const string SplitCount = $"{Constants.Sp}:splitCount";
    internal const string RotationX = $"{Constants.Sp}:rotationX";
    internal const string RotationZ = $"{Constants.Sp}:rotationZ";
    internal const string MinY = $"{Constants.Sp}:minY";

    internal static string[] DialogueIgnoredAttributes =>
    [
        RepairSmith,
        BrokenCount,
        RepairedToolStack,
        SmithingQuality,
        ToolRepairPenaltyModifier,
        CastTool,
        // Should not matter since they only gets applied to work items
        SplitCount,
        RotationX,
        RotationZ,
        MinY
    ];
}

public static class ModAttributes
{
    internal const string IsPureMetal = "isPureMetal";
}

public static class ModRecipeAttributes
{
    internal const string RepairOnly = "repairOnly";
    internal const string NuggetRecipe = "nuggetRecipe";
    internal const string WorkableRecipe = "workableRecipe";
    internal const string RecyclingRecipe = "recyclingRecipe";
}

public static class ModStats
{
    internal const string SmithingQuality = ModStackAttributes.SmithingQuality;
    internal const string ToolRepairPenalty = $"{Constants.Sp}:toolRepairPenalty";
}