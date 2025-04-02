using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.HammerTweaks;

[HarmonyPatchCategory(Core.HammerTweaksCategory)]
[HarmonyPatch(typeof(BlockEntityAnvil))]
public static class BlockEntityAnvilPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnPlayerInteract")]
    public static bool Prefix_OnPlayerInteract(BlockEntityAnvil __instance, IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSel)
    {
        var activeSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        var itemstack = activeSlot.Itemstack;
        if (itemstack == null)
            return true;
        if (itemstack.Collectible.Tool.GetValueOrDefault() != EnumTool.Hammer ||
            itemstack.Collectible.GetToolMode(activeSlot, byPlayer, blockSel) != 6)
            return true;
        __instance.FlipWorkItem();
        return false;
    }

    private static void FlipWorkItem(this BlockEntityAnvil anvil)
    {
        var flippedVoxels = new byte[16, 6, 16];

        // Calculate maxFilledY by finding the highest y index with a nonzero voxel.
        var maxFilledY = 0;
        for (var y = 0; y < 6; y++)
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
            if (anvil.Voxels[x, y, z] != 0)
                maxFilledY = Math.Max(maxFilledY, y);

        var shapeHeight = maxFilledY + 1;

        // Flip the voxels in the filled region and place them at the bottom.
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
            // For the filled region, mirror the voxels.
        for (var y = 0; y < shapeHeight; y++)
            flippedVoxels[x, y, z] = anvil.Voxels[x, shapeHeight - 1 - y, z];

        anvil.Voxels = flippedVoxels;
        var regenMethod = anvil.GetType()
            .GetMethod("RegenMeshAndSelectionBoxes", BindingFlags.Instance | BindingFlags.NonPublic);

        regenMethod?.Invoke(anvil, null);
        anvil.MarkDirty();
    }
}