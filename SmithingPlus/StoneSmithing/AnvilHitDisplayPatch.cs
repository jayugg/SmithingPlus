using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.StoneSmithing;

[HarmonyPatch(typeof(BlockEntityAnvil))]
[HarmonyPatchCategory(Core.StoneSmithingCategory)]
public class AnvilHitDisplayPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(BlockEntityAnvil.GetBlockInfo))]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix_GetBlockInfo(BlockEntityAnvil __instance, IPlayer forPlayer, StringBuilder dsc)
    {
        if (forPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Collectible is not ItemStoneHammer) return;
        var selectionBoxIndex = forPlayer.CurrentBlockSelection?.SelectionBoxIndex;
        if (selectionBoxIndex == null || __instance.WorkItemStack == null) return;
        var voxelHitCount = ItemStoneHammer.GetVoxelHitCount(__instance.WorkItemStack, selectionBoxIndex.Value);
        dsc.AppendLine($"Hits left: {ItemStoneHammer.MaxHitCount - voxelHitCount}");
    }
}

[HarmonyPatch(typeof(BlockEntityAnvilPart))]
[HarmonyPatchCategory(Core.StoneSmithingCategory)]
public class AnvilPartPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(BlockEntityAnvil.GetBlockInfo))]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix_GetBlockInfo(BlockEntityAnvil __instance, IPlayer forPlayer, StringBuilder dsc)
    {
        if (forPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack is not
            { Collectible: ItemStoneHammer } hammerStack) return;
        var selectionBoxIndex = forPlayer.CurrentBlockSelection?.SelectionBoxIndex;
        if (selectionBoxIndex == null || __instance.WorkItemStack == null) return;
        var voxelHitCount = ItemStoneHammer.GetVoxelHitCount(hammerStack, selectionBoxIndex.Value);
        dsc.AppendLine($"Hits left: {ItemStoneHammer.MaxHitCount - voxelHitCount}");
    }
}