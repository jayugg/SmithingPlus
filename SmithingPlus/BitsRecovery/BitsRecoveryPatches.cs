using System;
using HarmonyLib;
using SmithingPlus.ToolRecovery;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SmithingPlus.BitsRecovery;

[HarmonyPatch(typeof(BlockEntityAnvil))]
[HarmonyPatchCategory(Core.BitsRecoveryCategory)]
public class BitsRecoveryPatches
{
    [HarmonyPostfix]
    [HarmonyPatch("OnUseOver", typeof(IPlayer), typeof(Vec3i), typeof(BlockSelection))]
    static void BlockEntityAnvil_OnUseOver_Postfix(BlockEntityAnvil __instance, IPlayer byPlayer, Vec3i voxelPos, BlockSelection blockSel)
    {
        if (byPlayer.Entity.Api.Side.IsClient()) return;
        if (!__instance.CanWorkCurrent) return; // Can't work the item
        var activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (activeHotbarSlot?.Itemstack == null) return;
        if (__instance.WorkItemStack is not { } workItemStack) return;
    
        int toolMode = activeHotbarSlot.Itemstack.Collectible.GetToolMode(activeHotbarSlot, byPlayer, blockSel);
        if (toolMode != 5) return;  // 5 is the split mode
        var splitCount = activeHotbarSlot.Itemstack.GetSplitCount();
        var bitsPerVoxel = 1f/Core.Config.VoxelsPerBit;
        splitCount += bitsPerVoxel;
        if (splitCount < 1)
        {
            Core.Logger.VerboseDebug("[BitsRecovery] Failed to recover bits from {0}", workItemStack);
            activeHotbarSlot.Itemstack.SetSplitCount(splitCount);
            return;
        }
        activeHotbarSlot.Itemstack.SetSplitCount(Math.Max(splitCount - 1, 0));
        Core.Logger.VerboseDebug("[BitsRecovery] Attempting to recover bits from {0}", workItemStack);
        var metalVariant = ItemDamagedPatches.GetMetalOrMaterial(workItemStack);
        if (metalVariant == "blistersteel") {metalVariant = "steel";}
        var metalbit = byPlayer.Entity.World.GetItem(new AssetLocation("game:metalbit-copper")).ItemWithVariant("metal", metalVariant);
        if (metalbit == null) { Core.Logger.VerboseDebug("[BitsRecovery] no metalbit found for variant {0}", metalVariant); return;}
        var metalbitStack = new ItemStack(metalbit);
        var temperature = workItemStack.Collectible.GetTemperature(byPlayer.Entity.World, workItemStack);
        metalbitStack.Collectible.SetTemperature(byPlayer.Entity.World, metalbitStack, temperature);
        if (byPlayer.InventoryManager.TryGiveItemstack(metalbitStack)) return;
        byPlayer.Entity.World.SpawnItemEntity(metalbitStack, byPlayer.Entity.Pos.XYZ);
    }
}