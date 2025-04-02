using System.Linq;
using HarmonyLib;
using SmithingPlus.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus.HammerTweaks;

[HarmonyPatchCategory(Core.HammerTweaksCategory)]
[HarmonyPatch(typeof(ItemHammer))]
public class ItemHammerPatch
{
    [HarmonyPatch(nameof(ItemHammer.OnLoaded))]
    [HarmonyPostfix]
    public static void OnLoaded_Postfix(ItemHammer __instance, ICoreAPI api)
    {
        if (api is not ICoreClientAPI capi)
            return;
        var toolModes = __instance.GetField<SkillItem[]>("toolModes");
        var newModes = ObjectCacheUtil.GetOrCreate(api, "extraHammerToolModes", () => new[]
        {
            new SkillItem
            {
                Code = new AssetLocation("flip"),
                Name = Lang.Get("Flip")
            }.WithIcon(capi, capi.Gui.Icons.Drawrepeat_svg)
        });
        var allModes = toolModes.Concat(newModes).ToArray();
        __instance.SetField("toolModes", allModes);
    }
}