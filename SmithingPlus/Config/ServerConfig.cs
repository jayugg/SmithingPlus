using System.Linq;
using Newtonsoft.Json;
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
    public string ToolRepairForgettableAttributes { get; set; } = "quality,maxRepair,buffs";
    public float RepairableToolDurabilityMultiplier { get; set; } = 1.0f;
    public float BrokenToolVoxelPercent { get; set; } = 0.8f;

    public string RepairableToolSelector { get; set; } =
        "@.*(pickaxe|shovel|saw|axe|hoe|knife|hammer|chisel|shears|sword|spear|bow|shield|sickle|scythe|tongs|wrench|solderingiron|cleaver|prospectingpick|crossbow|pistol|rifle|shotgun|blade|halberd|poleaxe|quarterstaff|pike).*";

    public string ToolHeadSelector { get; set; } = "@(.*)(head|blade|boss|barrel|stirrup|part)(.*)";
    public string IngotSelector { get; set; } = "@(.*):ingot-(.*)";
    public string WorkItemSelector { get; set; } = "@(.*):workitem-(.*)";
    public bool DontRepairBrokenToolHeads { get; set; } = false;
    public bool CanRepairForlornHopeEstoc { get; set; } = true;
    public float HelveHammerSmithingQualityModifier { get; set; } = 1;
    public bool ArrowsDropBits { get; set; } = true;
    public string ArrowSelector { get; set; } = "@(.*):arrow-(.*)";
    public bool MetalCastingTweaks { get; set; } = true;
    public float CastToolDurabilityPenalty { get; set; } = 0.1f;
    public bool DynamicMoldUnits { get; set; } = false;
    public bool HammerTweaks { get; set; } = true;
    public bool RotationRequiresTongs { get; set; } = false;

    // public bool StoneSmithing { get; set; } = false;
    [JsonIgnore]
    public string[] GetToolRepairForgettableAttributes =>
        ToolRepairForgettableAttributes.Split(",")
            .Append("durability")
            .Append(ModStackAttributes.RepairedToolStack)
            .Append(ModStackAttributes.CastTool)
            .ToArray();
}
