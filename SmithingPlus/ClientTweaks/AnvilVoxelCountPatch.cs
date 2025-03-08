using System.Linq;
using System.Text;
using HarmonyLib;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace SmithingPlus.ClientTweaks;

[HarmonyPatch(typeof(BlockEntityAnvil))]
[HarmonyPatchCategory(Core.ClientTweaksCategories.AnvilShowRecipeVoxels)]
public class AnvilVoxelCountPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(BlockEntityAnvil.GetBlockInfo)), HarmonyPriority(Priority.VeryLow)]
    public static void Postfix_GetBlockInfo(BlockEntityAnvil __instance, IPlayer forPlayer, StringBuilder dsc)
    {
        if (forPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Collectible is not ItemHammer &&
            forPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Collectible is not IAnvilWorkable)
            return;
        if (__instance.recipeVoxels == null) return;
        if (__instance.CanWorkCurrent == false) return;
        var voxelCount = CacheHelper.GetOrAdd(Core.RecipeVoxelCountCache, __instance.SelectedRecipeId, () =>
            {
                Core.Logger.VerboseDebug("Calculating voxel count for: {0}", __instance.SelectedRecipeId);
                return __instance.recipeVoxels.Cast<bool>().Count(voxel => voxel);
            });
        var currentVoxelCount = __instance.Voxels?.Cast<byte>().Count(voxel => voxel != 0) ?? 0;
        dsc.AppendLine(Lang.Get($"{Core.ModId}:blockdesc-voxelcount", currentVoxelCount, voxelCount));
    }
}