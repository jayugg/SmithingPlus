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
    [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetMaxDurability)), HarmonyPriority(-int.MaxValue)]
    public static void Postfix_GetMaxDurability(ref int __result, ItemStack itemstack)
    {
        if (!itemstack.Collectible.HasBehavior<CollectibleBehaviorRepairableTool>()) return;
        var brokenCount = itemstack.GetBrokenCount();
        if (brokenCount < 0) return;
        var multiplier = Core.Config.RepairableToolDurabilityMultiplier * itemstack.Attributes.GetFloat("sp:smithingQuality", 1);
        var toolRepairPenaltyReduction = itemstack.Attributes.GetFloat("sp:toolRepairPenaltyReduction");
        var toolRepairPenalty = brokenCount * Core.Config.DurabilityPenaltyPerRepair * (1 - toolRepairPenaltyReduction);
        var reducedDurability = (int) (__result * multiplier * (1 - toolRepairPenalty));
        if (itemstack.Attributes.HasAttribute("durability"))
        {
            var durability = itemstack.Attributes.GetInt("durability");
            itemstack.SetDurability(Math.Min(durability, reducedDurability));
        }
        __result = Math.Max(reducedDurability, 1);
    }

    public static void ModifyBrokenCount(BlockEntityAnvil instance, ItemStack itemstack, IPlayer byPlayer)
    {
        var smithingQuality = byPlayer.Entity.Stats.GetBlended("sp:smithingQuality");
        if (Math.Abs(smithingQuality - 1) > 1E-3)
        {
            itemstack.Attributes.SetFloat("sp:smithingQuality", smithingQuality);
        }
        Core.Logger.VerboseDebug("ModifyBrokenCount: {0} by {1}", itemstack.Collectible.Code, instance.WorkItemStack);
        if (instance.WorkItemStack.GetBrokenCount() == 0) return;
        itemstack.CloneBrokenCount(instance.WorkItemStack);
        itemstack.CloneRepairedToolStack(instance.WorkItemStack);
        itemstack.SetRepairSmith(byPlayer.PlayerName);
        var toolRepairPenaltyStat = byPlayer.Entity.Stats.GetBlended("sp:toolRepairPenalty");
        if (Math.Abs(toolRepairPenaltyStat - 1) > 1E-3)
        {
            itemstack.Attributes.SetFloat("sp:toolRepairPenaltyModifier", (float) Math.Round(toolRepairPenaltyStat - 1, 3));
        }
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
            yield return new CodeInstruction(OpCodes.Ldarg_1); // Load player (first argument)
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ToolHeadRepairPatches), nameof(ModifyBrokenCount)));
        }
    }
}