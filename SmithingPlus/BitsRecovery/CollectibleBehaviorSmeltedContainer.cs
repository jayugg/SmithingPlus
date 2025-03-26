using System.Text;
using SmithingPlus.Util;
using Vintagestory.API.Common;

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
        { 
            dsc.AppendLine($"<i>Scrape with <hk>rightmouse</hk> to recover bits</i>");
        }
    }
}