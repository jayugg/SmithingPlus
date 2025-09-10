using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using Vintagestory.GameContent;

namespace SmithingPlus.AdjustedCastingMetalRequirements;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[HarmonyPatchCategory(Core.AdjustedCastingMetalRequirementsCategory)]
public static class CastingMetalRequirementsPatch
{
    private static readonly Dictionary<string, int> AdjustedRequiredUnits = new()
    {
        { "axe", 90 },
        { "pickaxe", 60 },
        { "shovel", 90 },
        { "hammer", 100 },
        { "hoe", 85 },
        { "prospectingpick", 40 },
        { "helvehammer", 190 },
        { "blade-falx", 100 }
    };

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockEntityToolMold), nameof(BlockEntityToolMold.Initialize))]
    public static void Postfix_Initialize(BlockEntityToolMold __instance)
    {
        string itemType = __instance.Block?.Variant?["tooltype"];
        if (itemType != null && AdjustedRequiredUnits.TryGetValue(itemType, out int adjustedUnits))
        {
            var requiredUnitsfield = AccessTools.Field(typeof(BlockEntityToolMold), "requiredUnits");
            requiredUnitsfield?.SetValue(__instance, adjustedUnits);
        }
    }
}
