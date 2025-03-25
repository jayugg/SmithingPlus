using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.ToolRecovery;

[HarmonyPatch]
[HarmonyPatchCategory(Core.ToolRecoveryCategory)]
public class NoSmithingEstocPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemIngot), nameof(ItemIngot.GetMatchingRecipes))]
    public static void GetMatchingRecipes_Postfix(ItemIngot __instance, ref List<SmithingRecipe> __result,
        ItemStack stack)
    {
        __result = __result.Where((System.Func<SmithingRecipe, bool>)(
            r => r.Ingredient.SatisfiesAsIngredient(stack)
                 && !(r.Ingredient.RecipeAttributes?["nuggetRecipe"]?.AsBool() ?? false)
                 && !(r.Ingredient.RecipeAttributes?["repairOnly"]?.AsBool() ?? false)
        )).OrderBy((System.Func<SmithingRecipe, AssetLocation>)(
                r => r.Output.ResolvedItemstack.Collectible.Code)
        ).ToList();
    }
}