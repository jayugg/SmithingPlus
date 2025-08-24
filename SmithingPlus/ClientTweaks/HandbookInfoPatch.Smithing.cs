using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SmithingPlus.Metal;
using SmithingPlus.SmithWithBits;
using SmithingPlus.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using LinkTextComponent = Vintagestory.API.Client.LinkTextComponent;

namespace SmithingPlus.ClientTweaks;

public partial class HandbookInfoPatch
{
    public static List<ItemStack> GetSmithingIngredientStacks(ICoreClientAPI capi, ItemStack stack,
        ItemStack baseMaterial,
        int voxelCount, int bitsCount, int recipeId)
    {
        var allMaterialCollectibles = capi.World.Collectibles
            .Where(collectible =>
                collectible is IAnvilWorkable and not ItemWorkItem &&
                !collectible.Equals(stack.Collectible) &&
                collectible.CombustibleProps?.SmeltedStack?.Resolve(capi.World, "worldForResolving") == true &&
                collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack is { } smeltedStack &&
                collectible.Satisfies(smeltedStack, baseMaterial) &&
                ((IAnvilWorkable)collectible).GetMatchingRecipes(new ItemStack(collectible))
                .Any(recipe => recipe.RecipeId == recipeId))
            .OrderBy(collectible => collectible.Code.Domain == "game" ? -100 : 0)
            .ThenByDescending(collectible => collectible.CombustibleProps?.SmeltedRatio ?? 1)
            .ToList();
        var allMaterialStacks = allMaterialCollectibles
            .Select(collectible => new ItemStack(collectible, collectible switch
            {
                ItemMetalPlate => (int)Math.Ceiling(voxelCount / 81.0),
                ItemXWorkableNugget => (int)Math.Ceiling(voxelCount / 2.0),
                ItemWorkableNugget => bitsCount,
                _ => (int)Math.Ceiling(voxelCount * collectible.CombustibleProps.SmeltedRatio / 42.0)
            }))
            .ToList();
        return allMaterialStacks;
    }
}