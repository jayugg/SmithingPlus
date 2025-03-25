namespace SmithingPlus;

public partial class Core
{
    internal const string ToolRecoveryCategory = "toolRecovery";
    internal const string SmithingBitsCategory = "smithingBits";
    internal const string StoneSmithingCategory = "stoneSmithing";
    internal const string BitsRecoveryCategory = "bitsRecovery";
    internal const string CastingTweaksCategory = "castingTweaks";

    internal const string ThriftySmithingCompatCategory = "thriftySmithingCompat";

    public static class ClientTweaksCategories
    {
        public const string AnvilShowRecipeVoxels = "anvilShowRecipeVoxels";
        public const string RememberHammerToolMode = "rememberHammerToolMode";
        public const string ShowWorkablePatches = "showWorkablePatches";
        public const string HandbookExtraInfo = "handbookExtraInfo";
    }
}

public static class PatchExtensions
{
    public static void PatchIfEnabled(this string patchCategory, bool configFlag)
    {
        if (!configFlag) return;
        Core.HarmonyInstance.PatchCategory(patchCategory);
        Core.Logger.VerboseDebug("Patched {0}...", patchCategory);
    }
}