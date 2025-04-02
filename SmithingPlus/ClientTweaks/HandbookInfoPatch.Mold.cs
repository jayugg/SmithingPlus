using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus.ClientTweaks;

public partial class HandbookInfoPatch
{
    public static float? MaxFuelTemp { get; set; }

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
        // Find where the "Metal molding" section is in the components list
        var moldingSectionIndex = -1;
        for (var i = 0; i < components.Count; i++)
        {
            if (components[i] is not LinkTextComponent linkComponent) continue;
            var isMoldingHeader =
                linkComponent.DisplayText != null && linkComponent.DisplayText.Contains(Lang.Get("Metal molding"));

            if (!isMoldingHeader || i + 1 >= components.Count) continue;
            moldingSectionIndex = i + 1;
            break;
        }

        var moldStacks = allStacks.Where(s =>
                s.Collectible is BlockToolMold &&
                stack.Collectible.FirstCodePart().Equals(ToolMoldType(s.Collectible)))
            .OrderBy(s => s.Collectible.Code.Domain == "game" ? -100 : 0)
            .ThenBy(s => s.ItemAttributes["requiredUnits"].AsInt())
            .ToArray();
        if (moldStacks.Length <= 0) return;
        var moldingSectionExists = moldingSectionIndex >= 0;
        if (!moldingSectionExists)
        {
            AddSubHeading(components, capi, openDetailPageFor,
                $"{Lang.Get("Metal molding")} {Lang.Get("with")}\n",
                "craftinginfo-smelting");
            components.Add(new ClearFloatTextComponent(capi, 2f));
            var slideshowMolds = new SlideshowItemstackTextComponent(capi, moldStacks.ToArray(), 40, EnumFloat.Inline,
                    cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)))
                { PaddingLeft = 2 };
            components.Add(slideshowMolds);
            components.Add(new ClearFloatTextComponent(capi, 2f));
        }
        else
        {
            components.RemoveAt(moldingSectionIndex - 1);
            components.Insert(moldingSectionIndex - 1,
                new LinkTextComponent(capi, $"{Lang.Get("Metal molding")} {Lang.Get("with")}\n",
                    CairoFont.WhiteSmallText(),
                    _ => openDetailPageFor("craftinginfo-smelting")));
            components.Insert(moldingSectionIndex, new ClearFloatTextComponent(capi, 2f));
            var slideshowMolds = new SlideshowItemstackTextComponent(capi, moldStacks.ToArray(), 40,
                    EnumFloat.Inline,
                    cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)))
                { PaddingLeft = 2 };
            components.Insert(moldingSectionIndex + 1, slideshowMolds);
            components.Insert(moldingSectionIndex + 2, new ClearFloatTextComponent(capi, 2f));
        }
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
        MaxFuelTemp ??= allStacks
            .Where(s => s.Collectible.CombustibleProps?.BurnTemperature > 0)
            .OrderByDescending(s => s.Collectible.CombustibleProps.BurnTemperature)
            .FirstOrDefault()?.Collectible.CombustibleProps?.BurnTemperature ?? 0;

        var mold = stack.Collectible;
        var requiredUnits = mold.Attributes["requiredUnits"].AsInt();
        mold.Attributes["drop"].AsObject<JsonItemStack>(null, mold.Code.Domain);
        var castStacks = StacksFromCode(capi, stack, out var existingMetalVariants);
        // Use linq search to find all metal bit stacks
        var metalBitStacks = allStacks.Where(s =>
            s.Collectible.Code.Path.Contains("metalbit") &&
            Enumerable.Contains(existingMetalVariants, s.Collectible.LastCodePart()) &&
            s.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack != null &&
            s.Collectible.CombustibleProps.SmeltingType == EnumSmeltType.Smelt &&
            s.Collectible.CombustibleProps.MeltingPoint <= MaxFuelTemp
        ).ToArray();
        var castableMetalVariants = metalBitStacks.Select(s => s.Collectible.Variant["metal"]).ToArray();
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