using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.ToolRecovery;

[HarmonyPatch]
[HarmonyPatchCategory(Core.ToolRecoveryCategory)]
public class ToolHeadRepairPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetMaxDurability)), HarmonyPriority(-1)]
    public static void Postfix_GetMaxDurability(ref int __result, ItemStack itemstack)
    {
        var brokenCount = itemstack.GetBrokenCount();
        if (brokenCount <= 0) return;
        if (itemstack.Attributes.HasAttribute("maxdurability"))
        {
            __result = itemstack.Attributes.GetInt("maxdurability");
        }
        var reducedDurability = (int) (__result * (1 - brokenCount * Core.Config.DurabilityPenaltyPerRepair));
        __result = Math.Max(reducedDurability, 1);
    }

    public static void ModifyBrokenCount(BlockEntityAnvil instance, ItemStack itemstack)
    {
        Core.Logger.VerboseDebug("ModifyBrokenCount: {0} by {1}", itemstack.Collectible.Code, instance.WorkItemStack);
        itemstack.CloneBrokenCount(instance.WorkItemStack);
        itemstack.CloneRepairedToolStack(instance.WorkItemStack);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BlockEntityAnvil), nameof(BlockEntityAnvil.CheckIfFinished))]
    public static IEnumerable<CodeInstruction> Transpiler_CheckIfFinished(IEnumerable<CodeInstruction> instructions)
    {
        Core.Logger.VerboseDebug("Starting Transpiler for BlockEntityAnvil.CheckIfFinished");

        var codes = new List<CodeInstruction>(instructions);
        var targetMethod = AccessTools.Method(typeof(CollectibleObject), nameof(CollectibleObject.SetTemperature));

        Core.Logger.VerboseDebug("Target method: {0}", targetMethod);

        for (int i = 0; i < codes.Count; i++)
        {
            yield return codes[i];

            // Insert custom processing after SetTemperature call
            if (codes[i].opcode != OpCodes.Callvirt || (MethodInfo)codes[i].operand != targetMethod) continue;
            Core.Logger.VerboseDebug("Found target method call at index {0}", i);
            yield return new CodeInstruction(OpCodes.Ldarg_0); // Load 'this' (instance)
            yield return new CodeInstruction(OpCodes.Ldloc_0); // Load itemstack (local variable at index 0)
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ToolHeadRepairPatches), nameof(ModifyBrokenCount)));
        }
    }
}