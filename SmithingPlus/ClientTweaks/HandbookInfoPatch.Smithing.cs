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
        if (smallestSmithingRecipe == null) return;
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
                !collectible.Equals(stack.Collectible) &&
                collectible.Satisfies(collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack, baseMaterial))
            .OrderBy(collectible => collectible.Code.Domain == "game" ? -100 : 0)
            .ThenByDescending(collectible => collectible.CombustibleProps?.SmeltedRatio ?? 1)
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
            foreach (var itemStack in allMaterialStacks)
            {
                ItemstackTextComponent itemstackTextComponent = new ItemstackTextComponent(capi, itemStack, 40, 2.0, EnumFloat.Inline,
                    cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                itemstackTextComponent.ShowStacksize = true;
                components.Add(itemstackTextComponent);
            }
        }
        else
        {
            components.RemoveAt(smithingSectionIndex-1);
            components.Insert(smithingSectionIndex-1, new LinkTextComponent(capi, $"{Lang.Get("Smithing")} {Lang.Get("with")}\n", CairoFont.WhiteSmallText(), cs => openDetailPageFor("craftinginfo-smithing")));
            components.Insert(smithingSectionIndex, new ClearFloatTextComponent(capi, 2f));
            foreach (var itemStack in allMaterialStacks)
            {
                smithingSectionIndex++;
                ItemstackTextComponent itemstackTextComponent = new ItemstackTextComponent(capi, itemStack, 40, 2.0, EnumFloat.Inline,
                    cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                itemstackTextComponent.ShowStacksize = true;
                components.Insert(smithingSectionIndex, itemstackTextComponent);
            }
        }
    }
}