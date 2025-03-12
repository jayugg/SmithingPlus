namespace SmithingPlus;

public partial class Core
{
    internal const string ToolRecoveryCategory = "toolRecovery";
    internal const string SmithingBitsCategory = "smithingBits";
    internal const string StoneSmithingCategory = "stoneSmithing";
    internal const string BitsRecoveryCategory = "bitsRecovery";
    internal const string CastingTweaksCategory = "castingTweaks";

    public static class ClientTweaksCategories
    {
        public const string AnvilShowRecipeVoxels = "anvilShowRecipeVoxels";
        public const string RememberHammerToolMode = "rememberHammerToolMode";
        public const string ShowWorkablePatches = "showWorkablePatches";
        public const string HandbookExtraInfo = "handbookExtraInfo";
    }

    internal const string ThriftySmithingCompatCategory = "thriftySmithingCompat";
    public static readonly string RecipeOutputNameCacheKey = "smithingplus:recipeOutputName";
    public static readonly string ToolToRecipeCacheKey = "smithingplus:toolToRecipe";
    public static readonly string RecipeVoxelCountCacheKey = "smithingplus:recipeVoxelCount";
}