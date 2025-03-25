using System.Collections.Generic;
using HarmonyLib;
using SmithingPlus.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace SmithingPlus.ClientTweaks;

[HarmonyPatchCategory(Core.ClientTweaksCategories.AnvilShowRecipeVoxels)]
public class RecipeVoxelCountPatch
{
    private static List<SmithingRecipe> _selectedRecipes = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockEntityAnvil), "OpenDialog")]
    public static void OpenDialog_Postfix(BlockEntityAnvil __instance, ItemStack ingredient)
    {
        if (Core.Api.Side != EnumAppSide.Client) return;
        if (__instance == null || ingredient == null) return;
        var recipes = (ingredient.Collectible as IAnvilWorkable)?.GetMatchingRecipes(ingredient);
        if (recipes != null) _selectedRecipes = recipes;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GuiDialogBlockEntityRecipeSelector), "SetupDialog")]
    public static void SetupDialog_Postfix(GuiDialogBlockEntityRecipeSelector __instance)
    {
        if (_selectedRecipes.Count == 0) return;
        if (Core.Api is not ICoreClientAPI capi) return;
        var skillItems = __instance.GetField<List<SkillItem>>("skillItems");
        var prevSlotOver = __instance.GetField<int>("prevSlotOver");
        __instance.SingleComposer.GetSkillItemGrid("skillitemgrid").OnSlotOver = num =>
        {
            if (num >= skillItems.Count || num == prevSlotOver || num >= _selectedRecipes.Count)
                return;
            var recipeId = _selectedRecipes[num].RecipeId;
            var voxelCount = CacheHelper.GetOrAdd(Core.RecipeVoxelCountCache, recipeId,
                () =>
                {
                    return capi.GetSmithingRecipes().Find(recipe => recipe.RecipeId == recipeId).Voxels.VoxelCount();
                });
            //var bitsCount = (int) (voxelCount / Core.Config.VoxelsPerBit);
            var descWithVoxelCount = $"{Lang.Get("Requires {0} voxels", voxelCount)}. {skillItems[num].Description}";
            __instance.SingleComposer.GetDynamicText("name").SetNewText(skillItems[num].Name);
            __instance.SingleComposer.GetDynamicText("desc").SetNewText(descWithVoxelCount);
        };
        _selectedRecipes.Clear();
    }
}