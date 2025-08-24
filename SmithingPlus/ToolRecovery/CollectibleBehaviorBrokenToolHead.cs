using System;
using System.Linq;
using System.Text;
using HarmonyLib;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus.ToolRecovery;

[HarmonyPatch(typeof(ItemWorkItem))]
[HarmonyPatchCategory(Core.ToolRecoveryCategory)]
public class CollectibleBehaviorBrokenToolHead(CollectibleObject collObj) : CollectibleBehaviorRepairableTool(collObj)
{
    protected override string LangKey => "Broken";

    public static bool IsBrokenToolHead(ItemStack itemStack)
    {
        return itemStack.GetBrokenCount() > 0;
    }

    public override void GetHeldItemName(StringBuilder dsc, ItemStack itemStack)
    {
        if (!IsBrokenToolHead(itemStack)) return;
        var recipeId = itemStack.Attributes.GetInt("selectedRecipeId");
        var toolName = CacheHelper.GetOrAdd(
            Core.RecipeOutputNameCache,
            recipeId,
            () =>
            {
                Core.Logger.VerboseDebug("Storing recipe output name: {0}", recipeId);
                return Core.Api.GetSmithingRecipes().FirstOrDefault(r => r.RecipeId == recipeId)?.Output
                    .ResolvedItemstack.GetName();
            });
        dsc.Clear();
        dsc.AppendLine(toolName == null
            ? Lang.Get($"{Core.ModId}:Unknown broken tool part")
            : Lang.Get($"{Core.ModId}:Broken {{0}}", toolName.ToLower()));
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        if (!IsBrokenToolHead(inSlot.Itemstack)) return;
        var brokenCount = inSlot.Itemstack.GetBrokenCount();
        if (brokenCount <= 0) return;
        if (Core.Config.ShowBrokenCount) dsc.AppendLine(Lang.Get($"{LangKey} {{0}} times", brokenCount));
        if (Core.Config.DontRepairBrokenToolHeads) dsc.AppendLine(Lang.Get($"{Core.ModId}:itemdesc-needschiseling"));
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ItemWorkItem.GetHeldItemInfo))]
    public static void Postfix_GetHeldItemInfo(
        ItemSlot inSlot,
        StringBuilder dsc,
        IWorldAccessor world,
        bool withDebugInfo)
    {
        if (!IsBrokenToolHead(inSlot.Itemstack)) return;
        // Remove lines containing the respective language entries
        var unknownWorkItem = Lang.Get("Unknown work item");
        var unfinished = $"@(.*){Lang.Get("Unfinished {0}", "(.*)")}(.*)";

        var lines = dsc.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        dsc.Clear();
        foreach (var line in lines)
            if (!line.Contains(unknownWorkItem) && !WildcardUtil.Match(unfinished, line))
                dsc.AppendLine(line);
    }
}