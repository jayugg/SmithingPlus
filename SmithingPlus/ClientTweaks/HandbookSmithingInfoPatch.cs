using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SmithingPlus.ToolRecovery;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using LinkTextComponent = Vintagestory.API.Client.LinkTextComponent;

namespace SmithingPlus.ClientTweaks;

[HarmonyPatchCategory(Core.ClientTweaksCategories.AnvilShowRecipeVoxels)]
public class HandbookSmithingInfoPatch
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
            bool isSmithingHeader = 
                linkComponent.DisplayText != null && linkComponent.DisplayText.Contains(Lang.Get("Smithing"));

            if (!isSmithingHeader || i + 1 >= components.Count) continue;
            smithingSectionIndex = i + 1;
            break;
        }
        var smallestSmithingRecipe = capi.GetSmithingRecipes()
            .FindAll(recipe => recipe.Output.Matches(capi.World, stack))
            .OrderBy(recipe => recipe.Voxels.Cast<bool>().Count(voxel => voxel))
            .FirstOrDefault();
        // If Smithing section was found, add custom info
        if (smithingSectionIndex < 0) smithingSectionIndex = components.Count;
        if (smallestSmithingRecipe == null) return;
        components.RemoveAt(smithingSectionIndex - 1);
        components.Insert(smithingSectionIndex - 1, new LinkTextComponent(capi, $"{Lang.Get("Smithing")} {Lang.Get("with")}\n", CairoFont.WhiteSmallText(), cs => openDetailPageFor("craftinginfo-smithing")));
        var voxelCount = smallestSmithingRecipe.Voxels.Cast<bool>().Count(voxel => voxel); 
        var bitsCount = (int) Math.Ceiling(voxelCount / Core.Config.VoxelsPerBit);
        var baseMaterial = smallestSmithingRecipe.Ingredients
            .FirstOrDefault(ing => ing.Code.Path.Contains("ingot"))
            ?.ResolvedItemstack.GetBaseMaterial();
        if (baseMaterial == null) return;
        var allMaterialCollectibles = capi.World.Collectibles
            .Where(collectible =>
                collectible is IAnvilWorkable &&
                collectible is not ItemWorkItem &&
                collectible.CombustibleProps != null &&
                collectible.CombustibleProps.SmeltedStack?.Resolve(capi.World, "worldForResolving") != null &&
                collectible.Satisfies(collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack, baseMaterial))
            .OrderBy(collectible => collectible.Code.Domain == "game")
            .ToList();
        var allMaterialStacks = allMaterialCollectibles
            .Select(collectible => new ItemStack(collectible, collectible switch
            {
                ItemMetalPlate => (int) Math.Ceiling(voxelCount / 81.0),
                ItemXWorkableNugget => (int) Math.Ceiling(voxelCount / 2.0),
                ItemWorkableNugget => bitsCount,
                _ => (int) Math.Ceiling(voxelCount * collectible.CombustibleProps.SmeltedRatio / 42.0 )
            }))
            .ToList();
        
        var materialStackComponent = new SlideshowItemstackTextComponent(capi, allMaterialStacks.ToArray(), 40.0, EnumFloat.Inline, cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
        if (allMaterialStacks.Count > 0)
        {
            components.Insert(smithingSectionIndex, new ClearFloatTextComponent(capi, 2f));
            foreach (var itemStack in allMaterialStacks)
            {
                ItemstackTextComponent itemstackTextComponent = new ItemstackTextComponent(capi, itemStack, 40, 10.0, EnumFloat.Inline, cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                itemstackTextComponent.ShowStacksize = true;
                components.Insert(smithingSectionIndex, itemstackTextComponent);
            }
            components.Insert(smithingSectionIndex, new ClearFloatTextComponent(capi, 3f));
        }
        materialStackComponent.ShowStackSize = true;
    }
}