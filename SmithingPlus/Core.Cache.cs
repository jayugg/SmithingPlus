using System.Collections.Generic;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus;

public partial class Core
{
    private const string RecipeOutputNameCacheKey = $"{ModId}:recipeOutputName";
    private const string ToolToRecipeCacheKey = $"{ModId}:toolToRecipe";
    private const string RecipeVoxelCountCacheKey = $"{ModId}:recipeVoxelCount";

    public static Dictionary<int, string> RecipeOutputNameCache =>
        ObjectCacheUtil.GetOrCreate(Api, RecipeOutputNameCacheKey, () => new Dictionary<int, string>());

    public static Dictionary<string, SmithingRecipe> ToolToRecipeCache =>
        ObjectCacheUtil.GetOrCreate(Api, ToolToRecipeCacheKey, () => new Dictionary<string, SmithingRecipe>());

    public static Dictionary<int, int> RecipeVoxelCountCache =>
        ObjectCacheUtil.GetOrCreate(Api, RecipeVoxelCountCacheKey, () => new Dictionary<int, int>());

    private static void ClearCache()
    {
        RecipeOutputNameCache?.Clear();
        ToolToRecipeCache?.Clear();
        RecipeVoxelCountCache?.Clear();
    }
}