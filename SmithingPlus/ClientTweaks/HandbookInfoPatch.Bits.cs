using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using SmithingPlus.SmithWithBits;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.ClientTweaks;
#nullable enable

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public partial class HandbookInfoPatch
{
    [HarmonyPostfix, HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), "addCreatedByInfo")]
    public static void PatchBitsInfo(
        CollectibleBehaviorHandbookTextAndExtraInfo __instance,
        ICoreClientAPI capi,
        ItemStack[] allStacks,
        ActionConsumable<string> openDetailPageFor,
        ItemStack stack,
        List<RichTextComponentBase> components)
    {
        if (stack.Collectible is not (ItemNugget or ItemWorkableNugget)) return;
        Core.MaxFuelBurnTemp ??= allStacks
            .Where(s => s.Collectible.CombustibleProps?.BurnTemperature > 0)
            .OrderByDescending(s => s.Collectible.CombustibleProps.BurnTemperature)
            .FirstOrDefault()?.Collectible.CombustibleProps?.BurnTemperature ?? 0;
        if (stack.Collectible.CombustibleProps?.MeltingPoint > Core.MaxFuelBurnTemp) return;
        var moldStacks = allStacks.Where(s =>
                s.Collectible is BlockToolMold &&
                GetStackForVariant(capi, s, stack.Collectible.LastCodePart()) != null)
            .OrderBy(s => s.Collectible.Code.Domain == "game" ? -100 : 0)
            .ThenBy(s => s.ItemAttributes["requiredUnits"].AsInt(100))
            .ToArray();
        var haveText = components.Count > 0;
        if (moldStacks.Length <= 0) return;
        AddHeading(components, capi, "Can be cast in", ref haveText);

        var groupedStacks = moldStacks
            .GroupBy(s =>
            {
                var code = s.Collectible.Code;
                return $"{code.Domain}:{string.Join("-", code.Path.Split('-').SkipLast(1))}";
            })
            .ToArray();

        foreach (var group in groupedStacks)
        {
            var stacksInGroup = group.ToArray();
            Array.ForEach(stacksInGroup, s =>
                s.StackSize = s.ItemAttributes["requiredUnits"].AsInt());
            var moldsSlideshow = new SlideshowItemstackTextComponent(capi, stacksInGroup, 40, EnumFloat.Inline,
                    cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)))
                { PaddingLeft = 2 };
            components.Add(moldsSlideshow);
        }
    }
}