using System.Text;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace SmithingPlus.BitsRecovery;

public class CollectibleBehaviorSmeltedContainer : CollectibleBehavior
{
    public CollectibleBehaviorSmeltedContainer(CollectibleObject collObj) : base(collObj)
    {
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        var temp = inSlot.Itemstack.GetTemperature(world);
        if (temp < CollectibleBehaviorScrapeCrucible.MaxScrapeTemperature)
            dsc.AppendLine(Lang.Get($"{Core.ModId}:heldhelp-scrapecrucible"));
    }
}