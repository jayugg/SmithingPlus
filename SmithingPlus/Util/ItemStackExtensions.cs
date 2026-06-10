#nullable enable
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace SmithingPlus.Util;

public static class ItemStackExtensions
{
    internal static int? GetRemainingDurability(this ItemStack itemStack)
    {
        return itemStack.Collectible.GetRemainingDurability(itemStack);
    }

    internal static int? GetMaxDurability(this ItemStack itemStack)
    {
        return itemStack.Collectible.GetMaxDurability(itemStack);
    }

    internal static float? GetDurabilityPercentage(this ItemStack itemStack)
    {
        if (itemStack.GetMaxDurability() == 0)
            return null;
        return itemStack.GetRemainingDurability() / itemStack.GetMaxDurability();
    }

    internal static void SetDurability(this ItemStack itemStack, int number)
    {
        itemStack.Collectible.SetDurability(itemStack, number);
    }

    internal static void CloneBrokenCount(this ItemStack itemStack, ItemStack fromStack, int extraCount = 0)
    {
        var brokenCount = fromStack.GetBrokenCount();
        itemStack.Attributes.SetInt(ModStackAttributes.BrokenCount, brokenCount + extraCount);
    }

    internal static void SetRepairedToolStack(this ItemStack itemStack, ItemStack fromStack)
    {
        itemStack.Attributes.SetItemstack(ModStackAttributes.RepairedToolStack, fromStack);
    }

    // Note: On server item stack needs to be resolved!
    internal static ItemStack? GetRepairedToolStack(this ItemStack itemStack)
    {
        return itemStack.Attributes?.GetItemstack(ModStackAttributes.RepairedToolStack);
    }

    internal static string? GetRepairSmith(this ItemStack itemStack)
    {
        var repairedStack = itemStack.GetRepairedToolStack();
        return repairedStack?.GetRepairSmith() ?? itemStack.Attributes.GetString(ModStackAttributes.RepairSmith);
    }

    internal static void SetRepairSmith(this ItemStack itemStack, string smith)
    {
        itemStack.Attributes.SetString(ModStackAttributes.RepairSmith, smith);
    }

    internal static float GetSmithingQuality(this ItemStack itemStack)
    {
        return itemStack.Attributes?.GetFloat(ModStackAttributes.SmithingQuality, 1) ?? 1f;
    }

    internal static void SetSmithingQuality(this ItemStack itemStack, float quality)
    {
        itemStack.Attributes?.SetFloat(ModStackAttributes.SmithingQuality, quality);
    }

    internal static float GetToolRepairPenaltyModifier(this ItemStack itemStack)
    {
        return itemStack.Attributes?.GetFloat(ModStackAttributes.ToolRepairPenaltyModifier) ?? 0f;
    }

    internal static void SetToolRepairPenaltyModifier(this ItemStack itemStack, float modifier)
    {
        itemStack.Attributes?.SetFloat(ModStackAttributes.ToolRepairPenaltyModifier, modifier);
    }

    internal static void CloneRepairedToolStackOrAttributes(this ItemStack itemStack, ItemStack fromStack,
        string[]? forgettableAttributes = null)
    {
        var repairedStack = fromStack.GetRepairedToolStack();
        if (forgettableAttributes != null)
            foreach (var attributeKey in forgettableAttributes)
                repairedStack?.Attributes?.RemoveAttribute(attributeKey);
        if (repairedStack == null)
        {
            Core.Logger.VerboseDebug("No repaired tool stack found in {0}", fromStack.Collectible.Code);
            return;
        }

        if (itemStack.Satisfies(repairedStack))
        {
            var repairedAttributes = repairedStack.Attributes ?? new TreeAttribute();
            foreach (var attribute in repairedAttributes) itemStack.Attributes[attribute.Key] = attribute.Value;
            Core.Logger.VerboseDebug("Not a tool head. Cloned repaired tool stack attributes from {0} to {1}",
                fromStack.Collectible.Code, itemStack.Collectible.Code);
        }
        else
        {
            itemStack.SetRepairedToolStack(repairedStack);
        }
    }

    internal static int GetBrokenCount(this ItemStack itemStack)
    {
        var repairedStack = itemStack.GetRepairedToolStack();
        return repairedStack?.GetBrokenCount() ?? (itemStack.Attributes?.GetInt(ModStackAttributes.BrokenCount) ?? 0);
    }

    public static bool CodeMatches(this ItemStack stack, ItemStack that)
    {
        return stack.Collectible.Code.Equals(that.Collectible.Code);
    }

    public static float GetWorkableTemperature(this ItemStack stack)
    {
        var meltingPoint = stack.Collectible.CombustibleProps?.MeltingPoint ?? 0.0f;
        var defaultTemperature = meltingPoint / 2f;
        return stack.ItemAttributes?["workableTemperature"]?.AsFloat(defaultTemperature) ?? defaultTemperature;
    }

    public static SmithingRecipe? GetSmithingRecipe(this ItemStack toolHead, ICoreAPI api)
    {
        var smithingRecipe = api.ModLoader
            .GetModSystem<RecipeRegistrySystem>()?
            .SmithingRecipes?
            .FirstOrDefault(r => r?.Output?.ResolvedItemstack?.Satisfies(toolHead) == true);
        return smithingRecipe;
    }

    public static SmithingRecipe? GetSmithingRecipe(this ItemStack toolHead, ICoreAPI api, int withOutputStackSize)
    {
        var smithingRecipe = api.ModLoader
            .GetModSystem<RecipeRegistrySystem>()?
            .SmithingRecipes?
            .FirstOrDefault(r =>
                r?.Output?.ResolvedItemstack?.Satisfies(toolHead) == true
                && r.Output.ResolvedItemstack.StackSize == withOutputStackSize);
        return smithingRecipe;
    }

    // Gets the smithing recipe with the largest output stack that satisfies the tool head
    public static SmithingRecipe? GetLargestSmithingRecipe(this ItemStack toolHead, ICoreAPI api)
    {
        var smithingRecipe = api.ModLoader
                .GetModSystem<RecipeRegistrySystem>()?
                .SmithingRecipes?
                .Where(r => r?.Output?.ResolvedItemstack?.Satisfies(toolHead) == true)
                .OrderByDescending(r => r.Output.ResolvedItemstack.StackSize)
                .FirstOrDefault()
            ;
        return smithingRecipe;
    }

    // Gets the smithing recipe with the least expensive output that satisfies the tool head
    public static SmithingRecipe? GetCheapestSmithingRecipe(this ItemStack toolHead, ICoreAPI api)
    {
        var smithingRecipe = api.ModLoader
                .GetModSystem<RecipeRegistrySystem>()?
                .SmithingRecipes?
                .Where(r => r?.Output?.ResolvedItemstack?.Satisfies(toolHead) == true)
                .OrderByDescending(r => r.Voxels.VoxelCount() / r.Output.ResolvedItemstack.StackSize)
                .FirstOrDefault()
            ;
        return smithingRecipe;
    }

    public static IEnumerable<GridRecipe> GetGridRecipes(this ItemStack itemStack, ICoreAPI api)
    {
        var gridRecipes =
            from recipe in api.World.GridRecipes
            where recipe.Output?.ResolvedItemStack?.Satisfies(itemStack) == true
            select recipe;
        return gridRecipes;
    }

    // Gets a smithing recipe only if the output item stack has a single item
    public static SmithingRecipe? GetSingleSmithingRecipe(this ItemStack toolHead, ICoreAPI api)
    {
        return toolHead.GetSmithingRecipe(api, 1);
    }

    public static float GetSplitCount(this ItemStack stack)
    {
        var splitCount = stack.TempAttributes.GetFloat(ModStackAttributes.SplitCount);
        return splitCount;
    }

    public static void SetSplitCount(this ItemStack stack, float count)
    {
        stack.TempAttributes.SetFloat(ModStackAttributes.SplitCount, count);
    }

    public static float GetTemperature(this ItemStack stack, IWorldAccessor world)
    {
        return stack.Collectible.GetTemperature(world, stack);
    }

    public static void SetTemperatureFrom(this ItemStack stack, IWorldAccessor world, ItemStack fromStack)
    {
        var temperature = fromStack.GetTemperature(world);
        stack.Collectible.SetTemperature(world, stack, temperature);
    }

    public static void SetTemperature(this ItemStack stack, IWorldAccessor world, float count)
    {
        stack.Collectible.SetTemperature(world, stack, count);
    }

    public static bool IsSmeltedContainer(this ItemStack stack)
    {
        return stack.Collectible is BlockSmeltedContainer;
    }

    public static bool IsCastTool(this ItemStack stack)
    {
        return stack.Attributes.GetBool(ModStackAttributes.CastTool);
    }
}