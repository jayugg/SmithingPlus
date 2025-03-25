using System;
using HarmonyLib;
using SmithingPlus.Util;
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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemIronBloom), "CreateVoxelsFromIronBloom")]
    public static bool Prefix(ref byte[,,] voxels, ItemIronBloom __instance)
    {
        // Get api private field with reflection
        var api = __instance.GetField<ICoreAPI>("api");
        if (api == null) return true;
        ItemIngot.CreateVoxelsFromIngot(api, ref voxels);
        var removedMaterialCount = 0;
        var rand = api.World.Rand;

        ProcessVoxels(ref voxels, ref removedMaterialCount, rand);

        while (removedMaterialCount > 0) ProcessVoxels(ref voxels, ref removedMaterialCount, rand);
        return false;
    }

    private static void ProcessVoxels(ref byte[,,] voxels, ref int removedMaterialCount, Random rand)
    {
        for (var index1 = -1; index1 < 8; ++index1)
        for (var index2 = 0; index2 < 5; ++index2)
        for (var index3 = -1; index3 < 5; ++index3)
        {
            var index4 = 4 + index1;
            var index5 = 6 + index3;
            if (index2 == 0 && voxels[index4, index2, index5] == 1) continue;
            var num = Math.Max(0, Math.Abs(index4 - 7) - 1) + Math.Max(0, Math.Abs(index5 - 8) - 1) +
                      Math.Max(0, index2 - 1f);
            if (!(rand.NextDouble() >= num / 3.0 - 0.4 + (index2 - 1.5) / 4.0)) continue;
            if (rand.NextDouble() <= num / 2.0)
            {
                if (voxels[index4, index2, index5] == 1) removedMaterialCount++;
                voxels[index4, index2, index5] = 2;
            }
            else
            {
                if (voxels[index4, index2, index5] != 1) removedMaterialCount--;
                voxels[index4, index2, index5] = 1;
            }
        }
    }
}