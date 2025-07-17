#nullable enable

using System;
using HarmonyLib;
using JetBrains.Annotations;
using SmithingPlus.Metal;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.BitsRecovery;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[HarmonyPatch(typeof(BlockEntityAnvil))]
[HarmonyPatchCategory(Core.HelveHammerBitsRecoveryCategory)]
public class HelveHammerRecoveryPatches
{
    [HarmonyPrefix]
    [HarmonyPatch("OnHelveHammerHit")]
    public static void Prefix(BlockEntityAnvil __instance, ref int __state)
    {
        if (__instance.Api.Side.IsClient()) return;
        if (__instance.WorkItemStack == null) return;
        var voxelCount = __instance.Voxels.MaterialCount();
        __state = voxelCount;
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnHelveHammerHit")]
    public static void Postfix(BlockEntityAnvil __instance, ref int __state)
    {
        if (__instance.Api.Side.IsClient()) return;
        if (__state == 0) return;
        if (__instance.WorkItemStack == null) return; // For now, don't bother about the last hit
        var voxelCount = __state;
        if (voxelCount == __instance.Voxels.MaterialCount()) return;
        var workItemStack = __instance.WorkItemStack;
        var api = __instance.Api;
        var metalMaterial = workItemStack.GetMetalMaterialProcessed(api);
        var splitCount = workItemStack.GetSplitCount();
        var bitsPerVoxel = 1f / Core.Config.VoxelsPerBit;
        splitCount += bitsPerVoxel;
        if (splitCount < 1)
        {
            __instance.WorkItemStack.SetSplitCount(splitCount);
            return;
        }

        __instance.WorkItemStack.SetSplitCount(Math.Max(splitCount - 1, 0));
        Core.Logger.VerboseDebug("[BitsRecovery][OnHelveHammerHit] Attempting to recover bits from {0}",
            workItemStack.Collectible.Code);
        if (!(metalMaterial?.Resolved ?? false))
        {
            Core.Logger.VerboseDebug(
                "[BitsRecovery#BEAnvil_OnHelveHammerHit_Postfix] No valid metal material found in work item.");
            return;
        }

        var metalBitStack = metalMaterial.MetalBitStack;
        var temperature = workItemStack.Collectible.GetTemperature(api.World, workItemStack);
        metalBitStack?.Collectible.SetTemperature(api.World, metalBitStack, temperature);
        __instance.Api.World.SpawnItemEntity(metalBitStack, __instance.Pos);
    }
}