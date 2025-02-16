using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SmithingPlus.Compat;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus.ToolRecovery;

[HarmonyPatch(typeof (CollectibleObject))]
[HarmonyPatchCategory(Core.ToolRecoveryCategory)]
public class ItemDamagedPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CollectibleObject.OnCreatedByCrafting)), HarmonyPriority(Priority.Last)]
    public static void Postfix_OnCreatedByCrafting(
        ItemSlot[] allInputslots,
        ItemSlot outputSlot,
        GridRecipe byRecipe)
    {
        if (outputSlot.Itemstack == null) return;
        var brokenStack = allInputslots.FirstOrDefault(slot => slot.Itemstack?.GetBrokenCount() > 0)?.Itemstack;
        if (brokenStack == null) return;
        var brokenCount = brokenStack.GetBrokenCount();
        if (!(brokenCount > 0)) return;
        var repairedStack = brokenStack.GetRepairedToolStack();
        if (repairedStack == null) return;
        repairedStack.Attributes?.RemoveAttribute("durability");
        repairedStack.Attributes?.RemoveAttribute("maxRepair"); // From necessaries mod grindstone
        var repairedAttributes = repairedStack.Attributes ?? new TreeAttribute();
        var outputAttributes = outputSlot.Itemstack.Attributes;
        foreach (var attribute in repairedAttributes)
        {
            outputAttributes[attribute.Key] = attribute.Value;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("DamageItem")]
    private static void Prefix_DamageItem(
      IWorldAccessor world,
      Entity byEntity,
      ItemSlot itemslot,
      int amount,
      CollectibleObject __instance)
    {
        if (world.Api.Side.IsClient())
          return;
        Core.Logger.VerboseDebug("Prefix_DamageItem: {0} by {1}", __instance.Code, amount);
        Core.Logger.VerboseDebug("InventoryID: {0}, Class: {1}", itemslot?.Inventory?.InventoryID, itemslot?.Inventory?.ClassName);
        var durability = itemslot?.Itemstack?.GetDurability();
        Core.Logger.VerboseDebug("Durability: {0}", durability);
        if (!durability.HasValue || durability > amount) return;
        if (itemslot?.Itemstack.Collectible.IsRepairableTool() != true) return;
        var entityPlayer = byEntity as EntityPlayer;
        var itemStack = itemslot?.Itemstack;
        var toolCode = itemStack?.Collectible.Code.ToString();
        var smithingRecipe = CacheHelper.GetOrAdd(Core.ToolToRecipeCache, toolCode, () => GetHeadSmithingRecipe(world, itemStack));

        if (smithingRecipe == null)
        {
            Core.Logger.VerboseDebug("Head or tool smithing recipe not found for: {0}", toolCode);
            return;
        }

        var metal = smithingRecipe.Output.ResolvedItemstack.Collectible.GetMetalOrMaterial();
        var workItemCode = new AssetLocation(itemStack?.Collectible.Code.Domain,$"workitem-{metal}");
        var workItem = world.GetItem(workItemCode);
        workItem ??= world.GetItem(new AssetLocation("game:workitem-" + metal));
        if (workItem is not { })
        {
            Core.Logger.VerboseDebug("Work item not found: {0}, {1}", workItemCode, "game:workitem-" + metal);
            return;
        }
        Core.Logger.VerboseDebug("Found work item: {0}", workItemCode);
        var wItemStack = new ItemStack(workItem);
        Core.Logger.VerboseDebug("Found smithing recipe: {0}", smithingRecipe.Output.ResolvedItemstack.Collectible.Code);
        var byteVoxels = ByteVoxelsFromRecipe(smithingRecipe, world, smithingRecipe.Output.ResolvedItemstack.StackSize);
        wItemStack.Attributes.SetBytes("voxels", BlockEntityAnvil.serializeVoxels(byteVoxels));
        wItemStack.Attributes.SetInt("selectedRecipeId", smithingRecipe.RecipeId);
        var cloneStack = itemStack?.Clone();
        cloneStack.CloneBrokenCount(itemStack, 1);
        cloneStack.SetRepairSmith(entityPlayer?.Player.PlayerName);
        wItemStack.SetRepairedToolStack(cloneStack);
        if (ThriftySmithingCompat.ThriftySmithingLoaded)
        {
            var voxelCount = CacheHelper.GetOrAdd(Core.RecipeVoxelCountCache, smithingRecipe.RecipeId,
                () => {
                    Core.Logger.VerboseDebug("Calculating voxel count for: {0}", smithingRecipe.RecipeId);
                    return smithingRecipe.Voxels.Cast<bool>().Count(voxel => voxel);
                });
            wItemStack.AddToCustomWorkData(voxelCount);
        }

        var gaveStack = false;
        if (entityPlayer != null) gaveStack = entityPlayer.TryGiveItemStack(wItemStack);
        if (!gaveStack) world.SpawnItemEntity(wItemStack, byEntity.Pos.XYZ);
    }

    private static SmithingRecipe GetHeadSmithingRecipe(IWorldAccessor world, ItemStack itemStack)
    {
        var toolHead = GetToolHead(world, itemStack);
        var smithingRecipe = toolHead.GetSmithingRecipe(world);
        return smithingRecipe;
    }

    private static ItemStack GetToolHead(IWorldAccessor world, ItemStack itemStack)
    {
        var toolRecipe = world.GridRecipes
            .FirstOrDefault(r =>
                r.Output.ResolvedItemstack.StackSize == 1 &&
                r.Output.ResolvedItemstack.Collectible.Code.Equals(itemStack?.Collectible.Code));
        var toolHead = toolRecipe?.resolvedIngredients.FirstOrDefault(k => k.ResolvedItemstack.Collectible.IsRepairableToolHead())?.ResolvedItemstack;
        if (toolHead == null)
        {
            toolHead = itemStack;
            Core.Logger.VerboseDebug("Tool head not found for: {0}", itemStack);
        }
        Core.Logger.VerboseDebug("Tool head: {0}", toolHead);
        return toolHead;
    }

    public static string GetMetalOrMaterial(ItemStack itemStack)
    {
        return itemStack.Collectible.Variant["metal"] ?? itemStack.Collectible.Variant["material"];
    }
    
    public static byte[,,] ByteVoxelsFromRecipe(SmithingRecipe recipe, IWorldAccessor world, int stackSize = 1)
    {
        var random = new Random(world.Rand.Next());
        var recipeVoxels = recipe.Voxels;
        float removableVoxelCount = 0;
        if (stackSize > 1) removableVoxelCount = CacheHelper.GetOrAdd(Core.RecipeVoxelCountCache, recipe.RecipeId,
            () => {
                Core.Logger.VerboseDebug("Calculating voxel count for: {0}", recipe.RecipeId);
                return recipeVoxels.Cast<bool>().Count(voxel => voxel);
            });
        removableVoxelCount /= stackSize;
        var byteVoxels = new byte[recipeVoxels.GetLength(0), recipeVoxels.GetLength(1), recipeVoxels.GetLength(2)];
        for (int x = 0; x < recipeVoxels.GetLength(0); x++)
        {
            for (int y = 0; y < recipeVoxels.GetLength(1); y++)
            {
                for (int z = 0; z < recipeVoxels.GetLength(2); z++)
                {
                    if (removableVoxelCount > 0)
                    {
                        removableVoxelCount--;
                        continue;
                    }
                    if (random.NextDouble() < Core.Config.BrokenToolVoxelChance)
                    {
                        byteVoxels[x, y, z] = recipeVoxels[x, y, z] ? (byte)1 : (byte)0;
                    }
                }
            }
        }
        return byteVoxels;
    }
}