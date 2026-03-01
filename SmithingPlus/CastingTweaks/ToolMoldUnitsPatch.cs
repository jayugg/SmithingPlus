#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace SmithingPlus.CastingTweaks;

[HarmonyPatchCategory(Core.DynamicMoldsCategory)]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[HarmonyPatch(typeof(BlockEntityToolMold))]
[HarmonyPriority(Priority.Last)]
public class ToolMoldUnitsPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(BlockEntityToolMold.Initialize))]
    public static void Initialize_Postfix(BlockEntityToolMold __instance, ref int ___requiredUnits, ICoreAPI api)
    {
        // Assume copper stack as metal for unit calculation,
        // This means the patch will only apply for standard molds!
        // Either vanilla or vanilla-like ones
        // It also means that having different smithing recipes
        // for different metals will cause unexpected imbalances
        var copperIngot = api.World.GetItem(new AssetLocation("game:ingot-copper"));
        if (copperIngot == null)
            return;
        var copperStack = new ItemStack(copperIngot);
        var requiredUnitsRounded = GetPatchedRequiredUnits(__instance.Api, __instance.Block, copperStack);
        if (requiredUnitsRounded == 0)
            return;
        ___requiredUnits = requiredUnitsRounded;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(BlockEntityToolMold.ToTreeAttributes))]
    public static void ToTreeAttributes_Postfix(BlockEntityToolMold __instance, int ___requiredUnits,
        ITreeAttribute tree)
    {
        tree.SetInt("requiredUnits", ___requiredUnits);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(BlockEntityToolMold.FromTreeAttributes))]
    public static void FromTreeAttributes_Postfix(ITreeAttribute tree, ref int ___requiredUnits,
        IWorldAccessor worldForResolve)
    {
        ___requiredUnits = tree.GetInt("requiredUnits");
    }

    public static int GetPatchedRequiredUnits(ICoreAPI api, Block toolMold, ItemStack fromMetal)
    {
        var dropStacks = GetMoldedStacksStatic(api, toolMold, fromMetal);
        if (dropStacks.Length == 0)
            return
                toolMold.Attributes["requiredUnits"]
                    .AsInt(); // <-- Patch will only apply for molds that work for copper!
        var voxelCount = VoxelCountForStacks(api, dropStacks);
        if (voxelCount is null or 0)
            return toolMold.Attributes["requiredUnits"].AsInt();
        // These are all assumptions that have to be made, should implement warnings if weird values are found
        const float voxelsPerIngot = 42f;
        const float unitsPerIngot = 100f;
        const float unitsPerVoxel = unitsPerIngot / voxelsPerIngot;
        // Round to lowest 5 units to avoid annoying numbers and making players sad
        return (int)MathF.Floor(voxelCount.Value * unitsPerVoxel / 5) * 5;
    }

    public static int? VoxelCountForStacks(ICoreAPI api, ItemStack[] smithedItemStacks)
    {
        var voxelCounts = smithedItemStacks.Select(stack =>
            VoxelCountForStack(api, stack)).ToArray();
        return voxelCounts.All(count => count == null) ? null : voxelCounts.Sum(count => count ?? 0);
    }

    private static int? VoxelCountForStack(ICoreAPI api, ItemStack stack)
    {
        var cheapestRecipe = stack.GetCheapestSmithingRecipe(api);
        if (cheapestRecipe == null) return null;
        var cheapestOutput = cheapestRecipe.Output.ResolvedItemstack.StackSize;
        var recipeMaterialVoxels = cheapestRecipe.Voxels.VoxelCount();
        var voxelsPerItem = Math.Max(recipeMaterialVoxels / cheapestOutput, 0);
        return voxelsPerItem * stack.StackSize;
    }

    private static ItemStack[] GetMoldedStacksStatic(ICoreAPI api, Block toolMold, ItemStack fromMetal)
    {
        // why try catch? vanilla code does this...
        try
        {
            if (toolMold.Attributes["drop"].Exists)
            {
                var jStack =
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    toolMold.Attributes["drop"].AsObject<JsonItemStack>(null, toolMold.Code.Domain);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                if (jStack == null)
                    return [];
                var itemStack = MoldOutputStackFromCode(jStack, api, toolMold, fromMetal);
                return itemStack == null ? [] : [itemStack];
            }

            var jsonItemStackArray =
                toolMold.Attributes["drops"].AsObject<JsonItemStack[]>([], toolMold.Code.Domain);
            var itemStackList = new List<ItemStack>();
            foreach (var jStack in jsonItemStackArray)
            {
                var itemStack = MoldOutputStackFromCode(jStack, api, toolMold, fromMetal);
                if (itemStack != null)
                    itemStackList.Add(itemStack);
            }

            return itemStackList.ToArray();
        }
        catch (JsonReaderException ex)
        {
            api.World.Logger.Error("Failed getting molded stacks from tool mold of block {0}, " +
                                   "probably unable to parse drop or drops attribute", toolMold.Code);
            api.World.Logger.Error(ex);
            throw;
        }
    }

    private static ItemStack? MoldOutputStackFromCode(JsonItemStack jstack, ICoreAPI api, Block toolMold,
        ItemStack fromMetal)
    {
        var newValue = fromMetal.Collectible.LastCodePart();
        jstack.Code.Path = jstack.Code.Path.Replace("{metal}", newValue);
        jstack.Resolve(api.World, "tool mold drop for " + toolMold.Code);
        return jstack.ResolvedItemstack;
    }
}