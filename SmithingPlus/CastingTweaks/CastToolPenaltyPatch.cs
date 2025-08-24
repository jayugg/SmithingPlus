using System;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace SmithingPlus.CastingTweaks;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[HarmonyPatchCategory(Core.CastingTweaksCategory)]
public class CastToolPenaltyPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockEntityToolMold), nameof(BlockEntityToolMold.GetMoldedStacks))]
    public static void Postfix_GetMoldedStacks(ref ItemStack[] __result, BlockEntityToolMold __instance,
        ItemStack fromMetal)
    {
        if (__result == null || __result.Length == 0)
            return;
        foreach (var stack in __result)
        {
            if (!stack.Collectible.HasBehavior<CollectibleBehaviorCastToolHead>()) continue;
            stack.Attributes ??= new TreeAttribute();
            stack.Attributes.SetBool(ModAttributes.CastTool, true);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.OnCreatedByCrafting))]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix_OnCreatedByCrafting(
        ItemSlot[] allInputslots,
        ItemSlot outputSlot,
        GridRecipe byRecipe)
    {
        if (outputSlot.Itemstack == null)
            return;
        var castToolsHeads = allInputslots
            .Where(slot => !slot.Empty)
            .Select(slot => slot.Itemstack)
            .Where(stack =>
                stack.Attributes?.GetBool(ModAttributes.CastTool) == true &&
                stack.Collectible.GetMaxDurability(stack) == 1)
            .ToArray();
        var hasCastToolHead = castToolsHeads.Any();
        if (!hasCastToolHead) return;
        outputSlot.Itemstack.Attributes ??= new TreeAttribute();
        outputSlot.Itemstack.Attributes.SetBool(ModAttributes.CastTool, true);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetMaxDurability))]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix_GetMaxDurability(ref int __result, ItemStack itemstack)
    {
        if (itemstack.Attributes?.GetBool(ModAttributes.CastTool) != true) return;
        var reducedDurability = __result * (1 - Core.Config.CastToolDurabilityPenalty);
        __result = (int)Math.Max(reducedDurability, 1);
    }
}