using HarmonyLib;
using JetBrains.Annotations;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.ToolRecovery;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[HarmonyPatch(typeof (ItemWorkItem))]
[HarmonyPatchCategory(Core.ToolRecoveryCategory)]
public class ItemWorkItemPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ItemWorkItem.TryPlaceOn))]
    public static void Postfix_CanWork(ItemStack stack, BlockEntityAnvil beAnvil, ref ItemStack __result)
    {
        if (__result == null || !Core.Config.DontRepairBrokenToolHeads) return;
        if (stack?.Collectible?.HasBehavior<CollectibleBehaviorRepairableToolHead>() != true) return;
        if (CollectibleBehaviorBrokenToolHead.IsBrokenToolHead(stack)) {__result = null;}
    }
    
}