using System;
using HarmonyLib;
using JetBrains.Annotations;
using SmithingPlus.Metal;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SmithingPlus.BitsRecovery;

#nullable enable

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[HarmonyPatch(typeof(BlockEntityAnvil), "OnUseOver", typeof(IPlayer), typeof(Vec3i), typeof(BlockSelection))]
[HarmonyPatchCategory(Core.BitsRecoveryCategory)]
public class BitsRecoveryPatches
{
    [HarmonyPrefix]
    public static void BlockEntityAnvil_OnUseOver_Prefix(BlockEntityAnvil __instance, out byte __state,
        IPlayer byPlayer, Vec3i voxelPos, BlockSelection blockSel)
    {
        __state = __instance.Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z];
    }

    [HarmonyPostfix]
    public static void BlockEntityAnvil_OnUseOver_Postfix(BlockEntityAnvil __instance, byte __state, IPlayer byPlayer,
        Vec3i voxelPos, BlockSelection blockSel)
    {
        if (byPlayer.Entity.Api.Side.IsClient()) return;
        if (!__instance.CanWorkCurrent) return; // Can't work the item
        var activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (activeHotbarSlot?.Itemstack == null) return;
        if (__instance.WorkItemStack is not { } workItemStack) return;

        var toolMode = activeHotbarSlot.Itemstack.Collectible.GetToolMode(activeHotbarSlot, byPlayer, blockSel);
        if (toolMode != 5) return; // 5 is the split mode
        var voxelType = __state;
        if (voxelType != 1) // only continue if voxel is metal
        {
            Core.Logger.VerboseDebug("[BitsRecovery] Non-metal voxel type: {0}", voxelType);
            return;
        }

        RecoverBitsFromWorkItem(__instance, byPlayer, workItemStack);
    }

    private static void RecoverBitsFromWorkItem(BlockEntityAnvil __instance, IPlayer byPlayer, ItemStack workItemStack)
    {
        var splitCount = __instance.WorkItemStack.GetSplitCount();
        var bitsPerVoxel = 1f / Core.Config.VoxelsPerBit;
        splitCount += bitsPerVoxel;
        if (splitCount < 1)
        {
            __instance.WorkItemStack.SetSplitCount(splitCount);
            return;
        }

        __instance.WorkItemStack.SetSplitCount(Math.Max(splitCount - 1, 0));
        Core.Logger.VerboseDebug("[BitsRecovery] Attempting to recover bits from {0}", workItemStack);
        var metalMaterial = workItemStack.GetMetalMaterialProcessed(byPlayer.Entity.Api);
        if (metalMaterial == null)
        {
            Core.Logger.VerboseDebug(
                "[BitsRecovery#BEAnvil_OnUseOver_Postfix] No valid metal material found in work item.");
            return;
        }

        var metalBitStack = metalMaterial.MetalBitStack;
        var temperature = workItemStack.Collectible.GetTemperature(byPlayer.Entity.World, workItemStack);
        metalBitStack?.Collectible.SetTemperature(byPlayer.Entity.World, metalBitStack, temperature);
        if (byPlayer.InventoryManager.TryGiveItemstack(metalBitStack)) return;
        byPlayer.Entity.World.SpawnItemEntity(metalBitStack, byPlayer.Entity.Pos.XYZ);
    }
}