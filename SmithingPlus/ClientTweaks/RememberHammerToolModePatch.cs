using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.ClientTweaks;

[HarmonyPatch(typeof(ItemHammer))]
[HarmonyPatchCategory(Core.ClientTweaksCategories.RememberHammerToolMode)]
public class RememberHammerToolModePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ItemHammer.SetToolMode))]
    public static void Postfix_SetToolMode(
        ItemSlot slot,
        IPlayer byPlayer,
        BlockSelection blockSel,
        int toolMode)
    {
        byPlayer.Entity?.Attributes?.SetInt("hammerToolMode", toolMode);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ItemHammer.GetToolMode))]
    public static void Postfix_GetToolMode(
        ref int __result,
        ItemSlot slot,
        IPlayer byPlayer,
        BlockSelection blockSel)
    {
        var playerToolMode = byPlayer.Entity?.Attributes?.GetInt("hammerToolMode");
        if (playerToolMode.HasValue)
        {
            __result = playerToolMode.Value;
        }
    }
}