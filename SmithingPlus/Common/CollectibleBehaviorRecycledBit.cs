using System;
using System.Linq;
using SmithingPlus.Common.Metal;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.Common;

public class CollectibleBehaviorRecycledBit(CollectibleObject collObj) : CollectibleBehavior(collObj)
{
    private ICoreAPI Api => collObj.GetField<ICoreAPI>("api");

    public override void OnCreatedByCrafting(
        ItemSlot[] allInputSlots,
        ItemSlot outputSlot,
        IRecipeBase byRecipe,
        ref EnumHandling bhHandling)
    {
        base.OnCreatedByCrafting(allInputSlots, outputSlot, byRecipe, ref bhHandling);
        if (outputSlot?.Itemstack == null ||
            allInputSlots == null)
            return;

        // Identify recipe tools from ingredients
        var toolIngredients = byRecipe.RecipeIngredients
            .Where(ing =>
                ing.ConsumeProperties is { Consume: false, DurabilityCost: > 0 } ||
                ing?.RecipeAttributes?[ModRecipeAttributes.RecyclingRecipe]?.AsBool() == true)
            .ToArray() ?? [];

        var metalInputSlots = allInputSlots
            .Where(s => s?.Itemstack != null)
            .Where(s => !IsToolStack(s.Itemstack, toolIngredients))
            .Where(s => s.Itemstack?.GetOrCacheMetalMaterial(Api)?.IngotStack != null)
            .ToList();

        if (metalInputSlots.Count == 0) return;
        var totalVoxels = 0L;
        // To calculate weighted temperature average by voxels
        var temperatureAccumulator = 0f;

        foreach (var slot in metalInputSlots)
        {
            var stack = slot.Itemstack;
            if (stack == null) continue;

            var voxelsForThisStack = 0;

            // Work item with serialized voxel field
            if (stack.Collectible is ItemWorkItem)
            {
                var bytes = stack.Attributes.GetBytes("voxels");
                var voxels = BlockEntityAnvil.deserializeVoxels(bytes);
                voxelsForThisStack = voxels.MaterialCount();
            }
            // Finished smithed item -> get via cheapest smithing recipe to prevent abuse of the mechanic
            else
            {
                var cheapestRecipe = stack.GetCheapestSmithingRecipe(Api);
                if (cheapestRecipe is { Output.ResolvedItemStack: not null })
                {
                    var cheapestOutput = Math.Max(cheapestRecipe.Output.ResolvedItemStack.StackSize, 1);
                    var recipeMaterialVoxels = cheapestRecipe.Voxels.VoxelCount();
                    var voxelsPerItem = Math.Max(recipeMaterialVoxels / cheapestOutput, 0);

                    var consumedStackSize =
                        0; // Use this NOT stack.StackSize because that could have more items than the recipe requires
                    foreach (var ingredient in byRecipe.RecipeIngredients)
                    {
                        if (!ingredient.SatisfiesAsIngredient(stack) || ingredient.ResolvedItemStack == null)
                            continue;
                        consumedStackSize = ingredient.ResolvedItemStack.StackSize;
                        break;
                    }

                    voxelsForThisStack = voxelsPerItem * consumedStackSize;
                }
            }

            var temp = stack.Collectible.GetTemperature(Api.World, stack);
            if (voxelsForThisStack > 0)
                temperatureAccumulator += temp * voxelsForThisStack;
            totalVoxels += Math.Max(voxelsForThisStack, 0);
        }

        if (totalVoxels <= 0) return;
        var temperature = temperatureAccumulator / totalVoxels;

        // Scale output stack size by VoxelsPerBit
        var bits = Math.Max((int)(totalVoxels / Core.Config.VoxelsPerBit), 1);
        outputSlot.Itemstack.StackSize = bits;
        outputSlot.Itemstack.Collectible.SetTemperature(Api.World, outputSlot.Itemstack, temperature);
    }

    private static bool IsToolStack(ItemStack stack, IRecipeIngredient[] toolIngredients)
    {
        return stack != null && toolIngredients.Any(ing => ing?.SatisfiesAsIngredient(stack) == true);
    }
}