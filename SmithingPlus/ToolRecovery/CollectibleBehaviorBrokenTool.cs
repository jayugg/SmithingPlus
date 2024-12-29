using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace SmithingPlus.ToolRecovery;

public class CollectibleBehaviorBrokenTool : CollectibleBehavior 
{
    public CollectibleBehaviorBrokenTool(CollectibleObject collObj) : base(collObj)
    {
    }
    
    protected virtual string LangKey => "Repaired";

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        if (inSlot?.Itemstack?.Collectible?.Code == null) return;
        AssetLocation code;
        try
        {
            if (inSlot?.Itemstack?.Collectible?.Code == null) throw new NullReferenceException("Itemstack or Collectible is null");
            code = inSlot.Itemstack.Collectible.Code;
        }
        catch (Exception e)
        {
            Core.Logger.Error("Failed to get code for {0}: {1}", inSlot?.Itemstack, e);
            return;
        }
        if (!WildcardUtil.Match(Core.Config.RepairableToolSelector, code?.ToString()))
            return;
        var brokenCount = inSlot.Itemstack.GetBrokenCount();
        if (brokenCount <= 0) return;
        if (Core.Config.ShowRepairedCount) dsc.AppendLine(Lang.Get($"{LangKey} {{0}} times", brokenCount));
        if (Core.Config.ShowRepairSmithName && inSlot.Itemstack.GetRepairSmith() is { } repairSmith)
            dsc.AppendLine(Lang.Get("Last repaired by {0}", repairSmith));
    }
}