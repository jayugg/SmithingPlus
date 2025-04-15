using System;
using HarmonyLib;
using JetBrains.Annotations;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace SmithingPlus.ExtraTweaks;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[HarmonyPatch]
[HarmonyPatchCategory(Core.AlwaysPatchCategory)]
public static class ForgeEventTransformPatch
{
    private const string EventId = "genjsontransform";

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockEntityForge), nameof(BlockEntityForge.Initialize))]
    public static void Initialize_Postfix(ICoreAPI api, ForgeContentsRenderer ___renderer)
    {
        api.Event.RegisterEventBusListener(
            (string _, ref EnumHandling _, IAttribute _) =>
                ___renderer.RegenMesh(), filterByEventName: EventId);
    }

    private static void RegenMesh(this ForgeContentsRenderer renderer)
    {
        RegenMesh_Reverse(renderer);
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(ForgeContentsRenderer), "RegenMesh")]
    public static void RegenMesh_Reverse(ForgeContentsRenderer __instance)
    {
        throw new NotImplementedException("Reverse patch stub");
    }
}