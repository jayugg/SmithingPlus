using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using SmithingPlus.Compat;
using SmithingPlus.Metal;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace SmithingPlus.ToolRecovery;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[HarmonyPatch(typeof(CollectibleObject))]
[HarmonyPatchCategory(Core.ToolRecoveryCategory)]
public class ItemDamagedPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CollectibleObject.OnCreatedByCrafting))]
    [HarmonyPriority(-int.MaxValue)]
    public static void Postfix_OnCreatedByCrafting(
        ItemSlot[] allInputslots,
        ItemSlot outputSlot,
        GridRecipe byRecipe)
    {
        if (outputSlot.Itemstack == null) return;
        var brokenStack = allInputslots.FirstOrDefault(slot =>
            slot.Itemstack?.GetBrokenCount() > 0 &&
            slot.Itemstack?.Collectible.HasBehavior<CollectibleBehaviorRepairableToolHead>() == true
        )?.Itemstack;
        if (brokenStack == null) return;
        var brokenCount = brokenStack.GetBrokenCount();
        if (brokenCount <= 0) return;
        if (brokenStack.Item?.IsRepairableTool() is not true) return;
        var repairedStack = brokenStack.GetRepairedToolStack();
        if (repairedStack == null) return;
        repairedStack.ResolveBlockOrItem((allInputslots.FirstOrDefault()?.Inventory?.Api ?? Core.Api)
            .World);
        if (repairedStack.Collectible.Code != byRecipe.Output.ResolvedItemstack.Collectible.Code) return;
        foreach (var attributeKey in Core.Config.GetToolRepairForgettableAttributes)
            repairedStack.Attributes?.RemoveAttribute(attributeKey);
        var repairSmith = brokenStack.GetRepairSmith();
        if (repairSmith != null) repairedStack.SetRepairSmith(repairSmith);
        var smithingQuality = brokenStack.Attributes.GetFloat(ModAttributes.SmithingQuality);
        if (smithingQuality != 0) repairedStack.Attributes?.SetFloat(ModAttributes.SmithingQuality, smithingQuality);
        var toolRepairPenaltyModifier = brokenStack.Attributes.GetFloat(ModAttributes.ToolRepairPenaltyModifier);
        if (toolRepairPenaltyModifier != 0)
            repairedStack.Attributes?.SetFloat(ModAttributes.ToolRepairPenaltyModifier, toolRepairPenaltyModifier);
        var repairedAttributes = repairedStack.Attributes ?? new TreeAttribute();
        var outputAttributes = outputSlot.Itemstack.Attributes;
        foreach (var attribute in repairedAttributes)
            outputAttributes[attribute.Key] = attribute.Value;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CollectibleObject.DamageItem))]
    private static void Prefix_DamageItem(
        IWorldAccessor world,
        Entity byEntity,
        ItemSlot itemslot,
        int amount)
    {
        if (world.Api.Side.IsClient())
            return;
        var durability = itemslot?.Itemstack?.GetRemainingDurability();
        if (!durability.HasValue || durability > amount) return;
        if (itemslot.Itemstack?.Collectible.HasBehavior<CollectibleBehaviorRepairableTool>() != true) return;
        Core.Logger.VerboseDebug("Broken tool in InventoryID: {0}, Entity: {1}", itemslot.Inventory?.InventoryID,
            byEntity.GetName());
        var entityPlayer = byEntity as EntityPlayer;
        var itemStack = itemslot.Itemstack;
        var toolCode = itemStack?.Collectible.Code.ToString();
        var smithingRecipe = CacheHelper.GetOrAdd(Core.ToolToRecipeCache, toolCode,
            () => GetHeadSmithingRecipe(world.Api, itemStack));
        if (smithingRecipe == null)
        {
            Core.Logger.VerboseDebug("Head or tool smithing recipe not found for: {0}", toolCode);
            return;
        }

        var metalMaterial = itemStack?.GetOrCacheMetalMaterial(byEntity.Api);
        var workItem = metalMaterial?.WorkItem;
        if (workItem is null)
        {
            Core.Logger.VerboseDebug(
                $"Work item not found. Metal material: {metalMaterial?.IngotCode}, " +
                $"collectible: {itemStack?.Collectible.Code}");
            return;
        }

        Core.Logger.VerboseDebug("Found work item: {0}", workItem.Code);
        var wItemStack = new ItemStack(workItem);
        Core.Logger.VerboseDebug("Found smithing recipe: {0}",
            smithingRecipe.Output.ResolvedItemstack.Collectible.Code);
        var byteVoxels = ByteVoxelsFromRecipe(smithingRecipe, smithingRecipe.Output.ResolvedItemstack.StackSize);
        wItemStack.Attributes.SetBytes("voxels", BlockEntityAnvil.serializeVoxels(byteVoxels));
        wItemStack.Attributes.SetInt("selectedRecipeId", smithingRecipe.RecipeId);
        var cloneStack = itemStack?.Clone();
        cloneStack.CloneBrokenCount(itemStack, 1);
        wItemStack.SetRepairedToolStack(cloneStack);
        if (ThriftySmithingCompat.ThriftySmithingLoaded)
        {
            var voxelCount = CacheHelper.GetOrAdd(Core.RecipeVoxelCountCache, smithingRecipe.RecipeId,
                () =>
                {
                    Core.Logger.VerboseDebug("Calculating voxel count for: {0}", smithingRecipe.RecipeId);
                    return smithingRecipe.Voxels.VoxelCount();
                });
            wItemStack.AddToCustomWorkData(voxelCount);
        }

        var gaveStack = false;
        if (entityPlayer != null) gaveStack = entityPlayer.TryGiveItemStack(wItemStack);
        if (!gaveStack) world.SpawnItemEntity(wItemStack, byEntity.Pos.XYZ);
        Core.Logger.VerboseDebug(gaveStack ? "Gave work item {0} to player {1}" : "Dropped work item {0} to player {1}",
            wItemStack.Collectible.Code, entityPlayer?.Player.PlayerName);
        itemslot.MarkDirty();
    }

    private static SmithingRecipe GetHeadSmithingRecipe(ICoreAPI api, ItemStack itemStack)
    {
        var toolHead = GetToolHead(api, itemStack);
        var smithingRecipe = toolHead.GetSmithingRecipe(api);
        return smithingRecipe;
    }

    private static ItemStack GetToolHead(ICoreAPI api, ItemStack itemStack)
    {
        var toolRecipe = itemStack.Collectible
            .GetGridRecipes(api)
            .FirstOrDefault(r =>
                r.Output.ResolvedItemstack.StackSize == 1);
        var toolHead = toolRecipe?.resolvedIngredients
            .FirstOrDefault(k =>
                k?.ResolvedItemstack?.Collectible?.HasBehavior<CollectibleBehaviorRepairableToolHead>() ?? false)
            ?.ResolvedItemstack;
        if (toolHead == null)
        {
            toolHead = itemStack;
            Core.Logger.VerboseDebug("Tool head not found for: {0}", itemStack);
        }

        Core.Logger.VerboseDebug("Tool head: {0}", toolHead);
        return toolHead;
    }

    private static byte[,,] ByteVoxelsFromRecipe(SmithingRecipe recipe, int stackSize = 1)
    {
        var recipeVoxels = recipe.Voxels;
        var totalVoxels = recipeVoxels.VoxelCount();
        var targetVoxelCount = (int)(totalVoxels * Core.Config.BrokenToolVoxelPercent);
        var currentVoxelCount = totalVoxels;

        var byteVoxels = recipeVoxels.ToByteArray();
        var layer = 0;
        while (currentVoxelCount > targetVoxelCount)
        {
            byteVoxels.ErodeLayer(layer, ref currentVoxelCount, targetVoxelCount);
            layer++;
        }

        return byteVoxels;
    }
}