using System.Collections.Generic;
using Cairo;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace SmithingPlus.ClientTweaks;

[HarmonyPatchCategory(Core.ClientTweaksCategories.HandbookExtraInfo)]
public partial class HandbookInfoPatch
{
    public static ItemStack[] StacksFromCode(ICoreClientAPI capi, ItemStack moldStack,
        out List<string> existingMetalVariants)
    {
        existingMetalVariants = new List<string>();
        var stacks = new List<ItemStack>();
        foreach (var metalVariant in capi.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.Keys)
        {
            var stack = GetStackForVariant(capi, moldStack, metalVariant);
            if (stack == null) continue;
            stacks.Add(stack);
            existingMetalVariants.Add(metalVariant);
        }

        return stacks.ToArray();
    }

    private static ItemStack GetStackForVariant(ICoreClientAPI capi, ItemStack moldStack, string metalVariant)
    {
        var mold = moldStack.Collectible;
        var jstack = mold.Attributes["drop"]?.AsObject<JsonItemStack>(null, mold.Code.Domain).Clone();
        var toolVariant = mold.LastCodePart();
        jstack.Code.Path = jstack.Code.Path.Replace("{tooltype}", toolVariant).Replace("{metal}", metalVariant);
        jstack.Resolve(capi.World, "tool mold drop for " + mold.Code, false);
        return jstack.ResolvedItemstack;
    }

    public static string ToolMoldType(CollectibleObject mold)
    {
        var jstack = mold.Attributes["drop"].AsObject<JsonItemStack>(null, mold.Code.Domain);
        return jstack.Code.Path.Contains("{tooltype}") ? mold.LastCodePart() : jstack.Code.FirstCodePart();
    }

    public static void AddHeading(
        List<RichTextComponentBase> components,
        ICoreClientAPI capi,
        string heading,
        ref bool haveText)
    {
        if (haveText)
            components.Add(new ClearFloatTextComponent(capi, 14f));
        haveText = true;
        var richTextComponent = new RichTextComponent(capi, Lang.Get(heading) + "\n",
            CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold));
        components.Add(richTextComponent);
    }

    public static void AddSubHeading(
        List<RichTextComponentBase> components,
        ICoreClientAPI capi,
        ActionConsumable<string> openDetailPageFor,
        string subheading,
        string detailpage)
    {
        if (detailpage == null)
        {
            var richTextComponent =
                new RichTextComponent(capi, "• " + Lang.Get(subheading) + "\n", CairoFont.WhiteSmallText())
                {
                    PaddingLeft = 2.0
                };
            components.Add(richTextComponent);
        }
        else
        {
            var richTextComponent = new RichTextComponent(capi, "• ", CairoFont.WhiteSmallText())
            {
                PaddingLeft = 2.0
            };
            components.Add(richTextComponent);
            components.Add(new LinkTextComponent(capi, Lang.Get(subheading) + "\n", CairoFont.WhiteSmallText(),
                cs => _ = openDetailPageFor(detailpage) ? 1 : 0));
        }
    }
}