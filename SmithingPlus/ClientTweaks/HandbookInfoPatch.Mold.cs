using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SmithingPlus.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus.ClientTweaks;
#nullable enable

public partial class HandbookInfoPatch
{

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), "addCreatedByInfo")]
    public static void PatchFromMoldInfo(
        CollectibleBehaviorHandbookTextAndExtraInfo __instance,
        ICoreClientAPI capi,
        ItemStack[] allStacks,
        ActionConsumable<string> openDetailPageFor,
        ItemStack stack,
        List<RichTextComponentBase> components)
    {
        var moldStacks = CacheHelper.GetOrAdd(
            Core.MoldStacksCache,
            stack.Collectible.Code.ToString(),
            () => allStacks.Where(s =>
                    s.Collectible is BlockToolMold &&
                    stack.Collectible.FirstCodePart().Equals(ToolMoldType(s.Collectible)))
                .OrderBy(s => s.Collectible.Code.Domain == "game" ? -100 : 0)
                .ThenBy(s => s.ItemAttributes["requiredUnits"].AsInt())
                .ToArray()
        );
        if (moldStacks.Length <= 0) return;
        AddSubHeading(components, capi, openDetailPageFor,
            $"{Lang.Get("Metal molding")} {Lang.Get("requires")}",
            "craftinginfo-smelting");
        var slideshowMolds = new SlideshowItemstackTextComponent(capi, moldStacks.ToArray(), 40, EnumFloat.Inline,
                cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)))
            { PaddingLeft = 2 };
        components.Add(slideshowMolds);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), "addCreatedByInfo")]
    public static void PatchMoldInfo(
        CollectibleBehaviorHandbookTextAndExtraInfo __instance,
        ICoreClientAPI capi,
        ItemStack[] allStacks,
        ActionConsumable<string> openDetailPageFor,
        ItemStack stack,
        List<RichTextComponentBase> components)
    {
        if (stack.Collectible is not BlockToolMold) return;
        Core.MaxFuelBurnTemp ??= allStacks
            .Where(s => s.Collectible.CombustibleProps?.BurnTemperature > 0)
            .OrderByDescending(s => s.Collectible.CombustibleProps.BurnTemperature)
            .FirstOrDefault()?.Collectible.CombustibleProps?.BurnTemperature ?? 0;

        var mold = stack.Collectible;
        var requiredUnits = mold.Attributes["requiredUnits"].AsInt();
        mold.Attributes["drop"].AsObject(new JsonItemStack(), mold.Code.Domain);
        var castStacks = StacksFromCode(capi, stack, out var existingMetalVariants);
        // Use linq search to find all metal bit stacks
        var metalBitStacks =
            Core.MetalBitStacksCache ??=
                allStacks.Where(s =>
                s.Collectible.Code.Path.Contains("metalbit") &&
                Enumerable.Contains(existingMetalVariants, s.Collectible.LastCodePart()) &&
                s.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack != null &&
                s.Collectible.CombustibleProps.SmeltingType == EnumSmeltType.Smelt &&
                s.Collectible.CombustibleProps.MeltingPoint <= Core.MaxFuelBurnTemp
        ).ToArray();
        var castableMetalVariants =
            Core.CastableMetalVariantsCache ??=
                metalBitStacks.Select(s => s.Collectible.Variant["metal"]).ToArray();
        
        castStacks = castStacks.Where(s => castableMetalVariants.Contains(s.Collectible.LastCodePart())).ToArray();

        var haveText = components.Count > 0;
        if (castStacks.Length > 0)
        {
            AddHeading(components, capi, "Mold for", ref haveText);
            var slideshowStack = new SlideshowItemstackTextComponent(capi, castStacks, 40, EnumFloat.Inline,
                    cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)))
                { PaddingLeft = 2 };
            components.Add(slideshowStack);
        }


        if (metalBitStacks.Length <= 0) return;
        {
            AddHeading(components, capi, "Requires for casting", ref haveText);
            // Group by everything except the last code part
            var groupedStacks = metalBitStacks
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
                    s.StackSize =
                        (int)Math.Ceiling(requiredUnits /
                                          (100f / (s.Collectible.CombustibleProps?.SmeltedRatio ?? 5))));
                var slideshowMetalBits = new SlideshowItemstackTextComponent(capi, stacksInGroup, 40, EnumFloat.Inline,
                        cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)))
                    { ShowStackSize = true, PaddingLeft = 2 };
                components.Add(slideshowMetalBits);
            }
        }
    }
}