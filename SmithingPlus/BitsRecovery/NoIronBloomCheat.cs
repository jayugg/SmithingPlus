using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.BitsRecovery;

[HarmonyPatchCategory(Core.BitsRecoveryCategory)]
[HarmonyPatch]
public class NoIronBloomCheat
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemIronBloom), nameof(ItemIronBloom.GetHelveWorkableMode))]
    public static void Postfix(ref EnumHelveWorkableMode __result, ItemStack stack, BlockEntityAnvil beAnvil)
    {
        __result = EnumHelveWorkableMode.TestSufficientVoxelsWorkable;
    }
}