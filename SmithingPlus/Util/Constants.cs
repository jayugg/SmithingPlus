namespace SmithingPlus.Util;

public static class Constants
{
    public const string AnvilWorkableColor = "#00A36C";
}

public static class ModAttributes
{
    public const string RepairSmith = "repairSmith";
    public const string RepairedToolStack = "repairedToolStack";
    public const string BrokenCount = "brokenCount";
    public const string SmithingQuality = "sp:smithingQuality";
    public const string ToolRepairPenaltyModifier = "sp:toolRepairPenaltyModifier";
    public const string SplitCount = "sp:splitCount";
    public const string IsPureMetal = "isPureMetal";
    public const string CastTool = "sp:castTool";
}

public static class ModRecipeAttributes
{
    public const string RepairOnly = "repairOnly";
    public const string NuggetRecipe = "nuggetRecipe";
}

public static class ModStats
{
    public const string SmithingQuality = ModAttributes.SmithingQuality;
    public const string ToolRepairPenalty = "sp:toolRepairPenalty";
}