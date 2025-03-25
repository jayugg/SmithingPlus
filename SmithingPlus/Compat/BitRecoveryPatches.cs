using System;
using System.Linq;
using HarmonyLib;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.Compat;

[HarmonyPatch(typeof(ItemNugget))]
[HarmonyPatchCategory(Core.ThriftySmithingCompatCategory)]
public class BitRecoveryPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ItemNugget.OnCreatedByCrafting))]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix_OnCreatedByCrafting(
        ItemSlot[] allInputslots,
        ItemSlot outputSlot,
        GridRecipe byRecipe)
    {
        if (outputSlot.Inventory.Api is not { } api) return;
        var world = api.World;
        var smithingRecipe = allInputslots.FirstOrDefault(slot => slot.Itemstack?.GetSmithingRecipe(world) != null)
            ?.Itemstack?.GetSmithingRecipe(world);
        if (smithingRecipe == null) return;
        if (outputSlot.Itemstack == null) return;
        var voxelCount = CacheHelper.GetOrAdd(Core.RecipeVoxelCountCache, smithingRecipe.RecipeId,
            () =>
            {
                Core.Logger.VerboseDebug("Calculating voxel count for: {0}", smithingRecipe.RecipeId);
                return smithingRecipe.Voxels.VoxelCount();
            });
        var ratio = 2f + 0.1 * (voxelCount / 42f);
        outputSlot.Itemstack.StackSize = Math.Max((int)(voxelCount / ratio), 1);
    }
}