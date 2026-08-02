using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using SmithingPlus.Common.Metal;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace SmithingPlus.ToolRecovery;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[HarmonyPatch(typeof(CollectibleObject))]
[HarmonyPatchCategory(Core.ToolRecoveryCategory)]
public class ItemDamagedPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CollectibleObject.OnCreatedByCrafting))]
    [HarmonyPriority(-int.MaxValue)]
    public static void Postfix_OnCreatedByCrafting(
        ItemSlot[] allInputSlots,
        ItemSlot outputSlot,
        IRecipeBase byRecipe)
    {
        if (outputSlot.Itemstack == null) return;
        var brokenStack = allInputSlots.FirstOrDefault(slot =>
            slot.Itemstack?.GetBrokenCount() > 0 &&
            slot.Itemstack?.Collectible.HasBehavior<CollectibleBehaviorRepairableToolHead>() == true
        )?.Itemstack;
        if (brokenStack == null) return;
        var brokenCount = brokenStack.GetBrokenCount();
        if (brokenCount <= 0) return;
        if (brokenStack.Item?.IsRepairableTool() is not true) return;
        var repairedStack = brokenStack.GetRepairedToolStack();
        if (repairedStack == null) return;
        repairedStack.ResolveBlockOrItem((allInputSlots.FirstOrDefault()?.Inventory?.Api ?? Core.Api)
            .World);
        if (repairedStack.Collectible.Code != byRecipe.RecipeOutput.ResolvedItemStack?.Collectible.Code) return;
        foreach (var attributeKey in Core.Config.GetToolRepairForgettableAttributes)
            repairedStack.Attributes?.RemoveAttribute(attributeKey);
        var repairSmith = brokenStack.GetRepairSmith();
        if (repairSmith != null) repairedStack.SetRepairSmith(repairSmith);
        var smithingQuality = brokenStack.GetSmithingQuality();
        if (smithingQuality != 0) repairedStack.SetSmithingQuality(smithingQuality);
        var toolRepairPenaltyModifier = brokenStack.GetToolRepairPenaltyModifier();
        if (toolRepairPenaltyModifier != 0)
            repairedStack.SetToolRepairPenaltyModifier(toolRepairPenaltyModifier);
        var repairedAttributes = repairedStack.Attributes ?? new TreeAttribute();
        var outputAttributes = outputSlot.Itemstack.Attributes;
        foreach (var attribute in repairedAttributes)
            outputAttributes[attribute.Key] = attribute.Value;
    }
}
