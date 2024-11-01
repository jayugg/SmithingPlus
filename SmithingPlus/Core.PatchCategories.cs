namespace SmithingPlus;

public partial class Core
{
    internal const string ToolRecoveryCategory = "toolRecovery";
    internal const string SmithingBitsCategory = "smithingBits";
    internal const string StoneSmithingCategory = "stoneSmithing";

    public static class ClientTweaksCategories
    {
        public const string AnvilShowRecipeVoxels = "anvilShowRecipeVoxels";
        public const string RememberHammerToolMode = "rememberHammerToolMode";
    }

    internal const string ThriftySmithingCompatCategory = "thriftySmithingCompat";
    public static readonly string RecipeOutputNameCacheKey = "smithingplus:recipeOutputName";
    public static readonly string ToolToRecipeCacheKey = "smithingplus:toolToRecipe";
    public static readonly string RecipeVoxelCountCacheKey = "smithingplus:recipeVoxelCount";
}