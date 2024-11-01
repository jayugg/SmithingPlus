using System.Text;
using SmithingPlus.ToolRecovery;
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
        if (workableTemp != null)
        {
            dsc.AppendLine(Lang.Get("Workable Temperature: {0}", workableTemp > 0 ?
                $"{workableTemp}\u00B0C" :
                Lang.Get($"{Core.ModId}:itemdesc-temp-always")));
        }
            
    }
}