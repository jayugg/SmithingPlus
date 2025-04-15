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
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), "addCreatedByInfo")]
    public static void PatchSmithingInfo(
        CollectibleBehaviorHandbookTextAndExtraInfo __instance,
        ICoreClientAPI capi,
        ItemStack[] allStacks,
        ActionConsumable<string> openDetailPageFor,
        ItemStack stack,
        List<RichTextComponentBase> components)
    {
        // Find where the "Smithing" section is in the components list
        var smithingSectionIndex = -1;
        for (var i = 0; i < components.Count; i++)
        {
            if (components[i] is not LinkTextComponent linkComponent) continue;
            var isSmithingHeader =
                linkComponent.DisplayText != null && linkComponent.DisplayText.Contains(Lang.Get("Smithing"));

            if (!isSmithingHeader || i + 1 >= components.Count) continue;
            smithingSectionIndex = i + 1;
            break;
        }

        var smallestSmithingRecipe = CacheHelper.GetOrAdd(
            Core.SmallestSmithingRecipeCache,
            stack.Collectible.Code,
            () => capi.GetSmithingRecipes()
                .FindAll(recipe => recipe.Output.Matches(capi.World, stack))
                .OrderBy(recipe => recipe.Voxels.VoxelCount())
                .FirstOrDefault());
        if (smallestSmithingRecipe == null) return;
        var voxelCount = smallestSmithingRecipe.Voxels.VoxelCount();
        var bitsCount = (int)Math.Ceiling(voxelCount / Core.Config.VoxelsPerBit);
        var baseMaterial = stack.GetOrCacheMetalMaterial(capi)?.IngotStack;
        if (baseMaterial == null) return;
        var allMaterialStacks = GetSmithingIngredientStacks(capi, stack, baseMaterial, voxelCount, bitsCount,
            smallestSmithingRecipe.RecipeId);
        if (allMaterialStacks.Count <= 0) return;
        // If Smithing section was found, add custom info
        var smithingSectionExists = smithingSectionIndex >= 0;
        if (!smithingSectionExists)
        {
            components.RemoveAt(components.Count - 1);
            components.RemoveAt(components.Count - 1);
            AddSubHeading(components, capi, openDetailPageFor,
                $"{Lang.Get("Smithing")} {Lang.Get("with")}\n",
                "craftinginfo-smithing");
            components.AddRange(allMaterialStacks.Select(itemStack =>
                new ItemstackTextComponent(capi, itemStack, 40, 2.0, EnumFloat.Inline,
                    cs =>
                        openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))) { ShowStacksize = true }));
        }
        else
        {
            components.RemoveAt(smithingSectionIndex - 1);
            components.Insert(smithingSectionIndex - 1,
                new LinkTextComponent(capi, $"{Lang.Get("Smithing")} {Lang.Get("with")}\n", CairoFont.WhiteSmallText(),
                    _ => openDetailPageFor("craftinginfo-smithing")));
            components.Insert(smithingSectionIndex, new ClearFloatTextComponent(capi, 2f));
            foreach (var itemStack in allMaterialStacks)
            {
                smithingSectionIndex++;
                var itemstackTextComponent = new ItemstackTextComponent(capi, itemStack, 40, 2.0, EnumFloat.Inline,
                    cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)))
                {
                    ShowStacksize = true
                };
                components.Insert(smithingSectionIndex, itemstackTextComponent);
            }
        }
    }

    public static List<ItemStack> GetSmithingIngredientStacks(ICoreClientAPI capi, ItemStack stack,
        ItemStack baseMaterial,
        int voxelCount, int bitsCount, int recipeId)
    {
        var allMaterialCollectibles = capi.World.Collectibles
            .Where(collectible =>
                collectible is IAnvilWorkable and not ItemWorkItem &&
                collectible.CombustibleProps?.SmeltedStack?.Resolve(capi.World, "worldForResolving") != null &&
                !collectible.Equals(stack.Collectible) &&
                collectible.Satisfies(collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack, baseMaterial) &&
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