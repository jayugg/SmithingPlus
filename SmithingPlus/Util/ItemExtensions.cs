using Vintagestory.API.Common;

namespace SmithingPlus.Util;

public static class ItemExtensions
{
    public static Item ItemWithVariant(this Item item, string key, string value)
    {
        if (Core.Api != null) return Core.Api.World.GetItem(item.CodeWithVariant(key, value));
        Core.Logger.Error("Core.Api is null, call this extension method after the mod has started");
        return item;
    } 
}