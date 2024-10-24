using Vintagestory.API.Common;

namespace SmithingPlus.ToolRecovery;

public static class Extensions
{
    internal static int? GetDurability(this ItemStack itemStack)
    {
        if (itemStack?.Attributes == null)
            return new int?();
        return itemStack.Attributes.HasAttribute("durability") ? itemStack.Attributes.GetInt("durability", itemStack.Item.Durability) : new int?();
    }

    internal static void SetDurability(this ItemStack itemStack, int number)
    {
        if (!itemStack.Attributes.HasAttribute("durability"))
            return;
        itemStack.Attributes.SetInt("durability", number);
    }

}