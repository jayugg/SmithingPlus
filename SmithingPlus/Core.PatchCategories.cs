namespace SmithingPlus;

public partial class Core
{
    internal const string AlwaysPatchCategory = "always";
    internal const string NeverPatchCategory = "never";
    internal const string ToolRecoveryCategory = "toolRecovery";
    internal const string SmithingBitsCategory = "smithingBits";
    internal const string StoneSmithingCategory = "stoneSmithing";
    internal const string BitsRecoveryCategory = "bitsRecovery";
    internal const string HelveHammerBitsRecoveryCategory = $"{BitsRecoveryCategory}.helveHammer";
    internal const string CastingTweaksCategory = "castingTweaks";
    internal const string HammerTweaksCategory = "hammerTweaks";

    internal static class ClientTweaksCategories
    {
        public const string AnvilShowRecipeVoxels = "anvilShowRecipeVoxels";
        public const string RememberHammerToolMode = "rememberHammerToolMode";
        public const string ShowWorkablePatches = "showWorkablePatches";
        public const string HandbookExtraInfo = "handbookExtraInfo";
    }
}

public static class PatchExtensions
{
    /// <summary>
    ///     Patches the category if the boolean flag is enabled.
    /// </summary>
    /// <param name="patchCategory">String HarmonyPatchCategory to patch.</param>
    /// <param name="configFlag">Boolean flag to determine if the patch should be applied.</param>
    /// <param name="withDebugLogs">Flag to determine if debug logs should be printed.</param>
    public static void PatchIfEnabled(this string patchCategory, bool configFlag, bool withDebugLogs = true)
    {
        if (!configFlag) return;
        Core.HarmonyInstance.PatchCategory(patchCategory);
        if (withDebugLogs) Core.Logger.VerboseDebug("Patched {0}...", patchCategory);
    }
}