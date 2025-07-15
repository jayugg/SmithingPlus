#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using SmithingPlus.Metal;
using SmithingPlus.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus.CastingTweaks;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[HarmonyPatch]
[HarmonyPatchCategory(Core.CastingTweaksCategory)]
public class AnvilWorkablePatches
{
    private static MethodInfo _getCollectibleInterfaceMethod = AccessTools.Method(
        typeof(CollectibleObject),
        "GetCollectibleInterface"
    ).MakeGenericMethod(typeof(IAnvilWorkable));

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BlockEntityAnvil), "get_CanWorkCurrent")]
    public static IEnumerable<CodeInstruction> get_CanWorkCurrent_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return PatchIsinstIAnvilWorkable(instructions);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BlockEntityAnvil), "TryPut")]
    public static IEnumerable<CodeInstruction> TryPut_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return PatchIsinstIAnvilWorkable(instructions);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BlockEntityAnvil), "PrintDebugText")]
    public static IEnumerable<CodeInstruction> PrintDebugText_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return PatchIsinstIAnvilWorkable(instructions);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BlockEntityAnvil), nameof(BlockEntityAnvil.OnHelveHammerHit))]
    public static IEnumerable<CodeInstruction> OnHelveHammerHit_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return PatchIsinstIAnvilWorkable(instructions);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BlockEntityAnvil), nameof(BlockEntityAnvil.ditchWorkItemStack))]
    public static IEnumerable<CodeInstruction> ditchWorkItemStack_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return PatchIsinstIAnvilWorkable(instructions);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BlockEntityAnvil), "OpenDialog")]
    public static IEnumerable<CodeInstruction> OpenDialog_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return PatchIsinstIAnvilWorkable(instructions);
    }

    private static IEnumerable<CodeInstruction> PatchIsinstIAnvilWorkable(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var code in instructions)
        {
            // When encountering the isinst check for IAnvilWorkable,
            // insert a call to GetCollectibleInterface<IAnvilWorkable>() before continuing.
            if (code.opcode == OpCodes.Isinst &&
                code.operand is Type typeOperand &&
                typeOperand == typeof(IAnvilWorkable))
                yield return new CodeInstruction(OpCodes.Call, _getCollectibleInterfaceMethod);
            yield return code;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockAnvil), nameof(BlockAnvil.OnLoaded))]
    public static void OnLoaded_Postfix(BlockAnvil __instance, WorldInteraction[] ___interactions, ICoreAPI ___api)
    {
        if (___api is not ICoreClientAPI capi)
            return;
        var metalVariant = __instance.GetMetalVariant();
        var metalTier = ___api.GetModSystem<SurvivalCoreSystem>()?.metalsByCode
            .TryGetValue(metalVariant, out var metalProperty) == true
            ? metalProperty?.Tier ?? 0
            : 0;
        var workableStackList = capi.World.Items.Where(i => i.Code != null && i is ItemIngot)
            .Select(i => new ItemStack(i)).ToList();
        var badInteraction = ___interactions.FirstOrDefault(i => i.ActionLangCode == "blockhelp-anvil-addvoxels");
        if (badInteraction == null) return;
        var newInteraction = ObjectCacheUtil.GetOrCreate(
            capi,
            "sp:anvilBlockInteractions" + metalTier,
            () => CreateAnvilInteraction(capi, workableStackList));
        ___interactions[Array.IndexOf(___interactions, badInteraction)] = newInteraction;
    }

    private static WorldInteraction CreateAnvilInteraction(ICoreClientAPI api,
        List<ItemStack> workableStackList)
    {
        return new WorldInteraction
        {
            ActionLangCode = "blockhelp-anvil-addvoxels",
            HotKeyCode = "shift",
            MouseButton = EnumMouseButton.Right,
            Itemstacks = workableStackList.ToArray(),
            GetMatchingStacks = (_, blockSelection, _) => GetAnvilMatchingStacks(api, blockSelection)
        };
    }

    private static ItemStack[]? GetAnvilMatchingStacks(ICoreClientAPI api, BlockSelection blockSelection)
    {
        var blockEntityAnvil = api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) as BlockEntityAnvil;
        if (blockEntityAnvil?.WorkItemStack == null) return null;
        return new[]
        {
            blockEntityAnvil.WorkItemStack.Collectible
                .GetCollectibleInterface<IAnvilWorkable>()
                .GetBaseMaterial(blockEntityAnvil.WorkItemStack)
        };
    }
}