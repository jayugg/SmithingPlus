using System;
using System.Linq;
using HarmonyLib;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace SmithingPlus.CastingTweaks;

[HarmonyPatchCategory(Core.CastingTweaksCategory)]
public class CastToolPenaltyPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockEntityToolMold), nameof(BlockEntityToolMold.GetMoldedStacks))]
    public static void Postfix_GetMoldedStacks(ref ItemStack[] __result, BlockEntityToolMold __instance, ItemStack fromMetal)
    {
        if (__result == null || __result.Length == 0)
            return;
        foreach (var stack in __result)
        {
            stack.Attributes ??= new TreeAttribute();
            stack.Attributes.SetBool(ModAttributes.CastTool, true);
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.OnCreatedByCrafting)), HarmonyPriority(Priority.Last)]
    public static void Postfix_OnCreatedByCrafting(
        ItemSlot[] allInputslots,
        ItemSlot outputSlot,
        GridRecipe byRecipe)
    {
        if (byRecipe is not {resolvedIngredients: not null} ||
            allInputslots == null ||
            outputSlot.Itemstack == null)
            return;
        var castTools = allInputslots.Where(slot =>
            !slot.Empty &&
            slot.Itemstack.Attributes?.GetBool(ModAttributes.CastTool) == true
        ).Select(slot => slot.Itemstack).ToArray();
        if (!castTools.Any()) return;
        //Core.Logger.VerboseDebug("Found cast tools in recipe");
        var nonToolsInRecipe = byRecipe.resolvedIngredients.Where(ing => !ing.IsTool).ToArray();
        //Core.Logger.VerboseDebug($"Non tools in recipe: {nonToolsInRecipe.Length}");
        var castToolHeads = castTools.Where(stack =>
            nonToolsInRecipe.Any(ing => ing.ResolvedItemstack.Satisfies(stack))
        ).ToArray();
        //Core.Logger.VerboseDebug($"Cast tool heads: {castToolHeads.Length}");
        var hasCastToolHead = castToolHeads.Any();
        if (!hasCastToolHead) return;
        outputSlot.Itemstack.Attributes ??= new TreeAttribute();
        outputSlot.Itemstack.Attributes.SetBool(ModAttributes.CastTool, true);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetMaxDurability)), HarmonyPriority(Priority.Last)]
    public static void Postfix_GetMaxDurability(ref int __result, ItemStack itemstack)
    {
        if (itemstack.Attributes?.GetBool(ModAttributes.CastTool) != true) return;
        var reducedDurability = __result * (1 - Core.Config.CastToolDurabilityPenalty);
        __result = (int) Math.Max(reducedDurability, 1);
    }
}