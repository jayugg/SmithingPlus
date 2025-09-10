using System.Linq;
using SmithingPlus.Util;

namespace SmithingPlus.Config;

public class ServerConfig
{
    public bool RecoverBitsOnSplit { get; set; } = true;
    public bool HelveHammerBitsRecovery { get; set; } = true;
    public float VoxelsPerBit { get; set; } = 2.1f;
    public bool SmithWithBits { get; set; } = true;
    public bool BitsTopUp { get; set; } = true;
    public bool EnableToolRecovery { get; set; } = true;
    public float DurabilityPenaltyPerRepair { get; set; } = 0.05f;
    public string ToolRepairForgettableAttributes { get; set; } = "quality,maxRepair";
    public float RepairableToolDurabilityMultiplier { get; set; } = 1.0f;
    public float BrokenToolVoxelPercent { get; set; } = 0.8f;

    public string RepairableToolSelector { get; set; } =
        "@.*(pickaxe|shovel|saw|axe|hoe|knife|hammer|chisel|shears|sword|spear|bow|shield|sickle|scythe|tongs|wrench|solderingiron|cleaver|prospectingpick|crossbow|pistol|rifle|shotgun|blade).*";

    public string ToolHeadSelector { get; set; } = "@(.*)(head|blade|boss|barrel|stirrup)(.*)";
    public string IngotSelector { get; set; } = "@(.*):ingot-(.*)";
    public string WorkItemSelector { get; set; } = "@(.*):workitem-(.*)";
    public bool DontRepairBrokenToolHeads { get; set; } = false;
    public bool CanRepairForlornHopeEstoc { get; set; } = true;
    public bool ShowRepairedCount { get; set; } = true;
    public bool ShowBrokenCount { get; set; } = true;
    public bool ShowRepairSmithName { get; set; } = false;
    public float HelveHammerSmithingQualityModifier { get; set; } = 1;
    public bool ArrowsDropBits { get; set; } = true;
    public string ArrowSelector { get; set; } = "@(.*):arrow-(.*)";
    public bool MetalCastingTweaks { get; set; } = true;
    public float CastToolDurabilityPenalty { get; set; } = 0.1f;
    public bool AdjustedCastingMetalRequirements { get; set; } = true;
    public bool HammerTweaks { get; set; } = true;
    public bool RotationRequiresTongs { get; set; } = false;
    public bool AnvilShowRecipeVoxels { get; set; } = true;
    public bool RememberHammerToolMode { get; set; } = true;
    public bool ShowWorkableTemperature { get; set; } = true;

    public bool HandbookExtraInfo { get; set; } = true;

    // public bool StoneSmithing { get; set; } = false;
    public string[] GetToolRepairForgettableAttributes =>
        ToolRepairForgettableAttributes.Split(",")
            .Append("durability")
            .Append(ModAttributes.RepairedToolStack)
            .Append(ModAttributes.CastTool)
            .ToArray();
}