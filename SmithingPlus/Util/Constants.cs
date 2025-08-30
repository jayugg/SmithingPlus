namespace SmithingPlus.Util;

public static class Constants
{
    internal const string AnvilWorkableColor = "#00A36C";
    internal const string Sp = "sp";
}

public static class ModAttributes
{
    internal const string RepairSmith = "repairSmith";
    internal const string RepairedToolStack = "repairedToolStack";
    internal const string BrokenCount = "brokenCount";
    internal const string SmithingQuality = $"{Constants.Sp}:smithingQuality";
    internal const string ToolRepairPenaltyModifier = $"{Constants.Sp}:toolRepairPenaltyModifier";
    internal const string SplitCount = $"{Constants.Sp}:splitCount";
    internal const string IsPureMetal = "isPureMetal";
    internal const string CastTool = $"{Constants.Sp}:castTool";
    internal const string FlipItemToolMode = $"{Constants.Sp}:flipItemToolMode";
    internal const string RotationX = $"{Constants.Sp}:rotationX";
    internal const string RotationZ = $"{Constants.Sp}:rotationZ";
    internal const string MinY = $"{Constants.Sp}:minY";
    internal const string LastScrapeMs = $"{Constants.Sp}:lastScrapeMs";
}

public static class ModRecipeAttributes
{
    internal const string RepairOnly = "repairOnly";
    internal const string NuggetRecipe = "nuggetRecipe";
    internal const string RecyclingRecipe = "recyclingRecipe";
}

public static class ModStats
{
    internal const string SmithingQuality = ModAttributes.SmithingQuality;
    internal const string ToolRepairPenalty = $"{Constants.Sp}:toolRepairPenalty";
}