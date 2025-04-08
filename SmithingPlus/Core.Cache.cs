using System.Collections.Generic;
using JetBrains.Annotations;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus;
#nullable enable

[UsedImplicitly]
public partial class Core
{
    private const string RecipeOutputNameCacheKey = $"{ModId}:recipeOutputName";
    private const string ToolToRecipeCacheKey = $"{ModId}:toolToRecipe";
    private const string RecipeVoxelCountCacheKey = $"{ModId}:recipeVoxelCount";
    private const string MaxFuelBurnTempCacheKey = $"{ModId}:maxFuelBurnTemperature";
    private const string MoldStacksCacheKey = $"{ModId}:moldStacks";
    private const string MetalBitStacksCacheKey = $"{ModId}:metalbitStacks";
    private const string CastableMetalVariantsCacheKey = $"{ModId}:castableMetalVariants";
    private const string SmallestSmithingRecipeCacheKey = $"{ModId}:smallestSmithingRecipe";

    public static Dictionary<int, string> RecipeOutputNameCache =>
        ObjectCacheUtil.GetOrCreate(Api, RecipeOutputNameCacheKey, () => new Dictionary<int, string>());

    public static Dictionary<string, SmithingRecipe> ToolToRecipeCache =>
        ObjectCacheUtil.GetOrCreate(Api, ToolToRecipeCacheKey, () => new Dictionary<string, SmithingRecipe>());

    public static Dictionary<int, int> RecipeVoxelCountCache =>
        ObjectCacheUtil.GetOrCreate(Api, RecipeVoxelCountCacheKey, () => new Dictionary<int, int>());

    public static float? MaxFuelBurnTemp
    {
        get => Api.ObjectCache.TryGetValue(MaxFuelBurnTempCacheKey, out var temperature) ? (float?)temperature : null;
        set => Api.ObjectCache[MaxFuelBurnTempCacheKey] = value;
    }

    public static Dictionary<string, ItemStack[]> MoldStacksCache =>
        ObjectCacheUtil.GetOrCreate(Api, MoldStacksCacheKey, () => new Dictionary<string, ItemStack[]>());

    public static ItemStack[]? MetalBitStacksCache
    {
        get => Api.ObjectCache.TryGetValue(MetalBitStacksCacheKey, out var stacks) ? (ItemStack[])stacks : null;
        set => Api.ObjectCache[MetalBitStacksCacheKey] = value;
    }

    public static string[]? CastableMetalVariantsCache
    {
        get => Api.ObjectCache.TryGetValue(CastableMetalVariantsCacheKey, out var variants) ? (string[])variants : null;
        set => Api.ObjectCache[CastableMetalVariantsCacheKey] = value;
    }

    public static Dictionary<string, SmithingRecipe> SmallestSmithingRecipeCache =>
        ObjectCacheUtil.GetOrCreate(Api, SmallestSmithingRecipeCacheKey,
            () => new Dictionary<string, SmithingRecipe>());
}