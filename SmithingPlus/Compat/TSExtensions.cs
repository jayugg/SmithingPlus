#nullable enable
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace SmithingPlus.Compat;

internal static class XBlockEntityAnvil
{
    private static readonly string WorkDataAttrCode = $"ef:ts:workData:{Core.ModId}";

    internal static void SetCustomWorkData(this ItemStack workItemStack, int? voxels = null, int? ingots = null,
        int? plates = null)
    {
        workItemStack.Attributes[WorkDataAttrCode] = new TreeAttribute
        {
            ["voxels"] = new IntAttribute(voxels ?? 0),
            ["ingots"] = new IntAttribute(ingots ?? 0),
            ["plates"] = new IntAttribute(plates ?? 0)
        };
    }

    private static TreeAttribute? GetCustomWorkData(ItemStack workItemStack)
    {
        return workItemStack.Attributes[WorkDataAttrCode] as TreeAttribute;
    }

    internal static void AddToCustomWorkData(this ItemStack workItemStack, int? voxels = null, int? ingots = null,
        int? plates = null)
    {
        var workData = GetCustomWorkData(workItemStack) ?? new TreeAttribute();
        workItemStack.SetCustomWorkData(
            voxels + workData.GetInt("voxels"),
            ingots + workData.GetInt("ingots"),
            plates + workData.GetInt("plates")
        );
    }
}