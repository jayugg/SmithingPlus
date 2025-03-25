using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace SmithingPlus.ClientTweaks;

[HarmonyPatchCategory(Core.ClientTweaksCategories.ShowWorkablePatches)]
public class ShowWorkablePatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockEntityAnvil), nameof(BlockEntityAnvil.GetBlockInfo))]
    [HarmonyPriority(Priority.VeryLow)]
    public static void Postfix_BlockEntityAnvil_GetBlockInfo(BlockEntityAnvil __instance, IPlayer forPlayer,
        StringBuilder dsc)
    {
        if (__instance.WorkItemStack == null || __instance.SelectedRecipe == null) return;
        if (!__instance.CanWorkCurrent) return;
        var temperature =
            (int)__instance.WorkItemStack.Collectible.GetTemperature(__instance.Api.World, __instance.WorkItemStack);
        var localizedString = Lang.Get("Temperature: {0}°C", temperature);
        const string pattern = @"(\d+°C)";
        var replacement = Regex.Replace(localizedString, pattern,
            $"<font color=\"{Constants.AnvilWorkableColor}\">$1</font>");
        dsc.Replace(localizedString, replacement);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockEntityForge), nameof(BlockEntityForge.GetBlockInfo))]
    [HarmonyPriority(Priority.VeryLow)]
    public static void Postfix_BlockEntityForge_GetBlockInfo(BlockEntityForge __instance, IPlayer forPlayer,
        StringBuilder dsc)
    {
        if (__instance.Contents == null) return;
        var temperature =
            (int)__instance.Contents.Collectible.GetTemperature(__instance.Api.World, __instance.Contents);
        var workableTemp = __instance.Contents.GetWorkableTemperature();
        if (!(temperature > workableTemp)) return;
        var localizedString = Lang.Get("forge-contentsandtemp", __instance.Contents.StackSize,
            __instance.Contents.GetName(), temperature);
        const string pattern = @"(\d+(.*)°C)";
        var replacement = Regex.Replace(localizedString, pattern,
            $"<font color=\"{Constants.AnvilWorkableColor}\">$1</font>");
        dsc.Replace(localizedString, replacement);
    }
}