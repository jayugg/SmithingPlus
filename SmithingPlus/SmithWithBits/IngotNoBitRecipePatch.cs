using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SmithingPlus.Util;
using Vintagestory.GameContent;
using AssetLocation = Vintagestory.API.Common.AssetLocation;
using ItemStack = Vintagestory.API.Common.ItemStack;

namespace SmithingPlus.SmithWithBits;

[HarmonyPatch]
[HarmonyPatchCategory(Core.SmithingBitsCategory)]
public class IngotNoBitRecipePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemIngot), nameof(ItemIngot.GetMatchingRecipes))]
    public static void GetMatchingRecipes_Postfix(ItemIngot __instance, ref List<SmithingRecipe> __result,
        ItemStack stack)
    {
        __result = __result.Where((Func<SmithingRecipe, bool>)(
            r => r.Ingredient.SatisfiesAsIngredient(stack)
                 && !(r.Ingredient.RecipeAttributes?[ModRecipeAttributes.NuggetRecipe]?.AsBool() ?? false)
        )).OrderBy((Func<SmithingRecipe, AssetLocation>)(
                r => r.Output.ResolvedItemstack.Collectible.Code)
        ).ToList();
    }
}