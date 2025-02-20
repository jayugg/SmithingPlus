using System;
using System.Collections.Generic;
using System.Text;
using SmithingPlus.ToolRecovery;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace SmithingPlus.ClientTweaks;

public class CollectibleBehaviorAnvilWorkable : CollectibleBehavior
{
    public CollectibleBehaviorAnvilWorkable(CollectibleObject collObj) : base(collObj)
    {
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        var workableTemp = inSlot.Itemstack?.GetWorkableTemperature();
        var temperature = inSlot.Itemstack?.Collectible.GetTemperature(world, inSlot.Itemstack);
        if (workableTemp != null)
        {
            dsc.AppendLine(Lang.Get("Workable Temperature: {0}", workableTemp > 0 ?
                (temperature > workableTemp ? $"<font color=\"{Constants.AnvilWorkableColor}\">{Math.Round((double)workableTemp)}\u00B0C</font>" : $"{Math.Round((double)workableTemp)}\u00B0C") :
                Lang.Get($"{Core.ModId}:itemdesc-temp-always")));
        }
    }
}